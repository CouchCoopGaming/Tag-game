"""Hier ground-locomotion stills.

Visual cycles only. Speeds stay at the game values: walk 6.9, run 10.35,
sprint 13.8, crouch 3.68. No root-motion clip and no gameplay timer.

A stance sole is solved in world space with the bone step capped, then the
swing returns on a smooth curve. Contact sheets are 4x2, eight samples across
each cycle. The pop table is the same motion at 30 fps.
"""
import math
import os

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Euler, Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.environ.get(
    "LOCO_FBX",
    os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx"),
)
PASS = os.environ.get("LOCO_PASS", "pass1")
OUT = os.environ.get("LOCO_OUT", os.path.join(ROOT, "Docs", "LocoStills", PASS))
ONLY = set(filter(None, os.environ.get("LOCO_ONLY", "").split(",")))
DO_RENDER = os.environ.get("LOCO_RENDER", "1") != "0"

WALK, RUN, SPRINT, CROUCH = 6.9, 10.35, 13.8, 3.68
DT = 1.0 / 30.0
CELL_W, CELL_H = 400, 540
FONT = "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf"

PLAYER = (0.235, 0.557, 0.847, 1.0)
ACCENT = (0.45, 0.82, 0.86, 1.0)
JOINT = (0.16, 0.17, 0.18, 1.0)
GROUND_C = (0.30, 0.34, 0.30, 1.0)
SKY = (0.55, 0.66, 0.78, 1.0)

MESHES = [
    "Mesh_Hips", "Mesh_Spine", "Mesh_Chest", "Mesh_Neck", "Mesh_Head",
    "Mesh_Shoulder_L", "Mesh_UpperArm_L", "Mesh_LowerArm_L", "Mesh_Hand_L",
    "Mesh_Shoulder_R", "Mesh_UpperArm_R", "Mesh_LowerArm_R", "Mesh_Hand_R",
    "Mesh_UpperLeg_L", "Mesh_LowerLeg_L", "Mesh_Foot_L",
    "Mesh_UpperLeg_R", "Mesh_LowerLeg_R", "Mesh_Foot_R",
]
BONE_OF = {m: m[5:] for m in MESHES}
# Candidate hinge spheres. Absent on the shipped mannequin, ignored unless the object exists.
CAP_BONES = {
    "Mesh_ElbowCap_L": "LowerArm_L",
    "Mesh_ElbowCap_R": "LowerArm_R",
    "Mesh_KneeCap_L": "LowerLeg_L",
    "Mesh_KneeCap_R": "LowerLeg_R",
}
PARENT = {
    "Spine": "Hips", "Chest": "Spine", "Neck": "Chest", "Head": "Neck",
    "Shoulder_L": "Chest", "UpperArm_L": "Shoulder_L", "LowerArm_L": "UpperArm_L", "Hand_L": "LowerArm_L",
    "Shoulder_R": "Chest", "UpperArm_R": "Shoulder_R", "LowerArm_R": "UpperArm_R", "Hand_R": "LowerArm_R",
    "UpperLeg_L": "Hips", "LowerLeg_L": "UpperLeg_L", "Foot_L": "LowerLeg_L",
    "UpperLeg_R": "Hips", "LowerLeg_R": "UpperLeg_R", "Foot_R": "LowerLeg_R",
}


def rad(d):
    return math.radians(d)


def clamp(v, lo, hi):
    return lo if v < lo else hi if v > hi else v


def lerp(a, b, u):
    return a + (b - a) * u


def smooth(u):
    u = clamp(u, 0.0, 1.0)
    return u * u * (3.0 - 2.0 * u)


def clear_pose(arm):
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")


