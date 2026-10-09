# Movement standard

One standard for every clip on `cursor/tag-movement`. The movement lead owns this file, the checks, and the final call. A helper may key on a sub-branch. The clip joins this branch only after the lead has measured it.

## What a clip is

One move has one played clip. A recovery that starts after the motor has already left that move is an exit beat, not a second copy of the move. The vault over the obstacle is `MantlePose.Cleared`. The exit catalog does not replace it.

Pose keys are joint angles plus a hips-bone drop. The capsule and the root stay where gameplay put them. Moving the hips bone down is keyframing. It is not a visual-root offset. Do not get a hip result with lift, damping, a render trick, a capsule move, or a spine reset to rest.

## Hip-sit

On every loaded frame the movement starts at the hip. A loaded frame is a plant, a landing, or a crouch: the support sole is on the surface. Cruise and airborne frames are reported, and they are not in `kneeMin`, `pelvisDropMin`, `hipFlexMin`, or `spineFlexMin`.

Measure at 30 fps, in centimetres and degrees:

- `pelvisBack` is how far the pelvis sits behind the support foot along the facing direction. At least 8 cm on a plant and a landing. At least 12 cm in a crouch.
- `hingeRatio` is hip flexion divided by lumbar plus chest flexion. At least 1.5. The torso pitches from the hip. The spine stays near neutral. Do not reset it to rest.
- Hip flexion on a plant is at least 25°. On a landing or a crouch it is at least 35°, and spine flexion (lumbar plus chest, the same denominator as the ratio) is at least 15°. The ratio stays at least 1.5. A ratio of 1.5 with both angles small is an upright half-squat and it fails. Do not reset the spine to rest to clear the floor.
- Support knee flexion is at least 25° on a plant and at least 45° on a landing or a crouch. The angle is the geometric bend between thigh and shin, not a straight leg leaned back.
- The shin angles forward. The knee is over the ankle or ahead of it. A knee behind the pelvis is a lean, and it fails.
- Pelvis drop versus standing height is at least 8 cm on a plant and at least 20 cm on a landing or a crouch. The drop is the hips bone.

The support foot is the lower sole, or both soles when they are within 4 cm of each other. The sole stays within 0.5 cm of the surface: no float, no sink.

Report one line:

`hip-sit clips=N fails=0 pelvisBackMin=.. hingeMin=.. kneeMin=.. pelvisDropMin=.. hipFlexMin=.. spineFlexMin=..`

`kneeMin`, `pelvisDropMin`, `hipFlexMin`, and `spineFlexMin` are the plant, landing, and crouch frames only. Hip flexion is the absolute hip pitch. Spine flexion is lumbar plus chest.

## Target shapes

S1 measured roll, vault, climb, mantle, wall, and slide on `cursor/tag-storror-mocap` at `8fccec65178afb30c4f3bd0dccdc4aa8240d8301`. Typical loaded plants in that set sit with the pelvis +13 to +43 cm behind the support foot and the support knee at 45–115°, shin forward. The ledger lists the key moments used as targets. The filmed tracks stay on that branch. This branch cites the numbers only.

## No-clip

Overlap at a joint that also overlaps at rest, the same parent and child, is `rigJoint`. The rig lane A2 (#128) owns it. Hip-into-thigh and shin-into-thigh stay `rigJoint` at any flexion. Do not stiffen a pose to hide that cuff.

`pose` is a non-adjacent pair, or the world, at an absolute 0.5 cm. Do not subtract rest. A clip fails only when pose is over 0.5 cm. Pose on a passing clip is 0 in the pass line when every frame is at or under 0.5 cm; the historical pass-21 line stays the historical line until a full stride-1 scan replaces it on purpose.

Report:

`no-clip clips=N frames=M worldMax=.. pose=.. rigJoint=.. fails=..`

## Locks

These stay printed and byte-identical. Do not edit them to pass a pose check.

- Coyote 0.10, jump buffer 0.16, cling grace 0.08
- Jump speed 24.7, terminal 56.16
- Roll at 65% of terminal, roll clock 0.52 s, RollShare 0.65
- Walk 6.9, crouch 3.68, sprint 13.8
- Root motion off, one CharacterController.Move per Update, no Rigidbody
- No ledge hang, no shimmy
- Hier rig rebuild is not approved. Do not bind it.

The 14 proof lines from StrafeJumpSim `--proofs` stay byte-identical, including `ropeBody=0` and `hot-path allocs before=101 after=0`.

## Seats

With the colour-blind setting off, the default paint is P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Palettes 1–4 are the colour-blind sets and stay behind that setting. Shapes separate the default seats.

## Stills

A fixed clip gets a side view, before and after, same camera, full label. Draw a vertical through the support foot and a dot on the pelvis. Files stay under 400 KB.
