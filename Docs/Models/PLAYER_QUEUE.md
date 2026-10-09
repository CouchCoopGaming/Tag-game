# Player queue

Re-checked 9 Oct 2026 with the tightened still and LOD rules. Rig worker #128 leads. Costumes #131 help. Do not bind the clearance rig into #118. Landon accepts stills first.

| Branch | Tip | Baseline | This run |
| --- | --- | --- | --- |
| #128 | `b804954f` | `pass=0/7` paper 5 / geom 2 | `models-validate assets=7 pass=0 fail=7` / `models-split paperwork=5 geometry=2` |
| #131 | `331e8e0d` | `pass=0/18` paper 5 / geom 13 | `models-validate assets=18 pass=0 fail=18` / `models-split paperwork=5 geometry=13` |

#128 moved, and the graded Hier files did not. Colour Hier geometry (positions, indices, UVs) differs from `Dummy_Mannequin_Tan_Hier_Hi`, so they are not `material-variant` of the tan body and do not inherit its stills. No pass to spot-check.

## Rig (#128)

1. Hips on `Dummy_Mannequin_Tan_Hier_Clearance.fbx`. It does not replace the shipped Hier until the stills are accepted. The color Hiers are the tan body with a tint.
2. Proof on this tip is unchanged: `pose=152`, `rigJoint=0`, `worldMax=5.51` cm, `selfMax=4.00` cm, `hip=10`, `ankle=8`, `knee=3`. `pose` has to reach 0. The limit is 0.5 cm on every frame at 30 fps.
3. The candidate hip is clear at rest, fails from about 9° through 80°, and is clear again at 110° and 130°. The shipped hip is already inside the thigh at rest. The ball is the right joint. The flex range is the work.
4. Hip-sit is still not measured on a loaded plant, landing, or crouch. Required: pelvis at least 8 cm behind the support foot on plants and landings, at least 12 cm in a crouch, hip flexion at least 1.5× spine flexion. The 81 cm fold at hip 110° / knee 70° is not that measurement.
5. Ankles after the hips (`ankle=8`). Then the knee, which fails at 155° by 0.97 cm. Neck (2) goes in the same pose pass.
6. Stills for the candidate are still 640×800 hinge frames. The quartet has to be 1280×720 or larger in `Docs/LocoStills/passN/`: quarter, side, close-up of the hip ball, and a 1.8 m scale frame.

## Costumes (#131)

`331e8e0d` pushes the shells out and reshoots pass 4. The summary line is unchanged. Every loadout now fails `cloth=7.13cm` and `cloth-fails=4128`, plus `rig-not-clearance`, `rig-proof-missing`, and the four still roles. LOD0 dropped (Reed hood 8394/4398/2242, was 10858/5630/2592). The cloth-band line is inside the band (`min=0.32 max=0.98`). `pass4/cover.txt` puts the remaining grey at about 1.3–1.9% of the figure. The fit header `worldMax=7.13` is what the checker uses, and `accessory-max=10.09` is `Lab_Brim`.

1. Bring `worldMax` back to 0.5 cm on every frame. The pass 4 lineup is still one shared set, so it does not count as a quartet. Each loadout needs its own. Clothes sit 0.3–1.0 cm outside the hull, and a clothed figure is at least 15% player color in that loadout's own still.
2. Keep the open joint gaps. A gap is fine. A shell inside the chest is not.
3. Bram stays Reed's meshes with a blue `PlayerColor` until the rig is stable.
4. Refit Reed, Pip, and Sol to the clearance candidate only after the shells read as clothes and the hip proof is `pose=0`. This branch has no clearance FBX and no pass 5 proof.

No redo against the environment lane. The Hier files that ride along on #122, #125, and #129 are the shipped body, not a second sculpt.
