# Known issues

Open items after the playtest-robustness pass. The seeded qa sweep fixed the bugs it could prove. These are the ones still open.

## Not in this build

- Ledge hang and shimmy are not moves. They were rejected and were not added.
- Jet stays off. `MovementConfig.enableJet` is false.
- Grapple stays off except the existing solo-pawn gate. Opponents and couch pawns do not get it.
- The ragdoll is a kinematic proxy for the stun window. It is not a bone simulation.

## Still allocating, on purpose or leftover

- A HUD line is rebuilt when its rounded number changes (speed, dash, fuse, compass, tag-back). The digits come from `HudDigits`. A frame whose rounded value is unchanged reuses the cached line. The join of those cached pieces still allocates on the frame the number changes.
- The Least It "All" board rebuilds one string while that board is on screen.
- `FindObjectsByType<ItController>` still runs when the mode roster is empty and the local It is looking for prey.
- Tag overlap uses a reused 16-slot buffer. The physics query itself still runs while you are It. Launch pads still overlap their trigger every frame.
- Match start and match end still format one log string and one winner list. That is once per round, not once per frame.
- The hot-path scan counts one level of calls out of `Update`, `FixedUpdate`, `LateUpdate`, and `OnGUI`. A helper two calls down is not in that count. Before this pass the count was 101. After it the count is 9, all of them on the once-per-round match start and match end.

## Bounds and AI

- Default arena 0 (PARK) uses world meters: graybox 72 by 54, times `WorldScale` 10, plus a 4 m margin. Mega Park is arena 1 only (160 by 100). A missing settings object uses the PARK box.
- Enemy AI still reads `MegaParkP1Layout.KillPlaneY` for its own park plan. That proof line was left unchanged.
- The kill height is `MegaParkP1Layout.KillPlaneY` (-2.5) for every arena.

## Feel, unchanged

Coyote 0.10, jump buffer 0.16, cling grace 0.08, jump speed 24.7, slide boost 0, air dash 0.10 / 15 / cooldown 30, punch reach 1.55, lunge 16 / 0.20 / cooldown 1, lunge tell 0.45, climb 6.0, slip 3.7, wall-run 9.5, punch stagger 0.25 / immunity 0.50, pad cooldown 0.3, zip 14 m/s, zip regrab 0.3, tag-back immunity 1.0. One `CharacterController.Move` per `Update`. No rigidbody locomotion. No root motion.

A punch stagger cancels an air dash that is already running and refuses a new one until the 0.25 s stumble ends. The air-dash numbers did not change. A pad arc is left on its ballistic path during that stumble.

## Lifecycle

- `StaticLifecycle` resets the statics it lists on `SubsystemRegistration` (domain reload off). Statics that are not in that list keep their values across a reload that does not reset the domain.
- The speed HUD draws nothing until `Start` has built its styles. That is the first frame.
