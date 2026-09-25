#!/usr/bin/env python3
"""
HiPoly hierarchical mannequin v2 — curved crash-dummy, mild A-pose, real knees.
Art brief: 3D-BRIEF-hipoly-mannequin-anim-v0.1.md
DummyLocomotor bones: Hips, Spine, Head, UpperArm_*, LowerArm_*, UpperLeg_*, LowerLeg_*
"""
import bpy
import math
import os
import uuid
from mathutils import Vector, Euler, Matrix

OUT_DIR = "/workspace/tag-unity/Assets/Art/Characters/HiPoly"
PREV = "/workspace/art-build/previews"
BLEND = "/workspace/art-build/Dummy_Mannequin_Hier_Hi.blend"
LOG = "/tmp/hipoly_build.log"

# Style bible paint (sRGB approx)
BONE = (0.910, 0.851, 0.753, 1.0)      # #E8D9C0
TEAL = (0.169, 0.702, 0.639, 1.0)      # #2BB3A3
ORANGE = (1.0, 0.416, 0.0, 1.0)        # #FF6A00
BLACK = (0.04, 0.04, 0.045, 1.0)
JOINT = (0.12, 0.12, 0.14, 1.0)        # near-black matte rubber
RIM = (1.0, 0.45, 0.05, 1.0)
SENSOR = (0.02, 0.02, 0.02, 1.0)

SEG = 28
RING = 14
CYL_V = 24

# Mild A-pose geometry (meters). Character faces -Y. Origin at ground.
# Upper arms ~28° off torso (outboard), slight forward; elbows soft; hands clear pelvis.
SHOULDER_Z = 1.42
SHOULDER_X = 0.268
UA_LEN = 0.34
LA_LEN = 0.30
ARM_OUT = math.radians(28.0)   # from vertical
ARM_FWD = math.radians(10.0)   # slight forward so hands clear butt
ELBOW_SOFT = math.radians(18.0)  # soft bend

HIP_Z = 0.92
HIP_X = 0.11
UL_LEN = 0.42
LL_LEN = 0.40

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


def mat(name, color, metallic=0.03, roughness=0.48, emit=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.diffuse_color = color  # workbench
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
        ob.data.auto_smooth_angle = math.radians(60)


def apply_subsurf(ob, levels=1):
    mod = ob.modifiers.new("SubD", "SUBSURF")
    mod.levels = levels
    mod.render_levels = levels
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)


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


def capsule(name, a, b, radius, v=CYL_V):
    """Soft foam capsule between points a and b (world)."""
    a, b = Vector(a), Vector(b)
    mid = (a + b) * 0.5
    direction = b - a
    length = direction.length
    if length < 1e-6:
        return sph(name, mid, radius)
    o = cyl(name, mid, radius, length, v=v)
    # Align +Z of cylinder to direction
    quat = direction.normalized().to_track_quat("Z", "Y")
    o.rotation_euler = quat.to_euler()
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    # Cap ends with spheres
    c1 = sph(f"{name}_capA", a, radius * 0.98, seg=20, ring=10)
    c2 = sph(f"{name}_capB", b, radius * 0.98, seg=20, ring=10)
    return join(name, [o, c1, c2])


