# Tag — playable party slice

Crash-test dummies playing punch-tag in a giant playground. **This slice ships Least It** (classic Tag: It punches to transfer It; least time-as-It wins). Hot Potato and Trail Tag stay in the project but are out of scope here.

Movement numbers: [`Docs/MOVEMENT.md`](MOVEMENT.md).

## Safe Mode / compile (PR #8)

If Unity 6 opens this worktree in **Safe Mode**, pull `cursor/apex-party-movement-f5fd` again. Fixes: isolated test asmdef removed (tests compile in Assembly-CSharp-Editor), `TagInputActions.AddAction` no longer uses `expectedControlType`, trail `Object.Destroy` is `UnityEngine.Object`. Then **Ignore** Safe Mode / reimport scripts. Smoke: **Tag → Run Movement Kinematics Smoke**.

## Amaterasu / reopen (do this after every pull if the Editor crashed)

The local bypass that kept Unity 6000.3 alive (delete `Assets/Settings/` URP stubs, delete `Resources/**/Fbx/`, clear Graphics/Quality pipeline, wipe `Library`) is what this branch ships. Do not re-add hand-written `TagURP*.asset` YAML.

1. `git pull` on `cursor/apex-party-movement-f5fd` (discard local copies of those deletions if git complains — the PR already removed them)
2. Quit Unity
3. Delete the worktree **`Library`** folder
4. Hub → Open this folder (6000.0.x or 6000.3.x)
5. Wait for `Rebuilding Library`. After scripts compile, **Tag → Ensure URP Pipeline** runs (also in the menu). Unity writes `Assets/Settings/TagURP*.asset` itself — those generated files are fine to keep locally; do not replace them with stubs.
6. If materials are still magenta: run the menu again, then Play.

If HiPoly `*_Hi.fbx` files are on disk, **Tag → Setup Hub Visuals** prefers them (see below).

## URP (fixes magenta / pink)

Materials under `Assets/Art` already use **URP Lit**. Pink was Built-in RP with no pipeline assigned.

Do **not** commit hand-written `TagURP*.asset` YAML. On open:

| Step | Who |
|------|-----|
| Create renderer + pipeline | **Tag → Ensure URP Pipeline** (`TagUrpSetup`, also `InitializeOnLoad`) |
| Assign Graphics + Quality | same menu |
| Play Mode if assets missing | `TagUrpBootstrap` builds an in-memory URP pipeline |

## Dummy meshes + motion

`Dummy_Runner.prefab` / `Dummy_It.prefab` stay as Hub stubs. Play uses `DummyLocomotor` on FBX bones after **Tag → Setup Hub Visuals**, or a primitive dummy with the same bone names. PARK toys dress from Resources prefabs after that menu (FBX is **not** duplicated under Resources).

There are **no `.anim` / `.controller` files** in the drop. `DummyLocomotor` is the shippable tell (party-punchy, not mocap). Bone names match Dummy_* FBX and the primitive fallback:

`Hips`, `Spine`, `Head`, `UpperArm_L/R`, `LowerArm_L/R`, `UpperLeg_L/R`, `LowerLeg_L/R` (Humanoid / mixamo aliases also bind).

| State | Readable tell |
|-------|----------------|
| Idle | Chest breathe + slight head/arm sway |
| Run / sprint | Opposite arm/leg swing, hip yaw, foot bob |
| Jump | Stretch on takeoff, limb tuck in air, land squash |
| Slide | Forward lean, hips down, arms out |
| Air dash | Fast limb snap + slight forward pose |
| Punch (It) | Right-arm windup (cock back) then hit extend; opposite arm braces |

`PunchHitbox.PhaseProgress` drives the windup→hit blend. `PlayerMotor.VerticalSpeed` / `IsAirDodgeLocked` / `IsSliding` drive jump, dash, and slide.

Optional Hub bind:

1. **Tag → Ensure URP Pipeline**
2. **Tag → Setup Dummy Prefabs From FBX** or **Tag → Setup Hub Visuals (Dummies + Props + Play Bind)**

## HiPoly setup

Landon’s denser meshes live next to the low-poly drop. **Tag → Setup Hub Visuals** (`ArtMeshPaths`) picks the first file that exists, then writes **prefabs** (never raw FBX) into `Assets/Art/Characters/Dummy_*.prefab` and `Assets/Resources/{Characters,Props}/`. `DummyAvatarBinder` and `ParkPropDresser` load those Resources prefabs at Play.

