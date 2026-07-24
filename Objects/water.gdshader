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

uniform sampler2D depth_tex : hint_depth_texture, filter_linear_mipmap;
uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;

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

	// --- Refraktion: verzerrter Blick auf das, was hinter dem Wasser liegt ---
	vec2 refr_uv = SCREEN_UV + raw_grad * refraction_strength;

	// --- Wassertiefe aus dem Tiefenpuffer (an der verzerrten Stelle, passt zum Durchblick) ---
	float depth_raw = texture(depth_tex, refr_uv).r;
	vec3 ndc = vec3(refr_uv * 2.0 - 1.0, depth_raw);
	vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	view.xyz /= view.w;
	float water_depth = (-view.z) - (-VERTEX.z);
	// Negative Tiefe (Objekt vor dem Wasser) -> keine Verzerrung, sonst leckt der Vordergrund.
	if (water_depth < 0.0) {
		refr_uv = SCREEN_UV;
		water_depth = 0.0;
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
	// Bewegte Normale -> Godot fuegt Umgebungs-/Sky-Reflexion mit Fresnel automatisch hinzu.
	vec2 grad = raw_grad * wave_strength * 40.0;
	NORMAL_MAP = normalize(vec3(-grad, 1.0)) * 0.5 + 0.5;
}
