"""Suburban house. Siding, gable roof, porch on +Z. 7.2 x 5.4 m body."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cabin import _add_roof
from _common import Asset, register, lod_pick


def _window(g, x, y, z, w, h, lod, face="Z"):
    if face == "Z":
        g.box((x, y, z), (w, h, 0.08), "Lib_PaintWhite", bevel=0.004 if lod == 0 else 0, segs=1)
        g.box((x, y, z + 0.02), (w - 0.08, h - 0.08, 0.02), "Lib_Glass")
    else:
        g.box((x, y, z), (0.08, h, w), "Lib_PaintWhite")
        g.box((x + 0.02, y, z), (0.02, h - 0.08, w - 0.08), "Lib_Glass")


@register
def create():
    a = Asset(
        "House",
        "Buildings",
        "Siding house. Body 7.2 x 5.4 m, walls 2.75 m, ridge 4.35 m, porch on +Z at 0.30 m.",
    )
    a.climbable = True
    a.climb_note = "Siding walls are cling. Windows are glass. Porch is open on +Z."
    a.vaultable = True
    a.vault_height = 0.95
    a.vault_note = "Porch rail is 0.95 m above the porch deck (deck y = 0.30, rail top y = 1.25)."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.01, 0.005, 0.0)
        bs = lod_pick(lod, 1, 1, 0)
        # Body shell as four walls so the porch and windows are not a solid block.
        g.box((0, 1.38, -2.62), (7.2, 2.75, 0.16), "Lib_Siding", bevel=bev, segs=bs, uv_scale=1.0)
        g.box((0, 1.38, 2.62), (7.2, 2.75, 0.16), "Lib_Siding", bevel=bev, segs=bs, uv_scale=1.0)
        g.box((-3.52, 1.38, 0), (0.16, 2.75, 5.1), "Lib_Siding", bevel=bev, segs=bs, uv_scale=1.0)
        g.box((3.52, 1.38, 0), (0.16, 2.75, 5.1), "Lib_Siding", bevel=bev, segs=bs, uv_scale=1.0)
        g.box((0, 0.10, 0), (7.4, 0.20, 5.6), "Lib_Concrete", uv_scale=0.6)
        if lod < 2:
            _window(g, -1.6, 1.55, 2.72, 1.1, 1.3, lod)
            _window(g, 1.8, 1.55, 2.72, 1.1, 1.3, lod)
            g.box((0, 1.15, 2.70), (0.96, 2.05, 0.06), "Lib_WoodDark", uv_scale=1.2)
            if lod == 0:
                g.box((0.32, 1.05, 2.75), (0.04, 0.08, 0.03), "Lib_Brass")
        # Porch
        g.box((0, 0.15, 3.65), (4.4, 0.30, 1.8), "Lib_Wood", uv_scale=1.0)
        for x in (-1.8, 1.8):
            g.box((x, 1.55, 4.4), (0.14, 2.5, 0.14), "Lib_PaintWhite", bevel=bev, segs=1)
        g.box((0, 2.55, 3.7), (4.8, 0.08, 2.0), "Lib_Roof", uv_scale=1.0)
        if lod < 2:
            g.pipe((-1.8, 1.25, 4.4), (1.8, 1.25, 4.4), 0.035, "Lib_PaintWhite", 6)
        _gable(g, lod)
        a.end()
    a.box("Climb_Back", (0, 1.38, -2.62), (7.2, 2.75, 0.16))
    a.box("Climb_FrontL", (-2.15, 1.38, 2.62), (2.9, 2.75, 0.16))
    a.box("Climb_FrontR", (2.15, 1.38, 2.62), (2.9, 2.75, 0.16))
    a.box("Col_Door", (0, 1.15, 2.70), (0.96, 2.05, 0.06))
    a.box("Col_GlassL", (-1.6, 1.55, 2.74), (1.0, 1.2, 0.02))
    a.box("Col_GlassR", (1.8, 1.55, 2.74), (1.0, 1.2, 0.02))
    a.box("Climb_SideL", (-3.52, 1.38, 0), (0.16, 2.75, 5.1))
    a.box("Climb_SideR", (3.52, 1.38, 0), (0.16, 2.75, 5.1))
    a.box("Col_Porch", (0, 0.15, 3.65), (4.4, 0.30, 1.8))
    a.box("Col_PorchRoof", (0, 2.55, 3.7), (4.8, 0.08, 2.0))
    a.capsule("Vault_PorchRail", (0, 1.25, 4.4), 0.035, 3.6, 0)
    a.loose_pivot = True
    _add_roof(a, 7.8, -3.05, 2.70, 0.0, 4.35, 0.08)
    return a


def _gable(g, lod):
    from cabin import _add_roof, _prism, _prism_faces
    g.mesh(_prism(7.8, -3.05, 2.70, 0.0, 4.35, 0.08), _prism_faces(), "Lib_Roof", uv_scale=1.0)
    g.mesh(_prism(7.8, 3.05, 2.70, 0.0, 4.35, 0.08), _prism_faces(), "Lib_Roof", uv_scale=1.0)
    if lod < 2:
        # Gable fill on the sides, under the slopes. A simple triangle is two faces.
        for x, sign in ((-3.52, 1), (3.52, -1)):
            verts = [
                (x, 2.70, -2.6), (x, 2.70, 2.6), (x, 4.25, 0.0),
            ]
            g.mesh(verts, [(0, 1, 2)] if sign > 0 else [(0, 2, 1)], "Lib_Siding")
