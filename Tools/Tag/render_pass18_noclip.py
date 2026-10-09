"""Pass 18 clip-wide interpenetration check.

Samples every 30 fps frame of each clip and writes
`no-clip clips=N frames=N worldMax=<cm> selfMax=<cm> fails=0`.
Set NOCLIP_SHOT=before or after to render the worst failing frame
with the overlap faces in red. Gameplay timers are not written.
"""
import importlib.util
import math
import os

import bpy
from mathutils import Euler, Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
spec = importlib.util.spec_from_file_location(
    "pass18", os.path.join(ROOT, "Tools", "Tag", "render_pass18_hier.py")
)
r = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r)
p = r.p
spec_n = importlib.util.spec_from_file_location(
    "noclip", os.path.join(ROOT, "Tools", "Tag", "noclip_check.py")
)
n = importlib.util.module_from_spec(spec_n)
spec_n.loader.exec_module(n)

os.environ["NOCLIP_SETTLE"] = "runner"
OUT = os.path.join(ROOT, "Docs", "SmoothStills", "pass18", "noclip")
SHOT = os.environ.get("NOCLIP_SHOT", "")
ONLY = set(filter(None, os.environ.get("NOCLIP_ONLY", "").split(",")))
SAMPLES = int(os.environ.get("NOCLIP_SAMPLES", "8"))

# Visual clocks. These match the published windows. They are not written back.
PUNCH_WIND = 0.12
PUNCH_ACTIVE = 0.10
PUNCH_RECOVER = 0.15
TAG_LIFE = 0.45
STAGGER = 0.25
ZIP_GRAB = 0.28
ZIP_DROP = 0.36
PAD_OPEN = 0.34
GRAPPLE_IN = 0.18
GRAPPLE_OUT = 0.24
IDLE_SHIFT = 0.72
IDLE_BREATH = 1.55


def smoothstep(u):
    u = 0.0 if u < 0.0 else (1.0 if u > 1.0 else u)
    return u * u * (3.0 - 2.0 * u)


def commit(u):
    u = 0.0 if u < 0.0 else (1.0 if u > 1.0 else u)
    return 1.0 - (1.0 - u) * (1.0 - u)


def lerp(a, b, w):
    return a + (b - a) * w


def pose_weight(speed):
    t = (speed - 0.35) / (12.0 - 0.35)
    t = 0.0 if t < 0.0 else (1.0 if t > 1.0 else t)
    return 1.0 - (1.0 - t) * (1.0 - t)


def sample_leg(phase, weight):
    s = math.sin(phase)
    c = math.cos(phase)
    front = lerp(26.0, 48.0, weight)
    back = lerp(14.0, 30.0, weight)
    thigh = s * front if s >= 0.0 else s * back
    if c <= 0.0:
        knee = -5.0
        foot = -(thigh + knee)
    else:
        knee = -(4.0 + c * lerp(48.0, 90.0, weight))
        foot = 0.0
    return thigh, knee, foot


def gait_at(phase, speed):
    weight = pose_weight(speed)
    tau = math.tau
    phase = phase % tau
    left = sample_leg(phase, weight)
    right = sample_leg((phase + math.pi) % tau, weight)
    return weight, left, right


def cadence_at(speed):
    weight = pose_weight(speed)
    front = lerp(26.0, 48.0, weight) * math.pi / 180.0
    back = lerp(14.0, 30.0, weight) * math.pi / 180.0
    travel = 0.90 * (math.sin(front) + math.sin(back))
    if travel < 0.08:
        travel = 0.08
    match = speed * math.pi / travel
    if match > 26.5:
        match = 26.5
    span = 4.0
    u = (speed - 0.35) / span
    u = 0.0 if u < 0.0 else (1.0 if u > 1.0 else u)
    gate = smoothstep(u)
    return match * gate


def run_arm_pitch(phase, amp):
    fwd = max(0.0, phase) * amp
    back = max(0.0, -phase) * amp * 0.22
    return -(fwd - back)


def seat_soles(arm, z=0.012):
    sole = min(r.lowest("Mesh_Foot_L"), r.lowest("Mesh_Foot_R"))
    if sole < z:
        arm.location.z += z - sole
        bpy.context.view_layer.update()


