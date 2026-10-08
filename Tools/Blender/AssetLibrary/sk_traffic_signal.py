"""Corner mast arm with one three-lamp vehicle head. No legend and no ped head.

The arm is 5.5 m. The bottom of the head is 4.60 m, about 15 ft over the road.
Lenses are 12 inch. Visors are plates.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

HEAD_X = 5.35
HEAD_Y = 5.17


def _head(g, lod):
    seg = lod_pick(lod, 16, 10)
    bev = lod_pick(lod, 0.003, 0.0)
    bs = 1 if lod == 0 else 0
    # Backplate, then the housing, then the three 12 inch lamps with visors.
    g.box((HEAD_X, HEAD_Y, -0.04), (0.50, 1.28, 0.025), "Lib_Black")
    g.box((HEAD_X, HEAD_Y, 0.08), (0.38, 1.14, 0.20), "Lib_Black", bevel=bev, segs=bs)
    lamps = (
        (HEAD_Y + 0.36, "Lib_Taillamp"),
        (HEAD_Y, "Lib_SignalAmber"),
        (HEAD_Y - 0.36, "Lib_SignalGreen"),
    )
    for y, mat in lamps:
        g.cylinder((HEAD_X, y, 0.195), 0.150, 0.030, mat, seg, axis="Z")
        g.box((HEAD_X, y + 0.11, 0.24), (0.32, 0.03, 0.16), "Lib_Black", euler=(28, 0, 0))
        if lod == 0:
            g.cylinder((HEAD_X, y, 0.212), 0.118, 0.012, mat, seg, axis="Z")


@register
def create():
    a = Asset(
        "TrafficSignal_Mast",
        "StreetFurniture",
        "Mast arm. Pole to 6.30 m, arm 5.5 m, three 12 inch lamps. Head bottom at 4.60 m. No legend.",
    )
    a.climb_note = "200 mm pole. Not a cling."
    a.vault_note = "The arm is 5.85 m up."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        bs = 1 if lod == 0 else 0
        g.box((0, 0.02, 0), (0.46, 0.04, 0.46), "Lib_SteelDark", bevel=bev, segs=bs)
        if lod == 0:
            for x in (-0.16, 0.16):
                for z in (-0.16, 0.16):
                    g.cylinder((x, 0.045, z), 0.016, 0.016, "Lib_Steel", 6)
        g.cylinder((0, 3.16, 0), 0.10, 6.20, "Lib_Steel", seg)
        g.cylinder((0, 6.28, 0), 0.11, 0.06, "Lib_SteelDark", seg)
        g.box((0.12, 1.15, 0), (0.02, 0.22, 0.12), "Lib_SteelDark", bevel=bev, segs=bs)
        # Arm starts inside the pole. A gusset sits under the root.
        g.cylinder((2.70, 5.85, 0), 0.055, 5.30, "Lib_Steel", seg, axis="X")
        g.box((0.28, 5.55, 0), (0.36, 0.50, 0.08), "Lib_SteelDark")
        g.cylinder((0, 5.85, 0), 0.13, 0.18, "Lib_SteelDark", seg)
        g.cylinder((HEAD_X - 0.05, 5.45, 0), 0.028, 0.70, "Lib_Steel", 8)
        _head(g, lod)
        a.end()
    a.box("Col_Base", (0, 0.016, 0), (0.32, 0.024, 0.32))
    # Stops below the arm gusset so the sample stays in the pole alone.
    a.capsule("Col_Pole", (0, 2.60, 0), 0.07, 5.00, direction=1)
    a.box("Col_Arm", (2.70, 5.85, 0), (4.80, 0.07, 0.07))
    a.box("Col_Head", (HEAD_X, HEAD_Y, 0.06), (0.28, 0.90, 0.12))
    return a
