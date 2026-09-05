# Tag — Steam demo checklist (SP MVP)

**Scope:** polished single-player demo (Player vs AI dummies). Couch 2–4p deferred. **Do not publish to Steam store** until Landon says go.

**Remote tip baseline:** `ed9c3ff` + EditorBuildSettings GUID fix (Boot `ec368…` / Play `bd270…`).

## Ship bar (must)

- [ ] Hub Play Mode smoke: Boot → Mode Select → Play (Hot Potato / Least It / Trail Tag)
- [ ] Dummy AI: chase + punch when It; flee when not
- [ ] Softlocks: NextPunch 20s, Trail clearance 1.05 / height 1.0, HP null-It void, trail stall 8s, self-grace AND
- [ ] Pause / rematch / mode return (Esc / R / M)
- [ ] Audio hear-pass (punch, transfer, slide, dodge, round, music bed)
- [ ] Win64 standalone: Editor **Tag → Build Windows Standalone** or `Tools/build-windows.sh` with `UNITY_PATH`

## Nice-to-have

- [ ] First-run tutorial / control hints on Mode Select
- [ ] Art polish pass on PARK props / dummy materials
- [ ] Steam page draft (caps, screenshots) — store publish still blocked

## Build notes

- Unity **6000.4.2f1** (see `Docs/UNITY-6000.4-ALIGN.md`; delete `Library/` after pull)
- Scenes in build: `Assets/Scenes/Boot.unity`, `Assets/Scenes/Play.unity`
- After GUID fix, `ProjectSettings/EditorBuildSettings.asset` must match scene `.meta` guids