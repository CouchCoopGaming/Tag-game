"""Hip-sit, plant, and no-clip numbers for the evasion clips.

pelvisBack is how far the hips head sits behind the support sole, along
the facing direction. Positive means the pelvis is behind the foot.
hinge is hip pitch over lumbar plus chest pitch. Chest is not keyed, so
lumbar is the spine pitch. A near-zero spine with a flexed hip passes.
Drop is not used as a horizontal offset.

  blender --background --python Tools/Tag/measure_evasion_hipsit.py
  POSE_KEYS=Docs/EvasionStills/pass5/pose_keys.tsv blender --background --python Tools/Tag/measure_evasion_hipsit.py
"""
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
import ingame_noclip as g
import noclip_check as n
import render_evasion_pass4 as p4

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
KEYS = os.environ.get(
    "POSE_KEYS",
    os.path.join(ROOT, "Docs", "EvasionStills", "pass4", "pose_keys.tsv"),
)
SKIP_NOCLIP = os.environ.get("SKIP_NOCLIP", "") == "1"
ENTRY = 0.10
MOVE = {
    "stutter": 0.38,
    "spinL": 0.35,
    "spinR": 0.35,
    "jukeL": 0.22,
    "jukeR": 0.22,
    "dive": 0.217391 + 0.60,
}


