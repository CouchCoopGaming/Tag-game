# Asset library, pass 5

Procedural props for the couch tag arenas. Real meters, +Y up, pivot at the ground contact (or the module origin called out in the notes). Players are about 1.8 m. Vault rails in the park kit sit at 0.90–1.05 m. Every mesh is rebuilt from `Tools/Blender/AssetLibrary/<asset>.py`.

No third-party textures. Brick, concrete, wood, bark, asphalt, and the worn metals are Blender node trees baked to albedo, roughness, normal, and occlusion, one meter per tile. Siding, roof, soil, and hydrant paint stay on the small procedural tiles. The court paint is one decal, not rescaled by the importer. Window glass and street-light lenses emit. Some shop windows use a warmer night glass.

## Rebuild

```
blender --background --python Tools/Blender/AssetLibrary/build_all.py
python3 Tools/Blender/AssetLibrary/write_unity.py
blender --background --python Tools/Blender/AssetLibrary/render_pass5.py
```

Blender 4.2 LTS is enough. `write_unity.py` does not need Blender. The showcase scene is `Assets/Scenes/AssetShowcase.unity`. It is not in the build settings and it does not touch the three arenas. `Tag/Asset Showcase` rebuilds that scene from the prefabs.

## Colliders

`Col_*` matches one solid piece. `Climb_*` is the cling face and is the wall, not a shell around it. `Vault_*` is the rail; `LibraryPropMeta.vaultHeightMeters` is the height of that rail above the deck you take off from. Chain-link `Col_Fabric` is a slab the thickness of the wire: the diamonds are not a passage. Storm-drain slots and crate slat gaps are the same idea, a walkable or solid skin with openings too small to move through. Road paint and the asphalt patch have no collider. Utility-pole wires have no collider.

## Assets

