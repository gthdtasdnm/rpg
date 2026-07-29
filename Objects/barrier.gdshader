shader_type spatial;
// Magische Barriere: additiv leuchtender Zylinder mit zwei Kreuz-Ebenen.
//
//   1. GROSSE Kreuze, weit oben in einem Band, geschwungen, wiegen sich langsam auf und ab.
//   2. KLEINE Kreuze, die nur auftauchen, wenn der Spieler nah an die Wand kommt - sie sitzen
//      da, wo er steht, und wandern mit ihm mit. Dafuer setzt barrier.gd "player_position".
//
// Von innen und aussen sichtbar, wirft keinen Schatten, schreibt keine Tiefe.
// fog_disabled, weil der volumetrische Nebel eine additive Flaeche auf 1400 m wegwaschen wuerde.
render_mode blend_add, cull_disabled, unshaded, depth_draw_never, shadows_disabled, fog_disabled;

group_uniforms allgemein;
uniform vec3 haze_color : source_color = vec3(0.30, 0.52, 0.95);
uniform vec3 cross_color : source_color = vec3(0.72, 0.88, 1.0);
uniform float strength : hint_range(0.0, 4.0, 0.01) = 1.0;
uniform float base_haze : hint_range(0.0, 1.0, 0.01) = 0.03;
// CylinderMesh legt UV.y je nach Godot-Version oben oder unten auf 0. Dreht alles gemeinsam um:
// Kreuze, Band und Ausblendungen. Wenn etwas auf dem Kopf steht, hier umschalten.
uniform bool flip_v = true;

group_uniforms grosse_kreuze;
uniform float big_columns : hint_range(4.0, 200.0, 1.0) = 28.0;
uniform float big_rows : hint_range(1.0, 20.0, 1.0) = 2.0;
uniform float big_row_offset : hint_range(0.0, 1.0, 0.01) = 0.5;
// Wo im Hoehenverlauf die grossen Kreuze sitzen. 1.0 = ganz oben.
uniform float big_band_center : hint_range(0.0, 1.0, 0.01) = 0.58;
uniform float big_band_width : hint_range(0.05, 1.0, 0.01) = 0.70;
uniform float big_cell_aspect : hint_range(0.2, 5.0, 0.01) = 1.30;
// Wie stark das Kreuz die Kachel ausfuellt (1.0 = ganze Kachel).
uniform float big_fill : hint_range(0.2, 2.0, 0.01) = 0.85;
uniform float big_core : hint_range(0.0, 6.0, 0.01) = 3.2;
uniform float big_glow : hint_range(0.0, 6.0, 0.01) = 0.7;
uniform float big_glow_lod : hint_range(0.0, 8.0, 0.1) = 4.0;

group_uniforms grosse_kreuze_bewegung;
// Seitliches Schwingen des Laengsbalkens - macht aus dem starren Kreuz eine S-Kurve.
uniform float big_curve : hint_range(0.0, 0.3, 0.001) = 0.055;
uniform float big_curve_freq : hint_range(0.5, 20.0, 0.1) = 5.0;
// Auf und ab. Jedes Kreuz bekommt eine eigene Phase, sonst wippt die ganze Wand im Gleichtakt.
uniform float big_bob : hint_range(0.0, 0.3, 0.001) = 0.045;
uniform float big_bob_speed : hint_range(0.0, 3.0, 0.01) = 0.35;

group_uniforms kleine_kreuze;
uniform float small_columns : hint_range(20.0, 800.0, 1.0) = 260.0;
uniform float small_rows : hint_range(2.0, 200.0, 1.0) = 26.0;
uniform float small_row_offset : hint_range(0.0, 1.0, 0.01) = 0.5;
uniform float small_cell_aspect : hint_range(0.2, 5.0, 0.01) = 1.30;
uniform float small_fill : hint_range(0.2, 2.0, 0.01) = 0.8;
uniform float small_core : hint_range(0.0, 6.0, 0.01) = 2.8;
uniform float small_glow : hint_range(0.0, 6.0, 0.01) = 0.9;
uniform float small_glow_lod : hint_range(0.0, 8.0, 0.1) = 3.0;
uniform float small_curve : hint_range(0.0, 0.3, 0.001) = 0.04;
uniform float small_bob : hint_range(0.0, 0.3, 0.001) = 0.03;
uniform float small_bob_speed : hint_range(0.0, 6.0, 0.01) = 1.4;

