#!/usr/bin/env python3
"""
HiPoly hierarchical mannequin v5.1 — Hybrid III refine v0.4.1

MIDDLE PATH (critical):
  v0.3 = toy robot (fail)
  v0.4 = smooth fashion mannequin (fail) — continuous remesh melted segmentation
  v0.4.1 = Hybrid III SEGMENTATION + human athletic mass

Rebuild from Hybrid III segmentation language — NOT a smooth remesh of v0.4.
Distinct vinyl shells with soft bead/lip seams, large hinge disks, large cals,
inset bellows, 4 neck rings, separated fingers, warm bone tan in stills.

DummyLocomotor bones unchanged. GUID-safe FBX overwrite. NO git push.
"""
import bpy
import math
import os
import uuid
from mathutils import Vector, Euler

OUT_DIR = "/workspace/tag-unity/Assets/Art/Characters/HiPoly"
PREV = "/workspace/art-build/previews"
BLEND = "/workspace/art-build/Dummy_Mannequin_Hier_Hi.blend"
LOG = "/tmp/hipoly_v51_build.log"
REF_CRASH = "/workspace/tag-gdd/art/refs/hybrid-iii-v03/ref_hybrid_iii_crash_dummy.jpg"

GUID_TAN = "ad3f2fa97db94e72869d746ecdf8e87d"
GUID_ORANGE = "b33974ad57284ef28a7564e3bdf00540"

# Warmer bone tan so workbench/EEVEE stills read #E8D9C0 warm, not cool grey.
# Slightly pushed toward warm ochre in linear-ish display.
BONE = (0.98, 0.90, 0.72, 1.0)         # warmer stills read of #E8D9C0
TEAL = (0.169, 0.702, 0.639, 1.0)      # #2BB3A3
ORANGE = (1.0, 0.416, 0.0, 1.0)        # #FF6A00
BLACK = (0.04, 0.04, 0.045, 1.0)
JOINT = (0.10, 0.10, 0.12, 1.0)        # dark metal hinges
METAL = (0.32, 0.32, 0.34, 1.0)        # neck rings — readable dark metal
DARK_BELLOWS = (0.06, 0.06, 0.07, 1.0)
RIM = (1.0, 0.45, 0.05, 1.0)
SENSOR = (0.02, 0.02, 0.02, 1.0)
CAL_YELLOW = (0.95, 0.82, 0.08, 1.0)
LIP = (0.12, 0.12, 0.13, 1.0)          # soft bead/lip seam dark edge

SEG = 36
RING = 18
CYL_V = 28

# Mild A-pose 20–35°; hands clear pelvis
SHOULDER_Z = 1.50
SHOULDER_X = 0.295
UA_LEN = 0.335
LA_LEN = 0.295
ARM_OUT = math.radians(28.0)
ARM_FWD = math.radians(14.0)

HIP_Z = 0.97
HIP_X = 0.125
UL_LEN = 0.465
LL_LEN = 0.425

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


def mat(name, color, metallic=0.02, roughness=0.48, emit=0.0, subsurface=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.diffuse_color = color
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    # Slight subsurface / warm push for Runner vinyl stills
    if subsurface > 0:
        if "Subsurface Weight" in bsdf.inputs:
            bsdf.inputs["Subsurface Weight"].default_value = subsurface
            if "Subsurface Radius" in bsdf.inputs:
                bsdf.inputs["Subsurface Radius"].default_value = (1.0, 0.55, 0.25)
            if "Subsurface Color" in bsdf.inputs:
                bsdf.inputs["Subsurface Color"].default_value = (
                    min(1.0, color[0] * 1.05),
                    min(1.0, color[1] * 0.95),
                    min(1.0, color[2] * 0.70),
                    1.0,
                )
        elif "Subsurface" in bsdf.inputs:
            bsdf.inputs["Subsurface"].default_value = subsurface
    if emit > 0:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = color
            bsdf.inputs["Emission Strength"].default_value = emit
        elif "Emission" in bsdf.inputs:
            bsdf.inputs["Emission"].default_value = (*color[:3], 1.0)
    return m


def set_mat(ob, m):
    ob.data.materials.clear()
    ob.data.materials.append(m)


def shade_smooth(ob):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.shade_smooth()
    if hasattr(ob.data, "use_auto_smooth"):
        ob.data.use_auto_smooth = True
        ob.data.auto_smooth_angle = math.radians(42)


def apply_mod(ob, mod_name):
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod_name)


def apply_subsurf(ob, levels=1):
    mod = ob.modifiers.new("SubD", "SUBSURF")
    mod.levels = levels
    mod.render_levels = levels
    apply_mod(ob, mod.name)


def apply_bevel(ob, width=0.010, segments=3):
    mod = ob.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(28)
    apply_mod(ob, mod.name)


def apply_smooth_corrective(ob, factor=0.5, iterations=8):
    mod = ob.modifiers.new("Smooth", "SMOOTH")
    mod.factor = factor
    mod.iterations = iterations
    apply_mod(ob, mod.name)


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


def torus_ring(name, loc, major=0.08, minor=0.012, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major, minor_radius=minor,
        major_segments=28, minor_segments=10, location=loc)
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
        apply_bevel(o, width=bevel, segments=3)
    shade_smooth(o)
    return o


def join(name, objs):
    objs = [o for o in objs if o is not None]
    if not objs:
        return None
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    objs[0].name = name
    return objs[0]


def light_smooth(ob, iterations=4):
    """Light smooth only — preserve shell edges / segmentation. NO voxel remesh."""
    apply_smooth_corrective(ob, factor=0.35, iterations=iterations)
    shade_smooth(ob)
    return ob


