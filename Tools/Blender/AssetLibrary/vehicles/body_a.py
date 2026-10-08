"""Midsize sedan shell. One quad cage, subdivided, for every model year.

The cage is a closed loop of cross-sections driven by the side crown, the
plan width, and the front view. A level-2 subdivision surface rounds it.
Hard edges are creases. Nothing is booleaned, inset, or pushed apart.

Fascia, grille, lamps, and mirrors are separate closed pieces held 3 mm
off the body. Shut lines are a 3 mm groove in the subdivided skin.

About 4.90 m long, 1.84 m wide without mirrors, 1.44 m tall, 2.82 m
wheelbase. The cowl sits just behind the front axle. Four doors, a long
greenhouse, a fastback into a short deck. No badges.
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
HEIGHT = 1.44
WHEELBASE = 2.82
FRONT_OVERHANG = 0.96
Z_FRONT = HALF_L - FRONT_OVERHANG
Z_REAR = Z_FRONT - WHEELBASE
BELLY = 5.4 * INCH
Z_NOSE = 2.435
Z_TAIL = -2.435
Z_HEADER = 0.95
Z_COWL = 1.40

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

GAP = 0.0035

YEARS = {
    2021: {"spokes": 5, "thick": 0.034, "bars": 4, "bar_h": 0.020, "lamps": "separate", "lamp_h": 0.026, "tails": "separate", "intake": False, "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2022: {"spokes": 10, "thick": 0.014, "bars": 8, "bar_h": 0.009, "lamps": "separate", "lamp_h": 0.020, "tails": "separate", "intake": False, "mirror": PAINT, "fog": "Lib_Headlamp"},
    2023: {"spokes": 7, "thick": 0.020, "bars": 6, "bar_h": 0.012, "lamps": "tier", "lamp_h": 0.018, "tails": "separate", "intake": False, "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2024: {"spokes": 12, "thick": 0.010, "bars": 7, "bar_h": 0.008, "lamps": "thin", "lamp_h": 0.012, "tails": "thin", "intake": False, "mirror": BLACK, "fog": "Lib_Headlamp"},
    2025: {"spokes": 15, "thick": 0.007, "bars": 9, "bar_h": 0.008, "lamps": "bar", "lamp_h": 0.024, "tails": "bar", "intake": True, "mirror": PAINT, "fog": None},
}

CROWN = (
    (-2.48, 0.50),
    (-2.22, 0.72),
    (-1.98, 0.98),
    (-1.70, 1.04),
    (-1.48, 1.08),
    (-1.15, 1.36),
    (-0.70, 1.45),
    (0.05, 1.475),
    (0.55, 1.46),
    (0.95, 1.34),
    (1.22, 1.14),
    (1.42, 1.02),
    (1.75, 0.94),
    (2.10, 0.86),
    (2.32, 0.78),
    (2.48, 0.72),
)

BELT = (
    (-1.15, 1.00),
    (-0.30, 0.99),
    (0.55, 0.98),
    (1.30, 0.97),
)

PLAN = (
    (-2.48, 0.66),
    (-2.10, 0.84),
    (-1.70, 0.905),
    (-1.05, 0.930),
    (0.50, 0.935),
    (1.30, 0.925),
    (1.70, 0.88),
    (2.05, 0.84),
    (2.30, 0.78),
    (2.48, 0.72),
)

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


def _smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def _crown(z):
    return _lerp(CROWN, z)


def _cabin(z):
    # Greenhouse runs from just behind the front axle to the rear door.
    front = _smooth((1.46 - z) / 0.14)
    rear = _smooth((z + 1.22) / 0.12)
    return front * rear


def _plan(z):
    base = _lerp(PLAN, z)
    flare = 0.0
    for axle in (Z_REAR, Z_FRONT):
        dz = abs(z - axle)
        if dz < ARCH_R:
            flare = max(flare, 0.012 * math.cos((dz / ARCH_R) * math.pi * 0.5))
    # The cage is a little wider than 1.84 m. Level-2 subdivision pulls it in.
    return min(HALF_W + 0.035, base + flare)


def _arch_top(z):
    best = None
    for axle in (Z_REAR, Z_FRONT):
        dz = abs(z - axle)
        if dz >= ARCH_R:
            continue
        y = AXLE_Y + math.sqrt(max(0.0, ARCH_R * ARCH_R - dz * dz))
        best = y if best is None else max(best, y)
    return best


def _fascia_z(x, y):
    """Nose surface. Corners and the hood lip sit behind the center."""
    z = Z_NOSE
    ax = abs(x)
    if ax > 0.22:
        t = min(1.0, (ax - 0.22) / 0.62)
        z -= 0.12 * (1.0 - math.cos(t * math.pi * 0.5))
    if y > 0.62:
        t = min(1.0, (y - 0.62) / 0.16)
        z -= 0.035 * (1.0 - math.cos(t * math.pi * 0.5))
    if y < 0.28:
        t = min(1.0, (0.28 - y) / 0.14)
        z -= 0.012 * t * t
    return z


def _tail_z(x, y):
    z = Z_TAIL
    ax = abs(x)
    if ax > 0.16:
        t = min(1.0, (ax - 0.16) / 0.70)
        z += 0.11 * (1.0 - math.cos(t * math.pi * 0.5))
    if y > 0.90:
        t = min(1.0, (y - 0.90) / 0.40)
        z += 0.05 * t
    if y < 0.32:
        t = min(1.0, (0.32 - y) / 0.16)
        z += 0.035 * t
    return z


def _stations():
    raw = [
        Z_TAIL, -2.18, -1.92, -1.68,
        Z_REAR - 0.36, Z_REAR - 0.18, Z_REAR, Z_REAR + 0.18, Z_REAR + 0.34,
        # C-pillar, rear door, B-pillar, front door, A-pillar.
        -1.10, -0.94, -0.55, -0.08,
        0.06, 0.20, 0.58, 0.96, 1.14, 1.28,
        Z_FRONT - 0.34, Z_FRONT - 0.16, Z_FRONT, Z_FRONT + 0.18, Z_FRONT + 0.36,
        2.08, 2.26, Z_NOSE,
    ]
    out = []
    for z in sorted(raw):
        if not out or abs(z - out[-1]) > 0.025:
            out.append(round(z, 5))
    return out


def _half_section(z):
    """Ten points from belly center to roof center, on the +X side."""
    hw = _plan(z)
    crown = _crown(z)
    cabin = _cabin(z)
    rocker = 0.30
    arch = _arch_top(z)
    skirt = rocker + 0.02
    if arch is not None:
        skirt = max(skirt, arch)
    skirt = min(skirt, crown - 0.18)
    belt = _lerp(BELT, max(-1.15, min(1.30, z)))
    belt = min(max(belt, skirt + 0.06), crown - 0.06)

    def mix(hood, glass):
        return hood * (1.0 - cabin) + glass * cabin

    ys = [
        BELLY,
        BELLY + 0.018,
        rocker - 0.04,
        rocker + 0.02,
        max(rocker + 0.06, skirt - 0.05),
        skirt,
        mix(min(crown - 0.12, skirt + 0.12), max(skirt + 0.06, belt - 0.04)),
        mix(crown - 0.055, belt + 0.015),
        mix(crown - 0.018, crown - 0.07),
        crown,
    ]
    xs = [
        0.0,
        min(0.40, hw * 0.46),
        min(0.58, hw * 0.60),
        min(0.72, hw * 0.76),
        hw * 0.93,
        hw,
        hw * mix(0.985, 0.97),
        hw * mix(0.94, 0.90),
        hw * mix(0.62, 0.72),
        0.0,
    ]
    gap = 0.012
    for i in range(1, len(ys)):
        ys[i] = max(ys[i], ys[i - 1] + gap)
    if ys[-1] > crown:
        span = crown - ys[0]
        for i in range(1, len(ys)):
            ys[i] = ys[0] + span * i / (len(ys) - 1)
    return list(zip(xs, ys))


def _crease_layer(bm):
    layer = bm.edges.layers.float.get("crease_edge")
    if layer is None:
        layer = bm.edges.layers.float.new("crease_edge")
    return layer


def _mark(bm, layer, a, b, weight):
    edge = bm.edges.get((a, b))
    if edge is not None:
        edge[layer] = max(edge[layer], weight)


def _orient_outward(bm):
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.normal_update()
    bvh = BVHTree.FromBMesh(bm)
    origin = Vector(unity_to_blender(0.0, 0.70, 0.0))
    direction = Vector((1.0, 0.17, 0.09)).normalized()
    hits = 0
    for _ in range(24):
        loc, _normal, _index, _dist = bvh.ray_cast(origin, direction)
        if loc is None:
            break
        hits += 1
        origin = loc + direction * 0.0008
    if hits % 2 == 0:
        bmesh.ops.reverse_faces(bm, faces=list(bm.faces))
        bm.normal_update()
    return hits


def _cap_strip(bm, ring):
    """Quad grid from the loop to a center column on the end profile.

    A straight bridge between the corners sinks the middle of the nose.
    """
    n = len(ring)
    half = n // 2
    right = ring[1:half]
    left = [ring[n - i] for i in range(1, half)]
    centers = []
    for rv, lv in zip(right, left):
        _rx, ry, rz = blender_to_unity(rv.co.x, rv.co.y, rv.co.z)
        _lx, ly, lz = blender_to_unity(lv.co.x, lv.co.y, lv.co.z)
        y = (ry + ly) * 0.5
        side_z = (rz + lz) * 0.5
        if side_z >= 0.0:
            z = max(_fascia_z(0.0, y), side_z + 0.012)
        else:
            z = min(_tail_z(0.0, y), side_z - 0.012)
        centers.append(bm.verts.new(unity_to_blender(0.0, y, z)))
    bm.faces.new((ring[0], right[0], centers[0], left[0]))
    for i in range(len(right) - 1):
        bm.faces.new((right[i], right[i + 1], centers[i + 1], centers[i]))
        bm.faces.new((centers[i], centers[i + 1], left[i + 1], left[i]))
    bm.faces.new((right[-1], ring[half], left[-1], centers[-1]))


def _cap(bm, ring):
    # A ruled strip across the loop. Grid-fill on the curved nose pinches.
    _cap_strip(bm, ring)
    return "strip"


def _clamp_ends(rings):
    """Pull the nose and tail rings onto the profile without crossing the neighbour."""
    last = len(rings) - 1
    for index, (x, y, _z) in enumerate(rings[last]):
        prev = rings[last - 1][index][2]
        z = max(_fascia_z(x, y), prev + 0.03)
        rings[last][index] = (x, y, z)
    for index, (x, y, z) in enumerate(rings[last - 1]):
        target = _fascia_z(x, y)
        bent = z * 0.62 + target * 0.38
        bent = min(bent, rings[last][index][2] - 0.025)
        bent = max(bent, rings[last - 2][index][2] + 0.025)
        rings[last - 1][index] = (x, y, bent)
    for index, (x, y, _z) in enumerate(rings[0]):
        nxt = rings[1][index][2]
        z = min(_tail_z(x, y), nxt - 0.03)
        rings[0][index] = (x, y, z)
    for index, (x, y, z) in enumerate(rings[1]):
        target = _tail_z(x, y)
        bent = z * 0.62 + target * 0.38
        bent = max(bent, rings[0][index][2] + 0.025)
        bent = min(bent, rings[2][index][2] - 0.025)
        rings[1][index] = (x, y, bent)


def _build_cage():
    zs = _stations()
    rings = []
    for z in zs:
        half = _half_section(z)
        ring = [(x, y, z) for x, y in half]
        for x, y in reversed(half[1:-1]):
            ring.append((-x, y, z))
        rings.append(ring)
    _clamp_ends(rings)

    bm = bmesh.new()
    layer = _crease_layer(bm)
    built = []
    for ring in rings:
        built.append([bm.verts.new(unity_to_blender(x, y, z)) for x, y, z in ring])
    bm.verts.index_update()
    n = len(built[0])
    half = n // 2
    for s in range(len(built) - 1):
        a = built[s]
        b = built[s + 1]
        zmid = (zs[s] + zs[min(s + 1, len(zs) - 1)]) * 0.5
        arching = _arch_top(zmid) is not None
        for i in range(n):
            j = (i + 1) % n
            try:
                bm.faces.new((a[i], a[j], b[j], b[i]))
            except ValueError:
                continue
            if i in (4, 5) or j in (4, 5):
                _mark(bm, layer, a[i], a[j], 0.35)
            if arching and (i in (4, 5) or j in (4, 5)):
                _mark(bm, layer, a[i], b[i], 0.55)
        if abs(zmid - Z_COWL) < 0.16 or abs(zmid + 1.42) < 0.16:
            for i in range(3, half - 1):
                _mark(bm, layer, a[i], a[i + 1], 0.45)
                _mark(bm, layer, a[n - i], a[n - i - 1], 0.45)
    nose = _cap(bm, built[-1])
    tail = _cap(bm, built[0])
    print("CAGE_CAP", "nose", nose, "tail", tail, "stations", len(zs), "ring", n)

    for face in bm.faces:
        xs, ys, zs_f = [], [], []
        for vert in face.verts:
            x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
            xs.append(x)
            ys.append(y)
            zs_f.append(z)
        cx = sum(xs) / len(xs)
        cy = sum(ys) / len(ys)
        cz = sum(zs_f) / len(zs_f)
        ax = abs(cx)
        face.material_index = _region(ax, cy, cz)
        face.smooth = True

    for edge in bm.edges:
        faces = edge.link_faces
        if len(faces) == 2 and faces[0].material_index != faces[1].material_index:
            edge[layer] = max(edge[layer], 0.82)
        if len(faces) != 2:
            continue
        lip = 0
        for vert in edge.verts:
            x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
            if abs(x) < HALF_W * 0.82:
                continue
            for axle in (Z_REAR, Z_FRONT):
                if y > AXLE_Y and abs(math.hypot(y - AXLE_Y, z - axle) - ARCH_R) < 0.04:
                    lip += 1
        if lip == 2:
            edge[layer] = max(edge[layer], 0.72)

    parity = _orient_outward(bm)
    print("CAGE_PARITY", parity, "faces", len(bm.faces))
    return bm


def _region(ax, cy, cz):
    """Glass only inside a window. The roof and the three pillars stay paint.

    Side glass starts outboard of the roof crown. A face whose centroid sits
    on the tumblehome used to swallow the whole roof after subdivision.
    """
    if cy > 1.01 and ax > 0.56:
        if 0.22 < cz < 1.12:
            return MAT_GLASS
        if -0.92 < cz < 0.04:
            return MAT_GLASS
    if 1.02 < cz < 1.38 and cy > 1.06 and ax < 0.78:
        return MAT_GLASS
    if -1.46 < cz < -1.02 and cy > 1.08 and ax < 0.68:
        return MAT_GLASS
    if cy < 0.28 and 0.25 < ax < 0.75 and abs(cz) < 1.55:
        return MAT_BLACK
    return MAT_PAINT


def _subdivide(bm, level):
    if level <= 0:
        return bm
    me = bpy.data.meshes.new("sedan_cage")
    bm.to_mesh(me)
    obj = bpy.data.objects.new("sedan_cage", me)
    bpy.context.scene.collection.objects.link(obj)
    mod = obj.modifiers.new("Subsurf", "SUBSURF")
    mod.levels = level
    mod.render_levels = level
    if hasattr(mod, "use_creases"):
        mod.use_creases = True
    shell._apply_mod(obj, "Subsurf")
    out = bmesh.new()
    out.from_mesh(obj.data)
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(me)
    bm.free()
    out.faces.ensure_lookup_table()
    for face in out.faces:
        face.smooth = True
    return out


def _move(vert, x, y, z):
    vert.co = Vector(unity_to_blender(x, y, z))


def _bisect_plane(bm, co, normal):
    geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
    bmesh.ops.bisect_plane(
        bm,
        geom=geom,
        plane_co=Vector(co),
        plane_no=Vector(normal),
        dist=0.0001,
    )
    bm.verts.ensure_lookup_table()
    bm.edges.ensure_lookup_table()
    bm.faces.ensure_lookup_table()


def _grooves(bm):
    """Shallow 3 mm panel gaps. Support loops sit 8 mm off the line so the
    clearcoat catches the groove. Handles are a flush recess, not a strap.
    """
    z_axis = unity_to_blender(0.0, 0.0, 1.0)
    y_axis = unity_to_blender(0.0, 1.0, 0.0)
    for shut_z, _y0, _y1 in ((1.21, 0.38, 1.05), (0.13, 0.36, 1.32), (-1.02, 0.38, 1.08)):
        for offset in (-0.008, 0.0, 0.008):
            _bisect_plane(bm, unity_to_blender(0.0, 0.0, shut_z + offset), z_axis)
    for offset in (-0.008, 0.0, 0.008):
        _bisect_plane(bm, unity_to_blender(0.0, 0.40 + offset, 0.0), y_axis)
    # Bisects next to an existing loop leave slivers. Weld those before the
    # groove pull, and leave the 8 mm support loops in place.
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=0.002)
    bm.verts.ensure_lookup_table()
    bm.edges.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    shuts = (
        (1.21, 0.38, 1.05),
        (0.13, 0.36, 1.32),
        (-1.02, 0.38, 1.08),
    )
    picked = {}
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        if abs(x) < 0.28:
            continue
        for shut_z, y0, y1 in shuts:
            if y < y0 or y > y1:
                continue
            dz = abs(z - shut_z)
            if dz > 0.045:
                continue
            key = (1 if x > 0.0 else -1, round(y * 50.0), round(x * 30.0), round(shut_z, 2))
            prev = picked.get(key)
            if prev is None or dz < prev[0]:
                picked[key] = (dz, vert)
    moved = 0
    for dz, vert in picked.values():
        if dz > 0.040:
            continue
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        _move(vert, x - math.copysign(0.003, x), y, z)
        moved += 1
    sill = {}
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        if abs(x) < 0.55 or not (-1.02 < z < 1.30):
            continue
        dy = abs(y - 0.40)
        if dy > 0.040:
            continue
        key = (1 if x > 0.0 else -1, round(z * 50.0), round(x * 30.0))
        prev = sill.get(key)
        if prev is None or dy < prev[0]:
            sill[key] = (dy, vert)
    for dy, vert in sill.values():
        if dy > 0.035:
            continue
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        _move(vert, x - math.copysign(0.003, x), y, z)
        moved += 1
    for hz, hy in ((0.72, 0.78), (-0.42, 0.78)):
        for vert in bm.verts:
            x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
            if abs(x) < 0.55:
                continue
            d = math.hypot((z - hz) / 0.040, (y - hy) / 0.016)
            if d >= 1.0:
                continue
            depth = 0.003 * (1.0 - d) ** 2
            _move(vert, x - math.copysign(depth, x), y, z)
            moved += 1
    # Fuel door, one side, so the profile has a panel break.
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        if x < 0.55:
            continue
        d = math.hypot((z + 1.22) / 0.055, (y - 0.84) / 0.040)
        if d >= 1.0:
            continue
        depth = 0.0025 * (1.0 - d) ** 2
        _move(vert, x - depth, y, z)
        moved += 1
    bm.normal_update()
    print("GROOVE_VERTS", moved)


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
    level = (2, 1, 0)[lod]
    bm = _build_cage()
    bm = _subdivide(bm, level)
    _grooves(bm)
    open_edges, flipped, _samples = _topology(bm)
    print("SHELL_LOD", lod, "level", level, "faces", len(bm.faces), "open", open_edges, "flips", flipped)
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
    segs = 22 if lod == 0 else (12 if lod == 1 else 8)
    hw = TIRE_W * 0.47
    bead = RIM_R + 0.010
    crown = TIRE_R
    profile = [
        (bead, -0.94),
        (bead + 0.040, -0.80),
        (crown - 0.045, -0.64),
        (crown - 0.012, -0.50),
        (crown, -0.40),
        (crown - 0.011, -0.30),
        (crown, -0.20),
        (crown - 0.009, -0.10),
        (crown, 0.0),
        (crown - 0.009, 0.10),
        (crown, 0.20),
        (crown - 0.011, 0.30),
        (crown, 0.40),
        (crown - 0.012, 0.50),
        (crown - 0.045, 0.64),
        (bead + 0.040, 0.80),
        (bead, 0.94),
    ]
    bm = bmesh.new()
    rings = []
    for i in range(segs):
        theta = 2.0 * math.pi * i / segs
        ct, st = math.cos(theta), math.sin(theta)
        ring = []
        for rad, axial in profile:
            ring.append(bm.verts.new(unity_to_blender(x + axial * hw, AXLE_Y + rad * ct, z + rad * st)))
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
    open_edges = sum(1 for edge in bm.edges if len(edge.link_faces) != 2)
    if open_edges:
        print("TIRE_OPEN", open_edges)
    g._ingest(bm, "Lib_Rubber", 1.0)


def _wheel(g, x, z, year, lod):
    sign = 1.0 if x > 0.0 else -1.0
    info = YEARS[year]
    segs = 20 if lod == 0 else (12 if lod == 1 else 8)
    _spin_tire(g, x, z, lod)
    bead = TIRE_W * 0.47 * 0.94
    rim_x = x + sign * (bead + 0.012)
    g.cylinder((rim_x - sign * 0.014, AXLE_Y, z), RIM_R * 0.90, 0.010, BLACK, segs, axis="X")
    g.cylinder((rim_x - sign * 0.002, AXLE_Y, z), RIM_R * 1.01, 0.016, "Lib_Steel", segs, axis="X")
    face = rim_x + sign * 0.016
    spokes = info["spokes"]
    if lod == 1:
        spokes = max(5, spokes // 2)
    if lod == 2:
        spokes = 5
    thick = max(info["thick"], 0.014) if lod == 0 else max(info["thick"], 0.018)
    for k in range(spokes):
        theta = math.radians(k * (360.0 / spokes) + 8.0)
        arm = RIM_R * 0.58
        g.box(
            (face, AXLE_Y + math.cos(theta) * arm, z + math.sin(theta) * arm),
            (0.014, RIM_R * 0.52, thick),
            "Lib_Steel",
            euler=(math.degrees(theta), 0.0, 0.0),
        )
    g.cylinder((face - sign * 0.010, AXLE_Y, z), RIM_R * 0.22, 0.012, "Lib_SteelDark", max(8, segs // 2), axis="X")
    g.cylinder((x, AXLE_Y, z), 0.062, 0.030, "Lib_SteelDark", 10, axis="X")


def _u_normal(normal):
    nx, ny, nz = blender_to_unity(normal.x, normal.y, normal.z)
    mag = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    return (nx / mag, ny / mag, nz / mag)


def _cast(bvh, x, y, z_sign):
    start = Vector(unity_to_blender(x * 1.30, y, z_sign * 3.20))
    end = Vector(unity_to_blender(x * 0.08, y * 0.72 + 0.12, z_sign * 0.20))
    direction = end - start
    length = direction.length
    if length < 1e-5:
        return None
    hit, normal, _index, _dist = bvh.ray_cast(start, direction / length, length)
    if hit is None:
        return None
    point = blender_to_unity(hit.x, hit.y, hit.z)
    n = _u_normal(normal)
    if abs(n[0]) + abs(n[1]) + abs(n[2]) < 0.5:
        n = (0.0, 0.0, float(z_sign))
    outward = (start.x - hit.x, start.y - hit.y, start.z - hit.z)
    ou = blender_to_unity(*outward)
    if n[0] * ou[0] + n[1] * ou[1] + n[2] * ou[2] < 0.0:
        n = (-n[0], -n[1], -n[2])
    return point, n


def _cast_side(bvh, sign, y, z):
    start = Vector(unity_to_blender(sign * 1.70, y, z))
    end = Vector(unity_to_blender(sign * 0.10, y, z))
    direction = end - start
    length = direction.length
    hit, normal, _index, _dist = bvh.ray_cast(start, direction / length, length)
    if hit is None:
        return None
    point = blender_to_unity(hit.x, hit.y, hit.z)
    n = _u_normal(normal)
    if abs(n[0]) + abs(n[1]) + abs(n[2]) < 0.5:
        n = (float(sign), 0.0, 0.0)
    if n[0] * sign < 0.0:
        n = (-n[0], -n[1], -n[2])
    return point, n


def _ribbon(g, row_a, row_b, gap, depth, mat):
    if len(row_a) < 2 or len(row_a) != len(row_b):
        return
    bm = bmesh.new()

    def lay(row, extra):
        verts = []
        for point, normal in row:
            q = (
                point[0] + normal[0] * extra,
                point[1] + normal[1] * extra,
                point[2] + normal[2] * extra,
            )
            verts.append(bm.verts.new(unity_to_blender(*q)))
        return verts

    back_a = lay(row_a, gap)
    back_b = lay(row_b, gap)
    front_a = lay(row_a, gap + depth)
    front_b = lay(row_b, gap + depth)
    count = len(back_a)
    for i in range(count - 1):
        bm.faces.new((back_a[i], back_b[i], back_b[i + 1], back_a[i + 1]))
        bm.faces.new((front_a[i], front_a[i + 1], front_b[i + 1], front_b[i]))
        bm.faces.new((back_a[i], back_a[i + 1], front_a[i + 1], front_a[i]))
        bm.faces.new((back_b[i], front_b[i], front_b[i + 1], back_b[i + 1]))
    bm.faces.new((back_a[0], front_a[0], front_b[0], back_b[0]))
    bm.faces.new((back_a[-1], back_b[-1], front_b[-1], front_a[-1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    open_edges = sum(1 for edge in bm.edges if len(edge.link_faces) != 2)
    if open_edges:
        print("RIBBON_OPEN", mat, open_edges, "pts", count)
    g._ingest(bm, mat, 1.0)


def _sample_row(bvh, x0, x1, y_of, z_sign, steps):
    """Place hardware on the analytic end profile, extruded straight out.

    Ray hits fold the strip wherever the nose normal is degenerate.
    """
    del bvh
    row = []
    steps = max(1, steps)
    z_of = _fascia_z if z_sign > 0.0 else _tail_z
    normal = (0.0, 0.0, float(z_sign))
    for i in range(steps + 1):
        x = x0 + (x1 - x0) * i / steps
        y = y_of(x) if callable(y_of) else y_of
        row.append(((x, y, z_of(x, y)), normal))
    return row, 0


def _trapezoid(g, bvh, y0, y1, x_bot, x_top, z_sign, mat, steps):
    def y_low(_x):
        return y0

    def y_high(x):
        return y1

    # Width tapers with height, so the row x limits change per edge.
    low, miss_a = _sample_row(bvh, -x_bot, x_bot, y_low, z_sign, steps)
    high, miss_b = _sample_row(bvh, -x_top, x_top, y_high, z_sign, steps)
    _ribbon(g, low, high, GAP, 0.010, mat)
    return miss_a + miss_b


def _bars(g, bvh, y0, y1, x_bot, x_top, count, bar_h, z_sign, steps):
    if count <= 0:
        return 0
    misses = 0
    span = (y1 - bar_h) - (y0 + bar_h)
    for i in range(count):
        t = (i + 0.5) / count
        y = (y0 + bar_h) + span * t
        half = x_bot + (x_top - x_bot) * ((y - y0) / (y1 - y0 or 1.0))
        half = max(0.08, half - 0.015)

        def y_at(_x, _y=y):
            return _y

        row, miss = _sample_row(bvh, -half, half, y_at, z_sign, steps)
        misses += miss
        low = [((p[0], p[1] - bar_h * 0.5, p[2]), n) for p, n in row]
        high = [((p[0], p[1] + bar_h * 0.5, p[2]), n) for p, n in row]
        _ribbon(g, low, high, GAP + 0.012, 0.004, "Lib_Steel")
    return misses


def _lamp_row(g, bvh, x0, x1, y_of, half_h, z_sign, mat, steps):
    def y_low(x):
        return y_of(x) - half_h

    def y_high(x):
        return y_of(x) + half_h

    low, miss_a = _sample_row(bvh, x0, x1, y_low, z_sign, steps)
    high, miss_b = _sample_row(bvh, x0, x1, y_high, z_sign, steps)
    _ribbon(g, low, high, GAP, 0.008, mat)
    return miss_a + miss_b


def _sweep(x, base, reach):
    t = min(1.0, abs(x) / max(reach, 0.2))
    return base + 0.10 * t * t


def _grille(g, bvh, year, lod):
    info = YEARS[year]
    steps = 8 if lod == 0 else 4
    misses = _trapezoid(g, bvh, 0.34, 0.50, 0.62, 0.46, 1.0, BLACK, steps)
    misses += _bars(g, bvh, 0.34, 0.50, 0.62, 0.46, info["bars"], info["bar_h"], 1.0, steps)
    if info["intake"]:
        # Sit the intake on the upright face, above the belly tuck.
        misses += _trapezoid(g, bvh, 0.20, 0.31, 0.70, 0.54, 1.0, BLACK, steps)
        misses += _bars(g, bvh, 0.20, 0.31, 0.70, 0.54, 2, 0.012, 1.0, max(3, steps // 2))
    return misses


def _lamps_front(g, bvh, year, lod):
    info = YEARS[year]
    kind = info["lamps"]
    steps = 10 if lod == 0 else 4
    half = info["lamp_h"] * (0.55 if kind == "thin" else 1.0)
    misses = 0
    if kind == "bar":
        misses += _lamp_row(
            g, bvh, -0.92, 0.92,
            lambda x: _sweep(x, 0.575, 0.92),
            half, 1.0, "Lib_Headlamp", steps,
        )
    elif kind == "tier":
        for sign in (1.0, -1.0):
            misses += _lamp_row(
                g, bvh, sign * 0.30, sign * 0.78,
                lambda x: _sweep(x, 0.60, 0.78),
                half * 0.55, 1.0, "Lib_Headlamp", steps,
            )
            misses += _lamp_row(
                g, bvh, sign * 0.36, sign * 0.70,
                lambda x: _sweep(x, 0.58, 0.70),
                half * 0.45, 1.0, "Lib_Headlamp", steps,
            )
    else:
        reach = 0.74 if kind == "separate" else 0.70
        for sign in (1.0, -1.0):
            misses += _lamp_row(
                g, bvh, sign * 0.32, sign * reach,
                lambda x, _reach=reach: _sweep(x, 0.58, _reach),
                half, 1.0, "Lib_Headlamp", steps,
            )
    if info["fog"]:
        for sign in (1.0, -1.0):
            misses += _lamp_row(
                g, bvh, sign * 0.42, sign * 0.68,
                lambda x: 0.24 + 0.02 * abs(x),
                0.012, 1.0, info["fog"], 4,
            )
    return misses


def _lamps_rear(g, bvh, year, lod):
    info = YEARS[year]
    kind = info["tails"]
    steps = 8 if lod == 0 else 3
    half = 0.014 if kind != "thin" else 0.008
    if kind == "bar":
        return _lamp_row(
            g, bvh, -0.78, 0.78,
            lambda x: 0.86 + 0.04 * (abs(x) / 0.78) ** 2,
            half, -1.0, "Lib_Taillamp", steps,
        )
    misses = 0
    reach = 0.62 if kind != "thin" else 0.70
    for sign in (1.0, -1.0):
        misses += _lamp_row(
            g, bvh, sign * 0.28, sign * reach,
            lambda x: 0.84,
            half, -1.0, "Lib_Taillamp", steps,
        )
    return misses


def _teardrop(g, center, mat, lod):
    segs = 12 if lod == 0 else 8
    steps = (
        (-0.11, 0.014),
        (-0.06, 0.040),
        (-0.01, 0.052),
        (0.045, 0.048),
        (0.085, 0.030),
        (0.115, 0.012),
        (0.132, 0.0),
    )
    bm = bmesh.new()
    rings = []
    cx, cy, cz = center
    for zoff, rad in steps:
        ring = []
        if rad < 1e-4:
            ring.append(bm.verts.new(unity_to_blender(cx, cy, cz + zoff)))
        else:
            for k in range(segs):
                ang = 2.0 * math.pi * k / segs
                ring.append(bm.verts.new(unity_to_blender(
                    cx + math.cos(ang) * rad * 0.70,
                    cy + math.sin(ang) * rad,
                    cz + zoff,
                )))
        rings.append(ring)
    for i in range(len(rings) - 1):
        a = rings[i]
        b = rings[i + 1]
        if len(a) == 1 or len(b) == 1:
            pole = a[0] if len(a) == 1 else b[0]
            other = b if len(a) == 1 else a
            order = other if len(a) == 1 else list(reversed(other))
            for k in range(len(other)):
                try:
                    bm.faces.new((pole, order[k], order[(k + 1) % len(other)]))
                except ValueError:
                    continue
            continue
        for k in range(segs):
            nk = (k + 1) % segs
            try:
                bm.faces.new((a[k], a[nk], b[nk], b[k]))
            except ValueError:
                continue
    try:
        bm.faces.new(list(reversed(rings[0])))
    except ValueError:
        pass
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    g._ingest(bm, mat, 1.0)


def _mirrors(g, bvh, year, lod):
    mat = YEARS[year]["mirror"]
    z = 1.08
    y = 1.00
    for sign in (1.0, -1.0):
        hit = _cast_side(bvh, sign, y, z)
        if hit is None:
            skin = sign * (_plan(z) + 0.01)
            normal = (sign, 0.0, 0.0)
            point = (skin, y, z)
        else:
            point, normal = hit
        head = (
            point[0] + normal[0] * 0.17,
            point[1] + normal[1] * 0.01,
            point[2] + 0.015,
        )
        x0 = point[0] + math.copysign(GAP, sign)
        x1 = head[0] - math.copysign(0.040, sign)
        g.cylinder(((x0 + x1) * 0.5, point[1], point[2]), 0.012, max(0.02, abs(x1 - x0)), mat, 8, axis="X")
        _teardrop(g, head, mat, lod)


def _keep_apart(bm, radius=0.0011):
    """Stop remove_doubles from welding separate shells into open edges."""
    from mathutils.kdtree import KDTree

    bm.verts.ensure_lookup_table()
    parent = list(range(len(bm.verts)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    for face in bm.faces:
        root = find(face.verts[0].index)
        for vert in face.verts:
            parent[find(vert.index)] = root
    tree = KDTree(len(bm.verts))
    for vert in bm.verts:
        tree.insert(vert.co, vert.index)
    tree.balance()
    moved = 0
    for vert in bm.verts:
        root = find(vert.index)
        for _co, index, dist in tree.find_range(vert.co, radius):
            if index <= vert.index or find(index) == root:
                continue
            other = bm.verts[index]
            delta = vert.co - other.co
            if delta.length < 1e-8:
                delta = Vector((1.0, 0.0, 0.0))
            else:
                delta.normalize()
            vert.co += delta * (radius - dist + 0.0004)
            moved += 1
            break
    if moved:
        print("KEEP_APART", moved)
    bm.normal_update()


def dress(g, year, lod, paint):
    del paint
    bvh = BVHTree.FromBMesh(g.bm)
    misses = 0
    misses += _grille(g, bvh, year, lod)
    misses += _lamps_front(g, bvh, year, lod)
    misses += _lamps_rear(g, bvh, year, lod)
    if lod < 2:
        _mirrors(g, bvh, year, lod)
    if misses:
        print("FASCIA_CAST_MISS", year, misses)
    for axle in (Z_REAR, Z_FRONT):
        for sign in (-1.0, 1.0):
            _wheel(g, sign * TIRE_X, axle, year, lod)
    _keep_apart(g.bm)


def add_colliders(asset):
    for i, axle in enumerate((Z_REAR, Z_FRONT)):
        for j, sign in enumerate((-1.0, 1.0)):
            # Inside the tire rubber. The hub sits in the capped wheel, so a
            # box on the axle is inside two shells and the parity test misses it.
            asset.box(
                "Col_Wheel_%d%d" % (i, j),
                (sign * TIRE_X, AXLE_Y - 0.28, axle),
                (0.036, 0.028, 0.028),
            )
    asset.box("Col_Cabin", (0.0, 0.48, -0.05), (0.55, 0.22, 0.85))
    asset.box("Col_Roof", (0.0, 1.28, 0.00), (0.36, 0.06, 0.55))
    asset.box("Col_Hood", (0.0, 0.78, 1.62), (0.46, 0.06, 0.28))
    asset.box("Col_Deck", (0.0, 0.88, -1.40), (0.50, 0.06, 0.22))
    asset.box("Col_Nose", (0.0, 0.48, 1.95), (0.36, 0.12, 0.16))


def probe_spec(name):
    y0 = _crown(Z_HEADER)
    y1 = _crown(Z_COWL)
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
    the opposite orientation. Those are the failures. A clean 90-degree
    corner has a neighbour dot near 0 and is left alone.
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
                c = blender_to_unity(*edge.verts[0].co)
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
        # Anti-parallel and sharing an edge: the panel reversed, even when
        # the two centers sit side by side so neither aims at the other.
        reversed_panel = dot < -0.92
        if not points_at_neighbour and not reversed_panel:
            continue
        flips += 1
        if len(samples) < 8:
            mid = (edge.verts[0].co + edge.verts[1].co) * 0.5
            c = blender_to_unity(mid.x, mid.y, mid.z)
            samples.append(("flip", tuple(round(v, 3) for v in c), round(dot, 3)))
    return nonmanifold, flips, samples


def _check_render(asset):
    """Front and hero, the two views that hid the crumpled nose."""
    if os.environ.get("SEDAN_SKIP_CHECK_RENDER") == "1":
        print("SHELL_CHECK render skipped")
        return
    import render_pass2 as stills

    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    out = os.path.join(REPO, "Docs", "AssetStills", "vehicles", "sedan_mid_a", "pass10")
    os.makedirs(out, exist_ok=True)
    year = asset.name[-2:]
    scene = bpy.context.scene
    stills._engine(scene, wide=False)
    scene.cycles.samples = 12
    stills._ensure_materials()
    for label, azimuth, elevation in (("front", 4.0, 3.0), ("hero", 48.0, 11.0)):
        stills._world(scene, night=False)
        obj = stills._spawn(asset, (0.0, 0.0, 0.0))
        stills._ground("asphalt", 40.0)
        stills._frame(scene, [obj], fill=0.90, elevation=elevation, azimuth=azimuth)
        path = os.path.join(out, "check_%s_%s.png" % (label, year))
        stills._render(scene, path)
        bpy.data.objects.remove(obj, do_unlink=True)


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
    _check_render(asset)
    if problems:
        raise RuntimeError("shell check failed: " + "; ".join(problems))
    print("SHELL_CHECK ok")


def measure(asset):
    self_check(asset)
    best = {}
    max_x = 0.0
    body_x = 0.0
    max_y = 0.0
    min_y = 99.0
    max_z = -99.0
    min_z = 99.0
    for vert in asset.lods[0].bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        max_x = max(max_x, abs(x))
        # Mirrors sit near z=1.08 and past the door skin. Keep them out of body width.
        if 0.40 < y < 1.20 and abs(z) < 0.85 and abs(x) < 0.97:
            body_x = max(body_x, abs(x))
        max_y = max(max_y, y)
        min_y = min(min_y, y)
        max_z = max(max_z, z)
        min_z = min(min_z, z)
        if abs(x) > 0.12:
            continue
        bucket = round(z * 10.0) / 10.0
        prev = best.get(bucket)
        if prev is None or y > prev[1]:
            best[bucket] = (z, y)

    def near(z_target):
        pick = None
        for _bucket, (z, y) in best.items():
            if abs(z - z_target) < 0.08 and (pick is None or abs(z - z_target) < abs(pick[0] - z_target)):
                pick = (z, y)
        return pick

    header = near(Z_HEADER)
    cowl = near(Z_COWL)
    angle = None
    if header and cowl and abs(cowl[0] - header[0]) > 0.05:
        angle = math.degrees(math.atan2(header[1] - cowl[1], cowl[0] - header[0]))
    tris = [asset.lods[lod].tri_count() for lod in (0, 1, 2) if lod in asset.lods]
    print(
        "BODY_A_MEASURE", asset.name,
        "rake", None if angle is None else round(angle, 1),
        "header", None if header is None else tuple(round(v, 3) for v in header),
        "cowl", None if cowl is None else tuple(round(v, 3) for v in cowl),
        "size", round(max_x * 2.0, 3), round(max_y - min_y, 3), round(max_z - min_z, 3),
        "body_w", round(body_x * 2.0, 3),
        "ymax", round(max_y, 3),
        "tris", tris,
    )