group_uniforms spieler_reaktion;
// Wird von barrier.gd gesetzt.
uniform vec3 player_position = vec3(0.0);
// Wie weit um den Spieler herum die kleinen Kreuze stehen (Meter).
uniform float react_radius : hint_range(5.0, 400.0, 1.0) = 90.0;
// Ab welchem Abstand zur Wand die Reaktion einsetzt (Meter).
uniform float react_distance : hint_range(5.0, 600.0, 1.0) = 160.0;
// Zusaetzliches Pulsieren, solange der Spieler nah ist.
uniform float react_pulse_speed : hint_range(0.0, 10.0, 0.01) = 2.5;
uniform float react_pulse_amount : hint_range(0.0, 1.0, 0.01) = 0.25;
// Die Wand leuchtet insgesamt staerker, wenn er nah ist.
uniform float react_haze_boost : hint_range(0.0, 3.0, 0.01) = 0.8;

group_uniforms energie;
uniform float scroll_speed : hint_range(-1.0, 1.0, 0.001) = 0.02;
uniform float noise_scale : hint_range(0.5, 40.0, 0.1) = 6.0;
uniform float noise_amount : hint_range(0.0, 1.0, 0.01) = 0.35;
uniform float flicker_speed : hint_range(0.0, 8.0, 0.01) = 1.1;

group_uniforms schein_unten;
// Polarlicht, aber umgedreht: der Vorhang steht am Boden und faellt nach OBEN aus.
uniform bool aurora_enabled = true;
uniform vec3 aurora_color : source_color = vec3(0.25, 0.85, 0.70);
uniform vec3 aurora_tip_color : source_color = vec3(0.45, 0.35, 0.95);
uniform float aurora_strength : hint_range(0.0, 6.0, 0.01) = 3.0;
// Wo die HELLE BASIS des Vorhangs sitzt (0 = Unterkante, 1 = Oberkante der Barriere).
// Muss ueber die Gelaendekante, sonst steckt der Vorhang im Berg.
uniform float aurora_base : hint_range(0.0, 1.0, 0.01) = 0.42;
// Wie weit der Vorhang von der Basis aus nach oben reicht.
uniform float aurora_height : hint_range(0.02, 1.5, 0.01) = 0.60;
// Groesser = schnellerer Abfall nach oben, also kuerzere Zungen.
uniform float aurora_falloff : hint_range(0.5, 6.0, 0.1) = 1.5;
// Dichte der senkrechten Strahlen. Ganze Zahl, damit es rundherum nahtlos bleibt.
uniform float aurora_streaks : hint_range(8.0, 800.0, 1.0) = 260.0;
// Wie stark die Oberkante wellt.
uniform float aurora_wave : hint_range(0.0, 2.0, 0.01) = 0.30;
uniform float aurora_speed : hint_range(0.0, 1.0, 0.005) = 0.06;
// Breite einer einzelnen Faser (klein = duenne, scharfe Strahlen).
uniform float aurora_thickness : hint_range(0.05, 2.0, 0.01) = 0.85;
// Durchgehender Grundschleier zwischen den Fasern (0 = nur Fasern, 1 = volle Flaeche).
uniform float aurora_fill : hint_range(0.0, 1.0, 0.01) = 0.45;

group_uniforms verlauf;
uniform float fresnel_power : hint_range(0.1, 8.0, 0.1) = 2.0;
uniform float fresnel_amount : hint_range(0.0, 3.0, 0.01) = 0.30;
uniform float fade_bottom : hint_range(0.0, 0.5, 0.005) = 0.10;
uniform float fade_top : hint_range(0.0, 0.9, 0.005) = 0.30;
uniform float soft_depth : hint_range(0.1, 200.0, 0.1) = 40.0;

uniform sampler2D depth_tex : hint_depth_texture, filter_linear_mipmap;

// Kreuz-Maske: WEISS = leuchtet, SCHWARZ = unsichtbar. Aus der schwarz-auf-weiss
// Zeichnung wird das beim Aufbereiten invertiert (Objects/barriere/kreuz.png).
// repeat_disable: ausserhalb der Kachel wird der schwarze Rand geklemmt -> nichts.
uniform sampler2D cross_tex : source_color, filter_linear_mipmap, repeat_disable;

varying vec3 world_pos;
varying vec3 axis_pos;


