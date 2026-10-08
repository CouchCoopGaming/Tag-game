"""FX kit stills. Hier mannequin, one pose per effect, screen flash on one pane.

No-clip measures evaluated meshes. Joined neighbours are exempt only within
3 cm of their joint (the child bone head). Depth above 0.5 cm fails. There is
no rest-pose credit. The authored hip/thigh overlap past that ball is a rig
failure and is drawn in red.
"""
import math
import os
import sys

import bpy
from mathutils import Euler, Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Characters", "HiPoly", "Dummy_Mannequin_Tan_Hier_Hi.fbx")
OUT = os.environ.get("FX_STILL_OUT", os.path.join(ROOT, "Docs", "FxStills", "pass1"))
SAMPLES = int(os.environ.get("FX_SAMPLES", "12"))
RES_X = 800
RES_Y = 450
MEASURE_ONLY = os.environ.get("FX_MEASURE") == "1"

PLAYER = (0.86, 0.55, 0.42, 1.0)
JOINT = (0.16, 0.17, 0.18, 1.0)
ACCENT = (0.45, 0.82, 0.86, 1.0)
GROUND = (0.42, 0.43, 0.44, 1.0)
SKY = (0.55, 0.64, 0.74, 1.0)
DUST = (0.38, 0.33, 0.28, 1.0)
SHOCK = (0.93, 0.90, 0.82, 1.0)
SPARK = (1.0, 0.86, 0.38, 1.0)
CYAN = (0.45, 0.88, 1.0, 1.0)
STAR = (1.0, 0.86, 0.28, 1.0)
RIM = (0.95, 0.28, 0.32, 1.0)
TAG = (0.95, 0.28, 0.32, 1.0)
WALL = (0.55, 0.52, 0.48, 1.0)
PAD = (0.78, 0.55, 0.18, 1.0)
ROPE = (0.55, 0.40, 0.22, 1.0)
SCUFF = (0.18, 0.16, 0.14, 1.0)

DEPTH_LIMIT = 0.005
JOINT_EXEMPT = 0.03
RIG_PAIRS = {
    frozenset(p) for p in (
        ("Mesh_Foot_L", "Mesh_LowerLeg_L"),
        ("Mesh_Foot_R", "Mesh_LowerLeg_R"),
        ("Mesh_Hand_L", "Mesh_LowerArm_L"),
        ("Mesh_Hand_R", "Mesh_LowerArm_R"),
        ("Mesh_Head", "Mesh_Neck"),
        ("Mesh_Hips", "Mesh_UpperLeg_L"),
        ("Mesh_Hips", "Mesh_UpperLeg_R"),
        ("Mesh_LowerLeg_L", "Mesh_UpperLeg_L"),
        ("Mesh_LowerLeg_R", "Mesh_UpperLeg_R"),
        ("Mesh_Shoulder_L", "Mesh_UpperArm_L"),
        ("Mesh_Shoulder_R", "Mesh_UpperArm_R"),
    )
}
POSE_ADDED = []
FAIL_DEPTH = {}
CHART_N = 0


def note_added(label):
    for name in FAIL_NAMES:
        depth_cm = round(FAIL_DEPTH.get(name, 0.0) * 100.0, 2)
        if "|" not in name:
            print("POSE-WORLD", label, name, depth_cm)
            continue
        if frozenset(name.split("|")) in RIG_PAIRS:
            print("POSE-RIG", label, name, depth_cm)
            continue
        POSE_ADDED.append((label, name, depth_cm))
        print("POSE-ADD", label, name, depth_cm)


def rad(deg):
    return math.radians(deg)


def set_euler(arm, name, xdeg, ydeg, zdeg):
    bone = arm.pose.bones[name]
    bone.rotation_mode = "XYZ"
    bone.rotation_euler = Euler((rad(xdeg), rad(ydeg), rad(zdeg)), "XYZ")


def set_zxy(arm, name, xdeg, ydeg, zdeg):
    """Blender ZXY matches Quaternion.Euler: Z, then X, then Y."""
    bone = arm.pose.bones[name]
    bone.rotation_mode = "ZXY"
    bone.rotation_euler = Euler((rad(xdeg), rad(ydeg), rad(zdeg)), "ZXY")


def clear_pose(arm):
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")


def leg(arm, side, thigh, knee, yaw=0.0):
    yaw_b = yaw if side == "L" else -yaw
    set_euler(arm, "UpperLeg_" + side, -thigh, yaw_b, 0.0)
    set_euler(arm, "LowerLeg_" + side, -knee, 0.0, 0.0)


def arm_pose(arm, side, pitch, yaw, elbow, hand, roll=0.0):
    yaw_b = yaw if side == "L" else -yaw
    roll_b = roll if side == "L" else -roll
    set_euler(arm, "UpperArm_" + side, pitch, yaw_b, roll_b)
    set_euler(arm, "LowerArm_" + side, elbow, 0.0, 0.0)
    set_euler(arm, "Hand_" + side, hand, 0.0, 0.0)


def torso(arm, hip, spine, head):
    set_euler(arm, "Hips", hip, 0.0, 0.0)
    set_euler(arm, "Spine", spine, 0.0, 0.0)
    set_euler(arm, "Chest", 0.0, 0.0, 0.0)
    set_euler(arm, "Head", head, 0.0, 0.0)


