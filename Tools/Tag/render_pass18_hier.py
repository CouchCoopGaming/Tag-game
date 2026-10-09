"""Pass 18 full-clip stills.

Eight frames sampled across each clip, not eight consecutive 30 fps
frames. The 30 fps pop check still walks every frame. Gameplay timers
are not written. Vault and slide contacts stay on the pass 16 poses.
The climb lip pose and the pad plant stay as they were.
"""
import importlib.util
import math
import os
import sys

import bpy
from mathutils import Euler, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
# The pass 17 renderer is the pose and prop source. Importing it does not render.
spec = importlib.util.spec_from_file_location(
    "pass17", os.path.join(ROOT, "Tools", "Tag", "render_pass17_hier.py")
)
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)

OUT = os.environ.get("PASS18_OUT", os.path.join(ROOT, "Docs", "SmoothStills", "pass18"))
ONLY = set(filter(None, os.environ.get("PASS18_ONLY", "").split(",")))
DO_RENDER = os.environ.get("PASS18_RENDER", "1") != "0"
SAMPLES = int(os.environ.get("PASS18_SAMPLES", "8"))

# Whole clip, seconds. Roll is the 65% land. Vault is mantleDuration.
# Slide is the 0.26 s in plus the 0.26 s out. Wall run, shove, then the arc.
# Climb is the short reach onto the lip, then the 0.40 s mantle to standing.
DUR = {
    "roll": 0.52,
    "climb": 0.4667,
    "vault": 0.40,
    "slide": 0.62,
    "wall": 0.75,
}
ORDER = ("vault", "climb", "slide", "wall", "roll")

FONT = {
    "0": ("111", "101", "101", "101", "111"),
    "1": ("010", "110", "010", "010", "111"),
    "2": ("111", "001", "111", "100", "111"),
    "3": ("111", "001", "111", "001", "111"),
    "4": ("101", "101", "111", "001", "001"),
    "5": ("111", "100", "111", "001", "111"),
    "6": ("111", "100", "111", "101", "111"),
    "7": ("111", "001", "001", "001", "001"),
    "8": ("111", "101", "111", "101", "111"),
    "9": ("111", "101", "111", "001", "111"),
    "f": ("111", "100", "110", "100", "100"),
    "s": ("111", "100", "111", "001", "111"),
    ".": ("0", "0", "0", "0", "1"),
    " ": ("0", "0", "0", "0", "0"),
}


def smooth(u):
    if u <= 0.0:
        return 0.0
    if u >= 1.0:
        return 1.0
    return u * u * (3.0 - 2.0 * u)


def pitch_of(arm, bone):
    bpy.context.view_layer.update()
    y = (arm.matrix_world @ arm.pose.bones[bone].matrix).to_3x3() @ Vector((0.0, 1.0, 0.0))
    if y.length < 1e-6:
        return 0.0
    y.normalize()
    forward = p.body_forward(arm)
    return math.degrees(math.atan2(y.dot(forward), y.z))


def pelvis_z(arm):
    bpy.context.view_layer.update()
    return p.bone_head(arm, "Hips").z


def lowest(mesh):
    return min(v.z for v in p.mesh_world(mesh))


def seat(arm, mesh, z):
    arm.location.z += z - lowest(mesh)
    bpy.context.view_layer.update()


def shift(arm, delta):
    arm.location = Vector(arm.location) + delta
    bpy.context.view_layer.update()


_NOCLIP = None


def _noclip():
    global _NOCLIP
    if _NOCLIP is None:
        spec_n = importlib.util.spec_from_file_location(
            "noclip_check", os.path.join(ROOT, "Tools", "Tag", "noclip_check.py")
        )
        _NOCLIP = importlib.util.module_from_spec(spec_n)
        spec_n.loader.exec_module(_NOCLIP)
    return _NOCLIP


def separate_world(arm):
    """Keep pieces out of each other and out of solids. Visual only."""
    # The noclip runner settles once per frame. Doing it here as well doubles the scan.
    if os.environ.get("NOCLIP_SETTLE") == "runner":
        return
    _noclip().settle_body(arm)


def pose_roll_over(arm):
    # Visual only. HandoffFeel timers stay. Forward pitch past 90, onto one shoulder.
    # Arms stay near the land and the run so a 30 fps step stays inside 25 degrees.
    p.leg(arm, "L", 96.0, -128.0)
    p.leg(arm, "R", 88.0, -120.0)
    p.arm_pose(arm, "L", -42.0, 12.0, -36.0, 0.0)
    p.arm_pose(arm, "R", 12.0, 8.0, -40.0, 0.0)
    p.torso(arm, 108.0, 40.0, -4.0)
    p.set_euler(arm, "Hips", 108.0, 0.0, 40.0)
    p.set_euler(arm, "Chest", 8.0, 0.0, 18.0)


