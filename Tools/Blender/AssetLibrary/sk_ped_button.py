"""Pedestrian pushbutton. Button center at 1.07 m.

The housing bites the pole. An arrow plate hangs off one side.
A speaker housing with slots sits under the button. No brand name.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "PedButton_Post",
        "StreetFurniture",
        "Pushbutton pole. Button center at 1.07 m. Arrow plate and speaker housing. Pole 90 mm.",
    )
    a.climb_note = "90 mm pole. Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        bev = lod_pick(lod, 0.002, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.020, 0), (0.20, 0.040, 0.20), "Lib_SteelDark", bevel=bev, segs=bs)
        g.cylinder((0, 0.68, 0), 0.045, 1.28, "Lib_Steel", seg)
        g.sphere((0, 1.34, 0), 0.050, "Lib_Steel", seg)
        g.box((0, 1.02, 0.070), (0.13, 0.30, 0.060), "Lib_Black", bevel=bev, segs=bs)
        # Bezel bites the housing. The button stands proud of the bezel.
        g.cylinder((0, 1.07, 0.104), 0.036, 0.014, "Lib_PaintYellow", 12, axis="Z")
        g.cylinder((0, 1.07, 0.114), 0.026, 0.014, "Lib_Black", 12, axis="Z")
        # Neck carries the arrow plate out past the housing.
        g.box((0.078, 1.08, 0.078), (0.050, 0.10, 0.040), "Lib_Black")
        g.box((0.118, 1.08, 0.086), (0.062, 0.140, 0.016), "Lib_PaintYellow", bevel=bev, segs=bs)
        if lod == 0:
            g.box((0.112, 1.08, 0.096), (0.036, 0.010, 0.008), "Lib_Black")
            g.box((0.128, 1.094, 0.096), (0.022, 0.008, 0.008), "Lib_Black", euler=(0, 0, 38))
            g.box((0.128, 1.066, 0.096), (0.022, 0.008, 0.008), "Lib_Black", euler=(0, 0, -38))
        g.box((0, 0.90, 0.092), (0.080, 0.060, 0.032), "Lib_SteelDark", bevel=bev, segs=bs)
        for y in (0.918, 0.902, 0.886):
            g.box((0, y, 0.110), (0.050, 0.006, 0.008), "Lib_Black")
        a.end()
    a.box("Col_Base", (0, 0.012, 0), (0.12, 0.016, 0.12))
    a.capsule("Col_Pole", (0, 0.42, 0), 0.028, 0.70)
    # Housing core, clear of the pole, the button, the speaker, and the neck.
    a.box("Col_Head", (0, 1.08, 0.062), (0.05, 0.10, 0.016))
    return a
