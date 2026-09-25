#!/usr/bin/env python3
"""
HiPoly hierarchical mannequin v4 — Hybrid III refine v0.3.
Art brief: 3D-BRIEF-hipoly-hybrid-iii-refine-v0.3.md
DummyLocomotor bones: Hips, Spine, Head, UpperArm_*, LowerArm_*, UpperLeg_*, LowerLeg_*
Paint locks: Tan #E8D9C0+#2BB3A3 / Orange #FF6A00+black nested Vs — NO NASA blue.

v0.3 NEW vs v0.2:
  - dark ribbed waist bellows between chest + pelvis
  - stacked 4–5 dark metal neck rings (not single tube)
  - LARGER hinge disks at shoulder/elbow/hip/knee/ankle
  - athletic store-mannequin chest/limb mass under vinyl plates
  - visible assembly seam rings at shoulder / waist / thigh roots
  - Hybrid III cal quadrant disks (temple + chest); Runner yellow→teal
  - UpperLeg U-fork knee with LowerLeg nested inside
  - chunky mitten hands + flat foot pads
"""
import bpy
import math
import os
import uuid
from mathutils import Vector, Euler, Matrix

OUT_DIR = "/workspace/tag-unity/Assets/Art/Characters/HiPoly"
PREV = "/workspace/art-build/previews"
BLEND = "/workspace/art-build/Dummy_Mannequin_Hier_Hi.blend"
LOG = "/tmp/hipoly_v4_build.log"

GUID_TAN = "ad3f2fa97db94e72869d746ecdf8e87d"
GUID_ORANGE = "b33974ad57284ef28a7564e3bdf00540"

BONE = (0.910, 0.851, 0.753, 1.0)      # #E8D9C0
TEAL = (0.169, 0.702, 0.639, 1.0)      # #2BB3A3
ORANGE = (1.0, 0.416, 0.0, 1.0)        # #FF6A00
BLACK = (0.04, 0.04, 0.045, 1.0)
JOINT = (0.10, 0.10, 0.12, 1.0)
METAL = (0.22, 0.22, 0.24, 1.0)
DARK_BELLOWS = (0.06, 0.06, 0.07, 1.0)
RIM = (1.0, 0.45, 0.05, 1.0)
SENSOR = (0.02, 0.02, 0.02, 1.0)
CAL_YELLOW = (0.95, 0.82, 0.08, 1.0)   # Hybrid III cal (mapped→teal on Runner)

SEG = 32
RING = 16
CYL_V = 28

# Mild A-pose — broader athletic shoulders, longer thighs
SHOULDER_Z = 1.48
SHOULDER_X = 0.310
UA_LEN = 0.34
LA_LEN = 0.30
ARM_OUT = math.radians(30.0)   # ~25–35° mild A-pose
ARM_FWD = math.radians(12.0)   # hands clear pelvis

HIP_Z = 0.96
HIP_X = 0.130
UL_LEN = 0.47
LL_LEN = 0.43

os.makedirs(OUT_DIR, exist_ok=True)
os.makedirs(PREV, exist_ok=True)


def log(msg):
    print(msg, flush=True)
    with open(LOG, "a") as f:
        f.write(msg + "\n")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for coll in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials,
                 bpy.data.curves, bpy.data.cameras, bpy.data.lights):
        for x in list(coll):
            coll.remove(x)


def mat(name, color, metallic=0.02, roughness=0.50, emit=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.diffuse_color = color
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
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
        ob.data.auto_smooth_angle = math.radians(55)


def apply_mod(ob, mod_name):
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod_name)


def apply_subsurf(ob, levels=1):
    mod = ob.modifiers.new("SubD", "SUBSURF")
    mod.levels = levels
    mod.render_levels = levels
    apply_mod(ob, mod.name)


