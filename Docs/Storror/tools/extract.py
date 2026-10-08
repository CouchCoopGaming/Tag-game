"""Video pose extraction for Storror reference clips (MediaPipe PoseLandmarker heavy, IMAGE mode on ROI crops, size+position continuity).
Usage: python extract.py <clip_id>   (reads ../clips.json)"""
import sys, os, json, math, cv2, numpy as np, mediapipe as mp
from mediapipe.tasks import python as mpt
from mediapipe.tasks.python import vision

ROOT = '/workspace/storror'
NAMES = ["nose","left_eye_inner","left_eye","left_eye_outer","right_eye_inner","right_eye","right_eye_outer","left_ear","right_ear",
 "mouth_left","mouth_right","left_shoulder","right_shoulder","left_elbow","right_elbow","left_wrist","right_wrist","left_pinky",
 "right_pinky","left_index","right_index","left_thumb","right_thumb","left_hip","right_hip","left_knee","right_knee","left_ankle",
 "right_ankle","left_heel","right_heel","left_foot_index","right_foot_index"]
EDGES = [(11,12),(11,23),(12,24),(23,24),(11,13),(13,15),(12,14),(14,16),(15,19),(16,20),(23,25),(25,27),(24,26),(26,28),
         (27,29),(29,31),(27,31),(28,30),(30,32),(28,32),(0,7),(0,8)]
LEFT = {11,13,15,17,19,21,23,25,27,29,31,7}; RIGHT = {12,14,16,18,20,22,24,26,28,30,32,8}
VIS_T = 0.5

def src_path(v):
    p = f'{ROOT}/scout/{v}_h264.mp4'
    if os.path.exists(p): return p
    for x in (f'{ROOT}/scout/{v}.mp4', f'{ROOT}/scout/{v}.mp4.part'):
        if os.path.exists(x): return x

class OneEuro:
    def __init__(s, fps, mincut=1.2, beta=0.3, dcut=1.0): s.f, s.mc, s.b, s.dc = fps, mincut, beta, dcut; s.x = None; s.dx = None
    @staticmethod
    def a(cut, f): tau = 1/(2*math.pi*cut); return 1/(1+tau*f)
    def __call__(s, x):
        if s.x is None: s.x = x.copy(); s.dx = np.zeros_like(x); return x.copy()
        dx = (x - s.x)*s.f; ad = s.a(s.dc, s.f); s.dx = ad*dx + (1-ad)*s.dx
        cut = s.mc + s.b*np.abs(s.dx); al = 1/(1 + (1/(2*math.pi*cut))*s.f)
        s.x = al*x + (1-al)*s.x; return s.x.copy()

def interp_gaps(arr, ok, maxgap, allow=None):
    """arr [T,...]; ok [T] bool. Linear fill gaps <= maxgap frames; returns filled, filled_mask.
    allow(a, b) -> bool can veto a fill (e.g. the endpoints are different people)."""
    T = len(arr); out = arr.copy(); filled = ok.copy(); idx = np.where(ok)[0]
    for a, b in zip(idx[:-1], idx[1:]):
        if 1 < b - a <= maxgap + 1 and (allow is None or allow(a, b)):
            for k in range(a+1, b):
                w = (k-a)/(b-a); out[k] = (1-w)*arr[a] + w*arr[b]; filled[k] = True
    return out, filled

def ang(a, b, c):
    u, v = a-b, c-b; n = np.linalg.norm(u)*np.linalg.norm(v)
    return float(np.degrees(np.arccos(np.clip(np.dot(u, v)/n, -1, 1)))) if n > 1e-9 else float('nan')

def body_frame(W):
    hipm = (W[23]+W[24])/2; shm = (W[11]+W[12])/2
    up = shm - hipm; up /= np.linalg.norm(up)+1e-9
    right = W[24] - W[23]  # subject's right hip minus left hip -> points to subject's right
    right = right - up*np.dot(right, up); right /= np.linalg.norm(right)+1e-9
    fwd = np.cross(up, right)
    if np.dot(fwd, W[0] - shm) < 0: fwd = -fwd  # forward must point toward nose
    return hipm, shm, up, right, fwd