| Name | Category | Dimensions (m) | Tris LOD0 | Colliders | Climb / vault | Status |
| --- | --- | --- | --- | --- | --- | --- |
| Asphalt_Patch | Roads | 1.8 × 0.016 × 1.1 | 32 (32/20) |  | Decal. The road slab under it is the collider. No collider of its own, so it cannot become a lip. | shipped |
| Barrier | Utility | 3 × 0.76 × 0.6 | 144 (144/36) | Col_Base, Col_Mid, Col_Top | Too low and sloped to cling. Top is 0.82 m, under the 0.90 m vault band. | shipped |
| Bench_Wood | StreetFurniture | 1.8 × 0.85 × 0.591 | 2200 (2200/1044) | Col_Seat, Col_Back, Col_Leg x4, Col_Arm x2 | Back slats are too broken up to cling. Not a wall-run panel. Seat is 0.45 m. Below the 0.90–1.05 m vault band. | shipped |
| BikeRack | StreetFurniture | 1.7 × 0.795 × 0.6 | 480 (480/408) | Col_LegA x3, Col_LegB x3, Col_Top x3 | Tubes are too thin to cling. Hoop tops are 0.85 m, just under the vault band, and round. | shipped |
| Boat | Harbor | 2.194 × 3.295 × 6.075 | 344 (344/156) | Col_Keel, Col_SideL, Col_SideR, Col_Transom, Col_Bow, Col_Deck, Col_Cabin, Col_CabinRoof | The hull is a solid prop. Not a cling wall. Gunwale is about 1.0 m. Not a vault rail. | shipped |
| Cabin | Buildings | 5 × 4.2 × 5.992 | 2840 (2840/816/324) | Climb_FrontL, Climb_FrontR, Climb_FrontHead, Climb_Back, Climb_SideL, Climb_SideR, Col_Door, Col_StepLow, Col_StepHigh, Col_Porch, Col_PorchRoof, Vault_PorchRail, Col_RoofS, Col... | climb vault 0.95 m Log walls are cling. Door is closed. Roof slopes are landings. Porch rail is 0.95 m above the porch deck (deck at 0.25 m, rail top at 1.20 m). | shipped |
| Boathouse | Harbor | 6.9 × 4.218 × 4.784 | 264 (264/120/108) | Col_Floor, Climb_Back, Climb_SideL, Climb_SideR, Climb_FrontL, Climb_FrontR, Climb_Header, Col_RoofS, Col_RoofN | climb Side and back walls are cling. The slip opening on +Z is empty. Roof slopes are landings. No rail. Wall top is 2.7 m. | shipped |
| Bollard | StreetFurniture | 0.22 × 0.975 × 0.22 | 736 (736/132) | Col_Bollard, Col_Base | Too narrow to cling. Round 0.95 m cap is not a vault rail. Treat it as a blocker. | shipped |
| Brick_Corner | Buildings | 2.065 × 3.2 × 2.065 | 200 (200/200/72) | Climb_LegX, Climb_LegZ | climb Both exterior faces (-X and -Z) are cling. Interior corner is solid too. No rail. | shipped |
| Brick_Door | Buildings | 4 × 3.2 × 0.59 | 488 (488/388/108) | Climb_PierL, Climb_PierR, Climb_Header, Col_Door, Col_FrameL, Col_FrameR, Col_Step, Col_Cornice | climb Piers are cling. The door is closed and collides. Exterior is +Z. Step is 0.12 m. Not a vault. | shipped |
| Brick_Parapet | Buildings | 4.06 × 0.5 × 0.38 | 88 (88/88/24) | Climb_Parapet, Col_Coping | climb Short cling face, 0.50 m. Useful as a roof edge, not a full wall. Coping is 0.50 m above its own base. Vault only if the roof you stand on makes the coping fall in the 0.90–1.05 band. | shipped |
| Brick_Wall | Buildings | 4 × 3.2 × 0.382 | 336 (336/144/48) | Climb_Wall, Col_Plinth, Col_Cornice | climb Both broad faces are cling panels. Exterior is +Z. Collider is Climb_Wall. No rail. The cornice is at 3.2 m. | shipped |
| Brick_Window | Buildings | 4 × 3.2 × 0.415 | 472 (472/412/72) | Climb_PierL, Climb_PierR, Climb_Sill, Climb_Header, Col_Plinth, Col_Cornice, Col_Glass, Col_FrameSill | climb Piers, sill, and header are cling. The opening is glass, not a hole. Exterior is +Z. Sill is 0.95 m but only 0.30 m deep. Not a vault rail. | shipped |
| Buoy | Harbor | 0.72 × 1.375 × 0.72 | 828 (828/188) | Col_Float, Col_Base, Col_Top | Round. Not a cling. No rail. | shipped |
| BusShelter | StreetFurniture | 3.6 × 2.56 × 1.7 | 368 (368/132) | Col_Post x4, Col_Roof, Col_GlassBack, Col_GlassSide, Col_Bench, Col_BenchLeg x2 | Glass and posts. The roof is a landing. Posts are 8 cm, not a cling wall. Roof edge is 2.45 m. Too high to vault from the ground; it is a landing. | shipped |
| ChainFence | Buildings | 2.001 × 1.875 × 0.07 | 1428 (1428/628) | Col_PostL, Col_PostR, Col_TopRail, Col_BotRail, Col_Fabric | climb The mesh is climbable in the loose sense, but the collider is the posts, rails, and a fabric slab the thickness of the wire. Top rail is 1.80 m. Too high to vault from flat ground. | shipped |
| ChainGate | Buildings | 1.225 × 1.8 × 0.085 | 422 (422/252) | Col_Hinge, Col_LatchStile, Col_Top, Col_Fabric | Same wire slab as the fence panel, inset to the leaf. Top is 1.80 m. | shipped |
| Cleat | Harbor | 0.27 × 0.113 × 0.09 | 284 (284/76) | Col_Base, Col_Horns, Col_Waist | Too small to cling. Not a rail. | shipped |
| TrafficCone | Utility | 0.36 × 0.71 × 0.36 | 176 (176/96) | Col_Base, Col_Cone | Not a cling. Too light and short. Not a vault. | shipped |
| Container_20 | Harbor | 2.598 × 2.63 × 6.118 | 1868 (1868/432/108) | Climb_Body | climb Long sides are cling. Door hardware is on +Z. Collider is the wall plate; ribs stand about 2 cm proud. No rail. Roof is a landing at 2.59 m. | shipped |
| Container_40 | Harbor | 2.598 × 2.63 × 12.247 | 2624 (2624/612/108) | Climb_Body | climb Long sides are cling. Door hardware is on +Z. Collider is the wall plate; ribs stand about 2 cm proud. No rail. Roof is a landing at 2.59 m. | shipped |
| Court | Park | 12 × 0.126 × 22 | 14 (14/14) | Col_Slab | Flat slab, 0.12 m thick. No rail. Place each Hoop at the baseline with the rim facing center court. | shipped |
| CourtFence | Park | 12.98 × 3.095 × 22.99 | 3304 (3304/2400/2148) | Col_Post_SW, Col_Post_SE, Col_Post_NW, Col_Post_NE, Col_Fabric_S, Col_Fabric_N, Col_Fabric_W, Col_Fabric_E1, Col_Fabric_E2, Col_Gate | climb Posts, rails, and a wire-thick fabric slab. The diamonds are not a passage. The gate is closed. Sideline top is 1.80 m and the baselines are 3.05 m. Too high to vault from the court. | shipped |
| HarborCrane | Harbor | 2.2 × 8.923 × 7.354 | 540 (540/300/108) | Col_Base, Col_Slew, Col_Chord x4, Col_Cab, Col_Boom, Col_BoomLow | The mast is a 0.45 m lattice, not a flat cling wall. No rail. The boom is overhead. | shipped |
| Crate | Harbor | 0.8 × 0.8 × 0.804 | 1360 (1360/252) | Col_Crate | A crate this size is a blocker, not a cling wall. Top is 0.80 m. Under vault height, and the lid is the whole top. | shipped |
| Dock_Corner | Harbor | 3.991 × 0.618 × 3.966 | 1444 (1444/364) | Col_Pile x5, Col_PlankZ x14, Col_PlankX x14 | Walk the deck. The inner corner is open water, not a collider. Pivot is the center of the 4 m square, not the pile centroid. No rail. | shipped |
| DockRamp | Harbor | 1.88 × 0.631 × 3.961 | 696 (696/160) | Col_Plank x14, Col_PileL, Col_PileR | Walk the planks. Not a cling wall. Pivot is the center of the ramp, not the pile centroid. The high end is 0.62 m. Under the vault band. | shipped |
| Dock_Straight | Harbor | 2.97 × 0.763 × 5.98 | 3320 (3320/416) | Col_Pile x4, Col_Stringer x2, Col_Plank x36, Col_Cleat x4 | Deck is a walk surface. Pilings are round, not cling panels. No rail on this module. Deck height is 0.62 m, under a vault. | shipped |
| Dumpster | Utility | 2.36 × 1.31 × 1.218 | 372 (372/60) | Col_Body, Col_LidL, Col_LidR | Side walls are short cling faces, 1.2 m. Lids are a landing. Lid top is 1.35 m. High for a ground vault; the side rail is not at 1.05. | shipped |
| ElectricalBox | Utility | 1.15 × 1.145 × 0.6 | 124 (124/36) | Col_Pad, Col_Cab | Cabinet face is flat but only 1.15 m and 0.40 m deep. Not a cling wall. Too shallow to vault. | shipped |
| FireEscape | Buildings | 1.3 × 4.1 × 0.865 | 824 (824/340/180) | Climb_Rail x2, Climb_Rung x10, Col_Deck, Vault_Rail, Col_Post x2 | climb vault 1.05 m Ladder rails and rungs are Climb_*. They run from 0.20 m to the deck. Vault_Rail is the platform handrail. Top of rail is 1.05 m above the deck (world y = 4.10). | shipped |
| FireHydrant | StreetFurniture | 0.4 × 0.761 × 0.395 | 1104 (1104/356) | Col_Flange, Col_Barrel, Col_Nozzle_L, Col_Nozzle_R, Col_Nozzle_Pumper | Round barrel under 0.8 m. Not a cling wall. Too short and too narrow to vault. | shipped |
| Fountain | Park | 0.56 × 1.07 × 0.56 | 404 (404/236) | Col_Pedestal, Col_Basin | Not a cling. Too narrow. | shipped |
| Garage | Buildings | 4 × 3.666 × 9.185 | 500 (500/196/120) | Climb_Back, Climb_SideL, Climb_SideR, Col_Door x4, Col_Header, Col_Apron, Col_RoofS, Col_RoofN | climb Side walls are cling. The overhead door is closed. No rail. | shipped |
| Gutter | Roads | 0.38 × 0.13 × 3.94 | 36 (36/36) | Col_RoadLip, Col_Channel, Col_CurbLip | A shallow channel. Not a cling face. Under 0.15 m. Not a vault. | shipped |
| HarborRail | Harbor | 1.98 × 1.1 × 0.08 | 168 (168/116) | Col_PostL, Col_PostR, Vault_Rail, Col_Mid, Col_Kick | vault 1.05 m Posts are 5 cm. Not a cling wall. Vault_Rail is the top bar. Rail top is 1.05 m. | shipped |
| HarborWater | Harbor | 16 × 0.02 × 12 | 12 (12/12) |  | Visual water. No collider, so it is not a floor and not a wall. No collider. | shipped |
| Hoop | Park | 1.84 × 4.005 × 1.327 | 1536 (1536/584) | Col_Base, Col_Pole, Col_Board, Col_Arm | Pole is 12 cm under the pad. Not a cling wall. No rail. The rim is 3.05 m. | shipped |
| House | Buildings | 7.8 × 4.42 × 9.769 | 778 (778/654/192) | Climb_Back, Climb_FrontL, Climb_FrontR, Col_Door, Col_GlassL, Col_GlassR, Climb_SideL, Climb_SideR, Col_Walk, Col_Porch, Col_PorchRoof, Vault_PorchRail, Col_RoofS, Col_RoofN | climb vault 0.95 m Siding walls are cling. Windows are glass. Porch is open on +Z. Porch rail is 0.95 m above the porch deck (deck y = 0.30, rail top y = 1.25). | shipped |
| House_Gable | Buildings | 7.95 × 5.927 × 9.458 | 2000 (2000/1724/1624) | Vault_PorchRail, Climb_Front x10, Col_Glass x8, Col_Door, Climb_Back x7, Climb_Right x7, Climb_Left x7, Col_Porch, Col_PorchRoof, Col_StepHigh, Col_StepLow, Col_Chimney | climb vault 0.95 m Siding walls are cling. Windows are glass. The porch is open on +Z. Porch rail is 0.95 m above the porch deck. | shipped |
| House_Hip | Buildings | 9.026 × 5.878 × 9.758 | 2008 (2008/1732/1592) | Vault_PorchRail, Climb_Front x10, Col_Glass x8, Col_Door, Climb_Back x7, Climb_Right x7, Climb_Left x7, Col_Porch, Col_PorchRoof, Col_StepHigh, Col_StepLow, Col_Chimney | climb vault 0.95 m Siding walls are cling. Windows are glass. The porch is open on +Z. Porch rail is 0.95 m above the porch deck. | shipped |
| LaneArrow | Roads | 0.72 × 0.008 × 1.875 | 20 (20/20) |  | Decal. The road slab under it is the collider. No collider of its own, so it cannot become a lip. | shipped |
| LightPost_Single | StreetFurniture | 0.62 × 5.469 × 1.53 | 792 (792/244) | Col_Base, Col_Pole, Col_Arm, Col_Head | Round tapered pole, 11 cm at the base. Not a cling wall. No vault edge. The arm is overhead. | shipped |
| LightPost_Double | StreetFurniture | 0.62 × 5.469 × 2.68 | 1016 (1016/332) | Col_Base, Col_Pole, Col_Arm_N, Col_Arm_S, Col_Head_N, Col_Head_S | Round tapered pole. Not a cling wall. No vault edge. Both arms are overhead. | shipped |
| Lighthouse | Harbor | 3.1 × 10.04 × 3.1 | 1240 (1240/764/316) | Col_Base, Col_Shaft, Col_Door, Col_Gallery, Col_Lantern, Col_Roof, Col_Post x8, Vault_Rail_N, Vault_Rail_S, Vault_Rail_E, Vault_Rail_W | vault 1.05 m The shaft is round, not a cling panel. The door is a closed hatch on +X. Gallery rail is 1.05 m above the gallery deck (deck y = 8.15, rail top y = 9.20). | shipped |
| Mailbox | StreetFurniture | 0.265 × 1.239 × 0.49 | 284 (284/96) | Col_Post, Col_Box | Post is a 5 cm tube. Not a cling. No vault edge. | shipped |
| Manhole | StreetFurniture | 0.72 × 0.04 × 0.72 | 760 (760/152) | Col_Cover | Flat ground cover. Flush. Not a vault. | shipped |
| Mannequin | Showcase | 0.63 × 1.8 × 0.32 | 624 (624/312) | Col_Body, Col_LegL, Col_LegR, Col_Head, Col_ArmL, Col_ArmR | Reference only. Reference only. | shipped |
| Median_Planter | Roads | 1.08 × 0.71 × 3.68 | 344 (344/156) | Col_EndN, Col_EndS, Col_SideL, Col_SideR, Col_Soil, Col_Shrub | Too low to cling. 0.40 m wall. Not a vault. | shipped |
| Mooring | Harbor | 1.32 × 0.665 × 3.155 | 348 (348/172) | Col_Plank x10, Col_PileA, Col_PileB, Col_Cleat | Walk the deck. Pilings are round and sit on -X, so the pivot stays the deck center. Deck is 0.55 m. Under the vault band. | shipped |
| NewspaperBox | StreetFurniture | 0.56 × 0.98 × 0.42 | 368 (368/60) | Col_Body, Col_Cap | Not a cling surface. Too short to vault. | shipped |
| Pallet | Utility | 1.22 × 0.125 × 1.02 | 204 (204/132) | Col_Top, Col_Stringer x3 | Not a wall. 0.14 m. Not a vault. | shipped |
| ParkLamp | Park | 0.418 × 3.4 × 0.44 | 220 (220/100) | Col_Base, Col_Post, Col_Lantern, Col_Cap | Post is 9 cm. Not a cling wall. No rail. | shipped |
| ParkSign | Park | 1.7 × 2.1 × 0.106 | 312 (312/48) | Col_Post x2, Col_Board | Posts are 8 cm. Not a cling wall. No rail. The board is overhead of a 1.8 m player. | shipped |
| ParkingMeter | StreetFurniture | 0.22 × 1.53 × 0.175 | 160 (160/76) | Col_Post, Col_Head | Not a cling. Too short to vault. | shipped |
| Pavilion | Park | 4.968 × 3.961 × 4.968 | 384 (384/224/104) | Col_Deck, Col_Soffit, Col_Post x4, Vault_Rail_S, Vault_Rail_N, Vault_Rail_W, Vault_Rail_E, Col_Roof x4 | vault 0.95 m Posts are 14 cm. Not a cling wall. The roof is a landing. Vault_Rail runs between the posts, 0.95 m above the deck (rail top y = 1.10). | shipped |
| PicnicTable | Park | 1.8 × 0.781 × 1.32 | 424 (424/156) | Col_Top, Col_BenchN, Col_BenchS, Col_Leg x2, Col_BenchLegN x2, Col_BenchLegS x2 | Not a wall. Top is 0.76 m, under the vault band. Benches are 0.45 m. | shipped |
| Piling | Harbor | 0.4 × 4.65 × 0.4 | 508 (508/140) | Col_Collar, Col_Pile | Round timber. Not a cling wall. About 1.4 m of the shaft is below the pivot, in the water. No rail. The head is 3.2 m, too high to vault from the dock. | shipped |
| Planter | Park | 0.9 × 0.84 × 0.9 | 356 (356/108) | Col_WallN, Col_WallS, Col_WallL, Col_WallR, Col_Soil, Col_Shrub | Too low. 0.48 m. Not a vault. | shipped |
| Quay_Edge | Harbor | 18 × 2.03 × 8.87 | 1240 (1240/724) | Col_Deck, Col_Face, Col_Coping, Col_Fender x5, Col_Bollard x4, Col_LadderL, Col_LadderR | The face is a wall. Too low and too thick to treat as a cling panel from the water. Deck is 0.90 m. The bullnose is rounded, not a rail. | shipped |
| RaisedCrosswalk | Roads | 6 × 0.28 × 4.014 | 120 (120/96) | Col_Slab, Col_Crown, Col_RampS, Col_RampN | The crown is 8 cm above the road. Walk it. Not a cling wall. 8 cm rise. Not a vault. | shipped |
| Road_Cross | Roads | 6 × 0.128 × 6 | 92 (92/60) | Col_Slab | Flat intersection. No curb on this tile. | shipped |
| Road_Crosswalk | Roads | 6 × 0.128 × 4 | 140 (140/72) | Col_Slab | Flat road. Paint has no collider. No curb on this tile. | shipped |
| Road_Curve | Roads | 6 × 0.27 × 6 | 156 (156/60) | Col_South, Col_East, Col_Walk | Curb at the sidewalk wedge is 0.15 m. Curb is not a vault. | shipped |
| Road_Straight | Roads | 6 × 0.128 × 4 | 104 (104/60) | Col_Slab | Flat road. No curb on this tile. Sidewalks carry the curb. | shipped |
| Road_T | Roads | 6 × 0.27 × 6 | 124 (124/60) | Col_Asphalt, Col_Curb | Curb face on -Z is 0.15 m. Too short to cling. Curb is 0.15 m. Not a vault. | shipped |
| Roof_Parapet | Buildings | 4.14 × 0.68 × 0.48 | 112 (112/36/36) | Climb_Parapet, Col_Cornice, Col_Coping | climb Short cling face. A roof edge, not a full wall. Coping is 0.72 m above its own base. | shipped |
| RooftopAC | Buildings | 1.33 × 0.895 × 0.9 | 488 (488/148/132) | Col_Unit, Col_RailL, Col_RailR | Not a wall. Too bulky and low to vault. It is a rooftop obstacle. | shipped |
| RopeCoil | Harbor | 0.376 × 0.14 × 0.367 | 544 (544/192) | Col_Turn x8 | Soft prop. Colliders follow the rope, so the middle of the coil is empty. Not a rail. | shipped |
| ShopFront | Buildings | 4 × 3.2 × 1.262 | 292 (292/152/96) | Climb_PierL, Climb_PierR, Climb_Header, Col_Glass, Col_Door, Col_Awning | climb Piers are cling. Glass and the door are solid. Awning is a landing. Awning front edge is at 2.55 m. A landing, not a ground vault. | shipped |
| Shrub | Park | 0.809 × 0.805 × 0.757 | 948 (948/312) | Col_Shrub | Not a cling. Not a vault. | shipped |
| Sidewalk | Roads | 2 × 0.275 × 4 | 80 (80/48) | Col_Walk | Curb face is 0.27 m tall from the pivot, 0.15 m above the road. Not a cling wall. Curb is 0.15 m above the road. Not a vault. | shipped |
| Sign_Stop | StreetFurniture | 0.739 × 2.3 × 0.064 | 368 (368/60) | Col_Pole, Col_Sign | Sign pole is a 4.5 cm tube. Not a cling wall. No rail at vault height. The sign face is overhead. | shipped |
| Sign_Street | StreetFurniture | 0.92 × 3 × 0.075 | 424 (424/44) | Col_Pole, Col_Blade | Pole is a 6 cm tube. Not a cling wall. Blade is overhead. No vault rail. | shipped |
| StopBar | Roads | 5.6 × 0.008 × 0.4 | 12 (12/12) |  | Decal. The road slab under it is the collider. No collider of its own, so it cannot become a lip. | shipped |
| Store_Corner | Buildings | 9.44 × 7.725 × 9.25 | 4324 (4324/3100/2388) | Climb_Front x19, Col_Glass x19, Climb_Back x7, Climb_Right x19, Climb_Left x4, Col_Roof, Col_ParapetF, Col_ParapetB, Col_Escape, Col_Kick x10, Col_Door x2, Col_Transom x2, Col_S... | climb The four walls are cling. Glass is solid. The roof is a parapet, not a rail. No ground-height rail. The parapet is on the roof. | shipped |
| Store_Diner | Buildings | 10.22 × 5.925 × 9.05 | 3012 (3012/2340/1692) | Climb_Front x19, Col_Glass x13, Climb_Back x7, Climb_Right x4, Climb_Left x4, Col_Roof, Col_ParapetF, Col_ParapetB, Col_Escape, Col_Kick x6, Col_Door, Col_Transom, Col_Sign, Col... | climb The four walls are cling. Glass is solid. The roof is a parapet, not a rail. No ground-height rail. The parapet is on the roof. | shipped |
| Store_Laundromat | Buildings | 10.64 × 6.125 × 8.85 | 4532 (4532/3388/2580) | Climb_Front x19, Col_Glass x27, Climb_Back x7, Climb_Right x4, Climb_Left x19, Col_Roof, Col_ParapetF, Col_ParapetB, Col_Escape, Col_Kick x18, Col_Door x2, Col_Transom x2, Col_S... | climb The four walls are cling. Glass is solid. The roof is a parapet, not a rail. No ground-height rail. The parapet is on the roof. | shipped |
| Storefront_Glass | Buildings | 4 × 3.2 × 0.38 | 300 (300/144/144) | Climb_PierL, Climb_PierR, Climb_Header, Col_Glass, Col_Door, Col_Cornice | climb Piers are cling. Glass and the door are solid. Exterior is +Z. No rail. The head is at 3.2 m. | shipped |
| StormDrain | StreetFurniture | 0.7 × 0.04 × 0.4 | 528 (528/96) | Col_Grate | Flat grate. Flush. Not a vault. | shipped |
| TrafficLight | StreetFurniture | 0.4 × 5.025 × 1.81 | 440 (440/212) | Col_Base, Col_Pole, Col_Arm, Col_Head | Pole is round and 14 cm. Not a cling wall. No rail at vault height. | shipped |
| TrashCan_Lidded | StreetFurniture | 0.48 × 1.09 × 0.519 | 948 (948/276) | Col_Body, Col_Door | Not a cling surface. Too narrow to vault. | shipped |
| TrashCan_Slat | StreetFurniture | 0.47 × 0.877 × 0.47 | 1128 (1128/260) | Col_Liner, Col_Rim | Not a cling surface. 0.92 m rim is narrow and round. Not a vault rail. | shipped |
| Tree | Park | 3.482 × 3.539 × 3.066 | 1584 (1584/732/336) | Col_Flare, Col_Trunk | Trunk is round, about 0.28 m at the flare. Not a flat cling wall. No rail. The canopy is visual; the trunk is the blocker. | shipped |
| Tree_Maple | Park | 2.739 × 3.702 × 2.385 | 1584 (1584/732/264) | Col_Flare, Col_Trunk | Trunk is round. Not a flat cling wall. No rail. The canopy is visual; the trunk is the blocker. | shipped |
| Tree_Palm | Park | 3.1 × 4.345 × 3.1 | 120 (120/60/48) | Col_Trunk | Trunk is round. Not a flat cling wall. No rail. Fronds are visual; the trunk is the blocker. | shipped |
| Tree_Pine | Park | 2.968 × 4.214 × 2.75 | 2932 (2932/816/528) | Col_Flare, Col_Trunk | Trunk is round. Not a flat cling wall. No rail. The crown is visual; the trunk is the blocker. | shipped |
| UtilityPole | Utility | 1.8 × 8.6 × 6.2 | 384 (384/244) | Col_Pole, Col_Arm, Col_Anchor | Pole is a 28 cm timber at the base, tapering. Not a flat cling wall. No rail. Wires are visual only. | shipped |
| WallAC | Utility | 0.7 × 0.48 × 0.616 | 204 (204/36) | Col_Sleeve, Col_Head | Not a cling. Too small to vault. | shipped |
| WaterTank | Buildings | 1.75 × 4.16 × 1.56 | 608 (608/228/180) | Col_Leg x4, Col_Tank | Legs are 8 cm tubes, not a cling wall. The tank is round. No rail at vault height. | shipped |
| WoodFence | Buildings | 2.04 × 1.9 × 0.12 | 676 (676/132) | Climb_Boards, Col_PostL, Col_PostR | The board faces are a cling panel. Gaps are 6 mm and are not a passage. Top is 1.80 m. Too high to vault from the ground. | shipped |

