"""Turbine roof vent on a curb. About 0.55 m across and 0.70 m tall."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "RoofVent_Turbine",
        "StreetFurniture",
        "Turbine vent, 0.46 m curb, throat 0.32 m across, cap at 0.70 m. Vanes are visual only.",
    )
    a.climb_note = "Curb is under 0.2 m. Not a wall."
    a.vault_note = "Too small to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 14, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        # Square curb, open in the middle.
        g.box((0, 0.08, -0.20), (0.46, 0.16, 0.06), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 0.08, 0.20), (0.46, 0.16, 0.06), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((-0.20, 0.08, 0), (0.06, 0.16, 0.34), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0.20, 0.08, 0), (0.06, 0.16, 0.34), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.26, 0), 0.13, 0.16, "Lib_SteelDark", seg)
        g.cylinder((0, 0.38, 0), 0.16, 0.04, "Lib_Steel", seg)
        g.cylinder((0, 0.52, 0), 0.15, 0.22, "Lib_Steel", seg, bevel=bev, segs=1 if lod == 0 else 0)
        vanes = 10 if lod == 0 else 6
        for i in range(vanes):
            ang = math.radians(i * (360.0 / vanes))
            yaw = math.degrees(ang)
            x = math.sin(ang) * 0.168
            z = math.cos(ang) * 0.168
            g.box((x, 0.52, z), (0.035, 0.18, 0.012), "Lib_SteelDark", euler=(8, yaw, 0))
        g.cone((0, 0.66, 0), 0.18, 0.04, 0.08, "Lib_SteelDark", seg)
        if lod == 0:
            g.cylinder((0, 0.71, 0), 0.02, 0.03, "Lib_Steel", 8)
        a.end()
    for name, center, size in (
        ("Col_CurbN", (0, 0.08, 0.20), (0.40, 0.12, 0.04)),
        ("Col_CurbS", (0, 0.08, -0.20), (0.40, 0.12, 0.04)),
        ("Col_CurbW", (-0.20, 0.08, 0), (0.04, 0.12, 0.28)),
        ("Col_CurbE", (0.20, 0.08, 0), (0.04, 0.12, 0.28)),
    ):
        a.box(name, center, size)
    a.box("Col_Throat", (0, 0.26, 0), (0.16, 0.10, 0.16))
    a.capsule("Col_Head", (0, 0.52, 0), 0.12, 0.20, 1)
    a.capsule("Col_Cap", (0, 0.66, 0), 0.06, 0.08, 1)
    return a
