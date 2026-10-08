"""Small log cabin. 4.6 x 3.6 m, porch on +Z, gable roof.

Logs are round (untextured, overlapping courses) with grey-tan chinking in
the groove. Corner notches show end grain from a ring texture, not mesh rings.
The door is one plank leaf.
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


@register
def create():
    a = Asset(
        "Cabin",
        "Buildings",
        "Log cabin 4.6 x 3.6 m, walls 2.2 m, ridge at 3.45 m, porch on +Z. "
        "Round logs with grey-tan chinking, notched corners, and one plank door about 2.0 m.",
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
        seg = lod_pick(lod, 12, 8, 6)
        step = (_Y1 - _Y0) / max(1, courses - 1)
        radius = max(0.132, step * 0.5 + 0.02)
        _walls(g, lod, courses, radius, step, seg)
        _door_and_window(g, lod, radius)
        if lod == 0:
            for x, sign in ((-width * 0.5 - 0.04, -1), (width * 0.5 + 0.04, 1)):
                g.box((x, 1.35, 0.15), (0.05, 0.72, 0.62), "Lib_Varnish")
                g.box((x + sign * 0.02, 1.35, 0.15), (0.015, 0.48, 0.40), "Lib_ShopGlass")
            g.box((1.15, 3.55, -0.35), (0.48, 1.15, 0.48), "Lib_Brick", uv_scale=1.0)
            g.box((1.15, 4.16, -0.35), (0.58, 0.08, 0.58), "Lib_Concrete")
        g.box((0, 0.05, depth * 0.5 + 1.95), (1.15, 0.08, 0.32), "Lib_Wood", grain=1.0)
        g.box((0, 0.14, depth * 0.5 + 1.62), (1.20, 0.10, 0.28), "Lib_Wood", grain=1.0)
        g.box((0, 0.12, depth * 0.5 + 0.7), (3.2, 0.10, 1.3), "Lib_Wood", bevel=0.004 if lod == 0 else 0, segs=1, uv_scale=1.0, grain=1.0)
        for x in (-1.3, 1.3):
            g.box((x, 1.15, depth * 0.5 + 1.25), (0.12, 2.05, 0.12), "Lib_WoodDark")
        g.box((0, 2.245, 3.08), (2.84, 0.16, 0.30), "Lib_WoodDark", grain=1.0)
        g.box((0, 2.29, depth * 0.5 + 0.7), (3.4, 0.08, 1.5), "Lib_Roof", uv_scale=1.0)
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
    a.box("Col_PorchRoof", (0, 2.29, depth * 0.5 + 0.7), (3.2, 0.06, 1.35))
    a.capsule("Vault_PorchRail", (0, 0.95, depth * 0.5 + 1.25), 0.03, 2.6, 0)
    _add_roof(a, width + 0.4, -2.05, 2.15, 0.0, 3.45, 0.06)
    return a


def _walls(g, lod, courses, radius, step, seg):
    valley = math.sqrt(max(0.0, radius * radius - (step * 0.5) ** 2))
    for i in range(courses):
        y = _Y0 + i * step
        proud = (i % 2 == 0)
        holes = _front_holes(y, radius)
        _run_x(g, lod, y, radius, seg, _OUT_Z - radius, proud, holes, 1)
        _run_x(g, lod, y, radius, seg, -(_OUT_Z - radius), proud, [], -1)
        _run_z(g, lod, y, radius, seg, -(_OUT_X - radius), not proud, -1)
        _run_z(g, lod, y, radius, seg, _OUT_X - radius, not proud, 1)
        if i + 1 < courses:
            _chink(g, y + step * 0.5, radius, valley, holes)


def _front_holes(y, radius):
    holes = []
    if y - radius < _DOOR[3] and y + radius > _DOOR[2]:
        holes.append((_DOOR[0] - 0.06, _DOOR[1] + 0.06))
    if y - radius < _WIN[3] and y + radius > _WIN[2]:
        holes.append((_WIN[0] - 0.06, _WIN[1] + 0.06))
    return holes


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


def _run_x(g, lod, y, radius, seg, z, proud, holes, face_sign):
    reach = _OUT_X + (_TAIL if proud else -radius)
    for x0, x1 in _spans(-reach, reach, holes):
        g.cylinder(((x0 + x1) * 0.5, y, z), radius, x1 - x0, "Lib_Varnish", seg, axis="X")
        if lod == 0 and proud:
            if x0 < -_OUT_X:
                _end_disc(g, (x0, y, z), "X", radius * 0.98, -1)
            if x1 > _OUT_X:
                _end_disc(g, (x1, y, z), "X", radius * 0.98, 1)


def _run_z(g, lod, y, radius, seg, x, proud, face_sign):
    reach = _OUT_Z + (_TAIL if proud else -radius)
    g.cylinder((x, y, 0.0), radius, reach * 2.0, "Lib_Varnish", seg, axis="Z")
    if lod == 0 and proud:
        _end_disc(g, (x, y, -reach), "Z", radius * 0.98, -1)
        _end_disc(g, (x, y, reach), "Z", radius * 0.98, 1)
    del face_sign


def _chink(g, y, radius, valley, holes):
    """Grey-tan bead in the groove. It sits outside the log shells, shy of the crown."""
    thick = 0.016
    band = 0.028
    # Valley is the dip. A few centimetres proud of that, still inside the round crown.
    proud = min(0.030, (radius - valley) * 0.45)
    outer = valley + proud
    z_front = (_OUT_Z - radius) + outer - thick * 0.5
    z_back = -z_front
    x_side = (_OUT_X - radius) + outer - thick * 0.5
    for x0, x1 in _spans(-_OUT_X + 0.20, _OUT_X - 0.20, holes):
        g.box(((x0 + x1) * 0.5, y, z_front), (x1 - x0, band, thick), "Lib_Chink")
        g.box(((x0 + x1) * 0.5, y, z_back), (x1 - x0, band, thick), "Lib_Chink")
    g.box((-x_side, y, 0.0), (thick, band, (_OUT_Z - 0.30) * 2.0), "Lib_Chink")
    g.box((x_side, y, 0.0), (thick, band, (_OUT_Z - 0.30) * 2.0), "Lib_Chink")


def _end_disc(g, center, axis, radius, sign):
    """Closed puck. Rings are a texture on the cut face, so the shell stays small."""
    steps = 8
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
    # One leaf. Vertical planks, no mid rail.
    leaf_w = (dx1 - dx0) - 0.08
    plank_n = 5
    plank_w = leaf_w / plank_n
    for i in range(plank_n):
        px = dx0 + 0.04 + plank_w * (i + 0.5)
        g.box((px, cy, z), (plank_w - 0.008, dy1 - dy0 - 0.06, 0.028), "Lib_Wood", grain=1.0)
    if lod == 0:
        g.box((dx1 - 0.12, cy - 0.05, z + 0.02), (0.035, 0.07, 0.025), "Lib_Brass")
    wx0, wx1, wy0, wy1 = _WIN
    wcx, wcy = (wx0 + wx1) * 0.5, (wy0 + wy1) * 0.5
    g.box((wcx, wcy, z), (wx1 - wx0, wy1 - wy0, 0.06), "Lib_Batten")
    g.box((wcx, wcy, z + 0.02), (wx1 - wx0 - 0.10, wy1 - wy0 - 0.10, 0.02), "Lib_ShopGlass")


def _log_climb(asset):
    """One box in the core of each log. Ends and the chink bead stay outside it."""
    courses = 9
    step = (_Y1 - _Y0) / (courses - 1)
    radius = 0.132
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
        asset.box("Climb_SideL_%d" % i, (-x_side, y, 0.0), (0.08, 0.08, 2.20))
        asset.box("Climb_SideR_%d" % i, (x_side, y, 0.0), (0.08, 0.08, 2.20))


def _roof(g, width, lod):
    g.mesh(_prism(width, -2.05, 2.15, 0.0, 3.45, 0.06), _prism_faces(), "Lib_Roof", uv_scale=1.0)
    g.mesh(_prism(width, 2.05, 2.15, 0.0, 3.45, 0.06), _prism_faces(), "Lib_Roof", uv_scale=1.0)


def _add_roof(asset, width, z0, y0, z1, y1, thick):
    center, size, euler = _roof_box(z0, y0, z1, y1, thick, width)
    asset.box("Col_RoofS", center, size, euler=euler)
    center, size, euler = _roof_box(-z0, y0, -z1 if z1 else 0.0, y1, thick, width)
    asset.box("Col_RoofN", center, size, euler=euler)


def _roof_box(z0, y0, z1, y1, thick, width):
    dz, dy = (z1 - z0), (y1 - y0)
    length = math.hypot(dz, dy) or 1.0
    nz, ny = -dy / length, dz / length
    if ny < 0:
        nz, ny = -nz, -ny
    cz = (z0 + z1) * 0.5 + nz * thick * 0.45
    cy = (y0 + y1) * 0.5 + ny * thick * 0.45
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
