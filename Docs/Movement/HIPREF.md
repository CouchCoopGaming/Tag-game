# Hip-rule reference

Measured for the movement lead on `cursor/tag-movement`. The ledger at `origin/cursor/tag-movement:Docs/Movement/LEDGER.md` was read and was not edited. Nothing here retunes a played clip.

The ledger's remaining hip fails, across the 33 locomotion and exit clips, are the played slide crouch (7), the played stagger land (8), exit-Roll (4), and exit-RollAbsorb (2). Stagger is not in this note. The categories below are the ones asked for: a landing into a roll, vaults, climb plants, mantles, wall plants and kicks, and slide entry and exit. The played vault already passes its plant and its land. These vault numbers are shape reference for that pass, not a second fail.

Every track here is **REFERENCE-ONLY**. Clips 01–20 are YouTube standard license, all rights reserved. Clips 22 and 24 are the unlicensed Vimeo film [Sport-Freestyle-freerunning](https://vimeo.com/721018315) (`vimeo-721018315`). No CC0 or public-domain live parkour clip was verified for these moves. The public-domain dive-roll GIF in `REFERENCE.md` is a 128×96 animation of a roll over a table. It is not one of these tracks, and it is not a drop from height.

The ledger lists terminal 56.16 and a roll at 65% of terminal, about 36.5 in game units. These tracks are monocular. They do not measure that speed. `02_drop_roll_gravel` is a high drop into a roll. It is not a verified 36.5 fall.

## How the numbers were taken

MediaPipe world is camera-aligned, y down. It is converted to (x, −z, −y) so the third axis is camera-up, not gravity. Facing is the flat shoulder-and-nose direction. The support foot is the lower foot, plus any foot within 12 cm. `pelvisBack` is (foot − hip) along that facing, in centimetres. Positive means the pelvis is behind the support foot. Knee flex is 180 minus the angle at the support knee. `knee max` is the deeper of the two knees when they disagree. Shin forward is the knee ahead of the ankle along facing. Positive is the shin pointing forward.

A frame is kept when `usable` is set and visibility is at least 0.25 on the nose, shoulders, hips, knees, and ankles. A negative pelvisBack in the footage is the footage. It is labeled not-a-plant. It is not a retarget error.

Plants and landings want pelvisBack at least 8 cm, or 12 cm in a crouch. Knee flex wants at least 25° on a plant and 45° on a landing. Degrees in the tables below are rounded. The JSON keeps tenths.

Full curves are arrays in `Docs/Movement/hipref/measurements.json` (`frame`, `time_s`, `pelvis_back_cm`, `knee_deg`, `knee_max_deg`, `shin_forward_cm`, `support`). Side sticks are `Docs/Movement/hipref/<clip>_side.png`. The cyan line is the support foot. The orange dot is the pelvis. No video frame is in those images. Regenerate with `python3 Tools/Tag/hipref_annotate.py`.

## Roll landing

Primary window: `02_drop_roll_gravel`, 48 of 76 frames measured. Side still: `Docs/Movement/hipref/02_drop_roll_gravel_side.png`.

| t | frame | pelvisBack | support knee | knee max | shin | what it is |
|---|---|---|---|---|---|---|
| 0.10 s | 3 | +19.8 cm | 115° | 123° | +9.9 cm | already folded at the start of the window |
| 0.50 s | 15 | +3.1 cm | 55° | 58° | +15.1 cm | opening, pelvis nearly over the feet |
| 0.70 s | 21 | +11.2 cm | 47° | 48° | +9.2 cm | brief sit, knee just at the landing bar |
| 1.24 s | 37 | +68.2 cm | 39° | 46° | −42.2 cm | extreme behind, shin backward, not a clean plant |
| 1.44 s | 43 | −46.5 cm | 83° | 83° | +17.3 cm | pelvis in front, a heap |
| 2.34 s | 70 | +33.7 cm | 102° | 117° | +2.3 cm | late crouch, pelvis behind, knee still deep |

The two frames that clear both 8 cm and a deep knee are 0.10 s and 2.34 s. The 68 cm reading is an inverted or tucked frame. Exit-Roll and exit-RollAbsorb still fail contact on the ledger. This does not clear them.

`01_roll_grass` is the same film, smaller figure, 32 of 59 measured. Still: `01_roll_grass_side.png`.

| t | frame | pelvisBack | support knee | knee max | shin | what it is |
|---|---|---|---|---|---|---|
| 0.57 s | 17 | −11.4 cm | 99° | 109° | +27.4 cm | roll contact, pelvis in front |
| 0.80 s | 24 | +18.0 cm | 52° | 69° | +7.4 cm | stand-up sit |
| 1.74 s | 52 | +32.6 cm | 71° | 71° | −5.3 cm | late sit, shin slightly back |

`24_landing_roll` is the clearer live picture and the sparser track: 13 of 66 frames measured, unlicensed, DO-NOT-SHIP. Still: `24_landing_roll_side.png`. At 0.64 s, frame 16, pelvisBack is −9.0 cm and the support knee is 152°. The pelvis is in front during that deep tuck. Do not treat 13 frames as a landing curve.

## Vaults

`09_run_vault_park` is the main window, 82 of 135 measured. Still: `09_run_vault_park_side.png`. The played vault already sits (plant back 11.8 cm, knee 62°, drop 10 cm; land at t=0.4, back 10.2 cm, knee 80°, drop 20 cm). `exit-Vault` is the recovery, not this cross.

| t | frame | pelvisBack | support knee | knee max | shin | what it is |
|---|---|---|---|---|---|---|
| 0.07 s | 2 | +23.9 cm | 13° | 42° | −19.8 cm | run, support knee nearly straight, shin trailing |
| 1.03 s | 31 | +3.9 cm | 24° | 32° | +0.5 cm | pre-vault, pelvis close to the feet |
| 2.54 s | 76 | −3.4 cm | 41° | 63° | +16.1 cm | over the block, not an 8 cm sit |
| 3.47 s | 104 | −4.2 cm | 51° | 84° | +6.9 cm | just before the land, pelvis in front |
| 3.67 s | 110 | +28.8 cm | 92° | 92° | +5.7 cm | landing absorb |

`08_vault_block_close`, 30 of 63 measured. Still: `08_vault_block_close_side.png`. The pelvis stays in front for the vault itself.

| t | frame | pelvisBack | support knee | knee max | shin | what it is |
|---|---|---|---|---|---|---|
| 0.23 s | 7 | −40.8 cm | 14° | 22° | +22.0 cm | over the block, pelvis well in front |
| 1.40 s | 42 | −21.7 cm | 79° | 80° | +29.8 cm | still in front, knee deep |
| 1.83 s | 55 | +13.9 cm | 31° | 40° | −5.3 cm | first sit, after the cross, knee modest |

`22_pike_vault`, 11 of 61 measured, unlicensed, DO-NOT-SHIP. Still: `22_pike_vault_side.png`. At 0.28 s, frame 7, pelvisBack −39.7 cm, support knee 75°, knee max 95°. That is the pike, pelvis in front. At 0.52 s, frame 13, pelvisBack +13.3 cm, support knee 38°, shin −8.0 cm.

## Climb plants

`15_cat_leap_wall` is the main window, 193 of 240 measured. Still: `15_cat_leap_wall_side.png`. The played climb is a cruise, not a plant, and the spine still enters the thigh.

| t | frame | pelvisBack | support knee | knee max | shin | what it is |
|---|---|---|---|---|---|---|
| 0.00 s | 0 | −2.6 cm | 42° | 70° | +16.5 cm | approach |
| 0.67 s | 20 | +30.4 cm | 58° | 61° | −0.6 cm | into the wall, a sit |
| 2.37 s | 71 | +19.1 cm | 102° | 104° | +3.9 cm | cling, deep knee, pelvis behind |
| 3.04 s | 91 | −33.9 cm | 51° | 70° | +27.3 cm | drive, pelvis in front, not a sit |
| 3.67 s | 110 | +53.4 cm | 35° | 51° | −20.0 cm | high reach, shin backward, not a clean plant |

`07_wall_climb_traverse` never puts the pelvis in front. Measured range is +0.8 to +39.2 cm, 119 of 180 frames. The figure is small and the feet are unreliable. Still: `07_wall_climb_traverse_side.png`. At 1.90 s, frame 57, pelvisBack +36.3 cm, knee 52°. At 5.84 s, frame 175, pelvisBack +29.3 cm, knee 79°.

## Mantle

`06_run_walljump_climb_window`, 104 of 135 measured. Still: `06_run_walljump_climb_window_side.png`. `exit-Mantle` is the sat recovery. This window is the climb onto the ledge.

| t | frame | pelvisBack | support knee | knee max | shin | what it is |
|---|---|---|---|---|---|---|
| 0.00 s | 0 | −17.1 cm | 21° | 57° | +10.0 cm | run, pelvis in front |
| 1.00 s | 30 | +3.9 cm | 78° | 78° | +17.9 cm | deepest support knee, pelvis barely behind |
| 1.44 s | 43 | −38.2 cm | 38° | 40° | +26.3 cm | wall reach, a drive |
| 2.37 s | 71 | +40.3 cm | 47° | 71° | −6.8 cm | on the ledge |
| 3.44 s | 103 | +43.1 cm | 45° | 78° | −5.4 cm | hip over the ledge |
| 4.37 s | 131 | +12.4 cm | 20° | 21° | −4.6 cm | stood up, knee under the 25° plant bar |

## Wall plants and kicks

`12_tictac_slanted_wall` is the track, not the Hier keys, 100 of 169 measured. Still: `12_tictac_slanted_wall_side.png`. Frames 34 and 76 on the retarget were already accepted as not-a-plant. These rows are the track at the same moments. Do not rewrite the tic-tac keys from them.

| t | frame | pelvisBack | support knee | knee max | shin | what it is |
|---|---|---|---|---|---|---|
| 0.40 s | 12 | +40.8 cm | 84° | 104° | −5.2 cm | early sit |
| 1.24 s | 37 | −13.0 cm | 53° | 53° | +15.9 cm | kick, pelvis in front |
| 1.44 s | 43 | +32.2 cm | 33° | 87° | −6.5 cm | one-foot sit |
| 2.54 s | 76 | −6.0 cm | 76° | 96° | +22.7 cm | second not-a-plant, pelvis still in front |
| 3.70 s | 111 | +32.4 cm | 72° | 102° | −4.2 cm | landing sit |
| 4.67 s | 140 | +69.6 cm | 45° | 54° | −35.5 cm | prone heap, shin backward |

The played wall run is still a cruise. `10_wallrun_slanted`, 102 of 127 measured. Still: `10_wallrun_slanted_side.png`. Strides alternate between a drive and a sit. At 0.50 s, frame 15, pelvisBack −22.4 cm, knee 79° (drive). At 1.07 s, frame 32, pelvisBack +9.4 cm, knee 69° (a sit, just over 8 cm). At 2.04 s, frame 61, pelvisBack −36.6 cm, support knee 29°, other knee 94° (drive again).

`20_wallpop_180`, 25 of 57 measured. Still: `20_wallpop_180_side.png`. The plant on the wall does not sit. At 0.27 s, frame 8, pelvisBack −4.4 cm, knee 50°. At 0.43 s, frame 13, pelvisBack −16.5 cm, knee 60°. The pop land at 0.77 s, frame 23, is pelvisBack +39.8 cm, knee 77°. The retired S2 note said frames 8 and 15 fail pelvisBack. The track agrees the plant is in front. The keys on that branch were not copied.

## Slide entry and exit

`17_slide_slope_crouch` only, 74 of 97 measured, 60 fps, a steep concrete slope, not a flat slide. Still: `17_slide_slope_crouch_side.png`. The played slide is proof-locked: crouch knee about 8°, shin behind, pelvisBack 39.1 cm, drop 52.5 cm, and `leadKnee=-10` holds the proof. Do not retune `leadKnee`. `exit-Slide` is the sat recovery and is not this slide. The reference knee on the slope is far deeper than 8°, and the played clip cannot meet a 45° knee while that lock holds.

| t | frame | pelvisBack | support knee | knee max | shin | what it is |
|---|---|---|---|---|---|---|
| 0.00 s | 0 | +16.9 cm | 77° | 132° | +12.5 cm | entry, already crouched, one knee much deeper |
| 0.32 s | 19 | −0.5 cm | 68° | 124° | +9.2 cm | into the slope, pelvis about even with the feet |
| 0.60 s | 36 | +9.7 cm | 137° | 137° | +17.2 cm | deepest support knee |
| 0.83 s | 50 | +34.8 cm | 110° | 142° | −1.6 cm | most pelvis-behind on this slope |
| 1.45 s | 87 | +0.8 cm | 44° | 74° | +8.5 cm | exit starting, knee opening |
| 1.55 s | 93 | +13.1 cm | 35° | 57° | +3.3 cm | exit, pelvis behind, support knee under 45° |
