# Asset library, pass 1

Procedural props for the couch tag arenas. Real meters, +Y up, pivot at the ground contact (or the module origin called out in the notes). Players are about 1.8 m. Vault rails in the park kit sit at 0.90–1.05 m. Every mesh is rebuilt from `Tools/Blender/AssetLibrary/<asset>.py`.

No third-party textures. Albedo maps under `Assets/Art/Props/Library/Textures` are generated in `_common.py` (brick, asphalt, wood, concrete, siding, roof, soil).

## Rebuild

```
blender --background --python Tools/Blender/AssetLibrary/build_all.py
python3 Tools/Blender/AssetLibrary/write_unity.py
blender --background --python Tools/Blender/AssetLibrary/render_pass1.py
```

Blender 4.2 LTS is enough. `write_unity.py` does not need Blender. The showcase scene is `Assets/Scenes/AssetShowcase.unity`. It is not in the build settings and it does not touch the three arenas. `Tag/Asset Showcase` rebuilds that scene from the prefabs.

## Colliders

`Col_*` matches one solid piece. `Climb_*` is the cling face and is the wall, not a shell around it. `Vault_*` is the rail; `LibraryPropMeta.vaultHeightMeters` is the height of that rail above the deck you take off from. Chain-link `Col_Fabric` is a slab the thickness of the wire: the diamonds are not a passage. Storm-drain slots and crate slat gaps are the same idea, a walkable or solid skin with openings too small to move through. Road paint and the asphalt patch have no collider. Utility-pole wires have no collider.

## Assets

