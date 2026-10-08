"""Flush four-way intersection. Asphalt top matches Road_Straight.

Each corner is one sidewalk: a 5 m curb return flowing into straight walks
along both arms, with a curb ramp cut in at the crosswalk. The box is empty.
Stop bars are white and cover the inbound lanes only. The double yellow ends
at that bar and continues onto Road_Straight.
"""

import math
import os
import sys

import bmesh
import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from _common import ROAD_TOP, WALK_TOP, Asset, register

HALF = 3.0
ARM = 11.0
WALK = 2.0
# Curb return. The arc centre sits one radius outside both curb lines.
R_CURB = 5.0
RAMP_RUN = 1.20
# Zebra on the straight walk, just past the tangent at HALF + R_CURB.
CROSS_0 = 8.15
CROSS_1 = 9.65
# White bar behind the zebra. Yellow starts on the far side of this bar.
STOP_AT = 10.02
STOP_T = 0.36
# Paint is buried 2 cm into the asphalt and proud by 6 mm, then welded.
PAINT_TOP = ROAD_TOP + 0.006
PAINT_BOT = 0.10


@register
def create():
    a = Asset(
        "Road_Junction",
        "Roads",
        "Flush asphalt intersection. Each corner is one sidewalk with a 5 m curb return "
        "into straight walks along both arms, and a curb ramp at each crosswalk. The box is clear. "
        "A white stop bar covers the inbound lanes only; the double yellow ends at that bar. "
        "Butt Road_Straight to the arm ends so the double yellow continues.",
    )
    a.climb_note = "Flat asphalt and sidewalk. The curb face is 0.15 m above the road."
    a.vault_note = "Curb is 0.15 m. Not a vault."
    for lod in (0, 1):
        g = a.begin(lod)
        _asphalt(g)
        _sidewalks(g, lod)
        _markings(g)
        a.end()
        _weld_paint(a.lods[lod])
    _colliders(a)
    return a


def _asphalt(g):
    """Five quads. Internal joint walls are omitted so the arms stay one shell."""
    h = HALF
    slabs = (
        (((-h, -h), (h, -h), (h, h), (-h, h)), (0, 1, 2, 3)),
        (((-h, h), (h, h), (h, ARM), (-h, ARM)), (0,)),
        (((-h, -ARM), (h, -ARM), (h, -h), (-h, -h)), (2,)),
        (((h, -h), (ARM, -h), (ARM, h), (h, h)), (3,)),
        (((-ARM, -h), (-h, -h), (-h, h), (-ARM, h)), (1,)),
    )
    verts = []
    faces = []
    for quad, skip in slabs:
        b = len(verts)
        for x, z in quad:
            verts.append((x, 0.0, z))
        for x, z in quad:
            verts.append((x, ROAD_TOP, z))
        faces.append((b + 0, b + 3, b + 2, b + 1))
        faces.append((b + 4, b + 5, b + 6, b + 7))
        for i in range(4):
            if i in skip:
                continue
            j = (i + 1) % 4
            faces.append((b + i, b + j, b + 4 + j, b + 4 + i))
    g.mesh(verts, faces, "Lib_Asphalt", uv_scale=0.4)


# 1.5 cm under the road edge so the curb face is not coplanar with the asphalt side.
LIP = 0.015


def _profile(curb, mid, back, lip):
    """Three points across the walk, curb first. lip drops the curb to the road."""
    y0 = ROAD_TOP if lip else WALK_TOP
    return [
        (curb[0], y0, curb[1]),
        (mid[0], WALK_TOP, mid[1]),
        (back[0], WALK_TOP, back[1]),
    ]