def apply_bevel(ob, width=0.012, segments=3):
    mod = ob.modifiers.new("Bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(30)
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


def capsule(name, a, b, radius, v=CYL_V):
    a, b = Vector(a), Vector(b)
    mid = (a + b) * 0.5
    direction = b - a
    length = direction.length
    if length < 1e-6:
        return sph(name, mid, radius)
    cyl_len = max(length - radius * 0.35, length * 0.55)
    o = cyl(name, mid, radius, cyl_len, v=v)
    quat = direction.normalized().to_track_quat("Z", "Y")
    o.rotation_euler = quat.to_euler()
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    c1 = sph(f"{name}_capA", a, radius * 0.98, seg=20, ring=10)
    c2 = sph(f"{name}_capB", b, radius * 0.98, seg=20, ring=10)
    return join(name, [o, c1, c2])


def hinge_disk(name, loc, axis="X", radius=0.070, thick=0.030, rivets=5):
    """LARGER Hybrid III rivet/hinge disk — readable at chase-cam distance."""
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
    core = sph(f"{name}_core", loc, radius * 0.70, seg=16, ring=8)
    disk = cyl(f"{name}_disk", loc, radius, thick, rot=rot, v=28)
    # Outer metal rim ring for Hybrid III chrome read
    rim = cyl(f"{name}_rim", loc, radius * 1.08, thick * 0.45, rot=rot, v=28)
    hub = sph(f"{name}_hub", Vector(loc) + face_nudge * 0.35, thick * 0.75, seg=12, ring=6)
    # Center pin
    pin = cyl(f"{name}_pin", loc, thick * 0.35, thick * 1.4, rot=rot, v=12)
    parts = [core, disk, rim, hub, pin]
    for i in range(rivets):
        ang = (2 * math.pi * i) / rivets + math.radians(20)
        offset = riv_plane(ang, radius * 0.62) + face_nudge
        if i % 2 == 0:
            offset = offset + Vector((0, -0.010, 0))
        rv = sph(f"{name}_riv{i}", Vector(loc) + offset, 0.013, seg=10, ring=5)
        parts.append(rv)
    return join(name, parts)


def cal_quadrant_disk(name, loc, radius=0.028, thick=0.010, accent_mat=None, black_mat=None, axis="Y"):
    """Hybrid III black/accent calibration quadrant disk (4 pie wedges)."""
    # Base black disk
    if axis == "Y":
        rot = (math.radians(90), 0, 0)
    elif axis == "X":
        rot = (0, math.radians(90), 0)
    else:
        rot = (0, 0, 0)
    base = cyl(f"{name}_base", loc, radius, thick, rot=rot, v=24)
    set_mat(base, black_mat)
    # Two opposite accent wedges as thin cubes sitting on face
    if axis == "Y":
        # Facing -Y (front) — wedges in XZ
        w1 = cube(f"{name}_q1",
                  Vector(loc) + Vector((radius * 0.28, -thick * 0.55, radius * 0.28)),
                  (radius * 0.42, thick * 0.35, radius * 0.42), bevel=0.002)
        w2 = cube(f"{name}_q2",
                  Vector(loc) + Vector((-radius * 0.28, -thick * 0.55, -radius * 0.28)),
                  (radius * 0.42, thick * 0.35, radius * 0.42), bevel=0.002)
    elif axis == "X":
        w1 = cube(f"{name}_q1",
                  Vector(loc) + Vector((thick * 0.55, radius * 0.28, radius * 0.28)),
                  (thick * 0.35, radius * 0.42, radius * 0.42), bevel=0.002)
        w2 = cube(f"{name}_q2",
                  Vector(loc) + Vector((thick * 0.55, -radius * 0.28, -radius * 0.28)),
                  (thick * 0.35, radius * 0.42, radius * 0.42), bevel=0.002)
    else:
        w1 = cube(f"{name}_q1",
                  Vector(loc) + Vector((radius * 0.28, radius * 0.28, thick * 0.55)),
                  (radius * 0.42, radius * 0.42, thick * 0.35), bevel=0.002)
        w2 = cube(f"{name}_q2",
                  Vector(loc) + Vector((-radius * 0.28, -radius * 0.28, thick * 0.55)),
                  (radius * 0.42, radius * 0.42, thick * 0.35), bevel=0.002)
    set_mat(w1, accent_mat)
    set_mat(w2, accent_mat)
    return join(name, [base, w1, w2])


def chevron_v(tag, cx, cy, cz, half_w, bar_len, thick=0.013, depth=0.016, ang_deg=38.0):
    ang = math.radians(ang_deg)
    drop = math.sin(ang) * bar_len * 0.55
    spread = math.cos(ang) * bar_len * 0.45
    left = cube(f"{tag}_L", (cx - spread, cy, cz + drop * 0.15),
                (bar_len * 0.5, depth, thick), (0, ang, 0))
    right = cube(f"{tag}_R", (cx + spread, cy, cz + drop * 0.15),
                 (bar_len * 0.5, depth, thick), (0, -ang, 0))
    return [left, right]


def waist_bellows(name, z_top, z_bot, radius=0.145, n_ribs=8):
    """Dark ribbed Hybrid III waist bellows between chest plate and pelvis."""
    parts = []
    span = z_top - z_bot
    for i in range(n_ribs):
        t = (i + 0.5) / n_ribs
        z = z_top - t * span
        # Alternate outward ribs (bellows ridges) and valleys
        r = radius * (1.08 if i % 2 == 0 else 0.92)
        depth = span / n_ribs * (0.72 if i % 2 == 0 else 0.55)
        rib = cyl(f"{name}_rib{i}", (0, 0.01, z), r, depth, v=28)
        parts.append(rib)
    # Soft inner core so bellows read solid
    core = cyl(f"{name}_core", (0, 0.01, (z_top + z_bot) * 0.5),
               radius * 0.78, span * 0.95, v=20)
    parts.append(core)
    return join(name, parts)


def neck_ring_stack(name, z_base, n=4, major=0.078, minor=0.011, spacing=0.022):
    """Stacked dark metal Hybrid III neck rings (not a single tube)."""
    parts = []
    for i in range(n):
        z = z_base + i * spacing
        # Slight taper — upper rings a touch smaller
        maj = major * (1.0 - i * 0.03)
        ring = torus_ring(f"{name}_r{i}", (0, 0, z), major=maj, minor=minor)
        parts.append(ring)
        # Thin disk filler between rings for solid collar read
        if i < n - 1:
            filler = cyl(f"{name}_f{i}", (0, 0, z + spacing * 0.5),
                         maj * 0.88, spacing * 0.35, v=20)
            parts.append(filler)
    # Inner neck column
    col = cyl(f"{name}_col", (0, 0, z_base + (n - 1) * spacing * 0.5),
              major * 0.55, (n - 1) * spacing + 0.04, v=16)
    parts.append(col)
    return join(name, parts)


def assembly_seam_ring(name, loc, radius, thick=0.010, axis="Z"):
    """Visible vinyl assembly seam ring (store mannequin join)."""
    if axis == "Z":
        rot = (0, 0, 0)
    elif axis == "X":
        rot = (0, math.radians(90), 0)
    else:
        rot = (math.radians(90), 0, 0)
    return cyl(name, loc, radius, thick, rot=rot, v=28)


def u_knee_fork(name, kn, sx, radius=0.078, fork_depth=0.055):
    """UpperLeg distal U-fork — LowerLeg nests visibly inside."""
    # Two lateral pads forming the U
    left = sph(f"{name}_padL",
               kn + Vector((-0.042, 0.0, 0.015)),
               (0.038, 0.055, 0.055), seg=16, ring=8)
    right = sph(f"{name}_padR",
                kn + Vector((0.042, 0.0, 0.015)),
                (0.038, 0.055, 0.055), seg=16, ring=8)
    # Bridge above the nest gap
    bridge = sph(f"{name}_bridge",
                 kn + Vector((0, 0.0, 0.055)),
                 (0.070, 0.060, 0.035), seg=16, ring=8)
    return join(name, [left, right, bridge])


def arm_points(sx):
    sh = Vector((sx * SHOULDER_X, 0.0, SHOULDER_Z))
    out = math.sin(ARM_OUT)
    down = math.cos(ARM_OUT)
    fwd = math.sin(ARM_FWD)
    dir_ua = Vector((sx * out, -fwd, -down)).normalized()
    el = sh + dir_ua * UA_LEN
    dir_la = (dir_ua + Vector((sx * 0.10, -0.25, -0.12))).normalized()
    dir_la = Vector((sx * abs(dir_la.x) + sx * 0.06, dir_la.y - 0.06, dir_la.z)).normalized()
    wr = el + dir_la * LA_LEN
    hand = wr + Vector((sx * 0.02, -0.045, -0.055))
    return sh, el, wr, hand


def leg_points(sx):
    hip = Vector((sx * HIP_X, 0.02, HIP_Z))
    kn = hip + Vector((sx * 0.015, 0.025, -UL_LEN))
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
    bone("Spine", "Hips", (0, 0, HIP_Z + 0.10), (0, 0, 1.20))
    bone("Chest", "Spine", (0, 0, 1.20), (0, 0, SHOULDER_Z))
    bone("Neck", "Chest", (0, 0, SHOULDER_Z), (0, 0, 1.57))
    bone("Head", "Neck", (0, 0, 1.57), (0, 0, 1.88))

    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr, hand = arm_points(sx)
        bone(f"Shoulder_{side}", "Chest", (sx * 0.13, 0, SHOULDER_Z), sh)
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
    """Hybrid III v0.3 — bellows waist, neck rings, big hinges, athletic mass, seams, cal marks."""
    base, accent, over, joint, sensor, metal, bellows_mat, cal_accent = mats
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

    # --- Head: egg + bible sensors (NO visor / painted face) ---
    head = sph("Head", (0, -0.01, 1.72), (0.172, 0.152, 0.210), seg=36, ring=18, sub=True)
    add("Head", head, base)
    for dx in (-0.050, 0.050):
        e = sph(f"Eye_{dx}", (dx, -0.145, 1.725), (0.022, 0.011, 0.022), seg=12, ring=6)
        add("Head", e, sensor)
    for i, z in enumerate([1.775, 1.725, 1.675]):
        t = sph(f"Temple_{i}", (0.158, -0.055, z), 0.015, seg=10, ring=5)
        add("Head", t, sensor)

    # Temple calibration quadrant (Hybrid III) — character right temple (-X) + left for 3/4
    cal_t = cal_quadrant_disk(
        "CalTemple",
        (-0.160, -0.040, 1.740),
        radius=0.026, thick=0.010,
        accent_mat=cal_accent, black_mat=sensor, axis="X")
    # cal_quadrant already set mats on children; re-join keeps them — parent as Head
    groups["Head"].append(cal_t)

    # --- Neck: stacked 4 dark metal rings (Hybrid III) ---
    neck = neck_ring_stack("NeckRings", z_base=1.530, n=4, major=0.080, minor=0.012, spacing=0.024)
    add("Neck", neck, metal)

    # --- Chest: athletic store-mannequin mass + polymer panel ---
    chest_foam = sph("ChestFoam", (0, 0.01, 1.365), (0.255, 0.155, 0.200), seg=32, ring=16, sub=True)
    add("Chest", chest_foam, base)
    # Pec definition bumps
    for sx in (1, -1):
        pec = sph(f"Pec_{sx}", (sx * 0.090, -0.095, 1.400), (0.095, 0.055, 0.070), seg=16, ring=8)
        add("Chest", pec, base)
    panel = cube("ChestPanel", (0, -0.160, 1.370), (0.150, 0.014, 0.120), bevel=0.018)
    add("Chest", panel, base)
    # Athletic deltoids
    for side, sx in (("L", 1), ("R", -1)):
        deltoid = sph(f"Deltoid_{side}", (sx * 0.235, 0.0, 1.445), (0.110, 0.092, 0.098), seg=20, ring=10)
        add("Chest", deltoid, base)
        # Assembly seam ring at shoulder root (store mannequin)
        seam = assembly_seam_ring(
            f"ShoulderSeam_{side}",
            (sx * 0.255, 0.0, 1.455),
            radius=0.078, thick=0.012, axis="X")
        add("Chest", seam, joint)

    # Chest cal mark (upper-left from viewer = character +X / L chest)
    cal_c = cal_quadrant_disk(
        "CalChest",
        (0.095, -0.175, 1.445),
        radius=0.030, thick=0.010,
        accent_mat=cal_accent, black_mat=sensor, axis="Y")
    groups["Chest"].append(cal_c)

    # --- WAIST BELLOWS (v0.3 hero feature) — dark ribbed segment ---
    bellows = waist_bellows("WaistBellows", z_top=1.230, z_bot=1.040, radius=0.140, n_ribs=8)
    add("Spine", bellows, bellows_mat)
    # Assembly seam rings at top + bottom of bellows
    seam_waist_top = assembly_seam_ring("WaistSeamTop", (0, 0.01, 1.235), 0.155, thick=0.012)
    add("Spine", seam_waist_top, joint)
    seam_waist_bot = assembly_seam_ring("WaistSeamBot", (0, 0.01, 1.035), 0.165, thick=0.012)
    add("Hips", seam_waist_bot, joint)

    # --- Pelvis yoke / athletic hip block ---
    pelvis = sph("PelvisYoke", (0, 0.02, 0.94), (0.215, 0.150, 0.125), seg=28, ring=14, sub=True)
    add("Hips", pelvis, base)
    hip_shell = cube("HipShell", (0, -0.135, 0.935), (0.130, 0.012, 0.068), bevel=0.012)
    add("Hips", hip_shell, base)
    for sx in (1, -1):
        wing = sph(f"HipWing_{sx}", (sx * 0.175, 0.025, 0.945), (0.085, 0.085, 0.075), seg=16, ring=8)
        add("Hips", wing, base)

    if is_it:
        for i, z in enumerate([1.455, 1.365, 1.275]):
            half = 0.105 - i * 0.014
            bars = chevron_v(f"ChC{i}", 0.0, -0.180, z, half, bar_len=half * 1.2,
                             thick=0.015, depth=0.018, ang_deg=36.0)
            for b in bars:
                add("Chest", b, accent)
        rim = cyl("ItRim", (0, 0, 1.605), 0.088, 0.014)
        add("Neck", rim, over)
    else:
        band = cube("ChestBand", (0, -0.175, 1.310), (0.155, 0.016, 0.028), bevel=0.006)
        add("Chest", band, accent)

    # --- Arms (athletic volume) ---
    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr, hand = arm_points(sx)
        # LARGER shoulder hinge
        shj = hinge_disk(f"ShoulderJ_{side}", sh, axis="X", radius=0.078, thick=0.034, rivets=5)
        add(f"Shoulder_{side}", shj, joint)

        ua = capsule(f"UA_{side}", sh, el, 0.060)
        add(f"UpperArm_{side}", ua, base)
        if not is_it:
            p = sh.lerp(el, 0.38)
            st = cyl(f"UAStripe_{side}", p, 0.065, 0.040)
            add(f"UpperArm_{side}", st, accent)

        elj = hinge_disk(f"ElbowJ_{side}", el, axis="X", radius=0.060, thick=0.030, rivets=4)
        add(f"LowerArm_{side}", elj, joint)

        la = capsule(f"LA_{side}", el, wr, 0.048)
        add(f"LowerArm_{side}", la, base)

        wrj = hinge_disk(f"WristJ_{side}", wr, axis="X", radius=0.040, thick=0.022, rivets=3)
        add(f"Hand_{side}", wrj, joint)

        # Chunky mitten / glove
        palm = sph(f"Palm_{side}",
                   hand + Vector((0, -0.015, 0.0)),
                   (0.055, 0.065, 0.040), seg=18, ring=9)
        add(f"Hand_{side}", palm, base)
        mitt = sph(f"Mitt_{side}",
                   hand + Vector((0, -0.048, -0.035)),
                   (0.052, 0.060, 0.036), seg=16, ring=8)
        add(f"Hand_{side}", mitt, base)
        thumb = sph(f"Thumb_{side}",
                    hand + Vector((sx * 0.055, 0.012, -0.008)),
                    (0.024, 0.034, 0.024), seg=12, ring=6)
        add(f"Hand_{side}", thumb, base)

    # --- Legs (athletic + nested U-knee) ---
    for side, sx in (("L", 1), ("R", -1)):
        hip, kn, an, toe = leg_points(sx)
        hipj = hinge_disk(f"HipJ_{side}", hip, axis="X", radius=0.085, thick=0.036, rivets=5)
        add(f"UpperLeg_{side}", hipj, joint)

        # Thigh mass — stop short of knee so U-fork reads
        thigh_end = kn + Vector((0, 0, 0.06))
        thigh = capsule(f"Thigh_{side}", hip, thigh_end, 0.080)
        add(f"UpperLeg_{side}", thigh, base)

        # Thigh-root assembly seam (store mannequin)
        seam_th = assembly_seam_ring(
            f"ThighSeam_{side}",
            hip + Vector((0, 0, -0.04)),
            radius=0.090, thick=0.012, axis="Z")
        add(f"UpperLeg_{side}", seam_th, joint)

        # U-fork at distal UpperLeg — LowerLeg nests here
        fork = u_knee_fork(f"KneeFork_{side}", kn, sx)
        add(f"UpperLeg_{side}", fork, base)

        if is_it:
            for i, tt in enumerate((0.28, 0.46, 0.64)):
                p = hip.lerp(kn, tt)
                half = 0.052 - i * 0.006
                cx = p.x + sx * 0.040
                cy = p.y - 0.082
                bars = chevron_v(f"ChT{side}{i}", cx, cy, p.z, half, bar_len=half * 1.2,
                                 thick=0.012, depth=0.016, ang_deg=36.0)
                for b in bars:
                    add(f"UpperLeg_{side}", b, accent)
        else:
            p = hip.lerp(kn, 0.40)
            ts = cyl(f"ThighStripe_{side}", p, 0.085, 0.044)
            add(f"UpperLeg_{side}", ts, accent)

        # Knee hinge — LARGER, parented to LowerLeg (nests in U)
        knj = hinge_disk(f"KneeJ_{side}", kn, axis="X", radius=0.072, thick=0.034, rivets=5)
        add(f"LowerLeg_{side}", knj, joint)

        # Nest ball at top of LowerLeg (sits inside U-fork)
        nest = sph(f"KneeNest_{side}", kn + Vector((0, 0, 0.01)), 0.048, seg=16, ring=8)
        add(f"LowerLeg_{side}", nest, joint)

        shin = capsule(f"Shin_{side}", kn, an, 0.055)
        add(f"LowerLeg_{side}", shin, base)

        anj = hinge_disk(f"AnkleJ_{side}", an, axis="X", radius=0.048, thick=0.024, rivets=3)
        add(f"Foot_{side}", anj, joint)

        foot_pad = cube(f"FootPad_{side}",
                        an + Vector((0, -0.055, -0.028)),
                        (0.058, 0.105, 0.030), bevel=0.014)
        add(f"Foot_{side}", foot_pad, base)
        toe_pad = cube(f"ToePad_{side}",
                       an + Vector((0, -0.118, -0.018)),
                       (0.050, 0.042, 0.022), bevel=0.010)
        add(f"Foot_{side}", toe_pad, base)

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
    base = mat("Base", ORANGE if is_it else BONE, roughness=0.50)
    accent = mat("Accent", BLACK if is_it else TEAL, roughness=0.42)
    over = mat("ItOverride", RIM if is_it else BONE, roughness=0.45, emit=(1.0 if is_it else 0.0))
    joint = mat("Joint" + suffix, JOINT, metallic=0.30, roughness=0.36)
    sensor = mat("Sensor" + suffix, SENSOR, roughness=0.25)
    metal = mat("Metal" + suffix, METAL, metallic=0.72, roughness=0.28)
    bellows_mat = mat("Bellows" + suffix, DARK_BELLOWS, metallic=0.08, roughness=0.55)
    # Runner: yellow→teal so bible holds; It: yellow cal on orange
    cal_col = TEAL if not is_it else CAL_YELLOW
    cal_accent = mat("CalAccent" + suffix, cal_col, roughness=0.40)
    base.name = "Base"
    accent.name = "Accent"
    over.name = "ItOverride"
    return base, accent, over, joint, sensor, metal, bellows_mat, cal_accent


def export_fbx(path, arm_ob):
    for name in list(bpy.data.objects.keys()):
        o = bpy.data.objects[name]
        if o.type in ("CAMERA", "LIGHT") or name in ("Ground", "KeySun", "Fill", "ShotCam"):
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


def setup_render(engine="BLENDER_WORKBENCH", res=1100):
    sc = bpy.context.scene
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
    if "Ground" not in bpy.data.objects:
        bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, 0))
        g = bpy.context.active_object
        g.name = "Ground"
        gm = mat("GroundMat", (0.55, 0.55, 0.58, 1), roughness=0.9)
        set_mat(g, gm)
    if "KeySun" not in bpy.data.objects:
        bpy.ops.object.light_add(type="SUN", location=(2, -3, 5))
        sun = bpy.context.active_object
        sun.name = "KeySun"
        sun.data.energy = 3.0
        sun.rotation_euler = (math.radians(45), math.radians(15), math.radians(-20))
        bpy.ops.object.light_add(type="AREA", location=(-2, 2, 3))
        fill = bpy.context.active_object
        fill.name = "Fill"
        fill.data.energy = 40
        fill.data.size = 3


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
    """Wood-mannequin run bar: stride knee bend + opposite arm swing."""
    reset_pose(arm_ob)
    set_bone_euler(arm_ob, "Spine", (10, 0, 0))
    set_bone_euler(arm_ob, "Hips", (6, 0, 0))
    # Lead leg forward, recovery knee clearly bent (~90°)
    set_bone_euler(arm_ob, "UpperLeg_L", (-48, 0, 0))
    set_bone_euler(arm_ob, "LowerLeg_L", (22, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_R", (52, 0, 0))
    set_bone_euler(arm_ob, "LowerLeg_R", (98, 0, 0))
    # Opposite arm swing
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
    """Grounded low flat bar — hips down, feet on floor."""
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
        render_shot(f"{PREV}/hipoly_v3_idle_front.png", (0.15, -3.3, 1.40), (0, 0, 1.05))
        render_shot(f"{PREV}/hipoly_v3_idle_34.png", (2.3, -2.5, 1.50), (0, 0, 1.05))
        pose_run_knee(arm_ob)
        render_shot(f"{PREV}/hipoly_v3_run_knee.png", (3.5, 0.1, 1.20), (0, 0, 0.95))
        pose_slide(arm_ob)
        fz, hz = measure_slide_grounding(arm_ob)
        render_shot(f"{PREV}/hipoly_v3_slide_crouch.png", (0.4, -3.8, 0.55), (0, 0, 0.35))
        pose_punch(arm_ob)
        render_shot(f"{PREV}/hipoly_v3_punch.png", (2.6, -2.2, 1.35), (0.05, -0.2, 1.25))
        arm_ob.location = (0, 0, 0)
        reset_pose(arm_ob)

    if do_still_orange:
        pose_idle(arm_ob)
        render_shot(f"{PREV}/hipoly_v3_it_idle_front.png", (0.15, -3.3, 1.40), (0, 0, 1.05))
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

DummyLocomotor-bindable **Hybrid III** crash-test dummies (athletic vinyl mass + ribbed waist bellows + stacked neck rings + large hinge disks).

**Pass:** Hybrid III refine **v0.3** (bellows + neck rings + big hinges + store-mannequin mass/seams).

## Assets
| File | Paint |
|------|-------|
| `Dummy_Mannequin_Tan_Hier_Hi.fbx` | Runner — Base `#E8D9C0`, Accent `#2BB3A3` chest stripe + limb stripes + teal cal marks |
| `Dummy_Mannequin_Orange_Hier_Hi.fbx` | It — Base `#FF6A00`, Accent black nested downward-V chevrons chest + outer thighs |

No NASA / classic Hybrid III blue-beige livery on hero paint slots.

## Bind pose
- **Mild A-pose** — upper arms ~25–35° off torso, elbows soft, wrists neutral.
- Hands / forearms **clear pelvis / butt** (no V-into-butt).
- Mitten glove hands; flat crash-dummy shoe pads; egg head + **2 eye dots + temple row of 3** — **no visor**.
- Neck: **stacked 4 dark metal rings**. Waist: **dark ribbed bellows**. Chest: athletic polymer panel. Pelvis: defined yoke. Knees: LowerLeg nests in UpperLeg U-fork.

## Bone hierarchy (DummyLocomotor — names unchanged)
`Root` → `Hips` → `Spine` → `Chest` → `Neck` → `Head`  
`Hips` → `UpperLeg_L/R` → `LowerLeg_L/R` → `Foot_L/R`  
`Chest` → `Shoulder_L/R` → `UpperArm_L/R` → `LowerArm_L/R` → `Hand_L/R`

Required aliases present: `Hips`, `Spine`, `Head`, `UpperArm_*`, `LowerArm_*`, `UpperLeg_*`, `LowerLeg_*`.  
**LowerLeg ⊂ UpperLeg** (visible nest knee + large hinge disk).

## Mat slots
`Base`, `Accent`, `ItOverride` (match Dummy_Runner / Dummy_It). Joint/Sensor/Metal/Bellows extras.

## Export
`-Z` forward, `+Y` up. Origin at feet. Rebuild: Blender 4.x  
`blender -b -P /workspace/art-build/scripts/build_mannequin_hier_v4.py`

## Stills (v0.3)
`/workspace/art-build/previews/hipoly_v3_*.png` — idle front/3-4, run knee, slide grounded low-bar, punch, It idle.
"""
    with open(path, "w") as f:
        f.write(text)
    log(f"README {path}")


def main():
    open(LOG, "w").write("=== hipoly hier v4 Hybrid III refine v0.3 ===\n")
    log("Building Tan/Runner Hier HiPoly v4 (v0.3)…")
    ok1, ang1, cx1, cy1 = build_variant(
        False,
        f"{OUT_DIR}/Dummy_Mannequin_Tan_Hier_Hi.fbx",
        GUID_TAN,
        do_stills_tan=True,
    )
    log("Building Orange/It Hier HiPoly v4 (v0.3)…")
    ok2, ang2, cx2, cy2 = build_variant(
        True,
        f"{OUT_DIR}/Dummy_Mannequin_Orange_Hier_Hi.fbx",
        GUID_ORANGE,
        do_still_orange=True,
    )
    write_readme()
    log(
        f"SUCCESS tan_ok={ok1} orange_ok={ok2} "
        f"a_pose_deg={ang1:.1f} hand_clear_x={cx1:.3f} hand_y={cy1:.3f}"
    )
    print("DONE")


if __name__ == "__main__":
    main()
