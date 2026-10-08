import json,glob,numpy as np
rows=[]
for f in sorted(glob.glob('out/json/*.json')):
    d=json.load(open(f)); res=np.array(d['proxy_resolution'],float); cid=d['clip_id']
    for i in range(len(d['times_s'])):
        if not d['detected'][i] or d['image_uv'][i][0][0] is None: continue
        v=np.array(d['visibility'][i]); 
        if min(v[[11,12,23,24,25,26,27,28]])<0.8: continue
        q=np.array(d['image_uv'][i],float)*res
        sh=(q[11]+q[12])/2; hp=(q[23]+q[24])/2
        tor=np.linalg.norm(sh-hp)
        # upright: torso within 15deg of image vertical
        if abs(np.degrees(np.arctan2(sh[0]-hp[0], hp[1]-sh[1])))>15: continue
        legs=[]
        ok=True
        for h,k,a in ((23,25,27),(24,26,28)):
            u=q[k]-q[h]; w=q[a]-q[k]
            ang=np.degrees(np.arccos(np.dot(u,w)/np.linalg.norm(u)/np.linalg.norm(w)))
            if ang>15: ok=False
            legs.append((np.linalg.norm(u),np.linalg.norm(w)))
        if not ok: continue
        th=np.mean([l[0] for l in legs]); sn=np.mean([l[1] for l in legs])
        sw=np.linalg.norm(q[11]-q[12]); hw=np.linalg.norm(q[23]-q[24])
        rows.append((cid,d['times_s'][i],tor,th,sn,sw,hw))
print(len(rows), sorted(set(r[0] for r in rows)))
a=np.array([r[2:] for r in rows])
tor,th,sn,sw,hw=a.T
print('leg/torso 2D median',np.median((th+sn)/tor).round(3),'p25',np.percentile((th+sn)/tor,25).round(3),'p75',np.percentile((th+sn)/tor,75).round(3))
print('shin/thigh 2D',np.median(sn/th).round(3))
fr=sw/tor>0.55  # frontal-ish frames
print('frontal n',fr.sum(),'shoulder/hip width',np.median(sw[fr]/hw[fr]).round(3),'shoulderW/torso',np.median(sw[fr]/tor[fr]).round(3),'hipW/torso',np.median(hw[fr]/tor[fr]).round(3))
from collections import Counter; print(Counter(r[0] for r in rows))