def _corner_profiles():
    """NE corner, from the north arm end around to the east arm end."""
    cx = HALF + R_CURB
    curb = HALF - LIP
    back = curb + WALK
    outer = cx - curb
    mid_r = cx - (curb + RAMP_RUN)
    inner = cx - back
    profiles = []

    def north(z, lip):
        profiles.append(_profile((curb, z), (curb + RAMP_RUN, z), (back, z), lip))

    def east(x, lip):
        profiles.append(_profile((x, curb), (x, curb + RAMP_RUN), (x, back), lip))

    # The ramp cut is a 2 mm step so the loft stays a closed shell.
    north(ARM, False)
    north(CROSS_1, False)
    north(CROSS_1 - 0.002, True)
    north(CROSS_0 + 0.002, True)
    north(CROSS_0, False)
    north(cx, False)
    steps = 8
    a0, a1 = math.pi, math.pi * 1.5
    for i in range(1, steps):
        a = a0 + (a1 - a0) * (i / float(steps))
        ca, sa = math.cos(a), math.sin(a)
        profiles.append(_profile(
            (cx + outer * ca, cx + outer * sa),
            (cx + mid_r * ca, cx + mid_r * sa),
            (cx + inner * ca, cx + inner * sa),
            False,
        ))
    east(cx, False)
    east(CROSS_0, False)
    east(CROSS_0 + 0.002, True)
    east(CROSS_1 - 0.002, True)
    east(CROSS_1, False)
    east(ARM, False)
    return profiles


def _loft(profiles):
    verts = []
    faces = []
    rings = []
    for prof in profiles:
        top = []
        bot = []
        for x, y, z in prof:
            top.append(len(verts))
            verts.append((x, y, z))
            bot.append(len(verts))
            verts.append((x, 0.0, z))
        rings.append((top, bot))
    n = len(profiles[0])

    def area(a, b, c):
        ax, ay, az = verts[a]
        bx, by, bz = verts[b]
        cx, cy, cz = verts[c]
        ux, uy, uz = bx - ax, by - ay, bz - az
        vx, vy, vz = cx - ax, cy - ay, cz - az
        cross = (uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx)
        return cross[0] * cross[0] + cross[1] * cross[1] + cross[2] * cross[2]

    def quad(a, b, c, d):
        if area(a, b, c) < 1e-8 and area(a, c, d) < 1e-8:
            return
        faces.append((a, b, c, d))

    for i in range(len(rings) - 1):
        t0, b0 = rings[i]
        t1, b1 = rings[i + 1]
        for k in range(n - 1):
            quad(t0[k], t1[k], t1[k + 1], t0[k + 1])
            quad(b0[k], b0[k + 1], b1[k + 1], b1[k])
        quad(t0[0], b0[0], b1[0], t1[0])
        quad(t0[n - 1], t1[n - 1], b1[n - 1], b0[n - 1])
    t, b = rings[0]
    for k in range(n - 1):
        quad(t[k], t[k + 1], b[k + 1], b[k])
    t, b = rings[-1]
    for k in range(n - 1):
        quad(t[k], b[k], b[k + 1], t[k + 1])
    return verts, faces


def _sidewalks(g, lod):
    profiles = _corner_profiles()
    if lod == 1:
        # Same silhouette. The return is already a short arc.
        pass
    base_v, base_f = _loft(profiles)
    for sx in (-1, 1):
        for sz in (-1, 1):
            verts = [(x * sx, y, z * sz) for x, y, z in base_v]
            faces = base_f
            if sx * sz < 0:
                faces = [tuple(reversed(f)) for f in base_f]
            g.mesh(verts, faces, "Lib_Concrete", uv_scale=0.7)


def _markings(g):
    y = (PAINT_TOP + PAINT_BOT) * 0.5
    h = PAINT_TOP - PAINT_BOT
    bars = 6
    span = 5.2
    width = span / bars * 0.55
    deep = CROSS_1 - CROSS_0
    mid = (CROSS_0 + CROSS_1) * 0.5
    for i in range(bars):
        t = -span * 0.5 + (i + 0.5) * span / bars
        for sign in (-1, 1):
            cz = sign * mid
            g.box((t, y, cz), (width, h, deep), "Lib_PaintWhite")
            g.box((cz, y, t), (deep, h, width), "Lib_PaintWhite")
    # Inbound lane only. Right-hand traffic: the half on the driver's right.
    half_w = 2.50
    # Centre of the inbound half, 1.45 m off the crown, clear of the double yellow.
    for sign in (-1, 1):
        # North arm (+Z) inbound is -X. South arm inbound is +X.
        g.box((-sign * 1.45, y, sign * STOP_AT), (half_w, h, STOP_T), "Lib_PaintWhite")
        # East arm (+X) inbound is -Z. West arm inbound is +Z.
        g.box((sign * STOP_AT, y, -sign * 1.45), (STOP_T, h, half_w), "Lib_PaintWhite")
    _edges(g, y, h)
    _yellow(g, y, h)


