# Local grind handoff (Amaterasu) — until tomorrow ~6pm CT

Branch: `cursor/apex-party-movement-f5fd`  
Parent pushes; **do not push** from casual local grind unless asked.

## Done this pass (feel + layout start)

- Movement: no jet (`enableJet=false`), slide carries/decays (no enter boost), jump ~10× height (`jumpSpeed=24.7`), air-crouch 2× fall, faster run (`sprint=12`) vs ski max 24.
- Arms hang/swing + human knee run in `DummyLocomotor`; jet VFX not auto-added.
- Experimental grapple: `Assets/Scripts/Experimental/ExperimentalGrapple.cs` (off by default).
- **Air dash done:** ~0.1s planar momentum burst (`enableAirDash`, jet stays off). Q / Left Alt / MMB-in-air; 1 charge refresh on land.
- Map readability start: `CutArenaBootstrap` clear lanes + cardinal spines only; PGK structure dump **off** by default in `PgkLandmarkPlacer`.

## Keep grinding (priority order)

### 1) Playground map layout (highest)
- **Primary script:** `Assets/Scripts/Level/CutArenaBootstrap.cs`
- **Related:** `Assets/Scripts/Art/PgkLandmarkPlacer.cs` (leave `placePgkStructures=false` unless intentionally testing), `Assets/Scripts/Art/ParkPropDresser.cs`, `Assets/Scripts/Level/ZoneNameMarkers.cs`
- Goals: readable zones, clear chase/ski lanes, less stacked geometry, good parkour silhouette per pad (Pirate / Army / Astro / Knight / Tron / Ninja / Crash).
- Workflow: edit graybox in `CutArenaBootstrap.Build*`, enter Play (or ContextMenu Rebuild), iterate. Prefer code layout over dumping random props.
- Optional: if PGK needed, re-enable `placePgkStructures` and keep connector list sparse.

### 2) Mannequin polish (overnight OK)
- **Builder:** `Tools/Tag/build_mannequin_hier.py`
- Exports → `Assets/Art/Characters/HiPoly/Dummy_Mannequin_*_Hier_Hi.fbx`
- Blender: `"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python Tools\Tag\build_mannequin_hier.py`
- TODO: further slick Navy Spade polymer (panel bevels, joint spheres, silhouette) without exploding FBX size (keep subsurf ≤1, segs ≤24). Validate DummyLocomotor bind + arm hang in Play.

### 3) Feel QA checklist in Play
1. Arms hang naturally (not V into butt) at idle/run  
2. Run shows knee bend  
3. RMB does **not** jet  
4. Slide: crouch+speed carries, no punch boost; downslope sustains  
5. Jump feels ~10×; air+crouch falls faster  
6. Run closer to ski; ski still wins on slopes  
7. Grapple only if enabled (see below)
8. Air dash: Q/Alt or MMB in air — short burst, not hover; 1/air until land

## How to enable experimental grapple
1. Add `Tag.Experimental.ExperimentalGrapple` to a player with `PlayerMotor` + `PlayerInputReader`.
2. Set `enableGrapple = true` (leave `useJetHeldAsFire=true` — RMB/JetHeld; safe while jet is off).
3. Hold fire to attach LineRenderer rope + pull; release cancels.
4. Core tag loop must work with component absent or `enableGrapple=false`.

## Re-enable jet (if ever needed)
- `Assets/Resources/TagArena/MovementConfig.asset` → `enableJet: 1` (and/or code default).
- Optionally add `JetThrustVisual` manually (binder no longer auto-adds).

## Compile
```
dotnet build Assembly-CSharp.csproj
```
Prefer this while Unity is open. Do not commit `_Staging/`, `*.slnx`, `_compile_*.log`, or unrelated prefab/scene dirt.

## Commit hygiene
Author: Landon Sikes / CouchCoopGaming@users.noreply.github.com (env author, no `git config`).  
Do not push; parent agent pushes.

## Next coding pass
- Playground map layout (highest) — see checklist above.
- Mannequin polish overnight OK.