def pose_climb_clear(arm):
    # Hands together over the head, chest on the face, lead foot low on the face.
    # The forearm cuff still crosses by about 6 cm. settle_body presses that
    # shell onto the wall. The thigh stays behind the chest, so the root does
    # not step back off the plant.
    p.torso(arm, 16.0, 4.0, -75.0)
    p.arm_pose(arm, "L", -160.0, -58.0, -20.0, 8.0, 0.0)
    p.arm_pose(arm, "R", -160.0, -58.0, -20.0, 8.0, 0.0)
    p.leg(arm, "L", 42.0, -35.0, 32.0)
    p.set_euler(arm, "Foot_L", -24.0, 0.0, 0.0)
    p.leg(arm, "R", 28.0, -36.0, 16.0)
    p.set_euler(arm, "Foot_R", 4.0, 0.0, 0.0)


def pose_climb_low(arm):
    # Same family as the lip, one step below it. The reach stays inside 25 degrees.
    p.torso(arm, 12.0, 2.0, -42.0)
    p.arm_pose(arm, "L", -128.0, -32.0, -20.0, 4.0, 0.0)
    p.arm_pose(arm, "R", -128.0, -32.0, -20.0, 4.0, 0.0)
    # Bent enough that the hips sit below the lip plant once the feet are on the ground.
    p.leg(arm, "L", 64.0, -78.0, 18.0)
    p.set_euler(arm, "Foot_L", -12.0, 0.0, 0.0)
    p.leg(arm, "R", 36.0, -48.0, 10.0)
    p.set_euler(arm, "Foot_R", 4.0, 0.0, 0.0)


def pose_stand(arm):
    p.leg(arm, "L", 6.0, -8.0)
    p.leg(arm, "R", 2.0, -6.0)
    p.arm_pose(arm, "L", -22.0, -12.0, -16.0, 0.0)
    p.arm_pose(arm, "R", -18.0, -12.0, -14.0, 0.0)
    p.torso(arm, 3.0, 4.0, -1.0)


def pose_vault_land(arm):
    p.leg(arm, "L", 48.0, -70.0)
    p.leg(arm, "R", 42.0, -62.0)
    p.arm_pose(arm, "L", -28.0, 10.0, -24.0, 0.0)
    p.arm_pose(arm, "R", -24.0, 10.0, -22.0, 0.0)
    p.torso(arm, 18.0, 8.0, -2.0)


def pose_vault_reach(arm):
    # In front of the box, hands clear of the top.
    p.leg(arm, "L", 36.0, -42.0)
    p.leg(arm, "R", 28.0, -36.0)
    p.arm_pose(arm, "L", -36.0, 14.0, -40.0, 8.0)
    p.arm_pose(arm, "R", -36.0, 14.0, -40.0, 8.0)
    p.torso(arm, 12.0, 6.0, -4.0)


def ages_30(duration):
    times = []
    t = 0.0
    step = 1.0 / 30.0
    while t < duration - 1e-4:
        times.append(t)
        t += step
    times.append(duration)
    return times


def still_ages(duration):
    return [i / 7.0 * duration for i in range(8)]


def blend(a, b, w):
    return p.slerp_pose(a, b, w)


def apply_key(arm, pose, yaw, lift):
    p.apply_dict(arm, pose, yaw, lift)


def body_meshes():
    return (
        "Mesh_Hand_L", "Mesh_Hand_R", "Mesh_Foot_L", "Mesh_Foot_R",
        "Mesh_Head", "Mesh_Hips", "Mesh_Chest",
        "Mesh_UpperArm_L", "Mesh_UpperArm_R", "Mesh_LowerArm_L", "Mesh_LowerArm_R",
        "Mesh_UpperLeg_L", "Mesh_UpperLeg_R", "Mesh_LowerLeg_L", "Mesh_LowerLeg_R",
    )


def body_points():
    pts = []
    for name in body_meshes():
        if name in bpy.data.objects:
            pts.extend(p.mesh_world(name))
    return pts


