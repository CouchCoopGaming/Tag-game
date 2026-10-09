# Player queue

Re-checked 9 Oct 2026 with the tightened still and LOD rules. Rig worker #128 leads. Costumes #131 help. Do not bind the clearance rig into #118. Landon accepts stills first.

| Branch | Tip | Baseline | This run |
| --- | --- | --- | --- |
| #128 | `44fbff3f` | `pass=0/7` paper 5 / geom 2 at `08a4d6b0` | `models-validate assets=7 pass=0 fail=7` / `models-split paperwork=5 geometry=2` |
| #131 | `ad1582a8` | `pass=0/18` paper 5 / geom 13 at `331e8e0d` | `models-validate assets=18 pass=0 fail=18` / `models-split paperwork=5 geometry=13` |

#128 `44fbff3f` rewrites `Dummy_Mannequin_Tan_Hier_Clearance.fbx` again. The shipped Hier files did not change. Colour Hier geometry (positions, indices, UVs) still differs from `Dummy_Mannequin_Tan_Hier_Hi`, so they are not `material-variant` of the tan body. The checker still reads `Docs/LocoStills`, and the summary is unchanged: `pass=0/7`. No pass to spot-check.

## Rig (#128)

1. Hips on `Dummy_Mannequin_Tan_Hier_Clearance.fbx`. It does not replace the shipped Hier until the stills are accepted. The colour Hiers are separate cages, not a tint of the tan mesh.
2. The proof the checker uses is still `Docs/LocoStills`: `pose=152`, `rigJoint=0`, `worldMax=5.51` cm, `selfMax=4.00` cm, `hip=10`, `ankle=8`, `knee=3`. `pose` has to reach 0 on that file. The limit is 0.5 cm on every frame at 30 fps.
3. Pass 7 `measure.txt` now also says the cuff edges are split, thigh max edge is L=2.09 cm R=2.00 cm, and at 110° hip / 70° knee no edge of at least 1 mm exceeds 1.2× rest (worst=1.0001). It still reports `rigJoint=0`, twist ±10° at 60°, knee L=160, knee R=155. That file is not the proof. Twenty stills are 1280×720 and every one is over 400 KB (516–595 KB). After captions still show pose 4–7. The step90 after frame has the sole at 4.7 cm. This does not clear the hip.
4. Hip-sit is still not measured the way the checker requires. Pass 7 captions list pelvisBack on hand poses (12.6–28.0 cm). The checker looks in `Docs/LocoStills` for a loaded plant, landing, or crouch, with the pelvis in cm behind the support foot and hip flexion at least 1.5× spine flexion.
5. Ankles and the knee stay on the old proof (`ankle=8`, `knee=3`). Neck (2) goes in the same pose pass.
6. The quartet still has to land in `Docs/LocoStills/passN/`, each frame 1280×720 or larger and under 400 KB: quarter, side, close-up of the hip ball, and a 1.8 m scale frame. Pass 7 is before/after pose frames in `Docs/Models/RigStills`.

## Costumes (#131)

`ad1582a8` splits the shells at the joints and reshoots pass 5. The summary line is unchanged: `pass=0/18`. The checker reads the fit header `worldMax=0.05 fails=0`, so `cloth=7.13cm` is gone. The lines under that header are not what it reads: sprint `worldMax=1.68` fails=12, slide `worldMax=4.14` fails=93, roll `worldMax=3.46` fails=120. Ten of the twelve loadouts fail `lod2-ratio`. Reed hood is 8668/4946/3151. Reed cap 8682/4892/2911 and Sol collar-cap 8650/4876/2903 are under 0.6×. Every loadout still fails `rig-not-clearance`, `rig-proof-missing`, and the four still roles. Cloth-band is `min=0.33 max=0.98`. `accessory-max=11.60` is `Lab_PackBram`.

1. The header is under 0.5 cm. Sprint, slide, and roll are not. Bring those clips under 0.5 cm. Pass 5 is one shared lineup (`joint-close`, `lineup-side`, `lineup-three-quarter`, `scale-figure`, all 1280×720 and under 400 KB). It does not count as a quartet. Each loadout needs its own.
2. Cut LOD2 to at most 0.6× LOD1 on the ten loadouts that fail `lod2-ratio`.
3. Bram stays Reed's meshes with a blue `PlayerColor` until the rig is stable.
4. This branch still has no clearance FBX. Refit onto the clearance candidate only after the hip proof the checker reads is `pose=0`. #128 `44fbff3f` does not unblock that.

No redo against the environment lane. The Hier files that ride along on #122, #125, and #129 are the shipped body, not a second sculpt.
