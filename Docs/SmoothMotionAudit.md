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

## Pass 4

Fifty-seven presentation pairs the motor can actually enter are measured in `TransitionMatrix`. Each pair reports the worst one-frame bone gap and the visual-root jump. A stride step at or under 33° stays on the exponential gait slew, so the feet do not lag. A larger gap uses a wider spring: 0.12 s up to 180°, 0.16 s past that. The 0.71 m mantle exit stays on the 0.10 s position spring. A larger absorbed pop, still under the respawn gate, uses 0.18 s. Every measured pair is now under the pass-1 limits. Exit clips, the landing roll, and particles stay on the anim-fx lane. These blends finish on the destination pose, so that lane can take the body from there.

Worst ten pose pairs, raw one-frame gap then the sprung step. Root is the visual mesh jump on that same pair.

| Pair | Pose before | Pose after | Root before | Root after |
|---|---|---|---|---|
| air → punch | 228.8° | 17.4° | 0.000 m | 0.000 m |
| punch → air | 228.8° | 17.4° | 0.000 m | 0.000 m |
| air → dash | 214.8° | 16.4° | 0.000 m | 0.000 m |
| dash → air | 214.8° | 16.4° | 0.000 m | 0.000 m |
| slide → air | 192.8° | 14.7° | 0.000 m | 0.000 m |
| air → stagger | 190.8° | 14.5° | 0.000 m | 0.000 m |
| stagger → air | 190.8° | 14.5° | 0.000 m | 0.000 m |
| dash → climb | 190.0° | 14.5° | 0.000 m | 0.000 m |
| ground → pad | 182.1° | 13.9° | 0.000 m | 0.000 m |
| ground → air | 158.8° | 16.2° | 0.000 m | 0.000 m |

The largest root pair is mantle → ground (and vault → ground, the same exit write): 0.710 m down to 0.086 m. Climb → wall run and wall run → climb are pose handoffs, not a new motor edge. Vault → mantle is the same vault playing forward.

- Idle already breathed and shifted weight. It now looks around once a period, and the It stands a little taller than a runner. Crouch walk at 3.68 uses a cadence of 35.31 rad/s, so the short step does not skate. A slide leans, drags the trailing hand, and looks forward. Slide speed is unchanged. Sprint arm swing scales from 1.00 at a walk to 1.35 at 13.8. A tag or a punch flinch adds a head and chest snap on the existing 0.25 s stagger clock. It does not change stagger time.
- A cling slip at 3.7 scrabbles the hands and feet on top of the drag. A wall run plants both feet on the wall for the entry, then the plant eases off. Climb and wall run crossfade over 0.12 s when the wall mode changes.

Stick figures from the joint angles are in `Docs/SmoothStills/pass4/idle-wall-flinch.png`. Twelve rows, eight frames. Idle, crouch, slide, slip, wall handoff, and flinch. Before, then after.

## Pass 5

The owner can watch every verb in one scene, and the upper body can play on top of a live stride.

- `Assets/Scenes/MotionGallery.unity` is a row of Hier dummies. Each one runs the real motor and the real pose stack through one loop: run start, stop, and pivot; climb cycle and the top; wall run and wall jump; vault; mantle; slide; dash; punch and lunge; zip; pad; grapple pull and release; stagger and flinch; idle It beside an idle runner. A label sits over each dummy. The camera orbits the row. F switches to a free-fly camera. T sets `Time.timeScale` to 0.25 and the scene puts it back to 1 when it closes. Gameplay `slowMo` stays 0. The menu is Tag → Motion Gallery. Play stays the first enabled build scene. The gallery is listed after Boot and left disabled, so the smoke line stays `scenes=2`.
- A punch, a lunge tell, and a grapple aim move the arms and twist the spine toward the aim. While the pawn is running, sliding, airborne, or wall-running, the legs keep that cycle. A stand can still take a full-body punch. The lunge burst still commits the legs. Measured: run, slide, air, and wall keep legs (`upper-body run=1 slide=1 air=1 wall=1 idle=0 twist=36`).
- A zip hang adds sway and a leg trail that grow with ride speed and sit at 0 when the ride is stopped. At speed 14 the trail is −22° and the extra sway is 6°, on top of the authored sway of 7°. A grapple pull pitches the chest along the rope (18° when the rope is 40° up) and trails the legs (−16° at speed 14). A pad arc windmills the arms on the way up (28° at the launch rise) and the windmill is 0 on the way down. Zip speed, pull speed, and the pad arc are unchanged.
- The transition matrix was run again. All 57 pairs stay under the pass-1 limits. Worst pose is still air → punch, 228.8° down to 17.4°. Worst root is still mantle → ground, 0.710 m down to 0.086 m. `over=0`. The response line is still 0/0 on every verb.

