"""Finds HPL world-space vertices in a .map_cache by byte scan: float triples whose z is argv[2] (docs/Reference/GeometryAudit.md)."""
import sys, numpy as np
data = open(sys.argv[1], 'rb').read()
zt = float(sys.argv[2]); tol = 2e-4
res = {}
for off in range(4):
    n = (len(data) - off) // 4
    f = np.frombuffer(data, dtype='<f4', count=n, offset=off)
    idx = np.nonzero(np.abs(f - zt) < tol)[0]
    for i in idx:
        if i < 2: continue
        x, y = f[i-2], f[i-1]
        if np.isfinite(x) and np.isfinite(y) and abs(x) < 100 and -5 < y < 20:
            res.setdefault(off, []).append((round(float(x),3), round(float(y),3), off + 4*(i-2)))
for off, v in res.items():
    xs = sorted(set((a, b) for a, b, _ in v))
    print('offset', off, 'count', len(v))
    print(' x range', min(a for a, b in xs), max(a for a, b in xs))
    print(' sample', xs[:60])
