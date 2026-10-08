"""Small log cabin. 4.6 x 3.6 m, porch on +Z, weathered cedar-shake gable.

Logs are round and saddle-notched at the corners. The door is vertical boards.
The porch shed sits on a ledger outside the logs, under the main eave. A brick
chimney stands on the +X gable, from a ground footing past the ridge.
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


@register
def create():
    a = Asset(
        "Cabin",
        "Buildings",
        "Log cabin 4.6 x 3.6 m, walls 2.2 m, ridge at 3.45 m, porch on +Z. "
        "Saddle-notched round logs, a vertical-board door, board-and-batten gables, "
        "and a side window on each wall. Weathered cedar shakes, a ridge cap, and a "
        "brick chimney on the +X gable from a ground footing past the ridge. "
        "The porch shed sits on a ledger outside the logs, under the eave.",
    )
    a.climbable = True
    a.climb_note = "Log walls are cling. Door is closed. Roof slopes are landings."
    a.vault_note = "Porch rail is 0.95 m above the porch deck (deck at 0.25 m, rail top at 1.20 m)."
    a.vaultable = True
    a.vault_height = 0.95
    width, depth = 4.6, 3.6
    for lod in (0, 1, 2):
        g = a.begin(lod)
        courses = lod_pick(lod, 9, 6, 4)
        seg = lod_pick(lod, 10, 8, 6)
        step = (_Y1 - _Y0) / max(1, courses - 1)
        radius = max(0.132, step * 0.5 + 0.02)
        _walls(g, lod, courses, radius, step, seg)
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
            g.pipe((-1.3, 0.95, depth * 0.5 + 1.25), (1.3, 0.95, depth * 0.5 + 1.25), 0.03, "Lib_WoodDark", 6, grain=1.0)
            for x in (-0.86, -0.43, 0.0, 0.43, 0.86):
                g.box((x, 0.58, depth * 0.5 + 1.25), (0.035, 0.66, 0.028), "Lib_Wood")
        _roof(g, width + 0.4, lod)
        a.end()
    a.loose_pivot = True
    _log_climb(a)
    dx0, dx1, dy0, dy1 = _DOOR
    a.box("Col_Door", ((dx0 + dx1) * 0.5, (dy0 + dy1) * 0.5, _OUT_Z - 0.132), (dx1 - dx0 - 0.08, dy1 - dy0 - 0.06, 0.04))
    a.box("Col_StepLow", (0, 0.05, depth * 0.5 + 1.95), (1.00, 0.05, 0.24))
    a.box("Col_StepHigh", (0, 0.15, depth * 0.5 + 1.62), (1.05, 0.06, 0.20))
    a.box("Col_Porch", (0, 0.12, depth * 0.5 + 0.7), (3.2, 0.10, 1.3))
    center, size, euler = _roof_box(_SHED_Z0, _SHED_Y0, _SHED_Z1, _SHED_Y1, 0.045, 3.2)
    a.box("Col_PorchRoof", center, size, euler=euler)
    a.capsule("Vault_PorchRail", (0, 0.95, depth * 0.5 + 1.25), 0.03, 2.6, 0)
    _add_roof(a, width + 0.4, -2.05, 2.15, 0.0, 3.45, 0.04)
    return a


def _walls(g, lod, courses, radius, step, seg):
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
            _run_z(g, lod, i, y_side, radius, seg, -(_OUT_X - radius), not proud, side_holes, 2)
            _run_z(g, lod, i, y_side, radius, seg, _OUT_X - radius, not proud, side_holes, 3)
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
        g.box(((x0 + x1) * 0.5, y, z_back), (x1 - x0, band, thick), "Lib_Chink")


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
    """Closed puck. Rings are a texture on the cut face, so the shell stays small."""
    steps = 6
    bm = bmesh.new()
    uv_layer = bm.loops.layers.uv.new("UVMap")
    # Proud of the log cap by 4 mm, 8 mm thick, so the ray sees a closed shell.
    outer = sign * 0.006
    inner = sign * -0.002
    def point(along, ca, sa):
        if axis == "X":
            return (center[0] + along, center[1] + radius * ca, center[2] + radius * sa)
        return (center[0] + radius * ca, center[1] + radius * sa, center[2] + along)
    outer_ring = []
    inner_ring = []
    uvs = []
    for i in range(steps):
        ang = 2.0 * math.pi * i / steps
        ca, sa = math.cos(ang), math.sin(ang)
        outer_ring.append(bm.verts.new(unity_to_blender(*point(outer, ca, sa))))
        inner_ring.append(bm.verts.new(unity_to_blender(*point(inner, ca, sa))))
        uvs.append((0.5 + 0.48 * ca, 0.5 + 0.48 * sa))
    hub_o = bm.verts.new(unity_to_blender(*point(outer, 0.0, 0.0)))
    hub_i = bm.verts.new(unity_to_blender(*point(inner, 0.0, 0.0)))
    for i in range(steps):
        j = (i + 1) % steps
        if sign > 0:
            face = bm.faces.new((hub_o, outer_ring[i], outer_ring[j]))
            order = ((0.5, 0.5), uvs[i], uvs[j])
            bm.faces.new((hub_i, inner_ring[j], inner_ring[i]))
            bm.faces.new((outer_ring[i], inner_ring[i], inner_ring[j], outer_ring[j]))
        else:
            face = bm.faces.new((hub_o, outer_ring[j], outer_ring[i]))
            order = ((0.5, 0.5), uvs[j], uvs[i])
            bm.faces.new((hub_i, inner_ring[i], inner_ring[j]))
            bm.faces.new((outer_ring[j], inner_ring[j], inner_ring[i], outer_ring[i]))
        for loop, uv in zip(face.loops, order):
            loop[uv_layer].uv = uv
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
    for z in (-1.05, -0.55, -0.05, 0.45, 0.95):
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
    """Exterior chimney on the +X gable. Footing on the ground, stack past the ridge."""
    x = 3.04
    # Footing, shaft, and cap overlap a few centimetres so the joint is not a slit.
    # Nothing here is a collider, and none of it enters the roof or the log tails.
    g.box((x, 0.17, 0.0), (0.92, 0.34, 1.24), "Lib_Concrete", uv_scale=0.7)
    g.box((x, 2.40, 0.0), (0.70, 4.20, 0.96), "Lib_Brick", uv_scale=0.65)
    g.box((x, 4.52, 0.0), (0.86, 0.12, 1.12), "Lib_Concrete")
    g.box((x, 4.66, 0.0), (0.26, 0.22, 0.26), "Lib_Brick", uv_scale=0.5)
    _flashing(g, x - 0.35)


def _flashing(g, face):
    """Apron on the roof, then a turned-up leg a few millimetres off the brick."""
    for z0, z1 in ((-1.55, -0.10), (0.10, 1.55)):
        y0 = _roof_y(z0) + 0.010
        y1 = _roof_y(z1) + 0.010
        _apron(g, 2.36, face - 0.020, z0, y0, z1, y1, 0.008)
        _apron(g, face - 0.014, face - 0.006, z0, y0 + 0.016, z1, y1 + 0.016, 0.11)


def _apron(g, x0, x1, z0, y0, z1, y1, thick):
    verts = [
        (x0, y0, z0), (x0, y1, z1), (x1, y0, z0), (x1, y1, z1),
        (x0, y0 + thick, z0), (x0, y1 + thick, z1), (x1, y0 + thick, z0), (x1, y1 + thick, z1),
    ]
    g.mesh(verts, _prism_faces(), "Lib_Steel", uv_scale=1.0)


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
            length = min(0.50, (x1 - x0) - 0.80)
            if length < 0.36:
                continue
            asset.box(
                "Climb_Front_%d_%d" % (i, int((x0 + x1) * 10)),
                ((x0 + x1) * 0.5, y, z_front),
                (length, 0.06, 0.06),
            )
        asset.box("Climb_Back_%d" % i, (0.0, y, z_back), (3.40, 0.06, 0.06))
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


def _roof(g, width, lod):
    """One closed gable so the ridge is a normal edge, plus a cap and the porch shed."""
    _gable(g, width * 0.5, -2.05, 2.15, 2.05, 2.15, 3.45, 0.055, "Lib_Roof")
    _ridge_cap(g, width * 0.5)
    # Shed starts on the ledger, outside the logs, and lands on the porch beam.
    _one_slope(g, 1.60, _SHED_Z0, _SHED_Y0, _SHED_Z1, _SHED_Y1, 0.045, "Lib_Roof")
    del lod


def _add_roof(asset, width, z0, y0, z1, y1, thick):
    # The gable line is the top surface. The box sits inside the thickness.
    center, size, euler = _roof_box(z0, y0, z1, y1, thick, width, below=True)
    asset.box("Col_RoofS", center, size, euler=euler)
    center, size, euler = _roof_box(-z0, y0, -z1 if z1 else 0.0, y1, thick, width, below=True)
    asset.box("Col_RoofN", center, size, euler=euler)


def _gable(g, hx, zs, ys, zn, yn, ridge_y, thick, mat):
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

    xs = (-hx, hx)
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


def _ridge_cap(g, hx):
    """Small cap above the ridge. It does not share the roof's ridge edge."""
    _gable(g, hx + 0.04, -0.28, 3.30, 0.28, 3.30, 3.48, 0.016, "Lib_Roof")


def _one_slope(g, hx, z0, y0, z1, y1, thick, mat):
    g.mesh(_prism(hx * 2.0, z0, y0, z1, y1, thick), _prism_faces(), mat, uv_scale=1.0)


def _roof_box(z0, y0, z1, y1, thick, width, below=False):
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
    return (0.0, cy, cz), (width * 0.82, thick * 0.5, length * 0.70), (pitch, 0.0, 0.0)


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
