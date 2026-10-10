"""Straight curb and gutter. 4 m long, to butt a 6 m road tile.

Gutter is 0.30 m wide at the 0.12 m road surface. The curb rises 0.15 m
to 0.27 m, the same walk height as the sidewalk tiles. Face batters about 25 mm.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _profile(g, lod):
    # One solid: gutter pan plus the curb, built as a prism so the face is battered.
    length = 4.0
    hz = length * 0.5
    # x: gutter from -0.28 to 0.02 at y=0.12, curb top from 0.06 to 0.24 at y=0.27.
    rings = [
        [(-0.28, 0.0, -hz), (-0.28, 0.12, -hz), (0.04, 0.12, -hz), (0.06, 0.27, -hz), (0.24, 0.27, -hz), (0.24, 0.0, -hz)],
        [(-0.28, 0.0, hz), (-0.28, 0.12, hz), (0.04, 0.12, hz), (0.06, 0.27, hz), (0.24, 0.27, hz), (0.24, 0.0, hz)],
    ]
    verts = rings[0] + rings[1]
    n = 6
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    faces.append(tuple(reversed(range(n))))
    faces.append(tuple(range(n, n * 2)))
    g.mesh(verts, faces, "Lib_Concrete", uv_scale=0.6, bevel=0.004 if lod == 0 else 0.0, segs=1 if lod == 0 else 0)
    if lod == 0:
        g.box((0.248, 0.16, 0.55), (0.016, 0.12, 0.40), "Lib_AsphaltWear")
        for z in (-1.0, 1.0):
            g.box((0.14, 0.275, z), (0.16, 0.008, 0.012), "Lib_Mortar")


@register
def create():
    a = Asset(
        "StreetCurb_Straight",
        "StreetFurniture",
        "Curb and gutter, 4.00 m long. Gutter 0.30 m at 0.12 m. Curb face 0.15 m up to 0.27 m. Place the gutter against the road edge.",
    )
    a.climb_note = "Curb face is 0.15 m. Not a cling wall."
    a.vault_note = "Curb is 0.15 m above the gutter. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        _profile(g, lod)
        a.end()
    # Gutter crown is 0.12. Collider stays in the pan, clear of the battered face.
    # Gutter crown is 0.12. Head crown is 0.27. Both tops sit about 8 mm under.
    a.box("Col_Gutter", (-0.12, 0.058, 0), (0.22, 0.108, 3.80))
    a.box("Col_Head", (0.15, 0.201, 0), (0.10, 0.122, 3.80))
    return a
