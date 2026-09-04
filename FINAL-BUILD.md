# Tag — SP Demo Final Build (Win64)

**Tip branch:** `release/sp-demo-final` · **Tag:** `v0.1.0-sp-demo`  
**No Steam store publish.** Hub play smoke + this build only.

## What this tip is

- Modes: Hot Potato / Least It / Trail Tag
- SP + Dummy AI (chase/punch when It)
- PARK playground + Style Bible mats
- Audio stubs wired via `AudioCuePlayer` / `Resources/Audio`
- Boot → Mode Select → Play → Esc pause → results
- EditorBuildSettings: real Boot/Play scene GUIDs

## Build (Landon machine)

1. Unity **6000.0.23f1** — open this repo (pull `release/sp-demo-final` or tag `v0.1.0-sp-demo`).
2. Wait for package resolve (slimmed manifest: URP + Input System + uGUI + core modules).
3. Menu **Tag → Setup Hub Visuals** once if dummies/props look graybox.
4. Menu **Tag → Build Windows Standalone**
   - Output: `Builds/Windows/Tag.exe`
   - Release flags: **no** Development / Profiler / Autoconnect; **LZ4HC** compression; IL2CPP + High managed strip on Standalone.
5. Smoke: Boot → Play SP → punch Dummy (both ways) → hear SFX/music → Esc pause → rematch.

Alternate CLI (optional):

```bash
# Set UNITY_PATH to the 6000.0.23f1 Editor binary
./Tools/build-windows.sh
```

## Do not delete (protected)

- Audio: `Assets/Resources/Audio/**`, `Assets/Audio/**`, `AudioCuePlayer`, `AudioResourcesCopy`
- Art: Dummy_Runner/It (+ mats), Trail_Ribbon + Mat_Trail_Cyan, `Props/Playground/**`, Mat_Park_*/Mat_Runner_*/Mat_It_*

## Cut in this release pass

- Editor/IDE/test/timeline/collab packages
- Unused modules (physics2d, vehicles, TMP)
- Graybox design docs under `Assets/Art/Graybox/` (runtime PARK stays)
