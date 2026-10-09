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

A straight leg leaned backward is not used. The knee stays bent, and the shin points forward so the knee is over or ahead of the ankle. Thigh yaw spreads the upper leg off the spine, so a 55° thigh can sit without the spine entering the mesh.

| Clip | Read |
|---|---|
| `stutter` | Both plant frames sit. Hips are 14° over a 4° spine, the pelvis is 10.4 cm down, and the plant knee is 55°. The support sole is within 0.21 cm on all 12 move frames, including the two chops that used to leave the floor. Shoulders stay square. |
| `spinL`, `spinR` | The pivot thigh is 48° and the knee is 55°, spread ±20° off the spine. The arms sit at −36° so the chest stays clear through the turn. The trunk does not lean. The head still leads the yaw. |
| `jukeL`, `jukeR` | The outside thigh is 48°, the knee is 55°, and the outside thigh yaw is 0 so the spine stays out of that mesh. The head turns into the cut. The spine stays square. The outside sole is seated by the drop table. |
| `dive` | One planted push-off, then a forward stretch. The plant is hips 14°, knees 50°, both soles within 0.5 cm. From 0.17 s through 0.53 s the chest is at 58° over a 4° spine, the arms reach ahead, and the legs trail. The knees fold while the chest is still leaning, the forearms meet the floor, then the body rolls to a shoulder and back up. No leg is kicked overhead. Root motion stays off. The roll-up crouch is thigh 55°, knee 79°, yaw ±20°, hips 6° over a 4° spine, both feet down. That clears the 12 cm crouch bar and the 20 cm drop bar, and the spine stays out of the thigh. |

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

Pass 7 measures the six evasion clips together with the soft land, the hard land, the 65% roll, `exit-Roll`, and `exit-RollAbsorb`. `pose` is 0 when every non-adjacent pair and the floor are at or under 0.5 cm. Knee and hip joint overlap stays `rigJoint`.

`hip-sit clips=11 fails=0 pelvisBackMin=9.72 hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.4`

`no-clip clips=11 frames=156 worldMax=0.0 pose=0.0 rigJoint=7.91 fails=0`

The airborne dive stretch is not a plant. Loaded frames keep the sole within 0.5 cm. When both soles are within 4 cm, both are support.

Pass 7 side stills of the three deepest misses (hard land, the 65% roll, exit roll absorb) are in `Docs/Movement/evasion/pass7/`. Same camera before and after. A vertical line runs through the support foot and a dot marks the pelvis. Pass 6 stills stay in `Docs/Movement/evasion-pass6/`. Pass 5 is still in `Docs/EvasionStills/pass5/`. Pass 4 is still in `Docs/EvasionStills/pass4/`. Pass 3 is still in `Docs/EvasionStills/pass3/`. Pass 2 is still in `Docs/EvasionStills/pass2/`. Pass 1 is still in `Docs/EvasionStills/pass1/`.
