# PARK prop bind map (Hub) — 3D → Engineer

**Meters locked.** Do not move CUT/PARK positions. Skin / swap mesh on existing `CutArenaBootstrap` Toy_* boxes.

## Priority (AD / Landon Hub)
| Bootstrap name | FBX | Mat | Notes |
|----------------|-----|-----|-------|
| Vault rails / benches / mid-cut / islands | `Toy_VaultRail_090.fbx` / `_100` / `_105` | `Mat_Park_Yellow` lip + `Mat_Park_Steel` body | Heights 0.90 / 1.00 / 1.05 — yellow lip language |
| `Toy_Bars_0..2` | `Toy_Bars.fbx` (unit) or instance 3× rail | Yellow/steel | X centers 10.6, 11.8, 13.0 Z=7 — vault only |
| `Toy_Slide_C1` | `Toy_Slide.fbx` | Yellow + blue rails | Footprint ~5×2 on X[2,7] Z[10,12]; scale to marker |
| `Toy_RubberTrack_C3` | `Toy_Slide.fbx` (retint rubber) or duplicate | `Mat_Park_Rubber` + yellow edge | X[16,22] Z[3,5] |
| `Toy_Tower` / `Toy_TowerLip` / tower slides | `Toy_Tower.fbx` + slide | Blue / yellow lip / concrete deck | Loft X[15,21] Z[24,28] Y=+1.5 |
| `Toy_ClimbWall_*` / twin towers | `Toy_WallPanel.fbx` | Steel + blue bands | Tile; don't bake world pos |
| Mulch floor / bowl | bootstrap colors already | `Mat_Park_Mulch` | Engineer owns recolor — match `#5C3A2E` |
| Spawns | `Toy_SpawnPad_*.fbx` | accent colors | SW/SE/NW/NE |
| Hedges / elbows | `Toy_Bumper.fbx` | Red/yellow | 1.2 m vaultable |

## Player
| Prefab | FBX | Slots |
|--------|-----|-------|
| `Dummy_Runner` | `Assets/Art/Characters/Dummy_Runner.fbx` | Base / Accent / ItOverride |
| `Dummy_It` | `Assets/Art/Characters/Dummy_It.fbx` | same |

## Trail
`Assets/Art/VFX/Trail/Trail_Ribbon.fbx` + `Mat_Trail_Cyan` (`#00E5FF`), bottom clear **1.05 m**.

## Folders
- Meshes: `Assets/Art/Props/Playground/*.fbx`
- Mats: `Assets/Art/Props/Playground/Materials/`
- Prefab stubs: `Assets/Art/Props/Playground/Prefabs/` (root placeholders — bind mesh in Editor)

## Blockers for 3D
- None on exports. Need Engineer to swap CreatePrimitive boxes → prefab/mesh refs in bootstrap or Hub scene.
- Chevron micro-pass parked (AD: after Hub).
