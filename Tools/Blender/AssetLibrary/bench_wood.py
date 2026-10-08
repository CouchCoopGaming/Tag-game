"""Park bench. Board seat on a continuous cast-iron side frame. Seat is 0.45 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Bench_Wood",
        "StreetFurniture",
        "1.80 m park bench. Five board seat slats and three back slats. Each end is one cast-iron frame: foot, leg, seat rail, back post, and arm.",
    )
    a.climb_note = "Back slats are too broken up to cling. Not a wall-run panel."
    a.vault_note = "Seat is 0.45 m. Below the 0.90–1.05 m vault band."
    length = 1.80
    for lod in (0, 1):
        g = a.begin(lod)
        slats = lod_pick(lod, 5, 3)
        backs = lod_pick(lod, 3, 2)
        for x in (-0.74, 0.74):
            # Front foot and leg.
            g.box((x, 0.02, 0.24), (0.11, 0.04, 0.16), "Lib_SteelDark")
            g.box((x, 0.22, 0.24), (0.046, 0.44, 0.046), "Lib_SteelDark")
            # Rear foot and one post that runs from the foot up through the back.
            g.box((x, 0.02, -0.28), (0.12, 0.04, 0.18), "Lib_SteelDark")
            g.box((x, 0.46, -0.28), (0.048, 0.92, 0.048), "Lib_SteelDark")
            # Seat rail and lower stretcher, buried in both legs.
            g.box((x, 0.42, -0.02), (0.042, 0.042, 0.58), "Lib_SteelDark")
            g.box((x, 0.14, -0.02), (0.032, 0.032, 0.48), "Lib_SteelDark")
            # Arm leaves the back post and lands on a bracket over the front leg.
            g.box((x, 0.64, -0.02), (0.050, 0.032, 0.56), "Lib_SteelDark")
            g.box((x, 0.50, 0.22), (0.040, 0.32, 0.040), "Lib_SteelDark")
            g.box((x, 0.662, -0.02), (0.072, 0.020, 0.52), "Lib_Board")
        # Rails tying the two frames, under the slats and behind the back.
        g.box((0, 0.43, 0.24), (1.52, 0.040, 0.040), "Lib_SteelDark")
        g.box((0, 0.43, -0.28), (1.52, 0.040, 0.040), "Lib_SteelDark")
        g.box((0, 0.78, -0.28), (1.52, 0.036, 0.036), "Lib_SteelDark")
        # Boards span the front rail (z=0.24) and the rear rail (z=-0.28).
        span0, span1 = -0.26, 0.24
        pitch = (span1 - span0) / slats
        for i in range(slats):
            z = span0 + (i + 0.5) * pitch
            width = pitch * 0.86
            g.box((0, 0.455, z), (length, 0.032, width), "Lib_Board")
        for i in range(backs):
            y = 0.56 + i * (0.24 / max(1, backs - 1))
            g.box((0, y, -0.30), (length * 0.92, 0.050, 0.028), "Lib_Board")
        a.end()
    a.box("Col_Seat", (0, 0.455, -0.01), (1.70, 0.026, 0.40))
    a.box("Col_Back", (0, 0.68, -0.30), (1.60, 0.22, 0.020))
    for i, x in enumerate((-0.74, 0.74)):
        a.box("Col_LegF_%d" % i, (x, 0.22, 0.24), (0.036, 0.36, 0.036))
        a.box("Col_LegR_%d" % i, (x, 0.36, -0.28), (0.036, 0.60, 0.036))
        a.box("Col_Arm_%d" % i, (x, 0.64, -0.02), (0.036, 0.022, 0.40))
    return a
