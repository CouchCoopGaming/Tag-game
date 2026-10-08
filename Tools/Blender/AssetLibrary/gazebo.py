"""Hexagonal gazebo. Posts, roof, and rails share one hex."""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick


# Deck corners and post centers. Roof corners sit on the same rays, past the posts.
POST_R = 1.90
EAVE_R = 2.22
DECK_TOP = 0.32
RAIL_Y = DECK_TOP + 0.95
OPEN_SIDE = 1


def _ring(radius, y, n=6):
    pts = []
    for i in range(n):
        ang = math.radians(i * (360.0 / n) - 90.0)
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
    t = 0.42
    inner = tuple(eave[i] + (peak[i] - eave[i]) * t for i in range(3))
    n = _up_normal(a, b, peak)
    center = tuple(inner[i] + n[i] * (thick * 0.40) for i in range(3))
    uphill = (peak[0] - eave[0], peak[1] - eave[1], peak[2] - eave[2])
    horiz = math.hypot(uphill[0], uphill[2]) or 1.0
    pitch = -math.degrees(math.atan2(uphill[1], horiz))
    yaw = math.degrees(math.atan2(uphill[0], uphill[2]))
    return center, (0.55, thick * 0.45, 0.70), (pitch, yaw, 0.0)


def _edge(i, radius):
    pts = _ring(radius, 0.0)
    p0, p1 = pts[i], pts[(i + 1) % 6]
    dx, dz = p1[0] - p0[0], p1[2] - p0[2]
    length = math.hypot(dx, dz) or 1.0
    # Outward is clockwise from the CCW ring.
    nx, nz = dz / length, -dx / length
    mid = ((p0[0] + p1[0]) * 0.5, (p0[2] + p1[2]) * 0.5)
    yaw = math.degrees(math.atan2(dx, dz))
    return p0, p1, mid, (nx, nz), length, yaw


@register
def create():
    a = Asset(
        "Gazebo",
        "Park",
        "Hexagonal gazebo, about 4.4 m across the posts. Deck is 0.32 m with a skirt. Six posts on the deck corners, roof corners on the same rays with a 0.32 m overhang, rails and balusters on five sides, one open side with a step. Rail is 0.95 m above the deck. The square shelter is Pavilion.",
    )
    a.climbable = False
    a.vaultable = True
    a.vault_height = 0.95
    a.loose_pivot = True
    a.climb_note = "Posts are 12 cm. Not a cling wall. The roof is a landing."
    a.vault_note = "Rail center is 0.95 m above the 0.32 m deck (y = 1.27). One side is the entry."
    posts = _ring(POST_R, 0.0)
    eaves = _ring(EAVE_R, 2.46)
    peak = (0.0, 3.42, 0.0)
    for lod in (0, 1, 2):
        g = a.begin(lod)
        seg = lod_pick(lod, 8, 6, 5)
        _prism(g, POST_R, 0.0, DECK_TOP, "Lib_Wood")
        _skirt(g, lod)
        for p in posts:
            g.cylinder((p[0], 1.39, p[2]), 0.06, 2.12, "Lib_WoodDark", seg)
        if lod < 2:
            _rails(g, lod, posts)
        _step(g)
        for i in range(6):
            verts, faces = _tri_slab(eaves[i], eaves[(i + 1) % 6], peak, 0.055)
            g.mesh(verts, faces, "Lib_Roof", uv_scale=1.0)
        if lod == 0:
            g.cylinder((0.0, 3.50, 0.0), 0.06, 0.10, "Lib_WoodDark", 6)
        a.end()
    # Square sits inside the hex apothem (1.90 * cos 30 = 1.65).
    a.box("Col_Deck", (0, 0.16, 0), (2.40, 0.26, 2.40))
    for i, p in enumerate(posts):
        a.capsule("Col_Post_%d" % i, (p[0], 1.39, p[2]), 0.045, 2.00, 1)
        center, size, euler = _slope_box(eaves[i], eaves[(i + 1) % 6], peak, 0.055)
        a.box("Col_Roof_%d" % i, center, size, euler=euler)
    for i in range(6):
        if i == OPEN_SIDE:
            continue
        _p0, _p1, mid, _normal, length, yaw = _edge(i, POST_R - 0.02)
        inset = 0.16
        span = max(0.4, length - inset * 2.0)
        a.box(
            "Vault_Rail_%d" % i,
            (mid[0], RAIL_Y, mid[1]),
            (0.028, 0.028, span),
            euler=(0.0, yaw, 0.0),
        )
    _p0, _p1, mid, normal, _length, yaw = _edge(OPEN_SIDE, POST_R)
    for i, (along, height, depth, width) in enumerate(((0.28, 0.18, 0.32, 1.20), (0.62, 0.09, 0.30, 1.36))):
        a.box(
            "Col_Step_%d" % i,
            (mid[0] + normal[0] * along, height * 0.5, mid[1] + normal[1] * along),
            (depth * 0.7, height * 0.7, width * 0.75),
            euler=(0.0, yaw, 0.0),
        )
    return a


def _skirt(g, lod):
    for i in range(6):
        _p0, _p1, mid, normal, length, yaw = _edge(i, POST_R)
        cx = mid[0] + normal[0] * 0.055
        cz = mid[1] + normal[1] * 0.055
        g.box((cx, DECK_TOP * 0.5, cz), (0.07, DECK_TOP, length - 0.18), "Lib_WoodDark", euler=(0.0, yaw, 0.0), uv_scale=1.0)


def _rails(g, lod, posts):
    for i in range(6):
        if i == OPEN_SIDE:
            continue
        p0, p1 = posts[i], posts[(i + 1) % 6]
        dx, dz = p1[0] - p0[0], p1[2] - p0[2]
        length = math.hypot(dx, dz) or 1.0
        inset = 0.10
        ax = p0[0] + dx / length * inset
        az = p0[2] + dz / length * inset
        bx = p1[0] - dx / length * inset
        bz = p1[2] - dz / length * inset
        g.pipe((ax, RAIL_Y, az), (bx, RAIL_Y, bz), 0.028, "Lib_Wood", 6)
        if lod == 0:
            for k in range(1, 4):
                t = k / 4.0
                x = ax + (bx - ax) * t
                z = az + (bz - az) * t
                # Tops stop 3 cm under the rail so the picket and the pipe do not share a volume.
                g.box((x, DECK_TOP + 0.44, z), (0.028, 0.78, 0.028), "Lib_Wood")


def _step(g):
    # Two treads outside the skirt. Local +X is the depth, local +Z runs along the opening.
    _p0, _p1, mid, normal, _length, yaw = _edge(OPEN_SIDE, POST_R)
    for along, height, depth in ((0.28, 0.18, 0.32), (0.62, 0.09, 0.30)):
        cx = mid[0] + normal[0] * along
        cz = mid[1] + normal[1] * along
        width = 1.20 if height > 0.12 else 1.36
        g.box((cx, height * 0.5, cz), (depth, height, width), "Lib_Wood", euler=(0.0, yaw, 0.0), uv_scale=1.0)