Stick figures from the joint angles are in `Docs/SmoothStills/pass5/layer-hang.png`. Twelve rows, eight frames. Punch, lunge tell, grapple aim, zip hang, rope pull, and pad windmill. Before, then after.

Items 2–9 below stay as they are. Exit poses, the landing roll, landing tiers, and particles stay with the anim-fx lane.

## Pass 6

Controls. The press still happens on the same frame. Coyote, jump, speeds, and the other locked numbers are unchanged.

The reader runs at execution order −200, the cameras apply look at −100, and the motor moves at 0. Rendering is after LateUpdate. Keyboard and gamepad move were already sampled in that same Update as the capsule. Mouse look already reached the picture before the frame was drawn. The extra frame was the body heading: yaw was written in LateUpdate, so the first Move of a look still used the previous facing.

| Path | Before | After |
|---|---|---|
| Keyboard to first capsule move | 0 frames | 0 frames |
| Gamepad to first capsule move | 0 frames | 0 frames |
| Mouse look to the picture | 0 frames | 0 frames |
| Look to the move that uses the new heading | 1 frame | 0 frames |

`beforeMouse=1` and `beforeLook=1` in the sim line are that heading frame. After the change both are 0. Gamepad events are processed in the dynamic update, so a stick is not held for the fixed step. Nothing reads move or look from FixedUpdate. The fixed timestep stays 0.02. The rigidbody interpolation stays None. Vsync default is now 1, and while vsync is on the frame cap stays with the display (`targetFrameRate` −1). If vsync is off, the fallback cap is 60.

The joystick axes in the input manager used a per-axis deadzone of 0.19 and passed the rest of the stick through unchanged. A diagonal whose two components were both under 0.19 produced no move, and a stick at 0.20 came out as 0.20 (a step). Those axes are now deadzone 0. The reader applies a radial inner of 0.19, an outer of 1, and a linear curve. A full cardinal and a full diagonal both come out at magnitude 1.000. A stick at 0.20 comes out at 0.012. A stick at 0.50 comes out at about 0.383. Keyboard axes stay −1, 0, or 1, so a full key press stays magnitude 1. The motor still ignores a wish shorter than length 0.1, so the capsule starts once the raw stick magnitude is about 0.27, on a cardinal and on a diagonal alike. Forward on the stick counts as sprint once the shaped axis passes 0.40, which is a raw deflection of about 0.51. The sprint button and the W key are unchanged.

Mouse look is still the delta times sensitivity. There is no look smoothing and no mouse acceleration. Gamepad look has an accel curve that stays off (`lookAccel=0`). Turning it up eases small deflections and keeps a full stick at full speed. Inner deadzone, outer deadzone, the stick curve, and look accel are stored on `GameSettings` and in the settings blob (`stickInner`, `stickOuter`, `stickCurve`, `lookAccel`). The blob version stays 2, so an older file keeps these defaults. The pause menu still has 19 rows. The options screen can grow those rows later.

Items 2–9 were left for a later pass. Exit poses, the landing roll, landing tiers, and particles stay with the anim-fx lane.

`response-latency kb=0 mouse=0 pad=0 look=0 beforeKb=0 beforeMouse=1 beforePad=0 beforeLook=1 read=-200 cam=-100 motor=0 vsync=1 rate=-1 fixed=0.02`

`stick-quality diag=1.000 card=1.000 ramp=0.012 axialHole=0 inner=0.19 outer=1 curve=1 lookAccel=0`

## Pass 7

Feet, a blink on a kill-box, the vault exit, and mesh yaw. Speeds, jump, coyote, and the other locked numbers are unchanged. One `CharacterController.Move` per Update. No root motion.

A planted sole was shorter than the step at the retuned speeds. Walk and sprint already had a play cadence that covers 6.9 and 13.8. Crouch already had its own cadence at 3.68. The proof cadence stays capped, and that cap is the "before" measurement. Wall-run cadence stays 26.50, locked to the gait cap, so the planted foot got longer instead: the inner swing stays tucked, the stance uses the full step, and the trailing thigh adds 2.2°. A strafe or a backpedal still shortens the swing. The foot that is down keeps the full step. Steps at or under about 33° stay on the exponential slew.

Centimeters of slip per planted foot, before then after:

