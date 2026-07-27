shader_type spatial;

// Shader fuer die Imposter-Kreuze der Baeume (erzeugt von scripts/Tools/generate_tree_imposters.gd).
//
// Die Geometrie sind ZWEI ueberkreuzte senkrechte Flaechen (von oben ein X), gebaut in
// _cross_mesh_arrays(). Der Shader hat drei Aufgaben:
//  1. Jeden Baum fest um die Hochachse drehen - zufaellig, aber immer gleich. FRUEHER war das
//     ein Billboard, das sich zur Kamera drehte; bei ueberkreuzten Flaechen ist das weder
//     noetig noch erwuenscht (alle Baeume standen dann gleich ausgerichtet da).
//  2. Aus dem Atlas (mehrere Ansichten desselben Baums in einem Bild) waehlt jede Instanz eine
//     Ansicht aus - anhand ihrer Weltposition. Dadurch sehen benachbarte Baeume verschieden
//     aus, ohne dass es dafuer mehrere Assets braucht. Die zweite Ebene des Kreuzes nimmt die
//     naechste Ansicht (90 Grad weiter), passend zu ihrer Ausrichtung.
//  3. Farbe/Helligkeit pro Baum streuen.
//
// Warum ueber die Position und nicht per Zufall: das Ergebnis muss in jedem Bild gleich sein,
// sonst flackern die Baeume. Dieselbe Position ergibt immer dieselbe Drehung und Ansicht.

render_mode cull_disabled, depth_prepass_alpha, specular_disabled;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap;
// Der Atlas ist grid x grid Zellen gross (2 = vier Ansichten).
uniform int grid : hint_range(1, 4) = 2;
uniform float alpha_cut : hint_range(0.0, 1.0, 0.01) = 0.35;

// Farbstreuung pro Baum. Das ist der Regler, der im Wald am meisten bewirkt - deutlich mehr
// als die gedrehten Ansichten, weil eine runde Krone von allen Seiten aehnlich aussieht.
// Ein echter Wald ist nie einfarbig: manche Baeume stehen heller im Licht, andere sind
// gelblicher oder dunkler. 0 = alle gleich.
uniform float color_variation : hint_range(0.0, 0.6, 0.01) = 0.22;
// Wie stark einzelne Baeume heller/dunkler sind.
uniform float brightness_variation : hint_range(0.0, 0.6, 0.01) = 0.18;

// Bricht die einfarbige Flaeche auf. Ein Imposter ist ein flaches Bild und wirkt neben den
// echten, schattierten Baeumen sonst wie ein Aufkleber. Das Rauschen deutet Blattbuescheln
// an, ohne dass dafuer Geometrie noetig waere.
uniform float noise_strength : hint_range(0.0, 0.8, 0.01) = 0.28;
// Groesse der angedeuteten Buescheln (hoeher = feiner).
uniform float noise_scale : hint_range(2.0, 60.0, 1.0) = 18.0;
// Verdunkelt die Krone nach unten hin - dort, wo bei einem echten Baum die oberen Aeste
// Schatten werfen. Gibt der Flaeche Tiefe.
uniform float depth_shading : hint_range(0.0, 1.0, 0.01) = 0.35;

// Wie weit die Zufallsdrehung um die Hochachse streut. 1 = volle Runde, 0 = alle Kreuze gleich
// ausgerichtet (nur zum Vergleichen sinnvoll).
uniform float yaw_variation : hint_range(0.0, 1.0, 0.01) = 1.0;

varying flat vec2 cell;
varying flat vec3 tint;
varying flat float noise_seed;


float hash12(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

// Weiches Rauschen statt reinem Pixelrauschen: letzteres flimmert, sobald der Baum klein wird.
float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash12(i), hash12(i + vec2(1.0, 0.0)), f.x),
			   mix(hash12(i + vec2(0.0, 1.0)), hash12(i + vec2(1.0, 1.0)), f.x), f.y);
}