def tapered_limb(name, a, b, r0, r1, v=CYL_V):
    """Segmented limb shell volume (upper→lower taper). Soft end caps, NO remesh."""
    a, b = Vector(a), Vector(b)
    mid = (a + b) * 0.5
    direction = b - a
    length = direction.length
    if length < 1e-6:
        return sph(name, mid, r0)
    body = cone(name, mid, r0, r1, max(length * 0.88, 0.05), v=v)
    quat = direction.normalized().to_track_quat("Z", "Y")
    body.rotation_euler = quat.to_euler()
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    c0 = sph(f"{name}_cap0", a, r0 * 0.92, seg=16, ring=8)
    c1 = sph(f"{name}_cap1", b, r1 * 0.92, seg=16, ring=8)
    return join(name, [body, c0, c1])


def bead_lip(name, loc, radius, axis="Z", thick=0.012, flare=1.12):
    """Soft Hybrid III bead/lip seam at shell terminus — NOT Lego brick edge."""
    if axis == "Z":
        rot = (0, 0, 0)
    elif axis == "X":
        rot = (0, math.radians(90), 0)
    else:
        rot = (math.radians(90), 0, 0)
    outer = cyl(f"{name}_outer", loc, radius * flare, thick, rot=rot, v=28)
    inner = cyl(f"{name}_inner", loc, radius * 0.92, thick * 1.15, rot=rot, v=24)
    return join(name, [outer, inner])


def hinge_disk(name, loc, axis="X", radius=0.078, thick=0.032, rivets=4):
    """LARGE dark metal Hybrid III hinge disk — readable at chase-cam distance.
    v0.4 hinges were too tiny; these match ref scale."""
    if axis == "X":
        rot = (0, math.radians(90), 0)
        riv_plane = lambda ang, r: Vector((0, math.cos(ang) * r, math.sin(ang) * r))
        face_nudge = Vector((thick * 0.55, 0, 0))
    elif axis == "Y":
        rot = (math.radians(90), 0, 0)
        riv_plane = lambda ang, r: Vector((math.cos(ang) * r, 0, math.sin(ang) * r))
        face_nudge = Vector((0, thick * 0.55, 0))
    else:
        rot = (0, 0, 0)
        riv_plane = lambda ang, r: Vector((math.cos(ang) * r, math.sin(ang) * r, 0))
        face_nudge = Vector((0, 0, thick * 0.55))
    core = sph(f"{name}_core", loc, radius * 0.48, seg=14, ring=7)
    disk = cyl(f"{name}_disk", loc, radius, thick, rot=rot, v=28)
    rim = cyl(f"{name}_rim", loc, radius * 1.08, thick * 0.42, rot=rot, v=28)
    pin = cyl(f"{name}_pin", loc, thick * 0.32, thick * 1.35, rot=rot, v=12)
    parts = [core, disk, rim, pin]
    for i in range(rivets):
        ang = (2 * math.pi * i) / rivets + math.radians(18)
        offset = riv_plane(ang, radius * 0.62) + face_nudge * 0.75
        rv = sph(f"{name}_riv{i}", Vector(loc) + offset, 0.012, seg=8, ring=4)
        parts.append(rv)
    return join(name, parts)


def cal_quadrant_disk(name, loc, radius=0.048, thick=0.012, accent_mat=None, black_mat=None, axis="Y"):
    """LARGE Hybrid III quadrant cal — real ref size, not micro ticks."""
    if axis == "Y":
        rot = (math.radians(90), 0, 0)
    elif axis == "X":
        rot = (0, math.radians(90), 0)
    else:
        rot = (0, 0, 0)
    base = cyl(f"{name}_base", loc, radius, thick, rot=rot, v=28)
    set_mat(base, black_mat)
    q = radius * 0.42
    if axis == "Y":
        w1 = cube(f"{name}_q1",
                  Vector(loc) + Vector((radius * 0.30, -thick * 0.55, radius * 0.30)),
                  (q, thick * 0.38, q), bevel=0.001)
        w2 = cube(f"{name}_q2",
                  Vector(loc) + Vector((-radius * 0.30, -thick * 0.55, -radius * 0.30)),
                  (q, thick * 0.38, q), bevel=0.001)
    elif axis == "X":
        w1 = cube(f"{name}_q1",
                  Vector(loc) + Vector((thick * 0.55, radius * 0.30, radius * 0.30)),
                  (thick * 0.38, q, q), bevel=0.001)
        w2 = cube(f"{name}_q2",
                  Vector(loc) + Vector((thick * 0.55, -radius * 0.30, -radius * 0.30)),
                  (thick * 0.38, q, q), bevel=0.001)
    else:
        w1 = cube(f"{name}_q1",
                  Vector(loc) + Vector((radius * 0.30, radius * 0.30, thick * 0.55)),
                  (q, q, thick * 0.38), bevel=0.001)
        w2 = cube(f"{name}_q2",
                  Vector(loc) + Vector((-radius * 0.30, -radius * 0.30, thick * 0.55)),
                  (q, q, thick * 0.38), bevel=0.001)
    set_mat(w1, accent_mat)
    set_mat(w2, accent_mat)
    return join(name, [base, w1, w2])


def chevron_v(tag, cx, cy, cz, half_w, bar_len, thick=0.012, depth=0.014, ang_deg=36.0):
    ang = math.radians(ang_deg)
    drop = math.sin(ang) * bar_len * 0.55
    spread = math.cos(ang) * bar_len * 0.45
    left = cube(f"{tag}_L", (cx - spread, cy, cz + drop * 0.15),
                (bar_len * 0.5, depth, thick), (0, ang, 0))
    right = cube(f"{tag}_R", (cx + spread, cy, cz + drop * 0.15),
                 (bar_len * 0.5, depth, thick), (0, -ang, 0))
    return [left, right]