def _edges(g, y, h):
    edge = HALF - 0.18
    z0 = HALF + 0.20
    length = ARM - z0
    mid = (z0 + ARM) * 0.5
    for sign in (-1, 1):
        g.box((sign * edge, y, mid), (0.10, h, length), "Lib_PaintWhite")
        g.box((sign * edge, y, -mid), (0.10, h, length), "Lib_PaintWhite")
        g.box((mid, y, sign * edge), (length, h, 0.10), "Lib_PaintWhite")
        g.box((-mid, y, sign * edge), (length, h, 0.10), "Lib_PaintWhite")


def _yellow(g, y, h):
    """Double yellow from the far side of the stop bar out to the arm end."""
    z0 = STOP_AT + STOP_T * 0.5 + 0.04
    if ARM <= z0:
        return
    length = ARM - z0
    mid = (z0 + ARM) * 0.5
    for sign in (-1, 1):
        for side in (-1, 1):
            g.box((side * 0.09, y, sign * mid), (0.08, h, length), "Lib_Lane")
            g.box((sign * mid, y, side * 0.09), (length, h, 0.08), "Lib_Lane")


def _extract(g, indices):
    bm = bmesh.new()
    vmap = {}
    for f in g.bm.faces:
        if f.material_index not in indices:
            continue
        verts = []
        for v in f.verts:
            if v not in vmap:
                vmap[v] = bm.verts.new(v.co)
            verts.append(vmap[v])
        try:
            nf = bm.faces.new(verts)
        except ValueError:
            continue
        nf.material_index = f.material_index
    return bm


def _weld_paint(g):
    """Union the buried paint into the asphalt so lane lines are one shell."""
    paint_idx = {i for i, name in enumerate(g.mats) if name in ("Lib_PaintWhite", "Lib_Lane")}
    asphalt_idx = {i for i, name in enumerate(g.mats) if name == "Lib_Asphalt"}
    other_idx = set(range(len(g.mats))) - paint_idx - asphalt_idx
    bm_a = _extract(g, asphalt_idx)
    bm_b = _extract(g, paint_idx)
    bm_c = _extract(g, other_idx)
    mesh_a = bpy.data.meshes.new("junction_asphalt")
    mesh_b = bpy.data.meshes.new("junction_paint")
    for name in g.mats:
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        mesh_a.materials.append(mat)
        mesh_b.materials.append(mat)
    bm_a.to_mesh(mesh_a)
    bm_b.to_mesh(mesh_b)
    oa = bpy.data.objects.new("junction_asphalt", mesh_a)
    ob = bpy.data.objects.new("junction_paint", mesh_b)
    bpy.context.scene.collection.objects.link(oa)
    bpy.context.scene.collection.objects.link(ob)
    mod = oa.modifiers.new("weld", "BOOLEAN")
    mod.operation = "UNION"
    mod.object = ob
    mod.solver = "EXACT"
    deps = bpy.context.evaluated_depsgraph_get()
    welded = bmesh.new()
    welded.from_object(oa, deps)
    bpy.data.objects.remove(oa, do_unlink=True)
    bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.meshes.remove(mesh_a)
    bpy.data.meshes.remove(mesh_b)
    out = bmesh.new()
    for src in (welded, bm_c):
        vmap = {}
        for f in src.faces:
            verts = []
            for v in f.verts:
                if v not in vmap:
                    vmap[v] = out.verts.new(v.co)
                verts.append(vmap[v])
            try:
                nf = out.faces.new(verts)
            except ValueError:
                continue
            nf.material_index = f.material_index
    g.bm.free()
    g.bm = out
    g.uv = g.bm.loops.layers.uv.new("UVMap")
    g.scale_layer = g.bm.faces.layers.float.new("uvscale")
    g.grain_layer = g.bm.faces.layers.float.new("uvgrain")
    for f in g.bm.faces:
        name = g.mats[f.material_index] if f.material_index < len(g.mats) else ""
        if name == "Lib_Asphalt":
            f[g.scale_layer] = 0.4
        elif name == "Lib_Concrete":
            f[g.scale_layer] = 0.7
        else:
            f[g.scale_layer] = 1.0
        f[g.grain_layer] = 0.0
    g.prepare()
    bm_a.free()
    bm_b.free()
    bm_c.free()
    welded.free()


