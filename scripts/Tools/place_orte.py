"""Erzeugt World/orte.tscn - Blocking-Boxen fuer Orte, nach Lore aus doc/konzept/."""
import numpy as np, os, math

SP = "/tmp/claude-1000/-home-cengiz-projects-RPG-rpg/5b875abc-7995-483a-8e40-b68729ffe26c/scratchpad"
PROJ = "/home/cengiz/projects/RPG/rpg"
H = np.load(SP+"/H.npy"); SLOPE = np.load(SP+"/slope.npy")
N = H.shape[0]
rng = np.random.default_rng(7)

def hq(x, z):
    fx, fz = x+1024.0, z+1024.0
    x0, z0 = int(np.floor(fx)), int(np.floor(fz))
    x0 = min(max(x0,0),N-2); z0 = min(max(z0,0),N-2)
    tx, tz = fx-x0, fz-z0
    a = H[z0,x0]*(1-tx)+H[z0,x0+1]*tx
    b = H[z0+1,x0]*(1-tx)+H[z0+1,x0+1]*tx
    return float(a*(1-tz)+b*tz)

def grad_dir(x, z, r=14):
    """Richtung des staerksten Anstiegs (zeigt zum Hang) als Winkel."""
    best, ba = -1e9, 0.0
    for i in range(16):
        a = i*math.pi/8
        d = hq(x+math.cos(a)*r, z+math.sin(a)*r) - hq(x, z)
        if d > best: best, ba = d, a
    return ba, best

out = []
def box(parent, name, x, y, z, w, h, d, yaw=0.0):
    cy, sy = math.cos(yaw), math.sin(yaw)
    M = [cy,0,sy, 0,1,0, -sy,0,cy]
    nums = ", ".join(("%.3f"%f).rstrip('0').rstrip('.') or "0" for f in M+[x,y,z])
    out.append('[node name="%s" type="CSGBox3D" parent="%s"]\ntransform = Transform3D(%s)\n'
               'size = Vector3(%.2f, %.2f, %.2f)\n' % (name, parent, nums, w, h, d))

def group(name, note):
    out.append('[node name="%s" type="Node3D" parent="Orte"]\n' % name)

SITES = []   # (name, x, z, beschreibung)

# ---------------------------------------------------------------------------
# 1) WEGSCHREIN kurz hinter dem Tor - erstes "hier war jemand" nach der Ankunft
def wegschrein(P, x, z, yaw):
    y = hq(x, z)
    box(P,"Sockel", x, y+0.4, z, 3.0, 0.8, 3.0, yaw)
    box(P,"Stufe",  x, y+0.9, z, 2.2, 0.4, 2.2, yaw)
    box(P,"Saeule", x, y+2.6, z, 0.7, 3.4, 0.7, yaw)
    box(P,"Querbalken", x, y+3.6, z, 2.2, 0.5, 0.5, yaw)
    box(P,"Kapitell", x, y+4.5, z, 1.1, 0.6, 1.1, yaw)
    for i,(dx,dz) in enumerate([(-2.6,-1.4),(2.4,1.8),(-1.8,2.6)]):
        px,pz = x+dx, z+dz
        box(P,"Stein%d"%i, px, hq(px,pz)+0.3, pz, 1.0, 0.7, 0.9, rng.uniform(0,3))

# 2) RASTPLATZ an der Strasse - Feuerstelle, Baenke, Unterstand
def rastplatz(P, x, z, yaw):
    y = hq(x, z)
    box(P,"Feuerstelle", x, y+0.15, z, 2.2, 0.3, 2.2, yaw)
    box(P,"Bank1", x-3.0, y+0.5, z, 0.7, 0.7, 4.0, yaw)
    box(P,"Bank2", x+3.0, y+0.5, z, 0.7, 0.7, 4.0, yaw)
    ux, uz = x+math.cos(yaw+1.2)*6.5, z+math.sin(yaw+1.2)*6.5
    uy = hq(ux,uz)
    box(P,"Unterstand_Dach", ux, uy+2.6, uz, 5.0, 0.4, 4.0, yaw)
    box(P,"Unterstand_Wand", ux, uy+1.3, uz+2.0, 5.0, 2.6, 0.4, yaw)
    box(P,"Pfosten1", ux-2.2, uy+1.3, uz-1.8, 0.3, 2.6, 0.3, yaw)
    box(P,"Pfosten2", ux+2.2, uy+1.3, uz-1.8, 0.3, 2.6, 0.3, yaw)

