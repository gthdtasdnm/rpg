"""Erzeugt World/bewuchs.tscn und World/orte.tscn aus den Terrain3D-Hoehendaten.

Liest die entpackten Karten (H, slope, eff) und setzt Felsen, Baeume, Straeucher
sowie Blocking-Boxen nach Regeln, die sich am Gelaende orientieren.
"""
import numpy as np, os, re, math

SP = "/tmp/claude-1000/-home-cengiz-projects-RPG-rpg/5b875abc-7995-483a-8e40-b68729ffe26c/scratchpad"
PROJ = "/home/cengiz/projects/RPG/rpg"

H     = np.load(SP+"/H.npy")
SLOPE = np.load(SP+"/slope.npy")
EFF   = np.load(SP+"/eff.npy")
N = H.shape[0]
rng = np.random.default_rng(20260728)

# ---------------------------------------------------------------- Gelaende-Zugriff
def hq(x, z):
    """Hoehe an Weltkoordinate, bilinear."""
    fx, fz = x + 1024.0, z + 1024.0
    x0, z0 = int(np.floor(fx)), int(np.floor(fz))
    x0 = min(max(x0, 0), N-2); z0 = min(max(z0, 0), N-2)
    tx, tz = fx-x0, fz-z0
    a = H[z0,x0]*(1-tx)+H[z0,x0+1]*tx
    b = H[z0+1,x0]*(1-tx)+H[z0+1,x0+1]*tx
    return float(a*(1-tz)+b*tz)

def at(arr, x, z):
    return arr[int(round(z))+1024, int(round(x))+1024]

def boxmean(a, k):
    a = np.nan_to_num(a).astype('f8')
    c = np.cumsum(np.cumsum(a,0),1); c = np.pad(c,((1,0),(1,0)))
    h,w = a.shape; out = np.zeros(a.shape); r = k//2
    I = np.arange(r,h-r)[:,None]; J = np.arange(r,w-r)[None,:]
    out[r:h-r,r:w-r] = (c[I+r+1,J+r+1]-c[I-r,J+r+1]-c[I+r+1,J-r]+c[I-r,J-r])/((2*r+1)**2)
    return out

ROAD   = (EFF == 2)
RIVERB = (EFF == 3)
TOUCH  = (np.abs(H) > 0.05) | (EFF > 0)
road_near   = boxmean(ROAD.astype('f4'), 15)     # Strasse im ~7 m Umkreis
road_wide   = boxmean(ROAD.astype('f4'), 61)     # Strasse im ~30 m Umkreis
steep_near  = boxmean((SLOPE > 48).astype('f4'), 31)
touch_near  = boxmean(TOUCH.astype('f4'), 61)

# ---------------------------------------------------------------- .tscn-Bausteine
def uid_of(path):
    with open(os.path.join(PROJ, path)) as f:
        m = re.search(r'uid="([^"]+)"', f.readline())
    return m.group(1) if m else None

def basis_rows(scale, yaw, tilt_x=0.0, tilt_z=0.0):
    cy, sy = math.cos(yaw), math.sin(yaw)
    Ry = np.array([[cy,0,sy],[0,1,0],[-sy,0,cy]])
    cx, sx = math.cos(tilt_x), math.sin(tilt_x)
    Rx = np.array([[1,0,0],[0,cx,-sx],[0,sx,cx]])
    cz, sz = math.cos(tilt_z), math.sin(tilt_z)
    Rz = np.array([[cz,-sz,0],[sz,cz,0],[0,0,1]])
    M = Rz @ Rx @ Ry * scale
    return M

def node(name, res_id, x, y, z, scale=1.0, yaw=0.0, tx=0.0, tz=0.0, parent="."):
    M = basis_rows(scale, yaw, tx, tz)
    v = [M[0,0],M[0,1],M[0,2], M[1,0],M[1,1],M[1,2], M[2,0],M[2,1],M[2,2], x,y,z]
    nums = ", ".join(("%.4f" % f).rstrip('0').rstrip('.') or "0" for f in v)
    return ('[node name="%s" parent="%s" instance=ExtResource("%s")]\ntransform = Transform3D(%s)\n'
            % (name, parent, res_id, nums))

