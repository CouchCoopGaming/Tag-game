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
        "City trash can, about 0.92 m. Dark green slats, hoops, and flap, black liner, shallow lid.",
    )
    a.climb_note = "Not a cling surface."
    a.vault_note = "Too narrow to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 18, 10)
        slats = lod_pick(lod, 14, 10)
        # Black liner. Slats stand just off it with about a 2 cm gap.
        g.cylinder((0, 0.04, 0), 0.27, 0.08, "Lib_Iron", seg)
        g.cylinder((0, 0.42, 0), 0.162, 0.64, "Lib_Black", seg)
        # Pitch at r=0.198 is ~8.9 cm. Slat width 6.9 cm leaves a 2 cm slot.
        for i in range(slats):
            ang = 360.0 * i / slats
            if abs(((ang + 180.0) % 360.0) - 180.0) < 20.0:
                continue
            rad = math.radians(ang)
            x = 0.198 * math.sin(rad)
            z = 0.198 * math.cos(rad)
            g.box((x, 0.42, z), (0.069, 0.74, 0.046), "Lib_Iron", euler=(0, ang, 0))
        # Round hoops. Major 0.208 and minor 0.017 wrap the slats (outer face 0.221).
        ring = lod_pick(lod, 20, 12)
        tube = lod_pick(lod, 8, 6)
        g.torus((0, 0.24, 0), 0.208, 0.017, "Lib_Iron", ring, tube)
        g.torus((0, 0.62, 0), 0.208, 0.017, "Lib_Iron", ring, tube)
        g.cylinder((0, 0.782, 0), 0.250, 0.036, "Lib_Iron", seg)
        g.cylinder((0, 0.756, 0), 0.210, 0.018, "Lib_Black", seg)
        g.cylinder((0, 0.818, 0), 0.224, 0.028, "Lib_Iron", seg)
        g.cone((0, 0.850, 0), 0.14, 0.022, 0.048, "Lib_Iron", seg)
        g.cylinder((0, 0.882, 0), 0.026, 0.020, "Lib_Black", 8)
        g.box((0, 0.50, 0.200), (0.16, 0.16, 0.120), "Lib_Iron")
        # Flap is the same powder coat, with a dark hinge along its top edge.
        g.box((0, 0.48, 0.268), (0.11, 0.09, 0.032), "Lib_Iron", euler=(12, 0, 0))
        # Hinge sits on the flap's top edge, toward the camera, so it reads as a line.
        g.cylinder((0, 0.518, 0.286), 0.009, 0.12, "Lib_Black", 8, axis="X")
        a.end()
    # Base disk is y 0–0.08, radius 0.27. Liner is y 0.10–0.74, radius 0.162.
    a.box("Col_Base", (0, 0.046, 0), (0.36, 0.066, 0.36))
    a.box("Col_Body", (0, 0.400, 0), (0.18, 0.590, 0.18))
    return a
