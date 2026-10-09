# Evasion moves (lane E)

Four ball-carrier moves, approved by Landon: stutter step, spin, side juke, and dive. They are ways to slip a tag. There is no hurdle, stiff arm, or truck.

The gameplay flag is `EvasionMoves.Enabled`. It starts **false**. With the flag off, the motor does not read the moves and the mannequin does not play them. Jump height stays on the jump button. Coyote 0.10, jump buffer 0.16, cling grace 0.08, jump speed 24.7, terminal 56.16, roll at 65% of terminal, walk 6.9, crouch 3.68, and sprint 13.8 are unchanged.

Pad gestures call `TryRaise` only while `EvasionMoves.Enabled` is true. The flag starts off, so a normal match does not run them. No keyboard key is bound.

## What each move does

Speeds are pawn-forward and pawn-right. The sprint cap is 13.8. A move may brake and come back up to that cap. It does not go past it. Root motion is off: the clips do not move the capsule. The motor writes the translation while the flag is on.

| Move | Gameplay | Whiff | Cooldown |
|---|---|---|---|
| Stutter | Hard brake to 35% of entry speed for 0.18 s, then ground accel (52) back up to the cap. Shoulders stay square. | The brake itself. A reach on the lane still tags. | 0.75 s |
| Spin | 0.35 s. Forward speed held at 90% of entry. Lateral slip 0.92 m so a centered reach misses. A reach aimed at the new spot still hits. | 0.12 s after the turn, still at the held speed. | 0.90 s |
| Juke | 1.40 m sideways over 0.22 s. Forward speed drops so the combined speed stays under the cap. | 0.12 s after the cut, still at the cut's forward speed. | 0.65 s |
| Dive | 3.00 m straight ahead at the cap (about 0.22 s), then 0.60 s at 40% speed to get up. No vertical impulse. | The whole 0.60 s recovery. A reach that covers the lane still tags. | 1.80 s |

Entry is 0.10 s of pose blend before the move owns speed. Exit is 0.12 s of pose blend back toward the run after the move lets go. The motor is on the normal sprint cap again once the move ends. Airborne `TryRaise` is rejected for stutter, spin, and juke. A dive may start in the air. A second dive inside the cooldown is rejected.

The tagger's existing punch volume is unchanged: reach 1.55, width 0.85, height 1.40, no new i-frame. While the flag is on, the capsule actually steps, so a box that was on the old spot can miss, and a box that still overlaps the body still tags.

## Clips

In-place clips on the shipped Tan Hier mannequin. Root motion is off, so the capsule stays on the motor's path. `Sample.Drop` lowers the hips bone. The capsule and the visual root stay where the motor put them. The motor never reads the drop. Each clip is an entry, the move, and an exit back toward a run.

Joined parent/child overlap is `rigJoint`, at any depth, and it belongs to the rig lane. A knee at 55° or a hip pitched forward deepens those cuffs, and that depth is not a pose fail. `pose` is only non-adjacent pairs (chest into an arm, spine into a thigh, hand into the other forearm) plus anything into the floor. The absolute limit is 0.5 cm. Nothing was lifted, damped, or pushed apart to clear it. The pelvis bone is keyed down. That is a pose, not a capsule or root offset. The support sole stays within 0.5 cm.

Plants keep the support thigh near 48°. Past that the upper-leg mesh passes through the spine. The dive roll-up goes to 55° anyway, because the crouch bar is 12 cm behind the foot, and that overlap is logged as rig-blocked. A straight leg leaned backward is not used: the knee stays bent, and the shin points forward so the knee is over or ahead of the ankle.

