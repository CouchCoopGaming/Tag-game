"""Midsize sedan, hard panel body.

The shell is a closed panel cage. Spans are straight between the rocker,
shoulder, belt, glass, and crown. There is no subdivision pass. The body
stays inside a 1.76 m shoulder so the door mirrors finish under the 1.90 m
cap. The hood leading edge sits near 0.75 m and rises to about 1.00 m at
the cowl. The deck is a short fastback above the hood, and the sail behind
the rear door is dark glass.

Proportions follow a published midsize exterior table, in metres: length
4.90, height 1.44, wheelbase 2.82, track 1.60, ground clearance 0.14.
No badges. Daylight lamps are clear lenses over a dark housing, emission 0.
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
WIDTH = 1.76
HALF_W = WIDTH * 0.5
HEIGHT = 1.44
WHEELBASE = 2.82
FRONT_OVERHANG = 0.96
Z_FRONT = HALF_L - FRONT_OVERHANG
Z_REAR = Z_FRONT - WHEELBASE
BELLY = 0.14
Z_NOSE = HALF_L
Z_TAIL = -HALF_L
Z_HEADER = 0.45
Z_COWL = 1.05
ROOF_HALF = 0.58
# Hard panels. Right half, then the mirror. 3+3+1+2+2 segments → 20 verts.
RING_FLARE = 3
RING_SIDE = 3
RING_BELT = 1
RING_GLASS = 2
RING_CROWN = 2
I_BELLY = 0
I_ROCKER = RING_FLARE
I_SHOULDER = RING_FLARE + RING_SIDE
I_BELT = I_SHOULDER + RING_BELT
I_RAIL = I_BELT + RING_GLASS
I_CROWN = I_RAIL + RING_CROWN
NOSE_PULL = 0.29
TAIL_PULL = 0.16
FASCIA_RAKE = math.radians(19.0)
TAIL_RAKE = math.radians(8.0)

TIRE_W = 0.225
RIM_R = 18.0 * INCH * 0.5
TIRE_R = 0.34
AXLE_Y = TIRE_R
ARCH_GAP = 0.035
ARCH_R = TIRE_R + ARCH_GAP
ARCH_SPAN = 0.34
TIRE_X = 0.80

PAINT = "Lib_PaintCrimson"
GLASS = "Lib_AutoGlass"
BLACK = "Lib_Black"
LENS = "Lib_Glass"
HOUSING = "Lib_SteelDark"
TAIL_LENS = "Lib_BoxRed"
MATS = (PAINT, GLASS, BLACK)
MAT_PAINT = 0
MAT_GLASS = 1
MAT_BLACK = 2

YEARS = {
    2022: {"spokes": 6, "lamps": "separate", "tails": "separate", "intake": False, "mirror": PAINT, "bars": 5, "bar_h": 0.012},
    2023: {"spokes": 5, "lamps": "tier", "tails": "separate", "intake": False, "mirror": PAINT, "bars": 5, "bar_h": 0.012},
    2024: {"spokes": 6, "lamps": "thin", "tails": "thin", "intake": False, "mirror": BLACK, "bars": 6, "bar_h": 0.008},
    2025: {"spokes": 5, "lamps": "swept", "tails": "wrap", "intake": True, "mirror": PAINT, "bars": 4, "bar_h": 0.014},
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


def _smooth01(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def _top_y(z):
    """Centerline. Leading edge 0.75, cowl 1.00, roof 1.44, short deck above the hood."""
    return _lerp((
        (-2.45, 1.02),
        (-2.15, 1.05),
        (-1.70, 1.08),
        (-1.25, 1.12),
        (-0.90, 1.20),
        (-0.55, 1.34),
        (-0.20, 1.43),
        (0.16, 1.44),
        (0.45, 1.30),
        (1.05, 1.00),
        (1.35, 0.97),
        (1.75, 0.90),
        (2.10, 0.82),
        (2.45, 0.75),
    ), z)


def _plan_x(z):
    """Half-width of the shoulder. Arches are widest. Mirrors must finish under 0.95."""
    return _lerp((
        (-2.45, 0.840),
        (-1.90, 0.860),
        (-1.33, 0.880),
        (-0.40, 0.860),
        (0.70, 0.860),
        (1.49, 0.880),
        (2.00, 0.860),
        (2.45, 0.840),
    ), z)


def _shoulder_y(z):
    return _lerp((
        (-2.45, 0.58),
        (-1.70, 0.72),
        (-0.80, 0.84),
        (0.20, 0.82),
        (1.10, 0.74),
        (1.70, 0.62),
        (2.45, 0.50),
    ), z)


def _belt_y(z):
    """Drops through the quarter so the sail behind the rear door is a window."""
    return _lerp((
        (-2.20, 0.98),
        (-1.45, 0.90),
        (-0.90, 0.94),
        (-0.40, 1.00),
        (0.30, 0.98),
        (0.90, 0.97),
        (1.40, 0.92),
        (2.45, 0.72),
    ), z)


def _rail_y(z):
    return _lerp((
        (-2.00, 1.02),
        (-1.40, 1.10),
        (-0.90, 1.20),
        (-0.55, 1.30),
        (0.00, 1.36),
        (0.55, 1.26),
        (1.05, 1.02),
        (2.45, 0.74),
    ), z)


def _rail_x(z):
    """Roof rail half-width. Cabin keys near 0.58 so the subdivided roof is about 1.25 m."""
    return _lerp((
        (-2.20, 0.50),
        (-1.15, 0.54),
        (-0.20, 0.58),
        (0.45, 0.58),
        (0.95, 0.46),
        (1.50, 0.38),
        (2.45, 0.42),
    ), z)


def _arch_t(z):
    best = 0.0
    for axle in (Z_FRONT, Z_REAR):
        dz = abs(z - axle)
        if dz >= ARCH_SPAN:
            continue
        best = max(best, math.sqrt(max(0.0, 1.0 - (dz / ARCH_SPAN) ** 2)))
    return best


def _lip_y(z):
    best = None
    for axle in (Z_FRONT, Z_REAR):
        dz = abs(z - axle)
        if dz >= ARCH_R - 1.0e-6:
            continue
        y = AXLE_Y + math.sqrt(max(0.0, ARCH_R * ARCH_R - dz * dz))
        if best is None or y > best:
            best = y
    return best


def _blend_out(z, z_body, z_end):
    """0 on the cabin, 1 on the hood or the deck."""
    if z_end > z_body:
        if z <= z_body:
            return 0.0
        if z >= z_end:
            return 1.0
        t = (z - z_body) / (z_end - z_body)
    else:
        if z >= z_body:
            return 0.0
        if z <= z_end:
            return 1.0
        t = (z_body - z) / (z_body - z_end)
    return t * t * (3.0 - 2.0 * t)


def _hood_blend(z):
    """0 on the greenhouse, including the quarter, 1 on the hood and the deck."""
    return max(_blend_out(z, 1.00, 1.55), _blend_out(z, -1.48, -1.92))


def _sagitta(x, pull, at_x=0.90):
    """Circular setback. Horizontal tangent on the centerline, `pull` metres at `at_x`."""
    radius = (pull * pull + at_x * at_x) / (2.0 * pull)
    ax = min(abs(x), radius * 0.999)
    return radius - math.sqrt(max(0.0, radius * radius - ax * ax))


def _end_pull(z, x, y):
    """Plan sweep plus fascia rake. Centerline belly stays on the length tips."""
    if z >= 1.55:
        t = _smooth01((z - 1.55) / (Z_NOSE - 1.55))
        sag = _sagitta(x, NOSE_PULL)
        rake = math.tan(FASCIA_RAKE) * max(0.0, y - 0.20)
        return z - (sag + rake) * t
    if z <= -1.70:
        t = _smooth01((-1.70 - z) / (Z_NOSE - 1.70))
        sag = _sagitta(x, TAIL_PULL)
        rake = math.tan(TAIL_RAKE) * max(0.0, y - 0.28)
        return z + (sag + rake) * t
    return z


def _resample_open(pts, segments):
    """Arc-length even samples. `segments` steps, both ends kept."""
    if segments < 1 or len(pts) < 2:
        return list(pts)
    lengths = [0.0]
    for i in range(len(pts) - 1):
        lengths.append(lengths[-1] + math.hypot(pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1]))
    total = lengths[-1]
    if total < 1.0e-8:
        return [pts[0]] * (segments + 1)
    out = []
    for k in range(segments + 1):
        target = total * k / segments
        i = 0
        while i < len(pts) - 2 and lengths[i + 1] < target:
            i += 1
        span = lengths[i + 1] - lengths[i]
        t = 0.0 if span < 1.0e-9 else (target - lengths[i]) / span
        out.append((
            pts[i][0] + (pts[i + 1][0] - pts[i][0]) * t,
            pts[i][1] + (pts[i + 1][1] - pts[i][1]) * t,
        ))
    out[0] = pts[0]
    out[-1] = pts[-1]
    return out


def _features(z):
    plan = _plan_x(z)
    top = _top_y(z)
    hood = _hood_blend(z)
    sy = min(_shoulder_y(z), top - 0.08)
    by = _belt_y(z)
    ry = _rail_y(z)
    if hood > 0.55:
        by = top - 0.040
        ry = top - 0.014
        sy = min(sy, by - 0.06)
    else:
        by = min(max(by, sy + 0.05), top - 0.08)
        ry = min(max(ry, by + 0.06), top - 0.016)
    sy = min(sy, by - 0.012)
    by = min(by, ry - 0.008)
    ry = min(ry, top - 0.008)
    rx = min(_rail_x(z), plan - 0.05)
    return plan, top, sy, by, ry, rx, _arch_t(z), hood


def _poly(points, segments):
    """Straight panel span. Equal steps on each control edge. No arc-length resample."""
    if segments < 1 or len(points) < 2:
        return list(points)
    edges = len(points) - 1
    counts = [max(1, segments // edges)] * edges
    extra = segments - sum(counts)
    i = 0
    while extra > 0:
        counts[i % edges] += 1
        extra -= 1
        i += 1
    while sum(counts) > segments and any(count > 1 for count in counts):
        for j, count in enumerate(counts):
            if count > 1 and sum(counts) > segments:
                counts[j] -= 1
    out = [points[0]]
    for edge, count in enumerate(counts):
        a, b = points[edge], points[edge + 1]
        for step in range(1, count + 1):
            t = step / float(count)
            out.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t))
    return out


def _section(z):
    """Closed ring. Same count, same start, same winding, even within each span."""
    plan, top, sy, by, ry, rx, t_arch, _hood = _features(z)
    rocker_x = plan - 0.018
    if t_arch > 0.02:
        rocker_x = (plan - 0.018) * (1.0 - t_arch) + 0.66 * t_arch
    rocker = (rocker_x, min(0.24, sy - 0.08))
    shoulder = (plan, sy)
    belt_x = min(plan - 0.012, max(rx + 0.04, plan - 0.03))
    rail = (min(rx, belt_x - 0.02), ry)
    keys = [
        (0.0, BELLY),
        (rocker_x * 0.72, BELLY + 0.010),
        rocker,
        ((rocker_x + plan) * 0.5, (rocker[1] + sy) * 0.5),
        shoulder,
        (belt_x, by),
        rail,
        (rail[0] * 0.45, (ry + top) * 0.5),
        (0.0, top),
    ]
    fixed = [keys[0]]
    for x, y in keys[1:]:
        px, py = fixed[-1]
        y = max(y, py + 0.006)
        # Lower body x only increases. Upper body x only decreases.
        if len(fixed) < 5:
            x = max(x, px + 0.004)
        else:
            x = min(x, px - 0.004)
        fixed.append((max(0.0, x), y))
    fixed[-1] = (0.0, max(top, fixed[-2][1] + 0.006))
    parts = (
        _poly(fixed[0:3], RING_FLARE),
        _poly(fixed[2:5], RING_SIDE),
        _poly(fixed[4:6], RING_BELT),
        _poly(fixed[5:7], RING_GLASS),
        _poly(fixed[6:9], RING_CROWN),
    )
    half = []
    for span in parts:
        if half:
            span = span[1:]
        half.extend(span)
    ring = list(half)
    for x, y in reversed(half[1:-1]):
        ring.append((-x, y))
    # Start on the belly, wind toward +X.
    start = min(range(len(ring)), key=lambda i: abs(ring[i][0]) + abs(ring[i][1] - BELLY))
    ring = ring[start:] + ring[:start]
    if ring[1][0] < ring[0][0]:
        ring = [ring[0]] + list(reversed(ring[1:]))
    return ring


def _station_zs():
    """Measured stations. Arch samples are few so level-2 LOD0 stays under 15k."""
    zs = [
        2.450,
        2.160,
        1.800,
        1.490,
        1.310,
        1.080,
        0.990,
        0.450,
        0.160,
        -0.180,
        0.100,
        0.020,
        -0.420,
        -0.730,
        -0.810,
        -1.100,
        -1.330,
        -1.700,
        -2.160,
        -2.450,
    ]
    ordered = []
    for z in sorted(set(round(z, 3) for z in zs), reverse=True):
        if ordered and abs(ordered[-1] - z) < 0.015:
            continue
        ordered.append(z)
    return ordered


def _shut_station(z):
    for shut in (2.160, 1.080, 0.990, 0.100, 0.020, -0.730, -0.810, -1.700, -2.160):
        if abs(z - shut) < 0.012:
            return True
    return False


def _paint_at(x, y, z):
    """Glass by where the face sits. The sail behind the rear door is dark glass."""
    ax = abs(x)
    hood = _hood_blend(z)
    _plan, top, _sy, belt, rail, _rx, _arch, _hood_b = _features(z)
    # Quarter: C-pillar back to where the fastback meets the deck.
    if -1.58 <= z <= -0.78 and 0.22 < ax and hood < 0.50 and top > belt + 0.06:
        hi = min(rail + 0.02, top - 0.01)
        if belt - 0.02 <= y <= hi:
            if -0.810 <= z <= -0.748:
                return MAT_BLACK
            return MAT_GLASS
    if hood < 0.35 and ax > 0.36 and -1.50 <= z <= 1.02 and (rail - belt) > 0.045:
        if belt + 0.008 <= y <= rail - 0.004:
            if 0.020 <= z <= 0.100:
                return MAT_BLACK
            if -0.810 <= z <= -0.730:
                return MAT_BLACK
            if 0.990 <= z <= 1.080:
                return MAT_BLACK
            return MAT_GLASS
        if abs(y - belt) <= 0.012 and -1.15 <= z <= 1.05:
            return MAT_BLACK
    if hood < 0.25 and ax < 0.55 and 0.35 < z < 1.12 and y >= rail - 0.04 and y <= top - 0.008 and top > 1.02:
        return MAT_GLASS
    if hood < 0.40 and ax < 0.62 and -1.55 < z < -0.50 and y >= belt - 0.01 and y <= top - 0.004 and top > 1.02:
        return MAT_GLASS
    return MAT_PAINT


def _u(co):
    return blender_to_unity(co.x, co.y, co.z)


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


def _apply_subsurf(bm, level):
    mesh = bpy.data.meshes.new("sedan_loft")
    bm.to_mesh(mesh)
    obj = bpy.data.objects.new("sedan_loft", mesh)
    bpy.context.scene.collection.objects.link(obj)
    if level > 0:
        mod = obj.modifiers.new("Sub", "SUBSURF")
        mod.levels = int(level)
        mod.render_levels = int(level)
        mod.use_creases = True
        shell._apply_mod(obj, "Sub")
    out = bmesh.new()
    out.from_mesh(obj.data)
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(mesh)
    bm.free()
    return out


def _build_cage(lod):
    # Hard panels. Subdivision rounded the fascia into a soft loft.
    level = 0
    stations = _station_zs()
    bm = bmesh.new()
    crease = bm.edges.layers.float.new("crease_edge")
    rings = []
    info = []
    for z in stations:
        ring = []
        ids = []
        for pi, (x, y) in enumerate(_section(z)):
            vert = bm.verts.new(unity_to_blender(x, y, _end_pull(z, x, y)))
            ring.append(vert)
            ids.append(pi)
        rings.append(ring)
        info.append(ids)
    count = len(rings[0])
    for i in range(len(rings) - 1):
        for j in range(count):
            j2 = (j + 1) % count
            try:
                face = bm.faces.new((rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]))
            except ValueError:
                continue
            face.smooth = False
    try:
        nose = bm.faces.new(list(reversed(rings[0])))
        nose.material_index = MAT_PAINT
        nose.smooth = True
    except ValueError as exc:
        print("NOSE_CAP_FAIL", exc)
    try:
        tail = bm.faces.new(rings[-1])
        tail.material_index = MAT_PAINT
        tail.smooth = True
    except ValueError as exc:
        print("TAIL_CAP_FAIL", exc)
    for face in bm.faces:
        c = _u(face.calc_center_median())
        face.material_index = _paint_at(c[0], c[1], c[2])
    bm.verts.index_update()
    bm.edges.ensure_lookup_table()
    vert_id = {}
    for si, ring in enumerate(rings):
        for pi, vert in enumerate(ring):
            vert_id[vert] = (si, pi)
    for edge in bm.edges:
        pair = [vert_id.get(vert) for vert in edge.verts]
        if pair[0] is None or pair[1] is None:
            edge[crease] = 0.55 if _shut_station(_u(edge.verts[0].co)[2]) else 0.0
            continue
        (s0, p0), (s1, p1) = pair
        if s0 == s1:
            edge[crease] = 0.90 if _shut_station(stations[s0]) else 0.0
            if s0 in (0, len(stations) - 1):
                if I_BELLY in (p0, p1):
                    edge[crease] = max(edge[crease], 0.98)
                else:
                    edge[crease] = max(edge[crease], 0.28)
            continue
        weights = {
            I_BELLY: 0.98,
            I_ROCKER: 0.22,
            I_SHOULDER: 0.88,
            I_BELT: 0.72,
            I_RAIL: 0.94,
            I_CROWN: 0.55,
        }
        for src, weight in list(weights.items()):
            if src not in (I_BELLY, I_CROWN):
                weights[count - src] = weight
        edge[crease] = weights.get(p0, 0.0)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _orient_outward(bm)
    print("LOFT", lod, "stations", len(stations), "ring", count, "faces", len(bm.faces), "level", level)
    bm = _apply_subsurf(bm, level)
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
    open_edges, flipped, samples = _topology(bm)
    print(
        "SHELL_LOD", lod, "faces", len(bm.faces), "verts", len(bm.verts),
        "open", open_edges, "flips", flipped,
    )
    if samples:
        print("SHELL_LOD samples", samples[:6])
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


def _spin_loop(g, x, z, profile, segs, mat):
    """Lathe a closed loop so the axle stays open. Axial is +X outward."""
    sign = 1.0 if x >= 0.0 else -1.0
    bm = bmesh.new()
    rings = []
    for i in range(segs):
        theta = 2.0 * math.pi * i / segs
        ct, st = math.cos(theta), math.sin(theta)
        ring = []
        for rad, axial in profile:
            ring.append(bm.verts.new(unity_to_blender(
                x + sign * axial, AXLE_Y + rad * ct, z + rad * st,
            )))
        rings.append(ring)
    n = len(profile)
    for i in range(segs):
        ni = (i + 1) % segs
        for j in range(n):
            j2 = (j + 1) % n
            try:
                bm.faces.new((rings[i][j], rings[ni][j], rings[ni][j2], rings[i][j2]))
            except ValueError:
                continue
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    axle = Vector(unity_to_blender(x, AXLE_Y, z))
    crown = max(bm.verts, key=lambda vert: (vert.co - axle).length)
    outward = crown.co - axle
    normal = Vector((0.0, 0.0, 0.0))
    for face in crown.link_faces:
        normal += face.normal
    if normal.dot(outward) < 0.0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
    g._ingest(bm, mat, 1.0)


def _spin_tire(g, x, z, lod):
    segs = (16, 10, 6)[lod]
    hw = TIRE_W * 0.5
    bead = RIM_R + 0.010
    crown = TIRE_R
    # Outer tread, then the inner liner, so the bead opening stays empty.
    profile = [
        (bead, -0.92 * hw),
        (bead + 0.055, -0.62 * hw),
        (crown, -0.20 * hw),
        (crown, 0.20 * hw),
        (bead + 0.055, 0.62 * hw),
        (bead, 0.92 * hw),
    ]
    if lod < 2:
        profile.extend((
            (bead + 0.020, 0.55 * hw),
            (crown - 0.055, 0.10 * hw),
            (crown - 0.055, -0.10 * hw),
            (bead + 0.020, -0.55 * hw),
        ))
    _spin_loop(g, x, z, profile, segs, "Lib_Rubber")


def _wheel(g, x, z, year, lod):
    """18 inch alloy. Lip proud of the spokes, brake disc in the openings."""
    sign = 1.0 if x >= 0.0 else -1.0
    info = YEARS[year]
    segs = (12, 8, 6)[lod]
    _spin_tire(g, x, z, lod)
    # Axial offsets are from the wheel center toward the outside of the car.
    # The lip face sits just inside the tire crown. Spokes are 2 cm behind it.
    lip = 0.096
    spoke_face = lip - 0.034
    disc_at = 0.030
    def _oval(major, axial, rad_r, rad_a, n):
        pts = []
        for i in range(n):
            ang = 2.0 * math.pi * i / n
            pts.append((major + rad_r * math.cos(ang), axial + rad_a * math.sin(ang)))
        return pts

    # Round lip proud of a deeper barrel. Both are convex, so the opening stays a hole.
    _spin_loop(g, x, z, _oval(RIM_R * 1.04, lip, 0.016, 0.012, 8), segs, "Lib_PaintSilver")
    if lod < 2:
        _spin_loop(g, x, z, _oval(RIM_R * 0.88, spoke_face - 0.010, 0.014, 0.028, 8), segs, "Lib_Steel")
        g.cylinder(
            (x + sign * disc_at, AXLE_Y, z),
            0.168, 0.018, HOUSING, segs, axis="X",
        )
    # LOD2 stays under 0.6x LOD1. Four spokes and no disc.
    spokes = info["spokes"] if lod == 0 else (4 if lod >= 2 else max(5, info["spokes"] - 1))
    arm = RIM_R * 0.55
    length = RIM_R * 0.78
    thick = 0.026 if lod == 0 else 0.034
    for k in range(spokes):
        theta = math.radians(k * (360.0 / spokes) - 90.0)
        g.box(
            (x + sign * spoke_face, AXLE_Y + math.cos(theta) * arm, z + math.sin(theta) * arm),
            (0.016, length, thick),
            "Lib_Steel",
            euler=(math.degrees(theta), 0.0, 0.0),
        )
    if lod < 2:
        g.cylinder(
            (x + sign * (spoke_face + 0.006), AXLE_Y, z),
            RIM_R * 0.16, 0.018, "Lib_SteelDark", 8, axis="X",
        )


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
    mount = (((x0 + x1) * 0.5, (y0 + y1) * 0.5, mount_z),)
    _track(g, name, kind, center, size, mount)


def _ray_surface(bvh, x, y, origin_z):
    start = Vector(unity_to_blender(x, y, origin_z))
    direction = Vector((0.0, 1.0, 0.0)) if origin_z > 0.0 else Vector((0.0, -1.0, 0.0))
    hit, _normal, _index, _dist = bvh.ray_cast(start, direction, 6.0)
    if hit is None:
        return None
    return _u(hit)[2]


def _n_unity(normal):
    n = Vector((normal.x, normal.z, -normal.y))
    if n.length < 1.0e-8:
        return Vector((0.0, 0.0, 1.0))
    return n.normalized()


def _aim_out(pos, normal):
    n = Vector(normal)
    if n.length < 1.0e-8:
        n = Vector((0.0, 0.0, 1.0 if pos[2] >= 0.0 else -1.0))
    n.normalize()
    aim = Vector((pos[0], pos[1] * 0.15, pos[2]))
    if aim.length < 0.25:
        aim = Vector((0.0, 0.0, 1.0 if pos[2] >= 0.0 else -1.0))
    if n.dot(aim) < 0.0:
        n = -n
    return n


def _cast(bvh, origin, direction, reach):
    hit, normal, _index, _dist = bvh.ray_cast(
        Vector(unity_to_blender(*origin)), Vector(direction), reach,
    )
    if hit is None:
        return None
    pos = _u(hit)
    return pos, _aim_out(pos, _n_unity(normal))


def _hit_front(bvh, x, y):
    return _cast(bvh, (x, y, 4.2), (0.0, 1.0, 0.0), 7.0)


def _hit_rear(bvh, x, y):
    return _cast(bvh, (x, y, -4.2), (0.0, -1.0, 0.0), 7.0)


def _hit_side(bvh, x, y, z):
    sign = 1.0 if x >= 0.0 else -1.0
    return _cast(bvh, (sign * 2.5, y, z), (-sign, 0.0, 0.0), 2.8)


def _lamp_seat(bvh, x, y, wrap):
    """Front ray, blending to a side ray so the lens wraps the fender."""
    front = _hit_front(bvh, x, y)
    z_guess = front[0][2] if front is not None else 2.05
    side = _hit_side(bvh, x, y, z_guess) if wrap > 0.15 else None
    if side is not None and (side[0][2] < 1.45 or abs(side[0][2] - z_guess) > 0.35):
        side = None
    if front is None and side is None:
        return None
    if front is None:
        return side
    if side is None:
        return front
    if (Vector(front[0]) - Vector(side[0])).length > 0.16:
        return front
    t = max(0.0, min(1.0, wrap))
    pos = tuple(front[0][i] * (1.0 - t) + side[0][i] * t for i in range(3))
    nrm = (front[1] * (1.0 - t) + side[1] * t).normalized()
    return pos, _aim_out(pos, nrm)


def _up_axis(normal):
    n = Vector(normal).normalized()
    up = Vector((0.0, 1.0, 0.0)) - n * n.y
    if up.length < 0.25:
        up = Vector((1.0, 0.0, 0.0)) - n * n.x
    return up.normalized()


def _ribbon(g, seats, heights, gap, thick, mat, name):
    """Closed strip seated on the body. `seats` are (pos, normal) samples."""
    if len(seats) < 2:
        print("LAMP_MISS", name)
        return
    bm = bmesh.new()
    rings = []
    mounts = []
    outer = []
    prev_up = None
    for (pos, normal), height in zip(seats, heights):
        n = Vector(normal).normalized()
        up = _up_axis(n)
        if prev_up is not None and up.dot(prev_up) < 0.0:
            up = -up
        prev_up = up
        half = height * 0.5
        back = Vector(pos) + n * gap
        front = Vector(pos) + n * (gap + thick)
        corners = (
            back - up * half,
            back + up * half,
            front + up * half,
            front - up * half,
        )
        rings.append([bm.verts.new(unity_to_blender(*tuple(p))) for p in corners])
        mounts.append(tuple(pos))
        outer.append(tuple(corners[2]))
        outer.append(tuple(corners[3]))
    for i in range(len(rings) - 1):
        for j in range(4):
            j2 = (j + 1) % 4
            try:
                bm.faces.new((rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]))
            except ValueError:
                continue
    try:
        bm.faces.new(tuple(reversed(rings[0])))
    except ValueError:
        pass
    try:
        bm.faces.new(tuple(rings[-1]))
    except ValueError:
        pass
    if not bm.faces:
        bm.free()
        print("LAMP_MISS", name, "empty")
        return
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _orient_outward(bm)
    g._ingest(bm, mat, 1.0)
    xs = [p[0] for p in outer]
    ys = [p[1] for p in outer]
    zs = [p[2] for p in outer]
    center = (
        (min(xs) + max(xs)) * 0.5,
        (min(ys) + max(ys)) * 0.5,
        (min(zs) + max(zs)) * 0.5,
    )
    size = (max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs))
    step = max(1, len(mounts) // 3)
    _track(g, name, "lamp", center, size, tuple(mounts[::step]))


def _headlamp_paths(kind):
    """Inner (x, y, height) to outer. Outer end is higher and wraps the fender."""
    if kind == "thin":
        return [((0.36, 0.58, 0.12), (0.74, 0.64, 0.11), 0.35)]
    if kind == "tier":
        return [
            ((0.34, 0.64, 0.11), (0.78, 0.72, 0.10), 0.55),
            ((0.36, 0.48, 0.10), (0.74, 0.54, 0.10), 0.40),
        ]
    if kind == "swept":
        # Stay on the fascia. A hard corner folds the ribbon if it wraps past it.
        return [((0.30, 0.52, 0.14), (0.68, 0.62, 0.12), 0.20)]
    return [((0.32, 0.56, 0.14), (0.78, 0.64, 0.13), 0.55)]


def _sample_path(path, count):
    (x0, y0, h0), (x1, y1, h1), wrap = path
    pts = []
    for i in range(count):
        t = i / (count - 1)
        s = _smooth01(t)
        pts.append((
            x0 + (x1 - x0) * s,
            y0 + (y1 - y0) * s,
            h0 + (h1 - h0) * s,
            wrap * s,
        ))
    return pts


def _lamps_front(g, info, bvh, lod):
    count = 7 if lod == 0 else (5 if lod == 1 else 3)
    for path in _headlamp_paths(info["lamps"]):
        samples = _sample_path(path, count)
        for sign in (-1.0, 1.0):
            seats = []
            height = samples[0][2]
            for x, y, h, wrap in samples:
                seat = _lamp_seat(bvh, sign * x, y, wrap)
                if seat is None:
                    print("LAMP_MISS", "head", round(sign * x, 3), round(y, 3))
                    seats = []
                    break
                seats.append(seat)
                height = h
            if not seats:
                continue
            heights = [h for _x, _y, h, _wrap in samples]
            _ribbon(g, seats, [h + 0.012 for h in heights], 0.0015, 0.003, HOUSING, "head_housing")
            _ribbon(g, seats, heights, 0.0042, 0.003, LENS, "headlamp")


def _grid_shell(g, bvh, samples, thick, mat, name, kind):
    """samples[row][col] = (x, y) design points. One closed shell on the fascia."""
    grid = []
    normals = []
    for row in samples:
        grow = []
        nrow = []
        for x, y in row:
            hit = _hit_front(bvh, x, y)
            if hit is None:
                print("LAMP_MISS", name, round(x, 3), round(y, 3))
                return
            grow.append(hit[0])
            nrow.append(hit[1])
        grid.append(grow)
        normals.append(nrow)
    rows = len(grid)
    cols = len(grid[0])
    bm = bmesh.new()
    front = []
    back = []
    for r in range(rows):
        fr = []
        bk = []
        for c in range(cols):
            p = Vector(grid[r][c])
            n = Vector(normals[r][c]).normalized()
            bk.append(bm.verts.new(unity_to_blender(*(p + n * 0.0015))))
            fr.append(bm.verts.new(unity_to_blender(*(p + n * (0.0015 + thick)))))
        front.append(fr)
        back.append(bk)

    def quad(a, b, c, d):
        try:
            bm.faces.new((a, b, c, d))
        except ValueError:
            return

    for r in range(rows - 1):
        for c in range(cols - 1):
            quad(front[r][c], front[r][c + 1], front[r + 1][c + 1], front[r + 1][c])
            quad(back[r][c], back[r + 1][c], back[r + 1][c + 1], back[r][c + 1])
    for c in range(cols - 1):
        quad(back[0][c], back[0][c + 1], front[0][c + 1], front[0][c])
        quad(front[-1][c], front[-1][c + 1], back[-1][c + 1], back[-1][c])
    for r in range(rows - 1):
        quad(front[r][0], front[r + 1][0], back[r + 1][0], back[r][0])
        quad(back[r][-1], back[r + 1][-1], front[r + 1][-1], front[r][-1])
    if not bm.faces:
        bm.free()
        return
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _orient_outward(bm)
    g._ingest(bm, mat, 1.0)
    mounts = [grid[r][c] for r in range(rows) for c in (0, cols // 2, cols - 1)]
    center = grid[rows // 2][cols // 2]
    _track(g, name, kind, center, (0.04, 0.04, 0.02), tuple(mounts))


def _trap_rows(y0, y1, x0, x1, rows, cols):
    """y0 is the narrow top, y1 the wide bottom. x0/x1 are half-widths."""
    out = []
    for r in range(rows):
        v = r / (rows - 1)
        y = y0 + (y1 - y0) * v
        hx = x0 + (x1 - x0) * v
        out.append([(-hx + 2.0 * hx * c / (cols - 1), y) for c in range(cols)])
    return out


def _grille(g, info, bvh, lod):
    cols = 6 if lod == 0 else 4
    _grid_shell(
        g, bvh, _trap_rows(0.44, 0.30, 0.26, 0.50, 3, cols),
        0.004, BLACK, "grille_mouth", "grille",
    )
    count = max(3, info["bars"]) if lod < 2 else 2
    y0, y1 = 0.32, 0.42
    span = (y1 - y0)
    step = span / count
    bar = min(info["bar_h"], step * 0.45)
    for i in range(count):
        cy = y0 + step * (i + 0.5)
        _grid_shell(
            g, bvh,
            [[(-0.22, cy - bar * 0.5), (0.0, cy - bar * 0.5), (0.22, cy - bar * 0.5)],
             [(-0.22, cy + bar * 0.5), (0.0, cy + bar * 0.5), (0.22, cy + bar * 0.5)]],
            0.003, "Lib_Steel", "bar%d" % i, "grille",
        )


def _intake(g, info, bvh, lod):
    cols = 8 if lod == 0 else 5
    y0, y1 = 0.20, 0.28
    half = 0.62
    if info["intake"]:
        _grid_shell(
            g, bvh, _trap_rows(y1, y0, half, half, 2, cols),
            0.004, BLACK, "intake", "intake",
        )
        return
    _grid_shell(
        g, bvh, _trap_rows(y1, y0, half, half, 2, cols),
        0.003, PAINT, "intake_plug", "plug",
    )
    if lod == 2:
        return
    for sign in (-1.0, 1.0):
        seats = []
        for x, y in ((sign * 0.42, 0.21), (sign * 0.52, 0.22), (sign * 0.62, 0.22)):
            hit = _hit_front(bvh, x, y)
            if hit is None:
                seats = []
                break
            seats.append(hit)
        if seats:
            _ribbon(g, seats, [0.045, 0.045, 0.045], 0.004, 0.003, LENS, "fog")


def _lamps_rear(g, info, bvh, lod):
    count = 6 if lod == 0 else (4 if lod == 1 else 3)
    if info["tails"] == "thin":
        path = ((0.24, 0.78, 0.09), (0.62, 0.84, 0.08), 0.25)
    elif info["tails"] == "wrap":
        path = ((0.22, 0.74, 0.12), (0.80, 0.86, 0.10), 0.70)
    else:
        path = ((0.24, 0.76, 0.11), (0.70, 0.84, 0.10), 0.35)
    if lod == 2:
        path = (path[0], path[1], 0.0)
    samples = _sample_path(path, count)
    for sign in (-1.0, 1.0):
        seats = []
        height = samples[0][2]
        for x, y, h, wrap in samples:
            rear = _hit_rear(bvh, sign * x, y)
            side = None
            if wrap > 0.2 and rear is not None:
                side = _hit_side(bvh, sign * x, y, rear[0][2])
                if side is not None and side[0][2] > -1.40:
                    side = None
                if side is not None and (Vector(rear[0]) - Vector(side[0])).length > 0.16:
                    side = None
            if rear is None and side is None:
                print("LAMP_MISS", "tail", round(sign * x, 3), round(y, 3))
                seats = []
                break
            if rear is None:
                seats.append(side)
            elif side is None:
                seats.append(rear)
            else:
                t = wrap
                pos = tuple(rear[0][i] * (1.0 - t) + side[0][i] * t for i in range(3))
                nrm = (rear[1] * (1.0 - t) + side[1] * t).normalized()
                seats.append((pos, _aim_out(pos, nrm)))
            height = h
        if not seats:
            continue
        heights = [h for _x, _y, h, _wrap in samples]
        _ribbon(g, seats, [h + 0.01 for h in heights], 0.0015, 0.003, HOUSING, "tail_housing")
        _ribbon(g, seats, heights, 0.0042, 0.003, TAIL_LENS, "tail")


def _skin_x(bvh, sign, y, z):
    start = Vector(unity_to_blender(sign * 1.60, y, z))
    hit, _normal, _index, _dist = bvh.ray_cast(start, Vector((-sign, 0.0, 0.0)), 2.2)
    if hit is None:
        return sign * (_plan_x(z) - 0.05)
    return _u(hit)[0]


def _mirrors(g, info, bvh):
    """Door pad at the A-pillar base. The head sits on the pad. No stalk."""
    mat = info["mirror"]
    z = 0.93
    y = _belt_y(z) - 0.045
    for sign in (-1.0, 1.0):
        skin = _skin_x(bvh, sign, y, z)
        sail_s = (0.010, 0.08, 0.10)
        inner = skin + sign * 0.001
        sail_c = (inner + sign * sail_s[0] * 0.5, y, z)
        g.box(sail_c, sail_s, BLACK)
        mount = (
            (inner, y - 0.02, z - 0.03),
            (inner, y + 0.03, z + 0.03),
            (inner, y, z),
        )
        _track(g, "sail", "mirror", sail_c, sail_s, mount)
        head_s = (0.034, 0.068, 0.12)
        head_c = (
            sail_c[0] + sign * (sail_s[0] * 0.5 + head_s[0] * 0.5 - 0.002),
            y + 0.012,
            z,
        )
        g.box(head_c, head_s, mat)
        _track(g, "mirror", "mirror", head_c, head_s, mount)


def _handles(g, bvh):
    for z in (0.42, -0.28):
        y = _belt_y(z) - 0.09
        for sign in (-1.0, 1.0):
            skin = _skin_x(bvh, sign, y, z)
            size = (0.012, 0.028, 0.11)
            center = (skin + sign * (0.001 + size[0] * 0.5), y, z)
            g.box(center, size, BLACK)
            mount = ((skin + sign * 0.001, y, z),)
            _track(g, "handle", "handle", center, size, mount)


def _audit(g, bvh):
    problems = []
    rows = []
    for item in g.accessory_items:
        dists = []
        for pt in item["mount"]:
            _loc, _normal, _index, dist = bvh.find_nearest(Vector(unity_to_blender(*pt)))
            dists.append(1.0 if dist is None else dist)
        gap = min(dists) if dists else 1.0
        ext = 0.0
        for x, y, z in item["corners"]:
            plan = _plan_x(z)
            top = _top_y(z)
            over_x = abs(x) - plan - 0.01
            over_y = y - top - 0.01
            under = BELLY - 0.01 - y
            over_z = max(z - (Z_NOSE + 0.02), (Z_TAIL - 0.02) - z)
            if item["kind"] == "mirror":
                over_x = abs(x) - plan - 0.22
            ext = max(ext, over_x, over_y, under, over_z)
        flag = ""
        if gap > 0.006:
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
    _lamps_front(g, info, bvh, lod)
    _grille(g, info, bvh, lod)
    _intake(g, info, bvh, lod)
    _lamps_rear(g, info, bvh, lod)
    if lod < 2:
        _mirrors(g, info, bvh)
        _handles(g, bvh)
    for axle in (Z_REAR, Z_FRONT):
        for sign in (-1.0, 1.0):
            _wheel(g, sign * TIRE_X, axle, year, lod)
    _audit(g, bvh)


def add_colliders(asset):
    for i, axle in enumerate((Z_REAR, Z_FRONT)):
        for j, sign in enumerate((-1.0, 1.0)):
            asset.box(
                "Col_Wheel_%d%d" % (i, j),
                (sign * TIRE_X, 0.022, axle),
                (0.014, 0.010, 0.010),
            )
    asset.box("Col_Cabin", (0.0, 0.78, -0.05), (0.42, 0.32, 0.62))
    # Crown is a thin hard surface. A wide box at 1.40 m left the cabin through
    # the sloping roof. This one stays inside and its top is 2 cm under 1.44 m.
    asset.box("Col_Roof", (0.0, 1.26, 0.05), (0.28, 0.32, 0.24))
    asset.box("Col_Hood", (0.0, 0.84, 1.48), (0.40, 0.12, 0.36))
    asset.box("Col_Deck", (0.0, 0.98, -1.55), (0.36, 0.10, 0.28))
    asset.box("Col_Nose", (0.0, 0.36, 1.55), (0.22, 0.12, 0.36))


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
        if dy * dy + dz * dz <= (TIRE_R + 0.04) ** 2 and abs(abs(x) - TIRE_X) < TIRE_W * 0.7:
            return True
    return False


def _body_points(bm):
    pts = []
    for vert in bm.verts:
        x, y, z = _u(vert.co)
        if abs(x) > _plan_x(z) + 0.04 or _is_wheel(x, y, z):
            continue
        pts.append((x, y, z))
    return pts


def _cross_section(bm, z):
    hits = []
    seen = set()
    bm.edges.ensure_lookup_table()
    for edge in bm.edges:
        key = tuple(sorted((edge.verts[0].index, edge.verts[1].index)))
        if key in seen:
            continue
        seen.add(key)
        a = _u(edge.verts[0].co)
        b = _u(edge.verts[1].co)
        if _is_wheel(*a) or _is_wheel(*b):
            continue
        if abs(a[0]) > _plan_x(a[2]) + 0.05 or abs(b[0]) > _plan_x(b[2]) + 0.05:
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


def _band_half(pts, y0, y1):
    xs = [abs(x) for x, y, _z in pts if y0 <= y <= y1]
    return max(xs) if xs else None


def _silhouette(bm):
    pts = _body_points(bm)
    max_y = max((y for x, y, _z in pts if abs(x) < 0.12 and y > 0.5), default=0.0)
    min_y = min((y for x, y, _z in pts if abs(x) < 0.40), default=BELLY)
    max_z = max(z for _x, _y, z in pts)
    min_z = min(z for _x, _y, z in pts)
    max_x = max(abs(x) for x, y, _z in pts if 0.30 < y < 1.20)
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
    for i in range(-16, 16):
        z = i * 0.10
        hits = _cross_section(bm, z)
        if not hits:
            continue
        crown = [y for x, y in hits if abs(x) < 0.08 and y > 0.55]
        if crown:
            keep("top@%+.1f" % z, max(crown) - _top_y(z))
        flank = [abs(x) for x, y in hits if 0.45 < y < 1.05]
        if flank and _arch_t(z) < 0.2:
            keep("plan@%+.1f" % z, max(flank) - _plan_x(z))
    bumper = _band_half(pts, 0.18, 0.42)
    shoulder = _band_half(pts, 0.55, 0.88)
    roof = _band_half(pts, 1.28, 1.42)
    print(
        "FRONT_GATE",
        "bumper", None if bumper is None else round(bumper * 2.0, 3),
        "shoulder", None if shoulder is None else round(shoulder * 2.0, 3),
        "roof", None if roof is None else round(roof * 2.0, 3),
    )
    if bumper is not None and shoulder is not None:
        keep("chin", max(0.0, (shoulder - 0.04) - bumper))
        keep("bumper_flare", max(0.0, bumper - (shoulder + 0.015)))
    if shoulder is not None:
        keep("shoulder_width", shoulder * 2.0 - WIDTH)
    if roof is not None:
        keep("roof_narrow", max(0.0, 1.15 - roof * 2.0))
        keep("roof_wide", max(0.0, roof * 2.0 - 1.35))
    tip = [y for x, y, z in pts if z > max_z - 0.22 and abs(x) < 0.12 and y > 0.55]
    if tip:
        keep("hood_low", max(0.0, 0.70 - max(tip)))
        keep("hood_high", max(0.0, max(tip) - 0.82))
    cowl = [y for x, y, z in pts if 0.90 <= z <= 1.20 and abs(x) < 0.10 and y > 0.85]
    if cowl:
        keep("cowl_low", max(0.0, 0.96 - max(cowl)))
        keep("cowl_high", max(0.0, max(cowl) - 1.06))
    deck = [y for x, y, z in pts if z < -1.55 and abs(x) < 0.40 and y > 0.90]
    if deck:
        keep("deck_low", max(0.0, 1.04 - max(deck)))
        keep("deck_high", max(0.0, max(deck) - 1.16))
        if tip:
            keep("deck_above_hood", max(0.0, (max(tip) + 0.18) - max(deck)))
    low_z = [z for x, y, z in pts if abs(x) < 0.06 and 0.18 <= y <= 0.32]
    lip_z = [z for x, y, z in pts if abs(x) < 0.06 and 0.68 <= y <= 0.84]
    if low_z and lip_z:
        dz = max(low_z) - max(lip_z)
        dy = 0.50
        angle = math.degrees(math.atan2(max(dz, 0.0), dy))
        print("FASCIA_RAKE", round(angle, 1), "dz", round(dz, 3))
        keep("rake_flat", max(0.0, 12.0 - angle) * 0.004)
        keep("rake_steep", max(0.0, angle - 24.0) * 0.004)
    nose_tip = [z for x, y, z in pts if abs(x) < 0.08 and y < 0.42]
    nose_corner = [z for x, y, z in pts if abs(x) > 0.75 and y < 0.55 and z > 1.2]
    if nose_tip and nose_corner:
        pull = max(nose_tip) - max(nose_corner)
        print("NOSE_SWEEP", round(pull, 3))
        keep("nose_flat", max(0.0, 0.22 - pull))
        keep("nose_deep", max(0.0, pull - 0.38))
    tail_tip = [z for x, y, z in pts if abs(x) < 0.08 and y < 0.55 and z < -1.4]
    tail_corner = [z for x, y, z in pts if abs(x) > 0.75 and y < 0.75 and z < -1.2]
    if tail_tip and tail_corner:
        pull = min(tail_corner) - min(tail_tip)
        print("TAIL_SWEEP", round(pull, 3))
        keep("tail_flat", max(0.0, 0.10 - pull))
        keep("tail_deep", max(0.0, pull - 0.28))
    bins = {}

    def _flank_add(x, y):
        if y < 0.18 or y > 0.78 or abs(x) < 0.45:
            return
        key = round(y * 20.0) / 20.0
        bins[key] = max(bins.get(key, 0.0), abs(x))

    bm.edges.ensure_lookup_table()
    for edge in bm.edges:
        a = _u(edge.verts[0].co)
        b = _u(edge.verts[1].co)
        if a[2] < 1.15 and b[2] < 1.15:
            continue
        if _is_wheel(*a) or _is_wheel(*b):
            continue
        for step in range(7):
            t = step / 6.0
            _flank_add(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
    ordered = sorted(bins)
    scallop = 0.0
    for i in range(1, len(ordered) - 1):
        here = bins[ordered[i]]
        if here < bins[ordered[i - 1]] - 0.012 and here < bins[ordered[i + 1]] - 0.012:
            scallop = max(scallop, min(bins[ordered[i - 1]], bins[ordered[i + 1]]) - here)
    print("FRONT_FLANK", round(scallop, 4), "bins", len(ordered))
    keep("fascia_scallop", scallop)
    print(
        "SILHOUETTE", round(worst, 4),
        "size", round(max_x * 2.0, 3), round(max_y, 3), round(max_z - min_z, 3),
        "tire_od", round(TIRE_R * 2.0, 3), "arch_gap", ARCH_GAP, "track", TIRE_X * 2.0,
    )
    if notes:
        print("SILHOUETTE_NOTES", notes[:16])
    return worst


def _stroke(g, a, b, radius=0.008):
    if (Vector(a) - Vector(b)).length < 1.0e-4:
        return
    g.pipe(a, b, radius, "Lib_Orange", segments=4)


def add_reference(g, views=("side",)):
    zs = [i * 0.05 - 2.45 for i in range(99)]
    if "side" in views:
        x_line = -(HALF_W + 0.06)
        crown = [(x_line, _top_y(z), z) for z in zs]
        for a, b in zip(crown, crown[1:]):
            _stroke(g, a, b)
        for a, b in (
            ((x_line, _top_y(Z_NOSE), Z_NOSE), (x_line, BELLY, Z_NOSE)),
            ((x_line, _top_y(Z_TAIL), Z_TAIL), (x_line, BELLY, Z_TAIL)),
            ((x_line, BELLY, Z_NOSE), (x_line, BELLY, Z_TAIL)),
        ):
            _stroke(g, a, b)
        belt = [(HALF_W + 0.02, _belt_y(z), z) for z in zs if -1.1 <= z <= 1.15]
        for a, b in zip(belt, belt[1:]):
            _stroke(g, a, b, 0.005)


def reference_asset(views=("side",)):
    from _common import Asset

    asset = Asset("SedanGuides", "Vehicles", "Orthographic sedan outlines.")
    g = asset.begin(0)
    add_reference(g, views=views)
    asset.end()
    return asset


def shade_object(obj):
    mod = obj.modifiers.new("WeightedNormal", "WEIGHTED_NORMAL")
    mod.keep_sharp = True
    mod.weight = 50
    mod.mode = "FACE_AREA_WITH_ANGLE"
    return obj


def _check_render(asset):
    if os.environ.get("SEDAN_SKIP_CHECK_RENDER") == "1":
        print("SHELL_CHECK render skipped")
        return
    import render_pass2 as stills

    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    out = os.path.join(REPO, "Docs", "AssetStills", "vehicles", "sedan_mid_a", "pass14")
    os.makedirs(out, exist_ok=True)
    year = asset.name[-2:]
    scene = bpy.context.scene
    stills._engine(scene, wide=True)
    scene.cycles.samples = 8
    stills._ensure_materials()
    views = (
        ("front", (0.0, 0.72, 8.0), (0.0, 0.72, 0.0), 3.4),
        ("side", (8.0, 0.72, 0.0), (0.0, 0.72, 0.0), 6.2),
        ("rear", (0.0, 0.72, -8.0), (0.0, 0.72, 0.0), 3.4),
    )
    for label, eye, aim, scale in views:
        for obj in list(bpy.data.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        stills._world(scene, night=False)
        body = stills._spawn(asset, (0.0, 0.0, 0.0))
        shade_object(body)
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
    max_y = 0.0
    min_z = 99.0
    max_z = -99.0
    body_x = 0.0
    for vert in asset.lods[0].bm.verts:
        x, y, z = _u(vert.co)
        if _is_wheel(x, y, z):
            continue
        if abs(x) < _plan_x(z) + 0.02 and 0.40 < y < 1.15:
            body_x = max(body_x, abs(x))
        if abs(x) < 0.70:
            max_y = max(max_y, y)
        max_z = max(max_z, z)
        min_z = min(min_z, z)
    counts = [asset.lods[lod].tri_count() for lod in (0, 1, 2)]
    print(
        "SEDAN_MEASURE", asset.name,
        "size", round(body_x * 2.0, 3), round(max_y, 3), round(max_z - min_z, 3),
        "tris", counts[0], counts[1], counts[2],
    )
    if asset.name.endswith("25") or asset.name.endswith("_25"):
        if not (12000 <= counts[0] <= 15000):
            print("BUDGET_LOD0", counts[0])
        print("BUDGET", counts[0], counts[1], counts[2])
