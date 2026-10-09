# Player queue

Re-checked 9 Oct 2026 with the tightened still and LOD rules. Rig worker #128 leads. Costumes #131 help. Do not bind the clearance rig into #118. Landon accepts stills first.

| Branch | Tip | Baseline | This run |
| --- | --- | --- | --- |
| #128 | `44fbff3f` | `pass=0/7` paper 5 / geom 2 at `08a4d6b0` | `models-validate assets=7 pass=0 fail=7` / `models-split paperwork=5 geometry=2` |
| #131 | `6dc8b9c4` | `pass=0/18` paper 5 / geom 13 at `ad1582a8` | `models-validate assets=18 pass=0 fail=18` / `models-split paperwork=5 geometry=13` |

#128 still has no quartet, so it has no framing-fail list. #131 `6dc8b9c4` submits stills that are not a quartet. Those frames are framed now. The pass count stayed 0.

#128 `44fbff3f` rewrites `Dummy_Mannequin_Tan_Hier_Clearance.fbx` again. The shipped Hier files did not change. Colour Hier geometry (positions, indices, UVs) still differs from `Dummy_Mannequin_Tan_Hier_Hi`, so they are not `material-variant` of the tan body. The checker still reads `Docs/LocoStills`, and the summary is unchanged: `pass=0/7`. No pass to spot-check.

## Rig (#128)

1. Hips on `Dummy_Mannequin_Tan_Hier_Clearance.fbx`. It does not replace the shipped Hier until the stills are accepted. The colour Hiers are separate cages, not a tint of the tan mesh.
2. The proof the checker uses is still `Docs/LocoStills`: `pose=152`, `rigJoint=0`, `worldMax=5.51` cm, `selfMax=4.00` cm, `hip=10`, `ankle=8`, `knee=3`. `pose` has to reach 0 on that file. The limit is 0.5 cm on every frame at 30 fps.
3. Pass 7 `measure.txt` now also says the cuff edges are split, thigh max edge is L=2.09 cm R=2.00 cm, and at 110° hip / 70° knee no edge of at least 1 mm exceeds 1.2× rest (worst=1.0001). It still reports `rigJoint=0`, twist ±10° at 60°, knee L=160, knee R=155. That file is not the proof. Twenty stills are 1280×720 and every one is over 400 KB (516–595 KB). After captions still show pose 4–7. The step90 after frame has the sole at 4.7 cm. This does not clear the hip.
4. Hip-sit is still not measured the way the checker requires. Pass 7 captions list pelvisBack on hand poses (12.6–28.0 cm). The checker looks in `Docs/LocoStills` for a loaded plant, landing, or crouch, with the pelvis in cm behind the support foot and hip flexion at least 1.5× spine flexion.
5. Ankles and the knee stay on the old proof (`ankle=8`, `knee=3`). Neck (2) goes in the same pose pass.
6. The quartet still has to land in `Docs/LocoStills/passN/`, each frame 1280×720 or larger and under 400 KB: quarter, side, close-up of the hip ball, and a 1.8 m scale frame. Pass 7 is before/after pose frames in `Docs/Models/RigStills`.

## Costumes (#131)

`6dc8b9c4` cuts the shells so the overlap header can read under 0.5 cm. The summary line is still `pass=0/18`, paperwork 5, geometry 13. The fit header is `worldMax=0.46 fails=0`. Sprint is 0.35 cm, slide is 0.43 cm, and roll is 0.46 cm, each with fails=0. That is not a garment. All twelve loadouts fail `cloth-coverage` and `cloth-shards`. A dressed segment is under 90% of its rest-pose area, and pieces such as the shoulder sleeves, hips, pocket, pack, hair, and hood roll are more than one island. Pass 6 is three 1280×720 frames. `sprint.png` and `roll.png` fail `stills-frame-coverage`. `slide.png` clears the silhouette test. They are still not a quartet. All twelve loadouts also fail `lod2-ratio`. Reed hood is 7212/3325/2141. Reed cap is 7226/3593/2315. Sol collar-cap is 7194/3566/2298. Every loadout still fails `rig-not-clearance`, `rig-proof-missing`, and the four still roles. Cloth-band is `min=0.34 max=0.98`. `accessory-max=11.60` is `Lab_PackBram`. Not a redo. The restructure count stays 13.

1. Put the cloth back on the segment. Each dressed body segment needs 90% of its rest surface covered, and each piece needs to stay one island. Pass 6 does not count as a quartet. Each loadout needs its own quarter, side, close-up, and 1.8 m scale frame.
2. Cut LOD2 to at most 0.6× LOD1 on all twelve loadouts.
3. Bram stays Reed's meshes with a blue `PlayerColor` until the rig is stable.
4. This branch still has no clearance FBX. Refit onto the clearance candidate only after the hip proof the checker reads is `pose=0`. #128 `44fbff3f` does not unblock that.

No redo against the environment lane. The Hier files that ride along on #122, #125, and #129 are the shipped body, not a second sculpt.
