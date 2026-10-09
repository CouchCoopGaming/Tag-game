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
    "belt_y": 0.88,
    "belly_y": 0.18,
    "rocker_y": 0.28,
    "roof_y": 1.56,
    "roof_x": 0.70,
    "roof_z": (-1.55, 0.62),
    "roof_crown": 0.012,
    "inset": 0.15,
    "a_pillar_z": 0.52,
    "c_pillar_z": -1.52,
    "pillars": ((-0.32, 0.045), (-1.16, 0.04)),
    "windows": ((-0.22, 0.44), (-1.06, -0.42), (-1.46, -1.24)),
    "windshield": (0.58, 1.56, 0.98, 1.04),
    "rear_glass": (-1.30, 1.54, -1.70, 1.04),
    "crown": (
        (-2.28, 0.50, 0.78),
        (-2.00, 0.78, 0.76),
        (-1.70, 1.04, 0.74),
        (-1.30, 1.56, 0.70),
        (0.58, 1.56, 0.70),
        (0.98, 1.04, 0.74),
        (1.62, 0.74, 0.76),
        (2.28, 0.50, 0.78),
    ),
    "lamp_x": 0.58,
    "lamp_y": 0.58,
    "tail_y": 0.62,
    "bumper_y": 0.32,
    "bed_z0": None,
}


def _wheel_cols(a):
    body.wheel_boxes(a, SPEC, SPEC["wheel_axles"], lambda i, j: "Col_Wheel_%d%d" % (i, j))


@register
def create():
    a = Asset(
        "Car_Sedan",
        "StreetFurniture",
        "Sedan shell, about 4.56 m long, roof 1.56 m. Taller greenhouse, shorter hood, quarter glass in the C-pillar.",
    )
    a.climb_note = "Sheet metal. Not a cling wall."
    a.vault_note = "Hood and roof are landings, not vault rails."
    for lod in (0, 1):
        g = a.begin(lod)
        body.build(g, SPEC, lod)
        a.end()
    a.box("Col_Body", (0, 0.50, -0.20), (1.05, 0.40, 1.70))
    a.box("Col_Roof", (0, 1.52, -0.36), (0.52, 0.035, 1.05))
    a.box("Col_Nose", (0, 0.46, 1.90), (1.05, 0.16, 0.32))
    a.box("Col_Tail", (0, 0.48, -1.95), (1.15, 0.18, 0.40))
    _wheel_cols(a)
    return a
