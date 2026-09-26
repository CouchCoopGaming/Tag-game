#!/usr/bin/env python3
"""
HiPoly hierarchical mannequin v8.8 / Hier v0.7.8 resting-fist + shoulders in

KEEP locked (v0.7.5 Bionicle + v0.7.6/b joint direction Landon-cleared):
  - FLAT continuous hard chest plate
  - Dense dark accordion waist (~6-8 ribs) + hard pelvis
  - Long hard limb shells / satin vinyl
  - Accordion neck rings / metal neck (NOT limb joints -- leave as-is)
  - Paint LOCKED: Runner cream #E8D9C0 / It warm tan + black nested Vs
  - Bionicle knob-and-slot / ball-socket language (NOT flat discs)
  - NO soft egg / mid-freq boil / inflated flesh
  - Joints SMALL: shoulder~0.022 elbow/ankle~0.017 wrist~0.012 hip~0.023 knee~0.019
  - Shoulders DOWN + BACK: SHOULDER_Z≈1.40, SHOULDER_Y≈0.06 (+Y=back)
  - SOLID vinyl capsule/sleeve cover (NO thin torus hole) — metal barely peeks
  - NO Finger_ bones (finger detail mesh-only)
  - Hybrid III body volumes + MCP knuckles + PIP mid joints (from v0.7.7)

CHANGE v0.7.8:
  - Shoulders notably narrower: SHOULDER_X 0.285 → 0.235; chest/clav/shelf
    girdle half-widths brought in so idle front reads less ape-shelf
  - Resting-fist soft finger curl (mesh-only): tip_extra / segment endpoints
    arc palm-ward (less -Y stretch, MCP+PIP flex); thumb tucked alongside
    index — soft closed fist at rest, NOT outstretched/splayed, NOT clenched rock

DummyLocomotor bones / hierarchy / GUIDs / FBX paths unchanged.
GUID-safe FBX overwrite (do NOT rewrite .meta). NO git push.
"""
import bpy
import math
import os
import uuid
from mathutils import Vector, Euler

OUT_DIR = "/workspace/tag-unity/Assets/Art/Characters/HiPoly"
PREV = "/workspace/art-build/previews"
BLEND = "/workspace/art-build/Dummy_Mannequin_Hier_Hi.blend"
LOG = "/tmp/hipoly_v78_build.log"
REF_CRASH = "/workspace/tag-gdd/art/refs/hybrid-iii-v03/ref_hybrid_iii_crash_dummy.jpg"

GUID_TAN = "ad3f2fa97db94e72869d746ecdf8e87d"
GUID_ORANGE = "b33974ad57284ef28a7564e3bdf00540"

# Paint LOCKED (v8.2 stills read) — Runner cream / It warm tan — NOT ref orange.
BONE = (0.95, 0.88, 0.76, 1.0)         # Runner cream #E8D9C0 stills read
WARM_TAN = (0.82, 0.66, 0.50, 1.0)     # It warm tan (NOT #FF6A00)
TEAL = (0.169, 0.702, 0.639, 1.0)      # #2BB3A3
BLACK = (0.04, 0.04, 0.045, 1.0)
JOINT = (0.10, 0.10, 0.12, 1.0)        # dark metal Bionicle joints
METAL = (0.32, 0.32, 0.34, 1.0)        # neck rings — readable dark metal
DARK_BELLOWS = (0.06, 0.06, 0.07, 1.0)
RIM = (0.15, 0.12, 0.10, 1.0)
SENSOR = (0.02, 0.02, 0.02, 1.0)
CAL_YELLOW = (0.95, 0.82, 0.08, 1.0)
LIP = (0.12, 0.12, 0.13, 1.0)          # dark bead/lip seam edge

SEG = 36
RING = 18
CYL_V = 28

# Mild A-pose 20–35°; hands clear pelvis
# v0.7.4 human mannequin proportions (store-mannequin anatomy):
# longer legs, narrower torso, real shoulder>hip width, smaller head.
# v0.7.6b: shoulders DOWN + BACK harder (Art: not clear vs tip 3322317).
# Face = -Y so back = +Y.
# v0.7.8: shoulders DOWN+BACK kept; girdle narrowed (SHOULDER_X 0.235)
SHOULDER_Z = 1.400
SHOULDER_Y = 0.060
SHOULDER_X = 0.235
UA_LEN = 0.370
LA_LEN = 0.330
ARM_OUT = math.radians(24.0)
ARM_FWD = math.radians(10.0)

HIP_Z = 1.05
HIP_X = 0.118
UL_LEN = 0.540
LL_LEN = 0.490

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


def mat(name, color, metallic=0.02, roughness=0.45, emit=0.0, subsurface=0.0,
        ss_color=None, ss_radius=(1.0, 0.45, 0.20)):
    """Principled helper. Vinyl uses moderate roughness + soft SSS; metal/rubber separate."""
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    m.diffuse_color = color
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    # Soft SSS / edge glow for crash-dummy vinyl (not chalky, not car-paint)
    if subsurface > 0:
        if ss_color is None:
            ss_color = (
                min(1.0, color[0] * 1.08),
                min(1.0, color[1] * 0.92),
                min(1.0, color[2] * 0.65),
                1.0,
            )
        if "Subsurface Weight" in bsdf.inputs:
            bsdf.inputs["Subsurface Weight"].default_value = subsurface
            if "Subsurface Radius" in bsdf.inputs:
                bsdf.inputs["Subsurface Radius"].default_value = ss_radius
            if "Subsurface Color" in bsdf.inputs:
                bsdf.inputs["Subsurface Color"].default_value = ss_color
            # Blender 4.x may use Subsurface Scale
            if "Subsurface Scale" in bsdf.inputs:
                bsdf.inputs["Subsurface Scale"].default_value = 0.08
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


def boolean_union(target, tools):
    """Union tool meshes into target (molded vinyl relief). Destroys tools."""
    tools = [t for t in tools if t is not None]
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = target
    target.select_set(True)
    for i, tool in enumerate(tools):
        mod = target.modifiers.new(f"BoolU_{i}", "BOOLEAN")
        mod.operation = "UNION"
        mod.solver = "EXACT"
        mod.object = tool
        try:
            apply_mod(target, mod.name)
        except Exception as e:
            log(f"boolean_union fail on {tool.name}: {e} — joining instead")
            if mod.name in target.modifiers:
                target.modifiers.remove(mod)
            join(target.name, [target, tool])
            target = bpy.context.active_object
            continue
        bpy.data.objects.remove(tool, do_unlink=True)
    shade_smooth(target)
    return target


