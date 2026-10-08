"""Small log cabin. 4.6 x 3.6 m, porch on +Z, weathered cedar-shake gable.

Logs are round and saddle-notched at the corners. The door is vertical boards.
The porch shed sits on a ledger outside the logs, under the main eave. A brick
chimney rises through the ridge. The shakes and the ridge run up to the flashing.
"""

import math
import os
import sys

import bmesh

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import Asset, register, lod_pick, unity_to_blender

# Outer face of the wall, just inside the nominal footprint.
_OUT_X = 4.6 * 0.5 - 0.02
_OUT_Z = 3.6 * 0.5 - 0.02
_Y0 = 0.24
_Y1 = 2.16
_TAIL = 0.20
# One leaf, about 2.0 m, left of a small window.
_DOOR = (-0.72, 0.32, 0.08, 2.08)
_WIN = (0.68, 1.36, 1.12, 1.82)
# One window on each side wall: z0, z1, y0, y1.
_SIDE_WIN = (-0.34, 0.34, 1.06, 1.78)
# Ledger sits outside the log face. The shed starts on its outer edge.
_LEDGER_Z0 = _OUT_Z + 0.006
_LEDGER_D = 0.08
_LEDGER_Y = 1.90
_SHED_Z0 = _LEDGER_Z0 + _LEDGER_D + 0.004
_SHED_Y0 = _LEDGER_Y + 0.056
_SHED_Z1 = 3.27
_SHED_Y1 = 1.886
# Chimney through the ridge. Shakes stop a few centimetres out; flashing covers the joint.
_CH_X = 0.70
_CH_Z = 0.0
_CH_SX = 0.66
_CH_SZ = 0.56
_HOLE_X = _CH_SX * 0.5 + 0.030
_HOLE_Z = _CH_SZ * 0.5 + 0.030
_SHAKE_N = 10
_SHAKE_BUTT = 0.028
_SHAKE_HEAD = 0.010
_SHAKE_UNDER = 0.016


@register
def create():
    a = Asset(
        "Cabin",
        "Buildings",
        "Log cabin 4.6 x 3.6 m, walls 2.2 m, ridge at 3.45 m, porch on +Z. "
        "Saddle-notched round logs, a vertical-board door, board-and-batten gables, "
        "and a side window on each wall. Cedar shakes, about 25 cm exposure, run to the "
        "flashing on both slopes, with a ridge that meets the chimney. Stepped flashing, "
        "a concrete cap, and a flue. "
        "The porch has a top rail and a mid rail between the posts.",
    )
    a.climbable = True
    a.climb_note = "Log walls are cling. Door is closed. Roof slopes are landings."
    a.vault_note = "Porch top rail is 1.20 m. The deck top is 0.17 m."
    a.vaultable = True
    a.vault_height = 1.02
    width, depth = 4.6, 3.6
    for lod in (0, 1, 2):
        g = a.begin(lod)
        courses = lod_pick(lod, 9, 6, 4)
        seg = lod_pick(lod, 6, 6, 6)
        side_seg = lod_pick(lod, 8, 6, 6)
        step = (_Y1 - _Y0) / max(1, courses - 1)
        radius = max(0.132, step * 0.5 + 0.02)
        _walls(g, lod, courses, radius, step, seg, side_seg)
        _door_and_window(g, lod, radius)
        if lod == 0:
            _side_windows(g, radius)
            _gable_ends(g, radius, step, courses)
            _chimney(g)
        g.box((0, 0.05, depth * 0.5 + 1.95), (1.15, 0.08, 0.32), "Lib_Wood", grain=1.0)
        g.box((0, 0.14, depth * 0.5 + 1.62), (1.20, 0.10, 0.28), "Lib_Wood", grain=1.0)
        g.box((0, 0.12, depth * 0.5 + 0.7), (3.2, 0.10, 1.3), "Lib_Wood", bevel=0.004 if lod == 0 else 0, segs=1, uv_scale=1.0, grain=1.0)
        for x in (-1.3, 1.3):
            # Stops a few millimetres under the beam so the two shells do not overlap.
            g.box((x, 0.965, depth * 0.5 + 1.25), (0.12, 1.582, 0.12), "Lib_WoodDark")
        # Beam under the low end of the shed. The shed sits on it.
        g.box((0, 1.82, 3.16), (2.84, 0.12, 0.22), "Lib_WoodDark", grain=1.0)
        # Ledger outside the logs, under the main eave. It does not enter the wall.
        g.box((0, _LEDGER_Y, _LEDGER_Z0 + _LEDGER_D * 0.5), (3.20, 0.10, _LEDGER_D), "Lib_WoodDark", grain=1.0)
        if lod < 2:
            # Top rail and one mid rail between the posts, clear of the posts and the beam.
            rail_z = depth * 0.5 + 1.22
            g.box((0, 1.16, rail_z), (2.44, 0.07, 0.045), "Lib_WoodDark", grain=1.0)
            g.box((0, 0.72, rail_z), (2.44, 0.055, 0.040), "Lib_Wood", grain=1.0)
        _roof(g, width + 0.4, lod)
        a.end()
    a.loose_pivot = True
    _log_climb(a)
    dx0, dx1, dy0, dy1 = _DOOR
    a.box("Col_Door", ((dx0 + dx1) * 0.5, (dy0 + dy1) * 0.5, _OUT_Z - 0.132), (dx1 - dx0 - 0.08, dy1 - dy0 - 0.06, 0.04))
    a.box("Col_StepLow", (0, 0.05, depth * 0.5 + 1.95), (1.00, 0.05, 0.24))
    a.box("Col_StepHigh", (0, 0.15, depth * 0.5 + 1.62), (1.05, 0.06, 0.20))
    a.box("Col_Porch", (0, 0.12, depth * 0.5 + 0.7), (3.2, 0.10, 1.3))
    center, size, euler = _roof_box(_SHED_Z0, _SHED_Y0, _SHED_Z1, _SHED_Y1, 0.008, 2.6)
    a.box("Col_PorchRoof", center, size, euler=euler)
    a.capsule("Vault_PorchRail", (0, 1.16, depth * 0.5 + 1.22), 0.018, 2.20, 0)
    _add_roof(a, width + 0.4, -2.05, 2.15, 0.0, 3.45, 0.008)
    # Footing and shaft block a runner. They do not overlap each other or the roof boxes.
    a.box("Col_ChimneyFoot", (_CH_X, 0.14, _CH_Z), (0.76, 0.22, 0.66))
    a.box("Col_Chimney", (_CH_X, 2.16, _CH_Z), (0.50, 3.48, 0.42))
    return a