| Slot | Preferred path | Fallback |
|------|----------------|----------|
| Runner | `Assets/Art/Characters/HiPoly/Dummy_Runner_Hi.fbx` | `Assets/Art/Characters/Dummy_Runner.fbx` |
| It | `Assets/Art/Characters/HiPoly/Dummy_It_Hi.fbx` | `Assets/Art/Characters/Dummy_It.fbx` |
| Park toys | `Assets/Art/Props/Playground/HiPoly/Toy_*_Hi.fbx` | `Assets/Art/Props/Playground/Toy_*.fbx` |

Examples now on the branch: `Toy_Bench_Hi.fbx`, `Toy_Slide_Hi.fbx`, `Toy_Bars_Hi.fbx`, `Toy_VaultRail_090_Hi.fbx`, spawn pads, bumpers, tower, wall panel. Assign `Mat_Runner_*` / `Mat_It_*` / `Mat_Park_*` in the Hub setup (already wired). After a HiPoly drop: quit Unity if it was open, then run **Tag → Setup Hub Visuals** so Resources prefabs refresh.

Do **not** copy FBX into `Resources/**/Fbx/`. Unity generates ModelImporter `.meta` on first import — do not hand-author truncated ones.

## Open in Unity

1. Hub → Open this repo (Unity **6000.0.23f1** / **6000.0.24f1** / **6000.3.x**).
2. Either:
   - Open **Boot**, Play → **Play Tag (Least It)** (Enter / Space), or
   - Open **Play** directly (skips Boot; starts Least It vs Dummy).
3. First-time art (optional): **Tag → Setup Hub Visuals**. Without it you still get a non-pink primitive dummy + locomotor.

## How to play

You are a **third-person dummy**. One AI dummy patrols the PARK arena.

1. Countdown (3s), then someone is randomly **It** (orange hat + ground halo).
2. **It punches** (LMB / X) to transfer It. Not a contact aura.
3. Clock is **120s**. HUD tracks each dummy’s **time-as-It**. Least wins.
4. Tie → next successful punch from a tied player (or timeout).
5. **R** rematch · **Esc** pause · **Q** menu.

## Controls (New Input System)

Bindings live in `TagInputActions` (runtime) and `Assets/Input/Tag.inputactions` (Editor asset). `activeInputHandler = Both`.

| Action | Keyboard / mouse | Gamepad |
|--------|------------------|---------|
| Move (auto-sprint) | WASD | Left stick |
| Look | Mouse | Right stick |
| Jump | Space | A / South |
| Slide (hold while fast) | Ctrl or C | B / East |
| Punch / tag | LMB | X / West |
| Air dash (air only) | Q or Left Alt | RB |
| Move HUD | F3 | — |

Sprint is automatic on full WASD. Light stick walks (~5.5 m/s). Sprint class **9 m/s**. Air dash ~**2.25 m**.

## What you should see

- Third-person dummy (not FPS). Vinyl Runner `#E8D9C0` / teal accent, or It `#FF6A00` / black chevrons — **not magenta**.
- Visible idle breathe, run cycle, jump stretch/tuck + land squash, slide lean, air-dash snap, and a right-arm punch windup→hit.
- **It** = orange hat + pulsing floor ring + point light. Punch dumps It; the other dummy swaps the hat.
- PARK mulch / yellow vaults / blue walls (URP Lit). Toy meshes dress over graybox after **Tag → Setup Hub Visuals**.
- Top banner: `YOU ARE IT` or `IT: Dummy`.
- Bottom: Least It timer + time-as-It scores.
- Top-left: live **m/s** (F3).

## Files

| Piece | Path |
|-------|------|
| Motor + tunables | `Assets/Scripts/Movement/` + `ScriptableObjects/MovementTuning.asset` |
| Punch / It | `Assets/Scripts/Tag/` |
| Least It | `Assets/Scripts/Modes/LeastItMode.cs` |
| Dummy AI | `Assets/Scripts/Modes/DummyPatrol.cs` |
| Dummy + It hat + locomotor | `Assets/Scripts/Art/` |
| URP pipeline | Created at `Assets/Settings/` by **Tag → Ensure URP Pipeline** |
| Input | `Assets/Scripts/Input/` + `Assets/Input/Tag.inputactions` |
| Arena | `CutArenaBootstrap` on Play |

## Out of slice

Netcode, Trail Tag light-cycle as the ship mode. Hub **Tag → Setup Hub Visuals** is optional (primitive dummy + locomotor work without it).
