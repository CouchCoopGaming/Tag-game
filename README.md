# Tag (Unity) — local scaffold

**Path:** `/workspace/tag-unity`  
**Engine:** Unity **6000.0.23f1** (Unity 6 LTS) — see `ProjectSettings/ProjectVersion.txt`  
**Remote:** https://github.com/CouchCoopGaming/Tag-game (`main`)

Vertical slice: **2–4p punch-tag** (transfer-It on successful punch) with **three modes** (HotPotato / LeastIt / TrailTag).  
Movement kit (Apex-adjacent): sprint, jump, slide, wall run, wall jump, vault, air dodge (Systems Tag v1).  
**Out of scope:** double jump, grapple, climb-as-verb, guns, final CUT art mesh (graybox is in).
**Modes:** HotPotato · LeastIt · TrailTag via `TagModeController` + `ITagMode`.

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



## Modes framework

Shared flow: **Boot → Mode Select → Play → Rematch** (selected mode retained).

| Key | Mode | End condition |
|-----|------|---------------|
| **1** | **HotPotato** | Fuse timer (default **75s**). Punch transfers the potato (It). At 0, **current It is eliminated / loses**; others win. |
| **2** | **LeastIt** | Timed round (default **105s**). Punch transfers It. Winner = **least cumulative TimeAsIt** among survivors. |
| **3** | **TrailTag** | Everyone leaves a Light-Cycle ribbon. Hitting another player's trail (or own after grace) **eliminates**. Last alive wins (or survivors at time cap **120s**). Punch still transfers It (brighter trail). |

Confirm with **Enter / Space**. After a round: **R / Enter** rematch (same mode), **M** back to Mode Select.

### Architecture

| Piece | Path | Role |
|-------|------|------|
| `TagModeId` | `Scripts/Modes/` | HotPotato / LeastIt / TrailTag |
| `ITagMode` | | OnRoundStart, Tick, OnPunchTransfer, OnPlayerEliminated, ShouldEndRound, GetWinnerIds, GetHud |
| `TagModeController` | | Players, It transfer, countdown/round/results; delegates rules to `ITagMode` |
| `TagRoundController` | `Scripts/Tag/` | Thin subclass of `TagModeController` (scene back-compat) |
| `ModeSelectUI` | | Boot Mode Select OnGUI + keys 1/2/3 |
| Tunings | SO + `CreateRuntimeDefaults()` | `HotPotatoTuning`, `LeastItTuning`, `TrailTagTuning`, `MatchTuning` |
| Trails | `Scripts/Trail/` | `PlayerTrailEmitter` + `TrailSegment` (LineRenderer + trigger box meshes) |

### TrailTag tunables (`TrailTagTuning`)

| Field | Default | Notes |
|-------|---------|-------|
| `trailWidth` / width | 0.55 | Ribbon + collider width |
| `trailHeight` | 0.9 | Collider height |
| `lifetime` | 4.5s | Segment lifetime |
| `minSpacing` / segmentSpacing | 0.45 | Sample spacing |
| `selfHitGraceSec` | 1.25s | Own-trail immunity time |
| `selfHitGraceDist` | 1.5 | Own-trail immunity distance |
| `eliminateSelfAfterGrace` | true | Self-collision after grace |
| `matchTimeCap` | 120s | Soft timer (0 = no cap) |
| `spawnEmitDelay` | 0.75s | Delay before emit at round start |
| `itTrailBrightness` | 1.45 | Current It's ribbon emphasis |
| `colors[]` | cyan/orange/green/pink | Per-player ownership tint |
| `maxTrailMeters` | 80 | Cap meters of ribbon |

Assets: `Assets/ScriptableObjects/{HotPotato,LeastIt,TrailTag}Tuning.asset`.

### Smoke-test modes (Editor)

1. Open **Boot**, Play → **Mode Select**.
2. Press **1** / **2** / **3**, then **Enter** → Play loads CUT arena.
3. **LeastIt:** punch DummyRunner to transfer It; watch TimeAsIt HUD; wait or Rematch.
4. **HotPotato:** hold It near fuse end (or temporarily set `fuseDuration` ~8s on the SO) → It should eliminate and round ends with others winning.
5. **TrailTag:** DummyRunner patrols and emits a ribbon; run into its trail → you eliminate (or chase it into yours). Trails are visible (line + colored boxes). **R** rematches same mode.

Play opened directly skips Mode Select and uses `selectedMode` on Systems / PlayerPrefs.

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
| Boot | `Assets/Scenes/Boot.unity` | Start → Mode Select (OnGUI) → loads Play |
| Play | `Assets/Scenes/Play.unity` | CUT graybox (v0.1 chains + v0.2 density) (v0.1) + Player + DummyRunner + round systems |

Play contents (**CUT graybox v0.1** via `CutArenaBootstrap`):
- Runtime `CUT` root: Floor G, Bowl Y=−1 + 20° ramps + rim vaults, Loft +1.5 + lip, West/SW/East/SE walls, vaults, slide markers, 3×3 G pads, 4 spawns + elbows
- `CutArenaBootstrap` empty GO (idempotent rebuild on Awake)
- `Player` at SW spawn **(3, 0, 3)**: CharacterController + motor + camera pivot + punch + It + ragdoll stub
- `DummyRunner` near Bowl south rim **(18, 0, 8)** for punch test
- `Systems`: `GameFlow` + `TagRoundController` (is `TagModeController`; mode tunings wired)
- `Player` / `DummyRunner`: `PlayerTrailEmitter` (TrailTag); Dummy also has `DummyPatrol`

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
| Air dodge | Left Alt or Q | RB |
| Boot Start | Enter / Space / button | — |
| Mode Select | 1 HotPotato · 2 LeastIt · 3 TrailTag · Enter | — |
| Rematch (round over) | R / Enter (keeps mode) | — |

