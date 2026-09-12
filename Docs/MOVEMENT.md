# Tag movement — party Apex pass

Play the slice: [`Docs/PLAY-SLICE.md`](PLAY-SLICE.md).

Fast chase locomotion for crash-test dummies in giant arenas. **Inspire Apex numbers, do not require Apex tech.**

Tunables live on `Assets/ScriptableObjects/MovementTuning.asset` (`Tag.Movement.MovementTuning`). Runtime fallback: `MovementTuning.CreateRuntimeDefaults()` (same field defaults). Debug HUD (F3) shows live **m/s**.

## Feel goals (v1)

- Snappy accel, strong sprint, readable slide
- Universal **short air dash** = dodge for everyone
- **No** superglide / wallbounce / tap-strafe skill ceiling
- Auto-sprint for party (light analog stick still walks)
- Apex-style slide = **crouch while fast**
- Dedicated **air dash** button (not crouch-in-air)
- Coyote time + jump buffer
- Kinematic `CharacterController.Move` only — punch/tag must not rubber-band
- **Third-person** boom (Landon refs are CC / slope / sprint / Input System / dash patterns — not FPS camera)

## Defaults vs Apex refs

| Verb | Apex (ref) | Tag party default | Notes |
|------|------------|-------------------|--------|
| Walk | ~5.07 m/s | **5.5** | Slightly punchier |
| Sprint | ~7.59 m/s | **9.0** | Giant-arena chase |
| Crouch walk | ~2.34 m/s | *not in v1* | Slide-from-speed instead |
| Slide peak | ~11.45 m/s | **12.0** | Boost-to-peak on enter |
| Slide-jump class | ~12 m/s | peak × 1.12 retain | Readable, not a tech window |
| Accel to full | snappy | **0.12 s** | Was 0.18 (Systems v1) |
| Brake | — | **0.10 s** | Was 0.12 |
| Jump apex | — | **1.15 m** | Coyote 120 ms, buffer 140 ms |
| Air dash distance | — | **~2.25 m** (clamp 2.5) | 15 m/s × 0.15 s |
| Air dash burst | — | **15 m/s** | Band 14–16 |
| Air dash lock | — | **0.15 s** | Band 0.12–0.18 |
| Air dash i-frames | — | **0.12 s** | Punch hurtbox only |
| Air dash charges | — | **1**, refresh on land | Travel recharge is fallback |

Systems Tag v1 (pre-pass) was walk 4.5 / sprint 7.0 / air dodge 6.5 m/s × 130 ms (~0.85 m, clamp 1.0 m) with 1.8 m grounded travel to recharge. That kit is **extended**, not deleted: wall-run / vault / punch hooks stay.

## How each verb works

### Sprint

`autoSprint = true` (party default): move magnitude ≥ `autoSprintThreshold` (0.55) is sprint. WASD is magnitude 1 → always sprints. Light gamepad stick walks at 5.5. Hold Shift / L3 / LB still sprints when auto-sprint is off.

### Jump

`JumpHorizRetain = 1` on every takeoff (walk, sprint, slide-exit). Coyote **120 ms**, buffer **140 ms**. Hard land (fall > 1.5× apex) keeps **×0.85** horiz for 0.1 s — never zeroes velocity.

### Slide

Hold **Ctrl / C / B** while planar speed ≥ `slideSpeedGate` (6.5) — Apex “crouch while fast.” Tap still works. Enter speed = `max(current, slidePeakSpeed)` so a sprint always reads as a **12 m/s** slide. Decay to 55% over 0.70 s after a 0.15 s punch. Jump-from-slide adds +12% horiz and exits into sprint. After a slide you must **release crouch** before hold can start another (no infinite slide chain). Blocked during punch unless `PunchTagTuning.allowSlideCancelDuringPunch`.

### Air dash (dodge)