def set_e(arm, name, x, y, z):
    bone = arm.pose.bones[name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = Euler((rad(x), rad(y), rad(z)), "XYZ")


def world_verts(name):
    deps = bpy.context.evaluated_depsgraph_get()
    obj = bpy.data.objects[name]
    ev = obj.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    verts = tuple(mw @ v.co for v in me.vertices)
    polys = tuple(tuple(p.vertices) for p in me.polygons)
    ev.to_mesh_clear()
    return verts, polys


def sole_of(name):
    verts, _ = world_verts(name)
    zmin = min(v.z for v in verts)
    band = [v for v in verts if v.z <= zmin + 0.008]
    acc = Vector((0.0, 0.0, 0.0))
    for v in band:
        acc += v
    return acc / len(band), zmin


def forefoot_anchor(name):
    """A fixed patch on the forefoot, in the mesh's own space, so the pivot cannot hop."""
    obj = bpy.data.objects[name]
    mw = obj.matrix_world
    coords = [v.co for v in obj.data.vertices]
    ys = [c.y for c in coords]
    zs = [c.z for c in coords]
    y_cut = min(ys) + 0.32 * (max(ys) - min(ys))
    z_cut = min(zs) + 0.28 * (max(zs) - min(zs))
    picked = [mw @ c for c in coords if c.y <= y_cut and c.z <= z_cut]
    if len(picked) < 3:
        picked = [mw @ c for c in coords if c.y <= y_cut]
    acc = Vector((0.0, 0.0, 0.0))
    for point in picked:
        acc += point
    return acc / len(picked), min(point.z for point in picked)


def ball_of(name):
    """Forward half of the ground band. The heel-to-toe plant pivots here."""
    verts, _ = world_verts(name)
    zmin = min(v.z for v in verts)
    band = [v for v in verts if v.z <= zmin + 0.012]
    if len(band) >= 6:
        ys = [v.y for v in band]
        mid = (min(ys) + max(ys)) * 0.5
        front = [v for v in band if v.y <= mid]
        if len(front) >= 3:
            band = front
    acc = Vector((0.0, 0.0, 0.0))
    for v in band:
        acc += v
    return acc / len(band), zmin


# Walk, run, and sprint set these so the knees stay on separate tracks.
# Other clips leave the turnout the bind pose uses.
ABDUCT = {"L": 0.0, "R": 0.0}
# Extra thigh-Y on top of the gait abduct, so a planted shoe can stay on its rail.
ABD_OFFSET = {"L": 0.0, "R": 0.0}
USE_TURNOUT = True
USE_BALL = False


def reset_leg_style():
    global USE_TURNOUT
    USE_TURNOUT = True
    ABDUCT["L"] = 0.0
    ABDUCT["R"] = 0.0
    ABD_OFFSET["L"] = 0.0
    ABD_OFFSET["R"] = 0.0


def apply_leg(arm, side, thigh, knee, foot, yaw=0.0, abduct=None):
    abd = ABDUCT[side] if abduct is None else abduct
    abd += ABD_OFFSET[side]
    # Positive abduct opens the knee away from the midline (left thigh -Y).
    y = -abd if side == "L" else abd
    base = (-5.0 if side == "L" else 5.0) if USE_TURNOUT else 0.0
    set_e(arm, "UpperLeg_" + side, thigh, y, base + yaw)
    set_e(arm, "LowerLeg_" + side, knee, 0.0, 0.0)
    set_e(arm, "Foot_" + side, foot, 0.0, 0.0)


def chain_step(current, goal, budget=22.0, foot_cap=8.0):
    """Step toward goal. Thigh, knee, and foot share one sagittal budget.

    The foot bone inherits the thigh and the knee, so those three local
    X deltas are capped as a sum. That is what keeps the world step under 25°.
    """
    d = [goal[i] - current[i] for i in range(3)]
    scale = 1.0
    limits = (
        abs(d[0]),
        abs(d[0] + d[1]),
        abs(d[0] + d[1] + d[2]),
    )
    for mag in limits:
        if mag > budget:
            scale = min(scale, budget / mag)
    if abs(d[2]) > foot_cap:
        scale = min(scale, foot_cap / abs(d[2]))
    return tuple(current[i] + d[i] * scale for i in range(3))


def build_swing(release, keys, count, budget, foot_cap):
    goals = []
    prev = 0
    for end, pose in keys:
        for _ in range(end - prev):
            goals.append(pose)
        prev = end
    if len(goals) != count:
        raise RuntimeError("swing keys cover {0} frames, cycle wants {1}".format(len(goals), count))
    cursor = (release[0], release[1], release[2])
    out = []
    for goal in goals:
        cursor = chain_step(cursor, goal, budget, foot_cap)
        out.append(cursor)
    return out


def clear_ground(arm, side, curve, floor=0.012):
    """Lift a swinging sole. The extra bend is capped so one frame cannot pop."""
    thigh, knee, foot = curve
    # Swing knees already pass 78° on a sprint. The extra bend is still capped at 12°.
    knee_lim = min(130.0, knee + 12.0)
    foot_lim = min(16.0, foot + 6.0)
    thigh_lim = min(28.0, thigh + 8.0)
    for _ in range(5):
        apply_leg(arm, side, thigh, knee, foot)
        bpy.context.view_layer.update()
        _c, zmin = sole_of("Mesh_Foot_" + side)
        if zmin >= floor:
            break
        if knee < knee_lim:
            knee = min(knee_lim, knee + 4.0)
        elif thigh < thigh_lim:
            thigh = min(thigh_lim, thigh + 3.0)
        if zmin < 0.0 and foot < foot_lim:
            foot = min(foot_lim, foot + 2.0)
    return (thigh, knee, foot)


def step_toward(current, goal, caps):
    """Move each joint toward goal without jumping more than caps degrees."""
    out = []
    for i in range(3):
        out.append(current[i] + clamp(goal[i] - current[i], -caps[i], caps[i]))
    return (out[0], out[1], out[2])


def solve_leg(arm, side, target, seed, cap=22.0, foot_cap=8.0):
    """Plant the sole on target. The step from seed stays inside the pop cap."""
    names_mesh = "Mesh_Foot_" + side
    best = tuple(seed)
    caps = (cap, cap, foot_cap)

    def eval_loss(trial):
        apply_leg(arm, side, trial[0], trial[1], trial[2])
        bpy.context.view_layer.update()
        contact, zmin = sole_of(names_mesh)
        dx = contact.x - target.x
        dy = contact.y - target.y
        dz = zmin - target.z
        return dx * dx * 8.0 + dy * dy * 14.0 + dz * dz * 30.0, contact, zmin

    bpy.context.view_layer.update()
    loss, _c, _z = eval_loss(best)
    cur = list(best)
    for step in (6.0, 3.0, 1.5, 0.75):
        for _ in range(4):
            improved = False
            for axis in range(3):
                for sign in (-1.0, 1.0):
                    trial = cur[:]
                    trial[axis] += sign * step
                    for a in range(3):
                        trial[a] = clamp(trial[a], seed[a] - caps[a], seed[a] + caps[a])
                    trial[0] = clamp(trial[0], -48.0, 46.0)
                    trial[1] = clamp(trial[1], 8.0, 78.0)
                    trial[2] = clamp(trial[2], -24.0, 18.0)
                    trial_l, _c, _z = eval_loss(trial)
                    if trial_l + 1e-7 < loss:
                        loss = trial_l
                        best = (trial[0], trial[1], trial[2])
                        cur = list(best)
                        improved = True
            if not improved:
                break
    apply_leg(arm, side, best[0], best[1], best[2])
    bpy.context.view_layer.update()
    return best


def solve_roll(arm, side, target, seed, foot, cap=16.0, lateral=False, chain=22.0, abd_step=6.0, abd_abs=12.0):
    """Plant the ball. The foot angle is the heel-to-toe roll, not a free joint.

    Walk also nudges thigh abduction. An abducted leg arcs sideways as it
    extends, and that arc is the planted slide. The nudge stays on Y so the
    sagittal step and the world bone step stay under 25°.
    """
    names_mesh = "Mesh_Foot_" + side
    best = (seed[0], seed[1], foot)
    best_off = ABD_OFFSET[side]
    thigh_lo, thigh_hi = (-48.0, 46.0)
    knee_hi = 110.0
    if chain > 40.0:
        # Stance may whip the leg to keep the sole down while the hips travel.
        thigh_lo, thigh_hi = (-80.0, 70.0)
        knee_hi = 140.0

    def eval_loss(trial, offset):
        ABD_OFFSET[side] = offset
        apply_leg(arm, side, trial[0], trial[1], trial[2])
        bpy.context.view_layer.update()
        contact, _patch_z = forefoot_anchor(names_mesh)
        _sole, sole_z = sole_of(names_mesh)
        dx = contact.x - target.x
        dy = contact.y - target.y
        dz = contact.z - target.z
        bite = max(0.0, 0.008 - sole_z)
        return dx * dx * 70.0 + dy * dy * 70.0 + dz * dz * 24.0 + bite * bite * 90.0

    def in_chain(trial):
        d0 = trial[0] - seed[0]
        d1 = trial[1] - seed[1]
        d2 = trial[2] - seed[2]
        return max(abs(d0), abs(d0 + d1), abs(d0 + d1 + d2)) <= chain

    loss = eval_loss(best, best_off)
    cur = [best[0], best[1]]
    cur_off = best_off
    for step in (6.0, 3.0, 1.5, 0.75):
        for _ in range(4):
            improved = False
            for axis in range(2):
                for sign in (-1.0, 1.0):
                    trial = [cur[0], cur[1], foot]
                    trial[axis] += sign * step
                    for a in range(2):
                        trial[a] = clamp(trial[a], seed[a] - cap, seed[a] + cap)
                    trial[0] = clamp(trial[0], thigh_lo, thigh_hi)
                    trial[1] = clamp(trial[1], 8.0, knee_hi)
                    if not in_chain(trial):
                        continue
                    trial_l = eval_loss(trial, cur_off)
                    if trial_l + 1e-7 < loss:
                        loss = trial_l
                        best = (trial[0], trial[1], foot)
                        cur = [best[0], best[1]]
                        improved = True
            if lateral:
                for sign in (-1.0, 1.0):
                    trial_off = clamp(cur_off + sign * min(step, abd_step), best_off - abd_step, best_off + abd_step)
                    trial_off = clamp(trial_off, -abd_abs, abd_abs)
                    trial_l = eval_loss((cur[0], cur[1], foot), trial_off)
                    if trial_l + 1e-7 < loss:
                        loss = trial_l
                        cur_off = trial_off
                        best_off = trial_off
                        improved = True
            if not improved:
                break
    # Coupled nudges. Axis-aligned search leaves a centimetre when thigh and knee trade off.
    for step in (2.0, 1.0, 0.5, 0.25):
        for _round in range(8):
            improved = False
            for d0, d1, doff in (
                (step, 0.0, 0.0), (-step, 0.0, 0.0),
                (0.0, step, 0.0), (0.0, -step, 0.0),
                (step, -step, 0.0), (-step, step, 0.0),
                (step, 0.0, step), (step, 0.0, -step),
                (0.0, step, step), (0.0, step, -step),
            ):
                if not lateral and doff != 0.0:
                    continue
                trial = [
                    clamp(cur[0] + d0, seed[0] - cap, seed[0] + cap),
                    clamp(cur[1] + d1, seed[1] - cap, seed[1] + cap),
                    foot,
                ]
                trial[0] = clamp(trial[0], thigh_lo, thigh_hi)
                trial[1] = clamp(trial[1], 8.0, knee_hi)
                if not in_chain(trial):
                    continue
                trial_off = cur_off
                if lateral and doff != 0.0:
                    trial_off = clamp(cur_off + doff, best_off - abd_step, best_off + abd_step)
                    trial_off = clamp(trial_off, -abd_abs, abd_abs)
                trial_l = eval_loss(trial, trial_off)
                if trial_l + 1e-9 < loss:
                    loss = trial_l
                    best = (trial[0], trial[1], foot)
                    cur = [best[0], best[1]]
                    cur_off = trial_off
                    improved = True
            if not improved:
                break
    ABD_OFFSET[side] = cur_off
    apply_leg(arm, side, best[0], best[1], best[2])
    bpy.context.view_layer.update()
    return best


def roll_feet(parm, count):
    heel = parm["strike"][2]
    toe = parm.get("roll_toe", heel)
    if count <= 1:
        return [heel for _ in range(count)]
    return [lerp(heel, toe, i / (count - 1.0)) for i in range(count)]


def heel_is_lowest(name):
    """True when the rear of the ground band is below the forefoot."""
    verts, _polys = world_verts(name)
    if not verts:
        return False
    zmin = min(v.z for v in verts)
    band = [v for v in verts if v.z <= zmin + 0.008]
    if len(band) < 4:
        return False
    ys = [v.y for v in band]
    mid = (min(ys) + max(ys)) * 0.5
    rear = [v.z for v in band if v.y > mid]
    front = [v.z for v in band if v.y <= mid]
    if not rear or not front:
        return False
    return min(rear) < min(front) - 0.0015


def lift_clear(arm, floor=0.004):
    """Raise the visual root so the lowest shell is not biting the floor.

    The candidate sweep sets LOCO_LIFT=0. A lifted worldMax is not a clearance.
    """
    if os.environ.get("LOCO_LIFT", "1") == "0":
        return
    low = None
    for name in active_meshes():
        verts, _polys = world_verts(name)
        if not verts:
            continue
        z = min(v.z for v in verts)
        low = z if low is None else min(low, z)
    if low is not None and low < floor:
        arm.location.z += floor - low
        bpy.context.view_layer.update()


def bone_quats(arm):
    out = {}
    for bone in arm.pose.bones:
        out[bone.name] = bone.matrix.to_quaternion().copy()
    return out


def quat_deg(a, b):
    ang = math.degrees(a.rotation_difference(b).angle)
    if ang < 0.0:
        ang = -ang
    if ang > 180.0:
        ang = 360.0 - ang
    return ang


def _pitch(s, fwd, back):
    # s = -1 is the hand in front, s = +1 is the hand back by the hip.
    return lerp(fwd, back, (s + 1.0) * 0.5)


def upper(phase, parm, bank=0.0, breath=0.0, shift=0.0):
    s = math.sin(2.0 * math.pi * phase)
    if "hip_lean" in parm:
        # Lean lives on the hips. The head counters so the face stays up.
        # The elbow stays bent; the shoulder does the pump.
        twist = parm.get("twist", 0.0) * s
        fwd = parm["arm_fwd"]
        back = parm["arm_back"]
        elbow = parm["elbow"]
        return {
            "Hips": (parm["hip_lean"] + breath * 0.25, 0.0, shift),
            "Spine": (parm.get("spine_lean", 0.0) + breath * 0.3, 0.0, -twist * 0.35 - shift * 0.3),
            "Chest": (parm.get("chest_lean", 0.0) + breath * 0.4, 0.0, -twist + bank),
            "Neck": (breath * 0.2, 0.0, 0.0),
            "Head": (-parm["hip_lean"] * parm.get("head_counter", 0.55) - breath * 0.3, 0.0, -bank * 0.35),
            "Shoulder_L": (0.0, 0.0, 2.0),
            "Shoulder_R": (0.0, 0.0, -2.0),
            # Y opens the elbow away from the ribs. X is the pump.
            "UpperArm_L": (_pitch(s, fwd, back), -parm["arm_out"], 0.0),
            "UpperArm_R": (_pitch(-s, fwd, back), parm["arm_out"], 0.0),
            "LowerArm_L": (elbow, 0.0, 0.0),
            "LowerArm_R": (elbow, 0.0, 0.0),
            "Hand_L": (0.0, 0.0, 0.0),
            "Hand_R": (0.0, 0.0, 0.0),
        }
    lean = parm["lean"]
    arm = parm["arm"]
    # Left heel-strike is phase 0, so the left arm is back (positive X).
    return {
        "Hips": (lean * 0.2 + breath * 0.25, 0.0, shift),
        "Spine": (lean * 0.28 + breath * 0.35, 0.0, -parm.get("chest", 0.0) * 0.35 * s - shift * 0.3),
        "Chest": (lean * 0.16 + breath * 0.5, 0.0, -parm.get("chest", 0.0) * 0.45 * s + bank),
        "Neck": (breath * 0.2, 0.0, 0.0),
        "Head": (-lean * 0.25 - breath * 0.3, 0.0, -bank * 0.2),
        "Shoulder_L": (0.0, 0.0, 1.5 + breath * 0.8),
        "Shoulder_R": (0.0, 0.0, -1.5 - breath * 0.8),
        "UpperArm_L": (arm * s, 0.0, parm["arm_out"]),
        "UpperArm_R": (-arm * s, 0.0, -parm["arm_out"]),
        "LowerArm_L": (-8.0, 0.0, 0.0),
        "LowerArm_R": (-8.0, 0.0, 0.0),
        "Hand_L": (0.0, 0.0, 0.0),
        "Hand_R": (0.0, 0.0, 0.0),
    }


def apply_upper(arm, pose):
    for name, eul in pose.items():
        set_e(arm, name, eul[0], eul[1], eul[2])


def set_root(arm, y, z, yaw_deg=0.0):
    arm.location = Vector((0.0, y, z))
    arm.rotation_euler = Euler((0.0, 0.0, rad(yaw_deg)), "XYZ")


# Strike pose: foot forward, knee soft, sole near the ground. Tuned on the mesh.
STRIKE = (-20.0, 44.0, -16.0)

# Start, stop, skid, and the blend still use the pass-2 sprint shape.
LEGACY_SPRINT = {
    "lean": 16.0, "arm": 12.0, "arm_out": 10.0, "hip": 0.0, "chest": 10.0,
    "root_z": -0.05, "lift": 36.0, "n": 8, "stance": 1,
    "strike": (-22.0, 46.0, -14.0), "cap": 18.0, "foot_cap": 8.0,
    "lift_floor": 0.55,
}


def gait_parm(speed, crouch=False):
    if crouch:
        # Hips drop only as far as a bent leg can still set the sole on the ground.
        return {
            "lean": 10.0, "arm": 8.0, "arm_out": 8.0, "hip": 0.0, "chest": 3.0,
            "root_z": -0.08, "lift": 26.0, "n": 12, "stance": 3,
            "strike": (-28.0, 52.0, -10.0), "cap": 12.0, "foot_cap": 6.0,
            "swing_thigh": -4.0,
        }
    if speed >= 12.0:
        # 13.8 m/s moves the hips 46 cm a frame, so the shoe taps for one frame.
        # The swing is long enough for a heel kick and a 65° thigh under 25°/frame.
        return {
            "lean": 18.0, "arm": 12.0,
            "hip_lean": 18.0, "spine_lean": 4.0, "chest_lean": 2.0,
            "head_counter": 0.55, "twist": 6.0,
            "elbow": -85.0, "arm_fwd": -78.0, "arm_back": 56.0, "arm_out": 28.0,
            "abduct": 14.0,
            # Extra drop so a straight leg can still pin the shoe across 46 cm.
            "root_z": -0.08, "root_swing": -0.08,
            "root_stance": (-0.08, -0.20),
            "n": 18, "stance": 2,
            "strike": (-22.0, 46.0, -14.0), "roll_toe": -2.0,
            "cap": 16.0, "foot_cap": 8.0,
            "budget": 22.0,
            # The hips move 46 cm a frame. Stance may exceed 22° so the sole stays put.
            "stance_cap": 80.0, "stance_chain": 100.0,
            "lateral": True, "abd_step": 14.0, "abd_abs": 28.0,
            "keys": [
                (2, (6.0, 70.0, -6.0)),
                (5, (16.0, 122.0, 3.0)),
                (12, (-85.0, 108.0, -2.0)),
                (16, (-24.0, 50.0, -12.0)),
            ],
        }
    if speed >= 9.0:
        return {
            "lean": 9.0, "arm": 12.0,
            "hip_lean": 9.0, "spine_lean": 3.0, "chest_lean": 1.5,
            "head_counter": 0.55, "twist": 5.0,
            "elbow": -85.0, "arm_fwd": -78.0, "arm_back": 56.0, "arm_out": 28.0,
            "abduct": 12.0,
            # Two stance intervals are 69 cm. The hips sit lower so the sole can stay.
            "root_z": -0.08, "root_swing": -0.08,
            "root_stance": (-0.08, -0.16, -0.26),
            "n": 18, "stance": 3,
            "strike": STRIKE, "roll_toe": -6.0,
            "cap": 16.0, "foot_cap": 8.0,
            "budget": 22.0,
            "stance_cap": 70.0, "stance_chain": 90.0,
            "lateral": True, "abd_step": 12.0, "abd_abs": 24.0,
            "keys": [
                (2, (-4.0, 55.0, -8.0)),
                (5, (16.0, 120.0, 2.0)),
                (12, (-72.0, 102.0, -4.0)),
                (15, (-20.0, 46.0, -14.0)),
            ],
        }
    return {
        "lean": 5.0, "arm": 12.0,
        "hip_lean": 5.0, "spine_lean": 2.0, "chest_lean": 1.0,
        "head_counter": 0.5, "twist": 4.0,
        "elbow": -85.0, "arm_fwd": -78.0, "arm_back": 56.0, "arm_out": 28.0,
        "abduct": 11.0,
        "root_z": -0.03, "n": 18, "stance": 3,
        "strike": (-18.0, 40.0, -12.0), "roll_toe": -4.0,
        "cap": 18.0, "foot_cap": 8.0,
        "budget": 20.0,
        "lateral": True,
        "keys": [
            (4, (-10.0, 36.0, -4.0)),
            (8, (2.0, 64.0, -2.0)),
            (11, (-36.0, 60.0, -6.0)),
            (15, (-18.0, 42.0, -12.0)),
        ],
    }


def frame_root(parm, i, n, stance):
    """Low hips on the later stance frames, so a long plant can still reach.

    Swing frames use root_swing when it is set, which keeps the passing shoe up.
    """
    half = n // 2
    levels = parm.get("root_stance")
    if i < stance:
        return levels[i] if levels else parm["root_z"]
    if half <= i < half + stance:
        phase = i - half
        return levels[phase] if levels else parm["root_z"]
    return parm.get("root_swing", parm["root_z"])


def bake_gait(arm, speed, crouch=False, bank_fn=None, yaw_fn=None):
    """One looping cycle. Stance frames lock the sole. Swing frames ease back."""
    global USE_TURNOUT, USE_BALL
    reset_leg_style()
    USE_BALL = False
    parm = gait_parm(speed, crouch)
    if "abduct" in parm:
        USE_TURNOUT = False
        ABDUCT["L"] = parm["abduct"]
        ABDUCT["R"] = parm["abduct"]
    USE_BALL = "roll_toe" in parm
    n = parm["n"]
    stance = parm["stance"]
    strike = parm["strike"]
    cap = parm["cap"]
    foot_cap = parm["foot_cap"]
    half = n // 2
    frames = []
    # Solve the left plant across the opening stance, then mirror it for the right.
    plant = []
    seed = strike
    clear_pose(arm)
    set_root(arm, 0.0, frame_root(parm, 0, n, stance))
    apply_upper(arm, upper(0.0, parm))
    apply_leg(arm, "L", *strike)
    apply_leg(arm, "R", 8.0, 36.0, -8.0)
    bpy.context.view_layer.update()
    contact, _z = forefoot_anchor("Mesh_Foot_L") if "roll_toe" in parm else sole_of("Mesh_Foot_L")
    target = Vector((contact.x, contact.y, max(contact.z, 0.012)))
    feet = roll_feet(parm, stance)
    # Learn which foot pitch lifts the heel, so the sole contact stays on the ball.
    heel_sign = 0.0
    if "roll_toe" in parm and stance >= 2:
        apply_leg(arm, "L", *strike)
        bpy.context.view_layer.update()
        base_rear = heel_is_lowest("Mesh_Foot_L")
        best_delta = 0.0
        for delta in (6.0, -6.0, 12.0, -12.0):
            apply_leg(arm, "L", strike[0], strike[1], strike[2] + delta)
            bpy.context.view_layer.update()
            if not heel_is_lowest("Mesh_Foot_L"):
                best_delta = delta
                break
        if base_rear and best_delta != 0.0:
            heel_sign = best_delta / abs(best_delta)
            feet = [f + heel_sign * abs(best_delta) * (0.45 + 0.55 * i / (stance - 1.0)) for i, f in enumerate(feet)]
    for i in range(stance):
        y = -speed * i * DT
        clear_pose(arm)
        set_root(arm, y, frame_root(parm, i, n, stance))
        apply_upper(arm, upper(i / n, parm))
        apply_leg(arm, "R", 8.0, 36.0, -8.0)
        if "roll_toe" in parm:
            seed = solve_roll(
                arm, "L", target, seed, feet[i],
                cap=parm.get("stance_cap", cap),
                lateral=parm.get("lateral", False),
                chain=parm.get("stance_chain", 22.0),
                abd_step=parm.get("abd_step", 6.0),
                abd_abs=parm.get("abd_abs", 12.0),
            )
        else:
            seed = solve_leg(arm, "L", target, seed, cap=cap, foot_cap=foot_cap)
        plant.append(seed)
    release = plant[-1]
    # The live loop solves the rail again. Do not carry the preview nudge into it.
    ABD_OFFSET["L"] = 0.0
    ABD_OFFSET["R"] = 0.0
    swing = []
    swing_n = n - stance
    if "keys" in parm:
        swing = build_swing(release, parm["keys"], swing_n, parm["budget"], foot_cap)
    else:
        for j in range(swing_n):
            u = (j + 1) / swing_n
            s = smooth(u)
            lift_u = math.sin(math.pi * u)
            # Keep the shoe up through the last swing frame. The stance frame is the contact.
            if u > 0.5:
                lift_u = max(lift_u, parm.get("lift_floor", 0.0))
            thigh = lerp(release[0], strike[0], s)
            if "swing_thigh" in parm:
                thigh = lerp(thigh, parm["swing_thigh"], lift_u)
            knee = lerp(release[1], strike[1], s) + parm["lift"] * lift_u
            foot = lerp(release[2], strike[2], s)
            swing.append((thigh, clamp(knee, 8.0, 78.0), foot))
    left = plant + swing
    # Right leg uses the same shape, half a cycle later. Stance frames are
    # solved again so the sole stays in the world while the hips move past it.
    right = left[half:] + left[:half]
    hold = {"L": None, "R": None}
    seed = {"L": strike, "R": right[0]}
    # The swing has to arrive on the plant or the loop seam pops the foot.
    locked = {"L": plant[0], "R": plant[0]}
    arrive = 3 if "keys" in parm else 5
    budget = parm.get("budget", cap)
    for i in range(n):
        y = -speed * i * DT
        yaw = 0.0 if yaw_fn is None else yaw_fn(i / n)
        bank = 0.0 if bank_fn is None else bank_fn(i / n)
        stance_l = i < stance
        stance_r = half <= i < half + stance
        clear_pose(arm)
        set_root(arm, y, frame_root(parm, i, n, stance), yaw)
        pose = upper(i / n, parm, bank)
        apply_upper(arm, pose)
        apply_leg(arm, "L", *left[i])
        apply_leg(arm, "R", *right[i])
        bpy.context.view_layer.update()
        for side, planted, curve in (("L", stance_l, left[i]), ("R", stance_r, right[i])):
            start = 0 if side == "L" else half
            ahead = (start - i) % n
            if planted:
                phase_i = i if side == "L" else i - half
                if hold[side] is None:
                    picker = forefoot_anchor if "roll_toe" in parm else sole_of
                    contact, _z = picker("Mesh_Foot_" + side)
                    hold[side] = Vector((contact.x, contact.y, max(contact.z, 0.012)))
                if "roll_toe" in parm:
                    seed[side] = solve_roll(
                        arm, side, hold[side], seed[side], feet[phase_i],
                        cap=parm.get("stance_cap", cap),
                        lateral=parm.get("lateral", False),
                        chain=parm.get("stance_chain", 22.0),
                        abd_step=parm.get("abd_step", 6.0),
                        abd_abs=parm.get("abd_abs", 12.0),
                    )
                else:
                    seed[side] = solve_leg(arm, side, hold[side], seed[side], cap=cap, foot_cap=foot_cap)
                if phase_i == 0:
                    locked[side] = seed[side]
                if os.environ.get("LOCO_DEBUG") == "1":
                    _c, zmin = sole_of("Mesh_Foot_" + side)
                    ball, _bz = ball_of("Mesh_Foot_" + side)
                    print(
                        " STANCE", side, i, [round(v, 1) for v in seed[side]],
                        "zmin", round(zmin, 3),
                        "ball", round(ball.x, 3), round(ball.y, 3),
                    )
            else:
                hold[side] = None
                if parm.get("lateral") and abs(ABD_OFFSET[side]) > 0.05:
                    ABD_OFFSET[side] += clamp(-ABD_OFFSET[side], -3.0, 3.0)
                goal = curve
                if 0 < ahead <= arrive:
                    goal = locked[side]
                if "keys" in parm or 0 < ahead <= arrive:
                    posed = chain_step(seed[side], goal, budget, foot_cap)
                elif parm.get("snap"):
                    posed = curve
                else:
                    gap = max(abs(seed[side][k] - curve[k]) for k in range(3))
                    if gap <= cap:
                        posed = curve
                    else:
                        posed = step_toward(seed[side], curve, (cap, cap, foot_cap))
                if 0 < ahead <= arrive:
                    apply_leg(arm, side, *posed)
                    bpy.context.view_layer.update()
                    _c, zmin = sole_of("Mesh_Foot_" + side)
                    if zmin < -0.002:
                        lifted = clear_ground(arm, side, posed)
                        # One step back toward the plant, so the seam and the pop cap agree.
                        posed = chain_step(lifted, locked[side], budget, foot_cap)
                    seed[side] = posed
                else:
                    seed[side] = clear_ground(arm, side, posed)
        bpy.context.view_layer.update()
        if os.environ.get("LOCO_DEBUG") == "1":
            la = arm.pose.bones["UpperArm_L"].rotation_euler
            print(
                " GAIT", i,
                "L", [round(v, 1) for v in seed["L"]],
                "R", [round(v, 1) for v in seed["R"]],
                "arm", round(math.degrees(la.x), 1),
                "plant", int(stance_l), int(stance_r),
            )
        lift_clear(arm, floor=0.006)
        frames.append(capture(arm, i * DT, stance_l=stance_l, stance_r=stance_r))
    return frames


def capture(arm, t, stance_l, stance_r):
    contacts = {}
    planted = {}
    for side, flag in (("L", stance_l), ("R", stance_r)):
        # A heel-to-toe plant is scored at the ball. The sole centroid walks forward as the heel lifts.
        picker = forefoot_anchor if USE_BALL else sole_of
        c, _patch_z = picker("Mesh_Foot_" + side)
        _sole, zmin = sole_of("Mesh_Foot_" + side)
        # Slide tracks the sole. The forefoot centroid rises as the heel lifts,
        # and that rise is the roll, not the shoe skating.
        contacts[side] = Vector((c.x, c.y, zmin))
        planted[side] = flag and zmin < 0.02
    return {
        "t": t,
        "quats": bone_quats(arm),
        "contacts": contacts,
        "planted": planted,
        "loc": arm.location.copy(),
        "yaw": math.degrees(arm.rotation_euler.z),
        "legs": {
            "L": tuple(math.degrees(a) for a in arm.pose.bones["UpperLeg_L"].rotation_euler),
            "R": tuple(math.degrees(a) for a in arm.pose.bones["UpperLeg_R"].rotation_euler),
        },
    }


def settle_both(arm, seed, root_z):
    """Put both soles on the ground and return the world targets plus the solved angles."""
    clear_pose(arm)
    set_root(arm, 0.0, root_z)
    apply_leg(arm, "L", *seed)
    apply_leg(arm, "R", *seed)
    bpy.context.view_layer.update()
    holds = {}
    solved = {}
    for side in ("L", "R"):
        contact, _z = sole_of("Mesh_Foot_" + side)
        holds[side] = Vector((contact.x, contact.y, 0.004))
        solved[side] = solve_leg(arm, side, holds[side], seed, cap=18.0, foot_cap=8.0)
    return holds, solved


def bake_idle(arm):
    """Breath and a small weight shift. Both soles stay where they were set down."""
    reset_leg_style()
    n = 48
    parm = {"lean": 2.0, "arm": 3.0, "arm_out": 16.0, "hip": 0.0, "chest": 0.0, "root_z": -0.02}
    holds, seed = settle_both(arm, (4.0, 22.0, -8.0), parm["root_z"])
    for i in range(n):
        t = i * DT
        breath = math.sin(2.0 * math.pi * t / 1.7)
        shift = 0.8 * math.sin(2.0 * math.pi * t / 2.6)
        clear_pose(arm)
        set_root(arm, 0.004 * math.sin(2.0 * math.pi * t / 2.6), parm["root_z"])
        pose = upper(0.0, parm, breath=breath, shift=shift)
        pose["UpperArm_L"] = (6.0 + 1.2 * breath, 0.0, 8.0)
        pose["UpperArm_R"] = (6.0 + 1.2 * breath, 0.0, -8.0)
        pose["LowerArm_L"] = (-8.0, 0.0, 0.0)
        pose["LowerArm_R"] = (-8.0, 0.0, 0.0)
        apply_upper(arm, pose)
        for side in ("L", "R"):
            seed[side] = solve_leg(arm, side, holds[side], seed[side], cap=12.0, foot_cap=6.0)
        capture(arm, t, True, True)
    return []


def bake_turn(arm):
    """Chest and head lead a turn in place. Hips stay nearly square so the planted shoe can hold."""
    reset_leg_style()
    n = 22
    parm = {"lean": 2.0, "arm": 5.0, "arm_out": 16.0, "hip": 0.0, "chest": 0.0}
    holds, seed = settle_both(arm, (3.0, 20.0, -6.0), -0.02)
    prev_step = seed["R"]
    for i in range(n):
        u = i / (n - 1.0)
        s = smooth(u)
        win = clamp((u - 0.18) / 0.50, 0.0, 1.0)
        lifting = 0.12 < win < 0.88
        clear_pose(arm)
        set_root(arm, 0.0, -0.02)
        yaw = 22.0 * s
        pose = upper(0.0, parm)
        pose["Hips"] = (1.0, 0.0, 0.0)
        pose["Spine"] = (0.4, 0.0, -yaw * 0.28)
        pose["Chest"] = (0.0, 0.0, -yaw * 0.40)
        pose["Head"] = (-0.6, 0.0, yaw * 0.16)
        pose["UpperArm_L"] = (6.0, 0.0, 8.0)
        pose["UpperArm_R"] = (lerp(4.0, -4.0, s), 0.0, -8.0)
        apply_upper(arm, pose)
        seed["L"] = solve_leg(arm, "L", holds["L"], seed["L"], cap=8.0, foot_cap=5.0)
        if lifting:
            k = math.sin(math.pi * clamp((win - 0.12) / 0.76, 0.0, 1.0))
            if k < 0.5:
                goal = (4.0, lerp(22.0, 56.0, k / 0.5), -4.0)
            else:
                goal = (lerp(4.0, -4.0, (k - 0.5) / 0.5), 56.0, lerp(-4.0, 2.0, (k - 0.5) / 0.5))
            prev_step = step_toward(prev_step, goal, (6.0, 6.0, 4.0))
            prev_step = clear_ground(arm, "R", prev_step, floor=0.02)
            apply_leg(arm, "R", *prev_step)
            bpy.context.view_layer.update()
            _c, zmin = sole_of("Mesh_Foot_R")
            if zmin < 0.016:
                prev_step = (min(16.0, prev_step[0] + 5.0), min(78.0, prev_step[1] + 8.0), prev_step[2])
                apply_leg(arm, "R", *prev_step)
            seed["R"] = prev_step
        else:
            seed["R"] = solve_leg(arm, "R", holds["R"], seed["R"], cap=10.0, foot_cap=6.0)
            prev_step = seed["R"]
        lift_clear(arm)
        capture(arm, i * DT, True, not lifting)
    return []


def bake_land(arm):
    """A drop of about 0.4 m, under the roll. Knees take it and both soles stay put."""
    reset_leg_style()
    n = 18
    parm = {"lean": 3.0, "arm": 6.0, "arm_out": 16.0, "hip": 0.0, "chest": 0.0}
    stand = (4.0, 22.0, -8.0)
    holds, seed = settle_both(arm, stand, -0.02)
    holds = None
    air = (14.0, 34.0, -4.0)
    hops = (
        0.30, 0.22, 0.15, 0.09, 0.04, 0.0,
        -0.02, -0.02, -0.02, -0.02, -0.02, -0.02,
        -0.02, -0.02, -0.02, -0.02, -0.02, -0.02,
    )
    for i in range(n):
        hop = hops[i]
        clear_pose(arm)
        set_root(arm, 0.0, hop)
        grounded = i >= 7
        chest = 6.0 if 8 <= i <= 12 else (3.0 if i < 7 else 1.5)
        if i < 6:
            arm_p = lerp(22.0, 10.0, i / 5.0)
        elif i < 12:
            arm_p = lerp(10.0, 4.0, (i - 6) / 5.0)
        else:
            arm_p = 5.0
        pose = upper(0.0, parm)
        pose["Hips"] = (chest * 0.35, 0.0, 0.0)
        pose["Spine"] = (chest * 0.45, 0.0, 0.0)
        pose["Head"] = (-chest * 0.25, 0.0, 0.0)
        pose["UpperArm_L"] = (arm_p, 0.0, 8.0)
        pose["UpperArm_R"] = (arm_p, 0.0, -8.0)
        pose["LowerArm_L"] = (-8.0, 0.0, 0.0)
        pose["LowerArm_R"] = (-8.0, 0.0, 0.0)
        apply_upper(arm, pose)
        if grounded:
            if holds is None:
                holds = {}
                for side in ("L", "R"):
                    apply_leg(arm, side, *seed[side])
                bpy.context.view_layer.update()
                for side in ("L", "R"):
                    contact, _z = sole_of("Mesh_Foot_" + side)
                    holds[side] = Vector((contact.x, contact.y, 0.01))
            for side in ("L", "R"):
                seed[side] = solve_leg(arm, side, holds[side], seed[side], cap=8.0, foot_cap=4.0)
        else:
            blend = smooth(clamp((0.10 - hop) / 0.10, 0.0, 1.0))
            shape = tuple(lerp(air[k], stand[k], blend) for k in range(3))
            apply_leg(arm, "L", *shape)
            apply_leg(arm, "R", *shape)
            seed["L"] = clear_ground(arm, "L", shape, floor=max(0.02, hop))
            seed["R"] = clear_ground(arm, "R", shape, floor=max(0.02, hop))
        capture(arm, i * DT, grounded, grounded)
    return []


def bake_stop(arm, skid=False):
    """Sprint shape, then the brake shoe locks while the hips sit down. Skid pitches back."""
    reset_leg_style()
    parm = dict(LEGACY_SPRINT)
    prev = {"L": sprint_leg_at(0.0, parm), "R": sprint_leg_at(0.5, parm)}
    y = 0.0
    for i in range(4):
        phase = i / 8.0
        clear_pose(arm)
        y = -SPRINT * i * DT
        set_root(arm, y, parm["root_z"])
        apply_upper(arm, upper(phase, parm))
        for side, ph in (("L", phase), ("R", phase + 0.5)):
            goal = sprint_leg_at(ph, parm)
            prev[side] = step_toward(prev[side], goal, (14.0, 14.0, 8.0))
            apply_leg(arm, side, *prev[side])
            prev[side] = clear_ground(arm, side, prev[side])
        lift_clear(arm)
        capture(arm, i * DT, False, False)
    hold = None
    n = 12
    for i in range(n):
        u = smooth(i / (n - 1.0))
        y -= lerp(0.03, 0.0, u)
        clear_pose(arm)
        set_root(arm, y, lerp(parm["root_z"], -0.06, u))
        pose = upper(0.12, parm)
        if skid:
            pose["Hips"] = (lerp(6.0, -12.0, u), 0.0, 0.0)
            pose["Spine"] = (lerp(4.0, 6.0, u), 0.0, 0.0)
            pose["Chest"] = (lerp(2.0, 3.0, u), 0.0, 0.0)
            pose["Head"] = (lerp(-2.0, -4.0, u), 0.0, 0.0)
            pose["UpperArm_L"] = (lerp(6.0, -8.0, u), 0.0, 10.0)
            pose["UpperArm_R"] = (lerp(-6.0, -8.0, u), 0.0, -10.0)
            pose["LowerArm_L"] = (-8.0, 0.0, 0.0)
            pose["LowerArm_R"] = (-8.0, 0.0, 0.0)
        else:
            pose["Hips"] = (lerp(4.0, -6.0, u), 0.0, 0.0)
            pose["Spine"] = (lerp(4.0, -1.0, u), 0.0, 0.0)
            pose["Head"] = (lerp(-2.0, 2.0, u), 0.0, 0.0)
            pose["UpperArm_L"] = (lerp(8.0, 2.0, u), 0.0, 8.0)
            pose["UpperArm_R"] = (lerp(-4.0, 2.0, u), 0.0, -8.0)
        apply_upper(arm, pose)
        goal = (-12.0, 50.0, -8.0) if not skid else (-4.0, 56.0, -4.0)
        stepped = step_toward(prev["L"], goal, (10.0, 10.0, 6.0))
        if hold is None:
            apply_leg(arm, "L", *stepped)
            bpy.context.view_layer.update()
            contact, zmin = sole_of("Mesh_Foot_L")
            prev["L"] = stepped
            if zmin < 0.018:
                hold = Vector((contact.x, contact.y, 0.02))
                prev["L"] = solve_leg(arm, "L", hold, stepped, cap=14.0, foot_cap=6.0)
        else:
            prev["L"] = solve_leg(arm, "L", hold, stepped, cap=14.0, foot_cap=6.0)
        trail = (6.0, 32.0, -6.0)
        prev["R"] = step_toward(prev["R"], trail, (10.0, 10.0, 6.0))
        prev["R"] = clear_ground(arm, "R", prev["R"])
        lift_clear(arm)
        capture(arm, (4 + i) * DT, hold is not None, False)
    return []


def bake_start(arm):
    """Quiet stand, one push, then the sprint shape. Feet stay off the floor while the speed climbs."""
    reset_leg_style()
    n = 16
    quiet_parm = {"lean": 2.0, "arm": 4.0, "arm_out": 8.0, "hip": 0.0, "chest": 0.0}
    quiet = upper(0.0, quiet_parm)
    holds, seed = settle_both(arm, (4.0, 22.0, -8.0), -0.02)
    parm = dict(LEGACY_SPRINT)
    y = 0.0
    for i in range(n):
        u = smooth(i / (n - 1.0))
        speed = SPRINT * u
        phase = i / 8.0
        clear_pose(arm)
        y -= speed * DT
        set_root(arm, y, lerp(-0.02, parm["root_z"], u))
        moving = upper(phase % 1.0, parm)
        pose = {k: tuple(lerp(quiet[k][j], moving[k][j], u) for j in range(3)) for k in moving}
        apply_upper(arm, pose)
        pushing = speed * DT < 0.18
        for side, ph in (("L", phase), ("R", phase + 0.5)):
            raw = sprint_leg_at(ph, parm)
            # Free shoes carry extra knee so a fast root cannot push them through the floor.
            goal = (raw[0] + 4.0 * u, raw[1] + 16.0 * u, raw[2])
            goal = tuple(lerp(4.0, c, u) for c in goal)
            if pushing and side == "L":
                seed[side] = solve_leg(arm, side, holds[side], seed[side], cap=12.0, foot_cap=6.0)
            else:
                seed[side] = step_toward(seed[side], goal, (12.0, 12.0, 8.0))
                apply_leg(arm, side, *seed[side])
                seed[side] = clear_ground(arm, side, seed[side])
                _c, zmin = sole_of("Mesh_Foot_" + side)
                if zmin < 0.004:
                    lifted = (min(24.0, seed[side][0] + 6.0), min(78.0, seed[side][1] + 6.0), seed[side][2])
                    seed[side] = clear_ground(arm, side, lifted)
        capture(arm, i * DT, pushing, False)
    return []


def sprint_leg_at(phase, parm):
    p = phase % 1.0
    # Mirror of the baked shape: strike, brief support, swing with a knee lift.
    if p < 0.18:
        u = p / 0.18
        return (
            lerp(STRIKE[0], 4.0, u),
            lerp(STRIKE[1], 32.0, u),
            lerp(STRIKE[2], -12.0, u),
        )
    u = (p - 0.18) / 0.82
    s = smooth(u)
    knee = lerp(32.0, STRIKE[1], s) + parm["lift"] * math.sin(math.pi * u)
    return (lerp(4.0, STRIKE[0], s), clamp(knee, 8.0, 78.0), lerp(-12.0, STRIKE[2], s))


def bake_blend(arm):
    """Walk eases into the sprint. One continuous speed change, soles kept off the floor."""
    reset_leg_style()
    n = 16
    prev = {"L": (4.0, 22.0, -8.0), "R": (4.0, 22.0, -8.0)}
    y = 0.0
    for i in range(n):
        u = smooth(i / (n - 1.0))
        speed = lerp(WALK, SPRINT, u)
        phase = i / 8.0
        parm = {
            "lean": lerp(8.0, 16.0, u),
            "arm": lerp(12.0, 12.0, u),
            "arm_out": lerp(8.0, 10.0, u),
            "chest": lerp(6.0, 10.0, u),
            "lift": lerp(26.0, 34.0, u),
        }
        clear_pose(arm)
        y -= speed * DT
        set_root(arm, y, lerp(-0.02, -0.05, u))
        apply_upper(arm, upper(phase % 1.0, parm))
        for side, ph in (("L", phase), ("R", phase + 0.5)):
            goal = sprint_leg_at(ph, parm)
            prev[side] = step_toward(prev[side], goal, (12.0, 12.0, 8.0))
            apply_leg(arm, side, *prev[side])
            prev[side] = clear_ground(arm, side, prev[side])
        capture(arm, i * DT, False, False)
    return []


def bake_lean(arm):
    """Run with the chest banking through the stride. The root heading stays straight so the plant holds."""
    def bank(u):
        return 8.0 * math.sin(math.pi * u)

    return bake_gait(arm, RUN, bank_fn=bank)


def summarize(frames, wrap=False):
    bone = 0.0
    bone_name = ""
    bone_at = -1
    slide = 0.0
    drift = {"L": 0.0, "R": 0.0}
    anchor = {"L": None, "R": None}
    prev = None
    rows = frames
    if wrap and len(frames) > 1:
        rows = list(frames) + [frames[0]]
    for index, row in enumerate(rows):
        if prev is not None:
            for name, q in row["quats"].items():
                d = quat_deg(prev["quats"][name], q)
                if d > bone:
                    bone = d
                    bone_name = name
                    bone_at = index
            for side in ("L", "R"):
                if row["planted"][side] and prev["planted"][side]:
                    dist = (row["contacts"][side] - prev["contacts"][side]).length * 100.0
                    if dist > slide:
                        slide = dist
        for side in ("L", "R"):
            if row["planted"][side]:
                if anchor[side] is None:
                    anchor[side] = row["contacts"][side].copy()
                drift[side] = max(drift[side], (row["contacts"][side] - anchor[side]).length * 100.0)
            else:
                anchor[side] = None
        prev = row
    if os.environ.get("LOCO_DEBUG") == "1":
        for row in frames:
            cl = row["contacts"]["L"]
            cr = row["contacts"]["R"]
            print(
                " FRAME", round(row["t"], 3),
                "L", round(cl.x, 3), round(cl.y, 3), round(cl.z, 3), "p", int(row["planted"]["L"]),
                "R", round(cr.x, 3), round(cr.y, 3), round(cr.z, 3), "p", int(row["planted"]["R"]),
            )
    return {
        "frames": len(frames),
        "bone": bone,
        "bone_name": bone_name,
        "bone_at": bone_at,
        "slide": slide,
        "drift": max(drift.values()) if drift else 0.0,
        "rows": frames,
    }


def noclip(arm):
    packed = {}
    for name in active_meshes():
        verts, polys = world_verts(name)
        packed[name] = (verts, BVHTree.FromPolygons(verts, polys))
    world = 0.0
    note = ""
    gnote = ""
    pairs = {}
    for name, (verts, _bvh) in packed.items():
        zmin = min(v.z for v in verts)
        pen = -zmin * 100.0
        if pen > world:
            world = pen
            gnote = name
    self_max = 0.0
    names = list(packed.keys())
    direction = Vector((1.0, 0.2, 0.05))
    for i in range(len(names)):
        for j in range(i + 1, len(names)):
            a, b = names[i], names[j]
            va, ba = packed[a]
            vb, bb = packed[b]
            if not boxes_near(va, vb):
                continue
            joined = bones_joined(a, b)
            joint = None
            if joined:
                child = hinge_bone(a, b)
                joint = arm.matrix_world @ arm.pose.bones[child].head
            depth = max(
                side_depth(va, bb, joined, joint, direction),
                side_depth(vb, ba, joined, joint, direction),
            )
            if depth > 0.0:
                pairs[a + "|" + b] = depth
            if depth > self_max:
                self_max = depth
                note = a + "|" + b
    return world, self_max, gnote + " " + note, pairs


def bone_name(mesh):
    if mesh in CAP_BONES:
        return CAP_BONES[mesh]
    return BONE_OF[mesh]


def bones_joined(mesh_a, mesh_b):
    """True when the two meshes share a hinge. Caps count for both bones of that hinge."""
    ba = bone_name(mesh_a)
    bb = bone_name(mesh_b)
    if ba == bb:
        return True
    if PARENT.get(ba) == bb or PARENT.get(bb) == ba:
        return True
    caps = CAP_BONES
    for mesh, other in ((mesh_a, bb), (mesh_b, ba)):
        if mesh not in caps:
            continue
        hinge = caps[mesh]
        if other == hinge or other == PARENT.get(hinge):
            return True
    return False


def hinge_bone(mesh_a, mesh_b):
    caps = CAP_BONES
    if mesh_a in caps:
        return caps[mesh_a]
    if mesh_b in caps:
        return caps[mesh_b]
    ba = bone_name(mesh_a)
    bb = bone_name(mesh_b)
    if PARENT.get(bb) == ba:
        return bb
    return ba


def active_meshes():
    names = [name for name in MESHES if name in bpy.data.objects]
    extra = CAP_BONES
    for name in extra:
        if name in bpy.data.objects and name not in names:
            names.append(name)
    return names


def boxes_near(va, vb):
    def box(vs):
        xs = [p.x for p in vs]
        ys = [p.y for p in vs]
        zs = [p.z for p in vs]
        return (min(xs), max(xs), min(ys), max(ys), min(zs), max(zs))
    a = box(va)
    b = box(vb)
    if a[1] < b[0] - 0.01 or b[1] < a[0] - 0.01:
        return False
    if a[3] < b[2] - 0.01 or b[3] < a[2] - 0.01:
        return False
    if a[5] < b[4] - 0.01 or b[5] < a[4] - 0.01:
        return False
    return True


def side_depth(verts, bvh, joined, joint, direction):
    """Depth past the surface. A handful of open-edge hits does not count as a body intersection."""
    worst = 0.0
    hits = 0
    for p in verts:
        nearest, normal, _i, dist = bvh.find_nearest(p)
        if nearest is None or dist is None or dist <= 0.005 or dist > 0.04 or normal is None:
            continue
        if (p - nearest).dot(normal) >= 0.0:
            continue
        if joined and joint is not None and (p - joint).length <= 0.03:
            continue
        hits += 1
        cm = dist * 100.0
        if cm > worst:
            worst = cm
    if hits < 4:
        return 0.0
    return worst


def inside(bvh, point, direction):
    hits = 0
    origin = point.copy()
    for _ in range(8):
        loc, _nrm, _idx, _dist = bvh.ray_cast(origin, direction)
        if loc is None:
            break
        hits += 1
        origin = loc + direction * 0.0005
    return hits % 2 == 1


def replay(arm, kind, index, store):
    """Re-pose a captured frame. The store keeps the baked degrees on the armature by re-baking once."""
    frames = store["frames"][kind]
    # Re-run the baker up to this index so the mesh matches the measured frame.
    # Bakers are deterministic. The caller poses by index through a cached pose log.
    pose = store["posed"][kind][index]
    clear_pose(arm)
    set_root(arm, pose["y"], pose["z"], pose["yaw"])
    apply_upper(arm, pose["upper"])
    apply_leg(arm, "L", *pose["L"])
    apply_leg(arm, "R", *pose["R"])
    bpy.context.view_layer.update()


def tint():
    for mat in bpy.data.materials:
        if not mat.use_nodes:
            continue
        node = mat.node_tree.nodes.get("Principled BSDF")
        if node is None:
            continue
        name = mat.name
        color = JOINT if name in ("Joint_Tan", "Sensor_Tan") else ACCENT if (name == "Accent" or name.startswith("Cal")) else PLAYER
        node.inputs["Base Color"].default_value = color
        if "Roughness" in node.inputs:
            node.inputs["Roughness"].default_value = 0.42


def scene_setup():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = CELL_W
    scene.render.resolution_y = CELL_H
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.compression = 15
    scene.eevee.taa_render_samples = 8
    scene.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("LocoSky")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = SKY
    bg.inputs["Strength"].default_value = 1.0
    bpy.ops.mesh.primitive_plane_add(size=80.0, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = "LocoGround"
    mat = bpy.data.materials.new("LocoGroundMat")
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = GROUND_C
    ground.data.materials.append(mat)
    tick_mat = bpy.data.materials.new("TickMat")
    tick_mat.use_nodes = True
    tick_mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.16, 0.18, 0.16, 1.0)
    for i in range(-10, 11):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, i * 0.5, 0.0015))
        tick = bpy.context.active_object
        tick.name = "Tick"
        tick.scale = (2.6, 0.008, 0.001)
        tick.data.materials.append(tick_mat)
    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 3.4
    sun = bpy.data.objects.new("Sun", sun_data)
    bpy.context.collection.objects.link(sun)
    sun.rotation_euler = Euler((rad(50.0), 0.0, rad(28.0)), "XYZ")
    fill_data = bpy.data.lights.new("Fill", "AREA")
    fill_data.energy = 200.0
    fill_data.size = 4.0
    fill = bpy.data.objects.new("Fill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = (-2.4, -1.6, 2.5)
    cam_data = bpy.data.cameras.new("LocoCam")
    cam_data.lens = 48
    cam = bpy.data.objects.new("LocoCam", cam_data)
    bpy.context.collection.objects.link(cam)
    scene.camera = cam
    return scene, cam


def place_camera(cam, arm):
    cam.data.type = "PERSP"
    cam.data.lens = 48
    origin = arm.matrix_world @ Vector((0.0, 0.0, 0.98))
    yaw = arm.rotation_euler.z
    fwd = Vector((math.sin(yaw), -math.cos(yaw), 0.0))
    left = Vector((-fwd.y, fwd.x, 0.0))
    eye = origin + fwd * 2.05 + left * 1.30 + Vector((0.0, 0.0, 0.16))
    look = origin + Vector((0.0, 0.0, -0.08))
    cam.location = eye
    cam.rotation_euler = (look - eye).to_track_quat("-Z", "Y").to_euler()
    bpy.context.view_layer.update()


def place_camera_side(cam, arm):
    """Orthographic profile. Forward (-Y) points to image left."""
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 2.55
    origin = arm.matrix_world @ Vector((0.0, 0.0, 0.92))
    eye = origin + Vector((3.4, 0.0, 0.02))
    look = origin
    cam.location = eye
    cam.rotation_euler = (look - eye).to_track_quat("-Z", "Y").to_euler()
    bpy.context.view_layer.update()


def frame_fill(scene, cam):
    deps = bpy.context.evaluated_depsgraph_get()
    z0, z1 = 1.0, 0.0
    for name in ("Mesh_Head", "Mesh_Foot_L", "Mesh_Foot_R"):
        obj = bpy.data.objects[name]
        ev = obj.evaluated_get(deps)
        me = ev.to_mesh()
        mw = ev.matrix_world
        for v in me.vertices:
            co = world_to_camera_view(scene, cam, mw @ v.co)
            z0 = min(z0, co.y)
            z1 = max(z1, co.y)
        ev.to_mesh_clear()
    return z1 - z0


def pose_frame(arm, baked):
    """baked entries do not store degrees. Re-apply from the pose log."""
    raise RuntimeError("use log")


def render_sheet(scene, cam, arm, name, log, view="threeq"):
    from PIL import Image, ImageDraw, ImageFont
    os.makedirs(OUT, exist_ok=True)
    n = len(log)
    paths = []
    fills = []
    for i in range(8):
        index = min(n - 1, int(round((n - 1) * i / 7.0)))
        apply_log(arm, log[index])
        if view == "side":
            place_camera_side(cam, arm)
        else:
            place_camera(cam, arm)
        fills.append(frame_fill(scene, cam))
        path = os.path.join(OUT, "_cell_{0}_{1}.png".format(name, i))
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        paths.append((path, i))
    font = ImageFont.truetype(FONT, 26)
    small = ImageFont.truetype(FONT, 18)
    sheet = Image.new("RGB", (CELL_W * 4, CELL_H * 2), (18, 22, 26))
    for path, i in paths:
        cell = Image.open(path).convert("RGB")
        x = (i % 4) * CELL_W
        y = (i // 4) * CELL_H
        sheet.paste(cell, (x, y))
        draw = ImageDraw.Draw(sheet)
        draw.rectangle((x + 8, y + 8, x + 44, y + 40), fill=(12, 16, 20))
        draw.text((x + 16, y + 10), str(i), fill=(240, 236, 220), font=font)
        os.remove(path)
    draw = ImageDraw.Draw(sheet)
    draw.rectangle((8, sheet.height - 34, 20 + 12 * len(name), sheet.height - 8), fill=(12, 16, 20))
    draw.text((14, sheet.height - 32), name, fill=(240, 236, 220), font=small)
    out = os.path.join(OUT, name + ".png")
    sheet.save(out, "PNG", optimize=True)
    if os.path.getsize(out) > 400 * 1024:
        # Palette PNG keeps the contact sheet under 400 KB. RGB after quantize does not.
        colors = 128
        while colors >= 32:
            sheet.quantize(colors=colors, method=Image.Quantize.MEDIANCUT).save(out, "PNG", optimize=True)
            if os.path.getsize(out) <= 400 * 1024:
                break
            colors -= 32
    return out, min(fills)


def apply_log(arm, entry):
    clear_pose(arm)
    set_root(arm, entry["y"], entry["z"], entry["yaw"])
    apply_upper(arm, entry["upper"])
    for side in ("L", "R"):
        leg = entry[side]
        if len(leg) >= 5:
            set_e(arm, "UpperLeg_" + side, leg[0], leg[3], leg[4])
            set_e(arm, "LowerLeg_" + side, leg[1], 0.0, 0.0)
            set_e(arm, "Foot_" + side, leg[2], 0.0, 0.0)
        else:
            yaw = leg[3] if len(leg) > 3 else 0.0
            apply_leg(arm, side, leg[0], leg[1], leg[2], yaw)
    bpy.context.view_layer.update()


def log_current(arm, upper_pose, left, right):
    return {
        "y": arm.location.y,
        "z": arm.location.z,
        "yaw": math.degrees(arm.rotation_euler.z),
        "upper": {k: tuple(v) for k, v in upper_pose.items()},
        "L": tuple(left),
        "R": tuple(right),
    }


def instrument(arm, fn):
    """Run a baker and also keep the degree log by wrapping apply. Simpler: read bones after each capture.

    The bakers already pose the armature. We re-read local eulers after the fact by
    making the baker return frames only. A second pass stores degrees if we patch capture.
    """
    return fn(arm)


def read_pose(arm):
    upper_names = (
        "Hips", "Spine", "Chest", "Neck", "Head",
        "Shoulder_L", "Shoulder_R",
        "UpperArm_L", "UpperArm_R", "LowerArm_L", "LowerArm_R", "Hand_L", "Hand_R",
    )
    upper = {}
    for name in upper_names:
        e = arm.pose.bones[name].rotation_euler
        upper[name] = tuple(math.degrees(v) for v in e)
    def leg(side):
        upper_leg = arm.pose.bones["UpperLeg_" + side].rotation_euler
        return (
            math.degrees(upper_leg.x),
            math.degrees(arm.pose.bones["LowerLeg_" + side].rotation_euler.x),
            math.degrees(arm.pose.bones["Foot_" + side].rotation_euler.x),
            math.degrees(upper_leg.y),
            math.degrees(upper_leg.z),
        )
    return log_current(arm, upper, leg("L"), leg("R"))


def bake_with_log(arm, fn):
    # Monkeypatch capture to record the degree pose alongside the measurement.
    global capture
    frames = []
    log = []
    real_capture = capture

    def wrapped(arm_, t, stance_l, stance_r):
        row = real_capture(arm_, t, stance_l, stance_r)
        frames.append(row)
        log.append(read_pose(arm_))
        return row

    capture = wrapped
    try:
        fn(arm)
    finally:
        capture = real_capture
    return frames, log


def write_table(rows, line):
    from PIL import Image, ImageDraw, ImageFont
    os.makedirs(OUT, exist_ok=True)
    header = "clip frames boneDeg bone slideCm driftCm worldCm selfCm fails"
    lines = [header]
    for row in rows:
        lines.append(
            "{name} {frames} {bone:.2f} {bone_name} {slide:.2f} {drift:.2f} {world:.2f} {self:.2f} {fails}".format(**row)
        )
    with open(os.path.join(OUT, "pop-table.txt"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines) + "\n")
    with open(os.path.join(OUT, "proof.txt"), "w", encoding="utf-8") as fh:
        fh.write(line + "\n")
        fh.write("\n".join(lines) + "\n")
    font = ImageFont.truetype(FONT, 15)
    img = Image.new("RGB", (1040, 34 * (len(lines) + 2)), (16, 20, 24))
    draw = ImageDraw.Draw(img)
    draw.text((10, 8), "30 fps  bone<=25 deg   planted slide<=3 cm   stride drift<=2 cm", fill=(230, 214, 180), font=font)
    for i, text in enumerate(lines):
        color = (230, 232, 228)
        if i > 0:
            parts = text.split()
            bad = float(parts[2]) > 25.0 or float(parts[4]) > 3.0 or float(parts[5]) > 2.0 or int(parts[8]) > 0
            color = (230, 140, 120) if bad else (176, 210, 180)
        draw.text((10, 40 + i * 28), text, fill=color, font=font)
    img.save(os.path.join(OUT, "pop-table.png"), "PNG", optimize=True)


def thigh_world_deg(arm, side):
    hip = arm.matrix_world @ arm.pose.bones["UpperLeg_" + side].head
    knee = arm.matrix_world @ arm.pose.bones["LowerLeg_" + side].head
    v = knee - hip
    # Positive when the knee is forward of the hip. Forward is -Y.
    return math.degrees(math.atan2(-v.y, -v.z)), knee, hip


def shape_line(arm, log):
    """Peak thigh, swing-shoe height, knee-track gap, heel height, hand height."""
    peak = -999.0
    peak_sole = 0.0
    min_gap = 999.0
    heel_z = -1.0
    hip_z = 0.0
    hand_front_z = 0.0
    hand_front_y = 99.0
    hand_back_z = 0.0
    hand_back_y = -99.0
    chin_z = 0.0
    front_arm = 99.0
    front_hand_z = 0.0
    back_arm = -99.0
    back_hand_z = 0.0
    for entry in log:
        apply_log(arm, entry)
        tl, kl, _hl = thigh_world_deg(arm, "L")
        tr, kr, _hr = thigh_world_deg(arm, "R")
        gap = (kl.x - kr.x) * 100.0
        if gap < min_gap:
            min_gap = gap
        hips = arm.matrix_world @ arm.pose.bones["Hips"].head
        for side, ang in (("L", tl), ("R", tr)):
            _c, zmin = sole_of("Mesh_Foot_" + side)
            if ang > peak:
                peak = ang
                peak_sole = zmin * 100.0
            pts, _p = world_verts("Mesh_Foot_" + side)
            heel = max(pts, key=lambda p: (p.y - hips.y))
            if heel.z > heel_z:
                heel_z = heel.z
                hip_z = hips.z
        head_pts, _hp = world_verts("Mesh_Head")
        z_mid = (min(p.z for p in head_pts) + max(p.z for p in head_pts)) * 0.5
        chin_pool = [p for p in head_pts if p.z <= z_mid + 0.02] or list(head_pts)
        chin = min(chin_pool, key=lambda p: p.y)
        chin_z = chin.z
        for side in ("L", "R"):
            hand_pts, _pp = world_verts("Mesh_Hand_" + side)
            fwd = min(hand_pts, key=lambda p: (p.y - hips.y))
            back = max(hand_pts, key=lambda p: (p.y - hips.y))
            rel_f = fwd.y - hips.y
            rel_b = back.y - hips.y
            if rel_f < hand_front_y:
                hand_front_y = rel_f
                hand_front_z = max(p.z for p in hand_pts)
            if rel_b > hand_back_y:
                hand_back_y = rel_b
                hand_back_z = max(p.z for p in hand_pts)
            ax = entry["upper"]["UpperArm_" + side][0]
            top_z = max(p.z for p in hand_pts)
            if ax < front_arm:
                front_arm = ax
                front_hand_z = top_z
            if ax > back_arm:
                back_arm = ax
                back_hand_z = top_z
    arms = []
    elbows = []
    for entry in log:
        for side in ("L", "R"):
            arms.append(entry["upper"]["UpperArm_" + side][0])
            elbows.append(entry["upper"]["LowerArm_" + side][0])
    return (
        "thighPeak={0:.1f} swingSoleCm={1:.1f} kneeGapCm={2:.1f} "
        "heelZ={3:.3f} hipZ={4:.3f} reachZ={5:.3f} rearZ={6:.3f} chinZ={7:.3f} "
        "peakArmHandZ={8:.3f} peakRearHandZ={9:.3f} armX={10:.0f}/{11:.0f} elbow={12:.0f}"
    ).format(
        peak, peak_sole, min_gap, heel_z, hip_z, hand_front_z, hand_back_z, chin_z,
        front_hand_z, back_hand_z,
        min(arms), max(arms), max(elbows) if elbows else 0.0,
    )


def main():
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = bpy.data.objects["DummyArmature"]
    tint()
    rw, rs, rn, rest_pairs = noclip(arm)
    print("REST", round(rw, 2), round(rs, 2), rn)
    scene, cam = scene_setup()
    jobs = [
        ("idle", lambda a: bake_idle(a)),
        ("walk", lambda a: bake_gait(a, WALK)),
        ("run", lambda a: bake_gait(a, RUN)),
        ("sprint", lambda a: bake_gait(a, SPRINT)),
        ("start", lambda a: bake_start(a)),
        ("stop", lambda a: bake_stop(a, False)),
        ("skid", lambda a: bake_stop(a, True)),
        ("turn", lambda a: bake_turn(a)),
        ("lean", lambda a: bake_lean(a)),
        ("crouch", lambda a: bake_gait(a, CROUCH, crouch=True)),
        ("land", lambda a: bake_land(a)),
        ("blend", lambda a: bake_blend(a)),
    ]
    if ONLY:
        jobs = [j for j in jobs if j[0] in ONLY]
    table = []
    total_frames = 0
    world_max = 0.0
    self_max = 0.0
    added_max = 0.0
    fails = 0
    for name, fn in jobs:
        global USE_BALL
        USE_BALL = False
        frames, log = bake_with_log(arm, fn)
        cycles = name in ("idle", "walk", "run", "sprint", "crouch", "lean")
        sim = summarize(frames, wrap=cycles)
        # A frame fails when the ground bite or any pair is deeper than 0.5 cm.
        # Joined neighbours are exempt inside 3 cm of their joint.
        w_clip = 0.0
        s_clip = 0.0
        a_clip = 0.0
        f_clip = 0
        note = ""
        clip_added = ""
        for entry in log:
            apply_log(arm, entry)
            w, s, n, pairs = noclip(arm)
            added = 0.0
            added_name = ""
            for key, depth in pairs.items():
                extra = depth - rest_pairs.get(key, 0.0)
                if extra > added:
                    added = extra
                    added_name = key + ("*" if key not in rest_pairs else "")
            if w > w_clip:
                w_clip = w
            if s > s_clip:
                s_clip = s
                note = n
            if added > a_clip:
                a_clip = added
                clip_added = added_name
            if w > 0.5 or s > 0.5:
                f_clip += 1
        total_frames += len(log)
        world_max = max(world_max, w_clip)
        self_max = max(self_max, s_clip)
        added_max = max(added_max, a_clip)
        fails += f_clip
        print(
            "POP", name, "frames", sim["frames"],
            "bone", sim["bone_name"], round(sim["bone"], 2), "at", sim["bone_at"],
            "slide", round(sim["slide"], 2),
            "drift", round(sim["drift"], 2),
            "world", round(w_clip, 2),
            "self", round(s_clip, 2),
            "added", round(a_clip, 2), clip_added,
            "fails", f_clip,
            "note", note,
        )
        row = {
            "name": name, "frames": sim["frames"], "bone": sim["bone"],
            "bone_name": sim["bone_name"] or "-", "slide": sim["slide"], "drift": sim["drift"],
            "world": w_clip, "self": s_clip, "fails": f_clip,
        }
        table.append(row)
        if name in ("walk", "run", "sprint", "crouch"):
            flight = None
            for row in frames:
                if row["planted"]["L"] or row["planted"]["R"]:
                    continue
                low = min(row["contacts"]["L"].z, row["contacts"]["R"].z)
                flight = low if flight is None else min(flight, low)
            if name in ("walk", "run", "sprint"):
                print("SHAPE", name, shape_line(arm, log))
            if flight is not None:
                print("FLIGHT", name, "lowestSoleCm", round(flight * 100.0, 1))
        if DO_RENDER:
            path, fill = render_sheet(scene, cam, arm, name, log)
            print("SHEET", name, path, "bytes", os.path.getsize(path), "fill", round(fill, 3))
            if name in ("walk", "run", "sprint"):
                side, side_fill = render_sheet(scene, cam, arm, name + "-side", log, view="side")
                print("SHEET", name + "-side", side, "bytes", os.path.getsize(side), "fill", round(side_fill, 3))
    line = "no-clip clips={0} frames={1} worldMax={2:.2f} selfMax={3:.2f} fails={4}".format(
        len(table), total_frames, world_max, self_max, fails,
    )
    line += "\nbind-self={0:.2f} addedMax={1:.2f} fail=ground>0.5cm or pair depth>0.5cm, joined verts exempt within 3cm of the joint".format(
        rs, added_max,
    )
    print(line)
    write_table(table, line)


if __name__ == "__main__":
    main()
