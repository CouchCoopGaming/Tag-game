# Model queue

Prioritized again 9 Oct 2026 from the tips that were pushed. #128 had not moved. Repair what the validator still fails, then fill holes. Do not open new body lines while a rejected mesh is still in the manifest.

The environment tips have diverged. #125 `fee26b67` is not a descendant of #122 `8576679b`, and #129 `8effda99` is not a descendant of either. Name the tip that owns a mesh before editing it. Do not rebase these branches onto each other from this lead branch, and do not merge them.

#128 `35085db` and #131 `e5b34c0e` are behind #118 `ac5ea8a3`. Leave them there. Landon accepts the clearance stills before anyone binds that rig into the map lane.

Run `python3 Tools/Models/validate_assets.py --root <checkout>` on the tip you touched. The summary line has to move. A still under 1280×720, or a still outside `passN`, does not move it.

## Environment sub-lead

Buildings worker, draft PR #122, is the sub-lead. Vehicles (#125) and street props (#129) are the helpers. Order inside this section is the order of work.

### 1. License rows still missing on #125 and #129

#122 `8576679b` put a `license` object on every library row. That tip no longer fails `license`. Do not redo it.

#125 and #129 still fail `license` on every shared name, because they are not stacked on that commit. `Art/Vehicles/LICENSES.md` names the sedan family in prose. A prose mention is not a row. Add one table row per asset, first cell the exact name, SPDX `CC0-1.0`, or copy the manifest object. OFL is for a font only. Original meshes use `"source": "original"`.

#131's costume table is accepted. Leave it.

### 2. Still quartet, then mesh

For every asset that should stay: 1280×720 or larger, each file under 400 KB, quarter, side, close-up, and a 1.8 m figure, under `Docs/AssetStills/passN/` (street kit and street objects use their own `passN` folders; vehicles use `Docs/AssetStills/vehicles/<slug>/passN/`). Catalog frames at 960×540 and 800×450 do not count. Bus hero, side, and scale frames currently sit in `Docs/AssetStills/vehicles/bus_city40/` with no `passN` segment, so they fail the folder rule.

Do the quartet for a mesh in the same pass that fixes that mesh. Do not render another catalog sheet of the whole library first.

### 3. Delete the rejected meshes

Stop iterating on these. Remove them from the manifest on the branch that has them.

| Asset | Where | Why |
| --- | --- | --- |
| `StreetFurniture/Car_Sedan` | #125 and #129 | Blockout. 4.595 m long and 1.581 m tall, outside the midsize envelope. No model year. |
| `StreetFurniture/Car_Hatch` | #125 and #129 | Same blockout lane. No model year. |
| `StreetFurniture/Car_Pickup` | #125 and #129 | Same blockout lane. No model year. |
| `Vehicles/Sedan_Midsize` | #125 | Earlier loft, parallel to Sedan_Mid_A. Over width, no year, no LOD2. |
| `Vehicles/Sedan_Mid_A_21` | #125 | Model year 2021 is outside 2022–2026. |

`Car_*` are not stand-ins for the Vehicles shells. After they are gone, the only cars are the Vehicles category.

### 4. Vehicles helper (#125) — replace the sedan loft

`Sedan_Mid_A_22` through `_25` and the six `_25` paints are one mesh: 4.894 × 1.975 × 1.434 m, about 14016 / 5296 / 2116 triangles, slack 0, land gap 12.4 cm. Width is 7.5 cm over the 1.90 m midsize cap, and 13.6 cm wider than the 1.839 m Camry-class width the lane used as a proportion reference. Pass 13 side, hero, front, scale, and rear are 1280×720 and under 400 KB. There is no fascia close-up. The body still reads as a soft loft: blunt nose, flush glass, not a 2022–2026 US fascia.

`fee26b67` opened the rear quarter as dark glass. The envelope did not move: still 1.975 m wide, land gap still 12.4 cm. That pass does not count. Do not resample this loft again. Build a new shell that:

- sits inside 4.70–5.05 × 1.75–1.90 × 1.38–1.50 m
- carries one model year in the name, with 2026 as a real body rather than another paint of `_25`
- puts the hood and roof colliders within 8 cm of the mesh top
- has a nose close-up in `passN`
- keeps zero badges

