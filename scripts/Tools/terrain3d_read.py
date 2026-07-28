import struct, zstandard

def decompress(path):
    d = open(path,'rb').read()
    assert d[:4] == b'RSCC', d[:4]
    cmode, block_size, read_total = struct.unpack_from('<III', d, 4)
    bc = read_total // block_size + 1
    off = 16
    sizes = list(struct.unpack_from('<%dI' % bc, d, off))
    off += bc*4
    dctx = zstandard.ZstdDecompressor()
    out = bytearray()
    for i, cs in enumerate(sizes):
        blk = d[off:off+cs]; off += cs
        want = block_size if i < bc-1 else (read_total - block_size*(bc-1))
        if want == 0: break
        out += dctx.decompress(blk, max_output_size=block_size)
    return bytes(out), cmode, block_size, read_total
