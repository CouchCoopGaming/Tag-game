"""Harbor lighthouse. Shaft to 8.2 m, lantern and gallery above."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _radius_at(y):
    return 1.20 - 0.38 * ((y - 0.40) / 7.80)


def _frustum(g, y0, y1, seg, mat):
    g.cone((0, (y0 + y1) * 0.5, 0), _radius_at(y0), _radius_at(y1), y1 - y0, mat, seg)


@register
def create():
    a = Asset(
        "Lighthouse",
        "Harbor",
        "Lighthouse, 10.2 m to the finial. Shaft tapers from 2.4 m to 1.64 m. Gallery rail is 1.05 m above the gallery deck.",
    )
    a.climbable = False
    a.vaultable = True
    a.vault_height = 1.05
    a.climb_note = "The shaft is round, not a cling panel. The door is a closed hatch on +X."
    a.vault_note = "Gallery rail is 1.05 m above the gallery deck (deck y = 8.15, rail top y = 9.20)."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 20, 12, 8)
        g.cylinder((0, 0.20, 0), 1.55, 0.40, "Lib_Concrete", seg, uv_scale=0.6)
        # Stacked frustums so the red bands meet the white shaft flush.
        _frustum(g, 0.40, 1.80, seg, "Lib_PaintWhite")
        _frustum(g, 1.80, 3.00, seg, "Lib_PaintRed")
        _frustum(g, 3.00, 4.60, seg, "Lib_PaintWhite")
        _frustum(g, 4.60, 5.80, seg, "Lib_PaintRed")
        _frustum(g, 5.80, 8.02, seg, "Lib_PaintWhite")
        g.box((1.19, 1.15, 0), (0.06, 1.55, 0.48), "Lib_PaintRed", uv_scale=1.0)
        if lod == 0:
            g.cylinder((1.24, 1.15, 0), 0.035, 0.06, "Lib_Brass", 8, axis="X")
        g.cylinder((0, 8.16, 0), 1.15, 0.12, "Lib_Concrete", seg, uv_scale=0.5)
        g.cylinder((0, 8.58, 0), 0.58, 0.62, "Lib_Glass", max(8, seg // 2))
        g.cylinder((0, 8.32, 0), 0.64, 0.06, "Lib_Steel", seg)
        g.cylinder((0, 8.88, 0), 0.64, 0.06, "Lib_Steel", seg)
        g.cone((0, 9.45, 0), 0.72, 0.05, 0.70, "Lib_PaintRed", seg)
        g.cylinder((0, 9.90, 0), 0.025, 0.28, "Lib_SteelDark", 6)
        posts = lod_pick(lod, 12, 8, 0)
        for i in range(posts):
            ang = math.radians(i * (360.0 / posts))
            x, z = math.sin(ang) * 1.02, math.cos(ang) * 1.02
            g.cylinder((x, 8.68, z), 0.025, 1.00, "Lib_Steel", 6)
        if lod < 2:
            g.torus((0, 9.18, 0), 1.02, 0.022, "Lib_Steel", lod_pick(lod, 18, 12), 6)
        a.end()
    a.box("Col_Base", (0, 0.20, 0), (2.10, 0.36, 2.10))
    a.capsule("Col_Shaft", (0, 4.20, 0), 0.78, 7.2, 1)
    a.box("Col_Door", (1.19, 1.15, 0), (0.05, 1.48, 0.44))
    a.box("Col_Gallery", (0, 8.16, 0), (1.00, 0.08, 1.00))
    a.box("Col_Lantern", (0, 8.58, 0), (0.78, 0.56, 0.78))
    a.box("Col_Roof", (0, 9.30, 0), (0.50, 0.40, 0.50))
    for i in range(8):
        ang = math.radians(i * 45.0)
        x, z = math.sin(ang) * 1.02, math.cos(ang) * 1.02
        a.capsule("Col_Post_%d" % i, (x, 8.68, z), 0.025, 1.00, 1)
    # Cardinal pieces of the gallery rail. Diagonals are the posts.
    a.capsule("Vault_Rail_N", (0, 9.18, 1.02), 0.02, 0.24, 0)
    a.capsule("Vault_Rail_S", (0, 9.18, -1.02), 0.02, 0.24, 0)
    a.capsule("Vault_Rail_E", (1.02, 9.18, 0), 0.02, 0.24, 2)
    a.capsule("Vault_Rail_W", (-1.02, 9.18, 0), 0.02, 0.24, 2)
    return a