# 3) FAEHRSTELLE am Fluss - Huette und Steg (Faehrmann Aldous)
def faehrstelle(P, x, z, yaw):
    y = hq(x, z)
    box(P,"Huette", x, y+2.0, z, 7.0, 4.0, 5.5, yaw)
    box(P,"Dach",   x, y+4.4, z, 8.0, 0.8, 6.5, yaw)
    box(P,"Anbau",  x+math.cos(yaw)*4.8, y+1.2, z+math.sin(yaw)*4.8, 3.0, 2.4, 3.0, yaw)
    # Steg Richtung tiefstes Wasser
    a = yaw + math.pi/2
    for i in range(7):
        px, pz = x+math.cos(a)*(5+i*2.6), z+math.sin(a)*(5+i*2.6)
        box(P,"Steg%d"%i, px, max(hq(px,pz), -0.6)+0.5, pz, 2.6, 0.35, 2.8, a)
    box(P,"Poller", x+math.cos(a)*23, 0.6, z+math.sin(a)*23, 0.5, 2.2, 0.5)

# 4) FREIES LAGER - Berghoehlengewoelbe + Palisade + Huetten
def freies_lager(P, x, z):
    a, _ = grad_dir(x, z, 22)          # a zeigt bergauf
    y = hq(x, z)
    hx, hz = x+math.cos(a)*16, z+math.sin(a)*16
    hy = hq(hx, hz)
    box(P,"Hoehlenmaul", hx, hy+4.0, hz, 16.0, 9.0, 12.0, a)
    box(P,"Torsturz",    hx-math.cos(a)*7, hy+8.5, hz-math.sin(a)*7, 18.0, 2.5, 3.0, a)
    box(P,"Feuer", x, y+0.2, z, 3.0, 0.4, 3.0)
    # Huetten im Halbkreis vor der Hoehle
    for i in range(6):
        w = a + math.pi + (i-2.5)*0.42
        px, pz = x+math.cos(w)*rng.uniform(16,26), z+math.sin(w)*rng.uniform(16,26)
        py = hq(px,pz)
        s = rng.uniform(0.85,1.25)
        box(P,"Huette%d"%i, px, py+1.5*s, pz, 5.5*s, 3.0*s, 4.5*s, rng.uniform(0,6.28))
        box(P,"Dach%d"%i,   px, py+3.3*s, pz, 6.5*s, 0.6*s, 5.5*s, rng.uniform(0,6.28))
    # Palisade als Bogen zur offenen Seite
    for i in range(14):
        w = a + math.pi + (i-6.5)*0.135
        px, pz = x+math.cos(w)*36, z+math.sin(w)*36
        box(P,"Palisade%d"%i, px, hq(px,pz)+2.2, pz, 3.2, 4.4, 0.7, w+math.pi/2)
    box(P,"Wachturm", x+math.cos(a+math.pi+0.95)*36, hq(x+math.cos(a+math.pi+0.95)*36,
        z+math.sin(a+math.pi+0.95)*36)+4.5, z+math.sin(a+math.pi+0.95)*36, 4.5, 9.0, 4.5)