def _walls(g, lod, courses, radius, step, seg, side_seg):
    valley = math.sqrt(max(0.0, radius * radius - (step * 0.5) ** 2))
    for i in range(courses):
        y = _Y0 + i * step
        proud = (i % 2 == 0)
        holes = _front_holes(y, radius)
        _run_x(g, lod, i, y, radius, seg, _OUT_Z - radius, proud, holes, 0)
        _run_x(g, lod, i, y, radius, seg, -(_OUT_Z - radius), proud, [], 1)
        # Side logs sit half a course higher, so each corner is a saddle, not a butt.
        y_side = y + step * 0.5
        if y_side <= _Y1 - step * 0.15:
            side_holes = _side_holes(y_side, radius)
            _run_z(g, lod, i, y_side, radius, side_seg, -(_OUT_X - radius), not proud, side_holes, 2)
            _run_z(g, lod, i, y_side, radius, side_seg, _OUT_X - radius, not proud, side_holes, 3)
            if y_side + step <= _Y1:
                _chink_sides(g, y_side + step * 0.5, radius, valley)
        if i + 1 < courses:
            _chink(g, y + step * 0.5, radius, valley, holes)


def _front_holes(y, radius):
    holes = []
    if y - radius < _DOOR[3] and y + radius > _DOOR[2]:
        holes.append((_DOOR[0] - 0.06, _DOOR[1] + 0.06))
    if y - radius < _WIN[3] and y + radius > _WIN[2]:
        holes.append((_WIN[0] - 0.06, _WIN[1] + 0.06))
    return holes


def _side_holes(y, radius):
    z0, z1, y0, y1 = _SIDE_WIN
    if y - radius < y1 and y + radius > y0:
        return [(z0 - 0.06, z1 + 0.06)]
    return []


def _spans(x0, x1, holes):
    spans = [(x0, x1)]
    for a, b in holes:
        nxt = []
        for s0, s1 in spans:
            if b <= s0 or a >= s1:
                nxt.append((s0, s1))
                continue
            if s0 < a - 0.02:
                nxt.append((s0, a))
            if b + 0.02 < s1:
                nxt.append((b, s1))
        spans = nxt
    return [(s0, s1) for s0, s1 in spans if s1 - s0 > 0.16]


_LOG_MATS = ("Lib_Log0", "Lib_Log1", "Lib_Log2", "Lib_Log3", "Lib_Log4")


def _log_mat(course, wall):
    return _LOG_MATS[(course * 3 + wall) % len(_LOG_MATS)]


def _run_x(g, lod, course, y, radius, seg, z, proud, holes, wall):
    mat = _log_mat(course, wall)
    reach = _OUT_X + (_TAIL if proud else -radius)
    for x0, x1 in _spans(-reach, reach, holes):
        g.cylinder(((x0 + x1) * 0.5, y, z), radius, x1 - x0, mat, seg, axis="X")
        if lod == 0 and proud:
            if x0 < -_OUT_X:
                _end_disc(g, (x0, y, z), "X", radius * 0.98, -1)
            if x1 > _OUT_X:
                _end_disc(g, (x1, y, z), "X", radius * 0.98, 1)


def _run_z(g, lod, course, y, radius, seg, x, proud, holes, wall):
    mat = _log_mat(course, wall)
    reach = _OUT_Z + (_TAIL if proud else -radius)
    for z0, z1 in _spans(-reach, reach, holes):
        g.cylinder((x, y, (z0 + z1) * 0.5), radius, z1 - z0, mat, seg, axis="Z")
        if lod == 0 and proud:
            if z0 < -_OUT_Z:
                _end_disc(g, (x, y, z0), "Z", radius * 0.98, -1)
            if z1 > _OUT_Z:
                _end_disc(g, (x, y, z1), "Z", radius * 0.98, 1)