def pose_punch(arm, cock):
    if cock:
        p.arm_pose(arm, "R", -80.0, -16.0, -110.0, 0.0, -16.0)
        p.arm_pose(arm, "L", -26.0, -12.0, -70.0, 0.0, 10.0)
        p.set_euler(arm, "Hips", 10.0, -44.0, 0.0)
        p.set_euler(arm, "Spine", 6.0, -58.0, 0.0)
        p.set_euler(arm, "Head", -6.0, -22.0, 0.0)
        p.leg(arm, "L", 14.0, -18.0)
        p.leg(arm, "R", -12.0, -12.0)
    else:
        p.arm_pose(arm, "R", -74.0, 4.0, -4.0, 0.0, -10.0)
        p.arm_pose(arm, "L", 84.0, -18.0, -36.0, 0.0, 10.0)
        p.set_euler(arm, "Hips", 10.0, 36.0, 0.0)
        p.set_euler(arm, "Spine", 6.0, 52.0, 0.0)
        p.set_euler(arm, "Head", -6.0, 18.0, 0.0)
        p.leg(arm, "L", 20.0, -8.0)
        p.leg(arm, "R", -18.0, -6.0)


def pose_tag_gather(arm):
    p.arm_pose(arm, "L", -36.0, 14.0, -96.0, 0.0, 6.0)
    p.arm_pose(arm, "R", -36.0, 14.0, -96.0, 0.0, 6.0)
    p.torso(arm, 2.0, 4.0, -4.0)
    p.leg(arm, "L", 8.0, -18.0)
    p.leg(arm, "R", 8.0, -18.0)


def pose_tag_claim(arm):
    p.arm_pose(arm, "L", -60.0, 22.0, -8.0, 0.0, 4.0)
    p.arm_pose(arm, "R", -60.0, 22.0, -8.0, 0.0, 4.0)
    p.torso(arm, 6.0, 16.0, 8.0)
    p.leg(arm, "L", 16.0, -42.0)
    p.leg(arm, "R", 16.0, -42.0)


def pose_stagger(arm):
    p.set_euler(arm, "UpperArm_L", 46.0, 28.0, 14.0)
    p.set_euler(arm, "LowerArm_L", -30.0, 0.0, 0.0)
    p.set_euler(arm, "UpperArm_R", 34.0, -26.0, -12.0)
    p.set_euler(arm, "LowerArm_R", -24.0, 0.0, 0.0)
    p.set_euler(arm, "Hips", -18.0, 8.0, 0.0)
    p.set_euler(arm, "Spine", -16.0, -6.0, 0.0)
    p.set_euler(arm, "Head", -24.0, 4.0, 0.0)
    p.leg(arm, "L", 38.0, -34.0)
    p.leg(arm, "R", -16.0, -8.0)


def pose_idle_at(arm, shift, breath):
    p.set_euler(arm, "Hips", 0.0, 0.0, shift * 3.2)
    p.set_euler(arm, "Chest", breath * 1.6, 0.0, -shift * 1.4)
    p.set_euler(arm, "Spine", breath * 1.6, 0.0, 0.0)
    p.set_euler(arm, "Head", breath * -0.7, 0.0, 0.0)
    load_l = shift if shift > 0.0 else 0.0
    load_r = -shift if shift < 0.0 else 0.0
    thigh_l = load_l * 2.2
    thigh_r = load_r * 2.2
    knee_l = -(4.0 + load_l * 5.5)
    knee_r = -(4.0 + load_r * 5.5)
    p.leg(arm, "L", thigh_l, knee_l)
    p.leg(arm, "R", thigh_r, knee_r)
    p.set_euler(arm, "Foot_L", -(thigh_l + knee_l), 0.0, 0.0)
    p.set_euler(arm, "Foot_R", -(thigh_r + knee_r), 0.0, 0.0)
    p.arm_pose(arm, "L", -12.0 + breath * 2.6, 12.0, -10.0, 0.0)
    p.arm_pose(arm, "R", -12.0 + breath * 2.6, 12.0, -10.0, 0.0)