---

## Scripts (`Assets/Scripts/`)

| Script | Namespace | Notes |
|--------|-----------|-------|
| `Movement/MovementTuning.cs` | Tag.Movement | ScriptableObject — Systems Tag v1 momentum + air dodge |
| `Movement/PlayerMotor.cs` | Tag.Movement | CharacterController motor (momentum jump/slide + air dodge) |
| `Input/PlayerInputReader.cs` | Tag.Input | New Input + legacy fallback (incl. air dodge) |
| `Tag/PunchTagTuning.cs` | Tag.Gameplay | Ragdoll 1.5s, +8% / 2s boost |
| `Tag/ItController.cs` | Tag.Gameplay | It flag + time-as-It |
| `Tag/TagRoundController.cs` | Tag.Gameplay | Legacy shim → `TagModeController` |
| `Modes/TagModeController.cs` | Tag.Modes | Mode select host, It transfer, HUD |
| `Modes/{HotPotato,LeastIt,TrailTag}Mode.cs` | Tag.Modes | Per-mode rules |
| `Trail/PlayerTrailEmitter.cs` | Tag.Trail | Ribbon + segment triggers |
| `Tag/PunchHitbox.cs` | Tag.Gameplay | It-only melee (windup/active/recover, box cast) |
| `Tag/PlayerRagdoll.cs` | Tag.Gameplay | CC off → Rigidbody fall → restore |
| `Core/GameFlow.cs` | Tag.Core | Boot → ModeSelect → Play → Rematch |
| `Core/FollowCamera.cs` | Tag.Core | Optional 3rd-person follow (disabled on pivot; look is FPS-style on pivot) |
| `Level/CutArenaBootstrap.cs` | Tag.Level | Builds CUT graybox primitives from brief v0.1 on Awake |

Default assets: `Assets/ScriptableObjects/MovementTuning.asset`, `PunchTagTuning.asset`.  
Runtime `CreateRuntimeDefaults()` if references are missing.

---

## What's playable vs stubbed

### Should work in Editor Play Mode (after import)

- Walk / sprint / accel / brake / mouse look
- Jump with coyote (120 ms) + buffer (140 ms), custom gravity ≈28, apex ~1.15 m
- Momentum (Systems Tag v1): JumpHorizRetain 1.0 on all takeoffs; SlideEnterWipe false; hard land ×0.85 horiz for 0.1 s if fall > 1.5× apex (never zero); vault/wall-jump exit carries speed
- Air control (~45%) with airMomentumPreserve (no bleed toward walk while coasting)
- Slide (speed gate ≥5.5, keep current horiz on enter, decay to 55% end, jump-from-slide bonus, exit sprint)
- Air dodge (Systems Tag v1): airborne; 6.5 m/s planar replace toward input/facing; 130 ms lock; 100 ms punch i-frames; 80 ms buffer; 1 charge; recharge 1.8 m grounded travel (or 3 footfalls); blocked on wall-run / vault-lock / ragdoll / punch windup·active·miss-recover
- Wall run **first-pass** (side ray attach, gravity scale, timer, detach, wall jump out/up)
- Vault **first-pass** (forward obstacle height bands, lock move, lip-jump window)
- Punch → It transfer (if puncher is It), target ragdoll stub 1.5 s, puncher +8% speed 2 s
- Mode Select + HotPotato / LeastIt / TrailTag end conditions
- Trail ribbons (visible + colliding eliminate) on Player + DummyRunner
- Round timer + time-as-It tracking + Boot/Rematch (mode retained)
- Same movement kit for It and runner

### Stubbed / TODO / incomplete

- Wall run: full face/vel angle gates, along-wall clamp polish, opposite-hold feel, chain-cap attach min 6.2 tuning edge cases
- Wall jump: falloff ×0.85 within 0.8 s (floor ×0.55) not fully applied over time
- Vault: 35° cone auto-detect, fail penalty −40%, more robust lip detection
- Hard-land uses descent speed vs apex×1.5 threshold (approx fall distance)
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
Systems Tag v1 momentum + air dodge are **locked** in `MovementTuning`.

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
    Scripts/{Movement,Tag,Core,Input,Level,Modes,Trail}/
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
6. Multi-elim HotPotato ladder / sudden-death polish; authored PARK mesh when Level drops it.


## Modes (Boot → Mode Select → Play)
Keys **1** Hot Potato · **2** Least It · **3** Trail Tag · Enter to play · **R** rematch.

- **Hot Potato:** fuse 45/40/35s (2/3/4p). Timer 0 → current It loses the *round*; others +1 round-win. First to 2 wins (max 3 rounds). No elim.
- **Least It:** 120s; least cumulative time-as-It wins.
- **Trail Tag:** Light Cycle trails (`#00E5FF`); collision eliminates. Emitters All/ItOnly tunable.
