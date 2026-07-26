using Godot;
using System.Collections.Generic;

namespace RPG.UI;

// Leistungsanzeige mit Schaltern zum Eingrenzen von Flaschenhälsen.
//
//   F3  Anzeige ein/aus          F5  Gras ein/aus
//   F4  V-Sync ein/aus           F6  Bäume ein/aus
//
// Warum Millisekunden statt Prozent: Godot kann die Auslastung von CPU und GPU nicht in Prozent
// messen - das weiß nur das Betriebssystem. Die pro Bild verbrauchte Zeit ist ohnehin die
// nützlichere Zahl, weil sie sich direkt gegen das Zeitbudget halten lässt:
//   60 FPS = 16,7 ms   ·   30 FPS = 33,3 ms
//
// ⚠️ Solange V-Sync an ist, sind FPS und CPU-Zeit kaum aussagekräftig: das Spiel wartet dann auf
// den Monitor, und diese Wartezeit steckt im gemessenen Prozess-Frame mit drin. Für eine echte
// Messung V-Sync mit F4 abschalten.
//
// Gras und Bäume lassen sich abschalten, um die Dreieckszahl zuzuordnen: Wert ablesen,
// umschalten, wieder ablesen - die Differenz ist der Anteil des jeweiligen Systems. Das ist
// verlässlicher als jede Schätzung.
public partial class PerformanceOverlay : CanvasLayer
{
	[Export] public Key ToggleKey = Key.F3;
	[Export] public Key VsyncKey = Key.F4;
	[Export] public Key GrassKey = Key.F5;
	[Export] public Key TreeKey = Key.F6;

	// Wie oft die Zahlen neu geschrieben werden. Jedes Bild wäre unlesbar und würde die
	// Messung selbst verfälschen.
	[Export] public float UpdateInterval = 0.25f;

	[Export] public bool VisibleOnStart = true;

	private Label _label = null!;
	private float _timer;

	private double _minFps = double.MaxValue;
	private double _worstFrameMs;

	private bool _grassVisible = true;
	private bool _treesVisible = true;
	private List<GeometryInstance3D>? _grassNodes;
	private List<GeometryInstance3D>? _treeNodes;