def pose_gait(arm, phase, speed):
    weight, left, right = gait_at(phase, speed)
    p.leg(arm, "L", left[0], left[1])
    p.leg(arm, "R", right[0], right[1])
    p.set_euler(arm, "Foot_L", left[2], 0.0, 0.0)
    p.set_euler(arm, "Foot_R", right[2], 0.0, 0.0)
    amp = lerp(36.0, 64.0, weight)
    # Arms oppose the thigh. sin(phase) is the left thigh sign.
    s = math.sin(phase)
    p.arm_pose(arm, "L", run_arm_pitch(-s, amp), 10.0, lerp(-10.0, -30.0, max(0.0, s) * weight), 0.0)
    p.arm_pose(arm, "R", run_arm_pitch(s, amp), 10.0, lerp(-10.0, -30.0, max(0.0, -s) * weight), 0.0)
    p.torso(arm, 4.0 * weight, 6.0 * weight, 1.0)


def show_punch(arm, age, ctx):
    if age <= PUNCH_WIND:
        w = age / PUNCH_WIND
        pose = r.blend(ctx["run"], ctx["cock"], w)
    elif age <= PUNCH_WIND + PUNCH_ACTIVE:
        w = commit((age - PUNCH_WIND) / PUNCH_ACTIVE)
        pose = r.blend(ctx["cock"], ctx["strike"], w)
    else:
        w = 1.0 - smoothstep((age - PUNCH_WIND - PUNCH_ACTIVE) / PUNCH_RECOVER)
        pose = r.blend(ctx["run"], ctx["strike"], w)
    r.apply_key(arm, pose, ctx["yaw"], 0.0)
    seat_soles(arm)


def show_tag(arm, age, ctx):
    u = age / TAG_LIFE
    if u <= 0.22:
        pose = ctx["gather"]
    elif u <= 0.58:
        w = commit((u - 0.22) / (0.58 - 0.22))
        pose = r.blend(ctx["gather"], ctx["claim"], w)
    else:
        w = 1.0 - smoothstep((u - 0.58) / (1.0 - 0.58))
        pose = r.blend(ctx["run"], ctx["claim"], w)
    r.apply_key(arm, pose, ctx["yaw"], 0.0)
    seat_soles(arm)


def stagger_weight(age):
    if age <= 0.0 or age >= STAGGER:
        return 0.0
    if age < 0.05:
        return smoothstep(age / 0.05)
    if age < 0.16:
        return 1.0
    return 1.0 - smoothstep((age - 0.16) / (STAGGER - 0.16))


def show_stagger(arm, age, ctx):
    pose = r.blend(ctx["run"], ctx["hit"], stagger_weight(age))
    r.apply_key(arm, pose, ctx["yaw"], 0.0)
    seat_soles(arm)


def show_idle(arm, age, ctx):
    shift = math.sin(IDLE_SHIFT * age)
    breath = math.sin(IDLE_BREATH * age)
    p.clear_pose(arm)
    pose_idle_at(arm, shift, breath)
    arm.rotation_euler = Euler((0.0, 0.0, p.rad(ctx["yaw"])), "XYZ")
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    seat_soles(arm)


def show_loco(arm, age, ctx):
    phase = ctx["rate"] * age
    p.clear_pose(arm)
    pose_gait(arm, phase, ctx["speed"])
    arm.rotation_euler = Euler((0.0, 0.0, p.rad(ctx["yaw"])), "XYZ")
    arm.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    seat_soles(arm)


def show_zip(arm, age, ctx):
    hold = 0.08
    if age <= ZIP_GRAB:
        w = p.raised(age / ZIP_GRAB)
        pose = r.blend(ctx["run"], ctx["hang"], w)
        lift = ctx["low_z"] + (ctx["hang_z"] - ctx["low_z"]) * w
    elif age <= ZIP_GRAB + hold:
        pose = ctx["hang"]
        lift = ctx["hang_z"]
        w = 1.0
    else:
        w = (age - ZIP_GRAB - hold) / ZIP_DROP
        pose = r.blend(ctx["hang"], ctx["run"], w)
        lift = ctx["hang_z"] + (ctx["low_z"] - ctx["hang_z"]) * w
    r.apply_key(arm, pose, ctx["yaw"], lift)
    if age > ZIP_GRAB + hold:
        r.shift(arm, ctx["back"] * (0.45 * (age - ZIP_GRAB - hold) / ZIP_DROP))


