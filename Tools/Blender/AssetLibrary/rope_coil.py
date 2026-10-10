"""Coil of rope, about 48 cm across and 16 cm tall."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("RopeCoil", "Harbor", "Three-turn rope coil, 48 cm across, 16 cm tall, with a loose tail. The center hole stays open.")
    a.climb_note = "Soft prop. Colliders follow the rope, so the middle of the coil is empty."
    a.vault_note = "Not a rail."
    for lod in (0, 1):
        g = a.begin(lod)
        major = lod_pick(lod, 14, 8)
        minor = lod_pick(lod, 6, 4)
        for i, y in enumerate((0.045, 0.085, 0.125)):
            rad = 0.16 - i * 0.012
            g.torus((0, y, 0), rad, 0.028, "Lib_Rust", major, minor)
        if lod == 0:
            g.pipe((0.10, 0.08, 0.04), (0.02, 0.13, 0.08), 0.016, "Lib_Rust", 6)
            g.pipe((0.02, 0.13, 0.08), (-0.06, 0.145, 0.02), 0.016, "Lib_Rust", 6)
        a.end()
    for i in range(8):
        ang = math.tau * i / 8.0
        a.capsule(
            "Col_Turn_%d" % i,
            (math.cos(ang) * 0.145, 0.085, math.sin(ang) * 0.145),
            0.03,
            0.15,
            1,
        )
    return a