def boolean_difference(target, tools):
    """Carve tool meshes out of target (shallow eye recesses). Destroys tools."""
    tools = [t for t in tools if t is not None]
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = target
    target.select_set(True)
    for i, tool in enumerate(tools):
        mod = target.modifiers.new(f"BoolD_{i}", "BOOLEAN")
        mod.operation = "DIFFERENCE"
        mod.solver = "EXACT"
        mod.object = tool
        try:
            apply_mod(target, mod.name)
        except Exception as e:
            log(f"boolean_difference fail on {tool.name}: {e} — skip recess")
            if mod.name in target.modifiers:
                target.modifiers.remove(mod)
            bpy.data.objects.remove(tool, do_unlink=True)
            continue
        bpy.data.objects.remove(tool, do_unlink=True)
    shade_smooth(target)
    return target


def light_smooth(ob, iterations=4):
    """Light smooth only — preserve shell edges / segmentation. NO voxel remesh."""
    apply_smooth_corrective(ob, factor=0.35, iterations=iterations)
    shade_smooth(ob)
    return ob


def tapered_limb(name, a, b, r0, r1, v=CYL_V, caps=True):
    """Segmented limb shell volume (upper→lower taper). Soft end caps, NO remesh.
    caps=False for finger shells (AD: kill tip-orb stacks).
    """
    a, b = Vector(a), Vector(b)
    mid = (a + b) * 0.5
    direction = b - a
    length = direction.length
    if length < 1e-6:
        return sph(name, mid, r0)
    body = cone(name, mid, r0, r1, max(length * 0.94, 0.04), v=v)
    quat = direction.normalized().to_track_quat("Z", "Y")
    body.rotation_euler = quat.to_euler()
    bpy.ops.object.select_all(action="DESELECT")
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    if not caps:
        return body
    # Minimal end rounding only — avoid stacked-egg boil read
    c0 = sph(f"{name}_cap0", a, r0 * 0.78, seg=12, ring=6)
    c1 = sph(f"{name}_cap1", b, r1 * 0.72, seg=10, ring=5)
    return join(name, [body, c0, c1])


def bead_lip(name, loc, radius, axis="Z", thick=0.015, flare=1.14):
    """Soft Hybrid III bead/lip seam at shell terminus — thicker v0.6 plate read, not toy stripe."""
    if axis == "Z":
        rot = (0, 0, 0)
    elif axis == "X":
        rot = (0, math.radians(90), 0)
    else:
        rot = (math.radians(90), 0, 0)
    outer = cyl(f"{name}_outer", loc, radius * flare, thick, rot=rot, v=28)
    mid = cyl(f"{name}_mid", loc, radius * 1.02, thick * 0.72, rot=rot, v=28)
    inner = cyl(f"{name}_inner", loc, radius * 0.90, thick * 1.20, rot=rot, v=24)
    return join(name, [outer, mid, inner])



def bionicle_joint(name, loc, axis="X", size=0.019, simple=False):
    """Bionicle-style knob-and-slot / ball-socket joint (v0.7.6 smaller).

    size = ball/knob radius (~70-80% of v0.7.5; shells dominate).
    Geometry: central ball seated between paired cup flanges + thin axle pin.
    NO large flat riveted disc / hockey-puck silhouette.
    simple=True: single knob-slot (wrist) — less dual-disc under sleeve.
    """
    loc = Vector(loc)
    if axis == "X":
        rot = (0, math.radians(90), 0)
        side = lambda d: Vector((d, 0, 0))
    elif axis == "Y":
        rot = (math.radians(90), 0, 0)
        side = lambda d: Vector((0, d, 0))
    else:
        rot = (0, 0, 0)
        side = lambda d: Vector((0, 0, d))

    ball_r = size
    # Central ball / knob -- primary readable mass
    ball = sph(f"{name}_ball", loc, ball_r, seg=16, ring=8)

    # Thin axle pin through the ball (hinge axis)
    pin = cyl(f"{name}_pin", loc, ball_r * 0.20, ball_r * 2.20, rot=rot, v=12)

    if simple:
        # Single cup/slot + thin collar — less dual-disc wrist read under sleeve
        cup_off = ball_r * 0.55
        cup_r = ball_r * 0.78
        cup_thick = ball_r * 0.38
        c1 = cyl(f"{name}_cupA", loc + side(cup_off), cup_r, cup_thick, rot=rot, v=14)
        collar = cyl(f"{name}_collar", loc, ball_r * 1.05, ball_r * 0.16, rot=rot, v=16)
        nub = sph(
            f"{name}_nub",
            loc + side(cup_off + cup_thick * 0.50),
            ball_r * 0.22,
            seg=10,
            ring=5,
        )
        return join(name, [ball, pin, c1, collar, nub])

    # Paired cup/slot flanges -- grip the ball from both sides (not plate-sized pucks)
    flange_off = ball_r * 0.88
    flange_r = ball_r * 0.90
    flange_thick = ball_r * 0.30
    f1 = cyl(f"{name}_flangeA", loc + side(flange_off), flange_r, flange_thick, rot=rot, v=18)
    f2 = cyl(f"{name}_flangeB", loc + side(-flange_off), flange_r, flange_thick, rot=rot, v=18)

    # Inner seating cups closer to the ball (C-socket walls)
    cup_off = ball_r * 0.52
    cup_r = ball_r * 0.68
    cup_thick = ball_r * 0.40
    c1 = cyl(f"{name}_cupA", loc + side(cup_off), cup_r, cup_thick, rot=rot, v=14)
    c2 = cyl(f"{name}_cupB", loc + side(-cup_off), cup_r, cup_thick, rot=rot, v=14)

    # Thin mid collar -- slot ring around equator, NOT a large face disc
    collar = cyl(f"{name}_collar", loc, ball_r * 1.06, ball_r * 0.16, rot=rot, v=18)

    # Small outer knob nub on one flange (Bionicle connector read)
    nub = sph(
        f"{name}_nub",
        loc + side(flange_off + flange_thick * 0.55),
        ball_r * 0.24,
        seg=10,
        ring=5,
    )

    # Tiny pin end-caps so axle reads through flanges
    cap_r = ball_r * 0.15
    cap_d = ball_r * 0.13
    cap1 = cyl(f"{name}_capA", loc + side(flange_off + flange_thick * 0.55),
               cap_r, cap_d, rot=rot, v=10)
    cap2 = cyl(f"{name}_capB", loc + side(-(flange_off + flange_thick * 0.55)),
               cap_r, cap_d, rot=rot, v=10)

    return join(name, [ball, pin, f1, f2, c1, c2, collar, nub, cap1, cap2])


