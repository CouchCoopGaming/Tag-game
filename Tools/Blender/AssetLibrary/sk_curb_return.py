"""Quarter curb return. Same gutter and batter as StreetCurb_Straight.

Face radius is 1.50 m. The two ends match the straight curb's cross section
so a straight piece can butt either end.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register

R = 1.50
OUTER = 0.24
# The unshifted arc sits in +X/+Z with a corner at the origin. Shift it
# so the ground footprint is centered on the pivot.
SHIFT = (R + OUTER) * 0.5
PROFILE = (
    (-0.28, 0.0),
    (-0.28, 0.12),
    (0.04, 0.12),
    (0.06, 0.27),
    (0.24, 0.27),
    (0.24, 0.0),
)


def _point(angle, s, y):
    rad = R + s
    return (rad * math.cos(angle) - SHIFT, y, rad * math.sin(angle) - SHIFT)


def _sweep(g, steps):
    rings = []
    for i in range(steps + 1):
        angle = math.radians(90.0 * i / steps)
        rings.append([_point(angle, s, y) for s, y in PROFILE])
    n = len(PROFILE)
    verts = [p for ring in rings for p in ring]
    faces = []
    for i in range(steps):
        for k in range(n):
            j = (k + 1) % n
            a = i * n + k
            b = i * n + j
            c = (i + 1) * n + j
            d = (i + 1) * n + k
            faces.append((a, b, c, d))
    faces.append(tuple(range(n - 1, -1, -1)))
    base = steps * n
    faces.append(tuple(range(base, base + n)))
    g.mesh(verts, faces, "Lib_Concrete", uv_scale=0.6, bevel=0.003, segs=1)


@register
def create():
    a = Asset(
        "StreetCurb_Return",
        "StreetFurniture",
        "Quarter curb return. Face radius 1.50 m. Gutter 0.30 m at 0.12 m. Curb top at 0.27 m. Ends match StreetCurb_Straight.",
    )
    a.climb_note = "Curb face is 0.15 m. Not a cling wall."
    a.vault_note = "Curb is 0.15 m above the gutter. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        _sweep(g, 6 if lod == 0 else 3)
        if lod == 0:
            for angle in (math.radians(8), math.radians(82)):
                x, y, z = _point(angle, 0.14, 0.271)
                g.box((x, y, z), (0.08, 0.006, 0.010), "Lib_Mortar")
        a.end()
    # Short boxes follow the arc. Each one stays inside the prism.
    for name, angle, s, y, size in (
        ("Col_GutterA", 18, -0.12, 0.058, (0.16, 0.108, 0.16)),
        ("Col_GutterB", 45, -0.12, 0.058, (0.16, 0.108, 0.16)),
        ("Col_GutterC", 72, -0.12, 0.058, (0.16, 0.108, 0.16)),
        ("Col_HeadA", 18, 0.15, 0.206, (0.12, 0.112, 0.12)),
        ("Col_HeadB", 45, 0.15, 0.206, (0.12, 0.112, 0.12)),
        ("Col_HeadC", 72, 0.15, 0.206, (0.12, 0.112, 0.12)),
    ):
        x, _, z = _point(math.radians(angle), s, y)
        a.box(name, (x, y, z), size)
    return a
