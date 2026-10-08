"""Modern midsize sedan shell. One loft for every model year.

The cage is a closed cross-section loft. Each station is a smooth half-section
(rocker, door bulge, belt shoulder, greenhouse) and the greenhouse is the top
of that same section. Glass and the fascia pockets are clean quad insets,
offset a few millimetres in from a creased edge loop. The nose is a fascia
grid that wraps a 36 cm corner, not a flat cap. Years only change what sits
in the pockets.

Sheet inches, family-sedan exterior page: length 193.5, width 72.4, height
56.9, wheelbase 111.2, clearance 5.4, front overhang 39.0. The hood height
(0.95 m) and deck height (1.0 m) are the class profile those inches describe.
Nothing here is traced from a drawing.
"""

import math

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

import shell
from _common import blender_to_unity, unity_to_blender

INCH = 0.0254
LENGTH = 193.5 * INCH
HALF_L = LENGTH * 0.5
WIDTH = 72.4 * INCH
HALF_W = WIDTH * 0.5
HEIGHT = 56.9 * INCH
WHEELBASE = 111.2 * INCH
FRONT_OVERHANG = 39.0 * INCH
Z_FRONT = HALF_L - FRONT_OVERHANG
Z_REAR = Z_FRONT - WHEELBASE
BELLY = 5.4 * INCH

TIRE_W = 0.235
RIM_R = 18.0 * INCH * 0.5
TIRE_R = RIM_R + TIRE_W * 0.45
AXLE_Y = TIRE_R
ARCH_GAP = 0.040
ARCH_R = TIRE_R + ARCH_GAP
TIRE_X = min(63.0 * INCH * 0.5, HALF_W - TIRE_W * 0.5 - 0.015)

Z_NOSE = HALF_L
Z_TAIL = -HALF_L
Z_DECK = -1.30
Z_ROOF_R = -0.72
Z_PEAK = -0.05
Z_HEADER = 0.38
Z_COWL = 1.10
Z_WRAP = 1.68
CORNER_R = 0.38
FRONT_HALF = 0.84

CROWN = (
    (Z_TAIL, 0.90),
    (-2.20, 0.93),
    (-1.95, 0.98),
    (Z_DECK, 1.00),
    (-1.00, 1.16),
    (Z_ROOF_R, 1.34),
    (-0.40, 1.42),
    (Z_PEAK, HEIGHT),
    (0.12, 1.42),
    (Z_HEADER, 1.35),
    (Z_COWL, 1.00),
    (1.55, 0.95),
    (1.90, 0.90),
    (2.12, 0.78),
    (2.28, 0.66),
    (2.40, 0.54),
    (Z_NOSE, 0.46),
)

SILL = (
    (Z_DECK, 0.97),
    (-0.40, 0.94),
    (0.55, 0.91),
    (Z_COWL + 0.4, 0.90),
)

PLAN = (
    (Z_TAIL, 0.64),
    (-1.90, 0.86),
    (-1.15, 0.95),
    (0.15, 0.985),
    (Z_COWL, 0.94),
    (1.70, 0.92),
    (2.10, 0.90),
    (2.32, 0.88),
    (Z_NOSE, 0.86),
)

HALF_N = 12
IDX_BULGE = 4
IDX_BELT = 6
TUMBLE = math.tan(math.radians(11.0))

PAINT = "Lib_PaintCrimson"
GLASS = "Lib_AutoGlass"
BLACK = "Lib_Black"

# Region id is the material_index on the cage until ingest.
REGIONS = (
    "paint",
    "windshield",
    "backlight",
    "front_door",
    "rear_door",
    "quarter",
    "grille",
    "lamp",
    "intake",
    "fuel",
    "rocker",
    "diffuser",
    "groove",
    "trim",
)
REGION_ID = {name: index for index, name in enumerate(REGIONS)}
# Center material and inset depth (metres). Glass is 4 mm; pockets are deeper
# so the lamp and the slats can sit in front of the cavity without touching it.
INSET = {
    "windshield": (GLASS, 0.004),
    "backlight": (GLASS, 0.004),
    "front_door": (GLASS, 0.004),
    "rear_door": (GLASS, 0.004),
    "quarter": (GLASS, 0.004),
    "grille": (BLACK, 0.012),
    "lamp": (BLACK, 0.012),
    "intake": (BLACK, 0.010),
    "fuel": (PAINT, 0.003),
    "rocker": (BLACK, 0.004),
    "diffuser": (BLACK, 0.006),
}
SURROUND = 0.009

TAIL_Z = Z_TAIL + 0.012

