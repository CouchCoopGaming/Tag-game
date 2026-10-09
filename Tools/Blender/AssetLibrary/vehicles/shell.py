"""Closed vehicle shells. Nothing here is registered.

Unity meters, +Y up, +Z forward. Glass is cut into the skin and set back
1.5 cm. Door gaps are 4 mm lines around two doors per side.
"""

import math

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

from _common import _point_inside, blender_to_unity, unity_to_blender


def _apply_mod(obj, name):
    if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=name)


def _box_cutter(center, size):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    sx, sy, sz = size
    bmesh.ops.scale(bm, verts=bm.verts, vec=Vector((sx, sz, sy)))
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(unity_to_blender(*center)))
    me = bpy.data.meshes.new("cutbox")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new("cutbox", me)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _well_cutter(center, radius, length):
    bm = bmesh.new()
    bmesh.ops.create_cone(
        bm, cap_ends=True, cap_tris=False, segments=16,
        radius1=radius, radius2=radius, depth=length,
    )
    bmesh.ops.rotate(bm, verts=bm.verts, cent=Vector((0, 0, 0)), matrix=Matrix.Rotation(math.pi * 0.5, 4, "Y"))
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(unity_to_blender(*center)))
    me = bpy.data.meshes.new("well")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new("well", me)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _poly_cutter(verts, faces):
    bm = bmesh.new()
    bverts = [bm.verts.new(unity_to_blender(*v)) for v in verts]
    for face in faces:
        try:
            bm.faces.new([bverts[i] for i in face])
        except ValueError:
            continue
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("cutpoly")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new("cutpoly", me)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _union_cut(obj, wells, pockets, prisms):
    """One difference against the union of every cutter.

    Sequential exact booleans on overlapping lamp and glass cuts delete the
    shell. Union first, then cut once.
    """
    cutters = []
    for center, radius, length in wells:
        cutters.append(_well_cutter(center, radius, length))
    for center, size in pockets:
        cutters.append(_box_cutter(center, size))
    for cverts, cfaces in prisms:
        cutters.append(_poly_cutter(cverts, cfaces))
    if not cutters:
        return
    acc = cutters[0]
    for i, other in enumerate(cutters[1:], 1):
        mod = acc.modifiers.new("U%d" % i, "BOOLEAN")
        mod.operation = "UNION"
        mod.solver = "EXACT"
        mod.object = other
        other_mesh = other.data
        try:
            _apply_mod(acc, "U%d" % i)
        except RuntimeError as exc:
            print("CUT_UNION_FAILED", i, exc)
            if acc.modifiers.get("U%d" % i) is not None:
                acc.modifiers.remove(mod)
        bpy.data.objects.remove(other, do_unlink=True)
        bpy.data.meshes.remove(other_mesh)
    _difference(obj, acc, "Cuts")


def _difference(obj, cut, name):
    boolean = obj.modifiers.new(name, "BOOLEAN")
    boolean.operation = "DIFFERENCE"
    boolean.solver = "EXACT"
    boolean.object = cut
    cut_mesh = cut.data
    try:
        _apply_mod(obj, name)
    except RuntimeError as exc:
        print("CUT_BOOLEAN_FAILED", name, exc)
        if obj.modifiers.get(name) is not None:
            obj.modifiers.remove(boolean)
    bpy.data.objects.remove(cut, do_unlink=True)
    bpy.data.meshes.remove(cut_mesh)


def _emit(g, verts, faces, mat, level, wells, pockets, prisms, crease_deg, hard=(), joined=False):
    bm = bmesh.new()
    bverts = [bm.verts.new(unity_to_blender(*v)) for v in verts]
    for face in faces:
        try:
            bm.faces.new([bverts[i] for i in face])
        except ValueError:
            continue
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if level:
        bm.verts.index_update()
        layer = bm.edges.layers.float.new("crease_edge")
        limit = math.radians(crease_deg)
        hard_set = {(min(a, b), max(a, b)) for a, b in hard}
        for edge in bm.edges:
            ang = edge.calc_face_angle(0.0) if len(edge.link_faces) == 2 else math.pi
            edge[layer] = 0.85 if ang > limit else 0.0
            i0 = edge.verts[0].index
            i1 = edge.verts[1].index
            if (min(i0, i1), max(i0, i1)) in hard_set:
                edge[layer] = 0.95
    me = bpy.data.meshes.new("shell")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new("shell", me)
    bpy.context.scene.collection.objects.link(obj)
    if level:
        mod = obj.modifiers.new("Sub", "SUBSURF")
        mod.levels = int(level)
        mod.render_levels = int(level)
        mod.use_creases = True
        _apply_mod(obj, "Sub")
    if joined:
        _union_cut(obj, wells, pockets, prisms)
    else:
        for i, (center, radius, length) in enumerate(wells):
            _difference(obj, _well_cutter(center, radius, length), "Well%d" % i)
        for i, (center, size) in enumerate(pockets):
            _difference(obj, _box_cutter(center, size), "Pocket%d" % i)
        for i, (cverts, cfaces) in enumerate(prisms):
            _difference(obj, _poly_cutter(cverts, cfaces), "Prism%d" % i)
    out = bmesh.new()
    out.from_mesh(obj.data)
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(me)
    bmesh.ops.recalc_face_normals(out, faces=out.faces)
    g._ingest(out, mat, 1.0)


def _loft(g, rings, mat, level, wells, pockets, prisms, crease_deg, hard=(), joined=False):
    count = len(rings[0])
    verts = [p for ring in rings for p in ring]
    faces = []

    def vid(ring_i, i):
        return ring_i * count + (i % count)

    for ring_i in range(len(rings) - 1):
        for i in range(count):
            faces.append((vid(ring_i, i), vid(ring_i, i + 1), vid(ring_i + 1, i + 1), vid(ring_i + 1, i)))
    for ring_i, flip in ((0, True), (len(rings) - 1, False)):
        acc = [0.0, 0.0, 0.0]
        for p in rings[ring_i]:
            acc[0] += p[0]
            acc[1] += p[1]
            acc[2] += p[2]
        center = tuple(v / float(count) for v in acc)
        center_i = len(verts)
        verts.append(center)
        for i in range(count):
            a = vid(ring_i, i)
            b = vid(ring_i, i + 1)
            faces.append((center_i, b, a) if flip else (center_i, a, b))
    _emit(g, verts, faces, mat, level, wells, pockets, prisms, crease_deg, hard, joined)


