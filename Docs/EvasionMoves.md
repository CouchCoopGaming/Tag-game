# Evasion moves (lane E)

Four ball-carrier moves, approved by Landon: stutter step, spin, side juke, and dive. They are ways to slip a tag. There is no hurdle, stiff arm, or truck.

The gameplay flag is `EvasionMoves.Enabled`. It starts **false**. With the flag off, the motor does not read the moves and the mannequin does not play them. Jump height stays on the jump button. Coyote 0.10, jump buffer 0.16, cling grace 0.08, jump speed 24.7, terminal 56.16, roll at 65% of terminal, walk 6.9, crouch 3.68, and sprint 13.8 are unchanged.

Nothing in this pass binds a key, a mouse button, or a pad button. `TryRaise` is the only way to start a move, and no input reader calls it.

## What each move does

Speeds are pawn-forward and pawn-right. The sprint cap is 13.8. A move may brake and come back up to that cap. It does not go past it. Root motion is off: the clips do not move the capsule. The motor writes the translation while the flag is on.

| Move | Gameplay | Whiff | Cooldown |
|---|---|---|---|
| Stutter | Hard brake to 35% of entry speed for 0.18 s, then ground accel (52) back up to the cap. Shoulders stay square. | The brake itself. A reach on the lane still tags. | 0.75 s |
| Spin | 0.35 s. Forward speed held at 90% of entry. Lateral slip 0.92 m so a centered reach misses. A reach aimed at the new spot still hits. | 0.12 s after the turn, still at the held speed. | 0.90 s |
| Juke | 1.40 m sideways over 0.22 s. Forward speed drops so the combined speed stays under the cap. | 0.12 s after the cut, still at the cut's forward speed. | 0.65 s |
| Dive | 3.00 m straight ahead at the cap (about 0.22 s), then 0.60 s at 40% speed to get up. No vertical impulse. | The whole 0.60 s recovery. A reach that covers the lane still tags. | 1.80 s |

Entry is 0.10 s of pose blend before the move owns speed. Exit is 0.12 s of pose blend back toward the run after the move lets go. The motor is on the normal sprint cap again once the move ends. Airborne `TryRaise` is rejected. A second dive inside the cooldown is rejected.

The tagger's existing punch volume is unchanged: reach 1.55, width 0.85, height 1.40, no new i-frame. While the flag is on, the capsule actually steps, so a box that was on the old spot can miss, and a box that still overlaps the body still tags.

## Clips

In-place clips on the shipped Tan Hier mannequin. Root motion is off, so the capsule stays on the motor's path. `Sample.Drop` is a visual pelvis offset on the mannequin only. The motor never reads it. Each clip is an entry, the move, and an exit back toward a run.

Joined parent/child overlap is `rigJoint`, at any depth, and it belongs to the rig lane. A knee at 55° or a hip pitched near horizontal deepens those cuffs, and that depth is not a pose fail. `pose` is only non-adjacent pairs (chest into an arm, spine into a thigh, hand into the other forearm) plus anything into the floor. The absolute limit is 0.5 cm. Nothing was lifted, damped, or pushed apart to clear it.

| Clip | Read |
|---|---|
| `stutter` | Unchanged from the planted pass. Hips sit about 15 cm down, then about 22 cm on the burst. The plant knee is about 50° and the swing knee about 68°. On 9 of the 12 move frames a support sole is within 0.44 cm. Two switches are airborne, about 10 cm. One late plant sole is 0.52 cm up, just outside the band. The plant ankle is flatter, so the foot-box corner that was 0.42 cm through the floor is now 0.12 cm. Shoulders stay square. |
| `spinL`, `spinR` | Hips stay 12.5 cm under the run for the whole turn. The pivot sole is planted on every move frame, within 0.43 cm, and the free foot stays up. The trunk leans 16° into the turn and the chest pitches over the pivot. The inside forearm folds across the front of the chest. The outside arm stays bent and close, a counterweight, not a straight wing. |
| `jukeL`, `jukeR` | The outside foot plants about 70–75 cm from the inside foot, shoulder width plus about 25 cm. The head and spine reach 15–18° into the cut while the hips are still square, then the hips yaw. The hips sit about 18 cm down. The outside sole is within 0.47 cm on every move frame. A foot-box corner is 0.11 cm through the floor. |
| `dive` | Bone rotations only. There is no visual-root bank. At 0.33 s the hands are on the floor (0.3 cm), the chin is tucked, and the hips are still 0.58 m up, so it is a reach and not a belly flop. The right upper arm is the low point by 0.57 s (0.8 cm). The left thigh is the low point at 0.70 s (0.1 cm). At 0.87 s the body is a crouch, knees bent, feet down, and the hands are 36 cm off the floor. Flight is still 3.00 m and the recovery is still 0.60 s. The resting fist is what meets the floor. An open palm still pulls the upper arm through the chest. |

## Bindings

Landon picked the pad gestures. Juke and spin are wired. They call `TryRaise` only while `EvasionMoves.Enabled` is true, so with the flag off a stick does nothing new. Keyboard keys are proposals only. Nothing on the keyboard was bound.

| Move | Pad | Keyboard proposal | Status |
|---|---|---|---|
| Juke | Right-stick flick left or right. The sign is the flick. | `X` | Wired for the pad. `X` is not bound. |
| Spin | Right-stick half circle. Clockwise on the stick is the clockwise spin (`spinR`). The other way is `spinL`. | `B` | Wired for the pad. `B` is not bound. |
| Stutter | Double-tap RT inside 0.25 s. The second tap's left-stick lean is the side. A centered stick uses the lateral move, or a straight plant when there is none. | `Z` | Prototype behind the flag. LT stays the couch rope. |
| Dive | Not bound. | `R` | Not recognized. See below. `R` is free and not bound. |

