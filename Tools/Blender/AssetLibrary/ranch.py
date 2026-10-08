"""Single-story ranch. Hip roof, attached garage, porch on +Z.

Footprint 10.8 x 7.2 m, plate at 2.50 m, ridge about 3.70 m.
The garage is the +X 3.6 m. The porch is in front of the living wing only.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick
from _volume import _apply_cols, _wall

SX = 10.8
SZ = 7.2
PLATE = 2.50
RISE = 1.20
ROOF_X = SX * 0.5 + 0.38
ROOF_Z = SZ * 0.5 + 0.32
# Shells stop this far short of the ridge so the two slopes do not share a volume.
_RIDGE_STOP = 0.10


def _front_holes():
    """Along X. Garage door on +X, entry and two windows on the living wing."""
    return [
        (-3.55, 1.58, 1.10, 1.22, False, False),
        (-1.85, 1.58, 1.10, 1.22, False, False),
        (-0.15, 1.06, 0.90, 2.02, False, "solid"),
        (3.55, 1.115, 2.44, 2.13, False, "opening"),
    ]


def _side_holes(right):
    if right:
        return [(-0.7, 1.62, 0.86, 0.52, False, False)]
    return [
        (-1.55, 1.55, 0.96, 1.16, False, False),
        (1.55, 1.55, 0.96, 1.16, False, False),
    ]


def _back_holes():
    return [
        (-2.6, 1.55, 1.05, 1.16, False, False),
        (2.4, 1.55, 1.05, 1.16, True, False),
    ]


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


def _quad_shell(g, quad, uvs, mat, gap=0.006, thick=0.024):
    """Closed shell just outside a roof plane. quad is CCW from above."""
    n = _normal(quad[0], quad[1], quad[3])
    inner = [_add(p, n, gap) for p in quad]
    outer = [_add(p, n, gap + thick) for p in quad]
    verts = inner + outer
    faces = [
        (4, 5, 6, 7),
        (0, 3, 2, 1),
        (0, 1, 5, 4),
        (1, 2, 6, 5),
        (2, 3, 7, 6),
        (3, 0, 4, 7),
    ]
    uv = list(uvs) + list(uvs)
    g.quad(verts, faces, uv, mat)


def _tri_shell(g, tri, uvs, mat, gap=0.006, thick=0.024):
    n = _normal(tri[0], tri[1], tri[2])
    inner = [_add(p, n, gap) for p in tri]
    outer = [_add(p, n, gap + thick) for p in tri]
    verts = inner + outer
    faces = [
        (3, 4, 5),
        (0, 2, 1),
        (0, 1, 4, 3),
        (1, 2, 5, 4),
        (2, 0, 3, 5),
    ]
    g.quad(verts, faces, list(uvs) + list(uvs), mat)


def _hip_solid(g):
    """Painted soffit and a closed hip. Shingle shells sit outside the slopes."""
    hx, hz = ROOF_X, ROOF_Z
    ridge = hx * 0.42
    y0 = PLATE + 0.01
    y1 = PLATE + RISE
    verts = [
        (-hx, y0, -hz),
        (hx, y0, -hz),
        (hx, y0, hz),
        (-hx, y0, hz),
        (-ridge, y1, 0.0),
        (ridge, y1, 0.0),
    ]
    faces = [
        (0, 1, 2, 3),
        (0, 4, 5, 1),
        (3, 2, 5, 4),
        (1, 5, 2),
        (0, 3, 4),
    ]
    g.mesh(verts, faces, "Lib_PaintWhite", uv_scale=0.5)


def _shingles(g):
    """One shell per slope. The long slopes stop short of the hip crease."""
    hx, hz = ROOF_X, ROOF_Z
    ridge = hx * 0.42
    y0 = PLATE + 0.01
    y1 = PLATE + RISE
    # Air along the hip so the long shell and the end shell do not share a volume.
    crease = 0.06
    run_z = hz - _RIDGE_STOP
    t_z = run_z / hz
    y_top = y0 + (y1 - y0) * t_z
    x_hip = hx + (ridge - hx) * t_z
    x_pos = x_hip - crease
    x_eave = hx - crease
    slope_z = math.hypot(run_z, y_top - y0)
    front = [
        (-x_eave, y0, hz),
        (x_eave, y0, hz),
        (x_pos, y_top, _RIDGE_STOP),
        (-x_pos, y_top, _RIDGE_STOP),
    ]
    _quad_shell(g, front, [
        (front[0][0], 0.0),
        (front[1][0], 0.0),
        (front[2][0], slope_z),
        (front[3][0], slope_z),
    ], "Lib_RanchRoof")
    back = [
        (x_eave, y0, -hz),
        (-x_eave, y0, -hz),
        (-x_pos, y_top, -_RIDGE_STOP),
        (x_pos, y_top, -_RIDGE_STOP),
    ]
    _quad_shell(g, back, [
        (back[0][0], 0.0),
        (back[1][0], 0.0),
        (back[2][0], slope_z),
        (back[3][0], slope_z),
    ], "Lib_RanchRoof")
    # Hip ends. The base stays off the crease. The tip stops short of the ridge.
    run_x = (hx - ridge) - 0.12
    t_x = run_x / (hx - ridge)
    y_end = y0 + (y1 - y0) * t_x
    x_end = hx + (ridge - hx) * t_x
    z_base = hz - crease
    end_len = math.hypot(hx - x_end, y_end - y0)
    _tri_shell(g, [
        (hx, y0, -z_base),
        (hx, y0, z_base),
        (x_end, y_end, 0.0),
    ], [(-z_base, 0.0), (z_base, 0.0), (0.0, end_len)], "Lib_RanchRoof")
    _tri_shell(g, [
        (-hx, y0, z_base),
        (-hx, y0, -z_base),
        (-x_end, y_end, 0.0),
    ], [(z_base, 0.0), (-z_base, 0.0), (0.0, end_len)], "Lib_RanchRoof")


def _ridge_and_chimney(g, cols, lod):
    """Ridge cap split around a brick stack on the living-wing ridge."""
    ridge = ROOF_X * 0.42
    y = PLATE + RISE + 0.055
    cx = -1.70
    # Cap stays inside the shingle gap at the ridge (shells end at |z| = 0.10).
    cap_z = 0.12
    for x0, x1 in ((-ridge + 0.08, cx - 0.34), (cx + 0.34, ridge - 0.08)):
        g.box(((x0 + x1) * 0.5, y, 0.0), (x1 - x0, 0.05, cap_z), "Lib_RanchRoof", uv_scale=1.0)
    # Shaft clears the shingles, which fall away below y=3.70 at the shaft's front face.
    g.box((cx, 4.36, 0.0), (0.42, 1.20, 0.42), "Lib_Brick", uv_scale=1.4)
    g.box((cx, 5.02, 0.0), (0.56, 0.08, 0.56), "Lib_Concrete", uv_scale=0.6)
    if lod == 0:
        # 4 mm above the cap so the flue and the cap are separate shells.
        g.box((cx, 5.164, 0.0), (0.18, 0.20, 0.18), "Lib_Brick", uv_scale=1.2)
    cols.append(("box", "Col_Chimney", (cx, 4.36, 0.0), (0.30, 1.00, 0.30)))
    # Flashing on each long slope, above the shingle shell and clear of the shaft.
    _flashing(g, cx, 1.0)
    _flashing(g, cx, -1.0)


def _flashing(g, cx, sign):
    """A brick saddle from the shaft down the slope. It does not enter the shingles."""
    hz = ROOF_Z
    y0 = PLATE + 0.01
    y1 = PLATE + RISE
    z_near, z_far = 0.28, 0.62

    def y_at(z):
        t = 1.0 - abs(z) / hz
        return y0 + (y1 - y0) * t + 0.055

    quad = [
        (cx - 0.26, y_at(z_near), sign * z_near),
        (cx + 0.26, y_at(z_near), sign * z_near),
        (cx + 0.26, y_at(z_far), sign * z_far),
        (cx - 0.26, y_at(z_far), sign * z_far),
    ]
    if sign < 0.0:
        quad = [quad[1], quad[0], quad[3], quad[2]]
    # Shingle shells occupy 6–30 mm outside the plane. This saddle starts past them.
    _quad_shell(g, quad, [(0.0, 0.0), (0.5, 0.0), (0.5, 0.4), (0.0, 0.4)], "Lib_Brick", gap=0.036, thick=0.016)


def _garage_door(g, lod):
    """Closed overhead door. The panels and jambs fill the opening."""
    # Hole is x 2.33..4.77, y 0.05..2.18. Panels stay inside it, clear of the jambs.
    panels = lod_pick(lod, 4, 2, 1)
    top, bot = 2.08, 0.10
    span = top - bot
    for i in range(panels):
        y = bot + (i + 0.5) * span / panels
        h = span / panels * 0.90
        g.box((3.55, y, 3.505), (2.20, h, 0.030), "Lib_PaintWhite")
    # Jambs in the opening, in front of the panels, still inside the wall face (z=3.60).
    g.box((2.40, 1.12, 3.565), (0.055, 2.02, 0.028), "Lib_PaintWhite")
    g.box((4.70, 1.12, 3.565), (0.055, 2.02, 0.028), "Lib_PaintWhite")
    g.box((3.55, 2.145, 3.565), (2.36, 0.055, 0.028), "Lib_PaintWhite")
    if lod == 0:
        g.box((4.42, 1.05, 3.528), (0.035, 0.09, 0.014), "Lib_Black")
        g.box((3.55, 2.30, 3.66), (2.70, 0.07, 0.035), "Lib_PaintWhite")


def _rail_run(g, cols, a, b, along, lod):
    """Top rail, bottom rail, and balusters. `along` is 0 for X and 2 for Z."""
    ax, ay, az = a
    bx, by, bz = b
    span = math.sqrt((bx - ax) ** 2 + (bz - az) ** 2)
    if span < 0.12:
        return
    mx, mz = (ax + bx) * 0.5, (az + bz) * 0.5
    deck_top = 0.19
    top_y = deck_top + 0.95 - 0.020
    bot_y = deck_top + 0.16
    if along == 0:
        size_top = (span, 0.040, 0.036)
        size_bot = (span, 0.032, 0.028)
    else:
        size_top = (0.036, 0.040, span)
        size_bot = (0.028, 0.032, span)
    g.box((mx, top_y, mz), size_top, "Lib_PaintWhite")
    g.box((mx, bot_y, mz), size_bot, "Lib_PaintWhite")
    cols.append(("box", "Col_RailTop", (mx, top_y, mz), (
        size_top[0] - 0.02, 0.026, size_top[2] - 0.02
    )))
    cols.append(("box", "Col_RailBot", (mx, bot_y, mz), (
        size_bot[0] - 0.02, 0.020, size_bot[2] - 0.02
    )))
    if lod == 0 and span > 0.28:
        cols.append(("cap", "Vault_PorchRail", (mx, top_y, mz), 0.012, span - 0.04, along))
    if lod >= 2:
        return
    # Between the rails, with 4 mm of air at each end.
    bal_bot = bot_y + 0.016 + 0.004
    bal_top = top_y - 0.020 - 0.004
    bal_h = bal_top - bal_bot
    bal_y = (bal_bot + bal_top) * 0.5
    count = max(2, int(span / 0.10))
    for i in range(count):
        t = (i + 0.5) / count
        x = ax + (bx - ax) * t
        z = az + (bz - az) * t
        g.box((x, bal_y, z), (0.020, bal_h, 0.020), "Lib_PaintWhite")
        if lod == 0:
            cols.append(("box", "Col_Baluster", (x, bal_y, z), (0.012, bal_h - 0.08, 0.012)))


def _porch(g, cols, lod):
    """Deck, roof, posts, and a rail with a gap at the steps. Living wing only."""
    deck_z0, deck_z1 = 3.608, 5.05
    deck_x0, deck_x1 = -4.55, 0.85
    cx = (deck_x0 + deck_x1) * 0.5
    cz = (deck_z0 + deck_z1) * 0.5
    g.box((cx, 0.14, cz), (deck_x1 - deck_x0, 0.10, deck_z1 - deck_z0), "Lib_Wood", uv_scale=1.1)
    cols.append(("box", "Col_Porch", (cx, 0.14, cz), (deck_x1 - deck_x0 - 0.08, 0.06, deck_z1 - deck_z0 - 0.08)))
    roof_z0, roof_z1 = 3.62, deck_z1 + 0.08
    roof_x0, roof_x1 = deck_x0 - 0.10, deck_x1 + 0.10
    rx = (roof_x0 + roof_x1) * 0.5
    rz = (roof_z0 + roof_z1) * 0.5
    roof_y = 2.36
    g.box((rx, roof_y, rz), (roof_x1 - roof_x0, 0.07, roof_z1 - roof_z0), "Lib_RanchRoof", uv_scale=1.0)
    cols.append((
        "box", "Col_PorchRoof", (rx, roof_y, rz),
        (roof_x1 - roof_x0 - 0.16, 0.04, roof_z1 - roof_z0 - 0.16),
    ))
    # Posts tie the roof to the deck. Rails stop short of them.
    posts = (
        (-4.45, 4.95),
        (-0.72, 4.95),
        (0.70, 4.95),
        (0.70, 3.78),
    )
    for x, z in posts:
        g.box((x, 1.26, z), (0.12, 2.08, 0.12), "Lib_PaintWhite")
        cols.append(("box", "Col_Post", (x, 1.26, z), (0.08, 1.90, 0.08)))
    if lod < 2:
        # Rails share the post centerline and stop 4 mm short of the post face.
        inset = 0.06 + 0.004
        z = 4.95
        _rail_run(g, cols, (-4.45 + inset, 0.0, z), (-0.72 - inset, 0.0, z), 0, lod)
        x = 0.70
        _rail_run(g, cols, (x, 0.0, 4.95 - inset), (x, 0.0, 3.78 + inset), 2, lod)


def _steps(g, cols):
    g.box((-0.15, 0.10, 5.28), (1.15, 0.08, 0.32), "Lib_Concrete", uv_scale=0.7)
    g.box((-0.15, 0.035, 5.62), (1.35, 0.05, 0.32), "Lib_Concrete", uv_scale=0.7)
    cols.append(("box", "Col_StepHigh", (-0.15, 0.10, 5.28), (1.05, 0.05, 0.24)))
    cols.append(("box", "Col_StepLow", (-0.15, 0.035, 5.62), (1.25, 0.03, 0.24)))
    g.box((3.55, 0.02, 4.55), (3.10, 0.04, 1.70), "Lib_Concrete", uv_scale=0.6)
    cols.append(("box", "Col_Apron", (3.55, 0.02, 4.55), (2.90, 0.028, 1.50)))
    # Walk from the bottom step to the driveway apron. 4 cm of air at each end.
    g.box((1.28, 0.025, 5.55), (1.36, 0.04, 0.90), "Lib_Concrete", uv_scale=0.7)
    cols.append(("box", "Col_Walk", (1.28, 0.025, 5.55), (1.24, 0.026, 0.76)))


def _roof_landing(asset, name, z0, y0, z1, y1, width):
    """A box under the shingle, inside the hip, on the long slope."""
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    nz, ny = -dy / length, dz / length
    if ny < 0:
        nz, ny = -nz, -ny
    thick = 0.24
    cz = (z0 + z1) * 0.5 - nz * thick * 0.42
    cy = (y0 + y1) * 0.5 - ny * thick * 0.42
    angle = math.degrees(math.atan2(abs(dy), abs(dz)))
    pitch = -angle if dz * dy > 0 else angle
    asset.box(name, (0.0, cy, cz), (width * 0.62, thick * 0.36, length * 0.50), euler=(pitch, 0.0, 0.0))


def _end_landing(asset, name, sign):
    """A box inside the hip-end triangle, on the slope."""
    hx = ROOF_X
    ridge = hx * 0.42
    y0 = PLATE + 0.01
    y1 = PLATE + RISE
    t = 0.40
    x = (sign * hx) + (sign * ridge - sign * hx) * t
    y = y0 + (y1 - y0) * t
    dx = abs(hx - ridge)
    dy = y1 - y0
    length = math.hypot(dx, dy)
    nx, ny = dy / length, dx / length
    inset = 0.11
    cx = x - sign * nx * inset
    cy = y - ny * inset
    angle = math.degrees(math.atan2(dy, dx))
    roll = -sign * angle
    asset.box(name, (cx, cy, 0.0), (1.05, 0.055, 0.80), euler=(0.0, 0.0, roll))


@register
def create():
    a = Asset(
        "Ranch_House",
        "Buildings",
        "Single-story ranch, 10.8 x 7.2 m, plate 2.50 m, hip ridge about 3.70 m. "
        "Attached single garage on +X with a closed overhead door. "
        "Porch, baluster rail, and steps on the living wing, +Z, with a walk to the driveway. "
        "Siding walls are cling. All four hip slopes are landings. "
        "A brick chimney stands on the living-wing ridge. The porch rail is 0.95 m above the deck.",
    )
    a.loose_pivot = True
    a.climbable = True
    a.vaultable = True
    a.vault_height = 0.95
    a.climb_note = "Siding walls are cling. Glass and the garage door are solid. The porch is open on +Z."
    a.vault_note = "Porch rail is 0.95 m above the porch deck (deck top 0.19 m, rail top 1.14 m)."
    hx, hz = SX * 0.5, SZ * 0.5
    span_x = SX - 0.44 - 0.012
    span_z = SZ - 0.44 - 0.012
    body, trim = "Lib_Siding", "Lib_PaintWhite"
    for lod in (0, 1, 2):
        g = a.begin(lod)
        cols = []
        _wall(g, cols, "z", hz - 0.11, -1.0, span_x, 0.0, PLATE, _front_holes(), body, trim, lod, "Climb_Front")
        _wall(g, cols, "z", -hz + 0.11, 1.0, span_x, 0.0, PLATE, _back_holes(), body, trim, lod, "Climb_Back")
        _wall(g, cols, "x", hx - 0.11, -1.0, span_z, 0.0, PLATE, _side_holes(True), body, trim, lod, "Climb_Right")
        _wall(g, cols, "x", -(hx - 0.11), 1.0, span_z, 0.0, PLATE, _side_holes(False), body, trim, lod, "Climb_Left")
        _garage_door(g, lod)
        _porch(g, cols, lod)
        _steps(g, cols)
        _hip_solid(g)
        if lod < 2:
            _shingles(g)
        _ridge_and_chimney(g, cols, lod)
        if lod == 0:
            # The low panel under the side window sits against the sill. Its full
            # climb box sends a ray through the casing corner and the parity flips.
            eased = []
            for item in cols:
                if item[0] == "Climb_Right" and item[1][1] < 1.0:
                    c, s = item[1], item[2]
                    eased.append((
                        item[0],
                        (c[0], c[1] - 0.06, c[2]),
                        (s[0], max(0.20, s[1] - 0.16), max(0.20, s[2] - 0.16)),
                    ))
                else:
                    eased.append(item)
            cols = eased
            kept = []
            for item in cols:
                if item[0] == "cap":
                    a.capsule(item[1], item[2], item[3], item[4], item[5])
                else:
                    kept.append(item)
            a._cols = kept
        a.end()
    _apply_cols(a)
    y0 = PLATE + 0.01
    y1 = PLATE + RISE
    _roof_landing(a, "Col_RoofS", ROOF_Z, y0, 0.0, y1, ROOF_X * 0.84)
    _roof_landing(a, "Col_RoofN", -ROOF_Z, y0, 0.0, y1, ROOF_X * 0.84)
    _end_landing(a, "Col_RoofE", 1.0)
    _end_landing(a, "Col_RoofW", -1.0)
    return a
