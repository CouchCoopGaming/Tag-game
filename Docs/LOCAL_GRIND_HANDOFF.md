# Local grind handoff (Amaterasu) — until tomorrow ~6pm CT

Branch: `cursor/apex-party-movement-f5fd`  
Parent pushes; **do not push** from casual local grind unless asked.

## Done this pass (feel + figure-8 + map pass2/pass3 playground)

- Movement: no jet (`enableJet=false`), slide carries/decays (no enter boost), jump ~10x height (`jumpSpeed=24.7`), air-crouch 2x fall, faster run (`sprint=12`) vs ski max 24.
- Arms hang/swing + human knee run in `DummyLocomotor`; jet VFX not auto-added.
- Experimental grapple: `Assets/Scripts/Experimental/ExperimentalGrapple.cs` (off by default).
- **Air dash done:** ~0.1s planar momentum burst (`enableAirDash`, jet stays off). Q / Left Alt / MMB-in-air; 1 charge refresh on land.
- **Path layout:** `CutArenaBootstrap` figure-8 chase campus — west/east loops through Crash X, outer ring via Tron/Ninja, cardinal ski spines only, sparse `Flow_*` mid-height stones, thinner pads with open sightlines.
- **Map pass2:** `Flow_*` tops fair for jumpSpeed 24.7 / gravity 22 / WorldScale 10 (apex ~1.39 graybox); stones offset off spine axes + mid stones for chain gaps; pad toys nudged for center sightlines; landmarks thinned/nudged off pad centers.
- **Map pass3:** real playground structures along chase paths (`placePgkStructures=true` in `PgkLandmarkPlacer`). Soft-play plaza, outer-ring monkey/tunnels/slides, loop wall-runs, pad slide exits, spawn playsets (spinner/seesaw/hopscotch/swings), kickball field. Stronger outer-ring tint + `SpawnLead_*` lanes + edge kerbs. Landmarks edge-nudged; gear sits **beside** spines/Flow/Conn (not on midlines).
- **Wall-run + slide banks:** denser `LoopWallRun`, `WallRunStrip` on ring W/E, `SlideBank` at ring corners, ring S/N panels/vaults — Toy_WallPanel + vault rails + PGK deck/stairs/slides. Chase midlines kept clear.

### Path design notes (playground)
- **Outer ring:** Pirate -> Tron -> Army -> Knight -> Ninja -> Astro -> Pirate (ring tint + edge kerbs + PGK monkey/tunnel/slide).
- **Figure-8:** west NS spine (Pirate/Astro/Crash) + east NS spine (Army/Knight/Crash); EW spines at z=18/36; Crash is the X.
- **Height flow:** pad vault -> mid deck -> high perch -> slide/ramp exit to nearest spine; `Flow_*` stones + HiPoly slides/decks for run->jump->slide.
- **Pads:** 3-5 signature graybox toys (dressed via `ParkPropDresser`); playground fantasy gear from `PgkLandmarkPlacer` along lanes.
- Prefer sparse chase-path structures over dense pad dumps. Keep ski midlines clear.

### Pass3/4 structure map (graybox coords)
- `Play_SoftPlay_CrashSW` (28,21): tube continuity mid?Bay B + readable net climb line (Mega_ClimbNet/tunnels/spiral); Crash/spines kept clear
- `Play_MerryGoRound` (15.5,22): Mega hub + 4 Toy cardinal ring + stand-on diagonals, mulch apron, west benches only (east open for SpineXw)
- `Play_Swing` (60.5,33.5): twin bars + rail A-frame + monkey bay, clear mulch fall zone, east benches only (west open for SpineXe)
- `Play_Kickball` (56.5,27): clear diamond + N/S goals, east benches only (west open for figure-8), larger rubber pad
- `Play_Hopscotch_SW/SE` (9.5,9)/(64,8): classic tile chain with gaps, larger rubber apron, outer-flank bench
- `Play_Ring_S` (36,5.5): twin monkey, tunnel, Mega_SlideTube, straight slide, rails, spinner
- `Play_Ring_N` (36,49): twin monkey, crawl/tunnel, tube slide, balance beams, Mega_Spinner
- `Play_Loop_W/E` (19,27)/(53,27): denser wall-run (4x Toy_WallPanel + vault rails), decks, stairs, slide/tube exit
- `Play_Ring_W/E` (11,27)/(61,27): outer-ring wall-run strips + mid deck slide bank
- `Play_Bank_SW/SE/NW/NE` (20,6)/(52,6)/(20,48)/(52,48): slide banks (deck+stairs+slide+side wall)
- `Play_Ring_S/N`: added wall-run panels + vaults + corner deck/slide
- `Play_Slide_*` Pirate/Army/Astro/Knight pad exits toward spines
- `Play_Spawn_*`: light spring/seesaw/bumper lead toys (named courts above)
- `Play_SpineAccents`: balance beams, vaults, wall panels, dome, skybridge - off midlines
- Graybox `PlayPad_*` mulch/rubber floors under named courts (`CutArenaBootstrap.BuildNamedPlayPads`)

## Keep grinding (priority order)

### 1) Playground map layout (iterate in Play)
- **Primary:** `Assets/Scripts/Level/CutArenaBootstrap.cs` + `Assets/Scripts/Art/PgkLandmarkPlacer.cs`
- **Related:** `ParkPropDresser`, `ZoneNameMarkers` (`placePgkStructures=true`, sparse path gear)
- Workflow: edit placer clusters / `Build*`, enter Play (or ContextMenu Rebuild/Place), feel chase through playground gear. Tune offsets if a piece blocks a spine.

### 2) Mannequin polish (overnight OK)
- **Builder:** `Tools/Tag/build_mannequin_hier.py`
- Exports -> `Assets/Art/Characters/HiPoly/Dummy_Mannequin_*_Hier_Hi.fbx`
- Blender: `"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python Tools\Tag\build_mannequin_hier.py`
- TODO: further slick Navy Spade polymer (panel bevels, joint spheres, silhouette) without exploding FBX size (keep subsurf <=1, segs <=24). Validate DummyLocomotor bind + arm hang in Play.

### 3) Feel QA checklist in Play
1. Arms hang naturally (not V into butt) at idle/run  
2. Run shows knee bend  
3. RMB does **not** jet  
4. Slide: crouch+speed carries, no punch boost; downslope sustains  
5. Jump feels ~10x; air+crouch falls faster  
6. Run closer to ski; ski still wins on slopes  
7. Grapple only if enabled (see below)
8. Air dash: Q/Alt or MMB in air — short burst, not hover; 1/air until land
9. Chase the figure-8: ski west/east loops + outer ring; vault playground gear without dead ends
10. Spawn -> nearest path obvious in ~3 seconds (lead tint + spawn playset)

## How to enable experimental grapple
1. Add `Tag.Experimental.ExperimentalGrapple` to a player with `PlayerMotor` + `PlayerInputReader`.
2. Set `enableGrapple = true` (leave `useJetHeldAsFire=true` — RMB/JetHeld; safe while jet is off).
3. Hold fire to attach LineRenderer rope + pull; release cancels.
4. Core tag loop must work with component absent or `enableGrapple=false`.

## Re-enable jet (if ever needed)
- `Assets/Resources/TagArena/MovementConfig.asset` -> `enableJet: 1` (and/or code default).
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
- Feel in Play: wall-run faces + slide banks along figure-8 / outer ring; ski midlines still clear?
- Tune offsets if a wall/bank clips Flow stones or Conn.
- Mannequin polish overnight OK.