def _chink(g, y, radius, valley, holes):
    """Grey-tan bead in the groove. It sits outside the log shells, shy of the crown."""
    thick = 0.016
    band = 0.028
    # Valley is the dip. A few centimetres proud of that, still inside the round crown.
    proud = min(0.030, (radius - valley) * 0.45)
    outer = valley + proud
    z_front = (_OUT_Z - radius) + outer - thick * 0.5
    z_back = -z_front
    for x0, x1 in _spans(-_OUT_X + 0.20, _OUT_X - 0.20, holes):
        g.box(((x0 + x1) * 0.5, y, z_front), (x1 - x0, band, thick), "Lib_Chink")
        # Back bead omitted so the shake courses stay inside the triangle budget.


def _chink_sides(g, y, radius, valley):
    """Mortar on the side walls, stopped short of the saddle so it stays one shell."""
    thick = 0.016
    band = 0.028
    proud = min(0.030, (radius - valley) * 0.45)
    outer = valley + proud
    x_side = (_OUT_X - radius) + outer - thick * 0.5
    for z0, z1 in _spans(-(_OUT_Z - 0.55), _OUT_Z - 0.55, _side_holes(y, radius)):
        g.box((-x_side, y, (z0 + z1) * 0.5), (thick, band, z1 - z0), "Lib_Chink")
        g.box((x_side, y, (z0 + z1) * 0.5), (thick, band, z1 - z0), "Lib_Chink")


def _end_disc(g, center, axis, radius, sign):
    """Closed end. Two growth ridges stand proud of the cut so it is not a flat disc."""
    steps = 4
    # Fraction of the radius, then extra metres proud of the cut. Two ridges.
    profile = (
        (0.16, 0.000),
        (0.34, 0.014),
        (0.50, 0.000),
        (0.72, 0.014),
        (1.00, 0.000),
    )
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    # Entirely outside the log cap, so the two closed shells do not overlap.
    base = sign * 0.008
    inner = sign * 0.003

    def point(along, frac, ca, sa):
        rad = radius * frac
        if axis == "X":
            return (center[0] + along, center[1] + rad * ca, center[2] + rad * sa)
        return (center[0] + rad * ca, center[1] + rad * sa, center[2] + along)

    rings = []
    uvs = []
    for frac, proud in profile:
        ring = []
        uv_ring = []
        along = base + sign * proud
        for i in range(steps):
            ang = 2.0 * math.pi * i / steps
            ca, sa = math.cos(ang), math.sin(ang)
            ring.append(bm.verts.new(unity_to_blender(*point(along, frac, ca, sa))))
            uv_ring.append((0.5 + 0.48 * frac * ca, 0.5 + 0.48 * frac * sa))
        rings.append(ring)
        uvs.append(uv_ring)
    hub = bm.verts.new(unity_to_blender(*point(base, 0.0, 0.0, 0.0)))
    inner_ring = []
    for i in range(steps):
        ang = 2.0 * math.pi * i / steps
        ca, sa = math.cos(ang), math.sin(ang)
        inner_ring.append(bm.verts.new(unity_to_blender(*point(inner, 1.0, ca, sa))))
    hub_i = bm.verts.new(unity_to_blender(*point(inner, 0.0, 0.0, 0.0)))
    for i in range(steps):
        j = (i + 1) % steps
        if sign > 0:
            face = bm.faces.new((hub, rings[0][i], rings[0][j]))
            order = ((0.5, 0.5), uvs[0][i], uvs[0][j])
            for loop, uv in zip(face.loops, order):
                loop[uv_layer].uv = uv
            for a in range(len(profile) - 1):
                face = bm.faces.new((rings[a][i], rings[a][j], rings[a + 1][j], rings[a + 1][i]))
                order = (uvs[a][i], uvs[a][j], uvs[a + 1][j], uvs[a + 1][i])
                for loop, uv in zip(face.loops, order):
                    loop[uv_layer].uv = uv
            bm.faces.new((hub_i, inner_ring[j], inner_ring[i]))
            bm.faces.new((rings[-1][i], inner_ring[i], inner_ring[j], rings[-1][j]))
        else:
            face = bm.faces.new((hub, rings[0][j], rings[0][i]))
            order = ((0.5, 0.5), uvs[0][j], uvs[0][i])
            for loop, uv in zip(face.loops, order):
                loop[uv_layer].uv = uv
            for a in range(len(profile) - 1):
                face = bm.faces.new((rings[a][j], rings[a][i], rings[a + 1][i], rings[a + 1][j]))
                order = (uvs[a][j], uvs[a][i], uvs[a + 1][i], uvs[a + 1][j])
                for loop, uv in zip(face.loops, order):
                    loop[uv_layer].uv = uv
            bm.faces.new((hub_i, inner_ring[i], inner_ring[j]))
            bm.faces.new((rings[-1][j], inner_ring[j], inner_ring[i], rings[-1][i]))
    g._ingest(bm, "Lib_LogEnd", -1.0)


