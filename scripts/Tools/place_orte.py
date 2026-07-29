"""Erzeugt World/orte.tscn - eine Blocking-Box je Ort, als Platzhalter/Zeichen.

Kein Nachbau von Gebaeuden: jede Box sagt nur "hier steht spaeter etwas, ungefaehr
so gross". Die festen Orte kommen aus doc/konzept/ (Lore), die uebrigen findet das
Skript selbst nach Gelaendekriterien.
"""
import numpy as np, os, math

SP = "/tmp/claude-1000/-home-cengiz-projects-RPG-rpg/5b875abc-7995-483a-8e40-b68729ffe26c/scratchpad"
PROJ = "/home/cengiz/projects/RPG/rpg"
H = np.load(SP+"/H.npy"); SLOPE = np.load(SP+"/slope.npy"); EFF = np.load(SP+"/eff.npy")
N = H.shape[0]
rng = np.random.default_rng(11)

def hq(x, z):
    fx, fz = x+1024.0, z+1024.0
    x0, z0 = int(np.floor(fx)), int(np.floor(fz))
    x0 = min(max(x0,0),N-2); z0 = min(max(z0,0),N-2)
    tx, tz = fx-x0, fz-z0
    a = H[z0,x0]*(1-tx)+H[z0,x0+1]*tx
    b = H[z0+1,x0]*(1-tx)+H[z0+1,x0+1]*tx
    return float(a*(1-tz)+b*tz)

def boxmean(a, k):
    a = np.nan_to_num(a).astype('f8')
    c = np.cumsum(np.cumsum(a,0),1); c = np.pad(c,((1,0),(1,0)))
    h,w = a.shape; out = np.zeros(a.shape); r = k//2
    I = np.arange(r,h-r)[:,None]; J = np.arange(r,w-r)[None,:]
    out[r:h-r,r:w-r] = (c[I+r+1,J+r+1]-c[I-r,J+r+1]-c[I+r+1,J-r]+c[I-r,J-r])/((2*r+1)**2)
    return out

ROAD = (EFF == 2); RIVERB = (EFF == 3)
flat30  = boxmean((SLOPE < 11).astype('f4'), 31)
flat60  = boxmean((SLOPE < 11).astype('f4'), 61)
wall    = boxmean((SLOPE > 55).astype('f4'), 81)
road_n  = boxmean(ROAD.astype('f4'), 61)
river_n = boxmean(RIVERB.astype('f4'), 61)
touch   = boxmean(((np.abs(H) > 0.05) | (EFF > 0)).astype('f4'), 61)
hi_avg  = boxmean(H, 81)

yy, xx = np.mgrid[0:N, 0:N]
WX = (xx-1024).astype('f4'); WZ = (yy-1024).astype('f4')
NETH = (-615.0, -570.0)
d_neth = np.hypot(WX-NETH[0], WZ-NETH[1])

tm = touch > 0.5
CX, CZ = float(WX[tm].mean()), float(WZ[tm].mean())

out = ['[node name="Orte" type="Node3D"]\n']
belegt = []
EXPORT = []   # (x, z, Freihalteradius) fuer place_vegetation.py

def frei(x, z, r):
    for (px, pz, pr) in belegt:
        if (px-x)**2 + (pz-z)**2 < (r+pr)**2: return False
    return True

def marke(gruppe, name, x, z, w, h, d, yaw=0.0, sperre=None):
    """Eine Box als Zeichen. Steckt zum Teil im Boden, damit sie am Hang nicht schwebt."""
    y = hq(x, z) + h*0.5 - min(h*0.18, 2.0)
    cy, sy = math.cos(yaw), math.sin(yaw)
    M = [cy,0,sy, 0,1,0, -sy,0,cy]
    nums = ", ".join(("%.2f"%f).rstrip('0').rstrip('.') or "0" for f in M+[x,y,z])
    out.append('[node name="%s" type="CSGBox3D" parent="%s"]\n'
               'transform = Transform3D(%s)\nsize = Vector3(%.1f, %.1f, %.1f)\n'
               % (name, gruppe, nums, w, h, d))
    belegt.append((x, z, sperre if sperre else max(w,d)*0.75))
    EXPORT.append((x, z, max(w, d)*0.6 + 6.0))

def gruppe(name):
    out.append('[node name="%s" type="Node3D" parent="."]\n' % name)

def suche(mask, score, n, minsep):
    s = np.where(mask, score, -1e9); res = []
    for _ in range(n*40):
        if len(res) >= n: break
        i = np.unravel_index(s.argmax(), s.shape)
        if s[i] <= -1e8: break
        x, z = float(WX[i]), float(WZ[i])
        s[np.hypot(WX-x, WZ-z) < minsep] = -1e9
        if not frei(x, z, minsep*0.30): continue
        res.append((x, z))
    return res

# Startkorridor und Nethora bleiben frei - dort steht schon Handarbeit,
# und die Ankunftsszene darf kein Blocking-Klotz stoeren.
START = (-698.0, -861.0)
GESPERRT = (np.hypot(WX-START[0], WZ-START[1]) < 150) \
         | ((WX > -720) & (WX < -510) & (WZ > -700) & (WZ < -440))

BASIS = (touch > 0.55) & (~ROAD) & (~RIVERB) & (H > 0.1) & (~GESPERRT)

