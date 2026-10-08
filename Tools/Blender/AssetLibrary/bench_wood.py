"""Park bench. Wood slats on a steel frame. Seat is 0.45 m, not a vault rail."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Bench_Wood",
        "StreetFurniture",
        "1.80 m park bench. Five seat slats and three back slats on a steel frame. Seat height 0.45 m.",
    )
    a.climb_note = "Back slats are too broken up to cling. Not a wall-run panel."
    a.vault_note = "Seat is 0.45 m. Below the 0.90–1.05 m vault band."
    length = 1.80
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.002)
        bs = lod_pick(lod, 2, 1)
        slats = lod_pick(lod, 5, 3)
        backs = lod_pick(lod, 3, 2)
        # Cast-iron legs: a box core the collider can sit in, plus feet and an arch.
        for x in (-0.72, 0.72):
            for z in (-0.22, 0.22):
                g.box((x, 0.22, z), (0.05, 0.44, 0.05), "Lib_SteelDark", bevel=bev, segs=bs)
                g.box((x, 0.015, z), (0.11, 0.03, 0.12), "Lib_SteelDark", bevel=bev, segs=1)
            g.pipe((x, 0.05, -0.16), (x, 0.22, 0.0), 0.016, "Lib_SteelDark", 6)
            g.pipe((x, 0.22, 0.0), (x, 0.05, 0.16), 0.016, "Lib_SteelDark", 6)
            g.pipe((x, 0.42, 0.16), (x, 0.60, -0.02), 0.016, "Lib_SteelDark", 6)
        g.box((0, 0.42, -0.22), (1.55, 0.04, 0.04), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 0.42, 0.22), (1.55, 0.04, 0.04), "Lib_SteelDark", bevel=bev, segs=1)
        # Seat slats.
        span = 0.46
        gap = span / slats
        for i in range(slats):
            z = -0.18 + (i + 0.5) * (0.36 / slats)
            g.box((0, 0.455, z), (length, 0.028, gap * 0.82), "Lib_Wood", bevel=bev, segs=bs, uv_scale=1.2)
        # Back posts and slats.
        for x in (-0.72, 0.72):
            g.box((x, 0.64, -0.28), (0.045, 0.42, 0.04), "Lib_SteelDark", bevel=bev, segs=1)
        for i in range(backs):
            y = 0.58 + i * (0.22 / max(1, backs - 1))
            g.box((0, y, -0.30), (length, 0.045, 0.022), "Lib_Wood", bevel=bev, segs=bs, uv_scale=1.2)
        # Wood armrests on a steel rail.
        for x in (-0.78, 0.78):
            g.box((x, 0.62, -0.02), (0.04, 0.028, 0.40), "Lib_SteelDark", bevel=bev, segs=bs)
            g.box((x, 0.648, -0.02), (0.07, 0.022, 0.46), "Lib_Wood", bevel=bev, segs=bs, uv_scale=1.4)
        a.end()
    a.box("Col_Seat", (0, 0.455, 0.0), (1.80, 0.028, 0.36))
    a.box("Col_Back", (0, 0.68, -0.30), (1.80, 0.26, 0.022))
    for i, x in enumerate((-0.72, 0.72)):
        for j, z in enumerate((-0.22, 0.22)):
            a.box("Col_Leg_%d%d" % (i, j), (x, 0.22, z), (0.05, 0.44, 0.05))
        a.box("Col_Arm_%d" % i, (x - 0.06 if x < 0 else x + 0.06, 0.62, -0.02), (0.04, 0.028, 0.38))
    return a
