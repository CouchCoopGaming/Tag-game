"""Drinking fountain. Pedestal 1.05 m, basin and bubbler."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("Fountain", "Park", "Pedestal drinking fountain, 1.05 m to the bubbler, round basin.")
    a.climb_note = "Not a cling."
    a.vault_note = "Too narrow."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        g.cylinder((0, 0.04, 0), 0.28, 0.08, "Lib_Concrete", seg, uv_scale=0.8)
        g.cylinder((0, 0.50, 0), 0.10, 0.84, "Lib_Concrete", seg, uv_scale=0.8)
        g.cylinder((0, 0.92, 0), 0.22, 0.08, "Lib_Steel", seg)
        g.torus((0, 0.90, 0), 0.16, 0.025, "Lib_Steel", 14 if lod == 0 else 8, 6)
        g.cylinder((0.10, 1.00, 0), 0.015, 0.10, "Lib_Brass", 6)
        g.sphere((0.10, 1.05, 0), 0.02, "Lib_Brass", 6)
        a.end()
    a.capsule("Col_Pedestal", (0, 0.50, 0), 0.10, 0.92, 1)
    a.box("Col_Basin", (0, 0.92, 0), (0.28, 0.08, 0.28))
    return a
