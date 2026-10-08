"""Midsize sedan, hard-surface panels.

Pass 12 keeps the panel shell. The hood falls to about 0.83 m, the deck
stays near 1.08 m, the bumper tucks in under the fenders, and the lamps
are separate pieces that turn the corners.

One shell for model years 2021-2025. The body is a closed ring of flat
panels: shoulder, belt, rocker, wheel-arch lip, and a greenhouse framed by
A, B, and C pillars. A bevel of two segments (one on LOD1) breaks the
creases. Lamps, the grille, and the lower intake are recessed into the
fascia, not floated in front of it.

Proportions are a 2025 mid-size sedan: 4.90 m long, 1.84 m wide, 1.45 m
tall, 2.82 m wheelbase. The windshield centerline is 60 degrees from
vertical. No badges.
"""

import math
import os

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import shell
from _common import REPO, blender_to_unity, unity_to_blender

INCH = 0.0254
LENGTH = 4.90
HALF_L = LENGTH * 0.5
WIDTH = 1.84
HALF_W = WIDTH * 0.5
HEIGHT = 1.45
WHEELBASE = 2.82
FRONT_OVERHANG = 0.96
Z_FRONT = HALF_L - FRONT_OVERHANG
Z_REAR = Z_FRONT - WHEELBASE
BELLY = 5.4 * INCH
Z_NOSE = HALF_L
Z_TAIL = -HALF_L
Z_HEADER = 0.52
Z_COWL = 1.42

TIRE_W = 0.235
RIM_R = 18.0 * INCH * 0.5
TIRE_R = RIM_R + TIRE_W * 0.45
AXLE_Y = TIRE_R
ARCH_GAP = 0.040
ARCH_R = TIRE_R + ARCH_GAP
# Outer sidewall about 3 cm inside the fender, so the tire sits in the arch.
TIRE_X = min(0.78, HALF_W - TIRE_W * 0.47 - 0.035)

PAINT = "Lib_PaintCrimson"
GLASS = "Lib_AutoGlass"
BLACK = "Lib_Black"
MATS = (PAINT, GLASS, BLACK, "Lib_Headlamp", "Lib_Taillamp")
MAT_PAINT = 0
MAT_GLASS = 1
MAT_BLACK = 2
MAT_HEAD = 3
MAT_TAIL = 4

# Separate lamps. Nothing runs the full width of the nose.
LAMPS = (
    {"x0": -0.64, "x1": -0.22, "y0": 0.62, "y1": 0.78},
    {"x0": 0.22, "x1": 0.64, "y0": 0.62, "y1": 0.78},
)
GRILLE = {"x0": -0.28, "x1": 0.28, "y0": 0.40, "y1": 0.58}
INTAKE = {"x0": -0.46, "x1": 0.46, "y0": 0.20, "y1": 0.34}
TAILS = (
    {"x0": -0.72, "x1": -0.16, "y0": 0.90, "y1": 1.05},
    {"x0": 0.16, "x1": 0.72, "y0": 0.90, "y1": 1.05},
)