# 5) ALTE KATHEDRALE - Ruine, Langhaus + Turmstumpf
def kathedrale(P, x, z, yaw):
    y = hq(x, z)
    L, B = 34.0, 14.0
    box(P,"Wand_West", x-math.cos(yaw+math.pi/2)*B/2, y+5.0, z-math.sin(yaw+math.pi/2)*B/2,
        L, 10.0, 1.4, yaw)
    box(P,"Wand_Ost",  x+math.cos(yaw+math.pi/2)*B/2, y+3.5, z+math.sin(yaw+math.pi/2)*B/2,
        L*0.7, 7.0, 1.4, yaw)
    box(P,"Chor", x+math.cos(yaw)*L/2, y+4.0, z+math.sin(yaw)*L/2, 1.4, 8.0, B, yaw)
    tx, tz = x-math.cos(yaw)*(L/2+4), z-math.sin(yaw)*(L/2+4)
    box(P,"Turmstumpf", tx, hq(tx,tz)+9.0, tz, 10.0, 18.0, 10.0, yaw)
    box(P,"Turm_Bruch", tx, hq(tx,tz)+18.6, tz, 10.5, 1.4, 7.0, yaw+0.15)
    for i in range(5):
        px = x + math.cos(yaw)*rng.uniform(-L/2,L/2) + math.cos(yaw+math.pi/2)*rng.uniform(-9,9)
        pz = z + math.sin(yaw)*rng.uniform(-L/2,L/2) + math.sin(yaw+math.pi/2)*rng.uniform(-9,9)
        box(P,"Saeulenrest%d"%i, px, hq(px,pz)+rng.uniform(1.0,2.6), pz,
            1.6, rng.uniform(2.0,5.2), 1.6, rng.uniform(0,3))
    box(P,"Truemmer", x, y+0.5, z, L*0.8, 1.0, B*0.7, yaw+0.1)

# 6) ERBAUER-TEMPEL - streng geometrisch, fremdartig, gestufte Plattform
def erbauer_tempel(P, x, z, yaw):
    y = hq(x, z)
    box(P,"Plattform1", x, y+1.2, z, 40.0, 2.4, 40.0, yaw)
    box(P,"Plattform2", x, y+3.2, z, 30.0, 2.0, 30.0, yaw)
    box(P,"Plattform3", x, y+4.8, z, 21.0, 1.6, 21.0, yaw)
    box(P,"Cella",      x, y+10.0, z, 13.0, 11.0, 13.0, yaw)
    box(P,"Kragen",     x, y+15.8, z, 15.5, 1.2, 15.5, yaw)
    box(P,"Spitze",     x, y+18.5, z, 5.0, 5.0, 5.0, yaw+0.785)
    for i in range(4):
        w = yaw + math.pi/4 + i*math.pi/2
        px, pz = x+math.cos(w)*17, z+math.sin(w)*17
        box(P,"Monolith%d"%i, px, y+9.0, pz, 2.4, 14.0, 2.4, w)
    a = yaw + math.pi
    for i in range(6):
        px, pz = x+math.cos(a)*(20+i*3.0), z+math.sin(a)*(20+i*3.0)
        box(P,"Treppe%d"%i, px, y+1.2-i*0.42, pz, 11.0, 0.6, 3.0, yaw)

# 7) MINE - Stollenmund im Hang, Foerdergeruest, Halden
def mine(P, x, z):
    a, _ = grad_dir(x, z, 20)
    y = hq(x, z)
    mx, mz = x+math.cos(a)*12, z+math.sin(a)*12
    my = hq(mx, mz)
    box(P,"Stollenmund", mx, my+2.6, mz, 7.0, 5.5, 8.0, a)
    box(P,"Sturzbalken", mx-math.cos(a)*3.6, my+5.6, mz-math.sin(a)*3.6, 8.5, 1.2, 1.2, a)
    box(P,"Geruest_Fuss", x, y+3.0, z, 6.0, 6.0, 6.0)
    box(P,"Geruest_Turm", x, y+9.5, z, 3.4, 7.0, 3.4)
    box(P,"Geruest_Rad",  x, y+13.4, z, 4.6, 0.8, 1.2)
    for i in range(3):
        w = a + math.pi + (i-1)*0.7
        px, pz = x+math.cos(w)*rng.uniform(18,30), z+math.sin(w)*rng.uniform(18,30)
        box(P,"Halde%d"%i, px, hq(px,pz)+1.4, pz, rng.uniform(8,14), 2.8, rng.uniform(8,13),
            rng.uniform(0,3))
    for i in range(2):
        w = a + math.pi + (i-0.5)*1.5
        px, pz = x+math.cos(w)*15, z+math.sin(w)*15
        box(P,"Schuppen%d"%i, px, hq(px,pz)+1.6, pz, 6.0, 3.2, 4.5, rng.uniform(0,3))

