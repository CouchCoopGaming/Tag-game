"""Channel buoy standing on its base. 1.35 m tall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Buoy",
        "Harbor",
        "Nun buoy, 1.35 m tall, 0.72 m across the float. Red over white. Pivot is the base so it can sit on a dock.",
    )
    a.climb_note = "Round. Not a cling."
    a.vault_note = "No rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        g.cone((0, 0.16, 0), 0.10, 0.22, 0.32, "Lib_SteelDark", seg)
        g.sphere((0, 0.52, 0), 0.36, "Lib_PaintRed", seg)
        g.cylinder((0, 0.34, 0), 0.34, 0.10, "Lib_PaintWhite", seg)
        g.cylinder((0, 0.70, 0), 0.34, 0.08, "Lib_PaintWhite", seg)
        g.cone((0, 1.05, 0), 0.16, 0.05, 0.42, "Lib_PaintRed", seg)
        g.cylinder((0, 1.28, 0), 0.045, 0.08, "Lib_Steel", seg)
        if lod == 0:
            g.sphere((0, 1.34, 0), 0.035, "Lib_PaintYellow", 8)
            g.torus((0, 0.52, 0), 0.34, 0.012, "Lib_SteelDark", 16, 6)
            g.torus((0, 0.06, 0), 0.06, 0.008, "Lib_Rust", 8, 4)
        a.end()
    a.sphere("Col_Float", (0, 0.52, 0), 0.34)
    a.box("Col_Base", (0, 0.10, 0), (0.08, 0.14, 0.08))
    a.capsule("Col_Top", (0, 1.08, 0), 0.07, 0.48, 1)
    return a
