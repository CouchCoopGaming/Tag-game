"""Closed car shells shared by the street-kit cars. Nothing is registered here."""

import math

import bmesh
import bpy
from mathutils import Matrix, Vector

from _common import unity_to_blender


def _apply_mod(obj, name):
    if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=name)


def _box_cutter(center, size):
    """Closed box in Unity meters, used to open a shallow door-handle pocket."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    sx, sy, sz = size
    bmesh.ops.scale(bm, verts=bm.verts, vec=Vector((sx, sz, sy)))
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(unity_to_blender(*center)))
    me = bpy.data.meshes.new("pocket")
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new("pocket", me)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def _well_cutter(center, radius, length):
    """Closed cylinder along Unity X, used to open a wheel well."""
    bm = bmesh.new()
    bmesh.ops.create_cone(
        bm, cap_ends=True, cap_tris=False, segments=20,
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


def _emit_smooth(g, verts, faces, mat, level, wells, pockets=()):
    """Subdivide the shell, crease panel edges, then cut the wheel openings."""
    bm = bmesh.new()
    bverts = [bm.verts.new(unity_to_blender(*v)) for v in verts]
    for face in faces:
        try:
            bm.faces.new([bverts[i] for i in face])
        except ValueError:
            continue
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if level:
        layer = bm.edges.layers.float.new("crease_edge")
        for edge in bm.edges:
            ang = edge.calc_face_angle(0.0) if len(edge.link_faces) == 2 else math.pi
            edge[layer] = 0.9 if ang > math.radians(32.0) else 0.0
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
    for center, radius, length in wells:
        cut = _well_cutter(center, radius, length)
        boolean = obj.modifiers.new("Well", "BOOLEAN")
        boolean.operation = "DIFFERENCE"
        boolean.solver = "EXACT"
        boolean.object = cut
        cut_mesh = cut.data
        try:
            _apply_mod(obj, "Well")
        except RuntimeError as exc:
            print("WELL_BOOLEAN_FAILED", exc)
            if obj.modifiers.get("Well") is not None:
                obj.modifiers.remove(boolean)
        bpy.data.objects.remove(cut, do_unlink=True)
        bpy.data.meshes.remove(cut_mesh)
    for center, size in pockets:
        cut = _box_cutter(center, size)
        boolean = obj.modifiers.new("Pocket", "BOOLEAN")
        boolean.operation = "DIFFERENCE"
        boolean.solver = "EXACT"
        boolean.object = cut
        cut_mesh = cut.data
        try:
            _apply_mod(obj, "Pocket")
        except RuntimeError as exc:
            print("POCKET_BOOLEAN_FAILED", exc)
            if obj.modifiers.get("Pocket") is not None:
                obj.modifiers.remove(boolean)
        bpy.data.objects.remove(cut, do_unlink=True)
        bpy.data.meshes.remove(cut_mesh)
    out = bmesh.new()
    out.from_mesh(obj.data)
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(me)
    bmesh.ops.recalc_face_normals(out, faces=out.faces)
    g._ingest(out, mat, 1.0)


def _wells(spec, axles):
    radius = spec["tire_r"] + 0.03
    length = spec["tire_half_w"] * 2.0 + 0.30
    return [((sign * spec["tire_x"], spec["axle_y"], z), radius, length) for z in axles for sign in (-1.0, 1.0)]


def _loft(g, rings, mat, smooth=0, wells=(), pockets=()):
    """Solid loft through closed rings of equal length. Ends are capped."""
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
    if smooth or wells:
        _emit_smooth(g, verts, faces, mat, smooth, wells, pockets)
    else:
        g.mesh(verts, faces, mat)


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
    """keys are (z, y, x) sorted by z."""
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


def _arch_lift(z, axles, axle_y, arch_r, rocker_y):
    y = rocker_y
    for axle in axles:
        dz = z - axle
        if abs(dz) < arch_r:
            y = max(y, axle_y + math.sqrt(arch_r * arch_r - dz * dz))
    return y


def _flare(z, spec, axles):
    x = spec["body_x"]
    reach = spec["arch_r"] + 0.08
    for axle in axles:
        dz = abs(z - axle)
        if dz < reach:
            t = math.cos((dz / reach) * math.pi * 0.5)
            x = max(x, spec["body_x"] + spec["flare"] * t)
    return x


def _smooth01(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def _pillar_weight(z, spec):
    """1 on a pillar, 0 in open glass, blended over 10 cm so the roof rail stays smooth."""
    fade = 0.10

    def edge(dist_inside):
        if dist_inside >= 0.0:
            return 1.0
        return _smooth01(1.0 + dist_inside / fade)

    weight = edge(z - spec["a_pillar_z"])
    weight = max(weight, edge(spec["c_pillar_z"] - z))
    for pz, half in spec["pillars"]:
        weight = max(weight, edge(half - abs(z - pz)))
    return weight


def _half(z, spec, axles):
    crown_y, crown_x = _lerp(spec["crown"], z)
    x_out = _flare(z, spec, axles)
    belt = min(spec["belt_y"], crown_y - 0.05)
    y_arch = min(_arch_lift(z, axles, spec["axle_y"], spec["arch_r"], spec["rocker_y"]), belt - 0.035)
    greenhouse = crown_y > spec["belt_y"] + 0.18 and spec["roof_z"][0] - 0.08 <= z <= spec["roof_z"][1] + 0.15
    rail_x = min(max(crown_x, 0.10), x_out - 0.035)
    y_rail = max(belt + 0.035, crown_y - 0.012)
    # The roof is the crown itself. A cut valley here pinched after subdivision.
    top = crown_y + (spec["roof_crown"] if greenhouse else 0.0)
    if top < y_rail + 0.01:
        top = y_rail + 0.01
    y_mid = (belt + y_rail) * 0.5
    # Full inset in the glass, nearly none on a pillar, with a long blend between.
    weight = _pillar_weight(z, spec) if greenhouse else 1.0
    inset = spec["inset"] * (1.0 - weight) + 0.012 * weight
    x_win = x_out - inset
    x_win = min(x_win, x_out - 0.004)
    x_win = max(x_win, rail_x + 0.02)
    return [
        (0.0, spec["belly_y"]),
        (0.36, spec["belly_y"]),
        (min(0.62, x_out - 0.16), spec["belly_y"] + 0.025),
        (x_out, y_arch),
        (x_out, belt),
        (x_win, y_mid),
        (rail_x, y_rail),
        (0.0, top),
    ]


def _ring(half, z):
    pts = [(x, y, z) for x, y in half]
    for x, y in reversed(half[1:-1]):
        pts.append((-x, y, z))
    return pts


def _shell(g, spec, step, mat, axles, level):
    extra = [k[0] for k in spec["crown"]]
    extra.extend((spec["a_pillar_z"], spec["c_pillar_z"]))
    extra.extend((spec["windshield"][0], spec["windshield"][2], spec["rear_glass"][0], spec["rear_glass"][2]))
    extra.append((spec["windshield"][0] + spec["windshield"][2]) * 0.5)
    for pz, half in spec["pillars"]:
        extra.extend((pz - half - 0.10, pz - half, pz, pz + half, pz + half + 0.10))
    extra.extend((spec["a_pillar_z"] - 0.10, spec["c_pillar_z"] + 0.10))
    for axle in axles:
        extra.extend((axle - spec["arch_r"], axle, axle + spec["arch_r"]))
    zs = _stations(spec["z0"], spec["z1"], step, extra)
    rings = [_ring(_half(z, spec, axles), z) for z in zs]
    _loft(g, rings, mat, smooth=level, wells=_wells(spec, axles), pockets=_pockets(spec))


def _slab(g, y0, z0, y1, z1, half_x0, half_x1, thick, mat):
    dy, dz = y1 - y0, z1 - z0
    norm = math.hypot(dy, dz) or 1.0
    ny, nz = (-dy / norm) * thick, (dz / norm) * thick
    verts = []
    for oy, oz in ((0.0, 0.0), (ny, nz)):
        verts.append((-half_x0, y0 + oy, z0 + oz))
        verts.append((half_x0, y0 + oy, z0 + oz))
        verts.append((half_x1, y1 + oy, z1 + oz))
        verts.append((-half_x1, y1 + oy, z1 + oz))
    g.mesh(verts, [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ], mat)


def _side_glass(g, spec, z0, z1, sign, mat):
    """One pane in the recess. The top stays under the drip rail so it cannot cut the roof."""
    y0 = spec["belt_y"] + 0.05
    zmid = (z0 + z1) * 0.5
    crown_y, _crown_x = _lerp(spec["crown"], zmid)
    y1 = min(spec["roof_y"] - 0.14, crown_y - 0.06)
    x_out = _flare(zmid, spec, spec["axles"])
    x_bot = x_out - spec["inset"] + 0.03
    x_top = min(spec["roof_x"] - 0.02, x_bot - 0.04)
    if y1 < y0 + 0.12:
        y1 = y0 + 0.12
    thick = 0.010
    verts = []
    for dx in (-thick * 0.5, thick * 0.5):
        for z, y, x in ((z0, y0, x_bot), (z1, y0, x_bot), (z1, y1, x_top), (z0, y1, x_top)):
            verts.append((sign * (x + dx), y, z))
    faces = [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ]
    g.mesh(verts, faces, mat)
    # Rubber surround just outboard of the pane, still under the drip rail.
    z_lo, z_hi = (z0, z1) if z0 <= z1 else (z1, z0)
    z_mid = (z_lo + z_hi) * 0.5
    z_span = max(0.08, z_hi - z_lo)
    x_seal = x_bot + 0.010
    g.box((sign * x_seal, y0 + 0.008, z_mid), (0.012, 0.016, z_span + 0.016), "Lib_Black")
    g.box((sign * (x_top + 0.008), y1 - 0.006, z_mid), (0.012, 0.014, z_span + 0.016), "Lib_Black")
    x_pillar = (x_bot + x_top) * 0.5 + 0.006
    for z_edge in (z_lo - 0.004, z_hi + 0.004):
        g.box((sign * x_pillar, (y0 + y1) * 0.5, z_edge), (0.014, (y1 - y0) + 0.012, 0.014), "Lib_Black")


def _rake_inset(y0, z0, y1, z1, hx0, hx1, margin):
    dy, dz = y1 - y0, z1 - z0
    norm = math.hypot(dy, dz) or 1.0
    uy, uz = dy / norm, dz / norm
    return (
        y0 + uy * margin, z0 + uz * margin,
        y1 - uy * margin, z1 - uz * margin,
        max(0.08, hx0 - margin), max(0.08, hx1 - margin),
        (-dy / norm, dz / norm),
    )


def _beveled_pane(g, y0, z0, y1, z1, hx0, hx1, thick, mat, bevel):
    """Front face smaller than the back face, so the edge reads as a bevel."""
    y0f, z0f, y1f, z1f, hx0f, hx1f, (ny, nz) = _rake_inset(y0, z0, y1, z1, hx0, hx1, bevel)
    verts = [
        (-hx0, y0, z0), (hx0, y0, z0), (hx1, y1, z1), (-hx1, y1, z1),
        (-hx0f, y0f + ny * thick, z0f + nz * thick),
        (hx0f, y0f + ny * thick, z0f + nz * thick),
        (hx1f, y1f + ny * thick, z1f + nz * thick),
        (-hx1f, y1f + ny * thick, z1f + nz * thick),
    ]
    g.mesh(verts, [
        (0, 1, 2, 3),
        (4, 7, 6, 5),
        (0, 4, 5, 1),
        (1, 5, 6, 2),
        (2, 6, 7, 3),
        (3, 7, 4, 0),
    ], mat)


def _rubber_frame(g, y0, z0, y1, z1, hx0, hx1, margin):
    """Even black surround. One width on every edge, proud of the skin."""
    y0i, z0i, y1i, z1i, hx0i, hx1i, (ny, nz) = _rake_inset(y0, z0, y1, z1, hx0, hx1, margin)
    thick = 0.014
    outer = [(-hx0, y0, z0), (hx0, y0, z0), (hx1, y1, z1), (-hx1, y1, z1)]
    inner = [(-hx0i, y0i, z0i), (hx0i, y0i, z0i), (hx1i, y1i, z1i), (-hx1i, y1i, z1i)]
    verts = []
    for ring in (outer, inner):
        for x, y, z in ring:
            verts.append((x, y, z))
        for x, y, z in ring:
            verts.append((x, y + ny * thick, z + nz * thick))
    # 0-3 outer back, 4-7 outer front, 8-11 inner back, 12-15 inner front.
    g.mesh(verts, [
        (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0),
        (8, 9, 13, 12), (9, 10, 14, 13), (10, 11, 15, 14), (11, 8, 12, 15),
        (4, 12, 13, 5), (5, 13, 14, 6), (6, 14, 15, 7), (7, 15, 12, 4),
        (0, 1, 9, 8), (1, 2, 10, 9), (2, 3, 11, 10), (3, 0, 8, 11),
    ], "Lib_Black")


def _raked(g, spec, z0, z1):
    """Pane proud of a smooth roof, with the same rubber margin on every edge."""
    span = abs(z1 - z0)
    if span < 0.12:
        return
    ya, xa = _lerp(spec["crown"], z0)
    yb, xb = _lerp(spec["crown"], z1)
    # Sit just off the sheet so the pane does not share a face with the roof.
    ya += 0.010
    yb += 0.010
    hx0 = max(0.22, xa - 0.05)
    hx1 = max(0.22, xb - 0.05)
    margin = 0.022
    _rubber_frame(g, ya, z0, yb, z1, hx0, hx1, margin)
    y0, z0i, y1, z1i, hx0i, hx1i, _ = _rake_inset(ya, z0, yb, z1, hx0, hx1, margin)
    _beveled_pane(g, y0, z0i, y1, z1i, hx0i, hx1i, 0.008, "Lib_TintGlass", 0.006)


def _cabin(g, spec):
    """Dark cabin behind the tint, inboard of the glass so the two meshes do not meet."""
    z0, z1 = spec["roof_z"]
    if z1 < z0:
        z0, z1 = z1, z0
    span = max(0.36, (z1 - z0) * 0.62)
    half_x = max(0.28, spec["roof_x"] - 0.16)
    g.box(
        (0.0, spec["belt_y"] + 0.18, (z0 + z1) * 0.5),
        (half_x * 2.0, 0.22, span),
        "Lib_Interior",
    )


def _glass(g, spec):
    hz, _hy, cz, _cy = spec["windshield"]
    _raked(g, spec, hz, cz)
    if spec.get("bed_z0") is not None:
        _ry0, _ry1 = spec["rear_glass"][1], spec["rear_glass"][3]
        z_cap = spec["z0"]
        _slab(
            g, _ry0, z_cap + 0.018, _ry1, z_cap - 0.010,
            spec["roof_x"] - 0.08, spec["roof_x"] - 0.05,
            0.012, "Lib_TintGlass",
        )
    else:
        rz0, _ry0, rz1, _ry1 = spec["rear_glass"]
        _raked(g, spec, rz0, rz1)
    for z0, z1 in spec["windows"]:
        lo, hi = (z0, z1) if z0 <= z1 else (z1, z0)
        for sign in (1.0, -1.0):
            _side_glass(g, spec, lo + 0.02, hi - 0.02, sign, "Lib_TintGlass")


def _seams(g, spec):
    cuts = [spec["a_pillar_z"], spec["c_pillar_z"]]
    for pz, half in spec["pillars"]:
        cuts.append(pz - half)
        cuts.append(pz + half)
    for z in cuts:
        if not (spec["z0"] + 0.3 < z < spec["z1"] - 0.3):
            continue
        x = _flare(z, spec, spec["axles"])
        y0 = spec["rocker_y"] + 0.04
        y1 = spec["belt_y"] - 0.012
        for sign in (1.0, -1.0):
            g.box((sign * (x - 0.001), (y0 + y1) * 0.5, z), (0.018, y1 - y0, 0.016), "Lib_Black")
    for z0, z1 in spec["windows"]:
        lo, hi = (z0, z1) if z0 <= z1 else (z1, z0)
        span = (hi - lo) - 0.06
        if span < 0.16:
            continue
        zmid = (lo + hi) * 0.5
        x = _flare(zmid, spec, spec["axles"])
        for sign in (1.0, -1.0):
            g.box((sign * (x - 0.001), spec["belt_y"] + 0.012, zmid), (0.018, 0.014, span), "Lib_Black")
            g.box((sign * (x - 0.001), spec["rocker_y"] + 0.07, zmid), (0.016, 0.012, span), "Lib_Black")


def _handle_sites(spec):
    """Door windows only. Quarter lights are too narrow for a handle."""
    sites = []
    for z0, z1 in spec["windows"]:
        lo, hi = (z0, z1) if z0 <= z1 else (z1, z0)
        if hi - lo < 0.30:
            continue
        z = (lo + hi) * 0.5 + 0.05
        x = _flare(z, spec, spec["axles"])
        y = spec["belt_y"] + 0.02
        for sign in (1.0, -1.0):
            sites.append((sign, x, y, z))
    return sites


def _pockets(spec):
    """Cutters that bite about 16 mm into the skin and leave the rest outside."""
    return [
        ((sign * (x + 0.014), y, z), (0.060, 0.034, 0.096))
        for sign, x, y, z in _handle_sites(spec)
    ]


def _handles(g, spec):
    for sign, x, y, z in _handle_sites(spec):
        # Black cup on the pocket floor, handle bar a few millimeters inside the skin.
        g.box((sign * (x - 0.013), y, z), (0.004, 0.028, 0.086), "Lib_Black")
        g.box((sign * (x - 0.010), y, z), (0.008, 0.012, 0.056), "Lib_SteelDark")


def _lamps(g, spec, lod):
    bev = 0.006 if lod == 0 else 0.0
    segs = 2 if lod == 0 else 0
    z_nose = spec["z1"]
    for x in (-spec["lamp_x"], spec["lamp_x"]):
        g.box((x, spec["lamp_y"], z_nose - 0.012), (0.30, 0.12, 0.028), "Lib_Black", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((x, spec["lamp_y"], z_nose + 0.010), (0.22, 0.072, 0.014), "Lib_Headlamp", bevel=bev, segs=segs)
    g.box((0, spec["lamp_y"] - 0.02, z_nose - 0.004), (0.52, 0.09, 0.018), "Lib_Black")
    if lod == 0:
        for i in range(int(spec.get("slats", 4))):
            g.box((0, spec["lamp_y"] - 0.048 + i * 0.022, z_nose + 0.008), (0.44, 0.008, 0.006), "Lib_SteelDark")
    if spec.get("bed_z0") is None:
        z_lens = spec["z0"] - 0.012
        z_bezel = spec["z0"] + 0.006
        z_bump = spec["z0"] + 0.02
    else:
        z_lens = spec["bed_z0"] - 0.022
        z_bezel = spec["bed_z0"] - 0.004
        z_bump = spec["bed_z0"] + 0.02
    for x in (-spec["lamp_x"], spec["lamp_x"]):
        g.box((x, spec["tail_y"], z_bezel), (0.26, 0.10, 0.016), "Lib_Black", bevel=bev, segs=1 if lod == 0 else 0)
        g.box((x, spec["tail_y"], z_lens), (0.20, 0.064, 0.012), "Lib_Taillamp", bevel=bev, segs=segs)
    g.box((0, spec["bumper_y"], spec["z1"] - 0.02), (spec["body_x"] * 1.92, 0.14, 0.07), "Lib_Black")
    width = spec["bed_x"] if spec.get("bed_z0") is not None else spec["body_x"]
    g.box((0, spec["bumper_y"], z_bump), (width * 1.92, 0.14, 0.07), "Lib_Black")


def _mirrors(g, spec, lod):
    """Stalk, black head, and a steel face just outside the front door."""
    z = spec["a_pillar_z"] - 0.05
    y = spec["belt_y"] + 0.045
    x = _flare(z, spec, spec["axles"]) + 0.05
    bev = 0.003 if lod == 0 else 0.0
    segs = 1 if lod == 0 else 0
    for sign in (1.0, -1.0):
        g.box((sign * (x - 0.028), y, z), (0.065, 0.014, 0.032), "Lib_Black")
        g.box((sign * (x + 0.012), y + 0.012, z), (0.042, 0.058, 0.096), "Lib_Black", bevel=bev, segs=segs)
        g.box((sign * (x + 0.036), y + 0.012, z), (0.008, 0.036, 0.060), "Lib_Steel")


def wheel_boxes(asset, spec, axles, name_fn):
    """Tread slab. The contact patch is y = 0 when the axle is the tire radius.

    A box centered on the axle with height about one radius leaves its bottom
    0.15 m (sedan, hatch) or 0.18 m (pickup) above the rubber. This slab sits
    in the carcass, 1.2 cm above the patch, inside the 3 cm slack limit.
    """
    radius = spec["tire_r"]
    bottom = 0.012
    height = 0.036
    depth = min(0.09, radius * 0.26)
    y = bottom + height * 0.5
    for i, z in enumerate(axles):
        for j, sign in enumerate((-1.0, 1.0)):
            asset.box(
                name_fn(i, j),
                (sign * spec["tire_x"], y, z),
                (spec["tire_half_w"] * 0.50, height, depth),
            )


def _wheels(g, spec, lod, axles):
    seg = 16 if lod == 0 else (4 if lod >= 2 else 10)
    r = spec["tire_r"]
    half_w = spec["tire_half_w"]
    for z in axles:
        for x in (-spec["tire_x"], spec["tire_x"]):
            g.cylinder(
                (x, spec["axle_y"], z), r, half_w * 2.0, "Lib_Rubber", seg,
                axis="X", bevel=0.016 if lod == 0 else 0.0, segs=2 if lod == 0 else 0,
            )
            sign = 1.0 if x > 0 else -1.0
            outer = x + sign * half_w
            # Proud of the sidewall by a few mm so the rim does not share a face with the tire.
            face = outer + sign * 0.012
            g.cylinder((face, spec["axle_y"], z), r * 0.70, 0.012, "Lib_Steel", seg, axis="X")
            g.cylinder((face + sign * 0.010, spec["axle_y"], z), r * 0.22, 0.012, "Lib_SteelDark", max(8, seg // 2), axis="X")
            if lod == 0:
                count = int(spec.get("spokes", 5))
                for k in range(count):
                    # Unity +X rotation swings local +Y toward +Z, so the spoke's long axis is (cos, sin).
                    theta = math.radians(k * (360.0 / count) + 8.0)
                    g.box(
                        (
                            face,
                            spec["axle_y"] + math.cos(theta) * r * 0.42,
                            z + math.sin(theta) * r * 0.42,
                        ),
                        (0.010, r * 0.46, 0.018),
                        "Lib_Steel",
                        euler=(math.degrees(theta), 0, 0),
                    )


def _bed(g, spec, step, mat, level):
    z1 = spec["bed_z1"]
    z0 = spec["bed_z0"]
    floor_y = spec["bed_floor_y"]
    rail = spec["bed_rail_y"]
    length = z1 - z0
    mid = (z0 + z1) * 0.5
    g.box((0, floor_y, mid), (spec["bed_x"] * 2.0 - 0.02, 0.06, length + 0.04), mat)
    g.box((0, (floor_y + rail) * 0.5, z0 + 0.02), ((spec["bed_x"] - 0.015) * 2.0, rail - floor_y + 0.04, 0.06), mat)
    axle = spec["bed_axle"]
    extra = [axle - spec["arch_r"], axle, axle + spec["arch_r"], z0, z1]
    zs = _stations(z0, z1, step, extra)
    for sign in (1.0, -1.0):
        rings = []
        for z in zs:
            lift = _arch_lift(z, (axle,), spec["axle_y"], spec["arch_r"], floor_y + 0.02)
            y_lo = min(lift, rail - 0.10)
            reach = spec["arch_r"] + 0.08
            flare = 0.0
            dz = abs(z - axle)
            if dz < reach:
                flare = spec["flare"] * math.cos((dz / reach) * math.pi * 0.5)
            x_i = spec["bed_x"] - 0.03
            x_o = spec["bed_x"] + flare
            if sign < 0:
                x_o, x_i = -x_o, -x_i
            rings.append([
                (x_o, y_lo, z),
                (x_o, rail, z),
                (x_i, rail, z),
                (x_i, max(y_lo, floor_y), z),
            ])
        _loft(g, rings, mat, smooth=level, wells=_wells(spec, (spec["bed_axle"],)))


def build(g, spec, lod):
    if lod >= 2:
        level = 0
        # Far shell only. LOD0 and LOD1 stay on the envelope that already fit.
        step = spec["step"] * 6.4
    elif lod == 1:
        level = 0
        step = spec["step"] * 1.8
    else:
        level = 1
        step = spec["step"]
    _shell(g, spec, step, spec["paint"], spec["axles"], level)
    if lod < 2:
        _cabin(g, spec)
        _seams(g, spec)
        _handles(g, spec)
    _glass(g, spec)
    _mirrors(g, spec, lod)
    _lamps(g, spec, lod)
    _wheels(g, spec, lod, spec["wheel_axles"])
    if spec.get("bed_z0") is not None:
        _bed(g, spec, step, spec["paint"], level)
        _wheels(g, spec, lod, (spec["bed_axle"],))
