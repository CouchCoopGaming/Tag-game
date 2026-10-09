#!/usr/bin/env python3
"""Pose extraction for motion-reference clips that are not on the Storror manifest.

Raw video stays outside the repo. This writes the same landmark JSON and a
stick strip as Docs/Storror/tools/extract.py. Contact sheets are not written:
they would contain video frames.
"""
import json
import os
import sys

import cv2
import numpy as np

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Storror", "tools"))
import extract as ex

MODEL = os.environ.get(
    "POSE_MODEL",
    "/tmp/motionref/models/pose_landmarker_heavy.task",
)
OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Movement"))


def _landmarker():
    from mediapipe.tasks import python as mpt
    from mediapipe.tasks.python import vision
    opts = vision.PoseLandmarkerOptions(
        base_options=mpt.BaseOptions(model_asset_path=MODEL),
        running_mode=vision.RunningMode.IMAGE,
        num_poses=1,
        min_pose_detection_confidence=0.3,
        min_pose_presence_confidence=0.3,
        min_tracking_confidence=0.3,
    )
    return vision.PoseLandmarker.create_from_options(opts)


def _read(path, t0, t1, max_side=720):
    cap = cv2.VideoCapture(path)
    fps = cap.get(cv2.CAP_PROP_FPS) or 30.0
    frames, times = [], []
    while True:
        ok, fr = cap.read()
        if not ok:
            break
        t = cap.get(cv2.CAP_PROP_POS_MSEC) / 1000.0
        if t < t0 - 1e-3:
            continue
        if t > t1:
            break
        h, w = fr.shape[:2]
        scale = max_side / max(h, w)
        if scale < 1.0:
            fr = cv2.resize(fr, (int(w * scale), int(h * scale)))
        frames.append(fr)
        times.append(t)
    cap.release()
    return frames, times, fps


def scout(path, step_s=0.2):
    """Print a coarse pose line so a window can be chosen before the full extract."""
    import mediapipe as mp
    frames, times, fps = _read(path, 0.0, 1e9, max_side=480)
    lm = _landmarker()
    step = max(1, int(round(step_s * fps)))
    print(f"scout frames={len(frames)} fps={fps:.2f} step={step}")
    for i in range(0, len(frames), step):
        fr = frames[i]
        rgb = cv2.cvtColor(fr, cv2.COLOR_BGR2RGB)
        image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
        res = lm.detect(image)
        if not res.pose_landmarks:
            print(f"t={times[i]:5.2f} NONE")
            continue
        pose = res.pose_landmarks[0]
        world = res.pose_world_landmarks[0]
        W = np.array([[p.x, p.y, p.z] for p in world], dtype=np.float64)
        ang = ex.angles(W)
        hip = (W[23] + W[24]) * 0.5
        feet = max(W[j, 1] for j in (27, 28, 29, 30, 31, 32))
        root = feet - hip[1]
        knee = max(ang["knee_flex_L"] or 0, ang["knee_flex_R"] or 0)
        hipf = max(ang["hip_flex_L"] or 0, ang["hip_flex_R"] or 0)
        wrist_y = min(W[15, 1], W[16, 1])
        hands_up = hip[1] - wrist_y
        foot_gap = abs(W[27, 1] - W[28, 1])
        print(
            f"t={times[i]:5.2f} root={root:5.2f} knee={knee:5.0f} hip={hipf:5.0f} "
            f"hands={hands_up:5.2f} footgap={foot_gap:4.2f} vis={pose[11].visibility:.2f}"
        )