float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	float a = hash21(i);
	float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0));
	float d = hash21(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

// Wie value_noise, aber in x periodisch. Ohne das klafft am Zylinder-Nahtpunkt
// (uv.x = 0 bzw. 1) eine sichtbare Kante im Vorhang.
float wrap_noise(vec2 p, float period) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	float x0 = mod(i.x, period);
	float x1 = mod(i.x + 1.0, period);
	float a = hash21(vec2(x0, i.y));
	float b = hash21(vec2(x1, i.y));
	float c = hash21(vec2(x0, i.y + 1.0));
	float d = hash21(vec2(x1, i.y + 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}


// Gibt vec2(Kern, Schein) zurueck - beides aus der Kreuz-Textur.
// Der Schein ist einfach eine hoehere Mipmap-Stufe, das ist eine kostenlose Unschaerfe.
vec2 cross_layer(vec2 uv, vec2 grid, float roffset, float aspect, float fill,
		float curve, float curve_freq, float bob, float bob_speed, float glow_lod) {
	vec2 g = uv * grid;
	float row = floor(g.y);
	g.x += mod(row, 2.0) * roffset;

	float phase = hash21(floor(g)) * 6.2831853;

	vec2 cell = fract(g) - 0.5;
	cell.y /= max(aspect, 0.001);

	// Auf und ab, jedes Kreuz mit eigener Phase.
	cell.y -= sin(TIME * bob_speed + phase) * bob;
	// Geschwungen: seitliche Auslenkung abhaengig von der Hoehe in der Zelle ->
	// aus dem geraden Kreuz wird eine weiche S-Kurve, die langsam durchwandert.
	cell.x += sin(cell.y * curve_freq + TIME * bob_speed * 0.6 + phase) * curve;

	// fill < 1 macht das Kreuz groesser (fuellt die Kachel staerker aus).
	vec2 tuv = cell / max(fill, 0.01);
	// Texturen haben V = 0 OBEN, cell.y zeigt aber nach oben. Ohne das Minus
	// steht das Kreuz auf dem Kopf - beim gerechneten Kreuz gab es das nicht.
	tuv.y = -tuv.y;
	tuv += 0.5;

	// Ableitungen aus der DURCHLAUFENDEN Koordinate nehmen. Mit fract() waeren sie
	// an jeder Zellgrenze unendlich und das Mipmapping wuerde dort aufreissen.
	vec2 ddx = dFdx(g) / max(fill, 0.01);
	vec2 ddy = dFdy(g) / max(fill, 0.01);

	float core = textureGrad(cross_tex, tuv, ddx, ddy).r;
	float glow = textureLod(cross_tex, tuv, glow_lod).r;
	return vec2(core, glow);
}


void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	axis_pos = MODEL_MATRIX[3].xyz;
}


void fragment() {
	vec2 uv = vec2(UV.x, flip_v ? 1.0 - UV.y : UV.y);
	float v = uv.y;

	// --- Grosse Kreuze, nur im oberen Band ---
	float band = 1.0 - smoothstep(0.0, max(big_band_width * 0.5, 0.001),
			abs(v - big_band_center));
	band = band * band;  // weichere Raender, klarer Schwerpunkt

	vec2 big = vec2(0.0);
	if (band > 0.001) {
		big = cross_layer(uv, vec2(big_columns, big_rows), big_row_offset, big_cell_aspect,
				big_fill, big_curve, big_curve_freq, big_bob, big_bob_speed, big_glow_lod);
		big *= band;
	}

	// --- Spielernaehe ---
	// Abstand des Spielers zur Zylinderwand (nur waagerecht gemessen).
	float wall_dist = abs(length(player_position.xz - axis_pos.xz)
			- length(world_pos.xz - axis_pos.xz));
	float engage = 1.0 - smoothstep(react_distance * 0.4, react_distance, wall_dist);
	// Abstand dieses Punktes zum Spieler.
	float d_player = distance(world_pos, player_position);
	float near_mask = 1.0 - smoothstep(react_radius * 0.35, react_radius, d_player);
	float react = engage * near_mask;

	// --- Kleine Kreuze, nur in Spielernaehe ---
	vec2 small = vec2(0.0);
	if (react > 0.001) {
		small = cross_layer(uv, vec2(small_columns, small_rows), small_row_offset,
				small_cell_aspect, small_fill, small_curve, big_curve_freq * 1.6,
				small_bob, small_bob_speed, small_glow_lod);
		float pulse = 1.0 + sin(TIME * react_pulse_speed) * react_pulse_amount;
		small *= react * pulse;
	}

	// --- Energie/Rauschen ---
	vec2 nuv = uv * vec2(big_columns, big_rows) * noise_scale * 0.1;
	nuv.y -= TIME * scroll_speed * 10.0;
	float n = value_noise(nuv) * 0.65 + value_noise(nuv * 2.7) * 0.35;
	float flicker = 0.85 + 0.15 * sin(TIME * flicker_speed + n * 6.2831853);

	// --- Fresnel: streifender Blick leuchtet staerker ---
	float fres = pow(1.0 - abs(dot(normalize(NORMAL), normalize(VIEW))), fresnel_power);

	// --- Hoehenverlauf ---
	float a_bottom = smoothstep(0.0, max(fade_bottom, 0.001), v);
	float a_top = 1.0 - smoothstep(1.0 - fade_top, 1.0, v);

	// --- Weicher Uebergang an Gelaendedurchdringungen ---
	float raw = texture(depth_tex, SCREEN_UV).x;
	vec4 upos = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, raw, 1.0);
	float scene_z = -(upos.z / upos.w);
	float frag_z = -VERTEX.z;
	float soft = clamp((scene_z - frag_z) / soft_depth, 0.0, 1.0);

	// --- Vorhang von unten ("umgedrehtes Polarlicht") ---
	vec3 aurora_col = vec3(0.0);
	float aurora = 0.0;
	if (aurora_enabled) {
		float t = TIME * aurora_speed;
		float per = max(floor(aurora_streaks), 4.0);

		// Seitliche Auslenkung haengt von der Hoehe ab -> die Fasern schimmern quer,
		// statt dass ein Band senkrecht durchlaeuft.
		float u = uv.x * per + sin(v * 2.4 + t * 2.0) * aurora_wave;
		float mitte = floor(u);

		// Auch die NACHBARFASERN auswerten. Nur so koennen sie breiter als ihre
		// Zelle werden und sich ueberlappen - sonst klafft zwischen ihnen immer
		// eine Luecke, egal wie gross aurora_thickness ist.
		float rel_use = 0.0;
		for (int k = -2; k <= 2; k++) {
			float id = mitte + float(k);
			float d = u - (id + 0.5);              // Abstand zur Mitte dieser Faser
			float rh = hash21(vec2(mod(id, per), 11.0));
			float rb = hash21(vec2(mod(id, per), 23.0));
			float rp = hash21(vec2(mod(id, per), 37.0));

			// Profil quer zur Faser. thickness > 0.5 laesst Nachbarn ueberlappen.
			float across = 1.0 - smoothstep(aurora_thickness * 0.25, aurora_thickness, abs(d));
			if (across <= 0.001) { continue; }

			// eigene Hoehe je Faser -> ausgefranste Oberkante ohne Wellenanimation
			float top = max(aurora_height * mix(0.45, 1.55, rh), 0.002);
			float rel = (v - aurora_base) / top;
			float oben = pow(clamp(1.0 - rel, 0.0, 1.0), aurora_falloff);
			float unten = smoothstep(aurora_base - top * 0.30, aurora_base, v);
			float puls = 0.72 + 0.28 * sin(TIME * (0.5 + rp * 1.4) + rp * 6.2831853);

			float wert = across * mix(0.60, 1.0, rb) * oben * unten * puls;
			if (wert > aurora) {
				aurora = wert;
				rel_use = clamp(rel, 0.0, 1.0);
			}
		}

		// Durchgehender Grundschleier: fuellt, was zwischen den Fasern noch offen
		// bleibt, damit der Vorhang als Flaeche liest und nicht als Kamm.
		float top_g = max(aurora_height, 0.002);
		float rel_g = (v - aurora_base) / top_g;
		float wash = pow(clamp(1.0 - rel_g, 0.0, 1.0), aurora_falloff * 1.2)
				* smoothstep(aurora_base - top_g * 0.30, aurora_base, v);
		// feine Koernung im Schleier, ohne neue Luecken zu reissen
		float korn = 0.75 + 0.25 * wrap_noise(vec2(uv.x * per * 0.7 - t * 0.6, v * 2.0), per);
		aurora = max(aurora, wash * aurora_fill * korn);
		if (aurora <= wash * aurora_fill * korn + 0.0001) { rel_use = clamp(rel_g, 0.0, 1.0); }

		aurora_col = mix(aurora_color, aurora_tip_color, rel_use);
	}

	// --- Zusammensetzen ---
	float haze = base_haze + fres * fresnel_amount + react * react_haze_boost;
	haze *= mix(1.0, n, noise_amount);

	float core = big.x * big_core + small.x * small_core;
	float glow = big.y * big_glow + small.y * small_glow;

	vec3 col = haze_color * haze + cross_color * (core + glow) * flicker;
	// Der Vorhang bekommt KEIN a_bottom - dort ist er am hellsten. Gegen harte
	// Kanten am Gelaende reicht "soft".
	col += aurora_col * aurora * aurora_strength;
	float alpha = (haze + big.x + small.x + (big.y + small.y) * 0.6) * a_bottom * a_top;
	alpha += aurora * aurora_strength * 1.6 * a_top;
	alpha *= soft;

	ALBEDO = col * strength;
	ALPHA = clamp(alpha, 0.0, 1.0);
}