def angles(W):
    hipm, shm, up, right, fwd = body_frame(W); r = {}
    for side, s, e, w, h, k, a, hl, fi, ix in (("L",11,13,15,23,25,27,29,31,19),("R",12,14,16,24,26,28,30,32,20)):
        out = -right if side == "L" else right
        th = W[k]-W[h]; r[f'hip_flex_{side}'] = math.degrees(math.atan2(np.dot(th, fwd), -np.dot(th, up)))
        r[f'hip_abd_{side}'] = math.degrees(math.atan2(np.dot(th, out), -np.dot(th, up)))
        r[f'knee_flex_{side}'] = 180 - ang(W[h], W[k], W[a])
        r[f'ankle_dorsi_{side}'] = 90 - ang(W[k], W[a], W[fi])
        ua = W[e]-W[s]; r[f'shoulder_flex_{side}'] = math.degrees(math.atan2(np.dot(ua, fwd), -np.dot(ua, up)))
        r[f'shoulder_abd_{side}'] = math.degrees(math.atan2(np.dot(ua, out), -np.dot(ua, up)))
        r[f'elbow_flex_{side}'] = 180 - ang(W[s], W[e], W[w])
        r[f'wrist_flex_{side}'] = 180 - ang(W[e], W[w], W[ix])
    # trunk vs camera vertical (camera y is DOWN; not gravity-aligned if camera tilts)
    r['trunk_lean_from_cam_vertical'] = ang(shm, hipm, hipm + np.array([0, -1.0, 0]))
    sl = W[12]-W[11]; hl = W[24]-W[23]
    sl_t = sl - up*np.dot(sl, up); hl_t = hl - up*np.dot(hl, up)
    tw = ang(sl_t, np.zeros(3), hl_t); sgn = np.sign(np.dot(np.cross(hl_t, sl_t), up)) or 1
    r['spine_twist'] = float(sgn*tw)
    r['spine_lateral_bend'] = math.degrees(math.atan2(np.dot(sl, up), np.linalg.norm(sl - up*np.dot(sl, up)))) - \
                              math.degrees(math.atan2(np.dot(hl, up), np.linalg.norm(hl - up*np.dot(hl, up))))
    earm = (W[7]+W[8])/2; nk = earm - shm
    r['neck_flex'] = math.degrees(math.atan2(np.dot(nk, fwd), np.dot(nk, up)))
    r['neck_lateral'] = math.degrees(math.atan2(np.dot(nk, right), np.dot(nk, up)))
    return {k: (round(float(v), 2) if np.isfinite(v) else None) for k, v in r.items()}

JOINT_DEPS = {'hip_flex':(23,25,24,26),'hip_abd':(23,25,24,26),'knee_flex':(23,25,27,24,26,28),'ankle_dorsi':(25,27,31,26,28,32),
              'shoulder_flex':(11,13,12,14),'shoulder_abd':(11,13,12,14),'elbow_flex':(11,13,15,12,14,16),'wrist_flex':(13,15,19,14,16,20)}

def draw_skel(img, P, vis, scale=1.0, thick=2):
    for a, b in EDGES:
        col = (255,160,40) if (a in LEFT and b in LEFT) else (40,140,255) if (a in RIGHT and b in RIGHT) else (230,230,230)
        if vis[a] < 0.3 or vis[b] < 0.3: col = tuple(int(c*0.4) for c in col)
        cv2.line(img, tuple(int(x) for x in P[a]), tuple(int(x) for x in P[b]), col, thick, cv2.LINE_AA)
    for i in range(33):
        if i in (1,2,3,4,5,6,9,10,17,18,21,22): continue
        cv2.circle(img, tuple(int(x) for x in P[i]), max(2, thick), (0,255,255) if vis[i] >= VIS_T else (0,0,180), -1, cv2.LINE_AA)

