"""Detectable curb ramp. A wedge with dome bumps, not the storm inlet."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _wedge(g):
    rings = []
    for z, y in ((-0.50, 0.012), (-0.34, 0.078), (0.22, 0.018), (0.50, 0.008)):
        rings.append([
            (-0.60, 0.0, z),
            (-0.60, y, z),
            (0.60, y, z),
            (0.60, 0.0, z),
        ])
    count = 4
    verts = [p for ring in rings for p in ring]
    faces = []
    for i in range(len(rings) - 1):
        for k in range(count):
            a = i * count + k
            b = i * count + (k + 1) % count
            c = (i + 1) * count + (k + 1) % count
            d = (i + 1) * count + k
            faces.append((a, b, c, d))
    faces.append((0, 3, 2, 1))
    base = (len(rings) - 1) * count
    faces.append((base, base + 1, base + 2, base + 3))
    g.mesh(verts, faces, "Lib_Concrete")


@register
def create():
    a = Asset(
        "CurbRamp_Detectable",
        "StreetFurniture",
        "Curb ramp, 1.20 m wide, rise 0.08 m. Truncated domes on the slope.",
    )
    a.climb_note = "8 cm rise. Not a wall."
    a.vault_note = "Too low to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        _wedge(g)
        if lod == 0:
            for ix, x in enumerate((-0.36, -0.12, 0.12, 0.36)):
                for iz, z in enumerate((-0.22, -0.08, 0.06)):
                    y = 0.082 - (z + 0.34) * 0.107
                    g.cylinder((x, y, z), 0.018, 0.014, "Lib_Concrete", 8)
        a.end()
    a.box("Col_Ramp", (0, 0.024, -0.22), (0.90, 0.032, 0.22))
    return a
