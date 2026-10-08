"""Roof parapet. 4.0 m long, 0.50 m tall. Stack it on a wall at y = 3.2 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Brick_Parapet",
        "Buildings",
        "4.00 m parapet, 0.50 m tall, coping overhangs 3 cm. Stack on a wall (base is the pivot).",
    )
    a.climbable = True
    a.climb_note = "Short cling face, 0.50 m. Useful as a roof edge, not a full wall."
    a.vault_note = "Coping is 0.50 m above its own base. Vault only if the roof you stand on makes the coping fall in the 0.90–1.05 band."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.006, 0.003, 0.0)
        g.box((0, 0.22, 0), (4.0, 0.44, 0.30), "Lib_Brick", bevel=bev, segs=lod_pick(lod, 1, 1, 0))
        g.box((0, 0.47, 0), (4.06, 0.06, 0.38), "Lib_Concrete", bevel=bev, segs=1)
        a.end()
    a.box("Climb_Parapet", (0, 0.22, 0), (4.0, 0.44, 0.30))
    a.box("Col_Coping", (0, 0.47, 0), (4.06, 0.06, 0.38))
    return a