def _door_and_window(g, lod, radius):
    z = _OUT_Z - radius
    dx0, dx1, dy0, dy1 = _DOOR
    cx = (dx0 + dx1) * 0.5
    cy = (dy0 + dy1) * 0.5
    g.box((dx0 - 0.04, cy, z), (0.08, dy1 - dy0 + 0.08, 0.10), "Lib_Batten")
    g.box((dx1 + 0.04, cy, z), (0.08, dy1 - dy0 + 0.08, 0.10), "Lib_Batten")
    g.box((cx, dy1 + 0.04, z), (dx1 - dx0 + 0.16, 0.08, 0.10), "Lib_Batten")
    g.box((cx, dy0 - 0.02, z), (dx1 - dx0 + 0.16, 0.06, 0.10), "Lib_Batten")
    # One leaf of vertical boards, about 13 cm, no horizontal plank tile.
    leaf0 = dx0 + 0.05
    leaf1 = dx1 - 0.05
    board = 0.135
    gap = 0.008
    n = max(1, int(round((leaf1 - leaf0) / board)))
    pitch = (leaf1 - leaf0) / n
    y_leaf0 = dy0 + 0.04
    y_leaf1 = dy1 - 0.04
    for i in range(n):
        px = leaf0 + pitch * (i + 0.5)
        g.box((px, (y_leaf0 + y_leaf1) * 0.5, z + 0.01), (pitch - gap, y_leaf1 - y_leaf0, 0.028), _LOG_MATS[i % 5])
    if lod == 0:
        _z_brace(g, leaf0, leaf1, y_leaf0, y_leaf1, z + 0.040)
        _hinge(g, leaf0 + 0.02, 0.48, z + 0.036)
        _hinge(g, leaf0 + 0.02, 1.55, z + 0.036)
        # Latch on the lock stile, clear of the boards and the brace.
        g.box((leaf1 - 0.05, 1.05, z + 0.058), (0.07, 0.028, 0.010), "Lib_SteelDark")
        g.box((leaf1 - 0.015, 1.05, z + 0.074), (0.028, 0.07, 0.012), "Lib_Steel")
    wx0, wx1, wy0, wy1 = _WIN
    wcx, wcy = (wx0 + wx1) * 0.5, (wy0 + wy1) * 0.5
    g.box((wcx, wcy, z), (wx1 - wx0, wy1 - wy0, 0.06), "Lib_Batten")
    g.box((wcx, wcy, z + 0.02), (wx1 - wx0 - 0.10, wy1 - wy0 - 0.10, 0.02), "Lib_ShopGlass")


def _z_brace(g, x0, x1, y0, y1, z):
    """Top rail, bottom rail, and the diagonal of a Z, in iron."""
    span = x1 - x0
    g.box(((x0 + x1) * 0.5, y0 + 0.06, z), (span * 0.92, 0.045, 0.012), "Lib_SteelDark")
    g.box(((x0 + x1) * 0.5, y1 - 0.06, z), (span * 0.92, 0.045, 0.012), "Lib_SteelDark")
    dx = span * 0.86
    dy = (y1 - y0) - 0.18
    length = math.hypot(dx, dy)
    angle = math.degrees(math.atan2(dy, dx))
    g.box(((x0 + x1) * 0.5, (y0 + y1) * 0.5, z), (length, 0.04, 0.012), "Lib_SteelDark", euler=(0.0, 0.0, angle))


def _hinge(g, x, y, z):
    # Strap and barrel stay a few millimetres apart so the shells do not overlap.
    g.box((x + 0.10, y, z), (0.14, 0.045, 0.010), "Lib_SteelDark")
    g.cylinder((x, y, z + 0.016), 0.014, 0.07, "Lib_Steel", 6, axis="Y")


def _side_windows(g, radius):
    """Frame and sill just outside the log face, one on each side wall."""
    z0, z1, y0, y1 = _SIDE_WIN
    zc = (z0 + z1) * 0.5
    yc = (y0 + y1) * 0.5
    zw = z1 - z0
    yh = y1 - y0
    for sign in (-1, 1):
        face = sign * _OUT_X
        x = face + sign * 0.024
        g.box((x, yc, z0 - 0.04), (0.032, yh + 0.10, 0.06), "Lib_Batten")
        g.box((x, yc, z1 + 0.04), (0.032, yh + 0.10, 0.06), "Lib_Batten")
        g.box((x, y1 + 0.04, zc), (0.032, 0.07, zw + 0.14), "Lib_Batten")
        g.box((x, y0 - 0.045, zc), (0.040, 0.055, zw + 0.18), "Lib_Batten")
        g.box((face + sign * 0.016, yc, zc), (0.016, yh - 0.08, zw - 0.08), "Lib_ShopGlass")
    del radius


def _roof_y(z):
    """Top of the main roof at this z. Ridge at 3.45, eaves at |z| = 2.05."""
    az = min(abs(z), 2.05)
    return 3.45 + (2.15 - 3.45) * (az / 2.05)


def _gable_ends(g, radius, step, courses):
    """Board-and-batten in each gable, under the roof and above the plate log."""
    top = _Y0
    for i in range(courses):
        y_side = _Y0 + i * step + step * 0.5
        if y_side <= _Y1 - step * 0.15:
            top = y_side
    base = top + radius + 0.012
    for sign in (-1, 1):
        _gable_boards(g, sign, base)


def _gable_boards(g, sign, base):
    """One closed panel under the roof, with battens over the seams."""
    x = sign * (_OUT_X - 0.06)
    _gable_panel(g, x, base, 0.020, "Lib_Log2")
    for z in (-1.05, -0.50, 0.50, 0.95):
        top = _roof_y(z) - 0.10
        if top > base + 0.12:
            _trap(g, x + sign * 0.020, z, z + 0.045, base, top, top, 0.010, "Lib_Batten")


