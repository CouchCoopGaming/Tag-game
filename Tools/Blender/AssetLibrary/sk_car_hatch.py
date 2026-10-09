"""Parked hatchback shell. Closed body, pillars, glass, and wrapped arches."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset
import sk_car_body as body


SPEC = {
    "paint": "Lib_PaintRed",
    "z0": -1.98,
    "z1": 2.02,
    "step": 0.06,
    "axles": (-1.25, 1.25),
    "wheel_axles": (-1.25, 1.25),
    "axle_y": 0.30,
    "tire_r": 0.30,
    "tire_x": 0.74,
    "tire_half_w": 0.095,
    "arch_r": 0.36,
    "body_x": 0.82,
    "flare": 0.07,
    "belt_y": 0.88,
    "belly_y": 0.17,
    "rocker_y": 0.27,
    "roof_y": 1.50,
    "roof_x": 0.64,
    "roof_z": (-0.85, 0.28),
    "roof_crown": 0.02,
    "inset": 0.15,
    "a_pillar_z": 0.24,
    "c_pillar_z": -0.78,
    "pillars": ((-0.22, 0.05),),
    "windows": ((-0.16, 0.20), (-0.72, -0.30)),
    "windshield": (0.26, 1.48, 0.58, 1.00),
    "rear_glass": (-0.82, 1.48, -1.48, 0.92),
    "crown": (
        (-1.98, 0.44, 0.74),
        (-1.72, 0.78, 0.72),
        (-1.48, 0.92, 0.70),
        (-0.85, 1.50, 0.64),
        (0.28, 1.50, 0.64),
        (0.58, 1.00, 0.72),
        (1.68, 0.66, 0.74),
        (2.02, 0.46, 0.74),
    ),
    "lamp_x": 0.52,
    "lamp_y": 0.56,
    "tail_y": 0.70,
    "bumper_y": 0.32,
    "bed_z0": None,
}


def _wheel_cols(a):
    r = SPEC["tire_r"]
    x = SPEC["tire_x"] + SPEC["tire_half_w"] * 0.15
    for i, z in enumerate(SPEC["wheel_axles"]):
        for j, sign in enumerate((-1.0, 1.0)):
            a.box("Col_Wheel_%d%d" % (i, j), (sign * x, SPEC["axle_y"], z), (0.016, r * 1.02, r * 1.02))


def create():
    a = Asset(
        "Car_Hatch",
        "StreetFurniture",
        "Hatchback shell, about 4.00 m long, roof 1.52 m. Closed body, pillars, glass, and fenders over the tires.",
    )
    a.climb_note = "Sheet metal. Not a cling wall."
    a.vault_note = "Roof is a landing, not a vault rail."
    for lod in (0, 1):
        g = a.begin(lod)
        body.build(g, SPEC, lod)
        a.end()
    a.box("Col_Body", (0, 0.52, -0.15), (1.00, 0.40, 1.40))
    a.box("Col_Roof", (0, 1.455, -0.30), (0.48, 0.04, 0.70))
    a.box("Col_Nose", (0, 0.42, 1.74), (0.90, 0.12, 0.28))
    a.box("Col_Tail", (0, 0.46, -1.74), (0.95, 0.12, 0.22))
    _wheel_cols(a)
    return a
