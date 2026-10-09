"""Hip-sit on the dumped 30 fps frames for the three exits this pass touches."""
import math
import sys

import bpy
import numpy as np

sys.path.insert(0, "/workspace/Tools")
import RenderPass7 as p7
import RenderPass9 as p9
import NoClipCheck as nc

FRAMES = "/tmp/noclip-frames-p25.txt"
NAMES = ("exit-ClimbTopOut", "exit-Vault", "exit-Mantle")


def load(path):
    clips = {}
    name = None
    for line in open(path):
        parts = line.split()
        if not parts:
            continue
        if parts[0] == "CLIP":
            name = parts[1]
            clips[name] = {"solid": parts[4], "dur": float(parts[2]), "frames": []}
        elif parts[0] == "F" and name:
            clips[name]["frames"].append((float(parts[1]), [float(x) for x in parts[2:]]))
    return clips


def knee_y(pieces, name, pelvis_y):
    cloud = nc.piece_cloud(pieces, (name,))
    if len(cloud) == 0:
        return 0.0
    zcut = np.quantile(cloud[:, 2], 0.85)
    top = cloud[cloud[:, 2] >= zcut]
    return (float(top.mean(0)[1]) - pelvis_y) * 100


def main():
    clips = load(FRAMES)
    p7.clear()
    arm = p7.import_runner()
    base = arm.rotation_quaternion.copy()
    parent = nc.build_joints(arm)
    p7.reset_arm(arm, base)
    p9.face_travel(arm, base)
    bind, _deps = nc.gather(arm)
    sole = float(nc.piece_cloud(bind, ("Mesh_Foot_L", "Mesh_Foot_R"))[:, 2].min())
    print("SOLE", round(sole * 100, 2))
    summary = []
    for name in NAMES:
        clip = clips[name]
        backs = []
        hinges = []
        fails = []
        print("CLIP", name, "frames", len(clip["frames"]))
        for t, nums in clip["frames"]:
            if len(nums) == 26:
                nums = nums + [0.0] * 6
            p7.reset_arm(arm, base)
            pieces, _deps = nc.pose_frame(arm, base, nums)
            hip = nc.piece_cloud(pieces, ("Mesh_Hips",))
            fl = nc.piece_cloud(pieces, ("Mesh_Foot_L",))
            fr = nc.piece_cloud(pieces, ("Mesh_Foot_R",))
            hl = nc.piece_cloud(pieces, ("Mesh_Hand_L",))
            hr = nc.piece_cloud(pieces, ("Mesh_Hand_R",))
            pelvis = hip.mean(0)
            lz, rz = float(fl[:, 2].min()), float(fr[:, 2].min())
            low = min(lz, rz)
            parts = []
            which = []
            if lz <= low + 0.04:
                parts.append(fl)
                which.append("L")
            if rz <= low + 0.04:
                parts.append(fr)
                which.append("R")
            foot = np.vstack(parts).mean(0)
            back = (float(foot[1]) - float(pelvis[1])) * 100
            hip_d = abs(math.degrees(arm.pose.bones["Hips"].rotation_euler.x))
            lum = abs(math.degrees(arm.pose.bones["Spine"].rotation_euler.x))
            chest = abs(math.degrees(arm.pose.bones["Chest"].rotation_euler.x))
            den = lum + chest
            ratio = hip_d / den if den > 0.5 else (99.0 if hip_d > 1 else 0.0)
            pd, pp, _a, _r, _rp, _h = nc.split_self(arm, pieces, parent)
            u = t / clip["dur"] if clip["dur"] else 0
            # Push and landing frames must sit. The last third of climb and vault
            # is the stride leaving the plant; mantle stays a plant the whole way.
            cruise = name != "exit-Mantle" and u > 0.62
            plant_fail = (not cruise) and back < 8.0
            hinge_fail = den > 1.0 and ratio < 1.5
            flag = []
            if plant_fail:
                flag.append("back")
            if hinge_fail:
                flag.append("hinge")
            if pd > 0.005:
                flag.append("pose")
            hx = max(abs(hl.mean(0)[0] - pelvis[0]), abs(hr.mean(0)[0] - pelvis[0])) * 100
            if hx > 55:
                flag.append("hand")
            backs.append(back)
            hinges.append(ratio)
            if flag:
                fails.append((t, flag))
            print(
                "F", round(t, 3),
                "u", round(u, 2),
                "cruise" if cruise else "plant",
                "back", round(back, 1),
                "hinge", round(ratio, 2),
                "hip", round(hip_d, 1),
                "lum", round(lum, 1),
                "dS", round((lz - sole) * 100, 1), round((rz - sole) * 100, 1),
                "sup", "".join(which),
                "knee", round(knee_y(pieces, "Mesh_LowerLeg_L", pelvis[1]), 1),
                round(knee_y(pieces, "Mesh_LowerLeg_R", pelvis[1]), 1),
                "hX", round(hx, 1),
                "pelvisAboveSole", round((float(hip[:, 2].min()) - sole) * 100, 1),
                "pose", round(pd * 100, 2),
                pp or "-",
                "FLAG" if flag else "ok",
                ",".join(flag),
            )
        plant_backs = []
        print("END", name, "fails", len(fails))
        summary.append((name, len(clip["frames"]), min(backs), min(hinges), len(fails)))
    print(
        "hip-sit clips=%d loadedFrames=%d pelvisBackMin=%.1f cm hingeMin=%.2f fails=%d"
        % (
            len(summary),
            sum(row[1] for row in summary),
            min(row[2] for row in summary),
            min(row[3] for row in summary),
            sum(row[4] for row in summary),
        )
    )
    print("HIPFRAMES_EXIT")


if __name__ == "__main__":
    main()
