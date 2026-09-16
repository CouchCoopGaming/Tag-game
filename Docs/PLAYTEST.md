# Tag — local playtest (Amaterasu)

## Open

1. Unity Hub → open `C:\Users\Zubal\Dev\Tag-game-playtest` (6000.3.x / 6000.0.x).
2. Open scene **Play** (`Assets/Scenes/Play.unity`) → **Play**.
3. Optional first-time art: **Tag → Ensure URP Pipeline**, then **Tag → Setup Hub Visuals**.

Branch: `cursor/apex-party-movement-f5fd`. Deeper notes: `Docs/MOVEMENT.md` / `Docs/PLAY-SLICE.md`.

## Stack snapshot

| Piece | What you get |
|-------|----------------|
| **TP cam** | `TpsMoveCamera` orbit/follow (~boom −5.2), soft collision, FOV by MoveState + slight speed look-ahead (`lookAheadMax` ~0.9, `speedFovBoostMax` ~3°) |
| **Hier dummy** | `DummyAvatarBinder` prefers `*_Hier_Hi` FBX → flat HiPoly → Navy Spade primitive (foam + polymer panels + matte joints; palette aligned with `Tools/Tag/build_mannequin_hier.py` — Tan runner / Orange It); `DummyLocomotor` swings limbs when bindable UpperArm/UpperLeg hierarchy exists |
| **Modes** | F1 Hot Potato / F2 Least It / F3 Trail Tag (`TagModeController` SetMode + StartRound) |
| **HUD** | `SpeedEnergyHUD` (local P0): km/h + MoveState, JET bar, ski/jet flags, controls cheat-sheet, mode + who is It, HP fuse (pulse when It ≤ warnSec), Least It times (brief all-standings flash); **LEAD** (mint) / **LAG** (coral) on local standings; **TAG flash** YOU'RE IT / YOU'RE FREE; **It compass** (flee / not It) + **Prey compass** (hunt / It → nearest alive); both pulse <12 m w/ distinct tints; cam bearing + m |
| **Void / XZ** | `VoidRespawn`: Y < −20 **or** mega-park XZ AABB (+~20 m) → nearest `LocalPlayerSpawner` pad; clear ragdoll/stun, zero vel, ~1 s punch i-frames |
| **Playground** | Figure-8 + outer ring chase through real playground (soft-play, slides, monkey bars, wall strips, slide banks, merry/swing/kickball/hopscotch). Rebuild: **CutArenaBootstrap** / **PgkLandmarkPlacer → Place**. Colliders on spawn (boxes if FBX not readable) |
| **Colliders** | `StaticPropColliders.EnsureStaticColliders` after dress/place so HiPoly/PGK toys keep Mesh/Box collision |
| **Trail Tag** | Wide bright light-cycle walls (mega-park WorldScale 10); Stay + Default-layer triggers so RB motor still eliminates; near-miss **TRAIL!** <4.5 m foreign; elim **OUT!** / TRAIL HIT |
| **SFX** | `TagSfx`: Resources/Audio clips when present, else procedural one-shots (punch, It, ski/jet, slide, lunge whoosh, jump) |
| **Ski spines** | Thicker mega-park ski spines (~3 m wide / 0.12 m thick) + zone approach ramps (pad → nearest spine); denser PGK connectors (`CutArenaBootstrap.BuildSkiSpines`) |
| **Ski crest** | Tribes leave: outward ski launch factor **1.0**, threshold `skiLaunchLeaveDot` ~0.12; DummyLocomotor air loft tell |
| **Punch** | Connect = loud kick + hold arm; miss = soft blip + limp arm (`TagSfx` / DummyLocomotor / `PunchHitbox`) |
| **Lunge** | It-only MMB: ~16 m/s, ~0.20 s, **CD ~1.0 s**; TP whip→settle via `LungeProgress` |
| **Jump / land** | Coyote ~0.10 s, jump buffer ~0.16 s; hard land → LandStun; DummyLocomotor firmer land squash |
| **Motor knobs** | Live on `Assets/Resources/TagArena/MovementConfig.asset` (`Resources.Load` `TagArena/MovementConfig`); recreate via **Tag → Create MovementConfig Asset** (won't overwrite) |
| **Mantle / climb / glide / bounce** | Stickier mega-park mantle + wall-climb, fairer super-glide window, punchier wall bounce (TP vault/climb/glide/kick tells) |
| **AI** | `DummyPatrol`: It chase + punch sync to `PunchHitbox` reach/cone; not-It flee with lead/strafe (wander when far); Least It: It prefers low TimeAsIt leaders, non-It clusters with non-It allies; drives `PlayerMotor` via input |

## Controls (`PlayerInputReader`)

| Action | Default |
|--------|---------|
| Move | WASD |
| Look | Mouse |
| Ski | Left Shift |
| Sprint (when not skiing) | Left Shift / Left Alt |
| Jet | RMB (Mouse1) |
| Jump | Space |
| Crouch / slide gate | Ctrl or C |
| Punch (It transfer) | LMB (Mouse0) or E |
| **Lunge (It only)** | **MMB (Mouse2)** |
| Mode hotkeys | F1 / F2 / F3 |

Punch is **not** a contact aura — only active punch hits transfer It (`PunchHitbox`).

## Known leftovers

- Prefab/mat dirt after Hub visuals / URP regen — do not commit unless intentional.
- Flat HiPoly mannequins may skip hierarchical `DummyLocomotor` binds (primitive / bindable-bone path is the readable tell).
- Legacy contact `TryTag` radius still exists on motor; play modes use punch transfer.
- Trail Tag / Hot Potato polish and AI still party-slice rough.
- Do not hand-author `TagURP*.asset` YAML; use **Ensure URP Pipeline**.