YEARS = {
    2021: {"spokes": 5, "thick": 0.036, "bars": 4, "bar_h": 0.016, "lamps": "separate", "tails": "separate", "intake": False, "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2022: {"spokes": 6, "thick": 0.024, "bars": 6, "bar_h": 0.010, "lamps": "separate", "tails": "separate", "intake": False, "mirror": PAINT, "fog": "Lib_Headlamp"},
    2023: {"spokes": 5, "thick": 0.030, "bars": 5, "bar_h": 0.012, "lamps": "tier", "tails": "separate", "intake": False, "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2024: {"spokes": 6, "thick": 0.018, "bars": 6, "bar_h": 0.008, "lamps": "thin", "tails": "thin", "intake": False, "mirror": BLACK, "fog": "Lib_Headlamp"},
    2025: {"spokes": 5, "thick": 0.026, "bars": 4, "bar_h": 0.014, "lamps": "swept", "tails": "wrap", "intake": True, "mirror": PAINT, "fog": None},
}

_TEMPLATE = {}


def _lerp(keys, z):
    if z <= keys[0][0]:
        return keys[0][1]
    if z >= keys[-1][0]:
        return keys[-1][1]
    for i in range(len(keys) - 1):
        z0, y0 = keys[i]
        z1, y1 = keys[i + 1]
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0 or 1.0)
            return y0 + (y1 - y0) * t
    return keys[-1][1]


def _top_y(z):
    """Centerline. Hood falls to 0.83, deck stays near 1.08."""
    return _lerp((
        (-2.45, 1.07),
        (-1.70, 1.09),
        (-1.35, 1.18),
        (-0.95, 1.40),
        (-0.40, 1.45),
        (0.20, 1.45),
        (0.52, 1.42),
        (1.42, 0.94),
        (1.85, 0.88),
        (2.20, 0.85),
        (2.45, 0.83),
    ), z)


def _plan_x(z):
    """Half-width of the shoulder, the widest painted line."""
    return _lerp((
        (-2.45, 0.80),
        (-2.05, 0.88),
        (-1.40, 0.92),
        (1.35, 0.92),
        (1.70, 0.90),
        (2.05, 0.86),
        (2.30, 0.82),
        (2.45, 0.78),
    ), z)


def _shoulder_y(z):
    return _lerp((
        (-2.45, 0.96),
        (-1.50, 0.98),
        (-0.20, 0.82),
        (1.20, 0.80),
        (2.00, 0.76),
        (2.45, 0.72),
    ), z)


def _belt_y(z):
    return _lerp((
        (-2.45, 1.04),
        (-1.50, 1.05),
        (-0.90, 1.08),
        (0.90, 1.05),
        (1.42, 0.94),
        (2.45, 0.80),
    ), z)


def _rail_y(z):
    return _lerp((
        (-2.45, 1.06),
        (-1.50, 1.08),
        (-0.95, 1.32),
        (0.52, 1.34),
        (1.20, 1.10),
        (1.42, 0.94),
        (2.45, 0.83),
    ), z)


def _rail_x(z):
    return _lerp((
        (-2.45, 0.50),
        (-1.40, 0.58),
        (-0.40, 0.62),
        (0.40, 0.62),
        (1.10, 0.52),
        (2.45, 0.28),
    ), z)


def _shape_z(z, y):
    """Tuck only the valence. The lamp faces stay on the upright fascia."""
    if z > 1.50:
        t = min(1.0, (z - 1.50) / (Z_NOSE - 1.50))
        drop = max(0.0, 0.36 - y)
        return z - t * drop * 0.85
    if z < -1.50:
        t = min(1.0, (-1.50 - z) / (-Z_TAIL - 1.50))
        drop = max(0.0, 0.78 - y)
        return z + t * drop * 0.40
    return z


def _arch_lip_y(z):
    best = None
    for axle in (Z_FRONT, Z_REAR):
        dz = abs(z - axle)
        if dz >= ARCH_R - 1.0e-4:
            continue
        y = AXLE_Y + math.sqrt(max(0.0, ARCH_R * ARCH_R - dz * dz))
        if best is None or y > best:
            best = y
    return best


def _u(co):
    return blender_to_unity(co.x, co.y, co.z)


def _section(z, door):
    """Fourteen (x, y) points. Bottom center, up the right, down the left.

    The shoulder is the widest line. The belt and the roof rail step in
    (tumblehome). The bumper is narrower than the shoulder, never flared.
    """
    plan = _plan_x(z)
    top = _top_y(z)
    nose = max(0.0, min(1.0, (z - 1.65) / 0.80))
    tail = max(0.0, min(1.0, (-1.50 - z) / 0.95))
    shoulder_x = plan
    belt_x = max(0.20, plan - 0.07 - 0.04 * nose)
    rail_x = min(_rail_x(z), belt_x - 0.05)
    rocker_x = min(plan - 0.10 - 0.16 * nose, shoulder_x - 0.06)
    shoulder = min(_shoulder_y(z), top - 0.03)
    belt = min(max(_belt_y(z), shoulder + 0.04), top - 0.012)
    rail = min(max(_rail_y(z), belt + 0.02), top - 0.004)
    if tail > 0.4:
        rail = max(rail, top - 0.012)
        belt = min(max(belt, top - 0.045), rail - 0.008)
        rail_x = max(rail_x, plan - 0.26)
        belt_x = min(belt_x, plan - 0.08)
    half = [
        (0.0, BELLY),
        (max(0.16, rocker_x * 0.62), BELLY),
        (rocker_x, 0.20),
        (min(shoulder_x - 0.05, rocker_x + 0.06), 0.42),
        (shoulder_x, shoulder),
        (belt_x, belt),
        (max(0.12, rail_x), rail),
        (0.0, top),
    ]
    for i in (1, 2, 3):
        x, y = half[i]
        half[i] = (min(x, shoulder_x - 0.04), y)
    lip = _arch_lip_y(z)
    if lip is not None and lip > 0.45 and not door and nose < 0.35 and tail < 0.35:
        half[4] = (shoulder_x, max(shoulder, min(top - 0.04, lip + 0.025)))
        half[3] = (shoulder_x - 0.015, min(lip, half[4][1] - 0.02))
        half[2] = (min(rocker_x, shoulder_x - 0.08), min(0.36, lip))
        if half[5][1] < half[4][1] + 0.02:
            half[5] = (half[5][0], half[4][1] + 0.02)
    if door:
        for i in (2, 3, 4, 5):
            x, y = half[i]
            half[i] = (max(0.05, x - 0.003), y)
    prev = -1.0
    stacked = []
    for i, (x, y) in enumerate(half):
        if i == 0:
            stacked.append((x, y))
            prev = y
            continue
        if i == len(half) - 1:
            stacked.append((0.0, top))
            continue
        y = min(max(y, prev + 0.01), top - 0.004)
        stacked.append((x, y))
        prev = y
    half = stacked
    ring = list(half)
    for x, y in reversed(half[1:-1]):
        ring.append((-x, y))
    return ring


def _stations():
    rows = [
        (2.450, False),
        (2.300, False),
        (2.120, False),
        (1.900, False),
        (1.420, False),
        (1.100, False),
        (1.097, True),
        (0.520, True),
        (0.143, True),
        (0.140, False),
        (0.040, False),
        (0.037, True),
        (-0.100, True),
        (-0.923, True),
        (-0.926, False),
        (-1.050, False),
        (-1.150, False),
        (-1.300, False),
        (-1.500, False),
        (-1.700, False),
        (-2.050, False),
        (-2.280, False),
        (-2.450, False),
    ]
    taken = [z for z, _door in rows]
    for axle in (Z_FRONT, Z_REAR):
        for dz in (-0.34, -0.22, -0.10, 0.0, 0.12, 0.24, 0.34):
            z = round(axle + dz, 3)
            if any(abs(z - t) < 0.012 for t in taken):
                continue
            door = (0.143 < z < 1.097) or (-0.923 < z < 0.037)
            rows.append((z, door))
            taken.append(z)
    rows.sort(key=lambda row: -row[0])
    return rows


def _orient_outward(bm):
    bm.normal_update()
    if not bm.verts or not bm.faces:
        return
    center = Vector((0.0, 0.0, 0.0))
    for vert in bm.verts:
        center += vert.co
    center /= len(bm.verts)
    votes = 0
    for face in bm.faces:
        if face.normal.length < 1.0e-8:
            continue
        mid = face.calc_center_median()
        if face.normal.dot(mid - center) >= 0.0:
            votes += 1
        else:
            votes -= 1
    if votes < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
        bm.normal_update()


def _faces_on_plane(bm, z_plane, eps=0.003):
    found = []
    for face in bm.faces:
        zs = [_u(vert.co)[2] for vert in face.verts]
        if max(zs) - min(zs) > eps:
            continue
        if abs(sum(zs) / len(zs) - z_plane) <= eps:
            found.append(face)
    return found


def _bisect_cap(bm, faces, origin_u, normal_u):
    if not faces:
        return
    geom = []
    seen = set()
    for face in faces:
        if face not in seen:
            geom.append(face)
            seen.add(face)
        for edge in face.edges:
            if edge not in seen:
                geom.append(edge)
                seen.add(edge)
        for vert in face.verts:
            if vert not in seen:
                geom.append(vert)
                seen.add(vert)
    normal = Vector(unity_to_blender(*normal_u))
    if normal.length < 1.0e-8:
        return
    normal.normalize()
    bmesh.ops.bisect_plane(
        bm,
        geom=geom,
        plane_co=Vector(unity_to_blender(*origin_u)),
        plane_no=normal,
        clear_inner=False,
        clear_outer=False,
    )


def _recess(bm, z_plane, rect):
    """Inset one opening and drop its floor to rect['floor']."""
    faces = []
    for face in _faces_on_plane(bm, z_plane):
        x, y, _z = _u(face.calc_center_median())
        if rect["x0"] < x < rect["x1"] and rect["y0"] < y < rect["y1"]:
            faces.append(face)
    if not faces:
        print("POCKET_MISS", round(z_plane, 3), round(rect["y0"], 3), round(rect["y1"], 3))
        return False
    bmesh.ops.inset_region(
        bm,
        faces=faces,
        thickness=0.006,
        depth=0.0,
        use_even_offset=True,
    )
    bm.verts.ensure_lookup_table()
    moved = 0
    x0 = rect["x0"] + 0.004
    x1 = rect["x1"] - 0.004
    y0 = rect["y0"] + 0.004
    y1 = rect["y1"] - 0.004
    for vert in bm.verts:
        x, y, z = _u(vert.co)
        if abs(z - z_plane) > 0.004:
            continue
        if x0 < x < x1 and y0 < y < y1:
            vert.co = Vector(unity_to_blender(x, y, rect["floor"]))
            moved += 1
    print("POCKET", round(z_plane, 3), round(rect["y0"], 3), "faces", len(faces), "floor_verts", moved)
    return moved > 0


def _unity_normal(face):
    n = face.normal
    return blender_to_unity(n.x, n.y, n.z)


def _opening_faces(bm, rect, axis):
    found = []
    for face in bm.faces:
        x, y, z = _u(face.calc_center_median())
        if not (rect["x0"] < x < rect["x1"] and rect["y0"] < y < rect["y1"]):
            continue
        nx, _ny, nz = _unity_normal(face)
        if axis == "z":
            if z < rect.get("zmin", 1.7) or nz < 0.25:
                continue
        elif axis == "-z":
            if z > rect.get("zmax", -1.7) or nz > -0.25:
                continue
        elif axis == "x":
            if z < rect.get("zmin", 1.85) or abs(nz) > 0.55:
                continue
            if x > 0.0 and nx < 0.45:
                continue
            if x < 0.0 and nx > -0.45:
                continue
        elif axis == "-x":
            if z > rect.get("zmax", -1.85) or abs(nz) > 0.55:
                continue
            if x > 0.0 and nx < 0.45:
                continue
            if x < 0.0 and nx > -0.45:
                continue
        else:
            continue
        found.append(face)
    return found


def _inset_opening(bm, faces, depth=0.011):
    if len(faces) < 1:
        return False
    try:
        bmesh.ops.inset_region(
            bm,
            faces=faces,
            thickness=0.008,
            depth=-depth,
            use_even_offset=True,
            use_boundary=True,
        )
    except (TypeError, ValueError, RuntimeError) as exc:
        print("POCKET_FAIL", exc)
        return False
    return True


def _region_faces(bm, z_test):
    found = []
    for face in bm.faces:
        zs = [_u(vert.co)[2] for vert in face.verts]
        if z_test(sum(zs) / len(zs)):
            found.append(face)
    return found


def _grid_end(bm, z_test, xs, ys):
    for x in xs:
        _bisect_cap(bm, _region_faces(bm, z_test), (x, 0.0, 0.0), (1.0, 0.0, 0.0))
        _bisect_cap(bm, _region_faces(bm, z_test), (-x, 0.0, 0.0), (1.0, 0.0, 0.0))
    for y in ys:
        _bisect_cap(bm, _region_faces(bm, z_test), (0.0, y, 0.0), (0.0, 1.0, 0.0))


def _cut_caps(bm):
    _grid_end(
        bm,
        lambda z: z > 1.85,
        (0.22, 0.28, 0.46, 0.76),
        (0.20, 0.34, 0.40, 0.58, 0.62, 0.78),
    )
    _grid_end(
        bm,
        lambda z: z < -1.85,
        (0.16, 0.72),
        (0.90, 1.05),
    )
    bm.normal_update()
    ok = True
    jobs = [(rect, "z", "lamp") for rect in LAMPS]
    jobs.append((GRILLE, "z", "grille"))
    jobs.append((INTAKE, "z", "intake"))
    jobs.extend((rect, "-z", "tail") for rect in TAILS)
    for rect, axis, label in jobs:
        faces = _opening_faces(bm, rect, axis)
        hit = _inset_opening(bm, faces)
        print("POCKET", label, "faces", len(faces), "ok", hit)
        if label in ("lamp", "grille", "intake", "tail") and not hit:
            ok = False
    return ok


def _mark_creases(bm):
    layer = bm.edges.layers.float.get("bevel_weight_edge")
    if layer is None:
        layer = bm.edges.layers.float.new("bevel_weight_edge")
    marked = 0
    for edge in bm.edges:
        edge[layer] = 0.0
        a = _u(edge.verts[0].co)
        b = _u(edge.verts[1].co)
        dx = b[0] - a[0]
        dy = b[1] - a[1]
        dz = b[2] - a[2]
        length = math.sqrt(dx * dx + dy * dy + dz * dz)
        if length < 0.02:
            continue
        # Bevels that run into the flat fascia or tail open those caps.
        if min(a[2], b[2]) < Z_TAIL + 0.08 or max(a[2], b[2]) > Z_NOSE - 0.08:
            continue
        z = (a[2] + b[2]) * 0.5
        # A 3 mm shut is two stations. A bevel run into that step crimps the belt.
        if any(abs(z - shut) < 0.04 for shut in (1.098, 0.141, 0.038, -0.924)):
            continue
        # The fascia and tail stay crisp. Beveling those rings opens the pockets.
        if abs(dz) < 0.75 * length:
            continue
        x = abs((a[0] + b[0]) * 0.5)
        y = (a[1] + b[1]) * 0.5
        plan = _plan_x(z)
        lip = _arch_lip_y(z)
        hit = False
        if abs(y - _shoulder_y(z)) < 0.035 and abs(x - plan) < 0.035:
            hit = True
        elif lip is not None and abs(y - lip) < 0.025 and abs(x - plan) < 0.03:
            hit = True
        elif abs(y - 0.16) < 0.03 and abs(x - (plan - 0.16)) < 0.05:
            hit = True
        if hit and edge.calc_face_angle(0.0) > math.radians(18.0):
            edge[layer] = 1.0
            marked += 1
    print("CREASE_EDGES", marked)
    return layer


def _bevel(bm, lod):
    segments = (2, 1, 0)[lod]
    _mark_creases(bm)
    if segments == 0:
        return bm
    mesh = bpy.data.meshes.new("sedan_bevel")
    bm.to_mesh(mesh)
    obj = bpy.data.objects.new("sedan_bevel", mesh)
    bpy.context.scene.collection.objects.link(obj)
    mod = obj.modifiers.new("Bevel", "BEVEL")
    mod.limit_method = "WEIGHT"
    mod.width = 0.008
    mod.segments = segments
    mod.profile = 0.5
    mod.use_clamp_overlap = True
    mod.harden_normals = True
    mod.miter_outer = "MITER_ARC"
    before = len(mesh.polygons)
    shell._apply_mod(obj, "Bevel")
    weighted = obj.modifiers.new("WeightedNormal", "WEIGHTED_NORMAL")
    weighted.keep_sharp = True
    weighted.weight = 80
    weighted.mode = "FACE_AREA_WITH_ANGLE"
    shell._apply_mod(obj, "WeightedNormal")
    out = bmesh.new()
    out.from_mesh(obj.data)
    after = len(obj.data.polygons)
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(mesh)
    bm.free()
    print("BEVEL", lod, "polys", before, "->", after)
    return out


def _glass_index(face):
    pts = [_u(vert.co) for vert in face.verts]
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    zs = [p[2] for p in pts]
    if max(zs) - min(zs) < 0.04 and max(xs) - min(xs) < 0.04:
        return None
    cx = sum(xs) / len(xs)
    cy = sum(ys) / len(ys)
    cz = sum(zs) / len(zs)
    touches = min(abs(x) for x in xs) < 0.08
    if touches and 0.55 < cz < 1.38 and cy > 0.98 and max(abs(x) for x in xs) < 0.72:
        return MAT_GLASS
    if touches and -1.62 < cz < -0.98 and cy > 1.10 and max(abs(x) for x in xs) < 0.70:
        return MAT_GLASS
    ax = abs(cx)
    if ax > 0.55 and 1.02 < cy < 1.36 and max(zs) - min(zs) > 0.06:
        if 0.18 < cz < 1.00 or -0.88 < cz < -0.08:
            return MAT_GLASS
        if -1.40 < cz < -1.02:
            return MAT_GLASS
    return None


def _paint(bm):
    glass = 0
    black = 0
    for face in bm.faces:
        x, y, z = _u(face.calc_center_median())
        kind = _glass_index(face)
        if kind is not None:
            face.material_index = MAT_GLASS
            glass += 1
            continue
        pocket = False
        if Z_NOSE - 0.020 < z < Z_NOSE - 0.007 and abs(x) < 0.62 and 0.16 < y < 0.68:
            pocket = True
        if Z_TAIL + 0.007 < z < Z_TAIL + 0.020 and abs(x) < 0.52 and 0.66 < y < 0.86:
            pocket = True
        well = False
        if abs(x) < 0.78:
            for axle in (Z_FRONT, Z_REAR):
                dy = y - AXLE_Y
                dz = z - axle
                if dy * dy + dz * dz < (ARCH_R - 0.02) ** 2 and y < AXLE_Y + ARCH_R - 0.01:
                    well = True
        if pocket or well or (y < 0.30 and abs(x) > 0.35 and z > Z_TAIL + 0.05 and z < Z_NOSE - 0.05):
            face.material_index = MAT_BLACK
            black += 1
        else:
            face.material_index = MAT_PAINT
        nx, _ny, nz = _unity_normal(face)
        if z > 1.92 and 0.64 < y < 0.78 and abs(nx) > 0.55 and abs(nz) < 0.50:
            face.material_index = MAT_HEAD
        elif z < -1.95 and 0.90 < y < 1.05 and abs(nx) > 0.55 and abs(nz) < 0.50:
            face.material_index = MAT_TAIL
    print("MATS glass", glass, "black", black)
    return glass


def _glass_islands(faces):
    remaining = set(faces)
    islands = []
    while remaining:
        seed = remaining.pop()
        stack = [seed]
        island = [seed]
        while stack:
            face = stack.pop()
            for edge in face.edges:
                for other in edge.link_faces:
                    if other in remaining:
                        remaining.remove(other)
                        stack.append(other)
                        island.append(other)
        islands.append(island)
    return islands


def _inset_glass(bm):
    faces = [face for face in bm.faces if face.material_index == MAT_GLASS]
    if not faces:
        return
    done = 0
    for island in _glass_islands(faces):
        if len(island) < 2:
            continue
        before = {face for face in bm.faces}
        try:
            bmesh.ops.inset_region(
                bm,
                faces=island,
                thickness=0.008,
                depth=-0.005,
                use_even_offset=True,
                use_boundary=True,
            )
        except (TypeError, ValueError, RuntimeError) as exc:
            print("GLASS_INSET_FAIL", exc)
            continue
        for face in bm.faces:
            if face in before or not face.is_valid:
                continue
            pts = [_u(vert.co) for vert in face.verts]
            span_x = max(p[0] for p in pts) - min(p[0] for p in pts)
            span_y = max(p[1] for p in pts) - min(p[1] for p in pts)
            span_z = max(p[2] for p in pts) - min(p[2] for p in pts)
            if span_z > 0.04 or span_y > 0.05 or span_x > 0.05:
                face.material_index = MAT_GLASS
            else:
                face.material_index = MAT_PAINT
        done += len(island)
    print("GLASS_INSET", done, "of", len(faces))


def _clamp_plan(bm):
    """Keep a bevel or a glass inset from pushing the belt outside the shoulder."""
    for vert in bm.verts:
        x, y, z = _u(vert.co)
        if y < 0.25 or _is_wheel(x, y, z):
            continue
        limit = _plan_x(z) + 0.004
        if abs(x) > limit:
            vert.co = Vector(unity_to_blender(math.copysign(limit, x), y, z))


def _build_cage(lod):
    bm = bmesh.new()
    rings = []
    for z, door in _stations():
        ring = [bm.verts.new(unity_to_blender(x, y, _shape_z(z, y))) for x, y in _section(z, door)]
        rings.append(ring)
    count = len(rings[0])
    for i in range(len(rings) - 1):
        for j in range(count):
            j2 = (j + 1) % count
            try:
                bm.faces.new((rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]))
            except ValueError:
                continue
    try:
        # Reversed so the cap points out the nose, not back into the cabin.
        bm.faces.new(list(reversed(rings[0])))
    except ValueError as exc:
        print("NOSE_CAP_FAIL", exc)
    try:
        bm.faces.new(rings[-1])
    except ValueError as exc:
        print("TAIL_CAP_FAIL", exc)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _orient_outward(bm)
    if not _cut_caps(bm):
        raise RuntimeError("fascia pockets were not cut")
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _orient_outward(bm)
    bm = _bevel(bm, lod)
    _clamp_plan(bm)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _orient_outward(bm)
    open_edges, flipped, samples = _topology(bm)
    print("AFTER_BEVEL", lod, len(bm.faces), "open", open_edges, "flips", flipped, samples[:4])
    glass = _paint(bm)
    if glass < 8:
        print("GLASS_LOW", glass)
    _inset_glass(bm)
    _clamp_plan(bm)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _orient_outward(bm)
    return bm


