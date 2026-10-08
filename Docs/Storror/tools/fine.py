import cv2, numpy as np, os, json, sys
C=json.load(open(sys.argv[1])); outp=sys.argv[2]
def src(v):
    p=f'/workspace/storror/scout/{v}_h264.mp4'
    return p if os.path.exists(p) else [x for x in [f'/workspace/storror/scout/{v}.mp4',f'/workspace/storror/scout/{v}.mp4.part'] if os.path.exists(x)][0]
blocks=[]
for k,(v,t0,t1,lab) in enumerate(C):
    cap=cv2.VideoCapture(src(v)); tiles=[]
    for t in np.arange(t0,t1,1/6):
        cap.set(cv2.CAP_PROP_POS_MSEC,t*1000); ok,fr=cap.read()
        if not ok: break
        fr=cv2.resize(fr,(240,135)); cv2.putText(fr,f"{t:.2f}",(3,14),cv2.FONT_HERSHEY_SIMPLEX,0.45,(0,255,255),1); tiles.append(fr)
    cols=8
    while len(tiles)%cols: tiles.append(np.zeros_like(tiles[0]))
    g=np.vstack([np.hstack(tiles[i:i+cols]) for i in range(0,len(tiles),cols)])
    l=np.zeros((22,g.shape[1],3),np.uint8); cv2.putText(l,f"{k}: {v} {t0}-{t1} {lab}",(4,16),cv2.FONT_HERSHEY_SIMPLEX,0.55,(255,255,255),1)
    blocks.append(np.vstack([l,g]))
cv2.imwrite(outp,np.vstack(blocks),[cv2.IMWRITE_JPEG_QUALITY,78]); print(outp)
