# Motion reference for the movement lead

This is the catalog of reference poses. It is not gameplay. Nothing here is root motion, and the raw video stays off the repo.

Pull the MediaPipe curves from `Docs/Storror/out/json/<id>.json` (clips 01–20) and `Docs/Movement/pose/<id>.json` (clips 21–24 and the emotes). Stick strips are next to those JSON files. Hier keys for the tic-tac live in `Docs/HierStills/v080/pass5/keyed_clips.json`. Hier keys for the CC BY emotes live in `Docs/Movement/emotes/keyed_emotes.json`. Hier keys for the CC0 emotes live in `Docs/Movement/emotes/pass16/keyed_emotes.json`.

Grades: **A** clean enough to retarget. **B** usable with the note. **C** timing only. **D** do not copy the pose.

## License

Clips 01–20 are STORROR YouTube uploads. The YouTube standard license applies (all rights reserved). They are reference only. The raw files are not in the repo.

The project ships CC0 or OFL only. A clip with no license, or a CC BY license, can guide a hand-keyed pose. It must not be retargeted into shipped animation.

Clips 21–23 and clip 24 are one Internet Archive mirror of a Vimeo upload, [Sport-Freestyle-freerunning](https://vimeo.com/721018315) (`vimeo-721018315`). No Creative Commons license is stated on the Archive item or in the Vimeo oEmbed record. Treat them as all-rights-reserved. Each pose header says `REFERENCE-ONLY / DO-NOT-SHIP`. YouTube itself refused the download without cookies, which this pass does not use.

## Storror poses

| Clip | Source and time | Good for | Game move | Quality |
|---|---|---|---|---|
| `01_roll_grass` | [Taking the Height Drop](https://www.youtube.com/watch?v=rq6XY_D_9M0&t=100s) 100.00–101.95 s | shoulder-roll timing and tuck | roll | B. 480p, small figure, a few mid-roll frames missing |
| `02_drop_roll_gravel` | same film 30.80–33.30 s | drop into a diagonal roll | roll | B. Cut at 33.34 s. Some inverted frames lost |
| `03_wall_drop_softland` | same film 52.00–53.70 s | deep absorb and stand-up | softland | A. Best soft-land. Front, static, vis 0.92 |
| `04_window_drop_softland` | [Brighton wall run](https://www.youtube.com/watch?v=TCyhLQBBPmg&t=162s) 162.30–164.00 s | ledge hop and absorb | softland | A. Fisheye. Cut at 164.04 s |
| `05_window_jump_land_run` | same film 182.00–184.40 s | jump-out, land, run off | softland | B. Dark clothes, vis 0.67 |
| `06_run_walljump_climb_window` | same film 173.00–177.50 s | wall step, cling, climb into a window | mantle | B. Hands occluded on the grate |
| `07_wall_climb_traverse` | same film 148.00–154.00 s | facade holds | cling | B. Small figure. Hands unreliable |
| `08_vault_block_close` | [Park built for parkour](https://www.youtube.com/watch?v=zt7xP3_K7uQ&t=108s) 108.30–110.40 s | vault toward camera | vault | B. Strong fisheye. Not a labeled kong or dash |
| `09_run_vault_park` | same film 121.50–126.00 s | two-hand vault over a block | vault | B. Bystanders. Not a labeled kong or dash |
| `10_wallrun_slanted` | [Brighton wallrun](https://www.youtube.com/watch?v=WGV_b8lPbyQ&t=136s) 136.50–140.71 s | slanted wall run, not the pop off | wallrun | A. Static, vis 0.80. The exit is not in the window |
| `11_wallrun_slanted_34` | same film 368.50–374.00 s | wall run toward camera | wallrun | B. Second runner and a hand in frame |
| `12_tictac_slanted_wall` | [New moves unlocked](https://www.youtube.com/watch?v=QvvJ2t9iVyE&t=290s) 290.50–296.11 s | run-up, wall kick, drop, land | walljump | A for the track. Hier plant labels are below |
| `13_sprint_front` | same film 296.75–298.80 s | sprint and slow-down | sprint | B. First 0.5 s is motion blur |
| `14_rail_precisions` | same film 283.50–289.20 s | precision landings | softland | B. Bystander. Last 0.3 s is a close-up |
| `15_cat_leap_wall` | same film 370.00–378.00 s | cat leap, cling, climb over | cling | A. Best cling. Vis 0.83 |
| `16_sprint_dive_hole` | [Parkour holes](https://www.youtube.com/watch?v=M8bu4tTqjg0&t=195s) 195.60–199.50 s | side sprint and a dive | sprint | B. Frames lost in the dive |
| `17_slide_slope_crouch` | [Ultimate slide test](https://www.youtube.com/watch?v=SqD-i_dTxws&t=13s) 13.30–14.90 s | feet-first slope slide | slide | B. Not a flat-ground slide. Retired S2 keyed clip |
| `18_vertical_wallrun_back` | [Urban warped wall](https://www.youtube.com/watch?v=JcEVTBq_T8k&t=36s) 36.05–38.45 s | vertical wall run from behind | wallclimb | B. Swap left and right before retargeting. Retired S2 keyed clip |
| `19_vertical_wallrun_side` | same film 48.00–50.40 s | warped-wall step rhythm | wallclimb | C. Camera swings. Left/right flip near 1.6 s. Retired S2 keyed clip |
| `20_wallpop_180` | [New moves unlocked](https://www.youtube.com/watch?v=QvvJ2t9iVyE&t=62s) 62.40–64.30 s | wall pop and 180 land | turn180 | B. Not a flat pivot. Retired S2 keyed clip |

The four retired S2 clips (17 slide, 18 vertical wall run, 19 warped wall, 20 wall-pop 180) also have Hier keys on `cursor/tag-storror-clips` at `80cd5f14`. Use those keys as reference. They are not wired into this branch.

## Tic-tac plants

Design-lead note on frame 34: the keyed pelvis was 4.7 cm in front of the trailing foot. The footage is further in front, tracked pelvisBack −14.7 cm. That frame is the wall-kick drive. It is labeled **not-a-plant / reference-only**. Do not copy it as a heel-sit plant. Frame 76 is the same kind of label (tracked −13.6 cm).

Frames the footage does plant (tracked pelvisBack at least 8 cm, sole down) were sat back to 8 cm where the leg could reach without the thigh entering the spine. Ten of those frames now clear 8 cm, including the landing at frame 110 (4.2 cm to 11.4 cm). Frames 118–152 are that landing copied forward. The track on 118–152 is a prone heap, so use frame 110.

Nine footage-plants still sit short of 8 cm. The thigh meets the spine, or the sole would leave the floor. They are labeled **retarget-limit / reference-only**: 12, 13, 46, 57, 94, 97, 102, 103, 112. Do not copy those as finished plants.

The per-frame list is `Docs/HierStills/v080/pass14/plant_labels_pass14.txt`. The side still with the support-foot line and the pelvis dot is `Docs/HierStills/v080/pass14/hipsit_pose_12_tictac_after.png`.

After that sit, on the tic-tac only:

`hip-sit clips=1 loadedFrames=169 pelvisBackMin=-34.5 cm hingeMin=-18.01 fails=90`

The minimum is an airborne or bridged frame, not a plant. Plant-ok frames are 44. Retarget-limit frames are 9. Not-a-plant frames are 81. Landing copies of frame 110 are 35.

`no-clip clips=1 frames=169 absMax=7.24 worldMax=3.17 rigJoint=2139 poseFails=128 pose=128`

The 7.24 cm worst pair is the hip-thigh rest nesting (rest 4.50 cm), counted as a rig joint.

The landing at frame 110 clears the 8 cm sit and a 33 cm pelvis drop. It does not clear the tightened landing rule: knee flex is 25° (want 45°) and the shin is 9.6 cm backward (the knee is behind the ankle). Pushing the knee further put the thigh into the spine. Do not treat frame 110 as a finished athletic landing.

## New parkour poses

These windows are the moves the older set does not label: a tuck landing, a pike vault, a split-foot exit, and a landing roll with a clearer body than the tuck. Pose JSON is `Docs/Movement/pose/`. Stick strips are `Docs/Movement/strips/`. They are not retargeted onto the Hier. None of them ship.

| Clip | Source and time | Good for | Game move | Quality |
|---|---|---|---|---|
| `21_tuck_land` | [Sport-Freestyle-freerunning](https://vimeo.com/721018315) 2.00–3.60 s | hips folding into a deep knee tuck | roll / softland | D. 41 f, 35 detected, vis 0.33. The stick breaks and the end inverts. Use the fold timing only. REFERENCE-ONLY / DO-NOT-SHIP |
| `22_pike_vault` | same film 24.60–27.00 s | hips pike while the hands drop, then the knees tuck | kong or dash vault | C. 61 f, 44 detected, vis 0.40. The obstacle is not in the pose, so kong and dash are not separated. REFERENCE-ONLY / DO-NOT-SHIP |
| `23_split_exit` | same film 17.60–19.80 s | one foot high, then both feet come back together | wall-run exit | C. 56 f, 49 detected, vis 0.64. The wall is not in frame. Foot split is the evidence. REFERENCE-ONLY / DO-NOT-SHIP |
| `24_landing_roll` | same film 14.60–17.20 s | approach, body goes to the ground, then stands | roll | C. 66 f, 43 detected, 47 usable, vis 0.43. Better than `21_tuck_land` (vis 0.33). 17 of the usable frames stay upright. Feet stay near vis 0.27. REFERENCE-ONLY / DO-NOT-SHIP |

A 320×240 praise-dance clip was extracted and rejected (38 of 121 frames detected). It is not in the catalog.

## Emotes

CC BY 3.0 stays REFERENCE-ONLY / DO-NOT-SHIP. The license line on both Commons pages is CC BY 3.0. That is not a ship license. Side and front stills of those two retargets stay under `Docs/Movement/emotes/`. The Hips bone is still unkeyed on them, so their pelvis drop is 0 cm. Do not key them into shipped animation.

| Clip | Source and time | Good for | Game move | Quality |
|---|---|---|---|---|
| `emote_6step` | [6-step example](https://commons.wikimedia.org/wiki/File:6-step_example.webm), Neil Sweeney, [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/) 0–4.0 s | one loop of the breakdance six-step | emote | B. 120/120 detected, vis 0.80. Floor move. REFERENCE-ONLY / DO-NOT-SHIP |
| `emote_charleston` | [Charleston dance](https://commons.wikimedia.org/wiki/File:Charleston-dance.webm), ARTSEDGE / The Kennedy Center, [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/), first 3.5 s (the Commons file is a cut from 6:06 of [the source film](https://www.youtube.com/watch?v=yXDpR-MthLo)) | standing kicks and arm swings | emote | B. 84/84 detected, vis 0.81. REFERENCE-ONLY / DO-NOT-SHIP |

Stills:

- `Docs/Movement/emotes/emote_6step_side.png`
- `Docs/Movement/emotes/emote_6step_front.png`
- `Docs/Movement/emotes/emote_charleston_side.png`
- `Docs/Movement/emotes/emote_charleston_front.png`

Hip-sit and no-clip, both labeled **not-a-plant / emote reference**. Pelvis drop is 0 cm on both because the Hips bone is not keyed. Only the limbs rotate. The plant thresholds are not the target.

`emote_6step` `hip-sit clips=1 loadedFrames=120 pelvisBackMin=-26.0 cm hingeMin=n/a fails=emote`

`no-clip clips=1 frames=120 absMax=7.26 worldMax=6.34 rigJoint=1593 poseFails=405 pose=405`

Both-leg knee is 18–135°. The 405 pose intersections are the floor step forced onto a standing rig.

`emote_charleston` `hip-sit clips=1 loadedFrames=84 pelvisBackMin=-24.4 cm hingeMin=n/a fails=emote`

`no-clip clips=1 frames=84 absMax=4.70 worldMax=5.12 rigJoint=978 poseFails=97 pose=97`

Both-leg knee is 23–79°. The standing leg stays at 9–28°. The 79° is the kick. Shin on the support leg stays forward (3.7–16.5 cm).

## Pass 16 — CC0 standing emotes

Two own-work Commons files checked out as CC0. The license line on each file page is: "This file is made available under the Creative Commons CC0 1.0 Universal Public Domain Dedication." Deed: http://creativecommons.org/publicdomain/zero/1.0/deed.en

The FX note (PR #135) asks for a silhouette that still reads at quarter-pane size, held about 0.4 s inside the first second: both arms in a V, one arm straight up with the other still out, a wide star, or a full-body spin. A shrug, a face, a hand sign, or a small prop does not. These two clips do not clear that bar. The picture of Tizi has both wrists above the shoulders for about 0.45 s at the start of the window, both toward the same side of the frame. The 3D track only keeps the right arm up, and the rear still at 0.15 s and 0.45 s does not show a V, a star, or one arm straight up. Agbadja never holds any of those outlines. Do not treat either retarget as a finished quarter-pane celebration.

The Hips bone is keyed. Local Y is world up. Negative Y is the pelvis drop. The armature stays at the origin. Both are standing, so the rig is not asked to lie down. They are not plants.

| Clip | Source and time | Good for | Game move | Quality |
|---|---|---|---|---|
| `emote_agbadja` | [Danse Agbadja](https://commons.wikimedia.org/wiki/File:Danse_Agbadja_avec_%C3%A0_Cotonou_au_B%C3%A9nin_avec_Gessi_Zolawadji.webm), Adoscam, own work, CC0, 3.40–7.40 s | standing steps | emote | C. 118/118 detected, body vis 0.57. Right ankle vis 0.20, so the leg bend is thin. No quarter-pane silhouette. CC0, may be retargeted. The 10 capped frames leave the sole as high as 8.6 cm |
| `emote_tizi` | [Tizi BR](https://commons.wikimedia.org/wiki/File:Tizi_BR.webm), Poiana11, own work, CC0, 35.70–39.10 s | one arm rises, then a step | emote | C. 103/103 detected, body vis 0.44. One frame near 1.1 s drops the head. The opening is not a held V. CC0, may be retargeted |

Stills, side, front, and rear:

- `Docs/Movement/emotes/pass16/emote_agbadja_side.png`
- `Docs/Movement/emotes/pass16/emote_agbadja_front.png`
- `Docs/Movement/emotes/pass16/emote_agbadja_rear.png`
- `Docs/Movement/emotes/pass16/emote_tizi_side.png`
- `Docs/Movement/emotes/pass16/emote_tizi_front.png`
- `Docs/Movement/emotes/pass16/emote_tizi_rear.png`

`emote_agbadja` `hip-sit clips=1 loadedFrames=118 pelvisBackMin=-51.1 cm hingeMin=-4.68 fails=emote`

`no-clip clips=1 frames=118 absMax=6.76 worldMax=15.30 rigJoint=1384 poseFails=278 pose=278`

Pelvis drop is 0.2–22.0 cm. The 22 cm cap caught 10 frames. Support knee stays 2–27°. The minimum pelvisBack is a step, not a plant.

`emote_tizi` `hip-sit clips=1 loadedFrames=103 pelvisBackMin=-49.1 cm hingeMin=-21.43 fails=emote`

`no-clip clips=1 frames=103 absMax=4.85 worldMax=14.47 rigJoint=1228 poseFails=215 pose=215`

Pelvis drop is −0.3–13.3 cm. The sole stays at 0.8 cm. The 4.85 cm absolute pair is the hip-thigh rest nest (rest 4.50 cm) plus a little. The 14.47 cm world pair is a limb through the torso on the bad frame. Support knee stays 2–34°. The minimum pelvisBack is a step, not a plant.