def show_grapple(arm, age, ctx):
    hold = 0.08
    if age <= GRAPPLE_IN:
        w = p.raised(age / GRAPPLE_IN)
        pose = r.blend(ctx["run"], ctx["hang"], w)
        along = ctx["reach"] * w
    elif age <= GRAPPLE_IN + hold:
        pose = ctx["hang"]
        along = ctx["reach"]
    else:
        w = (age - GRAPPLE_IN - hold) / GRAPPLE_OUT
        pose = r.blend(ctx["hang"], ctx["run"], w)
        along = ctx["reach"] * (1.0 - w)
    r.apply_key(arm, pose, ctx["yaw"], ctx["lift"])
    r.shift(arm, ctx["aim"] * along)
    seat_soles(arm)
    aim_rope(arm)


def aim_rope(arm):
    """Keep the rope outside the fist and the head. Visual only."""
    rope = bpy.data.objects.get("PropRope")
    if rope is None:
        return
    pts = p.mesh_world("Mesh_Hand_R")
    if not pts:
        return
    head = p.bone_head(arm, "Head")
    outer = max(pts, key=lambda point: (point - head).length)
    away = outer - head
    if away.length < 0.05:
        away = Vector((0.0, 0.0, 1.0))
    away = away.normalized()
    grip = outer + away * 0.20
    end = grip + away * 1.6
    rope.location = (grip + end) * 0.5
    rope.rotation_euler = (end - grip).to_track_quat("Z", "Y").to_euler()
    bpy.context.view_layer.update()


def show_pad(arm, age, ctx):
    hold = 0.10
    air = 0.30
    if age <= PAD_OPEN:
        w = p.raised(age / PAD_OPEN)
        pose = r.blend(ctx["run"], ctx["plant"], w)
        lift = ctx["high_z"] + (ctx["plant_z"] - ctx["high_z"]) * w
    elif age <= PAD_OPEN + hold:
        pose = ctx["plant"]
        lift = ctx["plant_z"]
    else:
        w = (age - PAD_OPEN - hold) / air
        pose = r.blend(ctx["plant"], ctx["air"], w)
        lift = ctx["plant_z"] + ctx["rise"] * w
    r.apply_key(arm, pose, ctx["yaw"], lift)


DUR = dict(r.DUR)
DUR.update({
    "pad": PAD_OPEN + 0.10 + 0.30,
    "zip": ZIP_GRAB + 0.08 + ZIP_DROP,
    "grapple": GRAPPLE_IN + 0.08 + GRAPPLE_OUT,
    "punch": PUNCH_WIND + PUNCH_ACTIVE + PUNCH_RECOVER,
    "tag": TAG_LIFE,
    "stagger": STAGGER,
    "idle": math.tau / IDLE_SHIFT,
    "loco": None,
    "sprint": None,
})


