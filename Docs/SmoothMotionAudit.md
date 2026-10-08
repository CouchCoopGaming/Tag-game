# Smooth motion audit (pass 1)

Ranked list of one-frame jumps in the visible body, the camera, or the capsule. Judged at 60 Hz. An exponential bone slew of 170 closes about 94% of the gap in one frame. A slew of 1400 or more closes all of it. A position write that is not `velocity * dt` is a pop. A camera offset added in one frame is a pop.

Gameplay stays instant. The capsule still takes one `CharacterController.Move` per Update. Feel locks are unchanged: coyote 0.10, jump buffer 0.16, cling grace 0.08, jump speed 24.7, rising gravity 22, fall gravity 1.62, terminal 56.16, walk 6.9, sprint 13.8, slide boost 0, air dash 0.10 / 15 / cooldown 30, punch reach 1.55, lunge 16 / 0.20 / cooldown 1, lunge tell 0.45, climb 6.0, slip 3.7, wall-run 9.5, stagger 0.25 / 0.50, pad cooldown 0.3, zip 14, regrab 0.3, tag-back immunity 1.0. Vertical jump height still does not depend on speed. No new verbs. No ledge hang.

The measured column is the largest change in one frame, before this pass and after. Pose and yaw are degrees. Position is meters. Source is `smooth-detail` from `Tools/StrafeJumpSim`.

## Fixed this pass

| Rank | What jumped | Where | Before | After |
|---|---|---|---|---|
| 1 | Punch arm | `SealPunchRight` slew 2400 | 110.0° | 21.8° |
| 2 | Air-dash chest | `AirDashPose.Slew` 2800 | 82.0° | 16.2° |
| 3 | Vault thigh | `MantlePose.Slew` 1600 | 102.0° | 20.2° |
| 4 | Grapple latch / miss | slew 2400 | 90.0° | 17.8° |
| 5 | Crouch into slide | `SlideHandoffSlew` 3600 | 68.0° | 13.5° |
| 6 | Crouch stance | `CrouchPose.Slew` 1400 | 64.0° | 12.7° |
| 7 | Hard visual yaw | wall attach / leaving it | 90° / 180° | 10.9° / 21.7° |
| 8 | Vault exit pop | `TickMantle` writes `transform.position` | 0.710 m | 0.086 m |
| 9 | Stair step | controller step offset 0.20 m | 0.200 m | 0.024 m |
| 10 | Ledge / skin | vertical correction 0.35 m / 0.08 m | 0.350 m | 0.042 m |
| 11 | Stagger | slew 280 | 47.5° | 9.5° |
| 12 | Jump takeoff | slew 170 | 37.6° | 7.9° |
| 13 | Wall lean | slew 170, ±20° into the wall | 18.8° | 4.0° |
| 14 | Air-strafe lean | slew 140 | 19.9° | 4.4° |
| 15 | Camera duck and wall offset | slide look −0.32, climb +0.28, wall-run look +0.42 | instant | 0.10–0.12 s spring |
| 16 | Landing buckle | squash assigned 0.55–1.35 on the arm frame | full in one frame | eases in over 0.06 s |
| 17 | Input edge | `PunchHitbox.Update` could run before `Read` | up to 1 frame late | same frame |

How the fixes work:

- Bone slews at or above 140 use a critically damped spring of 0.06 s (jump, punch, dash, vault, wall, grapple latch). Slews from 70 to 140 use 0.12 s. Gait slews at 64 and under stay on the old exponential so the run does not lag. The public slew constants are unchanged, so the old pose proofs still hold. The spring is what the mesh does.
- The capsule position is exact. A visual offset on the mannequin root eats a step, a skin push, or a state-change pop (the vault exit write) and eases back over 0.10 s. Pushing a wall every frame is not hidden, so the mesh does not sink into it. A teleport of 1.25 m or more (the kill box) still snaps the mesh with the capsule.
- On a climb the mesh yaws toward the wall. On a wall run it yaws along the wall, matching the run direction. That yaw eases over 0.10 s. The capsule keeps the camera yaw, so wish direction, cling, and the jump do not wait. Ground strafe still faces the camera. Slide is left on the capsule facing.
- The chase camera eases its look height and the wall-run sideways offset. Look-ahead direction still uses the existing catch-up. Field of view pop, shake, and slow motion stay 0.
- The landing squash eases in. The hold and the release back into the run are the same clock as before. A buffered hop still skips the thud.
- `PlayerInputReader` reads at execution order −200, once per frame. The motor still calls `Read` in the same Update. The second call does nothing, so the edge is not eaten. Punch, jump, and the other verbs see the press on the frame it happens. Nothing waits on an animation.

