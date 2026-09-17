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
- `Play_SoftPlay_CrashSW` (19,13.5): compact tower + tube continuity (Toy_TunnelTube / Tube90 / Mega_SlideTube on Stairs5DeckY via StemSeatYOffset) + crawl under; Crash/spines kept clear
- `Play_MerryGoRound` (15.5,22): Mega hub + mulch tiles + west bench (east open for SpineXw)
- `Play_Swing` (60.5,33.5): twin bars + rail A-frame + monkey bay, clear mulch fall zone, east benches only (west open for SpineXe)
- `Play_Kickball` (56.5,27): clear diamond + N/S goals, east benches only (west open for figure-8), larger rubber pad
- `Play_Hopscotch_SW/SE` (9.5,9)/(64,8): classic tile chain with gaps, larger rubber apron, outer-flank bench
- `Play_Ring_S` (36,5.5): twin monkey, tunnel, Mega_SlideTube, straight slide, rails, spinner
- `Play_Ring_N` (36,49): twin monkey, crawl/tunnel, tube slide, balance beams, Mega_Spinner
- `Play_Loop_W/E` (19,27)/(53,27): abutted wall-run (4x Toy_WallPanel yaw90 + vault rails), decks, stairs, slide exit
- `Play_Bank_SW/NE` (20,6)/(52,48): slide banks (deck+stairs+slide+side wall); Ring_W/E dropped
- `Play_Ring_S/N`: added wall-run panels + vaults + corner deck/slide
- `Play_Slide_*` Pirate/Army/Astro/Knight pad exits toward spines
- `Play_Spawn_*`: light spring/seesaw/bumper lead toys (named courts above)
- `Play_SpineAccents`: balance beams, vaults, wall panels, dome, skybridge - off midlines
- Graybox `PlayPad_*` mulch/rubber floors under named courts (`CutArenaBootstrap.BuildNamedPlayPads`)

## Playtest fix pass (Sep 16 evening CT)

- **Slide:** carry `_slideStartSpeed` only; friction degrade (softer downhill); hard clamp — never above entry. `slideDownhillAccel=0`.
- **Air dash:** still ~0.1s planar burst; **30s cooldown** (`airDashCooldown`); DummyLocomotor whip + brief cyan trail/squash; `OnAirDashed`.
- **Jump:** `v.y = jumpSpeed` (fixed launch, not additive with ski/slope residual).
- **Map:** `SpawnFbx` feet-snap to local Y; succinct PGK clusters (dropped SE hopscotch + 2 banks + Bay B clutter); prefer HiPoly on mulch.
- **Map grounding:** snap is ground-only + sink-biased (no lift on overhanging slides); dropped Ring_W/E + merry cardinal spinners; Flow thinned to 4 Conn->spine handoffs (no S/N ring / Core / mids).
- **Stem seating:** `SpawnFbx` name rules - `PGK_Slide_Straight*` mouth -1.77, `PGK_Slide_Spiral*` feet -0.51, `PGK_Stairs*` -0.06, `PGK_Slide_Tube*` feet +0.52, `Mega_SlideTube` +0.24, `Mega_ParkourRamp` +0.26 (Loop), `Toy_TunnelTube` +0.02 (Mega_CrawlTunnel ~0); `Toy_Bars` -0.23 (Rail 0); `Toy_Seesaw` -0.10 / `Toy_Bumper` -0.17 (SpawnLead); `Toy_Goal` +0.05 (Kickball); `Toy_WallPanel` / `Toy_VaultRail*` / SpringRider / Tower / Picnic / Bench / Spinner / tiles / monkey / NetFrame HiPoly feet ~0; snap still uses authored Y (elevated decks/mouths skip snap; sink-biased on ground).
- **Stairs/deck match:** soft-play + pad clusters use Stairs5DeckY=0.8 (Stairs_5 top ~0.84; kit snap). Decks/slide mouths/rails share that Y; StemSeatYOffset unchanged.
- **SoftPlay tube restore:** re-added TunnelTube->Tube90->Mega_SlideTube SW of core deck (author Y=Stairs5DeckY); no Bay B tower clutter; Crash EW clear.
- **VaultRail StemSeat check (no-op):** Blender AABB on `Toy_VaultRail_{090,100,105}_Hi` min height axis = 0.000 — feet already on pivot; no StemSeatYOffset. Spot-check vs wall strips: Ring_S/N vault z±2.8 vs panels ±1.85 (~0.25m face gap after yaw90); Loop/Bank/WallRunStrip vaults on +X opposite panels on -X (clear); SpineAccents vault beside panel intentional adjacency.
- **Swing/merry/hopscotch/kickball + SpawnLead StemSeat pass:** Blender AABB (FBX height axis = Z in source). `Play_MerryGoRound` Mega_Spinner feet~0; `Play_Hopscotch_SW` Safety_Tile feet~0 (author Y=0.02); `Play_Swing` `Toy_Bars` feet~+0.23 -> StemSeat -0.23 (Rail 0). SpawnLead `Toy_Seesaw` feet~+0.10 / `Toy_Bumper` feet~+0.17 -> StemSeat -0.10 / -0.17. `Play_Kickball` `Toy_Goal` feet~-0.05 -> StemSeat +0.05 (Kickball-only stem; snap still refuse larger lifts). SpringRider feet~0. Snap sink-biases residual floaters.