A flick is a sideways deflection past 0.85 that is back near the center, and stopped, within 0.15 s. A half circle sweeps at least 150° while the stick is past 0.70 deflection, inside 0.35 s. Looking up or down does not count as sideways, and a stick that stays out past 0.15 s is a look, including the release after that hold.

The camera keeps ordinary look. On the frame a juke or spin actually starts, that sample of stick look is replaced by the reverse of the stick samples from the gesture, so the move does not also yaw. A pan is not reversed. Scripted traces, each at 30 Hz, 60 Hz, and 120 Hz:

`evasion-gestures cameraFP=0/60 moveFN=0/24 swallow=commit dive=unbound stutter=unbound`

The 60 camera traces are slow pans, fast pans that hold, pans that cross the stick, snaps that look behind, a snap that holds and then releases, looking up and down held and as a flick, tracking a runner, a short rim slide, a slow out-and-back, and a wobbly pan. None of them raised a move. The 24 move traces are left and right flicks and both half circles. All of them raised the matching move. Zero false positives on that camera set did not require a modifier, so none is bound.

Stutter stays off the triggers. LT is already the couch rope: `JetHeld` on a pad seat is `BindSampler.LeftTriggerHeld`. The Input System map does not bind either trigger. RT is unused, and it was not taken either, but Landon said to stop if either trigger was taken rather than move the pair. LB stays sprint. RB stays air dash.

Dive stays off the stick. The shape he named, stick forward past 0.8 for 0.12 s while airborne, is looking up in the air. That trace is in the camera set above and it raises nothing. Recognizing it would make the false-positive rate on ordinary look nonzero. A modifier would be the way to add it without that. Not bound, waiting on his OK: hold RT, and only then let the stick start a dive. RT is free in the map and in `PlayerInputReader`. Conflicts if he says yes: RB is air dash, so the modifier is not RB; LB is sprint; LT is the couch rope; right-stick click is the practice input display; left-stick click is the ghost. RT was also the old dive proposal and the stutter-right tap, so those two cannot share it. Holding RT would telegraph the dive to the other players. That is the fallback he called okay but not ideal, and it is only needed for the dive.

## Proof

`evasion-moves stutter=0.35 spin=0.90 juke=1.40 dive=3.00 capOK=1 rootMotion=0 flagDefault=off`

`stutter` is the brake fraction, `spin` is the held speed fraction, `juke` is the lateral meters, `dive` is the flight meters.

No-clip, every 30 fps frame of the six clips. A pair that shares a joint is `rigJoint`. `pose` is non-adjacent pairs plus the floor, and it is 0 when every one of those is at or under 0.5 cm:

`no-clip clips=6 frames=115 worldMax=0.12 rigJoint=7.96 pose=0.0 fails=0`

`rigJoint` is the hip/thigh cuff, owned by the rig lane. `pose` is 0 because every non-adjacent pair and every floor contact is at or under 0.5 cm. The deepest of those is 0.42 cm, spine into a thigh on the dive. The deepest floor contact is 0.12 cm, a stutter foot-box corner. The juke plant corner is 0.11 cm. The ankle was re-keyed. The body was not lifted to hide either corner.

Support soles, same band as the storror sole gap (within 0.5 cm). Planted frames and the max gap, in centimetres:

`ground-contact stutter planted=9/12 maxGap=0.44 frames=0.133:L:0.44,0.167:L:0.44,0.200:R:0.44,0.233:R:0.44,0.267:L:0.44,0.300:L:0.40,0.333:R:0.29,0.367:R:0.31,0.433:L:0.39`

`ground-contact spinL planted=11/11 maxGap=0.43 frames=0.100:R:0.43,0.133:R:0.30,0.167:R:0.14,0.200:R:0.25,0.233:R:0.33,0.267:R:0.33,0.300:R:0.30,0.333:R:0.28,0.367:R:0.19,0.400:R:0.18,0.433:R:0.04`

`ground-contact spinR planted=11/11 maxGap=0.43 frames=0.100:L:0.43,0.133:L:0.30,0.167:L:0.14,0.200:L:0.25,0.233:L:0.33,0.267:L:0.33,0.300:L:0.30,0.333:L:0.28,0.367:L:0.19,0.400:L:0.18,0.433:L:0.04`

`ground-contact jukeL planted=7/7 maxGap=0.47 frames=0.100:L:0.40,0.133:L:0.33,0.167:L:0.42,0.200:L:0.44,0.233:L:0.47,0.267:L:0.44,0.300:L:0.44`

`ground-contact jukeR planted=7/7 maxGap=0.47 frames=0.100:R:0.40,0.133:R:0.33,0.167:R:0.42,0.200:R:0.44,0.233:R:0.47,0.267:R:0.44,0.300:R:0.44`

Stills, side and three-quarter, seven frames each, are in `Docs/EvasionStills/pass4/`. The floor is mid-grey with a grid. Stutter and juke plant frames carry a 4× inset of the feet. Pass 3 is still in `Docs/EvasionStills/pass3/`. Pass 2 is still in `Docs/EvasionStills/pass2/`. Pass 1 is still in `Docs/EvasionStills/pass1/`.
