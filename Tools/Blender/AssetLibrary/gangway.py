"""Gangway from a quay deck at 0.90 m down to a floating dock at 0.62 m.

High end is -Z. Yaw 180 when the pier is toward +Z. The mesh floats: both
ends land on decks, not on the ground.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

LENGTH = 2.6
HIGH = 0.90
LOW = 0.62
PITCH = math.degrees(math.atan2(HIGH - LOW, LENGTH))


def _y_at(z):
    t = (z + LENGTH * 0.5) / LENGTH
    return HIGH + (LOW - HIGH) * t


@register
def create():
    a = Asset(
        "Gangway",
        "Harbor",
        "Gangway 2.6 x 1.05 m. Deck falls from 0.90 m at -Z (quay) to 0.62 m at +Z (floating dock). Hinge leaf and pin at the high end, handrails both sides, anti-slip cleats on the deck.",
    )
    a.allow_float = True
    a.loose_pivot = True
    a.vaultable = True
    a.vault_height = 0.95
    a.climb_note = "Walk the planks. Not a cling wall."
    a.vault_note = "Vault_Rail is the handrail, 0.95 m above the deck."
    count = 10
    for lod in (0, 1):
        g = a.begin(lod)
        n = lod_pick(lod, count, 6)
        bev = 0.003 if lod == 0 else 0
        seg = lod_pick(lod, 8, 6)
        for i in range(n):
            t = (i + 0.5) / n
            z = -LENGTH * 0.5 + t * LENGTH
            y = HIGH + (LOW - HIGH) * t
            pitch = LENGTH / n
            g.box((0, y, z), (0.92, 0.028, pitch * 0.86), "Lib_WoodWeather", bevel=bev, segs=1 if lod == 0 else 0, euler=(PITCH, 0, 0), uv_scale=1.2)
        for x in (-0.48, 0.48):
            posts = lod_pick(lod, 4, 2)
            for i in range(posts):
                z = -LENGTH * 0.5 + 0.18 + i * (LENGTH - 0.36) / (posts - 1)
                deck = _y_at(z)
                g.cylinder((x, deck + 0.474, z), 0.016, 0.90, "Lib_Steel", seg)
            z0 = -LENGTH * 0.5 + 0.18
            z1 = LENGTH * 0.5 - 0.18
            g.pipe((x, _y_at(z0) + 0.95, z0), (x, _y_at(z1) + 0.95, z1), 0.016, "Lib_Steel", seg)
            inner = x * 0.92
            g.pipe((inner, _y_at(z0) + 0.52, z0), (inner, _y_at(z1) + 0.52, z1), 0.011, "Lib_SteelDark", seg)
        # Quay leaf stops short of the first plank. The pin sits in that gap.
        g.box((0, HIGH + 0.010, -LENGTH * 0.5 - 0.20), (0.66, 0.012, 0.24), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        pin_z = -LENGTH * 0.5 - 0.02
        g.cylinder((0, HIGH + 0.028, pin_z), 0.016, 0.58, "Lib_SteelDark", seg, axis="X")
        if lod == 0:
            for hx in (-0.24, 0.24):
                g.cylinder((hx, HIGH + 0.028, pin_z), 0.022, 0.032, "Lib_Steel", max(6, seg - 2), axis="X")
        cleats = lod_pick(lod, 8, 4)
        span = LENGTH - 0.44
        for i in range(cleats):
            t = (i + 0.5) / cleats
            z = -LENGTH * 0.5 + 0.22 + t * span
            g.box((0, _y_at(z) + 0.024, z), (0.70, 0.008, 0.018), "Lib_SteelDark", euler=(PITCH, 0, 0))
        a.end()
    for i in range(count):
        t = (i + 0.5) / count
        z = -LENGTH * 0.5 + t * LENGTH
        y = HIGH + (LOW - HIGH) * t
        pitch = LENGTH / count
        a.box("Col_Plank_%d" % i, (0, y, z), (0.78, 0.02, pitch * 0.70), euler=(PITCH, 0, 0))
    for side, x in enumerate((-0.48, 0.48)):
        a.box(
            "Vault_Rail_%d" % side,
            (x, _y_at(0.0) + 0.95, 0.0),
            (0.02, 0.02, LENGTH - 0.55),
            euler=(PITCH, 0, 0),
        )
    return a
