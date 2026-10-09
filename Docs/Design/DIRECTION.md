# Tag — direction

Single game-direction doc. Pass 1, 2026-10-08. Feel numbers below are quoted from this tip (`cursor/tag-map-lane-pass19-8c95`, Unity `6000.3.24f1` in `ProjectSettings/ProjectVersion.txt`). If a lane doc disagrees with this file, this file wins until Landon changes it.

Design does not build features, meshes, clips, or screens. Ororo relays corrections. Lanes do not take direction from anywhere else.

## Vision

Tag is a four-player couch game of tag played as parkour. Someone is It, a punch passes It, and the player who was It the least wins. The parks are big enough to chase and small enough to read on a split screen. Getting moving is one button: jump, slide, and a wall you can hold into. The ceiling is the same kind of ceiling as Super Smash Bros. Melee or Rocket League: air strafing and bunny hops are real, and a practiced player pulls away without a new move. Leaving a ledge is forgiving in the Celeste way. Coyote time and the jump buffer are long enough that a late press still jumps, and the jump you get is the same height you would have had on the ground.

## Pillars

1. **Couch read.** Four people on one television. A seat is a color plus a shape, large enough to pick out in a quadrant. The body is the Hier mannequin. A verb has to read when the runner is small.
2. **One motor.** Every pawn, human or AI, uses the same `CharacterController` step. One `Move` per `Update`. No root motion. No second solver.
3. **Forgiving takeoff, strict height.** Coyote and the buffer save a late jump. Jump height does not grow because you were fast.
4. **Speed is the skill.** Air strafing can raise horizontal speed. A bunny hop keeps speed the ground would have bled off. Neither one changes how high the jump goes.
5. **Plants look like weight.** On a plant the pelvis sits behind the support foot, the hip folds more than the spine, and the support shin points forward. Clips that intersect are fixed in the pose, not hidden.
6. **The park is the toy.** Mega Park, Pocket Park, and Stack Yard are the three places. Props are real meters next to a 1.8 m runner. Vehicles carry no logos.

## Locked feel

Source: `Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs` and `Assets/Scripts/Art/HandoffFeel.cs`. `Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs` forces `animator.applyRootMotion = false` (around lines 258 and 2122). `HandoffFeel.RootMotion` is `false`.

| Lock | Value | Where |
|---|---|---|
| Coyote | 0.10 s | `coyoteTime = 0.10f` |
| Jump buffer | 0.16 s | `jumpBuffer = 0.16f` |
| Cling grace | 0.08 s | `clingReleaseGrace = 0.08f` |
| Jump speed | 24.7 | `jumpSpeed = 24.7f` |
| Rising gravity | 22 | `gravity = 22f` |
| Fall gravity multiplier | 1.62 | `fallGravityMult = 1.62f` |
| Terminal fall | 56.16 | `maxFallSpeed = 56.16f` |
| Landing roll | 65% of terminal | `HandoffFeel.RollShare = 0.65f`, so `RollSpeed = 0.65 * 56.16` |
| Walk | 6.9 | `walkSpeed = 6.9f` |
| Crouch | 3.68 | `crouchSpeed = 3.68f` |
| Sprint | 13.8 | `sprintSpeed = 13.8f` |
| Root motion | off | `applyRootMotion = false` |
| Standing height | 1.8 m | `standingHeight = 1.8f` |
| Slide boost | 0 | `slideBoost = 0f` |

Air strafing is allowed. `KinematicStep.AirSteer` says turning the wish off the current velocity can raise total horizontal speed (`Assets/TagArenaMovement/Scripts/Systems/KinematicStep.cs`). The air wish cap is 13.8 and is not a clamp on the whole vector (`MovementConfig.airSpeedCap`).

Bunny-hop speed gain is allowed. `KinematicStep.GroundSteer` bleeds speed above the gait cap unless this frame jumps (`hopSkipsFriction`).

Jump height does not depend on speed. `PlayerMotor.TryJump` sets `v.y` from `JumpHeightNow()`, which returns `jumpSpeed` or the fatigue blend. The comment there: "Vertical impulse only. Walk and sprint keep the horizontal speed they already have, so a faster run jumps farther. Height is not a speed bonus." Fatigue (`jumpFatigueMin` 9.8 over `jumpFatigueWindow` 0.75 s) is time since landing, not horizontal speed. Do not retune it into a speed bonus.

