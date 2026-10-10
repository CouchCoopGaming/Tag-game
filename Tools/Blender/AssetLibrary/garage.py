"""Single garage. Overhead door, 3.6 x 6.2 m, gable."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cabin import _add_roof, _prism, _prism_faces
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Garage", "Buildings", "Single garage 3.6 m wide, 6.2 m deep, door 2.4 x 2.15 m on +Z. Ridge at 3.6 m.")
    a.climbable = True
    a.climb_note = "Side walls are cling. The overhead door is closed."
    a.vault_note = "No rail."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.006 if lod == 0 else 0
        g.box((0, 1.25, -3.02), (3.6, 2.5, 0.16), "Lib_Siding", bevel=bev, segs=1, uv_scale=1.0)
        g.box((-1.72, 1.25, 0), (0.16, 2.5, 5.9), "Lib_Siding", bevel=bev, segs=1, uv_scale=1.0)
        g.box((1.72, 1.25, 0), (0.16, 2.5, 5.9), "Lib_Siding", bevel=bev, segs=1, uv_scale=1.0)
        g.box((0, 2.35, 2.4), (2.4, 0.30, 0.16), "Lib_Siding")
        g.box((-1.82, 1.55, -0.8), (0.06, 0.78, 1.05), "Lib_PaintWhite", bevel=bev, segs=1)
        if lod < 2:
            g.box((-1.86, 1.55, -0.8), (0.015, 0.58, 0.82), "Lib_Window")
            if lod == 0:
                g.box((-1.87, 1.55, -0.8), (0.01, 0.58, 0.02), "Lib_PaintWhite")
                g.box((-1.87, 1.55, -0.8), (0.01, 0.02, 0.82), "Lib_PaintWhite")
            g.pipe((-1.9, 2.42, -3.35), (1.9, 2.42, -3.35), 0.04, "Lib_SteelDark", 6)
            g.pipe((1.7, 2.42, -3.35), (1.7, 0.15, -3.35), 0.03, "Lib_SteelDark", 6)
        g.box((0, 0.02, 4.55), (2.7, 0.04, 2.5), "Lib_Concrete", uv_scale=0.7)
        g.box((0, 2.28, 3.06), (2.45, 0.08, 0.06), "Lib_PaintWhite")
        # Door panels.
        panels = lod_pick(lod, 4, 3, 1)
        for i in range(panels):
            y = 0.28 + (i + 0.5) * (1.85 / panels)
            g.box((0, y, 3.02), (2.25, 1.85 / panels * 0.88, 0.05), "Lib_PaintWhite", bevel=bev, segs=1)
        if lod == 0:
            g.box((0.9, 1.1, 3.06), (0.04, 0.12, 0.03), "Lib_Black")
        g.mesh(_prism(4.0, -3.3, 2.5, 0.0, 3.6, 0.07), _prism_faces(), "Lib_Roof")
        g.mesh(_prism(4.0, 3.3, 2.5, 0.0, 3.6, 0.07), _prism_faces(), "Lib_Roof")
        a.end()
    a.box("Climb_Back", (0, 1.25, -3.02), (3.6, 2.5, 0.16))
    a.box("Climb_SideL", (-1.72, 1.25, 0), (0.16, 2.5, 5.9))
    a.box("Climb_SideR", (1.72, 1.25, 0), (0.16, 2.5, 5.9))
    a.loose_pivot = True
    panels = 4
    for i in range(panels):
        y = 0.28 + (i + 0.5) * (1.85 / panels)
        a.box("Col_Door_%d" % i, (0, y, 3.02), (2.15, 1.85 / panels * 0.80, 0.04))
    a.box("Col_Header", (0, 2.35, 2.4), (2.3, 0.26, 0.14))
    a.box("Col_Apron", (0, 0.02, 4.55), (2.6, 0.035, 2.4))
    _add_roof(a, 4.0, -3.3, 2.5, 0.0, 3.6, 0.07)
    return a
