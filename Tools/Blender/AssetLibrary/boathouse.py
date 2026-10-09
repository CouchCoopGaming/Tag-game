"""Wood boathouse. 6.4 x 4.4 m, slip opening on +Z, gable roof."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cabin import _add_roof, _prism, _prism_faces
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Boathouse",
        "Harbor",
        "Boathouse 6.4 m wide, 4.4 m deep, walls 2.7 m, ridge at 4.15 m. The +Z side is an open slip, 2.4 m wide.",
    )
    a.climbable = True
    a.vaultable = False
    a.climb_note = "Side and back walls are cling. The slip opening on +Z is empty. Roof slopes are landings."
    a.vault_note = "No rail. Wall top is 2.7 m."
    w, d = 6.4, 4.4
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.004 if lod == 0 else 0
        g.box((0, 0.08, 0), (w, 0.16, d), "Lib_WoodDark", uv_scale=1.0)
        # Back wall, solid.
        g.box((0, 1.45, -d * 0.5 + 0.08), (w - 0.2, 2.55, 0.12), "Lib_Wood", bevel=bev, segs=1, uv_scale=1.0)
        # Side walls.
        g.box((-w * 0.5 + 0.08, 1.45, 0), (0.12, 2.55, d - 0.2), "Lib_Wood", bevel=bev, segs=1, uv_scale=1.0)
        g.box((w * 0.5 - 0.08, 1.45, 0), (0.12, 2.55, d - 0.2), "Lib_Wood", bevel=bev, segs=1, uv_scale=1.0)
        # Front piers beside the slip, and a header. LOD2 keeps the open slip as a gap.
        if lod < 2:
            g.box((-2.15, 1.35, d * 0.5 - 0.08), (1.9, 2.35, 0.12), "Lib_Wood", uv_scale=1.0)
            g.box((2.15, 1.35, d * 0.5 - 0.08), (1.9, 2.35, 0.12), "Lib_Wood", uv_scale=1.0)
            g.box((0, 2.55, d * 0.5 - 0.08), (w - 0.2, 0.30, 0.12), "Lib_WoodDark", uv_scale=1.0)
        if lod < 2:
            g.box((0, 1.15, d * 0.5 - 0.02), (2.2, 0.08, 0.06), "Lib_WoodDark")
        g.mesh(_prism(w + 0.5, -d * 0.5 - 0.15, 2.70, 0.0, 4.15, 0.08), _prism_faces(), "Lib_Roof", uv_scale=1.0)
        g.mesh(_prism(w + 0.5, d * 0.5 + 0.15, 2.70, 0.0, 4.15, 0.08), _prism_faces(), "Lib_Roof", uv_scale=1.0)
        if lod == 0:
            for x in (-1.6, 1.6):
                g.box((x, 1.6, -d * 0.5 + 0.16), (0.7, 0.9, 0.02), "Lib_Glass")
                g.box((x, 1.6, -d * 0.5 + 0.14), (0.78, 0.98, 0.04), "Lib_WoodDark")
        a.end()
    a.box("Col_Floor", (0, 0.08, 0), (w, 0.16, d))
    a.box("Climb_Back", (0, 1.45, -d * 0.5 + 0.08), (w - 0.24, 2.50, 0.12))
    a.box("Climb_SideL", (-w * 0.5 + 0.08, 1.45, 0), (0.12, 2.50, d - 0.24))
    a.box("Climb_SideR", (w * 0.5 - 0.08, 1.45, 0), (0.12, 2.50, d - 0.24))
    a.box("Climb_FrontL", (-2.15, 1.15, d * 0.5 - 0.08), (1.70, 1.90, 0.08))
    a.box("Climb_FrontR", (2.15, 1.15, d * 0.5 - 0.08), (1.70, 1.90, 0.08))
    a.box("Climb_Header", (0, 2.55, d * 0.5 - 0.08), (w - 0.4, 0.24, 0.10))
    _add_roof(a, w + 0.5, -(d * 0.5 + 0.15), 2.70, 0.0, 4.15, 0.08)
    return a
