"""Concrete parking wheel stop. A tapered block, not a curb or a barrier."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _block(g, mat):
    """Trapezoid prism, 1.80 m long, 0.15 m at the ground, 0.10 m tall."""
    rings = []
    for x, y_top, z_base, z_top in (
        (-0.90, 0.02, 0.04, 0.02),
        (-0.76, 0.10, 0.075, 0.045),
        (0.76, 0.10, 0.075, 0.045),
        (0.90, 0.02, 0.04, 0.02),
    ):
        rings.append([
            (x, 0.0, -z_base),
            (x, y_top, -z_top),
            (x, y_top, z_top),
            (x, 0.0, z_base),
        ])
    count = 4
    verts = [p for ring in rings for p in ring]
    faces = []
    for i in range(len(rings) - 1):
        for k in range(count):
            a = i * count + k
            b = i * count + (k + 1) % count
            c = (i + 1) * count + (k + 1) % count
            d = (i + 1) * count + k
            faces.append((a, b, c, d))
    # End caps.
    faces.append((0, 3, 2, 1))
    base = (len(rings) - 1) * count
    faces.append((base, base + 1, base + 2, base + 3))
    g.mesh(verts, faces, mat)


@register
def create():
    a = Asset(
        "WheelStop_Concrete",
        "StreetFurniture",
        "Parking wheel stop, 1.80 m long and 0.10 m tall. Tapered ends, two bolts.",
    )
    a.climb_note = "10 cm. Not a wall."
    a.vault_note = "Too low to vault."
    for lod in (0, 1):
        g = a.begin(lod)
        _block(g, "Lib_Concrete")
        for x in (-0.42, 0.42):
            g.cylinder((x, 0.105, 0.0), 0.014, 0.018, "Lib_Steel", 8)
            if lod == 0:
                g.cylinder((x, 0.116, 0.0), 0.008, 0.006, "Lib_SteelDark", 8)
        a.end()
    # Inside the flat middle. The tapered ends are outside this box.
    a.box("Col_Block", (0, 0.04, 0), (1.40, 0.05, 0.08))
    return a
