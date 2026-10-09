"""Asphalt patch overlay. No collider — it sits on a road that already has one."""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register


@register
def create():
    a = Asset(
        "Asphalt_Patch",
        "Roads",
        "1.8 x 1.1 m patch, 1.2 cm thick. Visual only. Place it on a road top (y = 0.12).",
    )
    a.climb_note = "Decal. The road slab under it is the collider."
    a.vault_note = "No collider of its own, so it cannot become a lip."
    verts = [
        (-0.9, 0.0, -0.4), (0.7, 0.0, -0.55), (0.9, 0.0, 0.1),
        (0.4, 0.0, 0.55), (-0.6, 0.0, 0.45), (-0.85, 0.0, 0.05),
    ]
    top = [(v[0], 0.012, v[2]) for v in verts]
    for lod in (0, 1):
        g = a.begin(lod)
        n = len(verts)
        allv = verts + top
        faces = [tuple(range(n)), tuple(reversed(range(n, n * 2)))]
        for i in range(n):
            j = (i + 1) % n
            faces.append((i, j, n + j, n + i))
        g.mesh(allv, faces, "Lib_Asphalt", uv_scale=0.8)
        if lod == 0:
            g.box((0.1, 0.014, 0.05), (0.7, 0.004, 0.18), "Lib_Black")
        a.end()
    return a
