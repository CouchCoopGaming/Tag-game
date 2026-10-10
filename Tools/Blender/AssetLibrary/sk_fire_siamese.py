"""Freestanding fire-department connection.

Real size: sidewalk FDC about 0.58 m tall. Body OD 152 mm. Two 65 mm (2.5 in)
brass outlets angle up and out. Caps are chained to the body.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import chain, span_collider

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "LiberationSans-Regular.ttf",
))


def _outlet(g, a, b, seg, lod):
    g.pipe(a, b, 0.026, "Lib_Brass", segments=seg)
    # Cap sits on the outer end and overlaps the pipe.
    dx = b[0] - a[0]
    dy = b[1] - a[1]
    dz = b[2] - a[2]
    length = (dx * dx + dy * dy + dz * dz) ** 0.5 or 1.0
    ux, uy, uz = dx / length, dy / length, dz / length
    cap = (b[0] + ux * 0.012, b[1] + uy * 0.012, b[2] + uz * 0.012)
    lip = (b[0] - ux * 0.008, b[1] - uy * 0.008, b[2] - uz * 0.008)
    g.pipe(lip, cap, 0.036, "Lib_Brass", segments=seg)
    if lod == 0:
        g.pipe(cap, (cap[0] + ux * 0.008, cap[1] + uy * 0.008, cap[2] + uz * 0.008), 0.012, "Lib_SteelDark", segments=6)


@register
def create():
    a = Asset(
        "FireSiamese_Post",
        "StreetFurniture",
        "Siamese connection, 0.58 m tall. Body 152 mm OD. Two 65 mm outlets face +Z.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    left_a = (-0.045, 0.30, 0.06)
    left_b = (-0.145, 0.42, 0.155)
    right_a = (0.045, 0.30, 0.06)
    right_b = (0.145, 0.42, 0.155)
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.018, 0), (0.36, 0.036, 0.28), "Lib_SteelDark", bevel=bev, segs=bs)
        if lod == 0:
            g.box((0, 0.008, 0.0), (0.30, 0.012, 0.22), "Lib_Rust")
            for x in (-0.14, 0.14):
                for z in (-0.09, 0.09):
                    g.cylinder((x, 0.042, z), 0.010, 0.016, "Lib_Steel", 6)
        g.cylinder((0, 0.08, 0), 0.105, 0.06, "Lib_SteelDark", seg)
        g.cylinder((0, 0.26, 0), 0.076, 0.32, "Lib_Hydrant", seg, bevel=bev, segs=bs)
        g.torus((0, 0.16, 0), 0.082, 0.008, "Lib_SteelDark", seg, 6)
        g.torus((0, 0.38, 0), 0.082, 0.008, "Lib_Steel", seg, 6)
        g.cylinder((0, 0.46, 0), 0.090, 0.045, "Lib_SteelDark", seg)
        g.cylinder((0, 0.50, 0), 0.028, 0.04, "Lib_Brass", 8)
        _outlet(g, left_a, left_b, seg, lod)
        _outlet(g, right_a, right_b, seg, lod)
        if lod == 0:
            g.box((0, 0.22, 0.082), (0.10, 0.045, 0.008), "Lib_PaintWhite", bevel=0.001, segs=1)
            g.text("FDC", (0, 0.22, 0.090), 0.028, "Lib_Hydrant", extrude=0.003, font=_FONT)
            g.sphere((-0.07, 0.24, 0.07), 0.010, "Lib_SteelDark", 6)
            g.sphere((0.07, 0.24, 0.07), 0.010, "Lib_SteelDark", 6)
            chain(g, (-0.07, 0.24, 0.07), (-0.15, 0.40, 0.15), n=5, sag=0.03, radius=0.004)
            chain(g, (0.07, 0.24, 0.07), (0.15, 0.40, 0.15), n=5, sag=0.03, radius=0.004)
        a.end()
    a.box("Col_Foot", (0, 0.018, 0), (0.28, 0.024, 0.20))
    a.capsule("Col_Body", (0, 0.26, 0), 0.058, 0.28, 1)
    a.capsule("Col_Cap", (0, 0.47, 0), 0.045, 0.06, 1)
    for name, p0, p1 in (("Col_OutL", left_a, left_b), ("Col_OutR", right_a, right_b)):
        center, size, euler = span_collider(p0, p1, 0.020)
        a.box(name, center, size, euler=euler)
    return a
