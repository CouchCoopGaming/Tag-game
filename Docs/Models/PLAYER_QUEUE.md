# Player queue

Models player sub-lead. Draft PR #128 only. The clearance rig stays unbound until Landon accepts the stills. Costumes helper is #131 (`cursor/tag-character-costumes`).

Lead grade on `35085dbf`: `models-validate assets=7 pass=0 fail=7`. Proof `pose=152 rigJoint=0 worldMax=5.51cm selfMax=4.00cm hip=10 ankle=8 knee=3`.

Pass 7 is on `Dummy_Mannequin_Tan_Hier_Clearance.fbx`, measured after FBX reimport. `rigJoint=0`. Hip flexion is clear on both legs from 0° through 120°, extension 20°, and abduction 35°. The authoritative test is enclosure: a vertex is inside the other piece only when a ray in each of six axis directions hits an odd number of faces. One ray across an open strip does not count. The normal test is not authoritative for cuff vertices on the bone axis. `rigJoint` at rest still uses the normal test. Twist is clamped to ±10° at 60° of flexion (15° is not enclosed). Knee L clears 160°, knee R clears 155° and fails 160° by 0.91 cm. Ankle enclosure is dorsi 25°, plantar 45°, inv 15°, ev 15°. The normal test still reads the ankle at 20° dorsiflexion, so the combined dorsi number is 15°.

The 110° hip / 70° knee fans were 24 unsubdivided cuff edges, 34 cm from the hip ball to the knee end of the cone. Those edges are split. On the reimport the thigh's longest edge is 2.09 cm left and 2.00 cm right. Each `Mesh_*` is parented to its one bone. Vertex groups are 0 and modifiers are 0, so no vertex is blended across the hip. A skin deformer was reimported and left the left thigh 3.97 cm inside the pelvis at rest, so it was not kept. At 110° hip and 70° knee on both legs, no triangle edge at least 1 mm long exceeds 1.2× its rest length (worst 1.0001, bad 0). 53 degenerate edges under 0.01 mm move by matrix float; the longest of those posed is 0.0001 mm. The hinge still plants the support sole at 0.2 cm and keeps every mesh on or above the floor. The raised leg is the 110°/70° leg. See `Docs/Models/RigStills/pass7/after_hinge110_side.png`. The 90° step's support sole measures 4.7 cm because the shin mesh is the lowest point and the root stays up so that shin does not go through the floor.

#131 is unblocked. Flexion is clean on the reimported file, so the 12 costumes can refit and Bram can leave the Reed tint. Do not bind this rig into #118 until Landon accepts the stills. LOD1/LOD2, the joint close-up, and the scale still still wait on that review.

## Order

1. Done on the reimported clearance file: hip flex enclosure is 0 through the range above, and the hip cuff no longer fans. Stills are in `Docs/Models/RigStills/pass7/`, including `after_hinge110_side.png`. Not bound.
2. Hip-sit on loaded plants, landings, and a crouch. Pelvis at least 8 cm behind the support foot on plants and landings, 12 cm in a crouch. Hip flexion at least 1.5× spine flexion. Support knee at least 25° (45° on landings and crouches) and over or ahead of the ankle. Pelvis drop at least 8 cm (20 cm on landings and crouches). The pass-7 stills are hand poses, not the locomotion clips.
3. Ankle dorsiflexion still fails the normal test at 20° (3.15 cm, not enclosed). Right knee still overlaps at 160° (0.91 cm). Neck (2) in that same pose pass.
4. One CC0-1.0 license row per Hier file, and the still quartet at 1280×720 or larger, each under 400 KB, in `Docs/LocoStills/passN/`: quarter, side, close-up, 1.8 m scale figure.

## Helper

#131 refits the 12 costumes to this rig. The flex range is clean after export, so Bram no longer has to stay a Reed tint. LOD1/LOD2, the joint close-up, and the scale still wait on Landon's review of `Docs/Models/RigStills/pass7/`.
