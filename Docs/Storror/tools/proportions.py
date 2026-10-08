"""Aggregate body proportions + joint ROM from extracted clips; compare to Hier v0.7.8 rig."""
import json, glob, os, math, numpy as np
ROOT='/workspace/storror'
EXCLUDE={}
# windows inside a clip that are known-bad (camera whip / subject leaving frame), from contact sheets + root-jump scan
BAD_WINDOWS={'05_window_jump_land_run':[(184.25,184.5)]}
VIS=0.70
SEG={'upper_arm':[(11,13),(12,14)],'lower_arm':[(13,15),(14,16)],'thigh':[(23,25),(24,26)],'shin':[(25,27),(26,28)],
     'shoulder_width':[(11,12)],'hip_width':[(23,24)],'ear_to_ear':[(7,8)],'foot_heel_to_toe':[(29,31),(30,32)],
     'ankle_to_heel':[(27,29),(28,30)]}
def mid(W,a,b): return (W[a]+W[b])/2
def wrap(v, lo):  # unwrap angle so values below lo get +360 (shoulder/hip flex past overhead)
    return v+360 if v is not None and v<lo else v

per_clip={}; rom_rows=[]; frames_used={}
for f in sorted(glob.glob(f'{ROOT}/out/json/*.json')):
    d=json.load(open(f)); cid=d['clip_id']
    if cid in EXCLUDE: continue
    T=len(d['times_s']); t=np.array(d['times_s'])
    raw=d['world_xyz_m_raw']; vis=np.array(d['visibility'],float); uv=d['image_uv']; res=d['proxy_resolution']
    good=np.zeros(T,bool)
    for i in range(T):
        if not d['detected'][i] or raw[i][0][0] is None: continue
        if any(a<=t[i]<=b for a,b in BAD_WINDOWS.get(cid,[])): continue
        good[i]=True
    # continuity gate: hip-centre jump (px) vs torso length (px) to prev/next detected frame
    P=[None]*T; L=[None]*T
    for i in range(T):
        if uv[i][0][0] is None: continue
        q=np.array(uv[i],float)*np.array(res,float)
        P[i]=(q[23]+q[24])/2; L[i]=np.linalg.norm((q[11]+q[12])/2-P[i])
    for i in range(T):
        if not good[i] or P[i] is None: good[i]=False; continue
        for j in (i-1,i+1):
            if 0<=j<T and P[j] is not None and np.linalg.norm(P[j]-P[i])>0.6*max(L[i],1e-3): good[i]=False
    segs={k:[] for k in SEG}; chain={k:[] for k in ('torso','shoulder_to_ear')}; leftright_flip=0
    for i in np.where(good)[0]:
        W=np.array(raw[i],float); v=vis[i]
        # L/R flip guard: hip line vs shoulder line nearly opposite -> skip frame
        ja=d['joint_angles_deg'][i]
        if ja and ja.get('spine_twist') is not None and abs(ja['spine_twist'])>100: leftright_flip+=1; continue
        for k,pairs in SEG.items():
            for a,b in pairs:
                if v[a]>=VIS and v[b]>=VIS: segs[k].append(float(np.linalg.norm(W[a]-W[b])))
        if min(v[11],v[12],v[23],v[24])>=VIS: chain['torso'].append(float(np.linalg.norm(mid(W,11,12)-mid(W,23,24))))
        if min(v[11],v[12],v[7],v[8])>=VIS: chain['shoulder_to_ear'].append(float(np.linalg.norm(mid(W,7,8)-mid(W,11,12))))
        if ja:
            r={'clip':cid,'verb':d['game_verb']}
            for k,val in ja.items():
                if val is None: continue
                if k.startswith('shoulder_flex'): val=wrap(val,-120)
                if k.startswith('hip_flex'): val=wrap(val,-120)
                r[k]=val
            # only keep limb angles whose landmarks are confident
            m=d['confidence_mask'][i]
            for side,idx in (('L',(11,13,15,23,25,27,31,19)),('R',(12,14,16,24,26,28,32,20))):
                s,e,w,h,k_,a,fi,ix=idx
                need={'hip_flex':(h,k_),'hip_abd':(h,k_),'knee_flex':(h,k_,a),'ankle_dorsi':(k_,a,fi),'shoulder_flex':(s,e),
                      'shoulder_abd':(s,e),'elbow_flex':(s,e,w),'wrist_flex':(e,w,ix)}
                for j,ids in need.items():
                    if any(v[x]<VIS for x in ids): r.pop(f'{j}_{side}',None)
            if any(v[x]<VIS for x in (7,8,11,12)): r.pop('neck_flex',None); r.pop('neck_lateral',None)
            # robust re-derivation: elevation (0 = hanging/along -torso axis, 180 = overhead / along +torso axis) and
            # lateral abduction = asin(component of limb unit vector along body 'out' axis), -90..90 (no atan2 blow-up near 90 flex)
            hipm=mid(W,23,24); shm=mid(W,11,12); up=shm-hipm; up/=np.linalg.norm(up)
            right=W[24]-W[23]; right-=up*np.dot(right,up); right/=np.linalg.norm(right)
            for side,(s_,e_,h_,k2) in (('L',(11,13,23,25)),('R',(12,14,24,26))):
                out=-right if side=='L' else right
                for nm,(a_,b_) in (('hip',(h_,k2)),('shoulder',(s_,e_))):
                    if v[a_]<VIS or v[b_]<VIS: continue
                    u=W[b_]-W[a_]; u/=np.linalg.norm(u)
                    r[f'{nm}_elev_{side}']=round(float(np.degrees(np.arccos(np.clip(-np.dot(u,up),-1,1)))),2)
                    r[f'{nm}_abd_lat_{side}']=round(float(np.degrees(np.arcsin(np.clip(np.dot(u,out),-1,1)))),2)
                r.pop(f'hip_abd_{side}',None); r.pop(f'shoulder_abd_{side}',None)
            rom_rows.append(r)
    frames_used[cid]=dict(total=T,clean=int(good.sum()),lr_flip_skipped=leftright_flip)
    out={}
    for k,arr in {**segs,**chain}.items():
        if len(arr)>=5: out[k]=dict(median=round(float(np.median(arr)),4),p25=round(float(np.percentile(arr,25)),4),p75=round(float(np.percentile(arr,75)),4),n=len(arr))
    per_clip[cid]=out

