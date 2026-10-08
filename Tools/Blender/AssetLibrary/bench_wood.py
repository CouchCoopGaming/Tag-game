"""Park bench. Board seat on a connected steel frame. Seat is 0.45 m, not a vault rail."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Bench_Wood",
        "StreetFurniture",
        "1.80 m park bench. Five board seat slats and three back slats on a connected steel frame. Seat height 0.45 m.",
    )
    a.climb_note = "Back slats are too broken up to cling. Not a wall-run panel."
    a.vault_note = "Seat is 0.45 m. Below the 0.90–1.05 m vault band."
    length = 1.80
    for lod in (0, 1):
        g = a.begin(lod)
        slats = lod_pick(lod, 5, 3)
        backs = lod_pick(lod, 3, 2)
        for x in (-0.72, 0.72):
            for z in (-0.22, 0.22):
                g.box((x, 0.21, z), (0.055, 0.42, 0.055), "Lib_SteelDark")
                g.box((x, 0.02, z), (0.12, 0.04, 0.14), "Lib_SteelDark")
            # Stretcher buried in both legs, so the end frame is one piece.
            g.box((x, 0.10, 0.0), (0.040, 0.036, 0.50), "Lib_SteelDark")
            # Back post continues the rear leg.
            g.box((x, 0.64, -0.22), (0.050, 0.58, 0.050), "Lib_SteelDark")
            g.box((x, 0.58, -0.02), (0.040, 0.036, 0.44), "Lib_SteelDark")
            arm = x - 0.06 if x < 0 else x + 0.06
            g.box((arm, 0.62, -0.02), (0.046, 0.034, 0.42), "Lib_SteelDark")
            g.box((arm, 0.648, -0.02), (0.070, 0.022, 0.46), "Lib_Board")
        g.box((0, 0.43, -0.22), (1.58, 0.046, 0.046), "Lib_SteelDark")
        g.box((0, 0.43, 0.22), (1.58, 0.046, 0.046), "Lib_SteelDark")
        g.box((0, 0.72, -0.22), (1.58, 0.040, 0.040), "Lib_SteelDark")
        for i in range(slats):
            z = -0.18 + (i + 0.5) * (0.36 / slats)
            width = (0.36 / slats) * 0.92
            g.box((0, 0.455, z), (length, 0.032, width), "Lib_Board")
        for i in range(backs):
            y = 0.56 + i * (0.24 / max(1, backs - 1))
            g.box((0, y, -0.30), (length, 0.055, 0.028), "Lib_Board")
        a.end()
    a.box("Col_Seat", (0, 0.455, 0.0), (1.80, 0.028, 0.36))
    a.box("Col_Back", (0, 0.68, -0.30), (1.80, 0.26, 0.022))
    for i, x in enumerate((-0.72, 0.72)):
        for j, z in enumerate((-0.22, 0.22)):
            a.box("Col_Leg_%d%d" % (i, j), (x, 0.21, z), (0.05, 0.40, 0.05))
        a.box("Col_Arm_%d" % i, (x - 0.06 if x < 0 else x + 0.06, 0.62, -0.02), (0.04, 0.028, 0.38))
    return a
