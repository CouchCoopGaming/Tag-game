"""Twin-head digital parking meter. Same head as the single, no brand."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_meter_single import _FONT, meter_foot, meter_head


@register
def create():
    a = Asset(
        "ParkingMeter_Twin",
        "StreetFurniture",
        "Twin digital meter. Plate on the ground, two heads at 1.21 m, 0.36 m across. No brand.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        meter_foot(g, lod, 0.36, 0.20)
        g.cylinder((0.0, 0.545, 0.0), 0.032, 1.07, "Lib_Steel", seg)
        g.cylinder((0.0, 0.034, 0.0), 0.048, 0.016, "Lib_SteelDark", seg)
        g.box((0.0, 1.04, 0.0), (0.30, 0.04, 0.08), "Lib_SteelDark")
        for x in (-0.11, 0.11):
            meter_head(g, lod, x, _FONT)
        a.end()
    a.box("Col_Base", (0.0, 0.014, 0.0), (0.22, 0.018, 0.12))
    a.capsule("Col_Post", (0.0, 0.48, 0.0), 0.022, 0.76, 1)
    # Above the yoke so a sample is not inside two solids.
    a.box("Col_Head_0", (-0.11, 1.125, 0.0), (0.06, 0.06, 0.04))
    a.box("Col_Head_1", (0.11, 1.125, 0.0), (0.06, 0.06, 0.04))
    return a