def _commit(g, bm):
    vmap = {}
    for vert in bm.verts:
        vmap[vert] = g.bm.verts.new(vert.co)
    g.bm.verts.index_update()
    slots = [g.slot(name) for name in MATS]
    for face in bm.faces:
        try:
            made = g.bm.faces.new([vmap[vert] for vert in face.verts])
        except ValueError:
            continue
        index = face.material_index if 0 <= face.material_index < len(slots) else 0
        made.material_index = slots[index]
        made.smooth = True
        made[g.scale_layer] = 1.0
    bm.free()


def build_shell(g, lod):
    bm = _build_cage(lod)
    open_edges, flipped, _samples = _topology(bm)
    print(
        "SHELL_LOD", lod, "faces", len(bm.faces), "verts", len(bm.verts),
        "open", open_edges, "flips", flipped,
    )
    if open_edges or flipped:
        print("SHELL_LOD samples", _samples)
    _commit(g, bm)


def _copy_open(src, asset, lod):
    g = asset.begin(lod)
    index = []
    for mat in src.mats:
        index.append(g.slot(mat))
    vmap = {}
    for vert in src.bm.verts:
        vmap[vert] = g.bm.verts.new(vert.co.copy())
    g.bm.verts.index_update()
    for face in src.bm.faces:
        try:
            made = g.bm.faces.new([vmap[vert] for vert in face.verts])
        except ValueError:
            continue
        made.material_index = index[face.material_index]
        made.smooth = face.smooth
        made[g.scale_layer] = face[src.scale_layer]
    return g