## Modules

- Roads are 6 m wide and 4 m long. Top of asphalt is 0.12 m. Sidewalk top is 0.27 m (15 cm curb) and the curb faces -X. `Gutter` is 0.40 m wide and 4 m long; its +X lip meets that curb and its -X lip sits at the road edge. With the walk centered at x = 4.55, the gutter center is x = 3.25.
- Brick bays are 4.0 m wide, 3.2 m tall, 0.30 m thick, exterior +Z. Stack a second row at y = 3.2 for two stories. `Store_Corner`, `Store_Diner`, and `Store_Laundromat` are closed volumes. The ground floor is a shopfront: kickplate, mullioned display glass, a recessed door with a transom, a sign band, a brick string course, and a cornice. The awning is sloped fabric with a valance and a steel frame. Signs read MARKET, DINER, and WASH. `House_Gable` and `House_Hip` have a porch, steps, a paneled door, trimmed windows, eave gutters, and a chimney.
- Dock modules share a deck at 0.62 m. `Dock_Straight` is 6 × 3 m, with cleats on both sides and a rope coil. `Dock_Corner` is an L inside a 4 m square; its pivot is the center of that square.
- Containers are external ISO sizes: 20 ft is 6.06 × 2.44 × 2.59 m, 40 ft is 12.19 × 2.44 × 2.59 m. Doors face +Z. Ribs stand about 2 cm proud of the collider.
- The court is a 22 × 12 m street full court. Paint is one decal: FIBA markings scaled by 22/28 along the length and 12/15 across the width, every line 5 cm. Boundary, center line, center circle, lane, free-throw circle, restricted arc, and the 3-point arc are on that texture. `CourtFence` shares that pivot (baselines 3.05 m, sidelines 1.80 m, gate on +X). `Hoop` rim is 3.05 m and the overhang to the backboard is 0.86 m. Baskets are at z = ±9.71. Place the south pole at z = -10.57 (yaw 0) and the north pole at z = 10.57 (yaw 180).
- `DockRamp` is 4 × 2 m and falls from 0.62 m at -Z to 0.05 m at +Z. A dock centered at the origin meets a ramp centered at z = 4.
- `LaneArrow` and `StopBar` are paint. Place them on a road top (y = 0.12). They have no collider. `RaisedCrosswalk` replaces a 6 × 4 m road tile; the crown is 8 cm above the road and the collider follows that hump.
- `HarborWater` is a dark rippled sheet with no collider. `Quay_Edge` is 18 m long with an 8.2 m apron, deck at 0.90 m. Bullnose, edge stone, four bollards, a ladder, and five fenders are on -Z; the face runs below the pivot. `Container_20` stays the ISO 20 ft box (6.06 × 2.44 × 2.59 m) and sits on that apron. `Boat` is a work boat about 6.4 m long with a cabin, bow to -Z, gunwale near 1.0 m. `Piling` continues about 1.4 m below its pivot. `Mooring` is a 3.2 m finger at 0.55 m.
- No gazebo existed in the repo. `Pavilion` is the park shelter: 4.6 m square, rail 0.95 m above the deck, pyramid roof.
- `Mannequin` is a 1.80 m scale figure for the showcase. It is not a gameplay character.

## TODO

- Interior dressing once a shell is placed in an arena.
- Lighthouse fresnel, animated water, and night light cookies if a harbor becomes a landmark.
- A second dock length is two `Dock_Straight` modules. The quay is the working edge; the dock sits in the water in front of it.