keys=list(SEG)+['torso','shoulder_to_ear']
agg={}
for k in keys:
    vals=[per_clip[c][k]['median'] for c in per_clip if k in per_clip[c]]
    w=[per_clip[c][k]['n'] for c in per_clip if k in per_clip[c]]
    if vals: agg[k]=dict(median_of_clips=round(float(np.median(vals)),4),weighted_mean=round(float(np.average(vals,weights=w)),4),
                          clip_min=round(min(vals),4),clip_max=round(max(vals),4),n_clips=len(vals),n_frames=int(sum(w)))
m={k:agg[k]['median_of_clips'] for k in agg}
# Stature reconstruction (MediaPipe has no crown/sole landmarks):
#   H = ankle_height + shin + thigh + torso(hip-mid->shoulder-mid) + shoulder->ear + ear->crown
#   ankle_height ~0.039H and ear->crown ~0.070H (Winter 2009 anthropometry; tragion ~ eye level 0.936H, crown 1.0H)
chain_len=m['shin']+m['thigh']+m['torso']+m['shoulder_to_ear']
H=chain_len/(1-0.039-0.070)
# head height (chin->crown) from bitragion breadth: adult ratio head height / bitragion breadth ~ 1.59 (23.0cm / 14.5cm, ANSUR-style means)
head_h=m['ear_to_ear']*1.59
storror={'stature_est_m':round(H,3),
  'ratios_to_height':{k:round(m[k]/H,4) for k in ['torso','upper_arm','lower_arm','thigh','shin','shoulder_width','hip_width','shoulder_to_ear','ear_to_ear']},
}
storror['ratios_to_height']['head_height_est']=round(head_h/H,4)
storror['ratios_to_height']['leg_thigh_plus_shin']=round((m['thigh']+m['shin'])/H,4)
storror['ratios_to_height']['arm_upper_plus_lower']=round((m['upper_arm']+m['lower_arm'])/H,4)
storror['ratios_height_free']={'lower_over_upper_arm':round(m['lower_arm']/m['upper_arm'],3),'shin_over_thigh':round(m['shin']/m['thigh'],3),
  'shoulder_over_hip_width':round(m['shoulder_width']/m['hip_width'],3),'leg_over_torso':round((m['thigh']+m['shin'])/m['torso'],3),
  'arm_over_leg':round((m['upper_arm']+m['lower_arm'])/(m['thigh']+m['shin']),3)}

# ---- Hier v0.7.8 (Tools/Tag/build_mannequin_hier.py + Dummy_Mannequin_Tan_Hier_Hi.fbx armature, metres, Z up) ----
hier_joint={'hip_z':1.05,'shoulder_z':1.40,'hip_x':0.118,'shoulder_x':0.235,'ua':0.370,'la':0.330,
            'thigh':math.sqrt(0.54**2+0.012**2+0.02**2),'shin':0.490,'ankle_z':0.02,'sole_z':0.02-0.050-0.011,
            'crown_z':1.665+0.150,'chin_z':1.555-0.016,'head_breadth':0.250,'ear_z':1.665}
