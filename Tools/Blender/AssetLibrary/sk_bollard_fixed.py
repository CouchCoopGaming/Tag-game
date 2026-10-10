"""Fixed steel bollard. Flange, anchor bolts, domed pipe.

152 mm outside diameter. Dome crown at 0.98 m. No legend.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Bollard_Fixed",
        "StreetFurniture",
        "Fixed steel bollard, 152 mm pipe, dome at 0.98 m, bolted flange.",
    )
    a.climb_note = "152 mm round post. Not a cling."
    a.vault_note = "Top is 0.98 m and domed. Not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        g.box((0, 0.012, 0), (0.28, 0.024, 0.28), "Lib_SteelDark")
        if lod == 0:
            for i in range(4):
                ang = math.pi * 0.25 + i * math.pi * 0.5
                g.cylinder((math.sin(ang) * 0.10, 0.030, math.cos(ang) * 0.10), 0.008, 0.010, "Lib_Steel", 6)
        # Pipe is split so the band, the dark ring, and the dome do not intersect it.
        g.cylinder((0, 0.155, 0), 0.076, 0.230, "Lib_Steel", seg)
        if lod == 0:
            g.cylinder((0, 0.280, 0), 0.082, 0.016, "Lib_SteelDark", seg)
        g.cylinder((0, 0.444, 0), 0.076, 0.308, "Lib_Steel", seg)
        g.cylinder((0, 0.620, 0), 0.082, 0.040, "Lib_PaintYellow", seg)
        g.cylinder((0, 0.734, 0), 0.076, 0.184, "Lib_Steel", seg)
        g.sphere((0, 0.904, 0), 0.076, "Lib_Steel", seg)
        a.end()
    a.box("Col_Flange", (0, 0.010, 0), (0.20, 0.016, 0.20))
    a.capsule("Col_Post", (0, 0.45, 0), 0.068, 0.74, 1)
    return a