Other locks the play tip already treats as frozen, from `Docs/KnownIssues.md` and `Docs/WhatsNew.md`: air dash 0.10 s at 15 m/s, cooldown 30 s; punch reach 1.55; lunge 16 / 0.20 s / cooldown 1 s; climb 6.0; slip 3.7; wall-run 9.5; punch stagger 0.25 s then 0.50 s immunity; launch-pad cooldown 0.3 s; zip 14 m/s, regrab 0.3 s; tag-back immunity 1.0 s. Jet stays off (`enableJet = false`).

## Refused

These stay out. A lane that needs one asks in `DECISIONS.md` first.

- **Ledge hang and shimmy.** `Docs/KnownIssues.md`: "Ledge hang and shimmy are not moves. They were rejected and were not added." `Docs/SmoothMotionAudit.md` repeats "No ledge hang and no shimmy."
- **Rigidbody solver.** The live motor is one `CharacterController.Move` per `Update` (`PlayerMotor` around line 409). `Docs/KnownIssues.md`: "No rigidbody locomotion." The ragdoll is a kinematic stand-in for the stun, not a bone simulation. `Assets/TagArenaMovement/Docs/MOVEMENT_BIBLE.md` still tells you to add a Rigidbody and to run the motor in FixedUpdate. That section is stale. Do not follow it.
- **Half-gravity.** No live field scales gravity by half. Rising gravity is 22 and the fall multiplier is 1.62. `gravityWhileJetting` (0.15) is unused because jet is off. Do not add a half-gravity mode.
- **Squash as a scale.** Do not scale the mesh into a pancake. `Assets/Editor/JumpLandTellProof.cs` fails the case "thud squash is invisible or a pancake." The landing absorb is the pose clock in `HandoffFeel` (`SquashRate`), and the roll gate stays 65% of terminal. Docs that still say "land squash" as a feature (`Docs/PLAY-SLICE.md`, `Docs/PLAYTEST.md`, `Docs/WhatsNew.md`) mean that absorb, not a new scale verb.
- **Triple jump.** `TryJump` writes `v.y` once from jump speed. There is no second or third jump. `Assets/Art/Graybox/CUT-graybox-v0.1.md` also says "double jump" is out, but that brief is not current: it also outs climb and grapple, which the game has.
- **Jet.** `enableJet` is false. `Docs/KnownIssues.md`.
- **Slide enter boost.** `slideBoost` stays 0.
- **Chase-camera juice.** Field-of-view pop, shake, and slow motion stay 0 (`Docs/SmoothMotionAudit.md`, `Docs/CouchPlay.md`).

`Docs/MOVEMENT.md` is an old party pass (coyote 140 ms, buffer 140 ms, walk 5.5, sprint 9.0, jump apex 1.15 m). `README.md` repeats that table. Neither is the lock.

## Controls

Cling is not a button. Holding move into the wall is the cling. `ActionBinds.Defaults` sets Cling to `holdIntoWall` / `leftStickHold` (`Assets/Scripts/Settings/ActionBinds.cs`). `MovementConfig` says wall-run still needs the stick into the wall.

Defaults from `ActionBinds.Defaults` plus the rope path in `PlayerInputReader` and `BindSampler`:

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Jump | Space | South (A) `buttonSouth` |
| Cling | Hold move into the wall | Left stick held into the wall |
| Slide | Ctrl (C still slides on the default bind) | East (B) `buttonEast` |
| Punch / tag | LMB (E still punches on the default bind) | West (X) `buttonWest` |
| Sprint | Shift | LB `leftShoulder` |
| Air dash | Q (Left Alt still dashes on the default bind) | RB `rightShoulder` |
| Couch rope | Right mouse | LT `leftTrigger` |
| Right trigger | — | Free. Nothing reads it. |

The rope is the existing grapple, not a new action. `PlayerInputReader` (around line 388): "Same rope verb. Keyboard seat is RMB. Pad seat is LT." `BindSampler.LeftTriggerHeld` reads `pad.leftTrigger.isPressed` for device ids 1–4. `CouchRope.ProofLine` prints `fire=RMB,LT,LT,LT`. Commit `dabc9782` ("Give every couch seat the existing grapple rope.") is on this tip. `Docs/WhatsNew.md` tells the playtester: keyboard seat uses right mouse, each pad seat uses the left trigger, click to attach, second click inside 0.28 s lets go, pull speed 12. There is no `rightTrigger` read on this tip.

### Docs and UI strings that disagree