def frame_camera(cam, arm, side):
    pts = body_points()
    zs = [v.z for v in pts]
    height = max(zs) - min(zs)
    center = sum(pts, Vector()) / len(pts)
    # 48 mm, landscape sensor. Vertical half-angle tangent.
    half = math.tan(math.radians(31.44) * 0.5)
    dist = (height / 0.62) / (2.0 * half)
    width = 0.0
    side_n = side.normalized()
    # Width in the view plane, horizontal.
    right = Vector((0.0, 0.0, 1.0)).cross(side_n)
    if right.length > 1e-4:
        right.normalize()
        xs = [(v - center).dot(right) for v in pts]
        width = max(xs) - min(xs)
        dist_w = (width / 0.78) / (2.0 * half * (960.0 / 720.0))
        dist = max(dist, dist_w)
    dist = max(dist, 1.35)
    cam.data.type = "PERSP"
    cam.data.lens = p.CAM_LENS
    cam.data.sensor_fit = "AUTO"
    cam.location = center + side_n * dist + Vector((0.0, 0.0, height * 0.08))
    p.look_at(cam, center + Vector((0.0, 0.0, height * 0.02)))
    cam.data.clip_start = 0.05
    cam.data.clip_end = 80.0
    bpy.context.view_layer.update()


def contacts(arm, kind, ctx):
    cloud = {}

    def put(label, bone, planted):
        cloud[label] = (p.bone_head(arm, bone).copy(), planted)

    if kind == "vault":
        top = ctx["top"]
        for label, mesh, bone in (
            ("handL", "Mesh_Hand_L", "Hand_L"),
            ("handR", "Mesh_Hand_R", "Hand_R"),
        ):
            put(label, bone, abs(lowest(mesh) - top) <= 0.02)
        for label, mesh, bone in (("footL", "Mesh_Foot_L", "Foot_L"), ("footR", "Mesh_Foot_R", "Foot_R")):
            put(label, bone, lowest(mesh) <= 0.02)
    elif kind == "climb":
        forward = ctx["forward"]
        plane = ctx["plane"]
        top = ctx["top"]
        for label, mesh, bone in (
            ("handL", "Mesh_Hand_L", "Hand_L"),
            ("handR", "Mesh_Hand_R", "Hand_R"),
        ):
            on_lip = abs(lowest(mesh) - top) <= 0.02
            front = max(p.mesh_world(mesh), key=lambda v: v.dot(forward))
            on_face = abs((front - plane).dot(forward)) <= 0.02
            put(label, bone, on_lip or on_face)
        front = max(p.mesh_world("Mesh_Chest"), key=lambda v: v.dot(forward))
        put("chest", "Chest", abs((front - plane).dot(forward)) <= 0.02)
        front = max(p.mesh_world("Mesh_Foot_L"), key=lambda v: v.dot(forward))
        put("footL", "Foot_L", abs((front - plane).dot(forward)) <= 0.02)
    elif kind == "slide":
        for label, mesh, bone in (
            ("footL", "Mesh_Foot_L", "Foot_L"),
            ("footR", "Mesh_Foot_R", "Foot_R"),
            ("handL", "Mesh_Hand_L", "Hand_L"),
            ("handR", "Mesh_Hand_R", "Hand_R"),
        ):
            put(label, bone, lowest(mesh) <= 0.02)
    elif kind == "wall":
        left = ctx["left"]
        plane = ctx["plane"]
        for label, mesh, bone in (("footL", "Mesh_Foot_L", "Foot_L"), ("handL", "Mesh_Hand_L", "Hand_L")):
            front = max(p.mesh_world(mesh), key=lambda v: v.dot(left))
            put(label, bone, abs((front - plane).dot(left)) <= 0.02)
    elif kind == "roll":
        for label, mesh, bone in (
            ("footL", "Mesh_Foot_L", "Foot_L"),
            ("footR", "Mesh_Foot_R", "Foot_R"),
            ("chest", "Mesh_Chest", "Chest"),
            ("hips", "Mesh_Hips", "Hips"),
            ("handL", "Mesh_Hand_L", "Hand_L"),
            ("handR", "Mesh_Hand_R", "Hand_R"),
        ):
            put(label, bone, lowest(mesh) <= 0.02)
    return cloud


def measure_clip(arm, kind, show, ctx):
    prev_q = None
    prev_c = None
    rows = []
    worst = ("", 0.0, -1, "", 0.0)
    times = ages_30(DUR[kind])
    for index, age in enumerate(times):
        show(arm, age, ctx)
        quats = p.bone_quats(arm)
        cloud = contacts(arm, kind, ctx)
        if prev_q is not None:
            top_name = ""
            top_deg = 0.0
            for name, quat in quats.items():
                delta = p.quat_deg(prev_q[name], quat)
                if delta > top_deg:
                    top_deg = delta
                    top_name = name
            cname, cslide = p.slide_cm(prev_c, cloud)
            rows.append((index - 1, index, top_name, top_deg, cname, cslide, age))
            if top_deg > worst[1] or (abs(top_deg - worst[1]) < 0.05 and cslide > worst[4]):
                worst = (top_name, top_deg, index - 1, cname, cslide)
        prev_q = quats
        prev_c = cloud
    for a, b, bone, deg, cname, slide, _age in rows:
        print(
            "POPROW", kind,
            str(a) + "-" + str(b),
            bone, round(deg, 1),
            cname or "-", round(slide, 1),
        )
    print(
        "POP", kind,
        "pair", str(worst[2]) + "-" + str(worst[2] + 1),
        "bone", worst[0], round(worst[1], 1),
        "contact", worst[3] or "-", round(worst[4], 1),
        "frames", len(times),
        "seconds", DUR[kind],
    )
    return worst


