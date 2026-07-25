// Wasser-Shader mit tiefenabhaengiger Einfaerbung, Lichtbrechung und Reflexion.
// - DEPTH_TEXTURE: Wassertiefe -> tiefes Wasser dunkler/blauer als flaches.
// - SCREEN_TEXTURE: der Blick durchs Wasser wird von den Wellen verzerrt (Refraktion).
// - Reflexion: niedrige Roughness + bewegte Wellen-Normale -> Godot spiegelt Umgebung/Himmel
//   automatisch mit Fresnel (am Rand mehr Spiegelung, von oben mehr Durchsicht).
//   Fuer Spiegelung der Szene (nicht nur Himmel) im WorldEnvironment SSR aktivieren.
shader_type spatial;
render_mode blend_mix, cull_back, diffuse_burley, specular_schlick_ggx;

group_uniforms depth;
// Wassertoenung bei flachem Wasser (Alpha = Staerke der Toenung ueber dem Durchblick).
uniform vec4 shallow_color : source_color = vec4(0.15, 0.5, 0.6, 0.25);
// Farbe bei tiefem Wasser (dunkelblau).
uniform vec4 deep_color : source_color = vec4(0.02, 0.08, 0.15, 1.0);
// Ab dieser Wassertiefe (Meter) ist die volle deep_color erreicht.
uniform float depth_distance : hint_range(0.1, 40.0, 0.1) = 6.0;

group_uniforms refraction;
// Wie stark die Wellen den Durchblick verzerren.
uniform float refraction_strength : hint_range(0.0, 0.3, 0.001) = 0.03;

group_uniforms waves;
uniform sampler2D wave_texture : hint_default_black;
uniform vec2 wave_tiling = vec2(8.0, 8.0);
uniform vec2 wave_speed = vec2(0.03, 0.02);
uniform float wave_strength : hint_range(0.0, 2.0, 0.01) = 0.4;
uniform float water_roughness : hint_range(0.0, 1.0, 0.01) = 0.02;

group_uniforms swim_ripple;
// Vom Spieler ausgesandte Ringwellen (Kielwelle). wasser.gd sendet waehrend des Schwimmens
// periodisch neue aus; jede lebt eigenstaendig und bleibt an ihrer Aussende-Position, sodass
// beim Vorwaertsschwimmen ein Kegel hinter dem Spieler entsteht.
const int RIPPLE_COUNT = 48;
uniform vec2 ripple_centers[RIPPLE_COUNT];
uniform float ripple_ages[RIPPLE_COUNT];                           // Sek. seit Aussendung (gross = inaktiv)
uniform float ripple_duration = 2.5;                               // Lebensdauer (von wasser.gd gesetzt)
uniform float ripple_start_radius : hint_range(0.0, 2.0, 0.05) = 0.4;// Startradius -> sofort sichtbar am Spieler
uniform float ripple_speed : hint_range(0.2, 8.0, 0.1) = 2.0;      // Ausbreitung m/s
uniform float ripple_wavelength : hint_range(0.1, 3.0, 0.05) = 0.5;// Ringbreite (m)
uniform float ripple_strength : hint_range(0.0, 10.0, 0.1) = 3.0;  // Staerke
uniform float ripple_noise_strength : hint_range(0.0, 1.0, 0.01) = 0.15; // Unregelmaessigkeit der Ringe
uniform float ripple_noise_scale : hint_range(0.05, 3.0, 0.05) = 0.6;    // Groesse der Stoerung

uniform sampler2D depth_tex : hint_depth_texture, filter_linear_mipmap;
uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;

