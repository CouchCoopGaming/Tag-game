import json, glob, os, numpy as np
out=[]
for f in sorted(glob.glob('/workspace/storror/scoutpose/*.json')):
    vid=os.path.basename(f)[:-5]; rows=json.load(open(f))['rows']
    good=[(r['n']>=1 and r.get('vis',0)>=0.55 and r.get('h',0)>=0.28) for r in rows]
    i=0
    while i<len(rows):
        if not good[i]: i+=1; continue
        j=i; miss=0
        while j+1<len(rows) and (good[j+1] or (j+2<len(rows) and good[j+2])): j+=1
        seg=[r for r,g in zip(rows[i:j+1],good[i:j+1]) if g]
        dur=rows[j]['t']-rows[i]['t']
        if dur>=1.2:
            cx=np.array([r['cx'] for r in seg]); cy=np.array([r['cy'] for r in seg]); h=np.array([r['h'] for r in seg])
            single=np.mean([r['n']==1 for r in seg])
            motion=float(np.ptp(cy)+np.std(np.diff(cy))*5+np.ptp(h/h.mean())*0.5)
            out.append(dict(vid=vid,t0=rows[i]['t'],t1=rows[j]['t'],dur=round(dur,1),vis=round(float(np.mean([r['vis'] for r in seg])),2),
                h=round(float(h.mean()),2),single=round(float(single),2),motion=round(motion,2)))
        i=j+1
for w in out: w['score']=round(w['vis']*min(w['h'],0.8)*w['single']*(0.3+w['motion'])*min(w['dur'],6),3)
out.sort(key=lambda w:-w['score'])
json.dump(out,open('/workspace/storror/windows.json','w'),indent=0)
print(len(out))
for w in out[:70]: print(w)
