"""Street basketball hoop. Rim at 3.05 m, framed backboard, net, padded pole."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _net(g, lod):
    spokes = lod_pick(lod, 10, 6)
    drops = lod_pick(lod, 4, 2)
    rings = []
    for i in range(spokes):
        ang = 2.0 * math.pi * i / spokes
        col = []
        for k in range(drops + 1):
            t = k / float(drops)
            scale = 1.0 - t * 0.42
            x = math.sin(ang) * 0.225 * scale
            z = 0.86 + math.cos(ang) * 0.225 * scale
            y = 3.04 - t * 0.42
            col.append((x, y, z))
        rings.append(col)
        for k in range(drops):
            g.pipe(col[k], col[k + 1], 0.004, "Lib_PaintWhite", 4)
    for k in range(1, drops + 1):
        for i in range(spokes):
            g.pipe(rings[i][k], rings[(i + 1) % spokes][k], 0.0035, "Lib_PaintWhite", 4)


@register
def create():
    a = Asset(
        "Hoop",
        "Park",
        "Regulation rim at 3.05 m. 1.80 x 1.05 m backboard, net, pole pad, and base plate. Rim faces +Z.",
    )
    a.climb_note = "Pole is 12 cm under the pad. Not a cling wall."
    a.vault_note = "No rail. The rim is 3.05 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.02, 0), (0.56, 0.04, 0.46), "Lib_SteelDark", bevel=bev, segs=1)
        if lod == 0:
            for x in (-0.22, 0.22):
                for z in (-0.16, 0.16):
                    g.cylinder((x, 0.045, z), 0.012, 0.012, "Lib_Steel", 6)
        g.cylinder((0, 1.55, 0), 0.055, 3.02, "Lib_Steel", seg)
        g.cylinder((0, 1.05, 0), 0.085, 1.70, "Lib_PaintBlue", max(8, seg - 2))
        # Board center sits so the rim (3.05) is just above the lower padding.
        g.box((0, 3.48, 0.52), (1.80, 1.05, 0.035), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((0, 3.48, 0.542), (1.68, 0.92, 0.012), "Lib_PaintWhite")
        # Dark back so the board's face (toward +Z, the rim) is obvious.
        g.box((0, 3.48, 0.498), (1.70, 0.98, 0.008), "Lib_SteelDark")
        g.box((0, 3.00, 0.55), (1.84, 0.06, 0.05), "Lib_Black")
        g.box((0, 3.22, 0.548), (0.46, 0.35, 0.01), "Lib_PaintRed")
        g.box((0, 3.22, 0.555), (0.30, 0.02, 0.008), "Lib_PaintWhite")
        g.box((0, 3.22, 0.555), (0.02, 0.22, 0.008), "Lib_PaintWhite")
        g.box((0, 3.35, 0.32), (0.08, 0.08, 0.36), "Lib_SteelDark")
        g.torus((0, 3.05, 0.86), 0.225, 0.012, "Lib_Orange", 20 if lod == 0 else 12, 6)
        if lod == 0:
            for i in range(4):
                ang = math.radians(i * 90 + 45)
                g.cylinder((math.sin(ang) * 0.20, 3.05, 0.86 + math.cos(ang) * 0.20), 0.008, 0.01, "Lib_Steel", 5)
        _net(g, lod)
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.50, 0.035, 0.40))
    a.capsule("Col_Pole", (0, 1.55, 0), 0.05, 2.95, 1)
    a.box("Col_Board", (0, 3.48, 0.52), (1.72, 0.98, 0.03))
    a.box("Col_Arm", (0, 3.35, 0.32), (0.07, 0.07, 0.32))
    return a
