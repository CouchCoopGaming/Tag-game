"""One wooden distribution pole. Crossarm, pin insulators, pole-top can.

9.2 m timber. The transformer hangs under the arm. No second pole and no legend.
Climb colliders are capsules inside the taper, overlapped so the shaft has no gap.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

HEIGHT = 9.2
R0 = 0.17
R1 = 0.095
ARM_Y = 7.95


def _radius(y):
    return R0 + (R1 - R0) * (y / HEIGHT)


@register
def create():
    a = Asset(
        "WoodPole_Single",
        "StreetFurniture",
        "Wood pole, 9.2 m. Crossarm at 7.95 m, three pin insulators, transformer under the arm.",
    )
    a.climb_note = "Round timber, 34 cm at the ground. Capsules follow the taper. Not a flat cling."
    a.vault_note = "No rail. The arm is 7.95 m."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 12, 8)
        g.cone((0, HEIGHT * 0.5, 0), R0, R1, HEIGHT, "Lib_Bark", seg)
        # Two stubs. The pole hides the joint, and the solids do not intersect.
        g.box((-0.48, ARM_Y, 0.06), (0.74, 0.10, 0.12), "Lib_Wood", uv_scale=1.2)
        g.box((0.48, ARM_Y, 0.06), (0.74, 0.10, 0.12), "Lib_Wood", uv_scale=1.2)
        if lod == 0:
            g.pipe((-0.55, ARM_Y - 0.55, 0.0), (-0.18, ARM_Y - 0.08, 0.04), 0.014, "Lib_Steel", 6)
            g.pipe((0.55, ARM_Y - 0.55, 0.0), (0.18, ARM_Y - 0.08, 0.04), 0.014, "Lib_Steel", 6)
        for x in (-0.62, 0.0, 0.62):
            g.cylinder((x, ARM_Y + 0.082, 0.10), 0.022, 0.06, "Lib_SteelDark", 8)
            g.cone((x, ARM_Y + 0.154, 0.10), 0.040, 0.018, 0.08, "Lib_PaintCream", lod_pick(lod, 8, 6))
            if lod == 0:
                g.cylinder((x, ARM_Y + 0.206, 0.13), 0.006, 0.05, "Lib_Black", 6, axis="Z")
        # Can under the arm, on the street side. Caps and bushings stay clear of the can.
        g.box((0, 7.18, 0.16), (0.06, 0.18, 0.08), "Lib_SteelDark")
        g.cylinder((0, 6.85, 0.42), 0.20, 0.62, "Lib_Steel", seg, axis="X")
        g.cylinder((0.335, 6.85, 0.42), 0.22, 0.036, "Lib_SteelDark", seg, axis="X")
        g.cylinder((-0.335, 6.85, 0.42), 0.22, 0.036, "Lib_SteelDark", seg, axis="X")
        if lod == 0:
            for x in (-0.12, 0.12):
                g.cylinder((x, 7.085, 0.42), 0.018, 0.05, "Lib_PaintCream", 8)
                g.cylinder((x, 7.135, 0.42), 0.008, 0.03, "Lib_Steel", 6)
        a.end()
    # Short overlapped capsules. Radius is the wood at the top of each span, 6 mm in,
    # so the wide end of a 0.42 m span stays inside 1 cm.
    y0 = 0.06
    i = 0
    while y0 < 8.85:
        y1 = min(y0 + 0.42, 9.05)
        radius = max(0.05, _radius(y1) - 0.006)
        a.capsule("Col_Pole_%d" % i, (0, (y0 + y1) * 0.5, 0), radius, y1 - y0, 1)
        y0 += 0.30
        i += 1
    # Inner ends stop short of the pole so a ray toward +X does not graze the shaft.
    a.box("Col_ArmL", (-0.50, ARM_Y, 0.06), (0.66, 0.08, 0.08))
    a.box("Col_ArmR", (0.50, ARM_Y, 0.06), (0.66, 0.08, 0.08))
    a.capsule("Col_Can", (0, 6.85, 0.42), 0.19, 0.58, 0)
    a.box("Col_Hanger", (0, 7.18, 0.16), (0.04, 0.14, 0.06))
    return a
