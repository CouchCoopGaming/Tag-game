# Quick setup

1. Copy `TagArenaMovement/Scripts` into `Assets/TagArenaMovement/Scripts`.
2. Create a Movement Config: right-click → Create → Tag Arena → Movement Config.
3. New scene. Add empty GameObject `Bootstrap`, add `MovementBootstrap`, press Play.
   Or build the player manually using section 8 of MOVEMENT_BIBLE.md.
4. Layers: put world geo on a layer referenced by the config masks. Exclude Player.
5. Animator: create a controller with the parameters listed in the bible, assign to the body child, hook it on `PlayerMotor.animator` and `MoveAnimDriver.animator`.
6. Fixed timestep 0.02. Rigidbody gravity off. Rotation frozen.

Default keys: WASD, Space, C/Ctrl crouch, Shift ski, Alt sprint, RMB jet, MMB lunge.
