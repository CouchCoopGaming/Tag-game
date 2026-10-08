"""Square park pavilion. 4.6 m across, rail at 0.95 m, pyramid hip roof."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register


@register
def create():
    a = Asset(
        "Pavilion",
        "Park",
        "4.6 m square pavilion. Deck at 0.15 m, rail at 0.95 m above the deck, eave at 2.55 m, peak at 3.90 m. No gazebo was in the repo; this is the park pavilion.",
    )
    a.climbable = False
    a.vaultable = True
    a.vault_height = 0.95
    a.climb_note = "Posts are 14 cm. Not a cling wall. The roof is a landing."
    a.vault_note = "Vault_Rail runs between the posts, 0.95 m above the deck (rail top y = 1.10)."
    corners = [(-2.45, 2.55, -2.45), (2.45, 2.55, -2.45), (2.45, 2.55, 2.45), (-2.45, 2.55, 2.45)]
    peak = (0.0, 3.90, 0.0)
    for lod in (0, 1, 2):
        g = a.begin(lod)
        bev = 0.006 if lod == 0 else 0
        g.box((0, 0.075, 0), (4.6, 0.15, 4.6), "Lib_Wood", bevel=bev, segs=1, uv_scale=1.0)
        for x in (-1.9, 1.9):
            for z in (-1.9, 1.9):
                g.box((x, 1.40, z), (0.14, 2.50, 0.14), "Lib_WoodDark", bevel=bev, segs=1)
        if lod < 2:
            for z in (-1.9, 1.9):
                g.pipe((-1.7, 1.10, z), (1.7, 1.10, z), 0.035, "Lib_Wood", 6)
                g.pipe((-1.7, 0.55, z), (1.7, 0.55, z), 0.025, "Lib_WoodDark", 6)
            for x in (-1.9, 1.9):
                g.pipe((x, 1.10, -1.7), (x, 1.10, 1.7), 0.035, "Lib_Wood", 6)
        g.box((0, 2.50, 0), (4.55, 0.06, 4.55), "Lib_Wood", uv_scale=1.0)
        for i in range(4):
            verts, faces = _tri_slab(corners[i], corners[(i + 1) % 4], peak, 0.07)
            g.mesh(verts, faces, "Lib_Roof", uv_scale=1.0)
        a.end()
    a.box("Col_Deck", (0, 0.075, 0), (4.6, 0.15, 4.6))
    a.box("Col_Soffit", (0, 2.50, 0), (4.50, 0.05, 4.50))
    for i, (x, z) in enumerate(((-1.9, -1.9), (1.9, -1.9), (-1.9, 1.9), (1.9, 1.9))):
        a.box("Col_Post_%d" % i, (x, 1.40, z), (0.14, 2.50, 0.14))
    a.capsule("Vault_Rail_S", (0, 1.10, -1.9), 0.035, 3.4, 0)
    a.capsule("Vault_Rail_N", (0, 1.10, 1.9), 0.035, 3.4, 0)
    a.capsule("Vault_Rail_W", (-1.9, 1.10, 0), 0.035, 3.4, 2)
    a.capsule("Vault_Rail_E", (1.9, 1.10, 0), 0.035, 3.4, 2)
    for i in range(4):
        center, size, euler = _slope_box(corners[i], corners[(i + 1) % 4], peak, 0.07)
        a.box("Col_Roof_%d" % i, center, size, euler=euler)
    return a


def _tri_slab(a, b, c, thick):
    n = _up_normal(a, b, c)
    top = [tuple(p[i] + n[i] * thick for i in range(3)) for p in (a, b, c)]
    verts = [a, b, c] + top
    faces = [
        (0, 2, 1),
        (3, 4, 5),
        (0, 1, 4, 3),
        (1, 2, 5, 4),
        (2, 0, 3, 5),
    ]
    return verts, faces


def _up_normal(a, b, c):
    ux, uy, uz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    vx, vy, vz = c[0] - a[0], c[1] - a[1], c[2] - a[2]
    nx = uy * vz - uz * vy
    ny = uz * vx - ux * vz
    nz = ux * vy - uy * vx
    length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    nx, ny, nz = nx / length, ny / length, nz / length
    if ny < 0:
        nx, ny, nz = -nx, -ny, -nz
    return (nx, ny, nz)


def _slope_box(a, b, peak, thick):
    """Inset box sitting in the middle of one hip face."""
    eave = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5, (a[2] + b[2]) * 0.5)
    t = 0.42
    inner = tuple(eave[i] + (peak[i] - eave[i]) * t for i in range(3))
    n = _up_normal(a, b, peak)
    center = tuple(inner[i] + n[i] * (thick * 0.5) for i in range(3))
    uphill = (peak[0] - eave[0], peak[1] - eave[1], peak[2] - eave[2])
    horiz = math.hypot(uphill[0], uphill[2]) or 1.0
    pitch = -math.degrees(math.atan2(uphill[1], horiz))
    yaw = math.degrees(math.atan2(uphill[0], uphill[2]))
    # Narrow enough to stay inside the triangle at this station.
    return center, (1.15, thick * 0.72, 1.35), (pitch, yaw, 0.0)
