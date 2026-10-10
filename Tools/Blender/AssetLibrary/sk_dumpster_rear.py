"""4-yard rear-load dumpster. Trunnions, split lids, casters. Not the front-load Dumpster."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "Dumpster_Rear",
        "StreetFurniture",
        "Rear-load container 1.83 x 1.37 m, 1.28 m to the lid. Side trunnions, split lids, four casters.",
    )
    a.climb_note = "Side sheets are about 1.1 m and broken by ribs. Not a clean cling wall."
    a.vault_note = "Lid top is 1.28 m. Above the 0.90–1.05 m vault band."
    for lod in (0, 1):
        g = a.begin(lod)
        bev = lod_pick(lod, 0.008, 0.0)
        bs = lod_pick(lod, 2, 0)
        seg = lod_pick(lod, 12, 8)
        # Casters under the corners, then the tub sitting on a skirt.
        for x in (-0.78, 0.78):
            for z in (-0.52, 0.52):
                g.cylinder((x, 0.07, z), 0.07, 0.05, "Lib_Rubber", seg, axis="X")
        g.box((0, 0.16, 0), (1.70, 0.06, 1.20), "Lib_SteelDark", bevel=bev, segs=1)
        g.box((0, 0.68, 0), (1.78, 0.96, 1.28), "Lib_ContainerBlue", bevel=bev, segs=bs)
        # Corner angles and horizontal ribs sit proud of the shell.
        for x in (-0.90, 0.90):
            g.box((x, 0.68, 0), (0.04, 1.00, 1.32), "Lib_SteelDark", bevel=0.003 if lod == 0 else 0, segs=1)
        for z in (-0.66, 0.66):
            g.box((0, 0.68, z), (1.84, 1.00, 0.03), "Lib_SteelDark")
        for y in (0.40, 0.62, 0.84, 1.06):
            g.box((0, y, 0.655), (1.70, 0.025, 0.02), "Lib_Steel")
            g.box((0, y, -0.655), (1.70, 0.025, 0.02), "Lib_Steel")
        # Trunnion sleeves the truck grabs, one each side, near the top front.
        for x in (-1.0, 1.0):
            g.cylinder((x, 1.02, 0.28), 0.035, 0.10, "Lib_Steel", 8, axis="X")
            g.box((x * 0.90, 1.02, 0.28), (0.05, 0.07, 0.08), "Lib_SteelDark")
        # Split lids and a hinge rod along the back edge.
        g.cylinder((0, 1.20, -0.58), 0.018, 1.70, "Lib_Steel", 6, axis="X")
        g.box((-0.44, 1.24, 0.02), (0.86, 0.045, 1.16), "Lib_SteelDark", bevel=bev, segs=bs)
        g.box((0.44, 1.24, 0.02), (0.86, 0.045, 1.16), "Lib_SteelDark", bevel=bev, segs=bs)
        if lod == 0:
            for x in (-0.44, 0.44):
                for i in range(4):
                    g.box((x, 1.27, -0.30 + i * 0.22), (0.70, 0.012, 0.03), "Lib_Steel")
                g.box((x, 1.28, 0.35), (0.16, 0.03, 0.06), "Lib_Black")
            # Yellow and black stripes on the street face.
            for i, col in enumerate(("Lib_PaintYellow", "Lib_Black", "Lib_PaintYellow", "Lib_Black")):
                g.box((-0.36 + i * 0.24, 0.55, 0.70), (0.16, 0.22, 0.012), col, euler=(0, 0, 18))
        a.end()
    for i, x in enumerate((-0.78, 0.78)):
        for j, z in enumerate((-0.52, 0.52)):
            a.box("Col_Wheel_%d%d" % (i, j), (x, 0.07, z), (0.036, 0.09, 0.09))
    a.box("Col_Skirt", (0, 0.16, 0), (1.64, 0.05, 1.14))
    a.box("Col_Tub", (0, 0.68, 0), (1.72, 0.90, 1.22))
    a.box("Col_LidL", (-0.44, 1.24, 0.02), (0.80, 0.036, 1.10))
    a.box("Col_LidR", (0.44, 1.24, 0.02), (0.80, 0.036, 1.10))
    a.capsule("Col_TrunnionL", (-1.0, 1.02, 0.28), 0.028, 0.09, 0)
    a.capsule("Col_TrunnionR", (1.0, 1.02, 0.28), 0.028, 0.09, 0)
    return a
