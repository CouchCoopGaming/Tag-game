# Player queue

Re-graded 9 Oct 2026. Rig worker #128 leads. Costumes #131 help. Neither tip moved since pass 2. Do not bind the clearance rig into #118. Landon accepts stills first.

| Branch | Tip | Baseline | This run |
| --- | --- | --- | --- |
| #128 | `35085dbf` | `pass=0/7` paper 5 / geom 2 | `models-validate assets=7 pass=0 fail=7` / `models-split paperwork=5 geometry=2` |
| #131 | `e5b34c0e` | `pass=0/18` paper 5 / geom 13 | `models-validate assets=18 pass=0 fail=18` / `models-split paperwork=5 geometry=13` |

No pass to spot-check. The geometry fails are the same ones.

## Rig (#128)

1. Hips on `Dummy_Mannequin_Tan_Hier_Clearance.fbx`. It does not replace the shipped Hier until the stills are accepted. The color Hiers are the tan body with a tint.
2. Proof on this tip is unchanged: `pose=152`, `rigJoint=0`, `worldMax=5.51` cm, `selfMax=4.00` cm, `hip=10`, `ankle=8`, `knee=3`. `pose` has to reach 0. The limit is 0.5 cm on every frame at 30 fps.
3. The candidate hip is clear at rest, fails from about 9° through 80°, and is clear again at 110° and 130°. The shipped hip is already inside the thigh at rest. The ball is the right joint. The flex range is the work.
4. Hip-sit is still not measured on a loaded plant, landing, or crouch. Required: pelvis at least 8 cm behind the support foot on plants and landings, at least 12 cm in a crouch, hip flexion at least 1.5× spine flexion. The 81 cm fold at hip 110° / knee 70° is not that measurement.
5. Ankles after the hips (`ankle=8`). Then the knee, which fails at 155° by 0.97 cm. Neck (2) goes in the same pose pass.
6. Stills for the candidate are still 640×800 hinge frames. The quartet has to be 1280×720 or larger in `Docs/LocoStills/passN/`: quarter, side, close-up of the hip ball, and a 1.8 m scale frame.

## Costumes (#131)

Nothing new landed. License rows, LOD meshes, and the pass 3 quartet already pass. All 12 loadouts still fail `shell-buried=1%`, `rig-not-clearance`, and `rig-proof-missing`.

1. Pull the shells out of the grey body. Pass 3 is torn player color on the mannequin, and the knee close-up is about 1% garment. `worldMax=0.38` did not catch it. Clothes sit 0.3–1.0 cm outside the hull, and a clothed figure is at least 15% player color in the still.
2. Keep the open joint gaps. A gap is fine. A shell inside the chest is not.
3. Bram stays Reed's meshes with a blue `PlayerColor` until the rig is stable.
4. Refit Reed, Pip, and Sol to the clearance candidate only after the shells read as clothes and the hip proof is `pose=0`. This branch has no clearance FBX and no pass 5 proof.

No redo against the environment lane. The Hier files that ride along on #122, #125, and #129 are the shipped body, not a second sculpt.
