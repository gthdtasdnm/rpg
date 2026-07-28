import sys, struct, re, numpy as np
sys.path.insert(0,'/tmp/claude-1000/-home-cengiz-projects-RPG-rpg/5b875abc-7995-483a-8e40-b68729ffe26c/scratchpad')
from godot_res import decompress

def maps(path):
    """height (float32 1024x1024), control (uint32), color (rgba8) einer Region."""
    raw,_,_,_ = decompress(path)
    blocks = []
    for m in re.finditer(rb'data\x00', raw):
        for d in range(0, 10):
            p = m.end() + d
            if p+8 > len(raw): break
            vt, n = struct.unpack_from('<II', raw, p)
            if vt == 31 and n >= 1024*1024:
                blocks.append((p+8, n)); break
    res = {}
    for i,(off,n) in enumerate(blocks):
        buf = raw[off:off+n]
        if i == 0: res['height']  = np.frombuffer(buf, '<f4').reshape(1024,1024)
        elif i == 1: res['control'] = np.frombuffer(buf, '<u4').reshape(1024,1024)
        elif i == 2: res['color']   = np.frombuffer(buf[:1024*1024*4], 'u1').reshape(1024,1024,4)
    return res

if __name__ == '__main__':
    r = maps(sys.argv[1])
    for k,v in r.items():
        print(k, v.shape, v.dtype)
    h = r['height']; print("Hoehe min=%.2f max=%.2f" % (h.min(), h.max()))
