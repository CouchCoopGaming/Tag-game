# Tag (Unity) — local scaffold

**Path:** `/workspace/tag-unity`  
**Engine:** Unity **6000.0.23f1** (Unity 6 LTS) — see `ProjectSettings/ProjectVersion.txt`  
**Remote:** https://github.com/CouchCoopGaming/Tag-game (`main`)

Vertical slice: **2–4p punch-tag** (transfer-It on successful punch), timed round (~90–120s), score = **least time-as-It**.  
Movement kit (Apex-adjacent): sprint, jump, slide, wall run, wall jump, vault, air dodge (juke stub).  
**Out of scope:** double jump, grapple, climb-as-verb, guns, multi-mode framework, final CUT art mesh (graybox is in).

---

## Punch / Transfer-It (Systems v0 — LOCKED)

**It-only dedicated melee. NOT contact aura.**

| Phase | Value |
|-------|-------|
| Input buffer | 80 ms |
| Windup | 120 ms @ 70% move |
| Active | 100 ms (continuous cast preferred) |
| Hit recover | 150 ms |
| Miss recover | 320 ms (no slide cancel) |

**Hitbox:** reach 1.35 · W 0.70 · H 1.20 · mid-torso · ±15° pitch · LoS · closest runner

**On hit:** transfer-It · ragdoll 1.5s + i-frames · knock 4.0 horiz + 2.0 up · puncher +8% walk+sprint for 2s (no stack; refresh on hit; clears on losing It)

Baked into `PunchTagTuning` defaults + `Assets/ScriptableObjects/PunchTagTuning.asset`. Runtime `CreateRuntimeDefaults()` if asset missing.


## Open in Unity Hub

1. Install **Unity 6000.0.23f1** (or any 6000.0.x LTS close to it).
2. Hub → **Open** → select `/workspace/tag-unity` (or copy this folder to your machine).
3. First open will import URP + Input System + TMP from `Packages/manifest.json` (needs network for Package Manager).
4. **File → Build Settings** should list `Assets/Scenes/Boot` then `Play` (already in `EditorBuildSettings.asset`).
5. Open **Boot**, press Play → Start, or open **Play** directly.

> Editor is **not** installed on this shared box — do not expect Play Mode here.

### Input System note

`ProjectSettings` sets `activeInputHandler: 2` (Both). `PlayerInputReader` prefers the New Input System and falls back to legacy `Input` (WASD / mouse). No `.inputactions` asset yet — bindings are code-side.

---

## Scene order

| Scene | Path | Role |
|-------|------|------|
| Boot | `Assets/Scenes/Boot.unity` | Start UI (OnGUI) → loads Play |
| Play | `Assets/Scenes/Play.unity` | CUT graybox (v0.1) + Player + DummyRunner + round systems |

Play contents (**CUT graybox v0.1** via `CutArenaBootstrap`):
- Runtime `CUT` root: Floor G, Bowl Y=−1 + 20° ramps + rim vaults, Loft +1.5 + lip, West/SW/East/SE walls, vaults, slide markers, 3×3 G pads, 4 spawns + elbows
- `CutArenaBootstrap` empty GO (idempotent rebuild on Awake)
- `Player` at SW spawn **(3, 0, 3)**: CharacterController + motor + camera pivot + punch + It + ragdoll stub
- `DummyRunner` near Bowl south rim **(18, 0, 8)** for punch test
- `Systems`: `GameFlow` + `TagRoundController` (105 s default)

---

## Controls

| Action | Keyboard / mouse | Gamepad (New Input) |
|--------|------------------|---------------------|
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Sprint | Shift | L3 / LB |
| Jump | Space | A / South |
| Slide | Ctrl or C | B / East |
| Punch | LMB or R | X / West |
| Air dodge (juke) | Left Alt or Q | RB / Right shoulder |
| Boot Start | Enter / Space / button | — |
| Rematch (round over) | R / Enter | — |

---

## Scripts (`Assets/Scripts/`)

| Script | Namespace | Notes |
|--------|-----------|-------|
| `Movement/MovementTuning.cs` | Tag.Movement | ScriptableObject — Systems Movement Numbers v0 defaults |
| `Movement/PlayerMotor.cs` | Tag.Movement | CharacterController motor (momentum jump/slide + air dodge) |
| `Input/PlayerInputReader.cs` | Tag.Input | New Input + legacy fallback (incl. air dodge) |
| `Tag/PunchTagTuning.cs` | Tag.Gameplay | Ragdoll 1.5s, +8% / 2s boost |
| `Tag/ItController.cs` | Tag.Gameplay | It flag + time-as-It |
| `Tag/TagRoundController.cs` | Tag.Gameplay | Timer, transfer-It, score HUD stub |
| `Tag/PunchHitbox.cs` | Tag.Gameplay | It-only melee (windup/active/recover, box cast) |
| `Tag/PlayerRagdoll.cs` | Tag.Gameplay | CC off → Rigidbody fall → restore |
| `Core/GameFlow.cs` | Tag.Core | Boot → Play → Rematch |
| `Core/FollowCamera.cs` | Tag.Core | Optional 3rd-person follow (disabled on pivot; look is FPS-style on pivot) |
| `Level/CutArenaBootstrap.cs` | Tag.Level | Builds CUT graybox primitives from brief v0.1 on Awake |