def cube(name, loc, scale, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = scale
    o.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    shade_smooth(o)
    return o



def chevron_v(tag, cx, cy, cz, half_w, bar_len, thick=0.013, depth=0.016, ang_deg=38.0):
    """Two bars forming a downward V on the front face (XZ plane, -Y outward).
    Rotation about Y so strokes read as \\ and / from front camera.
    """
    ang = math.radians(ang_deg)
    # Place bars so tips meet slightly below center
    drop = math.sin(ang) * bar_len * 0.55
    spread = math.cos(ang) * bar_len * 0.45
    left = cube(f'{tag}_L', (cx - spread, cy, cz + drop * 0.15),
                (bar_len * 0.5, depth, thick), (0, ang, 0))
    right = cube(f'{tag}_R', (cx + spread, cy, cz + drop * 0.15),
                 (bar_len * 0.5, depth, thick), (0, -ang, 0))
    return [left, right]

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


def rivet(name, loc, r=0.012):
    return sph(name, loc, r, seg=12, ring=6)


def arm_points(sx):
    """World points for mild A-pose arm on side sx (+1=L / -1=R)."""
    sh = Vector((sx * SHOULDER_X, 0.0, SHOULDER_Z))
    # Outboard + slight forward from vertical down
    out = math.sin(ARM_OUT)
    down = math.cos(ARM_OUT)
    fwd = math.sin(ARM_FWD)
    dir_ua = Vector((sx * out, -fwd, -down)).normalized()
    el = sh + dir_ua * UA_LEN
    # Soft elbow: bend so forearm continues down/out with slight additional forward
    # Local bend around outboard axis
    dir_la = (dir_ua + Vector((sx * 0.08, -0.22, -0.15))).normalized()
    # Also rotate a bit more out so hand clears pelvis
    dir_la = Vector((sx * abs(dir_la.x) + sx * 0.05, dir_la.y - 0.05, dir_la.z)).normalized()
    wr = el + dir_la * LA_LEN
    hand = wr + Vector((sx * 0.02, -0.04, -0.06))
    return sh, el, wr, hand


def leg_points(sx):
    hip = Vector((sx * HIP_X, 0.0, HIP_Z))
    kn = hip + Vector((sx * 0.01, 0.02, -UL_LEN))
    an = kn + Vector((0.0, 0.0, -LL_LEN))
    toe = an + Vector((0.0, -0.11, -0.01))
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

    bone("Hips", "Root", (0, 0, HIP_Z - 0.02), (0, 0, HIP_Z + 0.12))
    bone("Spine", "Hips", (0, 0, HIP_Z + 0.12), (0, 0, 1.28))
    bone("Chest", "Spine", (0, 0, 1.28), (0, 0, 1.42))
    bone("Neck", "Chest", (0, 0, 1.42), (0, 0, 1.52))
    bone("Head", "Neck", (0, 0, 1.52), (0, 0, 1.82))

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


def parent_to_bone(ob, arm_ob, bone_name):
    ob.parent = arm_ob
    ob.parent_type = "BONE"
    ob.parent_bone = bone_name
    # Keep world transform after parenting
    bpy.context.view_layer.update()


def assign_mats_slots(mesh, base, accent, over, joint, sensor):
    """Collapse to Base / Accent / ItOverride (+ keep Joint/Sensor as Base if needed)."""
    # Remap: Joint+Sensor -> Base slot for Unity 3-slot prefabs, but keep accents.
    name_to_mat = {"Base": base, "Accent": accent, "ItOverride": over}
    # Rebuild material slots on joined mesh by renaming existing
    for m in mesh.data.materials:
        if not m:
            continue
        n = m.name.split(".")[0]
        if n.startswith("Base"):
            m.name = "Base"
        elif n.startswith("Accent"):
            m.name = "Accent"
        elif n.startswith("ItOverride"):
            m.name = "ItOverride"
        elif n.startswith("Joint") or n.startswith("Sensor"):
            m.name = "Base"
    # Ensure exactly Base, Accent, ItOverride exist and are ordered
    wanted = [("Base", base), ("Accent", accent), ("ItOverride", over)]
    # Clear and reassign via polygon material indices — simpler: join groups already tagged
    return mesh


def build_mesh_parts(is_it, mats):
    """Build segmented curved foam parts. Returns dict bone_name -> [objects]."""
    base, accent, over, joint, sensor = mats
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

    # --- Head (egg, oversized) + sensors only (NO visor) ---
    head = sph("Head", (0, 0, 1.68), (0.175, 0.155, 0.215), seg=32, ring=16, sub=True)
    add("Head", head, base)
    for dx in (-0.05, 0.05):
        e = sph(f"Eye_{dx}", (dx, -0.148, 1.695), (0.024, 0.012, 0.024), seg=12, ring=6)
        add("Head", e, sensor)
    for i, z in enumerate([1.74, 1.695, 1.65]):
        t = sph(f"Temple_{i}", (0.155, -0.03, z), 0.012, seg=10, ring=5)
        add("Head", t, sensor)

    # Neck joint
    neck = cyl("Neck", (0, 0, 1.50), 0.058, 0.07)
    add("Neck", neck, joint)

    # Torso — soft foam spheres (crash-dummy panels)
    chest = sph("ChestFoam", (0, 0, 1.34), (0.20, 0.145, 0.155), seg=28, ring=14, sub=True)
    add("Chest", chest, base)
    mid = sph("MidFoam", (0, 0, 1.12), (0.175, 0.125, 0.10), seg=24, ring=12)
    add("Spine", mid, base)
    pelvis = sph("PelvisFoam", (0, 0, 0.94), (0.18, 0.13, 0.11), seg=24, ring=12)
    add("Hips", pelvis, base)

    if is_it:
        # Nested downward-V chevrons on chest front (-Y) — bible hazard marks
        for i, z in enumerate([1.42, 1.335, 1.25]):
            half = 0.095 - i * 0.012
            bars = chevron_v(f"ChC{i}", 0.0, -0.155, z, half, bar_len=half * 1.15,
                             thick=0.014, depth=0.018, ang_deg=36.0)
            for b in bars:
                add("Chest", b, accent)
        rim = cyl("ItRim", (0, 0, 1.545), 0.072, 0.016)
        add("Neck", rim, over)
    else:
        band = cyl("ChestBand", (0, 0, 1.28), 0.205, 0.055)
        add("Chest", band, accent)

    # Arms
    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr, hand = arm_points(sx)
        # Shoulder rivet/hinge
        shj = sph(f"ShoulderJ_{side}", sh, 0.075, seg=20, ring=10)
        add(f"Shoulder_{side}", shj, joint)
        for ang in (0, 90, 180, 270):
            rv = rivet(f"ShRiv_{side}_{ang}",
                       sh + Vector((math.cos(math.radians(ang)) * 0.055,
                                    math.sin(math.radians(ang)) * 0.055, 0.02)))
            add(f"Shoulder_{side}", rv, joint)

        ua = capsule(f"UA_{side}", sh, el, 0.055)
        add(f"UpperArm_{side}", ua, base)
        if is_it:
            for i, t in enumerate((0.25, 0.45, 0.65)):
                p = sh.lerp(el, t)
                st = cyl(f"UAStripe_{side}_{i}", p, 0.058, 0.02)
                add(f"UpperArm_{side}", st, accent)
        else:
            p = sh.lerp(el, 0.35)
            st = cyl(f"UAStripe_{side}", p, 0.058, 0.042)
            add(f"UpperArm_{side}", st, accent)

        # Elbow hinge
        elj = sph(f"ElbowJ_{side}", el, 0.058, seg=18, ring=9)
        add(f"LowerArm_{side}", elj, joint)
        for dx in (-0.035, 0.035):
            rv = rivet(f"ElRiv_{side}_{dx}", el + Vector((dx, -0.04, 0)))
            add(f"LowerArm_{side}", rv, joint)

        la = capsule(f"LA_{side}", el, wr, 0.044)
        add(f"LowerArm_{side}", la, base)

        wrj = sph(f"WristJ_{side}", wr, 0.042, seg=14, ring=7)
        add(f"Hand_{side}", wrj, joint)

        # Chunky mitten hand (no spaghetti fingers)
        palm = sph(f"Palm_{side}", hand + Vector((0, -0.01, 0.01)), (0.055, 0.070, 0.045), seg=18, ring=9)
        add(f"Hand_{side}", palm, base)
        mitt = sph(f"Mitt_{side}", hand + Vector((0, -0.03, -0.035)), (0.050, 0.060, 0.038), seg=16, ring=8)
        add(f"Hand_{side}", mitt, base)
        thumb = sph(f"Thumb_{side}", hand + Vector((sx * 0.045, 0.02, -0.01)), (0.022, 0.030, 0.022), seg=12, ring=6)
        add(f"Hand_{side}", thumb, base)

    # Legs
    for side, sx in (("L", 1), ("R", -1)):
        hip, kn, an, toe = leg_points(sx)
        hipj = sph(f"HipJ_{side}", hip, 0.085, seg=20, ring=10)
        add(f"UpperLeg_{side}", hipj, joint)
        for ang in (45, 135, 225, 315):
            rv = rivet(f"HipRiv_{side}_{ang}",
                       hip + Vector((math.cos(math.radians(ang)) * 0.06,
                                     math.sin(math.radians(ang)) * 0.06, 0.01)))
            add(f"UpperLeg_{side}", rv, joint)

        thigh = capsule(f"Thigh_{side}", hip, kn, 0.072)
        add(f"UpperLeg_{side}", thigh, base)
        if is_it:
            # Outer-thigh nested downward-V chevrons (front-outer face)
            for i, tt in enumerate((0.28, 0.46, 0.64)):
                p = hip.lerp(kn, tt)
                half = 0.048 - i * 0.006
                # Bias to outer + front of thigh
                cx = p.x + sx * 0.03
                cy = p.y - 0.072
                bars = chevron_v(f"ChT{side}{i}", cx, cy, p.z, half, bar_len=half * 1.2,
                                 thick=0.012, depth=0.015, ang_deg=36.0)
                for b in bars:
                    add(f"UpperLeg_{side}", b, accent)
        else:
            p = hip.lerp(kn, 0.40)
            ts = cyl(f"ThighStripe_{side}", p, 0.076, 0.045)
            add(f"UpperLeg_{side}", ts, accent)

        # Knee hinge — CRITICAL readable joint
        knj = sph(f"KneeJ_{side}", kn, 0.065, seg=20, ring=10)
        add(f"LowerLeg_{side}", knj, joint)
        # Hinge axle + rivets
        axle = cyl(f"KneeAxle_{side}", kn, 0.018, 0.09, rot=(0, math.radians(90), 0), v=12)
        add(f"LowerLeg_{side}", axle, joint)
        for dx in (-0.048, 0.048):
            rv = rivet(f"KnRiv_{side}_{dx}", kn + Vector((dx, -0.035, 0)), 0.014)
            add(f"LowerLeg_{side}", rv, joint)

        shin = capsule(f"Shin_{side}", kn, an, 0.052)
        add(f"LowerLeg_{side}", shin, base)

        anj = sph(f"AnkleJ_{side}", an, 0.048, seg=14, ring=7)
        add(f"Foot_{side}", anj, joint)
        for dx in (-0.03, 0.03):
            rv = rivet(f"AnRiv_{side}_{dx}", an + Vector((dx, -0.02, 0.01)), 0.01)
            add(f"Foot_{side}", rv, joint)

        foot = sph(f"FootFoam_{side}", an + Vector((0, -0.055, -0.01)), (0.065, 0.110, 0.040), seg=18, ring=9)
        add(f"Foot_{side}", foot, base)

    return groups


def parent_groups(groups, arm_ob):
    for bone_name, objs in groups.items():
        if not objs:
            continue
        merged = join(f"Mesh_{bone_name}", objs)
        if merged is None:
            continue
        # Store world matrix, parent to bone, restore
        mw = merged.matrix_world.copy()
        merged.parent = arm_ob
        merged.parent_type = "BONE"
        merged.parent_bone = bone_name
        # Bone-space: need to counteract bone rest
        bpy.context.view_layer.update()
        bone = arm_ob.pose.bones[bone_name]
        # world = arm.matrix_world * bone.matrix * obj.matrix_local
        # => matrix_local = inv(arm.world * bone.matrix) * mw
        merged.matrix_parent_inverse = (arm_ob.matrix_world @ bone.matrix).inverted()
        merged.matrix_world = mw


def make_mats(is_it):
    suffix = "_It" if is_it else "_Tan"
    base = mat("Base", ORANGE if is_it else BONE, roughness=0.46)
    accent = mat("Accent", BLACK if is_it else TEAL, roughness=0.38)
    over = mat("ItOverride", RIM if is_it else BONE, roughness=0.4, emit=(1.1 if is_it else 0.0))
    joint = mat("Joint" + suffix, JOINT, metallic=0.35, roughness=0.32)
    sensor = mat("Sensor" + suffix, SENSOR, roughness=0.22)
    # Force datablock names for Unity slots
    base.name = "Base"
    accent.name = "Accent"
    over.name = "ItOverride"
    return base, accent, over, joint, sensor


def export_fbx(path, arm_ob):
    # Strip workbench helpers so they never land in the character FBX
    for name in list(bpy.data.objects.keys()):
        o = bpy.data.objects[name]
        if o.type in ("CAMERA", "LIGHT") or name in ("Ground", "KeySun", "Fill", "ShotCam"):
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.select_all(action="DESELECT")
    arm_ob.select_set(True)
    for o in bpy.data.objects:
        if o.type == "MESH" and (o.parent == arm_ob or o.name.startswith("Mesh_") or o.name.startswith("_It")):
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


def write_meta(fbx_path):
    meta = fbx_path + ".meta"
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
    # Ground
    if "Ground" not in bpy.data.objects:
        bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, 0))
        g = bpy.context.active_object
        g.name = "Ground"
        gm = mat("GroundMat", (0.55, 0.55, 0.58, 1), roughness=0.9)
        set_mat(g, gm)
    # Light
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