def joint_skin_cover(name, loc, axis="X", size=0.019):
    """SOLID vinyl capsule/sleeve enveloping ball-socket (v0.7.7 — no torus hole).

    LOCAL to joint region only (shoulders/elbows/wrists/hips/knees/ankles).
    Same Base mat as limb shells (NOT joint metal). Subordinate to hard shells —
    NO soft-egg torso regression. ADD AFTER dark joint so cover sits outside.
    Solid envelope + capsule + tube (NO open torus hole — that was the v76 fail).
    Radius >= ball*1.40; length bridges shell gap. Metal only barely peeks under cover.
    """
    loc = Vector(loc)
    if axis == "X":
        rot = (0, math.radians(90), 0)
        fill_scale = (size * 1.08, size * 1.55, size * 1.55)
        tube_rot = (math.radians(90), 0, 0)  # along Z for upright limbs
    elif axis == "Y":
        rot = (math.radians(90), 0, 0)
        fill_scale = (size * 1.55, size * 1.08, size * 1.55)
        tube_rot = (0, 0, 0)
    else:
        rot = (0, 0, 0)
        fill_scale = (size * 1.55, size * 1.55, size * 1.08)
        tube_rot = (0, math.radians(90), 0)

    # Outer SOLID envelope sphere — FULLY covers ball (r >= ball * 1.45)
    envelope = sph(
        f"{name}_env", loc,
        size * 1.48, seg=20, ring=12)

    # Elongated vinyl capsule bridging the shell gap (solid, not hollow)
    fill = sph(
        f"{name}_fill", loc,
        fill_scale, seg=16, ring=8)

    # Solid bridging tube — radius >= ball*1.38, length spans shell termini
    tube = cyl(
        f"{name}_tube", loc,
        size * 1.40, size * 1.70, rot=tube_rot, v=24)

    # End-cap spheres blend sleeve toward shell lips (LOCAL only — not torso boil)
    cap_a = sph(f"{name}_capA", loc, size * 1.32, seg=14, ring=8)
    cap_b = sph(f"{name}_capB", loc, (size * 1.22, size * 1.22, size * 1.05), seg=12, ring=6)
    return join(name, [envelope, fill, tube, cap_a, cap_b])



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
    """Thumb + SEPARATED fingers with knuckles + mid joints (v0.7.8 resting fist).

    Hands-only pass — static shells parented to Hand_L/R (no Finger_ bones).
    Readable MCP knuckle bumps + PIP mid-segment rings under vinyl.
    AD soft: NO tip-orb stacks — tapered shells end clean.
    Soft resting-fist curl at MCP+PIP (palm-ward); thumb tucked alongside index.
    """
    parts = []
    # wrist 0.012 simple + SOLID vinyl envelope
    wrj = bionicle_joint(f"{name}_WristJ", wr, axis="X", size=0.012, simple=True)
    set_mat(wrj, joint_mat)
    parts.append(wrj)
    wr_cover = joint_skin_cover(f"{name}_WristCover", wr, axis="X", size=0.012)
    set_mat(wr_cover, base_mat)
    parts.append(wr_cover)

    # Molded hard-shell palm — Hybrid III flatter plate (not blob)
    palm = sph(f"{name}_Palm",
               hand + Vector((0, -0.006, 0.006)),
               (0.040, 0.026, 0.046), seg=18, ring=10)
    set_mat(palm, base_mat)
    parts.append(palm)
    dorsum = cube(
        f"{name}_Dorsum",
        hand + Vector((0, -0.012, 0.020)),
        (0.036, 0.014, 0.024), bevel=0.004)
    set_mat(dorsum, base_mat)
    parts.append(dorsum)

    # Separated fingers — soft resting-fist curl (MCP + PIP flex palm-ward).
    # Dorsum is +Z → palm faces -Z; curl tips toward palm: less -Y reach, more -Z.
    # Two-segment shells (prox MCP→PIP, dist PIP→tip) so arc reads at hand_close.
    # NO tip orbs. Soft fist — not outstretched, not clenched rock.
    finger_offsets = [
        (-0.036, -0.042, -0.004),
        (-0.012, -0.054, -0.006),
        (0.012, -0.056, -0.006),
        (0.036, -0.042, -0.004),
    ]
    # PIP mid joints — partial curl (~35–40° flex feel)
    pip_extra = [
        (-0.038, -0.058, -0.036),
        (-0.012, -0.068, -0.040),
        (0.012, -0.070, -0.042),
        (0.038, -0.058, -0.036),
    ]
    # Distal tips — soft closed fist (less -Y than v0.7.7 ~-0.15; more -Z tuck)
    tip_extra = [
        (-0.030, -0.062, -0.078),
        (-0.009, -0.070, -0.088),
        (0.009, -0.072, -0.090),
        (0.030, -0.062, -0.078),
    ]
    for i, (ox, oy, oz) in enumerate(finger_offsets):
        start = hand + Vector((ox, oy * 0.28, oz * 0.30))
        mid = hand + Vector(pip_extra[i])
        end = hand + Vector(tip_extra[i])
        dir_prox = (mid - start)
        len_prox = dir_prox.length
        dirc = dir_prox.normalized() if len_prox > 1e-6 else Vector((0, -1, 0))
        dir_dist = (end - mid)
        dird = dir_dist.normalized() if dir_dist.length > 1e-6 else dirc
        r0 = 0.0110 if i in (1, 2) else 0.0100
        r_mid = 0.0080 if i in (1, 2) else 0.0075
        r1 = 0.0055
        fing0 = tapered_limb(f"{name}_F{i}a", start, mid, r0, r_mid, v=12, caps=False)
        set_mat(fing0, base_mat)
        parts.append(fing0)
        fing1 = tapered_limb(f"{name}_F{i}b", mid, end, r_mid, r1, v=12, caps=False)
        set_mat(fing1, base_mat)
        parts.append(fing1)

        # MCP knuckle bump — dorsal vinyl sphere at finger root (readable at hand close)
        kn_r = r0 * 1.35
        knuckle = sph(
            f"{name}_Knuckle{i}",
            start + dirc * (len_prox * 0.08) + Vector((0, -0.003, 0.004)),
            (kn_r, kn_r * 0.85, kn_r * 0.95), seg=10, ring=6)
        set_mat(knuckle, base_mat)
        parts.append(knuckle)

        # PIP mid-finger joint — vinyl crease ring + small nest ball under shell
        mid_r = (r0 + r1) * 0.55
        mid_ball = sph(
            f"{name}_MidJ{i}", mid,
            mid_r * 1.15, seg=10, ring=5)
        set_mat(mid_ball, base_mat)
        parts.append(mid_ball)
        # thin crease collar (solid cyl, not open torus hole)
        axis_guess = "Y" if abs(dird.y) > 0.55 else ("Z" if abs(dird.z) > 0.55 else "X")
        if axis_guess == "Y":
            mid_rot = (math.radians(90), 0, 0)
        elif axis_guess == "X":
            mid_rot = (0, math.radians(90), 0)
        else:
            mid_rot = (0, 0, 0)
        mid_ring = cyl(
            f"{name}_MidRing{i}", mid,
            mid_r * 1.28, mid_r * 0.55, rot=mid_rot, v=12)
        set_mat(mid_ring, base_mat)
        parts.append(mid_ring)
        # Clean tapered terminus only — no tip orb / pad stack

    # Thumb — rest natural alongside index (tucked); less extreme opposition stretch
    thumb_start = hand + Vector((sx * 0.024, 0.002, 0.016))
    thumb_end = hand + Vector((sx * 0.048, -0.022, -0.032))
    thenar = sph(f"{name}_Thenar",
                 hand + Vector((sx * 0.026, 0.004, 0.010)),
                 (0.018, 0.014, 0.020), seg=10, ring=5)
    set_mat(thenar, base_mat)
    parts.append(thenar)
    thumb = tapered_limb(
        f"{name}_Thumb",
        thumb_start,
        thumb_end,
        0.014, 0.007, v=12, caps=False)
    set_mat(thumb, base_mat)
    parts.append(thumb)
    tdir = (thumb_end - thumb_start)
    tlen = tdir.length
    td = tdir.normalized() if tlen > 1e-6 else Vector((sx, 0, -1))
    t_knuckle = sph(
        f"{name}_ThumbKnuckle",
        thumb_start + td * (tlen * 0.12),
        0.012, seg=10, ring=5)
    set_mat(t_knuckle, base_mat)
    parts.append(t_knuckle)
    t_mid = sph(
        f"{name}_ThumbMid",
        thumb_start + td * (tlen * 0.48),
        0.0095, seg=8, ring=4)
    set_mat(t_mid, base_mat)
    parts.append(t_mid)
    return join(name, parts)


