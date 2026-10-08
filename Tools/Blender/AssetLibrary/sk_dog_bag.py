"""Dog-bag dispenser on a post.

Real size: head about 165 x 250 x 100 mm, top of the head at 1.05 m.
A short tail of bags hangs out of the mouth. BAGS is Liberation Sans (OFL).
"""

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
        "Bag dispenser. Post to 1.05 m. Head 0.17 x 0.26 x 0.11 m. BAGS in Liberation Sans.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.012, 0), (0.20, 0.024, 0.20), "Lib_SteelDark", bevel=bev, segs=bs)
        if lod == 0:
            g.box((0, 0.006, 0), (0.14, 0.008, 0.14), "Lib_Rust")
            for x in (-0.07, 0.07):
                for z in (-0.07, 0.07):
                    g.cylinder((x, 0.028, z), 0.008, 0.012, "Lib_Steel", 6)
        g.cylinder((0, 0.50, 0), 0.026, 0.94, "Lib_Steel", seg, bevel=bev, segs=bs)
        g.cylinder((0, 0.96, 0), 0.034, 0.03, "Lib_SteelDark", seg)
        # Housing overlaps the collar.
        g.box((0, 0.96, 0.055), (0.17, 0.26, 0.11), "Lib_PaintGreen", bevel=bev, segs=bs)
        g.box((0, 1.075, 0.055), (0.18, 0.028, 0.12), "Lib_SteelDark", bevel=bev, segs=bs)
        g.cylinder((0, 1.075, 0.0), 0.006, 0.16, "Lib_Steel", 6, axis="X")
        # Mouth, set into the face. The bag tail starts inside the mouth.
        g.box((0, 0.90, 0.108), (0.09, 0.018, 0.012), "Lib_Black")
        if lod == 0:
            g.box((0.012, 0.855, 0.112), (0.055, 0.07, 0.004), "Lib_PaintWhite")
            g.box((0.018, 0.825, 0.114), (0.040, 0.04, 0.003), "Lib_PaintCream")
            g.text("BAGS", (0, 1.02, 0.116), 0.042, "Lib_PaintWhite", extrude=0.003, font=_FONT)
            g.box((0.055, 0.93, 0.112), (0.012, 0.012, 0.006), "Lib_Steel")
        a.end()
    a.box("Col_Base", (0, 0.012, 0), (0.14, 0.016, 0.14))
    a.capsule("Col_Post", (0, 0.48, 0), 0.020, 0.84, 1)
    a.box("Col_Head", (0, 0.96, 0.055), (0.13, 0.20, 0.07))
    return a
