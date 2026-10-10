"""Chain-link gate. 1.2 m wide, 1.8 m tall, latch on +X."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from chain_fence import _diamonds
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("ChainGate", "Buildings", "Gate leaf 1.20 m wide, 1.80 m tall. Hinge on -X, latch on +X. Pair it with ChainFence.")
    a.climb_note = "Same wire slab as the fence panel, inset to the leaf."
    a.vault_note = "Top is 1.80 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        for x in (-0.55, 0.55):
            g.cylinder((x, 0.90, 0), 0.025, 1.80, "Lib_Steel", seg)
        g.pipe((-0.55, 1.70, 0), (0.55, 1.70, 0), 0.016, "Lib_Steel", seg)
        g.pipe((-0.55, 0.12, 0), (0.55, 0.12, 0), 0.014, "Lib_SteelDark", seg)
        g.pipe((-0.55, 0.90, 0), (0.55, 0.90, 0), 0.014, "Lib_SteelDark", seg)
        # Local diamond grid scaled by using the helper's full span would be too wide.
        # Draw a tighter set inline.
        step = 0.24 if lod == 0 else 0.40
        x = -0.42
        while x < 0.42:
            y = 0.24
            while y < 1.55:
                g.pipe((x, y, 0), (min(x + step, 0.42), min(y + step, 1.55), 0), 0.005, "Lib_Chain", 4)
                y += step
            x += step
        g.box((0.62, 0.95, 0.04), (0.06, 0.16, 0.04), "Lib_SteelDark")
        a.end()
    a.capsule("Col_Hinge", (-0.55, 0.90, 0), 0.025, 1.80, 1)
    a.capsule("Col_LatchStile", (0.55, 0.90, 0), 0.025, 1.80, 1)
    a.capsule("Col_Top", (0, 1.70, 0), 0.016, 1.10, 0)
    a.box("Col_Fabric", (0, 0.95, 0), (1.05, 1.50, 0.012), approx=True)
    return a
