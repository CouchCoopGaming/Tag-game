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

Entry is 0.10 s of pose blend before the move owns speed. Exit is 0.12 s of pose blend back toward the run after the move lets go. The motor is on the normal sprint cap again once the move ends. The clip cannot use the full 48° sprint thigh, because that deepens the hip cuff past the rest overlap. Airborne `TryRaise` is rejected. A second dive inside the cooldown is rejected.

The tagger's existing punch volume is unchanged: reach 1.55, width 0.85, no new i-frame. While the flag is on, the capsule actually steps, so a box that was on the old spot can miss, and a box that still overlaps the body still tags.

## Clips

In-place clips on the shipped Tan Hier mannequin. Drop is 0. Root motion is off. Each clip is an entry, the move, and an exit back toward a short running stride.

The Hier pieces already overlap at the joints in the rest pose (hip inside the thigh by 7.72 cm, past the 3 cm joint exemption). That rest depth is another lane's rig. A pose fails if it goes deeper, or if two pieces that do not overlap at rest start to. On this mannequin that happens very early:

- Spine pitch of 1° deepens the hip/spine cuff.
- A knee bend past 2° deepens the knee cuff.
- A thigh past about +4° or −6° deepens the hip cuff or meets the spine.
- Arm pitch between a hanging arm and about −40° deepens the shoulder cuff. −60° is clear again, but a blend would have to pass through the angles that are not.
- An elbow past 2° deepens the elbow cuff on part of a full turn.

The clips stay inside those limits. They are not a full athletic squat, a shoulder-yoked arm swing, or a ground roll. No root lift, no vertex move, and no depth clamp was used to clear the check.

| Clip | Read |
|---|---|
| `stutter` | Rapid chops of a few degrees, knees barely bent, shoulders square (no hip, spine, or head yaw). The low chest and the burst lean are a hip pitch of about 5–8°, which is as far as the soles and the cuffs allow. |
| `spinL`, `spinR` | Full 360 on the hips. The head yaws ahead of the shoulders by up to 26°, then comes back. Lead foot stays forward with the sole tipped onto the ball. Elbows tuck 2°. A deeper tuck clips on the far side of the turn. |
| `jukeL`, `jukeR` | Thighs yaw ±8° for the wide plant, plant foot tipped onto the ball, hips pitch 6°. The head fakes about 22° opposite the cut, then turns back toward it. Spine yaw stays 0 because it meets the thigh. |
| `dive` | Hips pitch to 78°, so the chest and the trailing legs go nearly horizontal while the pelvis stays at standing height (the motor owns the 3 m translation). Arms hold −60° for the whole clip, including the exit, because that is the forward reach that clears the shoulder. Recovery is a pop back up into the short stride, not a floor roll. |

## Proposed bindings

Not wired. Every mouse button is already a verb, and the pad face, shoulders, stick clicks, Start, Select, and left trigger are already verbs. Right stick is Look.

| Move | Pad proposal | Keyboard proposal | Conflict |
|---|---|---|---|
| Stutter | Right-stick flick down, past a flick gate. The camera keeps slow look and drops that one flick. | `Z` | Right stick is Look. Swallowing the flick is the only way the camera does not kick. `Z` is free on this branch. |
| Juke | Right-stick flick left or right. The sign is the flick. | `X`, sign from A or D on that press | Same look conflict. A and D stay Move. `X` is free. |
| Spin | Right-stick flick left or right while Sprint (LB) is held. | `B`, sign from A or D | LB is already Sprint, and a sprint is the normal way to run, so a look flick during a sprint would spin. Same camera conflict. `B` is free. |
| Dive | Right trigger. This branch never reads it. Left trigger is the couch rope. | Backquote `` ` `` | Landon said every pad button is already used, so RT stays a proposal even though no script samples it. Do not take East (slide), South (jump), or a mouse button. Backquote is free. LMB is punch, RMB is jet and the couch rope, MMB is lunge, Mouse3 is a punch alt, Mouse4 is a dash alt. |

Other keys that are already taken, so they are not in the proposal: Space, Ctrl, C, Q, V, Left Alt, E, F, Shift, Esc, M, N, Comma, T, G, I, H, 1, 2, 3, F3, F6, WASD.

A modifier that is not a new button is the other pad option if the right-stick flick is rejected: double-tap East. East is Slide, so a double-tap would fight the slide. That one is listed only as a rejected alternative.

## Proof

`evasion-moves stutter=0.35 spin=0.90 juke=1.40 dive=3.00 capOK=1 rootMotion=0 flagDefault=off`

`stutter` is the brake fraction, `spin` is the held speed fraction, `juke` is the lateral meters, `dive` is the flight meters.

No-clip, every 30 fps frame of the six new clips, absolute depth, joined pieces exempt within 3 cm of the joint:

`no-clip clips=6 frames=115 worldMax=0.34 rawSelfMax=7.74 rigJoint=7.72 pose=0.0 fails=0`

`rigJoint` is the rest hip/thigh overlap, owned by another lane. `pose` is any pair deeper than its rest depth, or any new pair. World is the floor. The limit is 0.5 cm. The deepest sole on these clips is the spin plant, 0.34 cm.

Stills, side and three-quarter, seven frames each, are in `Docs/EvasionStills/pass1/`. The red stills in `Docs/EvasionStills/pass1/noclip/` are the deepest frames. They are the rest cuff, not a new pose intersection.
