"""Short quay davit. Base on the deck, jib reaches toward +Z."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "QuayDavit",
        "Harbor",
        "Quay davit. 0.7 m base, post to 2.15 m, jib about 1.7 m toward +Z with a hook. Set the base on a deck.",
    )
    a.climb_note = "The post is a 0.14 m tube, not a cling wall."
    a.vault_note = "No rail. The jib is overhead."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        g.box((0, 0.04, 0), (0.72, 0.08, 0.72), "Lib_SteelDark")
        g.cylinder((0, 0.12, 0), 0.16, 0.06, "Lib_Steel", seg)
        g.cylinder((0, 1.15, 0), 0.07, 2.04, "Lib_Steel", seg)
        g.box((0, 2.225, 0.15), (0.16, 0.08, 0.24), "Lib_SteelDark")
        g.box((0, 2.296, 0.85), (0.07, 0.055, 1.50), "Lib_CraneYellow")
        g.pipe((0, 1.40, 0.12), (0, 2.16, 0.70), 0.02, "Lib_Steel", seg)
        if lod == 0:
            g.pipe((0, 2.24, 1.55), (0, 1.52, 1.55), 0.010, "Lib_SteelDark", 5)
            g.torus((0, 1.42, 1.55), 0.045, 0.010, "Lib_Steel", 10, 6)
        a.end()
    a.box("Col_Base", (0, 0.04, 0), (0.64, 0.06, 0.64))
    a.capsule("Col_Post", (0, 1.15, 0), 0.05, 1.70, 1)
    a.box("Col_Head", (0, 2.225, 0.15), (0.10, 0.05, 0.16))
    a.box("Col_Jib", (0, 2.296, 0.85), (0.04, 0.03, 1.20))
    return a