def shoe_foot(name, an, toe, sx, base_mat, joint_mat, rubber_mat=None):
    """Heel/toe shoe pads with small Bionicle ankle joint. Sole/pads use rubber material."""
    rub = rubber_mat or base_mat
    parts = []
    anj = bionicle_joint(f"{name}_AnkleJ", an, axis="X", size=0.017)
    set_mat(anj, joint_mat)
    parts.append(anj)
    an_cover = joint_skin_cover(f"{name}_AnkleCover", an, axis="X", size=0.017)
    set_mat(an_cover, base_mat)
    parts.append(an_cover)
    heel = sph(f"{name}_Heel",
               an + Vector((0, 0.038, -0.028)),
               (0.042, 0.040, 0.036), seg=14, ring=7)
    set_mat(heel, rub)
    parts.append(heel)
    mid = sph(f"{name}_Mid",
              an + Vector((0, -0.042, -0.030)),
              (0.048, 0.078, 0.028), seg=16, ring=8)
    set_mat(mid, base_mat)  # upper shoe shell stays vinyl
    parts.append(mid)
    toe_pad = sph(f"{name}_Toe",
                  an + Vector((0, -0.130, -0.018)),
                  (0.036, 0.044, 0.022), seg=12, ring=6)
    set_mat(toe_pad, rub)
    parts.append(toe_pad)
    sole = sph(f"{name}_Sole",
               an + Vector((0, -0.045, -0.050)),
               (0.044, 0.098, 0.011), seg=14, ring=6)
    set_mat(sole, rub)
    parts.append(sole)
    instep = sph(f"{name}_Instep",
                 an + Vector((0, -0.072, -0.008)),
                 (0.036, 0.042, 0.024), seg=12, ring=6)
    set_mat(instep, base_mat)
    parts.append(instep)
    return join(name, parts)


def flat_chest_plate(name):
    """Hybrid III FLAT chest plate — narrower girdle (v0.7.8).

    Hard planar volumes matching PRIMARY Hybrid III ref: flat front slab,
    clear V-taper to accordion. NO pec/lat/delt sphere pillows.
    Shelves track SHOULDER_X/Z/Y (down+back, notably narrower biacromial).
    Clear bottom edge ABOVE accordion. KEEP hard shells — just less wide girdle.
    """
    parts = []
    # Front planar plate — narrowed with shoulder girdle (was 0.148)
    front = cube(
        f"{name}_Front", (0.0, -0.082, 1.285),
        (0.125, 0.026, 0.145), bevel=0.010)
    parts.append(front)
    # Upper clavicle flare — narrowed half-width (was 0.175)
    clav = cube(
        f"{name}_Clav", (0.0, -0.072, 1.355),
        (0.145, 0.030, 0.040), bevel=0.009)
    parts.append(clav)
    # Lower taper slab ending clean ABOVE bellows
    lower = cube(
        f"{name}_Lower", (0.0, -0.068, 1.155),
        (0.105, 0.024, 0.044), bevel=0.007)
    parts.append(lower)
    # Side walls — hard wrap, taper inward toward waist (not soft lats)
    for sx in (1, -1):
        side = cube(
            f"{name}_Side{sx}", (sx * 0.125, -0.004, 1.280),
            (0.024, 0.078, 0.130), bevel=0.007)
        parts.append(side)
        side_lo = cube(
            f"{name}_SideLo{sx}", (sx * 0.108, -0.002, 1.160),
            (0.020, 0.066, 0.038), bevel=0.005)
        parts.append(side_lo)
    # Back plate — narrowed with front (was 0.138)
    back = cube(
        f"{name}_Back", (0.0, 0.078, 1.280),
        (0.115, 0.024, 0.135), bevel=0.009)
    parts.append(back)
    # Shoulder shelves — HARD stubs at SHOULDER_X (was hardcoded 0.245)
    for sx in (1, -1):
        shelf = cube(
            f"{name}_Shelf{sx}", (sx * SHOULDER_X, SHOULDER_Y, SHOULDER_Z - 0.055),
            (0.046, 0.048, 0.036), bevel=0.009)
        parts.append(shelf)
    # Fill core — hard plate mass, not hollow egg
    core = cube(
        f"{name}_Core", (0.0, 0.0, 1.280),
        (0.100, 0.058, 0.125), bevel=0.005)
    parts.append(core)
    # Sharp bottom edge bead (distinct shell terminus above accordion)
    bot_edge = cube(
        f"{name}_BotEdge", (0.0, -0.004, 1.118),
        (0.110, 0.085, 0.010), bevel=0.003)
    parts.append(bot_edge)

    shell = join(name, parts)
    apply_bevel(shell, width=0.005, segments=2)
    light_smooth(shell, iterations=2)
    return shell


