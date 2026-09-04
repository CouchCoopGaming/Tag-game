# Tag (Unity) — local scaffold

**Path:** `/workspace/tag-unity`  
**Engine:** Unity **6000.0.23f1** (Unity 6 LTS) — see `ProjectSettings/ProjectVersion.txt`  
**Remote SCM:** https://github.com/CouchCoopGaming/Tag-game

Vertical slice: **2–4p punch-tag** (transfer-It on successful punch), timed round (~90–120s), score = **least time-as-It**.  
Movement kit (Apex-adjacent): sprint, jump, slide, wall run, wall jump, vault.  
**Out of scope:** double jump, grapple, climb-as-verb, guns, multi-mode framework, full CUT arena mesh.

---

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
| Play | `Assets/Scenes/Play.unity` | Test graybox: floor, 2 wall-run walls, low vault block, Player + DummyRunner, round systems |

Play contents (movement test only — **not** full CUT):
- Floor plane (~40×40)
- `Wall_TestA` / `Wall_TestB` (~3.2 m gap) for wall-run / wall-jump
- `Vault_LowBlock` (~1.0 m) for vault probe
- `Player` capsule: CharacterController + motor + camera pivot + punch + It + ragdoll stub
- `DummyRunner` stationary target for punch / It transfer
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
| Boot Start | Enter / Space / button | — |
| Rematch (round over) | R / Enter | — |

---

## Scripts (`Assets/Scripts/`)

| Script | Namespace | Notes |
|--------|-----------|-------|
| `Movement/MovementTuning.cs` | Tag.Movement | ScriptableObject — Systems Movement Numbers v0 defaults |
| `Movement/PlayerMotor.cs` | Tag.Movement | CharacterController motor |
| `Input/PlayerInputReader.cs` | Tag.Input | New Input + legacy fallback |
| `Tag/PunchTagTuning.cs` | Tag.Gameplay | Ragdoll 1.5s, +8% / 2s boost |
| `Tag/ItController.cs` | Tag.Gameplay | It flag + time-as-It |
| `Tag/TagRoundController.cs` | Tag.Gameplay | Timer, transfer-It, score HUD stub |
| `Tag/PunchHitbox.cs` | Tag.Gameplay | OverlapSphere punch |
| `Tag/PlayerRagdoll.cs` | Tag.Gameplay | CC off → Rigidbody fall → restore |
| `Core/GameFlow.cs` | Tag.Core | Boot → Play → Rematch |
| `Core/FollowCamera.cs` | Tag.Core | Optional 3rd-person follow (disabled on pivot; look is FPS-style on pivot) |

Default assets: `Assets/ScriptableObjects/MovementTuning.asset`, `PunchTagTuning.asset`.  
Runtime `CreateRuntimeDefaults()` if references are missing.

---

## What's playable vs stubbed

### Should work in Editor Play Mode (after import)

- Walk / sprint / accel / brake / mouse look
- Jump with coyote (120 ms) + buffer (140 ms), custom gravity ≈28, apex ~1.15 m
- Air control (~45%)
- Slide (speed gate, duration, height shrink, jump-from-slide bonus, exit sprint)
- Wall run **first-pass** (side ray attach, gravity scale, timer, detach, wall jump out/up)
- Vault **first-pass** (forward obstacle height bands, lock move, lip-jump window)
- Punch → It transfer (if puncher is It), target ragdoll stub 1.5 s, puncher +8% speed 2 s
- Round timer + time-as-It tracking + Boot/Rematch flow
- Same movement kit for It and runner

### Stubbed / TODO / incomplete

- Wall run: full face/vel angle gates, along-wall clamp polish, opposite-hold feel, chain-cap attach min 6.2 tuning edge cases
- Wall jump: falloff ×0.85 within 0.8 s (floor ×0.55) not fully applied over time
- Vault: 35° cone auto-detect, fail penalty −40%, more robust lip detection
- Hard-land detection is approximate
- Ragdoll is **not** bone ragdoll — whole-body Rigidbody freeze/fall
- No netcode / multiplayer session — local DummyRunner only
- No `.inputactions` asset; OnGUI menus only (no TMP canvas yet)
- URP asset/renderer may need Unity to auto-create on first open (manifest lists URP; no custom Renderer asset checked in)
- Full **CUT** arena not built — see Level brief below

---

## Level graybox (upcoming)

Do **not** drop the full CUT mesh into this scaffold yet. Brief + plan live at:

- `/workspace/tag-gdd/level/CUT-graybox-v0.md` (symlink: `Docs-CUT-graybox-v0.md` in this project)
- `/workspace/tag-gdd/level/CUT-plan.svg`
- In-repo note: `Assets/Art/Graybox/README_CUT_REF.md`

Systems Movement Numbers v0 are **locked** and baked into `MovementTuning`.

---

## Layout

```
/workspace/tag-unity/
  README.md
  .gitignore
  Docs-CUT-graybox-v0.md          → symlink to tag-gdd brief
  Packages/manifest.json
  ProjectSettings/ProjectVersion.txt   # 6000.0.23f1
  Assets/
    Scripts/{Movement,Tag,Core,Input}/
    Scenes/{Boot,Play}.unity
    ScriptableObjects/
    Prefabs/                      # empty — ready for player prefab extract
    Art/Graybox/
```

---

## Next needs

1. Open in Unity Hub; let packages resolve; create URP pipeline asset if prompted.
2. Smoke-test movement on Play walls/vault block; tune via SO assets.
3. Level: drop CUT graybox into `Art/Graybox` + new scene (or replace Play geo).
4. Flesh wall-run / vault TODOs; bone ragdoll optional.
5. Add Input Actions asset + simple TMP HUD; then netcode / 2–4p session.
6. Init remote SCM when ready (not done here).