- **StemSeat float audit (towers/picnic/benches/spinner/tiles/ramps):** Blender AABB height-axis min (FBX Z -> Unity Y). Clear float = min clearly > ~0.05. StemSeat only when float/sink clear and `SpawnFbx` stem path.

| Stem / asset | heightMinZ | Placer stem? | StemSeat | Notes |
|---|---:|:---:|---|---|
| Toy_Tower / Ultra | 0.00 | SoftPlay posts/decks | 0 (n/a) | Dresser TwinTower/Tower ~0 feet |
| Toy_PicnicTable | 0.00 | no | 0 (n/a) | Dresser only; grounded |
| Toy_Bench | 0.00 | yes | 0 | merry/swing/kickball/hopscotch |
| Toy_SpringRider | 0.00 | yes | 0 | SpawnLead |
| Mega_Spinner | 0.00 | yes | 0 | MerryGoRound |
| PGK_Safety_Tile | 0.00 | yes | 0 | author Y=0.02 apron |
| Toy_RubberTrack_C3 | 0.02 | yes | 0 | Kickball; not clear float |
| PGK_Monkey_4m | 0.00 | yes | 0 | Ring/Swing |
| Mega_ClimbNet | 0.00 | yes | 0 | SoftPlay |
| Mega_CrawlTunnel | 0.00 | yes | 0 | SoftPlay/Ring_N |
| **Mega_ParkourRamp** | **-0.26** | **yes (Loop)** | **+0.26** | sink like SlideTube; this pass |
| Mega_TowerFort / SkyBridge | 0.00 | no | - | dresser/landmark only |
| Toy_NetFrame | 0.00 | yes | 0 | Spawn Astro |
| PGK_Dome / BalanceBeam / Posts / Decks / Rail | ~0 | yes | 0 | decks elevated author Y intentional |
| Toy_WallPanel / VaultRail | ~0 | yes | 0 | prior pass |
| Toy_ClimberDome | **+0.33** | no | - | unused by placer; open if dressed later |
| Toy_Ramp | **+0.17** | no | **DresserSeat -0.17** | dresser FoxholeTrench; world Y after FitToParent |
| Toy_Slide_Hi | +0.09 | no | **DresserSeat -0.09** | dresser Toy_Slide_C1 -> Toy_Slide; PGK mouths still StemSeat |
| Toy_TireStack | +0.06 | no | **DresserSeat -0.06** | unused today; hook ready if dressed |
| Conn_* ramps | n/a | graybox SkiRamp | - | CutArenaBootstrap boxes, not HiPoly |

**Result:** no remaining clear *float* on `PgkLandmarkPlacer` stems. Applied StemSeat `Mega_ParkourRamp` +0.26 (Loop wall-run sink). Tower/Picnic/Bench/Spinner/tiles/monkey/net/decks already ~0.

- **Dresser seating (ParkPropDresser):** `DresserSeatYOffset` mirrors StemSeat for dressed PropMesh only — `Toy_Ramp` -0.17 (FoxholeTrench), `Toy_Slide*` -0.09 (`Toy_Slide_C1` -> HiPoly Slide), `Toy_TireStack` -0.06 (future). Applied as **world Y after FitToParent** (ChildBox hosts are scaled cubes; local nudge would shrink with host Y scale). Hedges/pads/towers/etc. still `localPosition` zero. Colliders rebuild after seat via `EnsureStaticColliders`.
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
- Feel SoftPlay tube run in Play: pieces sit on deck/mulch (not floating); tube path readable SW; Crash bowl open.
- Tune tube yaw/XZ if mouths misalign; stem Y offsets already at tip.
- **Wall-run strips:** feel continuous run face in Play (Loop/WallRunStrip yaw 90; Ring_S 0 / Ring_N 180; Bank 90). Feel `Mega_ParkourRamp` StemSeat +0.26 on Loop (feet on mulch).
- **SpawnLead Seesaw/Bumper + Kickball Goal:** StemSeat -0.10 / -0.17 / +0.05 applied; feel feet on mulch in Play after Place.
- **DresserSeat applied:** `Toy_Ramp` -0.17 / `Toy_Slide*` -0.09 / `Toy_TireStack` -0.06 (hook). Feel FoxholeTrench ramp feet in Play after Dress. Still open: unused `Toy_ClimberDome` (+0.33) if ever dressed.
- Mannequin polish overnight OK.

