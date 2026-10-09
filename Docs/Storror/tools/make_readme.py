import json, glob
ROOT='/workspace/storror'
NOTES=json.load(open(f'{ROOT}/tools/quality_notes.json'))
P=json.load(open(f'{ROOT}/proportions.json'))
rows=[]
for f in sorted(glob.glob(f'{ROOT}/out/json/*.json')):
    d=json.load(open(f)); s=d['source']; q=d['quality']; cid=d['clip_id']
    n=NOTES.get(cid,{})
    rows.append(f"| `{cid}` | [{s['title']}](https://www.youtube.com/watch?v={s['video_id']}&t={int(s['t_start_s'])}s) | {s['t_start_s']:.2f} to {s['t_end_s']:.2f} s"
                + (f" (scene cut, last frame {d['scene_cut_truncated_at_s']:.2f})" if d['scene_cut_truncated_at_s'] else '')
                + f" | {d['move']} | **{d['game_verb']}** | {d['camera_view']} | {d['proxy_resolution'][1]}p, {d['frame_count']} f, {q['frac_usable']*100:.0f}% usable, vis {q['mean_visibility_body']:.2f} | {n.get('grade','')} | {n.get('note','')} |")
fu=P['frames_used']
md=f"""# Storror video motion reference for Tag

This folder holds per-clip 3D pose, joint-angle curves, contact sheets and stick strips, extracted from short segments of Storror YouTube videos. It is motion *reference* from monocular video, not mocap. Use it for timing, poses and joint-angle ranges when keying or retargeting onto the Hier mannequin.

## Layout
| Path | What | Commit later? |
|---|---|---|
| `clips.json` | clip manifest (video id, t0/t1, move, game verb, camera view, optional subject seed) | yes |
| `out/json/<clip>.json` | per-frame world landmarks (smoothed + raw), image uv, visibility, confidence mask, joint angles, root height/velocity, fps | yes (derived data) |
| `out/strips/<clip>.png` | stick-figure-only strip, 12 frames | yes |
| `out/contact/<clip>.png` | 8 key frames with skeleton over the video | **no**: contains video frames, box only |
| `raw/`, `scout/` | 720p segments and 144p/240p scouting proxies | **never**: raw video stays on the box |
| `proportions.json`, `hier_comparison.md` | aggregate body proportions, joint ROM, Hier comparison and suggested deltas | yes |
| `tools/` | scout, windows, review, dense/fine sheets, `extract.py`, `proportions.py`, `dl720.sh`, `fbxbones.py` | yes |
| `out_proxy240/` | first extraction pass on the low-res proxies, kept for comparison | no |

## Pipeline
1. `yt-dlp --flat-playlist` listed the channel. Candidates were scouted on 144p/240p proxies with sparse MediaPipe passes (`tools/scout.py`, `tools/windows.py`), then checked by eye on review sheets (`review.py`, `dense.py`, `fine.py`).
2. `tools/dl720.sh` downloads only each clip's segment at 720p (H.264, padded 1 s each side, `--force-keyframes-at-cuts`). It writes `raw/<id>.json` with the segment's source start time and uses no cookies or credentials. YouTube's "confirm you're not a bot" check blocked about half the requests, but spaced retries got all 16 segments. Clips 01-03 come from a 2011 upload whose best stream is 854x468; the rest are 1280x720. The clip JSON field `source_video` records the source.
3. `tools/extract.py <clip>` runs MediaPipe PoseLandmarker **heavy** in IMAGE mode on a 512 px crop around the previous detection (ROI tracking), with a full-frame fallback and `num_poses=3`. The subject is picked by position plus torso-size continuity, which rejects bystanders, and an optional seed point in `clips.json` sets the starting subject. It also:
   - truncates the clip at a scene cut,
   - fills gaps of 0.12 s or less linearly, but never across an identity jump,
   - applies one-euro smoothing (world: min_cutoff 1.5, beta 0.6),
   - derives joint angles, root height above feet and image-space root velocity.
4. `tools/proportions.py` aggregates segment lengths and ROM over gated clean frames and compares them with the Hier rig (see below).

## Coordinate and angle conventions
- `world_xyz_m`: MediaPipe world landmarks in metres. The origin is the hip centre and the axes follow the camera (x right, **y down**, z negative toward the camera). They are **not gravity-aligned**.
- `confidence_mask[t][j]`: visibility ≥ 0.5 and the frame is detected or gap-filled. `usable[t]`: the frame has a pose at all. Prefer `world_xyz_m_raw` + `detected` if you want to do your own smoothing.
- Angles, in degrees:
  - hip and shoulder flex: + means the limb is forward of the torso.
  - knee, elbow and wrist: 0 means straight.
  - `ankle_dorsi` = 90 − knee/ankle/toe angle. Neutral standing reads about −15 to −20.
  - `spine_twist`, `spine_lateral_bend` and `neck_flex` (+ = head forward; neutral reads about +15 to +30).
  - `trunk_lean_from_cam_vertical` depends on the camera.
  - The clip-JSON `*_abd` values blow up near 90° of flexion. For ROM use the `*_elev` and `*_abd_lat` values in `proportions.json`.
- `root_height_above_feet_m` is a crouch/extension proxy. `root_vel_proxy_mps` includes camera pan, so it is not ground speed.

## Clips
| Clip | Source | Timestamps | Move | Game verb | View | Data | Grade | Quality notes |
|---|---|---|---|---|---|---|---|---|
""" + "\n".join(rows) + f"""

Grades: **A** = clean, retarget-ready. **B** = usable with care, or only in the window noted. **C** = reference for timing only.

### Coverage gaps
There is no clean **slide**, **quick 180 turn**, vertical (perpendicular-wall) wall run, or slow-motion tutorial vault in this set. The tic-tac and wall runs use Brighton's slanted sea wall. Another scouting pass would fill these, using e.g. the "How to" or "Parkour Basics" style uploads. Downloads work, but the intermittent YouTube bot check means each new batch needs spaced retries. Cookies are off-limits under the rules for this task.

## Body proportions and ROM
See `proportions.json` and `hier_comparison.md`. Clips used: {', '.join(k for k in fu)}, with per-frame gating (detected, no subject swap, no L/R flip, endpoints with visibility ≥ 0.7). Clips excluded entirely: {', '.join(P['clips_excluded']) or 'none'}.

## Rules followed
Raw video stays in `raw/` and `scout/` on the box only. Nothing was pushed to GitHub, the Hier model was not edited, nothing was spent, and the Amaterasu machine was not touched.
"""
open(f'{ROOT}/README.md','w').write(md); print('README written', len(rows))
