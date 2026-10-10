"""Door awning on two posts. 3.0 m wide, 1.15 m of projection.

Three overlapping boards step down toward the street so the slope has a
collider in each board. Fabric is Lib_Awning. Valance hangs off the front board.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Awning_Door",
        "StreetFurniture",
        "Post awning 3.00 m wide, projects 1.15 m. Rear board at 2.50 m, front board at 2.30 m, valance to 2.05 m.",
    )
    a.climb_note = "Fabric over boards. Not a cling wall."
    a.vault_note = "Front lip is 2.30 m. Too high to vault from the street."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.003, 0.0)
        seg = lod_pick(lod, 8, 6)
        for x in (-1.35, 1.35):
            g.cylinder((x, 1.25, 0.0), 0.032, 2.50, "Lib_SteelDark", seg)
            g.cylinder((x, 0.04, 0.0), 0.07, 0.08, "Lib_Steel", 8)
        g.box((0, 2.50, 0.0), (2.90, 0.08, 0.08), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        # Each board overlaps the next in both plan and height. Posts sit at z=0.
        g.box((0, 2.46, 0.22), (2.84, 0.08, 0.50), "Lib_Awning", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 2.40, 0.57), (2.84, 0.08, 0.50), "Lib_Awning")
        g.box((0, 2.34, 0.92), (2.84, 0.08, 0.50), "Lib_Awning")
        g.box((0, 2.20, 1.16), (2.84, 0.28, 0.04), "Lib_Awning")
        if lod == 0:
            for x in (-1.35, 1.35):
                g.pipe((x, 2.44, 0.34), (x, 2.32, 1.00), 0.014, "Lib_Steel", 6)
            g.box((0, 2.10, 1.172), (2.2, 0.012, 0.012), "Lib_PaintCream")
        a.end()
    a.capsule("Col_PostL", (-1.35, 1.10, 0.0), 0.018, 1.80)
    a.capsule("Col_PostR", (1.35, 1.10, 0.0), 0.018, 1.80)
    a.box("Col_Rear", (0, 2.484, 0.18), (2.20, 0.020, 0.16))
    a.box("Col_Mid", (0, 2.424, 0.57), (2.20, 0.020, 0.12))
    a.box("Col_Front", (0, 2.364, 0.98), (2.20, 0.020, 0.16))
    return a
