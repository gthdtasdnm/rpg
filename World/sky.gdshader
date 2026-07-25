shader_type sky;
// Prozeduraler Himmel: Tag/Nacht-Gradient, Sonne+Halo, Mond, Sterne und beleuchtete Wolken.
// Die Sonnenrichtung kommt als Uniform vom Tageszeit-Script (day_night.gd).

group_uniforms sky_colors;
uniform vec3 day_zenith : source_color = vec3(0.08, 0.28, 0.75);
uniform vec3 day_horizon : source_color = vec3(0.55, 0.72, 0.92);
uniform vec3 dusk_horizon : source_color = vec3(0.95, 0.42, 0.18);
uniform vec3 night_zenith : source_color = vec3(0.008, 0.015, 0.05);
uniform vec3 night_horizon : source_color = vec3(0.02, 0.03, 0.08);
uniform vec3 ground_color : source_color = vec3(0.15, 0.16, 0.18);

group_uniforms sun_moon;
uniform vec3 sun_direction = vec3(0.3, 0.7, -0.45);   // vom Script gesetzt
uniform float sun_radius : hint_range(0.005, 0.2, 0.001) = 0.03;
uniform vec3 sun_color : source_color = vec3(1.0, 0.95, 0.8);
uniform float halo_strength : hint_range(0.0, 3.0, 0.01) = 0.5;
uniform float moon_radius : hint_range(0.005, 0.2, 0.001) = 0.045;
uniform vec3 moon_color : source_color = vec3(0.85, 0.9, 1.0);

group_uniforms clouds;
uniform float cloud_coverage : hint_range(0.0, 1.0, 0.01) = 0.5;
uniform float cloud_softness : hint_range(0.01, 0.6, 0.01) = 0.25;
uniform float cloud_scale = 2.0;
uniform vec2 cloud_wind = vec2(0.008, 0.004);
uniform float cloud_horizon_fade : hint_range(0.02, 1.0, 0.01) = 0.12;

group_uniforms stars;
uniform float star_amount : hint_range(0.0, 1.0, 0.01) = 0.35;

// Hochwertige Hashes (Dave Hoskins) - vermeiden die "Glasscherben"-Facetten.
float hash21(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float hash31(vec3 p) {
	p = fract(p * 0.1031);
	p += dot(p, p.zyx + 31.32);
	return fract((p.x + p.y) * p.z);
}

float vnoise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	float a = hash21(i);
	float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0));
	float d = hash21(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
	float v = 0.0;
	float amp = 0.5;
	mat2 rot = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));  // Rotation je Oktave -> weniger blockig
	for (int i = 0; i < 6; i++) {
		v += amp * vnoise(p);
		p = rot * p * 2.0;
		amp *= 0.5;
	}
	return v;
}

void sky() {
	vec3 dir = EYEDIR;
	vec3 sdir = normalize(sun_direction);
	float sun_elev = sdir.y;
	float day = clamp(sun_elev * 4.0, 0.0, 1.0);
	float twilight = clamp(1.0 - abs(sun_elev) * 4.0, 0.0, 1.0);   // nur nahe Horizont
	float night = clamp(-sun_elev * 4.0 + 0.2, 0.0, 1.0);

	// --- Gradient ---
	float up = clamp(dir.y, 0.0, 1.0);
	vec3 zenith = mix(night_zenith, day_zenith, day);
	vec3 horizon = mix(night_horizon, day_horizon, day);
	horizon = mix(horizon, dusk_horizon, twilight);
	vec3 col = mix(horizon, zenith, pow(up, 0.5));
	if (dir.y < 0.0) {
		col = mix(col, ground_color, clamp(-dir.y * 4.0, 0.0, 1.0));
	}

	// --- Sterne (3D-Verteilung, nur nachts, obere Hemisphaere) ---
	if (night > 0.01 && dir.y > 0.0) {
		float sv = hash31(floor(dir * 250.0));
		float star = smoothstep(1.0 - star_amount * 0.02, 1.0, sv);
		col += vec3(star) * night * smoothstep(0.0, 0.1, dir.y);
	}

	// --- Wolken ---
	float mask = smoothstep(0.0, cloud_horizon_fade, dir.y);
	if (mask > 0.001) {
		vec2 uv = dir.xz / (dir.y + 0.12) * cloud_scale + TIME * cloud_wind;
		float n = fbm(uv);
		float thr = mix(0.72, 0.18, cloud_coverage);
		float cov = smoothstep(thr, thr + cloud_softness, n);
		float shade = fbm(uv * 2.0 + 5.0);                        // Struktur/Schattierung
		vec3 lit = mix(vec3(0.72, 0.75, 0.8), vec3(1.0), shade);  // Tagfarbe (nicht rein weiss)
		vec3 cbase = mix(vec3(0.05, 0.07, 0.12), lit, day);       // nachts dunkel
		float sun_glow = pow(clamp(dot(dir, sdir), 0.0, 1.0), 4.0);
		vec3 ccol = mix(cbase, sun_color, sun_glow * 0.5 * day);  // Richtung Sonne durchleuchtet
		ccol = mix(ccol, dusk_horizon, twilight * 0.5);
		col = mix(col, ccol, cov * mask);
	}

	// --- Sonne + Halo (nur ueber Horizont) ---
	float sun_above = smoothstep(-0.08, 0.05, sun_elev);
	float sa = acos(clamp(dot(dir, sdir), -1.0, 1.0));
	float sun_disk = 1.0 - smoothstep(sun_radius * 0.7, sun_radius, sa);
	// Enger, dezenter Halo, damit die Sonnenscheibe klar bleibt und nicht alles ueberstrahlt.
	float halo = (exp(-sa * 9.0) * 0.4 + exp(-sa * 3.0) * 0.06) * halo_strength;
	col += sun_color * (sun_disk * 6.0 + halo) * sun_above;

	// --- Mond (gegenueber der Sonne, nachts) ---
	vec3 mdir = -sdir;
	float moon_above = smoothstep(-0.05, 0.1, mdir.y);
	float ma = acos(clamp(dot(dir, mdir), -1.0, 1.0));
	float moon_disk = 1.0 - smoothstep(moon_radius * 0.8, moon_radius, ma);
	float moon_glow = exp(-ma * 8.0) * 0.15;
	col += moon_color * (moon_disk * 1.5 + moon_glow) * moon_above * night;

	COLOR = col;
}