def _stations(z0, z1, step, extra):
    zs = set()
    z = z0
    while z < z1 - 1e-6:
        zs.add(round(z, 4))
        z += step
    zs.add(round(z1, 4))
    for z in extra:
        if z0 < z < z1:
            zs.add(round(z, 4))
    return sorted(zs)


def _lerp(keys, z):
    if z <= keys[0][0]:
        return keys[0][1], keys[0][2]
    if z >= keys[-1][0]:
        return keys[-1][1], keys[-1][2]
    for i in range(len(keys) - 1):
        z0, y0, x0 = keys[i]
        z1, y1, x1 = keys[i + 1]
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0 or 1.0)
            return y0 + (y1 - y0) * t, x0 + (x1 - x0) * t
    return keys[-1][1], keys[-1][2]


def _smooth01(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def _arch_y(z, spec):
    y = spec["rocker_y"]
    r = spec["arch_r"]
    for axle in spec["axles"]:
        dz = z - axle
        if abs(dz) < r:
            y = max(y, spec["axle_y"] + math.sqrt(r * r - dz * dz))
    return y


def _x_out(z, spec):
    _y, x_plan = _lerp(spec["keys"], z)
    x = x_plan
    reach = spec["arch_r"] + 0.05
    for axle in spec["axles"]:
        dz = abs(z - axle)
        if dz < reach:
            t = math.cos((dz / reach) * math.pi * 0.5)
            x = max(x, x_plan + spec["flare"] * t)
    return x


def _half(z, spec):
    y_top, _x_plan = _lerp(spec["keys"], z)
    x_out = _x_out(z, spec)
    w = _smooth01((y_top - (spec["belt"] + 0.08)) / 0.18)
    y_arch = min(_arch_y(z, spec), y_top - 0.06)
    y_side = min(spec["belt"], y_top - 0.04)
    y_side = max(y_side, y_arch + 0.025)
    y_side = min(y_side, y_top - 0.025)
    tumble = spec["tumble"]
    x5 = (1.0 - w) * (x_out * 0.76) + w * (x_out - tumble)
    y5 = (1.0 - w) * (y_top - 0.008) + w * min(spec["belt"] + 0.40, y_top - 0.09)
    y5 = min(max(y5, y_side + 0.012), y_top - 0.006)
    x6 = (1.0 - w) * (x_out * 0.40) + w * (x_out * 0.60)
    x6 = min(x6, x5 - 0.025)
    x6 = max(x6, 0.06)
    y6 = y_top - (0.004 * (1.0 - w) + 0.014 * w)
    y_belly = spec["belly"]
    return [
        (0.0, y_belly),
        (min(0.34, x_out * 0.40), y_belly),
        (min(0.62, x_out - 0.16), y_belly + 0.03),
        (x_out, y_arch),
        (x_out, y_side),
        (x5, y5),
        (x6, y6),
        (0.0, y_top),
    ]


def _ring(half, z):
    pts = [(x, y, z) for x, y in half]
    for x, y in reversed(half[1:-1]):
        pts.append((-x, y, z))
    return pts


def _quad(corners):
    """Eight-vertex slab. corners are the outer ring, then the inner ring."""
    return [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ], corners


def _prism_from_rake(y0, z0, y1, z1, hx0, hx1, ny, nz, outer, inner):
    def shift(x, y, z, dist):
        return (x, y + ny * dist, z + nz * dist)

    outer_ring = [
        shift(-hx0, y0, z0, outer), shift(hx0, y0, z0, outer),
        shift(hx1, y1, z1, outer), shift(-hx1, y1, z1, outer),
    ]
    inner_ring = [
        shift(-hx0, y0, z0, -inner), shift(hx0, y0, z0, -inner),
        shift(hx1, y1, z1, -inner), shift(-hx1, y1, z1, -inner),
    ]
    return outer_ring + inner_ring


def _rake_normal(y0, z0, y1, z1):
    dz, dy = z1 - z0, y1 - y0
    length = math.hypot(dz, dy) or 1.0
    # Outward is up and toward the end the rake faces. +90 deg of tangent (dz, dy).
    nz, ny = -dy / length, dz / length
    if ny < 0.0:
        nz, ny = -nz, -ny
    return ny, nz


def _pane(g, verts_outer, ny, nz, recess, thick, mat):
    def shift(p, dist):
        return (p[0], p[1] + ny * dist, p[2] + nz * dist)

    front = [shift(p, -recess) for p in verts_outer]
    back = [shift(p, -(recess + thick)) for p in verts_outer]
    g.mesh(front + back, [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ], mat)


def _side_opening(spec, z0, z1, y0, y1):
    zmid = (z0 + z1) * 0.5
    ymid = (y0 + y1) * 0.5
    half = _half(zmid, spec)
    # Skin x at the shoulder (point 5) and the belt (point 4).
    x_belt = half[4][0]
    x_shoulder = half[5][0]
    t = (ymid - half[4][1]) / ((half[5][1] - half[4][1]) or 1.0)
    t = max(0.0, min(1.0, t))
    x_skin = x_belt + (x_shoulder - x_belt) * t
    return x_skin


def _windows(spec):
    """Openings inset from the pillars so a metal frame remains."""
    h = spec["header"]
    c = spec["cowl"]
    span = math.hypot(c[1] - h[1], c[0] - h[0]) or 1.0
    uy, uz = (c[1] - h[1]) / span, (c[0] - h[0]) / span
    margin = 0.07
    ws = (
        h[1] + uy * margin, h[0] + uz * margin,
        c[1] - uy * margin, c[0] - uz * margin,
    )
    deck = spec["deck_front"]
    roof = spec["roof_rear"]
    span_b = math.hypot(deck[1] - roof[1], deck[0] - roof[0]) or 1.0
    by, bz = (deck[1] - roof[1]) / span_b, (deck[0] - roof[0]) / span_b
    bl = (
        roof[1] + by * margin, roof[0] + bz * margin,
        deck[1] - by * 0.10, deck[0] - bz * 0.10,
    )
    doors = spec["doors"]
    side = []
    for z0, z1 in doors:
        side.append((z0 + 0.08, z1 - 0.08, spec["belt"] + 0.06, spec["glass_top"]))
    return ws, bl, side


def _cutters(spec):
    pockets = []
    prisms = []
    ws, bl, side = _windows(spec)
    y0, z0, y1, z1 = ws
    ny, nz = _rake_normal(y0, z0, y1, z1)
    hx0, hx1 = spec["ws_hx"]
    prisms.append((
        _prism_from_rake(y0, z0, y1, z1, hx0[0], hx1[0], ny, nz, 0.05, 0.045),
        [
            (0, 1, 2, 3), (4, 7, 6, 5),
            (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0),
        ],
    ))
    by0, bz0, by1, bz1 = bl
    nby, nbz = _rake_normal(by0, bz0, by1, bz1)
    prisms.append((
        _prism_from_rake(by0, bz0, by1, bz1, spec["bl_hx"][0], spec["bl_hx"][1], nby, nbz, 0.05, 0.045),
        [
            (0, 1, 2, 3), (4, 7, 6, 5),
            (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0),
        ],
    ))
    spec["glass_tests"] = []
    spec["ws_pane"] = (y0, z0, y1, z1, ny, nz, hx0[1], hx1[1])
    spec["bl_pane"] = (by0, bz0, by1, bz1, nby, nbz)
    # A point 8 mm under the skin, in the pocket, ahead of the 15 mm glass.
    spec["glass_tests"].append((
        0.0,
        (y0 + y1) * 0.5 - ny * 0.008,
        (z0 + z1) * 0.5 - nz * 0.008,
    ))
    for z_a, z_b, y_a, y_b in side:
        x_skin = _side_opening(spec, z_a, z_b, y_a, y_b)
        zmid = (z_a + z_b) * 0.5
        ymid = (y_a + y_b) * 0.5
        depth = 0.16
        y_lo, y_hi = (y_a, y_b) if y_a <= y_b else (y_b, y_a)
        z_lo, z_hi = (z_a, z_b) if z_a <= z_b else (z_b, z_a)
        for sign in (1.0, -1.0):
            pockets.append((
                (sign * x_skin, (y_lo + y_hi) * 0.5, (z_lo + z_hi) * 0.5),
                (depth, y_hi - y_lo, z_hi - z_lo),
            ))
        spec["glass_tests"].append((x_skin - 0.008, ymid, zmid))
    # Handle pockets and door grooves are LOD0/LOD1. LOD2 keeps the window holes only.
    if spec.get("_lod", 0) < 2:
        for z0, z1 in spec["doors"]:
            z = (z0 + z1) * 0.5 + 0.04
            x = _x_out(z, spec)
            y = spec["belt"] - 0.06
            for sign in (1.0, -1.0):
                pockets.append(((sign * (x - 0.004), y, z), (0.045, 0.032, 0.090)))
        for center, size in _door_slots(spec):
            pockets.append((center, size))
    if spec.get("panel_gaps") and spec.get("_lod", 0) < 2:
        pockets.extend(_panel_slots(spec))
    return pockets, prisms


def _door_slots(spec):
    slots = []
    belt = spec["belt"]
    rocker = spec["rocker_y"] + 0.04
    for z0, z1 in spec["doors"]:
        x0 = _x_out(z0, spec)
        x1 = _x_out(z1, spec)
        xmid = _x_out((z0 + z1) * 0.5, spec)
        span = abs(z1 - z0)
        zmid = (z0 + z1) * 0.5
        ymid = (rocker + belt) * 0.5
        for sign in (1.0, -1.0):
            # Vertical jambs and the belt / rocker, 6 mm cutter, 4 mm visible line later.
            slots.append(((sign * (x0 - 0.004), ymid, z0), (0.016, belt - rocker, 0.006)))
            slots.append(((sign * (x1 - 0.004), ymid, z1), (0.016, belt - rocker, 0.006)))
            slots.append(((sign * (xmid - 0.004), belt, zmid), (0.016, 0.006, span)))
            slots.append(((sign * (xmid - 0.004), rocker, zmid), (0.016, 0.006, span)))
    return slots


def _door_lines(g, spec):
    belt = spec["belt"]
    rocker = spec["rocker_y"] + 0.04
    for z0, z1 in spec["doors"]:
        x0 = _x_out(z0, spec)
        x1 = _x_out(z1, spec)
        xmid = _x_out((z0 + z1) * 0.5, spec)
        span = abs(z1 - z0)
        zmid = (z0 + z1) * 0.5
        ymid = (rocker + belt) * 0.5
        height = belt - rocker
        for sign in (1.0, -1.0):
            # Outer face 1 mm inside the skin, 4 mm wide, sitting in the groove.
            g.box((sign * (x0 - 0.006), ymid, z0), (0.008, height, 0.004), "Lib_Black")
            g.box((sign * (x1 - 0.006), ymid, z1), (0.008, height, 0.004), "Lib_Black")
            g.box((sign * (xmid - 0.006), belt, zmid), (0.008, 0.004, span), "Lib_Black")
            g.box((sign * (xmid - 0.006), rocker, zmid), (0.008, 0.004, span), "Lib_Black")
    if spec.get("panel_gaps") and spec.get("_lod", 0) < 2:
        _panel_lines(g, spec)


def _glass(g, spec):
    y0, z0, y1, z1, ny, nz, hx0, hx1 = spec["ws_pane"]
    _pane(g, [(-hx0, y0, z0), (hx0, y0, z0), (hx1, y1, z1), (-hx1, y1, z1)], ny, nz, 0.015, 0.008, "Lib_TintGlass")
    _pane(g, [(-hx0 * 0.92, y0, z0), (hx0 * 0.92, y0, z0), (hx1 * 0.92, y1, z1), (-hx1 * 0.92, y1, z1)], ny, nz, 0.030, 0.006, "Lib_Black")
    by0, bz0, by1, bz1, nby, nbz = spec["bl_pane"]
    bh0, bh1 = spec["bl_hx"]
    _pane(
        g,
        [(-bh0 * 0.86, by0, bz0), (bh0 * 0.86, by0, bz0), (bh1 * 0.86, by1, bz1), (-bh1 * 0.86, by1, bz1)],
        nby, nbz, 0.015, 0.008, "Lib_TintGlass",
    )
    _pane(
        g,
        [(-bh0 * 0.78, by0, bz0), (bh0 * 0.78, by0, bz0), (bh1 * 0.78, by1, bz1), (-bh1 * 0.78, by1, bz1)],
        nby, nbz, 0.030, 0.006, "Lib_Black",
    )
    _ws, _bl, side = _windows(spec)
    for z0, z1, y0, y1 in side:
        x_skin = _side_opening(spec, z0, z1, y0, y1)
        zmid = (z0 + z1) * 0.5
        for sign in (1.0, -1.0):
            xo = sign * (x_skin - 0.015)
            xi = sign * (x_skin - 0.023)
            xb = sign * (x_skin - 0.032)
            xc = sign * (x_skin - 0.038)
            g.mesh([
                (xo, y0, z0), (xo, y0, z1), (xo, y1, z1), (xo, y1, z0),
                (xi, y0, z0), (xi, y0, z1), (xi, y1, z1), (xi, y1, z0),
            ], [
                (0, 1, 2, 3), (4, 7, 6, 5),
                (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0),
            ], "Lib_TintGlass")
            g.mesh([
                (xb, y0 + 0.02, z0 + 0.02), (xb, y0 + 0.02, z1 - 0.02),
                (xb, y1 - 0.02, z1 - 0.02), (xb, y1 - 0.02, z0 + 0.02),
                (xc, y0 + 0.02, z0 + 0.02), (xc, y0 + 0.02, z1 - 0.02),
                (xc, y1 - 0.02, z1 - 0.02), (xc, y1 - 0.02, z0 + 0.02),
            ], [
                (0, 1, 2, 3), (4, 7, 6, 5),
                (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0),
            ], "Lib_Black")
            g.box((sign * (x_skin - 0.012), spec["belt"] - 0.06, zmid + 0.04), (0.010, 0.012, 0.055), "Lib_SteelDark")


def _mirrors(g, spec, lod):
    _z_rear, z_front = spec["doors"][0]
    z = z_front - 0.18
    y = spec["belt"] + 0.05
    x = _x_out(z, spec)
    bev = 0.003 if lod == 0 else 0.0
    segs = 1 if lod == 0 else 0
    for sign in (1.0, -1.0):
        # The head ends 1.8 cm outside the skin. A longer stalk pushed the
        # full AABB past the compact width cap.
        g.box((sign * (x + 0.002), y, z), (0.016, 0.010, 0.016), "Lib_Black")
        g.box((sign * (x + 0.012), y + 0.006, z), (0.012, 0.036, 0.064), "Lib_Black", bevel=bev, segs=segs)


def _fascia(g, spec, lod):
    kind = spec.get("fascia", "classic")
    if kind == "bar":
        _fascia_bar(g, spec, lod)
        return
    if kind == "split":
        _fascia_split(g, spec, lod)
        return
    z1 = spec["z1"]
    z0 = spec["z0"]
    bev = 0.004 if lod == 0 else 0.0
    segs = 1 if lod == 0 else 0
    half = spec["width"] * 0.5
    # Lamps and bumpers end on the published face. A box past z1/z0
    # made the AABB longer than the envelope.
    for sign in (-1.0, 1.0):
        g.box((sign * spec["lamp_x"], spec["lamp_y"], z1 - 0.012), (0.30, 0.100, 0.016), "Lib_Black", bevel=bev, segs=segs)
        g.box((sign * spec["lamp_x"], spec["lamp_y"], z1 - 0.006), (0.22, 0.064, 0.008), "Lib_Headlamp")
        g.box((sign * spec["lamp_x"], spec["tail_y"], z0 + 0.012), (0.32, 0.110, 0.016), "Lib_Black", bevel=bev, segs=segs)
        g.box((sign * spec["lamp_x"], spec["tail_y"], z0 + 0.006), (0.24, 0.070, 0.008), "Lib_Taillamp")
    g.box((0.0, spec["grille_y"], z1 - 0.016), (spec["grille_w"], 0.16, 0.024), "Lib_Black")
    slats = 4 if lod == 0 else (2 if lod == 1 else 0)
    for i in range(slats):
        g.box((0.0, spec["grille_y"] - 0.055 + i * 0.032, z1 - 0.008), (spec["grille_w"] - 0.08, 0.008, 0.006), "Lib_SteelDark")
    g.box((0.0, spec["bumper_y"], z1 - 0.040), (half * 1.70, 0.12, 0.060), "Lib_Black", bevel=bev, segs=segs)
    g.box((0.0, spec["bumper_y"], z0 + 0.040), (half * 1.75, 0.14, 0.060), "Lib_Black", bevel=bev, segs=segs)
    for sign in (-1.0, 1.0):
        g.box((sign * (half * 0.78), spec["bumper_y"] + 0.01, z1 - 0.12), (0.12, 0.14, 0.22), "Lib_Black")


def _wheels(g, spec, lod):
    if spec.get("tread"):
        _wheels_tread(g, spec, lod)
        return
    seg = 16 if lod == 0 else (6 if lod >= 2 else 10)
    r = spec["tire_r"]
    half_w = spec["tire_half_w"]
    for z in spec["axles"]:
        for x in (-spec["tire_x"], spec["tire_x"]):
            g.cylinder(
                (x, spec["axle_y"], z), r, half_w * 2.0, "Lib_Rubber", seg,
                axis="X", bevel=0.012 if lod == 0 else 0.0, segs=2 if lod == 0 else 0,
            )
            sign = 1.0 if x > 0 else -1.0
            face = x + sign * (half_w + 0.010)
            g.cylinder((face, spec["axle_y"], z), r * 0.68, 0.012, "Lib_Steel", seg, axis="X")
            g.cylinder((face + sign * 0.008, spec["axle_y"], z), r * 0.20, 0.010, "Lib_SteelDark", max(8, seg // 2), axis="X")
            if lod == 0:
                for k in range(5):
                    theta = math.radians(k * 72.0 + 8.0)
                    g.box(
                        (
                            face,
                            spec["axle_y"] + math.cos(theta) * r * 0.40,
                            z + math.sin(theta) * r * 0.40,
                        ),
                        (0.010, r * 0.42, 0.016),
                        "Lib_Steel",
                        euler=(math.degrees(theta), 0, 0),
                    )



def _panel_slots(spec):
    """6 mm grooves for the hood and the trunk lid. The 4 mm line sits in them."""
    slots = []
    z_cowl = spec["cowl"][0]
    z_nose = spec["z1"] - 0.04
    if z_nose > z_cowl + 0.25:
        _edge_grooves(slots, spec, z_cowl, z_nose, 0.62, 4)
        _shut(slots, spec, z_cowl + 0.012, 0.30)
        _shut(slots, spec, z_nose, 0.26)
    z_tail = spec["z0"] + 0.07
    z_lid = spec["deck_front"][0] - 0.03
    if z_lid > z_tail + 0.18:
        _edge_grooves(slots, spec, z_tail, z_lid, 0.56, 3)
        _shut(slots, spec, z_tail, 0.26)
        _shut(slots, spec, z_lid, 0.30)
    return slots


def _edge_grooves(slots, spec, z_a, z_b, x_rel, cuts):
    if z_b < z_a:
        z_a, z_b = z_b, z_a
    for i in range(cuts):
        za = z_a + (z_b - z_a) * (i / float(cuts))
        zb = z_a + (z_b - z_a) * ((i + 1) / float(cuts))
        zm = (za + zb) * 0.5
        y, _x = _lerp(spec["keys"], zm)
        x = _x_out(zm, spec) * x_rel
        span = (zb - za) + 0.012
        for sign in (1.0, -1.0):
            slots.append(((sign * x, y - 0.004, zm), (0.006, 0.018, span)))


def _shut(slots, spec, z, half_rel):
    y, _x = _lerp(spec["keys"], z)
    half = spec["width"] * half_rel
    slots.append(((0.0, y - 0.002, z), (half * 2.0, 0.016, 0.006)))


def _panel_lines(g, spec):
    for center, size in _panel_slots(spec):
        x, y, z = center
        sx, sy, sz = size
        # 4 mm bar, 1 mm under the skin, in the 6 mm groove.
        if sx <= 0.008:
            g.box((x, y - 0.004, z), (0.004, 0.004, max(0.004, sz - 0.008)), "Lib_Black")
        else:
            g.box((x, y - 0.004, z), (max(0.004, sx - 0.02), 0.004, 0.004), "Lib_Black")


def _published_face(spec, end):
    if end == "front":
        return spec.get("pub_z1", spec["z1"])
    return spec.get("pub_z0", spec["z0"])


def _bumper_shell(g, spec, end, y, height, plate_w, lod):
    """Bumper out to the published face, with the plate sitting back in a gap."""
    face = _published_face(spec, end)
    depth = max(spec.get("cap_inset", 0.0), 0.05)
    half = spec["width"] * 0.5
    bumper_w = half * 1.72
    side_w = max(0.12, (bumper_w - plate_w) * 0.5)
    size_z = depth + 0.004
    if end == "front":
        center_z = face - size_z * 0.5
        plate_z = face - depth + 0.010
    else:
        center_z = face + size_z * 0.5
        plate_z = face + depth - 0.010
    bev = 0.003 if lod == 0 else 0.0
    segs = 1 if lod == 0 else 0
    for sign in (1.0, -1.0):
        x = sign * (plate_w * 0.5 + side_w * 0.5)
        g.box((x, y, center_z), (side_w, height, size_z), "Lib_Black", bevel=bev, segs=segs)
    g.box((0.0, y, plate_z), (plate_w - 0.016, height * 0.58, 0.010), "Lib_SteelDark")


def _fascia_bar(g, spec, lod):
    """Later fascia: a thin full-width lamp, a recessed lower grille, separate tails joined by a bar."""
    zf = _published_face(spec, "front")
    zt = _published_face(spec, "tail")
    half = spec["width"] * 0.5
    depth = max(spec.get("cap_inset", 0.0), 0.05)
    _bumper_shell(g, spec, "front", 0.19, 0.15, 0.36, lod)
    _bumper_shell(g, spec, "tail", 0.20, 0.16, 0.36, lod)
    gy, gh, gw = 0.45, 0.18, half * 1.28
    g.box((0.0, gy, zf - 0.032), (gw, gh, 0.040), "Lib_Black")
    slats = 4 if lod == 0 else (2 if lod == 1 else 0)
    for i in range(slats):
        g.box((0.0, gy - gh * 0.32 + i * (gh * 0.20), zf - 0.020), (gw - 0.10, 0.007, 0.006), "Lib_SteelDark")
    ly = spec["lamp_y"] + 0.08
    g.box((0.0, ly, zf - 0.009), (half * 1.52, 0.038, 0.016), "Lib_Black")
    g.box((0.0, ly, zf - 0.004), (half * 1.38, 0.018, 0.006), "Lib_Headlamp")
    if lod < 2:
        for sign in (1.0, -1.0):
            g.box((sign * half * 0.70, ly - 0.055, zf - 0.008), (0.20, 0.032, 0.012), "Lib_Headlamp")
            g.box((sign * half * 0.62, 0.33, zf - 0.008), (0.14, 0.040, 0.010), "Lib_SignalAmber")
    ty = spec["tail_y"]
    g.box((0.0, ty, zt + 0.009), (half * 1.58, 0.048, 0.016), "Lib_Black")
    g.box((0.0, ty, zt + 0.004), (half * 1.44, 0.022, 0.006), "Lib_Taillamp")


def _swept_lamp(g, sign, z_face, z_back, reach_in, reach_out, y, h_in, h_out, mat):
    xi = sign * reach_in
    xo = sign * reach_out
    hi = h_in * 0.5
    ho = h_out * 0.5
    lift = 0.010
    verts = [
        (xi, y - hi, z_face),
        (xi, y + hi, z_face),
        (xo, y + ho + lift, z_face),
        (xo, y - ho + lift * 0.3, z_face),
        (xi, y - hi, z_back),
        (xi, y + hi, z_back),
        (xo, y + ho + lift, z_back),
        (xo, y - ho + lift * 0.3, z_back),
    ]
    g.mesh(verts, [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ], mat)


def _fascia_split(g, spec, lod):
    """Earlier fascia: separate swept lamps, a taller grille, separate tail lamps."""
    zf = _published_face(spec, "front")
    zt = _published_face(spec, "tail")
    half = spec["width"] * 0.5
    _bumper_shell(g, spec, "front", 0.175, 0.13, 0.34, lod)
    _bumper_shell(g, spec, "tail", 0.19, 0.15, 0.34, lod)
    # Horizontal lower intake, recessed between the bumper and the grille.
    g.box((0.0, 0.30, zf - 0.018), (half * 1.20, 0.045, 0.016), "Lib_Black")
    gy, gh, gw = 0.50, 0.24, half * 1.05
    g.box((0.0, gy, zf - 0.030), (gw, gh, 0.036), "Lib_Black")
    slats = 5 if lod == 0 else (3 if lod == 1 else 1)
    for i in range(slats):
        g.box((0.0, gy - gh * 0.36 + i * (gh * 0.16), zf - 0.018), (gw - 0.08, 0.007, 0.006), "Lib_SteelDark")
    y = spec["lamp_y"] + 0.02
    if lod < 2:
        for sign in (1.0, -1.0):
            _swept_lamp(g, sign, zf - 0.002, zf - 0.020, half * 0.28, half * 0.86, y, 0.050, 0.090, "Lib_Black")
            _swept_lamp(g, sign, zf - 0.001, zf - 0.012, half * 0.32, half * 0.80, y, 0.030, 0.058, "Lib_Headlamp")
            _swept_lamp(g, sign, zt + 0.002, zt + 0.018, half * 0.32, half * 0.88, spec["tail_y"], 0.055, 0.100, "Lib_Black")
            _swept_lamp(g, sign, zt + 0.001, zt + 0.010, half * 0.36, half * 0.82, spec["tail_y"], 0.032, 0.064, "Lib_Taillamp")
    else:
        for sign in (1.0, -1.0):
            g.box((sign * half * 0.62, y, zf - 0.008), (0.34, 0.08, 0.014), "Lib_Headlamp")
            g.box((sign * half * 0.62, spec["tail_y"], zt + 0.008), (0.36, 0.09, 0.014), "Lib_Taillamp")


def _torus_x(g, center, major, minor, mat, major_seg, minor_seg):
    """Ring around the Unity X axis. Used for a sidewall beside the tread."""
    cx, cy, cz = center
    rings = []
    verts = []
    for i in range(major_seg):
        a = 2.0 * math.pi * i / major_seg
        ca, sa = math.cos(a), math.sin(a)
        ring = []
        for j in range(minor_seg):
            b = 2.0 * math.pi * j / minor_seg
            cb, sb = math.cos(b), math.sin(b)
            ring.append(len(verts))
            verts.append((
                cx + minor * cb,
                cy + (major + minor * sb) * ca,
                cz + (major + minor * sb) * sa,
            ))
        rings.append(ring)
    faces = []
    for i in range(major_seg):
        ni = (i + 1) % major_seg
        for j in range(minor_seg):
            nj = (j + 1) % minor_seg
            faces.append((rings[i][j], rings[ni][j], rings[ni][nj], rings[i][nj]))
    g.mesh(verts, faces, mat)


def _wheels_tread(g, spec, lod):
    """One rubber carcass, sidewall rings outside it, tread blocks outside the carcass."""
    seg = 18 if lod == 0 else (12 if lod == 1 else 8)
    r = spec["tire_r"]
    half_w = spec["tire_half_w"]
    carcass_w = half_w * 2.0 * 0.78
    spokes = int(spec.get("spokes", 5))
    rim_r = r * spec.get("rim_ratio", 0.62)
    blocks = 12 if lod == 0 else 0
    for z in spec["axles"]:
        for x in (-spec["tire_x"], spec["tire_x"]):
            # Carcass sits just inside the contact radius so tread blocks land on y=0.
            g.cylinder(
                (x, spec["axle_y"], z), r * 0.955, carcass_w, "Lib_Rubber", seg, axis="X",
            )
            sign = 1.0 if x > 0.0 else -1.0
            if lod == 0:
                wall_x = half_w * 0.78 + 0.018
                _torus_x(
                    g, (x + sign * wall_x, spec["axle_y"], z),
                    r * 0.90, 0.014, "Lib_Rubber", 12, 6,
                )
                _torus_x(
                    g, (x - sign * wall_x, spec["axle_y"], z),
                    r * 0.90, 0.014, "Lib_Rubber", 12, 6,
                )
            face = x + sign * (half_w + 0.010)
            g.cylinder((face, spec["axle_y"], z), rim_r, 0.012, "Lib_Steel", seg, axis="X")
            g.cylinder((face + sign * 0.006, spec["axle_y"], z), rim_r * 0.28, 0.010, "Lib_SteelDark", max(8, seg // 2), axis="X")
            if lod < 2:
                count = spokes if lod == 0 else max(5, spokes // 2)
                for k in range(count):
                    theta = math.radians(k * (360.0 / count) + 6.0)
                    g.box(
                        (
                            face,
                            spec["axle_y"] + math.cos(theta) * rim_r * 0.55,
                            z + math.sin(theta) * rim_r * 0.55,
                        ),
                        (0.008, rim_r * 0.62, 0.014 if count >= 8 else 0.022),
                        "Lib_Steel",
                        euler=(math.degrees(theta), 0, 0),
                    )
            if blocks:
                for i in range(blocks):
                    theta = math.radians(i * (360.0 / blocks) + 4.0)
                    for rib in (-0.45, 0.45):
                        rad = r - 0.006
                        g.box(
                            (
                                x + rib * carcass_w * 0.55,
                                spec["axle_y"] + math.cos(theta) * rad,
                                z + math.sin(theta) * rad,
                            ),
                            (0.016, 0.010, 0.036),
                            "Lib_Rubber",
                            euler=(math.degrees(theta), 0, 0),
                        )


def _wipers(g, spec):
    y0, z0 = spec["cowl"][1], spec["cowl"][0]
    dy = 0.34
    dz = -dy * math.tan(math.radians(60.0))
    # Parked at the cowl, lying on the 60-degree glass.
    g.pipe((-0.02, y0 + 0.015, z0 - 0.02), (-0.36, y0 + 0.015 + dy, z0 - 0.02 + dz), 0.005, "Lib_Black", segments=5)
    g.pipe((0.10, y0 + 0.012, z0 - 0.015), (0.42, y0 + 0.012 + dy * 0.92, z0 - 0.015 + dz * 0.92), 0.005, "Lib_Black", segments=5)


def _cabin_block(g, spec):
    """Seat backs and a dash in the greenhouse, above the cabin collider."""
    cowl_z, cowl_y = spec["cowl"]
    width = spec["width"]
    g.box((0.0, max(0.96, cowl_y - 0.02), cowl_z - 0.22), (width * 0.58, 0.14, 0.22), "Lib_Interior")
    g.box((0.0, max(1.02, cowl_y + 0.04), cowl_z - 0.16), (width * 0.42, 0.035, 0.10), "Lib_Black")
    front_z = spec["doors"][0][0] - 0.20
    for x in (-0.34, 0.34):
        g.box((x, 0.96, front_z), (0.30, 0.22, 0.07), "Lib_Interior")
        g.box((x, 1.10, front_z + 0.02), (0.16, 0.08, 0.06), "Lib_Black")
    rear_z = spec["doors"][1][0] + 0.18
    g.box((0.0, 0.94, rear_z), (width * 0.52, 0.18, 0.08), "Lib_Interior")


def _wells(spec):
    radius = spec["tire_r"] + 0.028
    length = spec["tire_half_w"] * 2.0 + 0.28
    return [((sign * spec["tire_x"], spec["axle_y"], z), radius, length) for z in spec["axles"] for sign in (-1.0, 1.0)]


def build_sedan(g, spec, lod):
    spec["_lod"] = lod
    level = 1 if lod == 0 else 0
    # LOD2 at most 0.6x LOD1. The cage stays; only the far shell is coarser.
    step = 0.08 if lod == 0 else (0.64 if lod >= 2 else 0.16)
    extra = [k[0] for k in spec["keys"]]
    extra.extend(spec["axles"])
    for axle in spec["axles"]:
        extra.extend((axle - spec["arch_r"], axle + spec["arch_r"]))
    zs = _stations(spec["z0"], spec["z1"], step, extra)
    rings = [_ring(_half(z, spec), z) for z in zs]
    pockets, prisms = _cutters(spec)
    _loft(g, rings, spec["paint"], level, _wells(spec), pockets, prisms, 42.0)
    _glass(g, spec)
    if lod < 2:
        _door_lines(g, spec)
    _mirrors(g, spec, lod)
    _fascia(g, spec, lod)
    if spec.get("cladding"):
        z_a = spec["axles"][0] + spec["arch_r"] + 0.08
        z_b = spec["axles"][1] - spec["arch_r"] - 0.08
        x = spec["width"] * 0.5 * 0.98
        zmid = (z_a + z_b) * 0.5
        for sign in (-1.0, 1.0):
            g.box((sign * (x + 0.012), 0.40, zmid), (0.028, 0.20, z_b - z_a), "Lib_SteelDark")
    _wheels(g, spec, lod)
    if spec.get("wipers") and lod == 0:
        _wipers(g, spec)
    if spec.get("cabin") and lod < 2:
        _cabin_block(g, spec)


def add_sedan_colliders(asset, spec):
    z0, z1 = spec["z0"], spec["z1"]
    r = spec["tire_r"]
    x = spec["tire_x"] + spec["tire_half_w"] * 0.12
    for i, z in enumerate(spec["axles"]):
        for j, sign in enumerate((-1.0, 1.0)):
            # Tread cars keep the slab deeper inside the carcass. The corner of a
            # 0.96 box still fits, but a smaller one survives a ray that grazes the bead.
            yz = r * (0.80 if spec.get("tread") else 0.96)
            asset.box("Col_Wheel_%d%d" % (i, j), (sign * x, spec["axle_y"], z), (0.016, yz, yz))
    cabin_z = (spec["doors"][0][1] + spec["doors"][1][0]) * 0.5
    asset.box("Col_Cabin", (0.0, 0.55, cabin_z), (spec["width"] * 0.62, 0.46, spec["wheelbase"] * 0.42))
    asset.box("Col_Roof", (0.0, spec["height"] - 0.09, spec["roof_z"]), (spec["width"] * 0.36, 0.036, spec["roof_len"]))
    asset.box("Col_Hood", (0.0, spec["hood_col_y"], spec["hood_z"]), (spec["width"] * 0.46, spec["hood_col_h"], spec["hood_len"]))
    asset.box("Col_Deck", (0.0, spec["deck_col_y"], spec["deck_z"]), (spec["width"] * 0.48, spec["deck_col_h"], spec["deck_len"]))


def probe_sedan(asset, spec):
    """Print the silhouette and whether the glass pockets are actually open."""
    ys = {}
    for v in asset.lods[0].bm.verts:
        x, y, z = blender_to_unity(v.co.x, v.co.y, v.co.z)
        if abs(x) < 0.04:
            bucket = round(z * 5.0) / 5.0
            ys[bucket] = max(ys.get(bucket, 0.0), y)
    samples = []
    for z in sorted(ys):
        samples.append("%+.2f:%.3f" % (z, ys[z]))
    print("SEDAN_CENTERLINE", spec["name"], " ".join(samples))
    bvh = BVHTree.FromBMesh(asset.lods[0].bm)
    for i, (x, y, z) in enumerate(spec.get("glass_tests", [])):
        inside = _point_inside(bvh, Vector(unity_to_blender(x, y, z)))
        print("GLASS_POCKET", spec["name"], i, "INSIDE" if inside else "OPEN", "at", round(x, 3), round(y, 3), round(z, 3))


def make_sedan(
    length, width, height, wheelbase, track, front_overhang,
    tire_radius, tire_width, nose_y, paint, name,
    deck_ratio=0.745, roof_span=0.22, cowl_setback=0.38, belly=0.145,
    c_pillar_deg=58.0, cap_inset=0.0, tumble=0.042,
    fascia="classic", spokes=5, tread=False, panel_gaps=False,
    cabin=False, wipers=False, rim_ratio=0.62,
):
    """Family-sedan silhouette. A-pillar is 60 degrees from vertical by construction."""
    pub_z0 = -length * 0.5
    pub_z1 = length * 0.5
    # The bumper face is the published end. The loft stops one bumper-depth short.
    z0 = pub_z0 + cap_inset
    z1 = pub_z1 - cap_inset
    z_front = pub_z1 - front_overhang
    z_rear = z_front - wheelbase
    belt = height * 0.63
    roof = height
    cowl_z = z_front - cowl_setback
    cowl_y = nose_y + (roof * 0.66 - nose_y) * 0.55
    # Keep the cowl under the roof and above the belt.
    cowl_y = min(max(cowl_y, belt + 0.02), roof - 0.42)
    header_y = roof - 0.085
    dy = header_y - cowl_y
    dz = dy * math.tan(math.radians(60.0))
    header_z = cowl_z - dz
    roof_rear_y = roof - 0.055
    deck_y = roof * deck_ratio
    c_dy = roof_rear_y - deck_y
    c_dz = c_dy * math.tan(math.radians(c_pillar_deg))
    roof_rear_z = header_z - (length * roof_span)
    # Keep a real roof length, then drop the C-pillar to the deck.
    if roof_rear_z > header_z - 0.70:
        roof_rear_z = header_z - 0.70
    deck_front_z = roof_rear_z - c_dz
    arch_r = tire_radius + 0.07
    half = width * 0.5

    def roof_y(z):
        if z <= roof_rear_z:
            t = (z - deck_front_z) / ((roof_rear_z - deck_front_z) or 1.0)
            t = max(0.0, min(1.0, t))
            if z <= deck_front_z:
                u = (z - z0) / ((deck_front_z - z0) or 1.0)
                return (deck_y - 0.03) + (deck_y - (deck_y - 0.03)) * max(0.0, min(1.0, u))
            return deck_y + (roof_rear_y - deck_y) * t
        if z >= header_z:
            if z >= cowl_z:
                t = (z - cowl_z) / ((z1 - cowl_z) or 1.0)
                return cowl_y + (nose_y - cowl_y) * max(0.0, min(1.0, t))
            t = (z - header_z) / ((cowl_z - header_z) or 1.0)
            return header_y + (cowl_y - header_y) * t
        t = (z - roof_rear_z) / ((header_z - roof_rear_z) or 1.0)
        base = roof_rear_y + (header_y - roof_rear_y) * t
        # The cage sits a little high so subdivision lands on the published roof height.
        return base + math.sin(math.pi * t) * (roof + 0.004 - base)

    def plan_x(z):
        knots = (
            (z0, half * 0.86),
            (z0 + 0.35, half * 0.94),
            (z_rear, half * 0.98),
            ((z_rear + z_front) * 0.5, half),
            (z_front, half * 0.97),
            (cowl_z, half * 0.95),
            (z1 - 0.25, half * 0.84),
            (z1, half * 0.78),
        )
        _y, x = _lerp([(kz, 0.0, kx) for kz, kx in knots], z)
        return x

    knots_z = [z0]
    z = z0
    while z < z1 - 1e-6:
        z += 0.16
        knots_z.append(min(z, z1))
    for z in (deck_front_z, roof_rear_z, header_z, cowl_z, z_rear, z_front, z1):
        knots_z.append(z)
    keys = []
    for z in sorted(set(round(v, 4) for v in knots_z)):
        keys.append((z, round(roof_y(z), 4), round(plan_x(z), 4)))

    front_jamb = z_front - arch_r - 0.02
    b_pillar = (z_front + z_rear) * 0.5
    rear_jamb = z_rear + arch_r + 0.04
    # Two doors. The B-pillar is the shared edge, with a 4 mm gap between the lines.
    # Store each door as (rear_z, front_z) so spans stay positive.
    doors = (
        (b_pillar + 0.004, front_jamb),
        (rear_jamb, b_pillar - 0.004),
    )
    hood_z = (cowl_z + z1) * 0.5
    hood_chord = cowl_y + (nose_y - cowl_y) * ((hood_z - cowl_z) / ((z1 - cowl_z) or 1.0))
    deck_z = (z0 + deck_front_z) * 0.5
    spec = {
        "name": name,
        "paint": paint,
        "z0": z0,
        "z1": z1,
        "length": length,
        "width": width,
        "height": roof,
        "wheelbase": wheelbase,
        "axles": (z_rear, z_front),
        "axle_y": tire_radius,
        "tire_r": tire_radius,
        "tire_x": track * 0.5,
        "tire_half_w": tire_width * 0.5,
        "arch_r": arch_r,
        "flare": 0.018,
        "belt": belt,
        "belly": belly,
        "rocker_y": 0.30,
        "tumble": tumble,
        "pub_z0": pub_z0,
        "pub_z1": pub_z1,
        "cap_inset": cap_inset,
        "fascia": fascia,
        "spokes": spokes,
        "tread": tread,
        "panel_gaps": panel_gaps,
        "cabin": cabin,
        "wipers": wipers,
        "rim_ratio": rim_ratio,
        "keys": keys,
        "header": (header_z, header_y),
        "cowl": (cowl_z, cowl_y),
        "roof_rear": (roof_rear_z, roof_rear_y),
        "deck_front": (deck_front_z, deck_y),
        "doors": doors,
        "glass_top": min(roof - 0.16, belt + 0.38),
        "ws_hx": ((half * 0.62, half * 0.52), (half * 0.74, half * 0.62)),
        "bl_hx": (half * 0.58, half * 0.70),
        "lamp_x": half * 0.62,
        "lamp_y": nose_y - 0.10,
        "tail_y": deck_y * 0.72,
        "grille_y": 0.36,
        "grille_w": half * 1.15,
        "bumper_y": 0.20,
        "hood_z": hood_z,
        "hood_len": (z1 - cowl_z) * 0.28,
        "hood_col_y": hood_chord - 0.18,
        "hood_col_h": 0.08,
        "deck_z": deck_z,
        "deck_len": (deck_front_z - z0) * 0.36,
        "deck_col_y": 0.56,
        "deck_col_h": 0.14,
        "roof_z": (roof_rear_z + header_z) * 0.5,
        "roof_len": (header_z - roof_rear_z) * 0.55,
        "apillar_deg": math.degrees(math.atan2(dz, dy)),
    }
    print(
        "SEDAN_SPEC", name,
        "L", round(length, 3), "W", round(width, 3), "H", round(height, 3),
        "WB", round(wheelbase, 3),
        "A", round(spec["apillar_deg"], 2),
        "header", tuple(round(v, 3) for v in spec["header"]),
        "cowl", tuple(round(v, 3) for v in spec["cowl"]),
        "roof_rear", tuple(round(v, 3) for v in spec["roof_rear"]),
        "deck", tuple(round(v, 3) for v in spec["deck_front"]),
        "doors", tuple((round(a, 3), round(b, 3)) for a, b in doors),
    )
    return spec
