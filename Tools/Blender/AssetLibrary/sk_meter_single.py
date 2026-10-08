"""Single-space digital parking meter. Generic housing, no brand."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

_FONT = os.path.normpath(os.path.join(
    os.path.dirname(os.path.abspath(__file__)),
    "..", "..", "StrafeJumpSim", "Fonts", "Overpass-Bold.ttf",
))


@register
def create():
    a = Asset(
        "ParkingMeter_Single",
        "StreetFurniture",
        "Digital single-space meter. Screen center 1.24 m, head 0.16 m wide. No brand.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        # Flange, post sleeved into the head, black housing.
        g.box((0.0, 0.012, 0.0), (0.18, 0.024, 0.18), "Lib_SteelDark")
        g.cylinder((0.0, 0.56, 0.0), 0.030, 1.08, "Lib_Steel", seg)
        g.box((0.0, 1.18, 0.0), (0.16, 0.32, 0.11), "Lib_Black")
        # Screen, visor, card slot, and one button on the street face.
        g.box((0.0, 1.245, 0.060), (0.100, 0.062, 0.010), "Lib_ShopGlass")
        g.box((0.0, 1.286, 0.068), (0.112, 0.014, 0.030), "Lib_Black")
        g.box((0.0, 1.145, 0.060), (0.072, 0.010, 0.012), "Lib_SteelDark")
        g.cylinder((0.045, 1.075, 0.062), 0.012, 0.014, "Lib_SignalGreen", 8, axis="Z")
        if lod == 0:
            for i in range(4):
                ang = (i + 0.5) * 1.5708
                g.cylinder(
                    (math.sin(ang) * 0.062, 0.026, math.cos(ang) * 0.062),
                    0.006, 0.010, "Lib_Steel", 6,
                )
            g.box((0.0, 1.348, 0.0), (0.120, 0.016, 0.070), "Lib_ShopGlass")
            g.text("2:00", (0.0, 1.245, 0.068), 0.032, "Lib_SignalGreen", extrude=0.001, font=_FONT)
        a.end()
    a.box("Col_Base", (0.0, 0.012, 0.0), (0.10, 0.016, 0.10))
    a.capsule("Col_Post", (0.0, 0.50, 0.0), 0.020, 0.84, 1)
    a.box("Col_Head", (0.0, 1.18, 0.0), (0.10, 0.20, 0.06))
    return a
