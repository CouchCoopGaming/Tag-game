"""Plastic water-filled barrier.

Real size: 6 ft long, about 24 in wide at the foot, 32 in tall
(1.83 x 0.64 x 0.82 m). Tapered hull, fill cap, end knuckles, fork pockets.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _hull(g, bevel, segs):
    # (height, half-width). Foot flares, then the wall pinches toward the top.
    profile = (
        (0.000, 0.300),
        (0.045, 0.320),
        (0.100, 0.275),
        (0.220, 0.215),
        (0.420, 0.155),
        (0.620, 0.118),
        (0.760, 0.096),
        (0.820, 0.082),
    )
    n = len(profile)
    xs = (-0.860, 0.860)
    verts = []
    for x in xs:
        for y, hz in profile:
            verts.append((x, y, hz))
            verts.append((x, y, -hz))

    def loop(xi):
        base = xi * n * 2
        order = [base + i * 2 for i in range(n)]
        order += [base + i * 2 + 1 for i in range(n - 1, -1, -1)]
        return order

    ring0 = loop(0)
    ring1 = loop(1)
    count = len(ring0)
    faces = []
    for i in range(count):
        j = (i + 1) % count
        faces.append((ring0[i], ring0[j], ring1[j], ring1[i]))
    faces.append(tuple(reversed(ring0)))
    faces.append(tuple(ring1))
    g.mesh(verts, faces, "Lib_Orange", bevel=bevel, segs=segs)


@register
def create():
    a = Asset(
        "Barrier_Water",
        "StreetFurniture",
        "Water-filled barrier, 1.83 m long, 0.64 m at the foot, 0.82 m tall. Fill cap and end knuckles.",
    )
    a.climb_note = "Sloped plastic. Not a cling wall."
    a.vault_note = "Top is 0.82 m, under the vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.006, 0.0)
        bs = 1 if lod == 0 else 0
        _hull(g, bev, bs)
        # Bands overlap the hull so they are part of the shell, not a decal floating off it.
        g.box((0, 0.68, 0.104), (1.58, 0.055, 0.014), "Lib_PaintYellow", bevel=bev, segs=bs)
        g.box((0, 0.68, -0.104), (1.58, 0.055, 0.014), "Lib_PaintYellow", bevel=bev, segs=bs)
        if lod == 0:
            for x in (-0.46, 0.0, 0.46):
                g.box((x, 0.68, 0.112), (0.22, 0.032, 0.006), "Lib_PaintWhite")
                g.box((x, 0.68, -0.112), (0.22, 0.032, 0.006), "Lib_PaintWhite")
            # Fork pockets. Dark liners set into the foot, not a through-hole.
            for z in (0.27, -0.27):
                g.box((0, 0.14, z), (0.46, 0.10, 0.05), "Lib_Black")
            g.box((0.55, 0.16, 0.255), (0.08, 0.05, 0.016), "Lib_SteelDark")
        g.cylinder((-0.93, 0.40, 0), 0.055, 0.14, "Lib_Orange", seg, axis="X")
        g.cylinder((0.95, 0.40, 0), 0.038, 0.18, "Lib_Orange", seg, axis="X")
        g.cylinder((0.28, 0.835, 0.02), 0.050, 0.045, "Lib_Black", seg)
        g.cylinder((0.28, 0.858, 0.02), 0.032, 0.016, "Lib_SteelDark", 8)
        if lod == 0:
            g.box((-0.70, 0.11, 0.268), (0.16, 0.04, 0.014), "Lib_Rust")
        a.end()
    # Top slab is the stand surface. Its top is 0.8 cm under the hull crown.
    a.box("Col_Top", (0, 0.772, 0), (1.56, 0.08, 0.11))
    a.box("Col_Mid", (0, 0.46, 0), (1.56, 0.28, 0.20))
    a.box("Col_Foot", (0, 0.10, 0), (1.56, 0.14, 0.40))
    return a