def waist_bellows(name, z_top, z_bot, radius=0.118, n_ribs=8):
    """Dark inset waist bellows ~6–8 fine ribs between chest plate and pelvis shell."""
    parts = []
    span = z_top - z_bot
    # Dark core cylinder first so gaps between ribs stay dark
    core = cyl(f"{name}_core", (0, 0.01, (z_top + z_bot) * 0.5),
               radius * 0.78, span * 0.98, v=24)
    parts.append(core)
    for i in range(n_ribs):
        t = (i + 0.5) / n_ribs
        z = z_top - t * span
        # Alternating major/minor Hybrid III bellows ridges
        r = radius * (1.10 if i % 2 == 0 else 0.86)
        depth = span / n_ribs * (0.55 if i % 2 == 0 else 0.38)
        rib = cyl(f"{name}_rib{i}", (0, 0.01, z), r, depth, v=28)
        parts.append(rib)
    return join(name, parts)


def neck_ring_stack(name, z_base, n=4, major=0.082, minor=0.016, spacing=0.028):
    """4 stacked dark metal neck rings — MUST read in front + profile like Hybrid III."""
    parts = []
    for i in range(n):
        z = z_base + i * spacing
        maj = major * (1.0 - i * 0.025)
        # Thick torus ring
        ring = torus_ring(f"{name}_r{i}", (0, 0, z), major=maj, minor=minor)
        parts.append(ring)
        # Flat disk face so rings read as stacked plates from front
        disk = cyl(f"{name}_d{i}", (0, 0, z), maj * 1.02, minor * 1.1, v=28)
        parts.append(disk)
        if i < n - 1:
            filler = cyl(f"{name}_f{i}", (0, 0, z + spacing * 0.5),
                         maj * 0.70, spacing * 0.22, v=18)
            parts.append(filler)
    col = cyl(f"{name}_col", (0, 0, z_base + (n - 1) * spacing * 0.5),
              major * 0.42, (n - 1) * spacing + 0.04, v=14)
    parts.append(col)
    return join(name, parts)


def u_knee_fork(name, kn, sx):
    """UpperLeg distal U-nest — LowerLeg pivots inside."""
    left = sph(f"{name}_padL",
               kn + Vector((-0.040, 0.0, 0.012)),
               (0.034, 0.050, 0.050), seg=14, ring=7)
    right = sph(f"{name}_padR",
                kn + Vector((0.040, 0.0, 0.012)),
                (0.034, 0.050, 0.050), seg=14, ring=7)
    bridge = sph(f"{name}_bridge",
                 kn + Vector((0, 0.0, 0.050)),
                 (0.062, 0.054, 0.032), seg=14, ring=7)
    return join(name, [left, right, bridge])


def hybrid_hand(name, wr, hand, sx, base_mat, joint_mat):
    """Thumb + SEPARATED simple fingers (closer to Hybrid III). Fused paddle = weak."""
    parts = []
    wrj = hinge_disk(f"{name}_WristJ", wr, axis="X", radius=0.042, thick=0.020, rivets=3)
    set_mat(wrj, joint_mat)
    parts.append(wrj)

    palm = sph(f"{name}_Palm",
               hand + Vector((0, -0.008, 0.008)),
               (0.042, 0.030, 0.048), seg=18, ring=10)
    set_mat(palm, base_mat)
    parts.append(palm)

    # Four separated simple fingers (not fused paddle)
    finger_offsets = [
        (-0.028, -0.055, -0.010),
        (-0.010, -0.070, -0.012),
        (0.010, -0.072, -0.012),
        (0.028, -0.055, -0.010),
    ]
    tip_extra = [
        (-0.030, -0.105, -0.038),
        (-0.010, -0.118, -0.042),
        (0.010, -0.120, -0.042),
        (0.030, -0.105, -0.038),
    ]
    for i, (ox, oy, oz) in enumerate(finger_offsets):
        start = hand + Vector((sx * abs(ox) if ox == 0 else ox, oy * 0.35, oz * 0.4))
        # keep lateral spread absolute (already signed)
        start = hand + Vector((ox, oy * 0.30, oz * 0.35))
        end = hand + Vector(tip_extra[i])
        r0 = 0.013 if i in (1, 2) else 0.012
        r1 = 0.009
        fing = tapered_limb(f"{name}_F{i}", start, end, r0, r1, v=10)
        set_mat(fing, base_mat)
        parts.append(fing)
        tip = sph(f"{name}_Tip{i}", end, 0.010, seg=8, ring=4)
        set_mat(tip, base_mat)
        parts.append(tip)

    # Separate thumb
    thumb = tapered_limb(
        f"{name}_Thumb",
        hand + Vector((sx * 0.018, 0.008, 0.012)),
        hand + Vector((sx * 0.070, 0.028, -0.038)),
        0.015, 0.010, v=10)
    set_mat(thumb, base_mat)
    parts.append(thumb)
    return join(name, parts)


def shoe_foot(name, an, toe, sx, base_mat, joint_mat):
    """Heel/toe shoe pads with LARGE ankle hinge."""
    parts = []
    anj = hinge_disk(f"{name}_AnkleJ", an, axis="X", radius=0.052, thick=0.024, rivets=3)
    set_mat(anj, joint_mat)
    parts.append(anj)
    heel = sph(f"{name}_Heel",
               an + Vector((0, 0.038, -0.028)),
               (0.042, 0.040, 0.036), seg=14, ring=7)
    set_mat(heel, base_mat)
    parts.append(heel)
    mid = sph(f"{name}_Mid",
              an + Vector((0, -0.042, -0.030)),
              (0.048, 0.078, 0.028), seg=16, ring=8)
    set_mat(mid, base_mat)
    parts.append(mid)
    toe_pad = sph(f"{name}_Toe",
                  an + Vector((0, -0.130, -0.018)),
                  (0.036, 0.044, 0.022), seg=12, ring=6)
    set_mat(toe_pad, base_mat)
    parts.append(toe_pad)
    sole = sph(f"{name}_Sole",
               an + Vector((0, -0.045, -0.050)),
               (0.044, 0.098, 0.011), seg=14, ring=6)
    set_mat(sole, base_mat)
    parts.append(sole)
    instep = sph(f"{name}_Instep",
                 an + Vector((0, -0.072, -0.008)),
                 (0.036, 0.042, 0.024), seg=12, ring=6)
    set_mat(instep, base_mat)
    parts.append(instep)
    return join(name, parts)


