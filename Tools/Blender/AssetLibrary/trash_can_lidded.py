"""City trash can. Slatted body, rim, shallow lid, side flap. About 0.92 m."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


@register
def create():
    a = Asset(
        "TrashCan_Lidded",
        "StreetFurniture",
        "City trash can, about 0.92 m. Slatted steel body, liner ring, shallow lid, and a side flap.",
    )
    a.climb_note = "Not a cling surface."
    a.vault_note = "Too narrow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 18, 10)
        slats = lod_pick(lod, 12, 8)
        # Base, liner, and slats that bite into the liner.
        g.cylinder((0, 0.04, 0), 0.26, 0.08, "Lib_Steel", seg)
        g.cylinder((0, 0.42, 0), 0.195, 0.70, "Lib_SteelDark", seg)
        for i in range(slats):
            ang = 360.0 * i / slats
            # Leave a mouth on +Z for the flap.
            if abs(((ang + 180.0) % 360.0) - 180.0) < 40.0:
                continue
            rad = math.radians(ang)
            x = 0.205 * math.sin(rad)
            z = 0.205 * math.cos(rad)
            g.box((x, 0.42, z), (0.042, 0.62, 0.022), "Lib_Steel", euler=(0, ang, 0))
        # Bands sit proud of the slats, clear of the body collider rings.
        g.cylinder((0, 0.28, 0), 0.232, 0.028, "Lib_SteelDark", seg)
        g.cylinder((0, 0.58, 0), 0.232, 0.028, "Lib_SteelDark", seg)
        # Lip, and a rubber liner ring that shows just under it.
        g.cylinder((0, 0.782, 0), 0.255, 0.036, "Lib_Steel", seg)
        g.cylinder((0, 0.756, 0), 0.222, 0.020, "Lib_Rubber", seg)
        # Flat lid, then a low cone. The cone starts above the lid's middle
        # so the body collider's top pole sits in the lid alone.
        g.cylinder((0, 0.818, 0), 0.228, 0.028, "Lib_Steel", seg)
        g.cone((0, 0.850, 0), 0.14, 0.022, 0.048, "Lib_Steel", seg)
        g.cylinder((0, 0.882, 0), 0.026, 0.020, "Lib_SteelDark", 8)
        # Side flap. The frame overlaps the liner; the door collider is
        # the outer part of the frame, clear of the liner shell.
        g.box((0, 0.50, 0.220), (0.22, 0.16, 0.100), "Lib_Steel")
        g.box((0, 0.48, 0.278), (0.15, 0.09, 0.020), "Lib_Rubber", euler=(12, 0, 0))
        if lod == 0:
            g.cylinder((0, 0.575, 0.262), 0.008, 0.16, "Lib_SteelDark", 8, axis="X")
        a.end()
    a.capsule("Col_Body", (0, 0.427, 0), 0.17, 0.774, 1)
    a.box("Col_Door", (0, 0.50, 0.240), (0.16, 0.10, 0.024))
    return a
