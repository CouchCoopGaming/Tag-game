"""Bus-stop flag on its own pole. Not the shelter.

Real size: 8 ft pole (2.44 m), 64 mm OD. Blade is 18 x 12 in (0.457 x 0.305 m)
clamped to the pole. BUS is Liberation Sans (OFL).
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
        "BusFlag_Stop",
        "StreetFurniture",
        "Bus flag. Pole 2.44 m, 64 mm OD. Blade 0.457 x 0.305 m. BUS in Liberation Sans.",
    )
    a.climb_note = "Pole is 64 mm. Not a cling."
    a.vault_note = "Blade center is 2.20 m. Too high to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.015, 0), (0.22, 0.03, 0.22), "Lib_SteelDark", bevel=bev, segs=bs)
        if lod == 0:
            for x in (-0.07, 0.07):
                for z in (-0.07, 0.07):
                    g.cylinder((x, 0.034, z), 0.009, 0.014, "Lib_Steel", 6)
            g.box((0, 0.008, 0), (0.16, 0.01, 0.16), "Lib_Rust")
        g.cylinder((0, 1.22, 0), 0.032, 2.40, "Lib_Steel", seg, bevel=bev, segs=bs)
        g.cylinder((0, 2.44, 0), 0.036, 0.03, "Lib_SteelDark", seg)
        # Clamp saddle overlaps the pole and the blade spine.
        g.cylinder((0.04, 2.20, 0), 0.040, 0.08, "Lib_SteelDark", seg, axis="X")
        g.box((0.07, 2.20, 0), (0.04, 0.10, 0.06), "Lib_Steel", bevel=bev, segs=bs)
        # White border plate, yellow field set onto it, so the border is a real edge.
        g.box((0.30, 2.20, 0), (0.457, 0.305, 0.016), "Lib_PaintWhite", bevel=bev, segs=bs)
        g.box((0.30, 2.20, 0.006), (0.400, 0.248, 0.012), "Lib_PaintYellow", bevel=bev, segs=bs)
        if lod == 0:
            g.box((0.30, 2.30, 0.014), (0.34, 0.012, 0.004), "Lib_PaintWhite")
            g.box((0.30, 2.10, 0.014), (0.34, 0.012, 0.004), "Lib_PaintWhite")
            g.text("BUS", (0.30, 2.20, 0.016), 0.11, "Lib_Black", extrude=0.004, font=_FONT)
            g.cylinder((0.055, 2.25, 0.03), 0.006, 0.03, "Lib_Steel", 6, axis="Z")
            g.cylinder((0.055, 2.15, -0.03), 0.006, 0.03, "Lib_Steel", 6, axis="Z")
        a.end()
    a.box("Col_Base", (0, 0.015, 0), (0.16, 0.02, 0.16))
    a.capsule("Col_Pole", (0, 1.05, 0), 0.024, 1.90, 1)
    # Back half of the white plate, clear of the yellow field layered on +Z.
    a.box("Col_Blade", (0.30, 2.20, -0.004), (0.34, 0.20, 0.005))
    return a