def chest_gap(arm, ctx):
    forward = ctx["forward"]
    front = max(p.mesh_world("Mesh_Chest"), key=lambda v: v.dot(forward))
    return (front - ctx["plane"]).dot(forward)


def place_gap(arm, ctx, gap):
    forward = ctx["forward"]
    shift(arm, forward * (gap - chest_gap(arm, ctx)))


def _mesh_gap(arm, ctx, mesh):
    forward = ctx["forward"]
    front = max(p.mesh_world(mesh), key=lambda v: v.dot(forward))
    return (front - ctx["plane"]).dot(forward)


def hold_off_wall(arm, ctx, plant, include_hands=False):
    """Plant frame sits on the face. Every other frame keeps contacts out of the 2 cm band.

    Each contact is handled on its own. A hand already over the lip must not
    hide a chest that is still on the face. Shell relief then presses the lip
    cuff onto the wall without stepping the root.
    """
    forward = ctx["forward"]
    if plant:
        place_gap(arm, ctx, 0.0)
        return
    meshes = ["Mesh_Chest", "Mesh_Foot_L"]
    if include_hands:
        meshes.extend(("Mesh_Hand_L", "Mesh_Hand_R"))
    wall = bpy.data.objects.get("PropWall")
    module = _noclip()
    for _ in range(8):
        depth = 0.0
        if wall is not None and module is not None:
            depth = module._box_depth(wall, body_points())
        if depth > 0.001:
            shift(arm, -forward * (depth + 0.004))
            continue
        gaps = [_mesh_gap(arm, ctx, mesh) for mesh in meshes]
        # Planted band is about -2 cm to +2 cm. -3.5 cm is outside it.
        band = [gap for gap in gaps if -0.035 < gap <= 0.025]
        if not band:
            return
        shift(arm, forward * (-0.035 - max(band)))


def _clears_lip(arm, ctx):
    """True once every vertex is above the wall, so the step onto the top is empty air."""
    top = ctx["top"]
    pts = body_points()
    if not pts:
        return False
    return min(vert.z for vert in pts) >= top - 0.008


def show_roll(arm, age, ctx):
    u = 0.0 if DUR["roll"] <= 0 else age / DUR["roll"]
    # Linear across each half. The raised hump packed the arm turn into one step.
    if u < 0.5:
        w = u / 0.5
        pose = blend(ctx["land"], ctx["peak"], w)
        lift = ctx["land_z"] + (ctx["peak_z"] - ctx["land_z"]) * w
    else:
        w = (u - 0.5) / 0.5
        pose = blend(ctx["peak"], ctx["run"], w)
        lift = ctx["peak_z"] + (ctx["run_z"] - ctx["peak_z"]) * w
    apply_key(arm, pose, ctx["yaw"], lift)
    # Ends sit on the shoes. The middle sits on the chest. Everything else
    # stays clear so a moving foot is not a planted skate.
    if u <= 0.02 or u >= 0.99:
        seat(arm, "Mesh_Foot_L", 0.012)
    elif 0.42 <= u <= 0.58:
        seat(arm, "Mesh_Chest", 0.012)
        if lowest("Mesh_Head") < 0.02:
            arm.location.z += 0.02 - lowest("Mesh_Head")
            bpy.context.view_layer.update()
    else:
        sole = min(
            lowest("Mesh_Foot_L"), lowest("Mesh_Foot_R"),
            lowest("Mesh_Chest"), lowest("Mesh_Hips"),
        )
        if sole < 0.04:
            arm.location.z += 0.04 - sole
            bpy.context.view_layer.update()
    separate_world(arm)


