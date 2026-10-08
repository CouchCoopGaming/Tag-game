"""Playground set. Swings, a slide, and a low climber on a rubber pad."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _ramp(g, x0, y0, x1, y1, z0, z1, thick, mat):
    dx, dy = x1 - x0, y1 - y0
    length = math.hypot(dx, dy) or 1.0
    nx, ny = -dy / length, dx / length
    if ny < 0.0:
        nx, ny = -nx, -ny
    o0 = (x0 - nx * thick, y0 - ny * thick)
    o1 = (x1 - nx * thick, y1 - ny * thick)
    verts = [
        (x0, y0, z0), (x1, y1, z0), (x1, y1, z1), (x0, y0, z1),
        (o0[0], o0[1], z0), (o1[0], o1[1], z0), (o1[0], o1[1], z1), (o0[0], o0[1], z1),
    ]
    g.mesh(verts, [
        (0, 1, 2, 3), (4, 7, 6, 5),
        (0, 3, 7, 4), (1, 5, 6, 2),
        (0, 4, 5, 1), (3, 2, 6, 7),
    ], mat, uv_scale=1.0)


@register
def create():
    a = Asset(
        "Playground",
        "Park",
        "Play set about 6.0 x 3.6 m. Two swings, a slide, and a three-bar climber on a rubber pad. Pivot is the pad center.",
    )
    a.climb_note = "Bars and posts are too small to cling. Not a wall."
    a.vault_note = "The climber bars are under 1.25 m and too narrow to vault."
    # Slide runs down toward +X. Pitch is a Unity Z roll so local +X follows the slope.
    slide_roll = math.degrees(math.atan2(-0.92, 1.12))
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.025, 0), (6.0, 0.05, 3.6), "Lib_Rubber", bevel=bev, segs=1, uv_scale=0.6)
        for z in (-1.05, 1.05):
            g.box((-2.15, 1.20, z), (0.08, 2.28, 0.08), "Lib_PaintBlue", bevel=bev, segs=1)
        g.box((-2.15, 2.40, 0), (0.10, 0.08, 2.30), "Lib_PaintBlue", bevel=bev, segs=1)
        if lod < 2:
            for z in (-0.42, 0.42):
                g.pipe((-2.15, 2.33, z), (-2.15, 0.52, z), 0.012, "Lib_Steel", 5)
                g.pipe((-2.08, 2.33, z), (-2.08, 0.52, z), 0.012, "Lib_Steel", 5)
                g.box((-2.15, 0.46, z), (0.36, 0.04, 0.16), "Lib_Rubber", bevel=bev, segs=1)
        g.box((1.05, 1.14, 0), (0.95, 0.06, 0.90), "Lib_PaintBlue", bevel=bev, segs=1)
        for x in (0.70, 1.40):
            for z in (-0.34, 0.34):
                g.box((x, 0.58, z), (0.07, 1.04, 0.07), "Lib_Steel", bevel=bev, segs=1)
        if lod < 2:
            for z in (-0.22, 0.22):
                g.box((0.46, 0.58, z), (0.05, 1.04, 0.05), "Lib_Steel")
            for y in (0.30, 0.55, 0.80):
                g.box((0.46, y, 0), (0.04, 0.025, 0.36), "Lib_Steel")
        _ramp(g, 1.58, 1.08, 2.70, 0.16, -0.20, 0.20, 0.045, "Lib_Orange")
        if lod < 2:
            g.pipe((1.62, 1.06, 0.28), (2.66, 0.20, 0.28), 0.02, "Lib_PaintRed", 6)
            g.pipe((1.62, 1.06, -0.28), (2.66, 0.20, -0.28), 0.02, "Lib_PaintRed", 6)
        for z in (-0.50, 0.50):
            g.box((-0.15, 0.68, z), (0.07, 1.24, 0.07), "Lib_PaintRed", bevel=bev, segs=1)
        for y in (0.48, 0.82, 1.18):
            g.box((-0.15, y, 0), (0.045, 0.04, 0.86), "Lib_Steel")
        a.end()
    a.box("Col_Pad", (0, 0.025, 0), (5.90, 0.04, 3.50))
    for i, z in enumerate((-1.05, 1.05)):
        a.box("Col_SwingPost_%d" % i, (-2.15, 1.20, z), (0.06, 2.20, 0.06))
    a.box("Col_Beam", (-2.15, 2.40, 0), (0.07, 0.05, 2.16))
    for i, z in enumerate((-0.42, 0.42)):
        a.box("Col_Seat_%d" % i, (-2.15, 0.46, z), (0.28, 0.028, 0.10))
    a.box("Col_Deck", (1.05, 1.14, 0), (0.86, 0.04, 0.80))
    for i, (x, z) in enumerate(((0.70, -0.34), (0.70, 0.34), (1.40, -0.34), (1.40, 0.34))):
        a.box("Col_Leg_%d" % i, (x, 0.58, z), (0.05, 0.96, 0.05))
    a.box("Col_Slide", (2.12, 0.60, 0), (1.00, 0.024, 0.26), euler=(0.0, 0.0, slide_roll))
    for i, z in enumerate((-0.50, 0.50)):
        a.box("Col_Climb_%d" % i, (-0.15, 0.68, z), (0.05, 1.16, 0.05))
    for i, y in enumerate((0.48, 0.82, 1.18)):
        a.box("Col_Bar_%d" % i, (-0.15, y, 0), (0.03, 0.025, 0.78))
    return a