Default assets: `Assets/ScriptableObjects/MovementTuning.asset`, `PunchTagTuning.asset`.  
Runtime `CreateRuntimeDefaults()` if references are missing.

---

## What's playable vs stubbed

### Should work in Editor Play Mode (after import)

- Walk / sprint / accel / brake / mouse look
- Jump with coyote (120 ms) + buffer (140 ms), custom gravity ≈28, apex ~1.15 m
- Air control (~45%)
- Jump / slide carry current planar speed (no hard reset to walk); air coast preserves momentum
- Slide (speed gate ≥5.5, entry momentum into punch/coast, decay to 55% end, jump-from-slide bonus, exit sprint)
- Air dodge / juke **stub** (airborne only; ~4 m/s planar over ~0.15 s; 3 grounded stride recharge; i-frames off)
- Wall run **first-pass** (side ray attach, gravity scale, timer, detach, wall jump out/up)
- Vault **first-pass** (forward obstacle height bands, lock move, lip-jump window)
- Punch → It transfer (if puncher is It), target ragdoll stub 1.5 s, puncher +8% speed 2 s
- Round timer + time-as-It tracking + Boot/Rematch flow
- Same movement kit for It and runner

### Stubbed / TODO / incomplete

- Air dodge: stub numbers (impulse 4.0 / duration 0.15 / up 0.35 / stride 0.8 / 3 steps); i-frames false; await Systems sheet
- Wall run: full face/vel angle gates, along-wall clamp polish, opposite-hold feel, chain-cap attach min 6.2 tuning edge cases
- Wall jump: falloff ×0.85 within 0.8 s (floor ×0.55) not fully applied over time
- Vault: 35° cone auto-detect, fail penalty −40%, more robust lip detection
- Hard-land detection is approximate
- Ragdoll is **not** bone ragdoll — whole-body Rigidbody freeze/fall
- No netcode / multiplayer session — local DummyRunner only
- No `.inputactions` asset; OnGUI menus only (no TMP canvas yet)
- URP asset/renderer may need Unity to auto-create on first open (manifest lists URP; no custom Renderer asset checked in)
- CUT graybox is **runtime primitives** (not final art mesh)

---

## Level graybox (CUT v0.1 — in Play)

`CutArenaBootstrap` builds the arena from `/workspace/tag-gdd/level/CUT-graybox-v0.1.md` (+ `CUT-plan.svg`).

| Zone | Notes |
|------|--------|
| Bowl | Y=−1.0, X[13,23] Z[9,19], rim vault 1.00, corner ramps 20° |
| West Spine (Chain 1) | West Wall 10×3.2 @ X=8 Z[8,18]; slide X[2,7] Z[10,12]; landing vault 1.00 @ X[10.4,12.4] |
| East Alley (Chain 2) | Faces X=28.0 & 31.2 (3.2 m gap), 10×3.5; G pads at mouths |
| South Lane (Chain 3) | Rails 0.90 then 1.05; slide X[16,22] Z[3,5] → Bowl |
| North Loft | +1.5, 6×4, lip vault 1.50, side drop ramps 20° |
| Spawns | SW/SE/NW/NE on G; orange pads + yellow elbows |

Brief copies in-repo: `Assets/Art/Graybox/CUT-graybox-v0.1.md`, `CUT-plan.svg`, `README_CUT_REF.md`.  
Systems Movement Numbers v0 are **locked** in `MovementTuning`.

### Smoke-test the three chains (open Play, press Play)

1. **Chain 1 (West Spine → Bowl):** From SW spawn, sprint north along X≈4–7 → magenta slide X[2,7] Z[10,12] → jump onto West Wall (cyan @ X=8) → wall-run ≤0.8 s → wall-jump east ~2.8 m onto yellow Chain1 landing vault (1.00 @ X[10.4,12.4] Z[15,16]) → sprint into Bowl.
2. **Chain 2 (East Alley):** Enter alley between X=28 and X=31.2 → sprint/jump → wall-run → wall-jump opposite face (3.2 m gap) → 2–3 ping-pongs → land on magenta **G pad** at a mouth (no air-loop of 4 walls).
3. **Chain 3 (South Lane):** From west, sprint X[6,22] Z≈[2.5,5.5] → vault 0.90 (X[10,12]) → vault 1.05 (X[14,16]) → magenta slide X[16,22] Z[3,5] → jump into Bowl south rim (no wall in landing cone).

Also: loft lip vault 1.50 from G at Z≈24; bowl corner ramps 20°; punch DummyRunner near Bowl.

---

## Layout

```
/workspace/tag-unity/
  README.md
  .gitignore
  Packages/manifest.json
  ProjectSettings/ProjectVersion.txt   # 6000.0.23f1
  Assets/
    Scripts/{Movement,Tag,Core,Input,Level}/
    Scenes/{Boot,Play}.unity
    ScriptableObjects/
    Prefabs/
    Art/Graybox/                  # brief + plan + README_CUT_REF; geo via CutArenaBootstrap
```

---

## Next needs

1. Open in Unity Hub; let packages resolve; create URP pipeline asset if prompted.
2. Smoke-test Chains 1–3 on CUT graybox; tune via SO assets.
3. Replace runtime primitives with authored graybox/final mesh when Level drops meshes.
4. Flesh wall-run / vault / air-dodge Systems sheet; bone ragdoll optional.
5. Add Input Actions asset + simple TMP HUD; then netcode / 2–4p session.