| Name | Category | Dimensions (m) | Tris LOD0 | Colliders | Climb / vault | Status |
| --- | --- | --- | --- | --- | --- | --- |
| Asphalt_Patch | Roads | 1.8 × 0.016 × 1.1 | 32 (32/20) |  | Decal. The road slab under it is the collider. No collider of its own, so it cannot become a lip. | shipped |
| Barrier | Utility | 3 × 0.76 × 0.6 | 144 (144/36) | Col_Base, Col_Mid, Col_Top | Too low and sloped to cling. Top is 0.82 m, under the 0.90 m vault band. | shipped |
| Bench_Wood | StreetFurniture | 1.8 × 0.85 × 0.556 | 1776 (1776/748) | Col_Seat, Col_Back, Col_Leg x4, Col_Arm x2 | Back slats are too broken up to cling. Not a wall-run panel. Seat is 0.45 m. Below the 0.90–1.05 m vault band. | shipped |
| BikeRack | StreetFurniture | 1.7 × 0.795 × 0.6 | 480 (480/408) | Col_LegA x3, Col_LegB x3, Col_Top x3 | Tubes are too thin to cling. Hoop tops are 0.85 m, just under the vault band, and round. | shipped |
| Boat | Harbor | 1.087 × 0.52 × 3.01 | 456 (456/144) | Col_Floor x4, Col_SideL, Col_SideR, Col_Transom, Col_Bow, Col_SeatA, Col_SeatB | Hull is a solid prop. Not a cling wall. Gunwale is 0.46 m. Not a vault rail. | shipped |
| Cabin | Buildings | 5 × 3.501 × 5.332 | 1556 (1556/732/300) | Climb_FrontL, Climb_FrontR, Climb_FrontHead, Climb_Back, Climb_SideL, Climb_SideR, Col_Door, Col_Porch, Col_PorchRoof, Vault_PorchRail, Col_RoofS, Col_RoofN | climb vault 0.95 m Log walls are cling. Door is closed. Roof slopes are landings. Porch rail is 0.95 m above the porch deck (deck at 0.25 m, rail top at 1.20 m). | shipped |
| Boathouse | Harbor | 6.9 × 4.218 × 4.784 | 264 (264/120/108) | Col_Floor, Climb_Back, Climb_SideL, Climb_SideR, Climb_FrontL, Climb_FrontR, Climb_Header, Col_RoofS, Col_RoofN | climb Side and back walls are cling. The slip opening on +Z is empty. Roof slopes are landings. No rail. Wall top is 2.7 m. | shipped |
| Bollard | StreetFurniture | 0.22 × 0.975 × 0.22 | 592 (592/132) | Col_Bollard, Col_Base | Too narrow to cling. Round 0.95 m cap is not a vault rail. Treat it as a blocker. | shipped |
| Brick_Corner | Buildings | 2.065 × 3.2 × 2.065 | 176 (176/176/48) | Climb_LegX, Climb_LegZ | climb Both exterior faces (-X and -Z) are cling. Interior corner is solid too. No rail. | shipped |
| Brick_Door | Buildings | 4 × 3.2 × 0.59 | 288 (288/264/72) | Climb_PierL, Climb_PierR, Climb_Header, Col_Door, Col_Step, Col_Cornice | climb Piers are cling. The door is closed and collides. Exterior is +Z. Step is 0.12 m. Not a vault. | shipped |
| Brick_Parapet | Buildings | 4.06 × 0.5 × 0.38 | 88 (88/88/24) | Climb_Parapet, Col_Coping | climb Short cling face, 0.50 m. Useful as a roof edge, not a full wall. Coping is 0.50 m above its own base. Vault only if the roof you stand on makes the coping fall in the 0.90–1.05 band. | shipped |
| Brick_Wall | Buildings | 4 × 3.2 × 0.38 | 324 (324/132/36) | Climb_Wall, Col_Plinth, Col_Cornice | climb Both broad faces are cling panels. Exterior is +Z. Collider is Climb_Wall. No rail. The cornice is at 3.2 m. | shipped |
| Brick_Window | Buildings | 4 × 3.2 × 0.39 | 356 (356/356/72) | Climb_PierL, Climb_PierR, Climb_Sill, Climb_Header, Col_Plinth, Col_Cornice, Col_Glass, Col_FrameSill | climb Piers, sill, and header are cling. The opening is glass, not a hole. Exterior is +Z. Sill is 0.95 m but only 0.30 m deep. Not a vault rail. | shipped |
| Buoy | Harbor | 0.72 × 1.375 × 0.72 | 764 (764/188) | Col_Float, Col_Base, Col_Top | Round. Not a cling. No rail. | shipped |
| BusShelter | StreetFurniture | 3.6 × 2.56 × 1.7 | 368 (368/132) | Col_Post x4, Col_Roof, Col_GlassBack, Col_GlassSide, Col_Bench, Col_BenchLeg x2 | Glass and posts. The roof is a landing. Posts are 8 cm, not a cling wall. Roof edge is 2.45 m. Too high to vault from the ground; it is a landing. | shipped |
| ChainFence | Buildings | 2.001 × 1.875 × 0.07 | 1388 (1388/608) | Col_PostL, Col_PostR, Col_TopRail, Col_BotRail, Col_Fabric | climb The mesh is climbable in the loose sense, but the collider is the posts, rails, and a fabric slab the thickness of the wire. Top rail is 1.80 m. Too high to vault from flat ground. | shipped |
| ChainGate | Buildings | 1.225 × 1.8 × 0.085 | 422 (422/252) | Col_Hinge, Col_LatchStile, Col_Top, Col_Fabric | Same wire slab as the fence panel, inset to the leaf. Top is 1.80 m. | shipped |
| Cleat | Harbor | 0.27 × 0.113 × 0.09 | 284 (284/76) | Col_Base, Col_Horns, Col_Waist | Too small to cling. Not a rail. | shipped |
| TrafficCone | Utility | 0.36 × 0.71 × 0.36 | 132 (132/68) | Col_Base, Col_Cone | Not a cling. Too light and short. Not a vault. | shipped |
| Container_20 | Harbor | 2.489 × 2.63 × 6.11 | 1032 (1032/420/108) | Climb_Body | climb Long sides are cling. Door hardware is on +Z. Collider is the wall plate; ribs stand about 2 cm proud. No rail. Roof is a landing at 2.59 m. | shipped |
| Container_40 | Harbor | 2.489 × 2.63 × 12.24 | 1452 (1452/600/108) | Climb_Body | climb Long sides are cling. Door hardware is on +Z. Collider is the wall plate; ribs stand about 2 cm proud. No rail. Roof is a landing at 2.59 m. | shipped |
| Court | Park | 11.177 × 0.127 × 14 | 344 (344/216) | Col_Slab | Flat slab, 0.12 m thick. No rail. Place Hoop just outside the -Z baseline. | shipped |
| HarborCrane | Harbor | 2.2 × 8.923 × 7.354 | 540 (540/300/108) | Col_Base, Col_Slew, Col_Chord x4, Col_Cab, Col_Boom, Col_BoomLow | The mast is a 0.45 m lattice, not a flat cling wall. No rail. The boom is overhead. | shipped |
| Crate | Harbor | 0.78 × 0.8 × 0.78 | 1300 (1300/252) | Col_Crate | A crate this size is a blocker, not a cling wall. Top is 0.80 m. Under vault height, and the lid is the whole top. | shipped |
| Dock_Corner | Harbor | 3.991 × 0.618 × 3.966 | 1444 (1444/364) | Col_Pile x5, Col_PlankZ x14, Col_PlankX x14 | Walk the deck. The inner corner is open water, not a collider. Pivot is the center of the 4 m square, not the pile centroid. No rail. | shipped |
| Dock_Straight | Harbor | 2.01 × 0.68 × 3.982 | 1928 (1928/344) | Col_Pile x4, Col_Stringer x2, Col_Plank x26 | Deck is a walk surface. Pilings are round, not cling panels. No rail on this module. Deck height is 0.62 m, under a vault. | shipped |
| Dumpster | Utility | 2.36 × 1.28 × 1.2 | 300 (300/60) | Col_Body, Col_LidL, Col_LidR | Side walls are short cling faces, 1.2 m. Lids are a landing. Lid top is 1.35 m. High for a ground vault; the side rail is not at 1.05. | shipped |
| ElectricalBox | Utility | 1.15 × 1.145 × 0.6 | 124 (124/36) | Col_Pad, Col_Cab | Cabinet face is flat but only 1.15 m and 0.40 m deep. Not a cling wall. Too shallow to vault. | shipped |
| FireEscape | Buildings | 1.3 × 4.1 × 0.865 | 824 (824/340/180) | Climb_Rail x2, Climb_Rung x10, Col_Deck, Vault_Rail, Col_Post x2 | climb vault 1.05 m Ladder rails and rungs are Climb_*. They run from 0.20 m to the deck. Vault_Rail is the platform handrail. Top of rail is 1.05 m above the deck (world y = 4.10). | shipped |
| FireHydrant | StreetFurniture | 0.428 × 0.77 × 0.39 | 1000 (1000/348) | Col_Barrel, Col_Flange, Col_Nozzle_L, Col_Nozzle_R, Col_Nozzle_Pumper | Round barrel under 0.8 m. Not a cling wall. Too short and too narrow to vault. | shipped |
| Fountain | Park | 0.56 × 1.07 × 0.56 | 404 (404/236) | Col_Pedestal, Col_Basin | Not a cling. Too narrow. | shipped |
| Garage | Buildings | 4 × 3.666 × 6.644 | 356 (356/108/84) | Climb_Back, Climb_SideL, Climb_SideR, Col_Door x4, Col_Header, Col_RoofS, Col_RoofN | climb Side walls are cling. The overhead door is closed. No rail. | shipped |
| HarborRail | Harbor | 1.98 × 1.1 × 0.08 | 168 (168/116) | Col_PostL, Col_PostR, Vault_Rail, Col_Mid, Col_Kick | vault 1.05 m Posts are 5 cm. Not a cling wall. Vault_Rail is the top bar. Rail top is 1.05 m. | shipped |
| Hoop | Park | 1.4 × 3.8 × 1.312 | 444 (444/196) | Col_Base, Col_Pole, Col_Board, Col_Arm | Pole is 12 cm. Not a cling wall. No rail. The rim is 3.05 m. | shipped |
| House | Buildings | 7.8 × 4.42 × 7.788 | 482 (482/406/132) | Climb_Back, Climb_FrontL, Climb_FrontR, Col_Door, Col_GlassL, Col_GlassR, Climb_SideL, Climb_SideR, Col_Porch, Col_PorchRoof, Vault_PorchRail, Col_RoofS, Col_RoofN | climb vault 0.95 m Siding walls are cling. Windows are glass. Porch is open on +Z. Porch rail is 0.95 m above the porch deck (deck y = 0.30, rail top y = 1.25). | shipped |
| LightPost_Single | StreetFurniture | 0.62 × 5.45 × 1.53 | 736 (736/244) | Col_Base, Col_Pole, Col_Arm, Col_Head | Round tapered pole, 11 cm at the base. Not a cling wall. No vault edge. The arm is overhead. | shipped |
| LightPost_Double | StreetFurniture | 0.62 × 5.45 × 2.68 | 876 (876/332) | Col_Base, Col_Pole, Col_Arm_N, Col_Arm_S, Col_Head_N, Col_Head_S | Round tapered pole. Not a cling wall. No vault edge. Both arms are overhead. | shipped |
| Lighthouse | Harbor | 3.1 × 10.04 × 3.1 | 1240 (1240/764/316) | Col_Base, Col_Shaft, Col_Door, Col_Gallery, Col_Lantern, Col_Roof, Col_Post x8, Vault_Rail_N, Vault_Rail_S, Vault_Rail_E, Vault_Rail_W | vault 1.05 m The shaft is round, not a cling panel. The door is a closed hatch on +X. Gallery rail is 1.05 m above the gallery deck (deck y = 8.15, rail top y = 9.20). | shipped |
| Mailbox | StreetFurniture | 0.265 × 1.239 × 0.476 | 240 (240/96) | Col_Post, Col_Box | Post is a 5 cm tube. Not a cling. No vault edge. | shipped |
| Manhole | StreetFurniture | 0.72 × 0.04 × 0.72 | 760 (760/152) | Col_Cover | Flat ground cover. Flush. Not a vault. | shipped |
| Mannequin | Showcase | 0.63 × 1.8 × 0.32 | 624 (624/312) | Col_Body, Col_LegL, Col_LegR, Col_Head, Col_ArmL, Col_ArmR | Reference only. Reference only. | shipped |
| Median_Planter | Roads | 1.05 × 0.76 × 3.68 | 388 (388/144) | Col_EndN, Col_EndS, Col_SideL, Col_SideR, Col_Soil, Col_Shrub | Too low to cling. 0.40 m wall. Not a vault. | shipped |
| NewspaperBox | StreetFurniture | 0.56 × 0.98 × 0.42 | 368 (368/60) | Col_Body, Col_Cap | Not a cling surface. Too short to vault. | shipped |
| Pallet | Utility | 1.22 × 0.125 × 1.02 | 204 (204/132) | Col_Top, Col_Stringer x3 | Not a wall. 0.14 m. Not a vault. | shipped |
| ParkLamp | Park | 0.418 × 3.4 × 0.44 | 172 (172/76) | Col_Base, Col_Post, Col_Lantern, Col_Cap | Post is 9 cm. Not a cling wall. No rail. | shipped |
| ParkSign | Park | 1.7 × 2.1 × 0.106 | 312 (312/48) | Col_Post x2, Col_Board | Posts are 8 cm. Not a cling wall. No rail. The board is overhead of a 1.8 m player. | shipped |
| ParkingMeter | StreetFurniture | 0.22 × 1.53 × 0.175 | 160 (160/76) | Col_Post, Col_Head | Not a cling. Too short to vault. | shipped |
| Pavilion | Park | 4.968 × 3.961 × 4.968 | 384 (384/224/104) | Col_Deck, Col_Soffit, Col_Post x4, Vault_Rail_S, Vault_Rail_N, Vault_Rail_W, Vault_Rail_E, Col_Roof x4 | vault 0.95 m Posts are 14 cm. Not a cling wall. The roof is a landing. Vault_Rail runs between the posts, 0.95 m above the deck (rail top y = 1.10). | shipped |
| PicnicTable | Park | 1.8 × 0.768 × 1.32 | 364 (364/156) | Col_Top, Col_BenchN, Col_BenchS, Col_Leg x2, Col_BenchLegN x2, Col_BenchLegS x2 | Not a wall. Top is 0.76 m, under the vault band. Benches are 0.45 m. | shipped |
| Piling | Harbor | 0.4 × 3.2 × 0.4 | 464 (464/112) | Col_Collar, Col_Pile | Round timber. Not a cling wall. No rail. The head is 3.2 m, too high to vault from the dock. | shipped |
| Planter | Park | 0.9 × 0.84 × 0.9 | 356 (356/108) | Col_WallN, Col_WallS, Col_WallL, Col_WallR, Col_Soil, Col_Shrub | Too low. 0.48 m. Not a vault. | shipped |
| Road_Cross | Roads | 6 × 0.128 × 6 | 92 (92/60) | Col_Slab | Flat intersection. No curb on this tile. | shipped |
| Road_Crosswalk | Roads | 6 × 0.128 × 4 | 140 (140/72) | Col_Slab | Flat road. Paint has no collider. No curb on this tile. | shipped |
| Road_Curve | Roads | 6 × 0.27 × 6 | 156 (156/60) | Col_South, Col_East, Col_Walk | Curb at the sidewalk wedge is 0.15 m. Curb is not a vault. | shipped |
| Road_Straight | Roads | 6 × 0.128 × 4 | 104 (104/60) | Col_Slab | Flat road. No curb on this tile. Sidewalks carry the curb. | shipped |
| Road_T | Roads | 6 × 0.27 × 6 | 124 (124/60) | Col_Asphalt, Col_Curb | Curb face on -Z is 0.15 m. Too short to cling. Curb is 0.15 m. Not a vault. | shipped |
| RooftopAC | Buildings | 1.3 × 0.895 × 0.9 | 404 (404/148/132) | Col_Unit, Col_RailL, Col_RailR | Not a wall. Too bulky and low to vault. It is a rooftop obstacle. | shipped |
| RopeCoil | Harbor | 0.376 × 0.14 × 0.367 | 544 (544/192) | Col_Turn x8 | Soft prop. Colliders follow the rope, so the middle of the coil is empty. Not a rail. | shipped |
| ShopFront | Buildings | 4 × 3.2 × 1.263 | 388 (388/84/60) | Climb_PierL, Climb_PierR, Climb_Header, Col_Glass, Col_Door, Col_Awning | climb Piers are cling. Glass and the door are solid. Awning is a landing. Awning front edge is at 2.55 m. A landing, not a ground vault. | shipped |
| Shrub | Park | 0.809 × 0.805 × 0.757 | 948 (948/312) | Col_Shrub | Not a cling. Not a vault. | shipped |
| Sidewalk | Roads | 2 × 0.275 × 4 | 80 (80/48) | Col_Walk | Curb face is 0.27 m tall from the pivot, 0.15 m above the road. Not a cling wall. Curb is 0.15 m above the road. Not a vault. | shipped |
| Sign_Stop | StreetFurniture | 0.739 × 2.3 × 0.064 | 368 (368/60) | Col_Pole, Col_Sign | Sign pole is a 4.5 cm tube. Not a cling wall. No rail at vault height. The sign face is overhead. | shipped |
| Sign_Street | StreetFurniture | 0.92 × 3 × 0.075 | 424 (424/44) | Col_Pole, Col_Blade | Pole is a 6 cm tube. Not a cling wall. Blade is overhead. No vault rail. | shipped |
| StormDrain | StreetFurniture | 0.7 × 0.04 × 0.4 | 528 (528/96) | Col_Grate | Flat grate. Flush. Not a vault. | shipped |
| TrafficLight | StreetFurniture | 0.4 × 5.025 × 1.81 | 440 (440/212) | Col_Base, Col_Pole, Col_Arm, Col_Head | Pole is round and 14 cm. Not a cling wall. No rail at vault height. | shipped |
| TrashCan_Lidded | StreetFurniture | 0.48 × 1.09 × 0.519 | 872 (872/240) | Col_Body, Col_Door | Not a cling surface. Too narrow to vault. | shipped |
| TrashCan_Slat | StreetFurniture | 0.47 × 0.877 × 0.47 | 1052 (1052/224) | Col_Liner, Col_Rim | Not a cling surface. 0.92 m rim is narrow and round. Not a vault rail. | shipped |
| Tree | Park | 2.643 × 4.72 × 2.255 | 2260 (2260/752) | Col_Flare, Col_Trunk, Col_Canopy | Trunk is round, 0.28 m at the flare. Not a flat cling wall. No rail. Canopy is visual. | shipped |
| UtilityPole | Utility | 1.8 × 8.6 × 6.2 | 384 (384/244) | Col_Pole, Col_Arm, Col_Anchor | Pole is a 28 cm timber at the base, tapering. Not a flat cling wall. No rail. Wires are visual only. | shipped |
| WallAC | Utility | 0.7 × 0.48 × 0.6 | 144 (144/36) | Col_Sleeve, Col_Head | Not a cling. Too small to vault. | shipped |
| WaterTank | Buildings | 1.75 × 4.16 × 1.56 | 608 (608/228/180) | Col_Leg x4, Col_Tank | Legs are 8 cm tubes, not a cling wall. The tank is round. No rail at vault height. | shipped |
| WoodFence | Buildings | 2.02 × 1.9 × 0.1 | 640 (640/120) | Climb_Boards, Col_PostL, Col_PostR | The board faces are a cling panel. Gaps are 6 mm and are not a passage. Top is 1.80 m. Too high to vault from the ground. | shipped |

