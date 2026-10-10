"""Public bike pump. A head and a hose on a post, not the locker."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "BikePump_Public",
        "StreetFurniture",
        "Bike pump. Post to 1.05 m, head and gauge, hose down to a holder.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.015, 0), (0.22, 0.03, 0.22), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.54, 0), 0.032, 1.02, "Lib_Steel", seg)
        g.box((0, 1.12, 0), (0.10, 0.16, 0.08), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 1.22, 0.02), (0.16, 0.02, 0.04), "Lib_Black")
        g.cylinder((0, 1.08, 0.05), 0.028, 0.012, "Lib_PaintCream", 10, axis="Z")
        # Hose leaves the head and lands in a holder on the post.
        g.cylinder((0.06, 0.95, 0.02), 0.008, 0.22, "Lib_Rubber", 8)
        g.box((0.06, 0.82, 0.02), (0.02, 0.06, 0.02), "Lib_SteelDark")
        if lod == 0:
            g.cylinder((0, 1.08, 0.058), 0.006, 0.006, "Lib_Black", 8, axis="Z")
        a.end()
    a.box("Col_Base", (0, 0.015, 0), (0.14, 0.02, 0.14))
    a.capsule("Col_Post", (0, 0.52, 0), 0.024, 0.90, 1)
    a.box("Col_Head", (0, 1.12, 0), (0.07, 0.10, 0.05))
    return a
