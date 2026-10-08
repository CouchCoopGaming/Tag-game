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
        "4.00 x 3.20 x 0.30 m brick bay. Exterior is +Z, with a closed door and a window. Butt the ends to Brick_Corner.",
    )
    a.climbable = True
    a.climb_note = "Both broad faces are cling panels. Exterior is +Z. Collider is Climb_Wall."
    a.vault_note = "No rail. The cornice is at 3.2 m."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.008, 0.004, 0.0)
        bs = lod_pick(lod, 2, 1, 0)
        g.box((0, 0.09, 0), (W, 0.18, T + 0.06), "Lib_Brick", bevel=bev, segs=bs, uv_scale=1.0)
        # End piers and bands leave a door and a window. The cling samples sit on the piers and bands.
        g.box((-1.80, 1.65, 0), (0.50, 3.00, T), "Lib_Brick", bevel=bev, segs=bs, uv_scale=1.0)
        g.box((1.80, 1.65, 0), (0.50, 3.00, T), "Lib_Brick", bevel=bev, segs=bs, uv_scale=1.0)
        g.box((0.55, 0.42, 0), (2.00, 0.54, T), "Lib_Brick", uv_scale=1.0)
        g.box((0.0, 2.85, 0), (3.10, 0.60, T), "Lib_Brick", uv_scale=1.0)
        g.box((-0.20, 1.55, 0), (0.70, 1.70, T), "Lib_Brick", uv_scale=1.0)
        g.box((1.22, 1.55, 0), (0.66, 1.70, T), "Lib_Brick", uv_scale=1.0)
        # Closed door on the left, window to the right of the mullion.
        g.box((-0.95, 1.10, 0.02), (0.78, 2.00, 0.08), "Lib_WoodDark")
        if lod == 0:
            g.box((-0.95, 1.10, 0.07), (0.86, 2.08, 0.04), "Lib_PaintCream")
            g.box((-0.72, 1.05, 0.10), (0.04, 0.08, 0.04), "Lib_Brass")
        g.box((0.55, 1.85, 0.02), (0.78, 1.05, 0.06), "Lib_ShopGlass")
        g.box((0.55, 1.28, 0.10), (0.92, 0.06, 0.10), "Lib_PaintCream")
        g.box((0.55, 2.42, 0.09), (0.92, 0.08, 0.08), "Lib_Concrete")
        if lod == 0:
            g.box((0.55, 1.85, 0.06), (0.78, 0.03, 0.02), "Lib_PaintCream")
        g.box((0, 3.16, 0), (W, 0.08, T + 0.08), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.8)
        g.box((0, 2.55, T * 0.5 + 0.02), (W - 0.08, 0.08, 0.04), "Lib_Concrete")
        a.end()
    a.box("Climb_Wall", (0, 1.65, 0), (W, 2.94, T))
    a.box("Col_Plinth", (0, 0.09, 0), (W, 0.18, T + 0.06))
    a.box("Col_Cornice", (0, 3.16, 0), (W, 0.08, T + 0.08))
    return a
