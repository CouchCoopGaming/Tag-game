"""Three newspaper honor boxes on one rail. No brand, no masthead."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "Overpass-Bold.ttf",
))


def _box(g, x, color, lod, font):
    """One honor box. Door, coin door, and paper stack face +Z."""
    bev = lod_pick(lod, 0.003, 0.0)
    bs = 1 if lod == 0 else 0
    seg = lod_pick(lod, 10, 6)
    # Skirt on the ground. The cabinet sleeves it. Cabinet front stops at z=0.09
    # so the paper stack and the glass sit in front of the solid.
    g.box((x, 0.018, -0.02), (0.34, 0.036, 0.24), "Lib_SteelDark", bevel=bev, segs=bs)
    g.box((x, 0.42, -0.02), (0.40, 0.72, 0.22), color, bevel=bev, segs=bs)
    g.box((x, 0.80, 0.0), (0.46, 0.05, 0.36), color, bevel=bev, segs=bs)
    # Door frame. Four bars, so the glass and the paper stack stay visible.
    g.box((x, 0.70, 0.162), (0.34, 0.018, 0.016), "Lib_SteelDark")
    g.box((x, 0.22, 0.162), (0.34, 0.018, 0.016), "Lib_SteelDark")
    g.box((x - 0.161, 0.46, 0.162), (0.018, 0.50, 0.016), "Lib_SteelDark")
    g.box((x + 0.161, 0.46, 0.162), (0.018, 0.50, 0.016), "Lib_SteelDark")
    g.box((x, 0.46, 0.148), (0.28, 0.44, 0.006), "Lib_ShopGlass")
    g.box((x, 0.44, 0.112), (0.24, 0.32, 0.028), "Lib_PaintCream")
    if lod == 0:
        g.box((x + 0.01, 0.52, 0.128), (0.18, 0.05, 0.004), "Lib_PaintBlue")
        g.box((x, 0.44, 0.130), (0.16, 0.004, 0.004), "Lib_Black")
        g.text("NEWS", (x, 0.58, 0.156), 0.038, "Lib_PaintWhite", extrude=0.0015, font=font)
    g.cylinder((x - 0.175, 0.46, 0.170), 0.008, 0.48, "Lib_Steel", seg, axis="Y")
    g.box((x + 0.10, 0.22, 0.166), (0.10, 0.08, 0.016), "Lib_Steel")
    g.box((x + 0.10, 0.22, 0.176), (0.06, 0.008, 0.006), "Lib_Black")
    g.cylinder((x - 0.02, 0.24, 0.174), 0.010, 0.016, "Lib_Steel", 8, axis="Z")


@register
def create():
    a = Asset(
        "NewspaperRack",
        "StreetFurniture",
        "Three honor boxes, 1.46 m overall, lid at 0.82 m. Glass doors and coin slots. No masthead.",
    )
    a.climb_note = "Not a cling surface."
    a.vault_note = "Too short to vault."
    colors = ("Lib_PaintBlue", "Lib_PaintRed", "Lib_PaintGreen")
    xs = (-0.48, 0.0, 0.48)
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.002, 0.0)
        # Rail on the ground tying the three skirts together.
        g.box((0.0, 0.012, 0.0), (1.46, 0.024, 0.08), "Lib_SteelDark", bevel=bev, segs=1)
        for x, color in zip(xs, colors):
            _box(g, x, color, lod, _FONT)
        a.end()
    for i, x in enumerate(xs):
        # Inside the cabinet only, clear of the door, the papers, and the lid.
        a.box("Col_Cab_%d" % i, (x, 0.44, -0.02), (0.26, 0.48, 0.16))
        a.box("Col_Lid_%d" % i, (x, 0.808, 0.0), (0.30, 0.016, 0.20))
    return a
