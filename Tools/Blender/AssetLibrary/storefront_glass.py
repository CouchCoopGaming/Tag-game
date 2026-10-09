"""Ground-floor glass bay. Snaps to the 4 x 3.2 m brick module. No awning."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

W, H, T = 4.0, 3.2, 0.30


def _mullions(g, face):
    g.box((0, 1.45, face), (0.025, 2.15, 0.02), "Lib_Steel")
    for x in (-0.45, 0.45):
        g.box((x, 1.45, face), (0.018, 2.15, 0.016), "Lib_Steel")
    g.box((0, 1.85, face), (1.7, 0.02, 0.016), "Lib_Steel")
    g.box((0, 1.15, face), (1.7, 0.02, 0.016), "Lib_Steel")


@register
def create():
    a = Asset(
        "Storefront_Glass",
        "Buildings",
        "4.00 x 3.20 m ground-floor bay. Inset glass, mullions, and a framed door with a handle. Exterior is +Z. No awning.",
    )
    a.climbable = True
    a.climb_note = "Piers are cling. Glass and the door are solid. Exterior is +Z."
    a.vault_note = "No rail. The head is at 3.2 m."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.005, 0.0, 0.0)
        g.box((-1.55, 1.60, 0), (0.90, H, T), "Lib_Brick", bevel=bev, segs=1)
        g.box((1.55, 1.60, 0), (0.90, H, T), "Lib_Brick", bevel=bev, segs=1)
        g.box((0, 2.95, 0), (2.2, 0.50, T), "Lib_Brick")
        g.box((0, 3.16, 0), (W, 0.08, T + 0.08), "Lib_Concrete")
        face = 0.04
        g.box((-0.35, 1.45, 0.0), (1.55, 2.35, 0.015), "Lib_Window")
        g.box((-1.15, 1.45, face), (0.06, 2.45, 0.06), "Lib_SteelDark")
        g.box((0.45, 1.45, face), (0.06, 2.45, 0.06), "Lib_SteelDark")
        g.box((-0.35, 2.64, face), (1.7, 0.06, 0.05), "Lib_SteelDark")
        g.box((-0.35, 0.22, face), (1.7, 0.08, 0.06), "Lib_Concrete")
        g.box((1.05, 1.10, 0.02), (0.78, 2.05, 0.04), "Lib_WoodDark")
        g.box((0.62, 1.10, face), (0.05, 2.15, 0.06), "Lib_Wood")
        g.box((1.48, 1.10, face), (0.05, 2.15, 0.06), "Lib_Wood")
        if lod == 0:
            _mullions(g, face + 0.02)
            g.cylinder((1.38, 1.05, face + 0.04), 0.015, 0.05, "Lib_Brass", 6, axis="Z")
            g.box((1.05, 2.22, face), (0.9, 0.28, 0.012), "Lib_Window")
        a.end()
    a.box("Climb_PierL", (-1.55, 1.60, 0), (0.90, H, T))
    a.box("Climb_PierR", (1.55, 1.60, 0), (0.90, H, T))
    a.box("Climb_Header", (0, 2.95, 0), (2.2, 0.50, T))
    a.box("Col_Glass", (-0.35, 1.45, 0.0), (1.50, 2.30, 0.02))
    a.box("Col_Door", (1.05, 1.10, 0.02), (0.78, 2.05, 0.05))
    a.box("Col_Cornice", (0, 3.16, 0), (W, 0.08, T + 0.06))
    return a