hz=hier_joint; Hh=hz['crown_z']-hz['sole_z']
torso_h=math.hypot(1.40-1.05,0.06-0.02)
hier={'stature_m':round(Hh,3),'source':'Tools/Tag/build_mannequin_hier.py v0.7.8 constants (SHOULDER_X/Z/Y, UA_LEN, LA_LEN, HIP_X/Z, UL_LEN, LL_LEN, Head sph, shoe_foot sole) cross-checked against Dummy_Mannequin_Tan_Hier_Hi.fbx bone offsets (UpperLeg 0.5405, LowerLeg 0.49, UpperArm 0.37, LowerArm 0.33, Shoulder bone 0.1188 from x=0.12 -> joint x=0.235)',
  'lengths_m':{'torso':round(torso_h,4),'upper_arm':hz['ua'],'lower_arm':hz['la'],'thigh':round(hz['thigh'],4),'shin':hz['shin'],
               'shoulder_width':2*hz['shoulder_x'],'hip_width':2*hz['hip_x'],'shoulder_to_ear':round(hz['ear_z']-hz['shoulder_z'],4),
               'ear_to_ear':hz['head_breadth'],'head_height':round(hz['crown_z']-hz['chin_z'],4),'ankle_height':round(hz['ankle_z']-hz['sole_z'],4)}}
hl=hier['lengths_m']
hier['ratios_to_height']={k:round(hl[k]/Hh,4) for k in ['torso','upper_arm','lower_arm','thigh','shin','shoulder_width','hip_width','shoulder_to_ear','ear_to_ear']}
hier['ratios_to_height']['head_height_est']=round(hl['head_height']/Hh,4)
hier['ratios_to_height']['leg_thigh_plus_shin']=round((hl['thigh']+hl['shin'])/Hh,4)
hier['ratios_to_height']['arm_upper_plus_lower']=round((hl['upper_arm']+hl['lower_arm'])/Hh,4)
hier['ratios_height_free']={'lower_over_upper_arm':round(hl['lower_arm']/hl['upper_arm'],3),'shin_over_thigh':round(hl['shin']/hl['thigh'],3),
  'shoulder_over_hip_width':round(hl['shoulder_width']/hl['hip_width'],3),'leg_over_torso':round((hl['thigh']+hl['shin'])/hl['torso'],3),
  'arm_over_leg':round((hl['upper_arm']+hl['lower_arm'])/(hl['thigh']+hl['shin']),3)}
# Reference anthropometry (Winter, Biomechanics & Motor Control of Human Movement, segment lengths as fraction of H)
winter={'upper_arm':0.186,'lower_arm':0.146,'thigh':0.245,'shin':0.246,'head_height_est':0.130,
        'torso_joint_centres_approx':0.288-0.03,'shoulder_width_biacromial':0.259,'hip_width_bi_iliac':0.191}

# suggested deltas: keep Hier stature, move each length to the Storror ratio; blend option 50% to keep stylisation
sugg={}
for k in ['torso','upper_arm','lower_arm','thigh','shin','shoulder_width','hip_width','head_height_est']:
    tgt=storror['ratios_to_height'][k]*Hh; cur=(hl['head_height'] if k=='head_height_est' else hl[k])
    sugg[k]=dict(hier_m=round(cur,3),storror_ratio_target_m=round(tgt,3),delta_full_m=round(tgt-cur,3),delta_pct=round(100*(tgt-cur)/cur,1),
                 delta_half_blend_m=round((tgt-cur)/2,3))

# ROM
J=sorted({k for r in rom_rows for k in r if k not in('clip','verb')})
def stats(vals):
    a=np.array(vals,float)
    return dict(p2=round(float(np.percentile(a,2)),1),p10=round(float(np.percentile(a,10)),1),p50=round(float(np.percentile(a,50)),1),
                p90=round(float(np.percentile(a,90)),1),p98=round(float(np.percentile(a,98)),1),n=len(a))
rom_all={}
for j in J:
    vals=[r[j] for r in rom_rows if j in r]
    if len(vals)>=20: rom_all[j]=stats(vals)
# merge L/R
rom_lr={}
for j in sorted({k[:-2] for k in J if k.endswith(('_L','_R'))}):
    vals=[r[k] for r in rom_rows for k in (j+'_L',j+'_R') if k in r]
    if len(vals)>=20: rom_lr[j]=stats(vals)
