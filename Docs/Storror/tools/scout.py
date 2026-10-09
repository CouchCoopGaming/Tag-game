import sys, json, cv2, numpy as np, mediapipe as mp
from mediapipe.tasks import python as mpt
from mediapipe.tasks.python import vision
path, out = sys.argv[1], sys.argv[2]
step_s = 1/6
opts = vision.PoseLandmarkerOptions(base_options=mpt.BaseOptions(model_asset_path='/workspace/storror/models/pose_landmarker_full.task'),
    running_mode=vision.RunningMode.IMAGE, num_poses=3, min_pose_detection_confidence=0.4)
lm = vision.PoseLandmarker.create_from_options(opts)
cap = cv2.VideoCapture(path); fps = cap.get(cv2.CAP_PROP_FPS) or 30
rows=[]; i=0; nxt=0.0
while True:
    ok, fr = cap.read()
    if not ok: break
    t = i/fps; i+=1
    if t < nxt: continue
    nxt += step_s
    h,w = fr.shape[:2]
    big = cv2.resize(fr, (w*3, h*3), interpolation=cv2.INTER_CUBIC)
    res = lm.detect(mp.Image(image_format=mp.ImageFormat.SRGB, data=cv2.cvtColor(big, cv2.COLOR_BGR2RGB)))
    n = len(res.pose_landmarks)
    rec={'t':round(t,2),'n':n}
    if n:
        p = res.pose_landmarks[0]
        xs=np.array([q.x for q in p]); ys=np.array([q.y for q in p]); vs=np.array([q.visibility for q in p])
        rec.update(vis=float(vs[11:].mean()), h=float(ys.max()-ys.min()), cx=float(xs.mean()), cy=float(ys.mean()),
                   sw=float(abs(xs[11]-xs[12])), hw=float(abs(xs[23]-xs[24])))
    rows.append(rec)
json.dump({'fps':fps,'rows':rows}, open(out,'w'))
print(path, len(rows))