def main(cid):
    C = {c['id']: c for c in json.load(open(f'{ROOT}/clips.json'))}[cid]
    meta = json.load(open(f'{ROOT}/meta/{C["vid"]}.info.json'))
    seg = f'{ROOT}/raw/{cid}.mp4'
    if os.path.exists(seg):   # 720p segment downloaded with 1 s padding: local t + base = source t
        src_kind = '720p segment (raw/, box only)'; cap = cv2.VideoCapture(seg)
        sc_ = seg[:-4]+'.json'; base = json.load(open(sc_))['segment_start_s'] if os.path.exists(sc_) else max(0.0, C['t0']-1.0)
    else:
        src_kind = 'low-res scouting proxy'; cap = cv2.VideoCapture(src_path(C['vid'])); base = 0.0
        cap.set(cv2.CAP_PROP_POS_MSEC, max(0, C['t0']-0.5)*1000)
    fps = cap.get(cv2.CAP_PROP_FPS)
    frames, times = [], []
    while True:
        ok, fr = cap.read()
        if not ok: break
        t = cap.get(cv2.CAP_PROP_POS_MSEC)/1000.0 + base
        if t < C['t0'] - 1e-3: continue
        if t > C['t1']: break
        frames.append(fr); times.append(t)
    # scene-cut guard: truncate at first hard cut inside the window
    g = [cv2.resize(cv2.cvtColor(f, cv2.COLOR_BGR2GRAY), (64, 36)).astype(np.float32) for f in frames]
    hist = [cv2.calcHist([f.astype(np.uint8)], [0], None, [32], [0, 256]).ravel()/f.size for f in g]
    cut_at = None
    for i in (range(1, len(g)) if not C.get('nocut') else []):
        hd = 0.5*np.abs(hist[i]-hist[i-1]).sum(); md = np.abs(g[i]-g[i-1]).mean()
        if (hd > 0.45 and md > 40) or (hd > 0.3 and md > 45): cut_at = i; break
    if cut_at is not None:
        print('scene cut at', round(times[cut_at], 3), '-> truncating'); frames = frames[:cut_at]; times = times[:cut_at]
    h0, w0 = frames[0].shape[:2]; up = 3 if h0 <= 160 else (2 if h0 < 480 else 1)
    opts = vision.PoseLandmarkerOptions(base_options=mpt.BaseOptions(model_asset_path=f'{ROOT}/models/pose_landmarker_heavy.task'),
        running_mode=vision.RunningMode.IMAGE, num_poses=3, min_pose_detection_confidence=0.3,
        min_pose_presence_confidence=0.3, min_tracking_confidence=0.3)
    lm = vision.PoseLandmarker.create_from_options(opts)
    T = len(frames); Wr = np.full((T,33,3), np.nan); Ir = np.full((T,33,2), np.nan); V = np.zeros((T,33)); det = np.zeros(T, bool)
    big = []; prev_box = None; rsize = None; hist_sz = []; miss = 0; seed = C.get('seed')  # seed = [u,v] normalised point near the subject
    def run(img, box):  # box = (x0,y0,x1,y1) in source px; returns list of (img_uv_src, world, vis)
        x0, y0, x1, y1 = [int(round(v)) for v in box]
        crop = img[max(0,y0):min(h0,y1), max(0,x0):min(w0,x1)]
        if crop.size == 0: return []
        ch_, cw_ = crop.shape[:2]; sc = 512.0/max(ch_, cw_)
        cb = cv2.resize(crop, (max(1,int(cw_*sc)), max(1,int(ch_*sc))), interpolation=cv2.INTER_CUBIC)
        pad = np.zeros((512, 512, 3), np.uint8); pad[:cb.shape[0], :cb.shape[1]] = cb
        res = lm.detect(mp.Image(image_format=mp.ImageFormat.SRGB, data=cv2.cvtColor(pad, cv2.COLOR_BGR2RGB)))
        out = []
        for p, w in zip(res.pose_landmarks, res.pose_world_landmarks):
            uv = np.array([[ (q.x*512/sc + max(0,x0))/w0, (q.y*512/sc + max(0,y0))/h0 ] for q in p])
            out.append((uv, np.array([[q.x,q.y,q.z] for q in w]), np.array([q.visibility for q in p])))
        return out
    def csize(c):
        uv = c[0]*[w0, h0]   # torso length in px: pose-invariant (crouch/tuck safe) subject scale
        return float(np.linalg.norm((uv[11]+uv[12])/2 - (uv[23]+uv[24])/2) + 1e-3)
    def pick(cands, ref, rsize=None):
        cands = [c for c in cands if c[2][11:].mean() >= 0.25 and np.ptp(c[0][:,1]) < 1.2 and np.ptp(c[0][:,0]) < 1.2]
        if rsize: cands = [c for c in cands if 0.62 < csize(c)/rsize < 1.6]   # hard reject: much smaller/larger person = someone else
        if not cands: return None
        def key(c):
            uv, _, vis = c; good = vis[11:] > 0.3
            ctr = uv[11:][good].mean(0) if good.any() else uv.mean(0)
            d = np.linalg.norm(ctr - ref) if ref is not None else 0.0
            sz = 0.8*abs(math.log(csize(c)/rsize)) if rsize else 0.0   # subject size continuity (rejects small bystanders)
            return d + sz - 0.6*vis[11:].mean()
        return min(cands, key=key)
    for i, fr in enumerate(frames):
        b = cv2.resize(fr, (w0*up, h0*up), interpolation=cv2.INTER_CUBIC); big.append(b)
        ref = None; cand = None
        if prev_box is not None:
            cx, cy, half = prev_box
            ref = np.array([cx/w0, cy/h0])
            cand = pick(run(fr, (cx-half, cy-half, cx+half, cy+half)), ref, rsize)
            if cand is not None and rsize and not (0.6 < csize(cand)/rsize < 1.7):
                # size jump inside the ROI: likely a bystander; re-check the whole frame with the size prior
                alt = pick(run(fr, (0, 0, w0, h0)) + [cand], ref, rsize)
                cand = alt
        if cand is None:
            r0 = ref if ref is not None else (np.array(seed) if seed else None)
            cands = []
            if ref is None and seed and i < 3:   # start-up: look around the seed point first (small subject)
                sx, sy, hh = seed[0]*w0, seed[1]*h0, 0.3*h0
                cands = [c for c in run(fr, (sx-hh, sy-hh, sx+hh, sy+hh)) if np.linalg.norm(c[0][11:].mean(0) - r0) < 0.15]
            if not cands: cands = run(fr, (0, 0, w0, h0))
            if not cands:
                for x0 in (0, w0/4, w0/2):
                    cands += run(fr, (x0, 0, x0 + w0/2, h0))
            cand = pick(cands, r0, rsize)
            if cand is not None and ref is not None:
                uv = cand[0]; ctr = uv[11:].mean(0)
                if np.linalg.norm(ctr - ref) > 0.35: cand = None  # jumped to someone else
        if cand is not None:
            uv, w, vis = cand
            Ir[i] = uv; Wr[i] = w; V[i] = vis; det[i] = True
            g = vis > 0.2; pts = uv[g] if g.sum() >= 5 else uv
            px = pts*[w0, h0]; cx, cy = px.mean(0); ext = max(np.ptp(px[:,0]), np.ptp(px[:,1]))
            prev_box = (cx, cy, max(ext*0.95, 0.12*h0))
            cs = csize(cand); hist_sz.append(cs); rsize = float(np.median(hist_sz[-5:])); miss = 0
        else:
            miss += 1
            if miss >= 6: rsize = None; hist_sz = []   # lost for ~0.2 s: drop the size prior so we can re-acquire
            prev_box = None if (i > 0 and not det[i-1] and prev_box is not None and i > 1 and not det[i-2]) else prev_box
    # gap fill (<= 0.2 s) then one-euro smoothing
    maxgap = max(1, int(round(0.12*fps)))
    def same_subject(a, b):  # don't bridge a gap whose endpoints are far apart in the image (identity switch)
        ha = (Ir[a,23]+Ir[a,24])/2*[w0,h0]; hb = (Ir[b,23]+Ir[b,24])/2*[w0,h0]
        ta = np.linalg.norm((Ir[a,11]+Ir[a,12])/2*[w0,h0] - ha)
        return np.linalg.norm(ha-hb) < 0.5*ta*(b-a)
    Wf, filled = interp_gaps(Wr, det, maxgap, same_subject); If, _ = interp_gaps(Ir, det, maxgap, same_subject)
    Vf, _ = interp_gaps(V, det, maxgap, same_subject)
    fw = OneEuro(fps, mincut=1.5, beta=0.6); fi = OneEuro(fps, mincut=1.5, beta=8.0)
    Ws = np.full_like(Wf, np.nan); Is = np.full_like(If, np.nan); prev = False
    for i in range(T):
        if filled[i]:
            if not prev: fw.x = None; fi.x = None
            Ws[i] = fw(Wf[i]); Is[i] = fi(If[i])
        prev = bool(filled[i])
    mask = (Vf >= VIS_T) & filled[:, None]
    # derived
    ANG, root_h, img_hip = [], [], []
    torso_px = []
    for i in range(T):
        if not filled[i]: ANG.append(None); root_h.append(None); img_hip.append(None); continue
        W = Ws[i]; a = angles(W)
        for k, deps in JOINT_DEPS.items():
            for side, d in (("L", deps[:len(deps)//2]), ("R", deps[len(deps)//2:])):
                if not all(mask[i, j] for j in d): a[f'{k}_{side}'] = a[f'{k}_{side}']  # keep value; mask tells confidence
        ANG.append(a)
        hipm = (W[23]+W[24])/2; feet_y = max(W[j,1] for j in (27,28,29,30,31,32))
        root_h.append(round(float(feet_y - hipm[1]), 4))
        ih = (Is[i,23]+Is[i,24])/2*[w0, h0]; img_hip.append([round(float(ih[0]),2), round(float(ih[1]),2)])
        torso_px.append(np.linalg.norm(((Is[i,11]+Is[i,12])/2 - (Is[i,23]+Is[i,24])/2)*[w0, h0]))
    tpx = float(np.median(torso_px)) if torso_px else float('nan')
    torso_m = float(np.nanmedian([np.linalg.norm((Ws[i,11]+Ws[i,12])/2-(Ws[i,23]+Ws[i,24])/2) for i in range(T) if filled[i]]))
    vel = []
    for i in range(T):
        j0, j1 = i-1, i+1
        if 0 <= j0 and j1 < T and img_hip[j0] and img_hip[j1]:
            d = (np.array(img_hip[j1]) - np.array(img_hip[j0]))/(times[j1]-times[j0])
            vel.append([round(float(d[0]/tpx*torso_m),3), round(float(d[1]/tpx*torso_m),3)])
        else: vel.append(None)
    out_json = f'{ROOT}/out/json/{cid}.json'; os.makedirs(os.path.dirname(out_json), exist_ok=True)
    rnd = lambda a, n: np.where(np.isnan(a), None, np.round(a, n)).tolist()
    doc = {
      'clip_id': cid, 'source': {'url': f'https://www.youtube.com/watch?v={C["vid"]}&t={int(C["t0"])}s', 'video_id': C['vid'], 'title': meta['title'],
        'channel': meta.get('channel'), 'upload_date': meta.get('upload_date'), 't_start_s': C['t0'], 't_end_s': C['t1']},
      'move': C['move'], 'game_verb': C['verb'], 'camera_view': C['view'],
      'fps': round(fps, 4), 'frame_count': T, 'detected_frames': int(det.sum()), 'usable_frames': int(filled.sum()),
      'proxy_resolution': [w0, h0], 'source_video': src_kind, 'scene_cut_truncated_at_s': (round(times[-1],3) if cut_at is not None else None), 'inference_upscale': up,
      'model': 'MediaPipe PoseLandmarker heavy (float16), IMAGE mode on ROI crops, num_poses=3 (subject picked by ROI + size continuity)',
      'smoothing': {'gap_fill': f'linear, gaps <= {maxgap} frames (~0.12 s)', 'detector': 'per-frame IMAGE mode on a 512px crop around the previous detection (ROI tracking), full-frame fallback; heavy model', 'filter': 'one-euro', 'world': {'min_cutoff': 1.5, 'beta': 0.6}, 'image': {'min_cutoff': 1.5, 'beta': 8.0}},
      'coordinate_notes': 'world = MediaPipe world landmarks in metres, origin at hip centre, camera-aligned axes (x right, y DOWN, z toward camera is negative). Not gravity-aligned when camera is tilted. image = normalised [0,1] coords in source frame.',
      'angle_notes': 'degrees. hip/shoulder flex: + = limb forward of torso (signed in body frame); abd: + = away from midline; knee/elbow/wrist flex: 0 = straight; ankle_dorsi: + = toes toward shin (approx, 90 - knee-ankle-toe angle); spine_twist: shoulder line vs hip line about torso axis; trunk_lean_from_cam_vertical: torso vs camera up; neck_flex: + = head forward.',
      'root_notes': 'root_height_above_feet_m = vertical (camera y) distance hip-centre to lowest foot point, from world landmarks (crouch/extension proxy, not height above ground). root_img_px = hip centre in source-video pixels (see proxy_resolution). root_vel_proxy_mps = image-space hip velocity scaled by torso length (includes camera pan; NOT true ground speed).',
      'landmark_names': NAMES,
      'times_s': [round(t, 4) for t in times],
      'detected': det.tolist(), 'usable': filled.tolist(),
      'world_xyz_m': rnd(Ws, 4), 'world_xyz_m_raw': rnd(Wr, 4), 'image_uv': rnd(Is, 4),
      'visibility': np.round(Vf, 3).tolist(), 'confidence_mask': mask.tolist(),
      'joint_angles_deg': ANG, 'root_height_above_feet_m': root_h, 'root_img_px': img_hip, 'root_vel_proxy_mps': vel,
      'quality': {'mean_visibility_body': round(float(np.mean(Vf[filled][:, 11:])) if filled.any() else 0, 3),
                  'frac_usable': round(float(filled.mean()), 3), 'frac_joints_confident': round(float(mask[:, 11:].mean()), 3)}}
    json.dump(doc, open(out_json, 'w'), separators=(',', ':'))
    # contact sheet: 8 key frames, evenly spaced over usable frames
    us = np.where(filled)[0]
    if len(us) == 0: print(cid, 'NO DETECTIONS'); return
    keys = us[np.linspace(0, len(us)-1, 8).astype(int)]
    tiles = []
    for k in keys:
        b = big[k].copy(); P = Is[k]*[b.shape[1], b.shape[0]]; draw_skel(b, P, Vf[k], thick=2)
        cv2.putText(b, f't={times[k]:.2f}s', (8, 24), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255,255,255), 2)
        tiles.append(cv2.resize(b, (480, int(480*b.shape[0]/b.shape[1]))))
    sheet = np.vstack([np.hstack(tiles[:4]), np.hstack(tiles[4:])])
    head = np.full((34, sheet.shape[1], 3), 20, np.uint8)
    cv2.putText(head, f'{cid} | {meta["title"][:60]} | {C["vid"]} {C["t0"]}-{C["t1"]}s | verb: {C["verb"]} | usable {filled.mean()*100:.0f}% vis {doc["quality"]["mean_visibility_body"]:.2f}', (8, 23), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255,255,255), 1)
    os.makedirs(f'{ROOT}/out/contact', exist_ok=True); cv2.imwrite(f'{ROOT}/out/contact/{cid}.png', np.vstack([head, sheet]))
    # stick strip: 12 frames, world coords camera-plane, feet on common baseline
    keys = us[np.linspace(0, len(us)-1, 12).astype(int)]
    cw, ch, s = 160, 300, 120.0
    strip = np.full((ch, cw*len(keys), 3), 255, np.uint8)
    for n, k in enumerate(keys):
        W = Ws[k]; feet_y = float(np.nanmax(W[:,1]))
        P = np.stack([cw*n + cw/2 + W[:,0]*s, (ch-30) + (W[:,1]-feet_y)*s], 1)
        cv2.line(strip, (cw*n+10, ch-30), (cw*n+cw-10, ch-30), (200,200,200), 1)
        img = strip; draw_skel(img, P, Vf[k], thick=3)
        cv2.circle(img, tuple(int(x) for x in (P[0])), 9, (60,60,60), 2, cv2.LINE_AA)
        cv2.putText(img, f'{times[k]-C["t0"]:.2f}s', (cw*n+6, ch-10), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (80,80,80), 1)
    lab = np.full((26, strip.shape[1], 3), 255, np.uint8)
    cv2.putText(lab, f'{cid}  ({C["verb"]})  stick view = camera plane, lowest body point on baseline; orange=left, blue=right', (6, 18), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0,0,0), 1)
    os.makedirs(f'{ROOT}/out/strips', exist_ok=True); cv2.imwrite(f'{ROOT}/out/strips/{cid}.png', np.vstack([lab, strip]))
    print(cid, 'frames', T, 'det', int(det.sum()), 'usable', int(filled.sum()), 'vis', doc['quality'])

if __name__ == '__main__':
    main(sys.argv[1])
