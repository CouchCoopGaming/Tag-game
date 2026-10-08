"""Brick wall module. 4.0 m wide, 3.2 m tall, 0.30 m thick. Exterior is +Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

W, H, T = 4.0, 3.2, 0.30


@register
def create():
    a = Asset(
        "Brick_Wall",
        "Buildings",
        "4.00 x 3.20 x 0.30 m brick wall. Exterior normal is +Z. Butt the ends to Brick_Corner.",
    )
    a.climbable = True
    a.climb_note = "Both broad faces are cling panels. Exterior is +Z. Collider is Climb_Wall."
    a.vault_note = "No rail. The cornice is at 3.2 m."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.008, 0.004, 0.0)
        bs = lod_pick(lod, 2, 1, 0)
        g.box((0, 0.09, 0), (W, 0.18, T + 0.06), "Lib_Brick", bevel=bev, segs=bs, uv_scale=1.0)
        g.box((0, 1.65, 0), (W, 2.94, T), "Lib_Brick", bevel=bev, segs=bs, uv_scale=1.0)
        g.box((0, 3.16, 0), (W, 0.08, T + 0.08), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.8)
        a.end()
    a.box("Climb_Wall", (0, 1.65, 0), (W, 2.94, T))
    a.box("Col_Plinth", (0, 0.09, 0), (W, 0.18, T + 0.06))
    a.box("Col_Cornice", (0, 3.16, 0), (W, 0.08, T + 0.08))
    return a