## Modules

- Roads are 6 m wide and 4 m long. Top of asphalt is 0.12 m. Sidewalk top is 0.27 m (15 cm curb) and the curb faces -X, so it butts the road edge at x = ±3. Straight tiles step 4 m along Z.
- Brick bays are 4.0 m wide, 3.2 m tall, 0.30 m thick, exterior +Z. Corners turn the exterior onto -X and -Z. The parapet stacks on a wall at y = 3.2.
- Dock modules share a deck at 0.62 m. `Dock_Straight` is 4 × 2 m. `Dock_Corner` is an L inside a 4 m square; its pivot is the center of that square.
- Containers are external ISO sizes: 20 ft is 6.06 × 2.44 × 2.59 m, 40 ft is 12.19 × 2.44 × 2.59 m. Doors face +Z. Ribs stand about 2 cm proud of the collider.
- The court is 14 × 10 m. The hoop rim is 3.05 m and faces +Z. Fence the court with `ChainFence` and `ChainGate`; there is no separate cage mesh.
- No gazebo existed in the repo. `Pavilion` is the park shelter: 4.6 m square, rail 0.95 m above the deck, pyramid roof.
- `Mannequin` is a 1.80 m scale figure for the showcase. It is not a gameplay character.

## TODO

- Court perimeter as a placed kit (the fence panel and gate are individual bays).
- A dock ramp and a second straight length so a pier can change height.
- House and garage trim: gutters, downspouts, porch columns already exist on the house; the next pass adds window muntins and a driveway apron.
- Emissive window cards and a night variant of the street lights.
- Lane arrows, stop bars, and a raised crosswalk.
- Interior dressing (shelves, bollard line, cafe tables) once the shell kit is in an arena.
- Harbor water shader and a mooring layout. The buoy, boat, and crane are props, not a sim.
- Another pass on the lighthouse lantern (glass rooms, fresnel) if it becomes a landmark.