def sole_mid(arm, side):
    pb = arm.pose.bones["Foot_" + side]
    nrm = ((arm.matrix_world @ pb.matrix).to_3x3() @ Vector((0.0, 0.0, 1.0))).normalized()
    obj = bpy.data.objects.get("Mesh_Foot_" + side)
    verts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    scored = sorted(verts, key=lambda v: nrm.dot(v), reverse=True)
    patch = scored[: max(16, len(scored) // 8)]
    acc = Vector((0.0, 0.0, 0.0))
    for v in patch:
        acc += v
    return acc / len(patch)


FACE_AXIS = Vector((0.0, -1.0, 0.0))
STAND_Z = 1.0


def capture_stand(arm):
    """Rest hips height, and which local hips axis points along the face."""
    global FACE_AXIS, STAND_Z
    g.apply_frame(arm, {
        "thL": 0, "thR": 0, "knL": 0, "knR": 0, "yawL": 0, "yawR": 0,
        "armL": 0, "armR": 0, "armYawL": 0, "armYawR": 0,
        "rollL": 0, "rollR": 0, "elbL": 0, "elbR": 0,
        "hip": 0, "spine": 0, "head": 0, "lean": 0, "hipYaw": 0, "spineYaw": 0,
        "footL": 0, "footR": 0, "drop": 0, "elbYawL": 0, "elbYawR": 0, "headYaw": 0,
    })
    hips = arm.pose.bones["Hips"]
    STAND_Z = (arm.matrix_world @ hips.head).z
    basis = (arm.matrix_world @ hips.matrix).to_3x3()
    best = None
    want = Vector((0.0, -1.0, 0.0))
    for axis in (Vector((1, 0, 0)), Vector((-1, 0, 0)), Vector((0, 1, 0)), Vector((0, -1, 0)), Vector((0, 0, 1)), Vector((0, 0, -1))):
        world = basis @ axis
        score = world.dot(want)
        if best is None or score > best[0]:
            best = (score, axis.copy())
    FACE_AXIS = best[1]
    print("STAND_Z", round(STAND_Z, 3), "FACE", tuple(round(v, 2) for v in FACE_AXIS), flush=True)


def facing_now(arm):
    hips = arm.pose.bones["Hips"]
    world = (arm.matrix_world @ hips.matrix).to_3x3() @ FACE_AXIS
    flat = Vector((world.x, world.y, 0.0))
    if flat.length < 1.0e-6:
        return Vector((0.0, -1.0, 0.0))
    flat.normalize()
    return flat


def pelvis_back(arm, frame, side):
    del frame
    hips = arm.matrix_world @ arm.pose.bones["Hips"].head
    foot = sole_mid(arm, side)
    delta = Vector((foot.x - hips.x, foot.y - hips.y, 0.0))
    return delta.dot(facing_now(arm)) * 100.0


def support_leg(arm, frame, side):
    """Knee bend, shin direction, and how far the pelvis has dropped."""
    knee_flex = abs(frame["knL"] if side == "L" else frame["knR"])
    forward = facing_now(arm)
    hips = arm.matrix_world @ arm.pose.bones["Hips"].head
    knee = arm.matrix_world @ arm.pose.bones["LowerLeg_" + side].head
    ankle = arm.matrix_world @ arm.pose.bones["Foot_" + side].head
    knee_ahead = Vector((knee.x - ankle.x, knee.y - ankle.y, 0.0)).dot(forward) * 100.0
    knee_of_hip = Vector((knee.x - hips.x, knee.y - hips.y, 0.0)).dot(forward) * 100.0
    pelvis_drop = (STAND_Z - hips.z) * 100.0
    return knee_flex, knee_ahead, knee_of_hip, pelvis_drop


def hinge_of(frame):
    hip = abs(frame["hip"])
    denom = abs(frame["spine"]) + 0.0
    if denom < 1.0:
        return None if hip >= 8.0 else 0.0
    return hip / denom


def obligation(clip, t):
    move = MOVE[clip]
    in_move = ENTRY - 1.0e-4 <= t <= ENTRY + move + 1.0e-4
    if clip != "dive":
        return in_move
    local = t - ENTRY
    # The push-off is the planted frame. After that the body is airborne and stretched,
    # so those frames are not plants.
    takeoff = 0.0 <= local <= 0.02
    rollup = 0.76 <= local <= 0.817
    return takeoff or rollup


def main():
    frames = g.load_keys(KEYS)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=g.FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    g.add_ground()
    g.prepare(arm)
    capture_stand(arm)

    clips = []
    loaded = 0
    back_min = None
    hinge_min = None
    knee_min = None
    drop_min = None
    fails = []
    cruise = []
    rig_max = 0.0
    pose_max = 0.0
    world_max = 0.0
    pose_where = None
    plants = {}
    air = {}

    for frame in frames:
        clip = frame["clip"]
        if clip not in clips:
            clips.append(clip)
            plants[clip] = []
            air[clip] = []
        p4.apply_posed(arm, frame)
        loaded += 1
        t = frame["t"]
        gl = p4.sole_gap(arm, "L")
        gr = p4.sole_gap(arm, "R")
        side, gap = p4.support(clip, gl, gr)
        back = pelvis_back(arm, frame, side)
        hinge = hinge_of(frame)
        owed = obligation(clip, t)
        move = MOVE[clip]
        in_move = ENTRY - 1.0e-4 <= t <= ENTRY + move + 1.0e-4
        if clip != "dive" and in_move:
            corner = p4.foot_min(side)
            if -0.0008 <= gap <= 0.005:
                plants[clip].append((t, side, gap, corner, back))
            else:
                air[clip].append((t, side, gap, gl, gr, corner))
        if clip == "dive":
            who, low = p4.lowest_name()
            hand = min(p4.hand_z("L"), p4.hand_z("R"))
            fl = arm.matrix_world @ arm.pose.bones["Foot_L"].head
            fr = arm.matrix_world @ arm.pose.bones["Foot_R"].head
            hips = arm.matrix_world @ arm.pose.bones["Hips"].head
            foot_up = max(fl.z, fr.z) - hips.z
            if t >= 0.30:
                print(
                    "DIVE", "%.3f" % t, "low", who, round(low * 100, 1),
                    "hand", round(hand * 100, 1),
                    "footAboveHip", round(foot_up * 100, 1),
                    "back", round(back, 1),
                    "hip", round(frame["hip"], 1),
                    "spine", round(frame["spine"], 1),
                    flush=True,
                )
        if owed:
            crouch = clip == "dive" and (t - ENTRY) >= 0.76
            need = 12.0 if crouch else 8.0
            knee_need = 45.0 if crouch else 25.0
            drop_need = 20.0 if crouch else 8.0
            flex, ahead, of_hip, pdrop = support_leg(arm, frame, side)
            hinge_fail = hinge is not None and hinge < 1.5
            back_fail = back < need
            knee_fail = flex < knee_need
            shin_fail = ahead < 0.0 or of_hip < 0.0
            drop_fail = pdrop < drop_need
            planted_fail = not (-0.0008 <= gap <= 0.005)
            if back_min is None or back < back_min:
                back_min = back
            if hinge is not None and (hinge_min is None or hinge < hinge_min):
                hinge_min = hinge
            if knee_min is None or flex < knee_min:
                knee_min = flex
            if drop_min is None or pdrop < drop_min:
                drop_min = pdrop
            if hinge_fail or back_fail or knee_fail or shin_fail or drop_fail or planted_fail:
                fails.append((clip, t, side, back, hinge, need, flex, ahead, of_hip, pdrop, gap))
        elif in_move:
            cruise.append((clip, t, back, hinge))

        if not SKIP_NOCLIP:
            for hit in n.scan_frame(arm):
                if hit["kind"] == "world":
                    if hit["depth"] > world_max:
                        world_max = hit["depth"]
                    if hit["depth"] > pose_max:
                        pose_max = hit["depth"]
                        pose_where = (frame["clip"], frame["t"], "world", hit["a"], hit["b"])
                    if hit["depth"] > n.LIMIT_M:
                        print("OVER", frame["clip"], "%.3f" % frame["t"], "world", hit["a"], round(hit["depth"] * 100, 2), flush=True)
                    continue
                if hit.get("joined"):
                    if hit["depth"] > rig_max:
                        rig_max = hit["depth"]
                    continue
                if hit["depth"] > pose_max:
                    pose_max = hit["depth"]
                    pose_where = (frame["clip"], frame["t"], hit["a"], hit["b"], hit["kind"])
                if hit["depth"] > n.LIMIT_M:
                    print("OVER", frame["clip"], "%.3f" % frame["t"], hit["a"], hit["b"], round(hit["depth"] * 100, 2), flush=True)

    print("FAILS", len(fails), flush=True)
    for clip, t, side, back, hinge, need, flex, ahead, of_hip, pdrop, gap in fails:
        htxt = "neutral" if hinge is None else "%.2f" % hinge
        print(
            "FAIL", clip, "%.3f" % t, side,
            "back", round(back, 1), "need", need, "hinge", htxt,
            "knee", round(flex, 1), "shin", round(ahead, 1), "kneeOfHip", round(of_hip, 1),
            "drop", round(pdrop, 1), "gap", round(gap * 100.0, 2),
            flush=True,
        )
    print("CRUISE", len(cruise), flush=True)
    for clip, t, back, hinge in cruise[:8]:
        htxt = "neutral" if hinge is None else "%.2f" % hinge
        print("CRUISE", clip, "%.3f" % t, "back", round(back, 1), "hinge", htxt, flush=True)

    for clip in clips:
        if clip == "dive":
            continue
        rows = plants[clip]
        gaps = [item[2] for item in rows]
        move_n = len(rows) + len(air[clip])
        bits = ["%.3f:%s:%.2f" % (t, side, gap * 100.0) for t, side, gap, _, _ in rows]
        print(
            "ground-contact %s planted=%d/%d maxGap=%.2f frames=%s"
            % (clip, len(rows), move_n, (max(gaps) * 100.0 if gaps else -1.0), ",".join(bits)),
            flush=True,
        )
        if air[clip]:
            air_bits = ["%.3f:%s:%.1f" % (t, side, gap * 100.0) for t, side, gap, _, _, _ in air[clip]]
            print("  airborne", clip, " ".join(air_bits), flush=True)
        if rows:
            print("  corner", clip, "minZ", round(min(item[3] for item in rows) * 100.0, 2), flush=True)

    hinge_txt = "neutral" if hinge_min is None else "%.2f" % hinge_min
    back_txt = "0" if back_min is None else "%.2f" % back_min
    knee_txt = "0" if knee_min is None else "%.1f" % knee_min
    drop_txt = "0" if drop_min is None else "%.1f" % drop_min
    print(
        "hip-sit clips=%d loadedFrames=%d pelvisBackMin=%s cm hingeMin=%s kneeMin=%s pelvisDropMin=%s cm fails=%d"
        % (len(clips), loaded, back_txt, hinge_txt, knee_txt, drop_txt, len(fails)),
        flush=True,
    )
    if not SKIP_NOCLIP:
        if pose_where is not None:
            print("POSEWHERE", pose_where, flush=True)
        pose_report = 0.0 if pose_max <= n.LIMIT_M else n.cm(pose_max)
        print(
            "no-clip clips=%d frames=%d worldMax=%s rigJoint=%s pose=%s fails=%d"
            % (len(clips), loaded, n.cm(world_max), n.cm(rig_max), pose_report, 1 if pose_max > n.LIMIT_M else 0),
            flush=True,
        )
    print("EXIT", flush=True)


if __name__ == "__main__":
    main()
