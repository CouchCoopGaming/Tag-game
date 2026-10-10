"""Parked pickup shell. Closed cab, open bed, pillars, glass, and wrapped arches."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
import sk_car_body as body


SPEC = {
    "paint": "Lib_PaintWhite",
    "z0": -0.22,
    "z1": 2.48,
    "step": 0.06,
    "axles": (1.58,),
    "wheel_axles": (1.58,),
    "axle_y": 0.36,
    "tire_r": 0.36,
    "tire_x": 0.84,
    "tire_half_w": 0.11,
    "arch_r": 0.42,
    "body_x": 0.90,
    "flare": 0.10,
    "belt_y": 1.00,
    "belly_y": 0.26,
    "rocker_y": 0.34,
    "roof_y": 1.74,
    "roof_x": 0.70,
    "roof_z": (0.05, 0.78),
    "roof_crown": 0.02,
    "inset": 0.14,
    "a_pillar_z": 0.72,
    "c_pillar_z": 0.08,
    "pillars": (),
    "windows": ((0.16, 0.66),),
    "windshield": (0.74, 1.72, 1.22, 1.12),
    "rear_glass": (-0.16, 1.58, -0.30, 1.18),
    "crown": (
        (-0.22, 1.74, 0.70),
        (0.05, 1.74, 0.70),
        (0.78, 1.74, 0.70),
        (1.22, 1.12, 0.78),
        (2.10, 0.88, 0.82),
        (2.48, 0.55, 0.84),
    ),
    "lamp_x": 0.62,
    "lamp_y": 0.70,
    "tail_y": 0.85,
    "bumper_y": 0.40,
    "bed_z0": -2.58,
    "bed_z1": -0.06,
    "bed_floor_y": 0.55,
    "bed_rail_y": 1.12,
    "bed_x": 0.86,
    "bed_axle": -1.62,
}


def _wheel_cols(a, axles, tag):
    body.wheel_boxes(a, SPEC, axles, lambda i, j: "Col_%s_%d%d" % (tag, i, j))


@register
def create():
    a = Asset(
        "Pickup_FullSize_25",
        "Vehicles",
        "2025 full-size pickup, about 5.06 m long. Cab roof 1.76 m, bed rails 1.12 m. Closed cab and wrapped fenders.",
    )
    a.climb_note = "Sheet metal. Not a cling wall."
    a.vault_note = "Bed sides are about 1.12 m. Not a vault rail."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        body.build(g, SPEC, lod)
        a.end()
    a.box("Col_Cab", (0, 0.82, 0.78), (0.90, 0.48, 1.00))
    a.box("Col_Roof", (0, 1.70, 0.40), (0.36, 0.04, 0.32))
    a.box("Col_Bed", (0, 0.55, -1.20), (1.20, 0.02, 1.60))
    a.box("Col_SideL", (-0.845, 0.98, -1.30), (0.02, 0.12, 1.70))
    a.box("Col_SideR", (0.845, 0.98, -1.30), (0.02, 0.12, 1.70))
    # Cab ends at z = 1.28 and the front tires at z = 1.625. The nose sheet
    # continues to about z = 2.50. These stay inside that skin, clear of the
    # wheel wells (inner face x = 0.58, well ends near z = 1.97).
    a.box("Col_Hood", (0, 0.66, 1.62), (0.84, 0.32, 0.84))
    a.box("Col_Nose", (0, 0.43, 1.81), (0.84, 0.18, 1.22))
    a.box("Col_FenderL", (-0.64, 0.46, 2.18), (0.28, 0.12, 0.36))
    a.box("Col_FenderR", (0.64, 0.46, 2.18), (0.28, 0.12, 0.36))
    # Bumper face is a separate skin past the loft cap. This slab sits in that
    # cap only, so it does not share volume with the shell.
    a.box("Col_Bumper", (0, 0.40, 2.488), (1.20, 0.08, 0.010))
    # The panel runs down to the bed. The old box stopped at y=0.73, leaving
    # 0.17 m of open air above the bed collider (top 0.56). Bottom is now 0.585,
    # a 2.5 cm gap, inside the sheet.
    a.box("Col_Tailgate", (0, 0.7875, -2.568), (0.90, 0.405, 0.014))
    _wheel_cols(a, (1.58,), "Fr")
    _wheel_cols(a, (-1.62,), "Rr")
    return a
