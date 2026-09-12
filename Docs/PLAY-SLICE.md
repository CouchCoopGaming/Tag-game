# Tag — playable party slice

Crash-test dummies playing punch-tag in a giant playground. **This slice ships Least It** (classic Tag: It punches to transfer It; least time-as-It wins). Hot Potato and Trail Tag stay in the project but are out of scope here.

Movement numbers: [`Docs/MOVEMENT.md`](MOVEMENT.md).

## Safe Mode / compile (PR #8)

If Unity 6 opens this worktree in **Safe Mode**, pull `cursor/apex-party-movement-f5fd` again. Fixes: isolated test asmdef removed (tests compile in Assembly-CSharp-Editor), `TagInputActions.AddAction` no longer uses `expectedControlType`, trail `Object.Destroy` is `UnityEngine.Object`. Then **Ignore** Safe Mode / reimport scripts. Smoke: **Tag → Run Movement Kinematics Smoke**.

## Open in Unity

1. Hub → Open this repo (Unity **6000.0.23f1**).
2. Either:
   - Open **Boot**, Play → **Play Tag (Least It)** (Enter / Space), or
   - Open **Play** directly (skips Boot; starts Least It vs Dummy).
3. First-time art (optional): **Tag → Setup Hub Visuals**. Not required — empty dummy prefabs fall back to a primitive dummy + orange It hat.

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

- Third-person dummy (not FPS). You can see your body, slide, and dash.
- **It** = orange hat + pulsing floor ring + point light. Punch dumps It; the other dummy swaps the hat.
- PARK graybox (20° ramps stick + keep sprint path-speed, vaults, slide strips). Props dress if Hub Visuals ran.
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
| Dummy + It hat | `Assets/Scripts/Art/` |
| Input | `Assets/Scripts/Input/` + `Assets/Input/Tag.inputactions` |
| Arena | `CutArenaBootstrap` on Play |

## Out of slice

Netcode, Trail Tag light-cycle as the ship mode, Metropolis, polished FBX bind (optional Editor menu).