| Verb | Speed | Before | After |
|---|---|---|---|
| Walk | 6.9 | 11.4 cm | 0.0 cm |
| Sprint | 13.8 | 51.7 cm | 0.0 cm |
| Crouch | 3.68 | 61.7 cm | 0.0 cm |
| Wall run | 9.5 | 30.2 cm | 0.0 cm |

`foot-slide walk=11.4>0.0 sprint=51.7>0.0 crouch=61.7>0.0 wall=30.2>0.0 gameplayDelay=0 rootMotion=0`

A kill-plane fall and a practice restart still move the capsule on that frame. The mesh hides for 0.12 s and comes back, and the camera boom dips to 0.35 of its distance and eases out. Gallery resets, round start, and a safe spawn do not blink. Field of view, shake, and gameplay `slowMo` stay 0.

`respawn-blink hide=1.00 back=0.00 open=0.35 seconds=0.12 gameplayInstant=1`

The vault still lasts `mantleDuration` and still leaves at the speed you had on entry, at least the walk speed. The arc used to finish a full push past the lip, then the last frame wrote a quarter of that push and the body jumped backward about 0.71 m. The arc now ends on that stand point, so the last write is the point the body is already on. The transition matrix no longer has a root pop. Worst pose is still air → punch, 228.8° down to 17.4°. Worst root is a zero pair, ground → air, 0.000 m. `over=0`.

Landing on the same frame the ground is detected is not safe. `BunnyHopPose.Absorb(false, 0)` is 0 and `Absorb(false, 0.05)` is 1. A hop inside one sample (`ChainSeconds` is 1/60) must skip the thud. Playing the land on the grounding frame would play that thud into the hop. The wait stays.

Gallery mannequins and in-game mannequins already build a Hier visual (`DummyVisual_*`, or the Hier FBX, or the primitive fallback). The locomotor on the capsule yaws, bobs, and blinks that child. The capsule heading stays the camera yaw. A fast turn counter-rotates the mesh on that frame and eases it back over 0.10 s, so a 180 does not spin the mesh with the capsule. A pawn with no visual child and no limb rig still has nothing to turn. That case does not rotate the capsule.

Air dash stays 0.10 s. The pose still arrives over 0.06 s. The first pose is still the press frame. The slew stays 2800. Exit poses, the landing roll, landing tiers, and particles stay with the anim-fx lane.

Stick figures are in `Docs/SmoothStills/pass7/plant-blink-yaw.png`. Twelve rows, eight frames: walk, sprint, wall plant, vault exit, blink, and yaw. Before, then after.

`loco-polish`, `body-life`, `response-latency`, and `stick-quality` are unchanged. `runRate` stays 26.50. Enemy, pocket, and stack AI lines are unchanged. Hot-path allocs stay 0. Frame budget stays steady=ok.

## TODO still open

1. Done in pass 6. Small `LookRotation` steps ease. Large steps still snap with the mouse.
2. Done this pass for the skate. A stride step at or under about 33° stays on the exponential gait slew. Do not spring that step or the feet lag.
3. Done this pass. A kill-plane or practice restart blinks the mesh and the boom. The capsule still teleports. Do not ease a respawn across the park.
4. Wall-cling marks, slide scrape, dash ribbons, dust, and any new particle are the anim-fx lane (`cursor/tag-anim-fx`). That lane also owns the exit animation of each verb, the landing roll, and landing tiers.
5. Done this pass. Visual yaw eases on the Hier child. The capsule heading stays exact.
6. A mannequin with no Hier child and no limb rig still has no mesh to ease. The capsule is not rotated to fake it.
7. Done this pass. The vault arc ends on the stand point. Duration and exit speed are unchanged.
8. Air dash is 0.10 s long and the pose arrives over 0.06 s. The first visible pose is still the press frame. Do not put the slew back to 2800.
9. Landing still waits one sample. `Absorb(false, 0)` stays 0 so a hop inside 1/60 s skips the thud. Same-frame land is not safe.

## Pass 8

Climbing and parkour contact. The capsule still climbs at 6.0, slips at 3.7, and wall-runs at 9.5. One `CharacterController.Move` per Update. No root motion. Exit poses and particles stay with the anim-fx lane.

A climb plants one hand and the opposite foot on the wall, then swaps them with the reach. The old arm sweep covered less of the step than the body rose, so a hand slipped 60.1 cm and a foot slipped 82.8 cm. The plant now stays on the raycast point, so both are 0.0 cm. The other hand stays on the surface and keeps moving. A grab eases that contact in over the existing 0.10 s air blend, so the hands meet the wall instead of appearing on it.

