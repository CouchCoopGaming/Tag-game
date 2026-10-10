"""Harbor water sheet. Visual only, so it cannot become an invisible floor."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register


@register
def create():
    a = Asset(
        "HarborWater",
        "Harbor",
        "Water sheet 16 x 12 m, 2 cm thick. No collider. Set it at the waterline and let docks sit on their own pilings.",
    )
    a.climb_note = "Visual water. No collider, so it is not a floor and not a wall."
    a.vault_note = "No collider."
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.01, 0), (16.0, 0.02, 12.0), "Lib_Water", uv_scale=0.15)
        a.end()
    return a