def hard_pelvis_shell(name):
    """Hard Hybrid III pelvis — brief-shaped shell with hip cutouts (v0.7.7).

    Wider crash-dummy hip mass vs toy cylinder; clear top rim into accordion;
    U-shaped side cutouts for hip hinges. HARD shell — NOT balloon soft flesh.
    """
    parts = []
    # Main hard bowl — Hybrid III wider flatter front
    front = cube(
        f"{name}_Front", (0.0, -0.062, 0.970),
        (0.138, 0.028, 0.082), bevel=0.010)
    parts.append(front)
    back = cube(
        f"{name}_Back", (0.0, 0.070, 0.970),
        (0.128, 0.026, 0.076), bevel=0.010)
    parts.append(back)
    # Clear TOP rim into accordion (hard edge read)
    top_rim = cube(
        f"{name}_TopRim", (0.0, 0.0, 1.058),
        (0.140, 0.095, 0.014), bevel=0.004)
    parts.append(top_rim)
    # Side wings — brief cutouts + hard hip bowl (match wider HIP_X)
    for sx in (1, -1):
        wing = cube(
            f"{name}_Wing{sx}", (sx * 0.145, 0.006, 0.975),
            (0.042, 0.070, 0.072), bevel=0.010)
        parts.append(wing)
        # hard vinyl hip bowl — NOT soft flesh pillow
        hip_curve = sph(
            f"{name}_HipCurve{sx}",
            (sx * 0.158, 0.010, 0.955),
            (0.062, 0.062, 0.055), seg=16, ring=8)
        parts.append(hip_curve)
        # brief-cutout lip (hard edge around hip joint pocket)
        cut_lip = cube(
            f"{name}_CutLip{sx}", (sx * 0.168, 0.0, 0.990),
            (0.012, 0.055, 0.048), bevel=0.004)
        parts.append(cut_lip)
    # Lower crotch plate
    crotch = cube(
        f"{name}_Crotch", (0.0, 0.0, 0.895),
        (0.080, 0.058, 0.034), bevel=0.007)
    parts.append(crotch)
    # Fill core — hard plate mass
    core = cube(
        f"{name}_Core", (0.0, 0.006, 0.970),
        (0.105, 0.058, 0.062), bevel=0.005)
    parts.append(core)

    shell = join(name, parts)
    apply_bevel(shell, width=0.005, segments=2)
    light_smooth(shell, iterations=2)
    return shell


def limb_shell_with_lips(name, a, b, r0, r1, lip_mat=None, mid_bulge=0.0):
    """Upper or lower limb shell with soft bead lips — segmented hard-shell read.
    mid_bulge>0 adds athletic mid mass under hard shell (not soft flesh pillow).
    """
    a, b = Vector(a), Vector(b)
    direction = (b - a).normalized()
    length = (b - a).length
    if mid_bulge > 0 and length > 0.08:
        # Two-segment hard taper: proximal→mid (thick) → distal (slim)
        mid = a.lerp(b, 0.42)
        r_mid = max(r0, r1) * (1.0 + mid_bulge)
        body0 = tapered_limb(name + "_b0", a, mid, r0, r_mid, v=24)
        body1 = tapered_limb(name + "_b1", mid, b, r_mid * 0.96, r1, v=24)
        body = join(name + "_body", [body0, body1])
    else:
        body = tapered_limb(name + "_body", a, b, r0, r1, v=24)
    lip0_loc = a + direction * 0.018
    lip1_loc = b - direction * 0.018
    axis = "Z" if abs(direction.z) > 0.7 else "X"
    # Subtle seam beads — low flare so they don't read as mid-freq boil eggs
    lip0 = bead_lip(f"{name}_lip0", lip0_loc, r0 * 1.02, axis=axis, thick=0.010, flare=1.06)
    lip1 = bead_lip(f"{name}_lip1", lip1_loc, r1 * 1.02, axis=axis, thick=0.010, flare=1.06)
    shell = join(name, [body, lip0, lip1])
    light_smooth(shell, iterations=2)
    return shell


