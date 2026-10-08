import json, cv2, numpy as np, os
W=json.load(open('/workspace/storror/windows.json'))[:60]
src={}
for v in set(w['vid'] for w in W):
    p=f'/workspace/storror/scout/{v}_h264.mp4'
    if not os.path.exists(p):
        p=[f'/workspace/storror/scout/{v}.mp4',f'/workspace/storror/scout/{v}.mp4.part']
        p=[x for x in p if os.path.exists(x)][0]
    src[v]=p
os.makedirs('/workspace/storror/review',exist_ok=True)
rows=[]
for k,w in enumerate(W):
    cap=cv2.VideoCapture(src[w['vid']]); tiles=[]
    for t in np.linspace(w['t0']-0.3,w['t1']+0.3,8):
        cap.set(cv2.CAP_PROP_POS_MSEC,max(t,0)*1000); ok,fr=cap.read()
        fr=cv2.resize(fr if ok else np.zeros((144,256,3),np.uint8),(224,126))
        tiles.append(fr)
    strip=np.hstack(tiles)
    lab=np.zeros((22,strip.shape[1],3),np.uint8)
    cv2.putText(lab,f"#{k} {w['vid']} {w['t0']:.1f}-{w['t1']:.1f}s",(4,16),cv2.FONT_HERSHEY_SIMPLEX,0.55,(255,255,255),1)
    rows.append(np.vstack([lab,strip]))
for s in range(0,len(rows),6):
    cv2.imwrite(f'/workspace/storror/review/sheet_{s//6:02d}.jpg',np.vstack(rows[s:s+6]),[cv2.IMWRITE_JPEG_QUALITY,80])
print('ok')
