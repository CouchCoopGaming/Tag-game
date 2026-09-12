# Tag — playable party slice

Crash-test dummies playing punch-tag in a giant playground. **This slice ships Least It** (classic Tag: It punches to transfer It; least time-as-It wins). Hot Potato and Trail Tag stay in the project but are out of scope here.

Movement numbers: [`Docs/MOVEMENT.md`](MOVEMENT.md).

## Safe Mode / compile (PR #8)

If Unity 6 opens this worktree in **Safe Mode**, pull `cursor/apex-party-movement-f5fd` again. Fixes: isolated test asmdef removed (tests compile in Assembly-CSharp-Editor), `TagInputActions.AddAction` no longer uses `expectedControlType`, trail `Object.Destroy` is `UnityEngine.Object`. Then **Ignore** Safe Mode / reimport scripts. Smoke: **Tag → Run Movement Kinematics Smoke**.

## Amaterasu / reopen (do this after every pull if the Editor crashed)

Hand-authored URP YAML and abbreviated FBX `.meta` files crashed Unity 6000.3 `MetaFileHandling` on import. Those stubs are gone. Pipeline assets are created by the Editor, not checked in.

1. `git pull` on `cursor/apex-party-movement-f5fd`
2. Quit Unity
3. Delete the worktree **`Library`** folder (import cache from a crashed open is poison)
4. Hub → Open this folder (6000.0.x or 6000.3.x)
5. Wait for import. After scripts compile, **Tag → Ensure URP Pipeline** runs (also in the menu). That writes `Assets/Settings/TagURPAsset.asset` via Unity APIs and assigns Graphics/Quality.
6. If materials are still magenta: run the menu again, then Play. Do not switch mats to Standard.

## URP (fixes magenta / pink)

Materials under `Assets/Art` already use **URP Lit**. Pink was Built-in RP with no pipeline assigned.

Do **not** commit hand-written `TagURP*.asset` YAML. On open:

| Step | Who |
|------|-----|
| Create renderer + pipeline | **Tag → Ensure URP Pipeline** (`TagUrpSetup`, also `InitializeOnLoad`) |
| Assign Graphics + Quality | same menu |
| Play Mode if assets missing | `TagUrpBootstrap` builds an in-memory URP pipeline |

## Dummy meshes + motion

`Dummy_Runner.prefab` / `Dummy_It.prefab` stay as Hub stubs. Play uses `DummyLocomotor` on FBX bones (`UpperArm_L/R`, `UpperLeg_L/R`, …) after **Tag → Setup Hub Visuals**, or a primitive dummy with the same bone names. PARK toys dress from Resources prefabs after that menu (FBX is **not** duplicated under Resources).

There are **no `.anim` / `.controller` files** in the drop.

Optional Hub bind:

1. **Tag → Ensure URP Pipeline**
2. **Tag → Setup Dummy Prefabs From FBX** or **Tag → Setup Hub Visuals (Dummies + Props + Play Bind)**

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
- Visible idle sway, run cycle, jump tuck, slide crouch, and a right-arm punch tell.
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