def _colliders(asset):
    # Tops sit 2 cm under the asphalt. Each arm includes the outer lane.
    y, h = 0.05, 0.10
    # Stay off the 1.5 cm curb overlap so a sample does not start inside both shells.
    asset.box("Col_Asphalt", (0, y, 0), (5.70, h, 5.70))
    # Overlap the centre slab so the joint and the outer lane are both closed.
    arm_c = (2.90 + (ARM - 0.04)) * 0.5
    arm_l = (ARM - 0.04) - 2.90
    asset.box("Col_Arm_N", (0, y, arm_c), (5.60, h, arm_l))
    asset.box("Col_Arm_S", (0, y, -arm_c), (5.60, h, arm_l))
    asset.box("Col_Arm_E", (arm_c, y, 0), (arm_l, h, 5.60))
    asset.box("Col_Arm_W", (-arm_c, y, 0), (arm_l, h, 5.60))
    _walk_colliders(asset)


def _box_in(asset, name, x0, z0, x1, z1, y_top):
    if x1 < x0:
        x0, x1 = x1, x0
    if z1 < z0:
        z0, z1 = z1, z0
    if y_top < 0.08 or (x1 - x0) < 0.12 or (z1 - z0) < 0.12:
        return
    asset.box(
        name,
        ((x0 + x1) * 0.5, y_top * 0.5, (z0 + z1) * 0.5),
        (x1 - x0, y_top, z1 - z0),
    )


def _walk_colliders(asset):
    """Straight runs, the return bulb, and one box on each ramp."""
    n = 0
    y_walk = WALK_TOP - 0.02
    for sx in (-1, 1):
        for sz in (-1, 1):
            # Straight walk past the crosswalk, full height.
            _box_in(
                asset, "Col_Walk_%d" % n,
                sx * (HALF + 0.06), sz * (CROSS_1 + 0.08),
                sx * (HALF + WALK - 0.06), sz * (ARM - 0.06),
                y_walk,
            )
            n += 1
            _box_in(
                asset, "Col_Walk_%d" % n,
                sx * (CROSS_1 + 0.08), sz * (HALF + 0.06),
                sx * (ARM - 0.06), sz * (HALF + WALK - 0.06),
                y_walk,
            )
            n += 1
            # Landing beside the ramp, still full height.
            _box_in(
                asset, "Col_Walk_%d" % n,
                sx * (HALF + RAMP_RUN + 0.06), sz * (CROSS_0 + 0.08),
                sx * (HALF + WALK - 0.06), sz * (CROSS_1 - 0.08),
                y_walk,
            )
            n += 1
            _box_in(
                asset, "Col_Walk_%d" % n,
                sx * (CROSS_0 + 0.08), sz * (HALF + RAMP_RUN + 0.06),
                sx * (CROSS_1 - 0.08), sz * (HALF + WALK - 0.06),
                y_walk,
            )
            n += 1
            # Ramp slab. The lip is the low end; this box sits where the slope has room.
            ramp_y = 0.18
            _box_in(
                asset, "Col_Ramp_%d" % n,
                sx * (HALF + 0.62), sz * (CROSS_0 + 0.10),
                sx * (HALF + RAMP_RUN - 0.06), sz * (CROSS_1 - 0.10),
                ramp_y,
            )
            n += 1
            _box_in(
                asset, "Col_Ramp_%d" % n,
                sx * (CROSS_0 + 0.10), sz * (HALF + 0.62),
                sx * (CROSS_1 - 0.10), sz * (HALF + RAMP_RUN - 0.06),
                ramp_y,
            )
            n += 1
            # Bulb of the return, inside the 3–5 m band around the arc centre.
            _box_in(
                asset, "Col_Walk_%d" % n,
                sx * 4.85, sz * 4.85,
                sx * 5.65, sz * 5.65,
                y_walk,
            )
            n += 1
