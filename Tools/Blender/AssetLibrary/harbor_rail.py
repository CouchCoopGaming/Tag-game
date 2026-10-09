"""Harbor railing module. 2.0 m long, top rail at 1.05 m."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "HarborRail",
        "Harbor",
        "Railing bay 2.0 m long. Posts 5 cm, top rail at 1.05 m, mid rail, and a kick plate. Place end to end along X.",
    )
    a.vaultable = True
    a.vault_height = 1.05
    a.climb_note = "Posts are 5 cm. Not a cling wall."
    a.vault_note = "Vault_Rail is the top bar. Rail top is 1.05 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        for x in (-0.95, 0.95):
            g.cylinder((x, 0.55, 0), 0.025, 1.10, "Lib_Steel", seg)
            g.box((x, 0.02, 0), (0.08, 0.04, 0.08), "Lib_SteelDark")
        g.pipe((-0.95, 1.05, 0), (0.95, 1.05, 0), 0.02, "Lib_Steel", seg)
        g.pipe((-0.95, 0.55, 0), (0.95, 0.55, 0), 0.015, "Lib_Steel", seg)
        g.box((0, 0.08, 0), (1.80, 0.10, 0.012), "Lib_SteelDark")
        if lod == 0:
            g.pipe((-0.95, 0.78, 0), (0.95, 0.78, 0), 0.01, "Lib_SteelDark", 6)
        a.end()
    a.capsule("Col_PostL", (-0.95, 0.55, 0), 0.025, 1.10, 1)
    a.capsule("Col_PostR", (0.95, 0.55, 0), 0.025, 1.10, 1)
    a.capsule("Vault_Rail", (0, 1.05, 0), 0.02, 1.90, 0)
    a.capsule("Col_Mid", (0, 0.55, 0), 0.015, 1.90, 0)
    a.box("Col_Kick", (0, 0.08, 0), (1.80, 0.10, 0.012))
    return a
