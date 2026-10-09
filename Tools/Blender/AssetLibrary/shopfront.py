"""Shop bay with an awning. Same 4 x 3.2 m module as Storefront_Glass, plus the awning."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "ShopFront",
        "Buildings",
        "4.0 x 3.2 m shop bay. Framed glass, a door with a handle, and an awning that projects 1.1 m on +Z.",
    )
    a.climbable = True
    a.climb_note = "Piers are cling. Glass and the door are solid. Awning is a landing."
    a.vault_note = "Awning front edge is at 2.55 m. A landing, not a ground vault."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.005 if lod == 0 else 0
        g.box((-1.55, 1.60, 0), (0.90, 3.2, 0.28), "Lib_Brick", bevel=bev, segs=1)
        g.box((1.55, 1.60, 0), (0.90, 3.2, 0.28), "Lib_Brick", bevel=bev, segs=1)
        g.box((0, 2.95, 0), (2.2, 0.50, 0.28), "Lib_Brick")
        g.box((-0.45, 1.35, 0.0), (1.35, 2.15, 0.015), "Lib_Window")
        g.box((-1.15, 1.35, 0.06), (0.05, 2.25, 0.05), "Lib_SteelDark")
        g.box((0.25, 1.35, 0.06), (0.05, 2.25, 0.05), "Lib_SteelDark")
        if lod < 2:
            g.box((-0.45, 2.45, 0.06), (1.5, 0.05, 0.05), "Lib_SteelDark")
        g.box((0.95, 1.10, 0.03), (0.78, 2.05, 0.04), "Lib_WoodDark")
        if lod < 2:
            g.box((0, 2.62, 0.55), (3.4, 0.05, 1.15), "Lib_Awning", bevel=bev, segs=1, euler=(-8, 0, 0))
            g.box((0, 2.78, 0.08), (3.5, 0.06, 0.10), "Lib_SteelDark")
            for x in (-1.4, 1.4):
                g.cylinder((x, 2.35, 1.05), 0.012, 0.55, "Lib_SteelDark", 5)
        if lod == 0:
            g.box((-0.45, 1.35, 0.07), (0.02, 2.05, 0.015), "Lib_Steel")
            g.box((-0.45, 1.7, 0.07), (1.25, 0.018, 0.012), "Lib_Steel")
            g.cylinder((1.25, 1.05, 0.08), 0.014, 0.04, "Lib_Brass", 6, axis="Z")
        a.end()
    a.box("Climb_PierL", (-1.55, 1.60, 0), (0.90, 3.2, 0.28))
    a.box("Climb_PierR", (1.55, 1.60, 0), (0.90, 3.2, 0.28))
    a.box("Climb_Header", (0, 2.95, 0), (2.2, 0.50, 0.28))
    a.box("Col_Glass", (-0.45, 1.35, 0.0), (1.30, 2.10, 0.02))
    a.box("Col_Door", (0.95, 1.10, 0.03), (0.78, 2.05, 0.05))
    a.box("Col_Awning", (0, 2.62, 0.55), (3.4, 0.08, 1.15), euler=(-8, 0, 0))
    return a
