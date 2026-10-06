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

- Arena 0 is Mega Park (160 by 100), arena 1 is Pocket Park (80 by 50), arena 2 is Stack Yard (110 by 70). Each kill box is that park's containment fence plus a 4 m margin. A missing id uses Mega Park.
- The kill height is `MegaParkP1Layout.KillPlaneY` (-2.5) for every arena.
- Stack Yard route marks (loop, cover, counters, roof access) are the only Stack Yard chase tuning. Shared enemy scoring is unchanged.

## Feel, unchanged

Coyote 0.10, jump buffer 0.16, cling grace 0.08, jump speed 24.7, slide boost 0, air dash 0.10 / 15 / cooldown 30, punch reach 1.55, lunge 16 / 0.20 / cooldown 1, lunge tell 0.45, climb 6.0, slip 3.7, wall-run 9.5, punch stagger 0.25 / immunity 0.50, pad cooldown 0.3, zip 14 m/s, zip regrab 0.3, tag-back immunity 1.0. One `CharacterController.Move` per `Update`. No rigidbody locomotion. No root motion.

A punch stagger cancels an air dash that is already running and refuses a new one until the 0.25 s stumble ends. The air-dash numbers did not change. A pad arc is left on its ballistic path during that stumble.

## Lifecycle

- `StaticLifecycle` resets the statics it lists on `SubsystemRegistration` (domain reload off). Statics that are not in that list keep their values across a reload that does not reset the domain.
- The speed HUD draws nothing until `Start` has built its styles. That is the first frame.

## QA sweep 2

Each line is a repro. All of these are fixed. Feel locks were not retuned.

- Fixed. Set a slider to NaN or Infinity, or store `v=9` or a non-settings blob. Clamp used to keep NaN, and Read applied whatever keys it recognized. NaN and Infinity now fall back, a future or garbage version resets, `v=0` still migrates, and a missing file leaves the caller unchanged.
- Fixed. Pause, round end, or pulling a pad left the rumble motors at their last speed because decay uses delta time and pause delta is 0. Those three paths now zero the motors.
- Fixed. World one-shots and the music bed kept playing at timeScale 0, and a music volume of 0 still started the source. Pause silences world voices. UI still plays. A 0 slider does not.
- Fixed. With the input system on, pad index 0 missing fell through to `Gamepad.current`, so an unplugged seat stole another pad. A missing slot now reads as empty.
- Fixed. Unplugging a pad during play dropped the device with no pause. The seat and pawn stay, play pauses, and the card says that seat should reconnect. Plugging it back does not resume on its own.
- Fixed. Menu Start read only `Gamepad.current`, so P2–P4 could not pause, and a pause they did open used P1's accessibility seat. Start scans every pad and the pause card uses that seat.
- Fixed. One onboarding session covered every seat, and only the first prompt HUD ticked it. Each seat now has its own steps.
- Fixed. The It hat, the tag-back glow, and the couch tint all read palette 0. They follow the pawn's seat, including a change made mid-round.
- Fixed. Reduced flashing flattened the tag-back glow and still left the It hat pulsing. The hat pulse and bob use the same flatten.
- Fixed. Captions in a split pane kept drawing past the bottom of that pane. They stop at the pane edge.
- Fixed. Countdown and results leave timeScale at 1, so a zip, a launch pad, a punch stagger, and a kill-box teleport could start or grant i-frames after the round had left play. Those start only while the round is playing. The same motor gate covers Mega Park, Pocket Park, and Stack Yard pads and zips, and each park's kill box uses that park's bounds. Pause still keeps a zip you are already on.
- Fixed. Rematch left couch stagger, tag-back, and positions behind. Equal It times crowned player 1. The fourth quadrant with three players listed seats and not times, and a tie was not marked. Rematch clears that residue, a tie is a tie, and the score pane shows the times.
- Fixed. Twenty rematch, change-setup, and title passes left a second roster when spawn ran without a release, and two third-person rigs in one frame stacked AudioListeners because the old rig was destroyed at end of frame. The loop ends at 0 leftovers. The old rig is removed before the new one is built. A split camera past the shown count is disabled.
- Fixed. Soft-play ground used the concrete footstep. It uses the grass clip. Every other ground material painted in this tree maps to a clip that is in the tree. Sand stays concrete.
- Fixed. One human on a pad still used the solo reader, so the keyboard moved that pawn too. A pad seat reads only that pad. Two pads with the same button are not a stolen bind. The keyboard table still stores a clash so the rebind warning can show it.
