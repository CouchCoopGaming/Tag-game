"""90 cm offset dish on a 1.20 m mast."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import polyline


@register
def create():
    a = Asset(
        "SatelliteDish",
        "StreetFurniture",
        "Offset dish, 0.90 m across, mast 1.20 m. The dish faces +Z.",
    )
    a.climb_note = "3 cm mast. Not a cling."
    a.vault_note = "Dish is a thin shell."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.02, 0), (0.28, 0.04, 0.28), "Lib_SteelDark", bevel=bev, segs=1)
        g.cylinder((0, 0.62, 0), 0.028, 1.16, "Lib_Steel", seg)
        g.cylinder((0, 1.18, 0), 0.05, 0.06, "Lib_SteelDark", seg)
        # Shallow cone, tilted so the face looks up and forward.
        g.cone((0, 1.35, 0.28), 0.45, 0.12, 0.10, "Lib_PaintWhite", seg, axis="Z")
        g.cylinder((0, 1.35, 0.24), 0.46, 0.015, "Lib_SteelDark", seg, axis="Z")
        if lod == 0:
            polyline(g, [(0.0, 1.35, 0.22), (0.0, 1.42, 0.55), (0.0, 1.38, 0.62)], 0.008, "Lib_Steel", 5)
            g.box((0, 1.36, 0.66), (0.06, 0.04, 0.08), "Lib_Black", bevel=bev, segs=1)
            g.cylinder((0, 1.22, 0.18), 0.02, 0.16, "Lib_Steel", 6, axis="Z")
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.24, 0.03, 0.24))
    a.capsule("Col_Mast", (0, 0.62, 0), 0.022, 1.08, 1)
    # Inscribed in the dish cone. The shell is thin, so this box is the solid bite.
    a.box("Col_Dish", (0, 1.35, 0.24), (0.36, 0.36, 0.03))
    return a