def segmented_chest_shell(name):
    """DISTINCT chest plate vinyl shell with soft bottom bead lip.
    Athletic pec/delt mass UNDER the shell — NOT remeshed into fashion mannequin."""
    parts = []
    # Main thoracic plate — slightly flattened athletic chest
    thorax = sph(f"{name}_Thorax", (0, 0.015, 1.40), (0.225, 0.140, 0.175), seg=36, ring=18)
    parts.append(thorax)
    # Soft lower chest taper ending ABOVE bellows (distinct shell end)
    lower = sph(f"{name}_Lower", (0, 0.012, 1.22), (0.195, 0.125, 0.090), seg=28, ring=14)
    parts.append(lower)
    # Pec volumes (athletic mass under vinyl)
    for sx in (1, -1):
        pec = sph(f"{name}_Pec{sx}", (sx * 0.090, -0.088, 1.42),
                  (0.095, 0.055, 0.070), seg=18, ring=10)
        parts.append(pec)
    # Lat / side volume
    for sx in (1, -1):
        lat = sph(f"{name}_Lat{sx}", (sx * 0.180, 0.015, 1.34),
                  (0.065, 0.085, 0.100), seg=16, ring=8)
        parts.append(lat)
    # Deltoid roots as part of chest plate (Hybrid III shoulder shelf)
    for sx in (1, -1):
        delt = sph(f"{name}_Delt{sx}", (sx * 0.225, 0.0, 1.47),
                   (0.090, 0.080, 0.085), seg=18, ring=10)
        parts.append(delt)
    # Soft bottom overhang lip toward bellows — DISTINCT shell terminus
    flare = sph(f"{name}_Flare", (0, 0.01, 1.145), (0.185, 0.120, 0.040), seg=24, ring=10)
    parts.append(flare)

    shell = join(name, parts)
    light_smooth(shell, iterations=5)
    return shell


def segmented_pelvis_shell(name):
    """DISTINCT pelvis vinyl shell with soft top bead lip — NOT continuous with chest."""
    parts = []
    bowl = sph(f"{name}_Bowl", (0, 0.02, 0.92), (0.198, 0.142, 0.112), seg=32, ring=16)
    parts.append(bowl)
    # Upper rim that meets bellows from below
    upper = sph(f"{name}_Upper", (0, 0.015, 0.990), (0.175, 0.125, 0.045), seg=24, ring=10)
    parts.append(upper)
    for sx in (1, -1):
        wing = sph(f"{name}_Wing{sx}", (sx * 0.158, 0.02, 0.93),
                   (0.088, 0.095, 0.082), seg=18, ring=10)
        parts.append(wing)
    lower = sph(f"{name}_Lower", (0, 0.01, 0.84), (0.118, 0.098, 0.052), seg=20, ring=10)
    parts.append(lower)

    shell = join(name, parts)
    light_smooth(shell, iterations=5)
    return shell


def limb_shell_with_lips(name, a, b, r0, r1, lip_mat=None):
    """Upper or lower limb shell with soft bead lips at both ends — segmented read."""
    a, b = Vector(a), Vector(b)
    direction = (b - a).normalized()
    body = tapered_limb(name + "_body", a, b, r0, r1, v=24)
    # Bead lips near ends (inset slightly so hinge disks sit outside)
    lip0_loc = a + direction * 0.018
    lip1_loc = b - direction * 0.018
    # Orient lips perpendicular to limb axis
    # Use torus-like cyl via bead_lip with best-effort axis
    # Prefer face-on: if limb mostly vertical use Z, else approximate with X
    axis = "Z" if abs(direction.z) > 0.7 else "X"
    lip0 = bead_lip(f"{name}_lip0", lip0_loc, r0 * 1.02, axis=axis, thick=0.011, flare=1.10)
    lip1 = bead_lip(f"{name}_lip1", lip1_loc, r1 * 1.02, axis=axis, thick=0.011, flare=1.10)
    shell = join(name, [body, lip0, lip1])
    light_smooth(shell, iterations=3)
    return shell


def arm_points(sx):
    sh = Vector((sx * SHOULDER_X, 0.0, SHOULDER_Z))
    out = math.sin(ARM_OUT)
    down = math.cos(ARM_OUT)
    fwd = math.sin(ARM_FWD)
    dir_ua = Vector((sx * out, -fwd, -down)).normalized()
    el = sh + dir_ua * UA_LEN
    dir_la = (dir_ua + Vector((sx * 0.08, -0.22, -0.10))).normalized()
    dir_la = Vector((sx * abs(dir_la.x) + sx * 0.05, dir_la.y - 0.05, dir_la.z)).normalized()
    wr = el + dir_la * LA_LEN
    hand = wr + Vector((sx * 0.015, -0.050, -0.050))
    return sh, el, wr, hand


