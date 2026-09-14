# Tag — local playtest (Amaterasu)

## Open

1. Unity Hub → open `C:\Users\Zubal\Dev\Tag-game-playtest` (6000.3.x / 6000.0.x).
2. Open scene **Play** (`Assets/Scenes/Play.unity`) → **Play**.
3. Optional first-time art: **Tag → Ensure URP Pipeline**, then **Tag → Setup Hub Visuals**.

Branch: `cursor/apex-party-movement-f5fd`. Deeper movement notes: `Docs/MOVEMENT.md` / `Docs/PLAY-SLICE.md`.

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
| Mode hotkeys | F1 / F2 / F3 (see below) |

Punch is **not** a contact aura — only active punch hits transfer It (`PunchHitbox`).

## Modes (playtest hotkeys)

On `TagModeController` while playing:

| Key | Mode |
|-----|------|
| **F1** | Hot Potato |
| **F2** | Least It |
| **F3** | Trail Tag |

Each hotkey SetMode + StartRound cleanly.

## HUD legend (`SpeedEnergyHUD`, local P0)

Top-left:

- **km/h + MoveState**
- **JET** energy bar + jetting / ski flags
- Controls cheat-sheet (same bindings as above + F1–F3)
- Match status: mode name, who is It
- Hot Potato: fuse urgency line
- Least It: time-as-It standings (brief all-standings flash on a cycle)

## Void / XZ respawn (`VoidRespawn`)

Teleports to nearest `LocalPlayerSpawner` pad when:

- **Y** below kill plane (~−20), or
- **XZ** outside mega-park AABB (map size × world scale + ~20 m margin)

Clears ragdoll/stun, zeroes velocity, brief punch i-frames after teleport.

## Lunge (MMB)

It-only short forward dash (`PlayerMotor.TryLunge`):

- Speed ~16 m/s, duration ~0.20 s, **cooldown ~1.0 s**
- Applies on press (wish dir or facing); `TagSfx.LungeWhoosh`
- `DummyLocomotor` whip->settle arms/stride via `LungeProgress` while `IsLunging` (beats jet pack pose)

## Known leftovers

- Prefab/mat dirt often appears after Unity Hub visuals / URP regen — do not commit unless intentional.
- Flat HiPoly mannequins may skip hierarchical `DummyLocomotor` binds (primitive / bindable-bone path is the readable tell).
- Legacy contact `TryTag` radius still exists on motor; play modes use punch transfer.
- Placeholder SFX: Resources clips when present, else procedural `TagSfx`.
- Trail Tag / Hot Potato polish and AI still party-slice rough.
- Do not hand-author `TagURP*.asset` YAML; use **Ensure URP Pipeline**.