for j in ['trunk_lean_from_cam_vertical','spine_twist','spine_lateral_bend','neck_flex','neck_lateral']:
    if j in rom_all: rom_lr[j]=rom_all[j]
rom_verb={}
for vb in sorted({r['verb'] for r in rom_rows}):
    rr=[r for r in rom_rows if r['verb']==vb]; rom_verb[vb]={}
    for j in ['hip_flex','hip_elev','knee_flex','ankle_dorsi','shoulder_flex','shoulder_elev','shoulder_abd_lat','elbow_flex']:
        vals=[r[k] for r in rr for k in (j+'_L',j+'_R') if k in r]
        if len(vals)>=15: rom_verb[vb][j]=stats(vals)

doc={'generated':'2026-10-08','method':{
  'source':'MediaPipe PoseLandmarker heavy world landmarks (raw, unsmoothed) on 720p YouTube segments (480p for the 2011 episode: clips 01-03)',
  'frame_gate':f'detected (not interpolated) frames, outside known subject-swap windows, hip-centre jump < 0.6 torso length to neighbours, |spine_twist|<=100 (L/R flip guard); each segment only when both endpoint visibilities >= {VIS}',
  'aggregation':'per-clip median length per segment (L/R pooled) -> median across clips',
  'stature':'H reconstructed = (shin+thigh+torso+shoulder->ear)/(1-0.039-0.070); ankle height 0.039H and ear->crown 0.070H from Winter anthropometry (MediaPipe has no crown/sole landmarks)',
  'head':'head height (chin->crown) estimated as ear_to_ear * 1.59 (adult head height / bitragion breadth)',
  'caveats':['MediaPipe world landmarks are a learned metric prior, so absolute sizes regress toward the training-set average adult; ratios are more trustworthy than metres, but still pulled toward average human proportions',
             'wrist/ankle/ear landmarks are the noisiest at these resolutions (subjects are often 150-400 px tall); lower arm and head numbers have the widest spread',
             'MediaPipe hip landmarks sit near the hip joint/greater-trochanter, shoulder landmarks near the glenohumeral joint, i.e. roughly joint centres like the Hier bones',
             'angles are camera-frame body-frame estimates, not gravity aligned; shoulder/hip flex unwrapped so overhead > 180 is allowed']},
 'clips_excluded':EXCLUDE,'bad_windows_excluded':BAD_WINDOWS,'frames_used':frames_used,
 'segment_lengths_m_per_clip':per_clip,'segment_lengths_m_aggregate':agg,
 'storror':storror,'hier_v0_7_8':hier,'reference_winter_ratios':winter,'raw_deltas_to_match_storror_mediapipe_ratios_exactly (over-corrects torso and under-sizes arms because of MediaPipe landmark bias; use recommended_hier_changes)':sugg,
 'rom_deg_pooled_LR':rom_lr,'rom_deg_by_side':rom_all,'rom_deg_by_verb':rom_verb,
 'angle_conventions':'hip_abd/shoulder_abd from clip JSONs replaced here by *_elev (0 = limb along -torso axis, 90 = perpendicular, 180 = along +torso axis/overhead) and *_abd_lat (asin of lateral component, -90..90) because the atan2 abd in the clip JSONs blows up when the limb is near 90 deg flexion. ankle_dorsi neutral standing reads about -15..-20 with this landmark definition; neck_flex neutral reads about +15..+30 (ears sit forward of shoulders); neck_lateral is noisy at 240p, treat as unreliable. Others as in clip JSON angle_notes: flex + = limb forward of torso; abd + = away from midline; knee/elbow/wrist 0 = straight; ankle_dorsi + = toes up'}
json.dump(doc,open(f'{ROOT}/proportions.json','w'),indent=1)
print(json.dumps(frames_used)); print(json.dumps(agg,indent=0))
print('STORROR',json.dumps(storror,indent=0)); print('HIER',json.dumps(hier['ratios_to_height']),json.dumps(hier['ratios_height_free']),hier['stature_m'])
print('SUGG',json.dumps(sugg,indent=0)); print('ROM',json.dumps(rom_lr,indent=0)); print('VERB',json.dumps(rom_verb))

