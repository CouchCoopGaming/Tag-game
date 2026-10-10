"""Stop bar. Spans one 6 m road tile. No collider. Place on the road top."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register


@register
def create():
    a = Asset(
        "StopBar",
        "Roads",
        "Stop bar 6.0 m across and 0.40 m deep. Visual only. Place it on a road top (y = 0.12).",
    )
    a.climb_note = "Decal. The road slab under it is the collider."
    a.vault_note = "No collider of its own, so it cannot become a lip."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.004, 0), (5.6, 0.008, 0.40), "Lib_PaintWhite")
        a.end()
    return a
