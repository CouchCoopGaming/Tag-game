"""STOP sign. Octagon at 1.55–2.30 m. Pole is not a cling."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "LiberationSans-Regular.ttf",
))


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
    a = Asset("Sign_Stop", "StreetFurniture", "STOP sign. Octagon 0.75 m across, bottom at 1.52 m. STOP in Liberation Sans.")
    a.climb_note = "Sign pole is a 4.5 cm tube. Not a cling wall."
    a.vault_note = "No rail at vault height. The sign face is overhead."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        g.box((0, 0.015, 0), (0.22, 0.03, 0.22), "Lib_SteelDark", bevel=0.003 if lod == 0 else 0, segs=1 if lod == 0 else 0)
        if lod == 0:
            for x in (-0.07, 0.07):
                for z in (-0.07, 0.07):
                    g.cylinder((x, 0.034, z), 0.008, 0.012, "Lib_Steel", 6)
        g.cylinder((0, 1.16, 0), 0.030, 2.26, "Lib_Steel", seg)
        g.box((0, 1.90, 0.03), (0.08, 0.12, 0.05), "Lib_Steel")
        _octagon(g, (0, 1.90, 0.055), 0.42, 0.012, "Lib_PaintWhite", 0.0, 0)
        _octagon(g, (0, 1.90, 0.066), 0.36, 0.012, "Lib_PaintRed", 0.003 if lod == 0 else 0, 1 if lod == 0 else 0)
        if lod == 0:
            g.text("STOP", (0, 1.90, 0.078), 0.14, "Lib_PaintWhite", extrude=0.003, font=_FONT)
        else:
            g.box((0, 1.90, 0.038), (0.42, 0.16, 0.008), "Lib_PaintWhite")
        a.end()
    a.box("Col_Base", (0, 0.015, 0), (0.16, 0.02, 0.16))
    a.capsule("Col_Pole", (0, 1.10, 0), 0.022, 2.00, 1)
    # Inside the red octagon. Vertex radius is 0.36 m.
    a.box("Col_Sign", (0, 1.90, 0.066), (0.40, 0.40, 0.008))
    return a