def build_extra(arm, kind, yaw):
    ctx = {"yaw": yaw}
    if kind == "punch":
        run, _z = p.grab(arm, p.pose_stride, yaw, "none", 0.0)
        cock, _z = p.grab(arm, lambda a: pose_punch(a, True), yaw, "none", 0.0)
        strike, _z = p.grab(arm, lambda a: pose_punch(a, False), yaw, "none", 0.0)
        ctx.update(run=run, cock=cock, strike=strike)
        return ctx
    if kind == "tag":
        run, _z = p.grab(arm, p.pose_stride, yaw, "none", 0.0)
        gather, _z = p.grab(arm, pose_tag_gather, yaw, "none", 0.0)
        claim, _z = p.grab(arm, pose_tag_claim, yaw, "none", 0.0)
        ctx.update(run=run, gather=gather, claim=claim)
        return ctx
    if kind == "stagger":
        run, _z = p.grab(arm, p.pose_stride, yaw, "none", 0.0)
        hit, _z = p.grab(arm, pose_stagger, yaw, "none", 0.0)
        ctx.update(run=run, hit=hit)
        return ctx
    if kind == "idle":
        return ctx
    if kind in ("loco", "sprint"):
        speed = 6.0 if kind == "loco" else 12.0
        rate = cadence_at(speed)
        ctx["speed"] = speed
        ctx["rate"] = rate
        DUR[kind] = math.tau / rate
        return ctx
    if kind == "zip":
        hang, hang_z = p.grab(arm, p.pose_zip, yaw, "none", 0.0)
        p.clear_props()
        p.build_zip(arm)
        run, _z = p.grab(arm, p.pose_stride, yaw, "none", 0.0)
        ctx.update(hang=hang, hang_z=hang_z, run=run, low_z=hang_z - 0.35)
        ctx["back"] = -p.body_forward(arm)
        return ctx
    if kind == "grapple":
        hang, lift = p.grab(arm, p.pose_grapple, yaw, "none", 0.0)
        p.clear_props()
        # Rope starts on the outer hand surface so the cylinder is not born inside the fist.
        pts = p.mesh_world("Mesh_Hand_R")
        forearm = p.bone_head(arm, "Hand_R") - p.bone_head(arm, "LowerArm_R")
        if forearm.length < 0.05:
            forearm = Vector((0.0, 0.0, 1.0))
        forearm = forearm.normalized()
        outer = max(pts, key=lambda point: point.dot(forearm))
        radius = 0.012
        head = p.bone_head(arm, "Head")
        away = outer - head
        if away.length < 0.05:
            away = forearm
        away = away.normalized()
        # Start clear of the fist and aim away from the head so the rope is not born inside it.
        grip = outer + away * 0.18
        end = grip + away * 1.6
        p.add_cyl("PropRope", grip, end, radius, p.ROPE)
        run, _z = p.grab(arm, p.pose_stride, yaw, "none", 0.0)
        ctx.update(hang=hang, lift=lift, run=run, aim=forearm, reach=0.55)
        return ctx
    if kind == "pad":
        plant, plant_z = p.grab(arm, p.pose_pad, yaw, "pad", 0.0)
        p.clear_props()
        p.build_pad(arm)
        run, _z = p.grab(arm, p.pose_stride, yaw, "none", 0.0)
        air, _z = p.grab(arm, p.pose_apex, yaw, "none", 0.0)
        ctx.update(plant=plant, plant_z=plant_z, run=run, air=air, high_z=plant_z + 0.28, rise=0.55)
        return ctx
    return ctx


SHOWS = dict(r.SHOWS)
SHOWS.update({
    "punch": show_punch,
    "tag": show_tag,
    "stagger": show_stagger,
    "idle": show_idle,
    "loco": show_loco,
    "sprint": show_loco,
    "zip": show_zip,
    "grapple": show_grapple,
    "pad": show_pad,
})

ORDER = (
    "vault", "climb", "slide", "wall", "roll",
    "pad", "zip", "grapple",
    "punch", "tag", "stagger",
    "idle", "loco", "sprint",
)

# Frames that failed before the visual fix. After stills use the same time.
BEFORE_WORST = {
    "vault": 0.267,
    "climb": 0.0,
    "slide": 0.267,
    "wall": 0.0,
    "roll": 0.2,
    "pad": 0.167,
    "zip": 0.0,
    "grapple": 0.333,
    "punch": 0.133,
    "tag": 0.267,
    "stagger": 0.067,
    "idle": 2.533,
    "loco": 0.167,
    "sprint": 0.233,
}


def side_for(arm):
    forward = p.body_forward(arm)
    left = p.body_left(arm)
    return (left * 0.85 + -forward * 0.40).normalized()


def render_worst(arm, scene, cam, shadow, kind, age, index, hit):
    os.makedirs(OUT, exist_ok=True)
    depth = 0.0 if hit is None else hit["depth"]
    if hit is not None and depth > n.LIMIT_M:
        n.paint(hit)
    r.frame_camera(cam, arm, side_for(arm))
    p.place_shadow(shadow, arm, "none", None)
    scene.eevee.taa_render_samples = SAMPLES
    path = os.path.join(OUT, kind + "-" + SHOT + ".png")
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    label = kind + "  " + "{:.2f}".format(age) + "s  " + "{:.2f}".format(n.cm(depth)) + "cm"
    r.stamp_text(path, label)
    n.clear_hot()
    print("WROTE", path)


