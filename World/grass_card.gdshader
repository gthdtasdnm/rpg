// Gras als texturierte Karten statt Einzelhalme aus Geometrie.
//
// Der urspruengliche Shader (addons/.../grass.gdshader) rechnet die Farbe und braucht
// deshalb fuer JEDEN Halm echte Geometrie - bei sichtbarer Dichte sind das ~900.000
// Ribbons. Hier ist ein Partikel EIN Bueschel: zwei Dreiecke mit einer Textur, auf der
// zwanzig Halme stehen. Gleiche Optik bei einem Bruchteil der Last.
//
// Wind, Spieler-Spur und Entfernungs-Ausblendung sind aus dem Original uebernommen.
shader_type spatial;
render_mode skip_vertex_transform, cull_disabled;

group_uniforms textur;
// 2x2-Atlas mit vier Bueschel-Varianten.
uniform sampler2D gras_atlas : source_color, filter_linear_mipmap, repeat_disable;
uniform float alpha_schwelle : hint_range(0.05, 0.9, 0.01) = 0.35;
// Ein paar Pixel Rand um jedes Atlasfeld, oben und an den Seiten. Zwei Gruende:
// Feld 2 hat ab Zeile 3 abgeschnittene Halmspitzen stehen, und beim Verkleinern
// mischen die Mipmaps ueber die Kachelgrenze - die dichte Wurzelzone des Feldes
// DARUEBER landet dann als Streifen auf der Oberkante. Unten wird nicht
// beschnitten, da sitzen die Wurzeln.
uniform float atlas_rand : hint_range(0.0, 16.0, 1.0) = 3.0;
uniform float atlas_groesse = 1024.0;
// Groesse einer Karte in Metern (Breite, Hoehe) vor der Instanz-Skalierung.
uniform vec2 karten_groesse = vec2(1.10, 0.90);
// Obergrenze der Instanz-Skalierung. Der Prozess-Shader addiert fuer "Clods"
// (dichtere Bueschelnester) bis zu +3.0 auf die Hoehe - ungedeckelt werden daraus
// 4 m hohe Karten.
// Groessenstreuung der Karten untereinander (unabhaengig vom Prozess-Shader).
uniform vec2 sk_bereich = vec2(0.75, 1.35);
// Ab welchem "Wachstumswert" des Prozess-Shaders ueberhaupt Gras steht.
// CUSTOM[3] ist dort scale.y = (Hoehe + Clods) * patch - also 0, wo kein Gras hin soll.
// Bewusst sehr niedrig: schon minimales Wachstum ergibt eine volle Karte.
// Hoeher gesetzt schluckt es genau die Bereiche, in denen der Prozess-Shader
// "duennes" Gras vorsieht - und das war der Grund, warum um den Spieler herum
// gar nichts stand.
uniform vec2 wachstum_schwelle = vec2(0.0, 0.02);
// Der Prozess-Shader hebt jeden Partikel um position_offset.y * scale an (0.45).
// Fuer ein Ribbon, das um seinen Mittelpunkt gebaut ist, stimmt das - eine Karte
// mit Fusspunkt unten wuerde dadurch schweben.
uniform float wurzel_versatz : hint_range(0.0, 1.5, 0.01) = 0.45;

group_uniforms groesse;
// Der Prozess-Shader legt je Instanz einen Wachstumswert ab (CUSTOM[3] =
// (Grundhoehe + Clods) * patch). Die "Clods" sind grossflaechige Nester aus dem
// main_noise - beim Halmgras waren genau das die Stellen mit hohem Gras. Der Wert
// diente hier bisher nur als Schalter "waechst hier ueberhaupt was", die Hoehe war
// fest. Damit war jede Flaeche gleich hoch.
// Wachstumswert, der der normalen Kartenhoehe entspricht. Der Prozess-Shader liefert
// ueblicherweise 0.5 bis 0.5 + clod_scale_boost.
uniform float wachstum_referenz : hint_range(0.1, 3.0, 0.05) = 0.75;
// Untere/obere Grenze des Hoehenfaktors. y hoch = auffaellige Hochgras-Inseln.
uniform vec2 wachstum_hoehe = vec2(0.7, 1.9);
// Streuung von Bueschel zu Bueschel, unabhaengig von der Flaeche.
uniform float hoehe_streuung : hint_range(0.0, 0.8, 0.01) = 0.25;
// Wieviel der Hoehenaenderung auch in die Breite geht. 0 = hohes Gras wird zum
// schmalen Streifen, 1 = es waechst gleichmaessig in alle Richtungen.
uniform float hoehe_in_breite : hint_range(0.0, 1.0, 0.01) = 0.35;