void vertex() {
	vec2 world_xz = MODEL_MATRIX[3].xz;

	// --- Zufaellige, aber feste Drehung um die Hochachse ---
	// Im Modellraum gedreht, nicht ueber MODELVIEW_MATRIX: so bleiben Skalierung und die
	// Ausrichtung, die der Terrain3D-Instancer der Instanz gibt, unangetastet.
	float hy = fract(sin(dot(world_xz, vec2(41.1237, 289.1131))) * 17351.1173);
	float ang = hy * 6.2831853 * yaw_variation;
	float sa = sin(ang);
	float ca = cos(ang);
	mat3 yaw = mat3(vec3(ca, 0.0, -sa), vec3(0.0, 1.0, 0.0), vec3(sa, 0.0, ca));
	VERTEX = yaw * VERTEX;
	NORMAL = yaw * NORMAL;

	// --- Ansicht aus dem Atlas waehlen ---
	int cells = grid * grid;
	float h = fract(sin(dot(world_xz, vec2(12.9898, 78.233))) * 43758.5453);
	// Die zweite Ebene des Kreuzes (UV2.x = 1) steht 90 Grad verdreht, also zeigt sie auch die
	// naechste Ansicht aus dem Atlas - sonst klebte zweimal dasselbe Bild ueber Kreuz.
	int index = (int(h * float(cells)) + int(UV2.x)) % cells;
	cell = vec2(float(index % grid), float(index / grid));

	// --- Farbe pro Baum leicht verschieben ---
	// Zweiter, unabhaengiger Hash: sonst haetten alle Baeume mit derselben Ansicht auch
	// dieselbe Farbe, und das Muster waere sichtbar.
	float h2 = fract(sin(dot(world_xz, vec2(93.9898, 47.233))) * 24634.6345);
	float v = h2 * 2.0 - 1.0; // -1 .. 1
	// Rot/Blau gegenlaeufig = waermer oder kuehler, Gruen bleibt fast gleich (sonst wirkt es
	// giftig). Dazu die Helligkeit.
	tint = vec3(1.0 + v * color_variation,
				1.0 + v * color_variation * 0.35,
				1.0 - v * color_variation * 0.5)
			* (1.0 + v * brightness_variation);

	// Eigener Rausch-Versatz pro Baum, sonst haetten alle dasselbe Muster an derselben Stelle.
	noise_seed = h2 * 37.0;
}

void fragment() {
	// UV auf die gewaehlte Zelle des Atlas umrechnen.
	vec2 uv = (UV + cell) / float(grid);
	vec4 tex = texture(albedo_texture, uv);

	// Zwei Rausch-Ebenen: grobe Buescheln plus feinere Struktur darin.
	vec2 np = UV * noise_scale + vec2(noise_seed);
	float n = value_noise(np) * 0.65 + value_noise(np * 2.7) * 0.35;

	// Nach unten hin abdunkeln (UV.y = 0 ist oben) - deutet den Eigenschatten der Krone an.
	float depth = mix(1.0 - depth_shading, 1.0, 1.0 - UV.y);

	float shade = mix(1.0 - noise_strength, 1.0 + noise_strength * 0.5, n) * depth;

	// Godot dreht bei cull_disabled die Normale jeder Rueckseite um. Fuer Blattwerk ist das
	// falsch: die Normalen sollen wie bei einer Kugelkrone nach aussen zeigen, egal von welcher
	// Seite man auf die Flaeche schaut - sonst wird die Rueckseite jedes Kreuzes dunkel.
	// Hier wird das Umdrehen rueckgaengig gemacht.
	NORMAL = FRONT_FACING ? NORMAL : -NORMAL;

	ALBEDO = tex.rgb * tint * shade;
	ALPHA = tex.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cut;
	ROUGHNESS = 1.0;
	// Etwas Durchleuchten, damit die Kronen aus der Ferne nicht wie schwarze Flecken wirken.
	BACKLIGHT = vec3(0.25);
}
