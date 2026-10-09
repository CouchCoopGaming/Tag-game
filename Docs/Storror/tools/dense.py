import cv2, numpy as np, sys, os
vid, t0, t1 = sys.argv[1], float(sys.argv[2]), float(sys.argv[3]); fps_s=float(sys.argv[4]) if len(sys.argv)>4 else 2
p=f'/workspace/storror/scout/{vid}_h264.mp4'
if not os.path.exists(p): p=[x for x in [f'/workspace/storror/scout/{vid}.mp4',f'/workspace/storror/scout/{vid}.mp4.part'] if os.path.exists(x)][0]
cap=cv2.VideoCapture(p); tiles=[]
for t in np.arange(t0,t1,1/fps_s):
    cap.set(cv2.CAP_PROP_POS_MSEC,t*1000); ok,fr=cap.read()
    if not ok: break
    fr=cv2.resize(fr,(192,108)); cv2.putText(fr,f"{t:.1f}",(3,14),cv2.FONT_HERSHEY_SIMPLEX,0.45,(0,255,255),1); tiles.append(fr)
cols=10
while len(tiles)%cols: tiles.append(np.zeros_like(tiles[0]))
img=np.vstack([np.hstack(tiles[i:i+cols]) for i in range(0,len(tiles),cols)])
os.makedirs('/workspace/storror/review/dense',exist_ok=True)
out=f'/workspace/storror/review/dense/{vid}_{int(t0)}_{int(t1)}.jpg'; cv2.imwrite(out,img,[cv2.IMWRITE_JPEG_QUALITY,75]); print(out)
