"""Findet freie ebene Flaechen innerhalb der Barriere, gibt jeder eine Bedeutung
und gestaltet sie grob aus.

Ablauf:
  1. Belegungskarte aus allem bauen, was schon steht (bewuchs, orte, world)
  2. Freie, ebene, zusammenhaengende Gebiete finden
  3. Je Gebiet aus den Gelaendemerkmalen einen Typ ableiten
  4. Markierungsbox ueber die ganze Flaeche + typgerechte Ausgestaltung

Schreibt World/flaechen.tscn. Ruehrt bewuchs.tscn und orte.tscn nicht an.
"""
import numpy as np, os, re, math, json
from collections import deque

SP = "/tmp/claude-1000/-home-cengiz-projects-RPG-rpg/5b875abc-7995-483a-8e40-b68729ffe26c/scratchpad"
PROJ = "/home/cengiz/projects/RPG/rpg"

H     = np.load(SP+"/H.npy")
SLOPE = np.load(SP+"/slope.npy")
EFF   = np.load(SP+"/eff.npy")
N = H.shape[0]
rng = np.random.default_rng(4242)

# ------------------------------------------------------------------ Gelaende
def hq(x, z):
    fx, fz = x+1024.0, z+1024.0
    x0, z0 = int(np.floor(fx)), int(np.floor(fz))
    x0 = min(max(x0,0),N-2); z0 = min(max(z0,0),N-2)
    tx, tz = fx-x0, fz-z0
    a = H[z0,x0]*(1-tx)+H[z0,x0+1]*tx
    b = H[z0+1,x0]*(1-tx)+H[z0+1,x0+1]*tx
    return float(a*(1-tz)+b*tz)

def at(arr, x, z):
    return arr[min(max(int(round(z))+1024,0),N-1), min(max(int(round(x))+1024,0),N-1)]

def boxmean(a, k):
    a = np.nan_to_num(a).astype('f8')
    c = np.cumsum(np.cumsum(a,0),1); c = np.pad(c,((1,0),(1,0)))
    h,w = a.shape; out = np.zeros(a.shape); r = k//2
    I = np.arange(r,h-r)[:,None]; J = np.arange(r,w-r)[None,:]
    out[r:h-r,r:w-r] = (c[I+r+1,J+r+1]-c[I-r,J+r+1]-c[I+r+1,J-r]+c[I-r,J-r])/((2*r+1)**2)
    return out

yy, xx = np.mgrid[0:N, 0:N]
WX = (xx-1024).astype('f4'); WZ = (yy-1024).astype('f4')

ROAD   = (EFF == 2)
RIVERB = (EFF == 3)
TOUCH  = (np.abs(H) > 0.05) | (EFF > 0)
road_n  = boxmean(ROAD.astype('f4'), 25)
river_n = boxmean(RIVERB.astype('f4'), 41)
steep_n = boxmean((SLOPE > 48).astype('f4'), 61)
touch_n = boxmean(TOUCH.astype('f4'), 41)
hi_avg  = boxmean(H, 121)

# ------------------------------------------------------------------ Barriere
def barriere():
    s = open(PROJ+"/World/world.tscn", encoding="utf-8", errors="surrogateescape").read()
    m = re.search(r'\[node name="Barriere"[^\]]*\]\ntransform = Transform3D\(([^)]*)\)', s)
    v = [float(x) for x in m.group(1).split(",")]
    return v[9], v[11], 1450.0*math.hypot(v[0], v[2])
BX, BZ, BR = barriere()
print("Barriere: Mitte (%.0f, %.0f), Radius %.0f m" % (BX, BZ, BR))

# ------------------------------------------------------------------ Belegung
def objekte(pfad, muster, gruppe=1, tf=2):
    s = open(PROJ+"/"+pfad, encoding="utf-8", errors="surrogateescape").read()
    out = []
    for m in re.finditer(muster, s):
        t = [float(x) for x in m.group(tf).split(",")]
        sk = math.sqrt(t[0]**2 + t[1]**2 + t[2]**2)
        out.append((t[9], t[11], sk, m.group(gruppe)))
    return out

