"""Removable bollard in a ground socket.

Real size: 114 mm post, 0.90 m tall, sitting in a 180 mm socket flush with the pavement.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Bollard_Removable",
        "StreetFurniture",
        "Removable bollard, 114 mm post, 0.90 m tall, in a flush steel socket.",
    )
    a.climb_note = "114 mm post. Not a cling."
    a.vault_note = "Top is 0.90 m and round. Not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 14, 8)
        bev = lod_pick(lod, 0.003, 0.0)
        # Socket ring overlaps the post so the post is seated, not floating.
        g.cylinder((0, 0.02, 0), 0.11, 0.04, "Lib_SteelDark", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.cylinder((0, 0.045, 0), 0.13, 0.012, "Lib_Steel", seg)
        g.cylinder((0, 0.46, 0), 0.057, 0.84, "Lib_PaintYellow", seg, bevel=bev, segs=1 if lod == 0 else 0)
        g.sphere((0, 0.88, 0), 0.057, "Lib_PaintYellow", seg)
        g.cylinder((0, 0.70, 0), 0.062, 0.04, "Lib_PaintWhite", seg)
        # Lift slot, cut into the upper post as a dark bar that overlaps it.
        g.box((0, 0.78, 0), (0.09, 0.018, 0.07), "Lib_Black")
        if lod == 0:
            g.cylinder((0, 0.055, 0.10), 0.008, 0.02, "Lib_Steel", 6, axis="Z")
            g.box((0.04, 0.10, 0), (0.04, 0.03, 0.02), "Lib_Rust")
        a.end()
    a.box("Col_Socket", (0, 0.02, 0), (0.14, 0.024, 0.14))
    a.capsule("Col_Post", (0, 0.46, 0), 0.044, 0.76, 1)
    return a
