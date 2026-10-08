"""Blue street recycling bin. White rim, green lid, about 0.95 m tall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "RecyclingBin",
        "StreetFurniture",
        "Street recycling bin, 0.52 m wide and 0.95 m to the lid. Blue body, white rim, green lid, and a drop slot.",
    )
    a.climb_note = "Not a cling surface."
    a.vault_note = "Too short and narrow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        seg = lod_pick(lod, 10, 6)
        g.box((0, 0.40, 0), (0.48, 0.76, 0.40), "Lib_PaintBlue", bevel=bev, segs=1)
        # White rim clears the body. Green lid clears the rim.
        g.box((0, 0.798, 0), (0.52, 0.028, 0.44), "Lib_PaintWhite")
        g.box((0, 0.90, 0), (0.50, 0.14, 0.42), "Lib_PaintGreen", bevel=bev, segs=1)
        g.box((0, 0.955, 0), (0.22, 0.012, 0.08), "Lib_Black")
        if lod == 0:
            g.cylinder((0, 0.40, 0.212), 0.07, 0.012, "Lib_PaintWhite", seg, axis="Z")
            for ang_x, ang_y in ((0.0, 0.028), (-0.024, -0.016), (0.024, -0.016)):
                g.box((ang_x, 0.40 + ang_y, 0.230), (0.028, 0.012, 0.008), "Lib_PaintGreen")
        a.end()
    a.box("Col_Body", (0, 0.40, 0), (0.44, 0.72, 0.36))
    a.box("Col_Lid", (0, 0.90, 0), (0.46, 0.12, 0.38))
    return a
