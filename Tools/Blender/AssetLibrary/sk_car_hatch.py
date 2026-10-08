"""Parked hatchback shell. 4.05 m long, 1.70 m wide, 1.50 m tall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import look_euler


def _span_box(g, a, b, width, thick, mat):
    dx, dy, dz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    length = (dx * dx + dy * dy + dz * dz) ** 0.5
    mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5, (a[2] + b[2]) * 0.5)
    g.box(mid, (width, thick, length), mat, euler=look_euler(a, b))


@register
def create():
    a = Asset(
        "Car_Hatch",
        "StreetFurniture",
        "Hatchback shell, 3.98 m long, 2.00 m wide over the tires, roof at 1.49 m. Wheelbase 2.50 m.",
    )
    a.climb_note = "Sheet metal. Not a cling wall."
    a.vault_note = "Roof is a landing, not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.008, 0.0)
        bs = 1 if lod == 0 else 0
        seg = lod_pick(lod, 12, 8)
        g.box((0, 0.58, 0.05), (1.66, 0.48, 3.15), "Lib_PaintRed", bevel=bev, segs=bs)
        g.box((0, 0.40, 1.78), (1.68, 0.26, 0.42), "Lib_PaintRed", bevel=bev, segs=bs)
        g.box((0, 0.42, -1.78), (1.68, 0.30, 0.40), "Lib_PaintRed", bevel=bev, segs=bs)
        g.box((0, 0.34, 1.94), (1.60, 0.14, 0.10), "Lib_Black", bevel=bev, segs=1)
        g.box((0, 0.36, -1.94), (1.62, 0.16, 0.10), "Lib_Black", bevel=bev, segs=1)
        _span_box(g, (0, 0.88, 0.70), (0, 0.62, 1.75), 1.48, 0.05, "Lib_PaintRed")
        _span_box(g, (0, 0.95, 0.35), (0, 1.38, -0.25), 1.36, 0.04, "Lib_ShopGlass")
        g.box((0, 1.46, -0.55), (1.38, 0.06, 1.05), "Lib_PaintRed", bevel=bev, segs=bs)
        _span_box(g, (0, 1.40, -1.10), (0, 0.85, -1.75), 1.36, 0.04, "Lib_ShopGlass")
        g.box((0, 0.55, 1.82), (1.10, 0.10, 0.05), "Lib_PaintCream")
        g.box((0, 0.70, -1.88), (1.20, 0.16, 0.04), "Lib_PaintRed")
        if lod == 0:
            g.box((0.74, 1.00, 0.05), (0.07, 0.07, 0.14), "Lib_PaintRed")
            g.box((-0.74, 1.00, 0.05), (0.07, 0.07, 0.14), "Lib_PaintRed")
        for z in (-1.25, 1.25):
            for x in (-0.90, 0.90):
                g.cylinder((x, 0.30, z), 0.30, 0.18, "Lib_Rubber", seg, axis="X")
                g.cylinder((x + (0.08 if x > 0 else -0.08), 0.30, z), 0.16, 0.035, "Lib_Steel", 8, axis="X")
        a.end()
    a.box("Col_Body", (0, 0.62, 0.05), (1.36, 0.22, 2.60))
    a.box("Col_Nose", (0, 0.42, 1.78), (1.36, 0.14, 0.26))
    a.box("Col_Tail", (0, 0.46, -1.78), (1.36, 0.14, 0.24))
    a.box("Col_Roof", (0, 1.46, -0.55), (1.12, 0.04, 0.80))
    for i, z in enumerate((-1.25, 1.25)):
        for j, x in enumerate((-0.90, 0.90)):
            a.box("Col_Wheel_%d%d" % (i, j), (x, 0.30, z), (0.10, 0.34, 0.34))
    return a