def extract_clip(spec):
    """spec: id, path, t0, t1, move, verb, view, source dict, license."""
    import mediapipe as mp
    frames, times, fps = _read(spec["path"], spec["t0"], spec["t1"], max_side=720)
    if not frames:
        raise SystemExit(f"no frames in {spec['id']}")
    lm = _landmarker()
    T = len(frames)
    Wr = np.full((T, 33, 3), np.nan)
    Ir = np.full((T, 33, 2), np.nan)
    V = np.zeros((T, 33))
    det = np.zeros(T, bool)
    h0, w0 = frames[0].shape[:2]
    for i, fr in enumerate(frames):
        rgb = cv2.cvtColor(fr, cv2.COLOR_BGR2RGB)
        image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
        res = lm.detect(image)
        if not res.pose_landmarks:
            continue
        pose = res.pose_landmarks[0]
        world = res.pose_world_landmarks[0]
        Ir[i] = np.array([[p.x, p.y] for p in pose])
        Wr[i] = np.array([[p.x, p.y, p.z] for p in world])
        V[i] = np.array([p.visibility for p in pose])
        det[i] = True
        if i % 15 == 0:
            print(f"{spec['id']} {i}/{T}", flush=True)
    maxgap = max(1, int(round(0.12 * fps)))

    def same_subject(a, b):
        ha = (Ir[a, 23] + Ir[a, 24]) / 2 * [w0, h0]
        hb = (Ir[b, 23] + Ir[b, 24]) / 2 * [w0, h0]
        ta = np.linalg.norm(((Ir[a, 11] + Ir[a, 12]) / 2 * [w0, h0]) - ha)
        return np.linalg.norm(ha - hb) < 0.5 * ta * (b - a)

    Wf, filled = ex.interp_gaps(Wr, det, maxgap, same_subject)
    If, _ = ex.interp_gaps(Ir, det, maxgap, same_subject)
    Vf, _ = ex.interp_gaps(V, det, maxgap, same_subject)
    fw = ex.OneEuro(fps, mincut=1.5, beta=0.6)
    Ws = np.full_like(Wf, np.nan)
    prev = False
    for i in range(T):
        if filled[i]:
            if not prev:
                fw.x = None
            Ws[i] = fw(Wf[i])
        prev = bool(filled[i])
    mask = (Vf >= ex.VIS_T) & filled[:, None]
    ANG, root_h = [], []
    for i in range(T):
        if not filled[i]:
            ANG.append(None)
            root_h.append(None)
            continue
        W = Ws[i]
        ANG.append(ex.angles(W))
        hipm = (W[23] + W[24]) / 2
        feet_y = max(W[j, 1] for j in (27, 28, 29, 30, 31, 32))
        root_h.append(round(float(feet_y - hipm[1]), 4))
    rnd = lambda a, n: np.where(np.isnan(a), None, np.round(a, n)).tolist()
    vis_body = float(np.mean(Vf[filled][:, 11:])) if filled.any() else 0.0
    doc = {
        "clip_id": spec["id"],
        "source": spec["source"],
        "license": spec["license"],
        "move": spec["move"],
        "game_verb": spec["verb"],
        "camera_view": spec["view"],
        "fps": round(float(fps), 4),
        "frame_count": T,
        "detected_frames": int(det.sum()),
        "usable_frames": int(filled.sum()),
        "proxy_resolution": [w0, h0],
        "model": "MediaPipe PoseLandmarker heavy, IMAGE mode, num_poses=1",
        "coordinate_notes": ex.main.__doc__ and "",
        "angle_notes": "Same degrees as Docs/Storror/tools/extract.py.",
        "landmark_names": ex.NAMES,
        "times_s": [round(t, 4) for t in times],
        "detected": det.tolist(),
        "usable": filled.tolist(),
        "world_xyz_m": rnd(Ws, 4),
        "visibility": np.round(Vf, 3).tolist(),
        "confidence_mask": mask.tolist(),
        "joint_angles_deg": ANG,
        "root_height_above_feet_m": root_h,
        "quality": {
            "mean_visibility_body": round(vis_body, 3),
            "frac_usable": round(float(filled.mean()), 3),
        },
        "reference_only": True,
    }
    # The empty coordinate note above is replaced with the Storror wording.
    doc["coordinate_notes"] = (
        "world = MediaPipe world landmarks in metres, origin at hip centre, "
        "camera-aligned (x right, y DOWN). Not gravity-aligned. Raw video is not in the repo."
    )
    os.makedirs(os.path.join(OUT, "pose"), exist_ok=True)
    os.makedirs(os.path.join(OUT, "strips"), exist_ok=True)
    out_json = os.path.join(OUT, "pose", spec["id"] + ".json")
    json.dump(doc, open(out_json, "w"))
    _strip(spec, Ws, Vf, filled, times)
    print(
        spec["id"], "frames", T, "det", int(det.sum()),
        "usable", int(filled.sum()), "vis", round(vis_body, 3),
        "wrote", out_json,
    )


def _strip(spec, Ws, Vf, filled, times):
    us = np.where(filled)[0]
    if len(us) == 0:
        return
    keys = us[np.linspace(0, len(us) - 1, min(12, len(us))).astype(int)]
    cw, ch, s = 160, 300, 140.0
    strip = np.full((ch, cw * len(keys), 3), 255, np.uint8)
    t0 = times[0]
    for n, k in enumerate(keys):
        W = Ws[k]
        feet_y = float(np.nanmax(W[:, 1]))
        P = np.stack([cw * n + cw / 2 + W[:, 0] * s, (ch - 30) + (W[:, 1] - feet_y) * s], 1)
        cv2.line(strip, (cw * n + 10, ch - 30), (cw * n + cw - 10, ch - 30), (200, 200, 200), 1)
        ex.draw_skel(strip, P, Vf[k], thick=3)
        cv2.putText(
            strip, f"{times[k] - t0:.2f}s", (cw * n + 6, ch - 10),
            cv2.FONT_HERSHEY_SIMPLEX, 0.45, (80, 80, 80), 1,
        )
    lab = np.full((26, strip.shape[1], 3), 255, np.uint8)
    cv2.putText(
        lab, f"{spec['id']}  ({spec['verb']})  stick, camera plane. orange=left blue=right",
        (6, 18), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (0, 0, 0), 1,
    )
    cv2.imwrite(os.path.join(OUT, "strips", spec["id"] + ".png"), np.vstack([lab, strip]))


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "scout":
        scout(sys.argv[2])
    elif cmd == "clip":
        extract_clip(json.load(open(sys.argv[2])))
    else:
        raise SystemExit("usage: ref_extract.py scout <video> | clip <spec.json>")