class Scene:
    def __init__(self):
        self.res = {}     # pfad -> id
        self.body = []
        self._n = 0
    def rid(self, path):
        if path not in self.res:
            self._n += 1
            self.res[path] = "%d_r" % self._n
        return self.res[path]
    def add(self, path, name, x, y, z, scale=1.0, yaw=0.0, tx=0.0, tz=0.0, parent="."):
        self.body.append(node(name, self.rid(path), x, y, z, scale, yaw, tx, tz, parent))
    def raw(self, s):
        self.body.append(s)
    def write(self, out_path, root_name):
        head = ["[gd_scene load_steps=%d format=3]\n" % (len(self.res)+1)]
        for p, i in self.res.items():
            u = uid_of(p)
            head.append('[ext_resource type="PackedScene"%s path="res://%s" id="%s"]\n'
                        % ((' uid="%s"' % u) if u else "", p, i))
        head.append('\n[node name="%s" type="Node3D"]\n\n' % root_name)
        with open(os.path.join(PROJ, out_path), "w") as f:
            f.write("".join(head) + "\n".join(self.body))
        print("geschrieben: %s  (%d Knoten)" % (out_path, len(self.body)))

# ---------------------------------------------------------------- Kataloge
T = "Objects/trees/%s.tscn"
R = "Objects/rocks/%s.tscn"

LAUB   = [T%n for n in ["oak_25","maple_24","ash_tree_1","cherry_tree_2","apple_tree_0","plum_tree_3"]]
BIRKE  = [T%n for n in ["birch_1_20","birch_2_21","birch_3_26","birch_orange_1_22","birch_orange_2_23"]]
NADEL  = [T%n for n in ["fir_tree_17","noble_fir_tree_18","pine_tree_19"]]
JUNG   = [T%n for n in ["deciduous_sapling_32","deciduous_sapling_2_33","coniferous_sapling_15"]]
TOT    = [T%n for n in ["deciduous_dead_1_29"]]
BUSCH  = [T%n for n in ["deciduous_shrub_27","deciduous_shrub_2_31","holly_shrub_4",
                        "raspberry_shrub_5","coniferous_shrub_16"]]
FELS_L = [R%n for n in ["rock_16m_field","rock_09m_outcrop","rock_09m_scree","rock_09m_ridge","rock_17m_arch"]]
FELS_M = [R%n for n in ["rock_08m_slab_flat","rock_06m_slabs_a","rock_06m_slabs_b",
                        "rock_05m_wall","rock_05m_slab_mossy","rock_04m_block"]]
FELS_S = [R%n for n in ["rock_02m_shard","rock_02m_slab_mossy","rock_01m_stone",
                        "rock_01m_stone_mossy","rock_01m_pebbles"]]

def pick(lst): return lst[rng.integers(len(lst))]

