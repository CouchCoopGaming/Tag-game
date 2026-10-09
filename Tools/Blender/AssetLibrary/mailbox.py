"""Curbside mailbox on a post. Box center at 1.05 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Mailbox",
        "StreetFurniture",
        "Curbside mailbox. Concrete footing, post to 0.92 m, rounded box with a hinged door, a red flag on +X, and a newspaper tube.",
    )
    a.climb_note = "Post is a 5 cm tube. Not a cling."
    a.vault_note = "No vault edge."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        g.cylinder((0, 0.018, 0), 0.08, 0.036, "Lib_Concrete", seg)
        g.cylinder((0, 0.48, 0), 0.025, 0.88, "Lib_SteelDark", seg)
        g.box((0, 1.02, 0.02), (0.22, 0.22, 0.46), "Lib_Steel", bevel=0.006 if lod == 0 else 0, segs=1)
        g.cylinder((0, 1.13, 0.02), 0.11, 0.46, "Lib_Steel", seg, axis="Z", bevel=0.004 if lod == 0 else 0, segs=1)
        # Door proud of the box, with a raised panel.
        g.box((0, 1.02, 0.268), (0.16, 0.14, 0.012), "Lib_SteelDark")
        g.box((0, 1.02, 0.280), (0.11, 0.09, 0.008), "Lib_Steel")
        g.cylinder((-0.07, 1.07, 0.292), 0.007, 0.028, "Lib_Steel", 6, axis="Y")
        g.cylinder((-0.07, 0.97, 0.292), 0.007, 0.028, "Lib_Steel", 6, axis="Y")
        g.box((0.055, 1.02, 0.316), (0.018, 0.04, 0.010), "Lib_Brass")
        # Flag up on the +X side.
        g.cylinder((0.145, 1.08, -0.10), 0.006, 0.03, "Lib_Steel", 6, axis="X")
        g.box((0.168, 1.16, 0.02), (0.008, 0.16, 0.18), "Lib_PaintRed", euler=(0.0, 0.0, 18.0))
        # Newspaper tube under the box, clear of the post.
        g.cylinder((0.08, 0.84, 0.02), 0.030, 0.30, "Lib_SteelDark", max(8, seg // 2), axis="Z")
        if lod == 0:
            g.box((0, 0.90, 0.268), (0.10, 0.012, 0.008), "Lib_Black")
        a.end()
    a.capsule("Col_Post", (0, 0.48, 0), 0.022, 0.84, 1)
    a.box("Col_Box", (0, 1.05, 0.02), (0.20, 0.24, 0.44))
    a.capsule("Col_Tube", (0.08, 0.84, 0.02), 0.024, 0.26, 2)
    return a