Slide entry, slide stay, and the slide jump cap were not touched. The crouch-to-slide *pose* is softer. The slide speed is the same.

## Already smooth, left alone

- `PoseHandoff` weight curves, 0.08–0.18 s, weights on an edge sum to 1.
- Gait, idle, and pivot slews (about 18–46). Turn lean and accel lean already ease.
- Chase boom lengthening, and look-ahead direction. The boom still shortens in one frame when a wall is inside it. That is the collision pull, and `ChaseCam.Holds` locks it.
- Bunny-hop chain still skips the land thud so the next jump is not a squat.

## State transitions, what the eye does now

- Ground to air, and air to ground: the jump and land poses ease. The land mesh dips instead of buckling in one frame.
- Cling / climb start and stop: the wall pose eases, and the mesh yaws onto the wall instead of popping its chest to the normal.
- Wall-run start and stop: same yaw ease, plus the 20° lean eases. The camera's sideways look offset eases with it.
- Wall jump: the push pose eases. Jump height is unchanged.
- Vault / mantle enter and exit: the thigh and chest ease. The exit position write is still on the capsule. The mesh does not pop with it.
- Slide enter and exit: mechanics unchanged. A crouch into the slide pose eases. A run into the slide was already a 0.10 s weight curve on a gait slew.
- Zip grab and drop: the hang pose was a weight curve. The bone slew on that curve no longer snaps the arms.
- Pad launch: the launch pose follows the same spring rule when its slew is in the snap band. The arc speed is unchanged.
- Grapple attach, pull, and release: latch and miss no longer slam the arm. The pull speed is unchanged. Left hand, click to pull, double-click to release at 0.28 s.
- Punch and lunge: the punch arm eases in about 0.06 s from the frame of the click. Reach, lunge speed, and the tell are unchanged.
- Stagger: the stumble eases. 0.25 s and the 0.50 s immunity are unchanged.
- Landing: see the buckle above.

The frame strip is `Docs/SmoothStills/pass1/transitions.png` (and the same pixels as `transitions.ppm`). Six rows, sixteen frames at 60 Hz. Top to bottom: air-dash chest before, air-dash chest after, wall lean before, wall lean after, vault-exit position before, vault-exit position after. A full bar is the old snap. The after rows grow instead of appearing full on frame one.

## Pass 2

Climbing and the parkour poses now play as cycles, and the mesh follows those curves instead of lagging a second spring behind them. Gameplay is still the same frame as the press. Feel locks are unchanged.

- A climb at 6.0 is hand-over-hand. The reaching hand plants, holds, then the other hand takes the next grip. The rhythm is `WallPose.ClimbRate`, full at 16.5 rad/s when vertical speed is 6.0. A cling that is barely moving holds both hands on the wall. Dropping at the slip speed 3.7 drags both hands. The first part of a grab aims at an entry pose, hands coming up, then the cycle or the hold.
- A wall run at 9.5 keeps the stride. The mesh tracks it on the gait slew (64), so the outer arm and the outer leg keep about 93% of the swing. The old live slew was 42.
- Vault, wall-jump push-off, zip catch, and the pad arc follow their authored curves on that same gait slew. The public slew constants are unchanged, so the old pose proofs still hold. Exit animations and the landing roll are not in this pass.
- A one-frame horizontal ledge correction is hidden on the mesh the same way a step is. Pushing a wall every frame is not hidden, so the mesh does not sink into it. Measured: a 0.350 m one-frame ledge moves the mesh 0.000 m on that frame. A repeated 0.120 m push still moves the mesh 0.120 m.
- The chase boom still shortens in one frame when a wall is inside it. Lengthening was already an ease, and it was fast (rate 18, about a meter of a full boom on frame one). The re-extension rate is now 6. The pull-in check in `ChaseCam.Holds` is unchanged.
- A punch or a tag no longer punches the field of view. The camera offset eases in over 0.06 s and then settles. `fovPop`, `shake`, and `slowMo` stay 0.
- Every verb's first visible pose is the same frame as the press. The smoothing does not insert a windup. See the response line from the sim.

