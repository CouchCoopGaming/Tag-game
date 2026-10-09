"""Full hip-floor and no-clip table against the per-move floors (Oct 9 revision).

  POSE_KEYS=a.tsv[:b.tsv] OUT_TSV=table.tsv blender --background --python Tools/Tag/scan_hip_floors.py

Keys come from StrafeJumpSim --pose-keys and --pose-keys-evasion (the game's own
static clip classes). Nothing is lifted, nothing is relieved: the mesh is posed and
measured as-is.

Loaded frame = the clip has a floor class below AND a support sole is within
0.5 cm of the ground. Airborne frames of a loaded clip and every frame of a
cruise clip are scored for no-clip only.

Floors (hip/spine are absolute keyed flexion, degrees):
  landing      hip>=30 spine>=10, pelvis>=12 cm behind the support foot (crouch)
  plant        hip>=25 (run/sprint/evasion cuts), pelvis>=8 cm behind
  hand         hip>=12; pelvis-behind waived only on frames where a hand carries
               load (played vault plant); recovery exits on the feet keep >=8 cm
  slide        hip>=30 spine>=10, pelvis>=12 cm behind
  roll         per reference, no crouch floors while tucked: not floor-scored here
Every loaded frame also needs hinge = hip/spine >= 1.5. Hip 6 over spine 4 fails
on the hip floor regardless of ratio.
No-clip: limit 0.5 cm. Joined parent/child (joint-cuff) overlap is rigJoint and
belongs to the rig lane (#128). pose = non-adjacent self + world, must be 0.
"""
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(__file__))
import ingame_noclip as g
import noclip_check as n
import render_evasion_pass4 as p4
import measure_evasion_hipsit as m

CLASS = {
    "exit-WallRun": "landing", "exit-Vault": "landing", "exit-LaunchLand": "landing",
    "exit-SoftLand": "landing", "exit-Stagger": "landing", "stagger": "landing",
    "stutter": "plant", "jukeL": "plant", "jukeR": "plant", "spinL": "plant", "spinR": "plant",
    "vault": "hand", "exit-Mantle": "hand", "exit-ClimbTopOut": "hand",
    "slide": "slide", "exit-Slide": "slide",
    "roll": "roll", "exit-Roll": "roll", "exit-RollAbsorb": "roll", "dive": "roll",
}
FLOOR = {  # hip, spine, pelvisBack
    "landing": (30.0, 10.0, 12.0),
    "plant": (25.0, 0.0, 8.0),
    "hand": (12.0, 0.0, 8.0),
    "slide": (30.0, 10.0, 12.0),
}
HAND_LOADED = {"vault"}  # played vault plant: hands on the obstacle
CONTACT = 0.005


def main():
    frames = []
    for path in os.environ["POSE_KEYS"].split(":"):
        frames += g.load_keys(path)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=g.FBX)
    arm = bpy.data.objects["DummyArmature"]
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.mode_set(mode="POSE")
    g.add_ground()
    g.prepare(arm)
    m.capture_stand(arm)

    rows = []
    stats = {}
    order = []
    for fr in frames:
        clip = fr["clip"]
        if clip not in stats:
            order.append(clip)
            stats[clip] = dict(frames=0, loaded=0, hipFails=0, why=set(), pose=0.0, poseWhere="",
                               world=0.0, rig=0.0, poseFails=0, hipMin=None, spineMin=None, backMin=None)
        st = stats[clip]
        st["frames"] += 1
        p4.apply_posed(arm, fr)
        gl = p4.sole_gap(arm, "L")
        gr = p4.sole_gap(arm, "R")
        side, gap = p4.support(clip, gl, gr)
        cls = CLASS.get(clip, "cruise")
        loaded = cls in FLOOR and -0.0008 <= gap <= CONTACT
        why = []
        hip = abs(fr["hip"])
        spine = abs(fr["spine"])
        back = m.pelvis_back(arm, fr, side)
        hinge = m.hinge_of(fr)
        if loaded:
            st["loaded"] += 1
            hn, sn, bn = FLOOR[cls]
            if hip < hn:
                why.append("hip%.0f<%.0f" % (hip, hn))
            if spine < sn:
                why.append("spine%.0f<%.0f" % (spine, sn))
            waived = cls == "hand" and clip in HAND_LOADED
            if not waived and back < bn:
                why.append("back%.1f<%.0f" % (back, bn))
            if hinge is not None and hinge < 1.5:
                why.append("hinge%.2f" % hinge)
            for key, val in (("hipMin", hip), ("spineMin", spine), ("backMin", back)):
                if st[key] is None or val < st[key]:
                    st[key] = val
            if why:
                st["hipFails"] += 1
                st["why"].update(w.rstrip("0123456789.<-") for w in why)
        fpose = 0.0
        fwhere = ""
        for hit in n.scan_frame(arm):
            d = hit["depth"]
            if hit["kind"] == "world":
                st["world"] = max(st["world"], d)
            elif hit.get("joined"):
                st["rig"] = max(st["rig"], d)
                continue
            if d > fpose:
                fpose = d
                fwhere = hit["a"] + "|" + hit["b"]
        if fpose > st["pose"]:
            st["pose"] = fpose
            st["poseWhere"] = fwhere
        if fpose > n.LIMIT_M:
            st["poseFails"] += 1
        rows.append((clip, fr["t"], cls, int(loaded), side, gap * 100, hip, spine, back,
                     -1 if hinge is None else hinge, ",".join(why), fpose * 100, fwhere))

    out = os.environ.get("OUT_TSV", "/tmp/hip_floor_rows.tsv")
    with open(out, "w") as h:
        h.write("clip\tt\tclass\tloaded\tside\tgapCm\thip\tspine\tbackCm\thinge\tfails\tposeCm\tposePair\n")
        for r in rows:
            h.write("%s\t%.3f\t%s\t%d\t%s\t%.2f\t%.1f\t%.1f\t%.1f\t%.2f\t%s\t%.2f\t%s\n" % r)
    T = dict(frames=0, loaded=0, hipFails=0, poseFails=0, pose=0.0, world=0.0, rig=0.0)
    for clip in order:
        st = stats[clip]
        for k in ("frames", "loaded", "hipFails", "poseFails"):
            T[k] += st[k]
        for k in ("pose", "world", "rig"):
            T[k] = max(T[k], st[k])
        f = lambda v: "-" if v is None else "%.1f" % v
        print("ROW|%s|%s|%d|%d|%d|%s|%s|%s|%s|%.2f|%s|%.2f|%.2f|%d" % (
            clip, CLASS.get(clip, "cruise"), st["frames"], st["loaded"], st["hipFails"],
            ",".join(sorted(st["why"])) or "-", f(st["hipMin"]), f(st["spineMin"]), f(st["backMin"]),
            st["pose"] * 100, st["poseWhere"] or "-", st["world"] * 100, st["rig"] * 100, st["poseFails"]), flush=True)
    print("HIP clips %d frames %d loaded %d hipFails %d" % (len(order), T["frames"], T["loaded"], T["hipFails"]), flush=True)
    print("NOCLIP clips %d frames %d worldMax %.2f pose %.2f rigJoint %.2f poseFails %d" % (
        len(order), T["frames"], T["world"] * 100, T["pose"] * 100, T["rig"] * 100, T["poseFails"]), flush=True)


main()
