"""Chain-link bay with a gate. 8 ft long, 6 ft to the top rail.

Mesh is a sheet plus a few diagonal wires on the face, not a woven diamond.
Panels overlap the post shells and stop short of the post colliders.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _post(g, x, lod):
    seg = lod_pick(lod, 8, 6)
    g.cylinder((x, 0.98, 0), 0.030, 1.96, "Lib_Steel", seg)
    g.cylinder((x, 0.03, 0), 0.06, 0.06, "Lib_Concrete", 8)
    g.sphere((x, 1.98, 0), 0.038, "Lib_Steel", seg)


@register
def create():
    a = Asset(
        "Fence_ChainGate",
        "StreetFurniture",
        "Chain-link bay 2.44 m long, top rail at 1.83 m. Gate leaf about 1.20 m. Mesh is a sheet with diagonal wires.",
    )
    a.climb_note = "Mesh is not a solid cling."
    a.vault_note = "Top rail is 1.83 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        for x in (-1.22, -0.10, 1.22):
            _post(g, x, lod)
        g.cylinder((0.0, 1.78, 0), 0.016, 2.48, "Lib_Steel", seg, axis="X")
        g.cylinder((0.0, 0.12, 0), 0.012, 2.48, "Lib_Steel", 6, axis="X")
        # Panels bite the post shells and stay outside the post capsules.
        # Mesh stops short of the posts. The rails tie the sheet to the posts.
        g.box((-0.66, 0.95, 0), (1.00, 1.74, 0.010), "Lib_Chain")
        g.box((0.55, 0.95, 0.012), (1.20, 1.70, 0.010), "Lib_Chain")
        g.cylinder((0.55, 1.68, 0.012), 0.014, 1.16, "Lib_Steel", 6, axis="X")
        g.cylinder((0.55, 0.22, 0.012), 0.012, 1.16, "Lib_Steel", 6, axis="X")
        for x in (-0.02, 1.10):
            g.cylinder((x, 0.95, 0.012), 0.016, 1.52, "Lib_Steel", 6)
        # Collars bite the post shell only, then reach the gate stiles.
        for y in (0.45, 1.05):
            g.box((-0.055, y, 0.012), (0.05, 0.04, 0.03), "Lib_Steel")
            g.box((1.155, y, 0.012), (0.07, 0.04, 0.03), "Lib_Steel")
        if lod == 0:
            for i in range(3):
                x0 = -1.05 + i * 0.28
                g.pipe((x0, 0.30, 0.008), (x0 + 0.22, 1.55, 0.008), 0.004, "Lib_Steel", 4)
            g.box((1.16, 1.10, 0.02), (0.10, 0.05, 0.04), "Lib_SteelDark")
        a.end()
    a.capsule("Col_PostL", (-1.22, 0.95, 0), 0.018, 1.40)
    a.capsule("Col_PostH", (-0.10, 0.95, 0), 0.018, 1.40)
    a.capsule("Col_PostR", (1.22, 0.95, 0), 0.018, 1.40)
    a.box("Col_Mesh", (-0.66, 0.95, 0), (0.72, 1.20, 0.004))
    a.box("Col_Gate", (0.56, 0.95, 0.012), (0.80, 1.10, 0.004))
    return a
