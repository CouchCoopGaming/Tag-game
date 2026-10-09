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


def pose_slide(arm):
    """VerbPoseClips.SlideBodyPose(leadLeft). The still grounds the lowest vertex."""
    leg(arm, "L", 68.0, -10.0, 8.0)
    leg(arm, "R", 40.0, -130.0, 24.0)
    set_zxy(arm, "Foot_L", -(-58.0), 0.0, -6.0)
    set_zxy(arm, "Foot_R", -(16.0), 0.0, -8.0)
    arm_pose(arm, "L", 48.0, 22.0, -36.0, 0.0)
    arm_pose(arm, "R", -36.0, 22.0, -28.0, 0.0)
    torso(arm, -22.0, -14.0, -50.0)
    print("RUNTIME slide SlideBody leadLeft")


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


def pose_airdash(arm, side=0.0, fwd=1.0):
    """AirDashPose.At(side, fwd) at dash weight 1.

    Thigh, knee, and head negate into this FBX the same way pose_launch does.
    ApplyAirDashPose feeds the sample straight into Quaternion.Euler, and
    negates the right arm yaw. armZ is the resting 4°.
    """
    thigh = -16.0
    knee = -12.0
    pitch = 70.0
    yaw = 10.0
    elbow = -18.0
    hip = 58.0 * fwd
    spine = 24.0 * fwd
    head = 6.0 * fwd
    roll = 34.0 * side
    arm_z = 4.0
    set_zxy(arm, "UpperLeg_L", -thigh, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_L", -knee, 0.0, 0.0)
    set_zxy(arm, "UpperLeg_R", -thigh, 0.0, 0.0)
    set_zxy(arm, "LowerLeg_R", -knee, 0.0, 0.0)
    set_zxy(arm, "UpperArm_L", pitch, yaw, arm_z)
    set_zxy(arm, "LowerArm_L", elbow, 0.0, 0.0)
    set_zxy(arm, "UpperArm_R", pitch, -yaw, -arm_z)
    set_zxy(arm, "LowerArm_R", elbow, 0.0, 0.0)
    set_zxy(arm, "Hips", hip, 0.0, roll * 0.55)
    set_zxy(arm, "Spine", spine, 0.0, roll)
    set_zxy(arm, "Head", -head, 0.0, 0.0)
    print(
        "RUNTIME air dash",
        "fwd", round(fwd, 2), "side", round(side, 2),
        "hip", round(hip, 1), "spine", round(spine, 1), "roll", round(roll, 1),
    )


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


def p11_ground(surface, light=False, asphalt=False):
    p11_clear("P11")
    if surface == "concrete":
        if asphalt:
            # Mid-grey asphalt. The pass 14 slab (0.78) matched the dust (0.86).
            dark = (0.22, 0.22, 0.21, 1)
            pale = (0.40, 0.39, 0.37, 1)
        elif light:
            dark = (0.58, 0.58, 0.55, 1)
            pale = (0.78, 0.78, 0.74, 1)
        else:
            dark = (0.26, 0.26, 0.25, 1)
            pale = (0.38, 0.38, 0.36, 1)
        mat = p11_noise_mat(p11_name("Mat"), dark, pale, 9.0, 0.92)
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


def p12_flecks(origin, speed, across, along, scale=1.0):
    """Dried clippings above the lawn. Straw and tan, so they read against the blades.

    The earlier dark greens sat inside the lawn colour. These stay diffuse, not neon.
    scale < 1 keeps the clippings from out-voting the foot plume in the proof box.
    """
    t = p11_speed_t(speed)
    n = int(round((8 + int(round(4 * t))) * scale))
    if n < 1:
        n = 1
    chips = (
        (0.62, 0.48, 0.16, 1.0),
        (0.42, 0.36, 0.12, 1.0),
        (0.50, 0.32, 0.11, 1.0),
    )
    z_lo = 0.18 if scale >= 0.99 else 0.04
    z_span = 0.26 if scale >= 0.99 else 0.06
    for i in range(n):
        ox = (p11_rand(i, 11) - 0.30) * 0.46 * scale
        oy = (p11_rand(i, 12) - 0.35) * 0.38 * scale
        # Clear of the ~9 cm blades, up around the shin where the air is behind them.
        pos = origin + across * ox + along * oy + Vector((0.0, 0.0, z_lo + z_span * p11_rand(i, 13)))
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=pos)
        chip = bpy.context.active_object
        chip.name = p11_name("Fx")
        length = (0.14 + 0.06 * p11_rand(i, 14)) * scale
        chip.scale = (0.022 * scale, 0.007 * scale, length)
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


def p14_speed_u(speed):
    run = 9.0
    at_run = 0.42
    if speed <= 6.9:
        return 0.0
    if speed >= 13.8:
        return 1.0
    if speed <= run:
        return (speed - 6.9) / (run - 6.9) * at_run
    return at_run + (speed - run) / (13.8 - run) * (1.0 - at_run)


def p14_at(surface, speed, kick=1.0):
    """Same anchors as DustLook.At. kick is the size multiplier."""
    u = p14_speed_u(speed)
    table = {
        "grass": (0.04, 0.13, 0.20, 0.50, 2, 6, 0.14, 0.42, 0.07, 0.48, 0.10, (0.76, 0.77, 0.70)),
        "dirt": (0.12, 0.34, 0.55, 0.88, 5, 11, 0.22, 0.58, 0.18, 0.90, 0.22, (0.68, 0.46, 0.24)),
        "wood": (0.04, 0.10, 0.45, 0.78, 3, 8, 0.14, 0.40, 0.06, 0.38, 0.08, (0.90, 0.76, 0.56)),
        "concrete": (0.05, 0.20, 0.28, 0.82, 2, 9, 0.16, 0.50, 0.10, 0.72, 0.16, (0.78, 0.78, 0.76)),
    }
    s0, s1, o0, o1, c0, c1, l0, l1, sp0, sp1, lift1, color = table[surface]
    size = s0 + (s1 - s0) * u
    opacity = o0 + (o1 - o0) * u
    count = c0 + (c1 - c0) * u
    life = l0 + (l1 - l0) * u
    span = sp0 + (sp1 - sp0) * u
    lift = 0.02 + (lift1 - 0.02) * u
    back = span * (0.35 + 0.45 * u)
    size *= kick
    span *= kick
    back *= kick
    opacity = min(0.95, opacity * kick)
    n = int(count + 0.5)
    if n > 12:
        n = 12
    return {
        "size": size,
        "opacity": opacity,
        "count": n,
        "life": life,
        "span": span,
        "lift": lift,
        "back": back,
        "core": u,
        "color": color,
    }


def p14_aim(cam, foot, yaw_deg):
    """TpsMoveCamera at rest: pivot 1.4, boom (0.4, 0.45, -5.2), pitch +12°, look 1.25, fov 78°."""
    fwd, left = p11_heading(yaw_deg)
    cam.data.type = "PERSP"
    cam.data.sensor_fit = "VERTICAL"
    cam.data.sensor_height = 24.0
    cam.data.lens = 14.8
    cam.data.clip_start = 0.05
    cam.data.clip_end = 80.0
    cam.location = foot - left * 0.40 + Vector((0.0, 0.0, 2.921)) - fwd * 4.992
    look_at(cam, foot + Vector((0.0, 0.0, 1.25)))
    bpy.context.view_layer.update()
    return (cam.location - foot).length


def p14_mote(origin, fwd, left, spec, cam_loc, fade, salt):
    n = spec["count"]
    if n <= 0 or fade <= 0.02:
        return 0
    color = spec["color"]
    made = 0
    denom = max(n - 1, 1)
    for i in range(n):
        u = i / denom
        along = spec["back"] * (0.12 + 0.88 * u)
        side = (p11_rand(i, salt) - 0.5) * spec["span"] * 0.34
        rise = spec["lift"] * (u ** 0.75)
        pos = origin - fwd * along + left * side + Vector((0.0, 0.0, 0.03 + rise))
        width = spec["size"] * (0.65 + 0.55 * math.sin(u * math.pi))
        if width < 0.02:
            width = 0.02
        height = max(0.015, width * 0.42)
        tint = 0.92 + 0.10 * p11_rand(i, salt + 3)
        col = (color[0] * tint, color[1] * tint, color[2] * tint, 1.0)
        op = spec["opacity"] * fade * (1.0 - 0.28 * u)
        p11_puff(pos, width, height, col, op, cam_loc)
        made += 1
        if spec["core"] > 0.35 and u < 0.55:
            grit = 0.62
            core = (color[0] * grit, color[1] * grit, color[2] * grit, 1.0)
            p11_puff(pos + Vector((0.0, 0.0, 0.02)), width * 0.48, height * 0.55, core, min(0.9, op + 0.12), cam_loc)
            made += 1
    return made


def p14_cloud(foot, fwd, left, spec, cam_loc, step):
    """Fresh heel puff plus one older step, so a sprint plume is still in the air."""
    made = p14_mote(foot, fwd, left, spec, cam_loc, 1.0, 4)
    if spec["life"] < 0.22:
        return made
    older = dict(spec)
    older["opacity"] *= 0.45
    made += p14_mote(foot - fwd * step, fwd, left, older, cam_loc, 0.55, 19)
    return made


def p14_ring_radius(impact):
    gate = 36.5
    t = impact / gate
    if t < 0.0:
        t = 0.0
    if t > 1.35:
        t = 1.35
    base = 0.45 + t * 0.85
    if impact < 12.0:
        tier = 0.25
    elif impact < 24.0:
        tier = 0.55
    else:
        tier = 1.0
    return base * (0.55 + 0.45 * tier), t


def p14_ring(foot, spec, impact, cam_loc):
    radius, t = p14_ring_radius(impact)
    n = 8 + int(t * 8.0)
    if n > 16:
        n = 16
    color = spec["color"]
    size = 0.06 + t * 0.10
    op = min(0.90, 0.45 + t * 0.35)
    for i in range(n):
        ang = i / n * math.tau
        pos = foot + Vector((math.cos(ang) * radius, math.sin(ang) * radius, 0.04 + 0.05 * t))
        p11_puff(pos, size * 1.6, size * 0.7, color + (1.0,), op, cam_loc)
        if t > 0.45:
            grit = tuple(c * 0.62 for c in color) + (1.0,)
            p11_puff(pos + Vector((0.0, 0.0, 0.02)), size * 0.7, size * 0.35, grit, min(0.9, op + 0.1), cam_loc)
    return radius, n


def p14_jpeg(path, image):
    from PIL import Image

    os.makedirs(os.path.dirname(path), exist_ok=True)
    image = image.convert("RGB")
    for quality in (85, 75, 65, 55, 45):
        image.save(path, format="JPEG", quality=quality, optimize=True)
        if os.path.getsize(path) <= 400 * 1024:
            print("SIZE", os.path.basename(path), os.path.getsize(path), "q", quality, image.size)
            return
    w, h = image.size
    while os.path.getsize(path) > 400 * 1024 and w > 900:
        w = int(w * 0.9)
        h = int(h * 0.9)
        image = image.resize((w, h), Image.Resampling.LANCZOS)
        image.save(path, format="JPEG", quality=60, optimize=True)
    print("SIZE", os.path.basename(path), os.path.getsize(path), "scaled", image.size)


def p14_sheet(cells, titles, headline, path):
    from PIL import Image, ImageDraw

    font = p11_font(22)
    small = p11_font(16)
    head_h = 36
    foot_h = 28
    gap = 4
    w = sum(im.width for im in cells) + gap * (len(cells) - 1)
    h = head_h + cells[0].height + foot_h
    sheet = Image.new("RGB", (w, h), (28, 26, 24))
    draw = ImageDraw.Draw(sheet)
    draw.text((8, 6), headline, font=font, fill=(255, 228, 140))
    x = 0
    for im, title in zip(cells, titles):
        sheet.paste(im, (x, head_h))
        draw.text((x + 8, head_h + im.height + 4), title, font=small, fill=(255, 246, 226))
        x += im.width + gap
    p14_jpeg(path, sheet)


def p14_grid(cells, titles, headline, path, cols):
    from PIL import Image, ImageDraw

    font = p11_font(22)
    small = p11_font(16)
    head_h = 36
    foot_h = 26
    gap = 4
    cell_w = cells[0].width
    cell_h = cells[0].height
    rows = (len(cells) + cols - 1) // cols
    w = cols * cell_w + gap * (cols - 1)
    h = head_h + rows * (cell_h + foot_h) + gap * (rows - 1)
    sheet = Image.new("RGB", (w, h), (28, 26, 24))
    draw = ImageDraw.Draw(sheet)
    draw.text((8, 6), headline, font=font, fill=(255, 228, 140))
    for i, (im, title) in enumerate(zip(cells, titles)):
        col = i % cols
        row = i // cols
        x = col * (cell_w + gap)
        y = head_h + row * (cell_h + foot_h + gap)
        sheet.paste(im, (x, y))
        draw.text((x + 8, y + cell_h + 2), title, font=small, fill=(255, 246, 226))
    p14_jpeg(path, sheet)