def _gable_panel(g, x, base, thick, mat):
    """Closed gable. The top edge follows the roof, a few centimetres under it."""
    z_e = 1.42
    y_e = _roof_y(z_e) - 0.06
    y_p = _roof_y(0.0) - 0.07
    hx = thick * 0.5

    def side(s):
        return [
            (x + s * hx, base, -z_e),
            (x + s * hx, y_e, -z_e),
            (x + s * hx, y_p, 0.0),
            (x + s * hx, y_e, z_e),
            (x + s * hx, base, z_e),
        ]

    verts = side(-1) + side(1)
    faces = [
        (0, 1, 2), (0, 2, 4), (4, 2, 3),
        (5, 7, 6), (5, 9, 7), (9, 8, 7),
        (0, 5, 6, 1),
        (1, 6, 7, 2),
        (2, 7, 8, 3),
        (3, 8, 9, 4),
        (4, 9, 5, 0),
    ]
    g.mesh(verts, faces, mat, uv_scale=1.0)


def _trap(g, x, z0, z1, y0, y1a, y1b, thick, mat):
    """Closed vertical board. The top edge runs from (z0, y1a) to (z1, y1b)."""
    if z1 - z0 < 0.01 or y1a <= y0 + 0.02 or y1b <= y0 + 0.02:
        return
    hx = thick * 0.5
    verts = [
        (x - hx, y0, z0), (x - hx, y0, z1), (x - hx, y1b, z1), (x - hx, y1a, z0),
        (x + hx, y0, z0), (x + hx, y0, z1), (x + hx, y1b, z1), (x + hx, y1a, z0),
    ]
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 3, 7, 4),
        (1, 5, 6, 2),
        (3, 2, 6, 7),
        (0, 4, 5, 1),
    ]
    g.mesh(verts, faces, mat, uv_scale=1.0)


def _chimney(g):
    """Brick shaft through the ridge. Footing, cap, flue, and a stepped flashing ring."""
    x, z = _CH_X, _CH_Z
    sx, sz = _CH_SX, _CH_SZ
    # Footing and shaft overlap a few centimetres. Their colliders do not.
    g.box((x, 0.17, z), (sx + 0.26, 0.34, sz + 0.24), "Lib_Concrete", uv_scale=0.7)
    g.box((x, 2.16, z), (sx, 3.72, sz), "Lib_Brick", uv_scale=0.65)
    _flashing(g)
    # Concrete cap clear of the shaft, then a dark flue opening on top.
    g.box((x, 4.085, z), (sx + 0.16, 0.12, sz + 0.14), "Lib_Concrete")
    g.box((x, 4.190, z), (0.22, 0.08, 0.20), "Lib_Black")


def _flashing(g):
    """Aprons over the shake joint, then two stepped bands up the brick."""
    x, z = _CH_X, _CH_Z
    sx, sz = _CH_SX, _CH_SZ
    y_edge = _roof_y(_HOLE_Z) + _SHAKE_HEAD + 0.008
    # South and north aprons lap the thin head of the top course and stop short of the brick.
    for sign in (-1, 1):
        z_brick = z + sign * (sz * 0.5 + 0.006)
        z_roof = z + sign * (_HOLE_Z + 0.016)
        z0, z1 = (z_roof, z_brick) if sign < 0 else (z_brick, z_roof)
        y0 = _roof_y(z0) + _SHAKE_HEAD + 0.008
        y1 = _roof_y(z1) + _SHAKE_HEAD + 0.008
        _apron(g, x - sx * 0.5 + 0.02, x + sx * 0.5 - 0.02, z0, y0, z1, y1, 0.006)
    # East and west shoes where the ridge meets the brick. Clear of the shake mesh.
    for sign in (-1, 1):
        x_brick = x + sign * (sx * 0.5 + 0.006)
        x_roof = x + sign * _HOLE_X
        x0, x1 = (x_roof, x_brick) if sign < 0 else (x_brick, x_roof)
        _apron(g, x0, x1, -0.12, 3.42, 0.12, 3.42, 0.006)
    _band(g, y_edge + 0.08, 0.10, 0.016)
    _band(g, y_edge + 0.20, 0.09, 0.006)


def _band(g, y, height, gap):
    """Four plates around the shaft. Corners stay apart so the plates do not overlap."""
    x, z = _CH_X, _CH_Z
    sx, sz = _CH_SX, _CH_SZ
    thick = 0.008
    zf = z - sz * 0.5 - gap - thick * 0.5
    zb = z + sz * 0.5 + gap + thick * 0.5
    g.box((x, y, zf), (sx - 0.04, height, thick), "Lib_MetalWorn")
    g.box((x, y, zb), (sx - 0.04, height, thick), "Lib_MetalWorn")
    xf = x - sx * 0.5 - gap - thick * 0.5
    xb = x + sx * 0.5 + gap + thick * 0.5
    g.box((xf, y, z), (thick, height, sz - 0.04), "Lib_MetalWorn")
    g.box((xb, y, z), (thick, height, sz - 0.04), "Lib_MetalWorn")


def _apron(g, x0, x1, z0, y0, z1, y1, thick):
    verts = [
        (x0, y0, z0), (x0, y1, z1), (x1, y0, z0), (x1, y1, z1),
        (x0, y0 + thick, z0), (x0, y1 + thick, z1), (x1, y0 + thick, z0), (x1, y1 + thick, z1),
    ]
    g.mesh(verts, _prism_faces(), "Lib_MetalWorn", uv_scale=1.0)