def set_bone_euler(arm_ob, name, euler_deg):
    pb = arm_ob.pose.bones.get(name)
    if not pb:
        log(f"WARN missing bone {name}")
        return
    pb.rotation_mode = "XYZ"
    pb.rotation_euler = Euler(tuple(math.radians(a) for a in euler_deg), "XYZ")


def pose_idle(arm_ob):
    reset_pose(arm_ob)
    # Soft idle breath lean — still clear hands
    set_bone_euler(arm_ob, "Spine", (2, 0, 0))
    set_bone_euler(arm_ob, "LowerArm_L", (-8, 0, 0))
    set_bone_euler(arm_ob, "LowerArm_R", (-8, 0, 0))


def pose_run_knee(arm_ob):
    """Side-readable run: recovery knee clearly bent."""
    reset_pose(arm_ob)
    set_bone_euler(arm_ob, "Spine", (8, 0, 0))
    set_bone_euler(arm_ob, "Hips", (6, 0, 0))
    # Drive leg L forward, R recovery with knee bend
    set_bone_euler(arm_ob, "UpperLeg_L", (-42, 0, 0))
    set_bone_euler(arm_ob, "LowerLeg_L", (18, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_R", (48, 0, 0))   # thigh back
    set_bone_euler(arm_ob, "LowerLeg_R", (95, 0, 0))  # recovery knee bent hard
    set_bone_euler(arm_ob, "UpperArm_L", (55, 0, 10))
    set_bone_euler(arm_ob, "LowerArm_L", (-70, 0, 0))
    set_bone_euler(arm_ob, "UpperArm_R", (-50, 0, -10))
    set_bone_euler(arm_ob, "LowerArm_R", (-35, 0, 0))
    set_bone_euler(arm_ob, "Head", (-4, 0, 0))


def pose_slide(arm_ob):
    reset_pose(arm_ob)
    set_bone_euler(arm_ob, "Hips", (32, 0, 0))
    set_bone_euler(arm_ob, "Spine", (38, 0, 0))
    set_bone_euler(arm_ob, "Head", (12, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_L", (55, 5, 0))
    set_bone_euler(arm_ob, "LowerLeg_L", (70, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_R", (50, -5, 0))
    set_bone_euler(arm_ob, "LowerLeg_R", (75, 0, 0))
    set_bone_euler(arm_ob, "UpperArm_L", (70, -15, 25))
    set_bone_euler(arm_ob, "LowerArm_L", (-40, 0, 0))
    set_bone_euler(arm_ob, "UpperArm_R", (70, 15, -25))
    set_bone_euler(arm_ob, "LowerArm_R", (-40, 0, 0))
    # Drop root slightly for low silhouette
    arm_ob.location.z = -0.35


def pose_punch(arm_ob):
    reset_pose(arm_ob)
    arm_ob.location.z = 0
    set_bone_euler(arm_ob, "Hips", (4, 18, 0))
    set_bone_euler(arm_ob, "Spine", (6, 22, 0))
    set_bone_euler(arm_ob, "Head", (0, 10, 0))
    # Right arm punch extension (R = -X side)
    set_bone_euler(arm_ob, "UpperArm_R", (-12, 5, -75))
    set_bone_euler(arm_ob, "LowerArm_R", (-8, 0, 0))
    set_bone_euler(arm_ob, "UpperArm_L", (35, -10, 20))
    set_bone_euler(arm_ob, "LowerArm_L", (-55, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_L", (-15, 0, 0))
    set_bone_euler(arm_ob, "UpperLeg_R", (12, 0, 0))


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
    # Hierarchy checks
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
    ]
    log(f"Bones present: {names}")
    log(f"Missing required: {missing}")
    for label, ok in checks:
        log(f"  hierarchy {label}: {'OK' if ok else 'FAIL'}")
    return not missing and all(ok for _, ok in checks)


def measure_a_pose(arm_ob):
    """Report upper-arm angle off vertical and hand clearance vs pelvis."""
    sh_l = Vector(arm_ob.matrix_world @ arm_ob.pose.bones["UpperArm_L"].head)
    el_l = Vector(arm_ob.matrix_world @ arm_ob.pose.bones["UpperArm_L"].tail)
    hand_l = Vector(arm_ob.matrix_world @ arm_ob.pose.bones["Hand_L"].tail)
    pelvis = Vector((0, 0, HIP_Z))
    ua = (el_l - sh_l).normalized()
    # angle from vertical down (0,-1) in XZ / 3d
    vertical = Vector((0, 0, -1))
    ang = math.degrees(ua.angle(vertical))
    # Hand clearance: horizontal distance from pelvis center xz, and y-forward
    clear_x = abs(hand_l.x) - 0.18  # pelvis half-width ~0.18
    clear_y = hand_l.y  # more negative = more forward = clearer of butt
    log(f"A-pose UpperArm_L angle off vertical: {ang:.1f} deg")
    log(f"Hand_L world: {tuple(round(c,3) for c in hand_l)} clear_x={clear_x:.3f}m y={clear_y:.3f}")
    return ang, clear_x, clear_y


def build_variant(is_it, export_path, do_stills_tan=False, do_still_orange=False):
    clear_scene()
    mats = make_mats(is_it)
    arm_ob = build_armature()
    groups = build_mesh_parts(is_it, mats)
    parent_groups(groups, arm_ob)
    ok = verify_bones(arm_ob)
    ang, cx, cy = measure_a_pose(arm_ob)

    # Stills from Tan bind / poses; one Orange front
    if do_stills_tan:
        pose_idle(arm_ob)
        render_shot(f"{PREV}/hipoly_idle_front.png", (0.15, -3.2, 1.35), (0, 0, 1.05))
        render_shot(f"{PREV}/hipoly_idle_34.png", (2.2, -2.4, 1.45), (0, 0, 1.05))
        pose_run_knee(arm_ob)
        render_shot(f"{PREV}/hipoly_run_knee.png", (3.4, 0.1, 1.15), (0, 0, 0.95))
        pose_slide(arm_ob)
        render_shot(f"{PREV}/hipoly_slide_crouch.png", (0.2, -3.4, 0.7), (0, 0, 0.55))
        pose_punch(arm_ob)
        render_shot(f"{PREV}/hipoly_punch.png", (1.8, -2.8, 1.3), (0.15, 0, 1.15))
        # Reset for bind export
        arm_ob.location = (0, 0, 0)
        reset_pose(arm_ob)

    if do_still_orange:
        pose_idle(arm_ob)
        render_shot(f"{PREV}/hipoly_it_idle_front.png", (0.15, -3.2, 1.35), (0, 0, 1.05))
        reset_pose(arm_ob)

    export_fbx(export_path, arm_ob)
    write_meta(export_path)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND.replace(".blend", "_It.blend" if is_it else "_Tan.blend"))
    return ok, ang, cx, cy


def write_readme():
    path = os.path.join(OUT_DIR, "README.md")
    text = """# HiPoly Hierarchical Mannequins

DummyLocomotor-bindable curved crash-test dummies (Navy Spade soft foam + polymer panels + matte rubber joints).

## Assets
| File | Paint |
|------|-------|
| `Dummy_Mannequin_Tan_Hier_Hi.fbx` | Runner — Base `#E8D9C0`, Accent `#2BB3A3` chest band + limb stripes |
| `Dummy_Mannequin_Orange_Hier_Hi.fbx` | It — Base `#FF6A00`, Accent black nested downward-V chevrons chest + outer thighs |

## Bind pose
- **Mild A-pose** — upper arms ~25–30° off torso, elbows soft, wrists neutral.
- Hands / forearms **clear pelvis / butt** (no V-into-butt).
- Mitten hands; egg head + black sensor dots only — **no visor**.

## Bone hierarchy (DummyLocomotor — names unchanged)
`Root` → `Hips` → `Spine` → `Chest` → `Neck` → `Head`  
`Hips` → `UpperLeg_L/R` → `LowerLeg_L/R` → `Foot_L/R`  
`Chest` → `Shoulder_L/R` → `UpperArm_L/R` → `LowerArm_L/R` → `Hand_L/R`

Required aliases present: `Hips`, `Spine`, `Head`, `UpperArm_*`, `LowerArm_*`, `UpperLeg_*`, `LowerLeg_*`.  
**LowerLeg is a real bend joint under UpperLeg** (knee hinge readable).

## Mat slots
`Base`, `Accent`, `ItOverride` (match Dummy_Runner / Dummy_It).

## Export
`-Z` forward, `+Y` up. Rebuild: Blender 4.x  
`blender -b -P /workspace/art-build/scripts/build_mannequin_hier_v2.py`

## Stills
`/workspace/art-build/previews/hipoly_*.png` — idle front/3-4, run knee, slide crouch, punch, It idle.
"""
    with open(path, "w") as f:
        f.write(text)
    log(f"README {path}")


def main():
    open(LOG, "w").write("=== hipoly hier v2 build ===\n")
    log("Building Tan/Runner Hier HiPoly…")
    ok1, ang1, cx1, cy1 = build_variant(
        False,
        f"{OUT_DIR}/Dummy_Mannequin_Tan_Hier_Hi.fbx",
        do_stills_tan=True,
    )
    log("Building Orange/It Hier HiPoly…")
    ok2, ang2, cx2, cy2 = build_variant(
        True,
        f"{OUT_DIR}/Dummy_Mannequin_Orange_Hier_Hi.fbx",
        do_still_orange=True,
    )
    write_readme()
    log(f"SUCCESS tan_ok={ok1} orange_ok={ok2} a_pose_deg={ang1:.1f} hand_clear_x={cx1:.3f} hand_y={cy1:.3f}")
    print("DONE")


if __name__ == "__main__":
    main()
