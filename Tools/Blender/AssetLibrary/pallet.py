"""US pallet. 1.22 x 1.02 x 0.14 m, three stringers."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Pallet", "Utility", "48 x 40 inch pallet (1.22 x 1.02 m), 0.14 m tall, three stringers.")
    a.climb_note = "Not a wall."
    a.vault_note = "0.14 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        for z in (-0.40, 0.0, 0.40):
            g.box((0, 0.045, z), (1.22, 0.09, 0.09), "Lib_WoodDark", uv_scale=1.2)
        boards = lod_pick(lod, 7, 4)
        for i in range(boards):
            x = -0.52 + i * (1.04 / max(1, boards - 1))
            g.box((x, 0.115, 0), (0.10, 0.02, 1.02), "Lib_Wood", uv_scale=1.2)
            g.box((x, 0.012, 0), (0.10, 0.018, 0.70), "Lib_Wood", uv_scale=1.2)
        a.end()
    a.box("Col_Top", (0, 0.115, 0), (1.08, 0.016, 0.98))
    for i, z in enumerate((-0.40, 0.0, 0.40)):
        a.box("Col_Stringer_%d" % i, (0, 0.045, z), (1.22, 0.09, 0.09))
    return a