Then bring the other shells into their envelopes. Measured on this tip: compact sedan 4.78 × 1.998 × 1.412 (length and width over), hatch 4.633 × 1.998 × 1.413 (length and width over), crossover 4.78 × 2.065 × 1.68 (length and width over). Each needs a model year and LOD2. `Bus_City40` (and the blue and red liveries) and `Bus_City60` are 2.851 m wide against a 2.40–2.65 m cap, and they have no LOD2. One 40 ft body, one 60 ft body. Liveries are not extra buses.

Trains and trolleys are in the standard and absent. They wait until the sedan replacement and the bus width are in.

### 5. Buildings (#122) — landings, roof bake, LODs

`Lib_Roof.png` on this tip is 512×256. That is the texel fail on Garage, House, House_Gable, House_Hip, Ranch_House, GasCanopy, Boathouse, HarborShed, Pavilion, Gazebo, and Cabin. #125 and #129 already use a square 256×256 roof. Square the #122 bake. Do not paste the 512×256 file onto the other tips.

Landable tops, collider within 8 cm of mesh max Y:

- WalkUp, 42 cm gap on #122 and 54 cm on #129. The later tip is worse. Fix both, or pick one mesh and copy it.
- Ranch_House, 40.4 cm on #122 after the rail pass. Pass 29 scale reads. The roof is not a landing until the collider is within 8 cm of the mesh.
- Cabin, 22.3 cm on #122. #129 no longer reports the gap. Port that cabin back, or re-fix #122. Do not author a third cabin.
- Gazebo, 22.2 cm on the tips that have it.
- Boathouse on #122 now fails below-pivot. Pass 29 scale reads, and the base crosses Y=0. The #129 copy does not fail this. Pick one mesh. If the pivot is meant to be the waterline, say so in the module note. Do not leave it as an accident.
- Container_20, both color variants, and Container_40. Climb_Body top is 2.36 m and the roof mesh is 2.59 m (23 cm). Same shell, one fix, then the enamel copies.

LOD:

- Harbor/Rowboat is 4892 / 2820 with no LOD2, over the harbor ceilings. The pass 15 skiff reads. Reduce it. Do not replace the silhouette with a block.
- HarborShed is over the LOD1 ceiling and has no LOD2.
- Dock_Straight (slack 2.21 cm, still inside 3 cm), FishingBoat, and GasCanopy need LOD2.
- Park/CourtFence is over the LOD2 ceiling.
- Road_Junction is on #122 and #125 and missing on #129. LOD is inside the road ceiling on #125. Put the same mesh on #129 when that branch next takes roads. Do not draw a second junction.

`Ranch_House` exists only on #122. Keep it. Pass 28 scale still has the 1.8 m figure and the hip roof reads. Finish side and close-up after the roof bake is square.

### 6. Street props helper (#129) — budgets

These are over the prop LOD0 ceiling (2500) and have no LOD2. Hydrant colors and the base hydrant are one mesh family (3192 / 832, silver 2816 / 740). Cut once.

- FireHydrant, FireHydrant_Red, FireHydrant_Silver, FireHydrant_Yellow
- NewspaperRack, ParkingMeter_Twin, Planter_Street, PowerPole_Span
- Sign_AFrame at 5502, Sign_StreetName at 2884, StreetMedian_Planted at 3324

LOD2 missing, LOD0 still inside the class ceiling (dense props are allowed 6000): GasCanopy, Dock_Straight, FishingBoat, Bench_WoodIron, BikeRack_Hoop3 (4860), Fountain_Walk, LightPost_Globe, Newsstand_Corner (4600). Pass 23 fountain bowl and pass 20 newsstand front already read. Add the missing still roles in the same pass as LOD2.

#129 does not contain the Vehicles shells. Do not re-import them here. After section 3, `Car_Sedan`, `Car_Hatch`, and `Car_Pickup` are gone from this branch too.

Pass 24 added three props that read and are not geometry fails: `Barricade_Type3` (striped rails, no badges), `Delineator_Post` (orange post, white bands), `Rail_Sidewalk` (returned ends, figure in the scale still). Finish side and close-up, and the license row from section 1. Do not rebuild them. The court restore on this tip did not create a new geometry fail. `WoodFence_Corner`, `WoodFence_End`, and `WoodFence_Gate` landed on #122 only, with no quartet. Shoot them there.

### 7. Library gaps, after the fails above move