group_uniforms sorten;
// Welche Grasart wo waechst. Der Atlas enthaelt VIER Sorten (Feld 0..3), frueher hat
// sich jedes Bueschel aus allen vieren zusammengesetzt - ein Feld je Plane. Deshalb
// sah alles gleich aus, egal wo man stand. Jetzt bekommt jedes BUESCHEL eine Sorte,
// ausgewuerfelt aus einer Verteilungskarte: gleiche Karte -> gleiche Sorte, also
// Wiesen. An den Raendern liegen die Wahrscheinlichkeiten nah beieinander und beide
// Sorten stehen durcheinander - das ist die gewuenschte Ueberlappung.
uniform sampler2D sorten_karte : filter_linear_mipmap, repeat_enable;
// Weltmeter -> UV. 0.004 = eine Kachel je 250 m. Fuer eine von Hand gemalte Karte
// ueber die ganze Welt: 1/Weltgroesse, und sorten_versatz auf 0.5.
uniform float sorten_scale = 0.004;
uniform vec2 sorten_versatz = vec2(0.0);
// Aus zwei Werten der Karte wird ein Feld mit vier Ecken - je eine Sorte. Normal
// stammen sie aus zwei Proben derselben Textur; bei einer gemalten Karte stattdessen
// aus deren Rot- und Gruen-Kanal (dann kann man beide Achsen direkt malen).
uniform bool sorten_aus_kanaelen = false;
// 0 = weiche, breite Uebergaenge, 1 = scharfe Wiesengrenzen.
uniform float sorten_kontrast : hint_range(0.0, 1.0, 0.01) = 0.5;
// Anteil je Sorte (Atlasfeld 0..3). 0 = kommt nicht vor.
uniform vec4 sorten_gewicht = vec4(1.0, 1.0, 1.0, 1.0);
// Wie dicht die jeweilige Sorte steht (1 = alle Bueschel, 0.5 = jedes zweite).
uniform vec4 sorten_dichte = vec4(1.0, 1.0, 1.0, 1.0);
// Groesse je Sorte, multipliziert auf karten_groesse.
uniform vec4 sorten_hoehe = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 sorten_breite = vec4(1.0, 1.0, 1.0, 1.0);

group_uniforms draufsicht;
// Senkrechte Karten sind von oben unsichtbar (Flaeche = 0) - man sieht nur die Kanten.
// Dagegen helfen drei Dinge, alle ohne zusaetzliche Geometrie:
// 1. Karten dauerhaft nach aussen kippen: aus dem Kreuz wird ein Buschel/Faecher.
uniform float neigung : hint_range(0.0, 1.0, 0.01) = 0.3;
// 2. Beim Blick von oben staerker kippen. Erlaubt, weil man dann die Silhouette
//    ohnehin nicht sieht - und liefert genau dort Flaeche, wo sie fehlt.
uniform float neigung_von_oben : hint_range(0.0, 1.0, 0.01) = 0.45;
// Blickwinkel-Bereich als y der Richtung zur Kamera: x = ab wann gekippt wird,
// y = ab wann voll. (0.4 ~ 24 Grad ueber dem Boden, 0.9 ~ fast senkrecht drueber.)
uniform vec2 blickwinkel_bereich = vec2(0.4, 0.9);
// 3. Eine der vier Karten flach auf den Boden legen (Bodendecker).
uniform bool bodenkarte = true;
uniform float bodenkarte_index : hint_range(0.0, 3.0, 1.0) = 3.0;
uniform float boden_hoehe : hint_range(0.0, 0.3, 0.005) = 0.05;
uniform float boden_laenge : hint_range(0.5, 2.0, 0.05) = 1.15;
// Festes Atlasfeld fuer die liegende Karte (-1 = wie die anderen, zufaellig).
uniform float boden_feld : hint_range(-1.0, 3.0, 1.0) = -1.0;