# ---------------------------------------------------------------- Poisson-Ausduennung
class Thinner:
    """Haelt Mindestabstaende ein, ueber ein Raster."""
    def __init__(self, cell):
        self.cell = cell; self.g = {}
    def ok(self, x, z, r):
        c = int(r/self.cell)+1
        gx, gz = int(x//self.cell), int(z//self.cell)
        for i in range(gx-c, gx+c+1):
            for j in range(gz-c, gz+c+1):
                for (px,pz,pr) in self.g.get((i,j), ()):
                    if (px-x)**2 + (pz-z)**2 < max(r,pr)**2: return False
        return True
    def put(self, x, z, r):
        gx, gz = int(x//self.cell), int(z//self.cell)
        self.g.setdefault((gx,gz), []).append((x,z,r))

# ================================================================ BEWUCHS
sc = Scene()
occ  = Thinner(6.0)   # Einzelobjekte
occ_c = Thinner(40.0)  # Cluster-Mittelpunkte
occ_l = Thinner(70.0)  # Landmarken
counts = {"fels":0, "baum":0, "busch":0}

# --- Arbeitsgebiet: der bearbeitete Kessel
AREA = (touch_near > 0.35)

# --- Ausschlusszonen: Stadt, bestehende Handarbeit, meine eigenen Orte ---
import json
yy, xx = np.mgrid[0:N, 0:N]
WX = (xx - 1024).astype('f4'); WZ = (yy - 1024).astype('f4')

# Nethora samt Vorfeld freihalten
BLOCK = (WX > -710) & (WX < -520) & (WZ > -690) & (WZ < -450)

# Um jedes bereits von Hand gesetzte Objekt ein Loch (Tor, Kisten, Zaeune, CSG-Welt)
EXIST = json.load(open(SP+"/existing.json"))
for ex, ez in EXIST:
    BLOCK |= ((WX-ex)**2 + (WZ-ez)**2) < 7.5**2

# Meine Orte aus gen_orte.py - Radius nach Groesse des Ortes
ORTE = [(-676,-917,22),(-364,-807,26),(-431,-787,34),(-605,-165,58),
        (-592,-269,40),(-106,-399,48),(-862,-306,42),(-312,-626,32)]
for ox, oz, orad in ORTE:
    BLOCK |= ((WX-ox)**2 + (WZ-oz)**2) < orad**2

# Der Strassenkorridor gehoert immer dazu - auch wo das Gelaende noch flach
# und unbemalt ist, sonst faellt die halbe Route durchs Raster.
AREA = (AREA | (road_wide > 0.0015)) & ~BLOCK
print("Ausschlusszonen: %.2f %% der Karte gesperrt" % (100*BLOCK.mean()))

# ---- 2b) Strassenrahmung: Gruppen im WECHSEL links/rechts ---------------
# Wichtigster Block, laeuft zuerst. Bricht die Symmetrie des Tals und fuellt den
# Mittelgrund. Pro Stuetzstelle mehrere Versuche mit anderem Abstand, damit ein
# einzelner ungueltiger Punkt nicht die ganze Stelle verwirft.
chain = np.load(SP+"/road_chain.npy")
rahmen = 0
stellen = 0
side = 1
d_acc = 0.0
for i in range(1, len(chain)):
    d_acc += float(np.hypot(*(chain[i]-chain[i-1])))
    if d_acc < rng.uniform(16, 28): continue
    d_acc = 0.0
    side = -side
    cx, cz = float(chain[i][0]), float(chain[i][1])
    t = chain[min(i+1,len(chain)-1)] - chain[i-1]
    L = float(np.hypot(*t)) or 1.0
    nx, nz = -t[1]/L*side, t[0]/L*side

    px = pz = None
    for off in (14.0, 19.0, 24.0, 30.0, 36.0):
        qx, qz = cx+nx*off, cz+nz*off
        if not (-1015 < qx < 1015 and -1015 < qz < 1015): continue
        if not at(AREA, qx, qz): continue
        if at(SLOPE, qx, qz) > 40: continue
        if at(road_near, qx, qz) > 0.02: continue
        if not occ.ok(qx, qz, 7.0): continue
        px, pz = qx, qz; break
    if px is None: continue
    occ.put(px, pz, 7.0)
    stellen += 1
    hgt = hq(px, pz)

    if rng.random() < 0.45:
        # Felsgruppe: 1 grosser + Begleiter
        s0 = rng.uniform(1.0, 1.9)
        sc.add(pick(FELS_L), "Rahmen_%d" % rahmen, px, hgt-rng.uniform(0.2,0.4)*6*s0, pz,
               s0, rng.uniform(0,6.283), rng.normal(0,0.06), rng.normal(0,0.06), parent="Felsen")
        rahmen += 1
        for _ in range(int(rng.integers(5,11))):
            a2 = rng.uniform(0,6.283); d2 = rng.uniform(4,13)
            qx, qz = px+math.cos(a2)*d2, pz+math.sin(a2)*d2
            if at(road_near, qx, qz) > 0.05: continue
            sc.add(pick(FELS_M+FELS_S), "Rahmen_%d" % rahmen, qx,
                   hq(qx,qz)-rng.uniform(0.15,0.35)*3, qz, rng.uniform(0.7,1.4),
                   rng.uniform(0,6.283), rng.normal(0,0.12), rng.normal(0,0.12), parent="Felsen")
            rahmen += 1
    else:
        pool = NADEL if hgt > 30 else (LAUB + BIRKE)
        gesetzt = 0
        for _ in range(44):
            if gesetzt >= 16: break
            a2 = rng.uniform(0,6.283); d2 = abs(rng.normal(0,8))
            qx, qz = px+math.cos(a2)*d2, pz+math.sin(a2)*d2
            if at(road_near, qx, qz) > 0.002: continue
            if at(SLOPE, qx, qz) > 34: continue
            if at(RIVERB, qx, qz): continue
            if not occ.ok(qx, qz, 3.1): continue
            occ.put(qx, qz, 3.1)
            sc.add(pick(pool), "Rahmen_%d" % rahmen, qx, hq(qx,qz)-0.25, qz,
                   rng.uniform(0.85,1.4), rng.uniform(0,6.283), parent="Baeume")
            rahmen += 1; gesetzt += 1
        # ein paar Straeucher an den Fuss der Gruppe
        for _ in range(int(rng.integers(4,10))):
            a2 = rng.uniform(0,6.283); d2 = rng.uniform(2,9)
            qx, qz = px+math.cos(a2)*d2, pz+math.sin(a2)*d2
            if at(road_near, qx, qz) > 0.02: continue
            sc.add(pick(BUSCH), "Rahmen_%d" % rahmen, qx, hq(qx,qz)-0.15, qz,
                   rng.uniform(0.7,1.3), rng.uniform(0,6.283), parent="Straeucher")
            rahmen += 1
print("   Strassenrahmung: %d Objekte an %d Stellen" % (rahmen, stellen))

# ---- 1) Geroell am Fuss der Felswaende ---------------------------------
cand = AREA & (SLOPE > 12) & (SLOPE < 42) & (steep_near > 0.05) & (H > -0.2) & ~RIVERB
rr, cc = np.where(cand)
order = rng.permutation(len(rr))
for k in order:
    if counts["fels"] >= 2100: break
    x, z = float(cc[k]-1024), float(rr[k]-1024)
    if at(road_near, x, z) > 0.001: continue
    if not occ.ok(x, z, 5.5): continue
    occ.put(x, z, 5.5)
    # Gruppe: 1 grosser + 2-4 mittlere + 3-6 kleine
    yaw = rng.uniform(0, 6.283)
    s = rng.uniform(0.8, 1.5)
    y = hq(x, z) - rng.uniform(0.20, 0.42) * 6.0 * s
    sc.add(pick(FELS_L), "Fels_%d" % counts["fels"], x, y, z, s, yaw,
           rng.normal(0, 0.07), rng.normal(0, 0.07), parent="Felsen")
    counts["fels"] += 1
    for _ in range(rng.integers(3, 8)):
        a = rng.uniform(0, 6.283); d = rng.uniform(3, 15)
        px, pz = x+math.cos(a)*d, z+math.sin(a)*d
        if at(SLOPE, px, pz) > 55: continue
        s2 = rng.uniform(0.6, 1.2)
        sc.add(pick(FELS_M), "Fels_%d" % counts["fels"], px,
               hq(px,pz) - rng.uniform(0.18,0.40)*3.0*s2, pz, s2,
               rng.uniform(0,6.283), rng.normal(0,0.10), rng.normal(0,0.10), parent="Felsen")
        counts["fels"] += 1
    for _ in range(rng.integers(5, 13)):
        a = rng.uniform(0, 6.283); d = rng.uniform(2, 19)
        px, pz = x+math.cos(a)*d, z+math.sin(a)*d
        s2 = rng.uniform(0.7, 1.6)
        sc.add(pick(FELS_S), "Fels_%d" % counts["fels"], px,
               hq(px,pz) - rng.uniform(0.10,0.30)*1.0*s2, pz, s2,
               rng.uniform(0,6.283), rng.normal(0,0.16), rng.normal(0,0.16), parent="Felsen")
        counts["fels"] += 1

# ---- 2) Baumgruppen ----------------------------------------------------
tree_ok = AREA & (SLOPE < 30) & (H > 0.15) & ~RIVERB & (road_near < 0.0005)
rr, cc = np.where(tree_ok)
order = rng.permutation(len(rr))
clusters = 0
for k in order:
    if counts["baum"] >= 3000 or clusters >= 260: break
    cx, cz = float(cc[k]-1024), float(rr[k]-1024)
    if not occ_c.ok(cx, cz, 27.0): continue
    occ_c.put(cx, cz, 27.0)
    clusters += 1
    hgt = hq(cx, cz)
    # Artenwahl nach Hoehenlage
    if hgt > 38:   pool, sap = NADEL, [JUNG[2]]
    elif hgt > 18: pool, sap = NADEL + BIRKE, JUNG
    else:          pool, sap = LAUB + BIRKE, JUNG
    rad = rng.uniform(12, 30)
    n = int(rng.integers(14, 34))
    for _ in range(n):
        a = rng.uniform(0, 6.283); d = abs(rng.normal(0, rad*0.55))
        if d > rad: continue
        px, pz = cx+math.cos(a)*d, cz+math.sin(a)*d
        if at(SLOPE, px, pz) > 34: continue
        if at(road_near, px, pz) > 0.0005: continue
        if at(RIVERB, px, pz): continue
        if not occ.ok(px, pz, 2.9): continue
        occ.put(px, pz, 2.9)
        lst = sap if rng.random() < 0.18 else pool
        sc.add(pick(lst), "Baum_%d" % counts["baum"], px, hq(px,pz)-0.25, pz,
               rng.uniform(0.78, 1.32), rng.uniform(0,6.283),
               rng.normal(0,0.02), rng.normal(0,0.02), parent="Baeume")
        counts["baum"] += 1
    # Auslaeufer: einzelne Baeume, die sich vom Cluster wegstaffeln
    for _ in range(int(rng.integers(3,8))):
        a = rng.uniform(0,6.283); d = rng.uniform(rad, rad*2.1)
        px, pz = cx+math.cos(a)*d, cz+math.sin(a)*d
        if not (0 < int(pz)+1024 < N-1 and 0 < int(px)+1024 < N-1): continue
        if at(SLOPE, px, pz) > 32 or at(H, px, pz) < 0.15: continue
        if at(road_near, px, pz) > 0.0005 or at(RIVERB, px, pz): continue
        if not occ.ok(px, pz, 3.4): continue
        occ.put(px, pz, 3.4)
        sc.add(pick(pool), "Baum_%d" % counts["baum"], px, hq(px,pz)-0.25, pz,
               rng.uniform(0.8,1.25), rng.uniform(0,6.283), parent="Baeume")
        counts["baum"] += 1

# ---- 3) Totholz als Landmarken an der Strasse ---------------------------
cand = AREA & (SLOPE < 22) & (H > 0.2) & (road_wide > 0.004) & (road_near < 0.0005)
rr, cc = np.where(cand)
order = rng.permutation(len(rr))
tot = 0
for k in order:
    if tot >= 45: break
    x, z = float(cc[k]-1024), float(rr[k]-1024)
    if not occ_l.ok(x, z, 45.0): continue
    occ_l.put(x, z, 45.0)
    sc.add(pick(TOT), "Totholz_%d" % tot, x, hq(x,z)-0.3, z,
           rng.uniform(1.0,1.5), rng.uniform(0,6.283),
           rng.normal(0,0.05), rng.normal(0,0.05), parent="Baeume")
    tot += 1

# ---- 4) Straeucher an Kontaktstellen -----------------------------------
busch_ok = AREA & (SLOPE < 36) & (H > 0.1) & ~RIVERB
rr, cc = np.where(busch_ok)
order = rng.permutation(len(rr))
for k in order:
    if counts["busch"] >= 2600: break
    x, z = float(cc[k]-1024), float(rr[k]-1024)
    if at(road_near, x, z) > 0.02: continue
    if not occ.ok(x, z, 1.9): continue
    occ.put(x, z, 1.9)
    sc.add(pick(BUSCH), "Busch_%d" % counts["busch"], x, hq(x,z)-0.15, z,
           rng.uniform(0.7,1.4), rng.uniform(0,6.283), parent="Straeucher")
    counts["busch"] += 1

# Gruppen-Knoten voranstellen
groups = ('[node name="Felsen" type="Node3D" parent="."]\n\n'
          '[node name="Baeume" type="Node3D" parent="."]\n\n'
          '[node name="Straeucher" type="Node3D" parent="."]\n')
sc.body.insert(0, groups)
sc.write("World/bewuchs.tscn", "Bewuchs")
print("   Felsen %d, Baeume %d (%d Gruppen), Totholz %d, Straeucher %d"
      % (counts["fels"], counts["baum"], clusters, tot, counts["busch"]))