def template(asset_cls):
    if _TEMPLATE:
        return _TEMPLATE["asset"]
    asset = asset_cls("_SedanMidAShell", "Vehicles", "Shared midsize shell.")
    for lod in (0, 1, 2):
        g = asset.begin(lod)
        build_shell(g, lod)
        asset.end()
    _TEMPLATE["asset"] = asset
    return asset


def _spin_tire(g, x, z, lod):
    segs = 20 if lod == 0 else (12 if lod == 1 else 8)
    hw = TIRE_W * 0.47
    bead = RIM_R + 0.010
    crown = TIRE_R
    profile = [
        (bead, -0.92),
        (bead + 0.045, -0.72),
        (crown - 0.030, -0.48),
        (crown, -0.22),
        (crown, 0.0),
        (crown, 0.22),
        (crown - 0.030, 0.48),
        (bead + 0.045, 0.72),
        (bead, 0.92),
    ]
    bm = bmesh.new()
    rings = []
    for i in range(segs):
        theta = 2.0 * math.pi * i / segs
        ct, st = math.cos(theta), math.sin(theta)
        ring = []
        for rad, axial in profile:
            ring.append(bm.verts.new(unity_to_blender(
                x + axial * hw, AXLE_Y + rad * ct, z + rad * st,
            )))
        rings.append(ring)
    for i in range(segs):
        ni = (i + 1) % segs
        for j in range(len(profile) - 1):
            try:
                bm.faces.new((rings[i][j], rings[ni][j], rings[ni][j + 1], rings[i][j + 1]))
            except ValueError:
                continue
    try:
        bm.faces.new([rings[i][0] for i in range(segs)])
    except ValueError:
        pass
    try:
        bm.faces.new([rings[i][-1] for i in range(segs - 1, -1, -1)])
    except ValueError:
        pass
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    g._ingest(bm, "Lib_Rubber", 1.0)


