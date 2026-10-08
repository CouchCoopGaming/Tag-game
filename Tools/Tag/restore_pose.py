#!/usr/bin/env python3
"""Write the five grade-A clips from the Storror pose track.

Bone angles come from storror_pose.blender_euler. The preview capsule is
moved so a foot or a hand meets the wall; hips, knees and the spine are not
rotated off the track to dodge a shell overlap. A planted foot may roll so
its sole faces the wall, and a hand that would pass through that plant may
yaw back by a few degrees.

Root and the armature rotation keys stay empty. capsule_preview_m is the
preview path only.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bpy
from mathutils import Vector

import render_hier_v080_stills as rh
import render_hier_pass5 as p5
import storror_pose as sp

PASS6 = os.path.join(rh.OUT, "pass6")
ERROR_PATH = os.path.join(PASS6, "pose_error_pass9.txt")
# Story frames for the 2x4 reference strips. Cat leap is the crouch, the
# launch, the catch, the foot plant and the hang, not the old eight beats.
STORY = {
    "03_wall_drop_softland": [0, 9, 15, 21, 27, 30, 36, 48],
    "04_window_drop_softland": [0, 8, 14, 20, 25, 32, 40, 50],
    "10_wallrun_slanted": [0, 24, 40, 56, 72, 88, 104, 120],
    "12_tictac_slanted_wall": [8, 20, 28, 34, 42, 55, 80, 130],
    "15_cat_leap_wall": [0, 24, 48, 64, 72, 79, 84, 108],
}
SCORED = ("UpperLeg_L", "UpperLeg_R", "LowerLeg_L", "LowerLeg_R", "Spine")
RAD = math.pi / 180.0


def _locals(obj):
    return [tuple(v.co) for v in obj.data.vertices]


def _wx(obj, local):
    mw = obj.matrix_world
    best = None
    for co in local:
        x = (mw @ Vector(co)).x
        if best is None or x < best:
            best = x
    return best


def _wz(obj, local):
    mw = obj.matrix_world
    best = None
    for co in local:
        z = (mw @ Vector(co)).z
        if best is None or z < best:
            best = z
    return best


def _min_x(meshes, token, lip):
    best = None
    for name, obj, local in meshes:
        if token not in name:
            continue
        mw = obj.matrix_world
        for co in local:
            w = mw @ Vector(co)
            if lip is not None and w.z > lip - 0.01:
                continue
            if best is None or w.x < best:
                best = w.x
    return best


def _lowest(meshes):
    best = None
    for _name, obj, local in meshes:
        z = _wz(obj, local)
        if best is None or z < best:
            best = z
    return best


def _intruders(meshes, lip, ignore):
    """(name, metres past the face) for shells that are not the planted foot."""
    found = []
    for name, obj, local in meshes:
        if any(tok in name for tok in ignore):
            continue
        mw = obj.matrix_world
        deep = 0.0
        for co in local:
            w = mw @ Vector(co)
            if lip is not None and w.z > lip - 0.01:
                continue
            if -0.003 - w.x > deep:
                deep = -0.003 - w.x
        if deep > 0.005:
            found.append((name, deep))
    found.sort(key=lambda r: -r[1])
    return found


def _bone_zx(arm, name):
    pb = arm.pose.bones[name]
    z = (arm.matrix_world @ pb.matrix).to_3x3().col[2]
    return z.x


def _set_cap(arm, cap):
    arm.location = cap
    bpy.context.view_layer.update()


def _lift(arm, meshes, cap):
    low = _lowest(meshes)
    if low is not None and low < -0.001:
        cap = Vector((cap.x, cap.y, cap.z - low))
        _set_cap(arm, cap)
    return cap


def _push_out(arm, meshes, lip, cap):
    for _ in range(4):
        deep = 0.0
        for name, obj, local in meshes:
            mw = obj.matrix_world
            for co in local:
                w = mw @ Vector(co)
                if lip is not None and w.z > lip - 0.01:
                    continue
                if -0.003 - w.x > deep:
                    deep = -0.003 - w.x
        if deep <= 0.001:
            break
        cap = Vector((cap.x + deep, cap.y, cap.z))
        _set_cap(arm, cap)
    return cap


def _retract_hand(arm, meshes, lip, side):
    """Yaw the arm a few degrees so the hand leaves the wall. Hips stay."""
    bone = f"UpperArm_{side}"
    pb = arm.pose.bones.get(bone)
    if pb is None:
        return 0.0
    pb.rotation_mode = "XYZ"
    base = list(pb.rotation_euler)
    best = 0.0
    best_deep = None
    for deg in range(-16, 18, 2):
        if deg == 0:
            continue
        pb.rotation_euler = (base[0], base[1], base[2] + deg * RAD)
        bpy.context.view_layer.update()
        deep = 0.0
        for name, obj, local in meshes:
            if f"Hand_{side}" not in name and f"Arm_{side}" not in name:
                continue
            mw = obj.matrix_world
            for co in local:
                w = mw @ Vector(co)
                if lip is not None and w.z > lip - 0.01:
                    continue
                if -0.003 - w.x > deep:
                    deep = -0.003 - w.x
        if best_deep is None or deep < best_deep:
            best_deep = deep
            best = deg
    pb.rotation_euler = (base[0], base[1], base[2] + best * RAD)
    bpy.context.view_layer.update()
    return best


def _roll_sole(arm, meshes, lip, side):
    """Roll the planted foot so its sole axis points at the wall (-X)."""
    name = f"Foot_{side}"
    pb = arm.pose.bones.get(name)
    if pb is None:
        return 0.0, 0
    pb.rotation_mode = "XYZ"
    base = list(pb.rotation_euler)
    best = list(base)
    best_x = _bone_zx(arm, name)
    best_deg = 0.0
    best_comp = 0
    ignore = (f"Foot_{side}",)
    for comp in (0, 1, 2):
        for deg in range(-60, 65, 5):
            trial = list(base)
            trial[comp] = base[comp] + deg * RAD
            pb.rotation_euler = tuple(trial)
            bpy.context.view_layer.update()
            sx = _bone_zx(arm, name)
            if sx > best_x - 0.03:
                continue
            bad = _intruders(meshes, lip, ignore)
            # A roll that drives the shin or the torso through the face is dropped.
            if any("Foot_" not in n for n, _d in bad):
                continue
            best_x = sx
            best = trial
            best_deg = deg
            best_comp = comp
    pb.rotation_euler = tuple(best)
    bpy.context.view_layer.update()
    return best_deg, best_comp


def _seat(arm, meshes, cap, face, lip, pull_m):
    """Translation, then a sole roll and a small hand yaw. Returns the capsule."""
    cap = _lift(arm, meshes, cap)
    if face is None:
        return cap, 0.0, 0.0
    foot_x = None
    planted = None
    for side in ("L", "R"):
        x = _min_x(meshes, f"Foot_{side}", lip)
        if x is None:
            continue
        if foot_x is None or x < foot_x:
            foot_x = x
            planted = side
    rolled = 0.0
    yawed = 0.0
    if foot_x is not None and -0.02 <= foot_x <= pull_m:
        dx = -0.003 - foot_x
        cap = Vector((cap.x + dx, cap.y, cap.z))
        _set_cap(arm, cap)
        cap = _lift(arm, meshes, cap)
        if planted is not None and _bone_zx(arm, f"Foot_{planted}") > -0.75:
            rolled, _comp = _roll_sole(arm, meshes, lip, planted)
            # The roll moves the shell. Put the sole back on the face.
            fx = _min_x(meshes, f"Foot_{planted}", lip)
            if fx is not None:
                cap = Vector((cap.x + (-0.003 - fx), cap.y, cap.z))
                _set_cap(arm, cap)
        bad = _intruders(meshes, lip, (f"Foot_{planted}",) if planted else ())
        for name, _deep in bad:
            if "Hand_L" in name or "Arm_L" in name:
                yawed = _retract_hand(arm, meshes, lip, "L")
            elif "Hand_R" in name or "Arm_R" in name:
                yawed = _retract_hand(arm, meshes, lip, "R")
    cap = _push_out(arm, meshes, lip, cap)
    cap = _lift(arm, meshes, cap)
    return cap, rolled, yawed


def _read_eulers(arm):
    out = {}
    for pb in arm.pose.bones:
        if pb.name == "Root":
            continue
        pb.rotation_mode = "XYZ"
        e = pb.rotation_euler
        out[pb.name] = [round(math.degrees(e.x), 2), round(math.degrees(e.y), 2), round(math.degrees(e.z), 2)]
    return out


def _diff(a, b):
    d = abs(a - b) % 360.0
    if d > 180.0:
        d = 360.0 - d
    return d


def _track_euler(smooth, i):
    n = len(smooth["knee_flex_L"])
    ch = sp.sample_raw(smooth, 0.0 if n <= 1 else i / (n - 1))
    return sp.blender_euler(sp.unity_pose(ch))


def _score(keyed, track):
    worst = 0.0
    bone = ""
    comp = 0
    for name in SCORED:
        got = keyed.get(name, [0.0, 0.0, 0.0])
        ref = track.get(name, (0.0, 0.0, 0.0))
        for c in range(3):
            d = _diff(got[c], ref[c])
            if d > worst:
                worst = d
                bone = name
                comp = c
    return worst, bone, comp


def write_tracks():
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    meshes = [(o.name, o, _locals(o)) for o in bpy.data.objects if o.type == "MESH" and o.name.startswith("Mesh_")]
    doc = json.load(open(p5.OUT and os.path.join(p5.OUT, "keyed_clips.json")))
    lines = ["frame bone component error_deg   (hips, knees, spine vs the pose track)"]
    summary = []
    for spec in sp.CLIPS:
        cid = spec["id"]
        curves = sp.clip_curves(cid, keys=24)
        smooth = curves["raw_smooth"]
        facing = p5._facing_for(spec, smooth)
        path = p5.root_path(spec)
        _u, hero_i = p5._hero(spec, smooth)
        loc = p5._hero_locals(arm, spec, smooth, facing, hero_i)
        capsule, face, lip_z, _meta = p5._fit_path(spec, path, hero_i, loc)
        # A wall run may pull a foot onto the face from a short gap. A cat leap
        # only takes that pull once the path has already brought the foot close,
        # so the crouch stays on the ground.
        pull = 0.18 if spec["verb"] == "wallrun" else 0.10
        if spec["verb"] == "walljump":
            pull = 0.12
        keys_out = []
        caps_out = []
        over = 0
        worst = 0.0
        worst_at = ""
        roll_n = 0
        yaw_n = 0
        n = path["frames"]
        for i in range(n):
            p5._pose_frame(arm, smooth, i, facing, capsule[i], spec)
            cap, rolled, yawed = _seat(arm, meshes, arm.location.copy(), face, lip_z, pull)
            if abs(rolled) > 0.1:
                roll_n += 1
            if abs(yawed) > 0.1:
                yaw_n += 1
            eulers = _read_eulers(arm)
            track = _track_euler(smooth, i)
            err, bone, comp = _score(eulers, track)
            if err > worst:
                worst = err
                worst_at = f"f={i} {bone}[{comp}]"
            if err > 10.0:
                over += 1
            lines.append(f"{cid} {i} {bone} {comp} {err:.3f}")
            if i in STORY.get(cid, []):
                fx = _min_x(meshes, "Foot_", lip_z)
                hx = _min_x(meshes, "Hand_", lip_z)
                print(
                    f"STORY {cid} f={i} footx={None if fx is None else round(fx*100,1)} "
                    f"handx={None if hx is None else round(hx*100,1)} "
                    f"cap=({cap.x:.2f},{cap.y:.2f},{cap.z:.2f}) err={err:.2f}",
                    flush=True,
                )
            keys_out.append({"frame": i, "t": round(i / path["fps"], 4), "bones": eulers})
            caps_out.append([round(cap.x, 4), round(cap.y, 4), round(cap.z, 4)])
            if i % 40 == 0 or i == n - 1:
                print(
                    f"  {cid} {i+1}/{n} err={err:.2f} rollN={roll_n} yawN={yaw_n}",
                    flush=True,
                )
        entry = doc["clips"][cid]
        entry["keys"] = keys_out
        entry["capsule_preview_m"] = caps_out
        entry["root_location_keys"] = False
        entry["frames"] = n
        entry["hero_frame"] = hero_i
        entry["fps"] = path["fps"]
        msg = (
            f"TRACK {cid} frames={n} over10={over} worst={worst:.2f} {worst_at} "
            f"soleRolls={roll_n} armYaws={yaw_n}"
        )
        print(msg, flush=True)
        summary.append(msg)
    doc["root_motion"] = False
    with open(os.path.join(p5.OUT, "keyed_clips.json"), "w") as f:
        json.dump(doc, f)
    os.makedirs(PASS6, exist_ok=True)
    with open(ERROR_PATH, "w") as f:
        f.write("\n".join(lines) + "\n")
        f.write("\n".join(summary) + "\n")
    print("wrote", ERROR_PATH, flush=True)
    rh._reset(arm)
    rh._hide_wall()
    return summary


def _stick_cell(clip_id, frame, n):
    from PIL import Image
    path = os.path.join(sp.JSON_DIR, "..", "strips", clip_id + ".png")
    strip = Image.open(path).convert("RGB")
    cell = 0 if n <= 1 else int(round(frame / (n - 1) * 11))
    cell = max(0, min(11, cell))
    return strip.crop((cell * 160, 26, cell * 160 + 160, 26 + 300))


def _rest_sole_ids(arm):
    """Vertices that sit on the ground in the rest pose. Those are the sole."""
    rh._reset(arm)
    bpy.context.view_layer.update()
    ids = {}
    local = {}
    for side in ("L", "R"):
        obj = bpy.data.objects[f"Mesh_Foot_{side}"]
        co = [tuple(v.co) for v in obj.data.vertices]
        local[side] = co
        mw = obj.matrix_world
        order = sorted(range(len(co)), key=lambda i: (mw @ Vector(co[i])).z)
        n = max(6, len(order) // 5)
        ids[side] = order[:n]
    return ids, local


def _sole_span(obj, local, indices):
    mw = obj.matrix_world
    xs = [(mw @ Vector(local[i])).x for i in indices]
    return min(xs), max(xs)


def _other_min_x(meshes, skip, lip):
    best = None
    for name, obj, local in meshes:
        if name == skip:
            continue
        mw = obj.matrix_world
        for co in local:
            w = mw @ Vector(co)
            if lip is not None and w.z > lip - 0.01:
                continue
            if best is None or w.x < best:
                best = w.x
    return best


def flatten_soles():
    """Roll a planted foot so the sole, not a corner, meets the wall.

    Hips, knees and the spine are left on the pose track. Only Foot X changes,
    and only on a foot that is already the one nearest the wall.
    """
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    ids, foot_local = _rest_sole_ids(arm)
    meshes = [
        (o.name, o, _locals(o))
        for o in bpy.data.objects
        if o.type == "MESH" and o.name.startswith("Mesh_")
    ]
    doc = json.load(open(os.path.join(p5.OUT, "keyed_clips.json")))
    for spec in sp.CLIPS:
        if spec["verb"] not in ("wallrun", "cling", "walljump"):
            continue
        cid = spec["id"]
        entry = doc["clips"][cid]
        smooth = sp.clip_curves(cid, keys=24)["raw_smooth"]
        facing = p5._facing_for(spec, smooth)
        # Lip height from the keyed hero, matching the proof.
        import noclip_proof as nc
        _has, lip = nc._wall_plan(arm, "pass5", cid, entry, facing)
        n_flat = 0
        for i, key in enumerate(entry["keys"]):
            rh._apply_eulers(arm, key["bones"])
            cap = Vector(entry["capsule_preview_m"][i])
            arm.location = cap
            arm.rotation_euler = facing
            root = arm.pose.bones.get("Root")
            if root is not None:
                root.location = (0.0, 0.0, 0.0)
                root.rotation_euler = (0.0, 0.0, 0.0)
            bpy.context.view_layer.update()
            # Start the ankle from the pose track so a second run does not
            # stack rolls, then lay that sole on the face.
            track = _track_euler(smooth, i)
            for side in ("L", "R"):
                name = f"Foot_{side}"
                if name in track:
                    key["bones"][name] = [round(a, 2) for a in track[name]]
            rh._apply_eulers(arm, key["bones"])
            arm.location = cap
            arm.rotation_euler = facing
            if root is not None:
                root.location = (0.0, 0.0, 0.0)
                root.rotation_euler = (0.0, 0.0, 0.0)
            bpy.context.view_layer.update()
            planted = None
            planted_x = None
            for side in ("L", "R"):
                x = _min_x(meshes, f"Foot_{side}", lip)
                if x is None:
                    continue
                if planted_x is None or x < planted_x:
                    planted_x = x
                    planted = side
            if planted is None or planted_x > 0.18:
                continue
            obj = bpy.data.objects[f"Mesh_Foot_{planted}"]
            pb = arm.pose.bones[f"Foot_{planted}"]
            pb.rotation_mode = "XYZ"
            base = list(pb.rotation_euler)
            best = None
            for deg in range(-80, 85, 5):
                pb.rotation_euler = (base[0] + deg * RAD, base[1], base[2])
                bpy.context.view_layer.update()
                lo, hi = _sole_span(obj, foot_local[planted], ids[planted])
                spread = hi - lo
                foot_near = _min_x(meshes, f"Foot_{planted}", lip)
                # The sole has to be the face that meets the wall.
                if foot_near is None or foot_near < lo - 0.008:
                    continue
                other = _other_min_x(meshes, f"Mesh_Foot_{planted}", lip)
                dx = -0.003 - lo
                if other is not None and other + dx < -0.004:
                    dx = -0.004 - other
                sole_at = lo + dx
                gap = max(0.0, sole_at + 0.003)
                score = (gap > 0.015, round(gap, 3), round(spread, 3))
                if best is None or score < best[0]:
                    best = (score, deg, dx, spread, sole_at)
            if best is None:
                continue
            _score, deg, dx, spread, sole_at = best
            if spread > 0.04 and abs(deg) < 5:
                continue
            pb.rotation_euler = (base[0] + deg * RAD, base[1], base[2])
            cap = Vector((cap.x + dx, cap.y, cap.z))
            arm.location = cap
            bpy.context.view_layer.update()
            cap = _lift(arm, meshes, cap)
            cap = _push_out(arm, meshes, lip, cap)
            eulers = _read_eulers(arm)
            # Scored joints stay on the pose track. Copy those keys back so a
            # foot edit cannot rewrite them.
            for name in SCORED:
                if name in key["bones"]:
                    eulers[name] = key["bones"][name]
            key["bones"] = eulers
            entry["capsule_preview_m"][i] = [round(cap.x, 4), round(cap.y, 4), round(cap.z, 4)]
            n_flat += 1
            if i in STORY.get(cid, []):
                lo, hi = _sole_span(obj, foot_local[planted], ids[planted])
                print(
                    f"SOLE {cid} f={i} {planted} deg={deg} spread={spread*100:.1f} "
                    f"sole={lo*100:.1f}..{hi*100:.1f}",
                    flush=True,
                )
        print(f"FLAT {cid} frames={n_flat}", flush=True)
    with open(os.path.join(p5.OUT, "keyed_clips.json"), "w") as f:
        json.dump(doc, f)
    print("wrote soles", flush=True)
    rh._reset(arm)
    rh._hide_wall()


def render_refs():
    from PIL import Image, ImageDraw
    arm = bpy.data.objects["DummyArmature"]
    rh._prepare_scene()
    if hasattr(bpy.context.scene, "eevee"):
        bpy.context.scene.eevee.taa_render_samples = 8
    # Sharper than the cell, then scaled down into the 1600 px strip.
    bpy.context.scene.render.resolution_x = 640
    bpy.context.scene.render.resolution_y = 800
    doc = json.load(open(os.path.join(p5.OUT, "keyed_clips.json")))
    os.makedirs(PASS6, exist_ok=True)
    prev = os.path.join("/tmp", "pose_ref")
    os.makedirs(prev, exist_ok=True)
    labels = {
        "03_wall_drop_softland": "03 soft land",
        "04_window_drop_softland": "04 soft land",
        "10_wallrun_slanted": "10 wall run",
        "12_tictac_slanted_wall": "12 tic-tac",
        "15_cat_leap_wall": "15 cat leap",
    }
    for spec in sp.CLIPS:
        cid = spec["id"]
        entry = doc["clips"][cid]
        facing = p5._facing_for(spec, sp.clip_curves(cid, keys=24)["raw_smooth"])
        has_wall, wall_top = __import__("noclip_proof")._wall_plan(arm, "pass5", cid, entry, facing)
        frames = [i for i in STORY[cid] if i < len(entry["keys"])]
        shots = []
        for s, i in enumerate(frames):
            key = entry["keys"][i]
            cap = entry["capsule_preview_m"][i]
            rh._apply_eulers(arm, key["bones"])
            arm.location = Vector(cap)
            arm.rotation_euler = facing
            root = arm.pose.bones.get("Root")
            if root is not None:
                root.location = (0.0, 0.0, 0.0)
                root.rotation_euler = (0.0, 0.0, 0.0)
            bpy.context.view_layer.update()
            if has_wall:
                rh._show_wall(0.0, wall_top, arm.location.y, 2.4)
            else:
                rh._hide_wall()
            loc, look, ortho = p5._shot_at(arm, spec, True)
            path = os.path.join(prev, f"{cid}_{s}.png")
            rh._shot(path, loc, look, ortho=ortho)
            shots.append(path)
            print(f"ref {cid} {s} f={i}", flush=True)
        # 2 rows x 4 columns = 1600 px. Each cell is the stick beside the mannequin.
        cell_w = 400
        cell_h = 300
        canvas = Image.new("RGB", (cell_w * 4, cell_h * 2 + 28), (48, 44, 40))
        draw = ImageDraw.Draw(canvas)
        draw.rectangle((0, 0, 1600, 28), fill=(20, 18, 16))
        draw.text((12, 8), labels[cid] + "   pose track beside the reference stick", fill=(245, 236, 220))
        n = len(entry["keys"])
        for s, i in enumerate(frames):
            stick = _stick_cell(cid, i, n).resize((150, 280), Image.Resampling.LANCZOS)
            man = Image.open(shots[s]).convert("RGB")
            man.thumbnail((240, 280), Image.Resampling.LANCZOS)
            col = s % 4
            row = s // 4
            x0 = col * cell_w
            y0 = 28 + row * cell_h
            canvas.paste(stick, (x0 + 4, y0 + 10))
            canvas.paste(man, (x0 + 156 + (240 - man.width) // 2, y0 + 10 + (280 - man.height) // 2))
            draw.text((x0 + 8, y0 + 12), f"f{i}", fill=(245, 236, 220))
        slug = rh.SLUGS[cid]
        out = os.path.join(PASS6, f"ref_{slug}.png")
        canvas.save(out)
        print("wrote", out, flush=True)
    rh._reset(arm)
    rh._hide_wall()


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if "--stills" in argv:
        render_refs()
    elif "--soles" in argv:
        flatten_soles()
    else:
        write_tracks()
