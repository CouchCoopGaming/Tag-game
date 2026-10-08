"""96-gallon wheeled trash cart.

Real size: about 28 x 34 x 46 in (0.72 x 0.86 x 1.16 m).
Two 10 in wheels share an axle that passes into the body.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _frustum(g, y0, y1, hx0, hz0, hx1, hz1, mat, bevel, segs):
    verts = [
        (-hx0, y0, -hz0), (hx0, y0, -hz0), (hx0, y0, hz0), (-hx0, y0, hz0),
        (-hx1, y1, -hz1), (hx1, y1, -hz1), (hx1, y1, hz1), (-hx1, y1, hz1),
    ]
    g.mesh(verts, [
        (0, 3, 2, 1),
        (4, 5, 6, 7),
        (0, 1, 5, 4),
        (1, 2, 6, 5),
        (2, 3, 7, 6),
        (3, 0, 4, 7),
    ], mat, bevel=bevel, segs=segs)


def _cart(g, lod, body, lid):
    seg = lod_pick(lod, 12, 8)
    bev = lod_pick(lod, 0.004, 0.0)
    bs = 1 if lod == 0 else 0
    # Body drops to the axle. Wheels sit on the ground and overlap the rear corners.
    _frustum(g, 0.08, 1.02, 0.30, 0.32, 0.35, 0.40, body, bev, bs)
    g.box((0, 1.04, 0.02), (0.74, 0.08, 0.78), lid, bevel=bev, segs=bs)
    g.box((0, 1.09, 0.02), (0.50, 0.03, 0.36), lid, bevel=bev, segs=bs)
    g.cylinder((0, 1.02, -0.36), 0.012, 0.62, "Lib_Steel", 6, axis="X")
    g.cylinder((0, 0.125, -0.30), 0.014, 0.78, "Lib_SteelDark", 8, axis="X")
    for x in (-0.34, 0.34):
        g.cylinder((x, 0.125, -0.34), 0.125, 0.05, "Lib_Rubber", seg, axis="X")
        g.cylinder((x, 0.125, -0.34), 0.045, 0.058, "Lib_Steel", 8, axis="X")
        if lod == 0:
            g.cylinder((x, 0.125, -0.34), 0.016, 0.064, "Lib_SteelDark", 6, axis="X")
    g.box((0, 0.20, 0.34), (0.42, 0.06, 0.06), "Lib_Black")
    g.pipe((-0.22, 1.09, -0.28), (-0.22, 1.26, -0.42), 0.012, "Lib_Steel", 6)
    g.pipe((0.22, 1.09, -0.28), (0.22, 1.26, -0.42), 0.012, "Lib_Steel", 6)
    g.pipe((-0.22, 1.26, -0.42), (0.22, 1.26, -0.42), 0.014, "Lib_Steel", 6)
    if lod == 0:
        g.box((0, 0.16, -0.30), (0.36, 0.05, 0.016), "Lib_Rust")
        g.box((0.18, 0.62, 0.355), (0.16, 0.22, 0.012), "Lib_PaintWhite")


@register
def create():
    a = Asset(
        "TrashCart_96",
        "StreetFurniture",
        "96-gallon cart, 0.74 x 0.86 x 1.16 m. Two 10 in wheels on a shared axle.",
    )
    a.climb_note = "Lid is rounded and loose. Not a cling."
    a.vault_note = "Lid is 1.12 m and small. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        _cart(g, lod, "Lib_BoxGreen", "Lib_PaintGreen")
        a.end()
    a.box("Col_Body", (0, 0.54, 0.02), (0.50, 0.80, 0.52))
    a.box("Col_Lid", (0, 1.045, 0.02), (0.60, 0.04, 0.62))
    for i, x in enumerate((-0.34, 0.34)):
        a.box("Col_Wheel_%d" % i, (x, 0.125, -0.34), (0.03, 0.16, 0.16))
    return a
