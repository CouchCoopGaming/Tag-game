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
    "step": 0.068,
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


def _fit_midsize(spec):
    """The parked shell is short and tall for a midsize sedan.

    Stretch Z so the body, plus the bumper that hangs past z1, lands near
    4.84 m. Drop the greenhouse so the roof, plus subdivision, stays under 1.50 m.
    Width is already on the 1.90 m cap, so X is left alone.
    """
    span = spec["z1"] - spec["z0"]
    z_scale = 4.80 / span
    y_floor = 0.42
    y_from = spec["roof_y"]
    y_to = 1.44

    def zz(z):
        return round(z * z_scale, 4)

    def zy(y):
        if y <= y_floor:
            return y
        return round(y_floor + (y - y_floor) * ((y_to - y_floor) / (y_from - y_floor)), 4)

    spec["z0"] = zz(spec["z0"])
    spec["z1"] = zz(spec["z1"])
    spec["a_pillar_z"] = zz(spec["a_pillar_z"])
    spec["c_pillar_z"] = zz(spec["c_pillar_z"])
    spec["axles"] = tuple(zz(z) for z in spec["axles"])
    spec["wheel_axles"] = tuple(zz(z) for z in spec["wheel_axles"])
    spec["roof_z"] = tuple(zz(z) for z in spec["roof_z"])
    spec["roof_y"] = zy(spec["roof_y"])
    spec["belt_y"] = zy(spec["belt_y"])
    spec["lamp_y"] = zy(spec["lamp_y"])
    spec["tail_y"] = zy(spec["tail_y"])
    spec["pillars"] = tuple((zz(z), half) for z, half in spec["pillars"])
    spec["windows"] = tuple((zz(a), zz(b)) for a, b in spec["windows"])
    for key in ("windshield", "rear_glass"):
        z0, y0, z1, y1 = spec[key]
        spec[key] = (zz(z0), zy(y0), zz(z1), zy(y1))
    spec["crown"] = tuple((zz(z), zy(y), x) for z, y, x in spec["crown"])
    spec["z_scale"] = z_scale
    return spec


SPEC = _fit_midsize(SPEC)


def _wheel_cols(a):
    body.wheel_boxes(a, SPEC, SPEC["wheel_axles"], lambda i, j: "Col_Wheel_%d%d" % (i, j))


@register
def create():
    a = Asset(
        "Car_Sedan_25",
        "StreetFurniture",
        "2025 midsize sedan shell. Length and roof sit in the midsize band. Quarter glass in the C-pillar.",
    )
    a.climb_note = "Sheet metal. Not a cling wall."
    a.vault_note = "Hood and roof are landings, not vault rails."
    for lod in (0, 1, 2):
        g = a.begin(lod)
        body.build(g, SPEC, lod)
        a.end()
    z_scale = SPEC["z_scale"]
    a.box("Col_Body", (0, 0.50, -0.20 * z_scale), (1.05, 0.40, 1.70 * z_scale))
    # Top sits about 3 cm under the lowered crown.
    a.box("Col_Roof", (0, 1.40, -0.36 * z_scale), (0.48, 0.04, 0.90 * z_scale))
    a.box("Col_Nose", (0, 0.46, 1.90 * z_scale), (1.05, 0.16, 0.32))
    a.box("Col_Tail", (0, 0.48, -1.95 * z_scale), (1.15, 0.18, 0.40))
    _wheel_cols(a)
    return a
