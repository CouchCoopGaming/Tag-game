"""Fire escape. Ladder to a platform at 3.05 m, rail 1.05 m above the deck."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "FireEscape",
        "Buildings",
        "Ladder and a 1.30 m platform at 3.05 m. Rail is 1.05 m above the deck. Place -Z against a wall.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = True
    a.vault_height = 1.05
    a.climb_note = "Ladder rails and rungs are Climb_*. They run from 0.20 m to the deck."
    a.vault_note = "Vault_Rail is the platform handrail. Top of rail is 1.05 m above the deck (world y = 4.10)."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6, 4)
        # Ladder, centered so the ground footprint stays near the origin.
        for x in (-0.28, 0.28):
            g.pipe((x, 0.02, -0.15), (x, 3.05, -0.15), 0.025, "Lib_SteelDark", seg)
        rungs = lod_pick(lod, 10, 6, 4)
        for i in range(rungs):
            y = 0.35 + i * (2.55 / max(1, rungs - 1))
            g.pipe((-0.28, y, -0.15), (0.28, y, -0.15), 0.012, "Lib_Steel", seg)
        # Platform slats.
        slats = lod_pick(lod, 7, 4, 2)
        for i in range(slats):
            z = -0.05 + i * (0.7 / max(1, slats - 1))
            g.box((0, 3.05, z), (1.20, 0.04, 0.08), "Lib_Steel", bevel=0.003 if lod == 0 else 0, segs=1)
        g.box((0, 3.02, 0.30), (1.30, 0.04, 0.08), "Lib_SteelDark")
        # Rail at 1.05 above the deck.
        for x in (-0.62, 0.62):
            g.pipe((x, 3.08, -0.05), (x, 3.08, 0.62), 0.02, "Lib_SteelDark", seg)
            g.pipe((x, 3.08, 0.62), (x, 4.10, 0.62), 0.02, "Lib_Steel", seg)
        g.pipe((-0.62, 4.10, 0.62), (0.62, 4.10, 0.62), 0.02, "Lib_Steel", seg)
        g.pipe((-0.62, 3.55, 0.62), (0.62, 3.55, 0.62), 0.015, "Lib_SteelDark", seg)
        a.end()
    for i, x in enumerate((-0.28, 0.28)):
        a.capsule("Climb_Rail_%d" % i, (x, 1.60, -0.15), 0.025, 2.90, 1)
    for i in range(10):
        y = 0.35 + i * (2.55 / 9.0)
        a.capsule("Climb_Rung_%d" % i, (0, y, -0.15), 0.012, 0.56, 0)
    a.box("Col_Deck", (0, 3.05, 0.28), (1.20, 0.05, 0.70))
    a.capsule("Vault_Rail", (0, 4.10, 0.62), 0.02, 1.24, 0)
    for i, x in enumerate((-0.62, 0.62)):
        a.capsule("Col_Post_%d" % i, (x, 3.59, 0.62), 0.02, 1.02, 1)
    return a
