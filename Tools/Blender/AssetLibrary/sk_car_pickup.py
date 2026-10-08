"""Parked pickup shell. 5.30 m long, 1.90 m wide, cab roof at 1.80 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import fender_arch, look_euler


def _span_box(g, a, b, width, thick, mat):
    dx, dy, dz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    length = (dx * dx + dy * dy + dz * dz) ** 0.5
    mid = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5, (a[2] + b[2]) * 0.5)
    g.box(mid, (width, thick, length), mat, euler=look_euler(a, b))


@register
def create():
    a = Asset(
        "Car_Pickup",
        "StreetFurniture",
        "Pickup shell, 4.98 m long, 2.28 m wide over the fender brows. Cab roof 1.77 m, bed sides about 1.12 m. Wheelbase 3.20 m. Fender lips, cab beltline, and a slat grille. Empty cab and bed.",
    )
    a.climb_note = "Sheet metal. Not a cling wall."
    a.vault_note = "Bed sides are about 1.15 m. Not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.008, 0.0)
        bs = 1 if lod == 0 else 0
        seg = lod_pick(lod, 12, 8)
        # Cab
        g.box((0, 0.70, 0.85), (1.82, 0.70, 2.15), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0, 1.45, 0.55), (1.70, 0.62, 1.35), "Lib_PaintWhite", bevel=bev, segs=bs)
        _span_box(g, (0, 1.15, 1.55), (0, 0.85, 2.15), 1.60, 0.05, "Lib_PaintWhite")
        _span_box(g, (0, 1.20, 1.35), (0, 1.65, 0.85), 1.50, 0.04, "Lib_ShopGlass")
        g.box((0, 1.74, 0.45), (1.55, 0.06, 1.05), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0, 1.15, -0.15), (1.55, 0.55, 0.06), "Lib_ShopGlass")
        # Bed
        g.box((0, 0.55, -1.55), (1.70, 0.28, 2.00), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((-0.80, 0.85, -1.55), (0.08, 0.55, 1.85), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0.80, 0.85, -1.55), (0.08, 0.55, 1.85), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0, 0.90, -2.45), (1.70, 0.50, 0.08), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0, 0.48, -1.55), (1.45, 0.06, 1.70), "Lib_SteelDark")
        # Bumpers and lamps
        g.box((0, 0.40, 2.15), (1.78, 0.22, 0.28), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0, 0.38, 2.28), (1.60, 0.16, 0.08), "Lib_Black")
        g.box((0, 0.42, -2.55), (1.70, 0.20, 0.12), "Lib_Black")
        g.box((0.55, 0.55, 2.22), (0.28, 0.10, 0.04), "Lib_PaintCream")
        g.box((-0.55, 0.55, 2.22), (0.28, 0.10, 0.04), "Lib_PaintCream")
        g.box((0.60, 0.70, -2.50), (0.22, 0.10, 0.04), "Lib_PaintRed")
        g.box((-0.60, 0.70, -2.50), (0.22, 0.10, 0.04), "Lib_PaintRed")
        # Front brow on the cab, rear flare outboard of the bed-side collider.
        # Embedded a couple of centimetres so the brow does not share the cab face.
        g.box((0.99, 0.74, 1.60), (0.18, 0.08, 0.70), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((-0.99, 0.74, 1.60), (0.18, 0.08, 0.70), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((1.05, 0.70, -1.60), (0.18, 0.07, 0.66), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((-1.05, 0.70, -1.60), (0.18, 0.07, 0.66), "Lib_PaintWhite", bevel=bev, segs=1)
        for sign, outward in ((1.0, 1.0), (-1.0, -1.0)):
            fender_arch(g, sign * 0.922, 0.36, 1.60, 0.48, 0.060, 0.024, "Lib_SteelDark", outward=outward, steps=8)
            fender_arch(g, sign * 0.860, 0.36, -1.60, 0.48, 0.060, 0.022, "Lib_SteelDark", outward=outward, steps=8)
            g.box((sign * 0.928, 1.05, 0.85), (0.016, 0.028, 1.70), "Lib_Steel")
            g.box((sign * 0.926, 0.48, 0.55), (0.016, 0.040, 0.85), "Lib_Black")
            g.box((sign * 0.926, 0.85, 0.15), (0.014, 0.55, 0.014), "Lib_Black")
            g.box((sign * 0.926, 0.85, 1.35), (0.014, 0.40, 0.014), "Lib_Black")
            g.box((sign * 0.932, 0.95, 0.70), (0.016, 0.040, 0.12), "Lib_SteelDark")
            g.box((sign * 0.870, 1.35, 0.55), (0.030, 0.48, 0.70), "Lib_ShopGlass")
        g.box((0, 0.52, 2.340), (0.72, 0.16, 0.030), "Lib_Black")
        for y in (0.47, 0.52, 0.57):
            g.box((0, y, 2.360), (0.64, 0.014, 0.012), "Lib_SteelDark")
        g.box((0, 0.78, -2.500), (0.16, 0.06, 0.020), "Lib_SteelDark")
        g.box((0, 1.05, -2.500), (1.40, 0.025, 0.018), "Lib_Steel")
        if lod == 0:
            g.box((0.92, 1.35, 0.70), (0.08, 0.08, 0.16), "Lib_PaintWhite")
            g.box((-0.92, 1.35, 0.70), (0.08, 0.08, 0.16), "Lib_PaintWhite")
        for z in (-1.60, 1.60):
            for x in (-1.00, 1.00):
                g.cylinder((x, 0.36, z), 0.36, 0.22, "Lib_Rubber", seg, axis="X")
                g.cylinder((x + (0.10 if x > 0 else -0.10), 0.36, z), 0.20, 0.04, "Lib_Steel", 8, axis="X")
        a.end()
    a.box("Col_Cab", (0, 0.78, 0.85), (1.50, 0.40, 1.70))
    a.box("Col_Upper", (0, 1.45, 0.55), (1.40, 0.40, 1.00))
    a.box("Col_Bed", (0, 0.55, -1.55), (1.20, 0.12, 1.40))
    a.box("Col_SideL", (-0.80, 0.90, -1.55), (0.05, 0.36, 1.50))
    a.box("Col_SideR", (0.80, 0.90, -1.55), (0.05, 0.36, 1.50))
    a.box("Col_Tailgate", (0, 0.90, -2.45), (1.40, 0.32, 0.04))
    for i, z in enumerate((-1.60, 1.60)):
        for j, x in enumerate((-1.00, 1.00)):
            a.box("Col_Wheel_%d%d" % (i, j), (x, 0.36, z), (0.12, 0.42, 0.42))
    return a
