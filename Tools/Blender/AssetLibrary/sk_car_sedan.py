"""Parked sedan shell. 4.60 m long, 1.76 m wide, 1.45 m tall. No interior."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import look_euler


def _span_box(g, a, b, width, thick, mat, bevel, segs):
    dx = b[0] - a[0]
    dy = b[1] - a[1]
    dz = b[2] - a[2]
    length = (dx * dx + dy * dy + dz * dz) ** 0.5
    mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5, (a[2] + b[2]) * 0.5)
    g.box(mid, (width, thick, length), mat, bevel=bevel, segs=segs, euler=look_euler(a, b))


def _wheels(g, lod, z_axle):
    seg = lod_pick(lod, 12, 8)
    for z in z_axle:
        for x in (-0.92, 0.92):
            g.cylinder((x, 0.32, z), 0.32, 0.22, "Lib_Rubber", seg, axis="X")
            outer = x + (0.11 if x > 0 else -0.11)
            g.cylinder((outer, 0.32, z), 0.16, 0.012, "Lib_Steel", seg, axis="X")


@register
def create():
    a = Asset(
        "Car_Sedan",
        "StreetFurniture",
        "Sedan shell, 4.59 m long, 2.07 m wide over the tires, roof at 1.43 m. Wheelbase 2.70 m. Empty body.",
    )
    a.climb_note = "Sheet metal. Not a cling wall."
    a.vault_note = "Hood and roof are landings, not vault rails."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.008, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.58, 0.05), (1.96, 0.46, 3.70), "Lib_PaintBlue", bevel=bev, segs=bs)
        g.box((0, 0.42, 2.05), (1.96, 0.28, 0.50), "Lib_PaintBlue", bevel=bev, segs=bs)
        g.box((0, 0.40, -2.05), (1.96, 0.26, 0.48), "Lib_PaintBlue", bevel=bev, segs=bs)
        g.box((0, 0.36, 2.22), (1.70, 0.16, 0.12), "Lib_Black", bevel=bev, segs=1)
        g.box((0, 0.36, -2.22), (1.70, 0.16, 0.12), "Lib_Black", bevel=bev, segs=1)
        _span_box(g, (0, 0.86, 0.85), (0, 0.64, 2.05), 1.55, 0.05, "Lib_PaintBlue", bev, bs)
        _span_box(g, (0, 0.92, 0.55), (0, 1.30, -0.15), 1.40, 0.04, "Lib_ShopGlass", 0, 0)
        g.box((0, 1.40, -0.45), (1.42, 0.06, 1.35), "Lib_PaintBlue", bevel=bev, segs=bs)
        _span_box(g, (0, 1.32, -1.15), (0, 0.95, -1.85), 1.38, 0.04, "Lib_ShopGlass", 0, 0)
        g.box((0, 0.78, -1.55), (1.50, 0.08, 0.70), "Lib_PaintBlue", bevel=bev, segs=bs)
        g.box((0, 0.55, 2.08), (1.20, 0.10, 0.06), "Lib_PaintCream")
        g.box((0, 0.58, -2.12), (1.15, 0.08, 0.05), "Lib_PaintRed")
        if lod == 0:
            g.box((0.78, 0.95, 0.15), (0.08, 0.08, 0.16), "Lib_PaintBlue", bevel=0.004, segs=1)
            g.box((-0.78, 0.95, 0.15), (0.08, 0.08, 0.16), "Lib_PaintBlue", bevel=0.004, segs=1)
            g.box((0, 0.34, 1.55), (1.40, 0.04, 0.08), "Lib_Black")
        _wheels(g, lod, (-1.35, 1.35))
        a.end()
    a.box("Col_Body", (0, 0.64, 0.05), (1.55, 0.20, 3.00))
    a.box("Col_Tail", (0, 0.42, -2.02), (1.00, 0.08, 0.16))
    a.box("Col_Roof", (0, 1.40, -0.45), (1.16, 0.04, 1.05))
    for i, z in enumerate((-1.35, 1.35)):
        for j, sign in enumerate((-1, 1)):
            a.box("Col_Wheel_%d%d" % (i, j), (sign * 1.005, 0.32, z), (0.02, 0.32, 0.32))
    return a
