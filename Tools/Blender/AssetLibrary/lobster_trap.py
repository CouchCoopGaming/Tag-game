"""Wood lobster trap. Slatted box, funnel end, rope on top."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "LobsterTrap",
        "Harbor",
        "Lobster trap, 0.90 x 0.48 m, 0.38 m tall. Wood slats, a funnel at +Z, and a rope bridle.",
    )
    a.climb_note = "Too small to cling."
    a.vault_note = "Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        bev = 0.002 if lod == 0 else 0
        g.box((0, 0.025, 0), (0.86, 0.04, 0.44), "Lib_WoodDark", bevel=bev, segs=1 if lod == 0 else 0, uv_scale=1.2)
        for x, z in ((-0.40, -0.20), (-0.40, 0.20), (0.40, -0.20), (0.40, 0.20)):
            g.box((x, 0.195, z), (0.035, 0.28, 0.035), "Lib_Wood", uv_scale=1.0)
        slats = lod_pick(lod, 4, 3)
        for i in range(slats):
            y = 0.09 + i * 0.06
            g.box((0, y, -0.24), (0.74, 0.022, 0.016), "Lib_Wood", uv_scale=1.2)
            g.box((0, y, 0.24), (0.74, 0.022, 0.016), "Lib_Wood", uv_scale=1.2)
            g.box((-0.45, y, 0), (0.016, 0.022, 0.32), "Lib_Wood", uv_scale=1.2)
            g.box((0.45, y, 0), (0.016, 0.022, 0.32), "Lib_Wood", uv_scale=1.2)
        g.box((0, 0.355, 0), (0.74, 0.018, 0.36), "Lib_WoodDark", uv_scale=1.1)
        if lod == 0:
            g.cone((0, 0.16, 0.0), 0.09, 0.04, 0.18, "Lib_SteelDark", seg, axis="Z")
            g.torus((0, 0.39, 0), 0.14, 0.008, "Lib_Rust", 12, 5)
        a.end()
    a.box("Col_Base", (0, 0.025, 0), (0.78, 0.03, 0.38))
    for i, (x, z) in enumerate(((-0.40, -0.20), (-0.40, 0.20), (0.40, -0.20), (0.40, 0.20))):
        a.box("Col_Post_%d" % i, (x, 0.19, z), (0.025, 0.20, 0.025))
    return a