Stick figures drawn from the joint angles are in `Docs/SmoothStills/pass2/climb-wall-mantle.png`. Six rows, eight frames. Climb before and after, wall run before and after, mantle before and after. Before is the pass-1 filter. After is the cycle track. The wall is the line on the right of the climb and wall-run frames.

## Pass 3

The run, the jump, and the contact poses fill in. Gameplay is still the same frame as the press. Feel locks are unchanged. Exit animations, the landing roll, and particles stay on the anim-fx lane.

- The mesh cycle follows walk 6.9 and sprint 13.8, so the sole covers the stance. The proof cadence in `GaitBlend.CadenceAt` is unchanged and still caps at 26.5. Measured slip at those two speeds is 0.000. The old proof slip at 13.8 stays about 0.316.
- A run start leans the chest into the first step. A stop still plants and settles on the curve that was already there. A heading change of about 180° inside 0.28 s plants the outside foot for a short beat, then eases. A slow turn stays on the existing pivot. Strafe shortens the fore-aft step and yaws the hips toward travel. Backpedal flips the stride and shortens it. Straight ahead is the same stride as before.
- Air poses still come from vertical speed: takeoff, rise, then fall. The apex adds a float tuck that `JumpPose.Extend` does not have. A bunny-hop chain keeps the arms near a balance pose instead of replaying the full takeoff. Air-strafe banks stay as they were, on that same chain.
- On the ground, the stance foot pitches to the slope and lifts a few centimeters on a step. On a climb or a wall run, a ray from the chest adds arm pitch so the hands meet the wall. The capsule and `CharacterController.Move` are unchanged.
- The head looks along travel, up to 35°. The spine counters at about −0.4 of that. The arms coast up to 12° after the swing drops. All three use the 0.10 s spring. Punch, aim, mantle, climb, and wall run keep the bones they already own.
- A small chase look-point step eases. A mouse flick, and the boom pulling in, still snap. Gait slews of 18–64 stay exponential.

One frame of the hard-turn plant moves the thigh 2.7° where the unsmoothed gap is 38°. One frame of the apex tuck moves 1.1° of a 10° add. Both show on the press frame. The press-to-first-visible-pose table is unchanged: every verb is 0/0. `gameplayDelay=0`.

Stick figures from the joint angles are in `Docs/SmoothStills/pass3/loco-air-ik.png`. Twelve rows, eight frames. Sprint, strafe, air, hard turn, slope, and wall hand. Before, then after.

## TODO still open

1. Done this pass. Small `LookRotation` steps ease. Large steps still snap with the mouse.
2. Gait slews of 18–64 are a couple of frames, on purpose. Do not spring them or the feet lag the stride.
3. Kill-box and other `Place` calls still snap the mesh. They should. Do not ease a respawn across the park.
4. Wall-cling marks, slide scrape, dash ribbons, dust, and any new particle are the anim-fx lane (`cursor/tag-anim-fx`). That lane also owns the exit animation of each verb and the landing roll.
5. If the locomotor sits on the capsule instead of a child, the mesh yaw is skipped so it cannot fight the camera yaw. Those pawns still snap their facing with the camera.
6. An unbound mannequin (no limb rig) does not run the visual offset. The pill is hidden, but a failed bind would still pop.
7. The vault capsule still teleports on the last mantle frame (`transform.position` plus the one Move). The mesh hides it. Folding that write into the Move would move the capsule, which can move the AI lines. Leave it.
8. Air dash is 0.10 s long and the pose arrives over 0.06 s. The first visible pose is still the press frame. Do not put the slew back to 2800.
9. Landing still waits one sample so a hop can cancel the thud. That is not input delay. Leave it unless the hop and the land both read wrong.
