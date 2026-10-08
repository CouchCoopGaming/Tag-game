"""Parked sedan shell. Closed body, pillars, glass, and wrapped arches."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register
import sk_car_body as body


SPEC = {
    "paint": "Lib_PaintBlue",
    "z0": -2.28,
    "z1": 2.28,
    "step": 0.06,
    "axles": (-1.35, 1.35),
    "wheel_axles": (-1.35, 1.35),
    "axle_y": 0.31,
    "tire_r": 0.31,
    "tire_x": 0.78,
    "tire_half_w": 0.10,
    "arch_r": 0.37,
    "body_x": 0.86,
    "flare": 0.07,
    "belt_y": 0.86,
    "belly_y": 0.18,
    "rocker_y": 0.28,
    "roof_y": 1.45,
    "roof_x": 0.66,
    "roof_z": (-1.18, 0.38),
    "roof_crown": 0.02,
    "inset": 0.15,
    "a_pillar_z": 0.34,
    "c_pillar_z": -1.08,
    "pillars": ((-0.32, 0.055),),
    "windows": ((-0.26, 0.30), (-1.02, -0.40)),
    "windshield": (0.36, 1.43, 0.74, 0.98),
    "rear_glass": (-1.16, 1.42, -1.68, 0.92),
    "crown": (
        (-2.28, 0.46, 0.78),
        (-2.02, 0.74, 0.76),
        (-1.68, 0.92, 0.74),
        (-1.18, 1.45, 0.66),
        (0.38, 1.45, 0.66),
        (0.74, 0.98, 0.74),
        (1.92, 0.70, 0.76),
        (2.28, 0.48, 0.78),
    ),
    "lamp_x": 0.58,
    "lamp_y": 0.58,
    "tail_y": 0.62,
    "bumper_y": 0.32,
    "bed_z0": None,
}


def _wheel_cols(a):
    r = SPEC["tire_r"]
    x = SPEC["tire_x"] + SPEC["tire_half_w"] * 0.55
    for i, z in enumerate(SPEC["wheel_axles"]):
        for j, sign in enumerate((-1.0, 1.0)):
            a.box("Col_Wheel_%d%d" % (i, j), (sign * x, SPEC["axle_y"], z), (0.016, r * 1.15, r * 1.15))


@register
def create():
    a = Asset(
        "Car_Sedan",
        "StreetFurniture",
        "Sedan shell, about 4.56 m long, roof 1.47 m. Closed body, pillars, glass, and fenders over the tires.",
    )
    a.climb_note = "Sheet metal. Not a cling wall."
    a.vault_note = "Hood and roof are landings, not vault rails."
    for lod in (0, 1):
        g = a.begin(lod)
        body.build(g, SPEC, lod)
        a.end()
    a.box("Col_Body", (0, 0.50, -0.20), (1.05, 0.40, 1.70))
    a.box("Col_Roof", (0, 1.385, -0.40), (0.70, 0.05, 1.00))
    a.box("Col_Nose", (0, 0.46, 1.95), (1.15, 0.18, 0.40))
    a.box("Col_Tail", (0, 0.48, -1.95), (1.15, 0.18, 0.40))
    _wheel_cols(a)
    return a