| Place | What it says | Correction |
|---|---|---|
| `Docs/Controls.md` on this tip | No rope row | Add the rope: RMB / LT. RT stays free. |
| `Docs/KnownIssues.md` | "Grapple stays off except the existing solo-pawn gate. Opponents and couch pawns do not get it." | Couch humans have the rope. AI still does not. Solo gate stays the solo tell. |
| `Docs/PlaytestChecklist.md` | "grapple is the solo pawn only" | Same correction. |
| `Docs/CouchPlay.md` | No rope. Seat color "blue / amber / purple / white". | Rope is RMB / LT. Seats are the identity below. |
| `README.md` controls table | No cling, no rope, sprint "Auto, or L3 / LB", old speeds | Use the table in this file. |
| `Docs/MOVEMENT.md` | Coyote 140 ms, buffer 140 ms, walk 5.5, sprint 9 | Use the locked feel table. |
| PR #121 live controls (`0d3b9d74`) | Grapple is RMB / LT. `ActionBinds.GrapplePadDefault` is `leftTrigger`. | Done on the live row. Pass 48 of `Docs/UiPlan.md` and `Docs/WhatsNew.md` still says "Not on pad yet". That sentence is history. |
| PR #126 | Merged into #121 at `35a535d9` (2026-10-09 00:18 UTC) by cursor[bot]. | Do not unmerge. Do not merge another lane into D1. The couch rope is intentional. |

## Seat identity

| Seat | Color | Shape |
|---|---|---|
| P1 | Red | Circle |
| P2 | Blue | Triangle |
| P3 | Orange | Square |
| P4 | Lavender | Diamond |

The menu lane stores this for the results band. On #121 `0d3b9d74`, `MenuMannequin.Colors.cs` seats are Red/circle, Blue/triangle, Orange/square, Lavender/diamond, and `Accessibility.PlayerGlyph` is `● ▲ ■ ◆`. Red swatch is `0.90, 0.18, 0.20`. Lavender's band is `0.82, 0.70, 0.98` and the shape fill is `0.80, 0.72, 0.92`.

The play tip does not match yet. `Assets/Scripts/Settings/Accessibility.cs` on #118 sets `PlayerGlyph` to circle, square, triangle, diamond (`● ■ ▲ ◆`), so P2 and P3 are swapped. The default palette rows are yellow, green, white, and cyan (`PlayerR/G/B` first four of palette 0: about `0.78, 0.78, 0.00`, `0.57, 0.99, 0.45`, `1, 1, 1`, `0, 0.84, 0.75`), not the four seat colors. The menu copy of that palette is the same measured row; seat chrome is separate. Colour-blind palettes stay an option. They are not the default marks. Shapes are what keep red/orange and blue/lavender apart.

Hier look picks (Blue, Mint, Orange, Lavender, Tan, Red in `LocalProfiles.HierNames`) are costumes on top of the seat. The seat mark stays the table above. Default runner mesh is Tan, default It mesh is Orange (`HierMannequinCatalog`).

## Art direction

The character is the Hier crash-test mannequin, about 1.8 m, readable in a four-way split. At that size the read is the outline plus one solid color on the torso and the head (`Docs/Characters/CostumeBrief.md` on the costume lane: a runner is often 20–40 px in a 1080p quadrant). Joints stay visible. No capes or loose cloth. No logos on clothes.

`Docs/ChromaForge/ART_BIBLE.md` is an older seven-mold concept (ninja, pirate, knight, and the rest). It is not the current character. Do not build from it.

