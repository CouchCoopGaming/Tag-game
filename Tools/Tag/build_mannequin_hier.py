#!/usr/bin/env python3
"""
HiPoly hierarchical mannequin v8.1 / realism v0.7.1
Body topography smooth on v0.7 base — continuous high-realism store mannequin.
Kill soft-toy / unfinished body topo: joint gaps, seam ridges, wear bumps.

LOCKED from v0.7: slick athletic proportions, vinyl SSS, slim metal hinges,
anatomical_hand, molded human face, Runner cream / It warm tan + Vs,
DummyLocomotor bind tree + GUIDs / FBX paths.

DummyLocomotor bones / hierarchy / GUIDs / FBX paths unchanged.
GUID-safe FBX overwrite. NO git push.
"""
import bpy
import bmesh
import math
import os
import uuid
from mathutils import Vector, Euler, Matrix

OUT_DIR = "/workspace/tag-unity/Assets/Art/Characters/HiPoly"
PREV = "/workspace/art-build/previews"
BLEND = "/workspace/art-build/Dummy_Mannequin_Hier_Hi.blend"
LOG = "/tmp/hipoly_v71_build.log"
REF_STORE = "/workspace/tag-gdd/art/refs/hybrid-iii-v03/ref_store_mannequin_anatomy.jpg"

GUID_TAN = "ad3f2fa97db94e72869d746ecdf8e87d"
GUID_ORANGE = "b33974ad57284ef28a7564e3bdf00540"

# Runner cream #E8D9C0
BONE = (0.95, 0.88, 0.76, 1.0)  # warmer #E8D9C0 stills read
# It warm tan body (NOT toy-orange #FF6A00)
WARM_TAN = (0.82, 0.66, 0.50, 1.0)  # warm tan, not toy-orange
TEAL = (0.169, 0.702, 0.639, 1.0)
BLACK = (0.04, 0.04, 0.045, 1.0)
JOINT = (0.12, 0.12, 0.14, 1.0)
METAL = (0.28, 0.28, 0.30, 1.0)
SENSOR = (0.02, 0.02, 0.02, 1.0)
RIM = (0.15, 0.12, 0.10, 1.0)

SEG = 32
RING = 16
CYL_V = 24

# Human athletic proportions (~1.82m). Mild A-pose.
SHOULDER_Z = 1.48
SHOULDER_X = 0.22
UA_LEN = 0.30
LA_LEN = 0.27
ARM_OUT = math.radians(26.0)
ARM_FWD = math.radians(10.0)

HIP_Z = 0.95
HIP_X = 0.11
UL_LEN = 0.455
LL_LEN = 0.430

# Human head (NOT oversized egg) — top ~1.78, chin ~1.58
HEAD_Z = 1.70
HEAD_SCALE = (0.098, 0.105, 0.112)  # width, depth, height — human skull (not egg toy)

os.makedirs(OUT_DIR, exist_ok=True)
os.makedirs(PREV, exist_ok=True)
open(LOG, "w").close()


def log(msg):
    print(msg, flush=True)
    with open(LOG, "a") as f:
        f.write(msg + "\n")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials,
                 bpy.data.curves, bpy.data.cameras, bpy.data.lights,
                 bpy.data.metaballs):
        for x in list(coll):
            coll.remove(x)


def mat(name, color, metallic=0.02, roughness=0.42, emit=0.0, subsurface=0.0,
        ss_color=None, ss_radius=(1.0, 0.45, 0.20)):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.diffuse_color = color
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if subsurface > 0:
        if ss_color is None:
            ss_color = (
                min(1.0, color[0] * 1.05),
                min(1.0, color[1] * 0.95),
                min(1.0, color[2] * 0.75),
                1.0,
            )
        if "Subsurface Weight" in bsdf.inputs:
            bsdf.inputs["Subsurface Weight"].default_value = subsurface
            if "Subsurface Radius" in bsdf.inputs:
                bsdf.inputs["Subsurface Radius"].default_value = ss_radius
            if "Subsurface Color" in bsdf.inputs:
                bsdf.inputs["Subsurface Color"].default_value = ss_color
            if "Subsurface Scale" in bsdf.inputs:
                bsdf.inputs["Subsurface Scale"].default_value = 0.06
        elif "Subsurface" in bsdf.inputs:
            bsdf.inputs["Subsurface"].default_value = subsurface
            if "Subsurface Color" in bsdf.inputs:
                bsdf.inputs["Subsurface Color"].default_value = ss_color
            if "Subsurface Radius" in bsdf.inputs:
                bsdf.inputs["Subsurface Radius"].default_value = ss_radius
    if emit > 0:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = color
            bsdf.inputs["Emission Strength"].default_value = emit
        elif "Emission" in bsdf.inputs:
            bsdf.inputs["Emission"].default_value = (*color[:3], 1.0)
    return m


def set_mat(ob, m):
    if ob is None:
        return
    ob.data.materials.clear()
    ob.data.materials.append(m)


def shade_smooth(ob):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.shade_smooth()
    if hasattr(ob.data, "use_auto_smooth"):
        ob.data.use_auto_smooth = True
        ob.data.auto_smooth_angle = math.radians(50)


def apply_mod(ob, mod_name):
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod_name)


def apply_subsurf(ob, levels=1):
    mod = ob.modifiers.new("SubD", "SUBSURF")
    mod.levels = levels
    mod.render_levels = levels
    apply_mod(ob, mod.name)


def apply_bevel(ob, width=0.006, segments=2):
    mod = ob.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(30)
    apply_mod(ob, mod.name)


def light_smooth(ob, iterations=6, factor=0.5):
    mod = ob.modifiers.new("Smooth", "SMOOTH")
    mod.factor = factor
    mod.iterations = iterations
    apply_mod(ob, mod.name)


def voxel_remesh(ob, size=0.012):
    """Continuous slick shell — voxel remesh then smooth."""
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    mod = ob.modifiers.new("Remesh", "REMESH")
    mod.mode = "VOXEL"
    mod.voxel_size = size
    mod.adaptivity = 0.0
    apply_mod(ob, mod.name)
    light_smooth(ob, iterations=8, factor=0.6)
    shade_smooth(ob)
    return ob


def remesh_smooth(ob, size=0.010, iterations=10, factor=0.55):
    """v0.7.1 body topo pass — voxel remesh + extra smooth for continuous vinyl."""
    voxel_remesh(ob, size=size)
    light_smooth(ob, iterations=iterations, factor=factor)
    shade_smooth(ob)
    return ob


def sph(name, loc, scale, seg=SEG, ring=RING, sub=False):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=ring, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale if isinstance(scale, (tuple, list, Vector)) else (scale, scale, scale)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    shade_smooth(o)
    if sub:
        apply_subsurf(o, 1)
    return o


