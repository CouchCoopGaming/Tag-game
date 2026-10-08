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
| `stutter` | Hips sit about 15 cm down, then about 22 cm on the burst. The plant knee is about 50° and the swing knee about 68°. The chops alternate. On 10 of the 12 move frames a support sole is within 0.34 cm of the floor. Two switches are airborne, about 10 cm, and they are short. Shoulders stay square. |
| `spinL`, `spinR` | The pivot sole stays within 0.34 cm of the floor on every move frame. The free foot stays up. Hips drop with the lean so the pivot does not float as the body turns. The head still leads by up to 36°, the trunk leans 13°, and the forearms fold across the front. Upper arms stay off the chest. |
| `jukeL`, `jukeR` | The outside sole is planted on every move frame, within 0.33 cm. The outside knee is about 48° so it can reach, and the inside knee is about 72° so that foot stays up. The head and spine still fake opposite the cut, then the trunk leans into the push. |
| `dive` | A low launch brings the hips down to about 0.43 m. The hands meet the floor (0.3 cm) on the flat of the resting fist. Turning that fist so the back of the hand points straight up pulls the upper arm through the chest, so the palm plate is as flat as this reach allows: the bottom of the hand spans about 1.4 cm. The recovery then uses the landing-roll bank from the roll lane, peaking at 135° on the right shoulder. The shoulder and forearm are the contact, the head stays up, and the feet come back under the body into the run. The orbit and the seat are visual. The capsule stays on the motor. Flight is still 3.00 m and the recovery is still 0.60 s. |

## Proposed bindings

Not wired. Every mouse button is already a verb, and the pad face, shoulders, stick clicks, Start, Select, and left trigger are already verbs. Right stick is Look.

| Move | Pad proposal | Keyboard proposal | Conflict |
|---|---|---|---|
| Stutter | Right-stick flick down, past a flick gate. The camera keeps slow look and drops that one flick. | `Z` | Right stick is Look. Swallowing the flick is the only way the camera does not kick. `Z` is free on this branch. |
| Juke | Right-stick flick left or right. The sign is the flick. | `X`, sign from A or D on that press | Same look conflict. A and D stay Move. `X` is free. |
| Spin | Right-stick flick left or right while Sprint (LB) is held. | `B`, sign from A or D | LB is already Sprint, and a sprint is the normal way to run, so a look flick during a sprint would spin. Same camera conflict. `B` is free. |
| Dive | Right trigger. This branch never reads it. Left trigger is the couch rope. | `R` | Landon said every pad button is already used, so RT stays a proposal even though no script samples it. Do not take East (slide), South (jump), or a mouse button. `R` is free on this branch. LMB is punch, RMB is jet and the couch rope, MMB is lunge, Mouse3 is a punch alt, Mouse4 is a dash alt. |

Other keys that are already taken, so they are not in the proposal: Space, Ctrl, C, Q, V, Left Alt, E, F, Shift, Esc, M, N, Comma, T, G, I, H, 1, 2, 3, F3, F6, WASD.

A modifier that is not a new button is the other pad option if the right-stick flick is rejected: double-tap East. East is Slide, so a double-tap would fight the slide. That one is listed only as a rejected alternative.

## Proof

`evasion-moves stutter=0.35 spin=0.90 juke=1.40 dive=3.00 capOK=1 rootMotion=0 flagDefault=off`

`stutter` is the brake fraction, `spin` is the held speed fraction, `juke` is the lateral meters, `dive` is the flight meters.

No-clip, every 30 fps frame of the six clips. A pair that shares a joint is `rigJoint`. `pose` is non-adjacent pairs plus the floor, and it is 0 when every one of those is at or under 0.5 cm:

`no-clip clips=6 frames=115 worldMax=0.42 rigJoint=7.93 pose=0.0 fails=0`

`rigJoint` is the hip/thigh cuff, owned by the rig lane. `pose` is 0 because every non-adjacent pair and every floor contact is at or under 0.5 cm. The deepest of those is 0.43 cm, spine into a thigh during the dive roll. One stutter plant has a foot-box corner 0.42 cm into the floor while that sole patch is 0.20 cm up. The long foot box is what leaves the corner.

Support soles, same band as the storror sole gap (within 0.5 cm). Planted frames and the max gap, in centimetres:

`ground-contact stutter planted=10/12 maxGap=0.34 frames=0.133:L:0.19,0.167:L:0.19,0.200:R:0.19,0.233:R:0.19,0.267:L:0.19,0.300:L:0.16,0.333:R:0.05,0.367:R:0.09,0.433:L:0.20,0.467:L:0.34`

`ground-contact spinL planted=11/11 maxGap=0.34 frames=0.100:R:0.34,0.133:R:0.26,0.167:R:0.29,0.200:R:0.25,0.233:R:0.26,0.267:R:0.33,0.300:R:0.32,0.333:R:0.26,0.367:R:0.30,0.400:R:0.31,0.433:R:0.25`

`ground-contact spinR planted=11/11 maxGap=0.34 frames=0.100:L:0.34,0.133:L:0.26,0.167:L:0.29,0.200:L:0.25,0.233:L:0.26,0.267:L:0.33,0.300:L:0.32,0.333:L:0.26,0.367:L:0.30,0.400:L:0.31,0.433:L:0.25`

`ground-contact jukeL planted=7/7 maxGap=0.33 frames=0.100:L:0.33,0.133:L:0.30,0.167:L:0.25,0.200:L:0.32,0.233:L:0.26,0.267:L:0.29,0.300:L:0.25`

`ground-contact jukeR planted=7/7 maxGap=0.33 frames=0.100:R:0.33,0.133:R:0.30,0.167:R:0.25,0.200:R:0.32,0.233:R:0.26,0.267:R:0.29,0.300:R:0.25`

Stills, side and three-quarter, seven frames each, are in `Docs/EvasionStills/pass3/`. The floor is mid-grey with a grid. Stutter and juke plant frames carry a 4× inset of the feet. Pass 2 is still in `Docs/EvasionStills/pass2/`. Pass 1 is still in `Docs/EvasionStills/pass1/`.
