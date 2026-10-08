"""Hexagonal gazebo. Distinct from the square park pavilion."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


def _ring(radius, y, n=6):
    pts = []
    for i in range(n):
        ang = math.radians(i * (360.0 / n))
        pts.append((radius * math.cos(ang), y, radius * math.sin(ang)))
    return pts


def _prism(g, radius, y0, y1, mat):
    bottom = _ring(radius, y0)
    top = _ring(radius, y1)
    verts = bottom + top
    faces = [tuple(range(5, -1, -1)), tuple(range(6, 12))]
    for i in range(6):
        j = (i + 1) % 6
        faces.append((i, j, j + 6, i + 6))
    g.mesh(verts, faces, mat, uv_scale=1.0)


def _tri_slab(a, b, c, thick):
    n = _up_normal(a, b, c)
    top = [tuple(p[i] + n[i] * thick for i in range(3)) for p in (a, b, c)]
    verts = [a, b, c] + top
    return verts, [
        (0, 2, 1), (3, 4, 5),
        (0, 1, 4, 3), (1, 2, 5, 4), (2, 0, 3, 5),
    ]


def _up_normal(a, b, c):
    ux, uy, uz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    vx, vy, vz = c[0] - a[0], c[1] - a[1], c[2] - a[2]
    nx = uy * vz - uz * vy
    ny = uz * vx - ux * vz
    nz = ux * vy - uy * vx
    length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    nx, ny, nz = nx / length, ny / length, nz / length
    if ny < 0.0:
        nx, ny, nz = -nx, -ny, -nz
    return (nx, ny, nz)


def _slope_box(a, b, peak, thick):
    eave = ((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5, (a[2] + b[2]) * 0.5)
    t = 0.38
    inner = tuple(eave[i] + (peak[i] - eave[i]) * t for i in range(3))
    n = _up_normal(a, b, peak)
    center = tuple(inner[i] + n[i] * (thick * 0.45) for i in range(3))
    uphill = (peak[0] - eave[0], peak[1] - eave[1], peak[2] - eave[2])
    horiz = math.hypot(uphill[0], uphill[2]) or 1.0
    pitch = -math.degrees(math.atan2(uphill[1], horiz))
    yaw = math.degrees(math.atan2(uphill[0], uphill[2]))
    return center, (0.70, thick * 0.55, 0.85), (pitch, yaw, 0.0)


@register
def create():
    a = Asset(
        "Gazebo",
        "Park",
        "Hexagonal gazebo, about 4.4 m across. Deck at 0.12 m, rail at 0.95 m above the deck, eave at 2.40 m, peak at 3.55 m. The square shelter is Pavilion.",
    )
    a.climbable = False
    a.vaultable = True
    a.vault_height = 0.95
    a.climb_note = "Posts are 11 cm. Not a cling wall. The roof is a landing."
    a.vault_note = "Rail is 0.95 m above the deck (rail center y = 1.07)."
    posts = _ring(2.16, 0.0)
    eaves = _ring(2.28, 2.42)
    peak = (0.0, 3.55, 0.0)
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6, 5)
        _prism(g, 1.98, 0.0, 0.12, "Lib_Wood")
        for p in posts:
            g.cylinder((p[0], 1.18, p[2]), 0.055, 2.36, "Lib_WoodDark", seg)
        if lod < 2:
            for i in range(6):
                p0 = posts[i]
                p1 = posts[(i + 1) % 6]
                dx, dz = p1[0] - p0[0], p1[2] - p0[2]
                length = math.hypot(dx, dz) or 1.0
                inset = 0.12
                a0 = (p0[0] + dx / length * inset, 1.07, p0[2] + dz / length * inset)
                a1 = (p1[0] - dx / length * inset, 1.07, p1[2] - dz / length * inset)
                g.pipe(a0, a1, 0.032, "Lib_Wood", 6)
                if lod == 0:
                    b0 = (a0[0], 0.55, a0[2])
                    b1 = (a1[0], 0.55, a1[2])
                    g.pipe(b0, b1, 0.022, "Lib_WoodDark", 5)
        for i in range(6):
            verts, faces = _tri_slab(eaves[i], eaves[(i + 1) % 6], peak, 0.06)
            g.mesh(verts, faces, "Lib_Roof", uv_scale=1.0)
        a.end()
    # Square sits inside the hex (apothem 1.71). A 3.3 m square would leave the floor.
    a.box("Col_Deck", (0, 0.06, 0), (2.28, 0.08, 2.28))
    for i, p in enumerate(posts):
        a.capsule("Col_Post_%d" % i, (p[0], 1.18, p[2]), 0.045, 2.22, 1)
        center, size, euler = _slope_box(eaves[i], eaves[(i + 1) % 6], peak, 0.06)
        a.box("Col_Roof_%d" % i, center, size, euler=euler)
    for i in range(6):
        p0 = posts[i]
        p1 = posts[(i + 1) % 6]
        dx, dz = p1[0] - p0[0], p1[2] - p0[2]
        length = math.hypot(dx, dz) or 1.0
        inset = 0.18
        a0 = (p0[0] + dx / length * inset, p0[2] + dz / length * inset)
        a1 = (p1[0] - dx / length * inset, p1[2] - dz / length * inset)
        span = math.hypot(a1[0] - a0[0], a1[1] - a0[1])
        yaw = math.degrees(math.atan2(a1[0] - a0[0], a1[1] - a0[1]))
        center = ((a0[0] + a1[0]) * 0.5, 1.07, (a0[1] + a1[1]) * 0.5)
        a.box("Vault_Rail_%d" % i, center, (0.028, 0.028, max(0.2, span - 0.06)), euler=(0.0, yaw, 0.0))
    return a