def leg_points(sx):
    hip = Vector((sx * HIP_X, 0.02, HIP_Z))
    kn = hip + Vector((sx * 0.012, 0.020, -UL_LEN))
    an = kn + Vector((0.0, 0.0, -LL_LEN))
    toe = an + Vector((0.0, -0.13, -0.015))
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

    bone("Hips", "Root", (0, 0, HIP_Z - 0.04), (0, 0, HIP_Z + 0.10))
    bone("Spine", "Hips", (0, 0, HIP_Z + 0.10), (0, 0, 1.18))
    bone("Chest", "Spine", (0, 0, 1.18), (0, 0, SHOULDER_Z))
    bone("Neck", "Chest", (0, 0, SHOULDER_Z), (0, 0, 1.58))
    bone("Head", "Neck", (0, 0, 1.58), (0, 0, 1.90))

    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr, hand = arm_points(sx)
        bone(f"Shoulder_{side}", "Chest", (sx * 0.12, 0, SHOULDER_Z), sh)
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
    """v0.4.1 — Hybrid III SEGMENTATION + athletic mass. NO continuous remesh."""
    base, accent, over, joint, sensor, metal, bellows_mat, cal_accent, lip_mat = mats
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

    # --- Head: egg + 2 eye dots + temple row of 3 (NO visor / painted face) ---
    head = sph("Head", (0, -0.01, 1.73), (0.165, 0.148, 0.200), seg=36, ring=18, sub=True)
    add("Head", head, base)
    for dx in (-0.048, 0.048):
        e = sph(f"Eye_{dx}", (dx, -0.140, 1.735), (0.020, 0.010, 0.020), seg=12, ring=6)
        add("Head", e, sensor)
    for i, z in enumerate([1.785, 1.735, 1.685]):
        t = sph(f"Temple_{i}", (0.152, -0.050, z), 0.013, seg=10, ring=5)
        add("Head", t, sensor)

    # LARGE temple cal (Hybrid III size) — protrude for front+3/4 read
    cal_t = cal_quadrant_disk(
        "CalTemple", (-0.175, -0.020, 1.75),
        radius=0.048, thick=0.014,
        accent_mat=cal_accent, black_mat=sensor, axis="X")
    groups["Head"].append(cal_t)

    # --- Neck: 4 stacked dark metal rings — MUST read front + profile ---
    neck = neck_ring_stack("NeckRings", z_base=1.520, n=4, major=0.082, minor=0.016, spacing=0.028)
    add("Neck", neck, metal)

    # --- DISTINCT CHEST PLATE (segmented, soft bottom lip) ---
    chest = segmented_chest_shell("ChestShell")
    add("Chest", chest, base)

    # Shoulder plate seams (dark bead at delt roots)
    for side, sx in (("L", 1), ("R", -1)):
        seam = bead_lip(
            f"ShoulderSeam_{side}",
            (sx * 0.255, 0.0, 1.470),
            radius=0.072, thick=0.012, axis="X", flare=1.10)
        add("Chest", seam, joint)

    # LARGE chest cal
    cal_c = cal_quadrant_disk(
        "CalChest", (0.085, -0.160, 1.44),
        radius=0.050, thick=0.012,
        accent_mat=cal_accent, black_mat=sensor, axis="Y")
    groups["Chest"].append(cal_c)

    # Thin teal tick ONLY on Runner — NOT fat racing stripe
    if not is_it:
        tick = cube("ChestTick", (0.0, -0.162, 1.30), (0.040, 0.005, 0.006), bevel=0.001)
        add("Chest", tick, accent)

    # Soft DARK bead lips at shell termini (Hybrid III seam — not tan faux-ribs)
    chest_lip = bead_lip("ChestBotLip", (0, 0.01, 1.125), radius=0.178, axis="Z",
                         thick=0.012, flare=1.06)
    add("Chest", chest_lip, joint)

    # --- WAIST BELLOWS INSET (~8 fine dark ribs) between chest plate and pelvis ---
    bellows = waist_bellows("WaistBellows", z_top=1.118, z_bot=1.000, radius=0.108, n_ribs=8)
    add("Spine", bellows, bellows_mat)

    # --- DISTINCT PELVIS SHELL ---
    pelvis = segmented_pelvis_shell("PelvisShell")
    add("Hips", pelvis, base)
    pelvis_lip = bead_lip("PelvisTopLip", (0, 0.01, 1.005), radius=0.168, axis="Z",
                          thick=0.012, flare=1.06)
    add("Hips", pelvis_lip, joint)

    # It nested Vs ONLY on Orange — ZERO on Tan/Runner
    if is_it:
        for i, z in enumerate([1.48, 1.40, 1.32]):
            half = 0.105 - i * 0.012
            bars = chevron_v(f"ChC{i}", 0.0, -0.165, z, half, bar_len=half * 1.20,
                             thick=0.016, depth=0.018, ang_deg=35.0)
            for b in bars:
                add("Chest", b, accent)
        rim = cyl("ItRim", (0, 0, 1.615), 0.075, 0.012)
        add("Neck", rim, over)

    # --- Arms: DISTINCT upper/lower shells + LARGE hinges ---
    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr, hand = arm_points(sx)

        # LARGE shoulder hinge
        shj = hinge_disk(f"ShoulderJ_{side}", sh, axis="X", radius=0.072, thick=0.034, rivets=4)
        add(f"Shoulder_{side}", shj, joint)

        # Upper arm shell — stop short of elbow so hinge gap reads
        ua_end = el + (sh - el).normalized() * 0.028
        ua = limb_shell_with_lips(f"UA_{side}", sh + (el - sh).normalized() * 0.035,
                                  ua_end, 0.060, 0.046)
        add(f"UpperArm_{side}", ua, base)
        delt_arm = sph(f"UADelt_{side}", sh + Vector((sx * 0.02, 0, -0.025)),
                       (0.058, 0.052, 0.058), seg=14, ring=7)
        add(f"UpperArm_{side}", delt_arm, base)

        # LARGE elbow hinge
        elj = hinge_disk(f"ElbowJ_{side}", el, axis="X", radius=0.058, thick=0.028, rivets=3)
        add(f"LowerArm_{side}", elj, joint)

        la_start = el + (wr - el).normalized() * 0.030
        la_end = wr + (el - wr).normalized() * 0.022
        la = limb_shell_with_lips(f"LA_{side}", la_start, la_end, 0.044, 0.033)
        add(f"LowerArm_{side}", la, base)

        h = hybrid_hand(f"Hand_{side}", wr, hand, sx, base, joint)
        groups[f"Hand_{side}"].append(h)

    # --- Legs: DISTINCT thigh/shin shells + LARGE hinges + U-knee ---
    for side, sx in (("L", 1), ("R", -1)):
        hip, kn, an, toe = leg_points(sx)

        hipj = hinge_disk(f"HipJ_{side}", hip, axis="X", radius=0.078, thick=0.036, rivets=4)
        add(f"UpperLeg_{side}", hipj, joint)

        thigh_start = hip + Vector((0, 0, -0.040))
        thigh_end = kn + Vector((0, 0, 0.065))
        thigh = limb_shell_with_lips(f"Thigh_{side}", thigh_start, thigh_end, 0.080, 0.056)
        add(f"UpperLeg_{side}", thigh, base)

        quad = sph(f"Quad_{side}", hip.lerp(kn, 0.35) + Vector((0, -0.022, 0)),
                   (0.068, 0.058, 0.082), seg=14, ring=7)
        add(f"UpperLeg_{side}", quad, base)

        seam_th = bead_lip(
            f"ThighSeam_{side}",
            hip + Vector((0, 0, -0.038)),
            radius=0.085, thick=0.012, axis="Z", flare=1.08)
        add(f"UpperLeg_{side}", seam_th, joint)

        fork = u_knee_fork(f"KneeFork_{side}", kn, sx)
        add(f"UpperLeg_{side}", fork, base)

        if is_it:
            for i, tt in enumerate((0.30, 0.48, 0.66)):
                p = hip.lerp(kn, tt)
                half = 0.050 - i * 0.005
                cx = p.x + sx * 0.040
                cy = p.y - 0.078
                bars = chevron_v(f"ChT{side}{i}", cx, cy, p.z, half, bar_len=half * 1.15,
                                 thick=0.011, depth=0.013, ang_deg=35.0)
                for b in bars:
                    add(f"UpperLeg_{side}", b, accent)

        knj = hinge_disk(f"KneeJ_{side}", kn, axis="X", radius=0.068, thick=0.032, rivets=4)
        add(f"LowerLeg_{side}", knj, joint)

        nest = sph(f"KneeNest_{side}", kn + Vector((0, 0, 0.008)), 0.042, seg=14, ring=7)
        add(f"LowerLeg_{side}", nest, joint)

        shin_start = kn + Vector((0, 0, -0.040))
        shin = limb_shell_with_lips(f"Shin_{side}", shin_start, an + Vector((0, 0, 0.030)),
                                    0.050, 0.036)
        add(f"LowerLeg_{side}", shin, base)
        calf = sph(f"Calf_{side}", kn.lerp(an, 0.35) + Vector((0, 0.028, 0)),
                   (0.044, 0.052, 0.068), seg=12, ring=6)
        add(f"LowerLeg_{side}", calf, base)

        ft = shoe_foot(f"Foot_{side}", an, toe, sx, base, joint)
        groups[f"Foot_{side}"].append(ft)

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
    suffix = "_It" if is_it else "_Tan"
    # Runner: warmer bone + slight subsurface so stills don't go cool grey
    if is_it:
        base = mat("Base", ORANGE, roughness=0.46, subsurface=0.05)
    else:
        base = mat("Base", BONE, roughness=0.42, subsurface=0.18, emit=0.08)
    accent = mat("Accent", BLACK if is_it else TEAL, roughness=0.42)
    over = mat("ItOverride", RIM if is_it else BONE, roughness=0.45, emit=(1.0 if is_it else 0.0))
    joint = mat("Joint" + suffix, JOINT, metallic=0.45, roughness=0.32)
    sensor = mat("Sensor" + suffix, SENSOR, roughness=0.25)
    metal = mat("Metal" + suffix, METAL, metallic=0.78, roughness=0.28)
    bellows_mat = mat("Bellows" + suffix, DARK_BELLOWS, metallic=0.08, roughness=0.55)
    cal_col = TEAL if not is_it else CAL_YELLOW
    cal_accent = mat("CalAccent" + suffix, cal_col, roughness=0.38)
    lip_mat = mat("Lip" + suffix, LIP, metallic=0.20, roughness=0.40)
    base.name = "Base"
    accent.name = "Accent"
    over.name = "ItOverride"
    return base, accent, over, joint, sensor, metal, bellows_mat, cal_accent, lip_mat


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