def show_climb(arm, age, ctx):
    approach = DUR["climb"] / 7.0
    if age <= approach + 1e-4:
        w = age / approach if approach > 0 else 1.0
        pose = blend(ctx["low"], ctx["hero"], w)
        apply_key(arm, pose, ctx["yaw"], 0.0)
        target = ctx["pelvis0"] + (ctx["pelvis1"] - ctx["pelvis0"]) * w
        shift(arm, Vector((0.0, 0.0, target - pelvis_z(arm))))
        # Only the lip sample sits on the face. The reach keeps the hands off it too.
        hold_off_wall(arm, ctx, w >= 0.98, include_hands=True)
        separate_world(arm)
        return
    mantle = DUR["climb"] - approach
    u = 0.0 if mantle <= 0 else (age - approach) / mantle
    pose = blend(ctx["hero"], ctx["stand"], u)
    apply_key(arm, pose, ctx["yaw"], 0.0)
    target = ctx["pelvis1"] + (ctx["pelvis_stand"] - ctx["pelvis1"]) * u
    shift(arm, Vector((0.0, 0.0, target - pelvis_z(arm))))
    # Stay in front of the volume, then step on once the body is above the lip.
    hold_off_wall(arm, ctx, False)
    if _clears_lip(arm, ctx):
        shift(arm, ctx["forward"] * (0.05 + ctx["stand_along"] * u))
        if u > 0.92:
            seat(arm, "Mesh_Foot_L", ctx["top"] + 0.012)
        hold_off_wall(arm, ctx, False)
    separate_world(arm)


def show_vault(arm, age, ctx):
    u = age / DUR["vault"]
    # Linear so the knee drive is not packed into one 30 fps step.
    if u < 0.42:
        w = u / 0.42
        pose = blend(ctx["reach"], ctx["hero"], w)
        along = ctx["reach_along"] * (1.0 - w)
        lift = 0.0
    elif u < 0.58:
        pose = ctx["hero"]
        along = 0.0
        lift = ctx["hero_lift"]
    else:
        w = (u - 0.58) / 0.42
        pose = blend(ctx["hero"], ctx["land"], w)
        along = ctx["land_along"] * w
        lift = 0.0
    apply_key(arm, pose, ctx["yaw"], lift)
    if along != 0.0:
        shift(arm, ctx["forward"] * along)
    if u < 0.36:
        hand_z = min(lowest("Mesh_Hand_L"), lowest("Mesh_Hand_R"))
        if hand_z < ctx["top"] + 0.05:
            shift(arm, Vector((0.0, 0.0, (ctx["top"] + 0.05) - hand_z)))
    elif u >= 0.64:
        hand_z = min(lowest("Mesh_Hand_L"), lowest("Mesh_Hand_R"))
        if hand_z < ctx["top"] + 0.05:
            shift(arm, Vector((0.0, 0.0, (ctx["top"] + 0.05) - hand_z)))
        if u > 0.94:
            seat(arm, "Mesh_Foot_L", 0.012)
    separate_world(arm)


def show_slide(arm, age, ctx):
    # 0.26 s in, a short still plant, 0.26 s out. Feet are planted only while still.
    enter = 0.26
    hold = 0.10
    if age <= enter:
        w = p.raised(age / enter)
        pose = blend(ctx["run"], ctx["slide"], w)
        along = ctx["in_along"] * (1.0 - w)
        planted = False
    elif age <= enter + hold:
        pose = ctx["slide"]
        along = 0.0
        planted = True
    else:
        w = p.raised((age - enter - hold) / 0.26)
        pose = blend(ctx["slide"], ctx["run"], w)
        along = ctx["out_along"] * w
        planted = False
    apply_key(arm, pose, ctx["yaw"], 0.0)
    shift(arm, ctx["forward"] * along)
    if planted:
        seat(arm, "Mesh_Foot_L", 0.012)
    else:
        sole = min(lowest("Mesh_Foot_L"), lowest("Mesh_Foot_R"))
        if sole < 0.05:
            arm.location.z += 0.05 - sole
            bpy.context.view_layer.update()
    separate_world(arm)


def show_wall(arm, age, ctx):
    run_end = 0.18
    shove_end = 0.30  # WallSeconds 0.12 after the stride
    if age <= run_end:
        u = age / run_end
        # One stride of the outer leg. The plant stays put.
        swing = math.sin(u * math.pi)
        pose = dict(ctx["run"])
        trail = ctx["trail"]
        for name, base in trail.items():
            x, y, z = base
            pose[name] = (x + 14.0 * swing, y, z)
        apply_key(arm, pose, ctx["yaw"], ctx["run_lift"])
        separate_world(arm)
        return
    if age <= shove_end:
        w = p.raised((age - run_end) / 0.12)
        pose = blend(ctx["run"], ctx["push"], w)
        for held in ctx["plant_bones"]:
            # Spine and chest stay planted. Opening them walks the hand along the face.
            if held == "Head":
                continue
            pose[held] = ctx["run"][held]
        apply_key(arm, pose, ctx["yaw"], ctx["run_lift"])
        separate_world(arm)
        return
    arc_age = age - shove_end
    w = p.wall_arc(arc_age)
    air_u = arc_age / max(0.05, DUR["wall"] - shove_end)
    # Leave from the pose the shove actually showed. The stored push still
    # has the open chest, and blending from that snaps the plant arm.
    start = dict(ctx["push"])
    for held in ctx["plant_bones"]:
        if held == "Head":
            continue
        start[held] = ctx["run"][held]
    pose = blend(start, ctx["air"], w)
    apply_key(arm, pose, ctx["yaw"], ctx["run_lift"])
    # First air frame is already off the face, then the jump rises.
    off = 0.08 + 0.34 * min(1.0, air_u)
    up = 0.06 + 0.72 * min(1.0, air_u)
    shift(arm, ctx["left"] * (-off))
    shift(arm, Vector((0.0, 0.0, up)))
    separate_world(arm)


