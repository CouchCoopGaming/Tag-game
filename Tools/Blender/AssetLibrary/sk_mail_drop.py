"""Corner mail collection box. Not the newspaper honor rack."""

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
        "MailDrop_Corner",
        "StreetFurniture",
        "Collection box, 0.62 m wide and 1.20 m tall. Pedestal, chute, and MAIL in Liberation Sans.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.005, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.04, 0), (0.50, 0.08, 0.42), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0, 0.605, 0), (0.58, 1.11, 0.46), "Lib_PaintBlue", bevel=bev, segs=bs)
        g.box((0, 1.16, 0), (0.64, 0.08, 0.52), "Lib_PaintBlue", bevel=bev, segs=bs)
        # Chute door on the front, proud of the body by a few millimeters.
        g.box((0, 0.95, 0.24), (0.36, 0.16, 0.02), "Lib_SteelDark", bevel=0.003 if lod == 0 else 0, segs=bs)
        g.box((0, 0.95, 0.252), (0.28, 0.035, 0.008), "Lib_Black")
        g.box((0, 0.55, 0.238), (0.22, 0.28, 0.012), "Lib_Steel")
        g.cylinder((0.16, 0.55, 0.25), 0.016, 0.012, "Lib_Brass", 8, axis="Z")
        if lod == 0:
            g.text("MAIL", (0, 0.78, 0.242), 0.09, "Lib_PaintWhite", extrude=0.004, font=_FONT)
        a.end()
    a.box("Col_Foot", (0, 0.022, 0), (0.28, 0.028, 0.22))
    a.box("Col_Body", (0, 0.62, 0), (0.46, 0.82, 0.34))
    a.box("Col_Cap", (0, 1.178, 0), (0.40, 0.028, 0.32))
    return a
