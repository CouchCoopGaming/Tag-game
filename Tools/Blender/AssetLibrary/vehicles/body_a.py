"""Midsize sedan, hard-surface panels.

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
Z_HEADER = 0.55
Z_COWL = 1.381

TIRE_W = 0.235
RIM_R = 18.0 * INCH * 0.5
TIRE_R = RIM_R + TIRE_W * 0.45
AXLE_Y = TIRE_R
ARCH_GAP = 0.040
ARCH_R = TIRE_R + ARCH_GAP
TIRE_X = min(63.0 * INCH * 0.5, HALF_W - TIRE_W * 0.5 - 0.012)

PAINT = "Lib_PaintCrimson"
GLASS = "Lib_AutoGlass"
BLACK = "Lib_Black"
MATS = (PAINT, GLASS, BLACK)
MAT_PAINT = 0
MAT_GLASS = 1
MAT_BLACK = 2

# Fascia openings. Hardware sits inside these, 3 mm off the floor.
LAMP = {"x0": -0.60, "x1": 0.60, "y0": 0.52, "y1": 0.655, "floor": Z_NOSE - 0.011}
GRILLE = {"x0": -0.46, "x1": 0.46, "y0": 0.345, "y1": 0.495, "floor": Z_NOSE - 0.012}
INTAKE = {"x0": -0.50, "x1": 0.50, "y0": 0.185, "y1": 0.295, "floor": Z_NOSE - 0.012}
TAIL = {"x0": -0.50, "x1": 0.50, "y0": 0.70, "y1": 0.835, "floor": Z_TAIL + 0.010}

YEARS = {
    2021: {"spokes": 5, "thick": 0.036, "bars": 4, "bar_h": 0.016, "lamps": "separate", "tails": "separate", "intake": False, "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2022: {"spokes": 6, "thick": 0.024, "bars": 6, "bar_h": 0.010, "lamps": "separate", "tails": "separate", "intake": False, "mirror": PAINT, "fog": "Lib_Headlamp"},
    2023: {"spokes": 5, "thick": 0.030, "bars": 5, "bar_h": 0.012, "lamps": "tier", "tails": "separate", "intake": False, "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2024: {"spokes": 6, "thick": 0.018, "bars": 6, "bar_h": 0.008, "lamps": "thin", "tails": "thin", "intake": False, "mirror": BLACK, "fog": "Lib_Headlamp"},
    2025: {"spokes": 5, "thick": 0.026, "bars": 4, "bar_h": 0.014, "lamps": "bar", "tails": "bar", "intake": True, "mirror": PAINT, "fog": None},
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
    """Centerline of hood, windshield, roof, backlight, and deck."""
    return _lerp((
        (-2.45, 0.96),
        (-2.15, 1.00),
        (-1.50, 1.05),
        (-1.05, 1.40),
        (-0.55, 1.44),
        (-0.10, 1.45),
        (0.25, 1.445),
        (0.55, 1.40),
        (1.381, 0.92),
        (2.45, 0.70),
    ), z)


def _plan_x(z):
    """Half-width of the shoulder, the widest painted line."""
    return _lerp((
        (-2.45, 0.64),
        (-2.05, 0.76),
        (-1.55, 0.88),
        (-1.33, 0.92),
        (1.15, 0.92),
        (1.49, 0.92),
        (1.85, 0.84),
        (2.15, 0.76),
        (2.45, 0.70),
    ), z)


def _shoulder_y(z):
    return _lerp((
        (-2.45, 0.78),
        (-1.10, 0.76),
        (0.20, 0.74),
        (1.50, 0.72),
        (2.45, 0.66),
    ), z)


def _belt_y(z):
    return _lerp((
        (-2.45, 0.88),
        (-1.50, 0.94),
        (-1.05, 1.02),
        (1.05, 1.02),
        (1.381, 0.90),
        (2.45, 0.70),
    ), z)


def _rail_y(z):
    return _lerp((
        (-2.45, 0.94),
        (-1.50, 1.02),
        (-1.05, 1.36),
        (0.55, 1.36),
        (1.381, 0.92),
        (2.45, 0.70),
    ), z)


def _rail_x(z):
    return _lerp((
        (-2.45, 0.40),
        (-1.50, 0.52),
        (-1.05, 0.60),
        (0.20, 0.64),
        (0.55, 0.60),
        (1.381, 0.52),
        (2.45, 0.34),
    ), z)


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
    """Fourteen (x, y) points. Bottom center, up the right, down the left."""
    plan = _plan_x(z)
    top = _top_y(z)
    shoulder = _shoulder_y(z)
    belt = min(_belt_y(z), top - 0.004)
    rail = min(max(_rail_y(z), belt + 0.01), top - 0.004)
    belt_x = plan - 0.08
    rail_x = min(_rail_x(z), belt_x - 0.04)
    half = [
        (0.0, BELLY),
        (max(0.20, plan - 0.42), BELLY),
        (plan - 0.16, 0.155),
        (plan - 0.10, 0.34),
        (plan, shoulder),
        (max(0.20, plan - 0.08), max(belt, shoulder + 0.02)),
        (max(0.12, rail_x), max(rail, shoulder + 0.08)),
        (0.0, top),
    ]
    # Keep the stack under the centerline so the hood stays a low plane.
    for i in range(1, 7):
        x, y = half[i]
        cap = top - 0.004 * (7 - i)
        if y > cap:
            half[i] = (x, cap)
    half[7] = (0.0, top)
    lip = _arch_lip_y(z)
    if lip is not None and lip > 0.42 and not door:
        outer = plan
        half[4] = (outer, max(shoulder, lip + 0.030))
        half[3] = (outer, lip)
        half[2] = (0.56, lip)
        half[1] = (0.48, 0.16)
        if half[5][1] < half[4][1] + 0.02:
            half[5] = (half[5][0], half[4][1] + 0.02)
    if door:
        for i in (2, 3, 4, 5):
            x, y = half[i]
            half[i] = (max(0.05, x - 0.003), y)
    ring = list(half)
    for x, y in reversed(half[1:-1]):
        ring.append((-x, y))
    return ring


def _stations():
    rows = [
        (2.450, False),
        (1.381, False),
        (1.100, False),
        (1.097, True),
        (0.550, True),
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
        (-2.200, False),
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


def _cut_caps(bm):
    ok = True
    for z_plane, rects, cuts_x, cuts_y in (
        (Z_NOSE, (LAMP, GRILLE, INTAKE), (0.46, 0.50, 0.60), (0.185, 0.295, 0.345, 0.495, 0.52, 0.655)),
        (Z_TAIL, (TAIL,), (0.50,), (0.70, 0.835)),
    ):
        cap = _faces_on_plane(bm, z_plane)
        for x in cuts_x:
            _bisect_cap(bm, cap, (x, 0.0, 0.0), (1.0, 0.0, 0.0))
            _bisect_cap(bm, cap, (-x, 0.0, 0.0), (1.0, 0.0, 0.0))
            cap = _faces_on_plane(bm, z_plane)
        for y in cuts_y:
            _bisect_cap(bm, cap, (0.0, y, 0.0), (0.0, 1.0, 0.0))
            cap = _faces_on_plane(bm, z_plane)
        for rect in rects:
            if not _recess(bm, z_plane, rect):
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
        if min(a[2], b[2]) < Z_TAIL + 0.03 or max(a[2], b[2]) > Z_NOSE - 0.03:
            continue
        z = (a[2] + b[2]) * 0.5
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
        elif abs(y - _belt_y(z)) < 0.04 and abs(x - (plan - 0.08)) < 0.045:
            hit = True
        elif abs(y - _rail_y(z)) < 0.04 and abs(x - _rail_x(z)) < 0.05 and _rail_y(z) < _top_y(z) - 0.03:
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
    mod.width = 0.012
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
    if touches and 0.62 < cz < 1.32 and cy > 1.00 and max(abs(x) for x in xs) < 0.78:
        return MAT_GLASS
    if touches and -1.46 < cz < -1.08 and cy > 1.06 and max(abs(x) for x in xs) < 0.72:
        return MAT_GLASS
    ax = abs(cx)
    if ax > 0.58 and 1.05 < cy < 1.32 and max(zs) - min(zs) > 0.08:
        if 0.20 < cz < 0.98 or -0.88 < cz < -0.06:
            return MAT_GLASS
        if -1.28 < cz < -1.06:
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
    print("MATS glass", glass, "black", black)
    return glass


def _inset_glass(bm):
    faces = [face for face in bm.faces if face.material_index == MAT_GLASS]
    if not faces:
        return
    before = {face for face in bm.faces}
    try:
        bmesh.ops.inset_region(
            bm,
            faces=faces,
            thickness=0.012,
            depth=-0.008,
            use_even_offset=True,
            use_boundary=True,
        )
    except (TypeError, ValueError, RuntimeError) as exc:
        print("GLASS_INSET_FAIL", exc)
        return
    bm.faces.ensure_lookup_table()
    bm.normal_update()
    for face in bm.faces:
        if face in before or not face.is_valid:
            continue
        pts = [_u(vert.co) for vert in face.verts]
        span_x = max(p[0] for p in pts) - min(p[0] for p in pts)
        span_y = max(p[1] for p in pts) - min(p[1] for p in pts)
        span_z = max(p[2] for p in pts) - min(p[2] for p in pts)
        if span_z > 0.05 or span_y > 0.08 or span_x > 0.08:
            face.material_index = MAT_GLASS
        else:
            face.material_index = MAT_PAINT
    print("GLASS_INSET", len(faces))


def _build_cage(lod):
    bm = bmesh.new()
    rings = []
    for z, door in _stations():
        ring = [bm.verts.new(unity_to_blender(x, y, z)) for x, y in _section(z, door)]
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
        bm.faces.new(rings[0])
    except ValueError as exc:
        print("NOSE_CAP_FAIL", exc)
    try:
        bm.faces.new(list(reversed(rings[-1])))
    except ValueError as exc:
        print("TAIL_CAP_FAIL", exc)
    _orient_outward(bm)
    if not _cut_caps(bm):
        raise RuntimeError("fascia pockets were not cut")
    _orient_outward(bm)
    bm = _bevel(bm, lod)
    _orient_outward(bm)
    open_edges, flipped, samples = _topology(bm)
    print("AFTER_BEVEL", lod, len(bm.faces), "open", open_edges, "flips", flipped, samples[:4])
    glass = _paint(bm)
    if glass < 8:
        print("GLASS_LOW", glass)
    _inset_glass(bm)
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


def _nose_slab(g, name, kind, x0, x1, y0, y1, mat, floor):
    # Visible face 4 mm behind the fascia. Mount face 3 mm off the pocket floor.
    _slab(g, name, kind, x0, x1, y0, y1, floor + 0.003, Z_NOSE - 0.004, mat, floor + 0.003)


def _tail_slab(g, name, kind, x0, x1, y0, y1, mat, floor):
    _slab(g, name, kind, x0, x1, y0, y1, Z_TAIL + 0.004, floor - 0.003, mat, floor - 0.003)


def _lamps_front(g, info, lod):
    del lod
    y0 = LAMP["y0"] + 0.012
    y1 = LAMP["y1"] - 0.012
    floor = LAMP["floor"]
    kind = info["lamps"]
    if kind == "bar":
        _nose_slab(g, "lamp", "lamp", -0.56, 0.56, y0, y1, "Lib_Headlamp", floor)
        return
    gap = 0.22 if kind == "thin" else 0.16
    height = (y1 - y0)
    if kind == "thin":
        y0 = y0 + height * 0.28
        y1 = y1 - height * 0.28
    if kind == "tier":
        mid = (y0 + y1) * 0.5
        for sign, x_a, x_b in ((1, 0.18, 0.54), (-1, -0.54, -0.18)):
            _nose_slab(g, "lamp_hi", "lamp", x_a, x_b, mid + 0.004, y1, "Lib_Headlamp", floor)
            _nose_slab(g, "lamp_lo", "lamp", x_a, x_b, y0, mid - 0.004, "Lib_Headlamp", floor)
        _nose_slab(g, "lamp_plug", "plug", -0.16, 0.16, y0, y1, PAINT, floor)
        return
    _nose_slab(g, "lamp_l", "lamp", -0.56, -gap, y0, y1, "Lib_Headlamp", floor)
    _nose_slab(g, "lamp_r", "lamp", gap, 0.56, y0, y1, "Lib_Headlamp", floor)
    _nose_slab(g, "lamp_plug", "plug", -gap + 0.008, gap - 0.008, y0, y1, PAINT, floor)


def _grille(g, info, lod):
    del lod
    y0 = GRILLE["y0"] + 0.008
    y1 = GRILLE["y1"] - 0.008
    floor = GRILLE["floor"]
    count = max(3, info["bars"])
    span = y1 - y0
    bar = min(info["bar_h"], span / (count * 2.2))
    step = span / count
    for i in range(count):
        cy = y0 + step * (i + 0.5)
        _nose_slab(g, "bar%d" % i, "grille", -0.42, 0.42, cy - bar * 0.5, cy + bar * 0.5, "Lib_Steel", floor)


def _intake(g, info, lod):
    del lod
    y0 = INTAKE["y0"] + 0.008
    y1 = INTAKE["y1"] - 0.008
    floor = INTAKE["floor"]
    if info["intake"]:
        _nose_slab(g, "intake_bar", "intake", -0.36, 0.36, y0 + 0.012, y0 + 0.024, "Lib_SteelDark", floor)
        _nose_slab(g, "intake_bar2", "intake", -0.36, 0.36, y1 - 0.024, y1 - 0.012, "Lib_SteelDark", floor)
        return
    # Closed bumper. Fog lamps occupy the outer corners of the same pocket.
    fog = info.get("fog") or BLACK
    _nose_slab(g, "intake_plug", "plug", -0.28, 0.28, y0, y1, PAINT, floor)
    _nose_slab(g, "fog_l", "lamp", -0.46, -0.32, y0, y1, fog, floor)
    _nose_slab(g, "fog_r", "lamp", 0.32, 0.46, y0, y1, fog, floor)


def _lamps_rear(g, info, lod):
    del lod
    y0 = TAIL["y0"] + 0.012
    y1 = TAIL["y1"] - 0.012
    floor = TAIL["floor"]
    # Leave a black bezel of the pocket around every lens so the lamp is not body-colored.
    if info["tails"] == "bar":
        _tail_slab(g, "tail", "lamp", -0.40, 0.40, y0 + 0.008, y1 - 0.008, "Lib_Taillamp", floor)
        return
    if info["tails"] == "thin":
        mid = (y0 + y1) * 0.5
        band = (y1 - y0) * 0.22
        _tail_slab(g, "tail_l", "lamp", -0.42, -0.18, mid - band, mid + band, "Lib_Taillamp", floor)
        _tail_slab(g, "tail_r", "lamp", 0.18, 0.42, mid - band, mid + band, "Lib_Taillamp", floor)
        _tail_slab(g, "tail_plug", "plug", -0.14, 0.14, y0, y1, PAINT, floor)
        return
    _tail_slab(g, "tail_l", "lamp", -0.42, -0.16, y0, y1, "Lib_Taillamp", floor)
    _tail_slab(g, "tail_r", "lamp", 0.16, 0.42, y0, y1, "Lib_Taillamp", floor)
    _tail_slab(g, "tail_plug", "plug", -0.12, 0.12, y0, y1, PAINT, floor)


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
        y, z = 1.01, 1.14
        skin = _skin_x(bvh, sign, y, z)
        sail_s = (0.10, 0.050, 0.040)
        inner = skin + sign * 0.003
        sail_c = (inner + sign * sail_s[0] * 0.5, y, z)
        g.box(sail_c, sail_s, BLACK)
        mount = (
            (inner, y - 0.012, z - 0.012),
            (inner, y + 0.012, z + 0.012),
            (inner, y, z),
        )
        _track(g, "sail", "mirror", sail_c, sail_s, mount)
        head_s = (0.08, 0.065, 0.12)
        head_c = (sail_c[0] + sign * (sail_s[0] + head_s[0]) * 0.5, y + 0.006, z - 0.03)
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
    _lamps_front(g, info, lod)
    _grille(g, info, lod)
    _intake(g, info, lod)
    _lamps_rear(g, info, lod)
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
    asset.box("Col_Hood", (0.0, 0.68, 1.90), (0.36, 0.08, 0.40))
    asset.box("Col_Deck", (0.0, 0.88, -1.85), (0.36, 0.06, 0.40))
    asset.box("Col_Nose", (0.0, 0.42, 2.15), (0.32, 0.16, 0.28))


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
        if abs(x) > 0.98 or _is_wheel(x, y, z):
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
        if abs(a[0]) > 0.98 and abs(b[0]) > 0.98:
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
    print("SILHOUETTE", round(worst, 4), "size", round(max_x * 2.0, 3), round(max_y, 3), round(max_z - min_z, 3))
    if notes:
        print("SILHOUETTE_NOTES", notes[:14])
    return worst


def _stroke(g, a, b, radius=0.008):
    if (Vector(a) - Vector(b)).length < 1.0e-4:
        return
    g.pipe(a, b, radius, "Lib_Lane", segments=4)


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
        pts = _poly_yz(HALF_W + 0.018, profile)
        # The list above is not in loop order. Draw the crown, then the ends.
        crown = [(HALF_W + 0.018, _top_y(z), z) for z in zs]
        for a, b in zip(crown, crown[1:]):
            _stroke(g, a, b)
        _stroke(g, crown[0], (HALF_W + 0.018, BELLY, Z_NOSE))
        _stroke(g, crown[-1], (HALF_W + 0.018, BELLY, Z_TAIL))
        _stroke(g, (HALF_W + 0.018, BELLY, Z_TAIL), (HALF_W + 0.018, BELLY, Z_NOSE))
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


def reference_asset():
    from _common import Asset

    asset = Asset("SedanGuides", "Vehicles", "Orthographic sedan outlines.")
    g = asset.begin(0)
    add_reference(g)
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
    out = os.path.join(REPO, "Docs", "AssetStills", "vehicles", "sedan_mid_a", "pass11")
    os.makedirs(out, exist_ok=True)
    year = asset.name[-2:]
    scene = bpy.context.scene
    stills._engine(scene, wide=True)
    scene.cycles.samples = 8
    stills._ensure_materials()
    guides = reference_asset()
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
        body = stills._spawn(asset, (0.0, 0.0, 0.0))
        shade_object(body)
        stills._spawn(guides, (0.0, 0.0, 0.0))
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