def write_meta(fbx_path, guid=None):
    meta = fbx_path + ".meta"
    if guid is None:
        guid = uuid.uuid4().hex
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
    animationImportErrors: 
    animationImportWarnings: 
    animationRetargetingWarnings: 
    animationDoRetargetingWarnings: 0
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
    # Prefer EEVEE for warmer material color read; fall back to workbench
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
            bg.inputs[0].default_value = (0.62, 0.52, 0.40, 1.0)  # warmer env
            bg.inputs[1].default_value = 0.55
    if "Ground" not in bpy.data.objects:
        bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, 0))
        g = bpy.context.active_object
        g.name = "Ground"
        gm = mat("GroundMat", (0.42, 0.40, 0.38, 1), roughness=0.92)
        set_mat(g, gm)
    if "KeySun" not in bpy.data.objects:
        bpy.ops.object.light_add(type="SUN", location=(2.5, -3.5, 5.5))
        sun = bpy.context.active_object
        sun.name = "KeySun"
        sun.data.energy = 3.2
        sun.data.color = (1.0, 0.88, 0.70)  # warmer key
        sun.rotation_euler = (math.radians(48), math.radians(12), math.radians(-18))
        bpy.ops.object.light_add(type="AREA", location=(-2.2, 2.2, 3.2))
        fill = bpy.context.active_object
        fill.name = "Fill"
        fill.data.energy = 55
        fill.data.size = 3.5
        fill.data.color = (1.0, 0.95, 0.88)
        bpy.ops.object.light_add(type="AREA", location=(0.5, -1.5, 2.0))
        warm = bpy.context.active_object
        warm.name = "WarmFill"
        warm.data.energy = 35
        warm.data.size = 2.0
        warm.data.color = (1.0, 0.80, 0.55)
        warm.data.energy = 55


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