| Clip | Read |
|---|---|
| `stutter` | Both plant frames sit. Hips are 14° over a 4° spine, the pelvis is 10.4 cm down, and the plant knee is 55°. The support sole is within 0.21 cm on all 12 move frames, including the two chops that used to leave the floor. Shoulders stay square. |
| `spinL`, `spinR` | The pivot thigh is 48° and the knee is 55°. The pelvis is 10.4 cm down and the sole is within 0.21 cm on all 11 move frames. The trunk does not lean into the turn. A lean swings the pivot foot behind the pelvis. The head still leads the yaw. At a quarter turn and a half turn the existing overlap test reports the spine 1.4 cm into the pivot thigh. That pair is rig-blocked and was not retuned. |
| `jukeL`, `jukeR` | The outside thigh is 48°, the knee is 55°, and the pelvis is about 9 cm down. The outside sole is within 0.29 cm on all 7 move frames. The head turns into the cut. The spine stays square, because a spine yaw puts it through the thigh. `jukeR` still reports the spine 0.64 cm into the outside thigh (0.55 cm at 0.333 s). `jukeL` does not. The right thigh mesh sits closer to the spine than the left one at the same angle. `jukeR` is rig-blocked on that pair and was not retuned. |
| `dive` | One planted push-off, then a forward stretch. The plant is hips 14°, knees 50°, pelvis 9.3 cm down, sole within 0.2 cm. From 0.17 s through 0.53 s the chest is at 58° over a 4° spine, the arms reach ahead, and the legs trail with the feet about 40 cm below the hips. The knees fold while the chest is still leaning, the forearms meet the floor, then the body rolls to a shoulder and back up. No leg is kicked overhead. Root motion stays off. The roll-up crouch is thigh 55°, knee 79°, hips 6° over a 4° spine. At 0.867 s and 0.900 s the pelvis is 13.6 cm behind the support foot and the sole is at 0.2 cm. That clears the 12 cm crouch bar and the 20 cm drop bar. The spine mesh is 1.74 cm inside both thighs there. That overlap is rig-blocked. |

## Bindings

Landon picked the pad gestures. Juke, spin, the stutter double-tap, and the airborne dive flick call `TryRaise` only while `EvasionMoves.Enabled` is true, so with the flag off a stick does nothing new. Keyboard keys are proposals only. Nothing on the keyboard was bound.

| Move | Pad | Keyboard proposal | Status |
|---|---|---|---|
| Juke | Right-stick flick left or right. The sign is the flick. | `X` | Wired for the pad. `X` is not bound. |
| Spin | Right-stick half circle. Clockwise on the stick is the clockwise spin (`spinR`). The other way is `spinL`. | `B` | Wired for the pad. `B` is not bound. |
| Stutter | Double-tap RT inside 0.25 s. The second tap's left-stick lean is the side. A centered stick uses the lateral move, or a straight plant when there is none. | `Z` | Prototype behind the flag. LT stays the couch rope. |
| Dive | Airborne right-stick flick forward, same return rule as the juke. A grounded flick, and a tilt that stays forward, stay look. | `R` | Prototype behind the flag. No modifier. `R` is free and not bound. |

A flick is a sideways deflection past 0.85 that is back near the center, and stopped, within 0.15 s. A half circle sweeps at least 150° while the stick is past 0.70 deflection, inside 0.35 s. Looking up or down does not count as sideways, and a stick that stays out past 0.15 s is a look, including the release after that hold.

The camera keeps ordinary look. On the frame a juke, a spin, or a dive actually starts, that sample of stick look is replaced by the reverse of the stick samples from the gesture, so the move does not also yaw or pitch. A pan is not reversed. Scripted traces, each at 30 Hz, 60 Hz, and 120 Hz:

`evasion-gestures cameraFP=0/72 moveFN=0/24 swallow=commit diveFN=0/12 stutterFP=0/15 stutterFN=0/15`

The 72 camera traces are the old 60 (slow pans, fast pans that hold, pans that cross the stick, snaps that look behind, a snap that holds and then releases, looking up and down held and as a flick, tracking a runner, a short rim slide, a slow out-and-back, and a wobbly pan) plus four airborne look-ups at each of 30, 60, and 120 Hz: a slow tilt, a held tilt, a fast tilt that holds, and a tilt that releases after more than 0.15 s. None of them raised a move. The 24 move traces are left and right flicks and both half circles. All of them raised the matching move. The 12 dive traces are airborne forward flicks. All of them raised a dive. Zero false positives on that camera set did not require a modifier, so none is bound.

Stutter is a prototype behind the flag. LT stays the couch rope (`JetHeld` / `LeftTriggerHeld`). LB stays sprint. RB stays air dash. RT is a double-tap, not a PlayAction bind in the input map. A single press, a hold, and presses more than 0.25 s apart do not fire.

