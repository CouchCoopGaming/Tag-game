"""Two street blades crossing on one pole. Green with a white border, no lettering."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Sign_Blades",
        "StreetFurniture",
        "Crossed street blades at 2.70 m and 2.95 m. Each blade 0.90 x 0.22 m. Steel pole ends in a green collar.",
    )
    a.climb_note = "6 cm pole. Not a cling."
    a.vault_note = "Blades are thin plates."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.cylinder((0, 0.025, 0), 0.09, 0.05, "Lib_SteelDark", seg, bevel=bev, segs=1)
        # Steel stops inside the collar. The finial above the collar is green.
        g.cylinder((0, 1.47, 0), 0.032, 2.84, "Lib_Steel", seg)
        # Lower blade faces +Z, upper blade faces +X so both read from the corner.
        g.box((0, 2.70, 0.04), (0.90, 0.22, 0.025), "Lib_PaintGreen", bevel=bev, segs=1)
        g.box((0, 2.70, 0.056), (0.78, 0.12, 0.006), "Lib_PaintWhite")
        # Two blades stop short of the pole so the pole can pass between them.
        # Collar covers the gap where the two blades meet the pole.
        # Wider collar, and the green blades sink into it so the pole joint closes.
        g.cylinder((0, 2.96, 0), 0.095, 0.34, "Lib_PaintGreen", seg)
        g.cylinder((0, 3.15, 0), 0.055, 0.06, "Lib_PaintGreen", seg)
        for z in (-0.24, 0.24):
            for sign in (1.0, -1.0):
                g.box((sign * 0.046, 2.96, z), (0.036, 0.22, 0.52), "Lib_PaintGreen", bevel=bev, segs=1)
                g.box((sign * 0.066, 2.96, z), (0.006, 0.12, 0.36), "Lib_PaintWhite")
        a.end()
    a.box("Col_Base", (0, 0.025, 0), (0.14, 0.04, 0.14))
    # Stays in the bare shaft, below the blades, so it does not share the collar.
    a.capsule("Col_Pole", (0, 1.30, 0), 0.022, 2.20, 1)
    a.box("Col_BladeZ_0", (-0.26, 2.70, 0.04), (0.32, 0.16, 0.016))
    a.box("Col_BladeZ_1", (0.26, 2.70, 0.04), (0.32, 0.16, 0.016))
    a.box("Col_BladeX_0", (0.055, 2.96, -0.28), (0.012, 0.16, 0.36))
    a.box("Col_BladeX_1", (0.055, 2.96, 0.28), (0.012, 0.16, 0.36))
    return a