def pose_run_knee(arm_ob):
    reset_pose(arm_ob)
    set_bone_euler(arm_ob, "Spine", (10, 0, 0))
    set_bone_euler(arm_ob, "Hips", (6, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_L", (-48, 0, 0))
    set_bone_euler(arm_ob, "LowerLeg_L", (22, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_R", (52, 0, 0))
    set_bone_euler(arm_ob, "LowerLeg_R", (98, 0, 0))
    set_bone_euler(arm_ob, "UpperArm_L", (58, 0, 12))
    set_bone_euler(arm_ob, "LowerArm_L", (-75, 0, 0))
    set_bone_euler(arm_ob, "UpperArm_R", (-55, 0, -12))
    set_bone_euler(arm_ob, "LowerArm_R", (-38, 0, 0))
    set_bone_euler(arm_ob, "Head", (-4, 0, 0))


def ground_feet(arm_ob, target_z=0.02):
    bpy.context.view_layer.update()
    zs = []
    for side in ("L", "R"):
        pb = arm_ob.pose.bones.get(f"Foot_{side}")
        if pb:
            zs.append((arm_ob.matrix_world @ pb.tail).z)
            zs.append((arm_ob.matrix_world @ pb.head).z)
    if not zs:
        return
    arm_ob.location.z += (target_z - min(zs))
    bpy.context.view_layer.update()


def pose_slide(arm_ob):
    reset_pose(arm_ob)
    set_bone_euler(arm_ob, "Hips", (12, 0, 0))
    set_bone_euler(arm_ob, "Spine", (40, 0, 0))
    set_bone_euler(arm_ob, "Chest", (12, 0, 0))
    set_bone_euler(arm_ob, "Head", (-20, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_L", (-70, 14, 6))
    set_bone_euler(arm_ob, "LowerLeg_L", (135, 0, 0))
    set_bone_euler(arm_ob, "Foot_L", (-45, 0, 8))
    set_bone_euler(arm_ob, "UpperLeg_R", (-65, -14, -6))
    set_bone_euler(arm_ob, "LowerLeg_R", (130, 0, 0))
    set_bone_euler(arm_ob, "Foot_R", (-42, 0, -8))
    set_bone_euler(arm_ob, "UpperArm_L", (55, -30, 35))
    set_bone_euler(arm_ob, "LowerArm_L", (-35, 0, 0))
    set_bone_euler(arm_ob, "UpperArm_R", (55, 30, -35))
    set_bone_euler(arm_ob, "LowerArm_R", (-35, 0, 0))
    ground_feet(arm_ob, target_z=0.025)


def pose_punch(arm_ob):
    reset_pose(arm_ob)
    set_bone_euler(arm_ob, "Hips", (4, -18, 0))
    set_bone_euler(arm_ob, "Spine", (6, -25, 0))
    set_bone_euler(arm_ob, "Chest", (2, -12, 0))
    set_bone_euler(arm_ob, "Head", (0, -8, 0))
    set_bone_euler(arm_ob, "Shoulder_R", (0, 0, -15))
    set_bone_euler(arm_ob, "UpperArm_R", (-75, 15, -55))
    set_bone_euler(arm_ob, "LowerArm_R", (-8, 0, 0))
    set_bone_euler(arm_ob, "Hand_R", (0, 0, 0))
    set_bone_euler(arm_ob, "UpperArm_L", (50, -20, 35))
    set_bone_euler(arm_ob, "LowerArm_L", (-85, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_L", (-18, 0, 0))
    set_bone_euler(arm_ob, "LowerLeg_L", (12, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_R", (15, 0, 0))
    set_bone_euler(arm_ob, "LowerLeg_R", (8, 0, 0))


def render_shot(path, cam_loc, look=(0, 0, 1.05)):
    setup_render()
    place_camera(cam_loc, look)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    log(f"Still {path}")


def composite_vs_ref(idle_path, out_path):
    try:
        from PIL import Image, ImageDraw, ImageFont
    except ImportError:
        log("PIL missing — copying idle as vs_ref fallback")
        import shutil
        shutil.copy(idle_path, out_path)
        return
    idle = Image.open(idle_path).convert("RGBA")
    ref = Image.open(REF_CRASH).convert("RGBA")
    w, h = ref.size
    if w > h * 1.2:
        ref = ref.crop((0, 0, w // 2, h))
    target_h = 1100

    def fit_h(im, th):
        r = th / im.height
        return im.resize((max(1, int(im.width * r)), th), Image.Resampling.LANCZOS)

    idle_f = fit_h(idle, target_h)
    ref_f = fit_h(ref, target_h)
    gap = 24
    canvas_w = idle_f.width + gap + ref_f.width + 40
    canvas_h = target_h + 60
    canvas = Image.new("RGBA", (canvas_w, canvas_h), (48, 48, 52, 255))
    canvas.paste(idle_f, (20, 40), idle_f)
    canvas.paste(ref_f, (20 + idle_f.width + gap, 40), ref_f)
    draw = ImageDraw.Draw(canvas)
    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
    except Exception:
        font = ImageFont.load_default()
    draw.text((20, 10), "v0.4.1 Tan idle", fill=(220, 220, 220, 255), font=font)
    draw.text((20 + idle_f.width + gap, 10), "Hybrid III ref (PRIMARY)", fill=(220, 220, 220, 255), font=font)
    canvas.convert("RGB").save(out_path)
    log(f"Still {out_path} (vs ref composite)")


def verify_bones(arm_ob):
    required = [
        "Hips", "Spine", "Head",
        "UpperArm_L", "UpperArm_R", "LowerArm_L", "LowerArm_R",
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
    ]
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
    clear_x = abs(hand_l.x) - 0.20
    clear_y = hand_l.y
    log(f"A-pose UpperArm_L angle off vertical: {ang:.1f} deg")
    log(f"Hand_L world: {tuple(round(c, 3) for c in hand_l)} clear_x={clear_x:.3f}m y={clear_y:.3f}")
    return ang, clear_x, clear_y


def measure_slide_grounding(arm_ob):
    bpy.context.view_layer.update()
    feet_z = []
    for side in ("L", "R"):
        pb = arm_ob.pose.bones.get(f"Foot_{side}")
        if pb:
            tip = Vector(arm_ob.matrix_world @ pb.tail)
            feet_z.append(tip.z)
    hips = Vector(arm_ob.matrix_world @ arm_ob.pose.bones["Hips"].head)
    log(f"Slide feet_z={ [round(z, 3) for z in feet_z] } hips_z={hips.z:.3f}")
    return feet_z, hips.z


def build_variant(is_it, export_path, guid, do_stills_tan=False, do_still_orange=False):
    clear_scene()
    mats = make_mats(is_it)
    arm_ob = build_armature()
    groups = build_mesh_parts(is_it, mats)
    parent_groups(groups, arm_ob)
    ok = verify_bones(arm_ob)
    ang, cx, cy = measure_a_pose(arm_ob)

    if do_stills_tan:
        pose_idle(arm_ob)
        idle_front = f"{PREV}/hipoly_v41_idle_front.png"
        render_shot(idle_front, (0.15, -3.3, 1.40), (0, 0, 1.05))
        render_shot(f"{PREV}/hipoly_v41_idle_34.png", (2.3, -2.5, 1.50), (0, 0, 1.05))
        composite_vs_ref(idle_front, f"{PREV}/hipoly_v41_idle_vs_ref.png")
        pose_run_knee(arm_ob)
        render_shot(f"{PREV}/hipoly_v41_run_knee.png", (3.5, 0.1, 1.20), (0, 0, 0.95))
        pose_slide(arm_ob)
        measure_slide_grounding(arm_ob)
        render_shot(f"{PREV}/hipoly_v41_slide_crouch.png", (0.4, -3.8, 0.55), (0, 0, 0.35))
        pose_punch(arm_ob)
        render_shot(f"{PREV}/hipoly_v41_punch.png", (2.6, -2.2, 1.35), (0.05, -0.2, 1.25))
        arm_ob.location = (0, 0, 0)
        reset_pose(arm_ob)

    if do_still_orange:
        pose_idle(arm_ob)
        render_shot(f"{PREV}/hipoly_v41_it_idle_front.png", (0.15, -3.3, 1.40), (0, 0, 1.05))
        reset_pose(arm_ob)

    export_fbx(export_path, arm_ob)
    write_meta(export_path, guid=guid)
    bpy.ops.wm.save_as_mainfile(
        filepath=BLEND.replace(".blend", "_It.blend" if is_it else "_Tan.blend")
    )
    return ok, ang, cx, cy


def write_readme():
    path = os.path.join(OUT_DIR, "README.md")
    text = """# HiPoly Hierarchical Mannequins

DummyLocomotor-bindable **Hybrid III** crash-test dummies — segmented vinyl shells
+ athletic mass (middle path). v0.3 toy / v0.4 smooth mannequin both rejected.

**Pass:** Hybrid III refine **v0.4.1** (segmented chest/pelvis/limb shells with bead
lips, inset bellows, 4 neck rings, LARGE hinge disks + cal marks, separated fingers,
warm bone tan Runner).

## Assets
| File | Paint |
|------|-------|
| `Dummy_Mannequin_Tan_Hier_Hi.fbx` | Runner — Base warm bone `#E8D9C0`, Accent `#2BB3A3` thin tick + teal/black cals. **ZERO nested Vs.** |
| `Dummy_Mannequin_Orange_Hier_Hi.fbx` | It — Base `#FF6A00`, Accent black nested Vs chest + outer thighs |

## Bind pose
- Mild A-pose ~20–35°; hands clear pelvis.
- Egg + 2 eye dots + temple row of 3 — no visor / painted face.
- Segmented chest plate + pelvis shell; inset waist bellows (~7 ribs); 4 neck rings.
- Limb shells with soft bead/lip seams; LARGE dark metal hinges; shoe-pad feet.
- Knees: LowerLeg nests in UpperLeg U-fork.

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
    log("=== hipoly hier v5.1 Hybrid III refine v0.4.1 ===")
    tan = os.path.join(OUT_DIR, "Dummy_Mannequin_Tan_Hier_Hi.fbx")
    orn = os.path.join(OUT_DIR, "Dummy_Mannequin_Orange_Hier_Hi.fbx")

    log("Building Tan/Runner Hier HiPoly v5.1 (v0.4.1)…")
    ok_t, ang_t, cx_t, cy_t = build_variant(
        False, tan, GUID_TAN, do_stills_tan=True, do_still_orange=False)

    log("Building Orange/It Hier HiPoly v5.1 (v0.4.1)…")
    ok_o, ang_o, cx_o, cy_o = build_variant(
        True, orn, GUID_ORANGE, do_stills_tan=False, do_still_orange=True)

    write_readme()
    log(f"Tan OK={ok_t} A-pose={ang_t:.1f}deg clear_x={cx_t:.3f} clear_y={cy_t:.3f}")
    log(f"It  OK={ok_o} A-pose={ang_o:.1f}deg clear_x={cx_o:.3f} clear_y={cy_o:.3f}")
    log(f"Tan FBX {os.path.getsize(tan)} bytes guid={GUID_TAN}")
    log(f"Orange FBX {os.path.getsize(orn)} bytes guid={GUID_ORANGE}")
    log("DONE v0.4.1 — no git push (v0.3 tip stays live)")


if __name__ == "__main__":
    main()
