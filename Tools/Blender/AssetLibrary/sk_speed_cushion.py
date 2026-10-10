"""Asphalt speed cushion. A low hump, not the concrete wheel stop."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register


def _hump(g):
    rings = []
    for x, scale in ((-1.05, 0.25), (-0.88, 1.0), (0.88, 1.0), (1.05, 0.25)):
        rings.append([
            (x, 0.0, -0.42 * scale),
            (x, 0.012 * scale, -0.28 * scale),
            (x, 0.070 * scale, -0.12 * scale),
            (x, 0.078 * scale, 0.0),
            (x, 0.070 * scale, 0.12 * scale),
            (x, 0.012 * scale, 0.28 * scale),
            (x, 0.0, 0.42 * scale),
        ])
    count = len(rings[0])
    verts = [p for ring in rings for p in ring]
    faces = []
    for i in range(len(rings) - 1):
        for k in range(count - 1):
            a = i * count + k
            b = i * count + k + 1
            c = (i + 1) * count + k + 1
            d = (i + 1) * count + k
            faces.append((a, b, c, d))
    faces.append(tuple(range(count - 1, -1, -1)))
    base = (len(rings) - 1) * count
    faces.append(tuple(range(base, base + count)))
    g.mesh(verts, faces, "Lib_Asphalt")


@register
def create():
    a = Asset(
        "SpeedCushion_Asphalt",
        "StreetFurniture",
        "Speed cushion, 2.10 m wide and 0.08 m tall. Two yellow stripes on the crown.",
    )
    a.climb_note = "8 cm. Not a wall."
    a.vault_note = "Too low to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        _hump(g)
        for z in (-0.06, 0.06):
            g.box((0, 0.080, z), (1.50, 0.010, 0.045), "Lib_PaintYellow")
        a.end()
    a.box("Col_Hump", (0, 0.028, 0), (1.50, 0.04, 0.16))
    return a