DICHTE = np.zeros((N,N), np.float32)
BELEGT = np.zeros((N,N), bool)
def punkt(x, z, gew=1.0):
    ix, iz = int(round(x))+1024, int(round(z))+1024
    if 0 <= ix < N and 0 <= iz < N: DICHTE[iz, ix] += gew
def stanze(x, z, r):
    ix, iz = int(round(x))+1024, int(round(z))+1024
    ri = int(math.ceil(r))
    x0,x1 = max(ix-ri,0), min(ix+ri+1,N)
    z0,z1 = max(iz-ri,0), min(iz+ri+1,N)
    if x0>=x1 or z0>=z1: return
    sub = BELEGT[z0:z1, x0:x1]
    gx = np.arange(x0,x1)[None,:] - ix
    gz = np.arange(z0,z1)[:,None] - iz
    sub |= (gx*gx + gz*gz) <= r*r

n_obj = 0
for pfad, must in [
    ("World/bewuchs.tscn", r'\[node name="[^"]+" parent="([^"]+)"[^\]]*instance=ExtResource\("[^"]+"\)\]\ntransform = Transform3D\(([^)]*)\)'),
    ("World/world.tscn",   r'\[node name="[^"]+" parent="([^"]+)"[^\]]*instance=ExtResource\("[^"]+"\)\]\ntransform = Transform3D\(([^)]*)\)'),
]:
    for x, z, sk, grp in objekte(pfad, must):
        # Radius nach tatsaechlichem Platzbedarf, gedeckelt. Ohne Deckel stanzt
        # ein Fels mit Skalierung 19 einen Kreis von 170 m aus.
        if grp == "Felsen":     r = min(5.0 + 0.8*sk, 18.0)
        elif grp == "Baeume":   r = min(6.0*max(sk, 0.6), 11.0)
        else:                   r = 3.5
        stanze(x, z, r); punkt(x, z, r*r*0.01); n_obj += 1
for m in re.finditer(r'\[node name="[^"]+" type="CSGBox3D" parent="([^"]+)"\]\ntransform = Transform3D\(([^)]*)\)\nsize = Vector3\(([^)]*)\)',
                     open(PROJ+"/World/orte.tscn", encoding="utf-8", errors="surrogateescape").read()):
    t = [float(x) for x in m.group(2).split(",")]
    sz = [float(x) for x in m.group(3).split(",")]
    _r = max(sz[0], sz[2])*0.8 + 14.0
    stanze(t[9], t[11], _r); punkt(t[9], t[11], _r*_r*0.02); n_obj += 1
print("Belegung aus %d bestehenden Objekten" % n_obj)

# ------------------------------------------------------------------ freie Flaechen
# Zwei bewusste Entscheidungen:
#  - KEIN touch_n-Filter. Die grosse ebene Talsohle ist unbemalt und liegt auf
#    exakt 0 - genau deshalb faellt sie auf, und genau deshalb gehoert sie dazu.
#  - Belegung als DICHTE, nicht als harte Kreise. Sonst zerfaellt jede Flaeche in
#    Konfetti zwischen den bestehenden Baeumen und es bleibt nichts Zusammenhaengendes.
dichte = boxmean(DICHTE, 61)                    # ~30 m Umkreis
BODEN = (~ROAD) & (~RIVERB) & (SLOPE < 18) & (H > -0.5) \
      & (road_n < 0.02) & (river_n < 0.05) \
      & (((WX-BX)**2 + (WZ-BZ)**2) < (BR-25.0)**2)
BODEN &= ~((WX > -730) & (WX < -500) & (WZ > -710) & (WZ < -430))   # Nethora
BODEN &= (np.hypot(WX+698, WZ+861) > 130)                            # Startkorridor