def _log_climb(asset):
    """One box in the core of each log. Ends, the window, and the saddle stay outside it."""
    courses = 9
    step = (_Y1 - _Y0) / (courses - 1)
    radius = max(0.132, step * 0.5 + 0.02)
    z_front = _OUT_Z - radius
    z_back = -z_front
    x_side = _OUT_X - radius
    for i in range(courses):
        y = _Y0 + i * step
        holes = _front_holes(y, radius)
        reach = _OUT_X + (_TAIL if i % 2 == 0 else -radius)
        for x0, x1 in _spans(-reach, reach, holes):
            # Near the +X end, outer half of the log. A sample further back
            # drifts into the saddle and the ray count flips.
            x_hi = x1 - 0.22
            x_lo = max(x0 + 0.15, x_hi - 0.40)
            length = x_hi - x_lo
            if length < 0.28:
                continue
            asset.box(
                "Climb_Front_%d_%d" % (i, int((x0 + x1) * 10)),
                ((x_lo + x_hi) * 0.5, y, z_front + 0.07),
                (length, 0.03, 0.03),
            )
        asset.box("Climb_Back_%d" % i, (0.0, y, z_back - 0.07), (3.40, 0.03, 0.03))
        y_side = y + step * 0.5
        if y_side > _Y1 - step * 0.15:
            continue
        side_reach = _OUT_Z + (_TAIL if i % 2 == 1 else -radius)
        for z0, z1 in _spans(-side_reach, side_reach, _side_holes(y_side, radius)):
            # Keep the box out of the corner, where the saddle logs overlap.
            inner0 = max(z0, -(_OUT_Z - 0.55))
            inner1 = min(z1, _OUT_Z - 0.55)
            length = min(0.46, (inner1 - inner0) - 0.20)
            if length < 0.30:
                continue
            asset.box(
                "Climb_Side_%d_%d" % (i, int((z0 + z1) * 10)),
                (-x_side, y_side, (inner0 + inner1) * 0.5),
                (0.06, 0.06, length),
            )
            asset.box(
                "Climb_SideR_%d_%d" % (i, int((z0 + z1) * 10)),
                (x_side, y_side, (inner0 + inner1) * 0.5),
                (0.06, 0.06, length),
            )


def _outward(z0, y0, z1, y1):
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    nz, ny = -dy / length, dz / length
    if ny < 0.0:
        nz, ny = -nz, -ny
    return ny, nz, length


def _shake_pts(z0, y0, z1, y1, side, z_stop):
    """Sawtooth from the eave toward the ridge, cut at z_stop when that is set."""
    ny, nz, _length = _outward(z0, y0, z1, y1)
    n = _SHAKE_N
    t_max = 1.0
    if z_stop is not None:
        t_max = (z_stop - z0) / (z1 - z0)
        t_max = max(0.05, min(1.0, t_max))
    pts = []
    for i in range(n):
        t0 = i / n
        t1 = (i + 1) / n
        if t0 >= t_max - 1e-4:
            break
        t1 = min(t1, t_max)

        def put(t, offset, role):
            z = z0 + (z1 - z0) * t
            y = y0 + (y1 - y0) * t
            pts.append({
                "y": y + ny * offset,
                "z": z + nz * offset,
                "role": role,
                "course": i,
                "side": side,
                "v": 0.0 if role == "butt" else 1.0,
            })

        put(t0, _SHAKE_BUTT, "butt")
        put(t1, _SHAKE_HEAD, "head")
        if t1 >= t_max - 1e-6:
            break
    return pts, ny, nz


def _close_under(pts, z0, y0, z1, y1, ny, nz):
    """Return along the underside from the last outer point back to the eave."""
    under = []
    for src in (pts[-1], pts[0]):
        # Drop from the outer point back onto the base, then inside by _SHAKE_UNDER.
        # Use the eave and the cut as the underside ends so the solid stays simple.
        under.append(src)
    y_a = pts[-1]["y"] - ny * (_SHAKE_BUTT if pts[-1]["role"] == "butt" else _SHAKE_HEAD) - ny * _SHAKE_UNDER
    z_a = pts[-1]["z"] - nz * (_SHAKE_BUTT if pts[-1]["role"] == "butt" else _SHAKE_HEAD) - nz * _SHAKE_UNDER
    y_b = pts[0]["y"] - ny * _SHAKE_BUTT - ny * _SHAKE_UNDER
    z_b = pts[0]["z"] - nz * _SHAKE_BUTT - nz * _SHAKE_UNDER
    # The first point is always a butt. Recompute both from the base line to avoid drift.
    del y_a, z_a, y_b, z_b, under
    def base(p, role_off):
        return {
            "y": p["y"] - ny * role_off - ny * _SHAKE_UNDER,
            "z": p["z"] - nz * role_off - nz * _SHAKE_UNDER,
            "role": "under",
            "course": -1,
            "side": p["side"],
            "v": 0.62,
        }
    head_off = _SHAKE_HEAD
    butt_off = _SHAKE_BUTT
    far = base(pts[-1], head_off if pts[-1]["role"] == "head" else butt_off)
    near = base(pts[0], butt_off)
    return [far, near]


