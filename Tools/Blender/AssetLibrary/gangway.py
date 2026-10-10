"""Aluminum gangway. High end is -Z, roller end is +Z.

The deck is one plate. Cleats sit on it. Rail posts are welded into the
side stringers. The wheels at +Z bottom out at 0.62 m, the floating-dock deck.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick

LENGTH = 2.6
HIGH = 0.90
LOW = 0.72
PITCH = math.degrees(math.atan2(HIGH - LOW, LENGTH))
WHEEL_R = 0.050
WHEEL_Z = 1.22
WHEEL_Y = 0.620 + WHEEL_R


def _y_at(z):
    t = (z + LENGTH * 0.5) / LENGTH
    return HIGH + (LOW - HIGH) * t


@register
def create():
    a = Asset(
        "Gangway",
        "Harbor",
        "Aluminum gangway, 2.6 m. Plate falls from 0.90 m at -Z to 0.72 m at +Z. "
        "Side stringers carry the posts. Cleats are on the plate. Wheels at +Z rest at 0.62 m.",
    )
    a.allow_float = True
    a.loose_pivot = True
    a.vaultable = True
    a.vault_height = 0.88
    a.climb_note = "Walk the plate. Not a cling wall."
    a.vault_note = "Vault_Rail is the handrail, 0.88 m above the plate."
    for lod in (0, 1):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6)
        bev = 0.002 if lod == 0 else 0
        mid = _y_at(0.0)
        g.box((0, mid, 0), (0.92, 0.018, LENGTH), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0, euler=(PITCH, 0, 0))
        for x in (-0.47, 0.47):
            g.box((x, mid - 0.042, 0), (0.055, 0.070, LENGTH), "Lib_SteelDark", euler=(PITCH, 0, 0))
        posts = lod_pick(lod, 4, 3)
        z_posts = []
        for i in range(posts):
            z_posts.append(-1.05 + i * (2.00 / (posts - 1)))
        for x in (-0.47, 0.47):
            for z in z_posts:
                g.cylinder((x, _y_at(z) + 0.42, z), 0.018, 0.92, "Lib_Steel", seg)
            z0, z1 = z_posts[0], z_posts[-1]
            g.pipe((x, _y_at(z0) + 0.88, z0), (x, _y_at(z1) + 0.88, z1), 0.016, "Lib_Steel", seg)
            inner = x * 0.94
            g.pipe((inner, _y_at(z0) + 0.46, z0), (inner, _y_at(z1) + 0.46, z1), 0.012, "Lib_SteelDark", seg)
        # Hinge leaf overlaps the high end of the plate.
        g.box((0, HIGH, -LENGTH * 0.5 - 0.10), (0.55, 0.014, 0.28), "Lib_Steel", bevel=bev, segs=1 if lod == 0 else 0)
        pin_z = -LENGTH * 0.5 - 0.18
        g.cylinder((0, HIGH + 0.012, pin_z), 0.016, 0.52, "Lib_SteelDark", seg, axis="X")
        if lod == 0:
            for hx in (-0.22, 0.22):
                g.cylinder((hx, HIGH + 0.012, pin_z), 0.022, 0.028, "Lib_Steel", max(6, seg - 2), axis="X")
        cleats = lod_pick(lod, 6, 3)
        for i in range(cleats):
            z = -0.95 + (i + 0.5) * (1.70 / cleats)
            g.box((0, _y_at(z) + 0.012, z), (0.64, 0.010, 0.022), "Lib_SteelDark", euler=(PITCH, 0, 0))
        # Roller. The tire bottom is the floating-dock deck.
        g.cylinder((0, WHEEL_Y, WHEEL_Z), 0.014, 0.72, "Lib_SteelDark", seg, axis="X")
        for x in (-0.30, 0.30):
            g.cylinder((x, WHEEL_Y, WHEEL_Z), WHEEL_R, 0.046, "Lib_Black", lod_pick(lod, 12, 8), axis="X")
            shoe = 0.40 if x > 0 else -0.40
            g.box((shoe, _y_at(WHEEL_Z) - 0.050, WHEEL_Z), (0.14, 0.06, 0.08), "Lib_Steel")
        a.end()
    a.box("Col_Deck", (0, _y_at(0.0), 0), (0.68, 0.010, LENGTH - 0.36), euler=(PITCH, 0, 0))
    for side, x in enumerate((-0.47, 0.47)):
        a.box(
            "Vault_Rail_%d" % side,
            (x, _y_at(0.0) + 0.88, 0.0),
            (0.018, 0.018, 1.70),
            euler=(PITCH, 0, 0),
        )
    return a
