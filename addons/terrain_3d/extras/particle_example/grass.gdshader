// Copyright © 2023-2026 Cory Petkovsek, Roope Palmroos, and Contributors.

shader_type spatial;

render_mode skip_vertex_transform, cull_disabled, blend_mix;

uniform vec2 wind_direction = vec2(1.0, 1.0);

group_uniforms shading;
// Fake-AO: verdunkelt den Halmgrund. Dichtes Gras liest sich dadurch als dunklerer Boden,
// ohne teure Schattenberechnung. base_ao = wie dunkel (0 = schwarz), height = wie hoch es reicht.
uniform float base_ao : hint_range(0.0, 1.0, 0.01) = 0.45;
uniform float base_ao_height : hint_range(0.0, 1.0, 0.01) = 0.6;

group_uniforms player_interaction;
// A trail of the player's recent positions, fed from GDScript. As the player
// walks through, the grass parts to the left and right, and the parting slowly
// springs back as each trail point's strength fades over time.
const int TRAIL_SIZE = 16;
uniform vec3 trail_positions[TRAIL_SIZE];
uniform vec2 trail_dirs[TRAIL_SIZE];      // player movement direction at each point
uniform float trail_strength[TRAIL_SIZE]; // 1.0 = fresh, 0.0 = fully sprung back
// Horizontal radius (meters) of the parting around the player's path.
uniform float trail_radius : hint_range(0.1, 4.0, 0.05) = 0.9;
// How far the grass is pushed to the side (0 = off).
uniform float trail_push : hint_range(0.0, 1.5, 0.05) = 0.55;

varying vec4 data;

// A lot of hard coded things here atm.
void vertex() {
	// Wind effect from model data, in this case no vertex colors,
	// so just use vertex Y component, including mesh offset
	data[2] = (VERTEX.y + 0.55);
	data[2] *= data[2]; // make non-linear

	// Ribbon used as a grass mesh.. so pinch the top.
	VERTEX.xz *= (1.0 - data[2]);

	// Brighten tips
	COLOR = mix(COLOR, vec4(1.0), smoothstep(0.9, 1.0, data[2]));
	// Darken base (fake AO), skip if scale is less than threshold, as this means "grow in" is occuring.
	COLOR *= INSTANCE_CUSTOM[3] < 0.35 ? 1. : mix(1.0, base_ao, smoothstep(base_ao_height, 0.0, data[2]));
	// Save red/green shift for fragment
	data.rg = INSTANCE_CUSTOM.rg;

	// World space vertex
	vec3 w_vertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;

	// --- Player trail ---
	// Evaluate the parting at the blade ROOT (the instance origin), not per vertex.
	// This keeps the whole blade rigid: every vertex shares the same direction and
	// strength, so the blade bends as one piece instead of stretching or splitting.
	vec2 root_xz = MODEL_MATRIX[3].xz;
	float trail_infl = 0.0;
	vec2 trail_side = vec2(0.0);
	for (int i = 0; i < TRAIL_SIZE; i++) {
		float s = trail_strength[i];
		if (s <= 0.0) {
			continue;
		}
		vec2 d = root_xz - trail_positions[i].xz;
		float infl = s * (1.0 - smoothstep(0.0, trail_radius, length(d)));
		if (infl > trail_infl) {
			trail_infl = infl;
			// Perpendicular to the player's movement = the left/right axis.
			vec2 side = vec2(-trail_dirs[i].y, trail_dirs[i].x);
			// Push this blade toward whichever side of the path it is on.
			trail_side = side * sign(dot(d, side));
		}
	}

	// Get wind force and scale from process()
	float scale = pow(INSTANCE_CUSTOM[3] * INSTANCE_CUSTOM[3], 0.707);
	float force = INSTANCE_CUSTOM[2] * data[2] * scale;
	// Add some cheap jitter at high wind values
	force -= fract(force * 256.0) * force * 0.05;
	// Curve the result
	force = pow(force, 0.707);
	// Suppress the wind where the player has parted the grass, so it isn't blown
	// back into the player.
	force *= (1.0 - trail_infl);

	// These 2 combined result in a decent bend without resorting to matrices or pivot data.
	// Lateral move and wobble
	float lateral_wobble = sin(TIME * 2.0 * (1.0 + data.r + data.g)) * 0.25 * (1.0 - INSTANCE_CUSTOM[2]);
	vec2 direction = normalize(wind_direction);
	w_vertex.xz -= (vec2(-direction.y, direction.x) * lateral_wobble + direction) * force;
	// Flatten
	w_vertex.y -= INSTANCE_CUSTOM[2] * force * data[2];

	// Apply the parting bend: gentle sideways lean, tips move more than the base
	// (data[2]). Drop the tip slightly so the blade keeps its length - it bends
	// rather than stretching.
	if (trail_infl > 0.0) {
		vec2 bend = trail_side * trail_infl * trail_push * data[2];
		w_vertex.xz += bend;
		w_vertex.y -= length(bend) * 0.35;
	}

	// Save final wind force value for fragment.
	data[3] = force;

	VERTEX = (VIEW_MATRIX * vec4(w_vertex, 1.0)).xyz;
	NORMAL = MODELVIEW_NORMAL_MATRIX * NORMAL;
	BINORMAL = MODELVIEW_NORMAL_MATRIX * BINORMAL;
	TANGENT = MODELVIEW_NORMAL_MATRIX * TANGENT;
}

void fragment() {
	// Hard coded color.
	ALBEDO = vec3(0.20, 0.22, 0.05) * (data[2] * 0.5 + 0.5);
	ALBEDO.rg *= (data.rg * 0.3 + 0.9);
	ALBEDO *= pow(COLOR.rgb, vec3(2.2));
	// Modify roughness / specular based on wind force for added detail
	float spec_rough = clamp(max(data[2], data[3]), 0., 1.);
	ROUGHNESS = 1. - spec_rough;
	SPECULAR = clamp(spec_rough * 0.25, 0., .15);

	BACKLIGHT = vec3(0.33);
	#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	ALBEDO = pow(ALBEDO, vec3(0.4));
	#endif
}