def render_pass14(arm, cam):
    """Dust at the chase-cam height. Walk, run, and sprint are different clouds."""
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
    tmp = "/tmp/pass14-cells"
    os.makedirs(tmp, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    yaw = 32.0
    fwd, left = p11_heading(yaw)
    surfaces = ("concrete", "dirt", "grass", "wood")
    speeds = (("walk", 6.9), ("run", 9.0), ("sprint", 13.8))
    full = (960, 540)
    sprint_frames = {}
    for surface in surfaces:
        p11_ground(surface, light=(surface == "concrete"))
        cells = []
        titles = []
        for label, speed in speeds:
            apply_pose(arm, p12_footfall(speed), 0.0, yaw)
            foot = p11_foot(arm)
            dist = p14_aim(cam, foot, yaw)
            spec = p14_at(surface, speed)
            step = 0.55 + 0.45 * p14_speed_u(speed)
            print(
                "DUST14", surface, label,
                "size", round(spec["size"], 3),
                "op", round(spec["opacity"], 3),
                "n", spec["count"],
                "span", round(spec["span"], 3),
                "back", round(spec["back"], 3),
                "lift", round(spec["lift"], 3),
                "life", round(spec["life"], 3),
                "cam", round(dist, 2),
            )
            p11_clear("P11Fx")
            p14_cloud(foot, fwd, left, spec, cam.location, step)
            if surface == "grass":
                p12_flecks(foot, speed, left, -fwd)
            frame = p11_grab(os.path.join(tmp, "%s-%s.png" % (surface, label)), full[0], full[1])
            if label == "sprint":
                sprint_frames[surface] = frame
            cells.append(frame.resize((480, 270), __import__("PIL").Image.Resampling.LANCZOS))
            titles.append("%s  %.0fcm  op %.2f  %.2fs" % (label, spec["span"] * 100.0, spec["opacity"], spec["life"]))
        p14_sheet(cells, titles, "%s   chase cam   walk / run / sprint" % surface.upper(), os.path.join(OUT, "dust-%s.jpg" % surface))

    crops = []
    crop_titles = []
    for surface in surfaces:
        frame = sprint_frames[surface]
        # Foot band of a 960x540 split pane, 1:1 pixels.
        crop = frame.crop((240, 250, 760, 520))
        crops.append(crop)
        spec = p14_at(surface, 13.8)
        crop_titles.append("%s sprint  %.0fcm" % (surface, spec["span"] * 100.0))
    p14_grid(crops, crop_titles, "Split pane, sprint, foot crop", os.path.join(OUT, "dust-split.jpg"), 2)

    land_cells = []
    land_titles = []
    lands = (
        ("concrete", pose_land, 8.0, "concrete light"),
        ("concrete", pose_land, 36.5, "concrete hard"),
        ("dirt", pose_land, 36.5, "dirt hard"),
        ("dirt", pose_roll, 36.5, "dirt roll"),
    )
    for surface, pose, impact, title in lands:
        p11_ground(surface, light=(surface == "concrete"))
        apply_pose(arm, pose, 0.0, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        spec = p14_at(surface, 13.8)
        p11_clear("P11Fx")
        radius, n = p14_ring(foot, spec, impact, cam.location)
        print("LAND14", title, "r", round(radius, 3), "n", n)
        frame = p11_grab(os.path.join(tmp, "land-%s.png" % title.replace(" ", "-")), 640, 360)
        land_cells.append(frame.resize((420, 236), __import__("PIL").Image.Resampling.LANCZOS))
        land_titles.append("%s  r %.0fcm" % (title, radius * 100.0))
    p14_sheet(land_cells, land_titles, "Land ring, scaled by fall speed", os.path.join(OUT, "dust-land.jpg"))

    slide_cells = []
    slide_titles = []
    for surface in ("concrete", "dirt"):
        p11_ground(surface, light=(surface == "concrete"))
        apply_pose(arm, pose_slide, 0.0, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        spec = p14_at(surface, 13.8, kick=1.25)
        enter = p14_at(surface, 13.8, kick=2.10)
        p11_clear("P11Fx")
        p14_mote(foot, fwd, left, enter, cam.location, 0.7, 3)
        for i in range(7):
            aged = dict(spec)
            aged["opacity"] *= 0.85 - i * 0.09
            origin = foot - fwd * (0.32 * (i + 1))
            p14_mote(origin, fwd, left, aged, cam.location, 1.0, 30 + i)
        print("SLIDE14", surface, "span", round(spec["span"], 3), "enter", round(enter["span"], 3))
        frame = p11_grab(os.path.join(tmp, "slide-%s.png" % surface), 720, 405)
        slide_cells.append(frame)
        slide_titles.append("%s trail" % surface)
    p14_sheet(slide_cells, slide_titles, "Slide trail", os.path.join(OUT, "dust-slide.jpg"))
    print("PASS14 stills", OUT)


P15_AGE = 0.20
P15_CELL = (640, 360)


def p15_disc():
    """Soft falloff. The centre is dense and the edge fades, so it is not a hard circle."""
    img = bpy.data.images.get("P16Disc")
    if img is not None:
        return img
    n = 64
    img = bpy.data.images.new("P16Disc", n, n, alpha=True, float_buffer=True)
    pix = [0.0] * (n * n * 4)
    for y in range(n):
        for x in range(n):
            dx = (x + 0.5) / n - 0.5
            dy = (y + 0.5) / n - 0.5
            r = math.sqrt(dx * dx + dy * dy) / 0.5
            if r >= 1.0:
                alpha = 0.0
            else:
                t = 1.0 - r
                alpha = t * t * (3.0 - 2.0 * t)
            i = (y * n + x) * 4
            pix[i] = pix[i + 1] = pix[i + 2] = 1.0
            pix[i + 3] = alpha
    img.pixels.foreach_set(pix)
    try:
        img.alpha_mode = "STRAIGHT"
    except (TypeError, AttributeError):
        pass
    img.pack()
    img.update()
    return img


def p15_puff_mat(name, color, opacity):
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
    emit = nt.nodes.new("ShaderNodeEmission")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = p15_disc()
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = max(0.0, min(1.0, opacity))
    emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 1.0
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def p15_billboard(loc, diameter, color, opacity, cam_loc):
    """Unlit disc. diameter is the visible width, matching ParticleSystem startSize."""
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=loc)
    obj = bpy.context.active_object
    obj.name = p11_name("Fx")
    direction = cam_loc - loc
    if direction.length < 0.001:
        direction = Vector((0.0, -1.0, 0.4))
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    # Smoothstep is still strong near the middle. 0.55 makes that core the named diameter.
    span = max(diameter, 0.02) / 0.55
    obj.scale = (span, span, 1.0)
    obj.data.materials.append(p15_puff_mat(p11_name("Mat"), color, opacity))
    return obj


def p16_hash(i, salt):
    x = (i * 374761393 + salt * 668265263) & 0xFFFFFFFF
    x = ((x ^ (x >> 13)) * 1274126177) & 0xFFFFFFFF
    return (x & 65535) / 65535.0


def p16_motes(foot, fwd, left, spec, age, curl=False):
    """Low trail. Dense small motes at the plant, thinner and softer as it kicks back.

    curl lifts concrete and wood sprint wisps into a small arch. Span and
    diameter stay on the half-size curve.
    """
    n = spec["count"]
    if n <= 0:
        return []
    # DustLook still prints a 72 cm sprint span. The drawn cloud is half of that,
    # so a sprint trail is about 36 cm, a run is smaller, and a walk is a scrap.
    # Mote diameter uses that same half. Only a puff whose diameter is several
    # times the spacing (dirt) is cut down, and then only to the step ahead,
    # so the pieces stay visible and do not fuse.
    vis = 0.5
    span = (spec["span"] if spec["span"] > 0.05 else spec["back"]) * vis
    life = spec["life"] if spec["life"] > 0.05 else 0.05
    back = spec["back"] * vis
    lift = spec["lift"] * vis
    size0 = spec["size"] * vis
    grav = 0.25 * 9.81
    kick = (-fwd * back + Vector((0.0, 0.0, lift * 0.35))) / life
    gap = span / float(max(n, 1))
    split = size0 > gap * 2.8
    motes = []
    for i in range(n):
        u = 0.0 if n == 1 else i / float(n - 1)
        t = u ** 1.65
        h1 = p16_hash(i, 1)
        h2 = p16_hash(i, 2)
        h3 = p16_hash(i, 3)
        along = span * (0.02 + 0.96 * t)
        spread = span * (0.05 + 0.18 * t)
        side = (h1 - 0.5) * spread + 0.05
        up = 0.025 + lift * (0.10 + 0.28 * t) * (0.40 + 0.60 * h2)
        pos = foot - fwd * along + left * side + Vector((0.0, 0.0, up))
        vel = kick * (0.35 + 0.25 * h2)
        pos = pos + vel * age + Vector((0.0, 0.0, -0.5 * grav * age * age))
        if curl:
            arch = math.sin(math.pi * min(1.0, max(0.0, t)))
            pos = pos + Vector((0.0, 0.0, 0.11 * arch)) + left * (0.04 * arch)
        if pos.z < 0.018:
            pos.z = 0.018
        size = size0 * (0.55 + 0.45 * t) * (0.78 + 0.44 * h3)
        if split:
            u_next = 1.0 if n == 1 else min(1.0, (i + 1) / float(n - 1))
            step = span * 0.96 * max(0.04, u_next ** 1.65 - t)
            size = min(size, step * (0.62 + 0.22 * h3))
        if size < 0.025:
            size = 0.025
        fade = 1.0 - 0.58 * t
        op = spec["opacity"] * fade * (0.82 + 0.18 * h2)
        if op > 0.95:
            op = 0.95
        tint = 0.90 + 0.16 * h1
        color = spec["color"]
        col = (color[0] * tint, color[1] * tint, color[2] * tint)
        motes.append((pos, size, col, op))
        if spec["core"] > 0.15 and i < 2:
            grit = tuple(c * 0.62 for c in color)
            motes.append((pos + Vector((0.0, 0.0, 0.012)), size * 0.62, grit, min(0.9, op)))
    return motes


def p15_move(foot, fwd, left, spec, along, side, up, age):
    """Box birth along Span, then the plume kick for `age` seconds. Gravity 0.25."""
    back = spec["back"]
    lift = spec["lift"]
    aim = -fwd * back + Vector((0.0, 0.0, lift))
    if aim.length < 1e-4:
        aim = -fwd * 0.01 + Vector((0.0, 0.0, 0.01))
    aim = aim.normalized()
    life = spec["life"] if spec["life"] > 0.05 else 0.05
    speed = back / life
    grav = 0.25 * 9.81
    # Bias the cloud to the outside of the plant foot. A centred puff sits inside
    # the calf and the chase camera only catches a sliver.
    pos = foot - fwd * along + left * (side + 0.16) + Vector((0.0, 0.0, 0.05 + up))
    pos = pos + aim * speed * age + Vector((0.0, 0.0, -0.5 * grav * age * age))
    if pos.z < 0.04:
        pos.z = 0.04
    return pos


def p15_plume(foot, fwd, left, spec, cam_loc, age, salt, curl=False):
    """One footfall, aged. A puff whose life has ended is not drawn."""
    if spec["count"] <= 0 or spec["life"] <= age:
        return 0
    made = 0
    for pos, size, col, op in p16_motes(foot, fwd, left, spec, age, curl):
        p15_billboard(pos, size, col, op, cam_loc)
        made += 1
    return made


def p15_ring(foot, spec, impact, cam_loc, age):
    radius, t = p14_ring_radius(impact)
    n = 8 + int(t * 8.0)
    if n > 16:
        n = 16
    if n < 6:
        n = 6
    size = 0.06 + t * 0.10
    op = min(0.90, 0.45 + t * 0.35)
    lift = 0.05 + t * 0.07
    out_sp = 0.35 + lift
    up_sp = 0.20 + lift
    grav = 0.25 * 9.81
    color = spec["color"]
    core = t > 0.45
    for i in range(n):
        ang = i / n * math.tau
        radial = Vector((math.cos(ang), math.sin(ang), 0.0))
        pos = foot + radial * radius + Vector((0.0, 0.0, 0.06))
        pos = pos + radial * out_sp * age + Vector((0.0, 0.0, up_sp * age - 0.5 * grav * age * age))
        if pos.z < 0.04:
            pos.z = 0.04
        p15_billboard(pos, size, color, op, cam_loc)
        if core:
            grit = tuple(c * 0.62 for c in color)
            p15_billboard(pos + Vector((0.0, 0.0, 0.03)), size * 0.55, grit, op, cam_loc)
    return radius, n


def p15_slide(foot, fwd, left, surface, cam_loc):
    """Trail puffs every TrailGap while sliding. Newest is a young plume, older ones linger."""
    gap = 0.045
    speed = 13.8
    step = speed * gap
    made = 0
    for i in range(8):
        age = 0.06 + i * gap
        kick = 2.10 if i == 0 else 1.25
        spec = p14_at(surface, 13.8, kick=kick)
        if spec["life"] <= age:
            continue
        origin = foot - fwd * (step * i)
        origin = Vector((origin.x, origin.y, foot.z))
        made += p15_plume(origin, fwd, left, spec, cam_loc, age, 40 + i * 5)
    return made


def p16_aim_side(cam, foot, yaw_deg):
    """Low camera across the trail, so the plume's height reads against the leg."""
    fwd, left = p11_heading(yaw_deg)
    cam.data.type = "PERSP"
    cam.data.sensor_fit = "VERTICAL"
    cam.data.sensor_height = 24.0
    cam.data.lens = 40.0
    cam.data.clip_start = 0.02
    cam.data.clip_end = 40.0
    cam.location = foot + left * 2.15 - fwd * 0.28 + Vector((0.0, 0.0, 0.42))
    look_at(cam, foot - fwd * 0.32 + Vector((0.0, 0.0, 0.06)))
    bpy.context.view_layer.update()
    return (cam.location - foot).length


def p15_aim_close(cam, foot, yaw_deg, focus_back):
    """Low side view so the trail's length crosses the frame instead of running at the lens."""
    fwd, left = p11_heading(yaw_deg)
    cam.data.type = "PERSP"
    cam.data.sensor_fit = "VERTICAL"
    cam.data.sensor_height = 24.0
    cam.data.lens = 32.0
    cam.data.clip_start = 0.02
    cam.data.clip_end = 40.0
    cam.location = foot - fwd * 0.42 + left * 1.65 + Vector((0.0, 0.0, 0.58))
    look_at(cam, foot - fwd * focus_back + Vector((0.0, 0.0, 0.10)))
    bpy.context.view_layer.update()
    return (cam.location - foot).length


def p15_box(foot, fwd, left, reach, side, up):
    corners = []
    for along in (0.0, reach):
        for s in (-side, side):
            for z in (0.0, up):
                corners.append(foot - fwd * along + left * s + Vector((0.0, 0.0, z)))
    return corners


def p15_rect(cam, corners, width, height):
    from bpy_extras.object_utils import world_to_camera_view

    scene = bpy.context.scene
    xs = []
    ys = []
    for point in corners:
        co = world_to_camera_view(scene, cam, point)
        if co.z <= 0.02:
            continue
        xs.append(co.x * width)
        ys.append((1.0 - co.y) * height)
    if len(xs) < 2:
        return (0, int(height * 0.45), width, height)
    pad = 8
    x0 = max(0, int(min(xs)) - pad)
    x1 = min(width, int(max(xs)) + pad)
    y0 = max(0, int(min(ys)) - pad)
    y1 = min(height, int(max(ys)) + pad)
    if x1 < x0 + 12 or y1 < y0 + 12:
        return (0, int(height * 0.45), width, height)
    return (x0, y0, x1, y1)


def p15_changed(off, on, rect):
    x0, y0, x1, y1 = rect
    a = off.crop((x0, y0, x1, y1)).load()
    b = on.crop((x0, y0, x1, y1)).load()
    w = x1 - x0
    h = y1 - y0
    changed = 0
    total = w * h
    for y in range(h):
        for x in range(w):
            r1, g1, b1 = a[x, y]
            r2, g2, b2 = b[x, y]
            if max(abs(r1 - r2), abs(g1 - g2), abs(b1 - b2)) >= 18:
                changed += 1
    pct = 100.0 * changed / float(total) if total else 0.0
    return pct, changed, total


def p15_pair(stub, width, height, draw):
    p11_clear("P11Fx")
    off = p11_grab(stub + "-off.png", width, height)
    draw()
    on = p11_grab(stub + "-on.png", width, height)
    p11_clear("P11Fx")
    return off, on


def render_pass15(arm, cam):
    """Mid-life dust on mid-grey asphalt. Chase cam plus a close foot crop.

    Pass 14 drew a lit card whose opaque core was 44% of a quad only 42% as
    tall as the mote, on a 0.78 slab the same colour as the dust, with the
    plume running at the lens. The three concrete panels measured the same grey.
    """
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
    tmp = "/tmp/pass15-cells"
    os.makedirs(tmp, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    yaw = 32.0
    fwd, left = p11_heading(yaw)
    surfaces = ("concrete", "dirt", "grass", "wood")
    speeds = (("walk", 6.9), ("run", 9.0), ("sprint", 13.8))
    w, h = P15_CELL
    proof = []
    chase_proof = []
    sprint_close = {}
    sprint_side = {}
    only = os.environ.get("FX_PASS15_ONLY", "")
    if only:
        surfaces = tuple(s for s in surfaces if s == only)

    for surface in surfaces:
        p11_ground(surface, asphalt=(surface == "concrete"))
        chase_cells = []
        close_cells = []
        chase_titles = []
        close_titles = []
        for label, speed in speeds:
            apply_pose(arm, p12_footfall(speed), 0.0, yaw)
            foot = p11_foot(arm)
            spec = p14_at(surface, speed)
            age = P15_AGE
            alive = spec["life"] > age
            dist = p14_aim(cam, foot, yaw)
            # Box follows the drawn cloud, which is half the DustLook span.
            big = p14_at(surface, speed)
            vis = 0.5
            drift = big["back"] * vis / max(big["life"], 0.05) * 0.45 * P15_AGE
            reach = big["span"] * vis + drift + 0.05
            side = max(0.10, big["span"] * vis * 0.28 + 0.05)
            origin = foot
            # 0.26 covers the sprint curl. The trail length is unchanged.
            corners = p15_box(origin, fwd, left, reach, side, 0.26)
            rect = p15_rect(cam, corners, w, h)

            puff = surface in ("concrete", "wood") and speed >= 13.0

            def draw_foot(foot=foot, spec=spec, age=age, alive=alive, surface=surface, speed=speed, puff=puff):
                if alive:
                    p15_plume(foot, fwd, left, spec, cam.location, age, 4, puff)
                if surface == "grass" and alive:
                    p12_flecks(foot - fwd * spec["span"] * 0.35, speed, left, -fwd, scale=0.45)

            off, on = p15_pair(os.path.join(tmp, "%s-%s-chase" % (surface, label)), w, h, draw_foot)
            pct, changed, total = p15_changed(off, on, rect)
            key = "%s-%s" % (surface, label)
            chase_proof.append((key, pct))
            print(
                "DUST15", surface, label, "chase",
                "age", round(age, 2),
                "alive", int(alive),
                "size", round(spec["size"], 3),
                "op", round(spec["opacity"], 3),
                "span", round(spec["span"], 3),
                "cam", round(dist, 2),
                "pct", round(pct, 2),
                "px", changed, "/", total,
            )
            chase_cells.append(on)
            chase_titles.append("%s chase  %.0fcm  %s" % (
                label, spec["span"] * 50.0, "gone" if not alive else "%.1f%%" % pct))

            dist_c = p15_aim_close(cam, foot, yaw, max(0.25, spec["span"] * 0.45))
            rect_c = p15_rect(cam, corners, w, h)
            off_c, on_c = p15_pair(os.path.join(tmp, "%s-%s-close" % (surface, label)), w, h, draw_foot)
            pct_c, changed_c, total_c = p15_changed(off_c, on_c, rect_c)
            proof.append((key, pct_c))
            print(
                "DUST15", surface, label, "close",
                "cam", round(dist_c, 2),
                "pct", round(pct_c, 2),
                "px", changed_c, "/", total_c,
            )
            close_cells.append(on_c)
            close_titles.append("%s close  %.0fcm  %.1f%%" % (label, spec["span"] * 50.0, pct_c))
            if label == "sprint":
                sprint_close[surface] = on_c
                p16_aim_side(cam, foot, yaw)
                _off_s, on_s = p15_pair(
                    os.path.join(tmp, "%s-sprint-side" % surface), w, h, draw_foot)
                sprint_side[surface] = on_s
        p14_grid(
            chase_cells + close_cells,
            chase_titles + close_titles,
            "%s   chase (top) and close foot (bottom)   age %.2fs" % (surface.upper(), P15_AGE),
            os.path.join(OUT, "dust-%s.jpg" % surface),
            3,
        )

    if not only:
        crops = []
        crop_titles = []
        for surface in ("concrete", "dirt", "grass", "wood"):
            crops.append(sprint_close[surface])
            pct = dict(proof)["%s-sprint" % surface]
            spec = p14_at(surface, 13.8)
            crop_titles.append("%s sprint close  %.0fcm  %.1f%%" % (surface, spec["span"] * 50.0, pct))
        p14_grid(crops, crop_titles, "Close foot crop, sprint, four surfaces", os.path.join(OUT, "dust-split.jpg"), 2)
    sides = []
    side_titles = []
    for surface in ("concrete", "dirt", "grass", "wood"):
        if surface not in sprint_side:
            continue
        sides.append(sprint_side[surface])
        spec = p14_at(surface, 13.8)
        side_titles.append("%s sprint side  %.0fcm" % (surface, spec["span"] * 50.0))
    if sides:
        cols = 2 if len(sides) > 1 else 1
        p14_grid(sides, side_titles, "Side view, sprint plume, low to the ground", os.path.join(OUT, "dust-side.jpg"), cols)

    land_jobs = (
        ("concrete", pose_land, 8.0, "concrete light"),
        ("concrete", pose_land, 36.5, "concrete hard"),
        ("dirt", pose_land, 36.5, "dirt hard"),
        ("dirt", pose_roll, 36.5, "dirt roll"),
    )
    land_chase = []
    land_close = []
    land_chase_t = []
    land_close_t = []
    if only:
        land_jobs = ()
    for surface, pose, impact, title in land_jobs:
        p11_ground(surface, asphalt=(surface == "concrete"))
        apply_pose(arm, pose, 0.0, yaw)
        foot = p11_foot(arm)
        spec = p14_at(surface, 13.8)
        radius, _t = p14_ring_radius(impact)
        reach = radius + 0.45
        corners = p15_box(foot, fwd, left, reach, reach, 0.6)
        # The ring is around the foot, not only behind it. Use a square on the ground.
        ring_corners = []
        for sx in (-reach, reach):
            for sy in (-reach, reach):
                for z in (0.0, 0.55):
                    ring_corners.append(foot + Vector((sx, sy, z)))

        def draw_ring(foot=foot, spec=spec, impact=impact):
            p15_ring(foot, spec, impact, cam.location, P15_AGE)

        p14_aim(cam, foot, yaw)
        rect = p15_rect(cam, ring_corners, w, h)
        off, on = p15_pair(os.path.join(tmp, "land-%s-chase" % title.replace(" ", "-")), w, h, draw_ring)
        pct, changed, total = p15_changed(off, on, rect)
        print("LAND15", title, "chase", "r", round(radius, 3), "pct", round(pct, 2), "px", changed, "/", total)
        land_chase.append(on)
        land_chase_t.append("%s chase  r %.0fcm  %.1f%%" % (title, radius * 100.0, pct))
        proof.append(("land-%s" % title.replace(" ", "-"), pct))

        p15_aim_close(cam, foot, yaw, 0.15)
        # Pull back so the whole ring fits. The side camera is for the foot plume.
        fwd_l, left_l = p11_heading(yaw)
        cam.location = foot - fwd_l * (1.15 + radius * 0.35) + left_l * (1.35 + radius * 0.55) + Vector((0.0, 0.0, 0.85 + radius * 0.25))
        look_at(cam, foot + Vector((0.0, 0.0, 0.05)))
        bpy.context.view_layer.update()
        rect_c = p15_rect(cam, ring_corners, w, h)
        off_c, on_c = p15_pair(os.path.join(tmp, "land-%s-close" % title.replace(" ", "-")), w, h, draw_ring)
        pct_c, changed_c, total_c = p15_changed(off_c, on_c, rect_c)
        print("LAND15", title, "close", "pct", round(pct_c, 2), "px", changed_c, "/", total_c)
        land_close.append(on_c)
        land_close_t.append("%s close  r %.0fcm  %.1f%%" % (title, radius * 100.0, pct_c))
        proof.append(("land-%s-close" % title.replace(" ", "-"), pct_c))
    if land_chase:
        p14_grid(
            land_chase + land_close,
            land_chase_t + land_close_t,
            "Land ring at %.2fs   chase (top) close (bottom)" % P15_AGE,
            os.path.join(OUT, "dust-land.jpg"),
            4,
        )

    slide_cells = []
    slide_titles = []
    slide_surfaces = () if only else ("concrete", "dirt")
    for surface in slide_surfaces:
        p11_ground(surface, asphalt=(surface == "concrete"))
        apply_pose(arm, pose_slide, 0.0, yaw)
        foot = p11_foot(arm)
        corners = p15_box(foot, fwd, left, 2.6, 0.7, 0.55)

        def draw_slide(foot=foot, surface=surface):
            p15_slide(foot, fwd, left, surface, cam.location)

        p14_aim(cam, foot, yaw)
        rect = p15_rect(cam, corners, w, h)
        off, on = p15_pair(os.path.join(tmp, "slide-%s-chase" % surface), w, h, draw_slide)
        pct, changed, total = p15_changed(off, on, rect)
        print("SLIDE15", surface, "chase", "pct", round(pct, 2), "px", changed, "/", total)
        slide_cells.append(on)
        slide_titles.append("%s chase  %.1f%%" % (surface, pct))
        proof.append(("slide-%s" % surface, pct))

        p15_aim_close(cam, foot, yaw, 0.9)
        rect_c = p15_rect(cam, corners, w, h)
        off_c, on_c = p15_pair(os.path.join(tmp, "slide-%s-close" % surface), w, h, draw_slide)
        pct_c, changed_c, total_c = p15_changed(off_c, on_c, rect_c)
        print("SLIDE15", surface, "close", "pct", round(pct_c, 2), "px", changed_c, "/", total_c)
        slide_cells.append(on_c)
        slide_titles.append("%s close  %.1f%%" % (surface, pct_c))
        proof.append(("slide-%s-close" % surface, pct_c))
    if slide_cells:
        p14_grid(slide_cells, slide_titles, "Slide trail", os.path.join(OUT, "dust-slide.jpg"), 2)

    parts = ["%s=%.1f%%" % (key, pct) for key, pct in proof]
    print("dust-visible " + " ".join(parts))
    chase_parts = ["%s=%.1f%%" % (key, pct) for key, pct in chase_proof]
    print("dust-visible-chase " + " ".join(chase_parts))
    fails = []
    table = dict(proof)
    walk_pct = table.get("concrete-walk", 0.0)
    sprint_pct = table.get("concrete-sprint", 0.0)
    if walk_pct > 2.0:
        fails.append("concrete-walk %.1f" % walk_pct)
    if sprint_pct < 8.0:
        fails.append("concrete-sprint %.1f" % sprint_pct)
    hard = table.get("land-concrete-hard-close", 0.0)
    if land_jobs and hard < 3.0:
        fails.append("land-hard %.1f" % hard)
    if fails:
        print("DUST15 FAIL", " ".join(fails))
    else:
        print("DUST15 PASS")
    print("PASS15 stills", OUT)


def impact_state(surface, speed, age_u=0.45):
    """Same curve as ImpactFx.Measure. age_u is how far through the short life."""
    spec = p14_at(surface, speed)
    u = speed / 13.8
    if u < 0.0:
        u = 0.0
    if u > 2.8:
        u = 2.8
    radius = 0.35 + u * 0.45
    debris = spec["size"]
    if debris < 0.04:
        debris = 0.04
    debris *= 0.65 + u * 0.55
    bits = spec["count"]
    if bits < 3:
        bits = 3
    bits += int(u * 3.0)
    if bits > 12:
        bits = 12
    life_u = 2.0 if u > 2.0 else u
    life = 0.20 + life_u * 0.04
    opacity = spec["opacity"]
    if opacity < 0.35:
        opacity = 0.35
    if opacity > 0.90:
        opacity = 0.90
    grow = 0.28 + 0.92 * age_u
    fade = (1.0 - age_u) / 0.88 if age_u >= 0.12 else age_u / 0.12
    return {
        "radius": radius,
        "shown": radius * grow,
        "debris": debris,
        "bits": bits,
        "life": life,
        "age": age_u * life,
        "opacity": opacity * fade,
        "color": spec["color"],
        "out": 1.1 + u * 0.85,
        "up": 1.4 + u * 0.55,
        "u": u,
    }


def impact_draw(foot, state, cam_loc):
    shown = state["shown"]
    color = state["color"]
    ring_col = color + (1.0,)
    bpy.ops.mesh.primitive_torus_add(
        major_radius=shown,
        minor_radius=max(0.02, shown * 0.055),
        major_segments=40,
        minor_segments=8,
        location=(foot.x, foot.y, 0.03),
    )
    ring = bpy.context.active_object
    ring.name = p11_name("Fx")
    ring.data.materials.append(make_mat(
        p11_name("Mat"), ring_col, 0.4, max(0.35, state["opacity"]), emit=0.55,
    ))
    shock = tuple(min(1.0, c * 0.35 + 0.62) for c in color) + (1.0,)
    bpy.ops.mesh.primitive_torus_add(
        major_radius=shown * 1.18,
        minor_radius=max(0.012, shown * 0.028),
        major_segments=40,
        minor_segments=6,
        location=(foot.x, foot.y, 0.035),
    )
    outer = bpy.context.active_object
    outer.name = p11_name("Fx")
    outer.data.materials.append(make_mat(
        p11_name("Mat"), shock, 0.3, state["opacity"] * 0.7, emit=0.8,
    ))
    age = state["age"]
    bits = state["bits"]
    for i in range(bits):
        ang = i / float(bits) * math.tau
        hop = 0.75 + (0.35 if (i & 1) else 0.0)
        dist = state["radius"] * 0.18 + state["out"] * age
        z = 0.05 + state["up"] * hop * age - 0.5 * 12.0 * age * age
        if z < 0.03:
            z = 0.03
        pos = Vector((foot.x + math.cos(ang) * dist, foot.y + math.sin(ang) * dist, z))
        tint = 0.82 + 0.18 * p11_rand(i, 5)
        col = (color[0] * tint, color[1] * tint, color[2] * tint, 1.0)
        puff = state["debris"]
        p11_puff(pos, puff, puff * 0.72, col, min(0.92, state["opacity"] + 0.08), cam_loc)
        if i % 3 == 0:
            grit = tuple(c * 0.62 for c in color) + (1.0,)
            p11_puff(pos + Vector((0.0, 0.0, 0.03)), puff * 0.45, puff * 0.4, grit, state["opacity"], cam_loc)


def render_pass21_impact(arm, cam):
    """Hard-land / wall-slam ring and debris. One still per surface at low and high speed."""
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "SUN":
            obj.data.energy = 2.6
        elif obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 48
        if obj.name in ("PropGround", "PropSlab") or "Seam" in obj.name:
            obj.hide_render = True
    scene = bpy.context.scene
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    scene.eevee.taa_render_samples = 8
    out_dir = os.path.join(ROOT, "Docs", "FxStills", "pass21")
    os.makedirs(out_dir, exist_ok=True)
    tmp = "/tmp/pass21-impact"
    os.makedirs(tmp, exist_ok=True)
    surfaces = ("concrete", "grass", "dirt", "wood")
    levels = (("low", 13.8), ("high", 36.5))
    yaw = 24.0
    cells = []
    titles = []
    from PIL import Image

    for surface in surfaces:
        for label, speed in levels:
            p11_ground(surface, asphalt=(surface == "concrete"))
            apply_pose(arm, pose_land, 0.0, yaw)
            foot = p11_foot(arm)
            p14_aim(cam, foot, yaw)
            state = impact_state(surface, speed)
            impact_draw(foot, state, cam.location)
            png = os.path.join(tmp, "%s-%s.png" % (surface, label))
            scene.render.filepath = png
            bpy.ops.render.render(write_still=True)
            image = Image.open(png).convert("RGB")
            path = os.path.join(out_dir, "impact-%s-%s.jpg" % (surface, label))
            p14_jpeg(path, image)
            cells.append(image.resize((320, 180), Image.Resampling.LANCZOS))
            titles.append("%s %s  r %.0fcm  bits %d" % (
                surface, label, state["shown"] * 100.0, state["bits"],
            ))
            print(
                "IMPACT", surface, label,
                "u", round(state["u"], 2),
                "shown", round(state["shown"], 2),
                "debris", round(state["debris"], 3),
                "bits", state["bits"],
                "op", round(state["opacity"], 2),
            )
    p14_grid(
        cells, titles,
        "Impact ring + debris   low sprint 13.8   high land 36.5",
        os.path.join(out_dir, "impact-sheet.jpg"),
        4,
    )


def render_pass20_runners(arm, cam):
    """Attacker and victim, chase camera, transparent, for the arena couch composite."""
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.name.startswith(("Prop", "Park")):
            obj.hide_render = True
    scene = bpy.context.scene
    scene.render.film_transparent = True
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    out_dir = os.path.join(ROOT, "Docs", "FxStills", "pass20", "runners")
    os.makedirs(out_dir, exist_ok=True)
    layout_path = os.path.join(out_dir, "layout.txt")
    if os.path.exists(layout_path):
        os.remove(layout_path)
    from bpy_extras.object_utils import world_to_camera_view

    def project(world):
        p = world_to_camera_view(scene, cam, world)
        return p.x * 640.0, (1.0 - p.y) * 360.0

    shots = (
        ("attacker", pose_punch, 24.0),
        ("victim", pose_stagger, -150.0),
    )
    for name, pose, yaw in shots:
        apply_pose(arm, pose, yaw=yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        foot = p11_foot(arm)
        chest = bone_pos(arm, "Spine")
        head = bone_pos(arm, "Head", tail=True)
        fx, fy = project(foot)
        cx, cy = project(chest)
        hx, hy = project(head)
        path = os.path.join(out_dir, name + ".png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        print("SIZE", name, os.path.getsize(path))
        line = "RUNNER %s foot %.1f %.1f chest %.1f %.1f head %.1f %.1f body %.1f" % (
            name, fx, fy, cx, cy, hx, hy, fy - hy,
        )
        print(line)
        with open(os.path.join(out_dir, "layout.txt"), "a") as handle:
            handle.write(line + "\n")


def render_pass19_park(arm, cam):
    """Chase views of the runner on a Mega Park graybox. Prints the comic quad in pixels."""
    for obj in list(bpy.data.objects):
        if obj.name.startswith("PropSeam"):
            obj.hide_render = True
    ground = bpy.data.objects.get("PropGround")
    if ground is not None:
        ground.scale = (4.0, 4.0, 1.0)
        mat = ground.data.materials[0]
        ramp = mat.node_tree.nodes.get("ColorRamp")
        if ramp is not None:
            ramp.color_ramp.elements[0].color = (0.16, 0.38, 0.14, 1.0)
            ramp.color_ramp.elements[1].color = (0.28, 0.52, 0.18, 1.0)
    # Tan path and a couple of park blocks, same family as the Mega Park plates.
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 2.5, 0.02))
    path = bpy.context.active_object
    path.name = "ParkPath"
    path.scale = (2.2, 14.0, 0.02)
    path.data.materials.append(make_mat("ParkPathMat", (0.62, 0.48, 0.28, 1.0), 0.9))
    for name, loc, scale, color in (
        ("ParkBlue", (6.5, 8.0, 1.1), (2.2, 2.2, 2.2), (0.22, 0.42, 0.78, 1.0)),
        ("ParkOrange", (-5.5, 7.0, 0.7), (1.6, 1.6, 1.4), (0.78, 0.38, 0.14, 1.0)),
        ("ParkBlock", (3.2, -2.0, 0.55), (1.2, 1.2, 1.1), (0.45, 0.48, 0.52, 1.0)),
    ):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
        block = bpy.context.active_object
        block.name = name
        block.scale = scale
        block.data.materials.append(make_mat(name + "Mat", color, 0.85))
    scene = bpy.context.scene
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    out_dir = os.path.join("/tmp", "pass19_plates")
    os.makedirs(out_dir, exist_ok=True)
    shots = (
        ("sproing", pose_run, 18.0),
        ("whizz", pose_run, 150.0),
        ("pow", pose_punch, -24.0),
        ("smack", pose_punch, 64.0),
    )
    from bpy_extras.object_utils import world_to_camera_view
    for name, pose, yaw in shots:
        apply_pose(arm, pose, yaw=yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        fwd, _left = p11_heading(yaw)
        contact = foot + fwd * 1.35 + Vector((0.0, 0.0, 1.25))
        up = cam.matrix_world.to_quaternion() @ Vector((0.0, 1.0, 0.0))
        up.normalize()
        p0 = world_to_camera_view(scene, cam, contact)
        p1 = world_to_camera_view(scene, cam, contact + up * 1.65)
        pix = abs(p1.y - p0.y) * 360.0
        sx = p0.x * 640.0
        sy = (1.0 - p0.y) * 360.0
        render_to(os.path.join(out_dir, name + ".png"))
        print("PLATE", name, "px", round(pix, 1), "xy", round(sx, 1), round(sy, 1))


def impact22_strength(speed):
    span = 36.5 - 13.8
    k = (speed - 13.8) / span
    if k < 0.0:
        k = 0.0
    if k > 1.15:
        k = 1.15
    return k


def impact22_state(surface, speed, age_u=0.56, legacy=False, radius_override=None, bits_override=None, chunky=False):
    """Same curve as ImpactFx.Measure. age_u 0.56 is the peak-expansion frame.

    The ring is a donut: texture inner 0.78 over outer 0.96, so the hole is
    81% of the outer radius. Alpha falls as the ring grows.
    """
    k = impact22_strength(speed)
    radius = 1.45 + k * 1.85
    chunk = 0.34 + k * 0.22
    bits = 5 + int(k * 14.0)
    if bits < 5:
        bits = 5
    if bits > 30:
        bits = 30
    life = 0.25
    hop_lo = 0.04 + k * 0.26
    hop_hi = 0.10 + k * 0.70
    if surface == "grass":
        bit = (0.34, 0.40, 0.16)
        dust = (0.58, 0.64, 0.30)
        plumes = 0
        kind = "grass"
    elif surface == "dirt":
        bit = (0.42, 0.26, 0.12)
        dust = (0.72, 0.50, 0.26)
        plumes = impact25_plumes(k, True)
        kind = "chunk"
    elif surface == "wood":
        bit = (0.62, 0.48, 0.28)
        dust = (0.80, 0.70, 0.52)
        plumes = impact25_plumes(k, False)
        kind = "wood"
    else:
        bit = (0.50, 0.49, 0.47)
        dust = (0.76, 0.75, 0.72)
        plumes = impact25_plumes(k, True)
        kind = "chunk"
    grow = 1.0 if age_u >= 0.56 else 0.22 + 0.78 * (1.0 - (1.0 - age_u / 0.56) ** 2)
    fade = 1.0 if age_u <= 0.56 else 1.0 - (age_u - 0.56) / 0.44
    if fade < 0.0:
        fade = 0.0
    thin = 1.0 - 0.55 * grow
    if thin < 0.2:
        thin = 0.2
    base_op = (0.42 + k * 0.48) if legacy else (0.12 + k * 0.10)
    ring_inner = 0.78 if legacy else 0.74
    state = {
        "radius": radius,
        "shown": radius * grow,
        "chunk": chunk,
        "bits": bits,
        "plumes": plumes,
        "life": life,
        "age": age_u * life,
        "opacity": base_op * fade * thin,
        "legacy": legacy,
        "ring_inner": ring_inner,
        "plume": min(0.72, fade * 0.72),
        "bit": bit,
        "dust": dust,
        "kind": kind,
        "hop_lo": hop_lo,
        "hop_hi": hop_hi,
        "k": k,
        "u": age_u,
        "grow": grow,
        "inner": 0.78 / 0.96,
        "chunky": chunky,
    }
    if radius_override is not None:
        state["radius"] = radius_override
        state["shown"] = radius_override * grow
    if bits_override is not None:
        state["bits"] = bits_override
    return state


def impact22_bit(i, state, salt):
    """World offset from the impact at the still's age. Matches ImpactFx hops."""
    h = p11_rand(i, salt)
    h2 = p11_rand(i, salt + 4)
    age = state["age"]
    chunky = bool(state.get("chunky")) and state["k"] >= 0.75
    accent = (not chunky) and state["k"] >= 0.85 and i < 4
    if chunky:
        hop = state["hop_hi"] * (0.55 + 0.30 * h2)
    elif accent:
        hop = state["hop_hi"] * 0.72 + (state["hop_hi"] - state["hop_hi"] * 0.72) * h2
    else:
        hop = state["hop_lo"] + (state["hop_hi"] - state["hop_lo"]) * h2
    vy = math.sqrt(2.0 * 48.0 * hop)
    k = state["k"]
    spread = (0.8 + k * 2.2) * (0.4 + h)
    ang = (i + h * 0.35) / float(max(state["bits"], 1)) * math.tau
    if chunky:
        # Start outside the body. A full spread*age plants them in the torso.
        dist = (1.15 + h * 0.35) + spread * age * 0.20
    else:
        dist = (0.04 + h * 0.05) + spread * age
    z = 0.04 + vy * age - 0.5 * 48.0 * age * age
    # Landed chips fade out. Nothing is left sitting in a pile.
    if z < 0.03:
        flight = (vy / 48.0) * 2.0 if vy > 0.01 else 0.0
        settled = age - flight
        if settled > 0.08:
            z = -1.0
        else:
            z = 0.03
    return ang, dist, z, h, h2


def impact22_unlit(name, color, alpha, strength):
    """Emission is the surface colour, so a pale ring does not blow out to white."""
    mat = make_mat(name, (0.0, 0.0, 0.0, 1.0), 1.0, alpha, emit=strength)
    node = mat.node_tree.nodes.get("Principled BSDF")
    key = "Emission Color" if "Emission Color" in node.inputs else None
    if key:
        node.inputs[key].default_value = (color[0], color[1], color[2], 1.0)
    return mat


def impact23_ring_image(inner=0.90):
    """Donut mask. Outer 0.96. Pass 26 uses a narrower band than 0.78."""
    name = "ImpactRing%.2f" % inner
    cached = bpy.data.images.get(name)
    if cached is not None:
        return cached
    n = 128
    image = bpy.data.images.new(name, width=n, height=n, alpha=True, float_buffer=False)
    image.colorspace_settings.name = "Non-Color"
    mid = (n - 1) * 0.5
    outer = 0.96
    edge = 0.035 if inner < 0.85 else 0.012
    pixels = [0.0] * (n * n * 4)
    for y in range(n):
        for x in range(n):
            dx = (x - mid) / mid
            dy = (y - mid) / mid
            r = math.sqrt(dx * dx + dy * dy)
            a = 0.0
            if inner <= r <= outer:
                rise = (r - inner) / edge
                fall = (outer - r) / edge
                if rise > 1.0:
                    rise = 1.0
                if fall > 1.0:
                    fall = 1.0
                a = rise if rise < fall else fall
            i = (y * n + x) * 4
            pixels[i] = 1.0
            pixels[i + 1] = 1.0
            pixels[i + 2] = 1.0
            pixels[i + 3] = a
    image.pixels.foreach_set(pixels)
    image.pack()
    return image


def impact23_ring_mat(name, color, alpha, inner=0.90):
    """Flat decal. The hole and the ground under the band stay visible."""
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
    tex.image = impact23_ring_image(inner)
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = alpha
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    diff.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    diff.inputs["Roughness"].default_value = 1.0
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 0.35
    add = nt.nodes.new("ShaderNodeAddShader")
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(diff.outputs["BSDF"], add.inputs[0])
    nt.links.new(emit.outputs["Emission"], add.inputs[1])
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(add.outputs["Shader"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def impact25_plumes(k, heavy):
    """Sprint is a wisp. Concrete and dirt hard lands are 6–10 puffs."""
    if not heavy:
        return min(8, 2 + int(k * 2.0))
    if k < 0.25:
        return 2
    n = 6 + int(k * 2.5)
    if n > 10:
        n = 10
    return n


def impact25_center(age, peak):
    if age <= 0.12:
        t = 0.0 if age < 0.0 else age / 0.12
        t = 1.0 - (1.0 - t) * (1.0 - t)
        return 0.05 + (peak - 0.05) * t
    u = (age - 0.12) / (0.50 - 0.12)
    if u < 0.0:
        u = 0.0
    if u > 1.0:
        u = 1.0
    return peak * (1.0 - 0.22 * u)


def impact25_alpha(age, hard):
    body = 0.72 if hard else 0.28
    if age <= 0.03:
        return body * (age / 0.03)
    if age >= 0.50:
        return 0.0
    if age <= 0.16:
        return body
    u = (age - 0.16) / (0.50 - 0.16)
    return body * (1.0 - u)


def impact25_accent(pos, size, color, spin):
    """10–12 cm chunk. Longest axis is `size` metres, mildly irregular."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=pos)
    obj = bpy.context.active_object
    obj.name = p11_name("Fx")
    obj.scale = (
        size * (0.82 + p11_rand(spin, 1) * 0.18),
        size * (0.62 + p11_rand(spin, 2) * 0.22),
        size * (0.48 + p11_rand(spin, 3) * 0.28),
    )
    obj.rotation_euler = (
        p11_rand(spin, 4) * 2.4,
        p11_rand(spin, 5) * 2.0,
        spin * 0.31,
    )
    obj.data.materials.append(impact22_unlit(p11_name("Mat"), color, 1.0, 1.0))
    return obj


def impact24_size(h2):
    """Most chips 2–4 cm. The square bias keeps an 8 cm chunk rare."""
    size = 0.02 + (h2 * h2) * 0.06
    if size > 0.08:
        size = 0.08
    return size


def impact24_shade(color, h):
    """Luminance only, so a gray stays gray and a brown stays brown."""
    v = 0.82 + h * 0.28
    return (color[0] * v, color[1] * v, color[2] * v)


def impact24_chip(pos, size, color, kind, spin):
    """Irregular low-poly chip. Unlit, so the key light cannot tint it pink or mint."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=pos)
    obj = bpy.context.active_object
    obj.name = p11_name("Fx")
    for i, vert in enumerate(obj.data.vertices):
        vert.co.x *= 0.45 + p11_rand(i, spin) * 0.7
        vert.co.y *= 0.32 + p11_rand(i, spin + 2) * 0.5
        vert.co.z *= 0.22 + p11_rand(i, spin + 4) * 0.4
        vert.co.x += (p11_rand(i, spin + 1) - 0.5) * 0.28
        vert.co.y += (p11_rand(i, spin + 3) - 0.5) * 0.22
        vert.co.z += (p11_rand(i, spin + 5) - 0.5) * 0.16
    if kind == "splinter":
        obj.scale = (size * 0.18, size * 0.12, size)
    elif kind == "clip":
        obj.scale = (size, size * 0.16, size * 0.28)
    else:
        obj.scale = (
            size * (0.75 + p11_rand(spin, 6) * 0.55),
            size * (0.5 + p11_rand(spin, 7) * 0.45),
            size * (0.35 + p11_rand(spin, 8) * 0.4),
        )
    obj.rotation_euler = (
        p11_rand(spin, 9) * 3.0,
        p11_rand(spin, 10) * 2.4,
        spin * 0.17,
    )
    obj.data.materials.append(impact22_unlit(p11_name("Mat"), color, 1.0, 1.0))
    return obj


def impact24_puff(pos, size, color, alpha, cam_loc):
    """Soft unlit dust. The hole in the ring stays clear because these sit around it."""
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=pos)
    obj = bpy.context.active_object
    obj.name = p11_name("Fx")
    direction = cam_loc - pos
    if direction.length < 0.001:
        direction = Vector((0.0, -1.0, 0.2))
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.scale = (max(size * 1.15, 0.08), max(size, 0.06), 1.0)
    mat = bpy.data.materials.new(p11_name("Mat"))
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.use_backface_culling = False
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = p11_soft_image()
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = alpha
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 0.85
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    obj.data.materials.append(mat)


def impact22_draw(origin, normal, state, cam_loc):
    shown = state["shown"]
    dust = state["dust"]
    bit_col = state["bit"]
    n = Vector(normal).normalized()
    up = Vector((0.0, 0.0, 1.0))
    quat = up.rotation_difference(n)
    # Plane is 1 unit across. Texture outer sits at 0.96 of the half-extent,
    # so the visible outer radius equals `shown` and the hole is 81% of that.
    diameter = shown * 2.0 / 0.96
    bpy.ops.mesh.primitive_plane_add(
        size=1.0,
        location=origin + n * 0.03,
        rotation=quat.to_euler(),
    )
    ring = bpy.context.active_object
    ring.name = p11_name("Fx")
    ring.scale = (diameter, diameter, 1.0)
    ring.data.materials.append(impact23_ring_mat(
        p11_name("Mat"), dust, state["opacity"], state.get("ring_inner", 0.90),
    ))
    kind = state["kind"]
    pace = state["k"]
    for i in range(state["bits"]):
        ang, dist, z, h, h2 = impact22_bit(i, state, 5)
        if z < 0.0:
            continue
        radial = Vector((math.cos(ang), math.sin(ang), 0.0))
        if abs(n.z) < 0.75:
            along = Vector((-n.y, n.x, 0.0))
            if along.length < 0.001:
                along = Vector((1.0, 0.0, 0.0))
            along.normalize()
            slide = math.cos(ang) * (0.8 + pace * 2.2) * (0.4 + h) * state["age"]
            off = 0.12 + (0.8 + pace * 2.2) * (0.45 + h * 0.3) * state["age"]
            pos = origin + along * slide + n * off
            pos.z = origin.z + max(0.03, z - 0.04)
        else:
            pos = origin + radial * dist
            pos.z = z
        chunky = bool(state.get("chunky")) and pace >= 0.75
        if chunky and abs(n.z) >= 0.75:
            # Foreground arc. Far enough forward that the body does not cover them.
            to = Vector(cam_loc) - Vector(origin)
            to.z = 0.0
            if to.length < 0.001:
                to = Vector((0.0, -1.0, 0.0))
            to.normalize()
            side = Vector((-to.y, to.x, 0.0))
            mid = (max(state["bits"], 1) - 1) * 0.5
            pos = Vector(origin) + to * 1.25 + side * ((i - mid) * 0.62)
            pos.z = 0.50 + (i % 2) * 0.18
        accent = (not chunky) and pace >= 0.85 and i < 4
        if chunky:
            size = 0.36 + h2 * 0.08
        elif accent:
            size = 0.10 + h2 * 0.02
        else:
            size = impact24_size(h2)
        if chunky or accent:
            print("CHUNK", "age", round(state["age"], 3), "i", i, "z", round(float(pos.z), 2), "cm", round(size * 100.0, 1))
        dirt_clod = (not accent) and kind == "grass" and (i % 4) == 0
        if chunky:
            if (i % 2) == 0:
                col = (0.05, 0.04, 0.04)
            else:
                col = (1.0, 0.98, 0.94)
            pass27_solid(pos, size, col, i + 1)
        elif accent:
            col = impact24_shade(bit_col, h)
            impact25_accent(pos, size, col, i + 1)
        elif dirt_clod:
            col = impact24_shade((0.42, 0.26, 0.12), h)
            impact24_chip(pos, size, col, "chip", i + 20)
        elif kind == "wood":
            col = impact24_shade(bit_col, h)
            impact24_chip(pos, size, col, "splinter", i + 3)
        elif kind == "grass":
            col = impact24_shade(bit_col, h)
            impact24_chip(pos, size * 0.85, col, "clip", i + 5)
        else:
            col = impact24_shade(bit_col, h)
            impact24_chip(pos, size, col, "chip", i + 1)
    hard = pace >= 0.75
    legacy = bool(state.get("legacy"))
    tops = []
    for i in range(state["plumes"]):
        h = p11_rand(i, 11)
        h2 = p11_rand(i, 15)
        age = state["age"]
        if legacy:
            peak = (0.42 + h2 * 0.24) if hard else (0.08 + h2 * 0.06)
            spread = (0.10 if hard else 0.04) + h * (0.22 if hard else 0.06)
            spread += age * (0.45 if hard else 0.12)
            puff0 = (0.50 + h * 0.22) if hard else (0.16 + h * 0.06)
        else:
            peak = (0.56 + h2 * 0.08) if hard else (0.08 + h2 * 0.06)
            spread = (0.22 if hard else 0.04) + h * (0.48 if hard else 0.06)
            spread += age * (0.85 if hard else 0.12)
            puff0 = (0.66 + h * 0.04) if hard else (0.16 + h * 0.06)
        center = impact25_center(age, peak)
        ang = h * math.tau
        grow = age / 0.12
        if grow < 0.0:
            grow = 0.0
        if grow > 1.0:
            grow = 1.0
        grow = 0.72 + 0.28 * grow
        puff = puff0 * grow
        cap = 1.0 - center
        if cap < 0.05:
            cap = 0.05
        if (not legacy) and puff > cap * 2.0:
            puff = cap * 2.0
        if legacy:
            alpha = impact25_alpha(age, hard)
        else:
            body = 0.94 if hard else 0.28
            if age <= 0.04:
                alpha = body * (age / 0.04)
            elif age >= 0.50:
                alpha = 0.0
            elif age <= 0.16:
                alpha = body
            else:
                alpha = body * (1.0 - (age - 0.16) / (0.50 - 0.16))
        tops.append(center + puff * 0.5)
        radial = Vector((math.cos(ang), math.sin(ang), 0.0))
        if abs(n.z) < 0.75:
            along = Vector((-n.y, n.x, 0.0))
            if along.length < 0.001:
                along = Vector((1.0, 0.0, 0.0))
            along.normalize()
            pos = origin + along * math.cos(ang) * spread + n * center
        else:
            pos = origin + radial * spread
            pos.z = origin.z + center
        impact24_puff(pos, puff, dust, alpha, cam_loc)
    if tops:
        print("PLUME", "age", round(state["age"], 3), "puffs", len(tops), "top", round(max(tops), 2))


def render_pass22_impact(arm, cam):
    """Chase-cam impact sheet at peak expansion, a concrete time strip, and one wall slam."""
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "SUN":
            obj.data.energy = 1.4
        elif obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 28
        if obj.name in ("PropGround", "PropSlab") or "Seam" in obj.name:
            obj.hide_render = True
    scene = bpy.context.scene
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    scene.eevee.taa_render_samples = 8
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    out_dir = os.path.join(ROOT, "Docs", "FxStills", "pass25")
    os.makedirs(out_dir, exist_ok=True)
    tmp = "/tmp/pass25-impact"
    os.makedirs(tmp, exist_ok=True)
    surfaces = ("concrete", "grass", "dirt", "wood")
    levels = (("low", 13.8), ("high", 36.5))
    yaw = 24.0
    cells = []
    titles = []
    from PIL import Image

    for surface in surfaces:
        for label, speed in levels:
            p11_ground(surface, asphalt=(surface == "concrete"))
            apply_pose(arm, pose_land, 0.0, yaw)
            foot = p11_foot(arm)
            p14_aim(cam, foot, yaw)
            state = impact22_state(surface, speed)
            impact22_draw(Vector((foot.x, foot.y, 0.02)), (0.0, 0.0, 1.0), state, cam.location)
            png = os.path.join(tmp, "%s-%s.png" % (surface, label))
            scene.render.filepath = png
            bpy.ops.render.render(write_still=True)
            image = Image.open(png).convert("RGB")
            path = os.path.join(out_dir, "impact-%s-%s.jpg" % (surface, label))
            p14_jpeg(path, image)
            cells.append(image.resize((320, 180), Image.Resampling.LANCZOS))
            titles.append("%s %s  r %.0fcm  bits %d" % (
                surface, label, state["shown"] * 100.0, state["bits"],
            ))
            print(
                "IMPACT", surface, label,
                "k", round(state["k"], 2),
                "shown", round(state["shown"], 2),
                "inner", round(state["shown"] * state["inner"], 2),
                "ratio", round(state["inner"], 3),
                "alpha", round(state["opacity"], 3),
                "chunk", round(state["chunk"], 3),
                "bits", state["bits"],
                "plumes", state["plumes"],
                "age", round(state["age"], 3),
            )
    p11_ground("concrete", asphalt=True)
    apply_pose(arm, pose_stagger, 0.0, 0.0)
    foot = p11_foot(arm)
    fwd, _left = p11_heading(0.0)
    face = foot + fwd * 1.05
    center = face + fwd * 0.16
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(center.x, center.y, 2.2))
    wall = bpy.context.active_object
    wall.name = p11_name("Geo")
    wall.scale = (8.0, 0.28, 4.4)
    wall.data.materials.append(make_mat(p11_name("Mat"), (0.30, 0.30, 0.28, 1.0), 0.88))
    p14_aim(cam, foot, 0.0)
    state = impact22_state("concrete", 13.8)
    hit = center - fwd * 0.14
    hit.z = 1.25
    impact22_draw(hit, (-fwd.x, -fwd.y, 0.0), state, cam.location)
    png = os.path.join(tmp, "wall.png")
    scene.render.filepath = png
    bpy.ops.render.render(write_still=True)
    image = Image.open(png).convert("RGB")
    p14_jpeg(os.path.join(out_dir, "impact-wall.jpg"), image)
    print(
        "IMPACT wall",
        "shown", round(state["shown"], 2),
        "bits", state["bits"],
        "plumes", state["plumes"],
    )
    p14_grid(
        cells, titles,
        "Impact shockwave   thin ring   chase cam   sprint 13.8 vs hard land 36.5",
        os.path.join(out_dir, "impact-sheet.jpg"),
        4,
    )
    # Hard land, same chase camera. Concrete and dirt, so the plume is visible on both.
    strip = []
    strip_titles = []
    for surface, asphalt in (("concrete", True), ("dirt", False)):
        for age in (0.04, 0.10, 0.16, 0.22):
            p11_ground(surface, asphalt=asphalt)
            apply_pose(arm, pose_land, 0.0, yaw)
            foot = p11_foot(arm)
            p14_aim(cam, foot, yaw)
            p11_clear("P11Fx")
            state = impact22_state(surface, 36.5, age_u=age / 0.25)
            impact22_draw(Vector((foot.x, foot.y, 0.02)), (0.0, 0.0, 1.0), state, cam.location)
            png = os.path.join(tmp, "strip-%s-%.2f.png" % (surface, age))
            scene.render.filepath = png
            bpy.ops.render.render(write_still=True)
            image = Image.open(png).convert("RGB")
            strip.append(image.resize((400, 225), Image.Resampling.LANCZOS))
            strip_titles.append("%s  %.2f s  plume %d" % (surface, age, state["plumes"]))
            print(
                "STRIP", surface, round(age, 2),
                "shown", round(state["shown"], 2),
                "alpha", round(state["opacity"], 3),
                "bits", state["bits"],
                "plumes", state["plumes"],
                "hop", round(state["hop_lo"], 2), round(state["hop_hi"], 2),
            )
    p14_grid(
        strip, strip_titles,
        "Hard land   thin ring and dust plume   concrete then dirt",
        os.path.join(out_dir, "impact-strip.jpg"),
        4,
    )
    render_pass25_lines(arm, cam, out_dir, tmp)


def render_pass25_lines(arm, cam, out_dir, tmp):
    """Four chase panes. Air dash and grapple pull, seat tints, lines on for the still."""
    from PIL import Image, ImageDraw

    scene = bpy.context.scene
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "SUN":
            obj.data.energy = 1.4
        elif obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 28
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    seats = (
        (0.95, 0.28, 0.32),
        (0.25, 0.55, 1.00),
        (1.00, 0.82, 0.15),
        (0.25, 0.90, 0.45),
    )
    # Forward dash is the horizontal pose. The diagonal dash keeps that lean
    # and sends the streaks across the chase frame so the length can be judged.
    shots = (
        ("dash-fwd", "air dash  forward  red", 0, "dash", 0.0, 1.0),
        ("dash-diag", "air dash  diagonal  blue", 1, "dash", 0.70, 0.70),
        ("pull-fwd", "grapple pull  along rope  yellow", 2, "pull", 0.0, 1.0),
        ("pull-side", "grapple pull  aside  green", 3, "pull", 0.85, 0.45),
    )
    panes = []
    yaw = 24.0
    for name, title, seat, kind, side, fwd_w in shots:
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        if kind == "dash":
            apply_pose(arm, lambda a, s=side, f=fwd_w: pose_airdash(a, s, f), 0.85, yaw)
        else:
            apply_pose(arm, pose_grapple, 0.75, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        travel, _rope = pass25_travel(arm, foot, yaw, kind, side, fwd_w)
        pass25_streaks(arm, travel, seats[seat])
        if kind == "pull":
            pass25_rope(arm, foot, yaw, side, fwd_w)
        png = os.path.join(tmp, name + ".png")
        scene.render.filepath = png
        bpy.ops.render.render(write_still=True)
        image = Image.open(png).convert("RGB")
        draw = ImageDraw.Draw(image)
        draw.text((10, 8), title, font=p11_font(18), fill=(255, 246, 226))
        panes.append(image)
        print("LINES", name, "seat", seat, "travel", tuple(round(v, 2) for v in travel))
    sheet = Image.new("RGB", (1280, 720), (12, 12, 12))
    sheet.paste(panes[0], (0, 0))
    sheet.paste(panes[1], (640, 0))
    sheet.paste(panes[2], (0, 360))
    sheet.paste(panes[3], (640, 360))
    p14_jpeg(os.path.join(out_dir, "speed-lines.jpg"), sheet)


def pass25_travel(arm, foot, yaw, kind, side, fwd_w):
    """Unit travel of the dash, or the pull toward the hook. Streaks use the opposite."""
    fwd, left = p11_heading(yaw)
    if kind == "dash":
        # localForward is facing, localRight is -left on this heading.
        travel = fwd * fwd_w + (-left) * side
        if travel.length < 0.001:
            travel = fwd
        return travel.normalized(), None
    hand = bone_pos(arm, "Hand_L")
    anchor = pass25_anchor(hand, fwd, left, side, fwd_w)
    travel = anchor - hand
    if travel.length < 0.001:
        travel = fwd
    return travel.normalized(), anchor


def pass25_anchor(hand, fwd, left, side, fwd_w):
    right = -left
    return hand + fwd * (4.5 * fwd_w) + right * (3.2 * side) + Vector((0.0, 0.0, 2.4))


def pass25_streaks(arm, travel, tint):
    """8 tapered ribbons from the torso and the limbs, opposite velocity.

    The ribbon faces the chase camera so the length stays readable, and it
    stays aligned with the travel instead of standing up like a cone.
    """
    trail = -travel
    if trail.length < 0.001:
        return
    trail = trail.normalized()
    cam_loc = bpy.context.scene.camera.location
    names = ("Spine", "Hips", "Hand_L", "Hand_R", "Foot_L", "Foot_R", "Head", "UpperArm_L")
    for i, name in enumerate(names):
        h = (i * 3 % 10) / 9.0
        length = 0.60 + h * 0.60
        origin = bone_pos(arm, name)
        # Begin just outside the mesh so the ribbon is not buried in the body.
        start = origin + trail * 0.22
        end = start + trail * length
        side = trail.cross(cam_loc - (start + end) * 0.5)
        if side.length < 0.001:
            side = trail.cross(Vector((0.0, 0.0, 1.0)))
        side.normalize()
        w0 = 0.05
        w1 = 0.008
        verts = [
            start + side * w0,
            start - side * w0,
            end - side * w1,
            end + side * w1,
        ]
        mesh = bpy.data.meshes.new(p11_name("Fx"))
        mesh.from_pydata([tuple(v) for v in verts], [], [(0, 1, 2, 3)])
        mesh.update()
        obj = bpy.data.objects.new(p11_name("Fx"), mesh)
        bpy.context.collection.objects.link(obj)
        obj.data.materials.append(pass25_line_mat(p11_name("Mat"), tint, 0.35))
        print("STREAK", name, "len", round(length, 2))


def pass25_rope(arm, foot, yaw, side, fwd_w):
    """Thin tell so the pull streaks can be judged against the rope. Not GrappleRopeTell."""
    fwd, left = p11_heading(yaw)
    hand = bone_pos(arm, "Hand_L")
    anchor = pass25_anchor(hand, fwd, left, side, fwd_w)
    mid = (hand + anchor) * 0.5
    direction = anchor - hand
    length = direction.length
    if length < 0.05:
        return
    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.012, depth=length, location=mid)
    obj = bpy.context.active_object
    obj.name = p11_name("Fx")
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(impact22_unlit(p11_name("Mat"), (0.92, 0.90, 0.82), 0.55, 0.4))


def pass25_line_mat(name, color, alpha):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.use_backface_culling = False
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 1.4
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    mix.inputs["Fac"].default_value = alpha
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def p26_aim(cam, foot, yaw_deg, back=1.70, height=0.55, look_z=0.70):
    """Closer and lower than the chase boom. The figure is about a third of the frame."""
    fwd, left = p11_heading(yaw_deg)
    cam.data.type = "PERSP"
    cam.data.sensor_fit = "VERTICAL"
    cam.data.sensor_height = 24.0
    cam.data.lens = 14.8
    cam.data.clip_start = 0.02
    cam.data.clip_end = 80.0
    cam.location = foot - left * 0.12 + Vector((0.0, 0.0, height)) - fwd * back
    look_at(cam, foot + Vector((0.0, 0.0, look_z)))
    bpy.context.view_layer.update()
    return cam.location


def p26_figure_fraction(arm, cam):
    from bpy_extras.object_utils import world_to_camera_view

    scene = bpy.context.scene
    head = bone_pos(arm, "Head", tail=True)
    foot = bone_pos(arm, "Foot_L", tail=True)
    a = world_to_camera_view(scene, cam, head)
    b = world_to_camera_view(scene, cam, foot)
    frac = abs(a.y - b.y)
    print("FIGURE", "frac", round(frac, 3), "head", round(a.x, 3), round(a.y, 3), "foot", round(b.x, 3), round(b.y, 3))
    return frac


def render_pass26(arm, cam):
    """Close hard-land compare, forward-dash edge streaks, and wall scuffs."""
    from PIL import Image, ImageDraw

    scene = bpy.context.scene
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "SUN":
            obj.data.energy = 1.4
        elif obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 28
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    scene.eevee.taa_render_samples = 8
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    out_dir = os.path.join(ROOT, "Docs", "FxStills", "pass26")
    os.makedirs(out_dir, exist_ok=True)
    tmp = "/tmp/pass26"
    os.makedirs(tmp, exist_ok=True)
    if os.environ.get("FX_PASS26_SCUFF") == "1":
        render_pass26_scuff(arm, cam, out_dir, tmp)
        return
    yaw = 24.0
    cells = []
    titles = []
    for legacy, label in ((True, "before"), (False, "after")):
        for age in (0.04, 0.10, 0.16, 0.22):
            p11_clear("P11Fx")
            p11_ground("concrete", asphalt=True)
            apply_pose(arm, pose_land, 0.0, yaw)
            foot = p11_foot(arm)
            p26_aim(cam, foot, yaw)
            if legacy and age == 0.04:
                p26_figure_fraction(arm, cam)
            state = impact22_state("concrete", 36.5, age_u=age / 0.25, legacy=legacy)
            impact22_draw(Vector((foot.x, foot.y, 0.02)), (0.0, 0.0, 1.0), state, cam.location)
            png = os.path.join(tmp, "%s-%.2f.png" % (label, age))
            scene.render.filepath = png
            bpy.ops.render.render(write_still=True)
            image = Image.open(png).convert("RGB")
            p14_jpeg(os.path.join(out_dir, "land-%s-%.2f.jpg" % (label, age)), image)
            cells.append(image.resize((480, 270), Image.Resampling.LANCZOS))
            titles.append("%s  %.2f s" % (label, age))
            print("LAND", label, age, "top check", "plumes", state["plumes"], "alpha", round(state["opacity"], 3))
    p14_grid(
        cells, titles,
        "Hard land  before / after   same close camera   concrete",
        os.path.join(out_dir, "impact-compare.jpg"),
        4,
    )
    dirt_cells = []
    dirt_titles = []
    for age in (0.04, 0.10, 0.16, 0.22):
        p11_clear("P11Fx")
        p11_ground("dirt", asphalt=False)
        apply_pose(arm, pose_land, 0.0, yaw)
        foot = p11_foot(arm)
        p26_aim(cam, foot, yaw)
        state = impact22_state("dirt", 36.5, age_u=age / 0.25, legacy=False)
        impact22_draw(Vector((foot.x, foot.y, 0.02)), (0.0, 0.0, 1.0), state, cam.location)
        png = os.path.join(tmp, "dirt-%.2f.png" % age)
        scene.render.filepath = png
        bpy.ops.render.render(write_still=True)
        image = Image.open(png).convert("RGB")
        dirt_cells.append(image.resize((480, 270), Image.Resampling.LANCZOS))
        dirt_titles.append("dirt after  %.2f s" % age)
    p14_grid(
        dirt_cells, dirt_titles,
        "Hard land after   dirt   same close camera",
        os.path.join(out_dir, "impact-strip.jpg"),
        4,
    )
    render_pass26_dash(arm, cam, out_dir, tmp)
    render_pass26_scuff(arm, cam, out_dir, tmp)


def render_pass26_dash(arm, cam, out_dir, tmp):
    """Forward air dash from the real chase camera, with short edge streaks."""
    from PIL import Image

    scene = bpy.context.scene
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    yaw = 24.0
    p11_clear("P11Fx")
    p11_ground("concrete", asphalt=True)
    apply_pose(arm, lambda a: pose_airdash(a, 0.0, 1.0), 0.85, yaw)
    foot = p11_foot(arm)
    p14_aim(cam, foot, yaw)
    fwd, left = p11_heading(yaw)
    travel = fwd.normalized()
    tint = (0.95, 0.28, 0.32)
    pass25_streaks(arm, travel, tint)
    pass26_edge_streaks(arm, travel, tint)
    png = os.path.join(tmp, "dash-fwd.png")
    scene.render.filepath = png
    bpy.ops.render.render(write_still=True)
    image = Image.open(png).convert("RGB")
    p14_jpeg(os.path.join(out_dir, "dash-forward.jpg"), image)


def pass26_edge_streaks(arm, travel, tint):
    """Short ribbons just outside shoulders, hips, and hands, trailing backward."""
    from bpy_extras.object_utils import world_to_camera_view

    trail = -travel
    if trail.length < 0.001:
        return
    trail = trail.normalized()
    side = Vector((-trail.y, trail.x, 0.0))
    if side.length < 0.001:
        side = Vector((1.0, 0.0, 0.0))
    side.normalize()
    cam_loc = bpy.context.scene.camera.location
    specs = (
        ("UpperArm_L", 1.0, 0.38, 0.28),
        ("UpperArm_R", -1.0, 0.38, 0.36),
        ("Hips", 1.0, 0.30, 0.32),
        ("Hips", -1.0, 0.30, 0.40),
        ("Hand_L", 1.0, 0.18, 0.36),
        ("Hand_R", -1.0, 0.18, 0.44),
    )
    scene = bpy.context.scene
    cam = scene.camera
    for name, sign, outward, length in specs:
        origin = bone_pos(arm, name)
        start = origin + side * sign * outward + trail * 0.04
        end = start + trail * length + side * sign * 0.48
        view = cam_loc - (start + end) * 0.5
        width_axis = trail.cross(view)
        if width_axis.length < 0.001:
            width_axis = Vector((0.0, 0.0, 1.0))
        width_axis.normalize()
        w0, w1 = 0.07, 0.016
        verts = [
            start + width_axis * w0,
            start - width_axis * w0,
            end - width_axis * w1,
            end + width_axis * w1,
        ]
        mesh = bpy.data.meshes.new(p11_name("Fx"))
        mesh.from_pydata([tuple(v) for v in verts], [], [(0, 1, 2, 3)])
        mesh.update()
        obj = bpy.data.objects.new(p11_name("Fx"), mesh)
        bpy.context.collection.objects.link(obj)
        obj.data.materials.append(pass25_line_mat(p11_name("Mat"), tint, 0.35))
        a = world_to_camera_view(scene, cam, start)
        b = world_to_camera_view(scene, cam, end)
        ax = a.x * scene.render.resolution_x
        ay = (1.0 - a.y) * scene.render.resolution_y
        bx = b.x * scene.render.resolution_x
        by = (1.0 - b.y) * scene.render.resolution_y
        print(
            "EDGE", name, "len", length,
            "span", round(abs(ax - bx), 1), round(abs(ay - by), 1),
        )


def render_pass26_scuff(arm, cam, out_dir, tmp):
    """Wall-run contact. Brick, concrete, and wood, sprint and hard slam."""
    from PIL import Image

    scene = bpy.context.scene
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    cells = []
    titles = []
    shots = (
        ("brick", 13.8, (0.55, 0.28, 0.22), (0.26, 0.10, 0.07), (0.72, 0.40, 0.30)),
        ("concrete", 13.8, (0.62, 0.62, 0.60), (0.20, 0.20, 0.19), (0.84, 0.83, 0.80)),
        ("wood", 13.8, (0.48, 0.34, 0.20), (0.22, 0.12, 0.05), (0.78, 0.60, 0.36)),
        ("brick", 36.5, (0.55, 0.28, 0.22), (0.26, 0.10, 0.07), (0.72, 0.40, 0.30)),
        ("concrete", 36.5, (0.62, 0.62, 0.60), (0.20, 0.20, 0.19), (0.84, 0.83, 0.80)),
        ("wood", 36.5, (0.48, 0.34, 0.20), (0.22, 0.12, 0.05), (0.78, 0.60, 0.36)),
    )
    for surface, speed, wall_col, ink, dust in shots:
        p11_clear("P11Fx")
        p11_clear("P11Geo")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_wall, 0.0, 0.0)
        foot = p11_foot(arm)
        fwd, _left = p11_heading(0.0)
        # Wall in front of the chest. Normal points back toward the camera side.
        normal = -fwd
        center = foot + fwd * 0.42
        center.z = 1.15
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(center.x + fwd.x * 0.06, center.y + fwd.y * 0.06, 1.6))
        wall = bpy.context.active_object
        wall.name = p11_name("Geo")
        wall.scale = (2.4, 0.12, 3.2)
        wall.data.materials.append(make_mat(p11_name("Mat"), wall_col + (1.0,), 0.9))
        # Beside the chest, on open wall, so the body does not hide the smear.
        along_wall = Vector((-normal.y, normal.x, 0.0))
        hit = center + along_wall * 0.55
        hit.z = 1.05
        cam.data.lens = 14.8
        cam.data.sensor_fit = "VERTICAL"
        cam.data.sensor_height = 24.0
        cam.location = hit - fwd * 1.55 + Vector((-fwd.y, fwd.x, 0.0)) * 1.85 + Vector((0.0, 0.0, 0.12))
        look_at(cam, hit + Vector((0.0, 0.0, -0.05)))
        bpy.context.view_layer.update()
        pass26_scuff(hit, normal, speed, ink, dust, cam.location)
        png = os.path.join(tmp, "scuff-%s-%.0f.png" % (surface, speed))
        scene.render.filepath = png
        bpy.ops.render.render(write_still=True)
        image = Image.open(png).convert("RGB")
        label = "sprint" if speed < 20.0 else "hard"
        p14_jpeg(os.path.join(out_dir, "scuff-%s-%s.jpg" % (surface, label)), image)
        cells.append(image.resize((400, 225), Image.Resampling.LANCZOS))
        k = 0.0 if speed <= 13.8 else 1.0
        width = 0.26 + k * 0.52
        titles.append("%s %s  %.0f cm" % (surface, label, width * 100.0))
        print("SCUFF", surface, label, "width", round(width, 2))
    p14_grid(
        cells, titles,
        "Wall scuff   brick, concrete, wood   sprint then hard slam",
        os.path.join(out_dir, "scuff-sheet.jpg"),
        3,
    )


def pass26_scuff(hit, normal, speed, ink, dust, cam_loc):
    """Decal on the wall plus a short puff. Age 0.08 s, puff still up."""
    k = 0.0 if speed <= 13.8 else min(1.15, (speed - 13.8) / (36.5 - 13.8))
    width = 0.26 + k * 0.52
    height = 0.16 + k * 0.26
    n = Vector(normal).normalized()
    along = Vector((-n.y, n.x, 0.0))
    if along.length < 0.001:
        along = Vector((1.0, 0.0, 0.0))
    along.normalize()
    up = along.cross(n)
    if up.length < 0.001:
        up = Vector((0.0, 0.0, 1.0))
    up.normalize()
    loc = Vector(hit) + n * 0.05
    # Plane sits in local XY. Local X is the smear, local Y is its height, local Z is the wall normal.
    rot = Matrix((
        (along.x, up.x, n.x),
        (along.y, up.y, n.y),
        (along.z, up.z, n.z),
    ))
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=loc)
    mark = bpy.context.active_object
    mark.name = p11_name("Fx")
    mark.rotation_mode = "QUATERNION"
    mark.rotation_quaternion = rot.to_quaternion()
    mark.scale = (width, height, 1.0)
    age = 0.08
    # Holds 0.72 through 0.12 s, then fades. The still is at 0.08 s.
    mark_a = 0.72
    mark.data.materials.append(pass26_scuff_mat(p11_name("Mat"), ink, mark_a))
    bpy.context.view_layer.update()
    corners = [mark.matrix_world @ Vector(c) for c in mark.bound_box]
    span = Vector((
        max(c.x for c in corners) - min(c.x for c in corners),
        max(c.y for c in corners) - min(c.y for c in corners),
        max(c.z for c in corners) - min(c.z for c in corners),
    ))
    print("MARK", "span", round(span.x, 2), round(span.y, 2), round(span.z, 2), "alpha", round(mark_a, 2))
    puffs = 3 if k < 0.25 else 6
    for i in range(puffs):
        h = p11_rand(i, 4)
        puff_u = age / 0.22
        out_d = 0.08 + (0.14 + h * 0.18) * puff_u
        slide = (h - 0.5) * width * 0.85
        rise = height * 0.45 + (p11_rand(i, 8) - 0.2) * height * 0.35
        pos = Vector(hit) + n * out_d + along * slide + Vector((0.0, 0.0, rise))
        size = (0.16 + h * 0.14) * (0.75 + 0.35 * puff_u)
        fade = 1.0 - puff_u
        impact24_puff(pos, size, dust, 0.72 * fade, cam_loc)


def pass26_scuff_mat(name, color, alpha):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    mat.use_backface_culling = False
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = "NONE"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = p11_soft_image()
    mul = nt.nodes.new("ShaderNodeMath")
    mul.operation = "MULTIPLY"
    mul.inputs[1].default_value = alpha
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 1.0
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(tex.outputs["Alpha"], mul.inputs[0])
    nt.links.new(mul.outputs["Value"], mix.inputs["Fac"])
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    return mat


def pass27_solid(pos, size, color, spin):
    """Unlit chunk. Scene lights cannot lift a black piece into the ground tone."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=pos)
    obj = bpy.context.active_object
    obj.name = p11_name("Fx")
    obj.visible_shadow = False
    obj.scale = (size * 0.92, size * 0.72, size * 0.58)
    obj.rotation_euler = (0.35 + spin * 0.11, spin * 0.4, spin * 0.2)
    mat = bpy.data.materials.new(p11_name("Mat"))
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 1.0
    nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    obj.data.materials.append(mat)
    return obj


def pass27_span(start, end, cam):
    from bpy_extras.object_utils import world_to_camera_view

    scene = bpy.context.scene
    a = world_to_camera_view(scene, cam, start)
    b = world_to_camera_view(scene, cam, end)
    w = scene.render.resolution_x
    h = scene.render.resolution_y
    ax = a.x * w
    ay = (1.0 - a.y) * h
    bx = b.x * w
    by = (1.0 - b.y) * h
    return math.hypot(ax - bx, ay - by)


def pass27_ribbon(start, end, tint, cam_loc, width=0.11):
    view = cam_loc - (start + end) * 0.5
    axis = (end - start).cross(view)
    if axis.length < 0.001:
        axis = Vector((0.0, 0.0, 1.0))
    axis.normalize()
    w0, w1 = width, width * 0.22
    verts = [
        start + axis * w0,
        start - axis * w0,
        end - axis * w1,
        end + axis * w1,
    ]
    mesh = bpy.data.meshes.new(p11_name("Fx"))
    mesh.from_pydata([tuple(v) for v in verts], [], [(0, 1, 2, 3)])
    mesh.update()
    obj = bpy.data.objects.new(p11_name("Fx"), mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(pass25_line_mat(p11_name("Mat"), tint, 0.35))


def pass27_burst(arm, cam, tint):
    """Camera-facing flares. Printed span is pixels in the saved frame."""
    right = cam.matrix_world.to_3x3() @ Vector((1.0, 0.0, 0.0))
    up = cam.matrix_world.to_3x3() @ Vector((0.0, 1.0, 0.0))
    right.normalize()
    up.normalize()
    cam_loc = cam.location
    specs = (
        ("UpperArm_L", 1.0, 0.34, 1.15, 0.22),
        ("UpperArm_R", -1.0, 0.34, 1.33, -0.16),
        ("Hips", 1.0, 0.26, 1.05, -0.08),
        ("Hips", -1.0, 0.26, 1.23, 0.06),
        ("Hand_L", 1.0, 0.16, 1.41, 0.12),
        ("Hand_R", -1.0, 0.16, 1.15, -0.10),
    )
    shortest = 999.0
    for name, sign, outward, flare, lift in specs:
        origin = bone_pos(arm, name)
        start = origin + right * sign * outward
        end = start + right * sign * flare + up * lift
        pass27_ribbon(start, end, tint, cam_loc)
        span = pass27_span(start, end, cam)
        shortest = min(shortest, span)
        print("SPAN", name, round(span, 1), "flare", flare)
    # Head-on limb ribbons, same flare, so the old along-view set is not the only read.
    for i, name in enumerate(("Spine", "Hips", "Hand_L", "Hand_R", "Foot_L", "Foot_R", "Head", "UpperArm_L")):
        h = (i * 3 % 10) / 9.0
        sign = 1.0 if (i % 2) == 0 else -1.0
        flare = 0.95 + h * 0.45
        lift = 0.16 if (i % 2) == 0 else -0.10
        origin = bone_pos(arm, name)
        start = origin + right * sign * 0.22
        end = start + right * sign * flare + up * lift
        pass27_ribbon(start, end, tint, cam_loc, 0.09)
        span = pass27_span(start, end, cam)
        shortest = min(shortest, span)
        print("SPAN", "limb", name, round(span, 1))
    print("SPAN_MIN", round(shortest, 1))
    return shortest


def pass27_border(path, margin=6):
    from PIL import Image

    image = Image.open(path).convert("RGB")
    w, h = image.size
    px = image.load()
    ground = px[4, 4]
    n = 0
    for y in range(h):
        for x in range(w):
            if margin < x < w - 1 - margin and margin < y < h - 1 - margin:
                continue
            r, g, b = px[x, y]
            if abs(r - ground[0]) + abs(g - ground[1]) + abs(b - ground[2]) > 36:
                n += 1
    print("BORDER", os.path.basename(path), n)
    return n


def pass27_land(arm, cam, foot, yaw, age, radius, chunky, bits):
    k_bits = bits
    state = impact22_state(
        "concrete", 36.5, age_u=age / 0.25,
        radius_override=radius, bits_override=k_bits, chunky=chunky,
    )
    impact22_draw(Vector((foot.x, foot.y, 0.02)), (0.0, 0.0, 1.0), state, cam.location)
    print("RING", "r", round(state["radius"], 2), "shown", round(state["shown"], 2), "bits", state["bits"])


def pass28_trail(arm, travel, tint, cam):
    """Old trail ribbons. Prints the screen span so a short diagonal is visible in the log."""
    trail = -travel
    if trail.length < 0.001:
        return 999.0
    trail = trail.normalized()
    names = ("Spine", "Hips", "Hand_L", "Hand_R", "Foot_L", "Foot_R", "Head", "UpperArm_L")
    shortest = 999.0
    for i, name in enumerate(names):
        h = (i * 3 % 10) / 9.0
        length = 0.60 + h * 0.60
        origin = bone_pos(arm, name)
        start = origin + trail * 0.22
        end = start + trail * length
        pass27_ribbon(start, end, tint, cam.location, 0.045)
        span = pass27_span(start, end, cam)
        shortest = min(shortest, span)
        print("SPAN", "trail", name, round(span, 1))
    print("SPAN_MIN", round(shortest, 1))
    return shortest


def pass28_bottom(path):
    from PIL import Image

    image = Image.open(path).convert("RGB")
    w, h = image.size
    px = image.load()
    total = 0.0
    n = 0
    for y in range(h - 12, h):
        for x in range(w):
            r, g, b = px[x, y]
            total += 0.3 * r + 0.59 * g + 0.11 * b
            n += 1
    print("BOTTOM", os.path.basename(path), round(total / max(n, 1), 2))


def pass28_card(verts, color):
    """Unlit hard card. Alpha clip in game; the still is the clipped ink, not a glow."""
    mesh = bpy.data.meshes.new(p11_name("Fx"))
    mesh.from_pydata([tuple(v) for v in verts], [], [(0, 1, 2, 3)])
    mesh.update()
    obj = bpy.data.objects.new(p11_name("Fx"), mesh)
    bpy.context.collection.objects.link(obj)
    obj.visible_shadow = False
    mat = bpy.data.materials.new(p11_name("Mat"))
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 1.0
    nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    obj.data.materials.append(mat)
    return obj


def pass28_strip(start, end, normal, cam):
    """Seat ink with a dark edge. Total width 0.20 m. Core is 62% of that, about 0.12 m."""
    nrm = Vector(normal)
    if nrm.length < 0.001:
        nrm = Vector((0.0, 1.0, 0.0))
    nrm.normalize()
    direction = Vector(end) - Vector(start)
    if direction.length < 0.001:
        direction = Vector((1.0, 0.0, 0.0))
    direction.normalize()
    up = nrm.cross(direction)
    if up.length < 0.001:
        up = Vector((0.0, 0.0, 1.0))
    up.normalize()
    width = 0.20
    core = width * 0.62

    def quad(half, lift):
        a = Vector(start) + nrm * lift
        b = Vector(end) + nrm * lift
        return [
            a + up * half,
            a - up * half,
            b - up * half,
            b + up * half,
        ]

    pass28_card(quad(width * 0.5, 0.02), (0.08, 0.05, 0.04))
    pass28_card(quad(core * 0.5, 0.04), (0.95, 0.28, 0.32))
    span = pass27_span(
        Vector(start) + up * (width * 0.5),
        Vector(start) - up * (width * 0.5),
        cam,
    )
    print("RIBBON", "width_m", width, "core_m", round(core, 3), "width_px", round(span, 1))
    return span


def pass28_delta(before_path, after_path):
    from PIL import Image

    before = Image.open(before_path).convert("RGB")
    after = Image.open(after_path).convert("RGB")
    w, h = before.size
    bp = before.load()
    ap = after.load()
    n = 0
    for y in range(h):
        for x in range(w):
            br, bg, bb = bp[x, y]
            ar, ag, ab = ap[x, y]
            if abs(ar - br) + abs(ag - bg) + abs(ab - bb) > 36:
                n += 1
    print("DELTA", os.path.basename(after_path), n)
    return n


def pass29_star(center, right, up, size, color):
    verts = [Vector(center)]
    faces = []
    for i in range(10):
        ang = math.radians(i * 36.0 - 90.0)
        rad = size * (0.50 if (i % 2) == 0 else 0.20)
        verts.append(Vector(center) + Vector(right) * math.cos(ang) * rad + Vector(up) * math.sin(ang) * rad)
    for i in range(10):
        faces.append((0, 1 + i, 1 + ((i + 1) % 10)))
    mesh = bpy.data.meshes.new(p11_name("Fx"))
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.update()
    obj = bpy.data.objects.new(p11_name("Fx"), mesh)
    bpy.context.collection.objects.link(obj)
    obj.visible_shadow = False
    mat = bpy.data.materials.new(p11_name("Mat"))
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 1.0
    nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
    obj.data.materials.append(mat)
    return obj


def pass29_cam_axes(cam, dist):
    basis = cam.matrix_world.to_3x3()
    right = basis @ Vector((1.0, 0.0, 0.0))
    up = basis @ Vector((0.0, 1.0, 0.0))
    fwd = basis @ Vector((0.0, 0.0, -1.0))
    right.normalize()
    up.normalize()
    fwd.normalize()
    half_h = dist * math.tan(math.radians(39.0))
    half_w = half_h * (640.0 / 360.0)
    origin = cam.location + fwd * dist
    return origin, right, up, half_w, half_h


def pass29_quad(center, right, up, sx, sy, color, alpha, z=0.0):
    """Camera-facing quad. alpha is the still's opacity, 1 for a hard card."""
    c = Vector(center) + Vector((0.0, 0.0, 0.0))
    # z pushes toward the camera when the caller already put the quad on the view plane.
    right = Vector(right)
    up = Vector(up)
    hx = sx * 0.5
    hy = sy * 0.5
    verts = [
        c - right * hx - up * hy,
        c + right * hx - up * hy,
        c + right * hx + up * hy,
        c - right * hx + up * hy,
    ]
    mesh = bpy.data.meshes.new(p11_name("Fx"))
    mesh.from_pydata([tuple(v) for v in verts], [], [(0, 1, 2, 3)])
    mesh.update()
    obj = bpy.data.objects.new(p11_name("Fx"), mesh)
    bpy.context.collection.objects.link(obj)
    obj.visible_shadow = False
    mat = bpy.data.materials.new(p11_name("Mat"))
    mat.use_nodes = True
    mat.blend_method = "BLEND"
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    emit = nt.nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
    emit.inputs["Strength"].default_value = 1.0
    trans = nt.nodes.new("ShaderNodeBsdfTransparent")
    mix = nt.nodes.new("ShaderNodeMixShader")
    mix.inputs["Fac"].default_value = alpha
    nt.links.new(trans.outputs["BSDF"], mix.inputs[1])
    nt.links.new(emit.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    obj.data.materials.append(mat)
    return obj


def pass29_edges(cam, tint):
    """Six streaks, 48×4 px at 640×360, 1 px outline, inside the outer 12%."""
    dist = 2.4
    origin, right, up, half_w, half_h = pass29_cam_axes(cam, dist)
    ppm_x = 640.0 / (2.0 * half_w)
    ppm_y = 360.0 / (2.0 * half_h)
    length = 48.0 / ppm_x
    thick = 4.0 / ppm_y
    rim = 1.0 / ppm_y
    x = half_w * (1.0 - 0.06)
    y = half_h * (1.0 - 0.06)
    seats = (
        (0.95, 0.28, 0.32),
        (0.25, 0.55, 1.0),
        (1.0, 0.58, 0.15),
        (0.78, 0.66, 1.0),
        (0.95, 0.28, 0.32),
        (0.25, 0.55, 1.0),
    )
    specs = (
        (-x, half_h * 0.42, thick, length),
        (-x, -half_h * 0.42, thick, length),
        (x, half_h * 0.42, thick, length),
        (x, -half_h * 0.42, thick, length),
        (half_w * 0.28, y, length, thick),
        (-half_w * 0.22, -y, length, thick),
    )
    outline = (0.08, 0.07, 0.06)
    for i, (ox, oy, sx, sy) in enumerate(specs):
        center = origin + right * ox + up * oy
        color = seats[i] if tint is None else tint
        pass29_quad(center, right, up, sx, sy, outline, 0.90)
        core_x = max(sx - rim * 2.0, rim)
        core_y = max(sy - rim * 2.0, rim)
        # A hair toward the camera so the core sits on the outline.
        fwd = (cam.location - origin)
        if fwd.length > 0.001:
            fwd = fwd.normalized()
        pass29_quad(center + fwd * 0.004, right, up, core_x, core_y, color, 0.55)
    print("EDGE", "streaks", len(specs), "len_m", round(length, 4), "thick_m", round(thick, 4))


def pass29_boxes(before, after, tag):
    """Print each changed blob's pixel size, and how much sits in the inner 76%."""
    from PIL import Image

    ia = Image.open(before).convert("RGB")
    ib = Image.open(after).convert("RGB")
    pa, pb = ia.load(), ib.load()
    w, h = ia.size
    seen = [[False] * w for _ in range(h)]
    mx = int(w * 0.12)
    my = int(h * 0.12)
    blobs = []
    for y in range(h):
        for x in range(w):
            if seen[y][x]:
                continue
            ar, ag, ab = pa[x, y]
            br, bg, bb = pb[x, y]
            if abs(ar - br) + abs(ag - bg) + abs(ab - bb) <= 28:
                continue
            stack = [(x, y)]
            seen[y][x] = True
            minx = maxx = x
            miny = maxy = y
            n = 0
            inner = 0
            while stack:
                cx, cy = stack.pop()
                n += 1
                if mx <= cx < w - mx and my <= cy < h - my:
                    inner += 1
                if cx < minx:
                    minx = cx
                if cx > maxx:
                    maxx = cx
                if cy < miny:
                    miny = cy
                if cy > maxy:
                    maxy = cy
                for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
                    if nx < 0 or ny < 0 or nx >= w or ny >= h or seen[ny][nx]:
                        continue
                    ar, ag, ab = pa[nx, ny]
                    br, bg, bb = pb[nx, ny]
                    if abs(ar - br) + abs(ag - bg) + abs(ab - bb) <= 28:
                        continue
                    seen[ny][nx] = True
                    stack.append((nx, ny))
            if n < 8:
                continue
            blobs.append((n, maxx - minx + 1, maxy - miny + 1, inner, minx, miny))
    blobs.sort(key=lambda b: -b[0])
    print("BLOBS", tag, len(blobs))
    for blob in blobs[:8]:
        print(" BLOB", tag, "n", blob[0], "wh", blob[1], blob[2], "inner", blob[3], "at", blob[4], blob[5])


def pass29_margin(path):
    from PIL import Image

    image = Image.open(path).convert("RGB")
    w, h = image.size
    px = image.load()
    mx = int(w * 0.12)
    my = int(h * 0.12)
    edge = 0
    center = 0
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            if r < 150 or g > 80 or b > 80:
                continue
            if x < mx or x >= w - mx or y < my or y >= h - my:
                edge += 1
            else:
                center += 1
    print("MARGIN", os.path.basename(path), "edge", edge, "center", center)


def pass30_shape(center, right, up, kind, color, size):
    """kind: puff, cloud, sheet, splinter, streak, tick, chip."""
    if kind in ("puff", "cloud"):
        n = 8
        verts = [Vector(center)]
        for i in range(n):
            ang = math.radians(i * 360.0 / n)
            rad = size * (1.15 if kind == "cloud" else 0.55)
            verts.append(Vector(center) + Vector(right) * math.cos(ang) * rad + Vector(up) * math.sin(ang) * rad * 0.72)
        faces = [(0, 1 + i, 1 + ((i + 1) % n)) for i in range(n)]
        mesh = bpy.data.meshes.new(p11_name("Fx"))
        mesh.from_pydata([tuple(v) for v in verts], [], faces)
        mesh.update()
        obj = bpy.data.objects.new(p11_name("Fx"), mesh)
        bpy.context.collection.objects.link(obj)
        obj.visible_shadow = False
        mat = bpy.data.materials.new(p11_name("Mat"))
        mat.use_nodes = True
        nt = mat.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        emit = nt.nodes.new("ShaderNodeEmission")
        emit.inputs["Color"].default_value = (color[0], color[1], color[2], 1.0)
        emit.inputs["Strength"].default_value = 1.0
        nt.links.new(emit.outputs["Emission"], out.inputs["Surface"])
        obj.data.materials.append(mat)
        return
    # Still sizes only. The sim keeps DustLook's real metres.
    if kind == "sheet":
        sx, sy = size * 1.8, size * 0.50
    elif kind == "splinter":
        sx, sy = size * 0.28, size * 1.7
    elif kind == "streak":
        sx, sy = size * 2.2, size * 0.32
    elif kind == "tick":
        sx, sy = size * 0.26, size * 1.15
    else:
        sx, sy = size * 0.55, size * 0.42
    c = Vector(center)
    rr = Vector(right)
    uu = Vector(up)
    pass28_card(
        [c - rr * sx + uu * sy, c + rr * sx + uu * sy, c + rr * sx - uu * sy, c - rr * sx - uu * sy],
        color,
    )


def pass30_family(center, right, up, kind, color, size=0.36):
    """One readable cluster for the still. Grass is a small puff, dirt a fat cloud."""
    right = Vector(right)
    up = Vector(up)
    center = Vector(center)
    if kind == "puff":
        for i in range(3):
            off = right * ((i - 1) * size * 0.35) + up * ((i % 2) * size * 0.2)
            pass30_shape(center + off, right, up, "puff", color, size * 0.55)
        return
    if kind == "cloud":
        for i in range(6):
            off = right * (((i % 3) - 1) * size * 0.55) + up * ((i // 3 - 0.5) * size * 0.4)
            pass30_shape(center + off, right, up, "cloud", color, size * 0.7)
        return
    if kind == "splinter":
        for i in range(3):
            off = right * ((i - 1) * size * 0.28) + up * ((i - 1) * size * 0.05)
            pass30_shape(center + off, right, up, "splinter", color, size * 0.85)
        return
    if kind == "streak":
        for i in range(3):
            off = up * ((i - 1) * size * 0.28)
            pass30_shape(center + off, right, up, "streak", color, size * 0.7)
        return
    if kind == "tick":
        for i in range(4):
            off = right * ((i - 1.5) * size * 0.22) + up * (-0.15 - (i % 2) * size * 0.25)
            pass30_shape(center + off, right, up, "tick", color, size * 0.7)
        return
    if kind == "chip":
        for i in range(3):
            off = right * ((i - 1) * size * 0.45) + up * ((i % 2) * size * 0.12)
            pass30_shape(center + off, right, up, "chip", color, size * 0.55)
        return
    pass30_shape(center, right, up, kind, color, size)


def render_pass29(arm, cam):
    """Quarter-pane chase stills for margin streaks, the ink card, and who is It."""
    from PIL import Image

    scene = bpy.context.scene
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "SUN":
            obj.data.energy = 1.4
        elif obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 28
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    scene.eevee.taa_render_samples = 8
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    out_dir = os.path.join(ROOT, "Docs", "FxStills", "pass29")
    os.makedirs(out_dir, exist_ok=True)
    tmp = "/tmp/pass29"
    os.makedirs(tmp, exist_ok=True)
    yaw = 24.0
    seat = (0.95, 0.28, 0.32)
    crown = (0.15, 0.82, 1.0)

    def shoot(name, cells, titles):
        png = os.path.join(tmp, name + ".png")
        scene.render.filepath = png
        bpy.ops.render.render(write_still=True)
        image = Image.open(png).convert("RGB")
        p14_jpeg(os.path.join(out_dir, name + ".jpg"), image)
        cells.append(image.copy())
        titles.append(name.replace("-", " "))

    cells = []
    titles = []
    for label, on in (("edge-before", False), ("edge-after", True)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_run, 0.8, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        if on:
            pass29_edges(cam, None)
        else:
            print("EDGE", "off")
        shoot(label, cells, titles)
        pass29_margin(os.path.join(out_dir, label + ".jpg"))
    pass29_boxes(os.path.join(out_dir, "edge-before.jpg"), os.path.join(out_dir, "edge-after.jpg"), "edge")
    p14_grid(cells, titles, "Owner pane streaks   chase camera   quarter pane   off / six", os.path.join(out_dir, "edge-compare.jpg"), 2)

    cells = []
    titles = []
    for label, card in (("ink-before", False), ("ink-after", True)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_punch, 0.0, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        origin, right, up, half_w, half_h = pass29_cam_axes(cam, 4.6)
        wide = half_w * 2.0 * 0.42
        tall = half_h * 2.0 * 0.28
        toward = cam.location - origin
        if toward.length > 0.001:
            toward = toward.normalized()
        if card:
            pass29_quad(origin, right, up, wide, tall, (0.02, 0.02, 0.02), 0.72)
            pass29_quad(origin + toward * 0.03, right, up, wide * 0.16, tall * 0.42, (1.0, 0.55, 0.12), 1.0)
        else:
            pass29_quad(origin, right, up, wide * 0.16, tall * 0.42, (1.0, 0.55, 0.12), 1.0)
        shoot(label, cells, titles)
    pass29_boxes(os.path.join(out_dir, "ink-before.jpg"), os.path.join(out_dir, "ink-after.jpg"), "ink")
    p14_grid(cells, titles, "Contact ink   chase camera   quarter pane   burst / card", os.path.join(out_dir, "ink-compare.jpg"), 2)

    cells = []
    titles = []
    for label, wedge in (("wedge-before", False), ("wedge-after", True)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_run, 0.4, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        if wedge:
            origin, right, up, half_w, half_h = pass29_cam_axes(cam, 2.2)
            at = origin + right * (half_w * 0.92) + up * (half_h * 0.05)
            pass29_star(at, right, up, 0.22, crown)
            pass30_shape(at + right * 0.16, right, up, "chip", seat, 0.08)
        shoot(label, cells, titles)
    p14_grid(cells, titles, "Off-screen It   chase camera   quarter pane   hidden / wedge", os.path.join(out_dir, "wedge-compare.jpg"), 2)

    cells = []
    titles = []
    for label, plate in (("plate-before", False), ("plate-after", True)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_run, 0.2, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        head = bone_pos(arm, "Head")
        hat = head + Vector((0.0, 0.0, 0.22))
        pass30_shape(hat, Vector((1.0, 0.0, 0.0)), Vector((0.0, 0.0, 1.0)), "chip", (1.0, 0.82, 0.15), 0.16)
        if plate:
            origin, right, up, _hw, _hh = pass29_cam_axes(cam, (cam.location - hat).length)
            at = hat + Vector((0.0, 0.0, 0.28))
            pass28_card(
                [at - right * 0.16 - up * 0.16, at + right * 0.16 - up * 0.16, at + right * 0.16 + up * 0.16, at - right * 0.16 + up * 0.16],
                (0.06, 0.05, 0.04),
            )
            pass29_star(at, right, up, 0.22, crown)
        shoot(label, cells, titles)
    p14_grid(cells, titles, "Crown plate   chase camera   quarter pane   hat / plate", os.path.join(out_dir, "plate-compare.jpg"), 2)

    cells = []
    titles = []
    for label, moved in (("handoff-before", False), ("handoff-after", True)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_run, 0.2, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        fwd, left = p11_heading(yaw)
        other = foot + left * 1.35
        def body_at(pos, shirt):
            chest = Vector((pos.x, pos.y, 1.05))
            pass30_shape(chest, left, Vector((0.0, 0.0, 1.0)), "chip", shirt, 0.34)
            pass30_shape(chest + Vector((0.0, 0.0, 0.42)), left, Vector((0.0, 0.0, 1.0)), "puff", shirt, 0.16)
        body_at(foot, seat)
        body_at(other, (0.25, 0.55, 1.0))
        receiver = foot + Vector((0.0, 0.0, 1.7))
        tagger = other + Vector((0.0, 0.0, 1.7))
        origin, right, up, _hw, _hh = pass29_cam_axes(cam, 5.0)
        plate_at = receiver if moved else tagger
        pass29_star(plate_at, right, up, 0.18, crown)
        pass30_shape(receiver + Vector((0.0, 0.0, -1.55)), left, fwd, "sheet", seat if moved else (0.2, 0.2, 0.2), 0.22)
        pass30_shape(tagger + Vector((0.0, 0.0, -1.55)), left, fwd, "sheet", (0.85, 0.95, 1.0) if moved else seat, 0.22)
        shoot(label, cells, titles)
    p14_grid(cells, titles, "Handoff   chase camera   quarter pane   plate moves, no freeze", os.path.join(out_dir, "handoff-compare.jpg"), 2)


def render_pass30(arm, cam):
    """Quarter-pane chase stills. Shape carries the surface. Counts stay."""
    from PIL import Image

    scene = bpy.context.scene
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "SUN":
            obj.data.energy = 1.4
        elif obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 28
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    scene.eevee.taa_render_samples = 8
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    out_dir = os.path.join(ROOT, "Docs", "FxStills", "pass30")
    os.makedirs(out_dir, exist_ok=True)
    tmp = "/tmp/pass30"
    os.makedirs(tmp, exist_ok=True)
    yaw = 24.0
    row = (
        ("grass", (0.76, 0.77, 0.70), "puff", 0.13),
        ("dirt", (0.68, 0.46, 0.24), "cloud", 0.34),
        ("concrete", (0.78, 0.78, 0.76), "sheet", 0.20),
        ("wood", (0.90, 0.76, 0.56), "splinter", 0.16),
        ("metal", (1.0, 0.90, 0.45), "streak", 0.12),
        ("wet", (0.12, 0.22, 0.40), "tick", 0.14),
        ("brick", (0.62, 0.28, 0.16), "chip", 0.18),
    )

    def shoot(name, cells, titles):
        png = os.path.join(tmp, name + ".png")
        scene.render.filepath = png
        bpy.ops.render.render(write_still=True)
        image = Image.open(png).convert("RGB")
        p14_jpeg(os.path.join(out_dir, name + ".jpg"), image)
        cells.append(image.copy())
        titles.append(name.replace("-", " "))

    cells = []
    titles = []
    for label, shaped in (("shape-before", False), ("shape-after", True)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_run, 0.5, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        origin, right, up, _hw, _hh = pass29_cam_axes(cam, 2.5)
        # Drawn large in the pane so the shape family reads. Counts stay in DustLook.
        for i, (_name, color, kind, _size) in enumerate(row):
            at = origin + right * ((i - 3) * 0.62) + up * 0.05
            if shaped:
                pass30_family(at, right, up, kind, color)
            else:
                puff = "cloud" if kind == "cloud" else "puff"
                span = 0.34 if kind == "cloud" else 0.16
                pass30_family(at, right, up, puff, color, span)
        shoot(label, cells, titles)
    p14_grid(cells, titles, "Surface shape   chase camera   quarter pane   tint / shape", os.path.join(out_dir, "shape-compare.jpg"), 2)

    cells = []
    titles = []
    for label, mark in (("mark-before", False), ("mark-after", True)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_run, 0.6, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        fwd, left = p11_heading(yaw)
        origin, right, up, _hw, _hh = pass29_cam_axes(cam, 3.2)
        if mark:
            at = origin + up * -0.15
            pass30_shape(at, right, up, "sheet", (0.42, 0.42, 0.40), 0.62)
        else:
            for i in range(6):
                at = origin + right * ((i - 2.5) * 0.16) + up * ((i % 2) * 0.10)
                pass30_shape(at, right, up, "puff", (0.78, 0.78, 0.76), 0.20)
        shoot(label, cells, titles)
    p14_grid(cells, titles, "Contact mark   chase camera   quarter pane   puff / stain", os.path.join(out_dir, "mark-compare.jpg"), 2)

    cells = []
    titles = []
    for label, shaped in (("wall-before", False), ("wall-after", True)):
        p11_clear("P11Fx")
        p11_clear("P11Geo")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_wall, 0.0, 0.0)
        foot = p11_foot(arm)
        p14_aim(cam, foot, 0.0)
        bpy.context.view_layer.update()
        fwd, _left = p11_heading(0.0)
        normal = -fwd
        center = foot + fwd * 0.55
        center.z = 1.15
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(center.x + fwd.x * 0.08, center.y + fwd.y * 0.08, 1.6))
        wall = bpy.context.active_object
        wall.name = p11_name("Geo")
        wall.scale = (2.6, 0.12, 3.2)
        wall.data.materials.append(make_mat(p11_name("Mat"), (0.55, 0.56, 0.58, 1.0), 0.9))
        along = Vector((-normal.y, normal.x, 0.0))
        if along.length < 0.001:
            along = Vector((1.0, 0.0, 0.0))
        along.normalize()
        metal_at = center + along * 0.55
        metal_at.z = 1.15
        nick_at = center - along * 0.55
        nick_at.z = 1.05
        foot_at = foot + Vector((0.0, 0.0, 0.2))
        up = Vector((0.0, 0.0, 1.0))
        if shaped:
            pass30_shape(metal_at, along, up, "streak", (1.0, 0.90, 0.45), 0.90)
            pass30_shape(foot_at, along, up, "streak", (1.0, 0.90, 0.45), 0.70)
            pass30_shape(nick_at, along, up, "sheet", (0.78, 0.78, 0.76), 0.55)
        else:
            pass30_shape(metal_at, along, up, "puff", (0.75, 0.72, 0.66), 0.36)
            pass30_shape(foot_at, along, up, "puff", (0.75, 0.72, 0.66), 0.28)
            pass30_shape(nick_at, along, up, "puff", (0.75, 0.72, 0.66), 0.32)
        shoot(label, cells, titles)
    p14_grid(cells, titles, "Wall matches foot   chase camera   quarter pane   gray puff / streak and nick", os.path.join(out_dir, "wall-compare.jpg"), 2)


def render_pass28(arm, cam):
    """Quarter-pane chase stills for flares, the wall ring, the land stroke, and the wall ribbon."""
    from PIL import Image

    scene = bpy.context.scene
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "SUN":
            obj.data.energy = 1.4
        elif obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 28
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    scene.eevee.taa_render_samples = 8
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    out_dir = os.path.join(ROOT, "Docs", "FxStills", "pass28")
    os.makedirs(out_dir, exist_ok=True)
    tmp = "/tmp/pass28"
    os.makedirs(tmp, exist_ok=True)
    yaw = 24.0
    tint = (0.25, 0.55, 1.0)
    pull_tint = (1.0, 0.82, 0.15)

    def shoot(name, cells, titles):
        png = os.path.join(tmp, name + ".png")
        scene.render.filepath = png
        bpy.ops.render.render(write_still=True)
        image = Image.open(png).convert("RGB")
        p14_jpeg(os.path.join(out_dir, name + ".jpg"), image)
        cells.append(image.copy())
        titles.append(name.replace("-", " "))
        return image

    # 1a. Diagonal dash. Before is trail-aligned. After flares across the camera.
    diag_cells = []
    diag_titles = []
    for label, flare in (("diag-before", False), ("diag-after", True)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, lambda a: pose_airdash(a, 0.70, 0.70), 0.85, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        travel, _rope = pass25_travel(arm, foot, yaw, "dash", 0.70, 0.70)
        if flare:
            pass27_burst(arm, cam, tint)
        else:
            pass28_trail(arm, travel, tint, cam)
        shoot(label, diag_cells, diag_titles)
    p14_grid(
        diag_cells, diag_titles,
        "Diagonal dash   chase camera   quarter pane   trail / flare",
        os.path.join(out_dir, "diag-compare.jpg"), 2,
    )

    # 1b. Grapple pull along the rope. Same flare, plus the rope tell.
    pull_cells = []
    pull_titles = []
    for label, flare in (("pull-before", False), ("pull-after", True)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_grapple, 0.75, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        travel, _rope = pass25_travel(arm, foot, yaw, "pull", 0.0, 1.0)
        pass25_rope(arm, foot, yaw, 0.0, 1.0)
        if flare:
            pass27_burst(arm, cam, pull_tint)
        else:
            pass28_trail(arm, travel, pull_tint, cam)
        shoot(label, pull_cells, pull_titles)
    p14_grid(
        pull_cells, pull_titles,
        "Grapple pull   chase camera   quarter pane   trail / flare",
        os.path.join(out_dir, "pull-compare.jpg"), 2,
    )

    # 2. Wall-run start. Before is the 0.21 m thin hole. After pops to 0.26 m with a thicker stroke.
    wall_cells = []
    wall_titles = []
    for after in (False, True):
        p11_clear("P11Fx")
        p11_clear("P11Geo")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_wall, 0.0, 0.0)
        foot = p11_foot(arm)
        p14_aim(cam, foot, 0.0)
        bpy.context.view_layer.update()
        fwd, _left = p11_heading(0.0)
        normal = -fwd
        center = foot + fwd * 0.55
        center.z = 1.15
        along = Vector((-normal.y, normal.x, 0.0))
        if along.length < 0.001:
            along = Vector((1.0, 0.0, 0.0))
        along.normalize()
        hit = center + along * 0.95
        hit.z = 1.25
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(center.x + fwd.x * 0.08, center.y + fwd.y * 0.08, 1.6))
        wall = bpy.context.active_object
        wall.name = p11_name("Geo")
        wall.scale = (2.6, 0.12, 3.2)
        wall.data.materials.append(make_mat(p11_name("Mat"), (0.62, 0.62, 0.60, 1.0), 0.9))
        pass26_scuff(hit, normal, 8.0, (0.20, 0.20, 0.19), (0.84, 0.83, 0.80), cam.location)
        if after:
            radius = 0.26
            inner = 0.30
        else:
            radius = 0.21
            inner = 0.42
        state = impact22_state("concrete", 13.8, age_u=0.04 / 0.25, radius_override=radius, bits_override=0)
        state["bits"] = 0
        state["plumes"] = 0
        state["ring_inner"] = inner
        state["opacity"] = 0.96
        state["dust"] = (0.96, 0.94, 0.90)
        state["shown"] = radius
        impact22_draw(hit, normal, state, cam.location)
        nrm = Vector(normal).normalized()
        # Hole across the ring, in pixels, so a few-px hole fails the log.
        hole = radius * (inner / 0.96)
        span = pass27_span(hit - along * hole, hit + along * hole, cam)
        print("WALL", "after" if after else "before", "r", radius, "inner", inner, "hole_px", round(span, 1))
        if after:
            for i in range(2):
                h = p11_rand(i, 9)
                pos = Vector(hit) + nrm * (0.36 + h * 0.10) + along * ((i - 0.5) * 0.22)
                pos.z = hit.z + 0.62 + h * 0.12
                impact24_puff(pos, 0.32, (0.90, 0.88, 0.84), 0.78, cam.location)
        name = "wall-after" if after else "wall-before"
        shoot(name, wall_cells, wall_titles)
    p14_grid(
        wall_cells, wall_titles,
        "Wall-run start   chase camera   quarter pane   0.21 m / 0.26 m pop",
        os.path.join(out_dir, "wall-compare.jpg"), 2,
    )

    # 4. Wall-run seat ribbon. The small start ring stays. After adds the clipped strip.
    ribbon_cells = []
    ribbon_titles = []
    for label, strip in (("ribbon-before", False), ("ribbon-after", True)):
        p11_clear("P11Fx")
        p11_clear("P11Geo")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_wall, 0.0, 0.0)
        foot = p11_foot(arm)
        p14_aim(cam, foot, 0.0)
        bpy.context.view_layer.update()
        fwd, _left = p11_heading(0.0)
        normal = -fwd
        center = foot + fwd * 0.55
        center.z = 1.15
        along = Vector((-normal.y, normal.x, 0.0))
        if along.length < 0.001:
            along = Vector((1.0, 0.0, 0.0))
        along.normalize()
        hit = center + along * 0.95
        hit.z = 1.22
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(center.x + fwd.x * 0.08, center.y + fwd.y * 0.08, 1.6))
        wall = bpy.context.active_object
        wall.name = p11_name("Geo")
        wall.scale = (2.6, 0.12, 3.2)
        wall.data.materials.append(make_mat(p11_name("Mat"), (0.55, 0.32, 0.22, 1.0), 0.9))
        tail = hit - along * 1.50
        tail.z = 1.18
        head = hit - along * 0.08
        head.z = 1.22
        state = impact22_state("concrete", 8.0, age_u=0.04 / 0.25, radius_override=0.26, bits_override=0)
        state["bits"] = 0
        state["plumes"] = 0
        state["ring_inner"] = 0.30
        state["opacity"] = 0.96
        state["dust"] = (0.96, 0.94, 0.90)
        state["shown"] = 0.26
        impact22_draw(tail, normal, state, cam.location)
        if strip:
            pass28_strip(tail, head, normal, cam)
        else:
            print("RIBBON", "before", "strip", 0)
        shoot(label, ribbon_cells, ribbon_titles)
    p14_grid(
        ribbon_cells, ribbon_titles,
        "Wall ribbon   chase camera   quarter pane   ring / ring plus strip",
        os.path.join(out_dir, "ribbon-compare.jpg"), 2,
    )
    pass28_delta(
        os.path.join(out_dir, "ribbon-before.jpg"),
        os.path.join(out_dir, "ribbon-after.jpg"),
    )

    # 3. Hard-land ring. Same 0.80 m radius. The stroke is the difference.
    ring_cells = []
    ring_titles = []
    for label, inner in (("ring-before", 0.90), ("ring-after", 0.74)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_land, 0.0, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        state = impact22_state(
            "concrete", 36.5, age_u=0.12 / 0.25,
            radius_override=0.80, bits_override=5,
        )
        state["ring_inner"] = inner
        state["bits"] = 5
        state["plumes"] = 0
        impact22_draw(Vector((foot.x, foot.y, 0.02)), (0.0, 0.0, 1.0), state, cam.location)
        print("RING", label, "r", round(state["shown"], 2), "inner", inner)
        shoot(label, ring_cells, ring_titles)
        pass28_bottom(os.path.join(out_dir, label + ".jpg"))
    p14_grid(
        ring_cells, ring_titles,
        "Hard-land ring   chase camera   quarter pane   thin stroke / thick stroke",
        os.path.join(out_dir, "ring-compare.jpg"), 2,
    )


def render_pass27(arm, cam):
    """Quarter-pane chase stills. Before and after for each pass 27 fix."""
    from PIL import Image

    scene = bpy.context.scene
    for obj in bpy.data.objects:
        if obj.type == "LIGHT" and obj.data.type == "SUN":
            obj.data.energy = 1.4
        elif obj.type == "LIGHT" and obj.data.type == "AREA":
            obj.data.energy = 28
    scene.render.resolution_x = 640
    scene.render.resolution_y = 360
    scene.eevee.taa_render_samples = 8
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    out_dir = os.path.join(ROOT, "Docs", "FxStills", "pass27")
    os.makedirs(out_dir, exist_ok=True)
    tmp = "/tmp/pass27"
    os.makedirs(tmp, exist_ok=True)
    yaw = 24.0
    tint = (0.95, 0.28, 0.32)

    def shoot(name, cells, titles):
        png = os.path.join(tmp, name + ".png")
        scene.render.filepath = png
        bpy.ops.render.render(write_still=True)
        image = Image.open(png).convert("RGB")
        p14_jpeg(os.path.join(out_dir, name + ".jpg"), image)
        cells.append(image.copy())
        titles.append(name.replace("-", " "))
        return image

    # 1. Forward dash, real chase camera, quarter pane.
    line_cells = []
    line_titles = []
    for label, burst in (("lines-before", False), ("lines-after", True)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, lambda a: pose_airdash(a, 0.0, 1.0), 0.85, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        fwd, _left = p11_heading(yaw)
        if burst:
            pass27_burst(arm, cam, tint)
        else:
            pass25_streaks(arm, fwd.normalized(), tint)
            pass26_edge_streaks(arm, fwd.normalized(), tint)
        shoot(label, line_cells, line_titles)
    p14_grid(line_cells, line_titles, "Forward dash   chase camera   quarter pane   before / after", os.path.join(out_dir, "lines-compare.jpg"), 2)

    # 2. Brick tint versus concrete grey. Neutral ground, puffs in front of the feet.
    brick_cells = []
    brick_titles = []
    for label, color in (
        ("brick-before", (0.78, 0.78, 0.76)),
        ("brick-after", (0.62, 0.28, 0.16)),
    ):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_run, 0.4, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        to_cam = cam.location - foot
        to_cam.z = 0.0
        if to_cam.length < 0.001:
            to_cam = Vector((0.0, -1.0, 0.0))
        to_cam.normalize()
        side = Vector((-to_cam.y, to_cam.x, 0.0))
        for i in range(5):
            h = p11_rand(i, 3)
            pos = Vector((foot.x, foot.y, 0.0))
            pos += to_cam * (0.85 + (i % 2) * 0.28)
            pos += side * ((i - 2) * 0.48)
            pos.z = 0.55 + (i % 3) * 0.16
            impact25_accent(pos, 0.38, color, i + 4)
        shoot(label, brick_cells, brick_titles)
    p14_grid(brick_cells, brick_titles, "Brick dust   chase camera   quarter pane   concrete tint / brick tint", os.path.join(out_dir, "brick-compare.jpg"), 2)

    # 3. Wall-run start. Slow contact stays a small ring. Not the bounce shockwave.
    wall_cells = []
    wall_titles = []
    for speed, label_speed in ((8.0, "slow"), (13.8, "sprint")):
        for after in (False, True):
            p11_clear("P11Fx")
            p11_clear("P11Geo")
            p11_ground("concrete", asphalt=True)
            apply_pose(arm, pose_wall, 0.0, 0.0)
            foot = p11_foot(arm)
            p14_aim(cam, foot, 0.0)
            bpy.context.view_layer.update()
            fwd, _left = p11_heading(0.0)
            normal = -fwd
            center = foot + fwd * 0.55
            center.z = 1.15
            along = Vector((-normal.y, normal.x, 0.0))
            if along.length < 0.001:
                along = Vector((1.0, 0.0, 0.0))
            along.normalize()
            # Beside the shoulder, on open wall. A contact on the chest is hidden by the torso.
            hit = center + along * 0.95
            hit.z = 1.25
            bpy.ops.mesh.primitive_cube_add(size=1.0, location=(center.x + fwd.x * 0.08, center.y + fwd.y * 0.08, 1.6))
            wall = bpy.context.active_object
            wall.name = p11_name("Geo")
            wall.scale = (2.6, 0.12, 3.2)
            wall.data.materials.append(make_mat(p11_name("Mat"), (0.62, 0.62, 0.60, 1.0), 0.9))
            pass26_scuff(hit, normal, speed, (0.20, 0.20, 0.19), (0.84, 0.83, 0.80), cam.location)
            if after:
                slow = speed < 13.8
                u = max(0.0, min(1.0, speed / 13.8))
                radius = (0.14 + u * 0.12) if slow else 0.28
                puffs = 2 if slow else 3
                puff_size = 0.32 if slow else 0.40
                state = impact22_state("concrete", 13.8, age_u=0.10 / 0.25, radius_override=radius, bits_override=0)
                state["bits"] = 0
                state["plumes"] = 0
                state["ring_inner"] = 0.38
                state["opacity"] = 0.96
                state["dust"] = (0.96, 0.94, 0.90)
                impact22_draw(hit, normal, state, cam.location)
                nrm = Vector(normal).normalized()
                for i in range(puffs):
                    h = p11_rand(i, 9)
                    pos = Vector(hit) + nrm * (0.36 + h * 0.10) + along * ((i - 1) * 0.18)
                    pos.z = hit.z + 0.62 + h * 0.12
                    impact24_puff(pos, puff_size, (0.90, 0.88, 0.84), 0.78, cam.location)
                print("WALLRUN", label_speed, "radius", round(radius, 2), "puffs", puffs, "puff", puff_size)
            name = "wallrun-%s-%s" % (label_speed, "after" if after else "before")
            shoot(name, wall_cells, wall_titles)
    p14_grid(
        wall_cells, wall_titles,
        "Wall-run start   chase camera   quarter pane   scuff only / small ring",
        os.path.join(out_dir, "wallrun-compare.jpg"),
        2,
    )

    # 4. Hard-land ring. Before is the 3.3 m ring. After stays under a metre.
    ring_cells = []
    ring_titles = []
    age = 0.12
    for label, radius in (("ring-before", 3.30), ("ring-after", 0.80)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_land, 0.0, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        pass27_land(arm, cam, foot, yaw, age, radius, False, 5)
        path_name = label
        shoot(path_name, ring_cells, ring_titles)
        pass27_border(os.path.join(out_dir, label + ".jpg"))
    p14_grid(ring_cells, ring_titles, "Hard-land ring   chase camera   quarter pane   3.3 m / 0.80 m", os.path.join(out_dir, "ring-compare.jpg"), 2)

    # 5. Chunks. Same small ring so the pieces are the difference. Plume stays under 1 m.
    chunk_cells = []
    chunk_titles = []
    for label, chunky, bits in (("chunks-before", False, 19), ("chunks-after", True, 5)):
        p11_clear("P11Fx")
        p11_ground("concrete", asphalt=True)
        apply_pose(arm, pose_land, 0.0, yaw)
        foot = p11_foot(arm)
        p14_aim(cam, foot, yaw)
        bpy.context.view_layer.update()
        pass27_land(arm, cam, foot, yaw, 0.09, 0.80, chunky, bits)
        shoot(label, chunk_cells, chunk_titles)
    p14_grid(chunk_cells, chunk_titles, "Hard-land chunks   chase camera   quarter pane   grit / five chunks", os.path.join(out_dir, "chunks-compare.jpg"), 2)


def main():
    global OUT
    if os.environ.get("FX_PASS17") == "1":
        OUT = os.path.join(ROOT, "Docs", "FxStills", "pass17")
    elif os.environ.get("FX_PASS16") == "1":
        OUT = os.path.join(ROOT, "Docs", "FxStills", "pass16")
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=FBX)
    arm = next(obj for obj in bpy.data.objects if obj.type == "ARMATURE")
    cam = setup_world(arm)
    if os.environ.get("FX_PASS30") == "1":
        render_pass30(arm, cam)
        return
    if os.environ.get("FX_PASS29") == "1":
        render_pass29(arm, cam)
        return
    if os.environ.get("FX_PASS28") == "1":
        render_pass28(arm, cam)
        return
    if os.environ.get("FX_PASS27") == "1":
        render_pass27(arm, cam)
        return
    if os.environ.get("FX_PASS26") == "1":
        render_pass26(arm, cam)
        return
    if os.environ.get("FX_PASS25_LINES") == "1":
        out_dir = os.path.join(ROOT, "Docs", "FxStills", "pass25")
        os.makedirs(out_dir, exist_ok=True)
        tmp = "/tmp/pass25-lines"
        os.makedirs(tmp, exist_ok=True)
        render_pass25_lines(arm, cam, out_dir, tmp)
        return
    if os.environ.get("FX_PASS25") == "1" or os.environ.get("FX_PASS24") == "1":
        render_pass22_impact(arm, cam)
        return
    if os.environ.get("FX_PASS23") == "1" or os.environ.get("FX_PASS22") == "1":
        render_pass22_impact(arm, cam)
        return
    if os.environ.get("FX_PASS21") == "1":
        render_pass21_impact(arm, cam)
        return
    if os.environ.get("FX_PASS20") == "1":
        render_pass20_runners(arm, cam)
        return
    if os.environ.get("FX_PASS19") == "1":
        render_pass19_park(arm, cam)
        return
    if os.environ.get("FX_PASS17") == "1" or os.environ.get("FX_PASS16") == "1" or os.environ.get("FX_PASS15") == "1":
        render_pass15(arm, cam)
        return
    if os.environ.get("FX_PASS14") == "1":
        render_pass14(arm, cam)
        return
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