SCHWELLE = 0.0035
FREI = BODEN & (dichte < SCHWELLE)
# Loecher schliessen und Fransen abschneiden, damit zusammenhaengende Gebiete entstehen
FREI = (boxmean(FREI.astype("f4"), 41) > 0.45) & BODEN
print("freie Flaeche: %.1f ha (Dichteschwelle %.4f)" % (FREI.sum()/1e4, SCHWELLE))

# --- Zusammenhaengende Gebiete auf 4-m-Raster ---
S = 4
klein = FREI[::S, ::S]
hK, wK = klein.shape
label = np.zeros((hK, wK), np.int32)
gebiete = []
lid = 0
for sz in range(hK):
    for sx in range(wK):
        if not klein[sz,sx] or label[sz,sx]: continue
        lid += 1
        q = deque([(sz,sx)]); label[sz,sx] = lid; zellen = []
        while q:
            cz, cx = q.popleft(); zellen.append((cz,cx))
            for dz,dx in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)):
                nz, nx = cz+dz, cx+dx
                if 0<=nz<hK and 0<=nx<wK and klein[nz,nx] and not label[nz,nx]:
                    label[nz,nx] = lid; q.append((nz,nx))
        if len(zellen)*S*S >= 1800:      # mind. ~42 x 42 m
            gebiete.append(zellen)

