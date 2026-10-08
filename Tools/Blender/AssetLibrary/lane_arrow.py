"""Lane arrow decal. No collider. Place it on a road top (y = 0.12), pointing +Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register


@register
def create():
    a = Asset(
        "LaneArrow",
        "Roads",
        "White lane arrow, 2.0 m long. Visual only. Place it on a road top (y = 0.12), pointing +Z.",
    )
    a.climb_note = "Decal. The road slab under it is the collider."
    a.vault_note = "No collider of its own, so it cannot become a lip."
    head = [
        (0.0, 0.0, 1.05), (-0.36, 0.0, 0.42), (0.36, 0.0, 0.42),
        (0.0, 0.008, 1.05), (-0.36, 0.008, 0.42), (0.36, 0.008, 0.42),
    ]
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.004, -0.36), (0.16, 0.008, 1.15), "Lib_PaintWhite")
        g.mesh([(v[0], v[1], v[2] - 0.11) for v in head], [
            (0, 2, 1), (3, 4, 5),
            (0, 1, 4, 3), (1, 2, 5, 4), (2, 0, 3, 5),
        ], "Lib_PaintWhite")
        a.end()
    return a