def arm_points(sx):
    # v0.7.6: shoulder DOWN (Z) + BACK (+Y); face is -Y
    sh = Vector((sx * SHOULDER_X, SHOULDER_Y, SHOULDER_Z))
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

    bone("Hips", "Root", (0, 0, HIP_Z - 0.04), (0, 0, HIP_Z + 0.06))
    bone("Spine", "Hips", (0, 0, HIP_Z + 0.06), (0, 0, 1.18))
    bone("Chest", "Spine", (0, 0, 1.18), (0, 0, SHOULDER_Z))
    bone("Neck", "Chest", (0, 0, SHOULDER_Z), (0, 0, 1.535))
    bone("Head", "Neck", (0, 0, 1.535), (0, 0, 1.80))

    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr, hand = arm_points(sx)
        bone(f"Shoulder_{side}", "Chest", (sx * 0.12, SHOULDER_Y * 0.5, SHOULDER_Z), sh)
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
    """v0.7.7: Hybrid III body volumes + tiny joints under SOLID vinyl cover; knuckles on hands."""
    (base, accent, over, joint, sensor, metal, bellows_mat, cal_accent, lip_mat,
     rubber, wear) = mats
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

    # --- Head: molded Hybrid III vinyl face — CORRECT human head scale (v0.7.4) ---
    # Face toward -Y. Brow/nose/mouth/chin = relief IN shell. Eyes/temples = flush pits.
    # Smaller than v0.7.3 toy egg — ~1/7.5 body, still hard-shell (not soft flesh).
    head = sph("Head", (0, -0.008, 1.665), (0.125, 0.114, 0.150), seg=40, ring=20, sub=True)

    # Brow — continuous low-relief ridge
    brow = sph("BrowRidge", (0.0, -0.108, 1.730), (0.088, 0.012, 0.009), seg=28, ring=12)

    # Nose — Hybrid III wedge: narrower/taller tip so profile breaks egg clearly.
    nose_bridge = cube(
        "NoseBridge", (0.0, -0.125, 1.685),
        (0.008, 0.036, 0.034), bevel=0.0018)
    bpy.ops.object.select_all(action="DESELECT")
    nose_bridge.select_set(True)
    bpy.context.view_layer.objects.active = nose_bridge
    nose_bridge.scale = (0.48, 1.05, 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    nose_tip = cone(
        "NoseTip", (0.0, -0.148, 1.655),
        r1=0.009, r2=0.0012, depth=0.042,
        rot=(math.radians(90), 0, 0), v=18)
    bpy.ops.object.select_all(action="DESELECT")
    nose_tip.select_set(True)
    bpy.context.view_layer.objects.active = nose_tip
    nose_tip.scale = (0.52, 1.08, 1.55)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    # Mild chin / jaw break — hard shell, no cheek orbs
    chin = sph("Chin", (0.0, -0.092, 1.555), (0.030, 0.020, 0.016), seg=12, ring=8)
    jaw_l = sph("JawL", (-0.052, -0.068, 1.600), (0.028, 0.022, 0.022), seg=10, ring=6)
    jaw_r = sph("JawR", (0.052, -0.068, 1.600), (0.028, 0.022, 0.022), seg=10, ring=6)

    head = boolean_union(head, [brow, nose_bridge, nose_tip, chin, jaw_l, jaw_r])
    light_smooth(head, iterations=1)
    add("Head", head, base)

    # Eyes — deep oval recesses + FLAT dark oval PLATES
    recess_tools = []
    for dx in (-0.042, 0.042):
        recess_tools.append(
            sph(f"EyeRecess_{dx}", (dx, -0.118, 1.672), (0.028, 0.024, 0.022), seg=16, ring=10)
        )
    boolean_difference(head, recess_tools)
    for dx in (-0.042, 0.042):
        e = cube(
            f"Eye_{dx}", (dx, -0.098, 1.670),
            (0.022, 0.0006, 0.013), bevel=0.0012)
        add("Head", e, sensor)

    # Mouth — shallow horizontal SLIT
    mouth_carve = cube(
        "MouthCarve", (0.0, -0.114, 1.605), (0.040, 0.028, 0.0030), bevel=0.0005)
    boolean_difference(head, [mouth_carve])
    mouth = cube(
        "MouthSlit", (0.0, -0.110, 1.605), (0.036, 0.0006, 0.0016), bevel=0.0002)
    add("Head", mouth, sensor)

    # Temple row of 3 — shallow PIT carve + flat dark disk
    temple_pits = []
    for i, z in enumerate([1.710, 1.670, 1.630]):
        temple_pits.append(
            cyl(f"TemplePit_{i}", (0.122, -0.038, z), 0.010, 0.018,
                rot=(0, math.radians(90), 0), v=16)
        )
    boolean_difference(head, temple_pits)
    for i, z in enumerate([1.710, 1.670, 1.630]):
        t = cyl(
            f"Temple_{i}", (0.110, -0.038, z), 0.008, 0.0014,
            rot=(0, math.radians(90), 0), v=16)
        add("Head", t, sensor)

    # Temple cal — flat decal disk (scaled to smaller head)
    cal_t = cal_quadrant_disk(
        "CalTemple", (-0.126, -0.014, 1.680),
        radius=0.038, thick=0.006,
        accent_mat=cal_accent, black_mat=sensor, axis="X")
    groups["Head"].append(cal_t)

    # --- Neck: 4 stacked dark metal rings — MUST read front + profile ---
    neck = neck_ring_stack("NeckRings", z_base=1.480, n=4, major=0.062, minor=0.013, spacing=0.022)
    add("Neck", neck, metal)

    # --- DISTINCT CHEST PLATE (segmented, soft bottom lip) ---
    chest = flat_chest_plate("ChestShell")
    add("Chest", chest, base)

    # Shoulder plate seams (dark bead at delt roots) — v0.7.6 DOWN + BACK
    for side, sx in (("L", 1), ("R", -1)):
        seam = bead_lip(
            f"ShoulderSeam_{side}",
            (sx * SHOULDER_X, SHOULDER_Y, SHOULDER_Z - 0.040),
            radius=0.046, thick=0.010, axis="X", flare=1.10)
        add("Chest", seam, joint)

    # LARGE chest cal
    cal_c = cal_quadrant_disk(
        "CalChest", (0.072, -0.112, 1.350),
        radius=0.040, thick=0.008,
        accent_mat=cal_accent, black_mat=sensor, axis="Y")
    groups["Chest"].append(cal_c)

    # Thin teal tick ONLY on Runner — NOT fat racing stripe
    if not is_it:
        tick = cube("ChestTick", (0.0, -0.112, 1.195), (0.028, 0.0035, 0.0035), bevel=0.001)
        add("Chest", tick, accent)

    # Soft DARK bead lips at shell termini (Hybrid III seam — not tan faux-ribs)
    chest_lip = bead_lip("ChestBotLip", (0, 0.01, 1.118), radius=0.110, axis="Z",
                         thick=0.012, flare=1.08)
    add("Chest", chest_lip, joint)

    # --- WAIST BELLOWS INSET (~8 fine dark ribs) — narrower human waist ---
    bellows = waist_bellows("WaistBellows", z_top=1.115, z_bot=1.065, radius=0.092, n_ribs=8)
    add("Spine", bellows, bellows_mat)

    # --- DISTINCT PELVIS SHELL ---
    pelvis = hard_pelvis_shell("PelvisShell")
    add("Hips", pelvis, base)
    pelvis_lip = bead_lip("PelvisTopLip", (0, 0.01, 1.068), radius=0.135, axis="Z",
                          thick=0.012, flare=1.08)
    add("Hips", pelvis_lip, joint)

    # It nested black Vs on flat chest plate + outer thighs — ZERO on Tan/Runner
    if is_it:
        for i, z in enumerate([1.400, 1.340, 1.280]):
            half = 0.082 - i * 0.010
            bars = chevron_v(f"ChC{i}", 0.0, -0.112, z, half, bar_len=half * 1.18,
                             thick=0.012, depth=0.014, ang_deg=34.0)
            for b in bars:
                add("Chest", b, accent)

    # --- Arms: DISTINCT upper/lower shells + smaller Bionicle joints + vinyl cover ---
    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr, hand = arm_points(sx)

        # v0.7.6b: shoulder 0.022 + FULL vinyl envelope cover
        shj = bionicle_joint(f"ShoulderJ_{side}", sh, axis="X", size=0.022)
        add(f"Shoulder_{side}", shj, joint)
        sh_cover = joint_skin_cover(f"ShoulderCover_{side}", sh, axis="X", size=0.022)
        add(f"Shoulder_{side}", sh_cover, base)

        # Upper arm LONG hard shell — athletic taper (deltoid mass → elbow), not equal tube
        ua_end = el + (sh - el).normalized() * 0.038
        ua = limb_shell_with_lips(f"UA_{side}", sh + (el - sh).normalized() * 0.048,
                                  ua_end, 0.072, 0.038)
        add(f"UpperArm_{side}", ua, base)

        # v0.7.6b: elbow 0.017 + FULL vinyl envelope cover
        elj = bionicle_joint(f"ElbowJ_{side}", el, axis="X", size=0.017)
        add(f"LowerArm_{side}", elj, joint)
        el_cover = joint_skin_cover(f"ElbowCover_{side}", el, axis="X", size=0.017)
        add(f"LowerArm_{side}", el_cover, base)

        la_start = el + (wr - el).normalized() * 0.038
        la_end = wr + (el - wr).normalized() * 0.028
        # Forearm taper: thicker near elbow → slim wrist
        la = limb_shell_with_lips(f"LA_{side}", la_start, la_end, 0.044, 0.026)
        add(f"LowerArm_{side}", la, base)

        h = hybrid_hand(f"Hand_{side}", wr, hand, sx, base, joint)
        groups[f"Hand_{side}"].append(h)

    # --- Legs: DISTINCT thigh/shin shells + small Bionicle joints + U-knee ---
    for side, sx in (("L", 1), ("R", -1)):
        hip, kn, an, toe = leg_points(sx)

        # v0.7.6b: hip 0.023 + FULL vinyl envelope cover
        hipj = bionicle_joint(f"HipJ_{side}", hip, axis="X", size=0.023)
        add(f"UpperLeg_{side}", hipj, joint)
        hip_cover = joint_skin_cover(f"HipCover_{side}", hip, axis="X", size=0.023)
        add(f"UpperLeg_{side}", hip_cover, base)

        # LONG hard thigh shell — athletic mass (thick proximal → taper to knee)
        thigh_start = hip + Vector((0, 0, -0.048))
        thigh_end = kn + Vector((0, 0, 0.045))
        thigh = limb_shell_with_lips(f"Thigh_{side}", thigh_start, thigh_end, 0.092, 0.048)
        add(f"UpperLeg_{side}", thigh, base)

        seam_th = bead_lip(
            f"ThighSeam_{side}",
            hip + Vector((0, 0, -0.048)),
            radius=0.098, thick=0.012, axis="Z", flare=1.10)
        add(f"UpperLeg_{side}", seam_th, joint)

        fork = u_knee_fork(f"KneeFork_{side}", kn, sx)
        add(f"UpperLeg_{side}", fork, base)

        if is_it:
            for i, tt in enumerate((0.30, 0.48, 0.66)):
                p = hip.lerp(kn, tt)
                half = 0.045 - i * 0.005
                cx = p.x + sx * 0.038
                cy = p.y - 0.082
                bars = chevron_v(f"ChT{side}{i}", cx, cy, p.z, half, bar_len=half * 1.12,
                                 thick=0.010, depth=0.012, ang_deg=34.0)
                for b in bars:
                    add(f"UpperLeg_{side}", b, accent)

        # v0.7.6b: knee 0.019 + FULL vinyl envelope cover
        knj = bionicle_joint(f"KneeJ_{side}", kn, axis="X", size=0.019)
        add(f"LowerLeg_{side}", knj, joint)
        kn_cover = joint_skin_cover(f"KneeCover_{side}", kn, axis="X", size=0.019)
        add(f"LowerLeg_{side}", kn_cover, base)

        nest = sph(f"KneeNest_{side}", kn + Vector((0, 0, 0.008)), 0.028, seg=14, ring=7)
        add(f"LowerLeg_{side}", nest, joint)

        # LONG hard shin — mild calf mass then taper to ankle (hard shell, not soft pillow)
        shin_start = kn + Vector((0, 0, -0.042))
        shin = limb_shell_with_lips(f"Shin_{side}", shin_start, an + Vector((0, 0, 0.032)),
                                    0.054, 0.028)
        add(f"LowerLeg_{side}", shin, base)

        ft = shoe_foot(f"Foot_{side}", an, toe, sx, base, joint, rubber_mat=rubber)
        groups[f"Foot_{side}"].append(ft)

    # --- Micro wear: light stamp-ink dirt in seam recesses only (no gore) ---
    wear_spots = [
        ("Chest", (0.0, -0.130, 1.115), 0.012),
        ("Chest", (0.095, -0.118, 1.260), 0.009),
        ("Chest", (-0.095, -0.118, 1.260), 0.009),
        ("Hips", (0.0, -0.105, 1.040), 0.011),
        ("Hips", (0.090, -0.090, 0.960), 0.008),
        ("Spine", (0.070, -0.080, 1.070), 0.007),
        ("Spine", (-0.070, -0.080, 1.070), 0.007),
    ]
    for i, (bone, loc, r) in enumerate(wear_spots):
        d = cyl(f"WearDisk_{i}", loc, r, 0.0035, rot=(math.radians(90), 0, 0), v=12)
        add(bone, d, wear)
    # Shallow dark cubes in hinge recess pockets (elbow/knee vicinity)
    for side, sx in (("L", 1), ("R", -1)):
        sh, el, wr, hand = arm_points(sx)
        sc = cube(f"WearElbow_{side}", el + Vector((sx * 0.02, -0.035, 0)),
                  (0.008, 0.004, 0.010), bevel=0.001)
        add(f"LowerArm_{side}", sc, wear)
        hip, kn, an, toe = leg_points(sx)
        sk = cube(f"WearKnee_{side}", kn + Vector((sx * 0.025, -0.040, 0)),
                  (0.010, 0.004, 0.012), bevel=0.001)
        add(f"LowerLeg_{side}", sk, wear)

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
    """Satin vinyl hard shells ≠ darker metal hinges ≠ rubber pads ≠ dark bellows.
    Paint LOCKED: Runner cream / It warm tan + black Vs. Slot names preserved."""
    suffix = "_It" if is_it else "_Tan"
    if is_it:
        base = mat(
            "Base", WARM_TAN, metallic=0.015, roughness=0.40, subsurface=0.12,
            ss_color=(0.90, 0.55, 0.35, 1.0), ss_radius=(1.0, 0.45, 0.20), emit=0.02)
    else:
        base = mat(
            "Base", BONE, metallic=0.012, roughness=0.38, subsurface=0.16,
            ss_color=(1.0, 0.80, 0.55, 1.0), ss_radius=(1.0, 0.50, 0.22), emit=0.04)
    accent = mat("Accent", BLACK if is_it else TEAL, roughness=0.44)
    over = mat("ItOverride", RIM if is_it else BONE, roughness=0.45, emit=0.0)
    # Metal hinges: metallic 0.65–0.85, roughness 0.28–0.38
    joint = mat("Joint" + suffix, JOINT, metallic=0.72, roughness=0.34)
    sensor = mat("Sensor" + suffix, SENSOR, metallic=0.0, roughness=0.68)
    metal = mat("Metal" + suffix, METAL, metallic=0.82, roughness=0.30)
    # Bellows: very dark, low metal, mid-high roughness
    bellows_mat = mat("Bellows" + suffix, DARK_BELLOWS, metallic=0.04, roughness=0.72)
    cal_col = TEAL if not is_it else CAL_YELLOW
    cal_accent = mat("CalAccent" + suffix, cal_col, roughness=0.40)
    lip_mat = mat("Lip" + suffix, LIP, metallic=0.25, roughness=0.42)
    # Rubber shoe pads: high roughness, non-metal
    RUBBER_COL = (0.08, 0.08, 0.09, 1.0)
    rubber = mat("Rubber" + suffix, RUBBER_COL, metallic=0.0, roughness=0.80)
    # Micro-wear dirt (stamp-ink in recesses)
    wear = mat("Wear" + suffix, (0.05, 0.045, 0.04, 1.0), metallic=0.0, roughness=0.88)
    base.name = "Base"
    accent.name = "Accent"
    over.name = "ItOverride"
    return (base, accent, over, joint, sensor, metal, bellows_mat, cal_accent, lip_mat,
            rubber, wear)


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
    meshOptimizationFlags: 0
    autoGenerateAvatarMappingIfUnspecified: 1
    animationType: 2
    humanoidOversampling: 1
    avatarSetup: 0
    addHumanoidExtraBoneInCheck: 0
    additionalBone: 0
  tangentSpace:
    normalSmoothAngle: 60
    normalImportMode: 0
    tangentImportMode: 3
    normalCalculationMode: 4
    legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes: 0
    blendShapeNormalImportMode: 1
    normalSmoothingSource: 0
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



def ensure_meta_guid(fbx_path, guid):
    """Keep existing .meta if present (preserve Unity importer + GUID); else write minimal.
    Do NOT rewrite .meta content when GUID already matches.
    """
    meta = fbx_path + ".meta"
    if os.path.isfile(meta):
        with open(meta, "r") as f:
            text = f.read()
        if f"guid: {guid}" in text:
            log(f"Meta GUID intact {meta} guid={guid}")
            return
        import re
        text2 = re.sub(r"guid: [0-9a-f]+", f"guid: {guid}", text, count=1)
        with open(meta, "w") as f:
            f.write(text2)
        log(f"Meta GUID restored {meta} guid={guid}")
        return
    write_meta(fbx_path, guid=guid)


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


def hand_world(arm_ob, side="L"):
    """World-space tip of Hand_L/R for close still framing."""
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


def composite_vs_ref(idle_path, out_path):
    try:
        from PIL import Image, ImageDraw, ImageFont
    except ImportError:
        # Blender's embedded Python often lacks Pillow — shell out to system python3
        import subprocess, shutil
        helper = (
            "from PIL import Image, ImageDraw, ImageFont\n"
            "import sys\n"
            "idle_path, out_path, ref = sys.argv[1:4]\n"
            "idle = Image.open(idle_path).convert('RGBA')\n"
            "ref = Image.open(ref).convert('RGBA')\n"
            "w, h = ref.size\n"
            "\n"
            "ref = ref.crop((0, 0, w // 2, h)) if w > h * 1.2 else ref\n"
            "th = 1100\n"
            "fit = lambda im: im.resize((max(1, int(im.width * th / im.height)), th), Image.Resampling.LANCZOS)\n"
            "a, b = fit(idle), fit(ref)\n"
            "gap = 24\n"
            "c = Image.new('RGBA', (a.width + gap + b.width + 40, th + 60), (48, 48, 52, 255))\n"
            "c.paste(a, (20, 40), a); c.paste(b, (20 + a.width + gap, 40), b)\n"
            "d = ImageDraw.Draw(c)\n"
            "try:\n"
            " f = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 22)\n"
            "except Exception:\n"
            " f = ImageFont.load_default()\n"
            "d.text((20, 10), 'v0.6.1 Tan idle (Hybrid III hands)', fill=(220, 220, 220, 255), font=f)\n"
            "d.text((20 + a.width + gap, 10), 'Hybrid III ref (PRIMARY)', fill=(220, 220, 220, 255), font=f)\n"
            "c.convert('RGB').save(out_path)\n"
        )
        try:
            r = subprocess.run(
                ["python3", "-c", helper, idle_path, out_path, REF_CRASH],
                capture_output=True, text=True, timeout=60)
            if r.returncode == 0 and os.path.isfile(out_path):
                log(f"Still {out_path} (vs ref composite via system PIL)")
                return
            log(f"system PIL fail: {r.stderr[:200]} — copying idle as vs_ref fallback")
        except Exception as e:
            log(f"PIL missing ({e}) — copying idle as vs_ref fallback")
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
    draw.text((20, 10), "v0.6.1 Tan idle (Hybrid III hands)", fill=(220, 220, 220, 255), font=font)
    draw.text((20 + idle_f.width + gap, 10), "Hybrid III ref (PRIMARY)", fill=(220, 220, 220, 255), font=font)
    canvas.convert("RGB").save(out_path)
    log(f"Still {out_path} (vs ref composite)")


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
    ]
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
        render_shot(f"{PREV}/hipoly_v78_{tag}idle_front.png",
                    (0.10, -3.4, 1.28), (0, 0, 0.98))
        render_shot(f"{PREV}/hipoly_v78_{tag}idle_34.png",
                    (2.2, -2.6, 1.32), (0, 0, 1.00))
        hl = hand_world(arm_ob, "L")
        render_shot(
            f"{PREV}/hipoly_v78_{tag}hand_close.png",
            (hl.x + 0.32, hl.y - 0.38, hl.z + 0.14),
            (hl.x - 0.02, hl.y + 0.02, hl.z - 0.01),
        )
        render_shot(
            f"{PREV}/hipoly_v78_{tag}face_close.png",
            (0.06, -0.46, 1.665),
            (0.0, -0.04, 1.650),
        )
        # Limb/joint close -- L shoulder sells smaller joint + vinyl skin cover
        sh = arm_ob.pose.bones.get("Shoulder_L")
        if sh is not None:
            sw = arm_ob.matrix_world @ sh.head
            render_shot(
                f"{PREV}/hipoly_v78_{tag}joint_close.png",
                (sw.x + 0.42, sw.y - 0.55, sw.z + 0.08),
                (sw.x, sw.y, sw.z),
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

DummyLocomotor-bindable **Hybrid III hard-shell** crash-test dummies (v0.7.8).
Narrower shoulders; soft resting-fist finger curl; tiny Bionicle joints under SOLID vinyl capsule; knuckles + mid-finger mesh; shoulders down/back.

## Assets
| File | Paint |
|------|-------|
| `Dummy_Mannequin_Tan_Hier_Hi.fbx` | Runner — cream `#E8D9C0`, Accent teal tick. **ZERO nested Vs.** |
| `Dummy_Mannequin_Orange_Hier_Hi.fbx` | It — warm tan body + black nested Vs chest + outer thighs (NOT #FF6A00) |

## Bind pose
- Mild A-pose ~20–35°; hands clear pelvis.
- Human head scale + molded face; flat dark eye insets — zero orbs / tip stacks.
- Flat chest plate (narrower); dense accordion bellows; hard pelvis w/ mild hip curve.
- Hybrid III limb shell mass; tiny dark Bionicle joints under SOLID vinyl capsule; hard-shell hands w/ soft resting-fist curl + knuckles/mid joints (no Finger_ bones).
- Materials: satin vinyl Base / Accent / ItOverride + Joint metal + Rubber + Bellows.

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
    log("=== hipoly hier v8.8 / Hier v0.7.8 resting-fist + shoulders in ===")
    tan = os.path.join(OUT_DIR, "Dummy_Mannequin_Tan_Hier_Hi.fbx")
    orn = os.path.join(OUT_DIR, "Dummy_Mannequin_Orange_Hier_Hi.fbx")

    log("Building Tan/Runner Hier HiPoly v0.7.8 resting-fist + shoulders in…")
    ok_t, ang_t, cx_t, cy_t = build_variant(False, tan, GUID_TAN, do_stills=True)

    log("Building It/Orange Hier HiPoly v0.7.8 resting-fist + shoulders in…")
    ok_o, ang_o, cx_o, cy_o = build_variant(True, orn, GUID_ORANGE, do_stills=True)

    write_readme()
    log(f"Tan OK={ok_t} A-pose={ang_t:.1f}deg clear_x={cx_t:.3f} clear_y={cy_t:.3f}")
    log(f"It  OK={ok_o} A-pose={ang_o:.1f}deg clear_x={cx_o:.3f} clear_y={cy_o:.3f}")
    log(f"Tan FBX {os.path.getsize(tan)} bytes guid={GUID_TAN}")
    log(f"Orange FBX {os.path.getsize(orn)} bytes guid={GUID_ORANGE}")
    log("DONE v0.7.8 resting-fist + shoulders in — no git push (await Art / Ororo; Eng HOLD)")


if __name__ == "__main__":
    main()