def pose_land(arm):
    """LandPose.Hard(handLeft) at full absorb. sinC >= 0 plants the left hand.

    armZ is Lerp(4, 8, max(gait, runVis)) at planar 0, which is 4. The 0.50 m
    mesh drop is not added: the still grounds the lowest vertex.
    """
    foot = -(74.0 + -125.0)
    arm_z = 4.0
    set_zxy(arm, "UpperLeg_L", -74.0, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_L", -(-125.0), 0.0, 0.0)
    set_zxy(arm, "Foot_L", -foot, 0.0, 0.0)
    set_zxy(arm, "UpperLeg_R", -74.0, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_R", -(-125.0), 0.0, 0.0)
    set_zxy(arm, "Foot_R", -foot, 0.0, 0.0)
    set_zxy(arm, "UpperArm_L", 18.0, 16.0, arm_z)
    set_zxy(arm, "LowerArm_L", -16.0, 0.0, 0.0)
    set_zxy(arm, "UpperArm_R", -30.0, -12.0, -arm_z)
    set_zxy(arm, "LowerArm_R", -40.0, 0.0, 0.0)
    set_zxy(arm, "Hips", 46.0, 0.0, 0.0)
    set_zxy(arm, "Spine", 28.0, 0.0, 0.0)
    set_zxy(arm, "Head", -8.0, 0.0, 0.0)
    print("RUNTIME land Hard handLeft foot", round(foot, 2), "armZ", arm_z)


def _ease(u):
    if u <= 0.0:
        return 0.0
    if u >= 1.0:
        return 1.0
    return u * u * (3.0 - 2.0 * u)


def _lerp(a, b, t):
    return a + (b - a) * t


def _clamp(v, lo, hi):
    if v < lo:
        return lo
    if v > hi:
        return hi
    return v


def _inv(a, b, v):
    d = b - a
    if -0.00001 < d < 0.00001:
        return 0.0
    return _clamp((v - a) / d, 0.0, 1.0)


def _smooth(u):
    u = _clamp(u, 0.0, 1.0)
    return u * u * (3.0 - 2.0 * u)


def _pose_weight(speed):
    t = _inv(0.35, 12.0, speed)
    return 1.0 - (1.0 - t) * (1.0 - t)


def roll_sample():
    """LandingRollPose.RollAt(0.15 / 0.52). Tuck into the sweep, knees in."""
    u = 0.15 / 0.52
    span = _ease((u - 0.18) / (0.36 - 0.18))
    # Sweep() at 0.18, HandDown() at 0.36.
    # cursor/tag-anim-fx Sweep() and HandDown() after the #120 tuck change.
    # ThighRoll is the Unity Euler Z. Arm roll is 0 on both keys.
    sweep = dict(hip=22, spine=26, head=-36, thigh_l=42, thigh_r=36, knee_l=-74, knee_r=-68,
                 pitch_l=-28, pitch_r=-36, yaw_l=-24, yaw_r=28, roll_r=0, elbow_l=-36, elbow_r=-32,
                 thigh_roll_l=-36, thigh_roll_r=36, spine_roll=14)
    down = dict(hip=14, spine=18, head=-32, thigh_l=44, thigh_r=38, knee_l=-76, knee_r=-70,
                pitch_l=-18, pitch_r=-32, yaw_l=-20, yaw_r=24, roll_r=0, elbow_l=-32, elbow_r=-28,
                thigh_roll_l=-36, thigh_roll_r=36, spine_roll=20)
    s = {k: _lerp(sweep[k], down[k], span) for k in sweep}
    spin = 360.0 * u
    s["bank"] = math.sin(math.radians(spin * 0.5)) * 62.0
    s["span"] = span
    return s


def pose_roll(arm):
    """LandingRollPose.RollAt(0.15/0.52) with shoulderLeft false.

    Bone signs are the FBX map, not a clearance pose: this rig's toes point
    −Y, so a Unity forward thigh is a negative X and a Unity knee bend is a
    positive X. Quaternion.Euler is ZXY. The bank is BankDegrees, about 49°.
    """
    global POSE_BANK
    s = roll_sample()
    set_zxy(arm, "UpperLeg_L", -s["thigh_l"], 0.0, s["thigh_roll_l"])
    set_zxy(arm, "LowerLeg_L", -s["knee_l"], 0.0, 0.0)
    set_zxy(arm, "UpperLeg_R", -s["thigh_r"], 0.0, s["thigh_roll_r"])
    set_zxy(arm, "LowerLeg_R", -s["knee_r"], 0.0, 0.0)
    set_zxy(arm, "UpperArm_L", s["pitch_l"], s["yaw_l"], 0.0)
    set_zxy(arm, "LowerArm_L", s["elbow_l"], 0.0, 0.0)
    set_zxy(arm, "UpperArm_R", s["pitch_r"], s["yaw_r"], s["roll_r"])
    set_zxy(arm, "LowerArm_R", s["elbow_r"], 0.0, 0.0)
    set_zxy(arm, "Hips", s["hip"], 0.0, 0.0)
    set_zxy(arm, "Spine", s["spine"], 0.0, -s["spine_roll"])
    set_zxy(arm, "Head", -s["head"], 0.0, 0.0)
    # Unity axis (right, up, forward) = (0.62, −0.10, 0.78). On this FBX
    # right is −X, up is +Z, forward is −Y, and Unity's angle is left-handed.
    axis = Vector((0.62, 0.78, 0.10))
    axis.normalize()
    POSE_BANK = Quaternion(axis, rad(s["bank"]))
    print(
        "ROLL_RUNTIME",
        "t", 0.15, "dur", 0.52,
        "u", round(0.15 / 0.52, 4),
        "bank", round(s["bank"], 2),
        "thigh", round(s["thigh_l"], 1), round(s["thigh_r"], 1),
        "knee", round(s["knee_l"], 1), round(s["knee_r"], 1),
        "elbow", round(s["elbow_l"], 1), round(s["elbow_r"], 1),
        "thighRoll", round(s["thigh_roll_l"], 1), round(s["thigh_roll_r"], 1),
    )


def pose_grapple(arm):
    """Pull(phaseSin=0, vy=0, lean=0), ForBody once, weight 1.

    ApplyBodyLine(elev=0) and RopeLeg(0). armZ is 4 at planar 0.
    The still's hook sits level with the hand, so the rope elevation is 0.
    """
    tuck = _smooth(_inv(3.0, -10.0, 0.0))
    fwd = 0.5
    thigh_l = _lerp(_lerp(-30.0, 20.0, fwd), -6.0, tuck)
    thigh_r = _lerp(_lerp(20.0, -30.0, fwd), -6.0, tuck)
    knee_l = _lerp(_lerp(-8.0, -32.0, fwd), -94.0, tuck)
    knee_r = _lerp(_lerp(-32.0, -8.0, fwd), -90.0, tuck)
    pitch_l, pitch_r = -134.0, -126.0
    yaw_l, yaw_r = 8.0, -8.0
    elbow_l, elbow_r = -2.0, -4.0
    hip, spine, head = 36.0, 56.0, -20.0
    fix = 0.0 - (hip + spine)
    spine = spine + fix * 0.55
    hip = hip + fix * 0.45
    arm_z = 4.0
    set_zxy(arm, "UpperLeg_L", -thigh_l, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_L", -knee_l, 0.0, 0.0)
    set_zxy(arm, "UpperLeg_R", -thigh_r, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_R", -knee_r, 0.0, 0.0)
    set_zxy(arm, "UpperArm_L", pitch_l, yaw_l, arm_z)
    set_zxy(arm, "LowerArm_L", elbow_l, 0.0, 0.0)
    set_zxy(arm, "UpperArm_R", pitch_r, yaw_r, -arm_z)
    set_zxy(arm, "LowerArm_R", elbow_r, 0.0, 0.0)
    set_zxy(arm, "Hips", hip, 0.0, 0.0)
    set_zxy(arm, "Spine", spine, 0.0, 0.0)
    set_zxy(arm, "Head", -head, 0.0, 0.0)
    print(
        "RUNTIME grapple pull tuck", round(tuck, 4),
        "hip", round(hip, 2), "spine", round(spine, 2),
        "thigh", round(thigh_l, 2), round(thigh_r, 2),
        "knee", round(knee_l, 2), round(knee_r, 2),
    )


def _gait_leg(phase, weight):
    s = math.sin(phase)
    c = math.cos(phase)
    front = _lerp(26.0, 48.0, weight)
    back = _lerp(14.0, 30.0, weight)
    thigh = s * front if s >= 0.0 else s * back
    if c <= 0.0:
        knee = -5.0
        foot = -(thigh + knee)
    else:
        knee = -(4.0 + c * _lerp(48.0, 90.0, weight))
        foot = 0.0
    return thigh, knee, foot


def pose_run(arm):
    """Grounded run at GaitBlend.RunSpeed 9, phase 2 rad, breath 0, no turn, no look.

    Legs are GaitBlend.At. Arms are the grounded block: LocoFeel.ArmPitch,
    the outward yaw, and the reach elbow. Spine is the settled cruise lean.
    """
    phase = 2.0
    speed = 9.0
    gait_sin = math.sin(phase)
    weight = _pose_weight(speed)
    run_amt = _inv(5.5, 11.5, speed)
    gait = max(weight, run_amt)
    idle = 1.0 - gait
    arm_z = _lerp(4.0, 8.0, max(weight, run_amt))
    amp = _lerp(32.0, 46.0, _inv(6.9, 13.8, speed))
    pitch_l = -(-gait_sin) * amp - 12.0 * idle
    pitch_r = -(gait_sin) * amp - 12.0 * idle
    out_y = _lerp(12.0, 8.0, gait)
    reach_y = _lerp(out_y, out_y + 6.0, run_amt)
    y_l = _lerp(out_y, reach_y, max(0.0, -gait_sin) * gait)
    y_r = _lerp(out_y, reach_y, max(0.0, gait_sin) * gait)
    roll = _lerp(0.0, arm_z, gait)
    elbow_reach = _lerp(-10.0, -6.0, run_amt)
    elbow_pull = _lerp(-18.0, -30.0, run_amt)
    elbow_l = _lerp(elbow_reach, elbow_pull, max(0.0, gait_sin) * gait)
    elbow_r = _lerp(elbow_reach, elbow_pull, max(0.0, -gait_sin) * gait)
    tau = math.tau
    phase_r = phase + math.pi
    if phase_r >= tau:
        phase_r -= tau
    th_l, kn_l, ft_l = _gait_leg(phase, weight)
    th_r, kn_r, ft_r = _gait_leg(phase_r, weight)
    cruise = _inv(6.9, 13.8, speed) * 6.5
    set_zxy(arm, "UpperLeg_L", -th_l, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_L", -kn_l, 0.0, 0.0)
    set_zxy(arm, "Foot_L", -ft_l, 0.0, 0.0)
    set_zxy(arm, "UpperLeg_R", -th_r, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_R", -kn_r, 0.0, 0.0)
    set_zxy(arm, "Foot_R", -ft_r, 0.0, 0.0)
    set_zxy(arm, "UpperArm_L", pitch_l, y_l, roll)
    set_zxy(arm, "LowerArm_L", elbow_l, 0.0, 0.0)
    set_zxy(arm, "UpperArm_R", pitch_r, -y_r, -roll)
    set_zxy(arm, "LowerArm_R", elbow_r, 0.0, 0.0)
    set_zxy(arm, "Hips", 0.0, 0.0, 0.0)
    set_zxy(arm, "Spine", cruise, 0.0, 0.0)
    set_zxy(arm, "Head", 0.0, 0.0, 0.0)
    print(
        "RUNTIME run phase", round(phase, 3), "weight", round(weight, 4),
        "thigh", round(th_l, 2), round(th_r, 2),
        "knee", round(kn_l, 2), round(kn_r, 2),
        "pitch", round(pitch_l, 2), round(pitch_r, 2),
        "yaw", round(y_l, 2), round(-y_r, 2),
        "elbow", round(elbow_l, 2), round(elbow_r, 2),
        "cruise", round(cruise, 2), "armZ", round(arm_z, 2),
    )


def pose_gait(arm, speed, phase):
    """Grounded gait at one speed and cycle phase. Same keys as pose_run.

    Breath, turn, and look stay off. A footfall is phase π/2: the left thigh
    is forward and both cosines are 0, so both legs take the stance branch.
    """
    gait_sin = math.sin(phase)
    weight = _pose_weight(speed)
    run_amt = _inv(5.5, 11.5, speed)
    gait = max(weight, run_amt)
    idle = 1.0 - gait
    arm_z = _lerp(4.0, 8.0, max(weight, run_amt))
    amp = _lerp(32.0, 46.0, _inv(6.9, 13.8, speed))
    pitch_l = -(-gait_sin) * amp - 12.0 * idle
    pitch_r = -(gait_sin) * amp - 12.0 * idle
    out_y = _lerp(12.0, 8.0, gait)
    reach_y = _lerp(out_y, out_y + 6.0, run_amt)
    y_l = _lerp(out_y, reach_y, max(0.0, -gait_sin) * gait)
    y_r = _lerp(out_y, reach_y, max(0.0, gait_sin) * gait)
    roll = _lerp(0.0, arm_z, gait)
    elbow_reach = _lerp(-10.0, -6.0, run_amt)
    elbow_pull = _lerp(-18.0, -30.0, run_amt)
    elbow_l = _lerp(elbow_reach, elbow_pull, max(0.0, gait_sin) * gait)
    elbow_r = _lerp(elbow_reach, elbow_pull, max(0.0, -gait_sin) * gait)
    tau = math.tau
    phase_r = phase + math.pi
    if phase_r >= tau:
        phase_r -= tau
    th_l, kn_l, ft_l = _gait_leg(phase, weight)
    th_r, kn_r, ft_r = _gait_leg(phase_r, weight)
    cruise = _inv(6.9, 13.8, speed) * 6.5
    set_zxy(arm, "UpperLeg_L", -th_l, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_L", -kn_l, 0.0, 0.0)
    set_zxy(arm, "Foot_L", -ft_l, 0.0, 0.0)
    set_zxy(arm, "UpperLeg_R", -th_r, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_R", -kn_r, 0.0, 0.0)
    set_zxy(arm, "Foot_R", -ft_r, 0.0, 0.0)
    set_zxy(arm, "UpperArm_L", pitch_l, y_l, roll)
    set_zxy(arm, "LowerArm_L", elbow_l, 0.0, 0.0)
    set_zxy(arm, "UpperArm_R", pitch_r, -y_r, -roll)
    set_zxy(arm, "LowerArm_R", elbow_r, 0.0, 0.0)
    set_zxy(arm, "Hips", 0.0, 0.0, 0.0)
    set_zxy(arm, "Spine", cruise, 0.0, 0.0)
    set_zxy(arm, "Head", 0.0, 0.0, 0.0)
    print(
        "RUNTIME gait", round(speed, 2), "phase", round(phase, 3),
        "weight", round(weight, 4),
        "thigh", round(th_l, 2), round(th_r, 2),
        "knee", round(kn_l, 2), round(kn_r, 2),
        "pitch", round(pitch_l, 2), round(pitch_r, 2),
        "yaw", round(y_l, 2), round(-y_r, 2),
        "elbow", round(elbow_l, 2), round(elbow_r, 2),
        "cruise", round(cruise, 2), "armZ", round(arm_z, 2),
    )


def pose_stagger(arm):
    """PunchStaggerPose.Stumble at Weight 1 (age 0.10, inside the hold)."""
    set_zxy(arm, "UpperLeg_L", -38.0, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_L", -(-34.0), 0.0, 0.0)
    set_zxy(arm, "UpperLeg_R", -(-16.0), 0.0, 0.0)
    set_zxy(arm, "LowerLeg_R", -(-8.0), 0.0, 0.0)
    set_zxy(arm, "UpperArm_L", 46.0, 28.0, 14.0)
    set_zxy(arm, "LowerArm_L", -30.0, 0.0, 0.0)
    set_zxy(arm, "UpperArm_R", 34.0, -26.0, -12.0)
    set_zxy(arm, "LowerArm_R", -24.0, 0.0, 0.0)
    set_zxy(arm, "Hips", -18.0, 8.0, 0.0)
    set_zxy(arm, "Spine", -16.0, -6.0, 0.0)
    set_zxy(arm, "Head", -(-24.0), 4.0, 0.0)
    print("RUNTIME stagger Stumble weight 1")


def pose_launch(arm):
    """LaunchPose.At(24.7) after PadOpen has arrived. Windmill(time=0) is 0.

    ApplyLaunchPose negates the right arm yaw. Knees stay on the rise sample
    because OpenAmount(24.7) is 0.
    """
    set_zxy(arm, "UpperLeg_L", -42.0, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_L", -(-52.0), 0.0, 0.0)
    set_zxy(arm, "UpperLeg_R", -38.0, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_R", -(-46.0), 0.0, 0.0)
    set_zxy(arm, "UpperArm_L", -155.0, 16.0, 0.0)
    set_zxy(arm, "LowerArm_L", -14.0, 0.0, 0.0)
    set_zxy(arm, "UpperArm_R", -155.0, -16.0, 0.0)
    set_zxy(arm, "LowerArm_R", -14.0, 0.0, 0.0)
    set_zxy(arm, "Hips", 6.0, 0.0, 0.0)
    set_zxy(arm, "Spine", -4.0, 0.0, 0.0)
    set_zxy(arm, "Head", -(-10.0), 0.0, 0.0)
    print("RUNTIME launch At(24.7) padOpen 1 windmill 0")


def wall_run_angles():
    """WallPose.Run(+1, wallLeft: true) at the locked 9.5 m/s gait.

    The lane branch has no wall-run animation clip. The pose the game plays
    is WallPose.RunCycle, phase 3π/2. #120 sets LeanZ to the 15° roll,
    the inner arm at pitch −32 / yaw −14 / elbow −48, and PlantRoll −36.
    """
    idle, sprint = 0.35, 12.0
    t = (9.5 - idle) / (sprint - idle)
    t = 0.0 if t < 0.0 else (1.0 if t > 1.0 else t)
    weight = 1.0 - (1.0 - t) * (1.0 - t)
    front = 26.0 + (48.0 - 26.0) * weight
    back = 14.0 + (30.0 - 14.0) * weight
    thigh_l = -back - 2.2
    thigh_r = front
    along = (thigh_r - (-22.0)) / (48.0 - (-22.0))
    along = 0.0 if along < 0.0 else (1.0 if along > 1.0 else along)
    return {
        "thigh_l": thigh_l,
        "knee_l": -5.0,
        "foot_l": 6.0,
        "thigh_r": thigh_r,
        "knee_r": -5.0,
        "foot_r": -(thigh_r + (-5.0)),
        "thigh_roll_l": -36.0,
        "thigh_roll_r": 0.0,
        "pitch_l": -32.0 + (-1.0) * 3.0,
        "yaw_l": -14.0,
        "elbow_l": -48.0,
        "pitch_r": -78.0 + (24.0 - (-78.0)) * along,
        "yaw_r": 16.0,
        "elbow_r": -12.0 + (-36.0 - (-12.0)) * along,
        "hip": 6.0,
        "spine": 12.0,
        "head": -4.0,
        "lean": -15.0,
        # ApplyWallSample writes this on top of the sample. gaitW at 9.5
        # dominates runVis, so the A-pose roll is Lerp(4, 8, PoseWeight).
        "arm_z": 4.0 + 4.0 * weight,
    }


def pose_wall(arm):
    """WallPose.Run(+1, wallLeft) through ApplyWallSample.

    No wall-run clip is on this lane. The game poses RunCycle at phase 3π/2.
    Thigh and knee X are flipped to this FBX (toes point −Y). LeanZ stays
    −20. The inner yaw stays on Euler Y. Elbows stay −18 and the outer blend.
    """
    a = wall_run_angles()
    z = a["arm_z"]
    set_zxy(arm, "UpperLeg_L", -a["thigh_l"], 0.0, a["thigh_roll_l"])
    set_zxy(arm, "LowerLeg_L", -a["knee_l"], 0.0, 0.0)
    set_zxy(arm, "Foot_L", -a["foot_l"], 0.0, 0.0)
    set_zxy(arm, "UpperLeg_R", -a["thigh_r"], 0.0, a["thigh_roll_r"])
    set_zxy(arm, "LowerLeg_R", -a["knee_r"], 0.0, 0.0)
    set_zxy(arm, "Foot_R", -a["foot_r"], 0.0, 0.0)
    set_zxy(arm, "UpperArm_L", a["pitch_l"], a["yaw_l"], z)
    set_zxy(arm, "LowerArm_L", a["elbow_l"], 0.0, 0.0)
    set_zxy(arm, "UpperArm_R", a["pitch_r"], a["yaw_r"], -z)
    set_zxy(arm, "LowerArm_R", a["elbow_r"], 0.0, 0.0)
    hip_z = -a["lean"] * 0.45
    head_z = -a["lean"] * 0.25
    set_zxy(arm, "Hips", a["hip"], 0.0, -hip_z)
    set_zxy(arm, "Spine", a["spine"], 0.0, -a["lean"])
    set_zxy(arm, "Head", -a["head"], 0.0, -head_z)
    print(
        "WALL_RUNTIME",
        "phase", "3pi/2",
        "thigh", round(a["thigh_l"], 1), round(a["thigh_r"], 1),
        "knee", round(a["knee_l"], 1), round(a["knee_r"], 1),
        "pitch", round(a["pitch_l"], 1), round(a["pitch_r"], 1),
        "yaw", round(a["yaw_l"], 1), round(a["yaw_r"], 1),
        "elbow", round(a["elbow_l"], 1), round(a["elbow_r"], 1),
        "hip", a["hip"], "spine", a["spine"],         "lean", a["lean"],
        "armZ", round(z, 2),
        "thighRoll", a["thigh_roll_l"], a["thigh_roll_r"],
    )


def pose_punch(arm):
    """VerbPoseClips.PunchStrikePose at sample 1. The right arm is the strike."""
    set_zxy(arm, "UpperLeg_L", -20.0, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_L", -(-8.0), 0.0, 0.0)
    set_zxy(arm, "UpperLeg_R", -(-18.0), 0.0, 0.0)
    set_zxy(arm, "LowerLeg_R", -(-6.0), 0.0, 0.0)
    set_zxy(arm, "UpperArm_L", 84.0, -18.0, 10.0)
    set_zxy(arm, "LowerArm_L", -36.0, 0.0, 0.0)
    set_zxy(arm, "UpperArm_R", -74.0, 4.0, -10.0)
    set_zxy(arm, "LowerArm_R", -4.0, 0.0, 0.0)
    set_zxy(arm, "Hips", 10.0, 36.0, 0.0)
    set_zxy(arm, "Spine", 6.0, 52.0, 0.0)
    set_zxy(arm, "Head", -(-6.0), 18.0, 0.0)
    print("RUNTIME punch strike sample 1")


def look_at(obj, target):
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def tint():
    for mat in bpy.data.materials:
        if not mat.use_nodes:
            continue
        node = mat.node_tree.nodes.get("Principled BSDF")
        if node is None:
            continue
        name = mat.name
        if name in ("Joint_Tan", "Sensor_Tan"):
            color = JOINT
        elif name == "Accent" or name.startswith("Cal"):
            color = ACCENT
        else:
            color = PLAYER
        node.inputs["Base Color"].default_value = color
        if "Roughness" in node.inputs:
            node.inputs["Roughness"].default_value = 0.48


def make_mat(name, color, rough=0.55, alpha=1.0, emit=0.0):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
    node = mat.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = color
    if "Roughness" in node.inputs:
        node.inputs["Roughness"].default_value = rough
    if "Alpha" in node.inputs:
        node.inputs["Alpha"].default_value = alpha
    if emit > 0.0 and "Emission Strength" in node.inputs:
        node.inputs["Emission Strength"].default_value = emit
        key = "Emission Color" if "Emission Color" in node.inputs else None
        if key:
            node.inputs[key].default_value = color
    if alpha < 0.999:
        mat.blend_method = "BLEND"
        if hasattr(mat, "shadow_method"):
            mat.shadow_method = "NONE"
    return mat


def body_meshes():
    return [obj for obj in bpy.data.objects if obj.type == "MESH" and obj.name.startswith("Mesh_")]


def clear_fx():
    keep = {"PropGround", "PropSlab", "PropSeam"}
    for obj in list(bpy.data.objects):
        if obj.name in keep:
            continue
        if obj.name.startswith("Fx") or obj.name.startswith("Prop"):
            bpy.data.objects.remove(obj, do_unlink=True)


def mesh_world(obj):
    deps = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(deps)
    me = ev.to_mesh()
    mw = ev.matrix_world
    verts = [mw @ v.co for v in me.vertices]
    polys = [tuple(p.vertices) for p in me.polygons]
    ev.to_mesh_clear()
    return verts, polys


def bone_of(obj):
    if obj.parent_bone:
        return obj.parent_bone
    if obj.name.startswith("Mesh_"):
        return obj.name[5:]
    return obj.name


def neighbours(arm):
    pairs = set()
    for bone in arm.data.bones:
        if bone.parent is None:
            continue
        pairs.add((bone.name, bone.parent.name))
        pairs.add((bone.parent.name, bone.name))
    return pairs


def joint_world(arm, a, b):
    child = b if arm.data.bones[b].parent and arm.data.bones[b].parent.name == a else a
    if child not in arm.pose.bones:
        return None
    return arm.matrix_world @ arm.pose.bones[child].head


def inside_depth(tree, point):
    """Penetration of a closed mesh. The ray follows the nearest face so a
    turned character does not change the result."""
    loc, normal, index, dist = tree.find_nearest(point)
    if loc is None or normal is None or dist < 0.00005:
        return 0.0
    if (loc - point).dot(normal) <= 0.0:
        return 0.0
    inward = -normal.normalized()
    hit, hit_n, _idx, _dist = tree.ray_cast(point + inward * 0.0004, inward)
    if hit is None or hit_n is None:
        return 0.0
    if (hit - point).dot(hit_n) <= 0.0:
        return 0.0
    return dist


def overlap_ids(tree_a, polys_a, tree_b):
    overlap = tree_a.overlap(tree_b)
    used = set()
    if not overlap:
        return used
    for ia, _ib in overlap:
        if ia < len(polys_a):
            used.update(polys_a[ia])
    return used


def pair_depth(tree_a, verts_a, polys_a, tree_b, joint):
    """Deepest penetration. Joined verts within 3 cm of the joint are exempt."""
    used = overlap_ids(tree_a, polys_a, tree_b)
    worst = 0.0
    for idx in used:
        if idx >= len(verts_a):
            continue
        point = verts_a[idx]
        if joint is not None and (point - joint).length <= JOINT_EXEMPT:
            continue
        depth = inside_depth(tree_b, point)
        if depth > worst:
            worst = depth
    return worst


def pack_body():
    packed = []
    for obj in body_meshes():
        verts, polys = mesh_world(obj)
        if len(verts) < 4 or not polys:
            continue
        tree = BVHTree.FromPolygons(verts, polys, epsilon=0.0)
        packed.append((obj, bone_of(obj), tree, verts, polys))
    return packed


def each_pair(arm):
    """Joined pairs only. Yields (name, depth_m) using the 3 cm joint-head exemption."""
    packed = pack_body()
    joined = neighbours(arm)
    rows = []
    for i in range(len(packed)):
        obj_a, bone_a, tree_a, verts_a, polys_a = packed[i]
        for j in range(i + 1, len(packed)):
            obj_b, bone_b, tree_b, verts_b, polys_b = packed[j]
            if (bone_a, bone_b) not in joined:
                continue
            joint = joint_world(arm, bone_a, bone_b)
            d_ab = pair_depth(tree_a, verts_a, polys_a, tree_b, joint)
            d_ba = pair_depth(tree_b, verts_b, polys_b, tree_a, joint)
            depth = d_ab if d_ab > d_ba else d_ba
            rows.append((obj_a.name + "|" + obj_b.name, depth))
    rows.sort(key=lambda row: row[1], reverse=True)
    return rows


FAIL_NAMES = []


def measure(arm, solids):
    global FAIL_NAMES, FAIL_DEPTH
    bpy.context.view_layer.update()
    packed = pack_body()
    joined = neighbours(arm)
    self_max = 0.0
    world_max = 0.0
    fails = 0
    worst_pair = ""
    FAIL_NAMES = []
    FAIL_DEPTH = {}
    for i in range(len(packed)):
        obj_a, bone_a, tree_a, verts_a, polys_a = packed[i]
        for obj_s, tree_s in solids:
            depth = pair_depth(tree_a, verts_a, polys_a, tree_s, None)
            if depth > world_max:
                world_max = depth
                worst_pair = obj_a.name + ">" + obj_s.name
            if depth > DEPTH_LIMIT:
                fails += 1
                world_name = obj_a.name + ">" + obj_s.name
                FAIL_NAMES.append(world_name)
                FAIL_DEPTH[world_name] = depth
                if os.environ.get("FX_PAIRS") == "1":
                    print("WORLD", obj_a.name, obj_s.name, round(depth * 100.0, 2))
        for j in range(i + 1, len(packed)):
            obj_b, bone_b, tree_b, verts_b, polys_b = packed[j]
            linked = (bone_a, bone_b) in joined
            joint = joint_world(arm, bone_a, bone_b) if linked else None
            d_ab = pair_depth(tree_a, verts_a, polys_a, tree_b, joint)
            d_ba = pair_depth(tree_b, verts_b, polys_b, tree_a, joint)
            depth = d_ab if d_ab > d_ba else d_ba
            if depth > self_max:
                self_max = depth
                worst_pair = obj_a.name + "|" + obj_b.name
            if depth > DEPTH_LIMIT:
                fails += 1
                pair_name = obj_a.name + "|" + obj_b.name
                FAIL_NAMES.append(pair_name)
                prev = FAIL_DEPTH.get(pair_name, 0.0)
                if depth > prev:
                    FAIL_DEPTH[pair_name] = depth
                if os.environ.get("FX_PAIRS") == "1":
                    print("PAIR", obj_a.name, obj_b.name, round(depth * 100.0, 2))
    return self_max, world_max, fails, worst_pair


def lowest(arm):
    low = 99.0
    for obj in body_meshes():
        verts, _polys = mesh_world(obj)
        for p in verts:
            if p.z < low:
                low = p.z
    return low


POSE_BANK = None


def apply_pose(arm, fn, lift=0.0, yaw=20.0):
    global POSE_BANK
    POSE_BANK = None
    clear_pose(arm)
    fn(arm)
    yaw_q = Euler((0.0, 0.0, rad(yaw)), "XYZ").to_quaternion()
    if POSE_BANK is not None:
        arm.rotation_mode = "QUATERNION"
        arm.rotation_quaternion = yaw_q @ POSE_BANK
    else:
        arm.rotation_mode = "XYZ"
        arm.rotation_euler = Euler((0.0, 0.0, rad(yaw)), "XYZ")
    POSE_BANK = None
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    low = lowest(arm)
    arm.location.z += -low + 0.006 + lift
    bpy.context.view_layer.update()


def bone_pos(arm, name, tail=False):
    bone = arm.pose.bones[name]
    return arm.matrix_world @ (bone.tail if tail else bone.head)


def extreme(obj, axis, mode):
    verts, _ = mesh_world(obj)
    vals = [getattr(v, axis) for v in verts]
    return min(vals) if mode == "min" else max(vals)


def add_plane(name, location, scale, color, alpha=1.0, emit=0.0, rot=None):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    if rot is not None:
        obj.rotation_euler = rot
    obj.data.materials.append(make_mat(name + "Mat", color, 0.6, alpha, emit))
    return obj


def add_ico(name, location, radius, color, alpha=0.8, emit=0.4):
    bpy.ops.mesh.primitive_ico_sphere_add(radius=radius, subdivisions=1, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(make_mat(name + "Mat", color, 0.4, alpha, emit))
    return obj


def add_torus(name, location, major, minor, color, alpha=0.85, emit=0.3):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major, minor_radius=minor, location=location, major_segments=48, minor_segments=8
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.data.materials.append(make_mat(name + "Mat", color, 0.35, alpha, emit))
    return obj


def add_curve(name, points, radius, color, alpha=0.7, emit=0.2):
    curve = bpy.data.curves.new(name, type="CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = 2
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for i, p in enumerate(points):
        spline.points[i].co = (p.x, p.y, p.z, 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(make_mat(name + "Mat", color, 0.4, alpha, emit))
    return obj


def soft_image():
    img = bpy.data.images.get("FxSoftDisc")
    if img is not None:
        return img
    n = 48
    img = bpy.data.images.new("FxSoftDisc", n, n, alpha=True, float_buffer=True)
    pix = [0.0] * (n * n * 4)
    for y in range(n):
        for x in range(n):
            dx = (x + 0.5) / n - 0.5
            dy = (y + 0.5) / n - 0.5
            r = math.sqrt(dx * dx + dy * dy) * 2.0
            a = max(0.0, 1.0 - r)
            a = a * a
            i = (y * n + x) * 4
            pix[i] = 1.0
            pix[i + 1] = 1.0
            pix[i + 2] = 1.0
            pix[i + 3] = a
    img.pixels.foreach_set(pix)
    img.pack()
    return img


def soft_mat(name, color, strength=0.35):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = soft_image()
    transparent = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    emit.inputs["Color"].default_value = color
    emit.inputs["Strength"].default_value = strength
    nt.links.new(tex.outputs["Alpha"], mix.inputs["Fac"])
    nt.links.new(transparent.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def add_puff(name, location, size, color, strength=0.4):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size, size, size)
    obj.data.materials.append(soft_mat(name + "Mat", color, strength))
    return obj


def cloud_mat(name, color):
    # Lit only. The soft disc fades the edge. No emission, so it cannot glow.
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = soft_image()
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    diff.inputs["Color"].default_value = color
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(tex.outputs["Alpha"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(diff.outputs["BSDF"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def add_cloud(name, location, size, color, squash=0.72):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size, size * squash, size)
    obj.data.materials.append(cloud_mat(name + "Mat", color))
    return obj


def dust_puffs(origin, count, reach, size, color):
    # Three to five soft clouds, low, overlapping at the contact and spreading out.
    count = 3 if count < 3 else (5 if count > 5 else count)
    for i in range(count):
        ang = -0.55 + (1.1 * i / max(count - 1, 1))
        radial = 0.06 + reach * (0.16 + 0.10 * (i % 2))
        height = 0.035 + 0.028 * (i % 3)
        puff = size * (0.88 + 0.18 * (i % 2))
        pos = origin + Vector((math.cos(ang) * radial, math.sin(ang) * radial, height))
        add_cloud("FxPuff%d" % i, pos, puff, color, 0.62 + 0.12 * (i % 3))


def shockwave(origin, radius):
    add_torus("FxShock", origin + Vector((0, 0, 0.03)), radius, 0.022, SHOCK, 0.7, 0.55)
    add_torus("FxDustRing", origin + Vector((0, 0, 0.02)), radius * 0.72, 0.028, DUST, 0.55, 0.12)
    add_plane(
        "FxDisc",
        origin + Vector((0, 0, 0.012)),
        (radius * 1.15, radius * 1.15, 1),
        (DUST[0], DUST[1], DUST[2], 1),
        0.16,
        0.0,
    )


def star_object(name, location):
    mesh = bpy.data.meshes.new(name + "Mesh")
    verts = [(0.0, 0.0, 0.0)]
    for i in range(10):
        ang = i * math.pi / 5.0 - math.pi / 2.0
        radius = 0.10 if i % 2 == 0 else 0.04
        verts.append((math.cos(ang) * radius, math.sin(ang) * radius, 0.0))
    faces = [(0, 1 + i, 1 + (i + 1) % 10) for i in range(10)]
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.data.materials.append(glow_mat(name + "Mat", (1.0, 0.78, 0.08, 1.0), 3.2))
    return obj


def glow_mat(name, color, strength):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    if hasattr(mat, "blend_method"):
        mat.blend_method = "BLEND"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = color
    emit.inputs["Strength"].default_value = strength
    nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    return mat


def stars(head, count=5):
    # Vertical ring, shifted toward the camera, so all five clear the skull.
    for i in range(count):
        ang = math.pi * 0.5 + i / count * math.tau
        pos = head + Vector((math.cos(ang) * 0.30, -0.18, math.sin(ang) * 0.26))
        star_object("FxStar%d" % i, pos)


def fresnel_shell():
    mat = bpy.data.materials.get("FxRimShell")
    if mat is not None:
        return mat
    mat = bpy.data.materials.new("FxRimShell")
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    fres = nt.nodes.new("ShaderNodeFresnel")
    transparent = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    if hasattr(mat, "use_backface_culling"):
        mat.use_backface_culling = True
    fres.inputs["IOR"].default_value = 1.55
    emit.inputs["Color"].default_value = RIM
    emit.inputs["Strength"].default_value = 3.2
    nt.links.new(fres.outputs["Fac"], mix.inputs["Fac"])
    nt.links.new(transparent.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def add_shell():
    mat = fresnel_shell()
    for obj in body_meshes():
        shell = obj.copy()
        shell.data = obj.data.copy()
        shell.name = "FxShell" + obj.name
        bpy.context.collection.objects.link(shell)
        shell.parent = obj.parent
        shell.parent_type = obj.parent_type
        shell.parent_bone = obj.parent_bone
        shell.matrix_parent_inverse = obj.matrix_parent_inverse.copy()
        for vert in shell.data.vertices:
            vert.co += vert.normal * 0.014
        shell.data.materials.clear()
        shell.data.materials.append(mat)


def launch_fx(origin):
    add_torus("FxLaunch", origin + Vector((0, 0, 0.72)), 0.72, 0.03, CYAN, 0.85, 1.15)
    for i in range(6):
        ang = i / 6 * math.tau
        a = origin + Vector((math.cos(ang) * 0.28, math.sin(ang) * 0.28, 0.06))
        b = a + Vector((math.cos(ang) * 0.05, math.sin(ang) * 0.05, 1.15 + (i % 2) * 0.32))
        add_curve("FxStreak%d" % i, [a, b], 0.016, CYAN, 0.9, 0.85)


def kit_shimmer(t, tension, time):
    """FxKitLook.Shimmer. The proof sample at (0.5, 1, 0.4) stays 1.8 cm."""
    t = _clamp(t, 0.0, 1.0)
    tension = _clamp(tension, 0.0, 1.0)
    bell = 4.0 * t * (1.0 - t)
    amp = 0.028 * (0.35 + 0.65 * tension)
    return amp * bell * math.sin(time * 14.0 + t * 9.0)


# Peak of Shimmer is ShimmerAmp (2.8 cm). Drawn peak is 0.9 cm.
ROPE_SLACK_SCALE = 0.009 / 0.028


def rope_shimmer(hand, anchor):
    delta = anchor - hand
    dist = delta.length
    if dist < 0.05:
        return
    direction = delta / dist
    side = direction.cross(Vector((0, 0, 1)))
    if side.length < 0.001:
        side = Vector((1, 0, 0))
    side.normalize()
    pts = []
    peak = 0.0
    for i in range(8):
        t = i / 7.0
        wob = kit_shimmer(t, 1.0, 0.4) * ROPE_SLACK_SCALE
        peak = max(peak, abs(wob))
        pts.append(hand + direction * (dist * t) + side * wob)
    # Width stays 1.8 cm across. The sag is the scaled shimmer.
    add_curve("FxRope", pts, 0.009, ROPE, 1.0, 0.0)
    print("ROPE slack_cm", round(peak * 100.0, 2), "width_cm", 1.8, "peak_cap_cm", round(0.028 * ROPE_SLACK_SCALE * 100.0, 2))


def chip(name, location, size, color):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size, size * 0.45, size * 0.7)
    obj.rotation_euler = Euler((rad(20), rad(35), rad(15)), "XYZ")
    obj.data.materials.append(make_mat(name + "Mat", color, 0.55, 1.0, 0.05))
    return obj


def hook_fx(anchor, toward):
    # Burst sits in front of the post face, on the runner's side of the hit.
    away = toward.normalized() if toward.length > 0.001 else Vector((0.0, -1.0, 0.0))
    core = anchor + away * 0.08 + Vector((0.0, 0.0, 0.08))
    gold = (1.0, 0.62, 0.08, 1.0)
    add_puff("FxPuffSparkCore", core, 0.34, gold, 2.0)
    for i in range(10):
        ang = i / 10.0 * math.tau
        spread = 0.12 + (i % 3) * 0.07
        pos = core + away * (0.05 + (i % 3) * 0.04) + Vector((math.cos(ang) * spread, math.sin(ang) * spread * 0.45, 0.04 + (i % 4) * 0.06))
        add_puff("FxPuffSpark%d" % i, pos, 0.14 + (i % 3) * 0.05, gold, 1.6)
    for i in range(6):
        ang = i / 6.0 * math.tau + 0.4
        pos = core + away * 0.14 + Vector((math.cos(ang) * 0.22, math.sin(ang) * 0.12, (i % 3) * 0.05))
        chip("FxChip%d" % i, pos, 0.06 + (i % 3) * 0.02, (0.45, 0.32, 0.16, 1))


def scuff_mat():
    mat = bpy.data.materials.get("FxScuffDecal")
    if mat is not None:
        return mat
    mat = bpy.data.materials.new("FxScuffDecal")
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = soft_image()
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    diff.inputs["Color"].default_value = (0.16, 0.11, 0.07, 1.0)
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(tex.outputs["Alpha"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(diff.outputs["BSDF"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def wall_cluster(name, contact, normal, count, size):
    # Scuff speed is the 9.5 m/s wall run, smaller than the roll burst.
    trail = -WALL_TRAVEL if WALL_TRAVEL.length > 0.5 else Vector((-1.0, 0.0, 0.0))
    bitangent = trail.cross(normal)
    if bitangent.length < 0.2:
        bitangent = Vector((0.0, 0.0, 1.0))
    bitangent.normalize()
    on_face = contact - normal * 0.01
    for i in range(count):
        along = (i - (count - 1) * 0.5) * 0.07
        side = ((i % 3) - 1) * 0.045
        pos = on_face + trail * along + bitangent * side
        pos += normal * (-0.004 * (i % 2))
        obj = add_cloud("%s%d" % (name, i), pos, size * (0.9 + 0.15 * (i % 2)), DUST, 0.62)
        obj.rotation_euler = normal.to_track_quat("-Z", "Y").to_euler()


def ground_mat():
    mat = bpy.data.materials.new("GroundMat")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 22.0
    noise.inputs["Detail"].default_value = 6.0
    noise.inputs["Roughness"].default_value = 0.65
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35
    ramp.color_ramp.elements[0].color = (0.30, 0.31, 0.32, 1.0)
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = (0.50, 0.51, 0.52, 1.0)
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = 0.92
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def setup_world(arm):
    tint()
    bpy.ops.mesh.primitive_plane_add(size=16.0, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.name = "PropGround"
    ground.data.materials.append(ground_mat())
    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = SKY
    bg.inputs["Strength"].default_value = 0.42
    bpy.ops.object.light_add(type="SUN", location=(4.2, -3.2, 7.5))
    sun = bpy.context.active_object
    sun.data.energy = 1.35
    sun.data.color = (1.0, 0.97, 0.92)
    sun.data.use_shadow = True
    look_at(sun, Vector((0, 0, 0.8)))
    bpy.ops.object.light_add(type="AREA", location=(-2.6, -2.2, 3.4))
    fill = bpy.context.active_object
    fill.data.energy = 16
    fill.data.size = 3.2
    look_at(fill, Vector((0, 0, 1.1)))
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, -0.25))
    slab = bpy.context.active_object
    slab.name = "PropSlab"
    slab.scale = (14, 14, 0.5)
    slab.hide_render = True
    bpy.ops.object.camera_add(location=(2.4, -3.6, 1.55))
    cam = bpy.context.active_object
    cam.data.lens = 48
    look_at(cam, Vector((0.0, 0.1, 1.05)))
    bpy.context.scene.camera = cam
    arm.hide_render = True
    scene = bpy.context.scene
    for i in range(-3, 4):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(i * 0.72, 0.0, 0.001))
        seam = bpy.context.active_object
        seam.name = "PropSeam"
        seam.scale = (0.012, 6.0, 0.002)
        seam.data.materials.append(make_mat("SeamMat", (0.18, 0.19, 0.16, 1), 0.9))
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.eevee.taa_render_samples = SAMPLES
    try:
        scene.eevee.use_bloom = False
    except AttributeError:
        pass
    scene.render.resolution_x = RES_X
    scene.render.resolution_y = RES_Y
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    try:
        scene.view_settings.view_transform = "Standard"
        scene.view_settings.look = "None"
        scene.view_settings.exposure = 0.0
    except (TypeError, AttributeError):
        pass
    return cam


def solid_of(obj):
    verts, polys = mesh_world(obj)
    tree = BVHTree.FromPolygons(verts, polys, epsilon=0.0)
    return obj, tree


def render_to(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    shrink(path)


def shrink(path, limit=400 * 1024):
    if os.path.getsize(path) <= limit:
        print("SIZE", os.path.basename(path), os.path.getsize(path))
        return
    from PIL import Image

    im = Image.open(path).convert("RGB")
    for colors in (220, 180, 140, 110):
        q = im.quantize(colors=colors, method=Image.Quantize.MEDIANCUT)
        q.save(path, optimize=True)
        if os.path.getsize(path) <= limit:
            print("SIZE", os.path.basename(path), os.path.getsize(path), "colors", colors)
            return
    im = im.resize((640, 360), Image.Resampling.LANCZOS)
    im.save(path, optimize=True)
    print("SIZE", os.path.basename(path), os.path.getsize(path), "resized")


def bounds_of():
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    found = False
    skip = {"PropGround", "PropSlab", "PropSeam"}
    for obj in bpy.data.objects:
        if obj.hide_render or obj.name in skip:
            continue
        take = obj.name.startswith("Mesh_") or obj.name.startswith("Fx") or obj.name.startswith("Prop")
        if not take:
            continue
        for corner in obj.bound_box:
            p = obj.matrix_world @ Vector(corner)
            found = True
            mn.x = min(mn.x, p.x)
            mn.y = min(mn.y, p.y)
            mn.z = min(mn.z, p.z)
            mx.x = max(mx.x, p.x)
            mx.y = max(mx.y, p.y)
            mx.z = max(mx.z, p.z)
    if not found:
        return Vector((0, 0, 0)), Vector((1, 1, 2))
    return mn, mx


def aim_billboards(origin):
    for obj in bpy.data.objects:
        if not (obj.name.startswith("FxPuff") or obj.name.startswith("FxStar")):
            continue
        if "Scuff" in obj.name:
            continue
        direction = origin - obj.location
        if direction.length < 0.001:
            continue
        obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()


def body_bounds():
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    for obj in body_meshes():
        for corner in obj.bound_box:
            p = obj.matrix_world @ Vector(corner)
            mn.x = min(mn.x, p.x)
            mn.y = min(mn.y, p.y)
            mn.z = min(mn.z, p.z)
            mx.x = max(mx.x, p.x)
            mx.y = max(mx.y, p.y)
            mx.z = max(mx.z, p.z)
    return mn, mx


def body_frame_fraction(cam):
    from bpy_extras.object_utils import world_to_camera_view

    scene = bpy.context.scene
    ys = []
    for obj in body_meshes():
        for corner in obj.bound_box:
            co = world_to_camera_view(scene, cam, obj.matrix_world @ Vector(corner))
            if co.z > 0:
                ys.append(co.y)
    if len(ys) < 2:
        return 0.0
    return max(ys) - min(ys)


def point_in_frame(cam, point, margin=0.03):
    from bpy_extras.object_utils import world_to_camera_view

    co = world_to_camera_view(bpy.context.scene, cam, point)
    if co.z <= 0.05:
        return False
    return margin < co.x < 1.0 - margin and margin < co.y < 1.0 - margin


def fx_points(skip):
    pts = []
    for obj in bpy.data.objects:
        if obj.hide_render or obj.name in skip:
            continue
        if not (obj.name.startswith("Fx") or obj.name.startswith("Prop")):
            continue
        if obj.name in ("PropGround", "PropSlab", "PropSeam", "PropWall", "PropPost"):
            continue
        for corner in obj.bound_box:
            pts.append(obj.matrix_world @ Vector(corner))
    return pts


def frame_camera(cam, view=None, fraction=0.66):
    """Put the runner at about 65% of frame height, then back up only if an effect is clipped."""
    if view is None:
        view = Vector((0.55, -0.84, 0.24))
    view = view.normalized()
    cam.data.type = "PERSP"
    lens = 35.0
    cam.data.lens = lens
    aspect = RES_Y / float(RES_X)
    tan_v = (18.0 / lens) * aspect
    mn, mx = body_bounds()
    height = max(mx.z - mn.z, 1.45)
    center = (mn + mx) * 0.5
    dist = (height / fraction) * 0.5 / tan_v
    cam.data.clip_start = 0.04
    cam.data.clip_end = 80.0
    for _ in range(10):
        cam.location = center + view * dist
        look_at(cam, center)
        bpy.context.view_layer.update()
        aim_billboards(cam.location)
        outside = [p for p in fx_points(set()) if not point_in_frame(cam, p, 0.04)]
        frac = body_frame_fraction(cam)
        if not outside or frac <= 0.60:
            break
        dist *= 1.04
    bpy.context.view_layer.update()
    aim_billboards(cam.location)
    print("FRAME", round(body_frame_fraction(cam), 3), "dist", round(dist, 2))


def shot(arm, cam, name, pose, yaw, lift, build, frame=None):
    clear_fx()
    apply_pose(arm, pose, lift, yaw)
    slab = bpy.data.objects.get("PropSlab")
    bpy.context.view_layer.update()
    solids = [solid_of(slab)] if slab is not None else []
    build(arm, solids)
    self_max, world_max, fails, pair = measure(arm, solids)
    print(
        "NOCLIP",
        name,
        "self_cm", round(self_max * 100, 2),
        "world_cm", round(world_max * 100, 2),
        "fails", fails,
        "pair", pair,
    )
    note_added(name)
    if not MEASURE_ONLY:
        if frame is None:
            frame_camera(cam)
        else:
            frame(cam, arm)
            print("FRAME", name, round(body_frame_fraction(cam), 3))
        screen_box(cam)
        path = os.path.join(OUT, name + ".png")
        render_to(path)
    return self_max, world_max, fails


def ndc_box(cam):
    from bpy_extras.object_utils import world_to_camera_view

    scene = bpy.context.scene
    xs = []
    ys = []
    for obj in body_meshes():
        for corner in obj.bound_box:
            co = world_to_camera_view(scene, cam, obj.matrix_world @ Vector(corner))
            if co.z > 0:
                xs.append(co.x)
                ys.append(co.y)
    if not xs:
        return (0.0, 0.0, 0.0, 0.0)
    return (min(xs), max(xs), min(ys), max(ys))


def screen_box(cam):
    from bpy_extras.object_utils import world_to_camera_view

    scene = bpy.context.scene
    xs = []
    ys = []
    for obj in body_meshes():
        for corner in obj.bound_box:
            co = world_to_camera_view(scene, cam, obj.matrix_world @ Vector(corner))
            if co.z > 0:
                xs.append(co.x)
                ys.append(co.y)
    if not xs:
        return
    print(
        "SCREEN",
        "x", round(min(xs), 3), round(max(xs), 3),
        "y", round(min(ys), 3), round(max(ys), 3),
    )


def build_land(arm, solids):
    origin = Vector((arm.location.x, arm.location.y, 0.02))
    shockwave(origin, 0.72)
    dust_puffs(origin, 4, 0.70, 0.48, DUST)
    return solids


FX_FOCUS = Vector((0.0, 0.0, 0.05))


def iter_body_verts():
    for obj in body_meshes():
        verts, _polys = mesh_world(obj)
        for v in verts:
            yield obj.name, v


def dump_bones(arm, label):
    for name in ("Head", "Shoulder_L", "Shoulder_R", "Hips", "Hand_L", "Hand_R", "Foot_L", "Foot_R"):
        p = bone_pos(arm, name, tail=(name == "Head"))
        print(label, name, round(p.x, 3), round(p.y, 3), round(p.z, 3))


def build_roll(arm, solids):
    global FX_FOCUS
    dump_bones(arm, "ROLL_BONE")
    band = []
    low = None
    for name, v in iter_body_verts():
        if low is None or v.z < low:
            low = v.z
        band.append((name, v))
    touch = [(name, v) for name, v in band if v.z <= low + 0.025]
    if not touch:
        touch = [(band[0][0], Vector((0.0, 0.0, 0.02)))]
    acc = Vector((0.0, 0.0, 0.0))
    names = {}
    for name, v in touch:
        acc += v
        names[name] = names.get(name, 0) + 1
    contact = acc / len(touch)
    contact.z = 0.02
    FX_FOCUS = contact.copy()
    # Roll burst is the heavier impact: five puffs, wider than a hard land.
    dust_puffs(contact, 5, 0.95, 0.62, DUST)
    print(
        "ROLL_CONTACT",
        "low_z", round(low, 3),
        "n", len(touch),
        "at", round(contact.x, 3), round(contact.y, 3),
        "meshes", ",".join("%s:%d" % (k, names[k]) for k in sorted(names)),
    )
    return solids


def build_grapple(arm, solids):
    hand = bone_pos(arm, "Hand_L")
    shoulder = bone_pos(arm, "Shoulder_L")
    flat = Vector((hand.x - shoulder.x, hand.y - shoulder.y, 0.0))
    if flat.length < 0.15:
        flat = Vector((0.35, -0.94, 0.0))
    flat.normalize()
    # The hook face is 5 m out along the reach. A narrow post sits behind that face.
    hit = hand + flat * 5.0
    hit.z = hand.z
    if abs(flat.y) >= abs(flat.x):
        sign = 1.0 if flat.y >= 0.0 else -1.0
        center = Vector((hit.x, hit.y + sign * 0.17, 1.20))
        scale = (0.42, 0.34, 2.35)
    else:
        sign = 1.0 if flat.x >= 0.0 else -1.0
        center = Vector((hit.x + sign * 0.17, hit.y, 1.20))
        scale = (0.34, 0.42, 2.35)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    post = bpy.context.active_object
    post.name = "PropPost"
    post.scale = scale
    post.data.materials.append(make_mat("PostMat", (0.38, 0.40, 0.43, 1), 0.75))
    bpy.context.view_layer.update()
    solids.append(solid_of(post))
    empty = bpy.data.objects.new("PropHook", None)
    empty.location = hit
    empty.empty_display_size = 0.05
    bpy.context.collection.objects.link(empty)
    rope_shimmer(hand, hit)
    hook_fx(hit, -flat)
    print("GRAPPLE", "dist_m", round((hit - hand).length, 2))
    return solids


def build_immune(arm, solids):
    add_shell()
    return solids


def build_stagger(arm, solids):
    neck = bone_pos(arm, "Head", tail=False)
    crown = bone_pos(arm, "Head", tail=True)
    stars((neck + crown) * 0.5)
    return solids


def build_launch(arm, solids):
    pad_z = 0.04
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, pad_z * 0.5))
    pad = bpy.context.active_object
    pad.name = "PropPad"
    pad.scale = (1.1, 1.1, pad_z)
    pad.data.materials.append(make_mat("PadMat", PAD, 0.45, 1, 0.4))
    bpy.context.view_layer.update()
    solids.append(solid_of(pad))
    launch_fx(Vector((0, 0, pad_z)))
    return solids


def body_extent(axis, mode):
    val = None
    for obj in body_meshes():
        hit = extreme(obj, axis, mode)
        if val is None:
            val = hit
        elif mode == "max" and hit > val:
            val = hit
        elif mode == "min" and hit < val:
            val = hit
    return 0.0 if val is None else val


WALL_NORMAL = Vector((0.0, -1.0, 0.0))
WALL_TRAVEL = Vector((1.0, 0.0, 0.0))


def build_wall(arm, solids):
    # Upright wall run. The left foot is the plant. The wall sits 2 mm past it.
    global WALL_NORMAL, WALL_TRAVEL
    bpy.context.view_layer.update()
    low = lowest(arm)
    arm.location.z += 0.55 - low
    bpy.context.view_layer.update()
    foot = bpy.data.objects.get("Mesh_Foot_L")
    verts, _polys = mesh_world(foot)
    # Rest pose: toes −Y, character left +X. The armature yaw is the facing.
    # A stride does not move the wall; WallLeft keeps it on that side.
    rot = arm.matrix_world.to_quaternion()
    normal = rot @ Vector((1.0, 0.0, 0.0))
    normal.z = 0.0
    if normal.length < 0.2:
        normal = Vector((0.0, 1.0, 0.0))
    normal.normalize()
    travel = rot @ Vector((0.0, -1.0, 0.0))
    travel.z = 0.0
    if travel.length < 0.2:
        travel = Vector((1.0, 0.0, 0.0))
    travel.normalize()
    face = max(v.x * normal.x + v.y * normal.y for v in verts) + 0.002
    center = normal * (face + 0.16)
    center.z = 1.35
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=center)
    wall = bpy.context.active_object
    wall.name = "PropWall"
    wall.rotation_euler = Vector((normal.x, normal.y, 0.0)).to_track_quat("Y", "Z").to_euler()
    wall.scale = (3.2, 0.32, 2.8)
    wall.data.materials.append(make_mat("WallMat", (0.62, 0.64, 0.68, 1), 0.78))
    bpy.context.view_layer.update()
    solids.append(solid_of(wall))
    WALL_NORMAL = normal
    WALL_TRAVEL = travel
    planted = max(verts, key=lambda v: v.x * normal.x + v.y * normal.y)
    # The wall sits on the planted foot, which is where the game emits scuff.
    # Any other mesh through that plane is a real contact too.
    groups = {}
    for name, v in iter_body_verts():
        depth = v.x * normal.x + v.y * normal.y - (face - 0.002)
        if depth < -0.008:
            continue
        acc, n, deep = groups.get(name, (Vector((0.0, 0.0, 0.0)), 0, -1.0))
        groups[name] = (acc + v, n + 1, deep if deep > depth else depth)
    ranked = sorted(groups.items(), key=lambda item: item[1][2], reverse=True)
    contacts = []
    for name, (acc, n, deep) in ranked:
        center = acc / n
        center = center - normal * (center.x * normal.x + center.y * normal.y - (face - 0.002))
        contacts.append((name, center, deep))
        print("WALL_CONTACT", name, "n", n, "depth_cm", round(deep * 100.0, 2),
              "at", round(center.x, 3), round(center.y, 3), round(center.z, 3))
    if not contacts:
        contacts = [("Mesh_Foot_L", planted, 0.0)]
    # One cluster on the deepest contact, one on the foot when that is a
    # different mesh. Five puffs total, sized under the roll burst.
    primary = contacts[0]
    wall_cluster("FxPuffHit", primary[1], normal, 3, 0.42)
    foot_hit = next((c for c in contacts if "Foot" in c[0]), None)
    if foot_hit is not None and foot_hit[0] != primary[0]:
        wall_cluster("FxPuffFoot", foot_hit[1], normal, 2, 0.28)
    elif len(contacts) > 1:
        wall_cluster("FxPuffFoot", contacts[1][1], normal, 2, 0.26)
    else:
        wall_cluster("FxPuffFoot", primary[1], normal, 2, 0.28)
    global FX_FOCUS
    FX_FOCUS = primary[1].copy()
    dump_bones(arm, "WALL_BONE")
    head = bone_pos(arm, "Head", tail=True)
    hips = bone_pos(arm, "Hips")
    foot_r = bone_pos(arm, "Foot_R")
    foot_l = bone_pos(arm, "Foot_L")
    hand_l = bone_pos(arm, "Hand_L")
    up = head - hips
    print(
        "WALL_POSE",
        "head", round(head.x, 2), round(head.y, 2), round(head.z, 2),
        "hips", round(hips.x, 2), round(hips.y, 2), round(hips.z, 2),
        "footL", round(foot_l.x, 2), round(foot_l.y, 2), round(foot_l.z, 2),
        "footR", round(foot_r.x, 2), round(foot_r.y, 2), round(foot_r.z, 2),
        "handL", round(hand_l.x, 2), round(hand_l.y, 2), round(hand_l.z, 2),
        "up", round(up.x, 2), round(up.y, 2), round(up.z, 2),
        "normal", round(normal.x, 2), round(normal.y, 2),
    )
    return solids


def frame_on_contact(cam, view, dist, lens):
    focus = FX_FOCUS.copy()
    cam.data.type = "PERSP"
    cam.data.lens = lens
    cam.data.clip_start = 0.02
    cam.data.clip_end = 30.0
    cam.location = focus + view.normalized() * dist
    look_at(cam, focus)
    bpy.context.view_layer.update()
    aim_billboards(cam.location)
    from bpy_extras.object_utils import world_to_camera_view

    scene = bpy.context.scene
    for obj in bpy.data.objects:
        if not obj.name.startswith("FxPuff"):
            continue
        co = world_to_camera_view(scene, cam, obj.location)
        print("PUFF", obj.name, round(co.x, 3), round(co.y, 3), round(obj.scale.x, 3))
    print("FRAME", round(body_frame_fraction(cam), 3), "dist", round(dist, 2), "lens", lens)


def frame_roll_contact(cam, arm):
    # Low and close on the foot. The torso crops; the puffs stay in frame.
    view = Vector((0.22, -0.90, 0.16))
    frame_on_contact(cam, view, 1.05, 28.0)


def frame_wall_side(cam, arm):
    # Graze the face so the scuff sits on the wall and the body stays to the side.
    view = -WALL_NORMAL * 0.46 + WALL_TRAVEL * 0.78 + Vector((0.0, 0.0, 0.24))
    frame_on_contact(cam, view, 1.05, 28.0)


def frame_wall_top(cam, arm):
    # Plan view tight on the contact. Screen up is the wall normal.
    normal = WALL_NORMAL
    travel = WALL_TRAVEL
    if travel.cross(normal).z < 0.0:
        travel = -travel
    center = FX_FOCUS.copy()
    cam.data.type = "ORTHO"
    cam.data.sensor_fit = "HORIZONTAL"
    cam.data.ortho_scale = 0.95
    cam.data.clip_start = 0.01
    cam.data.clip_end = 40.0
    cam.matrix_world = Matrix((
        (travel.x, normal.x, 0.0, center.x),
        (travel.y, normal.y, 0.0, center.y),
        (0.0, 0.0, 1.0, center.z + 8.0),
        (0.0, 0.0, 0.0, 1.0),
    ))
    bpy.context.view_layer.update()
    print("FRAME", "top", round(body_frame_fraction(cam), 3))


def frame_grapple_shoulder(cam, arm):
    # Behind the shoulder and off the rope axis, so the line leaves the
    # left hand and the post is not sitting on the chest. Feet stay in frame.
    hand = bone_pos(arm, "Hand_L")
    hip = bone_pos(arm, "Hips")
    hook = bpy.data.objects["PropHook"].location.copy()
    to_hook = Vector((hook.x - hip.x, hook.y - hip.y, 0.0))
    if to_hook.length < 0.2:
        to_hook = Vector((0.0, -1.0, 0.0))
    to_hook.normalize()
    side = Vector((to_hook.y, -to_hook.x, 0.0))
    # Stand off the rope, on the side away from the hand, so the line
    # crosses open ground instead of running through the chest.
    if (hand - hip).dot(side) > 0.0:
        side = -side
    mn, mx = body_bounds()
    height = max(mx.z - mn.z, 1.5)
    center = (mn + mx) * 0.5
    lens = 26.0
    cam.data.type = "PERSP"
    cam.data.lens = lens
    cam.data.clip_start = 0.05
    cam.data.clip_end = 50.0
    aspect = RES_Y / float(RES_X)
    tan_v = (18.0 / lens) * aspect
    dist = (height / 0.64) * 0.5 / tan_v
    view = (-to_hook * 0.48 + side * 0.78 + Vector((0.0, 0.0, 0.22))).normalized()
    aim = Vector((center.x * 0.7 + hook.x * 0.3, center.y * 0.7 + hook.y * 0.3, center.z - 0.05))
    for _ in range(8):
        cam.location = center + view * dist
        look_at(cam, aim)
        bpy.context.view_layer.update()
        aim_billboards(cam.location)
        box = ndc_box(cam)
        body_ok = box[0] > 0.03 and box[1] < 0.97 and box[2] > 0.04 and box[3] < 0.96
        hook_ok = point_in_frame(cam, hook, 0.05)
        hand_ok = point_in_frame(cam, hand, 0.03)
        if body_ok and hook_ok and hand_ok:
            break
        if box[2] < 0.04:
            aim.z -= 0.12
        elif not body_ok:
            dist *= 1.08
        elif not hook_ok or not hand_ok:
            aim.x = aim.x * 0.8 + hook.x * 0.2
            aim.y = aim.y * 0.8 + hook.y * 0.2
            dist *= 1.05
    from bpy_extras.object_utils import world_to_camera_view

    hook_ndc = world_to_camera_view(bpy.context.scene, cam, hook)
    hand_ndc = world_to_camera_view(bpy.context.scene, cam, hand)
    print(
        "HOOK_IN", "shoulder",
        point_in_frame(cam, hook, 0.04),
        point_in_frame(cam, hand, 0.03),
        "frac", round(body_frame_fraction(cam), 3),
        "box", [round(v, 3) for v in ndc_box(cam)],
        "hook", round(hook_ndc.x, 3), round(hook_ndc.y, 3),
        "hand", round(hand_ndc.x, 3), round(hand_ndc.y, 3),
    )


def frame_grapple_side(cam, arm):
    hand = bone_pos(arm, "Hand_L")
    hook = bpy.data.objects["PropHook"].location.copy()
    mn, mx = body_bounds()
    body_c = (mn + mx) * 0.5
    flat = Vector((hook.x - hand.x, hook.y - hand.y, 0.0))
    if flat.length < 0.2:
        flat = Vector((0.0, -1.0, 0.0))
    flat.normalize()
    side = Vector((flat.y, -flat.x, 0.0))
    span = hook - Vector((body_c.x, body_c.y, hook.z))
    length = max(span.length + 1.3, 5.5)
    center = (hook + body_c) * 0.5
    center.z = 1.1
    lens = 32.0
    cam.data.type = "PERSP"
    cam.data.lens = lens
    cam.data.clip_start = 0.05
    cam.data.clip_end = 50.0
    tan_h = 18.0 / lens
    dist = (length / 0.82) * 0.5 / tan_h
    for _ in range(6):
        cam.location = Vector((center.x, center.y, 0.0)) + side * dist + Vector((0.0, 0.0, 1.7))
        look_at(cam, center)
        bpy.context.view_layer.update()
        aim_billboards(cam.location)
        feet = Vector(((mn.x + mx.x) * 0.5, (mn.y + mx.y) * 0.5, mn.z))
        head = Vector(((mn.x + mx.x) * 0.5, (mn.y + mx.y) * 0.5, mx.z))
        if point_in_frame(cam, hook, 0.05) and point_in_frame(cam, hand, 0.04) and point_in_frame(cam, feet, 0.04) and point_in_frame(cam, head, 0.04):
            break
        dist *= 1.08
    print(
        "HOOK_IN", "side",
        point_in_frame(cam, hook, 0.04),
        point_in_frame(cam, hand, 0.04),
        "frac", round(body_frame_fraction(cam), 3),
    )


def build_punch(arm, solids):
    return solids


def vignette(path, color):
    from PIL import Image

    im = Image.open(path).convert("RGB")
    px = im.load()
    w, h = im.size
    cr, cg, cb = [int(c * 255) for c in color[:3]]
    for y in range(h):
        for x in range(w):
            edge = min(x / w, (w - 1 - x) / w, y / h, (h - 1 - y) / h)
            band = max(0.0, 0.11 - edge) / 0.11
            if band <= 0:
                continue
            r, g, b = px[x, y]
            a = band * band * 0.88
            px[x, y] = (
                int(r * (1 - a) + cr * a),
                int(g * (1 - a) + cg * a),
                int(b * (1 - a) + cb * a),
            )
    im.save(path, optimize=True)


def stitch(left, right, dest):
    from PIL import Image

    a = Image.open(left).convert("RGB")
    b = Image.open(right).convert("RGB")
    w = a.width + b.width
    h = max(a.height, b.height)
    out = Image.new("RGB", (w, h), (12, 12, 14))
    out.paste(a, (0, 0))
    out.paste(b, (a.width, 0))
    for y in range(h):
        for x in range(a.width - 2, a.width + 2):
            if 0 <= x < w:
                out.putpixel((x, y), (16, 16, 18))
    out.save(dest, optimize=True)
    shrink(dest)


def paint_rig_overlap(arm):
    """Color hip and thigh faces that penetrate past the 3 cm joint ball."""
    clear_pose(arm)
    arm.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    low = lowest(arm)
    arm.location.z += -low + 0.006
    bpy.context.view_layer.update()
    red = make_mat("OverlapRed", (0.92, 0.05, 0.04, 1), 0.35, 1.0, 1.6)
    packed = {row[0].name: row for row in pack_body()}
    joined = neighbours(arm)
    marked = []
    worst = 0.0
    worst_pair = ""
    for name_a, row_a in packed.items():
        obj_a, bone_a, tree_a, verts_a, polys_a = row_a
        for name_b, row_b in packed.items():
            if name_a >= name_b:
                continue
            obj_b, bone_b, tree_b, verts_b, polys_b = row_b
            if (bone_a, bone_b) not in joined:
                continue
            joint = joint_world(arm, bone_a, bone_b)
            for src, other_tree, other_name in (
                (row_a, tree_b, name_b),
                (row_b, tree_a, name_a),
            ):
                obj, bone, tree, verts, polys = src
                bad = set()
                for idx in overlap_ids(tree, polys, other_tree):
                    if idx >= len(verts):
                        continue
                    point = verts[idx]
                    if joint is not None and (point - joint).length <= JOINT_EXEMPT:
                        continue
                    depth = inside_depth(other_tree, point)
                    if depth <= DEPTH_LIMIT:
                        continue
                    bad.add(idx)
                    if depth > worst:
                        worst = depth
                        worst_pair = obj.name + "|" + other_name
                if not bad:
                    continue
                me = obj.data
                slot = -1
                for mi, existing in enumerate(me.materials):
                    if existing == red:
                        slot = mi
                        break
                if slot < 0:
                    me.materials.append(red)
                    slot = len(me.materials) - 1
                for poly in me.polygons:
                    if any(v in bad for v in poly.vertices):
                        poly.material_index = slot
                marked.append(obj.name)
    return worst, worst_pair, sorted(set(marked))


def ghost_body():
    """Let the buried red faces read. The rig still is the only caller."""
    for obj in body_meshes():
        for slot in obj.material_slots:
            mat = slot.material
            if mat is None or mat.name == "OverlapRed":
                continue
            ghost = mat.copy()
            ghost.blend_method = "BLEND"
            if hasattr(ghost, "shadow_method"):
                ghost.shadow_method = "NONE"
            node = ghost.node_tree.nodes.get("Principled BSDF") if ghost.use_nodes else None
            if node is not None and "Alpha" in node.inputs:
                node.inputs["Alpha"].default_value = 0.28
            slot.material = ghost


def dust_puff_spec(surface, speed):
    """DustLook.At size, opacity, and count. Kick is none. Adopted pass-12 defaults."""
    t = _clamp((speed - 6.9) / (13.8 - 6.9), 0.0, 1.35)
    table = {
        "grass": (0.08, 0.11, 0.24, 0.31, 4.0, 5.0),
        "dirt": (0.24, 0.335, 0.62, 0.68, 7.0, 10.0),
        "wood": (0.055, 0.078, 0.70, 0.80, 6.0, 8.0),
        "concrete": (0.18, 0.24, 0.32, 0.44, 4.0, 5.0),
    }
    size0, size1, op0, op1, count0, count1 = table.get(surface, table["concrete"])
    size = size0 + (size1 - size0) * t
    opacity = min(0.95, op0 + (op1 - op0) * t)
    count = count0 + (count1 - count0) * t
    n = int(math.floor(count + 0.5))
    if n > 12:
        n = 12
    if n < 0:
        n = 0
    return size, opacity, n


def chart_hide():
    hidden = []
    for obj in list(bpy.data.objects):
        if obj.type in {"LIGHT", "CAMERA"} or obj.hide_render:
            continue
        hidden.append(obj)
        obj.hide_render = True
    return hidden


def chart_show(hidden):
    for obj in hidden:
        obj.hide_render = False


def chart_clear():
    for obj in list(bpy.data.objects):
        if obj.name.startswith("Chart"):
            mesh = obj.data
            bpy.data.objects.remove(obj, do_unlink=True)
            if mesh is not None and mesh.users == 0:
                if isinstance(mesh, bpy.types.Mesh):
                    bpy.data.meshes.remove(mesh)
                elif isinstance(mesh, bpy.types.Curve):
                    bpy.data.curves.remove(mesh)


def _chart_id(prefix):
    global CHART_N
    CHART_N += 1
    return "%s%d" % (prefix, CHART_N)


def chart_text(body, location, size, color):
    name = _chart_id("ChartText")
    bpy.ops.object.text_add(location=location, rotation=(math.pi / 2.0, 0.0, 0.0))
    obj = bpy.context.active_object
    obj.name = name
    obj.data.body = body
    obj.data.size = size
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.extrude = 0.002
    obj.data.materials.append(make_mat(name + "Mat", color, 0.5, 1.0, 0.0))
    return obj


def chart_plane(name, location, scale, color):
    name = _chart_id(name)
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location, rotation=(math.pi / 2.0, 0.0, 0.0))
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(make_mat(name + "Mat", color, 0.85, 1.0, 0.0))
    return obj


def chart_disc(name, location, size, color, opacity):
    name = _chart_id(name)
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location, rotation=(math.pi / 2.0, 0.0, 0.0))
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = (size, size * 0.72, 1.0)
    mat = bpy.data.materials.new(name + "Mat")
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = soft_image()
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = opacity
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    diff.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    diff.inputs["Roughness"].default_value = 0.9
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(diff.outputs["BSDF"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    obj.data.materials.append(mat)
    return obj


def render_dust_grid(cam):
    """3 speeds by 4 surfaces. Size and count follow DustLook.At. Colours are the still read."""
    hidden = chart_hide()
    bg = bpy.context.scene.world.node_tree.nodes["Background"]
    old_color = tuple(bg.inputs["Color"].default_value)
    old_strength = bg.inputs["Strength"].default_value
    bg.inputs["Color"].default_value = (0.94, 0.93, 0.90, 1.0)
    bg.inputs["Strength"].default_value = 0.85
    saved = (cam.location.copy(), cam.rotation_euler.copy(), cam.data.lens, cam.data.type, cam.data.ortho_scale)
    surfaces = ("concrete", "dirt", "grass", "wood")
    speeds = (("walk", 6.9), ("run", 9.0), ("sprint", 13.8))
    swatch = {
        "concrete": (0.55, 0.55, 0.53, 1.0),
        "dirt": (0.45, 0.30, 0.16, 1.0),
        "grass": (0.32, 0.46, 0.22, 1.0),
        "wood": (0.76, 0.62, 0.40, 1.0),
    }
    puff_color = {
        "concrete": (0.78, 0.78, 0.76, 1.0),
        "dirt": (0.62, 0.40, 0.20, 1.0),
        "grass": (0.70, 0.68, 0.60, 1.0),
        "wood": (0.90, 0.84, 0.70, 1.0),
    }
    grass_bit = (0.28, 0.62, 0.16, 1.0)
    chart_plane("ChartPaper", Vector((1.72, 0.08, 1.25)), (5.6, 3.3, 1.0), (0.93, 0.92, 0.89, 1.0))
    for col, surface in enumerate(surfaces):
        x = col * 1.15
        chart_text(surface, Vector((x, -0.02, 2.42)), 0.13, (0.12, 0.12, 0.12, 1.0))
        for row, (label, speed) in enumerate(speeds):
            size, opacity, count = dust_puff_spec(surface, speed)
            z = 1.85 - row * 0.72
            origin = Vector((x, -0.04, z))
            chart_text(label, origin + Vector((0.0, -0.01, 0.26)), 0.08, (0.12, 0.11, 0.10, 1.0))
            chart_plane("ChartSwatch", origin + Vector((0.0, 0.03, -0.22)), (0.98, 0.14, 1.0), swatch[surface])
            print("DUST", surface, label, "size", round(size, 3), "opacity", round(opacity, 3), "count", count)
            for i in range(count):
                ang = i / max(count, 1) * math.tau
                spread = 0.03 + (i % 3) * 0.028
                loc = origin + Vector((math.cos(ang) * spread, -0.01 * (i + 1), 0.02 + math.sin(ang) * spread * 0.35))
                shown = 0.055 + size * 0.55
                chart_disc("ChartPuff", loc, shown, puff_color[surface], opacity)
            if surface == "grass":
                bits = 1 if count < 3 else 2
                for b in range(bits):
                    fleck = origin + Vector((0.06 + b * 0.09, -0.08, 0.06))
                    name = _chart_id("ChartBit")
                    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.045, location=fleck, segments=10, ring_count=6)
                    bit = bpy.context.active_object
                    bit.name = name
                    bit.data.materials.append(make_mat(name + "Mat", grass_bit, 0.8, 1.0, 0.0))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 5.15
    cam.location = Vector((1.72, -3.2, 1.25))
    look_at(cam, Vector((1.72, 0.0, 1.25)))
    render_to(os.path.join(OUT, "running-dust.png"))
    chart_clear()
    cam.location, cam.rotation_euler = saved[0], saved[1]
    cam.data.lens = saved[2]
    cam.data.type = saved[3]
    cam.data.ortho_scale = saved[4]
    bg.inputs["Color"].default_value = old_color
    bg.inputs["Strength"].default_value = old_strength
    chart_show(hidden)


def burst_mat(name, color, dots):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    diff.inputs["Roughness"].default_value = 0.55
    if not dots:
        diff.inputs["Color"].default_value = color
        nt.links.new(diff.outputs["BSDF"], out.inputs["Surface"])
        return mat
    coord = nt.nodes.new("ShaderNodeTexCoord")
    vor = nt.nodes.new("ShaderNodeTexVoronoi")
    vor.inputs["Scale"].default_value = 42.0
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.12
    ramp.color_ramp.elements[0].color = (0.05, 0.04, 0.05, 1.0)
    ramp.color_ramp.elements[1].position = 0.22
    ramp.color_ramp.elements[1].color = color
    nt.links.new(coord.outputs["Object"], vor.inputs["Vector"])
    nt.links.new(vor.outputs["Distance"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], diff.inputs["Color"])
    nt.links.new(diff.outputs["BSDF"], out.inputs["Surface"])
    return mat


def burst_star(name, location, scale, color, dots):
    mesh = bpy.data.meshes.new(name + "Mesh")
    verts = [(0.0, 0.0, 0.0)]
    for i in range(16):
        ang = i * math.pi / 8.0 - math.pi / 2.0
        radius = 0.46 if i % 2 == 0 else 0.20
        verts.append((math.cos(ang) * radius, math.sin(ang) * radius, 0.0))
    faces = [(0, 1 + i, 1 + (i + 1) % 16) for i in range(16)]
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.scale = (scale, scale, scale)
    obj.rotation_euler = Euler((math.pi / 2.0, 0.0, 0.0), "XYZ")
    obj.data.materials.append(burst_mat(name + "Mat", color, dots))
    return obj


def render_comic(cam):
    """POP POW BAM WHAM by hit strength. On shows the bursts. Off shows the row only."""
    hidden = chart_hide()
    bg = bpy.context.scene.world.node_tree.nodes["Background"]
    old_color = tuple(bg.inputs["Color"].default_value)
    old_strength = bg.inputs["Strength"].default_value
    bg.inputs["Color"].default_value = (0.96, 0.95, 0.92, 1.0)
    bg.inputs["Strength"].default_value = 0.9
    saved = (cam.location.copy(), cam.rotation_euler.copy(), cam.data.lens, cam.data.type, cam.data.ortho_scale)
    words = (
        ("POP!", (1.0, 0.86, 0.12, 1.0), 0.42, "light"),
        ("POW!", (1.0, 0.46, 0.08, 1.0), 0.56, "firm"),
        ("BAM!", (0.95, 0.12, 0.18, 1.0), 0.72, "hard"),
        ("WHAM!", (0.62, 0.18, 0.95, 1.0), 0.90, "heavy"),
    )
    chart_plane("ChartPaper", Vector((0.65, 0.12, 1.20)), (5.4, 3.15, 1.0), (0.95, 0.93, 0.88, 1.0))
    chart_plane("ChartOn", Vector((0.65, 0.02, 2.28)), (4.6, 0.36, 1.0), (0.16, 0.15, 0.13, 1.0))
    chart_text("Comic words  On", Vector((0.65, -0.03, 2.28)), 0.16, (0.98, 0.95, 0.82, 1.0))
    chart_plane("ChartOff", Vector((0.65, 0.02, 0.22)), (4.6, 0.36, 1.0), (0.78, 0.76, 0.72, 1.0))
    chart_text("Comic words  Off", Vector((0.65, -0.03, 0.22)), 0.16, (0.28, 0.27, 0.25, 1.0))
    for i, (word, color, scale, strength) in enumerate(words):
        x = -1.05 + i * 1.15
        burst_star("ChartOutline", Vector((x, 0.05, 1.28)), scale * 1.18, (0.05, 0.04, 0.04, 1.0), False)
        burst_star("ChartBurst", Vector((x, 0.0, 1.28)), scale, color, True)
        chart_text(word, Vector((x, -0.06, 1.28)), 0.10 + scale * 0.05, (0.06, 0.05, 0.05, 1.0))
        chart_text(strength, Vector((x, -0.02, 0.62)), 0.09, (0.18, 0.16, 0.14, 1.0))
        print("COMIC", word, strength, "scale", scale)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 5.3
    cam.location = Vector((0.65, -3.4, 1.20))
    look_at(cam, Vector((0.65, 0.0, 1.20)))
    render_to(os.path.join(OUT, "comic-bursts.png"))
    chart_clear()
    cam.location, cam.rotation_euler = saved[0], saved[1]
    cam.data.lens = saved[2]
    cam.data.type = saved[3]
    cam.data.ortho_scale = saved[4]
    bg.inputs["Color"].default_value = old_color
    bg.inputs["Strength"].default_value = old_strength
    chart_show(hidden)


def render_charts(cam, only_set):
    if MEASURE_ONLY:
        return
    if not only_set or "running-dust" in only_set:
        render_dust_grid(cam)
    if not only_set or "comic-bursts" in only_set:
        render_comic(cam)


# Pass 11. Visual stills only. DustLook.At and ComicWords.Pick stay as they are.
BANGERS = os.path.join(ROOT, "Tools", "Tag", "fonts", "Bangers-Regular.ttf")
P11_N = 0
P11_CELL = (600, 420)
P11_COMIC_CELL = (520, 400)

# Fresh-footfall volume is size^2 * count * opacity. Sprint is about 3x walk.
# Wide/squash/lift are the still shape. They are not DustLook fields.
PROPOSAL = {
    "concrete": {
        "walk": (0.18, 0.32, 4),
        "sprint": (0.24, 0.44, 5),
        "color": (0.96, 0.96, 0.94, 1.0),
        "wide": 2.05,
        "squash": 0.36,
        "lift": 0.025,
        "spread": (0.28, 0.50),
        "layers": 2,
    },
    "dirt": {
        "walk": (0.24, 0.62, 7),
        "sprint": (0.335, 0.68, 10),
        "color": (0.84, 0.58, 0.30, 1.0),
        "wide": 1.05,
        "squash": 0.95,
        "lift": 0.09,
        "spread": (0.22, 0.42),
        "layers": 3,
    },
    "grass": {
        "walk": (0.08, 0.24, 4),
        "sprint": (0.11, 0.31, 5),
        "color": (0.80, 0.76, 0.62, 1.0),
        "wide": 1.0,
        "squash": 0.70,
        "lift": 0.05,
        "spread": (0.12, 0.18),
        "layers": 1,
    },
    "wood": {
        "walk": (0.055, 0.70, 6),
        "sprint": (0.078, 0.80, 8),
        "color": (1.0, 0.97, 0.88, 1.0),
        "wide": 0.85,
        "squash": 0.70,
        "lift": 0.04,
        "spread": (0.16, 0.30),
        "layers": 1,
    },
}
CURRENT_COLOR = {
    "concrete": (0.82, 0.82, 0.80, 1.0),
    "dirt": (0.76, 0.55, 0.30, 1.0),
    "grass": (0.40, 0.48, 0.18, 1.0),
    "wood": (0.55, 0.42, 0.28, 1.0),
}
# step along -travel, size scale, opacity scale. Newest is still forming.
P11_AGES = (
    (0.0, 0.78, 1.00),
    (1.0, 1.18, 0.65),
    (2.0, 1.55, 0.42),
)
P11_CLUSTER = (
    (0.04, 0.02),
    (-0.20, 0.10),
    (0.24, -0.08),
    (-0.10, -0.22),
    (0.34, 0.16),
    (-0.32, -0.12),
    (0.14, 0.28),
    (-0.18, 0.26),
    (0.30, -0.24),
    (-0.38, 0.14),
    (0.02, -0.32),
    (0.22, 0.06),
)


def p11_name(prefix):
    global P11_N
    P11_N += 1
    return "P11%s%d" % (prefix, P11_N)


def p11_rand(i, salt):
    x = math.sin(i * 12.9898 + salt * 78.233) * 43758.5453
    return x - math.floor(x)


def p11_clear(prefix):
    if bpy.context.object is not None and getattr(bpy.context.object, "mode", "OBJECT") != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    for obj in list(bpy.data.objects):
        if not obj.name.startswith(prefix):
            continue
        data = obj.data
        bpy.data.objects.remove(obj, do_unlink=True)
        if data is not None and data.users == 0 and isinstance(data, bpy.types.Mesh):
            bpy.data.meshes.remove(data)


def p11_speed_t(speed):
    return _clamp((speed - 6.9) / (13.8 - 6.9), 0.0, 1.0)


def p11_proposal_spec(surface, speed):
    row = PROPOSAL[surface]
    t = p11_speed_t(speed)
    sw, ow, cw = row["walk"]
    ss, os_, cs = row["sprint"]
    size = sw + (ss - sw) * t
    opacity = ow + (os_ - ow) * t
    count = int(round(cw + (cs - cw) * t))
    spread = row["spread"][0] + (row["spread"][1] - row["spread"][0]) * t
    wide = row["wide"]
    if surface == "concrete":
        wide = 1.85 + (2.20 - 1.85) * t
    return {
        "size": size,
        "opacity": opacity,
        "count": count,
        "spread": spread,
        "color": row["color"],
        "wide": wide,
        "squash": row["squash"],
        "lift": row["lift"],
        "layers": row["layers"],
        "blades": surface == "grass",
    }


def p11_current_spec(surface, speed):
    size, opacity, count = dust_puff_spec(surface, speed)
    return {
        "size": size,
        "opacity": opacity,
        "count": max(count, 0),
        "spread": max(0.04, size * 0.85),
        "color": CURRENT_COLOR[surface],
        "wide": 1.0,
        "squash": 0.72,
        "lift": 0.03,
        "layers": 1,
        "blades": False,
    }


def p11_volume(spec):
    return spec["size"] * spec["size"] * spec["count"] * spec["opacity"]


def p11_noise_mat(name, dark, light, scale, rough):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = scale
    noise.inputs["Detail"].default_value = 6.0
    noise.inputs["Roughness"].default_value = 0.55
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.32
    ramp.color_ramp.elements[0].color = dark
    ramp.color_ramp.elements[1].position = 0.70
    ramp.color_ramp.elements[1].color = light
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = rough
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.18
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def p11_link(mesh, name, mat):
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if mat is not None:
        obj.data.materials.append(mat)
    return obj


def p11_ground(surface):
    p11_clear("P11")
    if surface == "concrete":
        mat = p11_noise_mat(p11_name("Mat"), (0.26, 0.26, 0.25, 1), (0.38, 0.38, 0.36, 1), 9.0, 0.92)
        seam_mat = make_mat(p11_name("Mat"), (0.16, 0.16, 0.15, 1), 0.95)
    elif surface == "dirt":
        mat = p11_noise_mat(p11_name("Mat"), (0.24, 0.15, 0.08, 1), (0.40, 0.26, 0.13, 1), 7.0, 0.96)
    elif surface == "grass":
        mat = p11_noise_mat(p11_name("Mat"), (0.12, 0.26, 0.09, 1), (0.24, 0.40, 0.14, 1), 14.0, 0.94)
    else:
        mat = p11_noise_mat(p11_name("Mat"), (0.16, 0.10, 0.06, 1), (0.22, 0.14, 0.08, 1), 4.0, 0.9)
        plank_mat = p11_noise_mat(p11_name("Mat"), (0.28, 0.17, 0.09, 1), (0.42, 0.26, 0.14, 1), 18.0, 0.72)
    bpy.ops.mesh.primitive_plane_add(size=14.0, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = p11_name("Geo")
    ground.data.materials.append(mat)
    if surface == "concrete":
        for i in range(-4, 5):
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(i * 0.78, 0.0, 0.004))
            seam = bpy.context.active_object
            seam.name = p11_name("Geo")
            seam.scale = (0.012, 6.0, 0.004)
            seam.data.materials.append(seam_mat)
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, i * 0.78, 0.003))
            seam2 = bpy.context.active_object
            seam2.name = p11_name("Geo")
            seam2.scale = (6.0, 0.010, 0.003)
            seam2.data.materials.append(seam_mat)
    elif surface == "dirt":
        for i in range(9):
            ang = p11_rand(i, 3) * math.tau
            rad_i = 0.25 + p11_rand(i, 4) * 1.6
            loc = (math.cos(ang) * rad_i, math.sin(ang) * rad_i, 0.012)
            bpy.ops.mesh.primitive_ico_sphere_add(radius=0.012 + p11_rand(i, 5) * 0.014, location=loc, subdivisions=1)
            pebble = bpy.context.active_object
            pebble.name = p11_name("Geo")
            pebble.data.materials.append(make_mat(p11_name("Mat"), (0.22, 0.16, 0.11, 1), 0.9))
    elif surface == "grass":
        verts = []
        faces = []
        for i in range(80):
            x = (p11_rand(i, 1) - 0.5) * 4.6
            y = (p11_rand(i, 2) - 0.5) * 4.6
            h = 0.04 + p11_rand(i, 3) * 0.055
            w = 0.005 + p11_rand(i, 4) * 0.005
            yaw = p11_rand(i, 5) * math.tau
            dx = math.cos(yaw) * w
            dy = math.sin(yaw) * w
            base = len(verts)
            verts.extend(((x - dx, y - dy, 0.0), (x + dx, y + dy, 0.0), (x + dx, y + dy, h), (x - dx, y - dy, h)))
            faces.append((base, base + 1, base + 2, base + 3))
        mesh = bpy.data.meshes.new(p11_name("Lawn"))
        mesh.from_pydata(verts, [], faces)
        mesh.update()
        lawn = p11_link(mesh, mesh.name, make_mat(p11_name("Mat"), (0.20, 0.42, 0.14, 1), 0.8))
        lawn.data.materials[0].use_backface_culling = False
    else:
        for i in range(-5, 6):
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, i * 0.30, 0.02))
            plank = bpy.context.active_object
            plank.name = p11_name("Geo")
            plank.scale = (3.4, 0.132, 0.02)
            plank.data.materials.append(plank_mat)


def p11_soft_image():
    """Fat soft disc. The visible body is the puff size, with a short falloff."""
    img = bpy.data.images.get("P11SoftDisc")
    if img is not None:
        return img
    n = 64
    img = bpy.data.images.new("P11SoftDisc", n, n, alpha=True, float_buffer=True)
    pix = [0.0] * (n * n * 4)
    for y in range(n):
        for x in range(n):
            dx = (x + 0.5) / n - 0.5
            dy = (y + 0.5) / n - 0.5
            ang = math.atan2(dy, dx)
            wob = 0.84 + 0.10 * math.sin(ang * 3.0) + 0.06 * math.sin(ang * 7.0 + 0.8)
            r = math.sqrt(dx * dx + dy * dy) / (0.50 * wob)
            if r < 0.52:
                alpha = 1.0
            elif r < 1.0:
                u = (r - 0.52) / 0.48
                alpha = (1.0 - u) * (1.0 - u)
            else:
                alpha = 0.0
            i = (y * n + x) * 4
            pix[i] = pix[i + 1] = pix[i + 2] = 1.0
            pix[i + 3] = alpha
    img.pixels.foreach_set(pix)
    img.pack()
    return img


def p11_puff_mat(name, color, opacity):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.use_backface_culling = False
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    if hasattr(mat, "show_transparent_back"):
        mat.show_transparent_back = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = p11_soft_image()
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = opacity
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    diff.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    diff.inputs["Roughness"].default_value = 1.0
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(diff.outputs["BSDF"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def p11_puff(loc, width, height, color, opacity, cam_loc):
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=loc)
    obj = bpy.context.active_object
    obj.name = p11_name("Fx")
    direction = cam_loc - loc
    if direction.length < 0.001:
        direction = Vector((0.0, -1.0, 0.2))
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.scale = (max(width, 0.01), max(height, 0.008), 1.0)
    obj.data.materials.append(p11_puff_mat(p11_name("Mat"), color, opacity))
    return obj


def p11_heading(yaw_deg):
    th = math.radians(yaw_deg)
    fwd = Vector((math.sin(th), -math.cos(th), 0.0))
    left = Vector((math.cos(th), math.sin(th), 0.0))
    return fwd, left


def p11_foot(arm):
    obj = bpy.data.objects.get("Mesh_Foot_L")
    if obj is None:
        return bone_pos(arm, "Foot_L", tail=True)
    verts, _polys = mesh_world(obj)
    low = min(verts, key=lambda v: v.z)
    return Vector((low.x, low.y, 0.02))


def p11_cluster(origin, spec, across, along, cam_loc, salt):
    count = spec["count"]
    if count <= 0 or spec["size"] <= 0.001:
        return 0
    color = spec["color"]
    made = 0
    for i in range(count):
        ox, oy = P11_CLUSTER[i % len(P11_CLUSTER)]
        jitter = 0.85 + 0.3 * p11_rand(i, salt)
        pos = origin + across * ox * spec["spread"] * jitter + along * oy * spec["spread"] * 0.72
        rise = spec["lift"] + spec["size"] * spec["squash"] * (0.15 + 0.2 * p11_rand(i, salt + 1))
        pos = Vector((pos.x, pos.y, origin.z + rise))
        scale_i = 0.72 + 0.36 * p11_rand(i, salt + 2)
        width = spec["size"] * spec["wide"] * scale_i
        height = spec["size"] * spec["squash"] * (0.85 + 0.3 * p11_rand(i, salt + 3))
        tint = 0.92 + 0.12 * p11_rand(i, salt + 4)
        col = (color[0] * tint, color[1] * tint, color[2] * tint, 1.0)
        p11_puff(pos, width, height, col, spec["opacity"], cam_loc)
        made += 1
        for layer in range(1, spec["layers"]):
            off = across * (0.10 * layer) + along * (0.06 * layer) + Vector((0.0, 0.0, 0.045 * layer))
            p11_puff(
                pos + off,
                width * (0.78 - 0.12 * layer),
                height * (0.85 - 0.08 * layer),
                col,
                spec["opacity"] * 0.8,
                cam_loc,
            )
            made += 1
    return made


def p11_flecks(origin, speed, across, along, cam_loc):
    t = p11_speed_t(speed)
    blades = 4 + int(round(4 * t))
    leaves = 2 + int(round(2 * t))
    colors = (
        (0.90, 0.98, 0.32, 1.0),
        (0.72, 0.88, 0.24, 1.0),
        (0.55, 0.72, 0.18, 1.0),
    )
    for i in range(blades):
        ox = (p11_rand(i, 11) - 0.25) * 0.50
        oy = (p11_rand(i, 12) - 0.35) * 0.40
        pos = origin + across * ox + along * oy + Vector((0.0, 0.0, 0.08 + 0.08 * p11_rand(i, 13)))
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=pos)
        blade = bpy.context.active_object
        blade.name = p11_name("Fx")
        length = 0.16 + 0.10 * p11_rand(i, 14)
        blade.scale = (0.032, 0.008, length)
        to_cam = cam_loc - pos
        yaw = math.atan2(to_cam.y, to_cam.x) + (p11_rand(i, 15) - 0.5) * 0.7
        tilt = math.radians(58 + p11_rand(i, 16) * 24)
        blade.rotation_euler = Euler((tilt, 0.0, yaw), "XYZ")
        mat = make_mat(p11_name("Mat"), colors[i % 3], 0.75)
        mat.use_backface_culling = False
        blade.data.materials.append(mat)
    for i in range(leaves):
        ox = (p11_rand(i, 21) - 0.5) * 0.34
        oy = (p11_rand(i, 22) - 0.3) * 0.26
        pos = origin + across * ox + along * oy + Vector((0.0, 0.0, 0.05 + 0.04 * p11_rand(i, 23)))
        bpy.ops.mesh.primitive_plane_add(size=1.0, location=pos)
        leaf = bpy.context.active_object
        leaf.name = p11_name("Fx")
        leaf.scale = (0.09, 0.038, 1.0)
        to_cam = cam_loc - pos
        leaf.rotation_euler = (to_cam).to_track_quat("Z", "Y").to_euler()
        leaf.rotation_euler.rotate_axis("X", math.radians(30 + p11_rand(i, 24) * 40))
        mat = make_mat(p11_name("Mat"), (0.45, 0.72, 0.22, 1.0), 0.7)
        mat.use_backface_culling = False
        leaf.data.materials.append(mat)


def p11_emit_dust(arm, cam, spec, speed, yaw_deg):
    p11_clear("P11Fx")
    fwd, left = p11_heading(yaw_deg)
    foot = p11_foot(arm)
    step = 0.46 + 0.22 * p11_speed_t(speed)
    # Keep every footfall on the near side of the legs so the trail stays in view.
    side = (0.16, 0.36, 0.52)
    ahead = (0.20, 0.04, -0.02)
    puffs = 0
    for age_i, (step_mul, size_mul, op_mul) in enumerate(P11_AGES):
        aged = dict(spec)
        aged["size"] = spec["size"] * size_mul
        aged["opacity"] = spec["opacity"] * op_mul
        origin = foot - fwd * step * step_mul + left * side[age_i] + fwd * ahead[age_i]
        origin.z = 0.025
        if os.environ.get("FX_PASS11_DEBUG") == "1":
            aged = dict(aged)
            aged["color"] = ((1.0, 0.15, 0.1, 1.0), (0.15, 0.35, 1.0, 1.0), (0.1, 0.85, 0.2, 1.0))[age_i]
            aged["opacity"] = 0.9
        puffs += p11_cluster(origin, aged, left, -fwd, cam.location, 30 + age_i * 17)
        if spec["blades"] and age_i == 0:
            p11_flecks(origin, speed, left, -fwd, cam.location)
    return foot, puffs


def p11_aim_dust(cam, arm, foot, yaw_deg):
    fwd, left = p11_heading(yaw_deg)
    head = bone_pos(arm, "Head", tail=True)
    trail = foot - fwd * 1.7 + left * 0.3 + Vector((0.0, 0.0, 0.12))
    look = foot + Vector((0.0, 0.0, 0.48)) - fwd * 0.08
    cam.data.type = "PERSP"
    cam.data.lens = 30
    cam.data.clip_start = 0.05
    cam.data.clip_end = 40.0
    dist = 2.15
    side = 1.35
    for _ in range(8):
        cam.location = foot + fwd * dist + left * side + Vector((0.0, 0.0, 0.40))
        look_at(cam, look)
        bpy.context.view_layer.update()
        marks = (head, foot + Vector((0, 0, 0.05)), trail, bone_pos(arm, "Foot_R", tail=True))
        if all(point_in_frame(cam, p, 0.05) for p in marks):
            break
        dist *= 1.07
        side *= 1.03
    bpy.context.view_layer.update()
    return dist


def p11_grab(path, width, height):
    scene = bpy.context.scene
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.render.filepath = path
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.render.render(write_still=True)
    from PIL import Image

    return Image.open(path).convert("RGB")


def p11_font(size):
    from PIL import ImageFont

    return ImageFont.truetype(BANGERS, size)


def p11_finish(path, limit=400 * 1024):
    from PIL import Image

    im = Image.open(path).convert("RGB")
    im.save(path, optimize=True)
    if os.path.getsize(path) <= limit:
        print("SIZE", os.path.basename(path), os.path.getsize(path), im.size[0], im.size[1])
        return
    for colors in (180, 140, 112, 88):
        q = im.quantize(colors=colors, method=Image.Quantize.MEDIANCUT)
        q.save(path, optimize=True)
        if os.path.getsize(path) <= limit:
            print("SIZE", os.path.basename(path), os.path.getsize(path), "colors", colors)
            return
    w, h = im.size
    while w > 1080 and os.path.getsize(path) > limit:
        w = int(w * 0.9)
        h = int(h * 0.9)
        small = im.resize((w, h), Image.Resampling.LANCZOS)
        small.save(path, optimize=True)
        im = small
    print("SIZE", os.path.basename(path), os.path.getsize(path), "scaled", w, h)


def p11_sheet(cells, titles, headline, path):
    from PIL import Image, ImageDraw

    font = p11_font(26)
    small = p11_font(20)
    head_h = 40
    foot_h = 34
    gap = 6
    w = sum(im.width for im in cells) + gap * (len(cells) - 1)
    h = head_h + cells[0].height + foot_h
    sheet = Image.new("RGB", (w, h), (28, 26, 24))
    draw = ImageDraw.Draw(sheet)
    draw.text((10, 6), headline, font=font, fill=(255, 228, 140))
    x = 0
    for im, title in zip(cells, titles):
        sheet.paste(im, (x, head_h))
        tw = draw.textlength(title, font=small)
        draw.text((x + max(0, (im.width - tw) * 0.5), head_h + im.height + 6), title, font=small, fill=(255, 246, 226))
        x += im.width + gap
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sheet.save(path, optimize=True)
    p11_finish(path)


def p11_dust(arm, cam, yaw_deg, tmp):
    apply_pose(arm, pose_run, 0.0, yaw_deg)
    dist = p11_aim_dust(cam, arm, p11_foot(arm), yaw_deg)
    print("CAM dust dist", round(dist, 2), "lens", cam.data.lens)
    surfaces = ("concrete", "dirt", "grass", "wood")
    speeds = (("walk", 6.9), ("run", 9.0), ("sprint", 13.8))
    variants = ("current", "proposed")
    probe = os.environ.get("FX_PASS11_PROBE") == "1"
    if probe:
        surfaces = ("dirt", "grass", "concrete", "wood")
        variants = ("proposed",)
    for surface in surfaces:
        p11_ground(surface)
        for variant in variants:
            cells = []
            titles = []
            for label, speed in speeds:
                spec = p11_current_spec(surface, speed) if variant == "current" else p11_proposal_spec(surface, speed)
                vol = p11_volume(spec)
                print(
                    "PROPOSAL" if variant == "proposed" else "CURRENT",
                    surface,
                    label,
                    "size", round(spec["size"], 3),
                    "opacity", round(spec["opacity"], 3),
                    "count", spec["count"],
                    "volume", round(vol, 4),
                    "wide", spec["wide"],
                    "squash", spec["squash"],
                )
                _foot, puffs = p11_emit_dust(arm, cam, spec, speed, yaw_deg)
                print("PUFFS", surface, variant, label, puffs)
                cell_path = os.path.join(tmp, "%s-%s-%s.png" % (surface, variant, label))
                cells.append(p11_grab(cell_path, P11_CELL[0], P11_CELL[1]))
                titles.append("%s  %.3fm  op %.2f  x%d" % (label, spec["size"], spec["opacity"], spec["count"]))
            if variant == "proposed":
                headline = "%s   visual tuning proposal   FX, not a feel lock" % surface.upper()
            else:
                headline = "%s   DustLook.At current   true metre size" % surface.upper()
            p11_sheet(cells, titles, headline, os.path.join(OUT, "dust-%s-%s.png" % (surface, variant)))
            walk = p11_current_spec(surface, 6.9) if variant == "current" else p11_proposal_spec(surface, 6.9)
            sprint = p11_current_spec(surface, 13.8) if variant == "current" else p11_proposal_spec(surface, 13.8)
            wv = p11_volume(walk)
            sv = p11_volume(sprint)
            ratio = sv / wv if wv > 1e-8 else 0.0
            print("RATIO", variant, surface, "sprint/walk", round(ratio, 2))


def p11_star_mesh(name, radius, outline):
    outers = (1.00, 0.82, 1.16, 0.90, 1.08, 0.74, 1.18, 0.86, 0.98, 1.12, 0.78, 1.06, 0.92, 1.14)
    inners = (0.60, 0.50, 0.64, 0.46, 0.58, 0.52, 0.48, 0.62, 0.47, 0.56, 0.54, 0.49, 0.61, 0.51)
    scale = 1.16 if outline else 1.0
    verts = [(0.0, 0.0, 0.0)]
    for i in range(14):
        ang = i * math.tau / 14.0 - math.pi / 2.0 + (0.05 if i % 2 == 0 else -0.04)
        outer = radius * outers[i] * scale
        inner = radius * inners[i] * scale
        mid = ang + math.tau / 28.0
        verts.append((math.cos(ang) * outer, math.sin(ang) * outer, 0.0))
        verts.append((math.cos(mid) * inner, math.sin(mid) * inner, 0.0))
    n = 28
    faces = [(0, 1 + i, 1 + (i + 1) % n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    return mesh


def p11_dot_image():
    img = bpy.data.images.get("P11Halftone")
    if img is not None:
        return img
    n = 64
    img = bpy.data.images.new("P11Halftone", n, n, alpha=True, float_buffer=True)
    pix = [0.0] * (n * n * 4)
    for y in range(n):
        for x in range(n):
            dx = (x + 0.5) / n - 0.5
            dy = (y + 0.5) / n - 0.5
            r = math.sqrt(dx * dx + dy * dy)
            ink = 1.0 if r < 0.20 else 0.0
            i = (y * n + x) * 4
            pix[i] = pix[i + 1] = pix[i + 2] = ink
            pix[i + 3] = 1.0
    img.pixels.foreach_set(pix)
    img.pack()
    try:
        img.colorspace_settings.name = "Non-Color"
    except (TypeError, AttributeError):
        pass
    return img


def p11_ink_mat(name, color, fade, dots):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    mix.inputs["Fac"].default_value = fade
    if dots:
        coord = nt.nodes.new("ShaderNodeTexCoord")
        mapping = nt.nodes.new("ShaderNodeMapping")
        mapping.inputs["Scale"].default_value = (6.5, 6.5, 6.5)
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = p11_dot_image()
        tex.extension = "REPEAT"
        mix_rgb = nt.nodes.new("ShaderNodeMixRGB")
        dark = (color[0] * 0.28, color[1] * 0.28, color[2] * 0.28, 1.0)
        mix_rgb.inputs["Color1"].default_value = (color[0], color[1], color[2], 1.0)
        mix_rgb.inputs["Color2"].default_value = dark
        nt.links.new(coord.outputs["Object"], mapping.inputs["Vector"])
        nt.links.new(mapping.outputs["Vector"], tex.inputs["Vector"])
        nt.links.new(tex.outputs["Color"], mix_rgb.inputs["Fac"])
        nt.links.new(mix_rgb.outputs["Color"], emit.inputs["Color"])
    else:
        emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 1.0
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def p11_text_image(word, fill):
    from PIL import Image, ImageDraw

    box_w, box_h = 0.18, 0.100
    width_px = 720
    height_px = int(round(width_px * box_h / box_w))
    pad = 0.12
    target_w = width_px * (1.0 - 2.0 * pad)
    target_h = height_px * (1.0 - 2.0 * pad)
    best = 48
    lo, hi = 20, 320
    while lo <= hi:
        mid = (lo + hi) // 2
        stroke = max(6, int(mid * 0.16))
        font = p11_font(mid)
        dummy = Image.new("RGBA", (4, 4))
        bb = ImageDraw.Draw(dummy).textbbox((0, 0), word, font=font, stroke_width=stroke)
        tw, th = bb[2] - bb[0], bb[3] - bb[1]
        if tw <= target_w and th <= target_h:
            best = mid
            lo = mid + 1
        else:
            hi = mid - 1
    stroke = max(6, int(best * 0.16))
    font = p11_font(best)
    dummy = Image.new("RGBA", (4, 4))
    bb = ImageDraw.Draw(dummy).textbbox((0, 0), word, font=font, stroke_width=stroke)
    im = Image.new("RGBA", (width_px, height_px), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    x = (width_px - (bb[2] - bb[0])) / 2.0 - bb[0]
    y = (height_px - (bb[3] - bb[1])) / 2.0 - bb[1]
    draw.text((x, y), word, font=font, fill=fill + (255,), stroke_width=stroke, stroke_fill=(0, 0, 0, 255))
    print("COMIC-FONT", word, "px", best, "stroke", stroke)
    return im, box_w, box_h


def p11_image_mat(name, image, fade):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = image
    emit = nt.nodes.new("ShaderNodeEmission")
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = fade
    nt.links.new(tex.outputs["Color"], emit.inputs["Color"])
    emit.inputs["Strength"].default_value = 1.0
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def p11_speed_lines(parent, radius):
    angles = (0.4, 1.15, 2.05, 2.7, 3.6, 5.3)
    for i, ang in enumerate(angles):
        inner = radius * (1.22 + 0.06 * (i % 2))
        length = radius * (0.38 + 0.16 * (i % 3))
        mid = inner + length * 0.5
        cx = math.cos(ang) * mid
        cy = math.sin(ang) * mid
        mesh = bpy.data.meshes.new(p11_name("Line"))
        hw = 0.008 if i % 2 == 0 else 0.012
        hl = length * 0.5
        ca, sa = math.cos(ang), math.sin(ang)
        verts = []
        for sx, sy in ((-hw, -hl), (hw, -hl), (hw, hl), (-hw, hl)):
            verts.append((cx + ca * sx - sa * sy, cy + sa * sx + ca * sy, -0.01))
        mesh.from_pydata(verts, [], [(0, 1, 2, 3)])
        mesh.update()
        obj = p11_link(mesh, p11_name("Fx"), p11_ink_mat(p11_name("Mat"), (0.02, 0.02, 0.02, 1.0), 1.0, False))
        obj.parent = parent


def p11_burst(cam, center, word, color, text_fill, scale, fade, shear, spin):
    p11_clear("P11Fx")
    empty = bpy.data.objects.new(p11_name("Fx"), None)
    bpy.context.collection.objects.link(empty)
    empty.location = center
    empty.rotation_euler = (cam.location - center).to_track_quat("Z", "Y").to_euler()
    empty.scale = (scale, scale, scale)
    radius = 0.30
    outline = p11_link(
        p11_star_mesh(p11_name("Star"), radius, True),
        p11_name("Fx"),
        p11_ink_mat(p11_name("Mat"), (0.0, 0.0, 0.0, 1.0), fade, False),
    )
    outline.parent = empty
    outline.location = (0.0, 0.0, -0.012)
    outline.rotation_euler = Euler((0.0, 0.0, math.radians(spin)), "XYZ")
    fill = p11_link(
        p11_star_mesh(p11_name("Star"), radius, False),
        p11_name("Fx"),
        p11_ink_mat(p11_name("Mat"), color, fade, True),
    )
    fill.parent = empty
    fill.location = (0.0, 0.0, 0.0)
    fill.rotation_euler = Euler((0.0, 0.0, math.radians(spin)), "XYZ")
    p11_speed_lines(empty, radius)
    for child in list(empty.children):
        if child.data and any(s.name.startswith("P11Line") or "Line" in child.name for s in []):
            pass
    # Speed-line fade follows the burst. Rebuild their material factor.
    for child in empty.children:
        if child.data is None or not child.material_slots:
            continue
        if child == fill or child == outline:
            continue
        child.material_slots[0].material = p11_ink_mat(p11_name("Mat"), (0.02, 0.02, 0.02, 1.0), fade, False)
    im, box_w, box_h = p11_text_image(word, text_fill)
    tmp = os.path.join("/tmp", "p11-%s.png" % word.replace("!", ""))
    im.save(tmp)
    image = bpy.data.images.load(tmp)
    try:
        image.colorspace_settings.name = "sRGB"
    except (TypeError, AttributeError):
        pass
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0.0, 0.0, 0.0))
    text = bpy.context.active_object
    text.name = p11_name("Fx")
    for vert in text.data.vertices:
        vert.co.x += vert.co.y * shear
    text.scale = (box_w, box_h, 1.0)
    text.location = (0.0, 0.0, 0.018)
    text.rotation_euler = Euler((0.0, 0.0, math.radians(spin * 0.65)), "XYZ")
    text.data.materials.append(p11_image_mat(p11_name("Mat"), image, fade))
    text.parent = empty
    bpy.context.view_layer.update()


def p11_aim_punch(cam, arm, hand, yaw_deg):
    fwd, left = p11_heading(yaw_deg)
    right = -left
    head = bone_pos(arm, "Head", tail=True)
    hips = bone_pos(arm, "Hips")
    look = (hand + hips) * 0.5 + Vector((0.0, 0.0, 0.05))
    cam.data.type = "PERSP"
    cam.data.lens = 42
    cam.data.clip_start = 0.04
    dist = 1.15
    side = 0.55
    for _ in range(8):
        cam.location = hand + fwd * dist + right * side + Vector((0.0, 0.0, 0.18))
        look_at(cam, look)
        bpy.context.view_layer.update()
        if point_in_frame(cam, head, 0.04) and point_in_frame(cam, hand, 0.08) and point_in_frame(cam, hips, 0.02):
            break
        dist *= 1.08
        side *= 1.04
    bpy.context.view_layer.update()


def p11_comic(arm, cam, yaw_deg, tmp):
    from PIL import Image, ImageDraw

    p11_clear("P11")
    for obj in bpy.data.objects:
        if obj.name == "PropGround":
            obj.hide_render = False
    apply_pose(arm, pose_punch, 0.0, yaw_deg)
    hand = bone_pos(arm, "Hand_R", tail=True)
    p11_aim_punch(cam, arm, hand, yaw_deg)
    to_cam = (cam.location - hand).normalized()
    center = hand + to_cam * 0.08
    panels = (
        ("POP!", (1.0, 0.86, 0.12, 1.0), (255, 230, 40), 0.60, 0.72, 0.16, -7.0, "0.6 pop-in"),
        ("POW!", (1.0, 0.46, 0.08, 1.0), (255, 236, 60), 1.15, 1.00, -0.14, 8.0, "1.15 overshoot"),
        ("BAM!", (0.95, 0.12, 0.18, 1.0), (255, 255, 255), 1.00, 1.00, 0.12, -5.0, "1.0"),
        ("WHAM!", (0.62, 0.18, 0.95, 1.0), (255, 255, 255), 1.00, 0.50, -0.10, 6.0, "1.0 fade"),
    )
    cells = []
    titles = []
    for word, color, fill, scale, fade, shear, spin, title in panels:
        p11_burst(cam, center, word, color, fill, scale, fade, shear, spin)
        print("COMIC", word, "scale", scale, "fade", fade, "spin", spin)
        cells.append(p11_grab(os.path.join(tmp, "comic-%s.png" % word.replace("!", "")), P11_COMIC_CELL[0], P11_COMIC_CELL[1]))
        titles.append(title)
    p11_clear("P11Fx")
    off = p11_grab(os.path.join(tmp, "comic-off.png"), P11_COMIC_CELL[0], P11_COMIC_CELL[1])
    key = Image.new("RGB", P11_COMIC_CELL, (32, 28, 26))
    draw = ImageDraw.Draw(key)
    font = p11_font(28)
    body = p11_font(22)
    draw.text((24, 28), "POP IN", font=font, fill=(255, 220, 80))
    lines = (
        (70, "0.60   starts"),
        (108, "1.15   overshoot"),
        (146, "1.00   settles"),
        (184, "then fades"),
        (250, "Comic words  On"),
        (292, "Comic words  Off"),
    )
    for y, line in lines:
        draw.text((24, y), line, font=body, fill=(255, 246, 230))
    cells.extend((off, key))
    titles.extend(("Comic words  Off", "toggle"))
    # 3 over 3
    from PIL import Image as PILImage

    row_h = P11_COMIC_CELL[1]
    row_w = P11_COMIC_CELL[0]
    gap = 6
    head = 44
    foot = 32
    sheet_w = row_w * 3 + gap * 2
    sheet_h = head + (row_h + foot) * 2 + gap
    sheet = PILImage.new("RGB", (sheet_w, sheet_h), (24, 22, 20))
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 8), "Comic words  On     punch contact     Bangers", font=p11_font(26), fill=(255, 228, 140))
    label_font = p11_font(18)
    for i, (cell, title) in enumerate(zip(cells, titles)):
        col = i % 3
        row = i // 3
        x = col * (row_w + gap)
        y = head + row * (row_h + foot + gap)
        sheet.paste(cell, (x, y))
        draw.text((x + 8, y + row_h + 4), title, font=label_font, fill=(255, 246, 226))
    path = os.path.join(OUT, "comic-bursts.png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sheet.save(path, optimize=True)
    p11_finish(path)


def render_pass11(arm, cam):
    """In-scene dust at DustLook size and a visual proposal, plus comic bursts on the punch."""
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "SUN":
            obj.data.energy = 2.6
        elif obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 48
        if obj.name in ("PropGround", "PropSlab") or "Seam" in obj.name:
            obj.hide_render = True
    bg = bpy.context.scene.world.node_tree.nodes["Background"]
    bg.inputs["Strength"].default_value = 0.62
    bpy.context.scene.eevee.taa_render_samples = 8
    tmp = "/tmp/pass11-cells"
    os.makedirs(tmp, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    yaw = 32.0
    part = os.environ.get("FX_PASS11_PART", "all")
    if part in ("all", "dust"):
        p11_dust(arm, cam, yaw, tmp)
        for surface in ("concrete", "dirt", "grass", "wood"):
            walk = p11_proposal_spec(surface, 6.9)
            sprint = p11_proposal_spec(surface, 13.8)
            print(
                "PROPOSAL-LOCK",
                surface,
                "walk", round(walk["size"], 3), round(walk["opacity"], 3), walk["count"],
                "sprint", round(sprint["size"], 3), round(sprint["opacity"], 3), sprint["count"],
                "ratio", round(p11_volume(sprint) / max(p11_volume(walk), 1e-8), 3),
            )
    if part in ("all", "comic"):
        p11_comic(arm, cam, 18.0, tmp)
    print("PASS11 stills", OUT)


# Pass 12 dust. Adopted DustLook defaults, one runtime footfall per speed.
# The comic review sheet is Tools/Tag/render_comic_sheet.py. This blender
# comic path is not that sheet, and it does not draw the expanded word set.
P12_RADIUS = 0.30
P12_OUTERS = (1.00, 0.88, 1.08, 0.92, 1.04, 0.86, 1.12, 0.90, 0.98, 1.06, 0.87, 1.02, 0.94, 1.09)
P12_INNERS = (0.72, 0.64, 0.76, 0.66, 0.74, 0.68, 0.62, 0.78, 0.65, 0.73, 0.70, 0.63, 0.75, 0.67)
P12_OUTLINE = 1.10
P12_WORD = 0.50
P12_DOTS = 20.0


def p12_burst_width():
    return 2.0 * P12_RADIUS * max(P12_OUTERS) * P12_OUTLINE


def p12_footfall(speed):
    phase = math.pi / 2.0

    def fn(arm):
        pose_gait(arm, speed, phase)

    return fn


def p12_flecks(origin, speed, across, along):
    """Dried clippings above the lawn. Straw and tan, so they read against the blades.

    The earlier dark greens sat inside the lawn colour. These stay diffuse, not neon.
    """
    t = p11_speed_t(speed)
    n = 8 + int(round(4 * t))
    chips = (
        (0.62, 0.48, 0.16, 1.0),
        (0.42, 0.36, 0.12, 1.0),
        (0.50, 0.32, 0.11, 1.0),
    )
    for i in range(n):
        ox = (p11_rand(i, 11) - 0.30) * 0.46
        oy = (p11_rand(i, 12) - 0.35) * 0.38
        # Clear of the ~9 cm blades, up around the shin where the air is behind them.
        pos = origin + across * ox + along * oy + Vector((0.0, 0.0, 0.18 + 0.26 * p11_rand(i, 13)))
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=pos)
        chip = bpy.context.active_object
        chip.name = p11_name("Fx")
        length = 0.14 + 0.06 * p11_rand(i, 14)
        chip.scale = (0.022, 0.007, length)
        chip.rotation_euler = Euler((
            p11_rand(i, 15) * math.tau,
            p11_rand(i, 16) * math.tau,
            p11_rand(i, 17) * math.tau,
        ), "XYZ")
        mat = make_mat(p11_name("Mat"), chips[i % 3], 0.85)
        mat.use_backface_culling = False
        chip.data.materials.append(mat)


def p12_emit_dust(arm, cam, spec, speed, yaw_deg):
    p11_clear("P11Fx")
    fwd, left = p11_heading(yaw_deg)
    foot = p11_foot(arm)
    step = 0.46 + 0.22 * p11_speed_t(speed)
    side = (0.16, 0.36, 0.52)
    ahead = (0.20, 0.04, -0.02)
    puffs = 0
    for age_i, (step_mul, size_mul, op_mul) in enumerate(P11_AGES):
        aged = dict(spec)
        aged["size"] = spec["size"] * size_mul
        aged["opacity"] = spec["opacity"] * op_mul
        origin = foot - fwd * step * step_mul + left * side[age_i] + fwd * ahead[age_i]
        origin.z = 0.07 if spec.get("blades") else 0.03
        puffs += p11_cluster(origin, aged, left, -fwd, cam.location, 30 + age_i * 17)
        if spec["blades"] and age_i == 0:
            p12_flecks(origin, speed, left, -fwd)
    return foot, puffs


def p12_dust(arm, cam, yaw_deg, tmp):
    surfaces = ("concrete", "dirt", "grass", "wood")
    speeds = (("walk", 6.9), ("run", 9.0), ("sprint", 13.8))
    only = os.environ.get("FX_PASS12_SURFACE", "")
    if only:
        surfaces = tuple(s for s in surfaces if s == only)
    for surface in surfaces:
        p11_ground(surface)
        cells = []
        titles = []
        for label, speed in speeds:
            apply_pose(arm, p12_footfall(speed), 0.0, yaw_deg)
            spec = p11_proposal_spec(surface, speed)
            foot = p11_foot(arm)
            dist = p12_aim_dust(cam, foot, yaw_deg)
            vol = p11_volume(spec)
            print(
                "ADOPTED", surface, label,
                "size", round(spec["size"], 3),
                "opacity", round(spec["opacity"], 3),
                "count", spec["count"],
                "volume", round(vol, 4),
                "cam", round(dist, 2),
            )
            _foot, puffs = p12_emit_dust(arm, cam, spec, speed, yaw_deg)
            print("PUFFS", surface, label, puffs)
            cell_path = os.path.join(tmp, "%s-%s.png" % (surface, label))
            cells.append(p11_grab(cell_path, P11_CELL[0], P11_CELL[1]))
            titles.append("%s  %.3fm  op %.2f  x%d" % (label, spec["size"], spec["opacity"], spec["count"]))
        headline = "%s   DustLook defaults   footfall walk / run / sprint" % surface.upper()
        p11_sheet(cells, titles, headline, os.path.join(OUT, "dust-%s.png" % surface))
        walk = p11_proposal_spec(surface, 6.9)
        sprint = p11_proposal_spec(surface, 13.8)
        print(
            "ADOPTED-LOCK", surface,
            "ratio", round(p11_volume(sprint) / max(p11_volume(walk), 1e-8), 3),
        )


def p12_aim_dust(cam, foot, yaw_deg):
    """One distance for walk, run, and sprint so the dust scale stays comparable."""
    fwd, left = p11_heading(yaw_deg)
    look = foot + Vector((0.0, 0.0, 0.42)) - fwd * 0.08
    cam.data.type = "PERSP"
    cam.data.lens = 30
    cam.data.clip_start = 0.05
    cam.data.clip_end = 40.0
    dist = 2.60
    side = 1.48
    cam.location = foot + fwd * dist + left * side + Vector((0.0, 0.0, 0.36))
    look_at(cam, look)
    bpy.context.view_layer.update()
    return dist


def p12_star_mesh(name, radius, outline):
    scale = P12_OUTLINE if outline else 1.0
    verts = [(0.0, 0.0, 0.0)]
    for i in range(14):
        ang = i * math.tau / 14.0 - math.pi / 2.0 + (0.04 if i % 2 == 0 else -0.03)
        outer = radius * P12_OUTERS[i] * scale
        inner = radius * P12_INNERS[i] * scale
        mid = ang + math.tau / 28.0
        verts.append((math.cos(ang) * outer, math.sin(ang) * outer, 0.0))
        verts.append((math.cos(mid) * inner, math.sin(mid) * inner, 0.0))
    n = 28
    faces = [(0, 1 + i, 1 + (i + 1) % n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    xs = [v.co.x for v in mesh.vertices]
    ys = [v.co.y for v in mesh.vertices]
    minx, maxx = min(xs), max(xs)
    miny, maxy = min(ys), max(ys)
    spanx = max(maxx - minx, 1e-4)
    spany = max(maxy - miny, 1e-4)
    uv = mesh.uv_layers.new(name="Halftone")
    for poly in mesh.polygons:
        for li in poly.loop_indices:
            co = mesh.vertices[mesh.loops[li].vertex_index].co
            uv.data[li].uv = ((co.x - minx) / spanx, (co.y - miny) / spany)
    return mesh


def p12_dot_image():
    """Fine regular screen. Dense at the left, sparse at the right. Small dots."""
    img = bpy.data.images.get("P12Halftone")
    if img is not None:
        return img
    n = 256
    cells = P12_DOTS
    img = bpy.data.images.new("P12Halftone", n, n, alpha=True, float_buffer=True)
    pix = [0.0] * (n * n * 4)
    for y in range(n):
        for x in range(n):
            u = x / float(n - 1)
            radius = 0.32 * (1.0 - u) + 0.10 * u
            cx = (x + 0.5) / n * cells
            cy = (y + 0.5) / n * cells
            fx = cx - math.floor(cx) - 0.5
            fy = cy - math.floor(cy) - 0.5
            ink = 1.0 if fx * fx + fy * fy <= radius * radius else 0.0
            i = (y * n + x) * 4
            pix[i] = pix[i + 1] = pix[i + 2] = ink
            pix[i + 3] = 1.0
    img.pixels.foreach_set(pix)
    img.pack()
    try:
        img.colorspace_settings.name = "Non-Color"
    except (TypeError, AttributeError):
        pass
    return img


def _p12_math(nt, op, a=None, b=None, aval=None, bval=None):
    node = nt.nodes.new("ShaderNodeMath")
    node.operation = op
    if a is not None:
        nt.links.new(a, node.inputs[0])
    elif aval is not None:
        node.inputs[0].default_value = aval
    if b is not None:
        nt.links.new(b, node.inputs[1])
    elif bval is not None:
        node.inputs[1].default_value = bval
    return node.outputs[0]


def p12_ink_mat(name, color, fade, dots):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    mix.inputs["Fac"].default_value = fade
    if dots:
        coord = nt.nodes.new("ShaderNodeTexCoord")
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = p12_dot_image()
        tex.interpolation = "Closest"
        tex.extension = "EXTEND"
        mix_rgb = nt.nodes.new("ShaderNodeMixRGB")
        dark = (color[0] * 0.40, color[1] * 0.40, color[2] * 0.40, 1.0)
        mix_rgb.inputs["Color1"].default_value = (color[0], color[1], color[2], 1.0)
        mix_rgb.inputs["Color2"].default_value = dark
        nt.links.new(coord.outputs["UV"], tex.inputs["Vector"])
        nt.links.new(tex.outputs["Color"], mix_rgb.inputs["Fac"])
        nt.links.new(mix_rgb.outputs["Color"], emit.inputs["Color"])
    else:
        emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 1.0
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def p12_text_image(word, fill, plane_w):
    from PIL import Image, ImageDraw

    size = 340
    stroke = max(10, int(round(size * 0.20)))
    font = p11_font(size)
    dummy = Image.new("RGBA", (8, 8))
    bb = ImageDraw.Draw(dummy).textbbox((0, 0), word, font=font, stroke_width=stroke)
    tw, th = bb[2] - bb[0], bb[3] - bb[1]
    pad = 6
    im = Image.new("RGBA", (tw + pad * 2, th + pad * 2), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    draw.text(
        (pad - bb[0], pad - bb[1]),
        word,
        font=font,
        fill=fill + (255,),
        stroke_width=stroke,
        stroke_fill=(0, 0, 0, 255),
    )
    crop = im.split()[-1].getbbox()
    if crop:
        im = im.crop(crop)
    aspect = im.height / float(max(im.width, 1))
    print("COMIC-FONT", word, "stroke", stroke, "px", im.size[0], im.size[1], "plane", round(plane_w, 3))
    return im, plane_w, plane_w * aspect


def p12_burst(cam, center, word, color, text_fill, scale, fade, shear, spin):
    p11_clear("P11Fx")
    empty = bpy.data.objects.new(p11_name("Fx"), None)
    bpy.context.collection.objects.link(empty)
    empty.location = center
    empty.rotation_euler = (cam.location - center).to_track_quat("Z", "Y").to_euler()
    empty.scale = (scale, scale, scale)
    width = p12_burst_width()
    outline = p11_link(
        p12_star_mesh(p11_name("Star"), P12_RADIUS, True),
        p11_name("Fx"),
        p12_ink_mat(p11_name("Mat"), (0.0, 0.0, 0.0, 1.0), fade, False),
    )
    outline.parent = empty
    outline.location = (0.0, 0.0, -0.012)
    outline.rotation_euler = Euler((0.0, 0.0, math.radians(spin)), "XYZ")
    fill = p11_link(
        p12_star_mesh(p11_name("Star"), P12_RADIUS, False),
        p11_name("Fx"),
        p12_ink_mat(p11_name("Mat"), color, fade, True),
    )
    fill.parent = empty
    fill.location = (0.0, 0.0, 0.0)
    fill.rotation_euler = Euler((0.0, 0.0, math.radians(spin)), "XYZ")
    p11_speed_lines(empty, P12_RADIUS)
    for child in empty.children:
        if child == fill or child == outline or child.data is None or not child.material_slots:
            continue
        child.material_slots[0].material = p12_ink_mat(p11_name("Mat"), (0.02, 0.02, 0.02, 1.0), fade, False)
    im, box_w, box_h = p12_text_image(word, text_fill, P12_WORD * width)
    tmp = os.path.join("/tmp", "p12-%s.png" % word.replace("!", ""))
    im.save(tmp)
    image = bpy.data.images.load(tmp)
    try:
        image.colorspace_settings.name = "sRGB"
    except (TypeError, AttributeError):
        pass
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0.0, 0.0, 0.0))
    text = bpy.context.active_object
    text.name = p11_name("Fx")
    for vert in text.data.vertices:
        vert.co.x += vert.co.y * shear
    text.scale = (box_w, box_h, 1.0)
    text.location = (0.0, 0.0, 0.02)
    text.rotation_euler = Euler((0.0, 0.0, math.radians(spin * 0.35)), "XYZ")
    text.data.materials.append(p11_image_mat(p11_name("Mat"), image, fade))
    text.parent = empty
    bpy.context.view_layer.update()
    print("COMIC-FIT", word, "burst", round(width, 3), "word", round(box_w, 3), "ratio", round(box_w / width, 3))


def p12_comic(arm, cam, yaw_deg, tmp):
    from PIL import Image, ImageDraw

    p11_clear("P11")
    for obj in bpy.data.objects:
        if obj.name == "PropGround":
            obj.hide_render = False
    apply_pose(arm, pose_punch, 0.0, yaw_deg)
    hand = bone_pos(arm, "Hand_R", tail=True)
    p11_aim_punch(cam, arm, hand, yaw_deg)
    to_cam = (cam.location - hand).normalized()
    center = hand + to_cam * 0.08
    panels = (
        ("POP!", (1.0, 0.86, 0.12, 1.0), (255, 230, 40), 0.60, 0.72, 0.10, -6.0, "tap   POP!   0.6"),
        ("POW!", (1.0, 0.46, 0.08, 1.0), (255, 236, 60), 1.15, 1.00, -0.08, 7.0, "punch   POW!   1.15"),
        ("BAM!", (0.95, 0.12, 0.18, 1.0), (255, 255, 255), 1.00, 1.00, 0.08, -4.0, "sprint punch   BAM!"),
        ("WHAM!", (0.62, 0.18, 0.95, 1.0), (255, 255, 255), 1.00, 0.50, -0.06, 5.0, "tag   WHAM!   fade"),
    )
    cells = []
    titles = []
    for word, color, fill, scale, fade, shear, spin, title in panels:
        p12_burst(cam, center, word, color, fill, scale, fade, shear, spin)
        print("COMIC", word, "scale", scale, "fade", fade, "spin", spin)
        cells.append(p11_grab(os.path.join(tmp, "comic-%s.png" % word.replace("!", "")), P11_COMIC_CELL[0], P11_COMIC_CELL[1]))
        titles.append(title)
    p11_clear("P11Fx")
    off = p11_grab(os.path.join(tmp, "comic-off.png"), P11_COMIC_CELL[0], P11_COMIC_CELL[1])
    key = Image.new("RGB", P11_COMIC_CELL, (32, 28, 26))
    draw = ImageDraw.Draw(key)
    font = p11_font(28)
    body = p11_font(22)
    draw.text((24, 24), "HIT STRENGTH", font=font, fill=(255, 220, 80))
    lines = (
        (78, "tap            POP!"),
        (116, "punch          POW!"),
        (154, "sprint punch   BAM!"),
        (192, "tag            WHAM!"),
        (258, "Comic words  On"),
        (300, "Comic words  Off"),
    )
    for y, line in lines:
        draw.text((24, y), line, font=body, fill=(255, 246, 230))
    cells.extend((off, key))
    titles.extend(("Comic words  Off", "toggle"))
    row_h = P11_COMIC_CELL[1]
    row_w = P11_COMIC_CELL[0]
    gap = 6
    head = 44
    foot = 32
    sheet_w = row_w * 3 + gap * 2
    sheet_h = head + (row_h + foot) * 2 + gap
    sheet = Image.new("RGB", (sheet_w, sheet_h), (24, 22, 20))
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 8), "Comic words  On     punch contact     hit strength", font=p11_font(26), fill=(255, 228, 140))
    label_font = p11_font(18)
    for i, (cell, title) in enumerate(zip(cells, titles)):
        col = i % 3
        row = i // 3
        x = col * (row_w + gap)
        y = head + row * (row_h + foot + gap)
        sheet.paste(cell, (x, y))
        draw.text((x + 8, y + row_h + 4), title, font=label_font, fill=(255, 246, 226))
    path = os.path.join(OUT, "comic-bursts.png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    sheet.save(path, optimize=True)
    p11_finish(path)


def render_pass12(arm, cam):
    """Adopted dust on walk, run, and sprint footfalls, plus strength-picked bursts."""
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "SUN":
            obj.data.energy = 2.6
        elif obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 48
        if obj.name in ("PropGround", "PropSlab") or "Seam" in obj.name:
            obj.hide_render = True
    bg = bpy.context.scene.world.node_tree.nodes["Background"]
    bg.inputs["Strength"].default_value = 0.62
    bpy.context.scene.eevee.taa_render_samples = 8
    tmp = "/tmp/pass12-cells"
    os.makedirs(tmp, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    print(
        "P12-STAR",
        "width", round(p12_burst_width(), 3),
        "word", round(P12_WORD * p12_burst_width(), 3),
        "dots", P12_DOTS,
    )
    part = os.environ.get("FX_PASS12_PART", "all")
    if part in ("all", "dust"):
        p12_dust(arm, cam, 32.0, tmp)
    if part in ("all", "comic"):
        p12_comic(arm, cam, 18.0, tmp)
    print("PASS12 stills", OUT)


def main():
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
    cam = setup_world(arm)
    if os.environ.get("FX_PASS12") == "1":
        render_pass12(arm, cam)
        return
    if os.environ.get("FX_PASS11") == "1":
        render_pass11(arm, cam)
        return
    shots = [
        ("landing-impact", pose_land, 24, 0.0, build_land, None),
        ("landing-roll", pose_roll, 28, 0.0, build_roll, frame_roll_contact),
        ("grapple-shoulder", pose_grapple, -12, 0.0, build_grapple, frame_grapple_shoulder),
        ("grapple-side", pose_grapple, -12, 0.0, build_grapple, frame_grapple_side),
        ("immunity-glow", pose_run, 20, 0.0, build_immune, None),
        ("punch-stagger", pose_stagger, -14, 0.0, build_stagger, None),
        ("launch-pad", pose_launch, 18, 0.55, build_launch, None),
        ("wall-run", pose_wall, 90, 0.0, build_wall, frame_wall_side),
        ("wall-run-top", pose_wall, 90, 0.0, build_wall, frame_wall_top),
    ]
    only = os.environ.get("FX_ONLY", "")
    only_set = set(part.strip() for part in only.split(",") if part.strip())
    totals = []
    for name, pose, yaw, lift, build, frame in shots:
        if only_set and name not in only_set:
            continue
        totals.append((name,) + shot(arm, cam, name, pose, yaw, lift, build, frame))
    if only_set:
        self_max = max(row[1] for row in totals) if totals else 0.0
        world_max = max(row[2] for row in totals) if totals else 0.0
        fails = sum(row[3] for row in totals)
        print(
            "no-clip clips=%d frames=%d worldMax=%.2f selfMax=%.2f fails=%d"
            % (len(totals), len(totals), world_max * 100.0, self_max * 100.0, fails)
        )
        render_charts(cam, only_set)
        return

    clear_fx()
    punch_path = os.path.join(OUT, "_tagger.png")
    victim_path = os.path.join(OUT, "_runner.png")
    apply_pose(arm, pose_punch, 0.0, 22)
    slab = bpy.data.objects.get("PropSlab")
    ground_solids = [solid_of(slab)] if slab is not None else []
    s1, w1, f1, pair = measure(arm, ground_solids)
    print("NOCLIP", "tagger", "self_cm", round(s1 * 100, 2), "world_cm", round(w1 * 100, 2), "fails", f1, "pair", pair)
    note_added("tagger")
    if not MEASURE_ONLY:
        frame_camera(cam)
        bpy.context.scene.render.filepath = punch_path
        bpy.ops.render.render(write_still=True)
        vignette(punch_path, TAG)

    apply_pose(arm, pose_stagger, 0.0, -18)
    s2, w2, f2, pair2 = measure(arm, ground_solids)
    print("NOCLIP", "runner", "self_cm", round(s2 * 100, 2), "world_cm", round(w2 * 100, 2), "fails", f2, "pair", pair2)
    note_added("runner")
    if not MEASURE_ONLY:
        frame_camera(cam)
        bpy.context.scene.render.filepath = victim_path
        bpy.ops.render.render(write_still=True)
        dest = os.path.join(OUT, "tagged-flash.png")
        stitch(punch_path, victim_path, dest)
        os.remove(punch_path)
        os.remove(victim_path)

    clear_fx()
    clear_pose(arm)
    arm.rotation_euler = Euler((0.0, 0.0, 0.0), "XYZ")
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    low = lowest(arm)
    arm.location.z += -low + 0.006
    bpy.context.view_layer.update()
    for pair_name, pair_depth in each_pair(arm):
        if pair_depth <= DEPTH_LIMIT:
            continue
        print("RIG-PAIR %s depth_cm %.2f" % (pair_name, pair_depth * 100.0))
    depth, rig_pair, pieces = paint_rig_overlap(arm)
    print(
        "RIG-FAIL depth_cm %.2f pair %s pieces %s"
        % (depth * 100.0, rig_pair, ",".join(pieces))
    )
    rig_self, rig_world, rig_fails, rig_worst = measure(arm, ground_solids)
    print(
        "NOCLIP",
        "rig-rest",
        "self_cm", round(rig_self * 100, 2),
        "world_cm", round(rig_world * 100, 2),
        "fails", rig_fails,
        "pair", rig_worst,
    )
    if not MEASURE_ONLY:
        ghost_body()
        hip = bpy.data.objects.get("Mesh_Hips")
        focus = hip.matrix_world.translation if hip is not None else Vector((0, 0, 1))
        cam.location = focus + Vector((0.72, -1.05, 0.18))
        look_at(cam, focus + Vector((0.0, 0.0, -0.05)))
        cam.data.lens = 62
        render_to(os.path.join(OUT, "rig-overlap-hips-thigh.png"))

    self_max = max([row[1] for row in totals] + [s1, s2, rig_self])
    world_max = max([row[2] for row in totals] + [w1, w2, rig_world])
    fails = sum(row[3] for row in totals) + f1 + f2 + rig_fails
    clips = len(totals) + 3
    print("rig-rest fails=%d pair %s" % (rig_fails, rig_worst))
    if POSE_ADDED:
        print("pose-added fails=%d" % len(POSE_ADDED))
    else:
        print("pose-added fails=0")
    print(
        "no-clip clips=%d frames=%d worldMax=%.2f selfMax=%.2f fails=%d"
        % (clips, clips, world_max * 100.0, self_max * 100.0, fails)
    )
    render_charts(cam, only_set)


if __name__ == "__main__":
    main()
