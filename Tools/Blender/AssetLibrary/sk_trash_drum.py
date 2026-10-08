"""32-gallon steel drum can. Ribs, lid, and side handles. 0.84 m to the lid knob."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import polyline


@register
def create():
    a = Asset(
        "TrashCan_Drum",
        "StreetFurniture",
        "Steel drum can, 0.48 m across, 0.80 m to the lid, knob at 0.84 m. Three ribs and two handles.",
    )
    a.climb_note = "Round can under 0.9 m. Not a cling wall."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 20, 10)
        bev = lod_pick(lod, 0.003, 0.0)
        g.cylinder((0, 0.028, 0), 0.25, 0.056, "Lib_SteelDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.385, 0), 0.215, 0.69, "Lib_MetalWorn", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.74, 0), 0.235, 0.045, "Lib_SteelDark", seg)
        for y in (0.24, 0.42, 0.60):
            g.torus((0, y, 0), 0.224, 0.006, "Lib_Steel", seg, 5)
        g.cylinder((0, 0.785, 0), 0.245, 0.03, "Lib_SteelDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.805, 0), 0.04, 0.02, "Lib_Black", 8)
        g.sphere((0, 0.825, 0), 0.018, "Lib_Black", 8)
        if lod == 0:
            for sign in (-1, 1):
                polyline(g, [
                    (sign * 0.21, 0.58, 0),
                    (sign * 0.28, 0.58, 0),
                    (sign * 0.28, 0.70, 0),
                    (sign * 0.21, 0.70, 0),
                ], 0.01, "Lib_Steel", 6)
            for i in range(4):
                ang_x = 0.16 * (1 if i < 2 else -1)
                ang_z = 0.16 * (1 if i % 2 == 0 else -1)
                g.cylinder((ang_x, 0.80, ang_z), 0.008, 0.012, "Lib_Steel", 6)
        a.end()
    a.box("Col_Skirt", (0, 0.028, 0), (0.30, 0.040, 0.30))
    a.capsule("Col_Body", (0, 0.385, 0), 0.19, 0.60, 1)
    a.box("Col_Rim", (0, 0.74, 0), (0.32, 0.036, 0.32))
    a.box("Col_Lid", (0, 0.785, 0), (0.32, 0.024, 0.32))
    return a
