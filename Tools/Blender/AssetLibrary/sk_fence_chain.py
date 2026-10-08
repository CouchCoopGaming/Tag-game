"""Chain-link gate bay. Same diamond cutout as Fence_ChainWeave.

The leaf hangs on two hinges and closes with a fork latch.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import polyline

_UV = 1.0 / (0.0508 * (2.0 ** 0.5))


def _post(g, x, lod):
    seg = lod_pick(lod, 8, 6)
    g.cylinder((x, 0.98, 0), 0.030, 1.96, "Lib_Steel", seg)
    g.cylinder((x, 0.025, 0), 0.065, 0.05, "Lib_Concrete", 8)
    g.sphere((x, 1.98, 0), 0.038, "Lib_Steel", seg)


def _band(g, x, y):
    g.cylinder((x, y, 0), 0.036, 0.014, "Lib_Steel", 8)
    g.cylinder((x, y, 0.034), 0.005, 0.018, "Lib_Steel", 6, axis="Z")
    g.cylinder((x, y, 0.045), 0.009, 0.004, "Lib_SteelDark", 6, axis="Z")


@register
def create():
    a = Asset(
        "Fence_ChainGate",
        "StreetFurniture",
        "Chain-link gate bay 2.44 m. Diamond cutout, two hinges, fork latch. Top of the leaf at 1.78 m.",
    )
    a.climb_note = "The fabric is a cutout sheet. Not a solid cling."
    a.vault_note = "Header is 1.90 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        _post(g, -1.22, lod)
        _post(g, -0.06, lod)
        _post(g, 1.22, lod)
        # Header over the opening, and a rail over the fixed panel.
        g.cylinder((-0.64, 1.83, 0), 0.016, 1.16, "Lib_Steel", seg, axis="X")
        g.cylinder((0.58, 1.90, 0), 0.016, 1.28, "Lib_Steel", seg, axis="X")
        g.cylinder((-0.64, 0.05, 0.016), 0.0035, 1.10, "Lib_Steel", 6, axis="X")
        # Fixed panel. The sheet runs into both posts.
        g.box((-0.63, 0.95, 0.016), (1.10, 1.76, 0.016), "Lib_ChainMesh", bevel=0, uv_scale=_UV)
        g.box((-1.16, 0.95, 0.016), (0.008, 1.68, 0.008), "Lib_SteelDark")
        g.box((-0.12, 0.95, 0.016), (0.008, 1.68, 0.008), "Lib_SteelDark")
        for x in (-1.22, -0.06):
            for y in (0.40, 1.05, 1.60):
                _band(g, x, y)
        # Gate frame. Stiles clear the hinge post and the latch post.
        g.cylinder((0.06, 0.93, 0), 0.016, 1.72, "Lib_Steel", seg)
        g.cylinder((1.06, 0.93, 0), 0.016, 1.72, "Lib_Steel", seg)
        g.cylinder((0.56, 1.76, 0), 0.014, 1.04, "Lib_Steel", seg, axis="X")
        g.cylinder((0.56, 0.10, 0), 0.014, 1.04, "Lib_Steel", seg, axis="X")
        g.box((0.56, 0.93, 0.016), (0.92, 1.58, 0.016), "Lib_ChainMesh", bevel=0, uv_scale=_UV)
        g.box((0.12, 0.93, 0.016), (0.008, 1.56, 0.008), "Lib_SteelDark")
        g.box((1.00, 0.93, 0.016), (0.008, 1.56, 0.008), "Lib_SteelDark")
        # Two hinges: a leaf across the gap and a barrel the pin runs through.
        for y in (0.42, 1.40):
            g.box((-0.00, y, 0.030), (0.14, 0.040, 0.008), "Lib_Steel")
            g.cylinder((-0.00, y, 0.042), 0.014, 0.055, "Lib_SteelDark", 8)
        # Fork latch. The prongs pass over a strike pin on the latch post.
        g.pipe((1.06, 1.00, 0.040), (1.20, 1.00, 0.040), 0.006, "Lib_Steel", 4)
        g.pipe((1.06, 1.10, 0.040), (1.20, 1.10, 0.040), 0.006, "Lib_Steel", 4)
        g.pipe((1.08, 1.00, 0.040), (1.08, 1.22, 0.030), 0.007, "Lib_Steel", 4)
        g.box((1.20, 1.05, 0.040), (0.05, 0.016, 0.016), "Lib_Steel")
        g.cylinder((1.175, 1.05, 0.040), 0.008, 0.055, "Lib_SteelDark", 6)
        if lod == 0:
            x = -1.00
            while x <= -0.28:
                polyline(g, [(x, 1.72, 0.020), (x, 1.84, 0.0), (x, 1.72, -0.008)], 0.003, "Lib_Chain", 4)
                x += 0.18
            x = 0.22
            while x <= 0.90:
                polyline(g, [(x, 1.64, 0.020), (x, 1.76, 0.0), (x, 1.64, -0.008)], 0.003, "Lib_Chain", 4)
                x += 0.18
        a.end()
    a.capsule("Col_PostL", (-1.22, 0.95, 0), 0.016, 1.50)
    a.capsule("Col_PostH", (-0.06, 0.95, 0), 0.016, 1.50)
    a.capsule("Col_PostR", (1.22, 0.95, 0), 0.016, 1.50)
    a.box("Col_Fixed", (-0.62, 0.95, 0.016), (0.70, 1.30, 0.010))
    a.box("Col_Gate", (0.56, 0.95, 0.016), (0.60, 1.10, 0.010))
    return a
