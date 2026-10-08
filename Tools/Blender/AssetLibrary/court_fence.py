"""Court perimeter kit. Same pivot as Court: 12 m on X, 22 m on Z.

Baselines are 3.05 m (behind the hoops). Sidelines are 1.80 m. A closed gate
sits on the +X sideline. Fabric is a diamond of flat wires, not a passage.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

# Slab is x ±6, z ±11. Posts sit 0.45 m outside that edge.
X = 6.45
Z = 11.45


def _wire(g, a, b, normal):
    dx, dy, dz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    nx, ny, nz = normal
    px = dy * nz - dz * ny
    py = dz * nx - dx * nz
    pz = dx * ny - dy * nx
    plen = math.sqrt(px * px + py * py + pz * pz) or 1.0
    s = 0.007 / plen
    px, py, pz = px * s, py * s, pz * s
    verts = [
        (a[0] + px, a[1] + py, a[2] + pz),
        (a[0] - px, a[1] - py, a[2] - pz),
        (b[0] - px, b[1] - py, b[2] - pz),
        (b[0] + px, b[1] + py, b[2] + pz),
    ]
    g.mesh(verts, [(0, 1, 2, 3), (3, 2, 1, 0)], "Lib_Chain")


def _point(axis, origin, along, up):
    if axis == "X":
        return (origin[0] + along, up, origin[1])
    return (origin[0], up, origin[1] + along)


def _fabric(g, axis, origin, length, height, step, normal, gap=None):
    n = max(1, int(length / step))
    m = max(1, int((height - 0.3) / step))
    for i in range(n):
        for j in range(m):
            a = _point(axis, origin, i * step, 0.18 + j * step)
            b = _point(axis, origin, min(length, (i + 1) * step), 0.18 + min(height - 0.2, (j + 1) * step))
            c = _point(axis, origin, min(length, (i + 1) * step), 0.18 + j * step)
            d = _point(axis, origin, i * step, 0.18 + min(height - 0.2, (j + 1) * step))
            mid = (a[2] + b[2]) * 0.5 if axis == "Z" else (a[0] + b[0]) * 0.5
            if gap and gap[0] < mid < gap[1]:
                continue
            _wire(g, a, b, normal)
            _wire(g, c, d, normal)


def _posts(g, axis, origin, length, height, seg):
    # Corners are placed once, at the taller baseline height, so two runs do not occupy the same post.
    count = max(3, int(round(length / 2.0)) + 1)
    for i in range(1, count - 1):
        t = length * i / (count - 1)
        p = _point(axis, origin, t, height * 0.5)
        g.cylinder(p, 0.035, height, "Lib_SteelDark", seg)
        g.sphere(_point(axis, origin, t, height), 0.04, "Lib_Steel", 6)


@register
def create():
    a = Asset(
        "CourtFence",
        "Park",
        "Perimeter for the 22 x 12 m court. Same pivot as Court. Baselines 3.05 m, sidelines 1.80 m, closed gate on +X.",
    )
    a.climbable = True
    a.climb_note = "Posts, rails, and a wire-thick fabric slab. The diamonds are not a passage. The gate is closed."
    a.vault_note = "Sideline top is 1.80 m and the baselines are 3.05 m. Too high to vault from the court."
    runs = [
        ("X", (-X, -Z), X * 2, 3.05, (0, 0, -1), None),
        ("X", (-X, Z), X * 2, 3.05, (0, 0, 1), None),
        ("Z", (-X, -Z), Z * 2, 1.80, (-1, 0, 0), None),
        ("Z", (X, -Z), Z * 2, 1.80, (1, 0, 0), (-0.8, 0.8)),
    ]
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6, 5)
        step = lod_pick(lod, 0.72, 1.15, None)
        for x in (-X, X):
            for z in (-Z, Z):
                g.cylinder((x, 1.525, z), 0.04, 3.05, "Lib_SteelDark", seg)
                g.sphere((x, 3.05, z), 0.045, "Lib_Steel", 6)
        for axis, origin, length, height, normal, gap in runs:
            _posts(g, axis, origin, length, height, seg)
            y = height - 0.06
            g.pipe(_point(axis, origin, 0.08, y), _point(axis, origin, length - 0.08, y), 0.018, "Lib_Steel", seg)
            g.pipe(_point(axis, origin, 0.08, 0.08), _point(axis, origin, length - 0.08, 0.08), 0.014, "Lib_SteelDark", seg)
            if step and not (gap and lod == 2):
                _fabric(g, axis, origin, length, height, step, normal, gap if lod < 2 else None)
        if lod < 2:
            g.box((X, 0.90, 0), (0.04, 1.70, 1.45), "Lib_SteelDark")
            if step:
                _fabric(g, "Z", (X, -0.7), 1.4, 1.70, step, (1, 0, 0), None)
        a.end()
    a.capsule("Col_Post_SW", (-X, 1.52, -Z), 0.035, 3.05, 1)
    a.capsule("Col_Post_SE", (X, 1.52, -Z), 0.035, 3.05, 1)
    a.capsule("Col_Post_NW", (-X, 1.52, Z), 0.035, 3.05, 1)
    a.capsule("Col_Post_NE", (X, 1.52, Z), 0.035, 3.05, 1)
    a.box("Col_Fabric_S", (0, 1.50, -Z), (X * 2, 2.90, 0.02), approx=True)
    a.box("Col_Fabric_N", (0, 1.50, Z), (X * 2, 2.90, 0.02), approx=True)
    a.box("Col_Fabric_W", (-X, 0.90, 0), (0.02, 1.60, Z * 2), approx=True)
    a.box("Col_Fabric_E1", (X, 0.90, -4.1), (0.02, 1.60, 6.4), approx=True)
    a.box("Col_Fabric_E2", (X, 0.90, 4.1), (0.02, 1.60, 6.4), approx=True)
    a.box("Col_Gate", (X, 0.90, 0), (0.04, 1.60, 1.40), approx=True)
    return a
