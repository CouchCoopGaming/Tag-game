"""Park lamp. 3.2 m post and a framed glass lantern."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset("ParkLamp", "Park", "Path lamp, 3.20 m to the lantern cap. Four-post glass lantern with a bulb inside.")
    a.climb_note = "Post is 9 cm. Not a cling wall."
    a.vault_note = "No rail."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 10, 6)
        # Tapered cast base, 36 cm tall, 40 cm across the foot.
        g.box((0, 0.03, 0), (0.40, 0.06, 0.40), "Lib_Black", bevel=0.008 if lod == 0 else 0, segs=1)
        g.cone((0, 0.22, 0), 0.16, 0.050, 0.32, "Lib_Black", seg)
        g.cylinder((0, 1.70, 0), 0.045, 2.70, "Lib_Black", seg)
        # Collar where the post meets the lantern.
        g.cylinder((0, 2.86, 0), 0.07, 0.06, "Lib_SteelDark", seg)
        g.box((0, 2.90, 0), (0.32, 0.04, 0.32), "Lib_Steel")
        for x in (-0.13, 0.13):
            for z in (-0.13, 0.13):
                g.box((x, 3.06, z), (0.028, 0.30, 0.028), "Lib_SteelDark")
        # Inset panes, not an emissive sheet through the middle.
        if lod == 0:
            g.box((0, 3.06, 0.128), (0.20, 0.22, 0.008), "Lib_ShopGlass")
            g.box((0, 3.06, -0.128), (0.20, 0.22, 0.008), "Lib_ShopGlass")
            g.box((0.128, 3.06, 0), (0.008, 0.22, 0.20), "Lib_ShopGlass")
            g.box((-0.128, 3.06, 0), (0.008, 0.22, 0.20), "Lib_ShopGlass")
            g.sphere((0, 3.06, 0), 0.045, "Lib_Lamp", 10)
        g.box((0, 3.22, 0), (0.36, 0.06, 0.36), "Lib_SteelDark")
        g.cone((0, 3.32, 0), 0.20, 0.02, 0.12, "Lib_Steel", seg)
        a.end()
    a.box("Col_Base", (0, 0.16, 0), (0.14, 0.18, 0.14))
    a.capsule("Col_Post", (0, 1.72, 0), 0.036, 2.46, 1)
    a.box("Col_Lantern", (0, 3.22, 0), (0.30, 0.04, 0.30))
    a.box("Col_Cap", (0, 3.32, 0), (0.08, 0.06, 0.08))
    return a
