"""Corner traffic signal. Pole, mast arm, and a three-lens head."""

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
        "TrafficSignal_Mast",
        "StreetFurniture",
        "Corner signal. Pole 4.70 m, mast arm 4.6 m, three-lens head and a WALK ped head.",
    )
    a.climb_note = "14 cm pole. Not a cling."
    a.vault_note = "Mast arm is 4.2 m up."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        segs = 1 if lod == 0 else 0
        g.cylinder((0, 0.025, 0), 0.18, 0.05, "Lib_SteelDark", seg, bevel=bev, segs=segs)
        g.cylinder((0, 2.35, 0), 0.070, 4.60, "Lib_Steel", seg)
        g.cylinder((0, 4.68, 0), 0.085, 0.06, "Lib_SteelDark", seg)
        # Arm starts inside the pole and runs out to the hanger.
        g.cylinder((2.30, 4.22, 0), 0.055, 4.50, "Lib_Steel", seg, axis="X")
        g.cylinder((0, 4.22, 0), 0.095, 0.16, "Lib_SteelDark", seg)
        g.cylinder((4.50, 4.02, 0), 0.028, 0.36, "Lib_Steel", 8)
        # Vehicle head faces +Z.
        g.box((4.50, 3.42, -0.02), (0.36, 1.02, 0.018), "Lib_Black")
        g.box((4.50, 3.42, 0.08), (0.26, 0.86, 0.16), "Lib_Black", bevel=bev, segs=segs)
        lenses = (
            (3.66, "Lib_Taillamp"),
            (3.42, "Lib_SignalAmber"),
            (3.18, "Lib_SignalGreen"),
        )
        for y, mat in lenses:
            g.cylinder((4.50, y, 0.175), 0.078, 0.028, mat, seg, axis="Z")
            if lod == 0:
                g.box((4.50, y + 0.09, 0.20), (0.20, 0.016, 0.10), "Lib_Black", euler=(38, 0, 0))
        # Pedestrian head on the pole, with OFL WALK.
        g.box((0.11, 2.28, 0.0), (0.08, 0.50, 0.30), "Lib_Black", bevel=bev, segs=segs)
        g.cylinder((0.155, 2.40, 0.0), 0.07, 0.02, "Lib_SignalGreen", seg, axis="X")
        if lod == 0:
            g.text("WALK", (0.162, 2.14, 0.0), 0.11, "Lib_PaintWhite", extrude=0.004, yaw=90.0, font=_FONT)
            for ang_x, ang_z in ((0.12, 0.12), (0.12, -0.12), (-0.12, 0.12), (-0.12, -0.12)):
                g.cylinder((ang_x, 0.028, ang_z), 0.012, 0.012, "Lib_Steel", 6)
            g.box((0.0, 1.15, 0.078), (0.10, 0.16, 0.012), "Lib_SteelDark", bevel=0.002, segs=1)
        a.end()
    a.box("Col_Base", (0, 0.026, 0), (0.16, 0.020, 0.16))
    a.capsule("Col_Pole", (0, 2.05, 0), 0.050, 3.70, 1)
    a.box("Col_Arm", (2.20, 4.22, 0), (3.80, 0.07, 0.07))
    a.box("Col_Head", (4.50, 3.42, 0.08), (0.18, 0.70, 0.10))
    a.box("Col_Ped", (0.125, 2.28, 0.0), (0.04, 0.36, 0.20))
    return a
