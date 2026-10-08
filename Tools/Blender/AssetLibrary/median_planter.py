"""Concrete median. Closed coping, soil, and a clipped box hedge."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import TILE_L, Asset, register


def _pwr(val, power):
    a = abs(val)
    if a < 1e-8:
        return 0.0
    return math.copysign(a ** power, val)


def _hedge(g, mat, seg):
    """Beveled box (superellipsoid) with a little surface noise. One closed shell."""
    hx, hy, hz = 0.24, 0.28, 0.55
    cy = 0.42
    power = 0.24
    rings = 8 if seg >= 12 else 5
    verts = []
    grid = []
    for i in range(rings + 1):
        v = math.pi * i / rings
        steps = 1 if i == 0 or i == rings else seg
        row = []
        sv, cv = math.sin(v), math.cos(v)
        for k in range(steps):
            u = 2.0 * math.pi * k / seg
            su, cu = math.sin(u), math.cos(u)
            x = hx * _pwr(cu, power) * _pwr(sv, power)
            y = hy * _pwr(cv, power)
            z = hz * _pwr(su, power) * _pwr(sv, power)
            amp = 0.012 * math.sin(4.0 * u + 1.7) * math.cos(3.0 * v)
            # Push along the box direction so the noise stays on the shell.
            x += amp * (1.0 if x >= 0 else -1.0)
            y += amp * 0.6 * (1.0 if y >= 0 else -1.0)
            z += amp * (1.0 if z >= 0 else -1.0)
            row.append(len(verts))
            verts.append((x, cy + y, z))
        grid.append(row)
    faces = []
    for i in range(rings):
        a = grid[i]
        b = grid[i + 1]
        if len(a) == 1:
            for k in range(len(b)):
                faces.append((a[0], b[k], b[(k + 1) % len(b)]))
            continue
        if len(b) == 1:
            for k in range(len(a)):
                faces.append((b[0], a[(k + 1) % len(a)], a[k]))
            continue
        for k in range(len(a)):
            k2 = (k + 1) % len(a)
            faces.append((a[k], a[k2], b[k2], b[k]))
    g.mesh(verts, faces, mat)


@register
def create():
    a = Asset(
        "Median_Planter",
        "Roads",
        "4.0 x 1.05 m median, 0.40 m concrete coping closed on all four sides, soil and a clipped hedge.",
    )
    a.climb_note = "Too low to cling."
    a.vault_note = "0.40 m wall. Not a vault."
    half = TILE_L * 0.5
    end_z = half - 0.08
    for lod in (0, 1):
        g = a.begin(lod)
        g.box((0, 0.20, end_z), (1.05, 0.40, 0.16), "Lib_Concrete", uv_scale=0.8)
        g.box((0, 0.20, -end_z), (1.05, 0.40, 0.16), "Lib_Concrete", uv_scale=0.8)
        g.box((-0.44, 0.20, 0), (0.16, 0.40, TILE_L - 0.32), "Lib_Concrete", uv_scale=0.8)
        g.box((0.44, 0.20, 0), (0.16, 0.40, TILE_L - 0.32), "Lib_Concrete", uv_scale=0.8)
        g.box((0, 0.405, end_z), (1.12, 0.05, 0.22), "Lib_Concrete")
        g.box((0, 0.405, -end_z), (1.12, 0.05, 0.22), "Lib_Concrete")
        g.box((-0.44, 0.405, 0), (0.22, 0.05, TILE_L - 0.28), "Lib_Concrete")
        g.box((0.44, 0.405, 0), (0.22, 0.05, TILE_L - 0.28), "Lib_Concrete")
        g.box((0, 0.08, 0), (0.72, 0.16, 3.36), "Lib_Soil", uv_scale=1.0)
        _hedge(g, "Lib_FoliageDark", 16 if lod == 0 else 10)
        a.end()
    a.box("Col_EndN", (0, 0.20, end_z), (1.00, 0.36, 0.12))
    a.box("Col_EndS", (0, 0.20, -end_z), (1.00, 0.36, 0.12))
    a.box("Col_SideL", (-0.44, 0.20, 0), (0.14, 0.36, 3.60))
    a.box("Col_SideR", (0.44, 0.20, 0), (0.14, 0.36, 3.60))
    a.box("Col_Soil", (0, 0.08, 0), (0.68, 0.14, 3.20))
    a.sphere("Col_Shrub", (0, 0.52, 0), 0.14)
    return a
