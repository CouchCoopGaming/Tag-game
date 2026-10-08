"""Single-head parking meter. Separate from the twin-head meter."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "ParkingMeter_Single",
        "StreetFurniture",
        "Single meter. Post to 1.05 m, head 1.08–1.40 m, 0.18 m wide.",
    )
    a.climb_note = "Not a cling."
    a.vault_note = "Too short to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        g.box((0, 0.015, 0), (0.20, 0.03, 0.20), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.54, 0), 0.032, 1.02, "Lib_Steel", seg)
        g.cylinder((0, 1.08, 0), 0.055, 0.06, "Lib_SteelDark", seg)
        g.box((0, 1.26, 0), (0.18, 0.30, 0.14), "Lib_SteelDark", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((0, 1.30, 0.072), (0.12, 0.10, 0.008), "Lib_Glass")
        g.box((0, 1.18, 0.074), (0.10, 0.035, 0.008), "Lib_PaintCream")
        g.cylinder((0, 1.16, 0.082), 0.012, 0.012, "Lib_Brass", 8, axis="Z")
        if lod == 0:
            g.box((0, 1.38, 0), (0.14, 0.025, 0.10), "Lib_Steel")
            g.box((0.0, 1.22, 0.078), (0.06, 0.008, 0.006), "Lib_Black")
        a.end()
    a.box("Col_Base", (0, 0.015, 0), (0.14, 0.02, 0.14))
    a.capsule("Col_Post", (0, 0.55, 0), 0.024, 0.90, 1)
    a.box("Col_Head", (0, 1.26, 0), (0.12, 0.22, 0.09))
    return a
