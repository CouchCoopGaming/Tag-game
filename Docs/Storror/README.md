# Storror video motion reference for Tag

> **Source note.** Everything in this folder is derived from short segments of public [Storror](https://www.youtube.com/@STORROR) parkour YouTube videos (links and timestamps per clip below). It contains **pose skeletons only**: MediaPipe landmark coordinates, joint angles, aggregate body proportions and stick-figure strips drawn on a blank canvas. No video, no video frames and no images of the athletes are in this repo. The raw clips and the contact sheets (skeleton drawn over video frames) stay on the build box and are never committed. All rights to the source videos remain with Storror.

This folder holds per-clip 3D pose, joint-angle curves and stick strips, extracted from short segments of Storror YouTube videos. It is motion *reference* from monocular video, not mocap. Use it for timing, poses and joint-angle ranges when keying or retargeting onto the Hier mannequin.
## Layout
| Path (relative to `Docs/Storror/`) | What | In this repo? |
|---|---|---|
| `clips.json` | clip manifest (video id, t0/t1, move, game verb, camera view, optional subject seed) | yes |
| `out/json/<clip>.json` | per-frame world landmarks (smoothed + raw), image uv, visibility, confidence mask, joint angles, root height/velocity, fps | yes (derived data) |
| `out/strips/<clip>.png` | stick-figure-only strip, 12 frames | yes |
| `out/contact/<clip>.png` | 8 key frames with skeleton over the video | **no**: contains video frames, build box only |
| `raw/`, `scout/` | 720p segments and 144p/240p scouting proxies | **never**: raw video stays on the build box |
| `proportions.json`, `hier_comparison.md` | aggregate body proportions, joint ROM, Hier comparison and suggested deltas | yes |
| `tools/` | scout, windows, review, dense/fine sheets, `extract.py`, `proportions.py`, `dl720.sh`, `fbxbones.py` | yes |
| `out_proxy240/` | first extraction pass on the low-res proxies, kept for comparison | no (build box only) |

## Pipeline
The scripts in `tools/` are committed for provenance. They were run on the build box from a working directory `/workspace/storror` (hard-coded as `ROOT` in several scripts) that also held the raw video, scouting proxies and model files, none of which are in the repo. To re-run them, point `ROOT` at a local working copy and re-download the segments with `tools/dl720.sh`. `tools/make_readme.py` regenerates the box-side version of this README (without this repo source note).

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
| `01_roll_grass` | ['Taking the Height Drop' - Episode 46](https://www.youtube.com/watch?v=rq6XY_D_9M0&t=100s) | 100.00 to 101.95 s | jump down onto grass, parkour roll over shoulder, sit up | **roll** | side-on, static camera, small figure | 468p, 59 f, 100% usable, vis 0.72 | B | 480p source (2011). Small figure. Tracked from take-off through the shoulder roll; a few mid-roll frames missing (55/59 detected). Good for roll timing and tuck shape. |
| `02_drop_roll_gravel` | ['Taking the Height Drop' - Episode 46](https://www.youtube.com/watch?v=rq6XY_D_9M0&t=30s) | 30.80 to 33.50 s (scene cut, last frame 33.30) | wall drop, landing into diagonal roll on gravel | **roll** | side-on / 3/4, static camera | 468p, 76 f, 91% usable, vis 0.74 | B | 480p. Wall drop into diagonal roll. Truncated at a scene cut (33.34 s). Some inverted roll frames lost (67/76 detected). |
| `03_wall_drop_softland` | ['Taking the Height Drop' - Episode 46](https://www.youtube.com/watch?v=rq6XY_D_9M0&t=52s) | 52.00 to 53.70 s | drop off ~3 m wall, deep crouch absorb, stand up | **softland** | front-on, static camera | 468p, 51 f, 100% usable, vis 0.92 | A | 480p front-on, static. Drop, deep absorb, stand up. Visibility 0.92. Best soft-land reference. |
| `04_window_drop_softland` | [HUGE Parkour Wall Run (Brighton) 🇬🇧](https://www.youtube.com/watch?v=TCyhLQBBPmg&t=162s) | 162.30 to 164.20 s (scene cut, last frame 164.00) | hop down from window ledge, land, absorb | **softland** | 3/4 front, handheld pan | 720p, 52 f, 100% usable, vis 0.90 | A | Fisheye pan. Hop down from window ledge and land. Truncated at a cut (164.04 s). Visibility 0.90. |
| `05_window_jump_land_run` | [HUGE Parkour Wall Run (Brighton) 🇬🇧](https://www.youtube.com/watch?v=TCyhLQBBPmg&t=182s) | 182.00 to 184.40 s | jump out of window, land, run away | **softland** | side-on, handheld follow | 720p, 72 f, 100% usable, vis 0.67 | B | Side follow-cam, dark clothing (visibility 0.67). Jump out of window, land, run off. Last ~0.15 s camera swing excluded from stats. |
| `06_run_walljump_climb_window` | [HUGE Parkour Wall Run (Brighton) 🇬🇧](https://www.youtube.com/watch?v=TCyhLQBBPmg&t=173s) | 173.00 to 177.50 s | approach run, wall step/jump to window grate, cling, climb up into window | **climbtop/mantle** | side/3/4, fisheye handheld | 720p, 135 f, 100% usable, vis 0.69 | B | Fisheye. Run-in, wall step to grate, cling, climb into window. Usable for climb-up/mantle timing; hands partly occluded on the grate. |
| `07_wall_climb_traverse` | [HUGE Parkour Wall Run (Brighton) 🇬🇧](https://www.youtube.com/watch?v=TCyhLQBBPmg&t=148s) | 148.00 to 154.00 s | climbing on building facade between windows (cling holds) | **cling** | low 3/4, fisheye, small figure | 720p, 180 f, 100% usable, vis 0.72 | B | Small figure, fisheye. Facade climbing between holds. Good cling poses; fine hand detail unreliable. |
| `08_vault_block_close` | [This park is built for parkour!](https://www.youtube.com/watch?v=zt7xP3_K7uQ&t=108s) | 108.30 to 110.40 s | run and vault/dive over concrete block toward camera | **vault** | front 3/4, close handheld | 720p, 63 f, 100% usable, vis 0.70 | B | Close fisheye, athlete runs and vaults toward the camera. Strong lens distortion; limbs leave frame at the end. |
| `09_run_vault_park` | [This park is built for parkour!](https://www.youtube.com/watch?v=zt7xP3_K7uQ&t=121s) | 121.50 to 126.00 s | jog, sprint, two-hand vault over block | **vault** | 3/4 side, handheld | 720p, 135 f, 100% usable, vis 0.72 | B | Jog, sprint, two-hand vault with bystanders in frame. The size-continuity tracker keeps the athlete through the vault. Scene-cut detection is disabled here because a camera whip falsely triggers it. |
| `10_wallrun_slanted` | [ULTIMATE Brighton Wallrun Challenges 🇬🇧](https://www.youtube.com/watch?v=WGV_b8lPbyQ&t=136s) | 136.50 to 141.00 s (scene cut, last frame 140.71) | run across Brighton slanted wall (wall run) | **wallrun** | 3/4 from below, static | 720p, 127 f, 100% usable, vis 0.80 | A | Slanted sea-wall run from below, static. Truncated at a cut (140.74 s). Visibility 0.80. |
| `11_wallrun_slanted_34` | [ULTIMATE Brighton Wallrun Challenges 🇬🇧](https://www.youtube.com/watch?v=WGV_b8lPbyQ&t=368s) | 368.50 to 374.00 s | run along slanted wall toward camera | **wallrun** | front 3/4, handheld | 720p, 165 f, 99% usable, vis 0.65 | B | Handheld, front 3/4. The filmer's hand enters frame and a second runner is in the first frames. |
| `12_tictac_slanted_wall` | [New moves unlocked! Brighton Parkour 🇬🇧](https://www.youtube.com/watch?v=QvvJ2t9iVyE&t=290s) | 290.50 to 296.40 s (scene cut, last frame 296.11) | run up slanted brick wall, tic-tac off wall, drop and land | **walljump** | 3/4 side, static | 720p, 169 f, 100% usable, vis 0.73 | A | Run-up, tic-tac off the slanted brick wall, drop and land. Truncated at a cut (296.14 s). |
| `13_sprint_front` | [New moves unlocked! Brighton Parkour 🇬🇧](https://www.youtube.com/watch?v=QvvJ2t9iVyE&t=296s) | 296.75 to 298.80 s | sprint toward camera then decelerate | **sprint** | front-on, static | 720p, 62 f, 74% usable, vis 0.78 | B | Front-on sprint and deceleration. First ~0.5 s too motion-blurred to detect (46/62). |
| `14_rail_precisions` | [New moves unlocked! Brighton Parkour 🇬🇧](https://www.youtube.com/watch?v=QvvJ2t9iVyE&t=283s) | 283.50 to 289.20 s | stair-rail to wall precision jumps and landings | **softland** | side-on, static, second person in frame | 720p, 171 f, 92% usable, vis 0.69 | B | Stair-rail to wall precisions; a bystander is in frame (tracked correctly now). Last ~0.3 s is a camera close-up. |
| `15_cat_leap_wall` | [New moves unlocked! Brighton Parkour 🇬🇧](https://www.youtube.com/watch?v=QvvJ2t9iVyE&t=370s) | 370.00 to 378.00 s | jump to wall, cat leap / cling, climb over | **cling** | 3/4 side, static | 720p, 240 f, 100% usable, vis 0.83 | A | Jump to wall, cat leap / cling, climb over. Visibility 0.83. Best cling reference. |
| `16_sprint_dive_hole` | [STORROR PARKOUR HOLES ⭕](https://www.youtube.com/watch?v=M8bu4tTqjg0&t=195s) | 195.60 to 199.50 s | climb out through wall window, sprint side-on, dive through wall hole | **sprint** | side-on, handheld follow | 720p, 117 f, 92% usable, vis 0.67 | B | Climb out of wall window, side-on sprint, dive through wall hole. Handheld follow; some frames lost in the dive (106/117). Starts at 195.6 (after a cut at 195.30), seeded on the athlete. |

Grades: **A** = clean, retarget-ready. **B** = usable with care, or only in the window noted. **C** = reference for timing only.

### Coverage gaps
There is no clean **slide**, **quick 180 turn**, vertical (perpendicular-wall) wall run, or slow-motion tutorial vault in this set. The tic-tac and wall runs use Brighton's slanted sea wall. Another scouting pass would fill these, using e.g. the "How to" or "Parkour Basics" style uploads. Downloads work, but the intermittent YouTube bot check means each new batch needs spaced retries. Cookies are off-limits under the rules for this task.

## Body proportions and ROM
See `proportions.json` and `hier_comparison.md`. Clips used: 01_roll_grass, 02_drop_roll_gravel, 03_wall_drop_softland, 04_window_drop_softland, 05_window_jump_land_run, 06_run_walljump_climb_window, 07_wall_climb_traverse, 08_vault_block_close, 09_run_vault_park, 10_wallrun_slanted, 11_wallrun_slanted_34, 12_tictac_slanted_wall, 13_sprint_front, 14_rail_precisions, 15_cat_leap_wall, 16_sprint_dive_hole, with per-frame gating (detected, no subject swap, no L/R flip, endpoints with visibility ≥ 0.7). Clips excluded entirely: none.

## Rules followed
Raw video, scouting proxies and contact sheets stay on the build box only. Only derived data (pose JSON, stick strips, proportions, docs and scripts) is committed here. Downloads used no cookies or credentials, nothing was spent, and the Amaterasu machine was not touched. The Hier model was not edited by this pass; `hier_comparison.md` lists suggested changes only.