Dive is a prototype behind the flag. It is the juke's flick rule on the forward axis, and only while airborne. Holding the stick forward stays look. RT is the stutter tap, so it is not a dive modifier.

## Proof

`evasion-moves stutter=0.35 spin=0.90 juke=1.40 dive=3.00 capOK=1 rootMotion=0 flagDefault=off`

`stutter` is the brake fraction, `spin` is the held speed fraction, `juke` is the lateral meters, `dive` is the flight meters.

No-clip, every 30 fps frame of the six clips. A pair that shares a joint is `rigJoint`. `pose` is non-adjacent pairs plus the floor, and it is 0 when every one of those is at or under 0.5 cm:

`no-clip clips=6 frames=115 worldMax=0.0 rigJoint=7.89 pose=1.74 fails=1`

`rigJoint` is the hip/thigh cuff, owned by the rig lane. The floor is clear. `pose` is not 0. The deepest report is 1.74 cm, the spine into both thighs on the dive roll-up. The spin still reports 1.4 cm of that pair, and `jukeR` reports 0.64 cm. Those three stay rig-blocked. Knee and hip joint overlap stays `rigJoint`.

Support soles, within 0.5 cm. Planted frames and the max gap, in centimetres:

`ground-contact stutter planted=12/12 maxGap=0.21 frames=0.100:L:0.21,0.133:L:0.21,0.167:L:0.21,0.200:R:0.21,0.233:R:0.21,0.267:L:0.21,0.300:L:0.21,0.333:R:0.21,0.367:R:0.21,0.400:L:0.21,0.433:L:0.21,0.467:L:0.21`

`ground-contact spinL planted=11/11 maxGap=0.21 frames=0.100:R:0.21,0.133:R:0.21,0.167:R:0.21,0.200:R:0.21,0.233:R:0.21,0.267:R:0.21,0.300:R:0.21,0.333:R:0.21,0.367:R:0.21,0.400:R:0.21,0.433:R:0.21`

`ground-contact spinR planted=11/11 maxGap=0.21 frames=0.100:L:0.21,0.133:L:0.21,0.167:L:0.21,0.200:L:0.21,0.233:L:0.21,0.267:L:0.21,0.300:L:0.21,0.333:L:0.21,0.367:L:0.21,0.400:L:0.21,0.433:L:0.21`

`ground-contact jukeL planted=7/7 maxGap=0.29 frames=0.100:L:0.29,0.133:L:0.28,0.167:L:0.22,0.200:L:0.21,0.233:L:0.27,0.267:L:0.29,0.300:L:0.26`

`ground-contact jukeR planted=7/7 maxGap=0.29 frames=0.100:R:0.29,0.133:R:0.28,0.167:R:0.22,0.200:R:0.21,0.233:R:0.27,0.267:R:0.29,0.300:R:0.26`

Hip-sit on the stutter, juke, and spin plants, on the dive push-off, and on the dive roll-up. The airborne stretch is not a plant. `pelvisBack` is how far the pelvis sits behind the support foot. `hinge` is hip flexion over lumbar flexion. `kneeMin` is support-knee flexion. `pelvisDrop` is how far the pelvis bone is below its standing height. The roll-up frames clear the crouch bar: 13.6 cm behind the foot, knee 79°, drop at least 20 cm, sole 0.2 cm. `pelvisBackMin` is the lowest passing plant, not the roll-up.

`hip-sit clips=6 loadedFrames=115 pelvisBackMin=9.27 cm hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.9 cm fails=0`

Stills for the forward dive and the seated roll-up are in `Docs/Movement/evasion-pass6/`. `before/` is the earlier crouch hop and the 9.0 cm roll-up. `after/` is the lean and reach, and the roll-up with the pelvis 13.6 cm behind the foot. Side views have a vertical line through the support foot and a dot on the pelvis. Three-quarter views cover 0.17 s, 0.35 s, and 0.52 s. Pass 5 is still in `Docs/EvasionStills/pass5/`. Pass 4 is still in `Docs/EvasionStills/pass4/`. Pass 3 is still in `Docs/EvasionStills/pass3/`. Pass 2 is still in `Docs/EvasionStills/pass2/`. Pass 1 is still in `Docs/EvasionStills/pass1/`.
