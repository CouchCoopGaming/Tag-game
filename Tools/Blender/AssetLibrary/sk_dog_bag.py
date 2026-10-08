"""Dog-bag dispenser on a post."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "LiberationSans-Regular.ttf",
))


@register
def create():
    a = Asset(
        "DogBag_Post",
        "StreetFurniture",
        "Bag dispenser. Post to 1.05 m, box at 0.95 m. BAGS in Liberation Sans.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.015, 0), (0.18, 0.03, 0.18), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.52, 0), 0.028, 0.98, "Lib_Steel", seg)
        g.box((0, 0.98, 0.04), (0.16, 0.22, 0.10), "Lib_PaintBlue", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 0.92, 0.094), (0.10, 0.02, 0.008), "Lib_Black")
        if lod == 0:
            g.text("BAGS", (0, 1.04, 0.098), 0.05, "Lib_PaintWhite", extrude=0.004, font=_FONT)
        a.end()
    a.box("Col_Base", (0, 0.015, 0), (0.12, 0.02, 0.12))
    a.capsule("Col_Post", (0, 0.46, 0), 0.020, 0.76, 1)
    a.box("Col_Box", (0, 0.98, 0.055), (0.10, 0.12, 0.04))
    return a
