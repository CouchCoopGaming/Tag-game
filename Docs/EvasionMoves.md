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
| `stutter` | Hips sit 16 cm down. Knees stay between about 52° and 55°. Five short chops on the balls of the feet, shoulders square, arms pumping a short opposite swing. The chest stays over the knees (hip pitch 18°), then pitches to about 28° on the burst. |
| `spinL`, `spinR` | Hips drop 15 cm. The lead thigh stays forward and the sole comes down to about 1.5 cm while the other foot is light. The head yaws up to 36° ahead of the shoulders, the trunk leans 13° into the turn, then the head comes back and the body drives out. Forearms fold across the front of the chest (elbows near 64°, rolled in). Upper arms stay off the chest, so the hands are not tucked onto the sternum. |
| `jukeL`, `jukeR` | Hips drop 16 cm. The outside knee is about 54°, the inside knee about 64°, and the feet yaw wide. The head and spine fake about 28° and 20° opposite the cut, then the trunk leans about 14° into the push off the outside foot. |
| `dive` | The pelvis travels. Push, then the body lies nearly horizontal with the hips about 0.71 m off the floor. The arms angle down in front of the chest, clear of it. A reach past the head puts the upper arm through the chest, so the hands stay in front of the shoulders and come down to about 3 cm above the floor. The hips then pitch on toward 95° and the body pops back up into the run. The capsule does not leave the motor path. |

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

`no-clip clips=6 frames=115 worldMax=0.0 rigJoint=7.83 pose=0.0 fails=0`

`rigJoint` is the hip/thigh cuff, owned by the rig lane. The deepest non-adjacent graze under that limit is 0.42 cm, chest into an upper arm on the spin. No sole or hand goes through the floor.

Stills, side and three-quarter, seven frames each, are in `Docs/EvasionStills/pass2/`. Pass 1 is still in `Docs/EvasionStills/pass1/`.
