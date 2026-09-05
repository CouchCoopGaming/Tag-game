# Align Tag SP demo to Unity 6000.4.2f1

**Required editor:** Unity **6000.4.2f1** (revision `7a4c1aeef971`).  
Do **not** reopen this branch in 6000.0.23f1 — that rewrites `ProjectVersion.txt` and leaves `packages-lock.json` on 17.4 while `manifest.json` falls back to URP 17.0.3 (ShaderGraph GUID CS0246).

## Pins (committed)

| Item | Version | Where |
|------|---------|--------|
| Editor | 6000.4.2f1 | `ProjectSettings/ProjectVersion.txt` |
| Input System | 1.19.0 (registry) | `Packages/manifest.json` + lock |
| URP | 17.4.0 (**builtin**) | manifest + lock |
| Shader Graph | 17.4.0 (**embedded** lean override) | `Packages/com.unity.shadergraph/` + lock |
| SRP Core / URP Config | 17.4.0 (builtin) | lock |

## ShaderGraph CS0246 (`UnityEngine.GUID`) — fixed in-repo

**Root cause (Unity 6000.4.2f1 builtin):** `GUID` moved from `UnityEditor` → `UnityEngine`, but builtin ShaderGraph 17.4.0 still uses bare `GUID` in files that lack `using UnityEngine;` (notably `BuiltInCanvasSubTarget.cs`, `TargetSetupContext.cs`). First `Csc` of `Unity.ShaderGraph.Editor.dll` logs **CS0246**; ApiUpdater then rewrites PackageCache and a later compile succeeds. Not a missing Canvas/UGUI package.

**Upstream:** Unity fixed the package API calls for **6000.4.8f1** / **6000.5.0a3+**. We stay on 6000.4.2f1 for SP demo.

**In-repo fix:** embed a **lean** ShaderGraph 17.4.0 under `Packages/com.unity.shadergraph/` (no `Samples~` / `Documentation~` / `Tests`) with those GUID sites already qualified as `UnityEngine.GUID` (same as Graphics Api Upgrader / staging). Overrides builtin on resolve so **first compile after Library wipe has 0× CS0246**.

Do **not** patch `Library/PackageCache` alone — wiped on reimport; the embed is the durable source.

## After pull (Library wipe)

1. Close the Unity Editor if it is open.
2. Delete the project `Library/` folder (and `Temp/` if present). Do not commit either; they are gitignored.
3. Open the project with **6000.4.2f1** only. Let Package Manager resolve (embedded ShaderGraph should show as local/custom).
4. Confirm console is clear of `CS0104` (trail `Object.Destroy`) and ShaderGraph `GUID` `CS0246`.
5. Boot → Mode Select → Play. Build settings must stay Boot `ec368bfefa9a45d78f3bb3e926b918a4` / Play `bd270ceafb804f7b9b708ab620cc2734`.

If Unity rewrites nested lock versions on first resolve, keep the **17.4.0 / 1.19.0** top-level pins and **embedded** `com.unity.shadergraph`. No Steam publish; SP demo only.