SHOWS = {
    "roll": show_roll,
    "climb": show_climb,
    "vault": show_vault,
    "slide": show_slide,
    "wall": show_wall,
}


def side_for(kind, ctx):
    forward = ctx["forward"]
    left = ctx.get("left") or p.horiz(Vector((0.0, 0.0, 1.0)).cross(forward))
    if kind == "climb":
        # Runner's side of the wall, 3/4 on the face they are climbing.
        return (-forward * 0.82 + left * 0.42).normalized()
    if kind == "wall":
        return (-left * 0.78 + forward * 0.40).normalized()
    if kind in ("roll", "slide"):
        return (left * 0.90 + -forward * 0.30).normalized()
    return (-forward * 0.72 + left * 0.55).normalized()


def glyph(rows, text, origin_x, origin_y, scale=3):
    height = len(rows)
    width = len(rows[0]) // 4
    x = origin_x
    for ch in text:
        rows_ch = FONT.get(ch, FONT[" "])
        cw = len(rows_ch[0])
        for sy, line in enumerate(rows_ch):
            for sx, bit in enumerate(line):
                if bit != "1":
                    continue
                for dy in range(scale):
                    for dx in range(scale):
                        px = x + sx * scale + dx
                        py = origin_y + sy * scale + dy
                        if px < 0 or py < 0 or px >= width or py >= height:
                            continue
                        i = px * 4
                        rows[py][i:i + 4] = bytes((250, 248, 240, 255))
        x += (cw + 1) * scale


def stamp_text(path, label):
    width, height, rows = p.png_rows(path)
    # Plate so the stamp reads on the sky and on the wall.
    for y in range(8, 36):
        row = rows[y]
        for x in range(8, 8 + 8 * 3 * (len(label) + 1)):
            if x >= width:
                break
            i = x * 4
            row[i] = 16
            row[i + 1] = 18
            row[i + 2] = 20
            row[i + 3] = 255
    glyph(rows, label, 14, 12, 3)
    p.write_png(path, rows)


