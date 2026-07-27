shader_type spatial;

// Angedeuteter Bodenschatten unter den Imposter-Baeumen.
//
// WARUM: Ein Imposter ist eine Flaeche, die sich zur Kamera dreht - er kann keinen echten
// Schatten werfen, der wuerde sich beim Umsehen mitdrehen. Ohne Ersatz haben aber alle Baeume
// jenseits der LOD-Grenze keinen Schatten, waehrend das Terrain noch beschattet wird. Genau
// diese Kante faellt auf.
//
// Statt echter Schattenberechnung wird hier einfach der Boden abgedunkelt: eine waagerechte
// Flaeche mit weichem, runden Verlauf. Kostet zwei Dreiecke und keine Schattenkarte.
//
// blend_mul = multiplizieren statt ueberlagern: die Bodentextur bleibt sichtbar und wird nur
// dunkler. Ein halbtransparentes graues Rechteck wuerde die Farben stattdessen ausbleichen.

render_mode blend_mul, unshaded, cull_disabled, depth_draw_never, shadows_disabled;

// Wie dunkel in der Mitte. 0 = unsichtbar, 1 = schwarz.
uniform float strength : hint_range(0.0, 1.0, 0.01) = 0.4;
// Wie weich der Rand ausläuft. Klein = harte Kante, gross = diffuser Fleck.
uniform float softness : hint_range(0.05, 1.0, 0.01) = 0.75;

void fragment() {
	// Abstand vom Mittelpunkt: 0 in der Mitte, 1 am Rand der Flaeche.
	float dist = length(UV - vec2(0.5)) * 2.0;

	// Weicher Kreis. Aussen 0 (kein Einfluss), innen 1 (volle Abdunklung).
	float mask = 1.0 - smoothstep(1.0 - softness, 1.0, dist);
	mask *= mask; // zur Mitte hin etwas kraeftiger

	ALBEDO = mix(vec3(1.0), vec3(1.0 - strength), mask);
}
