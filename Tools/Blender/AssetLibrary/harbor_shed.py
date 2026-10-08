"""Small wood harbor shed. Door faces +Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

W = 3.6
D = 2.8
H = 2.35


def _roof(g):
    """Closed gable. Eaves sit above the wall tops so the two shells do not meet."""
    ox = W * 0.5 + 0.18
    ez = D * 0.5 + 0.22
    ye = 2.48
    yr = 3.20
    ins = 0.06
    verts = [
        (-ox, yr, 0.0),
        (ox, yr, 0.0),
        (-ox, ye, ez),
        (ox, ye, ez),
        (-ox, ye, -ez),
        (ox, ye, -ez),
        (-ox + ins, yr - 0.10, 0.0),
        (ox - ins, yr - 0.10, 0.0),
        (-ox + ins, ye - 0.08, ez - ins),
        (ox - ins, ye - 0.08, ez - ins),
        (-ox + ins, ye - 0.08, -ez + ins),
        (ox - ins, ye - 0.08, -ez + ins),
    ]
    faces = [
        (0, 2, 3, 1),
        (0, 1, 5, 4),
        (0, 4, 2),
        (1, 3, 5),
        (6, 7, 9, 8),
        (6, 10, 11, 7),
        (6, 8, 10),
        (7, 11, 9),
        (0, 2, 8, 6),
        (0, 6, 10, 4),
        (1, 7, 9, 3),
        (1, 5, 11, 7),
        (2, 3, 9, 8),
        (4, 10, 11, 5),
    ]
    g.mesh(verts, faces, "Lib_Roof", uv_scale=0.9)
    # Fascia hangs just clear of the eave face.
    g.box((0, ye - 0.07, ez + 0.03), (ox * 2, 0.12, 0.035), "Lib_WoodDark")
    g.box((0, ye - 0.07, -ez - 0.03), (ox * 2, 0.12, 0.035), "Lib_WoodDark")


@register
def create():
    a = Asset(
        "HarborShed",
        "Harbor",
        "Wood shed, 3.6 x 2.8 m, walls at 2.35 m. Closed gable roof, ridge at 3.20 m, with fascia. Door and a small window face +Z.",
    )
    a.climbable = True
    a.climb_note = "The board walls are cling. The door is closed."
    a.vault_note = "No rail. Wall top is 2.35 m."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = 0.003 if lod == 0 else 0
        g.box((0, 0.04, 0), (W + 0.08, 0.08, D + 0.08), "Lib_Concrete", uv_scale=0.7)
        g.box((0, 1.22, -D * 0.5 + 0.04), (W - 0.16, 2.15, 0.06), "Lib_WoodWeather", uv_scale=1.0)
        g.box((-W * 0.5 + 0.04, 1.22, 0), (0.06, 2.15, D - 0.20), "Lib_WoodWeather", uv_scale=1.0)
        g.box((W * 0.5 - 0.04, 1.22, 0), (0.06, 2.15, D - 0.20), "Lib_WoodWeather", uv_scale=1.0)
        g.box((-0.85, 1.22, D * 0.5 - 0.04), (1.35, 2.15, 0.06), "Lib_WoodWeather", uv_scale=1.0)
        g.box((1.15, 1.55, D * 0.5 - 0.04), (0.85, 1.45, 0.06), "Lib_WoodWeather", uv_scale=1.0)
        g.box((0.15, 2.15, D * 0.5 - 0.04), (1.15, 0.22, 0.06), "Lib_WoodWeather", uv_scale=1.0)
        g.box((0.15, 1.05, D * 0.5 + 0.02), (0.92, 1.85, 0.04), "Lib_WoodDark")
        if lod == 0:
            g.box((1.15, 1.55, D * 0.5 + 0.01), (0.55, 0.45, 0.02), "Lib_ShopGlass")
        _roof(g)
        a.end()
    a.box("Col_Floor", (0, 0.04, 0), (W - 0.05, 0.05, D - 0.05))
    a.box("Climb_Back", (0, 1.22, -D * 0.5 + 0.04), (W - 0.40, 1.90, 0.04))
    a.box("Climb_SideL", (-W * 0.5 + 0.04, 1.22, 0), (0.04, 1.90, D - 0.40))
    a.box("Climb_SideR", (W * 0.5 - 0.04, 1.22, 0), (0.04, 1.90, D - 0.40))
    a.box("Climb_FrontL", (-0.85, 1.22, D * 0.5 - 0.04), (1.05, 1.70, 0.03))
    a.box("Col_Door", (0.15, 1.05, D * 0.5 + 0.02), (0.78, 1.65, 0.025))
    return a
