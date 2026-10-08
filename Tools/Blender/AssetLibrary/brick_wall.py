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
        "4.00 x 3.20 x 0.30 m brick bay. Exterior is +Z. A door and a window sit in 10 cm brick reveals, "
        "with one stone lintel and a stone sill. Butt the ends to Brick_Corner.",
    )
    a.climbable = True
    a.climb_note = (
        "Both broad faces are cling panels. Exterior is +Z. Climb_Wall is still the solid 0.30 m wall, "
        "so the 10 cm door recess does not change the cling surface."
    )
    a.vault_note = "No rail. The cornice is at 3.2 m."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.008, 0.004, 0.0)
        bs = lod_pick(lod, 2, 1, 0)
        brick = dict(bevel=bev, segs=bs, uv_scale=1.0)
        g.box((0, 0.09, 0), (W, 0.18, T + 0.06), "Lib_Brick", **brick)
        # Piers and the mullion are full height. The door and window are the only holes.
        g.box((-1.70, 1.65, 0), (0.70, 3.00, T), "Lib_Brick", **brick)  # x -2.05..-1.35
        g.box((1.50, 1.65, 0), (1.10, 3.00, T), "Lib_Brick", **brick)  # x 0.95..2.05
        g.box((-0.20, 1.605, 0), (0.70, 2.89, T), "Lib_Brick", uv_scale=1.0)  # x -0.55..0.15
        g.box((0.55, 0.70, 0), (0.84, 1.04, T), "Lib_Brick", uv_scale=1.0)  # under the sill, y 0.18..1.22
        g.box((-0.95, 2.40, 0), (0.90, 0.56, T), "Lib_Brick", uv_scale=1.0)  # over the door
        g.box((0.55, 2.40, 0), (0.90, 0.56, T), "Lib_Brick", uv_scale=1.0)  # over the window
        g.box((0, 2.85, 0), (3.10, 0.60, T), "Lib_Brick", uv_scale=1.0)
        # One stone lintel across both openings, and a sill on the brick under the window.
        g.box((-0.20, 2.20, 0.11), (2.70, 0.10, 0.14), "Lib_Concrete")
        g.box((0.55, 1.16, 0.12), (1.02, 0.08, 0.16), "Lib_Concrete")
        _door(g, lod)
        _window(g, lod)
        g.box((0, 3.16, 0), (W, 0.08, T + 0.08), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.8)
        a.end()
    a.box("Climb_Wall", (0, 1.65, 0), (W, 2.94, T))
    a.box("Col_Plinth", (0, 0.09, 0), (W, 0.18, T + 0.06))
    a.box("Col_Cornice", (0, 3.16, 0), (W, 0.08, T + 0.08))
    return a


def _door(g, lod):
    """Frame set 10 cm back from the +Z face. The slab fills the frame."""
    z = 0.01
    g.box((-1.33, 1.16, z), (0.08, 2.00, 0.08), "Lib_Wood")
    g.box((-0.57, 1.16, z), (0.08, 2.00, 0.08), "Lib_Wood")
    g.box((-0.95, 2.12, z), (0.68, 0.08, 0.08), "Lib_Wood")
    g.box((-0.95, 1.16, z + 0.01), (0.74, 1.96, 0.04), "Lib_WoodDark")
    if lod == 0:
        g.box((-0.95, 1.55, z + 0.035), (0.52, 0.70, 0.012), "Lib_Wood")
        g.box((-0.95, 0.70, z + 0.035), (0.52, 0.72, 0.012), "Lib_Wood")
        g.box((-0.68, 1.05, z + 0.05), (0.04, 0.08, 0.03), "Lib_Brass")


def _window(g, lod):
    z = 0.01
    g.box((0.17, 1.66, z), (0.10, 0.96, 0.08), "Lib_Wood")
    g.box((0.93, 1.66, z), (0.10, 0.96, 0.08), "Lib_Wood")
    g.box((0.55, 2.10, z), (0.70, 0.10, 0.08), "Lib_Wood")
    g.box((0.55, 1.22, z), (0.70, 0.10, 0.08), "Lib_Wood")
    g.box((0.55, 1.66, z + 0.01), (0.72, 0.82, 0.03), "Lib_ShopGlass")
    if lod == 0:
        g.box((0.55, 1.66, z + 0.03), (0.62, 0.02, 0.012), "Lib_Wood")
        g.box((0.55, 1.66, z + 0.03), (0.02, 0.76, 0.012), "Lib_Wood")
