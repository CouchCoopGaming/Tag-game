# Hip targets for the loaded frames

These numbers are for the movement floors on a plant, a landing, and a crouch. They come from tracks already in this branch. Nothing here is a played key, and nothing here is root motion.

Every clip below is REFERENCE-ONLY. Clips 01, 02, 09, 16, and 17 are STORROR YouTube uploads under the YouTube standard license (all rights reserved). `25_side_absorb` is the Archive mirror `youtube-cSMbjuzhqiQ` of that same license. Raw video is not in the repo.

## How a landmark becomes a Hier angle

MediaPipe world landmarks are camera-aligned, y down. The conversion used here is `(x, -z, -y)`, so z is camera-up. That is not gravity. The same frame is used in `Docs/Movement/hipref/`.

Facing is the flat forward from the shoulders and the nose, with the vertical component removed. Pelvis-behind is `(support foot − hip) · facing`, in centimetres. Positive means the pelvis is behind the support foot. The support foot is the lowest foot, plus any foot within 12 cm of it. Knee angle is the geometric bend at that support knee, 0° straight. The movement floor compares this geometric knee. The Hier knee key is the negative bend; `Tools/Tag/storror_pose.py` writes `-(knee − 12)`.

Hip flexion on the Hier is the absolute pitch of the Hips bone. Spine flexion is lumbar plus chest. On this rig a positive hip pitch and a positive spine pitch both pitch the chest forward, and they add. PunchHipPitch + PunchSpinePitch is that sum. These tracks have a shoulder point and a hip point, and no lumbar landmark, so the split between the two keys is not in the film. The measured angle is the sum:

`chestForward = hipPitch + spinePitch`

It is the signed angle of the shoulder-to-hip line from camera-up, positive when the shoulders sit toward the facing direction. A frame is quoted only when that signed angle agrees with the trunk's inclination from camera-up within 12°. A wrapped sign (the roll past vertical) is left out of the hip key.

The upper-leg rest is about 180° on X, so thigh pitch does not add into the hip key. The thigh-versus-torso angle in the pose JSON (often 80–150° on these frames) is the thigh channel. It is not the 25° or 35° hip floor.

A landing or a crouch that clears both new floors needs hip ≥ 35° and spine ≥ 15°, so the chest-forward sum is at least 50°, and hip / spine stays at least 1.5. A plant needs hip ≥ 25°. A sum under those floors cannot host both keys without pitching the chest past the frame.

## Loaded frames

Times are seconds from the first sample of the clip. Source time is the film clock.

| Move | Clip | Grade | t | Source | Chest forward | Knee | Pelvis behind | Thigh forward |
|---|---|---|---|---|---|---|---|---|
| Soft land | `25_side_absorb` | B | 1.50 s, i=45 | 5.70 s | +47.9° | 105.2° | +15.8 cm | +62.4° |
| Hard land | — | — | — | — | — | — | — | — |
| Roll entry | `02_drop_roll_gravel` | B | 0.10 s, i=3 | 30.90 s | +19.2° | 114.7° | +19.8 cm | +76.6° |
| Roll contact | `02_drop_roll_gravel` | B | gap | 31.57–32.00 s | — | — | — | — |
| Roll contact, readable | `01_roll_grass` | B | 0.57 s, i=17 | 100.57 s | +10.3° | 98.8° | −11.4 cm | +26.9° |
| Roll exit | `01_roll_grass` | B | 0.80 s, i=24 | 100.80 s | +19.9° | 52.0° | +18.0 cm | +38.7° |
| Vault plant | `09_run_vault_park` | B | 2.54 s, i=76 | 124.04 s | +21.2° | 40.7° | −3.4 cm | +3.5° |
| Sprint contact | `16_sprint_dive_hole` | B | 0.20 s, i=6 | 195.80 s | +41.2° | 46.0° | +26.0 cm | +41.8° |
| Slide crouch | `17_slide_slope_crouch` | B | 0.60 s, i=36 | 13.90 s | — | 137.2° | +9.7 cm | +58.0° |

Thigh forward is the support thigh from straight down, positive toward facing. It is the thigh channel, listed so the deep leg is not lost. It is not hip flexion.

Shin forward on the quoted frames: soft land +10.0 cm, roll entry +9.9 cm, roll contact +27.4 cm, roll exit +7.4 cm, vault plant +16.1 cm, sprint contact +5.2 cm, slide crouch +17.2 cm.

### Soft land

