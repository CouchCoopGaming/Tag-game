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

## TODO for the next pass

1. Chase boom snap-in. `ChaseCam.BoomDistance` returns the shorter distance on the same frame, and the proof requires that. A visual-only ease has to stay out of the wall. Do not change the proof's snap-in check until a collision-safe ease exists.
2. The lens `LookRotation` is still written every frame. The look point is smooth. The rotation step is whatever the point did. Worth a pass if the horizon still ticks.
3. `TpsMoveCamera.AddKick` still adds a small field-of-view kick on punch and tag. The chase-cam constants `fovPop`, `shake`, and `slowMo` stay 0. The kick is a separate write.
4. Gait slews of 18–64 are a couple of frames, on purpose. Do not spring them or the feet lag the stride.
5. Kill-box and other `Place` calls still snap the mesh. They should. Do not ease a respawn across the park.
6. Horizontal ledge pops that do not change move state are not absorbed. Only the vertical part of a steady-state correction is. A later pass can detect a one-frame horizontal spike that is not "I am pushing this wall."
7. Wall-cling marks, slide scrape, and dash ribbons still appear in one frame. They are tells, not the body.
8. If the locomotor sits on the capsule instead of a child, the mesh yaw is skipped so it cannot fight the camera yaw. Those pawns still snap their facing with the camera.
9. An unbound mannequin (no limb rig) does not run the visual offset. The pill is hidden, but a failed bind would still pop.
10. The vault capsule still teleports on the last mantle frame (`transform.position` plus the one Move). The mesh hides it. Folding that write into the Move would move the capsule, which can move the AI lines. Leave it until a visual-only path is not enough.
11. Air dash is 0.10 s long and the pose now arrives over 0.06 s. If a dash reads late in play, shorten only that spring. Do not put the slew back to 2800.
12. Landing still waits one sample so a hop can cancel the thud. That is not input delay. Leave it unless the hop and the land both read wrong.