def _emit_shake(g, x0, x1, pts):
    """Extrude a closed profile in x. Course faces carry the shake UVs."""
    if x1 - x0 < 0.02 or len(pts) < 3:
        return
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    rings = []
    for x in (x0, x1):
        ring = []
        for p in pts:
            ring.append(bm.verts.new(unity_to_blender(x, p["y"], p["z"])))
        rings.append(ring)
    n = len(pts)

    def face_uv(face, i, j, xa, xb):
        pa, pb = pts[i], pts[j]
        course = pa["course"] if pa["course"] == pb["course"] and pa["course"] >= 0 else -1
        off = 0.0
        if course >= 0 and {pa["role"], pb["role"]} == {"butt", "head"}:
            off = _hash01(course, pa["side"], 5) * 0.63
        coords = []
        for p, x in ((pa, xa), (pb, xa), (pb, xb), (pa, xb)):
            coords.append((x + off, p["v"] if course >= 0 else p["v"]))
        # loop order is (i, j, n+j, n+i) = (xa,i), (xa,j), (xb,j), (xb,i)
        order = (coords[0], coords[1], coords[2], coords[3])
        for loop, uv in zip(face.loops, order):
            loop[uv_layer].uv = uv

    for i in range(n):
        j = (i + 1) % n
        f = bm.faces.new((rings[0][i], rings[0][j], rings[1][j], rings[1][i]))
        face_uv(f, i, j, x0, x1)
    cap0 = bm.faces.new([rings[0][i] for i in reversed(range(n))])
    cap1 = bm.faces.new([rings[1][i] for i in range(n)])
    for cap in (cap0, cap1):
        for loop in cap.loops:
            loop[uv_layer].uv = (0.2, 0.5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    g._ingest(bm, "Lib_Roof", -1.0)


def _hash01(a, b, c):
    return ((a * 374761393 + b * 668265263 + c * 1442695041) & 0x7FFFFFFF) / 2147483647.0


def _shake_slope(g, x0, x1, z0, y0, z1, y1, side, z_stop):
    pts, ny, nz = _shake_pts(z0, y0, z1, y1, side, z_stop)
    if len(pts) < 2:
        return
    pts.extend(_close_under(pts, z0, y0, z1, y1, ny, nz))
    _emit_shake(g, x0, x1, pts)


def _shake_gable(g, x0, x1):
    """Both slopes and the ridge, one solid, from x0 to x1."""
    south, ny_s, nz_s = _shake_pts(-2.05, 2.15, -0.02, _roof_y(0.02), 0, None)
    north, ny_n, nz_n = _shake_pts(2.05, 2.15, 0.02, _roof_y(0.02), 1, None)
    # North runs ridge to eave so the profile walks around the outside.
    north_rev = list(reversed(north))
    peak = {
        "y": 3.45 + _SHAKE_HEAD + 0.022,
        "z": 0.0,
        "role": "peak",
        "course": -1,
        "side": 0,
        "v": 0.0,
    }
    outer = south + [peak] + north_rev
    # Underside: south eave, south ridge, north ridge, north eave.
    def under_of(p, ny, nz, off):
        return {
            "y": p["y"] - ny * off - ny * _SHAKE_UNDER,
            "z": p["z"] - nz * off - nz * _SHAKE_UNDER,
            "role": "under",
            "course": -1,
            "side": 0,
            "v": 0.62,
        }
    under = [
        under_of(north[0], ny_n, nz_n, _SHAKE_BUTT),
        under_of(north[-1], ny_n, nz_n, _SHAKE_HEAD),
        under_of(south[-1], ny_s, nz_s, _SHAKE_HEAD),
        under_of(south[0], ny_s, nz_s, _SHAKE_BUTT),
    ]
    _emit_shake(g, x0, x1, outer + under)


def _roof(g, width, lod):
    """Shakes and ridge run up to the flashing. Higher LODs stay a plain gable."""
    hx = width * 0.5
    if lod > 0:
        _gable(g, -hx, hx, -2.05, 2.15, 2.05, 2.15, 3.45, 0.04, "Lib_Roof")
        _one_slope(g, 1.60, _SHED_Z0, _SHED_Y0, _SHED_Z1, _SHED_Y1, 0.04, "Lib_Roof")
        return
    x_w = _CH_X - _HOLE_X - 0.004
    x_e = _CH_X + _HOLE_X + 0.004
    _shake_gable(g, -hx, x_w)
    _shake_gable(g, x_e, hx)
    _shake_slope(g, x_w + 0.004, x_e - 0.004, -2.05, 2.15, -0.02, _roof_y(0.02), 0, -_HOLE_Z)
    _shake_slope(g, x_w + 0.004, x_e - 0.004, 2.05, 2.15, 0.02, _roof_y(0.02), 1, _HOLE_Z)
    _shake_slope(g, -1.60, 1.60, _SHED_Z0, _SHED_Y0, _SHED_Z1, _SHED_Y1, 2, None)


def _add_roof(asset, width, z0, y0, z1, y1, thick):
    # Boxes stay on the shingles and clear of the chimney shaft.
    hx = width * 0.5
    x_w = _CH_X - _HOLE_X - 0.05
    x_e = _CH_X + _HOLE_X + 0.05
    parts = (("W", -hx + 0.2, x_w), ("E", x_e, hx - 0.2))
    for name, a, b in parts:
        span = b - a
        xc = (a + b) * 0.5
        center, size, euler = _roof_box(z0, y0, z1, y1, thick, span, below=False, x=xc)
        asset.box("Col_RoofS" + name, center, size, euler=euler)
        center, size, euler = _roof_box(-z0, y0, -z1 if z1 else 0.0, y1, thick, span, below=False, x=xc)
        asset.box("Col_RoofN" + name, center, size, euler=euler)
    span = (_HOLE_X * 2.0) - 0.08
    z_h = _HOLE_Z - 0.04
    center, size, euler = _roof_box(-2.05, 2.15, -z_h, _roof_y(z_h), thick, span, below=False, x=_CH_X)
    asset.box("Col_RoofSM", center, size, euler=euler)
    center, size, euler = _roof_box(z_h, _roof_y(z_h), 2.05, 2.15, thick, span, below=False, x=_CH_X)
    asset.box("Col_RoofNM", center, size, euler=euler)


def _gable(g, x0, x1, zs, ys, zn, yn, ridge_y, thick, mat):
    """Closed gable. The two slopes share the ridge, so that edge has two faces."""
    def normal(z_eave, y_eave):
        dy = y_eave - ridge_y
        dz = z_eave
        ny, nz = -dz, dy
        if ny < 0:
            ny, nz = -ny, -nz
        length = math.hypot(ny, nz) or 1.0
        return ny / length, nz / length

    nsy, nsz = normal(zs, ys)
    nny, nnz = normal(zn, yn)

    def put(x, y, z, ny, nz, under):
        if under:
            return (x, y - ny * thick, z - nz * thick)
        return (x, y, z)

    xs = (x0, x1)
    verts = []
    for x in xs:
        verts.append(put(x, ys, zs, nsy, nsz, False))
    for x in xs:
        verts.append((x, ridge_y, 0.0))
    for x in xs:
        verts.append(put(x, yn, zn, nny, nnz, False))
    for x in xs:
        verts.append(put(x, ys, zs, nsy, nsz, True))
    for x in xs:
        verts.append(put(x, ridge_y, 0.0, nsy, nsz, True))
    for x in xs:
        verts.append(put(x, ridge_y, 0.0, nny, nnz, True))
    for x in xs:
        verts.append(put(x, yn, zn, nny, nnz, True))
    faces = [
        (0, 1, 3, 2),
        (2, 3, 5, 4),
        (6, 8, 9, 7),
        (10, 12, 13, 11),
        (0, 6, 7, 1),
        (4, 5, 13, 12),
        (8, 10, 11, 9),
        (0, 2, 8, 6),
        (2, 4, 12, 10),
        (2, 10, 8),
        (1, 7, 9, 3),
        (3, 11, 13, 5),
        (3, 9, 11),
    ]
    g.mesh(verts, faces, mat, uv_scale=1.0)


def _ridge_cap(g, x0, x1):
    """Small cap above the ridge. It does not share the roof's ridge edge."""
    _gable(g, x0, x1, -0.28, 3.30, 0.28, 3.30, 3.48, 0.016, "Lib_Roof")


def _slope_span(g, x0, x1, z0, y0, z1, y1, thick, mat):
    """Closed slope. The given line is the top, matching the main gable."""
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    ny, nz = -dz, dy
    if ny < 0:
        ny, nz = -ny, -nz
    length = math.hypot(ny, nz) or 1.0
    ny /= length
    nz /= length
    xs = (x0, x1)
    verts = []
    for x in xs:
        for z, y in ((z0, y0), (z1, y1)):
            verts.append((x, y, z))
    for x in xs:
        for z, y in ((z0, y0), (z1, y1)):
            verts.append((x, y - ny * thick, z - nz * thick))
    g.mesh(verts, _prism_faces(), mat, uv_scale=1.0)


def _one_slope(g, hx, z0, y0, z1, y1, thick, mat):
    g.mesh(_prism(hx * 2.0, z0, y0, z1, y1, thick), _prism_faces(), mat, uv_scale=1.0)


def _roof_box(z0, y0, z1, y1, thick, width, below=False, x=0.0):
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    nz, ny = -dy / length, dz / length
    if ny < 0:
        nz, ny = -nz, -ny
    sign = -1.0 if below else 1.0
    cz = (z0 + z1) * 0.5 + nz * thick * 0.45 * sign
    cy = (y0 + y1) * 0.5 + ny * thick * 0.45 * sign
    angle = math.degrees(math.atan2(abs(dy), abs(dz)))
    pitch = -angle if dz * dy > 0 else angle
    return (x, cy, cz), (width * 0.74, thick * 0.45, length * 0.60), (pitch, 0.0, 0.0)


def _prism(width, z0, y0, z1, y1, thick):
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    nz, ny = -dy / length, dz / length
    if ny < 0:
        nz, ny = -nz, -ny
    xs = (-width * 0.5, width * 0.5)
    verts = []
    for x in xs:
        for z, y in ((z0, y0), (z1, y1)):
            verts.append((x, y, z))
    for x in xs:
        for z, y in ((z0, y0), (z1, y1)):
            verts.append((x, y + ny * thick, z + nz * thick))
    return verts


def _prism_faces():
    return [
        (0, 2, 3, 1),
        (4, 5, 7, 6),
        (0, 1, 5, 4),
        (2, 6, 7, 3),
        (0, 4, 6, 2),
        (1, 3, 7, 5),
    ]