group_uniforms wind;
uniform vec2 wind_direction = vec2(1.0, 1.0);
uniform float wind_fade_start : hint_range(5.0, 200.0, 5.0) = 45.0;
uniform float wind_fade_end : hint_range(10.0, 400.0, 5.0) = 90.0;

group_uniforms detail_distance;
uniform float trail_max_distance : hint_range(2.0, 60.0, 1.0) = 14.0;
// Ab hier wird ausgeduennt: ein wachsender Teil der Bueschel verschwindet mit der
// Entfernung. Perspektivisch deckt ein Bueschel in 80 m kaum Pixel ab - dort reicht
// ein Bruchteil fuer denselben Eindruck.
uniform float thin_start : hint_range(5.0, 200.0, 1.0) = 28.0;
uniform float thin_end : hint_range(10.0, 300.0, 1.0) = 95.0;
// Wieviel am Ende uebrig bleibt (0.25 = ein Viertel).
uniform float thin_rest : hint_range(0.05, 1.0, 0.01) = 0.22;
// Was ausgeduennt wird, waechst dafuer - so bleibt die Flaechendeckung erhalten.
uniform float thin_groesser : hint_range(1.0, 3.0, 0.05) = 1.7;

group_uniforms shading;
uniform vec3 gras_farbe : source_color = vec3(0.42, 0.52, 0.22);
uniform float base_ao : hint_range(0.0, 1.0, 0.01) = 0.55;
uniform float base_ao_height : hint_range(0.0, 1.0, 0.01) = 0.55;
// Wie stark die Normale zur Senkrechten gezogen wird. Karten mit echter Flaechennormale
// werden je nach Blickwinkel dunkel; Gras sieht mit "nach oben" gerichteter Normale besser aus.
uniform float normale_aufrecht : hint_range(0.0, 1.0, 0.01) = 0.75;

group_uniforms player_interaction;
const int TRAIL_SIZE = 16;
uniform vec3 trail_positions[TRAIL_SIZE];
uniform vec2 trail_dirs[TRAIL_SIZE];
uniform float trail_strength[TRAIL_SIZE];
uniform float trail_radius : hint_range(0.1, 4.0, 0.05) = 0.9;
uniform float trail_push : hint_range(0.0, 1.5, 0.05) = 0.55;

varying vec2 atlas_uv;
varying float hoehe;       // 0 = Wurzel, 1 = Spitze
varying float wind_kraft;
varying vec3 farb_schub;