def scan_one(arm, scene, cam, shadow, kind, show, ctx, authored):
    times = r.ages_30(DUR[kind])
    worst = None
    worst_age = 0.0
    worst_index = 0
    worst_count = 0.0
    frame_fails = 0
    world_max = 0.0
    self_max = 0.0
    for index, age in enumerate(times):
        show(arm, age, ctx)
        n.settle_body(arm)
        hits = n.scan_frame(arm)
        bad = n.over_limit(hits, authored)
        if bad:
            frame_fails += 1
        for hit in hits:
            depth = n.counted(hit, authored)
            if hit["kind"] == "world" and depth > world_max:
                world_max = depth
            if hit["kind"] == "self" and depth > self_max:
                self_max = depth
            if depth > worst_count:
                worst = hit
                worst_count = depth
                worst_age = age
                worst_index = index
        if bad:
            top_bad = max(bad, key=lambda hit: n.counted(hit, authored))
            print(
                "NOCLIPFAIL", kind, index, "t", round(age, 3),
                top_bad["a"], top_bad["b"], top_bad["kind"],
                n.cm(n.counted(top_bad, authored)),
                "raw", n.cm(top_bad["depth"]),
                "pairs", len(bad),
            )
    if worst is None:
        print("NOCLIPWORST", kind, "-", "t", 0, "-", "-", "none", 0.0)
    else:
        print(
            "NOCLIPWORST", kind, worst_index, "t", round(worst_age, 3),
            worst["a"], worst["b"], worst["kind"],
            n.cm(worst_count), "raw", n.cm(worst["depth"]),
        )
        shot_age = BEFORE_WORST.get(kind, worst_age) if SHOT == "after" else worst_age
        if SHOT and (worst_count > n.LIMIT_M or SHOT == "after"):
            show(arm, shot_age, ctx)
            n.settle_body(arm)
            hits = n.scan_frame(arm)
            bad = n.over_limit(hits, authored)
            hit = max(bad, key=lambda item: n.counted(item, authored)) if bad else None
            render_worst(arm, scene, cam, shadow, kind, shot_age, worst_index, hit)
    return {
        "frames": len(times),
        "fails": frame_fails,
        "world": world_max,
        "self": self_max,
    }


def main():
    os.makedirs(r.OUT, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=p.FBX)
    arm = bpy.data.objects["DummyArmature"]
    p.ensure_pose(arm)
    p.clear_pose(arm)
    n.remember_base()
    # Rest-pose joined depth is recorded only so a later pass can see the
    # shells before relief. The 0.5 cm limit is applied to the relieved pose.
    n.authored_depth(arm)
    authored = {}
    p.tint()
    scene, cam, shadow = p.scene_setup(arm)
    p.ensure_pose(arm)
    p.beauty_camera(cam)
    # Same yaws as the pass 18 stills. The overlap test is in body space, so a
    # yaw search does not change the depths.
    yaws = {
        "vault": p.VAULT_YAW,
        "climb": 140.0,
        "slide": p.SLIDE_YAW,
        "wall": 140.0,
        "roll": p.SLIDE_YAW,
    }
    plain = p.SLIDE_YAW
    totals = {"clips": 0, "frames": 0, "fails": 0, "world": 0.0, "self": 0.0}
    for kind in ORDER:
        if ONLY and kind not in ONLY:
            continue
        p.clear_props()
        p.ensure_pose(arm)
        yaw = yaws.get(kind, plain)
        if kind in r.SHOWS:
            ctx = r.build_context(arm, kind, yaw)
        else:
            ctx = build_extra(arm, kind, yaw)
        print("CLIP", kind, "seconds", round(DUR[kind], 4))
        row = scan_one(arm, scene, cam, shadow, kind, SHOWS[kind], ctx, authored)
        totals["clips"] += 1
        totals["frames"] += row["frames"]
        totals["fails"] += row["fails"]
        totals["world"] = max(totals["world"], row["world"])
        totals["self"] = max(totals["self"], row["self"])
    print(
        "no-clip clips={clips} frames={frames} worldMax={world} selfMax={self} fails={fails}".format(
            clips=totals["clips"],
            frames=totals["frames"],
            world=n.cm(totals["world"]),
            self=n.cm(totals["self"]),
            fails=totals["fails"],
        )
    )


if __name__ == "__main__":
    main()