YEARS = {
    2021: {"spokes": 5, "thick": 0.034, "grille": "slat4", "lamps": "separate", "tails": "separate", "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2022: {"spokes": 10, "thick": 0.014, "grille": "slat8", "lamps": "separate", "tails": "separate", "mirror": PAINT, "fog": "Lib_Headlamp"},
    2023: {"spokes": 7, "thick": 0.020, "grille": "mesh", "lamps": "tier", "tails": "separate", "mirror": PAINT, "fog": "Lib_SignalAmber"},
    2024: {"spokes": 12, "thick": 0.010, "grille": "mesh", "lamps": "thin", "tails": "thin", "mirror": "Lib_Black", "fog": "Lib_Headlamp"},
    2025: {"spokes": 15, "thick": 0.007, "grille": "mesh", "lamps": "bar", "tails": "bar", "mirror": PAINT, "fog": "Lib_Headlamp"},
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


def y_crown(z):
    return _lerp(CROWN, z)


def y_sill(z):
    return _lerp(SILL, z)


def x_body(z):
    x = _lerp(PLAN, z) * HALF_W
    reach = ARCH_R + 0.08
    for axle in (Z_REAR, Z_FRONT):
        dz = abs(z - axle)
        if dz < reach:
            x += 0.028 * math.cos((dz / reach) * math.pi * 0.5)
    return min(HALF_W - 0.004, x)


def _arch_lip(z):
    lip = 0.20
    for axle in (Z_REAR, Z_FRONT):
        dz = abs(z - axle)
        if dz < ARCH_R:
            rise = math.sqrt(max(0.0, ARCH_R * ARCH_R - dz * dz))
            lip = max(lip, AXLE_Y + rise * 0.92)
    return lip


def _greenhouse_weight(z):
    room = y_crown(z) - y_sill(z)
    return max(0.0, min(1.0, (room - 0.05) / 0.16))


def half_at(z):
    """Twelve-point half section. The belt is a shoulder. Above it the side
    falls inward about 11 degrees where there is a real greenhouse."""
    yc = y_crown(z)
    xb = x_body(z)
    w = _greenhouse_weight(z)
    yb = (1.0 - w) * (yc - 0.050) + w * min(y_sill(z), yc - 0.10)
    yb = min(max(yb, BELLY + 0.20), yc - 0.040)
    lip = min(_arch_lip(z), yb - 0.08)
    y_rock = min(max(lip, BELLY + 0.06), yb - 0.08)

    ys = [0.0] * HALF_N
    ys[0] = BELLY
    ys[1] = BELLY + (y_rock - BELLY) * 0.42
    ys[2] = y_rock
    ys[3] = y_rock + (yb - y_rock) * 0.40
    ys[IDX_BULGE] = y_rock + (yb - y_rock) * 0.68
    ys[5] = y_rock + (yb - y_rock) * 0.86
    ys[IDX_BELT] = yb
    span = max(0.02, yc - yb)
    for i, frac in ((7, 0.30), (8, 0.55), (9, 0.76), (10, 0.93)):
        ys[i] = yb + span * frac
    ys[-1] = yc
    for i in range(1, HALF_N):
        if ys[i] < ys[i - 1] + 0.006:
            ys[i] = ys[i - 1] + 0.006
    if ys[-1] > yc:
        ys[-1] = yc
        for i in range(HALF_N - 2, IDX_BELT, -1):
            ys[i] = min(ys[i], ys[i + 1] - 0.006)

    xs = [0.0] * HALF_N
    xs[1] = xb * 0.46
    xs[2] = xb * 0.84
    xs[3] = xb * 0.95
    xs[IDX_BULGE] = xb
    xs[5] = xb * 0.985
    xs[IDX_BELT] = xb * 0.955
    pull = TUMBLE if w > 0.45 else (0.55 * xs[IDX_BELT] / max(span, 0.05))
    for i in (7, 8, 9):
        xs[i] = xs[IDX_BELT] - (ys[i] - yb) * pull
    xs[9] = max(0.06, min(xs[9], xs[IDX_BELT] - 0.02))
    xs[10] = max(0.03, xs[9] * 0.40)
    for i in range(1, HALF_N - 1):
        xs[i] = max(0.02, min(HALF_W - 0.002, xs[i]))
    return list(zip(xs, ys))


def skin_x(z, y):
    half = half_at(z)
    if y <= half[0][1]:
        return half[1][0]
    if y >= half[-1][1]:
        return 0.0
    for i in range(len(half) - 1):
        y0, y1 = half[i][1], half[i + 1][1]
        if y0 <= y <= y1 and y1 > y0:
            t = (y - y0) / (y1 - y0)
            return half[i][0] + (half[i + 1][0] - half[i][0]) * t
    return half[IDX_BELT][0]


def _corner_setback(x):
    ax = abs(x)
    inner = FRONT_HALF - CORNER_R
    if ax <= inner:
        return 0.0
    dx = min(CORNER_R, ax - inner)
    return CORNER_R - math.sqrt(max(0.0, CORNER_R * CORNER_R - dx * dx))


def _roll_setback(y):
    """Chin tuck, a vertical grille face, then a quarter-circle into the hood."""
    if y < 0.18:
        return (0.18 - y) * 0.70
    if y <= 0.46:
        return 0.0
    radius = 0.30
    dy = y - 0.46
    if dy >= radius:
        return radius
    return radius - math.sqrt(max(0.0, radius * radius - dy * dy))


def _fascia_z(x, y):
    """38 cm plan corner, plus the hood rolling down into the fascia."""
    return Z_NOSE - _corner_setback(x) - _roll_setback(y)


def _warp_point(x, y, z):
    if z <= Z_WRAP:
        return (x, y, z)
    span = Z_NOSE - Z_WRAP
    t = max(0.0, min(1.0, (z - Z_WRAP) / span))
    t = t * t * (3.0 - 2.0 * t)
    target = _fascia_z(x, y)
    return (x, y, (1.0 - t) * z + t * min(z, target))


def _stations(lod):
    raw = [z for z, _y in CROWN]
    raw.append((Z_HEADER + Z_COWL) * 0.5)
    raw.append(Z_REAR)
    raw.append(Z_FRONT)
    raw.append(Z_WRAP)
    if lod == 0:
        for axle in (Z_REAR, Z_FRONT):
            raw.append(axle - ARCH_R * 0.92)
            raw.append(axle + ARCH_R * 0.92)
            raw.append(axle)
    zs = sorted(set(round(z, 4) for z in raw if Z_TAIL - 1e-4 <= z <= Z_NOSE + 1e-4))
    kept = [zs[0]]
    gap = 0.05 if lod == 0 else (0.12 if lod == 1 else 0.22)
    for z in zs[1:]:
        if z - kept[-1] < gap and abs(z - Z_NOSE) > 1e-4 and abs(z - Z_TAIL) > 1e-4:
            continue
        kept.append(z)
    if abs(kept[0] - Z_TAIL) > 1e-4:
        kept[0] = Z_TAIL
    if abs(kept[-1] - Z_NOSE) > 1e-4:
        kept.append(Z_NOSE)
    return kept


def _half_index(k, count):
    if k < HALF_N:
        return k
    return count - k


def _region_for_quad(z0, z1, ha, hb):
    zmid = (z0 + z1) * 0.5
    lo, hi = (ha, hb) if ha <= hb else (hb, ha)
    if Z_HEADER + 0.03 < zmid < Z_COWL - 0.04 and lo >= 9:
        return "windshield"
    if Z_DECK + 0.08 < zmid < Z_ROOF_R - 0.05 and lo >= 9:
        return "backlight"
    if 7 <= lo and hi <= 9:
        if 0.12 < zmid < 0.88:
            return "front_door"
        if -0.66 < zmid < -0.04:
            return "rear_door"
        if -1.06 < zmid < -0.78:
            return "quarter"
    if ha < HALF_N and hb < HALF_N and 4 <= lo and hi <= 5 and -1.16 < zmid < -0.96:
        return "fuel"
    if 2 <= lo and hi <= 3:
        if min(abs(zmid - Z_FRONT), abs(zmid - Z_REAR)) > ARCH_R + 0.06 and abs(zmid) < 1.35:
            return "rocker"
    if lo <= 1 and zmid < -2.00:
        return "diffuser"
    return "paint"


def _assemble(lod):
    zs = _stations(lod)
    rings = []
    for z in zs:
        ring = [_warp_point(x, y, z) for x, y, _z in shell._ring(half_at(z), z)]
        rings.append(ring)
    count = len(rings[0])
    verts = [p for ring in rings for p in ring]
    faces = []
    regions = []

    def vid(ring_i, i):
        return ring_i * count + (i % count)

    for ring_i in range(len(rings) - 1):
        z0 = zs[ring_i]
        z1 = zs[ring_i + 1]
        for i in range(count):
            ha = _half_index(i, count)
            hb = _half_index((i + 1) % count, count)
            faces.append((vid(ring_i, i), vid(ring_i, i + 1), vid(ring_i + 1, i + 1), vid(ring_i + 1, i)))
            regions.append(_region_for_quad(z0, z1, ha, hb))
    # Tail cap. The nose is a quad grid, not a fan.
    acc = [0.0, 0.0, 0.0]
    for p in rings[0]:
        acc[0] += p[0]
        acc[1] += p[1]
        acc[2] += p[2]
    center_i = len(verts)
    verts.append(tuple(v / float(count) for v in acc))
    for i in range(count):
        a = i
        b = (i + 1) % count
        faces.append((center_i, b, a))
        regions.append("paint")
    _add_fascia_grid(verts, faces, regions, count, len(rings))
    return zs, verts, faces, regions


def _add_fascia_grid(verts, faces, regions, count, n_rings):
    """Quad patch across the nose. Outer columns sit on the corner radius."""
    xs = [-0.84, -0.76, -0.66, -0.54, -0.40, -0.24, -0.10,
          0.10, 0.24, 0.40, 0.54, 0.66, 0.76, 0.84]
    ys = [0.12, 0.18, 0.26, 0.34, 0.42, 0.50, 0.58, 0.64]
    grid = []
    for y in ys:
        row = []
        for x in xs:
            row.append(len(verts))
            verts.append((x, y, _fascia_z(x, y)))
        grid.append(row)
    for j in range(len(ys) - 1):
        for i in range(len(xs) - 1):
            ymid = (ys[j] + ys[j + 1]) * 0.5
            if ymid < 0.18:
                region = "paint"
            elif ymid < 0.31:
                region = "intake"
            elif ymid < 0.48:
                region = "grille"
            elif ymid < 0.66:
                region = "lamp"
            else:
                region = "paint"
            faces.append((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))
            regions.append(region)
    del count, n_rings


def _u_co(co):
    return blender_to_unity(co.x, co.y, co.z)


def _u_n(normal):
    return (normal.x, normal.z, -normal.y)


def _bmesh_from_lists(verts, faces, regions):
    bm = bmesh.new()
    bverts = [bm.verts.new(unity_to_blender(*v)) for v in verts]
    bm.verts.index_update()
    for face, region in zip(faces, regions):
        try:
            nf = bm.faces.new([bverts[i] for i in face])
        except ValueError:
            continue
        nf.material_index = REGION_ID.get(region, 0)
        nf.smooth = True
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    _bridge_nose(bm)
    _cull_covered_paint(bm)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def _cull_covered_paint(bm):
    """Drop bridge faces that landed on the fascia grid."""
    solids = [face for face in bm.faces if face.material_index != 0]
    kill = []
    for face in bm.faces:
        if face.material_index != 0 or len(face.verts) < 3:
            continue
        center = face.calc_center_median()
        for other in solids:
            delta = center - other.calc_center_median()
            dist = abs(delta.dot(other.normal))
            if dist > 0.004:
                continue
            if _point_in_face(other, center - other.normal * delta.dot(other.normal)):
                kill.append(face)
                break
    if kill:
        bmesh.ops.delete(bm, geom=kill, context="FACES")
    print("BRIDGE_CULL", len(kill))


def _bridge_nose(bm):
    """Connect the open nose ring to the fascia grid. Two boundary loops."""
    edges = [edge for edge in bm.edges if len(edge.link_faces) == 1]
    if len(edges) < 8:
        print("BRIDGE_SKIP", len(edges))
        return
    try:
        bmesh.ops.bridge_loops(bm, edges=edges)
        print("BRIDGE_LOOPS", len(edges))
    except (TypeError, ValueError, RuntimeError) as exc:
        print("BRIDGE_FAIL", exc)


def _crease(bm):
    layer = bm.edges.layers.float.get("crease_edge")
    if layer is None:
        layer = bm.edges.layers.float.new("crease_edge")
    for edge in bm.edges:
        ids = {face.material_index for face in edge.link_faces}
        named = {REGIONS[i] for i in ids if i < len(REGIONS)}
        if len(named - {"paint"}) >= 1 and ("paint" in named or len(named) > 1):
            edge[layer] = 0.95
        elif len(edge.link_faces) == 2:
            ang = edge.calc_face_angle(0.0)
            if ang > math.radians(68):
                edge[layer] = 0.80
        # Belt shoulder through the cabin, and the tail cap.
        if len(edge.link_faces) == 2:
            a = _u_co(edge.verts[0].co)
            b = _u_co(edge.verts[1].co)
            if abs(a[2] - b[2]) > abs(a[1] - b[1]) and Z_DECK < a[2] < Z_COWL:
                if abs(a[1] - y_sill(a[2])) < 0.03 and abs(b[1] - y_sill(b[2])) < 0.03:
                    edge[layer] = 0.92
            if a[2] < Z_TAIL + 0.04 and b[2] < Z_TAIL + 0.04:
                edge[layer] = 0.95
    return layer


def _tag_fascia(bm):
    """Mark forward quads on the nose. Triangles from the bridge stay paint."""
    bm.faces.ensure_lookup_table()
    bm.normal_update()
    counts = {"grille": 0, "lamp": 0, "intake": 0}
    for face in bm.faces:
        if len(face.verts) != 4 or face.material_index != 0:
            continue
        x, y, z = _u_co(face.calc_center_median())
        _nx, _ny, nz = _u_n(face.normal)
        nx = _nx
        # Fender wrap past the grid. The fascia quads are already tagged.
        if z < 1.92 or abs(x) < 0.58 or nz < 0.05:
            continue
        forward = nz > 0.20 or abs(nx) > 0.30
        if not forward:
            continue
        kind = None
        if 0.46 < y < 0.74:
            kind = "lamp"
        if kind is None:
            continue
        face.material_index = REGION_ID[kind]
        counts[kind] += 1
    print("FASCIA_TAG", counts)
    return counts


def _wells():
    length = 0.72
    return [
        ((sign * TIRE_X, AXLE_Y, axle), ARCH_R, length)
        for axle in (Z_REAR, Z_FRONT)
        for sign in (-1.0, 1.0)
    ]


def _groove_cutters():
    """Shallow slots for the door shuts. Full box size, biting the skin."""
    cuts = []
    ys = (0.40, 0.86)
    height = ys[1] - ys[0]
    ymid = (ys[0] + ys[1]) * 0.5
    for z in (0.96, 0.06, -0.74):
        x = skin_x(z, ymid)
        for sign in (1.0, -1.0):
            cuts.append((
                (sign * (x + 0.004), ymid, z),
                (0.040, height, 0.016),
            ))
    z0, z1 = -0.70, 0.92
    y = 0.40
    x = skin_x((z0 + z1) * 0.5, y)
    for sign in (1.0, -1.0):
        cuts.append((
            (sign * (x + 0.004), y, (z0 + z1) * 0.5),
            (0.040, 0.016, z1 - z0),
        ))
    return cuts


def _apply_cage(bm, lod):
    me = bpy.data.meshes.new("sedan_cage")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new("sedan_cage", me)
    bpy.context.scene.collection.objects.link(obj)
    level = 2 if lod == 0 else (1 if lod == 1 else 0)
    if level:
        mod = obj.modifiers.new("Sub", "SUBSURF")
        mod.levels = int(level)
        mod.render_levels = int(level)
        mod.use_creases = True
        shell._apply_mod(obj, "Sub")
    for i, (center, radius, length) in enumerate(_wells()):
        shell._difference(obj, shell._well_cutter(center, radius, length), "Well%d" % i)
    if lod == 0:
        for i, (center, size) in enumerate(_groove_cutters()):
            shell._difference(obj, shell._box_cutter(center, size), "Groove%d" % i)
    out = bmesh.new()
    out.from_mesh(obj.data)
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(me)
    bmesh.ops.recalc_face_normals(out, faces=out.faces)
    return out


def _paint_grooves(bm):
    """Faces left inside a shut slot become the black groove."""
    planes = []
    for z in (0.96, 0.06, -0.74):
        planes.append(("z", z, 0.38, 0.88, -1.2, 1.4))
    planes.append(("y", 0.40, -0.70, 0.92, 0.0, 0.0))
    painted = 0
    for face in bm.faces:
        if face.material_index != 0:
            continue
        x, y, z = _u_co(face.calc_center_median())
        if abs(x) < 0.45:
            continue
        hit = False
        for kind, pos, a, b, _c, _d in planes:
            if kind == "z" and abs(z - pos) < 0.012 and a - 0.02 < y < b + 0.02:
                hit = True
            if kind == "y" and abs(y - pos) < 0.012 and a < z < b:
                hit = True
        if hit:
            face.material_index = REGION_ID["groove"]
            painted += 1
    print("GROOVE_FACES", painted)


def _hold_fascia(bm):
    """Put the outer nose skin back on the wrapped plan after subdivision.

    Pocket faces sit 10 mm or more behind that surface and are left there.
    """
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        if z < Z_WRAP or y < 0.12:
            continue
        target = _fascia_z(x, y)
        gap = target - z
        if z > Z_NOSE + 0.001:
            vert.co.y = -Z_NOSE
            z = Z_NOSE
        elif 0.002 < gap <= 0.008:
            vert.co.y = -(z + gap * 0.9)
        crown = y_crown(z)
        half = 0.55
        if z > 2.00 and abs(x) < half and y > crown + 0.012:
            influence = (1.0 - abs(x) / half) ** 2
            drop = (y - crown - 0.006) * influence
            if drop > 0.004:
                vert.co.z -= drop


def _hold_crown(bm):
    """Lift only the centerline sag from subdivision, not the fascia below it."""
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        target = y_crown(z)
        if y < 0.55 or y > target - 0.002:
            continue
        if abs(x) > 0.42:
            continue
        short = target - y
        if short < 0.004 or short > 0.07:
            continue
        influence = (1.0 - abs(x) / 0.42) ** 2
        vert.co.z += short * influence


def _components(bm, region_id):
    wanted = [face for face in bm.faces if face.material_index == region_id]
    seen = set()
    groups = []
    for face in wanted:
        if face in seen:
            continue
        stack = [face]
        seen.add(face)
        group = [face]
        while stack:
            current = stack.pop()
            for edge in current.edges:
                for other in edge.link_faces:
                    if other.material_index != region_id or other in seen:
                        continue
                    seen.add(other)
                    stack.append(other)
                    group.append(other)
        groups.append(group)
    return groups


def _inset_component(bm, faces, center_index, black_index, depth):
    """One average normal, one edge loop, glass or cavity set back 3–12 mm.

    Boundary verts stay on the body. A new loop sits `SURROUND` in from that
    edge and `depth` along the outward normal. The original faces become the
    inner panel. Border quads are the black reveal.
    """
    face_set = set(faces)
    if len(face_set) < 2:
        return 0
    normal = Vector((0.0, 0.0, 0.0))
    for face in face_set:
        normal += face.normal
    if normal.length < 1e-8:
        return 0
    normal.normalize()
    boundary = []
    bverts = set()
    for face in face_set:
        for edge in face.edges:
            inside = 0
            for other in edge.link_faces:
                if other in face_set:
                    inside += 1
            if inside == 1:
                boundary.append(edge)
                bverts.add(edge.verts[0])
                bverts.add(edge.verts[1])
    if len(boundary) < 3:
        return 0
    center = Vector((0.0, 0.0, 0.0))
    for vert in bverts:
        center += vert.co
    center /= float(len(bverts))
    # Each vert follows its own region normal so a wrapped fascia stays a
    # parallel panel instead of one flat plate cutting through the corner.
    region_verts = set()
    vnorm = {}
    for face in face_set:
        for vert in face.verts:
            region_verts.add(vert)
            vnorm[vert] = vnorm.get(vert, Vector((0.0, 0.0, 0.0))) + face.normal
    new_for = {}
    for vert in region_verts:
        nrm = vnorm.get(vert, normal)
        if nrm.length > 1e-8:
            nrm = nrm.normalized()
        else:
            nrm = normal
        if vert in bverts:
            inward = center - vert.co
            inward = inward - nrm * inward.dot(nrm)
            if inward.length > 1e-6:
                inward.normalize()
            else:
                inward = Vector((0.0, 0.0, 0.0))
            new_for[vert] = bm.verts.new(vert.co + inward * SURROUND - nrm * depth)
        else:
            new_for[vert] = bm.verts.new(vert.co - nrm * depth)
    made = 0
    failed = []
    for face in face_set:
        try:
            created = bm.faces.new([new_for[vert] for vert in face.verts])
        except ValueError as exc:
            failed.append(face)
            if len(failed) <= 2:
                print("INSET_FACE_FAIL", exc)
            continue
        created.material_index = center_index
        created.smooth = True
        made += 1
    if failed:
        print("INSET_KEPT", len(failed), "of", len(face_set))
    for edge in boundary:
        face = next(f for f in edge.link_faces if f in face_set)
        fv = list(face.verts)
        v0, v1 = edge.verts
        i0 = fv.index(v0)
        if fv[(i0 + 1) % len(fv)] == v1:
            a, b = v0, v1
        else:
            a, b = v1, v0
        try:
            border = bm.faces.new((a, new_for[a], new_for[b], b))
        except ValueError:
            continue
        border.material_index = black_index
        border.smooth = True
    doomed = [face for face in face_set if face not in failed]
    if doomed:
        bmesh.ops.delete(bm, geom=doomed, context="FACES")
    return made


def _inset_openings(bm):
    black_index = REGION_ID["trim"]
    # Remap trim to black at ingest. Using a dedicated index keeps the border
    # out of the "paint" id so a later inset does not swallow the reveal.
    report = []
    for name, (mat, depth) in INSET.items():
        del mat
        groups = _components(bm, REGION_ID[name])
        done = 0
        for group in groups:
            done += _inset_component(bm, group, REGION_ID[name], black_index, depth)
        report.append("%s:%d" % (name, done))
    print("INSET_FACES", " ".join(report))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)


def _flare_arches(bm):
    """Push the outer cut ring out so the opening reads as a lip, not a skirt.

    The well boolean stays manifold, so the lip is the ring of verts sitting
    on the cutter, out near the fender skin.
    """
    moved = 0
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        for sign in (-1.0, 1.0):
            if abs(x - sign * TIRE_X) < 0.05:
                continue
            if abs(x) < abs(TIRE_X):
                continue
            for axle in (Z_REAR, Z_FRONT):
                ry = y - AXLE_Y
                rz = z - axle
                radial = math.hypot(ry, rz)
                if abs(radial - ARCH_R) > 0.014:
                    continue
                if abs(x) < skin_x(z, max(y, 0.3)) - 0.08:
                    continue
                mag = radial or 1.0
                dx = sign * 0.020
                dy = ry / mag * 0.012
                dz = rz / mag * 0.012
                vert.co.x += dx
                vert.co.y -= dz
                vert.co.z += dy
                moved += 1
                break
    print("ARCH_LIP_VERTS", moved)


def _nearest_edge(bm, point, max_dist):
    target = Vector(unity_to_blender(*point))
    best = None
    best_d = max_dist
    for edge in bm.edges:
        if len(edge.link_faces) < 1:
            continue
        mid = (edge.verts[0].co + edge.verts[1].co) * 0.5
        dist = (mid - target).length
        if dist < best_d:
            best_d = dist
            best = edge
    return best


def _wiper(bm, root, tip):
    edge = _nearest_edge(bm, root, 0.12)
    if edge is None:
        print("WIPER_MISS", root)
        return
    v0, v1 = edge.verts
    mid = (v0.co + v1.co) * 0.5
    delta = Vector(unity_to_blender(tip[0] - root[0], tip[1] - root[1], tip[2] - root[2]))
    span = Vector(unity_to_blender(0.018, 0.0, 0.0))
    t0 = bm.verts.new(mid + delta * 0.45 + span)
    t1 = bm.verts.new(mid + delta * 0.45 - span)
    u0 = bm.verts.new(mid + delta + span * 0.55)
    u1 = bm.verts.new(mid + delta - span * 0.55)
    trim = REGION_ID["trim"]
    for quad in ((v0, v1, t1, t0), (t0, t1, u1, u0)):
        try:
            face = bm.faces.new(quad)
        except ValueError:
            continue
        face.material_index = trim
        face.smooth = True


def _add_wipers(bm):
    _wiper(bm, (0.16, 0.97, 1.08), (-0.02, 1.20, 0.52))
    _wiper(bm, (-0.12, 0.96, 1.06), (-0.38, 1.16, 0.58))


def _raise_handles(bm):
    """Pull handles welded to a door edge so they stay on the body island."""
    spots = ((0.58, 0.74), (-0.32, 0.72))
    raised = 0
    trim = REGION_ID["trim"]
    for z, y in spots:
        for sign in (1.0, -1.0):
            skin = skin_x(z, y)
            edge = _nearest_edge(bm, (sign * skin, y, z), 0.10)
            if edge is None:
                print("HANDLE_MISS", round(sign * skin, 2), round(y, 2), round(z, 2))
                continue
            v0, v1 = edge.verts
            mid = (v0.co + v1.co) * 0.5
            span = v1.co - v0.co
            if span.length < 1e-5:
                continue
            span.normalize()
            out = Vector(unity_to_blender(sign * 0.026, 0.006, 0.0))
            along = Vector(unity_to_blender(0.0, 0.0, 0.070))
            up = Vector(unity_to_blender(0.0, 0.028, 0.0))
            root0 = bm.verts.new(mid + span * 0.012)
            root1 = bm.verts.new(mid - span * 0.012)
            a = bm.verts.new(mid - along + out)
            b = bm.verts.new(mid + along + out)
            c = bm.verts.new(mid + along + out + up)
            d = bm.verts.new(mid - along + out + up)
            for quad in ((v0, v1, root1, root0), (root0, root1, a, d), (a, b, c, d)):
                try:
                    face = bm.faces.new(quad)
                except ValueError:
                    continue
                face.material_index = trim
                face.smooth = True
            raised += 1
    print("HANDLES", raised)


def _diffuser_fins(bm):
    """Three short strakes under the rear bumper, joined to the diffuser."""
    origin_z = -2.15
    made = 0
    for x in (-0.28, 0.0, 0.28):
        origin = Vector(unity_to_blender(x, 0.22, origin_z))
        near = []
        for face in bm.faces:
            if face.material_index != REGION_ID["diffuser"]:
                continue
            if (face.calc_center_median() - origin).length < 0.08:
                near.append(face)
        near = near[:2]
        if not near:
            continue
        ret = bmesh.ops.extrude_face_region(bm, geom=near)
        verts = [ele for ele in ret["geom"] if isinstance(ele, bmesh.types.BMVert)]
        bmesh.ops.translate(bm, verts=verts, vec=Vector((0.0, 0.006, -0.004)))
        made += 1
    print("DIFFUSER_FINS", made)


def _islands(bm):
    seen = set()
    islands = []
    bm.faces.ensure_lookup_table()
    for face in bm.faces:
        if face in seen:
            continue
        stack = [face]
        seen.add(face)
        group = [face]
        while stack:
            current = stack.pop()
            for edge in current.edges:
                for other in edge.link_faces:
                    if other in seen:
                        continue
                    seen.add(other)
                    stack.append(other)
                    group.append(other)
        islands.append(group)
    return islands


def _drop_specks(bm, limit=24):
    islands = _islands(bm)
    if len(islands) < 2:
        return 0
    islands.sort(key=len)
    dropped = 0
    kill = []
    for group in islands[:-1]:
        if len(group) <= limit:
            kill.extend(group)
            dropped += 1
    if kill:
        bmesh.ops.delete(bm, geom=kill, context="FACES")
    return dropped


def _ingest(g, src):
    """Map region ids onto real palette slots. Trim borders are black."""
    slot = {}

    def resolve(region_index):
        name = REGIONS[region_index] if 0 <= region_index < len(REGIONS) else "paint"
        if name in ("trim", "groove", "grille", "lamp", "intake", "rocker", "diffuser"):
            mat = BLACK
        elif name in INSET:
            mat = INSET[name][0]
        else:
            mat = PAINT
        if mat not in slot:
            slot[mat] = g.slot(mat)
        return slot[mat]

    vmap = {}
    for vert in src.verts:
        vmap[vert] = g.bm.verts.new(vert.co.copy())
    g.bm.verts.index_update()
    for face in src.faces:
        try:
            nf = g.bm.faces.new([vmap[vert] for vert in face.verts])
        except ValueError:
            continue
        nf.material_index = resolve(face.material_index)
        nf.smooth = True
        nf[g.scale_layer] = 1.0
    src.free()


def _section_report(zs):
    rake = math.degrees(math.atan2(1.35 - 1.00, Z_COWL - Z_HEADER))
    print("BODY_A_STATIONS", len(zs), "RAKE_DEG", round(rake, 2))
    half = half_at(0.0)
    belt = half[IDX_BELT]
    upper = half[9]
    dy = upper[1] - belt[1]
    dx = belt[0] - upper[0]
    tumble = math.degrees(math.atan2(dx, dy)) if dy > 0.02 else 0.0
    print(
        "BODY_A_TUMBLE_DEG", round(tumble, 2),
        "belt", tuple(round(v, 3) for v in belt),
        "rail", tuple(round(v, 3) for v in upper),
        "crown", round(half[-1][1], 3),
    )


def build_shell(g, lod):
    zs, verts, faces, regions = _assemble(lod)
    if lod == 0:
        _section_report(zs)
        if len(zs) < 14:
            raise RuntimeError("expected at least 14 stations, got %d" % len(zs))
    bm = _bmesh_from_lists(verts, faces, regions)
    _crease(bm)
    if lod < 2:
        _tag_fascia(bm)
        _crease(bm)
    bm = _apply_cage(bm, lod)
    if lod == 0:
        _paint_grooves(bm)
    _hold_crown(bm)
    if lod < 2:
        _inset_openings(bm)
        _hold_fascia(bm)
    _flare_arches(bm)
    if lod == 0:
        _add_wipers(bm)
        _raise_handles(bm)
        _diffuser_fins(bm)
    specks = _drop_specks(bm)
    if specks:
        print("BODY_A_SPECKS", "lod", lod, specks)
    _ingest(g, bm)
    if lod == 0:
        _separate_coplanar(g.bm)


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
            nf = g.bm.faces.new([vmap[vert] for vert in face.verts])
        except ValueError:
            continue
        nf.material_index = index[face.material_index]
        nf.smooth = face.smooth
        nf[g.scale_layer] = face[src.scale_layer]
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
            ux = x + axial * hw
            uy = AXLE_Y + rad * ct
            uz = z + rad * st
            ring.append(bm.verts.new(unity_to_blender(ux, uy, uz)))
        rings.append(ring)
    for i in range(segs):
        ni = (i + 1) % segs
        for j in range(len(profile) - 1):
            try:
                bm.faces.new((rings[i][j], rings[ni][j], rings[ni][j + 1], rings[i][j + 1]))
            except ValueError:
                continue
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    g._ingest(bm, "Lib_Rubber", 1.0)


def _wheel(g, x, z, year, lod):
    sign = 1.0 if x > 0.0 else -1.0
    info = YEARS[year]
    segs = 20 if lod == 0 else (12 if lod == 1 else 8)
    _spin_tire(g, x, z, lod)
    # Proud of the sidewall. The old face sat inside the tyre and read as a disc.
    bead = TIRE_W * 0.47 * 0.94
    rim_x = x + sign * (bead + 0.012)
    g.cylinder((rim_x - sign * 0.014, AXLE_Y, z), RIM_R * 0.90, 0.010, BLACK, segs, axis="X")
    g.cylinder((rim_x - sign * 0.002, AXLE_Y, z), RIM_R * 1.01, 0.016, "Lib_Steel", segs, axis="X")
    face = rim_x + sign * 0.008
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
    # Hub sits behind the spoke face so the recesses stay dark.
    g.cylinder((face - sign * 0.010, AXLE_Y, z), RIM_R * 0.22, 0.012, "Lib_SteelDark", max(8, segs // 2), axis="X")
    # Solid core so the wheel collider has a closed volume at the axle.
    g.cylinder((x, AXLE_Y, z), 0.062, 0.030, "Lib_SteelDark", 10, axis="X")


def _cast(bvh, x, y, z_from, z_to):
    start = Vector(unity_to_blender(x, y, z_from))
    end = Vector(unity_to_blender(x, y, z_to))
    direction = end - start
    hit, normal, _index, _dist = bvh.ray_cast(start, direction.normalized(), direction.length)
    if hit is None:
        return None
    hx, hy, hz = blender_to_unity(hit.x, hit.y, hit.z)
    nx, ny, nz = _u_n(normal)
    return (hx, hy, hz), (nx, ny, nz)


def _analytic_hit(x, y):
    """Point and outward normal on the wrapped fascia, before the pocket inset."""
    z = _fascia_z(x, y)
    eps = 0.02
    dzdx = (_fascia_z(x + eps, y) - _fascia_z(x - eps, y)) / (2.0 * eps)
    dzdy = (_fascia_z(x, y + eps) - _fascia_z(x, y - eps)) / (2.0 * eps)
    nx, ny, nz = -dzdx, -dzdy, 1.0
    mag = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    return (x, y, z), (nx / mag, ny / mag, nz / mag)


def _lamp_y(x, base):
    """Ends rise a little as the lamp wraps onto the fender."""
    return base + 0.045 * (abs(x) / 0.86) ** 1.4


def _sample_band(bvh, x0, x1, y, steps, sweep=False):
    del bvh
    samples = []
    count = max(2, steps)
    for i in range(count + 1):
        x = x0 + (x1 - x0) * i / count
        yy = _lamp_y(x, y) if sweep else y
        samples.append(_analytic_hit(x, yy))
    return samples


def _strip(g, samples, half_h, mat, inset):
    """Quad strip seated `inset` metres inside the hit points, following the surface."""
    if len(samples) < 2:
        return
    bm = bmesh.new()
    low = []
    high = []
    for (x, y, z), (nx, ny, nz) in samples:
        n = Vector((nx, ny, nz))
        if n.length < 1e-6:
            n = Vector((0.0, 0.0, 1.0))
        n.normalize()
        up = Vector((0.0, 1.0, 0.0)) - n * n.y
        if up.length < 0.25:
            up = Vector((1.0, 0.0, 0.0)) - n * n.x
        up.normalize()
        base = Vector((x, y, z)) - n * inset
        for bucket, scale in ((low, -half_h), (high, half_h)):
            p = base + up * scale
            bucket.append(bm.verts.new(unity_to_blender(p.x, p.y, p.z)))
    for i in range(len(low) - 1):
        try:
            bm.faces.new((low[i], low[i + 1], high[i + 1], high[i]))
        except ValueError:
            continue
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    g._ingest(bm, mat, 1.0)


def _grille(g, bvh, year, lod):
    kind = YEARS[year]["grille"]
    y = 0.38
    span = 0.58
    samples = _sample_band(bvh, -span, span, y, 10)
    if len(samples) < 2:
        print("GRILLE_CAST_MISS")
        return
    if kind == "slat4":
        for i in range(4):
            yy = 0.32 + i * 0.028
            _strip(g, _sample_band(bvh, -0.52, 0.52, yy, 8), 0.004, "Lib_SteelDark", 0.005)
    elif kind == "slat8":
        count = 6 if lod == 0 else 3
        for i in range(count):
            yy = 0.31 + i * 0.020
            _strip(g, _sample_band(bvh, -0.50, 0.50, yy, 8), 0.003, "Lib_Steel", 0.005)
    else:
        for i in range(5 if lod == 0 else 3):
            yy = 0.31 + i * 0.022
            _strip(g, _sample_band(bvh, -span, span, yy, 12), 0.003, "Lib_SteelDark", 0.006)
        cols = 5 if lod == 0 else 3
        for i in range(cols):
            x = -0.36 + i * (0.72 / max(1, cols - 1))
            (hx, hy, hz), (nx, ny, nz) = _analytic_hit(x, y)
            g.box(
                (hx - nx * 0.006, hy - ny * 0.006, hz - nz * 0.006),
                (0.004, 0.10, 0.004),
                "Lib_SteelDark",
            )


def _lamps_front(g, bvh, year, lod):
    kind = YEARS[year]["lamps"]
    y = 0.54
    if kind == "bar":
        _strip(g, _sample_band(bvh, -0.86, 0.86, y, 18, sweep=True), 0.016, "Lib_Headlamp", 0.012)
        return
    span = (0.22, 0.84)
    tall = 0.026 if kind == "separate" else (0.010 if kind == "thin" else 0.014)
    steps = 10 if lod == 0 else 5
    for sign in (1.0, -1.0):
        if kind == "tier":
            _strip(g, _sample_band(bvh, sign * span[0], sign * span[1], y + 0.02, steps, sweep=True), 0.008, "Lib_Headlamp", 0.012)
            _strip(g, _sample_band(bvh, sign * (span[0] + 0.06), sign * (span[1] - 0.04), y - 0.01, steps, sweep=True), 0.007, "Lib_Headlamp", 0.012)
        else:
            _strip(g, _sample_band(bvh, sign * span[0], sign * span[1], y, steps, sweep=True), tall, "Lib_Headlamp", 0.012)


def _lamps_rear(g, bvh, year):
    kind = YEARS[year]["tails"]
    y = 0.78

    def rear_band(x0, x1, steps):
        samples = []
        for i in range(steps + 1):
            x = x0 + (x1 - x0) * i / max(1, steps)
            hit = _cast(bvh, x, y, Z_TAIL - 0.30, Z_TAIL + 0.40)
            if hit is not None:
                samples.append(hit)
        return samples

    if kind == "bar":
        _strip(g, rear_band(-0.55, 0.55, 10), 0.012, "Lib_Taillamp", 0.004)
        return
    span = 0.28 if kind != "thin" else 0.34
    tall = 0.016 if kind == "separate" else 0.008
    for sign in (1.0, -1.0):
        _strip(g, rear_band(sign * 0.18, sign * (0.18 + span), 4), tall, "Lib_Taillamp", 0.004)


def _mirrors(g, year, lod):
    mat = YEARS[year]["mirror"]
    z = 0.72
    y = y_sill(z) + 0.015
    x_skin = skin_x(z, y)
    for sign in (1.0, -1.0):
        g.cylinder((sign * (x_skin + 0.03), y, z), 0.010, 0.06, mat, 8, axis="X")
        cap_x = x_skin + 0.045 + 0.045
        g.box((sign * cap_x, y, z), (0.09, 0.11, 0.16), mat)
        g.box((sign * (cap_x + 0.04), y, z + 0.01), (0.006, 0.07, 0.11), "Lib_SteelDark")
    del lod


def _fogs(g, bvh, year):
    mat = YEARS[year]["fog"]
    y = 0.26
    for sign in (1.0, -1.0):
        samples = _sample_band(bvh, sign * 0.38, sign * 0.58, y, 3)
        _strip(g, samples, 0.012, mat, 0.004)


def dress(g, year, lod, paint):
    del paint
    bvh = BVHTree.FromBMesh(g.bm)
    for axle in (Z_REAR, Z_FRONT):
        for sign in (-1.0, 1.0):
            _wheel(g, sign * TIRE_X, axle, year, lod)
    _grille(g, bvh, year, lod)
    _lamps_front(g, bvh, year, lod)
    _lamps_rear(g, bvh, year)
    _fogs(g, bvh, year)
    if lod < 2:
        _mirrors(g, year, lod)
    if lod == 0:
        _separate_coplanar(g.bm)


def add_colliders(asset):
    # Inside the solid hub. A wider box reaches the wheel opening.
    for i, axle in enumerate((Z_REAR, Z_FRONT)):
        for j, sign in enumerate((-1.0, 1.0)):
            asset.box(
                "Col_Wheel_%d%d" % (i, j),
                (sign * TIRE_X, AXLE_Y, axle),
                (0.006, 0.028, 0.028),
            )
    # Below the side glass. A corner up in the glass crosses the inset panel
    # and the parity test treats the cabin as outside.
    asset.box("Col_Cabin", (0.0, 0.40, -0.08), (0.44, 0.18, 0.64))
    asset.box("Col_Roof", (0.0, 1.30, -0.05), (0.28, 0.05, 0.40))
    asset.box("Col_Hood", (0.0, 0.72, 1.45), (0.40, 0.08, 0.28))
    asset.box("Col_Deck", (0.0, 0.78, -1.55), (0.46, 0.08, 0.28))


def probe_spec(name):
    ny, nz = shell._rake_normal(1.35, Z_HEADER, 1.00, Z_COWL)
    y = (1.35 + 1.00) * 0.5
    z = (Z_HEADER + Z_COWL) * 0.5
    return {
        "name": name,
        "glass_tests": [
            (0.0, y + ny * 0.04, z + nz * 0.04),
            (0.0, y - ny * 0.05, z - nz * 0.05),
        ],
    }


def _centroid(faces):
    acc = [0.0, 0.0, 0.0]
    for face in faces:
        x, y, z = _u_co(face.calc_center_median())
        acc[0] += x
        acc[1] += y
        acc[2] += z
    n = float(len(faces) or 1)
    return (acc[0] / n, acc[1] / n, acc[2] / n)


def _near_wheel(center):
    x, y, z = center
    if abs(abs(x) - TIRE_X) > 0.28:
        return False
    for axle in (Z_REAR, Z_FRONT):
        if math.hypot(y - AXLE_Y, z - axle) < TIRE_R + 0.12:
            return True
    return False


def _near_mirror(center):
    x, y, z = center
    return abs(x) > 0.85 and 0.75 < y < 1.20 and 0.35 < z < 1.15


def _near_fascia(center):
    _x, y, z = center
    return (z > 1.55 and y < 1.05) or (z < -1.55 and y < 1.05)


def _point_in_face(face, point):
    normal = face.normal
    verts = list(face.verts)
    for i in range(len(verts)):
        edge = verts[(i + 1) % len(verts)].co - verts[i].co
        if edge.cross(point - verts[i].co).dot(normal) < -1e-5:
            return False
    return True


def _separate_coplanar(bm):
    """Push the smaller face of each coplanar pair 4 mm inward so nothing z-fights."""
    moved = 0
    for _pass in range(3):
        pairs = _overlap_pairs(bm)
        if not pairs:
            break
        seen = set()
        for face, other in pairs:
            small, large = (face, other) if face.calc_area() <= other.calc_area() else (other, face)
            if small in seen:
                continue
            seen.add(small)
            normal = large.normal.normalized()
            for vert in small.verts:
                vert.co -= normal * 0.004
            moved += 1
    if moved:
        print("COPLANAR_SEPARATED", moved)
    return moved


def _overlap_pairs(bm):
    bm.faces.ensure_lookup_table()
    bm.faces.index_update()
    buckets = {}
    centers = {}
    for face in bm.faces:
        if face.calc_area() < 1e-7:
            continue
        center = face.calc_center_median()
        centers[face] = center
        key = (
            int(round(center.x * 50.0)),
            int(round(center.y * 50.0)),
            int(round(center.z * 50.0)),
        )
        buckets.setdefault(key, []).append(face)
    pairs = []
    seen = set()
    for face, center in centers.items():
        normal = face.normal
        ix = int(round(center.x * 50.0))
        iy = int(round(center.y * 50.0))
        iz = int(round(center.z * 50.0))
        owned = set(face.verts)
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for other in buckets.get((ix + dx, iy + dy, iz + dz), ()):
                        if other is face or other.index <= face.index:
                            continue
                        pair = (face.index, other.index)
                        if pair in seen:
                            continue
                        if any(vert in owned for vert in other.verts):
                            continue
                        if abs(normal.dot(other.normal)) < 0.985:
                            continue
                        other_c = centers[other]
                        dist = abs((other_c - center).dot(normal))
                        if dist > 0.0015:
                            continue
                        projected = other_c - normal * (other_c - center).dot(normal)
                        if _point_in_face(face, projected):
                            seen.add(pair)
                            pairs.append((face, other))
    return pairs


def _coplanar_overlaps(bm):
    """Pairs of faces that occupy the same patch of a plane and share no vert."""
    bm.faces.ensure_lookup_table()
    bm.faces.index_update()
    buckets = {}
    centers = {}
    for face in bm.faces:
        if face.calc_area() < 1e-7:
            continue
        center = face.calc_center_median()
        centers[face] = center
        key = (
            int(round(center.x * 50.0)),
            int(round(center.y * 50.0)),
            int(round(center.z * 50.0)),
        )
        buckets.setdefault(key, []).append(face)
    hits = 0
    seen = set()
    samples = []
    for face, center in centers.items():
        normal = face.normal
        ix = int(round(center.x * 50.0))
        iy = int(round(center.y * 50.0))
        iz = int(round(center.z * 50.0))
        owned = set(face.verts)
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for other in buckets.get((ix + dx, iy + dy, iz + dz), ()):
                        if other is face or other.index <= face.index:
                            continue
                        pair = (face.index, other.index)
                        if pair in seen:
                            continue
                        if any(vert in owned for vert in other.verts):
                            continue
                        if abs(normal.dot(other.normal)) < 0.985:
                            continue
                        other_c = centers[other]
                        dist = abs((other_c - center).dot(normal))
                        if dist > 0.0015:
                            continue
                        projected = other_c - normal * (other_c - center).dot(normal)
                        if _point_in_face(face, projected):
                            seen.add(pair)
                            hits += 1
                            if len(samples) < 6:
                                cu = _u_co(center)
                                samples.append((tuple(round(v, 3) for v in cu), round(dist, 4)))
    if samples:
        print("COPLANAR_SAMPLE", samples)
    return hits


def self_check(asset):
    """Roof, islands, silhouette, and coplanar overlaps. Prints every line."""
    bm = asset.lods[0].bm
    mats = asset.lods[0].mats
    _separate_coplanar(bm)
    over = []
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        if abs(x) > x_body(z) + 0.04:
            continue
        limit = y_crown(z) + 0.035
        if y > limit and y > 1.02:
            over.append((round(x, 3), round(y, 3), round(z, 3), round(y - y_crown(z), 3)))
    over.sort(key=lambda row: -row[3])
    print("SHELL_CHECK faces_above_roof", len(over), "worst", over[:6])

    islands = _islands(bm)
    islands.sort(key=len, reverse=True)
    counts = {"body": 0, "wheel": 0, "mirror": 0, "lamp": 0, "glass": 0, "extra": 0}
    extras = []
    for index, group in enumerate(islands):
        center = _centroid(group)
        names = set()
        for face in group:
            if 0 <= face.material_index < len(mats):
                names.add(mats[face.material_index])
        if index == 0:
            kind = "body"
        elif _near_wheel(center) or "Lib_Rubber" in names or "Lib_Steel" in names or "Lib_SteelDark" in names:
            kind = "wheel"
        elif _near_mirror(center):
            kind = "mirror"
        elif _near_fascia(center) or names & {"Lib_Headlamp", "Lib_Taillamp", "Lib_SignalAmber"}:
            kind = "lamp"
        elif names <= {GLASS}:
            kind = "glass"
        else:
            kind = "extra"
            extras.append((len(group), tuple(round(v, 3) for v in center), sorted(names)))
        counts[kind] += 1
    print(
        "SHELL_CHECK islands body=%d wheels=%d mirrors=%d lamps=%d glass=%d extra=%d" % (
            counts["body"], counts["wheel"], counts["mirror"], counts["lamp"], counts["glass"], counts["extra"],
        )
    )
    if extras:
        print("SHELL_CHECK extra_detail", extras[:12])

    overlaps = _coplanar_overlaps(bm)
    print("SHELL_CHECK coplanar_overlaps", overlaps)

    best = {}
    max_z = Z_TAIL
    min_z = Z_NOSE
    for vert in bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        max_z = max(max_z, z)
        min_z = min(min_z, z)
        if abs(x) > 0.10:
            continue
        bucket = round(z * 40.0) / 40.0
        prev = best.get(bucket)
        if prev is None or y > prev:
            best[bucket] = y
    worst = (0.0, 0.0, 0.0, 0.0)
    for bucket, mesh_y in best.items():
        if mesh_y < 0.35:
            continue
        target = y_crown(bucket)
        delta_cm = (mesh_y - target) * 100.0
        if abs(delta_cm) >= abs(worst[0]):
            worst = (delta_cm, bucket, mesh_y, target)
    rows = []
    z = Z_TAIL
    while z <= Z_NOSE + 1e-6:
        mesh_y = best.get(round(z * 40.0) / 40.0)
        if mesh_y is None:
            near = None
            for bucket, height in best.items():
                if abs(bucket - z) <= 0.03 and (near is None or height > near):
                    near = height
            mesh_y = near
        if mesh_y is not None and mesh_y > 0.35 and abs((z * 4.0) - round(z * 4.0)) < 0.04:
            target = y_crown(z)
            delta_cm = (mesh_y - target) * 100.0
            rows.append("%+.2f: mesh %.3f target %.3f d %+.1fcm" % (z, mesh_y, target, delta_cm))
        z += 0.05
    print("SHELL_CHECK silhouette max_dev_cm %.2f at z=%.2f mesh=%.3f target=%.3f" % worst)
    if abs(worst[0]) > 20.0:
        band = []
        wide = []
        for vert in bm.verts:
            x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
            if abs(z - worst[1]) > 0.06:
                continue
            if y > 0.5:
                wide.append((round(x, 3), round(y, 3), round(z, 3)))
            if abs(x) < 0.35 and y > 0.3:
                band.append((round(x, 3), round(y, 3), round(z, 3)))
        wide.sort(key=lambda row: -row[1])
        band.sort(key=lambda row: -row[1])
        print("HOLE_WIDE", wide[:8], "count", len(wide))
        print("HOLE_BAND", band[:8])
    print("SHELL_CHECK profile")
    for row in rows:
        print("  ", row)
    print(
        "SHELL_CHECK ends max_z=%.3f nose=%.3f min_z=%.3f tail=%.3f" % (
            max_z, Z_NOSE, min_z, Z_TAIL,
        )
    )
    problems = []
    if over:
        problems.append("faces above the roof")
    if counts["body"] != 1:
        problems.append("body islands %d" % counts["body"])
    if counts["extra"]:
        problems.append("extra islands %d" % counts["extra"])
    if overlaps:
        problems.append("coplanar overlaps %d" % overlaps)
    if abs(worst[0]) > 8.0:
        problems.append("silhouette deviation %.1f cm" % worst[0])
    if max_z > Z_NOSE + 0.012 or min_z < Z_TAIL - 0.012:
        problems.append("fascia past the cap")
    if problems:
        raise RuntimeError("shell check failed: " + "; ".join(problems))
    print("SHELL_CHECK ok")


def measure(asset):
    self_check(asset)
    best = {}
    max_x = 0.0
    max_y = 0.0
    min_y = 99.0
    max_z = -99.0
    min_z = 99.0
    for vert in asset.lods[0].bm.verts:
        x, y, z = blender_to_unity(vert.co.x, vert.co.y, vert.co.z)
        max_x = max(max_x, abs(x))
        max_y = max(max_y, y)
        min_y = min(min_y, y)
        max_z = max(max_z, z)
        min_z = min(min_z, z)
        if abs(x) > 0.15:
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
    print(
        "BODY_A_MEASURE", asset.name,
        "rake", None if angle is None else round(angle, 1),
        "header", None if header is None else tuple(round(v, 3) for v in header),
        "cowl", None if cowl is None else tuple(round(v, 3) for v in cowl),
        "size", round(max_x * 2.0, 3), round(max_y - min_y, 3), round(max_z - min_z, 3),
        "ymax", round(max_y, 3),
    )
    return angle