float hash12(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

void vertex() {
	vec3 wurzel = MODEL_MATRIX[3].xyz;
	// Die Skalierung des Prozess-Shaders ist komplett auf duenne Ribbons ausgelegt:
	// X/Z werden mit der Entfernung BREITER gemacht (width_modifier 1..4), Y traegt
	// Hoehe + Clods + Patch. Beides ergibt fuer eine Karte unbrauchbare Groessen -
	// direkt vor der Kamera kam 0,14 m heraus.
	// Deshalb hier: eigene, entfernungsunabhaengige Kartengroesse. CUSTOM[3] dient
	// nur noch als Schalter "waechst hier ueberhaupt Gras" (patch/Randausblendung).
	float wachstum = INSTANCE_CUSTOM[3];
	float da = smoothstep(wachstum_schwelle.x, wachstum_schwelle.y, wachstum);
	float sk = mix(sk_bereich.x, sk_bereich.y, hash12(MODEL_MATRIX[3].xz * 1.93)) * da;
	// Partikel sitzt bereits angehoben - die Karte wieder auf den Boden setzen.
	wurzel.y -= wurzel_versatz * INSTANCE_CUSTOM[3];

	float cam_dist = distance(wurzel, CAMERA_POSITION_WORLD);
	float rnd = hash12(wurzel.xz * 7.31);

	// --- Ausduennen mit der Entfernung ---
	// Kein "return" - das ist in vertex() nicht erlaubt. Stattdessen Skalierung auf 0:
	// alle drei Eckpunkte fallen auf die Wurzel zusammen, das Dreieck entartet und
	// wird gar nicht erst gerastert.
	float verblasst = smoothstep(thin_start, thin_end, cam_dist);
	float behalten = mix(1.0, thin_rest, verblasst);
	float sichtbar = rnd <= behalten ? 1.0 : 0.0;
	// Die verbliebenen wachsen, damit die Flaechendeckung nicht einbricht.
	sk *= mix(1.0, thin_groesser, verblasst) * sichtbar;

	bool ist_boden = bodenkarte && abs(UV2.x - bodenkarte_index) < 0.5;

	// --- Sorte des Bueschels aus der Verteilungskarte ---
	// Zwei Werte spannen ein Quadrat auf, dessen vier Ecken die vier Atlasfelder sind.
	// Damit ergibt jede Kombination eine Wiese, und zwischen zwei Wiesen liegt ein
	// Streifen, in dem beide Sorten gemischt stehen.
	vec2 s_uv = wurzel.xz * sorten_scale + sorten_versatz;
	// textureLod, nicht texture: im Vertexshader gibt es keine Ableitungen, aus denen
	// die Mipstufe kommen koennte - je nach Treiber ist das Ergebnis sonst undefiniert.
	vec4 karte = textureLod(sorten_karte, s_uv, 0.0);
	float n1 = karte.r;
	float n2 = sorten_aus_kanaelen ? karte.g
			: textureLod(sorten_karte, s_uv * 0.73 + vec2(0.37, 0.71), 0.0).r;
	float schaerfe = mix(1.0, 14.0, sorten_kontrast);
	n1 = clamp((n1 - 0.5) * schaerfe + 0.5, 0.0, 1.0);
	n2 = clamp((n2 - 0.5) * schaerfe + 0.5, 0.0, 1.0);
	vec4 gewicht = vec4((1.0 - n1) * (1.0 - n2), n1 * (1.0 - n2),
						(1.0 - n1) * n2, n1 * n2) * sorten_gewicht;
	// Sorte je Bueschel wuerfeln statt den Uebergang zu mischen: eine halb-blaue,
	// halb-gruene Textur gibt es nicht - gemischt wird ueber die Anzahl der Bueschel.
	float summe = max(gewicht.x + gewicht.y + gewicht.z + gewicht.w, 1e-5);
	float wahl = hash12(wurzel.xz * 11.7) * summe;
	vec4 maske = vec4(0.0, 0.0, 0.0, 1.0);
	if (wahl < gewicht.x) { maske = vec4(1.0, 0.0, 0.0, 0.0); }
	else if (wahl < gewicht.x + gewicht.y) { maske = vec4(0.0, 1.0, 0.0, 0.0); }
	else if (wahl < gewicht.x + gewicht.y + gewicht.z) { maske = vec4(0.0, 0.0, 1.0, 0.0); }
	float feld = dot(maske, vec4(0.0, 1.0, 2.0, 3.0));

	// Sortenabhaengige Dichte und Groesse.
	sk *= hash12(wurzel.zx * 4.41) <= dot(maske, sorten_dichte) ? 1.0 : 0.0;
	vec2 kg = karten_groesse * vec2(dot(maske, sorten_breite), dot(maske, sorten_hoehe));

	// --- Hoehenvariation ---
	// Flaechig aus dem Wachstumswert (Clod-Nester des Prozess-Shaders) und dazu eine
	// Streuung je Bueschel, damit auch innerhalb einer Flaeche nicht alles gleich
	// hoch steht.
	float aus_flaeche = clamp(wachstum / max(wachstum_referenz, 0.001),
			wachstum_hoehe.x, wachstum_hoehe.y);
	float je_bueschel = mix(1.0 - hoehe_streuung, 1.0 + hoehe_streuung,
			hash12(wurzel.xz * 2.13));
	float h_faktor = aus_flaeche * je_bueschel;
	kg.y *= h_faktor;
	kg.x *= mix(1.0, h_faktor, hoehe_in_breite);

	// Die liegende Karte zeigt ein Bueschel von der SEITE, flach hingelegt. Das geht
	// durch, ist aber nicht dasselbe wie eine Aufsicht. Sobald ein von oben
	// fotografiertes Bueschel im Atlas liegt, hier dessen Feld eintragen (0..3).
	if (ist_boden && boden_feld >= 0.0) { feld = boden_feld; }
	vec2 feld_uv = vec2(mod(feld, 2.0), floor(feld * 0.5)) * 0.5;
	vec2 lokal = UV;
	if (hash12(wurzel.zx * 5.11 + UV2.x) > 0.5) { lokal.x = 1.0 - lokal.x; }
	// UV.y = 0 ist die Kartenspitze, also die OBERE Kante des Feldes - dort der Rand,
	// unten bleibt das Feld bis zur Kante erhalten.
	float rd = atlas_rand / max(atlas_groesse, 1.0);
	atlas_uv = feld_uv + vec2(lokal.x * (0.5 - 2.0 * rd) + rd,
							  lokal.y * (0.5 - rd) + rd);
	hoehe = 1.0 - UV.y;

	// --- Feste Ausrichtung, KEIN Billboard ---
	// Die Drehung kommt aus der Instanz (random_rotation + align_to_normal im
	// Prozess-Material). Nur die Groesse ist eigene Rechnung - die Skalierung des
	// Prozess-Shaders ist auf duenne Ribbons ausgelegt und hier unbrauchbar.
	mat3 dreh = mat3(normalize(MODEL_MATRIX[0].xyz),
					 normalize(MODEL_MATRIX[1].xyz),
					 normalize(MODEL_MATRIX[2].xyz));
	vec3 lokal_pos = vec3(VERTEX.x * kg.x,
						  VERTEX.y * kg.y,
						  VERTEX.z * kg.x) * sk;
	// Kipprichtung = Normale der Plane, im Mesh-Raum und auf die Waagerechte
	// projiziert. Ohne die Projektion wuerde eine Plane, die im Mesh nicht exakt
	// senkrecht steht, beim Kippen zusaetzlich in der Hoehe verrutschen.
	vec3 n_l = vec3(NORMAL.x, 0.0, NORMAL.z);
	float n_len = length(n_l);
	n_l = n_len > 0.001 ? n_l / n_len : vec3(1.0, 0.0, 0.0);
	// Planes 0/2 kippen nach +n, 1/3 nach -n. Alle in dieselbe Richtung gekippt
	// haengt das ganze Bueschel schief, weil die vier Normalen nur einen Halbraum
	// abdecken.
	float kipp_seite = mod(UV2.x, 2.0) < 0.5 ? 1.0 : -1.0;

	// --- Blickwinkel: 0 = von der Seite, 1 = senkrecht von oben ---
	// cam_dist statt normalize(): steht die Kamera exakt auf der Wurzel, waere die
	// Normalisierung 0/0.
	float von_oben = smoothstep(blickwinkel_bereich.x, blickwinkel_bereich.y,
			(CAMERA_POSITION_WORLD.y - wurzel.y) / max(cam_dist, 0.001));

	vec3 n_lokal = vec3(0.0, 1.0, 0.0);
	if (ist_boden) {
		// Eine der vier Karten liegt flach auf dem Boden. Von oben ist sie die
		// einzige, die ueberhaupt Flaeche zeigt - von der Seite sieht man sie
		// kaum, weil sie dann auf der Kante steht. Kostet nichts: dieselben zwei
		// Dreiecke, nur anders ausgerichtet.
		// Hoehe wird zur Tiefe entlang der Normalen, um den Fusspunkt zentriert.
		lokal_pos = vec3(lokal_pos.x, boden_hoehe * sk, lokal_pos.z)
				+ n_l * (lokal_pos.y - 0.5 * kg.y * sk) * boden_laenge;
		// Sonst liefe der AO-Verlauf des Fragmentshaders quer ueber die liegende
		// Karte und ein Rand davon waere fast schwarz.
		hoehe = 0.8;
	} else {
		// Karten nach aussen kippen. Senkrechte Karten haben in der Draufsicht
		// null Flaeche - das ist der Grund, warum es von oben nach Kanten aussieht.
		// Beim Blick von oben wird zusaetzlich gekippt: dann sieht man die
		// Silhouette ohnehin nicht, und die Flaeche zeigt zur Kamera.
		float winkel = clamp(neigung + von_oben * neigung_von_oben, 0.0, 1.0) * 1.35;
		float y = lokal_pos.y;
		lokal_pos += n_l * (y * sin(winkel) * kipp_seite);
		lokal_pos.y = y * cos(winkel);
		// Die Normale kippt mit - sonst bleibt eine fast liegende Karte waagerecht
		// beleuchtet wie eine senkrechte und wird von oben dunkel. Von den beiden
		// Seiten der Flaeche (cull_disabled) nehmen wir die nach oben zeigende.
		n_lokal = normalize(n_l * cos(winkel) - vec3(0.0, 1.0, 0.0) * sin(winkel) * kipp_seite);
		n_lokal = n_lokal.y < 0.0 ? -n_lokal : n_lokal;
	}

	vec3 w_vertex = wurzel + dreh * lokal_pos;
	vec3 plane_n = normalize(dreh * n_lokal);

	// --- Spieler-Spur (nur in Naehe pruefen) ---
	float trail_infl = 0.0;
	vec2 trail_side = vec2(0.0);
	if (cam_dist < trail_max_distance) {
		for (int i = 0; i < TRAIL_SIZE; i++) {
			float s = trail_strength[i];
			if (s <= 0.0) { continue; }
			vec2 d = wurzel.xz - trail_positions[i].xz;
			float infl = s * (1.0 - smoothstep(0.0, trail_radius, length(d)));
			if (infl > trail_infl) {
				trail_infl = infl;
				vec2 seite = vec2(-trail_dirs[i].y, trail_dirs[i].x);
				trail_side = seite * sign(dot(d, seite));
			}
		}
	}

	// --- Wind ---
	float force = INSTANCE_CUSTOM[2] * hoehe * hoehe;
	force = pow(max(force, 0.0), 0.707);
	// Die liegende Karte hat keinen Hoehenverlauf mehr, ueber den sich die Auslenkung
	// verteilen koennte - voller Wind wuerde sie als Ganzes ueber den Boden schieben.
	force *= ist_boden ? 0.15 : 1.0;
	force *= (1.0 - trail_infl);
	force *= 1.0 - smoothstep(wind_fade_start, wind_fade_end, cam_dist);
	float wobble = sin(TIME * 2.0 * (1.0 + rnd * 2.0)) * 0.22 * (1.0 - INSTANCE_CUSTOM[2]);
	vec2 richtung = normalize(wind_direction);
	w_vertex.xz -= (vec2(-richtung.y, richtung.x) * wobble + richtung) * force * 0.45 * sk;
	w_vertex.y -= force * hoehe * 0.20 * sk;

	if (trail_infl > 0.0) {
		vec2 bend = trail_side * trail_infl * trail_push * hoehe;
		w_vertex.xz += bend * sk;
		w_vertex.y -= length(bend) * 0.35 * sk;
	}

	wind_kraft = force;
	farb_schub = vec3(INSTANCE_CUSTOM.r * 0.3 + 0.9, INSTANCE_CUSTOM.g * 0.3 + 0.9, 1.0);

	VERTEX = (VIEW_MATRIX * vec4(w_vertex, 1.0)).xyz;
	// Normale zwischen Planeflaeche und Senkrechter mischen. Reine Flaechennormalen
	// lassen Gras je nach Sonnenstand schwarz werden.
	vec3 n = normalize(mix(plane_n, vec3(0.0, 1.0, 0.0), normale_aufrecht));
	NORMAL = (VIEW_MATRIX * vec4(n, 0.0)).xyz;
}

void fragment() {
	vec4 tex = texture(gras_atlas, atlas_uv);
	ALPHA = tex.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_schwelle;

	vec3 col = tex.rgb * gras_farbe * 2.0;
	col *= farb_schub;
	// Fake-AO am Bueschelgrund - dichtes Gras liest sich als dunklerer Boden
	col *= mix(base_ao, 1.0, smoothstep(0.0, base_ao_height, hoehe));
	// Spitzen leicht aufhellen
	col = mix(col, col * 1.25, smoothstep(0.75, 1.0, hoehe));
	ALBEDO = col;

	float spec_rough = clamp(max(hoehe, wind_kraft), 0.0, 1.0);
	ROUGHNESS = 1.0 - spec_rough * 0.6;
	SPECULAR = clamp(spec_rough * 0.2, 0.0, 0.12);
	BACKLIGHT = vec3(0.38);
}
