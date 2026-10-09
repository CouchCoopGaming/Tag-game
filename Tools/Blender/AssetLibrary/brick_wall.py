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
        "4.00 x 3.20 x 0.30 m brick bay. Exterior is +Z. A door and a window sit in brick reveals, "
        "with one stone lintel and a stone sill. A concrete corona projects about 23 cm on +Z under the cap. "
        "Butt the ends to Brick_Corner.",
    )
    a.climbable = True
    a.climb_note = (
        "Both broad faces are cling panels. Exterior is +Z. Climb_Wall is still the solid 0.30 m wall, "
        "so the 10 cm door recess does not change the cling surface."
    )
    a.vault_note = "No rail. The cornice is at 3.2 m."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.008, 0.004, 0.0)
        bs = lod_pick(lod, 2, 1, 0)
        g.box((0, 0.09, 0), (W, 0.18, T + 0.06), "Lib_Brick", bevel=bev, segs=bs, uv_scale=1.0)
        # Each front face is its own brick, butted, so nothing shares a plane.
        # Left pier x -2.00..-1.35, right pier 0.95..2.00, both up to the cornice.
        g.box((-1.675, 1.65, 0), (0.65, 2.94, T), "Lib_Brick", uv_scale=1.0)
        g.box((1.475, 1.65, 0), (1.05, 2.94, T), "Lib_Brick", uv_scale=1.0)
        # Mullion between the openings, only up to the header.
        g.box((-0.20, 1.15, 0), (0.70, 1.94, T), "Lib_Brick", uv_scale=1.0)
        # Brick under the sill. Meets the mullion and the right pier without an overlap.
        g.box((0.55, 0.65, 0), (0.80, 0.94, T), "Lib_Brick", uv_scale=1.0)
        # Header fills above both openings and stops at the piers.
        g.box((-0.20, 2.62, 0), (2.30, 1.00, T), "Lib_Brick", uv_scale=1.0)
        # One stone lintel across both openings, and a sill on the brick under the window.
        g.box((-0.20, 2.20, 0.11), (2.70, 0.10, 0.14), "Lib_Concrete")
        g.box((0.55, 1.16, 0.12), (0.84, 0.08, 0.16), "Lib_Concrete")
        _door(g, lod)
        _window(g, lod)
        g.box((0, 3.16, 0), (W, 0.08, T + 0.08), "Lib_Concrete", bevel=bev, segs=bs, uv_scale=0.8)
        # Corona on the street face, clear of the brick (face at z=0.15), under the cap.
        g.box((0, 2.98, 0.30), (3.84, 0.12, 0.16), "Lib_Concrete", uv_scale=0.6)
        a.end()
    a.box("Climb_Wall", (0, 1.65, 0), (W, 2.94, T))
    a.box("Col_Plinth", (0, 0.09, 0), (W, 0.18, T + 0.06))
    a.box("Col_Cornice", (0, 3.16, 0), (3.70, 0.05, 0.28))
    return a


def _door(g, lod):
    """Frame set 10 cm back from the +Z face. The slab fills the frame."""
    z = 0.01
    g.box((-1.33, 1.16, z), (0.08, 2.00, 0.08), "Lib_Wood")
    g.box((-0.57, 1.16, z), (0.08, 2.00, 0.08), "Lib_Wood")
    g.box((-0.95, 2.12, z), (0.68, 0.08, 0.08), "Lib_Wood")
    # Vertical planks, about 13 cm, grain running with the height.
    left, right = -1.29, -0.61
    count = 5
    seam = 0.004
    span = right - left
    pw = (span - seam * (count - 1)) / count
    for i in range(count):
        x = left + pw * 0.5 + i * (pw + seam)
        g.box((x, 1.14, z + 0.012), (pw, 1.88, 0.028), "Lib_WoodDark")
    g.box((-0.95, 2.02, z + 0.030), (0.64, 0.055, 0.012), "Lib_Wood")
    g.box((-0.95, 1.14, z + 0.030), (0.64, 0.055, 0.012), "Lib_Wood")
    g.box((-0.95, 0.34, z + 0.030), (0.64, 0.055, 0.012), "Lib_Wood")
    if lod == 0:
        g.box((-0.68, 1.05, z + 0.046), (0.04, 0.08, 0.03), "Lib_Brass")


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
