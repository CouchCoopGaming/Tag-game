"""Wood boathouse on piles. Slip on +Z, sized to the dock kit.

Deck boards match Dock_Straight: 20 cm boards, 5 cm gaps, top at 0.62 m.
The slip is 3.20 m clear so a 3.0 m dock can enter. Piles are the dock pile.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register

W = 6.6
D = 8.4
WALL_T = 0.10
WALL_TOP = 2.90
ROOF_Y0 = 2.904
RISE = 1.35
EAVE_X = 3.55
ROOF_Z = 4.40
RIDGE_STOP = 0.12
SHELL_GAP = 0.006
SHELL_THICK = 0.040

PLANK_TOP = 0.618
PLANK_T = 0.036
PITCH = 0.25
BOARD = 0.20
PILE_R = 0.15
PILE_BOT = -1.22
PILE_TOP = 0.370
STRINGER_Y = 0.476

WALK_X = 2.40
WALK_W = 1.52
SLIP = 1.60


def _add(p, n, scale):
    return (p[0] + n[0] * scale, p[1] + n[1] * scale, p[2] + n[2] * scale)


def _normal(a, b, c):
    ux, uy, uz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    vx, vy, vz = c[0] - a[0], c[1] - a[1], c[2] - a[2]
    nx = uy * vz - uz * vy
    ny = uz * vx - ux * vz
    nz = ux * vy - uy * vx
    length = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    if ny < 0.0:
        nx, ny, nz = -nx, -ny, -nz
    return (nx / length, ny / length, nz / length)


def _quad_shell(g, quad, uvs, mat, gap, thick):
    n = _normal(quad[0], quad[1], quad[3])
    inner = [_add(p, n, gap) for p in quad]
    outer = [_add(p, n, gap + thick) for p in quad]
    faces = [
        (4, 5, 6, 7),
        (0, 3, 2, 1),
        (0, 1, 5, 4),
        (1, 2, 6, 5),
        (2, 3, 7, 6),
        (3, 0, 4, 7),
    ]
    g.quad(inner + outer, faces, list(uvs) + list(uvs), mat)


def _roof_solid(g):
    y0, y1 = ROOF_Y0, ROOF_Y0 + RISE
    ex, z0, z1 = EAVE_X, -ROOF_Z, ROOF_Z
    verts = [
        (-ex, y0, z0),
        (ex, y0, z0),
        (ex, y0, z1),
        (-ex, y0, z1),
        (0.0, y1, z0),
        (0.0, y1, z1),
    ]
    faces = [
        (0, 3, 2, 1),
        (1, 2, 5, 4),
        (0, 4, 5, 3),
        (3, 2, 5),
        (0, 1, 4),
    ]
    g.mesh(verts, faces, "Lib_Wood", uv_scale=1.0)


def _shells(g):
    y0 = ROOF_Y0
    ex, z0, z1 = EAVE_X, -ROOF_Z, ROOF_Z
    x_top = RIDGE_STOP
    y_top = y0 + RISE * (1.0 - x_top / ex)
    slope = math.hypot(ex - x_top, y_top - y0)
    right = [
        (ex, y0, z0),
        (ex, y0, z1),
        (x_top, y_top, z1),
        (x_top, y_top, z0),
    ]
    _quad_shell(g, right, [
        (z0, 0.0), (z1, 0.0), (z1, slope), (z0, slope),
    ], "Lib_RanchRoof", SHELL_GAP, SHELL_THICK)
    left = [
        (-ex, y0, z1),
        (-ex, y0, z0),
        (-x_top, y_top, z0),
        (-x_top, y_top, z1),
    ]
    _quad_shell(g, left, [
        (z1, 0.0), (z0, 0.0), (z0, slope), (z1, slope),
    ], "Lib_RanchRoof", SHELL_GAP, SHELL_THICK)
    cap_y = (y0 + RISE) + 0.055
    g.box((0.0, cap_y, 0.0), (0.14, 0.05, ROOF_Z * 2.0 - 0.24), "Lib_RanchRoof", uv_scale=1.0)


def _walls(g, lod):
    side_x = W * 0.5 - WALL_T * 0.5
    end_z = D * 0.5 - WALL_T * 0.5
    span_x = W - WALL_T * 2.0 - 0.008
    wall_h = WALL_TOP - 0.02
    wall_y = 0.02 + wall_h * 0.5
    g.box((-side_x, wall_y, 0.0), (WALL_T, wall_h, D), "Lib_Wood", uv_scale=1.0)
    g.box((side_x, wall_y, 0.0), (WALL_T, wall_h, D), "Lib_Wood", uv_scale=1.0)
    # Back wall between the sides.
    g.box((0.0, wall_y, -end_z), (span_x, wall_h, WALL_T), "Lib_Wood", uv_scale=1.0)
    # Front piers beside the slip, and a header above the opening.
    pier_h = 2.556 - 0.02
    pier_y = 0.02 + pier_h * 0.5
    pier_w = (span_x * 0.5) - SLIP
    pier_x = SLIP + pier_w * 0.5
    g.box((-pier_x, pier_y, end_z), (pier_w, pier_h, WALL_T), "Lib_Wood", uv_scale=1.0)
    g.box((pier_x, pier_y, end_z), (pier_w, pier_h, WALL_T), "Lib_Wood", uv_scale=1.0)
    head_h = WALL_TOP - 2.560
    g.box((0.0, 2.560 + head_h * 0.5, end_z), (span_x, head_h, WALL_T), "Lib_Wood", uv_scale=1.0)
    # Fascia across the slip opening, proud of the wall.
    g.box((0.0, 2.40, end_z + WALL_T * 0.5 + 0.028), (SLIP * 2.0 - 0.08, 0.10, 0.036), "Lib_WoodDark")
    if lod < 2:
        for sx in (-1.0, 1.0):
            for sz in (-1.0, 1.0):
                g.box((
                    sx * (W * 0.5 + 0.016),
                    wall_y,
                    sz * (D * 0.5 + 0.016),
                ), (0.024, wall_h - 0.06, 0.024), "Lib_WoodDark")


def _window(g, sign):
    # Glass 8 mm outside the siding. The frame sits 4 mm further out and wraps it.
    x = sign * (W * 0.5 + 0.014)
    g.box((x, 1.72, 0.0), (0.012, 0.70, 0.82), "Lib_Glass")
    frame_x = sign * (W * 0.5 + 0.034)
    g.box((frame_x, 2.096, 0.0), (0.018, 0.044, 0.92), "Lib_WoodDark")
    g.box((frame_x, 1.344, 0.0), (0.018, 0.044, 0.92), "Lib_WoodDark")
    g.box((frame_x, 1.72, 0.436), (0.018, 0.70, 0.044), "Lib_WoodDark")
    g.box((frame_x, 1.72, -0.436), (0.018, 0.70, 0.044), "Lib_WoodDark")


def _battens(g):
    outer_x = W * 0.5
    bx = outer_x + 0.012
    z = -3.6
    while z <= 3.6:
        if abs(z) > 0.55:
            g.box((bx, 1.46, z), (0.016, 2.72, 0.036), "Lib_Batten")
            g.box((-bx, 1.46, z), (0.016, 2.72, 0.036), "Lib_Batten")
        z += 0.60
    bz = -(D * 0.5) - 0.012
    x = -2.7
    while x <= 2.7:
        g.box((x, 1.46, bz), (0.036, 2.72, 0.016), "Lib_Batten")
        x += 0.60


def _walks(g, cols, lod):
    z0, z1 = -4.00, 4.00
    count = int(round((z1 - z0) / PITCH))
    y = PLANK_TOP - PLANK_T * 0.5
    if lod == 0:
        for sign in (-1.0, 1.0):
            for i in range(count):
                z = z0 + (i + 0.5) * PITCH
                g.box((sign * WALK_X, y, z), (WALK_W, PLANK_T, BOARD), "Lib_Wood", uv_scale=1.0)
                cols.append(("box", "Col_Plank", (sign * WALK_X, y, z), (WALK_W - 0.12, 0.020, BOARD - 0.04)))
    else:
        span = count * PITCH - (PITCH - BOARD)
        for sign in (-1.0, 1.0):
            g.box((sign * WALK_X, y, 0.0), (WALK_W, PLANK_T, span), "Lib_Wood", uv_scale=1.0)
    sy = STRINGER_Y
    slen = 7.55
    for sign in (-1.0, 1.0):
        g.box((sign * WALK_X, sy, 0.0), (0.05, 0.20, slen), "Lib_WoodDark")
        cols.append(("box", "Col_Stringer", (sign * WALK_X, sy, 0.0), (0.030, 0.12, slen - 0.12)))


def _piles(g, cols, lod):
    zs = (-3.0, -1.0, 1.0, 3.0) if lod == 0 else (-2.0, 2.0)
    for sign in (-1.0, 1.0):
        for z in zs:
            g.cylinder_bands(sign * WALK_X, z, PILE_R, (
                (PILE_BOT, -0.02, "Lib_WoodDark"),
                (-0.02, PILE_TOP, "Lib_Wood"),
            ), segments=8)
            cols.append((
                "box", "Col_Pile",
                (sign * WALK_X, (PILE_BOT + PILE_TOP) * 0.5 + 0.04, z),
                (0.16, (PILE_TOP - PILE_BOT) - 0.16, 0.16),
            ))


def _lift(g, lod):
    for z in (2.50, -1.10):
        g.box((0.0, 2.48, z), (SLIP * 2.0 - 0.16, 0.08, 0.10), "Lib_SteelDark")
    g.box((-0.62, 0.28, 0.4), (0.14, 0.12, 6.2), "Lib_WoodDark")
    g.box((0.62, 0.28, 0.4), (0.14, 0.12, 6.2), "Lib_WoodDark")
    if lod == 0:
        for z in (2.50, -1.10):
            for x in (-0.62, 0.62):
                g.pipe((x, 2.444, z), (x, 0.344, z), 0.008, "Lib_Steel", 6)


def _slope_landing(asset, name, sign):
    ex = EAVE_X
    x_top = RIDGE_STOP
    y0 = ROOF_Y0
    y_top = y0 + RISE * (1.0 - x_top / ex)
    t = 0.46
    x = ex + (x_top - ex) * t
    y = y0 + (y_top - y0) * t
    dx = ex - x_top
    dy = y_top - y0
    length = math.hypot(dx, dy)
    nx, ny = dy / length, dx / length
    mid = SHELL_GAP + SHELL_THICK * 0.5
    cx = sign * x + sign * nx * mid
    cy = y + ny * mid
    angle = math.degrees(math.atan2(dy, dx))
    asset.box(
        name, (cx, cy, 0.0),
        (length * 0.42, 0.018, ROOF_Z * 1.15),
        euler=(0.0, 0.0, -sign * angle),
    )


@register
def create():
    a = Asset(
        "Boathouse",
        "Harbor",
        "Wood boathouse 6.6 m wide, 8.4 m deep, eave 2.90 m, ridge about 4.25 m. "
        "The +Z gable is an open slip 3.20 m clear, wide enough for the 3 m dock. "
        "Side walks use the dock board: 20 cm planks, 5 cm gaps, top at 0.62 m. "
        "Piles match the dock pile. A steel lift beam, cables, and two bunks sit in the slip. "
        "Roof slopes are landings. The piles extend below the pivot.",
    )
    a.loose_pivot = True
    a.allow_below = True
    a.climbable = True
    a.vaultable = False
    a.climb_note = "Side and back walls are cling. The slip on +Z is empty. Roof slopes are landings."
    a.vault_note = "No rail. Wall top is 2.90 m."
    for lod in (0, 1):
        g = a.begin(lod)
        cols = []
        _walls(g, lod)
        _walks(g, cols, lod)
        _piles(g, cols, lod)
        _lift(g, lod)
        _roof_solid(g)
        if lod < 2:
            _shells(g)
        if lod == 0:
            _window(g, 1.0)
            _window(g, -1.0)
            _battens(g)
            a._cols = cols
        a.end()
    from _volume import _apply_cols
    _apply_cols(a)
    side_x = W * 0.5 - WALL_T * 0.5
    end_z = D * 0.5 - WALL_T * 0.5
    span_x = W - WALL_T * 2.0 - 0.08
    wall_h = WALL_TOP - 0.08
    a.box("Climb_SideL", (-side_x, 0.04 + wall_h * 0.5, 0.0), (0.06, wall_h - 0.08, D - 0.20))
    a.box("Climb_SideR", (side_x, 0.04 + wall_h * 0.5, 0.0), (0.06, wall_h - 0.08, D - 0.20))
    a.box("Climb_Back", (0.0, 0.04 + wall_h * 0.5, -end_z), (span_x - 0.10, wall_h - 0.08, 0.06))
    pier_h = 2.40
    pier_w = (W - WALL_T * 2.0 - 0.008) * 0.5 - SLIP
    pier_x = SLIP + pier_w * 0.5
    a.box("Climb_FrontL", (-pier_x, 0.08 + pier_h * 0.5, end_z), (pier_w - 0.08, pier_h - 0.10, 0.06))
    a.box("Climb_FrontR", (pier_x, 0.08 + pier_h * 0.5, end_z), (pier_w - 0.08, pier_h - 0.10, 0.06))
    a.box("Col_Header", (0.0, 2.73, end_z), (span_x - 0.10, 0.24, 0.06))
    a.box("Col_Beam", (0.0, 2.48, 2.50), (SLIP * 2.0 - 0.28, 0.045, 0.06))
    a.box("Col_Beam_2", (0.0, 2.48, -1.10), (SLIP * 2.0 - 0.28, 0.045, 0.06))
    a.box("Col_BunkL", (-0.62, 0.28, 0.4), (0.08, 0.06, 5.8))
    a.box("Col_BunkR", (0.62, 0.28, 0.4), (0.08, 0.06, 5.8))
    _slope_landing(a, "Col_RoofR", 1.0)
    _slope_landing(a, "Col_RoofL", -1.0)
    return a
