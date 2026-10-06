# Practice

Practice is a free arena for the verbs that already exist. Nobody is It. AI stays off unless the card asks for one passive dummy. The dummy wanders and does not punch.

The title row sits between Play and Settings. The card reads the arena list from `ArenaRegistry`, so a later map shows up without a new menu. This tree has PARK (`PARK`) and Mega Park (`MegaPark`). Choosing an arena enables that scene root and disables the other registry roots. Leaving practice turns those roots back on.

Routes are optional. Free roam has no clock. A route is a start gate, any number of checkpoints, and a finish. Data lives in `Assets/Resources/TagArena/PracticeRoutes.json`. `PracticeRouteTable` is the ScriptableObject handle: its `Pull` reads that file. Add a route for another arena by editing the JSON. The arena name must match a registry entry.

## Mega Park routes

| Id | Name | What it asks |
|---|---|---|
| mega-beginner | South fringe | Sprint the south mulch and jump once |
| mega-wall | Cling lane | Wall-run the north cling face, then wall-jump off it |
| mega-toy | Pad zip | Launch pad, air dash on the way down, then the west-rim zip |

Cling stays a hold. Nothing in these routes adds a verb.

## Time and ghosts

Each route keeps a personal best and the checkpoint splits in the same settings blob as look and audio (`pb.<id>`, `sp.<id>`, `gh.<id>`). The HUD prints each split when you reach it, with `+` or `-` against that split on the best run. The clock and the best are separate digits, so the HUD does not build a new string every frame.

The best run is recorded at 20 Hz: position and a pose byte, capped at 512 samples, into buffers allocated once. A better run replaces it. The replay is a translucent mesh driven by the pose weights the pawn already uses (`WallPose`, `WallJumpPose`, `AirDashPose`, `ZipPose`, `LaunchPose`, `JumpPose`, `CrouchPose`, `IdlePose`). The figure has no physics step and no body of its own. G hides it. Recording still happens while it is hidden.

## Binds

These are not rebind rows. They do not use F3 (Trail Tag), F6 (frame overlay), M (minimap), or Comma (mute).

| | Keyboard | Gamepad |
|---|---|---|
| Restart the run | T | North (Y) |
| Ghost | G | Left stick press |
| Input display | I | Right stick press |

Restart puts the pawn back on the start gate and leaves no extra figure behind. The input line lists Jump, Slide, Air dash, Punch, Sprint, and Cling. Cling lights while the hold is active.

## Proof

`Tools/StrafeJumpSim` prints one `practice` line after `qa-sweep-2`. It loads the JSON, runs each Mega Park route with the kinematic step, the launch-pad set, the zip hang, and the wall-run numbers, checks the best and the ghost round-trip, checks that a restart leaves no leftover samples, and checks that the figure source has no body.
