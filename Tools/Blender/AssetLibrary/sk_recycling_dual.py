"""Two-stream street recycling bin. Blue and green lids, slot hoods, casters."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "RecyclingBin_Dual",
        "StreetFurniture",
        "Two-stream bin, 1.10 m wide, 0.48 m deep, 1.04 m to the lids. Blue and green lids, four casters.",
    )
    a.climb_note = "The front is broken by the slots. Not a cling wall."
    a.vault_note = "Lid is 1.04 m, but the top is a thin lid over an open slot, not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = lod_pick(lod, 1, 0)
        seg = lod_pick(lod, 12, 8)
        for x in (-0.52, 0.52):
            for z in (-0.21, 0.21):
                g.box((x, 0.50, z), (0.045, 0.98, 0.045), "Lib_SteelDark", bevel=bev, segs=bs)
                g.cylinder((x, 0.04, z), 0.04, 0.045, "Lib_Rubber", seg, axis="X")
        g.box((0, 0.12, 0), (1.00, 0.03, 0.38), "Lib_Steel", bevel=bev, segs=1)
        g.box((0, 0.56, 0.20), (1.00, 0.82, 0.02), "Lib_PaintBlue", bevel=bev, segs=bs)
        for x in (-0.50, 0.50):
            g.box((x, 0.56, 0.0), (0.02, 0.82, 0.38), "Lib_PaintBlue", bevel=bev, segs=1)
        g.box((0, 0.26, -0.21), (1.00, 0.22, 0.02), "Lib_PaintBlue", bevel=bev, segs=1)
        g.box((0, 0.52, -0.21), (1.00, 0.08, 0.02), "Lib_SteelDark")
        g.box((0, 0.86, -0.21), (1.00, 0.18, 0.02), "Lib_PaintBlue", bevel=bev, segs=1)
        g.box((0, 0.62, -0.21), (0.04, 0.55, 0.025), "Lib_SteelDark")
        # Slot hoods. The opening is the gap under the hood, not a texture.
        for x, color in ((-0.26, "Lib_PaintBlue"), (0.26, "Lib_PaintGreen")):
            g.box((x, 0.70, -0.25), (0.40, 0.035, 0.08), "Lib_SteelDark", bevel=bev, segs=1)
            g.box((x, 1.015, 0.0), (0.50, 0.03, 0.44), color, bevel=bev, segs=bs)
            g.cylinder((x, 1.00, 0.20), 0.012, 0.48, "Lib_Steel", 6, axis="X")
            g.box((x, 1.04, -0.16), (0.10, 0.02, 0.06), "Lib_Steel")
        if lod == 0:
            g.cylinder((-0.26, 0.78, -0.23), 0.06, 0.008, "Lib_PaintWhite", 12, axis="Z")
            g.cylinder((0.18, 0.76, -0.24), 0.018, 0.05, "Lib_PaintWhite", 8, axis="Z")
            g.cylinder((0.26, 0.80, -0.24), 0.018, 0.06, "Lib_PaintWhite", 8, axis="Z")
            g.cylinder((0.34, 0.76, -0.24), 0.018, 0.045, "Lib_PaintWhite", 8, axis="Z")
        a.end()
    for i, x in enumerate((-0.52, 0.52)):
        for j, z in enumerate((-0.21, 0.21)):
            a.box("Col_Post_%d%d" % (i, j), (x, 0.50, z), (0.038, 0.94, 0.038))
            a.box("Col_Wheel_%d%d" % (i, j), (x, 0.04, z), (0.032, 0.050, 0.050))
    a.box("Col_Floor", (0, 0.12, 0), (0.96, 0.024, 0.34))
    a.box("Col_Back", (0, 0.56, 0.20), (0.96, 0.78, 0.016))
    a.box("Col_SideL", (-0.50, 0.56, 0.0), (0.016, 0.78, 0.34))
    a.box("Col_SideR", (0.50, 0.56, 0.0), (0.016, 0.78, 0.34))
    a.box("Col_Kick", (0, 0.26, -0.21), (0.96, 0.18, 0.016))
    a.box("Col_Rail", (0, 0.52, -0.21), (0.96, 0.06, 0.016))
    a.box("Col_Head", (0, 0.86, -0.21), (0.96, 0.14, 0.016))
    a.box("Col_Stile", (0, 0.62, -0.21), (0.032, 0.50, 0.018))
    a.box("Col_LidL", (-0.26, 1.015, 0.0), (0.46, 0.024, 0.40))
    a.box("Col_LidR", (0.26, 1.015, 0.0), (0.46, 0.024, 0.40))
    a.box("Col_HoodL", (-0.26, 0.70, -0.25), (0.36, 0.028, 0.06))
    a.box("Col_HoodR", (0.26, 0.70, -0.25), (0.36, 0.028, 0.06))
    return a