Props and vehicles are real meters next to that 1.8 m body. `Docs/AssetLibrary.md` on the asset lane: "Players are about 1.8 m." Vault rails in the park kit sit at 0.90–1.05 m. The midsize sedan reference is length 4.90, width 1.84, height 1.44 (`Tools/Blender/AssetLibrary/vehicles/body_a.py` on PR #125). The city bus uses the published 40 ft sheet, height 126 inches, and says "No badges or brand marks."

## Priorities

This is the order until Landon says otherwise.

1. Keep one playable couch tip. PR #118 is that tip: three parks, the locked feel, the couch rope, the Hier body.
2. Every new plant passes hip-sit and no-clip before it is treated as done. The clearance rig stays unbound until Landon says to bind it (`DECISIONS.md`).
3. Every controls string matches the table above. LT is the pad rope. RT is free.
4. Props and vehicles stay at player scale. Hoop geometry stays a real rim in front of a real backboard. No vehicle logos.
5. Costumes stay a prep lab. They do not enter the player build, and they do not grow a second skeleton.
6. World dressing is PR #134 (`cursor/tag-world-c420`, stacked on the props branch). It dresses Mega Park. It does not add a fourth park.

Movement consolidation: A1 #118 is the only branch that consolidates. C1 #120 and E #130 stay helpers. Storror S1 #123 is reference only. S2 #124 stays idle. `cursor/tag-movement` (#136) is not a second play tip. Extra hip floors written on that branch are not this lock.

## Department map

| Department | Who | Branch / PR | Job |
|---|---|---|---|
| Movement lead A1 | Map lane | `cursor/tag-map-lane-pass19-8c95` #118 | The play tip. Feel stays locked. This branch consolidates. |
| Movement helper C1 | Animation | `cursor/tag-anim-fx` #120 | Pose the verbs that already exist. Helper. Does not merge itself. |
| Movement helper E | Evasion | `cursor/tag-evasion-moves` #130 | Stutter, spin, juke, dive. Flag stays off until Landon binds them. Helper. |
| Effects C2 | FX kit | `cursor/tag-fx-kit` #127 | Land, rope, immunity, stagger, launch, wall, tag flash. Visual only. |
| Motion reference S1 | Storror reference | `cursor/tag-storror-mocap` #123 | Reference only. Not a motor, and not a ship clip. |
| Motion reference S2 | Storror clips | `cursor/tag-storror-clips` #124 | Idle. Do not hand clips to A1. |
| Effects researcher | FX research | `cursor/tag-fx-research` #135 | Notes only, stacked on C2. No feel edits. |
| Models lead | Model standard | `cursor/tag-models-lead` #133 | Scale, license, and the env and player queues. |
| Environment sub-lead B1 | Asset library | `cursor/tag-asset-library` #122 | Buildings, harbor, park kit, at player scale. |
| Vehicles B2 | Street kit | `cursor/tag-asset-street-kit` #125 | Sedans and buses. No logos. Court is 22 × 15 m. |
| Props B3 | Street objects | `cursor/tag-street-objects` #129 | Props, signs, poles. Same court and the 0.375 m face-to-rim gap. |
| Player sub-lead A2 | Rig | `cursor/tag-loco-smooth` #128 | Locomotion on the Hier rig. The clearance candidate waits for Landon. |
| Costumes F | Costume lab | `cursor/tag-character-costumes` #131 | Prep only. Not in the player build. |
| UI D1 | Couch menu | `cursor/tag-ui-menu` #121 | Screens. #126 was merged into this branch. Do not merge another lane into it. |
| World | World designer | `cursor/tag-world-c420` #134 | Mega Park dressing, stacked on props. Not a fourth park. |
| Design | This doc | `cursor/tag-design-lead` #132 | Direction and drift. No feature work. Draft only. |

## Standing rules

Every lane, every pass.

### No-clip

Every shipped clip is checked at 30 fps. The limit is 0.5 cm (`Tools/Tag/noclip_check.py`: `LIMIT_M = 0.005`). A pair that meets at a joint, or that already overlaps at rest, is `rigJoint` (joint ball 3 cm). Pose is a non-adjacent pair or a world hit. Pose has to be 0. The rig is not edited to hide a pose, and a pose is not lifted, damped, or pushed apart to clear the number. `Docs/SmoothMotionAudit.md` states the same split.

### Hip-sit

On a plant or a landing the pelvis is at least 8 cm behind the support foot. In a crouch it is at least 12 cm behind. The motion-reference report already uses those targets (`Docs/HierStills/v080/pass14/pose_error_pass14.txt` on #123: "Plants and landings want 8 cm. A crouch wants 12 cm."). Hip flexion is at least 1.5 times spine flexion. The support knee is bent at least 25°, shin forward. The pelvis drops at least 8 cm on a plant. If the leg cannot reach that sit, change the pose. Do not leave the pelvis in front of the foot, and do not drive the thigh through the spine to fake the distance.

### Assets, words, marks

- CC0 or OFL only. Audio on this tip is original synthesis under CC0 (`Docs/AudioCredits.md`). The UI font note on the menu lane is SIL OFL. No purchases, no paid packs.
- The results screen says **RESULTS**. It does not say podium. A map prop named `Merry_Podium` and a code type named `MenuPodium` are not player-facing copy. Do not add the word to a screen.
- No logos on vehicles. The sedan and bus sources already say no badges. An original geometric mark is not a brand. A wordmark on a car is a logo.
