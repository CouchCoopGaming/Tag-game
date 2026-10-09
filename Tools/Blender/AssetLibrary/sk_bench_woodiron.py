"""Wood-slat bench on a cast frame, with a center leg. Seat top at 0.45 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import polyline


@register
def create():
    a = Asset(
        "Bench_WoodIron",
        "StreetFurniture",
        "1.83 m wood bench. Four seat slats and three back slats, cast legs, center leg. Seat top 0.45 m.",
    )
    a.climb_note = "Back slats are too broken up to cling. Not a wall-run panel."
    a.vault_note = "Seat top is 0.45 m. Below the 0.90–1.05 m vault band."
    xs = (-0.78, 0.0, 0.78)
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = lod_pick(lod, 2, 1)
        iron = "Lib_Black"
        for x in xs:
            for z, h in ((-0.20, 0.42), (0.18, 0.42)):
                g.box((x, h * 0.5, z), (0.055, h, 0.045), iron, bevel=bev, segs=bs)
                g.box((x, 0.015, z), (0.12, 0.03, 0.11), iron, bevel=bev, segs=1)
            if lod == 0:
                polyline(g, [
                    (x, 0.08, -0.16),
                    (x, 0.20, 0.0),
                    (x, 0.08, 0.14),
                ], 0.012, iron, 6)
            g.box((x, 0.62, -0.30), (0.05, 0.40, 0.04), iron, bevel=bev, segs=1)
            if abs(x) > 0.1:
                polyline(g, [
                    (x, 0.50, 0.16),
                    (x, 0.58, 0.0),
                    (x, 0.66, -0.26),
                ], 0.014, iron, 6)
                g.box((x, 0.67, -0.02), (0.07, 0.022, 0.42), "Lib_Wood", bevel=bev, segs=bs, uv_scale=1.5)
        g.box((0, 0.42, -0.20), (1.70, 0.035, 0.04), iron, bevel=bev, segs=1)
        g.box((0, 0.42, 0.18), (1.70, 0.035, 0.04), iron, bevel=bev, segs=1)
        seat_n = lod_pick(lod, 4, 3)
        for i in range(seat_n):
            z = -0.10 + i * (0.26 / max(1, seat_n - 1))
            g.box((0, 0.452, z), (1.78, 0.028, 0.055), "Lib_Wood", bevel=bev, segs=bs, uv_scale=1.2)
            if lod == 0:
                for x in (-0.78, 0.78):
                    g.cylinder((x, 0.452, z), 0.008, 0.012, "Lib_Steel", 6, axis="Z")
        for i, y in enumerate((0.58, 0.70, 0.82)):
            if lod >= 1 and i == 1:
                continue
            g.box((0, y, -0.32), (1.78, 0.04, 0.022), "Lib_Wood", bevel=bev, segs=bs, uv_scale=1.2)
        a.end()
    for i, x in enumerate(xs):
        a.box("Col_LegF_%d" % i, (x, 0.24, 0.18), (0.040, 0.34, 0.032))
        a.box("Col_LegB_%d" % i, (x, 0.24, -0.20), (0.040, 0.34, 0.032))
        a.box("Col_FootF_%d" % i, (x, 0.015, 0.18), (0.10, 0.024, 0.09))
        a.box("Col_FootB_%d" % i, (x, 0.015, -0.20), (0.10, 0.024, 0.09))
        a.box("Col_Post_%d" % i, (x, 0.62, -0.30), (0.042, 0.36, 0.032))
        if abs(x) > 0.1:
            a.box("Col_Arm_%d" % i, (x, 0.67, -0.02), (0.06, 0.016, 0.38))
    for i in range(4):
        z = -0.10 + i * (0.26 / 3.0)
        a.box("Col_Seat_%d" % i, (0, 0.452, z), (1.74, 0.022, 0.046))
    for i, y in enumerate((0.58, 0.70, 0.82)):
        a.box("Col_Back_%d" % i, (0, y, -0.32), (1.70, 0.028, 0.014))
    return a