# =============================================================== feste Orte
gruppe("Lore_Orte")
FEST = [
    ("Freies_Lager",         -605,  -165,  70,  14,  70, "Berghoehlengewoelbe der Freien"),
    ("Alte_Kathedrale",      -592,  -269,  36,  22,  18, "Ruine aus der Zeit vor der Barriere"),
    ("Erbauer_Tempel_Ost",   -106,  -399,  42,  26,  42, "Aeusserer Tempel, Plateau 50 m"),
    ("Mine_Nethora",         -862,  -306,  24,  16,  24, "Aufgegebene Mine im Westgebirge"),
    ("Gehoeft_Verlassen",    -312,  -626,  28,  10,  24, "Verlassenes Gehoeft an der Suedroute"),
    ("Faehrstelle_Aldous",   -431,  -787,  12,   7,  10, "Flussquerung, Faehrmann"),
    ("Rastplatz_Ostroute",   -364,  -807,  10,   4,  10, "Lagerfeuer an der Strasse"),
    ("Wegschrein_Ankunft",   -676,  -917,   4,   6,   4, "Erster Schrein hinter dem Tor"),
]
for name, x, z, w, h, d, note in FEST:
    marke("Lore_Orte", name, x, z, w, h, d, rng.uniform(0, 3.14))
    print("%-22s x=%6d z=%6d  h=%5.1f  %2dx%2dx%2d  %s" % (name, x, z, hq(x,z), w, h, d, note))

# =============================================================== Pentagramm
gruppe("Erbauer_Pentagramm")
marke("Erbauer_Pentagramm", "Haupttempel_Nethora", NETH[0]+40, NETH[1]+70, 34, 20, 34)
print("\nPentagramm (Haupttempel bei Nethora + 4 aeussere):")
for k in range(4):
    a = k * (2*math.pi/5) + 0.9
    tx, tz = CX + math.cos(a)*470, CZ + math.sin(a)*470
    m = BASIS & (flat30 > 0.70) & (np.hypot(WX-tx, WZ-tz) < 230) & (d_neth > 300)
    if not m.any():
        print("   Tempel_%d: kein Platz im Umkreis" % (k+1)); continue
    p = suche(m, -np.hypot(WX-tx, WZ-tz)/300 + flat60, 1, 120)
    if not p: continue
    x, z = p[0]
    marke("Erbauer_Pentagramm", "Tempel_%d" % (k+1), x, z, 34, 22, 34, rng.uniform(0,3.14))
    print("   Tempel_%d  x=%6.0f z=%6.0f  h=%5.1f" % (k+1, x, z, hq(x,z)))

# =============================================================== weitere Orte
print()
def stapel(gname, label, mask, score, n, minsep, w, h, d, sperre=None, jitter=False):
    gruppe(gname)
    pts = suche(mask, score, n, minsep)
    for i,(x,z) in enumerate(pts):
        ww = float(rng.uniform(w*0.7, w*1.35)) if jitter else w
        dd = float(rng.uniform(d*0.7, d*1.35)) if jitter else d
        hh = float(rng.uniform(h*0.6, h*1.4)) if jitter else h
        marke(gname, "%s_%d" % (label, i+1), x, z, ww, hh, dd, rng.uniform(0,6.28), sperre)
    print("%-18s %d" % (gname+":", len(pts)))
    return pts

stapel("Wegschreine", "Schrein",
       BASIS & (flat30 > 0.7) & (road_n > 0.004) & (road_n < 0.5),
       road_n*3, 26, 60, 3.5, 5.5, 3.5, sperre=14)

stapel("Ruinen", "Ruine",
       BASIS & (flat30 > 0.75) & (road_n < 0.03) & (d_neth > 170),
       rng.random(H.shape)*0.4 + flat60*0.6, 30, 100, 15, 7, 13, jitter=True)

stapel("Hoehlen", "Hoehle",
       BASIS & (flat30 > 0.5) & (wall > 0.03),
       wall*4, 24, 90, 11, 8, 9, sperre=24)

stapel("Lagerplaetze", "Lager",
       BASIS & (flat30 > 0.75) & (d_neth > 150),
       rng.random(H.shape), 30, 80, 12, 4, 12, sperre=18)

stapel("Wachtuerme", "Turm",
       BASIS & (H > hi_avg + 2.5) & (flat30 > 0.22) & (H > 10),
       H/120, 26, 95, 7, 16, 7, sperre=18)

pts = stapel("Flussquerungen", "Bruecke",
       (river_n > 0.08) & (river_n < 0.70) & (road_n > 0.002) & (~GESPERRT),
       road_n*4, 10, 100, 9, 5, 26, sperre=22)
if len(pts) < 4:
    gruppe("Furten")
    p2 = suche((river_n > 0.15) & (river_n < 0.65) & (touch > 0.5) & (~GESPERRT), -d_neth/2000, 10, 150)
    for i,(x,z) in enumerate(p2):
        marke("Furten", "Furt_%d" % (i+1), x, z, 8, 3, 22, rng.uniform(0,6.28), sperre=22)
    print("%-18s %d" % ("Furten:", len(p2)))

stapel("Kultplaetze", "Steinkreis",
       BASIS & (flat30 > 0.72) & (road_n < 0.02) & (d_neth > 200),
       rng.random(H.shape), 16, 150, 18, 4, 18, sperre=26)

with open(os.path.join(PROJ, "World/orte.tscn"), "w") as f:
    f.write("[gd_scene format=3]\n\n" + "\n".join(out))
import json
json.dump(EXPORT, open(SP+"/orte_positions.json","w"))
boxen = sum(1 for o in out if "CSGBox3D" in o)
print("\ngeschrieben: World/orte.tscn  (%d Boxen)" % boxen)
