"""Street basketball hoop. Rim at 3.05 m, framed backboard, net, padded pole.

The pole is the origin. On the 22 m court the slab ends at |z| = 11 and the
painted basket is at |z| = 9.7125. A pole 1.2 m behind that end line (world
|z| = 12.2) needs the rim 2.4875 m in front of the pole.

FIBA puts the backboard face 1.20 m inside the end line and the rim 1.575 m
inside it. This court's paint is scaled by 22/28, so that face-to-rim gap is
0.295 m. A literal 1.20 m face on the unscaled court would pass through the rim.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

# World |z| of the pole minus world |z| of the painted basket.
RIM_Z = 12.2 - 9.7125
FACE_GAP = (1.575 - 1.20) * (22.0 / 28.0)
BOARD_Z = (RIM_Z - FACE_GAP) - 0.0175
# Arm stops short of the pole (r = 0.055) and short of the board's back face.
ARM_Z0 = 0.064
ARM_Z1 = (BOARD_Z - 0.0175) - 0.008
ARM_ZC = (ARM_Z0 + ARM_Z1) * 0.5
ARM_ZS = ARM_Z1 - ARM_Z0


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
            z = RIM_Z + math.cos(ang) * 0.225 * scale
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
        "Regulation rim at 3.05 m, 2.49 m in front of the pole. Backboard face is 0.29 m behind the rim. "
        "Place the pole 1.2 m behind the baseline: south z=-12.2 yaw 0, north z=12.2 yaw 180, so the rim "
        "sits on the restricted-area center (z=±9.71). 1.80 x 1.05 m backboard, net, pole pad, and base plate. Rim faces +Z.",
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
        g.box((0, 3.48, BOARD_Z), (1.80, 1.05, 0.035), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((0, 3.48, BOARD_Z + 0.022), (1.68, 0.92, 0.012), "Lib_PaintWhite")
        g.box((0, 3.48, BOARD_Z - 0.022), (1.70, 0.98, 0.008), "Lib_SteelDark")
        g.box((0, 3.00, BOARD_Z + 0.030), (1.84, 0.06, 0.05), "Lib_Black")
        g.box((0, 3.22, BOARD_Z + 0.028), (0.46, 0.35, 0.01), "Lib_PaintRed")
        g.box((0, 3.22, BOARD_Z + 0.035), (0.30, 0.02, 0.008), "Lib_PaintWhite")
        g.box((0, 3.22, BOARD_Z + 0.035), (0.02, 0.22, 0.008), "Lib_PaintWhite")
        g.box((0, 3.35, ARM_ZC), (0.08, 0.08, ARM_ZS), "Lib_SteelDark")
        # Diagonal stays clear of the pole and stops under the arm.
        if lod == 0:
            g.pipe((0, 2.55, 0.10), (0, 3.24, ARM_ZC + 0.35), 0.028, "Lib_Steel", 6)
        g.torus((0, 3.05, RIM_Z), 0.225, 0.012, "Lib_Orange", 20 if lod == 0 else 12, 6)
        if lod == 0:
            for i in range(4):
                ang = math.radians(i * 90 + 45)
                g.cylinder(
                    (math.sin(ang) * 0.20, 3.05, RIM_Z + math.cos(ang) * 0.20),
                    0.008, 0.01, "Lib_Steel", 5,
                )
        _net(g, lod)
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.50, 0.035, 0.40))
    a.capsule("Col_Pole", (0, 1.55, 0), 0.05, 2.95, 1)
    a.box("Col_Board", (0, 3.48, BOARD_Z), (1.72, 0.98, 0.03))
    a.box("Col_Arm", (0, 3.35, ARM_ZC), (0.06, 0.06, ARM_ZS - 0.06))
    return a