def tile_sheet(paths, dest):
    cells = [p.png_rows(path) for path in paths]
    cw, ch, _ = cells[0]
    sheet_w = cw * 4
    sheet_h = ch * 2
    rows = [bytearray(sheet_w * 4) for _ in range(sheet_h)]
    for index, (_w, _h, cell) in enumerate(cells):
        ox = (index % 4) * cw
        oy = (index // 4) * ch
        for y in range(ch):
            src = cell[y]
            dst = rows[oy + y]
            dst[ox * 4:(ox + cw) * 4] = src
    p.write_png(dest, rows)
    print("WROTE", dest)


def fill_of(path):
    _count, span = p.blue_count(path)
    frac = span / float(p.RES_Y)
    print("FILL", os.path.basename(path), round(frac, 3), "span", span)
    return frac


def build_context(arm, kind, yaw):
    ctx = {"yaw": yaw}
    p.ensure_pose(arm)
    if kind == "roll":
        land, _z = p.grab(arm, p.pose_land, yaw, "none", 0.0)
        foot = min(lowest("Mesh_Foot_L"), lowest("Mesh_Foot_R"))
        land_z = arm.location.z + (0.012 - foot)
        peak, _z = p.grab(arm, pose_roll_over, yaw, "none", 0.0)
        chest = lowest("Mesh_Chest")
        peak_z = arm.location.z + (0.012 - chest)
        run, _z = p.grab(arm, p.pose_stride, yaw, "none", 0.0)
        foot = min(lowest("Mesh_Foot_L"), lowest("Mesh_Foot_R"))
        run_z = arm.location.z + (0.012 - foot)
        ctx.update(land=land, peak=peak, run=run, land_z=land_z, peak_z=peak_z, run_z=run_z)
        apply_key(arm, peak, yaw, 0.0)
        ctx["forward"] = p.body_forward(arm)
        ctx["left"] = p.body_left(arm)
        print(
            "ROLLPEAK",
            "spine", round(pitch_of(arm, "Spine"), 1),
            "chest", round(pitch_of(arm, "Chest"), 1),
            "hips", round(pitch_of(arm, "Hips"), 1),
        )
        return ctx
    if kind == "climb":
        p.apply_pose(arm, pose_climb_clear, 0.0, yaw)
        p.settle(arm, "climb")
        hero = p.capture_pose(arm)
        hero_lift = arm.location.z
        ctx["pelvis1"] = pelvis_z(arm)
        p.clear_props()
        info = p.build_climb(arm)
        ctx["top"] = min(v.z for v in p.mesh_world("Mesh_Hand_L") + p.mesh_world("Mesh_Hand_R"))
        ctx["forward"] = p.body_forward(arm)
        ctx["left"] = p.body_left(arm)
        chest = max(p.mesh_world("Mesh_Chest"), key=lambda v: v.dot(ctx["forward"]))
        ctx["plane"] = chest.copy()
        ctx["hero"] = hero
        ctx["hero_lift"] = hero_lift
        low, _z = p.grab(arm, pose_climb_low, yaw, "none", 0.0)
        ctx["low"] = low
        apply_key(arm, low, yaw, 0.0)
        foot_name = "Mesh_Foot_L" if lowest("Mesh_Foot_L") <= lowest("Mesh_Foot_R") else "Mesh_Foot_R"
        seat(arm, foot_name, 0.012)
        ctx["pelvis0"] = pelvis_z(arm)
        stand, _z = p.grab(arm, pose_stand, yaw, "none", 0.0)
        ctx["stand"] = stand
        apply_key(arm, stand, yaw, 0.0)
        seat(arm, "Mesh_Foot_L", 0.012)
        hip = pelvis_z(arm)
        ctx["pelvis_stand"] = ctx["top"] + hip
        ctx["stand_along"] = 0.28
        print(
            "CLIMBSETUP",
            "pelvis0", round(ctx["pelvis0"], 3),
            "pelvis1", round(ctx["pelvis1"], 3),
            "stand", round(ctx["pelvis_stand"], 3),
            "top", round(ctx["top"], 3),
        )
        return ctx
    if kind == "vault":
        p.apply_pose(arm, p.pose_vault, 0.0, yaw)
        hero = p.capture_pose(arm)
        hero_lift = 0.0
        p.clear_props()
        info = p.build_vault(arm)
        ctx["top"] = max(item["b"].z for item in info["gaps"] if item["name"].endswith("boxTop"))
        ctx["forward"] = p.body_forward(arm)
        ctx["left"] = p.body_left(arm)
        ctx["hero"] = hero
        ctx["hero_lift"] = hero_lift
        reach, _z = p.grab(arm, pose_vault_reach, yaw, "none", 0.0)
        land, _z = p.grab(arm, pose_vault_land, yaw, "none", 0.0)
        ctx["reach"] = reach
        ctx["land"] = land
        ctx["reach_along"] = -0.72
        ctx["land_along"] = 0.78
        return ctx
    if kind == "slide":
        p.apply_pose(arm, p.pose_slide, 0.0, yaw)
        p.settle(arm, "slide")
        slide = p.capture_pose(arm)
        p.clear_props()
        p.build_slide(arm)
        ctx["forward"] = p.body_forward(arm)
        ctx["left"] = p.body_left(arm)
        ctx["slide"] = slide
        run, _z = p.grab(arm, p.pose_stride, yaw, "none", 0.0)
        ctx["run"] = run
        ctx["in_along"] = -0.85
        ctx["out_along"] = 0.85
        return ctx
    # wall
    p.fit_wall_plant(arm)
    p.apply_pose(arm, p.pose_wall_run, 0.0, yaw)
    run = p.capture_pose(arm)
    run_lift = arm.location.z
    p.clear_props()
    info = p.build_wall_run(arm)
    ctx["left"] = info["left"]
    ctx["plane"] = info["plane"]
    ctx["forward"] = p.body_forward(arm)
    ctx["run"] = run
    ctx["run_lift"] = run_lift
    push, _z = p.grab(arm, p.pose_wall_push, yaw, "none", 0.0)
    for held in (
        "UpperArm_L", "LowerArm_L", "Hand_L",
        "UpperLeg_L", "LowerLeg_L", "Foot_L",
    ):
        push[held] = run[held]
    ctx["push"] = push
    ctx["plant_bones"] = (
        "Hips", "Spine", "Chest", "Head",
        "UpperArm_L", "LowerArm_L", "Hand_L",
        "UpperLeg_L", "LowerLeg_L", "Foot_L",
    )
    ctx["trail"] = {
        name: run[name]
        for name in ("UpperLeg_R", "LowerLeg_R", "Foot_R", "UpperArm_R", "LowerArm_R", "Hand_R")
        if name in run
    }
    air, _z = p.grab(arm, p.pose_apex, yaw, "none", 0.0)
    ctx["air"] = air
    return ctx


def report_samples(arm, kind, show, ctx):
    print("SAMPLES", kind, "seconds", DUR[kind])
    peak_spine = -999.0
    for index, age in enumerate(still_ages(DUR[kind])):
        show(arm, age, ctx)
        hip = pelvis_z(arm)
        spine = pitch_of(arm, "Spine")
        hips = pitch_of(arm, "Hips")
        peak_spine = max(peak_spine, spine)
        extra = ""
        if kind == "climb":
            top = ctx["top"]
            forward = ctx["forward"]
            plane = ctx["plane"]
            hand = lowest("Mesh_Hand_L")
            chest = max(p.mesh_world("Mesh_Chest"), key=lambda v: v.dot(forward))
            foot = max(p.mesh_world("Mesh_Foot_L"), key=lambda v: v.dot(forward))
            extra = " hand_cm " + str(round((hand - top) * 100.0, 1)) + " chest_cm " + str(round((chest - plane).dot(forward) * 100.0, 1)) + " foot_cm " + str(round((foot - plane).dot(forward) * 100.0, 1))
        if kind == "roll":
            extra = " chest_cm " + str(round(lowest("Mesh_Chest") * 100.0, 1))
        print(
            "SAMPLE", kind, index,
            "t", round(age, 3),
            "pelvis_cm", round(hip * 100.0, 1),
            "spine", round(spine, 1),
            "hips", round(hips, 1),
            extra,
        )
    if kind == "roll":
        # Peak across the 30 fps clock, not only the 8 stills.
        best = -999.0
        best_t = 0.0
        for age in ages_30(DUR["roll"]):
            show(arm, age, ctx)
            spine = pitch_of(arm, "Spine")
            if spine > best:
                best = spine
                best_t = age
        print("PEAKSPINE", round(best, 1), "t", round(best_t, 3))


def render_clip(arm, scene, cam, shadow, kind, show, ctx):
    side = side_for(kind, ctx)
    paths = []
    for index, age in enumerate(still_ages(DUR[kind])):
        show(arm, age, ctx)
        frame_camera(cam, arm, side)
        p.place_shadow(shadow, arm, kind if kind != "roll" else "none", None)
        scene.eevee.taa_render_samples = SAMPLES
        path = os.path.join(OUT, kind + "-" + str(index) + ".png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        stamp_text(path, "f" + str(index) + "  " + "{:.2f}".format(age) + "s")
        fill_of(path)
        print("WROTE", path)
        paths.append(path)
    tile_sheet(paths, os.path.join(OUT, kind + "-sheet.png"))


def main():
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=p.FBX)
    arm = bpy.data.objects["DummyArmature"]
    p.ensure_pose(arm)
    p.tint()
    scene, cam, shadow = p.scene_setup(arm)
    p.ensure_pose(arm)
    p.beauty_camera(cam)
    env_yaw = os.environ.get("PASS18_YAW", "")
    if env_yaw:
        climb_yaw = float(env_yaw)
        print("CLIMB_YAW", round(climb_yaw, 1), "facing", "env")
    else:
        climb_yaw, facing = p.choose_climb_yaw(arm, cam, scene)
        print("CLIMB_YAW", round(climb_yaw, 1), "facing", round(facing, 2))
    p.clear_props()
    p.ensure_pose(arm)
    if not ONLY or "wall" in ONLY:
        wall_yaw, _wf = p.choose_wall_yaw(arm, cam, scene)
    else:
        wall_yaw = 140.0
    p.clear_props()
    yaws = {
        "vault": p.VAULT_YAW,
        "climb": climb_yaw,
        "slide": p.SLIDE_YAW,
        "wall": wall_yaw,
        "roll": p.SLIDE_YAW,
    }
    ready = {}
    for kind in ORDER:
        if ONLY and kind not in ONLY:
            continue
        p.clear_props()
        p.ensure_pose(arm)
        ctx = build_context(arm, kind, yaws[kind])
        ready[kind] = ctx
        measure_clip(arm, kind, SHOWS[kind], ctx)
        report_samples(arm, kind, SHOWS[kind], ctx)
    if not DO_RENDER:
        return
    for kind, ctx in ready.items():
        p.clear_props()
        p.ensure_pose(arm)
        # Props were cleared. Rebuild by running the setup again.
        ctx = build_context(arm, kind, yaws[kind])
        render_clip(arm, scene, cam, shadow, kind, SHOWS[kind], ctx)


if __name__ == "__main__":
    main()
