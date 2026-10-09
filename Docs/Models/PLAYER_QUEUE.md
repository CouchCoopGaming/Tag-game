# Player queue

Re-checked 9 Oct 2026 with the tightened still and LOD rules. Rig worker #128 leads. Costumes #131 help. Do not bind the clearance rig into #118. Landon accepts stills first.

| Branch | Tip | Baseline | This run |
| --- | --- | --- | --- |
| #128 | `44fbff3f` | `pass=0/7` paper 5 / geom 2 at `08a4d6b0` | `models-validate assets=7 pass=0 fail=7` / `models-split paperwork=5 geometry=2` |
| #131 | `331e8e0d` | `pass=0/18` paper 5 / geom 13 | `models-validate assets=18 pass=0 fail=18` / `models-split paperwork=5 geometry=13` |

#128 `44fbff3f` rewrites `Dummy_Mannequin_Tan_Hier_Clearance.fbx` again. The shipped Hier files did not change. Colour Hier geometry (positions, indices, UVs) still differs from `Dummy_Mannequin_Tan_Hier_Hi`, so they are not `material-variant` of the tan body. The checker still reads `Docs/LocoStills`, and the summary is unchanged: `pass=0/7`. No pass to spot-check.

## Rig (#128)

1. Hips on `Dummy_Mannequin_Tan_Hier_Clearance.fbx`. It does not replace the shipped Hier until the stills are accepted. The colour Hiers are separate cages, not a tint of the tan mesh.
2. The proof the checker uses is still `Docs/LocoStills`: `pose=152`, `rigJoint=0`, `worldMax=5.51` cm, `selfMax=4.00` cm, `hip=10`, `ankle=8`, `knee=3`. `pose` has to reach 0 on that file. The limit is 0.5 cm on every frame at 30 fps.
3. Pass 7 `measure.txt` now also says the cuff edges are split, thigh max edge is L=2.09 cm R=2.00 cm, and at 110° hip / 70° knee no edge of at least 1 mm exceeds 1.2× rest (worst=1.0001). It still reports `rigJoint=0`, twist ±10° at 60°, knee L=160, knee R=155. That file is not the proof. Twenty stills are 1280×720 and every one is over 400 KB (516–595 KB). After captions still show pose 4–7. The step90 after frame has the sole at 4.7 cm. This does not clear the hip.
4. Hip-sit is still not measured the way the checker requires. Pass 7 captions list pelvisBack on hand poses (12.6–28.0 cm). The checker looks in `Docs/LocoStills` for a loaded plant, landing, or crouch, with the pelvis in cm behind the support foot and hip flexion at least 1.5× spine flexion.
5. Ankles and the knee stay on the old proof (`ankle=8`, `knee=3`). Neck (2) goes in the same pose pass.
6. The quartet still has to land in `Docs/LocoStills/passN/`, each frame 1280×720 or larger and under 400 KB: quarter, side, close-up of the hip ball, and a 1.8 m scale frame. Pass 7 is before/after pose frames in `Docs/Models/RigStills`.

## Costumes (#131)

`331e8e0d` pushes the shells out and reshoots pass 4. The summary line is unchanged. Every loadout now fails `cloth=7.13cm` and `cloth-fails=4128`, plus `rig-not-clearance`, `rig-proof-missing`, and the four still roles. LOD0 dropped (Reed hood 8394/4398/2242, was 10858/5630/2592). The cloth-band line is inside the band (`min=0.32 max=0.98`). `pass4/cover.txt` puts the remaining grey at about 1.3–1.9% of the figure. The fit header `worldMax=7.13` is what the checker uses, and `accessory-max=10.09` is `Lab_Brim`.

1. Bring `worldMax` back to 0.5 cm on every frame. The pass 4 lineup is still one shared set, so it does not count as a quartet. Each loadout needs its own. Clothes sit 0.3–1.0 cm outside the hull, and a clothed figure is at least 15% player color in that loadout's own still.
2. Keep the open joint gaps. A gap is fine. A shell inside the chest is not.
3. Bram stays Reed's meshes with a blue `PlayerColor` until the rig is stable.
4. Refit Reed, Pip, and Sol to the clearance candidate only after the shells read as clothes and the hip proof the checker reads is `pose=0`. #128 `44fbff3f` does not unblock that. This branch has no clearance FBX and no pass 5 proof.

No redo against the environment lane. The Hier files that ride along on #122, #125, and #129 are the shipped body, not a second sculpt.
