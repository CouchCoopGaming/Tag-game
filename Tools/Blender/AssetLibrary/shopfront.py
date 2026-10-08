"""Shop front. 4 m bay, display glass, door, and a fabric awning."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "ShopFront",
        "Buildings",
        "4.0 x 3.2 m shop bay. Display glass, closed door, awning projects 1.1 m on +Z.",
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
        g.box((-0.55, 1.35, 0.02), (1.15, 2.15, 0.02), "Lib_Glass")
        g.box((0.85, 1.15, 0.04), (0.85, 2.10, 0.05), "Lib_WoodDark", uv_scale=1.0)
        if lod < 2:
            # Awning: a shallow slope, high at the wall.
            g.box((0, 2.62, 0.55), (3.4, 0.06, 1.15), "Lib_Awning", bevel=bev, segs=1, euler=(-8, 0, 0))
            g.box((0, 2.78, 0.08), (3.5, 0.08, 0.12), "Lib_SteelDark")
        if lod == 0:
            g.text("OPEN", (-0.55, 2.35, 0.05), 0.10, "Lib_PaintWhite", extrude=0.004)
        a.end()
    a.box("Climb_PierL", (-1.55, 1.60, 0), (0.90, 3.2, 0.28))
    a.box("Climb_PierR", (1.55, 1.60, 0), (0.90, 3.2, 0.28))
    a.box("Climb_Header", (0, 2.95, 0), (2.2, 0.50, 0.28))
    a.box("Col_Glass", (-0.55, 1.35, 0.02), (1.15, 2.15, 0.02))
    a.box("Col_Door", (0.85, 1.15, 0.04), (0.85, 2.10, 0.05))
    a.box("Col_Awning", (0, 2.62, 0.55), (3.4, 0.08, 1.15), euler=(-8, 0, 0))
    return a
