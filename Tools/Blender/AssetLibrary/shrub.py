"""Rounded shrub, 0.9 m tall."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Shrub", "Park", "Rounded shrub, 0.90 m tall, 1.05 m across.")
    a.climb_note = "Not a cling."
    a.vault_note = "Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        g.blob(
            [((0, 0.42, 0), 0.42), ((0.18, 0.55, 0.05), 0.28), ((-0.16, 0.48, -0.08), 0.26)],
            "Lib_FoliageDark",
            voxel=0.08 if lod == 0 else 0.14,
        )
        a.end()
    # Main crown is a 0.42 m ball centered at 0.42 m, so it meets the ground.
    a.sphere("Col_Shrub", (0, 0.30, 0), 0.28)
    return a