varying vec3 v_world;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	// --- Bewegte Wellen-Normale aus zwei gescrollten Ebenen der Graustufen-Textur ---
	vec2 uv = UV * wave_tiling;
	vec2 uv1 = uv + TIME * wave_speed;
	vec2 uv2 = uv * 0.7 - TIME * wave_speed * 0.8;
	float e = 1.0 / 256.0;
	float gx = (texture(wave_texture, uv1 + vec2(e, 0.0)).r - texture(wave_texture, uv1 - vec2(e, 0.0)).r)
			 + (texture(wave_texture, uv2 + vec2(e, 0.0)).r - texture(wave_texture, uv2 - vec2(e, 0.0)).r);
	float gy = (texture(wave_texture, uv1 + vec2(0.0, e)).r - texture(wave_texture, uv1 - vec2(0.0, e)).r)
			 + (texture(wave_texture, uv2 + vec2(0.0, e)).r - texture(wave_texture, uv2 - vec2(0.0, e)).r);
	vec2 raw_grad = vec2(gx, gy);

	// --- Kielwelle: jede ausgesandte Welle laeuft von ihrer festen Aussende-Position nach aussen
	// und klingt ueber ihre Lebensdauer ab. Summe aller aktiven Wellen ergibt den Kegel. ---
	// Noise fuer unregelmaessige Ringe: einmal pro Pixel aus der Wellen-Textur, dann bei jeder
	// Welle auf den Ring-Abstand angewandt -> die Ringe werden leicht wellig statt perfekt rund.
	float rnoise = (texture(wave_texture, v_world.xz * ripple_noise_scale + TIME * 0.03).r - 0.5)
		* 2.0 * ripple_noise_strength;

	vec2 ripple_n = vec2(0.0);
	for (int i = 0; i < RIPPLE_COUNT; i++) {
		float age = ripple_ages[i];
		if (age >= ripple_duration) {
			continue;
		}
		vec2 to_c = v_world.xz - ripple_centers[i];
		float d_raw = length(to_c);
		float d = d_raw + rnoise;                    // Ring-Abstand leicht verrauscht
		float radius = ripple_start_radius + age * ripple_speed;  // startet sofort sichtbar am Spieler
		if (d > radius + ripple_wavelength) {
			continue;                                // noch ausserhalb der Front
		}
		vec2 rdir = d_raw > 1e-4 ? to_c / d_raw : vec2(0.0);
		// nur ~2 Ringe hinter der Front sichtbar (kurzer Wellenschlag)
		float band = smoothstep(radius, radius - ripple_wavelength * 2.0, d) * step(d, radius);
		float time_fade = 1.0 - age / ripple_duration;
		float phase = (d - radius) / ripple_wavelength * 6.2831853;
		ripple_n += rdir * cos(phase) * band * time_fade;
	}
	ripple_n *= ripple_strength;

	// --- Refraktion: verzerrter Blick auf das, was hinter dem Wasser liegt ---
	// Nur die sanften Wellen verzerren (NICHT die Ringwellen), sonst flackert es um den Spieler.
	// UV auf den Bildschirm klemmen, sonst Artefakte am Rand.
	vec2 refr_uv = clamp(SCREEN_UV + raw_grad * refraction_strength, vec2(0.0), vec2(1.0));

	// --- Wassertiefe aus dem Tiefenpuffer (an der verzerrten Stelle) ---
	float depth_raw = texture(depth_tex, refr_uv).r;
	vec3 ndc = vec3(refr_uv * 2.0 - 1.0, depth_raw);
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view.xyz /= view.w;
	float water_depth = (-view.z) - (-VERTEX.z);
	// Liegt die verzerrte Stelle VOR dem Wasser (Spieler/Vordergrund)? -> unverzerrt nehmen und
	// die Tiefe dort NEU messen, sonst scheint der Vordergrund durchs Wasser.
	if (water_depth < 0.0) {
		refr_uv = SCREEN_UV;
		depth_raw = texture(depth_tex, refr_uv).r;
		ndc = vec3(refr_uv * 2.0 - 1.0, depth_raw);
		view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
		view.xyz /= view.w;
		water_depth = max((-view.z) - (-VERTEX.z), 0.0);
	}

	vec3 background = texture(screen_tex, refr_uv).rgb;

	// 0 = flach/Oberflaeche, 1 = tief.
	float t = clamp(water_depth / depth_distance, 0.0, 1.0);
	// Farbe UND Deckkraft je nach Tiefe interpolieren. Der Alpha-Kanal beider Farben steuert,
	// wie stark die Wasserfarbe den Durchblick ueberdeckt (deep_color.a -> Deckkraft tief).
	vec3 tint = mix(shallow_color.rgb, deep_color.rgb, t);
	float coverage = mix(shallow_color.a, deep_color.a, t);
	ALBEDO = mix(background, tint, coverage);
	ALPHA = 1.0; // Durchsicht wird ueber die Refraktion/Deckkraft gemacht, nicht ueber Transparenz.

	ROUGHNESS = water_roughness;
	// Bewegte Normale (Wellen + Ringwellen) -> Godot fuegt Reflexion mit Fresnel automatisch hinzu.
	vec2 grad = raw_grad * wave_strength * 40.0 + ripple_n;
	NORMAL_MAP = normalize(vec3(-grad, 1.0)) * 0.5 + 0.5;
}