	public override void _Ready()
	{
		_label = GetNode<Label>("Panel/Margin/Label");
		Visible = VisibleOnStart;

		// Ohne das liefert die GPU-Zeit immer 0. Kostet minimal Leistung (Zeitstempel im
		// Renderer), ist aber der einzige Weg, die GPU-Last von innen zu sehen.
		RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(), true);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.Echo)
			return;

		if (key.Keycode == ToggleKey)
		{
			Visible = !Visible;
			if (Visible)
				ResetPeaks();
		}
		else if (key.Keycode == VsyncKey)
		{
			bool on = DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled;
			DisplayServer.WindowSetVsyncMode(on ? DisplayServer.VSyncMode.Disabled : DisplayServer.VSyncMode.Enabled);
			ResetPeaks();
		}
		else if (key.Keycode == GrassKey)
		{
			_grassVisible = !_grassVisible;
			SetVisible(GrassNodes(), _grassVisible);
		}
		else if (key.Keycode == TreeKey)
		{
			_treesVisible = !_treesVisible;
			SetVisible(TreeNodes(), _treesVisible);
		}
	}

	private void ResetPeaks()
	{
		_minFps = double.MaxValue;
		_worstFrameMs = 0;
	}

	private static void SetVisible(List<GeometryInstance3D> nodes, bool visible)
	{
		foreach (GeometryInstance3D node in nodes)
		{
			if (IsInstanceValid(node))
				node.Visible = visible;
		}
	}

	// Das Gras hängt am Terrain3DParticles-Knoten (addons/terrain_3d/extras/particle_example).
	private List<GeometryInstance3D> GrassNodes()
	{
		if (_grassNodes != null)
			return _grassNodes;

		_grassNodes = new List<GeometryInstance3D>();
		Node? root = GetTree().CurrentScene;
		if (root != null)
			CollectUnder(root, "Terrain3DParticles", _grassNodes);

		return _grassNodes;
	}

	// Bäume erzeugt der Terrain3D-Instancer als MultiMeshInstance3D unter dem Terrain-Knoten.
	// Erst zur Laufzeit vorhanden, deshalb wird die Liste beim ersten Umschalten aufgebaut.
	private List<GeometryInstance3D> TreeNodes()
	{
		if (_treeNodes != null)
			return _treeNodes;

		_treeNodes = new List<GeometryInstance3D>();
		Node? root = GetTree().CurrentScene;
		if (root != null)
			CollectMultiMeshes(root, _treeNodes);

		return _treeNodes;
	}

	private static void CollectUnder(Node node, string nameContains, List<GeometryInstance3D> into)
	{
		if (node.Name.ToString().Contains(nameContains))
		{
			CollectGeometry(node, into);
			return;
		}

		foreach (Node child in node.GetChildren())
			CollectUnder(child, nameContains, into);
	}

	private static void CollectGeometry(Node node, List<GeometryInstance3D> into)
	{
		if (node is GeometryInstance3D geometry)
			into.Add(geometry);

		foreach (Node child in node.GetChildren())
			CollectGeometry(child, into);
	}

	private static void CollectMultiMeshes(Node node, List<GeometryInstance3D> into)
	{
		if (node is MultiMeshInstance3D multiMesh)
			into.Add(multiMesh);

		foreach (Node child in node.GetChildren())
			CollectMultiMeshes(child, into);
	}

	public override void _Process(double delta)
	{
		double fps = Performance.GetMonitor(Performance.Monitor.TimeFps);
		double frameMs = delta * 1000.0;

		if (fps > 0 && fps < _minFps)
			_minFps = fps;
		if (frameMs > _worstFrameMs)
			_worstFrameMs = frameMs;

		if (!Visible)
			return;

		_timer += (float)delta;
		if (_timer < UpdateInterval)
			return;

		_timer = 0f;
		_label.Text = BuildText(fps, frameMs);
	}

	private string BuildText(double fps, double frameMs)
	{
		Rid viewport = GetViewport().GetViewportRid();
		double gpuMs = RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewport);
		double renderCpuMs = RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewport);

		double processMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
		double physicsMs = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0;

		double videoMem = Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed);
		double textureMem = Performance.GetMonitor(Performance.Monitor.RenderTextureMemUsed);
		double ram = Performance.GetMonitor(Performance.Monitor.MemoryStatic);

		double drawCalls = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
		double primitives = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);

		bool vsync = DisplayServer.WindowGetVsyncMode() != DisplayServer.VSyncMode.Disabled;

		// Flaschenhals über die GPU-Zeit gegen die echte Bildzeit bestimmen, NICHT über
		// "CPU Frame": dieser Godot-Wert misst den ganzen Prozess-Frame samt Wartezeiten und
		// ist dadurch fast immer der größere - die Anzeige stünde sonst dauerhaft auf "CPU".
		// Füllt die GPU die Bildzeit nahezu aus, wartet die CPU auf sie und sie ist der Bremser.
		string verdict;
		if (vsync)
			verdict = "⚠ V-Sync an — F4 für echte Werte";
		else if (gpuMs >= frameMs * 0.9)
			verdict = $"→ GPU-begrenzt ({gpuMs / frameMs:0%} der Bildzeit)";
		else if (drawCalls > 2000)
			verdict = $"→ CPU-begrenzt (viele Draw Calls)";
		else
			verdict = $"→ ausgeglichen (GPU {gpuMs / frameMs:0%})";

		return $"""
			{fps,6:0} FPS      {frameMs,6:0.0} ms/Bild
			   min {(_minFps == double.MaxValue ? 0 : _minFps),3:0}      Spitze {_worstFrameMs,5:0.0} ms
			Budget: 16,7 ms = 60 FPS · 33,3 = 30
			─────────────────────────────
			CPU  Frame    {processMs,7:0.00} ms{(vsync ? "  (+Warten)" : "")}
			     Physik   {physicsMs,7:0.00} ms
			     Render   {renderCpuMs,7:0.00} ms
			GPU  Render   {gpuMs,7:0.00} ms
			{verdict}
			─────────────────────────────
			VRAM gesamt   {Mb(videoMem),7:0.0} MB
			  Texturen    {Mb(textureMem),7:0.0} MB
			RAM           {Mb(ram),7:0.0} MB
			─────────────────────────────
			Draw Calls  {drawCalls,9:0}
			Dreiecke    {primitives,9:0}
			─────────────────────────────
			F4 V-Sync {(vsync ? "AN " : "AUS")}   F5 Gras {(_grassVisible ? "AN " : "AUS")}
			F6 Bäume  {(_treesVisible ? "AN" : "AUS")}    F3 schließt
			Dreiecke ablesen, umschalten, vergleichen
			""";
	}

	private static double Mb(double bytes) => bytes / 1024.0 / 1024.0;
}
