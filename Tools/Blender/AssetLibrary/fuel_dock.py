"""Marine fuel dispenser. Sits on a dock; the pivot is the base."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "FuelDock",
        "Harbor",
        "Fuel dock pump, 0.48 m wide and 1.35 m tall. Hose and nozzle on +Z. Place the base on the dock deck.",
    )
    a.climb_note = "Cabinet. Not a cling wall."
    a.vault_note = "No rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        g.box((0, 0.02, 0), (0.52, 0.04, 0.40), "Lib_SteelDark")
        g.box((0, 0.59, 0), (0.42, 1.08, 0.32), "Lib_PaintWhite")
        g.box((0, 1.165, 0), (0.46, 0.05, 0.36), "Lib_SteelDark")
        g.box((0, 0.95, 0.175), (0.28, 0.14, 0.01), "Lib_PaintRed")
        if lod == 0:
            g.text("FUEL", (0, 0.95, 0.192), 0.07, "Lib_PaintWhite")
            g.torus((0.0, 0.72, 0.30), 0.09, 0.014, "Lib_Rubber", 12, 6)
            g.pipe((0.10, 0.64, 0.30), (0.16, 0.40, 0.30), 0.014, "Lib_Rubber", seg)
            g.box((0.16, 0.34, 0.30), (0.05, 0.08, 0.12), "Lib_Black")
            g.cylinder((0, 1.25, 0), 0.03, 0.10, "Lib_Steel", seg)
        a.end()
    a.box("Col_Cabinet", (0, 0.62, 0), (0.36, 1.00, 0.26))
    a.box("Col_Base", (0, 0.02, 0), (0.46, 0.03, 0.34))
    return a