# ---- Recommended Hier changes (judgement: where Storror/MediaPipe and Winter agree on direction; keep stature, SHOULDER_Z, neck) ----
sr=storror['ratios_to_height']; sf=storror['ratios_height_free']
rec={'keep':{'stature_m':round(Hh,3),'SHOULDER_Z':1.40,'HIP_X':0.118,'neck/head_bone_heights':'unchanged except head shell'},
 'changes':[
  dict(const='HIP_Z',cur=1.05,new=0.93,delta=-0.12,why=f"torso (hip->shoulder joint) 0.352 m = 0.190H vs Storror {sr['torso']:.3f}H / Winter ~0.258H; leg 0.555H vs Storror {sr['leg_thigh_plus_shin']:.3f}H / Winter 0.491H. Lowering the hips moves 12 cm from legs into torso at the same stature: torso -> 0.47 m (0.254H), legs -> 0.91 m (0.490H)"),
  dict(const='UL_LEN',cur=0.540,new=0.455,delta=-0.085,why=f"thigh 0.291H -> 0.245H (Winter 0.245H, Storror {sr['thigh']:.3f}H)"),
  dict(const='LL_LEN',cur=0.490,new=0.455,delta=-0.035,why=f"shin 0.264H -> 0.245H (Winter 0.246H, Storror {sr['shin']:.3f}H); shin/thigh 0.91 -> 1.00 (Winter 1.00, Storror 3D {sf['shin_over_thigh']:.2f} / 2D 0.97)"),
  dict(const='UA_LEN',cur=0.370,new=0.340,delta=-0.030,why=f"upper arm 0.199H -> 0.183H (Winter 0.186H, Storror {sr['upper_arm']:.3f}H)"),
  dict(const='LA_LEN',cur=0.330,new=0.275,delta=-0.055,why=f"forearm 0.178H -> 0.148H (Winter 0.146H, Storror {sr['lower_arm']:.3f}H); whole arm 0.377H -> 0.331H (Storror {sr['arm_upper_plus_lower']:.3f}H), so hanging fingertips land near mid-thigh (~0.38H) instead of just above the knee"),
  dict(const='SHOULDER_X',cur=0.235,new=0.205,delta=-0.030,why=f"shoulder joint spacing 0.253H -> 0.221H (Storror {sr['shoulder_width']:.3f}H; human glenohumeral spacing ~0.21-0.22H). Continues the v0.7.8 narrowing; shoulder/hip becomes 1.74 (Storror 3D {sf['shoulder_over_hip_width']:.2f} / 2D 1.60)"),
  dict(const='Head sph scale (x,y,z)',cur=[0.125,0.114,0.150],new=[0.100,0.108,0.140],delta=[-0.025,-0.006,-0.010],why=f"head breadth 0.25 m = 0.135H vs Storror ear-to-ear {sr['ear_to_ear']:.3f}H (human ~0.085H) -> 0.20 m = 0.108H, still a bit chunky for the Hybrid III read. Head height (chin->crown) 0.276 m = 0.149H (1/6.7): Storror estimate {sr['head_height_est']:.3f}H is weak (derived from ear breadth), Winter 0.130H, script comment target ~1/7.5 -> trim z to 0.140 (about 0.256 m = 0.138H). Raise head centre +0.010 (1.665 -> 1.675) to keep the crown at 1.815. Scale brow/nose/chin/jaw about the head centre too"),
 ],
 'not_changed':{'HIP_X':f"hip joint spacing 0.127H already matches Storror {sr['hip_width']:.3f}H (MediaPipe hip landmarks sit slightly wide of true joint centres ~0.10H, so at most -0.01 m per side)"},
 'half_blend_option':'if the long-leg/small-torso look is deliberate art direction, apply ~50% of each delta (HIP_Z 0.99, UL 0.50, LL 0.47, UA 0.355, LA 0.30, SHOULDER_X 0.22, head x 0.112 / z 0.145)',
 'side_effects':['hip height drops 12 cm: DummyLocomotor/CharacterController capsule centre, foot IK and any authored hip-height constants (crouch, slide, ledge-grab reach) need re-tuning',
                 'arm reach drops ~8.5 cm: ledge-grab / climb-up reach checks keyed to hand position will change',
                 'clip curves are angles, so they retarget regardless; root height curves in the clip JSONs are in MediaPipe metres (~0.87 scale vs a 1.75 m adult) and should be scaled by leg length'],
 'observation_not_a_proportion':'feet: Ankle bone at z=0.02 but shoe_foot sole bottom at z=-0.041 and heel pad bottom about z=-0.044, i.e. the soles sit ~4 cm below Root (z=0). Worth checking whether the game offsets the model up or the feet clip into the floor'}
doc['recommended_hier_changes']=rec
doc['storror_2d_check']={'note':'independent 2D image-space check (tools/check2d.py); MediaPipe landmark definitions, not anatomical joint centres',
  'leg_over_torso':1.432,'shin_over_thigh':0.97,'shoulder_over_hip_width_frontal':1.601,'frames':'33 upright straight-leg frames, 25 frontal'}
json.dump(doc,open(f'{ROOT}/proportions.json','w'),indent=1)
