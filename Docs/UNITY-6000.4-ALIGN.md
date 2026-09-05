# Align Tag SP demo to Unity 6000.4.2f1

**Required editor:** Unity **6000.4.2f1** (revision `7a4c1aeef971`).  
Do **not** reopen this branch in 6000.0.23f1 — that rewrites `ProjectVersion.txt` and leaves `packages-lock.json` on 17.4 while `manifest.json` falls back to URP 17.0.3 (ShaderGraph GUID CS0246).

## Pins (committed)

| Item | Version | Where |
|------|---------|--------|
| Editor | 6000.4.2f1 | `ProjectSettings/ProjectVersion.txt` |
| Input System | 1.19.0 (registry) | `Packages/manifest.json` + lock |
| URP | 17.4.0 (**builtin**) | manifest + lock |
| Shader Graph | 17.4.0 (**builtin**, via URP) | `Packages/packages-lock.json` |
| SRP Core / URP Config | 17.4.0 (builtin) | lock |

## After pull (Library wipe)

1. Close the Unity Editor if it is open.
2. Delete the project `Library/` folder (and `Temp/` if present). Do not commit either; they are gitignored.
3. Open the project with **6000.4.2f1** only. Let Package Manager resolve.
4. Confirm console is clear of `CS0104` (trail `Object.Destroy`) and ShaderGraph `GUID` `CS0246`.
5. Boot → Mode Select → Play. Build settings must stay Boot `ec368bfefa9a45d78f3bb3e926b918a4` / Play `bd270ceafb804f7b9b708ab620cc2734`.

If Unity rewrites nested lock versions on first resolve, keep the **17.4.0 / 1.19.0** top-level pins. No Steam publish; SP demo only.
