"""Highway mast-arm luminaire. Lamp head at 9.7 m, arm reaches 4.7 m over the lane."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from sk_parts import bolt_ring, polyline, span_collider


@register
def create():
    a = Asset(
        "LightPost_Mast",
        "StreetFurniture",
        "Galvanized mast arm. Pole to 9.6 m, cobra head 4.7 m out at 9.7 m. Arm rises 0.22 m.",
    )
    a.climb_note = "Tapered round pole, 32 cm at the base. Not a cling wall."
    a.vault_note = "No rail. The arm is overhead."
    arm_a = (0.0, 9.28, 0.12)
    arm_b = (0.0, 9.58, 4.56)
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 16, 8)
        bev = lod_pick(lod, 0.004, 0.0)
        bs = lod_pick(lod, 1, 0)
        g.box((0, 0.016, 0), (0.52, 0.032, 0.52), "Lib_SteelDark", bevel=bev, segs=bs)
        if lod == 0:
            bolt_ring(g, (0, 0.04, 0), 0.20, 4, 0.016, 0.02, "Lib_Steel")
        g.cylinder((0, 0.10, 0), 0.18, 0.12, "Lib_Steel", seg, bevel=bev, segs=bs)
        g.cone((0, 4.82, 0), 0.155, 0.068, 9.40, "Lib_Steel", seg)
        g.cylinder((0, 1.55, 0), 0.145, 0.08, "Lib_PaintYellow", seg)
        if lod == 0:
            g.box((0.145, 0.70, 0), (0.016, 0.22, 0.12), "Lib_SteelDark", bevel=0.002, segs=1)
            g.box((0.156, 0.70, 0), (0.008, 0.16, 0.08), "Lib_Black")
        g.cylinder((0, 9.22, 0), 0.08, 0.10, "Lib_SteelDark", seg)
        polyline(g, [arm_a, arm_b], 0.045, "Lib_Steel", max(6, seg // 2))
        if lod == 0:
            g.torus((0, 9.28, 0.20), 0.055, 0.01, "Lib_SteelDark", 12, 5)
        hx, hy, hz = 0.0, 9.62, 4.85
        g.box((hx, hy, hz), (0.36, 0.16, 0.62), "Lib_SteelDark", bevel=max(bev, 0.006), segs=max(bs, 1))
        g.box((hx, 9.528, hz), (0.26, 0.012, 0.40), "Lib_Lamp")
        g.box((hx, hy - 0.02, 5.22), (0.40, 0.035, 0.08), "Lib_Steel")
        if lod == 0:
            g.cylinder((hx, hy + 0.11, hz - 0.12), 0.025, 0.04, "Lib_Black", 8)
            g.box((hx, 9.715, hz), (0.40, 0.016, 0.66), "Lib_Steel")
        a.end()
    a.box("Col_Base", (0, 0.016, 0), (0.48, 0.028, 0.48))
    a.capsule("Col_PoleLow", (0, 1.70, 0), 0.12, 3.20, 1)
    a.capsule("Col_PoleHigh", (0, 6.30, 0), 0.060, 6.40, 1)
    arm_dx = arm_b[0] - arm_a[0]
    arm_dy = arm_b[1] - arm_a[1]
    arm_dz = arm_b[2] - arm_a[2]
    arm_len = (arm_dx * arm_dx + arm_dy * arm_dy + arm_dz * arm_dz) ** 0.5
    arm_pad = 0.12 / arm_len
    arm_c0 = (arm_a[0] + arm_dx * arm_pad, arm_a[1] + arm_dy * arm_pad, arm_a[2] + arm_dz * arm_pad)
    arm_c1 = (arm_b[0] - arm_dx * arm_pad, arm_b[1] - arm_dy * arm_pad, arm_b[2] - arm_dz * arm_pad)
    center, size, euler = span_collider(arm_c0, arm_c1, 0.028)
    a.box("Col_Arm", center, size, euler=euler)
    a.box("Col_Head", (0, 9.62, 4.85), (0.32, 0.13, 0.54))
    return a
