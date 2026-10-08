"""Midsize sedan, lofted shell.

Pass 13 stops pushing vertices. The body is a closed loft through measured
stations (bumper, lamp face, axle, cowl, A-pillar, B-pillar, C-pillar, deck,
rear bumper, plus the arch shoulders the wheel openings need). Each station
is one smooth section: shoulder widest, tumblehome above it, lower body
tucked only a few centimetres. A Subdivision Surface modifier at level 2
(level 1 on LOD1, the cage itself on LOD2) is applied before the shell is
copied into the year variants.

Proportions follow a 2025 Camry-class table, in metres: length 4.90, width
1.84, height 1.44, wheelbase 2.82, track 1.60, ground clearance 0.14.
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
WIDTH = 1.84
HALF_W = WIDTH * 0.5
HEIGHT = 1.44
WHEELBASE = 2.82
FRONT_OVERHANG = 0.96
Z_FRONT = HALF_L - FRONT_OVERHANG
Z_REAR = Z_FRONT - WHEELBASE
BELLY = 0.14
Z_NOSE = HALF_L
Z_TAIL = -HALF_L
Z_HEADER = 0.42
Z_COWL = 1.26
ROOF_HALF = 0.625

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

# Half-section index. 0 is the belly center, the last is the roof crown.
# Glass sits between the belt and the rail. Trim is the band just outside.
I_BELLY = 0
I_SHOULDER = 5
I_BELT = 7
I_GLASS = 8
I_RAIL = 10
I_CROWN = 12
N_HALF = 13

YEARS = {
    2021: {"spokes": 5, "lamps": "separate", "tails": "separate", "intake": False, "mirror": PAINT, "bars": 4, "bar_h": 0.014},
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


def _top_y(z):
    """Centerline. Hood leading edge near 0.81, roof 1.44, deck near 1.07."""
    return _lerp((
        (-2.45, 1.05),
        (-2.05, 1.07),
        (-1.55, 1.08),
        (-1.05, 1.26),
        (-0.62, 1.40),
        (-0.15, 1.44),
        (0.28, 1.44),
        (0.42, 1.36),
        (1.26, 0.88),
        (1.75, 0.855),
        (2.15, 0.835),
        (2.45, 0.805),
    ), z)


def _plan_x(z):
    """Half-width of the shoulder. Arches are the widest, bumper stays full."""
    return _lerp((
        (-2.45, 0.905),
        (-2.00, 0.92),
        (-1.33, 0.92),
        (-0.50, 0.900),
        (0.70, 0.900),
        (1.49, 0.92),
        (2.05, 0.92),
        (2.45, 0.905),
    ), z)


def _shoulder_y(z):
    return _lerp((
        (-2.45, 0.86),
        (-1.55, 0.94),
        (-0.50, 0.86),
        (0.50, 0.80),
        (1.35, 0.74),
        (2.05, 0.66),
        (2.45, 0.56),
    ), z)


def _belt_y(z):
    """Rises a little toward the rear. Merges into the hood and the deck."""
    return _lerp((
        (-2.20, 1.05),
        (-1.20, 1.055),
        (-0.70, 1.04),
        (0.05, 0.995),
        (0.75, 0.96),
        (1.15, 0.93),
        (1.70, 0.84),
        (2.45, 0.74),
    ), z)


def _rail_y(z):
    return _lerp((
        (-2.10, 1.06),
        (-1.15, 1.22),
        (-0.55, 1.36),
        (0.10, 1.40),
        (0.55, 1.32),
        (1.10, 1.02),
        (1.70, 0.855),
        (2.45, 0.795),
    ), z)


def _rail_x(z):
    """Roof rail half-width. 0.625 is a 1.25 m roof."""
    return _lerp((
        (-2.30, 0.62),
        (-1.15, 0.58),
        (-0.35, 0.58),
        (0.40, 0.58),
        (0.95, 0.50),
        (1.45, 0.36),
        (2.45, 0.55),
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


def _section(z):
    """One closed section. X grows to the shoulder, then falls to the crown.

    The shoulder is the widest point. The bumper tuck is a few centimetres.
    A folded x (out, then back out) creases the hood, so the run is monotonic.
    """
    plan = _plan_x(z)
    top = _top_y(z)
    t_arch = _arch_t(z)
    hood = max(_blend_out(z, 0.50, 1.70), _blend_out(z, -0.80, -1.65))
    shoulder_y = min(_shoulder_y(z), top - 0.06)
    belt_y = _belt_y(z)
    rail_y = _rail_y(z)
    if hood > 0.55:
        shoulder_y = min(shoulder_y, top - 0.14)
        prebelt = top - 0.10
        belt_y = top - 0.055
        glass_y = top - 0.038
        prerail = top - 0.026
        rail_y = top - 0.016
        roof_y = top - 0.008
    else:
        belt_y = min(max(belt_y, shoulder_y + 0.07), top - 0.08)
        rail_y = min(max(rail_y, belt_y + 0.10), top - 0.025)
        prebelt = belt_y - 0.028
        glass_y = belt_y + (rail_y - belt_y) * 0.42
        prerail = rail_y - 0.04
        roof_y = rail_y + (top - rail_y) * 0.62
    lip = _lip_y(z)
    mid_y = 0.48
    if t_arch > 0.02 and lip is not None:
        mid_y = (1.0 - t_arch) * 0.48 + t_arch * min(lip, shoulder_y - 0.03)
    ys = [
        BELLY, BELLY + 0.006, 0.18, 0.36, mid_y, shoulder_y,
        prebelt, belt_y, glass_y, prerail, rail_y, roof_y, top,
    ]
    for i in range(1, len(ys)):
        ys[i] = max(ys[i], ys[i - 1] + 0.008)
    for i in range(len(ys) - 2, -1, -1):
        ys[i] = min(ys[i], ys[i + 1] - 0.008)
    ys[0] = BELLY
    # Roof rail is 0.625 on the cabin. Hood and deck spread out, still inside the shoulder.
    rail_x = _rail_x(z) * (1.0 - hood) + (plan - 0.22) * hood
    upper = [
        plan,
        plan - 0.018,
        plan - 0.045,
        plan - 0.045 - (plan - 0.045 - rail_x) * 0.45,
        rail_x + 0.012,
        rail_x,
        rail_x * 0.52,
        0.0,
    ]
    if hood > 0.45:
        upper = [
            plan,
            plan - 0.012,
            plan - 0.04,
            plan - 0.10,
            plan - 0.18,
            plan - 0.28,
            plan * 0.34,
            0.0,
        ]
    for i in range(1, len(upper)):
        upper[i] = min(upper[i], upper[i - 1] - 0.012)
    upper[-1] = 0.0
    rocker_x = plan - 0.028
    lower_x = plan - 0.010
    mid_x = plan - 0.004
    if t_arch > 0.02:
        rocker_x = (plan - 0.028) * (1.0 - t_arch) + 0.64 * t_arch
        lower_x = (plan - 0.010) * (1.0 - t_arch) + 0.70 * t_arch
        mid_x = plan
    lower = [0.0, min(plan * 0.42, rocker_x - 0.08), rocker_x, lower_x, mid_x, plan]
    for i in range(1, len(lower)):
        lower[i] = max(lower[i], lower[i - 1] + 0.01)
    lower[-1] = plan
    lower[4] = min(lower[4], plan)
    xs = lower + upper[1:]
    half = list(zip(xs, ys))
    ring = list(half)
    for x, y in reversed(half[1:-1]):
        ring.append((-x, y))
    return ring


def _station_zs():
    """Measured stations, nose to tail. Arch shoulders sit on the axle circle."""
    zs = [
        2.450,  # front bumper
        2.180,  # headlamps
        1.880,  # hood
        1.260,  # cowl
        1.090,  # A-pillar, windshield side
        1.015,  # A-pillar, door side
        0.820,  # windshield
        0.460,  # header
        0.280,  # roof
        0.580,  # front door
        0.100,  # B-pillar forward edge
        0.020,  # B-pillar aft edge
        -0.420,  # rear door
        -0.750,  # C-pillar forward
        -0.830,  # C-pillar aft
        -1.900,  # deck
        -2.160,  # tail lamps
        -2.450,  # rear bumper
    ]
    for axle in (Z_FRONT, Z_REAR):
        for dz in (-0.32, -0.24, -0.16, -0.08, 0.0, 0.08, 0.16, 0.24, 0.32):
            zs.append(round(axle + dz, 3))
    ordered = []
    for z in sorted(set(round(z, 3) for z in zs), reverse=True):
        if ordered and abs(ordered[-1] - z) < 0.02:
            continue
        ordered.append(z)
    return ordered


def _shut_station(z):
    for shut in (1.260, 1.090, 1.015, 0.100, 0.020, -0.750, -0.830, -1.900, 2.180, -2.160):
        if abs(z - shut) < 0.012:
            return True
    return False


def _pillar_band(z):
    if 0.020 <= z <= 0.100:
        return True
    if 1.015 <= z <= 1.090:
        return True
    if -0.830 <= z <= -0.750:
        return True
    return False


def _seg_kind(j, n):
    """paint, trim, glass, or upper. j is the edge between point j and j+1."""
    mirror = (n - 1) - j
    base = j if j < mirror else mirror
    if base in (6, 9):
        return "trim"
    if base in (7, 8):
        return "glass"
    if base in (10, 11):
        return "upper"
    return "paint"


def _face_material(kind, z_mid):
    if kind == "trim" and -1.05 <= z_mid <= 1.20:
        return MAT_BLACK
    if kind == "glass" and -0.83 <= z_mid <= 1.015:
        if _pillar_band(z_mid):
            return MAT_BLACK
        return MAT_GLASS
    if kind == "upper":
        top = _top_y(z_mid)
        if 0.48 < z_mid < 1.22 and top > 1.02:
            return MAT_GLASS
        if -1.28 < z_mid < -0.78 and top > 1.12:
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
    level = (2, 1, 0)[lod]
    stations = _station_zs()
    bm = bmesh.new()
    crease = bm.edges.layers.float.new("crease_edge")
    rings = []
    info = []
    for z in stations:
        ring = []
        ids = []
        for pi, (x, y) in enumerate(_section(z)):
            vert = bm.verts.new(unity_to_blender(x, y, z))
            ring.append(vert)
            ids.append(pi)
        rings.append(ring)
        info.append(ids)
    count = len(rings[0])
    for i in range(len(rings) - 1):
        z_mid = (stations[i] + stations[i + 1]) * 0.5
        for j in range(count):
            j2 = (j + 1) % count
            try:
                face = bm.faces.new((rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]))
            except ValueError:
                continue
            face.material_index = _face_material(_seg_kind(j, count), z_mid)
            face.smooth = True
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
            edge[crease] = 0.92 if _shut_station(stations[s0]) else 0.0
            if s0 in (0, len(stations) - 1):
                edge[crease] = max(edge[crease], 0.72)
            continue
        p = p0
        weight = 0.0
        if p == I_SHOULDER:
            weight = 0.90
        elif p == I_BELT:
            weight = 0.78
        elif p == I_RAIL:
            weight = 0.96
        elif p == 4:
            weight = 0.88
        elif p == I_CROWN:
            weight = 0.72
        elif p == I_BELLY:
            weight = 0.45
        edge[crease] = weight
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
    segs = 24 if lod == 0 else (16 if lod == 1 else 10)
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
        (bead + 0.020, 0.55 * hw),
        (crown - 0.055, 0.10 * hw),
        (crown - 0.055, -0.10 * hw),
        (bead + 0.020, -0.55 * hw),
    ]
    _spin_loop(g, x, z, profile, segs, "Lib_Rubber")


def _wheel(g, x, z, year, lod):
    """18 inch alloy. Lip proud of the spokes, brake disc in the openings."""
    sign = 1.0 if x >= 0.0 else -1.0
    info = YEARS[year]
    segs = 20 if lod == 0 else 12
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
    _spin_loop(g, x, z, _oval(RIM_R * 0.88, spoke_face - 0.010, 0.014, 0.028, 8), segs, "Lib_Steel")
    g.cylinder(
        (x + sign * disc_at, AXLE_Y, z),
        0.168, 0.018, HOUSING, segs, axis="X",
    )
    spokes = info["spokes"] if lod == 0 else max(5, info["spokes"] - 1)
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


def _nose_pair(g, bvh, name, x0, x1, y0, y1, lens):
    """Dark housing, then a clear lens. Neither material emits."""
    y = (y0 + y1) * 0.5
    x = (x0 + x1) * 0.5
    hit = _ray_surface(bvh, x, y, 3.6)
    if hit is None:
        print("LAMP_MISS", name, round(x, 3), round(y, 3))
        return
    _slab(g, name + "_housing", "lamp", x0, x1, y0, y1, hit + 0.001, hit + 0.004, HOUSING, hit + 0.001)
    _slab(g, name, "lamp", x0 + 0.006, x1 - 0.006, y0 + 0.004, y1 - 0.004, hit + 0.004, hit + 0.008, lens, hit + 0.004)


def _tail_pair(g, bvh, name, x0, x1, y0, y1):
    y = (y0 + y1) * 0.5
    x = (x0 + x1) * 0.5
    hit = _ray_surface(bvh, x, y, -3.6)
    if hit is None:
        print("LAMP_MISS", name, round(x, 3), round(y, 3))
        return
    _slab(g, name + "_housing", "lamp", x0, x1, y0, y1, hit - 0.004, hit - 0.001, HOUSING, hit - 0.001)
    _slab(g, name, "lamp", x0 + 0.006, x1 - 0.006, y0 + 0.004, y1 - 0.004, hit - 0.008, hit - 0.004, TAIL_LENS, hit - 0.004)


def _lamp_spans(kind):
    if kind == "thin":
        return [(0.34, 0.70, 0.66, 0.74)]
    if kind == "swept":
        return [(0.28, 0.76, 0.60, 0.78)]
    if kind == "tier":
        return [(0.30, 0.72, 0.70, 0.78), (0.30, 0.72, 0.60, 0.67)]
    return [(0.30, 0.72, 0.62, 0.76)]


def _lamps_front(g, info, bvh):
    for span in _lamp_spans(info["lamps"]):
        inner, outer, y0, y1 = span
        for sign in (-1.0, 1.0):
            lo, hi = (sign * inner, sign * outer) if sign > 0 else (-outer, -inner)
            _nose_pair(g, bvh, "headlamp", lo, hi, y0, y1, LENS)


def _grille(g, info, bvh):
    y0, y1 = 0.40, 0.56
    hit = _ray_surface(bvh, 0.0, (y0 + y1) * 0.5, 3.6)
    if hit is None:
        print("GRILLE_MISS")
        return
    _slab(g, "grille_mouth", "grille", -0.30, 0.30, y0, y1, hit + 0.001, hit + 0.003, BLACK, hit + 0.001)
    count = max(3, info["bars"])
    span = (y1 - 0.010) - (y0 + 0.010)
    bar = min(info["bar_h"], span / (count * 2.6))
    step = span / count
    for i in range(count):
        cy = y0 + 0.010 + step * (i + 0.5)
        bar_hit = _ray_surface(bvh, 0.0, cy, 3.6) or hit
        _slab(
            g, "bar%d" % i, "grille", -0.24, 0.24,
            cy - bar * 0.5, cy + bar * 0.5,
            bar_hit + 0.003, bar_hit + 0.006, "Lib_Steel", bar_hit + 0.003,
        )


def _intake(g, info, bvh):
    y0, y1 = 0.20, 0.32
    hit = _ray_surface(bvh, 0.0, (y0 + y1) * 0.5, 3.6)
    if hit is None:
        print("INTAKE_MISS")
        return
    if info["intake"]:
        _slab(g, "intake", "intake", -0.42, 0.42, y0, y1, hit + 0.001, hit + 0.004, BLACK, hit + 0.001)
        return
    _slab(g, "intake_plug", "plug", -0.42, 0.42, y0, y1, hit + 0.001, hit + 0.003, PAINT, hit + 0.001)
    for sign, name in ((-1.0, "fog_l"), (1.0, "fog_r")):
        _nose_pair(g, bvh, name, sign * 0.18 - 0.08, sign * 0.18 + 0.08, y0 + 0.002, y1 - 0.002, LENS)


def _lamps_rear(g, info, bvh):
    y0, y1 = 0.96, 1.05
    outer = 0.78 if info["tails"] == "wrap" else 0.70
    inner = 0.22
    if info["tails"] == "thin":
        y0, y1 = 0.98, 1.03
        outer = 0.66
    for sign in (-1.0, 1.0):
        lo = -outer if sign < 0 else inner
        hi = -inner if sign < 0 else outer
        _tail_pair(g, bvh, "tail", lo, hi, y0, y1)


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
        sail_s = (0.014, 0.09, 0.12)
        inner = skin + sign * 0.001
        sail_c = (inner + sign * sail_s[0] * 0.5, y, z)
        g.box(sail_c, sail_s, BLACK)
        mount = (
            (inner, y - 0.02, z - 0.03),
            (inner, y + 0.03, z + 0.03),
            (inner, y, z),
        )
        _track(g, "sail", "mirror", sail_c, sail_s, mount)
        head_s = (0.095, 0.075, 0.14)
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
    _lamps_front(g, info, bvh)
    _grille(g, info, bvh)
    _intake(g, info, bvh)
    _lamps_rear(g, info, bvh)
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
    asset.box("Col_Roof", (0.0, 1.28, -0.10), (0.32, 0.06, 0.55))
    asset.box("Col_Hood", (0.0, 0.74, 1.70), (0.40, 0.06, 0.40))
    asset.box("Col_Deck", (0.0, 0.96, -1.85), (0.40, 0.05, 0.36))
    asset.box("Col_Nose", (0.0, 0.46, 2.05), (0.36, 0.16, 0.22))


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
    for i in range(-23, 24):
        z = i * 0.10
        hits = _cross_section(bm, z)
        if not hits:
            continue
        crown = [y for x, y in hits if abs(x) < 0.08 and y > 0.55]
        if crown:
            keep("top@%+.1f" % z, max(crown) - _top_y(z))
        flank = [abs(x) for x, y in hits if 0.45 < y < 1.05]
        if flank:
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
    hood = [y for x, y, z in pts if z > 2.20 and abs(x) < 0.12 and y > 0.6]
    if hood:
        keep("hood_low", max(0.0, 0.78 - max(hood)))
        keep("hood_high", max(0.0, max(hood) - 0.86))
    deck = [y for x, y, z in pts if z < -1.85 and abs(x) < 0.45 and y > 0.85]
    if deck:
        keep("deck_low", max(0.0, 1.04 - max(deck)))
        keep("deck_high", max(0.0, max(deck) - 1.12))
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
    out = os.path.join(REPO, "Docs", "AssetStills", "vehicles", "sedan_mid_a", "pass13")
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
    print(
        "SEDAN_MEASURE", asset.name,
        "size", round(body_x * 2.0, 3), round(max_y, 3), round(max_z - min_z, 3),
        "tris", asset.lods[0].tri_count(),
    )