def cyl(name, loc, radius, depth, rot=(0, 0, 0), v=CYL_V, sub=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=v, radius=radius, depth=depth, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    shade_smooth(o)
    if sub:
        apply_subsurf(o, 1)
    return o


def cone(name, loc, r1, r2, depth, rot=(0, 0, 0), v=CYL_V):
    bpy.ops.mesh.primitive_cone_add(
        vertices=v, radius1=r1, radius2=r2, depth=depth, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    shade_smooth(o)
    return o


def cube(name, loc, scale, rot=(0, 0, 0), bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    o.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if bevel > 0:
        apply_bevel(o, width=bevel, segments=2)
    shade_smooth(o)
    return o


def join(name, objs):
    objs = [o for o in objs if o is not None]
    if not objs:
        return None
    if len(objs) == 1:
        objs[0].name = name
        return objs[0]
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    objs[0].name = name
    return objs[0]


def boolean_union(target, tools):
    tools = [t for t in tools if t is not None]
    for i, t in enumerate(tools):
        mod = target.modifiers.new(f"BU{i}", "BOOLEAN")
        mod.operation = "UNION"
        mod.solver = "EXACT"
        mod.object = t
        apply_mod(target, mod.name)
        bpy.data.objects.remove(t, do_unlink=True)
    return target


def boolean_difference(target, tools):
    tools = [t for t in tools if t is not None]
    for i, t in enumerate(tools):
        mod = target.modifiers.new(f"BD{i}", "BOOLEAN")
        mod.operation = "DIFFERENCE"
        mod.solver = "EXACT"
        mod.object = t
        apply_mod(target, mod.name)
        bpy.data.objects.remove(t, do_unlink=True)
    return target


def orient_along(ob, direction):
    """Rotate object so local +Z aligns with direction."""
    direction = Vector(direction)
    if direction.length < 1e-8:
        return
    quat = direction.normalized().to_track_quat("Z", "Y")
    ob.rotation_euler = quat.to_euler()
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)


def tapered_limb(name, a, b, r0, r1, v=CYL_V, bulge=0.0, bulge_t=0.35, caps=True):
    """Sculpted tapered limb with optional muscle bulge — continuous soft shell."""
    a, b = Vector(a), Vector(b)
    direction = b - a
    length = direction.length
    if length < 1e-6:
        return sph(name, a, r0)
    mid = a.lerp(b, 0.5)
    body = cone(name + "_shaft", mid, r0, r1, max(length * 0.92, 0.04), v=v)
    orient_along(body, direction)
    parts = [body]
    if caps:
        c0 = sph(f"{name}_c0", a, r0 * 0.95, seg=16, ring=8)
        c1 = sph(f"{name}_c1", b, r1 * 0.95, seg=16, ring=8)
        parts.extend([c0, c1])
    else:
        # Soft proximal cap only — distal open for mannequin wrist attach
        c0 = sph(f"{name}_c0", a, r0 * 0.95, seg=16, ring=8)
        parts.append(c0)
    if bulge > 0:
        bp = a.lerp(b, bulge_t)
        br = (r0 + r1) * 0.5 * (1.0 + bulge)
        mus = sph(f"{name}_mus", bp, (br * 1.05, br * 0.92, br * 1.15), seg=16, ring=8)
        parts.append(mus)
    return join(name, parts)


def slim_hinge(name, loc, axis="X", radius=0.038, thick=0.010):
    """Nearly-flush metal articulation line — thin ring, not ridge / LEGO disk."""
    if axis == "X":
        rot = (0, math.radians(90), 0)
    elif axis == "Y":
        rot = (math.radians(90), 0, 0)
    else:
        rot = (0, 0, 0)
    # Single thin disk — no outer rim ridge (v0.7.1 flush)
    disk = cyl(f"{name}_d", loc, radius, thick, rot=rot, v=28)
    return disk


def seam_ring(name, loc, radius, axis="Z", thick=0.006):
    """Subtle assembly seam — panel join, not plate armor."""
    if axis == "Z":
        rot = (0, 0, 0)
    elif axis == "X":
        rot = (0, math.radians(90), 0)
    else:
        rot = (math.radians(90), 0, 0)
    outer = cyl(f"{name}_o", loc, radius * 1.02, thick, rot=rot, v=28)
    return outer


def chevron_v(tag, cx, cy, cz, half_w, bar_len, thick=0.010, depth=0.012, ang_deg=34.0):
    ang = math.radians(ang_deg)
    drop = math.sin(ang) * bar_len * 0.55
    spread = math.cos(ang) * bar_len * 0.45
    left = cube(f"{tag}_L", (cx - spread, cy, cz + drop * 0.15),
                (bar_len * 0.5, depth, thick), (0, ang, 0))
    right = cube(f"{tag}_R", (cx + spread, cy, cz + drop * 0.15),
                 (bar_len * 0.5, depth, thick), (0, -ang, 0))
    return [left, right]


def phalanx(name, a, b, r0, r1, knuckle_r=None):
    """Single finger phalanx: tapered shell + proximal knuckle thickening."""
    a, b = Vector(a), Vector(b)
    parts = []
    shaft = tapered_limb(f"{name}_sh", a, b, r0, r1, v=12, bulge=0.0)
    parts.append(shaft)
    kr = knuckle_r if knuckle_r is not None else r0 * 1.22
    kn = sph(f"{name}_kn", a, (kr * 1.05, kr * 0.95, kr * 1.10), seg=12, ring=6)
    parts.append(kn)
    return join(name, parts)


def continuous_digit(name, a, b, r0, r1, n_seg=16, n_rings=22, tip_flat=0.48):
    """ONE continuous tapered digit — radius profile knuckles (subtle), flattened tip pad.
    No separate sphere parts.
    """
    a, b = Vector(a), Vector(b)
    direction = b - a
    length = direction.length
    if length < 1e-6:
        return sph(name, a, r0)
    direction_n = direction.normalized()

    def radius_at(t):
        # Gentle taper + subtle knuckle thickenings (NOT bead lobes)
        base = r0 * (1.0 - t) + r1 * t
        kn_mcp = math.exp(-((t - 0.12) ** 2) / (2 * 0.045 ** 2)) * r0 * 0.14
        kn_pip = math.exp(-((t - 0.45) ** 2) / (2 * 0.050 ** 2)) * r0 * 0.11
        kn_dip = math.exp(-((t - 0.75) ** 2) / (2 * 0.040 ** 2)) * r0 * 0.07
        return base + kn_mcp + kn_pip + kn_dip

    bm = bmesh.new()
    ring_verts = []
    for i in range(n_rings + 1):
        tparm = i / n_rings
        z = (tparm - 0.5) * length
        r = radius_at(tparm)
        flat = 1.0
        if tparm > 0.80:
            u = (tparm - 0.80) / 0.20
            flat = 1.0 - (1.0 - tip_flat) * (u * u)
            # Soft pad mass — slightly fatter but flattened, not spherical
            r = r * (1.0 + 0.08 * u)
        row = []
        for j in range(n_seg):
            ang = (2 * math.pi * j) / n_seg
            x = math.cos(ang) * r
            y = math.sin(ang) * r * flat
            row.append(bm.verts.new((x, y, z)))
        ring_verts.append(row)
    bm.verts.ensure_lookup_table()
    for i in range(n_rings):
        for j in range(n_seg):
            j2 = (j + 1) % n_seg
            bm.faces.new((ring_verts[i][j], ring_verts[i][j2],
                          ring_verts[i + 1][j2], ring_verts[i + 1][j]))
    for cap_i, rev in ((0, True), (n_rings, False)):
        fverts = list(ring_verts[cap_i])
        if rev:
            fverts = list(reversed(fverts))
        ctr = bm.verts.new((0, 0, (cap_i / n_rings - 0.5) * length))
        for j in range(n_seg):
            j2 = (j + 1) % n_seg
            if rev:
                bm.faces.new((ctr, fverts[j2], fverts[j]))
            else:
                bm.faces.new((ctr, fverts[j], fverts[j2]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(mesh)
    bm.free()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    mid = a.lerp(b, 0.5)
    ob.location = mid
    ob.rotation_euler = direction_n.to_track_quat("Z", "Y").to_euler()
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    shade_smooth(ob)
    return ob


def anatomical_hand(name, wr, hand, sx, base_mat, joint_mat):
    """Store-mannequin hand via deep-overlap continuous digits + soft palm sleeve.
    Digits = single lofted shells (knuckle radius profile + flattened tip pads).
    Palm remeshed alone; digits NOT remeshed/booleaned (preserves tips).
    Bases planted deep in palm so junctions read continuous. Slim wrist ring only.
    """
    # Thin metal wrist seam — nearly flush inside sleeve
    wrj = slim_hinge(f"{name}_WristJ", wr, axis="X", radius=0.020, thick=0.0022)
    set_mat(wrj, joint_mat)

    palm_parts = []
    # Wrist sleeve — continuous vinyl into forearm (kills floating ball gap)
    mid_sl = wr.lerp(hand, 0.45)
    dir_sl = (hand - wr)
    sleeve = cyl(f"{name}_Sleeve", mid_sl, 0.027, max(dir_sl.length * 1.05, 0.05), v=22)
    orient_along(sleeve, dir_sl)
    palm_parts.append(sleeve)
    # Soft anatomical palm (flattened)
    palm_parts.append(sph(f"{name}_Palm",
                          hand + Vector((0, -0.008, 0.002)),
                          (0.042, 0.016, 0.044), seg=28, ring=16))
    palm_parts.append(sph(f"{name}_Belly",
                          hand + Vector((0, -0.022, -0.006)),
                          (0.034, 0.010, 0.036), seg=18, ring=10))
    palm_parts.append(sph(f"{name}_Dors",
                          hand + Vector((0, 0.002, 0.014)),
                          (0.038, 0.013, 0.030), seg=18, ring=10))
    # Thenar + hypothenar
    palm_parts.append(sph(f"{name}_Thenar",
                          hand + Vector((sx * 0.032, -0.002, 0.010)),
                          (0.026, 0.017, 0.030), seg=16, ring=10))
    palm_parts.append(sph(f"{name}_Hypo",
                          hand + Vector((sx * -0.030, -0.006, 0.0)),
                          (0.017, 0.013, 0.022), seg=12, ring=6))
    # Distal palm cups that swallow finger bases
    for i, lx in enumerate((-0.028, -0.009, 0.010, 0.028)):
        lx_s = lx if sx > 0 else -lx
        palm_parts.append(sph(f"{name}_Cup{i}",
                              hand + Vector((lx_s, -0.030, 0.010)),
                              (0.014, 0.014, 0.014), seg=12, ring=6))

    palm = join(f"{name}_PalmBody", palm_parts)
    set_mat(palm, base_mat)
    voxel_remesh(palm, size=0.005)
    light_smooth(palm, iterations=3, factor=0.45)
    set_mat(palm, base_mat)

    # Continuous digits — start INSIDE palm cups; NO remesh / NO boolean
    parts = [wrj, palm]
    finger_specs = [
        # lx, base_y (deep in palm), base_z, tip_y, tip_z, r0, r1
        (-0.028, -0.022, 0.012, -0.125, -0.014, 0.0130, 0.0070),
        (-0.009, -0.026, 0.014, -0.140, -0.016, 0.0135, 0.0072),
        (0.010, -0.024, 0.012, -0.132, -0.014, 0.0128, 0.0068),
        (0.028, -0.018, 0.006, -0.112, -0.008, 0.0115, 0.0060),
    ]
    for i, (lx, by, bz, ty, tz, r0, r1) in enumerate(finger_specs):
        lx_s = lx if sx > 0 else -lx
        base = hand + Vector((lx_s, by, bz))
        tip = hand + Vector((lx_s * 1.04, ty, tz))
        fing = continuous_digit(f"{name}_F{i}", base, tip, r0, r1,
                                n_seg=16, n_rings=24, tip_flat=0.42)
        set_mat(fing, base_mat)
        light_smooth(fing, iterations=1, factor=0.25)
        parts.append(fing)

    # Opposed thumb — base deep in thenar
    t0 = hand + Vector((sx * 0.018, 0.000, 0.014))
    t2 = hand + Vector((sx * 0.082, 0.048, -0.032))
    thumb = continuous_digit(f"{name}_Thumb", t0, t2, 0.0155, 0.0078,
                             n_seg=16, n_rings=22, tip_flat=0.45)
    set_mat(thumb, base_mat)
    light_smooth(thumb, iterations=1, factor=0.25)
    parts.append(thumb)

    return join(name, parts)


def shoe_foot(name, an, toe, sx, base_mat, joint_mat, rubber_mat=None):
    """Heel/toe foot pad — readable shoe sole, slim ankle hinge."""
    rub = rubber_mat or base_mat
    parts = []
    # Core hidden in shin sleeve overlap; flush ankle line
    an_ball = sph(f"{name}_AnBall", an, 0.024, seg=12, ring=6)
    set_mat(an_ball, base_mat)
    parts.append(an_ball)
    anj = slim_hinge(f"{name}_AnkleJ", an, axis="X", radius=0.028, thick=0.0018)
    set_mat(anj, joint_mat)
    parts.append(anj)
    # Foot shell continuous
    heel = sph(f"{name}_Heel", an + Vector((0, 0.030, -0.022)),
               (0.036, 0.038, 0.030), seg=14, ring=8)
    set_mat(heel, base_mat)
    parts.append(heel)
    mid = sph(f"{name}_Mid", an + Vector((0, -0.040, -0.024)),
              (0.042, 0.070, 0.024), seg=16, ring=8)
    set_mat(mid, base_mat)
    parts.append(mid)
    # Soft toe volume with slight toe separation read
    toe_pad = sph(f"{name}_Toe", an + Vector((0, -0.115, -0.014)),
                  (0.034, 0.038, 0.018), seg=12, ring=6)
    set_mat(toe_pad, base_mat)
    parts.append(toe_pad)
    # Thin sole pad
    sole = sph(f"{name}_Sole", an + Vector((0, -0.040, -0.042)),
               (0.038, 0.090, 0.008), seg=12, ring=6)
    set_mat(sole, rub)
    parts.append(sole)
    return join(name, parts)


def athletic_torso(name):
    """Slick continuous athletic torso — pecs/abs/delts as soft mass, NOT plate armor."""
    parts = []
    # Main thorax
    thorax = sph(f"{name}_Thorax", (0, 0.01, 1.36), (0.195, 0.125, 0.165), seg=36, ring=18)
    parts.append(thorax)
    # Soft lower ribs / abs taper
    abs_shell = sph(f"{name}_Abs", (0, 0.008, 1.14), (0.160, 0.105, 0.100), seg=28, ring=14)
    parts.append(abs_shell)
    # Soft waist — extends down to meet pelvis
    waist = sph(f"{name}_Waist", (0, 0.01, 1.00), (0.140, 0.100, 0.070), seg=24, ring=12)
    parts.append(waist)
    # Pec volumes — soft continuous mass (no plate/bump noise)
    for sx in (1, -1):
        pec = sph(f"{name}_Pec{sx}", (sx * 0.070, -0.070, 1.37),
                  (0.090, 0.052, 0.070), seg=20, ring=12)
        parts.append(pec)
    # v0.7.1: NO discrete ab bump spheres (were soft-toy noise)
    # Soft continuous abs plane instead
    abs_plane = sph(f"{name}_AbsPlane", (0, -0.075, 1.16),
                    (0.070, 0.022, 0.080), seg=18, ring=10)
    parts.append(abs_plane)
    # Lats / side — blended
    for sx in (1, -1):
        lat = sph(f"{name}_Lat{sx}", (sx * 0.150, 0.015, 1.28),
                  (0.060, 0.080, 0.095), seg=16, ring=10)
        parts.append(lat)
    # Deltoid shelves — smaller, deeper into thorax so not reading as exposed ShBall
    for sx in (1, -1):
        delt = sph(f"{name}_Delt{sx}", (sx * 0.175, 0.0, 1.450),
                   (0.065, 0.062, 0.068), seg=18, ring=10)
        parts.append(delt)
        # Sleeve stub toward arm so shoulder reads continuous into UA
        stub = sph(f"{name}_DeltStub{sx}", (sx * 0.215, 0.0, 1.448),
                   (0.042, 0.040, 0.042), seg=14, ring=8)
        parts.append(stub)
    # Soft clavicle / upper chest
    clav = sph(f"{name}_Clav", (0, -0.02, 1.48), (0.145, 0.058, 0.038), seg=22, ring=12)
    parts.append(clav)
    # Waist bridge down into pelvis (closes soft waist cleft)
    waist_bridge = sph(f"{name}_WaistBridge", (0, 0.01, 0.94),
                       (0.145, 0.105, 0.055), seg=22, ring=12)
    parts.append(waist_bridge)

    shell = join(name, parts)
    remesh_smooth(shell, size=0.010, iterations=14, factor=0.58)
    return shell


def athletic_pelvis(name):
    """Continuous pelvis / hip bowl — soft athletic mass."""
    parts = []
    bowl = sph(f"{name}_Bowl", (0, 0.015, 0.92), (0.170, 0.128, 0.115), seg=30, ring=16)
    parts.append(bowl)
    for sx in (1, -1):
        wing = sph(f"{name}_Wing{sx}", (sx * 0.125, 0.015, 0.90),
                   (0.065, 0.078, 0.072), seg=18, ring=10)
        parts.append(wing)
        # Hip sleeve stub — continuous into thigh
        stub = sph(f"{name}_HipStub{sx}", (sx * 0.115, 0.015, 0.95),
                   (0.050, 0.048, 0.048), seg=14, ring=8)
        parts.append(stub)
    lower = sph(f"{name}_Lower", (0, 0.01, 0.82), (0.105, 0.088, 0.050), seg=20, ring=12)
    parts.append(lower)
    # Upward waist fill into chest bridge
    up = sph(f"{name}_UpWaist", (0, 0.012, 0.98), (0.140, 0.100, 0.045), seg=22, ring=12)
    parts.append(up)
    shell = join(name, parts)
    remesh_smooth(shell, size=0.010, iterations=14, factor=0.58)
    return shell


def human_head(name, base_mat, sensor_mat):
    """Molded human face — brow, nose, lips, chin, ears. Flat dark eye insets. ZERO orbs.
    No makeup / goatee. Human scale (not oversized egg).
    Returns list of (bone, ob, mat) — caller adds Head group.
    """
    out = []
    # Human skull proportions — slightly wider than deep egg; natural scale
    skull = sph(f"{name}_Skull", (0, -0.01, HEAD_Z), HEAD_SCALE, seg=40, ring=22, sub=True)
    # Soft forehead flatten slightly via second mass
    forehead = sph(f"{name}_Fore", (0, -0.04, HEAD_Z + 0.055),
                   (0.090, 0.070, 0.050), seg=24, ring=12)
    # Jaw / cheek volume
    jaw = sph(f"{name}_Jaw", (0, -0.02, HEAD_Z - 0.070),
              (0.078, 0.085, 0.055), seg=24, ring=12)
    cheeks = []
    for sx in (1, -1):
        cheeks.append(sph(f"{name}_Cheek{sx}", (sx * 0.055, -0.055, HEAD_Z - 0.020),
                          (0.038, 0.040, 0.040), seg=14, ring=8))

    # Brow ridge
    brow = sph(f"{name}_Brow", (0.0, -0.098, HEAD_Z + 0.032),
               (0.072, 0.012, 0.010), seg=24, ring=10)

    # Nose bridge + tip (molded human, not wedge toy)
    nose_bridge = sph(f"{name}_NBridge", (0.0, -0.115, HEAD_Z - 0.005),
                      (0.012, 0.035, 0.030), seg=14, ring=8)
    nose_tip = sph(f"{name}_NTip", (0.0, -0.138, HEAD_Z - 0.035),
                   (0.014, 0.018, 0.014), seg=12, ring=8)

    # Lip volume (upper + lower) — soft mouth mass, not slit-only
    upper_lip = sph(f"{name}_ULip", (0.0, -0.108, HEAD_Z - 0.062),
                    (0.028, 0.012, 0.008), seg=14, ring=8)
    lower_lip = sph(f"{name}_LLip", (0.0, -0.105, HEAD_Z - 0.075),
                    (0.026, 0.011, 0.008), seg=12, ring=6)

    # Chin
    chin = sph(f"{name}_Chin", (0.0, -0.085, HEAD_Z - 0.105),
               (0.028, 0.030, 0.022), seg=14, ring=8)

    # Ears
    ear_parts = []
    for sx in (1, -1):
        ear_parts.append(sph(f"{name}_Ear{sx}", (sx * 0.100, 0.005, HEAD_Z - 0.005),
                             (0.014, 0.022, 0.030), seg=12, ring=8))

    # Boolean-union face features into skull
    extras = [forehead, jaw, brow, nose_bridge, nose_tip, upper_lip, lower_lip, chin] + ear_parts + cheeks
    head = boolean_union(skull, extras)
    light_smooth(head, iterations=3, factor=0.4)
    shade_smooth(head)
    set_mat(head, base_mat)
    out.append(("Head", head, base_mat))

    # Eye sockets: carve recesses, then flat dark oval insets (NOT beads/orbs)
    recess_tools = []
    for dx in (-0.032, 0.032):
        recess_tools.append(
            sph(f"{name}_EyeRec{dx}", (dx, -0.108, HEAD_Z + 0.012),
                (0.022, 0.018, 0.016), seg=14, ring=8)
        )
    boolean_difference(head, recess_tools)
    for dx in (-0.032, 0.032):
        # Flat dark oval plate — very thin in Y
        eye = sph(f"{name}_Eye{dx}", (dx, -0.095, HEAD_Z + 0.010),
                  (0.016, 0.0025, 0.011), seg=14, ring=8)
        set_mat(eye, sensor_mat)
        out.append(("Head", eye, sensor_mat))

    # Mouth crease — shallow dark line between lips (not painted goatee)
    # Soft mouth crease only — avoid painted-goatee read
    mouth = cube(f"{name}_Mouth", (0.0, -0.112, HEAD_Z - 0.068),
                 (0.018, 0.0008, 0.0014), bevel=0.0003)
    set_mat(mouth, sensor_mat)
    out.append(("Head", mouth, sensor_mat))

    return out


def arm_points(sx):
    sh = Vector((sx * SHOULDER_X, 0.0, SHOULDER_Z))
    out = math.sin(ARM_OUT)
    down = math.cos(ARM_OUT)
    fwd = math.sin(ARM_FWD)
    dir_ua = Vector((sx * out, -fwd, -down)).normalized()
    el = sh + dir_ua * UA_LEN
    dir_la = (dir_ua + Vector((sx * 0.06, -0.18, -0.08))).normalized()
    dir_la = Vector((sx * abs(dir_la.x) + sx * 0.04, dir_la.y - 0.04, dir_la.z)).normalized()
    wr = el + dir_la * LA_LEN
    hand = wr + Vector((sx * 0.012, -0.045, -0.040))
    return sh, el, wr, hand


def leg_points(sx):
    hip = Vector((sx * HIP_X, 0.015, HIP_Z))
    kn = hip + Vector((sx * 0.010, 0.015, -UL_LEN))
    an = kn + Vector((0.0, 0.0, -LL_LEN))
    toe = an + Vector((0.0, -0.12, -0.012))
    return hip, kn, an, toe


def build_armature():
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm_ob = bpy.context.active_object
    arm_ob.name = "DummyArmature"
    arm = arm_ob.data
    arm.name = "DummyArmature"
    arm.display_type = "OCTAHEDRAL"
    eb = arm.edit_bones

    root = eb[0]
    root.name = "Root"
    root.head = (0, 0, 0)
    root.tail = (0, 0, 0.08)

    def bone(name, parent, head, tail, connect=False):
        b = eb.new(name)
        b.head = Vector(head)
        b.tail = Vector(tail)
        if parent:
            b.parent = eb[parent]
            b.use_connect = connect
        return b

    bone("Hips", "Root", (0, 0, HIP_Z - 0.04), (0, 0, HIP_Z + 0.08))
    bone("Spine", "Hips", (0, 0, HIP_Z + 0.08), (0, 0, 1.16))
    bone("Chest", "Spine", (0, 0, 1.16), (0, 0, SHOULDER_Z))
    bone("Neck", "Chest", (0, 0, SHOULDER_Z), (0, 0, 1.58))
    bone("Head", "Neck", (0, 0, 1.58), (0, 0, 1.82))

    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr, hand = arm_points(sx)
        bone(f"Shoulder_{side}", "Chest", (sx * 0.10, 0, SHOULDER_Z), sh)
        bone(f"UpperArm_{side}", f"Shoulder_{side}", sh, el)
        bone(f"LowerArm_{side}", f"UpperArm_{side}", el, wr)
        bone(f"Hand_{side}", f"LowerArm_{side}", wr, hand)

        hip, kn, an, toe = leg_points(sx)
        bone(f"UpperLeg_{side}", "Hips", hip, kn)
        bone(f"LowerLeg_{side}", f"UpperLeg_{side}", kn, an)
        bone(f"Foot_{side}", f"LowerLeg_{side}", an, toe)

    bpy.ops.object.mode_set(mode="OBJECT")
    return arm_ob


def build_mesh_parts(is_it, mats):
    """v0.7.1 — smooth body topo on v0.7 base: deep sleeve overlaps, flush hinges, no soft-toy seams."""
    (base, accent, over, joint, sensor, metal, rubber, wear) = mats
    groups = {k: [] for k in (
        "Hips", "Spine", "Chest", "Neck", "Head",
        "UpperArm_L", "UpperArm_R", "LowerArm_L", "LowerArm_R",
        "Hand_L", "Hand_R",
        "UpperLeg_L", "UpperLeg_R", "LowerLeg_L", "LowerLeg_R",
        "Foot_L", "Foot_R", "Shoulder_L", "Shoulder_R",
    )}

    def add(bone, ob, m):
        set_mat(ob, m)
        groups[bone].append(ob)

    # --- Head: molded human face (UNCHANGED from v0.7) ---
    for bone, ob, m in human_head("Head", base, sensor):
        add(bone, ob, m)

    # --- Neck: continuous vinyl column — NO seam ring ridge ---
    neck = cyl("NeckCol", (0, 0.0, 1.540), 0.050, 0.125, v=26)
    neck_fill = sph("NeckFill", (0, 0.0, 1.490), (0.058, 0.055, 0.040), seg=20, ring=12)
    neck_top = sph("NeckTop", (0, 0.0, 1.590), (0.052, 0.050, 0.030), seg=18, ring=10)
    neck_shell = join("NeckShell", [neck, neck_fill, neck_top])
    remesh_smooth(neck_shell, size=0.007, iterations=10, factor=0.55)
    add("Neck", neck_shell, base)

    # --- Chest / torso continuous athletic shell ---
    chest = athletic_torso("ChestShell")
    add("Chest", chest, base)
    # v0.7.1: NO ShSeam shoulder rings (were soft-toy ridges)

    # Thin teal tick on Runner only
    if not is_it:
        tick = cube("ChestTick", (0.0, -0.125, 1.22), (0.030, 0.004, 0.005), bevel=0.001)
        add("Chest", tick, accent)

    # Continuous waist fill — NO WaistSeam ridge
    spine_fill = sph("SpineFill", (0, 0.01, 0.995), (0.138, 0.102, 0.080), seg=26, ring=14)
    remesh_smooth(spine_fill, size=0.009, iterations=12, factor=0.55)
    add("Spine", spine_fill, base)

    # --- Pelvis ---
    pelvis = athletic_pelvis("PelvisShell")
    add("Hips", pelvis, base)

    # It nested black Vs on chest + outer thighs
    if is_it:
        for i, z in enumerate([1.42, 1.34, 1.26]):
            half = 0.095 - i * 0.012
            bars = chevron_v(f"ChC{i}", 0.0, -0.130, z, half, bar_len=half * 1.15,
                             thick=0.012, depth=0.014, ang_deg=34.0)
            for b in bars:
                add("Chest", b, accent)

    # --- Arms: deep sleeve overlap past joints + flush hinges; no exposed balls ---
    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr, hand = arm_points(sx)
        dir_ua = (el - sh).normalized()
        dir_la = (wr - el).normalized()

        # Nearly-flush hinge only — no separate ShBall / ShCore (sleeves cover joint)
        shj = slim_hinge(f"ShoulderJ_{side}", sh, axis="X", radius=0.030, thick=0.0016)
        add(f"Shoulder_{side}", shj, joint)

        # Upper arm: deep past shoulder into chest delt stub and past elbow into forearm
        ua_a = sh - dir_ua * 0.045
        ua_b = el + dir_ua * 0.048
        ua = tapered_limb(f"UA_{side}", ua_a, ua_b, 0.045, 0.035, v=24,
                          bulge=0.22, bulge_t=0.40)
        delt = sph(f"UADelt_{side}", sh + Vector((sx * 0.008, 0, -0.012)),
                   (0.050, 0.046, 0.052), seg=18, ring=12)
        el_sleeve_ua = sph(f"UAElSlv_{side}", el,
                           (0.036, 0.034, 0.036), seg=16, ring=10)
        sh_sleeve = sph(f"UAShSlv_{side}", sh,
                        (0.044, 0.042, 0.044), seg=16, ring=10)
        ua_shell = join(f"UAShell_{side}", [ua, delt, el_sleeve_ua, sh_sleeve])
        remesh_smooth(ua_shell, size=0.008, iterations=12, factor=0.58)
        set_mat(ua_shell, base)
        add(f"UpperArm_{side}", ua_shell, base)

        elj = slim_hinge(f"ElbowJ_{side}", el, axis="X", radius=0.024, thick=0.0015)
        add(f"LowerArm_{side}", elj, joint)

        # Forearm: deep past elbow and into wrist sleeve
        la_a = el - dir_la * 0.045
        la_b = wr + (hand - wr).normalized() * 0.025
        la = tapered_limb(f"LA_{side}", la_a, la_b, 0.033, 0.024, v=22,
                          bulge=0.12, bulge_t=0.35, caps=False)
        la_prox = sph(f"LAProx_{side}", el, (0.034, 0.032, 0.034), seg=16, ring=10)
        la_dist = sph(f"LADist_{side}", wr, (0.026, 0.024, 0.024), seg=14, ring=8)
        la_shell = join(f"LAShell_{side}", [la, la_prox, la_dist])
        remesh_smooth(la_shell, size=0.007, iterations=12, factor=0.58)
        set_mat(la_shell, base)
        add(f"LowerArm_{side}", la_shell, base)

        # Hands UNCHANGED from v0.7 anatomical_hand
        h = anatomical_hand(f"Hand_{side}", wr, hand, sx, base, joint)
        groups[f"Hand_{side}"].append(h)

    # --- Legs: deep sleeve overlap + flush hinges; no exposed balls ---
    for side, sx in (("L", 1), ("R", -1)):
        hip, kn, an, toe = leg_points(sx)
        dir_ul = (kn - hip).normalized()
        dir_ll = (an - kn).normalized()

        hipj = slim_hinge(f"HipJ_{side}", hip, axis="X", radius=0.034, thick=0.0016)
        add(f"UpperLeg_{side}", hipj, joint)

        thigh_a = hip - dir_ul * 0.048
        thigh_b = kn + dir_ul * 0.050
        thigh = tapered_limb(f"Thigh_{side}", thigh_a, thigh_b, 0.066, 0.045, v=26,
                             bulge=0.20, bulge_t=0.32)
        quad = sph(f"Quad_{side}", hip.lerp(kn, 0.35) + Vector((0, -0.016, 0)),
                   (0.058, 0.050, 0.070), seg=18, ring=10)
        kneecap = sph(f"Kneecap_{side}", kn + Vector((0, -0.022, 0.002)),
                      (0.026, 0.016, 0.022), seg=14, ring=8)
        kn_sleeve_ul = sph(f"ThKnSlv_{side}", kn, (0.042, 0.038, 0.040), seg=16, ring=10)
        hip_sleeve = sph(f"ThHipSlv_{side}", hip, (0.055, 0.052, 0.052), seg=16, ring=10)
        thigh_shell = join(f"ThighShell_{side}", [thigh, quad, kneecap, kn_sleeve_ul, hip_sleeve])
        remesh_smooth(thigh_shell, size=0.009, iterations=12, factor=0.58)
        set_mat(thigh_shell, base)
        add(f"UpperLeg_{side}", thigh_shell, base)

        if is_it:
            for i, tt in enumerate((0.28, 0.46, 0.64)):
                p = hip.lerp(kn, tt)
                half = 0.045 - i * 0.005
                cx = p.x + sx * 0.038
                cy = p.y - 0.065
                bars = chevron_v(f"ChT{side}{i}", cx, cy, p.z, half, bar_len=half * 1.10,
                                 thick=0.010, depth=0.011, ang_deg=34.0)
                for b in bars:
                    add(f"UpperLeg_{side}", b, accent)

        knj = slim_hinge(f"KneeJ_{side}", kn, axis="X", radius=0.026, thick=0.0015)
        add(f"LowerLeg_{side}", knj, joint)

        shin_a = kn - dir_ll * 0.045
        shin_b = an + dir_ll * 0.030
        shin = tapered_limb(f"Shin_{side}", shin_a, shin_b, 0.039, 0.027, v=22,
                            bulge=0.14, bulge_t=0.40)
        calf = sph(f"Calf_{side}", kn.lerp(an, 0.35) + Vector((0, 0.022, 0)),
                   (0.040, 0.046, 0.062), seg=16, ring=10)
        shin_prox = sph(f"ShinProx_{side}", kn, (0.038, 0.036, 0.038), seg=16, ring=10)
        shin_shell = join(f"ShinShell_{side}", [shin, calf, shin_prox])
        remesh_smooth(shin_shell, size=0.008, iterations=12, factor=0.58)
        set_mat(shin_shell, base)
        add(f"LowerLeg_{side}", shin_shell, base)

        ft = shoe_foot(f"Foot_{side}", an, toe, sx, base, joint, rubber_mat=rubber)
        groups[f"Foot_{side}"].append(ft)

    # v0.7.1: NO wear cylinders (soft-toy noise)

    return groups


def parent_groups(groups, arm_ob):
    for bone_name, objs in groups.items():
        if not objs:
            continue
        merged = join(f"Mesh_{bone_name}", objs)
        if merged is None:
            continue
        mw = merged.matrix_world.copy()
        merged.parent = arm_ob
        merged.parent_type = "BONE"
        merged.parent_bone = bone_name
        bpy.context.view_layer.update()
        bone = arm_ob.pose.bones[bone_name]
        merged.matrix_parent_inverse = (arm_ob.matrix_world @ bone.matrix).inverted()
        merged.matrix_world = mw


def make_mats(is_it):
    """Soft vinyl / skin-like satin SSS. Darker metal hinges. It = warm tan + black Vs."""
    suffix = "_It" if is_it else "_Tan"
    if is_it:
        base = mat(
            "Base", WARM_TAN, metallic=0.015, roughness=0.40, subsurface=0.18,
            ss_color=(0.90, 0.55, 0.35, 1.0), ss_radius=(1.0, 0.45, 0.20), emit=0.02)
    else:
        base = mat(
            "Base", BONE, metallic=0.012, roughness=0.38, subsurface=0.22,
            ss_color=(1.0, 0.80, 0.55, 1.0), ss_radius=(1.0, 0.50, 0.22), emit=0.04)
    accent = mat("Accent", BLACK if is_it else TEAL, roughness=0.44)
    over = mat("ItOverride", RIM if is_it else BONE, roughness=0.45, emit=0.0)
    joint = mat("Joint" + suffix, JOINT, metallic=0.70, roughness=0.32)
    sensor = mat("Sensor" + suffix, SENSOR, metallic=0.0, roughness=0.70)
    metal = mat("Metal" + suffix, METAL, metallic=0.80, roughness=0.28)
    rubber = mat("Rubber" + suffix, (0.08, 0.08, 0.09, 1.0), metallic=0.0, roughness=0.82)
    wear = mat("Wear" + suffix, (0.05, 0.045, 0.04, 1.0), metallic=0.0, roughness=0.88)
    base.name = "Base"
    accent.name = "Accent"
    over.name = "ItOverride"
    return (base, accent, over, joint, sensor, metal, rubber, wear)


def export_fbx(path, arm_ob):
    for name in list(bpy.data.objects.keys()):
        o = bpy.data.objects[name]
        if o.type in ("CAMERA", "LIGHT") or name in ("Ground", "KeySun", "Fill", "WarmFill",
                                                     "ShotCam", "RefPlane"):
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.select_all(action="DESELECT")
    arm_ob.select_set(True)
    for o in bpy.data.objects:
        if o.type == "MESH" and (o.parent == arm_ob or o.name.startswith("Mesh_")):
            o.select_set(True)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        mesh_smooth_type="FACE",
        use_armature_deform_only=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        armature_nodetype="NULL",
    )
    log(f"Exported {path} ({os.path.getsize(path)} bytes)")


def ensure_meta_guid(fbx_path, guid):
    """Keep existing .meta if present (preserve Unity importer + GUID); else write minimal."""
    meta = fbx_path + ".meta"
    if os.path.isfile(meta):
        with open(meta, "r") as f:
            text = f.read()
        if f"guid: {guid}" in text:
            log(f"Meta GUID intact {meta} guid={guid}")
            return
        # Rewrite GUID line only
        import re
        text2 = re.sub(r"guid: [0-9a-f]+", f"guid: {guid}", text, count=1)
        with open(meta, "w") as f:
            f.write(text2)
        log(f"Meta GUID restored {meta} guid={guid}")
        return
    content = f"""fileFormatVersion: 2
guid: {guid}
ModelImporter:
  serializedVersion: 22200
  internalIDToNameTable: []
  externalObjects: {{}}
  materials:
    materialImportMode: 2
    materialName: 0
    materialSearch: 1
    materialLocation: 1
  animations:
    legacyGenerateAnimations: 4
    bakeSimulation: 0
    resampleCurves: 1
    optimizeGameObjects: 0
    motionNodeName: 
  meshes:
    lODScreenPercentages: []
    globalScale: 1
    meshCompression: 0
    addColliders: 0
    useSRGBMaterialColor: 1
    sortHierarchyByName: 1
    importPhysicalCameras: 1
    importVisibility: 1
    importBlendShapes: 1
    importCameras: 1
    importLights: 1
    nodeNameCollisionStrategy: 1
    fileIdsGeneration: 2
    swapUVChannels: 0
    generateSecondaryUV: 0
    useFileUnits: 1
    keepQuads: 0
    weldVertices: 1
    bakeAxisConversion: 0
    preserveHierarchy: 1
    skinWeightsMode: 0
    maxBonesPerVertex: 4
    minBoneWeight: 0.001
    optimizeBones: 1
    meshOptimizationFlags: -1
    autoGenerateAvatarMappingIfUnspecified: 1
    animationType: 2
    humanoidOversampling: 1
    avatarSetup: 0
    addHumanoidExtraBoneInCheck: 0
    additionalBone: 0
  importAnimation: 0
  humanDescription:
    serializedVersion: 3
    rootMotionBoneName: 
    hasTranslationDoF: 0
    hasExtraRoot: 0
    skeleton: []
    rootMotionBoneIndex: -1
    hasExtraRoot: 0
  lastHumanDescriptionAvatarSource: {{instanceID: 0}}
  autoGenerateAvatarMappingIfUnspecified: 1
  animationType: 2
  humanoidOversampling: 1
  avatarSetup: 0
  addHumanoidExtraBoneInCheck: 0
  additionalBone: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(meta, "w") as f:
        f.write(content)
    log(f"Wrote meta {meta} guid={guid}")


def setup_render(engine="BLENDER_EEVEE_NEXT", res=1100):
    sc = bpy.context.scene
    available = {e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items}
    if engine not in available:
        if "BLENDER_EEVEE" in available:
            engine = "BLENDER_EEVEE"
        else:
            engine = "BLENDER_WORKBENCH"
    sc.render.engine = engine
    sc.render.resolution_x = res
    sc.render.resolution_y = res
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.film_transparent = False
    if engine == "BLENDER_WORKBENCH":
        sc.display.shading.light = "STUDIO"
        sc.display.shading.color_type = "MATERIAL"
        sc.display.shading.show_shadows = True
    else:
        sc.world = bpy.data.worlds.new("WarmWorld") if "WarmWorld" not in bpy.data.worlds else bpy.data.worlds["WarmWorld"]
        sc.world.use_nodes = True
        bg = sc.world.node_tree.nodes.get("Background")
        if bg:
            bg.inputs[0].default_value = (0.70, 0.68, 0.64, 1.0)
            bg.inputs[1].default_value = 0.65
    if "Ground" not in bpy.data.objects:
        bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, 0))
        g = bpy.context.active_object
        g.name = "Ground"
        gm = mat("GroundMat", (0.55, 0.54, 0.52, 1), roughness=0.92)
        set_mat(g, gm)
    if "KeySun" not in bpy.data.objects:
        bpy.ops.object.light_add(type="SUN", location=(2.5, -3.5, 5.5))
        sun = bpy.context.active_object
        sun.name = "KeySun"
        sun.data.energy = 3.4
        sun.data.color = (1.0, 0.92, 0.82)
        sun.rotation_euler = (math.radians(48), math.radians(12), math.radians(-18))
        bpy.ops.object.light_add(type="AREA", location=(-2.2, 2.2, 3.2))
        fill = bpy.context.active_object
        fill.name = "Fill"
        fill.data.energy = 60
        fill.data.size = 3.5
        fill.data.color = (1.0, 0.96, 0.92)
        bpy.ops.object.light_add(type="AREA", location=(0.5, -1.5, 2.0))
        warm = bpy.context.active_object
        warm.name = "WarmFill"
        warm.data.energy = 50
        warm.data.size = 2.0
        warm.data.color = (1.0, 0.85, 0.65)


def place_camera(loc, look_at=(0, 0, 1.0)):
    if "ShotCam" in bpy.data.objects:
        cam = bpy.data.objects["ShotCam"]
    else:
        bpy.ops.object.camera_add()
        cam = bpy.context.active_object
        cam.name = "ShotCam"
    cam.location = loc
    direction = Vector(look_at) - Vector(loc)
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = cam
    return cam


def reset_pose(arm_ob):
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="POSE")
    for pb in arm_ob.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)
        pb.scale = (1, 1, 1)
    bpy.ops.object.mode_set(mode="OBJECT")
    arm_ob.location = (0, 0, 0)


def set_bone_euler(arm_ob, name, euler_deg):
    pb = arm_ob.pose.bones.get(name)
    if not pb:
        log(f"WARN missing bone {name}")
        return
    pb.rotation_mode = "XYZ"
    pb.rotation_euler = Euler(tuple(math.radians(a) for a in euler_deg), "XYZ")


def pose_idle(arm_ob):
    reset_pose(arm_ob)
    set_bone_euler(arm_ob, "Spine", (2, 0, 0))
    set_bone_euler(arm_ob, "LowerArm_L", (-8, 0, 0))
    set_bone_euler(arm_ob, "LowerArm_R", (-8, 0, 0))


def hand_world(arm_ob, side="L"):
    pb = arm_ob.pose.bones.get(f"Hand_{side}")
    if not pb:
        return Vector((0.35 if side == "L" else -0.35, -0.25, 1.05))
    return Vector(arm_ob.matrix_world @ pb.tail)


def render_shot(path, cam_loc, look=(0, 0, 1.05)):
    setup_render()
    place_camera(cam_loc, look)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    log(f"Still {path}")


def verify_bones(arm_ob):
    required = [
        "Hips", "Spine", "Head",
        "UpperArm_L", "UpperArm_R", "LowerArm_L", "LowerArm_R",
        "Hand_L", "Hand_R",
        "UpperLeg_L", "UpperLeg_R", "LowerLeg_L", "LowerLeg_R",
    ]
    names = [b.name for b in arm_ob.data.bones]
    missing = [n for n in required if n not in names]

    def parent_of(n):
        b = arm_ob.data.bones.get(n)
        return b.parent.name if b and b.parent else None

    checks = [
        ("LowerArm_L under UpperArm_L", parent_of("LowerArm_L") == "UpperArm_L"),
        ("LowerArm_R under UpperArm_R", parent_of("LowerArm_R") == "UpperArm_R"),
        ("Hand_L under LowerArm_L", parent_of("Hand_L") == "LowerArm_L"),
        ("Hand_R under LowerArm_R", parent_of("Hand_R") == "LowerArm_R"),
        ("LowerLeg_L under UpperLeg_L", parent_of("LowerLeg_L") == "UpperLeg_L"),
        ("LowerLeg_R under UpperLeg_R", parent_of("LowerLeg_R") == "UpperLeg_R"),
        ("UpperLeg_L under Hips", parent_of("UpperLeg_L") == "Hips"),
        ("Spine under Hips", parent_of("Spine") == "Hips"),
        ("Chest under Spine", parent_of("Chest") == "Spine"),
        ("Neck under Chest", parent_of("Neck") == "Chest"),
        ("Head under Neck", parent_of("Head") == "Neck"),
        ("Shoulder_L under Chest", parent_of("Shoulder_L") == "Chest"),
        ("UpperArm_L under Shoulder_L", parent_of("UpperArm_L") == "Shoulder_L"),
        ("Foot_L under LowerLeg_L", parent_of("Foot_L") == "LowerLeg_L"),
        ("Mesh_Head parent Head", True),
    ]
    # Mesh_Head parent check
    mesh_head = bpy.data.objects.get("Mesh_Head")
    mh_ok = (mesh_head is not None
             and mesh_head.parent == arm_ob
             and mesh_head.parent_bone == "Head")
    checks.append(("Mesh_Head under Head bone", mh_ok))
    log(f"Bones present: {names}")
    log(f"Missing required: {missing}")
    for label, ok in checks:
        log(f"  hierarchy {label}: {'OK' if ok else 'FAIL'}")
    return not missing and all(ok for _, ok in checks)


def measure_a_pose(arm_ob):
    sh_l = Vector(arm_ob.matrix_world @ arm_ob.pose.bones["UpperArm_L"].head)
    el_l = Vector(arm_ob.matrix_world @ arm_ob.pose.bones["UpperArm_L"].tail)
    hand_l = Vector(arm_ob.matrix_world @ arm_ob.pose.bones["Hand_L"].tail)
    ua = (el_l - sh_l).normalized()
    vertical = Vector((0, 0, -1))
    ang = math.degrees(ua.angle(vertical))
    clear_x = abs(hand_l.x) - 0.18
    clear_y = hand_l.y
    log(f"A-pose UpperArm_L angle off vertical: {ang:.1f} deg")
    log(f"Hand_L world: {tuple(round(c, 3) for c in hand_l)} clear_x={clear_x:.3f}m y={clear_y:.3f}")
    return ang, clear_x, clear_y


def build_variant(is_it, export_path, guid, do_stills=False):
    clear_scene()
    mats = make_mats(is_it)
    arm_ob = build_armature()
    groups = build_mesh_parts(is_it, mats)
    parent_groups(groups, arm_ob)
    ok = verify_bones(arm_ob)
    ang, cx, cy = measure_a_pose(arm_ob)

    tag = "it_" if is_it else ""
    if do_stills:
        pose_idle(arm_ob)
        render_shot(f"{PREV}/hipoly_v71_{tag}idle_front.png",
                    (0.12, -3.2, 1.35), (0, 0, 1.00))
        render_shot(f"{PREV}/hipoly_v71_{tag}idle_34.png",
                    (2.1, -2.5, 1.40), (0, 0, 1.05))
        hl = hand_world(arm_ob, "L")
        render_shot(
            f"{PREV}/hipoly_v71_{tag}hand_close.png",
            (hl.x + 0.32, hl.y - 0.38, hl.z + 0.14),
            (hl.x - 0.02, hl.y + 0.02, hl.z - 0.01),
        )
        # Optional face close
        render_shot(
            f"{PREV}/hipoly_v71_{tag}face_close.png",
            (0.08, -0.55, HEAD_Z + 0.02),
            (0.0, -0.05, HEAD_Z - 0.02),
        )
        reset_pose(arm_ob)

    export_fbx(export_path, arm_ob)
    ensure_meta_guid(export_path, guid)
    bpy.ops.wm.save_as_mainfile(
        filepath=BLEND.replace(".blend", "_It.blend" if is_it else "_Tan.blend")
    )
    return ok, ang, cx, cy


def write_readme():
    path = os.path.join(OUT_DIR, "README.md")
    text = """# HiPoly Hierarchical Mannequins

DummyLocomotor-bindable **athletic store mannequin** (v0.7.1 body topography smooth).
NOT Hybrid III toy kit / sphere-palm / cylinder-finger / egg-head / LEGO hinges.

## Assets
| File | Paint |
|------|-------|
| `Dummy_Mannequin_Tan_Hier_Hi.fbx` | Runner — cream `#E8D9C0`, Accent teal tick. **ZERO nested Vs.** |
| `Dummy_Mannequin_Orange_Hier_Hi.fbx` | It — warm tan body + black nested Vs chest + outer thighs |

## Bind pose
- Mild A-pose ~20–30°; hands clear pelvis.
- Molded human face: brow, nose, lip volume, chin, ears; flat dark eye insets — no orbs/makeup/goatee.
- Continuous athletic torso/limbs; deep sleeve joint overlaps; nearly-flush slim metal hinges.
- Anatomical hands: flattened palm, knuckled fingers, opposed thumb + thenar; parented to Hand_L/R.
- Materials: soft vinyl SSS Base / Accent / ItOverride + Joint metal + Rubber soles.

## Bone hierarchy (DummyLocomotor — names unchanged)
`Root` → `Hips` → `Spine` → `Chest` → `Neck` → `Head`  
`Hips` → `UpperLeg_L/R` → `LowerLeg_L/R` → `Foot_L/R`  
`Chest` → `Shoulder_L/R` → `UpperArm_L/R` → `LowerArm_L/R` → `Hand_L/R`

## Export
`-Z` forward, `+Y` up. Materials: `Base`, `Accent`, `ItOverride`.
"""
    with open(path, "w") as f:
        f.write(text)
    log(f"Wrote {path}")


def main():
    log("=== hipoly hier v8.1 / realism v0.7.1 body topography smooth ===")
    tan = os.path.join(OUT_DIR, "Dummy_Mannequin_Tan_Hier_Hi.fbx")
    orn = os.path.join(OUT_DIR, "Dummy_Mannequin_Orange_Hier_Hi.fbx")

    log("Building Tan/Runner Hier HiPoly v0.7.1…")
    ok_t, ang_t, cx_t, cy_t = build_variant(False, tan, GUID_TAN, do_stills=True)

    log("Building Orange/It Hier HiPoly v0.7.1…")
    ok_o, ang_o, cx_o, cy_o = build_variant(True, orn, GUID_ORANGE, do_stills=True)

    write_readme()
    log(f"Tan OK={ok_t} A-pose={ang_t:.1f}deg clear_x={cx_t:.3f} clear_y={cy_t:.3f}")
    log(f"It  OK={ok_o} A-pose={ang_o:.1f}deg clear_x={cx_o:.3f} clear_y={cy_o:.3f}")
    log(f"Tan FBX {os.path.getsize(tan)} bytes guid={GUID_TAN}")
    log(f"Orange FBX {os.path.getsize(orn)} bytes guid={GUID_ORANGE}")
    log("DONE v0.7.1 body topo smooth — no git push (await AD approve)")


if __name__ == "__main__":
    main()