def _annulus(g, x, z, r0, r1, depth, mat, segs):
    """Closed rim band in the wheel plane, so the spokes stay visible in the middle."""
    bm = bmesh.new()
    bands = []
    for axial in (-depth * 0.5, depth * 0.5):
        inner = []
        outer = []
        for i in range(segs):
            ang = 2.0 * math.pi * i / segs
            cy, cz = math.cos(ang), math.sin(ang)
            inner.append(bm.verts.new(unity_to_blender(x + axial, AXLE_Y + r0 * cy, z + r0 * cz)))
            outer.append(bm.verts.new(unity_to_blender(x + axial, AXLE_Y + r1 * cy, z + r1 * cz)))
        bands.append((inner, outer))
    (inner0, outer0), (inner1, outer1) = bands
    for i in range(segs):
        j = (i + 1) % segs
        bm.faces.new((outer0[i], outer0[j], outer1[j], outer1[i]))
        bm.faces.new((inner0[j], inner0[i], inner1[i], inner1[j]))
        bm.faces.new((inner0[i], outer0[i], outer0[j], inner0[j]))
        bm.faces.new((outer1[i], inner1[i], inner1[j], outer1[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    g._ingest(bm, mat, 1.0)


def _wheel(g, x, z, year, lod):
    sign = 1.0 if x > 0.0 else -1.0
    info = YEARS[year]
    segs = 18 if lod == 0 else 10
    _spin_tire(g, x, z, lod)
    # Proud of the sidewall. A solid disc here hid the spokes, so the rim is a band.
    # Past the sidewall cap. TIRE_W * 0.36 sat inside the rubber and the spokes vanished.
    outer = x + sign * (TIRE_W * 0.52)
    g.cylinder((outer - sign * 0.005, AXLE_Y, z), RIM_R * 0.70, 0.006, BLACK, segs, axis="X")
    _annulus(g, outer, z, RIM_R * 0.84, RIM_R * 1.05, 0.016, "Lib_PaintSilver", segs)
    face = outer + sign * 0.012
    spokes = info["spokes"] if lod == 0 else 5
    thick = max(info["thick"], 0.032) if lod == 0 else 0.038
    arm = RIM_R * 0.48
    length = RIM_R * 0.70
    for k in range(spokes):
        theta = math.radians(k * (360.0 / spokes) - 90.0)
        g.box(
            (face, AXLE_Y + math.cos(theta) * arm, z + math.sin(theta) * arm),
            (0.012, length, thick),
            "Lib_PaintSilver",
            euler=(math.degrees(theta), 0.0, 0.0),
        )
    g.cylinder((face + sign * 0.005, AXLE_Y, z), RIM_R * 0.15, 0.010, "Lib_SteelDark", 8, axis="X")


def _track(g, name, kind, center, size, mount):
    hx, hy, hz = size[0] * 0.5, size[1] * 0.5, size[2] * 0.5
    cx, cy, cz = center
    corners = []
    for dx in (-hx, hx):
        for dy in (-hy, hy):
            for dz in (-hz, hz):
                corners.append((cx + dx, cy + dy, cz + dz))
    g.accessory_items.append({
        "name": name,
        "kind": kind,
        "center": center,
        "size": size,
        "corners": corners,
        "mount": mount,
    })


def _slab(g, name, kind, x0, x1, y0, y1, z0, z1, mat, mount_z):
    if x1 - x0 < 0.004 or y1 - y0 < 0.004 or abs(z1 - z0) < 0.001:
        return
    center = ((x0 + x1) * 0.5, (y0 + y1) * 0.5, (z0 + z1) * 0.5)
    size = (abs(x1 - x0), abs(y1 - y0), abs(z1 - z0))
    g.box(center, size, mat)
    mx0 = x0 + 0.01
    mx1 = x1 - 0.01
    my0 = y0 + 0.006
    my1 = y1 - 0.006
    mount = (
        (mx0, my0, mount_z),
        (mx1, my0, mount_z),
        (mx0, my1, mount_z),
        (mx1, my1, mount_z),
        ((mx0 + mx1) * 0.5, (my0 + my1) * 0.5, mount_z),
    )
    _track(g, name, kind, center, size, mount)


def _ray_surface(bvh, x, y, origin_z):
    """First hit on the body, shot from outside along Z."""
    start = Vector(unity_to_blender(x, y, origin_z))
    direction = Vector((0.0, 1.0, 0.0)) if origin_z > 0.0 else Vector((0.0, -1.0, 0.0))
    hit, _normal, _index, _dist = bvh.ray_cast(start, direction, 6.0)
    if hit is None:
        return None
    return _u(hit)[2]


def _nose_slab(g, bvh, name, kind, x0, x1, y0, y1, mat):
    y = (y0 + y1) * 0.5
    x = (x0 + x1) * 0.5
    hit = _ray_surface(bvh, x, y, 3.6)
    if hit is None:
        hit = _shape_z(Z_NOSE, y) - 0.011
    # Proud of the pocket floor by 3 mm. +Z is out the nose.
    floor = hit
    _slab(g, name, kind, x0, x1, y0, y1, floor + 0.003, floor + 0.008, mat, floor + 0.003)


def _tail_slab(g, bvh, name, kind, x0, x1, y0, y1, mat):
    y = (y0 + y1) * 0.5
    x = (x0 + x1) * 0.5
    hit = _ray_surface(bvh, x, y, -3.6)
    if hit is None:
        hit = _shape_z(Z_TAIL, y) + 0.011
    floor = hit
    _slab(g, name, kind, x0, x1, y0, y1, floor - 0.008, floor - 0.003, mat, floor - 0.003)


def _lamps_front(g, info, lod, bvh):
    del lod
    kind = info["lamps"]
    y0, y1 = 0.635, 0.765
    inner, outer = 0.26, 0.60
    if kind == "thin":
        y0, y1 = 0.66, 0.74
        outer = 0.56
    elif kind == "swept":
        y0, y1 = 0.63, 0.78
        inner, outer = 0.24, 0.62
    elif kind == "tier":
        mid = (y0 + y1) * 0.5
        for sign in (-1.0, 1.0):
            x_a = sign * inner if sign > 0 else -outer
            x_b = sign * outer if sign > 0 else -inner
            lo, hi = (x_a, x_b) if x_a < x_b else (x_b, x_a)
            _nose_slab(g, bvh, "lamp_hi", "lamp", lo, hi, mid + 0.004, y1, "Lib_Headlamp")
            _nose_slab(g, bvh, "lamp_lo", "lamp", lo, hi, y0, mid - 0.004, "Lib_Headlamp")
        return
    for sign in (-1.0, 1.0):
        lo, hi = (sign * inner, sign * outer) if sign > 0 else (-outer, -inner)
        _nose_slab(g, bvh, "lamp", "lamp", lo, hi, y0, y1, "Lib_Headlamp")


def _grille(g, info, lod, bvh):
    del lod
    y0 = GRILLE["y0"] + 0.008
    y1 = GRILLE["y1"] - 0.008
    count = max(3, info["bars"])
    span = y1 - y0
    bar = min(info["bar_h"], span / (count * 2.2))
    step = span / count
    for i in range(count):
        cy = y0 + step * (i + 0.5)
        _nose_slab(g, bvh, "bar%d" % i, "grille", -0.22, 0.22, cy - bar * 0.5, cy + bar * 0.5, "Lib_Steel")


def _intake(g, info, lod, bvh):
    del lod
    y0 = INTAKE["y0"] + 0.008
    y1 = INTAKE["y1"] - 0.008
    if info["intake"]:
        _nose_slab(g, bvh, "intake_bar", "intake", -0.30, 0.30, y0 + 0.010, y0 + 0.022, "Lib_SteelDark")
        _nose_slab(g, bvh, "intake_bar2", "intake", -0.30, 0.30, y1 - 0.022, y1 - 0.010, "Lib_SteelDark")
        return
    fog = info.get("fog") or BLACK
    _nose_slab(g, bvh, "intake_plug", "plug", -0.22, 0.22, y0, y1, PAINT)
    _nose_slab(g, bvh, "fog_l", "lamp", -0.40, -0.26, y0, y1, fog)
    _nose_slab(g, bvh, "fog_r", "lamp", 0.26, 0.40, y0, y1, fog)


def _lamps_rear(g, info, lod, bvh):
    del lod
    y0, y1 = 0.92, 1.03
    inner, outer = 0.20, 0.66
    if info["tails"] == "thin":
        mid = (y0 + y1) * 0.5
        band = (y1 - y0) * 0.28
        for sign in (-1.0, 1.0):
            lo = -outer if sign < 0 else inner
            hi = -inner if sign < 0 else outer
            _tail_slab(g, bvh, "tail", "lamp", lo, hi, mid - band, mid + band, "Lib_Taillamp")
        return
    if info["tails"] == "wrap":
        outer = 0.70
    for sign in (-1.0, 1.0):
        lo = -outer if sign < 0 else inner
        hi = -inner if sign < 0 else outer
        _tail_slab(g, bvh, "tail", "lamp", lo, hi, y0, y1, "Lib_Taillamp")


def _skin_x(bvh, sign, y, z):
    """Door skin where a mirror sail meets it."""
    start = Vector(unity_to_blender(sign * 1.50, y, z))
    hit, _normal, _index, _dist = bvh.ray_cast(start, Vector((-sign, 0.0, 0.0)), 2.0)
    if hit is None:
        return sign * (_plan_x(z) - 0.04)
    return _u(hit)[0]


def _mirrors(g, info, lod, bvh):
    del lod
    mat = info["mirror"]
    for sign in (-1.0, 1.0):
        y, z = 1.00, 0.92
        skin = _skin_x(bvh, sign, y, z)
        sail_s = (0.014, 0.11, 0.18)
        inner = skin + sign * 0.001
        sail_c = (inner + sign * sail_s[0] * 0.5, y + 0.01, z)
        g.box(sail_c, sail_s, BLACK)
        mount = (
            (inner, y - 0.02, z - 0.04),
            (inner, y + 0.04, z + 0.04),
            (inner, y, z),
        )
        _track(g, "sail", "mirror", sail_c, sail_s, mount)
        stalk_s = (0.09, 0.016, 0.016)
        stalk_c = (
            sail_c[0] + sign * (sail_s[0] + stalk_s[0]) * 0.5,
            sail_c[1] + 0.045,
            sail_c[2] + 0.01,
        )
        g.box(stalk_c, stalk_s, BLACK)
        _track(g, "stalk", "mirror", stalk_c, stalk_s, mount)
        head_s = (0.055, 0.085, 0.13)
        head_c = (
            stalk_c[0] + sign * (stalk_s[0] + head_s[0]) * 0.5,
            stalk_c[1] + 0.01,
            stalk_c[2],
        )
        g.box(head_c, head_s, mat)
        _track(g, "mirror", "mirror", head_c, head_s, mount)


def _audit(g, bvh):
    problems = []
    rows = []
    for item in g.accessory_items:
        dists = []
        for pt in item["mount"]:
            _loc, _normal, _index, dist = bvh.find_nearest(Vector(unity_to_blender(*pt)))
            if dist is None:
                dists.append(1.0)
            else:
                dists.append(dist)
        gap = min(dists) if dists else 1.0
        ext = 0.0
        for x, y, z in item["corners"]:
            plan = _plan_x(z)
            top = _top_y(z)
            over_x = abs(x) - plan - 0.01
            over_y = y - top - 0.01
            under = BELLY - 0.01 - y
            over_z = max(z - (Z_NOSE + 0.01), (Z_TAIL - 0.01) - z)
            if item["kind"] == "mirror":
                over_x = abs(x) - plan - 0.20
            ext = max(ext, over_x, over_y, under, over_z)
        flag = ""
        if gap > 0.005:
            flag = "gap"
            problems.append("%s gap %.4f" % (item["name"], gap))
        if ext > 0.0:
            flag = (flag + "+ext") if flag else "ext"
            problems.append("%s extends %.4f" % (item["name"], ext))
        rows.append((item["name"], round(gap, 4), round(ext, 4), flag))
    g.accessory_report = {"rows": rows, "problems": problems}
    print("ACCESSORY", rows)
    return problems


def dress(g, year, lod, paint):
    del paint
    info = YEARS[year]
    bvh = BVHTree.FromBMesh(g.bm)
    g.accessory_items = []
    _lamps_front(g, info, lod, bvh)
    _grille(g, info, lod, bvh)
    _intake(g, info, lod, bvh)
    _lamps_rear(g, info, lod, bvh)
    if lod < 2:
        _mirrors(g, info, lod, bvh)
    for axle in (Z_REAR, Z_FRONT):
        for sign in (-1.0, 1.0):
            _wheel(g, sign * TIRE_X, axle, year, lod)
    _audit(g, bvh)


def add_colliders(asset):
    for i, axle in enumerate((Z_REAR, Z_FRONT)):
        for j, sign in enumerate((-1.0, 1.0)):
            # Low in the tread, clear of the hub discs. A box on the axle
            # sits in two shells and the parity test calls it outside.
            asset.box(
                "Col_Wheel_%d%d" % (i, j),
                (sign * TIRE_X, 0.08, axle),
                (0.020, 0.016, 0.016),
            )
    asset.box("Col_Cabin", (0.0, 0.78, -0.05), (0.46, 0.36, 0.70))
    asset.box("Col_Roof", (0.0, 1.30, -0.15), (0.36, 0.06, 0.70))
    asset.box("Col_Hood", (0.0, 0.78, 1.85), (0.36, 0.08, 0.36))
    asset.box("Col_Deck", (0.0, 0.98, -1.90), (0.36, 0.06, 0.36))
    asset.box("Col_Nose", (0.0, 0.48, 2.02), (0.28, 0.14, 0.18))


def probe_spec(name):
    y0 = _top_y(Z_HEADER)
    y1 = _top_y(Z_COWL)
    ny, nz = shell._rake_normal(y0, Z_HEADER, y1, Z_COWL)
    y = (y0 + y1) * 0.5
    z = (Z_HEADER + Z_COWL) * 0.5
    return {
        "name": name,
        "glass_tests": [
            (0.0, y + ny * 0.04, z + nz * 0.04),
            (0.0, y - ny * 0.05, z - nz * 0.05),
        ],
    }


def _topology(bm):
    """Non-manifold edges, and faces whose normal flips against a neighbour.

    A grille corner or lamp end is a sharp convex edge: both normals point
    outward, so each face sees the neighbour behind its plane. A crumpled
    or reversed face points at that neighbour, or lies almost coplanar with
    the opposite orientation. Those are the failures.
    """
    bm.edges.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    bm.normal_update()
    nonmanifold = 0
    flips = 0
    samples = []
    for edge in bm.edges:
        faces = edge.link_faces
        if len(faces) != 2:
            nonmanifold += 1
            if len(samples) < 8:
                c = _u(edge.verts[0].co)
                samples.append(("open", tuple(round(v, 3) for v in c), len(faces)))
            continue
        dot = faces[0].normal.dot(faces[1].normal)
        if dot >= -0.15:
            continue
        c0 = faces[0].calc_center_median()
        c1 = faces[1].calc_center_median()
        delta = c1 - c0
        points_at_neighbour = (
            faces[0].normal.dot(delta) > 1.0e-4
            and faces[1].normal.dot(delta) < -1.0e-4
        )
        reversed_panel = dot < -0.92
        if not points_at_neighbour and not reversed_panel:
            continue
        flips += 1
        if len(samples) < 8:
            mid = (edge.verts[0].co + edge.verts[1].co) * 0.5
            c = _u(mid)
            samples.append(("flip", tuple(round(v, 3) for v in c), round(dot, 3)))
    return nonmanifold, flips, samples


def _is_wheel(x, y, z):
    if abs(x) < 0.55:
        return False
    for axle in (Z_FRONT, Z_REAR):
        dy = y - AXLE_Y
        dz = z - axle
        if dy * dy + dz * dz <= (TIRE_R + 0.03) ** 2:
            return True
    return False


def _body_points(bm):
    pts = []
    for vert in bm.verts:
        x, y, z = _u(vert.co)
        if abs(x) > _plan_x(z) + 0.015 or _is_wheel(x, y, z):
            continue
        pts.append((x, y, z))
    return pts


def _cross_section(bm, z):
    """Where body edges cross a constant-z plane. Skips wheels and mirrors."""
    hits = []
    seen = set()
    bm.edges.ensure_lookup_table()
    bm.verts.ensure_lookup_table()
    for edge in bm.edges:
        key = tuple(sorted((edge.verts[0].index, edge.verts[1].index)))
        if key in seen:
            continue
        seen.add(key)
        a = _u(edge.verts[0].co)
        b = _u(edge.verts[1].co)
        if _is_wheel(*a) or _is_wheel(*b):
            continue
        if abs(a[0]) > _plan_x(a[2]) + 0.02 or abs(b[0]) > _plan_x(b[2]) + 0.02:
            continue
        dz = b[2] - a[2]
        if abs(dz) < 1.0e-8:
            continue
        if (a[2] - z) * (b[2] - z) > 0.0:
            continue
        t = (z - a[2]) / dz
        if t < -1.0e-4 or t > 1.0001:
            continue
        hits.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t))
    return hits


def _silhouette(bm):
    """Max deviation of the body from the orthographic profiles, in meters."""
    pts = _body_points(bm)
    max_y = max((y for x, y, _z in pts if abs(x) < 0.12 and y > 0.5), default=0.0)
    min_y = min((y for x, y, _z in pts if abs(x) < 0.35), default=BELLY)
    max_z = max(z for _x, _y, z in pts)
    min_z = min(z for _x, _y, z in pts)
    max_x = max(abs(x) for x, y, _z in pts if 0.30 < y < 1.25)
    worst = 0.0
    notes = []

    def keep(label, err):
        nonlocal worst
        worst = max(worst, abs(err))
        if abs(err) > 0.02:
            notes.append((label, round(err, 4)))

    keep("length", (max_z - min_z) - LENGTH)
    keep("nose", max_z - Z_NOSE)
    keep("tail", min_z - Z_TAIL)
    keep("height", max_y - HEIGHT)
    keep("width", max_x * 2.0 - WIDTH)
    keep("belly", min_y - BELLY)
    for i in range(-23, 24):
        z = i * 0.10
        hits = _cross_section(bm, z)
        if not hits:
            continue
        crown = [y for x, y in hits if abs(x) < 0.10 and y > 0.45]
        if crown:
            keep("top@%+.1f" % z, max(crown) - _top_y(z))
        flank = [abs(x) for x, y in hits if 0.28 < y < 1.25]
        if flank:
            keep("plan@%+.1f" % z, max(flank) - _plan_x(z))
    for x, y, z in pts:
        if abs(x) > _plan_x(z) + 0.04:
            continue
        over = y - _top_y(z)
        if over > 0.02:
            keep("above@%+.1f" % z, over - 0.02)
    nose_low = [abs(x) for x, y, z in pts if z > 2.05 and y < 0.38]
    nose_high = [abs(x) for x, y, z in pts if z > 2.05 and 0.64 < y < 0.90]
    if nose_low and nose_high:
        keep("fascia_flare", max(0.0, max(nose_low) - (max(nose_high) - 0.03)))
    deck = [y for x, y, z in pts if z < -1.85 and abs(x) < 0.55 and y > 0.85]
    if deck:
        keep("deck_low", max(0.0, 1.05 - max(deck)))
        keep("deck_high", max(0.0, max(deck) - 1.12))
        quarters = [y for x, y, z in pts if z < -1.85 and 0.25 < abs(x) < 0.55 and y > 0.90]
        if quarters:
            keep("deck_drop", max(0.0, max(deck) - min(quarters) - 0.05))
    hood = [y for x, y, z in pts if z > 2.25 and abs(x) < 0.15 and y > 0.55]
    if hood:
        keep("hood_low", max(0.0, 0.80 - max(hood)))
        keep("hood_high", max(0.0, max(hood) - 0.86))
    print("SILHOUETTE", round(worst, 4), "size", round(max_x * 2.0, 3), round(max_y, 3), round(max_z - min_z, 3))
    if notes:
        print("SILHOUETTE_NOTES", notes[:14])
    return worst


def _stroke(g, a, b, radius=0.010):
    if (Vector(a) - Vector(b)).length < 1.0e-4:
        return
    g.pipe(a, b, radius, "Lib_Orange", segments=4)


def _poly_yz(x, samples):
    return [(x, y, z) for z, y in samples]


def _poly_xy(z, samples):
    return [(x, y, z) for x, y in samples]


def add_reference(g, views=("side", "front", "rear", "top")):
    """Orthographic outline, drawn just outside the body. Our own lines."""
    zs = [i * 0.05 - 2.45 for i in range(99)]
    side = [(z, _top_y(z)) for z in zs]
    side += [(Z_TAIL, BELLY), (Z_NOSE, BELLY)]
    side[0:0] = [(Z_NOSE, _top_y(Z_NOSE))]
    # Close the profile the long way: nose down, along the belly, tail up.
    profile = [(z, _top_y(z)) for z in zs]
    profile.append((Z_TAIL, BELLY))
    profile.append((Z_NOSE, BELLY))
    if "side" in views:
        x_line = -(HALF_W + 0.04)
        crown = [(x_line, _top_y(z), z) for z in zs]
        for a, b in zip(crown, crown[1:]):
            _stroke(g, a, b)
        nose = []
        for i in range(16):
            y = 0.83 - (0.83 - BELLY) * i / 15.0
            nose.append((x_line, y, _shape_z(Z_NOSE, y)))
        tail = []
        for i in range(16):
            y = 1.07 - (1.07 - BELLY) * i / 15.0
            tail.append((x_line, y, _shape_z(Z_TAIL, y)))
        for a, b in zip(nose, nose[1:]):
            _stroke(g, a, b)
        for a, b in zip(tail, tail[1:]):
            _stroke(g, a, b)
        _stroke(g, crown[-1], nose[0])
        _stroke(g, crown[0], tail[0])
        _stroke(g, nose[-1], tail[-1])
        for axle in (Z_FRONT, Z_REAR):
            arc = []
            for i in range(19):
                ang = math.pi * i / 18.0
                arc.append((
                    HALF_W + 0.018,
                    AXLE_Y + math.sin(ang) * ARCH_R,
                    axle + math.cos(ang) * ARCH_R,
                ))
            for a, b in zip(arc, arc[1:]):
                _stroke(g, a, b, 0.006)
        belt = [(HALF_W + 0.012, _belt_y(z), z) for z in zs if -1.05 <= z <= 1.10]
        for a, b in zip(belt, belt[1:]):
            _stroke(g, a, b, 0.005)
    if "front" in views:
        ring = _section(Z_NOSE, False)
        pts = [(x, y, Z_NOSE + 0.02) for x, y in ring]
        pts.append(pts[0])
        for a, b in zip(pts, pts[1:]):
            _stroke(g, a, b, 0.006)
    if "rear" in views:
        ring = _section(Z_TAIL, False)
        pts = [(x, y, Z_TAIL - 0.02) for x, y in ring]
        pts.append(pts[0])
        for a, b in zip(pts, pts[1:]):
            _stroke(g, a, b, 0.006)
    if "top" in views:
        plan = [(z, _plan_x(z)) for z in zs]
        left = [(-x, HEIGHT + 0.02, z) for z, x in plan]
        right = [(x, HEIGHT + 0.02, z) for z, x in plan]
        for seq in (left, right):
            for a, b in zip(seq, seq[1:]):
                _stroke(g, a, b, 0.006)
        _stroke(g, left[0], right[0], 0.006)
        _stroke(g, left[-1], right[-1], 0.006)


def reference_asset(views=("side", "front", "rear", "top")):
    from _common import Asset

    asset = Asset("SedanGuides", "Vehicles", "Orthographic sedan outlines.")
    g = asset.begin(0)
    add_reference(g, views=views)
    asset.end()
    return asset


def shade_object(obj):
    """Crisp panel shading for stills. The bevel is already in the mesh."""
    mod = obj.modifiers.new("WeightedNormal", "WEIGHTED_NORMAL")
    mod.keep_sharp = True
    mod.weight = 80
    mod.mode = "FACE_AREA_WITH_ANGLE"
    return obj


def _check_render(asset):
    if os.environ.get("SEDAN_SKIP_CHECK_RENDER") == "1":
        print("SHELL_CHECK render skipped")
        return
    import render_pass2 as stills

    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    out = os.path.join(REPO, "Docs", "AssetStills", "vehicles", "sedan_mid_a", "pass12")
    os.makedirs(out, exist_ok=True)
    year = asset.name[-2:]
    scene = bpy.context.scene
    stills._engine(scene, wide=True)
    scene.cycles.samples = 8
    stills._ensure_materials()
    views = (
        ("front", (0.0, 0.72, 8.0), (0.0, 0.72, 0.0), 3.4),
        ("side", (8.0, 0.72, 0.0), (0.0, 0.72, 0.0), 6.2),
        ("top", (0.0, 12.0, 0.0), (0.0, 0.4, 0.0), 6.2),
        ("rear", (0.0, 0.72, -8.0), (0.0, 0.72, 0.0), 3.4),
    )
    for label, eye, aim, scale in views:
        for obj in list(bpy.data.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        stills._world(scene, night=False)
        body =         stills._spawn(asset, (0.0, 0.0, 0.0))
        shade_object(body)
        stills._spawn(reference_asset(views=(label,)), (0.0, 0.0, 0.0))
        stills._ground("asphalt", 40.0)
        cam_data = bpy.data.cameras.new("Cam")
        cam_data.type = "ORTHO"
        cam_data.ortho_scale = scale
        cam = bpy.data.objects.new("Cam", cam_data)
        scene.collection.objects.link(cam)
        scene.camera = cam
        cam.location = Vector(unity_to_blender(*eye))
        direction = Vector(unity_to_blender(*aim)) - cam.location
        cam.rotation_mode = "QUATERNION"
        cam.rotation_quaternion = direction.to_track_quat("-Z", "Y")
        path = os.path.join(out, "check_%s_%s.png" % (label, year))
        stills._render(scene, path)
        print("CHECK_RENDER", path)


def self_check(asset):
    print("SHELL_CHECK", asset.name)
    problems = []
    for lod, geo in sorted(asset.lods.items()):
        nonmanifold, flips, samples = _topology(geo.bm)
        print(
            "SHELL_CHECK lod%d nonmanifold_edges %d normal_flips %d" % (
                lod, nonmanifold, flips,
            )
        )
        if samples:
            print("SHELL_CHECK lod%d samples" % lod, samples)
        if nonmanifold:
            problems.append("lod%d nonmanifold %d" % (lod, nonmanifold))
        if flips:
            problems.append("lod%d normal flips %d" % (lod, flips))
        worst = _silhouette(geo.bm)
        print("SHELL_CHECK lod%d silhouette_m %.4f" % (lod, worst))
        if worst > 0.03:
            problems.append("lod%d silhouette %.3f" % (lod, worst))
        report = getattr(geo, "accessory_report", None)
        if report is None:
            problems.append("lod%d missing accessory report" % lod)
        elif report["problems"]:
            problems.extend("lod%d %s" % (lod, row) for row in report["problems"])
    _check_render(asset)
    if problems:
        raise RuntimeError("shell check failed: " + "; ".join(problems))
    print("SHELL_CHECK ok")


def measure(asset):
    self_check(asset)
    max_x = 0.0
    body_x = 0.0
    max_y = 0.0
    min_z = 99.0
    max_z = -99.0
    for vert in asset.lods[0].bm.verts:
        x, y, z = _u(vert.co)
        if _is_wheel(x, y, z):
            continue
        max_x = max(max_x, abs(x))
        if 0.45 < y < 1.15 and abs(z) < 1.0 and abs(x) < 0.98:
            body_x = max(body_x, abs(x))
        if abs(x) < 0.98:
            max_y = max(max_y, y)
        max_z = max(max_z, z)
        min_z = min(min_z, z)
    print(
        "SEDAN_MEASURE", asset.name,
        "size", round(body_x * 2.0, 3), round(max_y, 3), round(max_z - min_z, 3),
        "ymax", round(max_y, 3),
        "tris", asset.lods[0].tri_count(),
    )