# 8) GEHOEFT / verlassene Siedlung an der Strasse
def gehoeft(P, x, z, yaw):
    y = hq(x, z)
    box(P,"Haupthaus", x, y+2.2, z, 9.0, 4.4, 7.0, yaw)
    box(P,"Haupthaus_Dach", x, y+5.0, z, 10.0, 1.2, 8.0, yaw)
    for i,(dx,dz,w,d) in enumerate([(-11,5,6,5),(9,-7,5,4),(11,7,7,5)]):
        px = x+math.cos(yaw)*dx-math.sin(yaw)*dz
        pz = z+math.sin(yaw)*dx+math.cos(yaw)*dz
        py = hq(px,pz)
        box(P,"Nebengebaeude%d"%i, px, py+1.5, pz, w, 3.0, d, yaw+rng.uniform(-0.3,0.3))
    for i in range(10):
        w = i*0.628
        px, pz = x+math.cos(w)*19, z+math.sin(w)*19
        box(P,"Zaun%d"%i, px, hq(px,pz)+0.8, pz, 3.6, 1.6, 0.3, w+math.pi/2)

# ---------------------------------------------------------------------------
PLAN = [
    ("Wegschrein_Ankunft", -676, -917, lambda P: wegschrein(P, -676, -917, 0.4),
     "Erster Schrein hinter dem Tor - markiert die Ankunft"),
    ("Rastplatz_Strasse",  -364, -807, lambda P: rastplatz(P, -364, -807, 1.1),
     "Lagerfeuer an der Ostroute"),
    ("Faehrstelle",        -431, -787, lambda P: faehrstelle(P, -431, -787, 0.6),
     "Faehrmann Aldous - Flussquerung"),
    ("Freies_Lager",       -605, -165, lambda P: freies_lager(P, -605, -165),
     "Berghoehlengewoelbe der Freien, Suedwesten"),
    ("Alte_Kathedrale",    -592, -269, lambda P: kathedrale(P, -592, -269, 0.9),
     "Ruine aus der Zeit vor der Barriere"),
    ("Erbauer_Tempel_Ost", -106, -399, lambda P: erbauer_tempel(P, -106, -399, 0.3),
     "Aeusserer Tempel des Pentagramms, erhoeht"),
    ("Mine_Nethora",       -862, -306, lambda P: mine(P, -862, -306),
     "Aufgegebene Mine im Westgebirge"),
    ("Gehoeft_Verlassen",  -312, -626, lambda P: gehoeft(P, -312, -626, 2.2),
     "Verlassenes Gehoeft an der Suedroute"),
]

out.append('[node name="Orte" type="Node3D"]\n')
for name, x, z, fn, note in PLAN:
    out.append('[node name="%s" type="Node3D" parent="."]\n' % name)
    n0 = len(out)
    fn(name)
    print("%-22s x=%6d z=%6d  h=%5.1f  %2d Boxen   %s"
          % (name, x, z, hq(x,z), len(out)-n0, note))

with open(os.path.join(PROJ, "World/orte.tscn"), "w") as f:
    f.write("[gd_scene format=3]\n\n" + "\n".join(out))
print("\ngeschrieben: World/orte.tscn  (%d Knoten)" % (len(out)-1))
