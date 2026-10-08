"""Post clock. A round face on a pole, not a parking meter."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "StreetClock_Post",
        "StreetFurniture",
        "Post clock. Pole to 2.35 m, face 0.46 m across, hands at 10:10.",
    )
    a.climb_note = "Pole is 8 cm. Not a cling."
    a.vault_note = "Too tall and smooth to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 10)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.02, 0), (0.28, 0.04, 0.28), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 1.14, 0), 0.040, 2.20, "Lib_Steel", seg)
        g.cylinder((0, 2.28, 0.04), 0.23, 0.06, "Lib_SteelDark", seg, axis="Z")
        g.cylinder((0, 2.28, 0.072), 0.19, 0.012, "Lib_PaintCream", seg, axis="Z")
        g.cylinder((0, 2.28, 0.082), 0.012, 0.010, "Lib_Black", 8, axis="Z")
        # 10:10. Hour hand toward 10, minute hand toward 2.
        for deg, length in ((-60.0, 0.09), (60.0, 0.13)):
            rad = math.radians(deg)
            g.box(
                (math.sin(rad) * length * 0.45, 2.28 + math.cos(rad) * length * 0.45, 0.086),
                (0.012, length, 0.006),
                "Lib_Black",
                euler=(0, 0, deg),
            )
        a.end()
    a.box("Col_Base", (0, 0.02, 0), (0.18, 0.024, 0.18))
    a.capsule("Col_Pole", (0, 1.00, 0), 0.026, 1.90, 1)
    a.box("Col_HeadL", (-0.12, 2.28, 0.04), (0.12, 0.18, 0.03))
    a.box("Col_HeadR", (0.12, 2.28, 0.04), (0.12, 0.18, 0.03))
    return a