The chest ray keeps a 0.42 m gap. On an uneven wall the old gap wandered by 4.2 cm. The offset cancels the dents, so the variance is 0.0 cm. The capsule is not moved.

A mantle and a vault keep `mantleDuration`. During the plant and the knee, each hand eases onto a downward ray on the lip, or onto the probe's stand point when the ray misses. The hands let go across the existing roll, so the chest comes over without a pop at the edge. A vault uses that same ray, so the palms sit on the rail's real top instead of a fixed 1.05 m. A lip at 1.40 m used to miss by 35.0 cm. It now misses by 0.0 cm.

A wall run rolls toward the wall by speed: 0 at rest, 10° at half of 9.5, 20° at 9.5. The entry uses the 0.10 s air blend. The exit uses the 0.10 s release blend. The inner foot and the inner hand pin to the wall. The outer leg keeps the stride.

A slip at 3.7 drags both hands down 11° and adds a 7° friction wobble. The scrabble on top of that is unchanged.

The gallery climb holds forward, then pulls back for the slip. The climb wall, the lip, the wall-run face, the vault rail, and the mantle ledge are the surfaces those rays hit.

`climb-contact hand=60.1>0.0 foot=82.8>0.0 chest=4.2>0.0 tilt=20.0 grab=1.00 lip=35.0>0.0 wobble=7.0 gameplayDelay=0 rootMotion=0`

Stick figures are in `Docs/SmoothStills/pass8/climb-contact.png`. Twelve rows, eight frames: climb, lip, vault, wall run, grab, and slip.

`loco-polish`, `body-life`, `foot-slide`, `respawn-blink`, `response-latency`, and `stick-quality` are unchanged. `climbRate` stays 16.50 and `runRate` stays 26.50. Enemy, pocket, and stack AI lines are unchanged. Hot-path allocs stay 0. Frame budget stays steady=ok.

## Pass 9

The body line through a wall jump, a rope, a zip, a pad, a landing, a dash tell, a sharp reversal, and a punch while moving. Speeds, jump, coyote, cling, dash 0.10/15/30, punch reach 1.55, climb 6.0, slip 3.7, wall-run 9.5, pad cooldown 0.3, zip 14/0.3, and the grapple (left hand, click-pull, double-click release 0.28 s) are unchanged. One `CharacterController.Move` per Update. No root motion. No new verbs. No ledge hang and no shimmy. Slide is untouched. Chase-cam `fovPop`, shake, and `slowMo` stay 0. Exit poses and particles stay with the anim-fx lane.

A wall jump still shoves for 0.15 s and still eases for 0.12 s. The old curve held the shove, then dumped the arm onto the tuck in one step of 35.6°. The arms now follow a raised cosine across the whole 0.27 s, so the peak step is 16.6°. Age 0 is still the shove, on the press frame.

On a rope at 40° the old hang bent the chest 70.0° off the line (hip 36 + spine 56 + the rope add). Hip and spine now share a correction that puts the chest on the rope, so the kink is 0.0°. Letting go fades that correction with the existing release, and the capsule still drops on the double-click.

A zip hang used to stop the arms at the cling reach, 58.0° short of the cable. They now rise onto the cable across the existing catch. Letting go used to snap the arms from that hang to the release in one frame, 128.0°. The drop now passes through a short release accent and into the air pose. The peak step is 53.2°. Ride speed stays 14.

A launch pad used to throw the arms from the walk rest to the full swing in one frame, 155.0°. The swing now opens over 0.16 s, so the first step is 30.6°. The knees ease toward the soft land as the arc rises, and the windmill uses the same open so it does not pop on frame 1. Pad cooldown stays 0.3.

A hard land still waits one sample. `Absorb(false, 0)` stays 0 so a hop inside 1/60 s skips the thud. Once the absorb plays, a sprint used to fold the thighs 104.0° off the stride. At the locked sprint those thighs now stay on the stride, and the absorb stays in the knees. A stand still takes the full absorb.

The dash tell used to put the full 0.42 sheen on in one frame. It now opens over 0.08 s, so the first mix is 0.16, and the wing and ankle ribbons use that same open. The dash is still 0.10 s. The first pose is still the press frame.

A sharp stick reversal used to sign-flip the thighs, an 82.6° gap at a sprint. The stride eases across 0.14 s, peak step 15.3°. The capsule heading stays instant. Turn-in-place was already on the pivot blend and is left alone.

