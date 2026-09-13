# Sundeck Playground Kit (Landon 2026-09-12)

Source: production bible xlsx (Sundeck Playground Kit v0.1).
Tag uses this as the modular park art direction. Graybox PARK still uses CutArenaBootstrap WorldScale=10 for giant fantasy; kit pieces author at 1m then assemble/scale in Unity.

## Non-negotiables
- Blender meters, FBX: -Z forward, Y up, Apply Scalings = FBX All, Scale 1
- Snap 0.25 m; posts on 1.00 m; deck heights 0.40 / 0.80 / 1.20 / 1.60 / 2.00 only
- Naming: PGK_{Category}_{Name}_{Variant}_LOD0
- Author _COL boxes/capsules — never Generate Colliders on play gear
- Pivot ground / connect face; ~10 shared materials
- Hundreds of prefabs = kit + 6 colorways + assemblies — do NOT unique-model 400 heroes

## 12h grind priority (Amaterasu)
1. Kit_Post: Square 1/1.5/2/2.5/3m + caps
2. Kit_Deck: 1x1, 1x2, 2x2, Corner L
3. Kit_Rail: 1m, 2m, Corner90
4. Kit_Access: Stairs 5, Ladder Rung, Incline Climber
5. Play_Slide: Straight M, Tube90, Spiral270 (hero)
6. Play_Climb: Monkey 4m, Dome Geo, Tunnel Plastic, Balance Beam
7. Play_Motion: Spinner StandOn, SeeSaw Beam (static first)
8. Ground: SafetySurface tiles + mulch plane

Mannequin color variants continue in parallel for player.
