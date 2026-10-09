"""Street basketball hoop. Rim at 3.05 m, framed backboard, net, padded pole.

The pole is the origin and the rim faces +Z. The backboard face is 0.375 m
behind the rim, the real FIBA gap (1.575 m minus 1.20 m). On the 22 m court
the baseline is |z| = 11. Place the south pole at z = -10.235 (yaw 0) and the
north pole at z = 10.235 (yaw 180): the face lands 1.20 m inside the baseline
and the rim lands on the painted basket at |z| = 9.425.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

# Rim is 0.810 m in front of the pole. Board face is 0.375 m behind that rim.
RIM_Z = 0.810
BOARD_Z = 0.4175
# Arm stops short of the pole (r = 0.055) and short of the board's back face.
ARM_Z0 = 0.064
ARM_Z1 = (BOARD_Z - 0.0175) - 0.008
ARM_ZC = (ARM_Z0 + ARM_Z1) * 0.5
ARM_ZS = ARM_Z1 - ARM_Z0


def _net(g, lod):
    spokes = lod_pick(lod, 10, 6)
    drops = lod_pick(lod, 4, 2)
    # First ring sits under the tube (minor radius 0.028) with air around the shell.
    y0 = 3.05 - 0.040
    r0 = 0.195
    rings = []
    for i in range(spokes):
        ang = 2.0 * math.pi * i / spokes
        col = []
        for k in range(drops + 1):
            t = k / float(drops)
            scale = 1.0 - t * 0.42
            x = math.sin(ang) * r0 * scale
            z = RIM_Z + math.cos(ang) * r0 * scale
            y = y0 - t * 0.42
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
        "Regulation rim at 3.05 m, 0.81 m in front of the pole. Backboard face is 0.375 m behind the rim. "
        "Place the pole so that face is 1.2 m inside the baseline: south z=-10.235 yaw 0, north z=10.235 "
        "yaw 180. The rim then sits on the painted basket (z=±9.425). 1.80 x 1.05 m backboard, net, pole pad, "
        "and base plate. Rim faces +Z.",
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
        g.box((0, 2.94, BOARD_Z + 0.018), (1.84, 0.05, 0.036), "Lib_Black")
        g.box((0, 3.22, BOARD_Z + 0.028), (0.46, 0.35, 0.01), "Lib_PaintRed")
        g.box((0, 3.22, BOARD_Z + 0.035), (0.30, 0.02, 0.008), "Lib_PaintWhite")
        g.box((0, 3.22, BOARD_Z + 0.035), (0.02, 0.22, 0.008), "Lib_PaintWhite")
        g.box((0, 3.35, ARM_ZC), (0.08, 0.08, ARM_ZS), "Lib_SteelDark")
        # Diagonal stays clear of the pole and stops under the arm.
        if lod == 0:
            g.pipe((0, 2.55, 0.08), (0, 3.22, min(ARM_ZC + 0.10, BOARD_Z - 0.09)), 0.028, "Lib_Steel", 6)
        g.torus((0, 3.05, RIM_Z), 0.225, 0.028, "Lib_Orange", 20 if lod == 0 else 12, 6)
        if lod == 0:
            for i in range(4):
                ang = math.radians(i * 90 + 45)
                g.cylinder(
                    (math.sin(ang) * 0.225, 3.05 + 0.040, RIM_Z + math.cos(ang) * 0.225),
                    0.008, 0.012, "Lib_Steel", 5,
                )
        _net(g, lod)
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.50, 0.035, 0.40))
    a.capsule("Col_Pole", (0, 1.55, 0), 0.05, 2.95, 1)
    a.box("Col_Board", (0, 3.48, BOARD_Z), (1.72, 0.98, 0.03))
    a.box("Col_Arm", (0, 3.35, ARM_ZC), (0.06, 0.06, ARM_ZS - 0.06))
    return a