City: alley walls, a subway stair entrance, an overpass span, a mid-block infill bay, a parking-garage face. Trains and trolleys from section 4.

Suburb: a driveway apron that meets the existing curb and sidewalk, and `Ranch_House` ported off #122 once the 40 cm roof gap is closed. `WoodFence_Gate` now exists on #122. It needs a quartet before anyone copies it.

Park: a restroom building and bleachers. Court, hoop, gazebo, playground, pond, and trees already exist.

Harbor: a ferry (the cabin cruiser and the fishing boat are not a ferry) and a rail spur that meets HarborRail. Crane, quay, containers, and the rowboat already exist.

## Player sub-lead

Rig worker, draft PR #128, is the sub-lead. Costumes (#131) are the helper. Hips, then ankles, then costumes on the new rig.

### 1. Hips on the clearance candidate

The candidate is `Dummy_Mannequin_Tan_Hier_Clearance.fbx`. It does not replace `Dummy_Mannequin_Tan_Hier_Hi.fbx` until Landon accepts stills. The color Hiers are the tan body with a tint. They are not five extra sculpts.

`Docs/LocoStills/pass5/proof.txt` on #128: `no-clip clips=77 frames=4557 worldMax=5.51 selfMax=4.00 rigJoint=0 pose=152` and `joints knee=3 hip=10 neck=2 ankle=8`. `rigJoint=0` means parent/child overlap at the joint is clean. `pose=152` must become 0. The absolute limit is 0.5 cm on every frame at 30 fps.

Pass 6 `hip-range.txt`: the candidate hip is clear at rest, fails from about 9° through 80°, and is clear again at 110° and 130°. The shipped hip is already past the limit at rest (Hips inside the thigh by 3.99 cm). The ball-and-socket on the candidate is the right joint. The flex range is the work. One posed frame puts the pelvis 81 cm behind the foot at hip 110° / knee 70° / spine rest. That is not a loaded plant, landing, or crouch, so hip-sit still fails.

Hip-sit, measured on loaded frames, not on a hinge sweep:

- plants and landings: pelvis at least 8 cm behind the support foot
- crouch: at least 12 cm behind the support foot
- hip flexion at least 1.5× spine flexion on those frames

Ship a new proof line with `pose=0`, `hip=0`, and those three measurements. Hinge stills are 640×800 today. The quartet has to be 1280×720 or larger in `Docs/LocoStills/passN/`: quarter, side, close-up of the hip ball, and a scale frame at 1.8 m.

### 2. Ankles, then the last knee degree

The ankle ball is already authored (Foot socket 3.0 cm). It still accounts for 8 of the pose hits. Clear those to 0 before any costume refit. The knee is clear through 153° and fails at 155° by 0.97 cm. Close that after the ankle count is 0. Neck (2) goes in the same pose pass. Shoulders, elbows, and wrists stay on the ball table in `Tools/Tag/build_ball_joints.py`.

`worldMax=5.51` cm and `selfMax=4.00` cm are both over 0.5 cm. `rigJoint` stays reported separately and stays 0.

### 3. Costumes: pull the shells out of the body

#131 `e5b34c0e` did the paperwork. The license table passes. LOD meshes are in the blend and the validator now reads them (`Reed_1_Hood` is 10858/5630/2592, not a missing LOD). `stillsCheck` passes: pass 3 quarter, side, knee close-up, and scale staff are 1280×720 and under 400 KB.

The geometry does not pass. `shell-buried=1%` on all 12 loadouts. Pass 3 shows torn color patches on the grey body. The knee close-up is almost entirely grey. `worldMax=0.38` stayed under 0.5 cm and missed it. A clothed figure has to be at least 15% player color against the backdrop, and the shell has to sit 0.3–1.0 cm outside the hull.

Do this before any refit onto the clearance rig:

- Move each piece off the body surface so the pass-3 frames read as solid clothes, not flecks.
- Keep the open joint gaps. A gap is fine. A shell inside the chest is not.
- Bram stays Reed's meshes with a blue PlayerColor until the rig is stable.

Then, after sections 1 and 2, refit Reed, Pip, and Sol to the clearance candidate, one bone per garment. #131 still has no clearance FBX and no pass5 proof, so `rig-not-clearance` and `rig-proof-missing` stay until that rig is the body they fit. Do not bind either branch into #118.
