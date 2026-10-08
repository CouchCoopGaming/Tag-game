"""Removable bollard. Ground socket and a locking collar.

114 mm post, dome at 0.90 m. The collar carries a hasp. No legend.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import sweep_tube


@register
def create():
    a = Asset(
        "Bollard_Removable",
        "StreetFurniture",
        "Removable bollard, 114 mm post, dome at 0.90 m, socket and lock collar.",
    )
    a.climb_note = "114 mm post. Not a cling."
    a.vault_note = "Top is 0.90 m and round. Not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        g.cylinder((0, 0.010, 0), 0.11, 0.020, "Lib_SteelDark", seg)
        # Post is split around the white band. Dome sits 2 mm above the cut.
        g.cylinder((0, 0.310, 0), 0.057, 0.576, "Lib_PaintYellow", seg)
        g.cylinder((0, 0.618, 0), 0.062, 0.036, "Lib_PaintWhite", seg)
        g.cylinder((0, 0.710, 0), 0.057, 0.144, "Lib_PaintYellow", seg)
        g.sphere((0, 0.843, 0), 0.057, "Lib_PaintYellow", seg)
        # Collar clears the post. The hasp and shackle sit outside it.
        g.torus((0, 0.09, 0), 0.066, 0.007, "Lib_Steel", lod_pick(lod, 14, 8), 5)
        g.box((0, 0.09, 0.086), (0.022, 0.032, 0.016), "Lib_SteelDark")
        if lod == 0:
            sweep_tube(
                g,
                (
                    (0.012, 0.108, 0.086),
                    (0.012, 0.122, 0.086),
                    (0.0, 0.132, 0.086),
                    (-0.012, 0.122, 0.086),
                    (-0.012, 0.108, 0.086),
                ),
                0.0035,
                "Lib_Steel",
                segments=8,
            )
        a.end()
    a.box("Col_Socket", (0, 0.010, 0), (0.16, 0.014, 0.16))
    a.capsule("Col_Post", (0, 0.42, 0), 0.050, 0.70, 1)
    return a
