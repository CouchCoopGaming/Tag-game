"""Closed boat hulls. Bow is -Z. Not an asset module."""

import math


def section(half, keel, sheer):
    """Starboard outer skin, keel to gunwale. x >= 0."""
    rise = max(0.05, sheer - keel)
    return [
        (0.0, keel),
        (half * 0.20, keel + rise * 0.12),
        (half * 0.58, keel + rise * 0.34),
        (half * 1.02, keel + rise * 0.60),
        (half * 0.96, sheer),
    ]


def solid_hull(g, z0, z1, count, profile, top_at, mat, bow_extra=0.14):
    """Loft a closed solid. top_at(t, half, sheer) returns 7 (x, y) samples, gunwale to gunwale."""
    verts = []
    rings = []
    for i in range(count):
        t = i / (count - 1)
        z = z0 + (z1 - z0) * t
        half, keel, sheer = profile(t)
        sec = section(half, keel, sheer)
        top = list(top_at(t, half, sheer))
        top[0] = sec[-1]
        top[-1] = (-sec[-1][0], sec[-1][1])
        idxs = []
        for x, y in sec:
            idxs.append(len(verts))
            verts.append((x, y, z))
        for x, y in top[1:]:
            idxs.append(len(verts))
            verts.append((x, y, z))
        for x, y in reversed(sec[1:-1]):
            idxs.append(len(verts))
            verts.append((-x, y, z))
        rings.append(idxs)
    n = len(rings[0])
    faces = []
    for s in range(count - 1):
        a = rings[s]
        b = rings[s + 1]
        for i in range(n):
            j = (i + 1) % n
            faces.append((a[i], a[j], b[j], b[i]))
    half, keel, sheer = profile(0.0)
    bow = len(verts)
    verts.append((0.0, keel + (sheer - keel) * 0.62, z0 - bow_extra))
    a = rings[0]
    for i in range(n):
        j = (i + 1) % n
        faces.append((bow, a[i], a[j]))
    sx = sy = 0.0
    for idx in rings[-1]:
        sx += verts[idx][0]
        sy += verts[idx][1]
    stern = len(verts)
    verts.append((sx / n, sy / n, z1))
    a = rings[-1]
    for i in range(n):
        j = (i + 1) % n
        faces.append((stern, a[j], a[i]))
    g.mesh(verts, faces, mat, uv_scale=1.05)


def beam_at(profile, t, y):
    half, keel, sheer = profile(t)
    sec = section(half, keel, sheer)
    for (x0, y0), (x1, y1) in zip(sec, sec[1:]):
        lo, hi = (y0, y1) if y0 <= y1 else (y1, y0)
        if lo - 1e-6 <= y <= hi + 1e-6 and abs(y1 - y0) > 1e-6:
            u = (y - y0) / (y1 - y0)
            return abs(x0 + (x1 - x0) * u)
    return half * 0.96
