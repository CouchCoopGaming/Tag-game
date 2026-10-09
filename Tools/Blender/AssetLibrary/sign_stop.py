"""STOP sign. Octagon at 1.55–2.30 m. Pole is not a cling."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _octagon(g, center, radius, thick, mat, bevel, segs):
    verts = []
    for side in (-1, 1):
        for i in range(8):
            ang = math.radians(22.5 + i * 45.0)
            verts.append((
                center[0] + math.cos(ang) * radius,
                center[1] + math.sin(ang) * radius,
                center[2] + side * thick * 0.5,
            ))
    faces = [tuple(range(8, 16)), tuple(reversed(range(0, 8)))]
    for i in range(8):
        j = (i + 1) % 8
        faces.append((i, j, 8 + j, 8 + i))
    g.mesh(verts, faces, mat, bevel=bevel, segs=segs)


@register
def create():
    a = Asset("Sign_Stop", "StreetFurniture", "STOP sign. Octagon 0.75 m across, bottom of the sign at 1.52 m.")
    a.climb_note = "Sign pole is a 4.5 cm tube. Not a cling wall."
    a.vault_note = "No rail at vault height. The sign face is overhead."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        g.cylinder((0, 1.15, 0), 0.022, 2.30, "Lib_Steel", seg)
        _octagon(g, (0, 1.90, 0.02), 0.40, 0.025, "Lib_PaintRed", 0.004 if lod == 0 else 0, 1 if lod == 0 else 0)
        if lod == 0:
            g.text("STOP", (0, 1.90, 0.038), 0.16, "Lib_PaintWhite", extrude=0.004)
        else:
            g.box((0, 1.90, 0.038), (0.42, 0.16, 0.008), "Lib_PaintWhite")
        a.end()
    a.capsule("Col_Pole", (0, 1.15, 0), 0.022, 2.30, 1)
    # Inscribed in the octagon so the corners of the box stay inside the sign.
    a.box("Col_Sign", (0, 1.90, 0.02), (0.56, 0.56, 0.03))
    return a