# Grosse Gebiete in Teilstuecke zerlegen. Ein 7-ha-Klumpen ist keine "Flaeche"
# mehr, sondern eine Landschaft - der bekommt sonst zwangslaeufig einen
# Sammeltyp und wird nichts Halbes und nichts Ganzes.
MAX_HA = 1.8
KACHEL = 120.0
zerlegt = []
for zellen in gebiete:
    if len(zellen)*S*S <= MAX_HA*1e4:
        zerlegt.append(zellen); continue
    eimer = {}
    for cz, cx in zellen:
        wx, wz = cx*S-1024.0, cz*S-1024.0
        eimer.setdefault((int(wx//KACHEL), int(wz//KACHEL)), []).append((cz,cx))
    for teil in eimer.values():
        if len(teil)*S*S >= 1800: zerlegt.append(teil)
gebiete = zerlegt
print("%d zusammenhaengende Flaechen ab 0,22 ha" % len(gebiete))

# ------------------------------------------------------------------ Merkmale + Typ
def merkmale(zellen):
    arr = np.array(zellen)
    wz = arr[:,0]*S - 1024.0
    wx = arr[:,1]*S - 1024.0
    flaeche = len(zellen)*S*S
    cx, cz = float(wx.mean()), float(wz.mean())
    bx0, bx1 = float(wx.min()), float(wx.max())
    bz0, bz1 = float(wz.min()), float(wz.max())
    hs = np.array([at(H, x, z) for x, z in zip(wx, wz)])
    umgeb = np.array([at(hi_avg, x, z) for x, z in zip(wx, wz)])
    return dict(
        zellen=list(zip(wx, wz)), flaeche=flaeche, cx=cx, cz=cz,
        bx0=bx0, bx1=bx1, bz0=bz0, bz1=bz1,
        breite=bx1-bx0+S, tiefe=bz1-bz0+S,
        hoehe=float(hs.mean()), erhoben=float((hs-umgeb).mean()),
        # Anteil der Flaeche mit dem Merkmal, nicht das Maximum. Sonst gilt eine
        # 7-ha-Flaeche als "am Wasser", nur weil eine Ecke den Fluss streift.
        strasse=float(np.mean([at(road_n, x, z)  > 0.004 for x, z in zip(wx, wz)])),
        wasser=float(np.mean([at(river_n, x, z) > 0.02  for x, z in zip(wx, wz)])),
        wand=float(np.mean([at(steep_n, x, z)   > 0.04  for x, z in zip(wx, wz)])),
        d_neth=float(math.hypot(cx+615, cz+570)),
        d_start=float(math.hypot(cx+698, cz+861)),
    )

def typ_waehlen(f, idx):
    """Jede Flaeche bekommt eine Bedeutung - auch die, die leer bleibt.

    Zuerst entscheiden starke Gelaendesignale (Wasser, Strasse, Erhebung, Wand).
    Was uebrig bleibt, wird bewusst durchgewechselt - sonst bekommt die halbe
    Karte denselben Typ, nur weil ueberall irgendwo ein Hang in der Naehe ist.
    """
    a = f["flaeche"]
    if f["wasser"]  > 0.30:                       return "Ufer"
    if f["strasse"] > 0.25:                       return "Rastlichtung"
    if f["erhoben"] > 5.0:                        return "Steinkreis"
    if f["wand"]    > 0.55:                       return "Geroellfeld"
    if f["d_neth"] < 300 and a > 9000:            return "Obstgarten"
    wechsel = ["Hain", "Lichtung", "Geroellfeld", "Freiflaeche",
               "Hain", "Steinkreis", "Lichtung", "Hain"]
    return wechsel[idx % len(wechsel)]

# ------------------------------------------------------------------ Ausgabe
T = "Objects/trees/%s.tscn"
R = "Objects/rocks/%s.tscn"
LAUB  = [T%n for n in ["oak_25","maple_24","ash_tree_1","cherry_tree_2"]]
OBST  = [T%n for n in ["apple_tree_0","plum_tree_3","cherry_tree_2"]]
BIRKE = [T%n for n in ["birch_1_20","birch_2_21","birch_3_26","birch_orange_1_22","birch_orange_2_23"]]
NADEL = [T%n for n in ["fir_tree_17","noble_fir_tree_18","pine_tree_19"]]
TOT   = [T%n for n in ["deciduous_dead_1_29"]]
BUSCH = [T%n for n in ["deciduous_shrub_27","deciduous_shrub_2_31","holly_shrub_4","raspberry_shrub_5"]]
BROCKEN = [(R%"rock_01m_stone",1.0,5.0,13.0), (R%"rock_01m_stone_mossy",0.7,6.0,16.0),
           (R%"rock_01m_pebbles",0.6,7.0,19.0), (R%"rock_02m_shard",1.8,3.5,10.0),
           (R%"rock_04m_block",3.9,2.5,6.0)]
GROSS   = [(R%"rock_09m_outcrop",9.5,1.3,3.2), (R%"rock_17m_arch",16.8,0.9,2.2),
           (R%"rock_20m_arch",20.5,0.9,2.0)]

def pick(l): return l[rng.integers(len(l))]
def brocken(l):
    p, laenge, a, b = l[rng.integers(len(l))]
    sk = float(rng.uniform(a, b)); return p, sk, laenge*sk

res_ids = {}
body = []
def rid(p):
    if p not in res_ids: res_ids[p] = "%d_f" % (len(res_ids)+1)
    return res_ids[p]

def setze(pfad, name, x, y, z, sk=1.0, yaw=0.0, tx=0.0, tz=0.0, parent="."):
    cy, sy = math.cos(yaw), math.sin(yaw)
    Ry = np.array([[cy,0,sy],[0,1,0],[-sy,0,cy]])
    cxx, sxx = math.cos(tx), math.sin(tx); Rx = np.array([[1,0,0],[0,cxx,-sxx],[0,sxx,cxx]])
    czz, szz = math.cos(tz), math.sin(tz); Rz = np.array([[czz,-szz,0],[szz,czz,0],[0,0,1]])
    M = Rz @ Rx @ Ry * sk
    v = [M[0,0],M[0,1],M[0,2],M[1,0],M[1,1],M[1,2],M[2,0],M[2,1],M[2,2],x,y,z]
    nums = ", ".join(("%.3f"%q).rstrip('0').rstrip('.') or "0" for q in v)
    body.append('[node name="%s" parent="%s" instance=ExtResource("%s")]\ntransform = Transform3D(%s)\n'
                % (name, parent, rid(pfad), nums))

def box(parent, name, x, y, z, w, h, d, yaw=0.0):
    cy, sy = math.cos(yaw), math.sin(yaw)
    M = [cy,0,sy, 0,1,0, -sy,0,cy]
    nums = ", ".join(("%.2f"%q).rstrip('0').rstrip('.') or "0" for q in M+[x,y,z])
    body.append('[node name="%s" type="CSGBox3D" parent="%s"]\ntransform = Transform3D(%s)\n'
                'size = Vector3(%.1f, %.1f, %.1f)\n' % (name, parent, nums, w, h, d))

def gruppe(name, parent="."):
    body.append('[node name="%s" type="Node3D" parent="%s"]\n' % (name, parent))

def drin(f, x, z, rand=0.0):
    """Liegt der Punkt in der Flaeche (grob ueber das 4-m-Raster)?"""
    return any(abs(x-qx) <= S+rand and abs(z-qz) <= S+rand for qx, qz in f["zellen"])

def rand_punkte(f, n):
    """Punkte auf dem Rand der Flaeche - fuer Einrahmung."""
    pts = f["zellen"]
    aus = []
    for qx, qz in pts:
        nachbarn = sum(1 for ox, oz in pts if abs(ox-qx) <= S+0.1 and abs(oz-qz) <= S+0.1)
        if nachbarn < 8: aus.append((qx, qz))
    if not aus: aus = pts
    rng.shuffle(aus)
    return aus[:n]

# ------------------------------------------------------------------ Gestaltung
def baum_setzen(f, g, name, x, z, pool, smin=0.9, smax=1.45):
    setze(pick(pool), name, x, hq(x,z)-0.25, z, float(rng.uniform(smin,smax)),
          float(rng.uniform(0,6.283)), parent=g)

def fels_setzen(f, g, name, x, z, lst=None):
    p, sk, gr = brocken(lst or BROCKEN)
    setze(p, name, x, hq(x,z)-rng.uniform(0.25,0.45)*gr, z, sk,
          float(rng.uniform(0,6.283)), float(rng.normal(0,0.05)), float(rng.normal(0,0.05)),
          parent=g)
    return gr

def gestalte(f, typ, g, nr):
    zellen = f["zellen"]; c = 0
    def frei(n=1):
        nonlocal c
        out = []
        for _ in range(n):
            qx, qz = zellen[rng.integers(len(zellen))]
            out.append((qx + rng.uniform(-S,S), qz + rng.uniform(-S,S)))
        return out

    if typ == "Hain":
        # dichter Baumbestand, ein paar Findlinge dazwischen, Unterholz am Rand
        n = int(f["flaeche"] / 260)
        pool = NADEL if f["hoehe"] > 35 else (LAUB + BIRKE)
        for i in range(n):
            (x, z), = frei()
            baum_setzen(f, g, "Baum_%d_%d" % (nr,i), x, z, pool); c += 1
        for i in range(max(2, n//14)):
            (x, z), = frei()
            fels_setzen(f, g, "Fels_%d_%d" % (nr,i), x, z); c += 1
        for i, (x, z) in enumerate(rand_punkte(f, max(6, n//3))):
            setze(pick(BUSCH), "Busch_%d_%d" % (nr,i), x, hq(x,z)-0.15, z,
                  float(rng.uniform(0.9,1.6)), float(rng.uniform(0,6.283)), parent=g); c += 1

    elif typ in ("Lichtung", "Rastlichtung"):
        # Mitte bleibt frei, Rand wird gerahmt, eine Landmarke leicht aus der Mitte
        pool = NADEL if f["hoehe"] > 35 else (LAUB + BIRKE)
        rand = rand_punkte(f, max(10, int(f["flaeche"]/420)))
        for i, (x, z) in enumerate(rand):
            if rng.random() < 0.72:
                baum_setzen(f, g, "Rahmen_%d_%d" % (nr,i), x, z, pool, 1.0, 1.6)
            else:
                fels_setzen(f, g, "Rahmen_%d_%d" % (nr,i), x, z)
            c += 1
        lx = f["cx"] + rng.uniform(-1,1)*f["breite"]*0.18
        lz = f["cz"] + rng.uniform(-1,1)*f["tiefe"]*0.18
        setze(pick(TOT), "Landmarke_%d" % nr, lx, hq(lx,lz)-0.3, lz,
              float(rng.uniform(1.3,1.9)), float(rng.uniform(0,6.283)),
              float(rng.normal(0,0.05)), float(rng.normal(0,0.05)), parent=g); c += 1
        if typ == "Rastlichtung":
            box(g, "Feuerstelle_%d" % nr, f["cx"], hq(f["cx"],f["cz"])+0.2, f["cz"], 3.0, 0.5, 3.0)
            for k in range(3):
                a = k*2.1 + rng.uniform(0,1)
                bx, bz = f["cx"]+math.cos(a)*5.0, f["cz"]+math.sin(a)*5.0
                box(g, "Sitzstein_%d_%d" % (nr,k), bx, hq(bx,bz)+0.5, bz, 2.4, 1.0, 1.0, a)
            c += 4

    elif typ == "Steinkreis":
        rad = min(f["breite"], f["tiefe"]) * 0.34
        n = max(7, int(2*math.pi*rad/13))
        for i in range(n):
            a = i * 2*math.pi/n + rng.uniform(-0.05, 0.05)
            x, z = f["cx"]+math.cos(a)*rad, f["cz"]+math.sin(a)*rad
            p, sk, gr = brocken(BROCKEN)
            setze(p, "Stein_%d_%d" % (nr,i), x, hq(x,z)-rng.uniform(0.15,0.3)*gr, z,
                  sk*1.15, float(rng.uniform(0,6.283)),
                  float(rng.normal(0,0.04)), float(rng.normal(0,0.04)), parent=g); c += 1
        box(g, "Altar_%d" % nr, f["cx"], hq(f["cx"],f["cz"])+1.0, f["cz"], 5.0, 2.0, 3.0,
            float(rng.uniform(0,3)))
        for i, (x, z) in enumerate(rand_punkte(f, 8)):
            baum_setzen(f, g, "Saum_%d_%d" % (nr,i), x, z, NADEL, 1.1, 1.7); c += 1

    elif typ == "Geroellfeld":
        n = max(6, int(f["flaeche"]/1400))
        for i in range(n):
            (x, z), = frei()
            fels_setzen(f, g, "Block_%d_%d" % (nr,i), x, z,
                        GROSS if rng.random() < 0.18 else BROCKEN); c += 1
        for i, (x, z) in enumerate(rand_punkte(f, max(4, n//2))):
            setze(pick(BUSCH), "Busch_%d_%d" % (nr,i), x, hq(x,z)-0.15, z,
                  float(rng.uniform(0.8,1.4)), float(rng.uniform(0,6.283)), parent=g); c += 1

    elif typ == "Ufer":
        n = max(8, int(f["flaeche"]/900))
        for i in range(n):
            (x, z), = frei()
            if rng.random() < 0.55:
                setze(pick(BUSCH), "Schilf_%d_%d" % (nr,i), x, hq(x,z)-0.15, z,
                      float(rng.uniform(1.0,1.8)), float(rng.uniform(0,6.283)), parent=g)
            else:
                baum_setzen(f, g, "Ufer_%d_%d" % (nr,i), x, z, BIRKE, 0.9, 1.4)
            c += 1
        for i in range(max(2, n//6)):
            (x, z), = frei()
            fels_setzen(f, g, "Uferstein_%d_%d" % (nr,i), x, z); c += 1

    elif typ == "Obstgarten":
        # lockere Reihen - lesbar als bewirtschaftet, nicht als Wildwuchs
        a = rng.uniform(0, math.pi)
        ca, sa = math.cos(a), math.sin(a)
        schritt = 13.0
        i = 0
        for u in np.arange(-f["breite"]*0.5, f["breite"]*0.5, schritt):
            for w in np.arange(-f["tiefe"]*0.5, f["tiefe"]*0.5, schritt):
                x = f["cx"] + ca*u - sa*w + rng.uniform(-1.6, 1.6)
                z = f["cz"] + sa*u + ca*w + rng.uniform(-1.6, 1.6)
                if not drin(f, x, z): continue
                baum_setzen(f, g, "Obst_%d_%d" % (nr,i), x, z, OBST, 0.95, 1.25)
                i += 1; c += 1
        for k, (x, z) in enumerate(rand_punkte(f, 10)):
            box(g, "Zaun_%d_%d" % (nr,k), x, hq(x,z)+0.9, z, 4.0, 1.8, 0.4,
                float(rng.uniform(0,3))); c += 1

    elif typ == "Freiflaeche":
        # bewusst leer: nur ein klarer Rand, damit die Leere Absicht wirkt
        pool = NADEL if f["hoehe"] > 35 else (LAUB + BIRKE)
        for i, (x, z) in enumerate(rand_punkte(f, max(12, int(f["flaeche"]/500)))):
            if rng.random() < 0.55:
                baum_setzen(f, g, "Saum_%d_%d" % (nr,i), x, z, pool, 1.1, 1.7)
            else:
                fels_setzen(f, g, "Saum_%d_%d" % (nr,i), x, z)
            c += 1
    return c

# ------------------------------------------------------------------ Lauf
gruppe("Flaechen_Marker")
KAT = {}
zeilen = []
for i, zellen in enumerate(sorted(gebiete, key=len, reverse=True)):
    f = merkmale(zellen)
    typ = typ_waehlen(f, i)
    KAT[typ] = KAT.get(typ, 0) + 1
    name = "%s_%02d" % (typ, KAT[typ])
    gruppe(name)
    # Markierungsbox ueber die ganze Flaeche, flach und ohne Kollision
    mh = hq(f["cx"], f["cz"])
    box("Flaechen_Marker", "Marker_"+name, f["cx"], mh+0.4, f["cz"],
        f["breite"], 0.8, f["tiefe"])
    n = gestalte(f, typ, name, i)
    zeilen.append((name, f, n))

# ------------------------------------------------------------------ schreiben
kopf = ['[gd_scene load_steps=%d format=3]\n' % (len(res_ids)+1)]
def uid_of(p):
    with open(os.path.join(PROJ, p)) as fh:
        m = re.search(r'uid="([^"]+)"', fh.readline())
    return m.group(1) if m else None
for p, i in res_ids.items():
    u = uid_of(p)
    kopf.append('[ext_resource type="PackedScene"%s path="res://%s" id="%s"]\n'
                % ((' uid="%s"' % u) if u else "", p, i))
kopf.append('\n[node name="Flaechen" type="Node3D"]\n\n')
open(PROJ+"/World/flaechen.tscn", "w").write("".join(kopf) + "\n".join(body))

print()
print("%-22s %8s %7s %7s  %s" % ("Flaeche","Groesse","Hoehe","Objekte","Lage"))
print("-"*78)
for name, f, n in zeilen:
    lage = []
    if f["strasse"] > 0.004: lage.append("an der Strasse")
    if f["wasser"]  > 0.02:  lage.append("am Wasser")
    if f["wand"]    > 0.05:  lage.append("am Fels")
    if f["erhoben"] > 6:     lage.append("erhoeht")
    if f["d_neth"] < 320:    lage.append("nahe Nethora")
    print("%-22s %6.2f ha %6.1f m %6d   %s"
          % (name, f["flaeche"]/1e4, f["hoehe"], n, ", ".join(lage) or "-"))
print("-"*78)
print("%d Flaechen, %d Objekte -> World/flaechen.tscn" % (len(zeilen), sum(z[2] for z in zeilen)))
for k, v in sorted(KAT.items(), key=lambda kv: -kv[1]):
    print("   %-14s %d" % (k, v))
