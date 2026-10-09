"""Parked pickup shell. Closed cab, open bed, pillars, glass, and wrapped arches."""

import os
import sys

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
        "Car_Pickup_25",
        "StreetFurniture",
        "2025 pickup shell, about 5.06 m long. Cab roof 1.76 m, bed rails 1.12 m. Closed cab and wrapped fenders.",
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
    a.box("Col_Tailgate", (0, 0.86, -2.568), (0.90, 0.26, 0.014))
    _wheel_cols(a, (1.58,), "Fr")
    _wheel_cols(a, (-1.62,), "Rr")
    return a