A moving punch or tag leans the chest 22° at the locked sprint. The fist used to sit 12.9 cm behind that lead. It now sits on it. The arm stays on the authored strike. A stand does not lean. Reach stays 1.55, and the lunge stays 16/0.20/1.

`body-line wall=35.6>16.6 rope=70.0>0.0 zip=58.0>0.0 drop=128.0>53.2 pad=155.0>30.6 land=104.0>0.0 tell=0.42>0.16 rev=82.6>15.3 reach=12.9>0.0 gameplayDelay=0 rootMotion=0`

Stick figures are in `Docs/SmoothStills/pass9/body-line.png`. Twelve rows, eight frames: wall jump, rope, zip, pad, reversal, and the moving punch. Before, then after.

`loco-polish`, `body-life`, `foot-slide`, `respawn-blink`, `climb-contact`, `response-latency`, and `stick-quality` are unchanged. `climbRate` stays 16.50 and `runRate` stays 26.50. Enemy, pocket, and stack AI lines are unchanged. Hot-path allocs stay 0. Frame budget stays steady=ok. Mouse flicks still snap. Landing still waits one sample.

## Pass 10

Core locomotion. Visual only. Walk stays 6.9, sprint stays 13.8, crouch stays 3.68. Slide decay and `slideBoost` stay 0. Coyote, buffer, cling, jump, dash, punch, climb, slip, wall-run, pad, zip, and the grapple are unchanged. One `CharacterController.Move` per Update. No root motion. No new verbs. No ledge hang and no shimmy. Chase-cam `fovPop`, shake, and `slowMo` stay 0.

The sprint arm used a rectified swing, so the shoulder stepped 52.0° in one frame. It is now a continuous swing, about 46° at a sprint, opposite the same-side thigh. The peak step is 28.9°. A sprint also leans the chest 6.5° once it is up to speed. That lean eases in, so the first step is 0.7° instead of the whole 6.5°. The head keeps 65% of the hip bob out of the skull. The bounce steps 4.9 cm, then 1.7 cm. Foot travel is unchanged, so the sole still slides 0.0 cm. The stance sole used to snap 39.8° at contact. It now eases across 0.05 s, and the peak step is 19.7°.

A run start used to put the full 4.0° anticipation on the first frame. It now rises over one step (0.16 s) and settles as the stride opens. The peak step is 0.5°. A stop still plants the lead foot and then hands the bones to idle. The plant used to replace a sprint thigh in one frame, a 32.0° gap. It now opens over 0.14 s, peak step 5.9°.

Turn lean follows the turn rate. A sharp turn while walking eases the outside foot onto the plant instead of cutting it there. The old gap was 51.8°. The peak step is 12.9°. Idle already breathes and shifts weight. The look-around used to be a 14.0° snap. The sine peaks at 2.4° per frame. A crouch walk at 3.68 keeps the low stride. The contact kink stepped 7.8°. Blending that kink steps 6.4°. Foot slide on the crouch stays 0.0 cm.

A slide still decays on the old ground decel. The drop and the rise are a raised cosine over 0.22 s, so the head steps 5.9° instead of 12.0°. `SlideBlendSeconds` stays 0.10. The capsule is not delayed.

`loco-feel stride=52.0>28.9 foot=39.8>19.7 slideCm=51.7>0.0 lean=6.5>0.7 start=4.0>0.5 stop=32.0>5.9 turn=51.8>12.9 idle=14.0>2.4 crouch=7.8>6.4 drop=12.0>5.9 head=4.9>1.7 gameplayDelay=0 rootMotion=0`

Stick figures are in `Docs/SmoothStills/pass10/locomotion.png`. Fourteen rows, eight frames: stride, start, stop, turn, idle, crouch, and slide. Before, then after.

The pass 9 row labels were a 5-by-7 bitmap that had no J, P, Z, D, or V, so WALL JUMP, ROPE, ZIP, PAD, and REVERSAL drew as fragments. Stills now rasterize Liberation Sans from `Tools/StrafeJumpSim/Fonts`. `Docs/SmoothStills/pass9/body-line.png` was rendered again with those words.

`loco-polish`, `body-life`, `foot-slide`, `respawn-blink`, `climb-contact`, `body-line`, `response-latency`, and `stick-quality` are unchanged. `climbRate` stays 16.50 and `runRate` stays 26.50. Enemy, pocket, and stack AI lines are unchanged. Hot-path allocs stay 0. Frame budget stays steady=ok. `transition-matrix` stays `over=0`. Mouse flicks still snap. Landing still waits one sample.