**Air only.** Left Alt / Q / RB. Replaces planar velocity toward input (or facing), **keeps vertical**. 1 charge, restored the moment you land. 80 ms input buffer. No control during the 150 ms lock. Punch i-frames 120 ms (`PlayerMotor.HasAirDodgeIFrames`). Blocked on wall-run, vault, ragdoll, punch windup/active/miss-recover.

Soft clamp: if `airDodgeSpeed × airDodgeLock` would exceed `airDodgeMaxDistance` (2.5 m), speed is scaled down. Helpers live in `MovementKinematics` (EditMode tests assert the 2.0–2.5 m band).

### Slopes (PARK 20° ramps)

CharacterController `isGrounded` flickers on ramps. The motor SphereCasts under the capsule (`slopeProbeExtra`), then:

- **Walkable** (≤ `slopeLimit` 45°): `ProjectOnPlane` so path speed stays sprint/walk (downhill is not faster), plus a stick force so you do not bunny-hop. Standing still kills downhill creep.
- **Steep** (> 45°): slide down at `steepSlopeSlideSpeed` — no walking up cliffs.
- Jump sets `exitingSlope` so the probe does not glue you back to the ramp this frame.
- Stand-up after slide SphereCasts the ceiling (crouch-standup check) and stays low if blocked.

HUD shows `slope N°` when grounded.

### Camera

`thirdPerson = true` by default: boom behind the dummy (`offset` 0, 2.1, −5.8) with a sphere-cast clip so slide/dash is readable. Do **not** switch this to an FPS look for the Brackeys-style tutorials — those videos are CC/gravity/input references only.

### Punch / tag stability

Motor never assigns `transform.position` during play. Side collisions do **not** invert planar velocity. Ragdoll is a one-shot CC-off → impulse → CC-on settle (`Move` down 2 cm), not a spring. Air-dash i-frames still ignore the punch hurtbox.

## Debug HUD

Top-left (P0). Toggle **F3**. Shows live horiz m/s, vy, state, dash charges, walk/sprint/slide targets, and effective dash meters. Disable via `showDebugHud` on the SO.

## Out of scope (v1)

- Superglide, wallbounce, tap-strafe, mantle-boost
- Ground dash / extra air charges
- Crouch-walk as a separate gait
- Wall-run / vault polish (first-pass kit unchanged)

## Coding refs (patterns only)

Landon’s Unity videos — **not** an FPS camera:

| Topic | Video | What we took |
|-------|--------|----------------|
| CC Move, gravity, grounded Y | [Brackeys FPS movement](https://www.youtube.com/watch?v=_QajrabyTJc) | `CharacterController.Move`, gravity, jump launch — look stays 3rd-person |
| Slope + sprint + crouch | [xCxSjgYTw9c](https://www.youtube.com/watch?v=xCxSjgYTw9c) | Probe, ProjectOnPlane, stick, speed-on-slope, stand-up ceiling check. Crouch-while-fast = **slide**, not a crouch-walk gait |
| New Input System | [v_ncMFEoHTg](https://www.youtube.com/watch?v=v_ncMFEoHTg) | `InputAction` map in `TagInputActions` + `Assets/Input/Tag.inputactions` |
| Throw | [F20Sr5FlUlE](https://www.youtube.com/watch?v=F20Sr5FlUlE) | Hot Potato later — not this pass |

Also useful CC primers: [z3dequX5g_E](https://www.youtube.com/watch?v=z3dequX5g_E), [1uW-GbHrtQc](https://www.youtube.com/watch?v=1uW-GbHrtQc), [f473C43s8nE](https://www.youtube.com/watch?v=f473C43s8nE).

## Edit in Unity

1. Select `Assets/ScriptableObjects/MovementTuning.asset`
2. Tweak speeds; Play Mode HUD confirms m/s
3. Movement smoke: Editor menu **Tag → Run Movement Kinematics Smoke** (`Assets/Editor/Tests/MovementKinematicsTests.cs`, Assembly-CSharp-Editor)
