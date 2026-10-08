"""Three newspaper honor boxes on one rail. No shared mesh with NewspaperBox."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _box(g, x, color, lod, bev):
    g.box((x, 0.08, 0), (0.40, 0.12, 0.32), "Lib_SteelDark", bevel=bev, segs=1)
    g.box((x, 0.52, 0), (0.42, 0.72, 0.34), color, bevel=bev, segs=1 if lod == 0 else 0)
    g.box((x, 0.90, 0), (0.46, 0.04, 0.38), color, bevel=bev, segs=1)
    g.box((x, 0.55, 0.175), (0.30, 0.42, 0.01), "Lib_Glass")
    g.box((x, 0.28, 0.18), (0.16, 0.06, 0.016), "Lib_Steel")
    g.box((x, 0.78, 0.18), (0.28, 0.06, 0.012), "Lib_PaintWhite")
    if lod == 0:
        g.cylinder((x, 0.32, 0.19), 0.012, 0.012, "Lib_Brass", 6, axis="Z")
        g.box((x, 0.20, 0.18), (0.10, 0.03, 0.012), "Lib_Black")


@register
def create():
    a = Asset(
        "NewspaperRack",
        "StreetFurniture",
        "Three honor boxes, 1.55 m overall, 0.92 m tall. Glass fronts and coin doors.",
    )
    a.climb_note = "Not a cling surface."
    a.vault_note = "Too short to vault."
    colors = ("Lib_PaintBlue", "Lib_PaintRed", "Lib_PaintGreen")
    xs = (-0.50, 0.0, 0.50)
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.015, 0), (1.55, 0.03, 0.08), "Lib_SteelDark", bevel=bev, segs=1)
        for x, color in zip(xs, colors):
            _box(g, x, color, lod, bev)
        a.end()
    a.box("Col_Rail", (0, 0.015, 0), (1.50, 0.024, 0.06))
    for i, x in enumerate(xs):
        a.box("Col_Cab_%d" % i, (x, 0.48, 0), (0.40, 0.84, 0.32))
        a.box("Col_Cap_%d" % i, (x, 0.90, 0), (0.44, 0.03, 0.36))
    return a
