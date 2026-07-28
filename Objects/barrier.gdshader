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
uniform float base_haze : hint_range(0.0, 1.0, 0.01) = 0.08;
// CylinderMesh legt UV.y je nach Godot-Version oben oder unten auf 0. Dreht alles gemeinsam um:
// Kreuze, Band und Ausblendungen. Wenn etwas auf dem Kopf steht, hier umschalten.
uniform bool flip_v = true;

group_uniforms grosse_kreuze;
uniform float big_columns : hint_range(4.0, 200.0, 1.0) = 28.0;
uniform float big_rows : hint_range(1.0, 20.0, 1.0) = 2.0;
uniform float big_row_offset : hint_range(0.0, 1.0, 0.01) = 0.5;
// Wo im Hoehenverlauf die grossen Kreuze sitzen. 1.0 = ganz oben.
uniform float big_band_center : hint_range(0.0, 1.0, 0.01) = 0.74;
uniform float big_band_width : hint_range(0.05, 1.0, 0.01) = 0.42;
uniform float big_cell_aspect : hint_range(0.2, 5.0, 0.01) = 1.0;
// Balkendicke / Laenge Laengsbalken / Laenge Querbalken / Hoehe Querbalken (in Zellbreiten).
uniform vec4 big_shape = vec4(0.030, 0.30, 0.15, 0.10);
uniform float big_core : hint_range(0.0, 6.0, 0.01) = 2.4;
uniform float big_glow : hint_range(0.0, 6.0, 0.01) = 1.1;
uniform float big_falloff : hint_range(4.0, 200.0, 1.0) = 40.0;

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
uniform float small_cell_aspect : hint_range(0.2, 5.0, 0.01) = 1.0;
uniform vec4 small_shape = vec4(0.055, 0.28, 0.14, 0.09);
uniform float small_core : hint_range(0.0, 6.0, 0.01) = 2.8;
uniform float small_glow : hint_range(0.0, 6.0, 0.01) = 0.9;
uniform float small_falloff : hint_range(4.0, 200.0, 1.0) = 55.0;
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

group_uniforms verlauf;
uniform float fresnel_power : hint_range(0.1, 8.0, 0.1) = 2.0;
uniform float fresnel_amount : hint_range(0.0, 3.0, 0.01) = 1.2;
uniform float fade_bottom : hint_range(0.0, 0.5, 0.005) = 0.10;
uniform float fade_top : hint_range(0.0, 0.9, 0.005) = 0.30;
uniform float soft_depth : hint_range(0.1, 200.0, 0.1) = 40.0;

uniform sampler2D depth_tex : hint_depth_texture, filter_linear_mipmap;

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

float sd_box(vec2 p, vec2 b) {
	vec2 d = abs(p) - b;
	return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0);
}

// Lateinisches Kreuz: langer Laengsbalken, kuerzerer Querbalken im oberen Drittel.
float sd_cross(vec2 p, vec4 shape) {
	float shaft = sd_box(p, vec2(shape.x, shape.y));
	float bar = sd_box(p - vec2(0.0, shape.w), vec2(shape.z, shape.x));
	return min(shaft, bar);
}

// Gibt vec2(Kern, Schein) zurueck.
vec2 cross_layer(vec2 uv, vec2 grid, float roffset, float aspect, vec4 shape,
		float curve, float curve_freq, float bob, float bob_speed, float falloff) {
	vec2 g = uv * grid;
	float row = floor(g.y);
	g.x += mod(row, 2.0) * roffset;

	float rnd = hash21(floor(g));
	float phase = rnd * 6.2831853;

	vec2 cell = fract(g) - 0.5;
	cell.y /= max(aspect, 0.001);

	// Auf und ab, jedes Kreuz mit eigener Phase.
	cell.y -= sin(TIME * bob_speed + phase) * bob;
	// Geschwungen: der Laengsbalken wird seitlich ausgelenkt, abhaengig von der Hoehe
	// in der Zelle -> aus dem geraden Balken wird eine weiche S-Kurve, die sich bewegt.
	cell.x += sin(cell.y * curve_freq + TIME * bob_speed * 0.6 + phase) * curve;

	float d = sd_cross(cell, shape);
	float core = 1.0 - smoothstep(0.0, fwidth(d) * 1.5, d);
	float glow = exp(-max(d, 0.0) * falloff);
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
				big_shape, big_curve, big_curve_freq, big_bob, big_bob_speed, big_falloff);
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
				small_cell_aspect, small_shape, small_curve, big_curve_freq * 1.6,
				small_bob, small_bob_speed, small_falloff);
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

	// --- Zusammensetzen ---
	float haze = base_haze + fres * fresnel_amount + react * react_haze_boost;
	haze *= mix(1.0, n, noise_amount);

	float core = big.x * big_core + small.x * small_core;
	float glow = big.y * big_glow + small.y * small_glow;

	vec3 col = haze_color * haze + cross_color * (core + glow) * flicker;
	float alpha = (haze + big.x + small.x + (big.y + small.y) * 0.6) * a_bottom * a_top * soft;

	ALBEDO = col * strength;
	ALPHA = clamp(alpha, 0.0, 1.0);
}