`25_side_absorb`, side, 640×480, grade B. The loaded bottom is 1.50 s: both feet, chest-forward +47.9°, knee 105.2°, pelvis +15.8 cm. One frame earlier, 1.47 s (i=44), is the same absorb with the pelvis further back: chest-forward +48.2°, knee 99.0°, pelvis +21.1 cm, shin +4.6 cm.

47.9° is the whole chest. A 35° hip and a 15° spine add to 50°. A split that stays inside this frame can clear one floor. It cannot clear both. At a ratio of 1.5 the share is about hip 29° and spine 19°, and the hip is under 35°. Putting 35° on the hip leaves about 13° of spine, under 15°.

`03_wall_drop_softland` is grade A and front-on, so the sagittal chest is not this measurement.

### Hard land

No track is a readable side-on hard landing. `21_tuck_land` is grade D, three measured frames, and the stick breaks. `04_window_drop_softland` is a three-quarter view; at the deep knee (0.83 s, knee 125.1°) the pelvis is 6.0 cm in front and the chest-forward angle is +26.6°. `05_window_jump_land_run` is side and handheld, and the deep-knee frames wrap past vertical (chest-forward +125° at 0.90 s). No hard-land target is given.

### Landing roll

Entry is `02_drop_roll_gravel`, grade B, side / three-quarter, static. At 0.10 s the window is already folded: chest-forward +19.2°, knee 114.7°, pelvis +19.8 cm, shin +9.9 cm. The chest sum is under a 25° hip key. The depth on this frame is the knee and the thigh (+76.6°).

Contact on `02` is a gap. From 0.77 s through 1.20 s the frames are unusable or below the visibility floor. The next measured frame, 1.23 s (i=37), is the inverted one already in HIPREF: pelvis +68.2 cm, knee 38.7°, shin −42.2 cm, chest sign wrapped. It is not a plant.

The readable contact is `01_roll_grass`, grade B, small figure. At 0.57 s the chest-forward angle is +10.3° and the trunk's full inclination from camera-up is 23°, so part of the lean sits out of the facing plane. Pelvis is 11.4 cm in front. Knee 98.8°, shin +27.4 cm. This is the contact the track has. It is not an 8 cm sit.

Exit is the same clip at 0.80 s: chest-forward +19.9°, knee 52.0°, pelvis +18.0 cm, shin +7.4 cm. `02` at 2.34 s still has pelvis +33.7 cm and knee 101.6°, and the trunk inclination is 101° with the signed chest wrapped past vertical. That inclination is not converted into a hip key.

### Vault plant

`09_run_vault_park`, grade B, three-quarter side. The hand-support moment is 2.54 s, over the block: chest-forward +21.2°, knee 40.7°, pelvis 3.4 cm in front, shin +16.1 cm. The footage puts the pelvis in front. The chest sum is under a 25° hip key.

The landing absorb on the same clip, 3.67 s, is a landing and not this plant: chest-forward +11.7°, knee 92.3°, pelvis +28.8 cm. The depth there is the thigh (+62.3°) and the knee.

`30_gym_pike`, grade B, no Creative Commons license, is a pike with the hands near the hips, not a two-hand plant. At 1.80 s the chest-forward angle is about +54°, pelvis +21.7 cm, knee 94.5°. It is not the plant row.

### Sprint contact

`13_sprint_front` is front-on, so a chest angle near 0° is the camera, not a sagittal pitch. The side contact is `16_sprint_dive_hole`, grade B, at 0.20 s: chest-forward +41.2°, knee 46.0°, pelvis +26.0 cm, shin +5.2 cm. This is a stride in the sprint that opens the dive, not a precision stop. 41° is under the 50° landing sum. At a ratio of 1.5 the share is about hip 25° and spine 16°.

### Slide crouch

`17_slide_slope_crouch`, grade B, is a steep slope filmed from below in a three-quarter front view. At the deepest support knee, 0.60 s, the knee is 137.2°, the pelvis is +9.7 cm, and the shin is +17.2 cm. The signed chest-forward angle is +1.7° and the trunk inclination from camera-up is 21°. Those two disagree, so no hip pitch or spine flexion is quoted. The most-behind frame, 0.83 s, is pelvis +34.8 cm and knee 109.5°, with the same camera. The played slide's knee and the new floors are a separate measurement; `leadKnee=-10` stays as it is.

## Search

Side-on precision landings, side-on landing rolls, and a side-on one-foot tic-tac were looked for again. No new window was clean enough to add. The sources and the license lines are in `Docs/Movement/REFERENCE.md` under Pass 20 search. Emotes stayed parked.
