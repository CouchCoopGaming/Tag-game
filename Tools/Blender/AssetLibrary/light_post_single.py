"""Residential cobra street light. 5.45 m to the lamp head."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _head(g, lod, z_sign, bev, bs, seg):
    z = 1.15 * z_sign
    g.pipe((0, 5.22, 0.06 * z_sign), (0, 5.28, z), 0.035, "Lib_SteelDark", max(6, seg // 2), bevel=bev, segs=bs)
    g.box((0, 5.36, z), (0.58, 0.14, 0.30), "Lib_SteelDark", bevel=max(bev, 0.006), segs=max(bs, 1))
    g.box((0, 5.275, z), (0.40, 0.012, 0.16), "Lib_Lamp")
    if lod == 0:
        for i in range(3):
            g.box((0, 5.268, z - 0.05 + i * 0.05), (0.36, 0.006, 0.008), "Lib_SteelDark")
        g.cylinder((0, 5.455, z), 0.022, 0.028, "Lib_Black", 6)
    g.box((0, 5.44, z + 0.02 * z_sign), (0.62, 0.02, 0.34), "Lib_Steel")


@register
def create():
    a = Asset(
        "LightPost_Single",
        "StreetFurniture",
        "Residential cobra head on a tapered steel pole with a 36 cm cast base. Lamp at 5.45 m.",
    )
    a.climb_note = "Round tapered pole, 11 cm at the base. Not a cling wall."
    a.vault_note = "No vault edge. The arm is overhead."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 20, 10)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = lod_pick(lod, 1, 0)
        # Cast base, 36 cm tall, wider at the foot.
        g.box((0, 0.03, 0), (0.42, 0.06, 0.42), "Lib_SteelDark", bevel=0.006 if lod == 0 else 0, segs=1)
        if lod == 0:
            for x in (-0.15, 0.15):
                for z in (-0.15, 0.15):
                    g.cylinder((x, 0.065, z), 0.012, 0.016, "Lib_Steel", 6)
        g.cone((0, 0.22, 0), 0.16, 0.09, 0.32, "Lib_Steel", seg)
        g.cone((0, 2.78, 0), 0.085, 0.048, 4.90, "Lib_Steel", seg)
        g.cylinder((0, 4.55, 0), 0.058, 0.04, "Lib_SteelDark", seg)
        g.cylinder((0, 5.20, 0), 0.055, 0.08, "Lib_SteelDark", seg)
        if lod == 0:
            g.box((0.09, 0.55, 0), (0.01, 0.18, 0.10), "Lib_SteelDark", bevel=0.002, segs=1)
        _head(g, lod, 1, bev, bs, seg)
        a.end()
    a.box("Col_Base", (0, 0.16, 0), (0.12, 0.16, 0.12))
    a.capsule("Col_Pole", (0, 2.85, 0), 0.042, 4.50, 1)
    a.box("Col_Arm", (0, 5.25, 0.60), (0.05, 0.05, 0.95), euler=(-3.2, 0, 0))
    a.box("Col_Head", (0, 5.36, 1.15), (0.58, 0.18, 0.34))
    return a
