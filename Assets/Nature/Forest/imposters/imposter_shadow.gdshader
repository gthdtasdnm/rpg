shader_type spatial;

// Angedeuteter Bodenschatten unter den Imposter-Baeumen.
//
// VERALTET, steht ueber SHADOW_STRENGTH = 0.0 im Generator auf aus.
// Der Ersatzschatten war noetig, solange der Imposter ein Billboard war: eine Flaeche, die sich
// zur Kamera dreht, kann keinen echten Schatten werfen - der wuerde sich beim Umsehen mitdrehen.
// Seit dem festen Kreuz (27.07.) wirft der Imposter einen echten Schatten, siehe
// generate_tree_imposters.gd und last_shadow_lod in World/world.tscn. Der Code bleibt nur als
// Rueckfalloption stehen.
//
// Statt echter Schattenberechnung wird hier einfach der Boden abgedunkelt: eine waagerechte
// Flaeche mit weichem, runden Verlauf. Kostet zwei Dreiecke und keine Schattenkarte.
//
// blend_mul = multiplizieren statt ueberlagern: die Bodentextur bleibt sichtbar und wird nur
// dunkler. Ein halbtransparentes graues Rechteck wuerde die Farben stattdessen ausbleichen.

// fog_disabled ist PFLICHT: Godot legt den Nebel auch auf transparente Flaechen, und bei
// blend_mul wird die Nebelfarbe dann in den Hintergrund multipliziert. Die Flaeche ist dann
// nicht mehr unsichtbar, sondern ein quadratischer Fleck, der mit der Nebeldichte staerker
// wird - in der Ferne deutlich sichtbar als "eckiger Schatten" um jeden Baum.
render_mode blend_mul, unshaded, cull_disabled, depth_draw_never, shadows_disabled, fog_disabled;

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
