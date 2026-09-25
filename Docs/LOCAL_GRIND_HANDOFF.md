# Local grind handoff (Amaterasu) — until tomorrow ~6pm CT

Branch: `cursor/apex-party-movement-f5fd`  
Parent pushes; **do not push** from casual local grind unless asked.

## Campus density pause

No new props this pass. Wait for the next instruction before adding kit.

Seating audit (domes, ground slides, spiral climbers, beams, arches, benches): feet already meet the support plane. Dome stem −0.330 (minY 0.330). Ground-slide mouth stem −0.095 (minY 0.095). Spiral climber, balance beams, arches, benches, mushrooms, spring riders, and the net frame are within 0.5 mm of y=0 with no stem. Safety tiles stay at y=0.02 on purpose. Crawl and plastic caps sit about 4 mm high; snap sinks that. No y edit.

Clearance audit: the three arch piers still clear courts and bars by ≥1.3 m. The SE arch is the minimum, about 1.32 m off the army net and about 1.35 m off hopscotch SE. The NE hop/seesaw squeeze was a real gap, so those pieces moved. The other sub-0.5 m gaps are side clearances, not closed passages:

- SW bench to the climb net, 0.29 m. The bar passage stays east of the net.
- Swing fall tiles to the kickball fence, 0.16 m.
- SE spring to hopscotch SE, 0.35 m.
- Army spiral to the south bar, 0.40 m.
- West dome to the SoftS mushrooms, 0.43 m.
- Astro dome to the west lane, 0.45 m. The lane itself stays open.
- NW mushrooms to the north bar, about 0.22 m. The bar line itself stays clear.
- NE hops to the spawn seesaw: was 0.11 m (south hop x ended 68.25, seesaw started 68.36). Hops are now local x=1.75 (east edge 68.45). The south hop is local z=-0.75 so it clears the mushroom cap by 0.16 m and the net's southeast post by 0.10 m. The seesaw is local (-1.960, 0.540), west edge x=69.01, 0.56 m east of the hops. Spawn_NE's southwest exit and the NE arch span were not moved.

## Mega tube deck-height grid (blocked)

`PGK_Slide_TubeDeck_2m` is placed on the west play-place deck lips (soft-play and astro). Yaw 90, stem 0, pivot local (0, 0, 5.90). High-mouth center stays at mesh y=1.91, 0.10 inside the 2.00 lip. East forts, rings, and loops keep the straight chute. `Mega_SlideTube` and `PGK_Slide_Tube90` stay unspawned and unscaled. Measured in Unity space (node transform applied). Deck pivots in use are 0.40 / 0.80 / 1.60 / 2.00. A `PGK_Deck_2x2` is 0.08 thick above its pivot, so the tallest walk surface is 2.08. The tallest rise from mulch onto a deck pivot is 2.00. No yaw seats either tube on that grid without a float or a bury. No new deck was added.

`PGK_Slide_Tube90` is one mesh, 0.590 m deep in Z. Bounds: X −1.725..1.725, Y −0.525..2.925, Z −0.295..0.295. Vertical span is 3.450 m. Stem +0.52 puts the feet at Y −0.005 and the crown at 3.445. Feet on mulch leave the crown 1.445 m above the 2.00 deck pivot (1.365 m above the slab top). Crown on the 2.00 pivot buries the feet at −1.445. The grid is 1.445 m short.

`Mega_SlideTube` is a bed (`Cube`) plus a shell (`Cylinder`), and the shell is not a sleeve on the bed. At the exit (z ≈ 4.8) the bed is Y 0.693..0.898 and the shell is Y 3.105..3.812.

- Bed: Y 0.693..4.605 (span 3.912), Z −2.160..4.977 (run 7.137), X ±1.100. Lower envelope runs from 4.279 at the high end to 0.693 at the exit, a drop of 3.586 m.
- Shell: Y −0.234..5.234 (span 5.468), Z −5.067..5.067 (length 10.134), X ±1.097. The shell bottom is 0.927 m below the bed exit.
- Stem +0.24 grounds the shell (minY ≈ 0.006) and leaves the bed exit at 0.927 (floating) and the bed crown at 4.839 (2.839 m above the 2.00 pivot).
- Bed exit on mulch (shift −0.693): shell buried 0.927 m, crown at 3.912 (1.912 m above the 2.00 pivot).
- Bed crown on the 2.00 pivot (shift −2.605): bed exit at −1.912, shell bottom at −2.839.

## KIT_REQUEST

No floater, buried foot, or closed walk under 0.5 m remained, so nothing was moved. The west `Toy_Slide` still reaches x=20.54, 0.04 m into the 7 m lane tint (lane starts x=20.5). The spine starts at x=22.4, so that nick does not close the chase.

`PGK_Slide_TubeDeck_2m` is that mesh and is bound on the west deck lips. Do not place or scale `Mega_SlideTube` or `PGK_Slide_Tube90`.

- Mouth-to-crown ≤ 2.00 m. Low mouth on mulch (y=0). High mouth on the 2.00 deck pivot. A tuck of about 0.09 m under the lip matches the straight slide (mouth at 1.91, deck slab 2.00–2.08).
- One sleeve. The shell follows the bed for the whole run. At the exit, Mega's shell is Y 3.105–3.812 while the bed is Y 0.693–0.898, so those two parts cannot be seated together.
- Bed and shell share one pivot. Grounding the low mouth must not bury the shell or float the bed. A single stem offset is enough.
- Vertical span of Tube90 is 3.450 m and Mega's bed span is 3.912 m. Both are past the 2.00 cap. A new mesh has to come in under that cap. Scaling either asset in the placer is not the fix.

## Campus zone pass (place)

Graybox pad cubes are gone (they were stretching HiPoly into a low-poly look). Flow stones are gone. The chase is kit districts:

- **Soft-play** (14, 9.75) and **astro loft** (14, 44.25): decks 0.80 / 1.60 / 2.00, spiral at local (2.50, 0, −0.5) yaw 180, deck chute `PGK_Slide_TubeDeck_2m` yaw 90 stem 0 at local z=5.90 (east forts, rings, and loops keep the straight chute), tube street at z=−4 with the north mouth at z=−3. Rung ladder at local (−1.15, 0, 0.90) yaw 90 (~0.12 m off the deck; closer hits the post). Climb net at local (−4.15, 0, −0.78) yaw 90, about 0.25 m off the tube street. Low beam at local (−2.52, 0, −1.19) yaw 0 steps off that net onto the SW corner (that pad tile is omitted): 0.13 m south of the deck, 0.08 m south of the post. Slide pit is still three tiles long (~2.2 m after the chute). Mid and far tiles have an outer wing plus a second outer column. A third column meets the west bars. The inner wing would sit on the Conn ramp. A `Toy_Slide` at local (5.90, 0, -5.53) is west-only: stem -0.095 seats the low mouth, high end stays ~2.41, 0.66 m east of the tube cap and 0.79 m south of the street. East forts do not get it. Soft-play world z about 1.0-4.2. Astro yaw 180 puts the same piece north of its tubes, 0.36 m east of Spawn_NW.
- **Army bunker** (58, 9.75) and **knight keep** (58, 44.25): same tower and pit, crawl at local (−2, −4.5), climb net, a 1.8 m `Toy_Ladder` at local (1.20, 0, 0.45) yaw −90 on the net side (reaches the 1.60 deck), and the same 2.4 m rung as soft-play at local (−1.15, 0, 0.90) yaw 90 (reaches the 2.00 deck). Side stair ends at local z=0.45; the rung starts at z=0.62. A `Toy_SpiralClimber` at local (2.80, 0, 0) is east-only: feet y=0, top 2.40, about 0.90 m off the deck face. Army world x 59.9–61.8, z 8.8–10.7 (0.40 m south of the south bar). Knight yaw 180 world x 54.2–56.1, z 43.3–45.2 (1.7 m north of Conn_Knight). West forts do not get this climber.
- **BARS W** x=11 and **BARS E** x=62.5. West stays at 11 (mast). Merry apron covers the middle spans. East bar mesh is fully on the kickball rubber (pad starts x=62).
- **Beam lanes:** west x=13.5, z centers 22.25 / 25.25 / 28.25 (ends z=29.75, tower deck starts z=30). East x=60.5, z centers 25.75 / 28.75 / 31.75 (starts z=24.25, tower deck ends z=24).
- **Hopscotch** SW (4.5, 9) yaw 90; SE (70, 12); NE (70, 38); NW (3.2, 42) yaw 0, south of Spawn_NW and west of astro. Mushroom steps at (4.5, 6.94) sit between Spawn_SW and the SW court, beside the faced exit. Merry picnic is local (−2.2, −2.6), feet on y=0.
- **West climber dome** local (−4.66, −6.70) on soft-play and astro only. `Toy_ClimberDome` stem −0.330, top ~1.31. Soft-play world x 7.95–10.73, z 1.66–4.44 (0.59 m south of the tubes, 0.43 m west of the apron mushrooms). Astro yaw 180: x 17.27–20.05, z 49.56–52.34, 0.45 m west of the west lane. East bunkers do not get it.
- **Soft-play south apron** (14.5, 2.6): balance beam along X, mushroom steps at local (-2.9, 0.6) yaw 90, spring rider at local (2.7, 0.7), two hop tiles at local (-0.6, -1.35)/(0.6, -1.35). Tube mesh ends z=5.03; mushrooms end z=4.45. Spring ends x=17.70, 1.56 m west of the ground slide (x=19.26). Not copied onto astro. Not a bench.
- **NW pocket** (9.4, 45.0): mushroom steps, spring rider at local (-0.6, 1.1), two hop tiles at local (0.5, -1.0)/(0.5, 0.1). North of the NW arch (z=42), west of the bar face (x=10.96). Not a bench.
- **SE pocket** (66.2, 7.5): mushroom steps, spring rider at local (1.7, -0.8), two hop tiles at local (-1.2, -1.0)/(-0.2, -1.0). South of the SE arch (z=10.5), west of hopscotch SE (x=68.75), east of the army net (x=63.08). Not a bench.
- **Merry north pocket** (7.2, 31.2): mushroom steps yaw 90, spring rider at local (1.6, 0.9), two safety tiles. Lawn between the merry pad (ends z=28) and the north spine (starts z=34.4), west of the bar face (x=10.96). East tile ends x=10.4. Not a bench.
- **NE pocket** (66.2, 45.0): net frame yaw 90, mushroom steps at local (0.3, -1.85), spring rider at local (-1.45, 0). Two hop tiles at local (1.75, -0.75) and (1.75, 0.55); east edge x=68.45, 0.56 m west of the spawn seesaw (west edge x=69.01). West edge x=67.45, 0.10 m off the net's southeast post. South tile starts z=43.75, 0.16 m north of the mushroom cap. Lawn north of the NE bench (ends z=42.13) and east of the bars (face x=62.54), south of the knight crawl (z=47.35). Spring x starts 64.40. Not a bench. Spawn_NE's southwest exit stays north of it. The NE arch span is unchanged.
- **Swings** (67, 31.2): south tiles clear the kickball north fence by ~0.16 m. The swing pad stops at z=34.2, short of the north spine. Kickball's west side stays open. A bench at (61.30, 21.05) yaw 90 is west of the bars (face x=62.46), south of the Loop E ground stair (z ends 22.55), and north of SpineZs. It is not in the diamond.
- **Arches** (`Toy_Bridge`, feet y=0, deck ~1.05, open span under the middle): NW (7.70, 42) between hopscotch NW and the north bar; NE (65.65, 41) between the east bars and hopscotch NE; SE (65.90, 10.5) between the army net and hopscotch SE. SW has no arch (the court and the south bar are ~2.2 m apart; an arch is 3 m, so piers cannot clear by 1.3 m). The SW bench is now (9.26, 9) yaw 90, west of the climb net (the old (9.85, 9) sat on that net). Soft-play arrival bench (8.20, 5.75) yaw 0 is 0.50 m west of the tube cap and south of the net. NE bench (65.65, 41.90) yaw 0 is north of the arch, 2.4 m off the bars and hopscotch NE. Merry keeps its west bench (4, 24) and south picnic (4.8, 21.4). Soft-play east shoulder is a spring rider at (19.20, 10.40), feet on y=0, 0.78 m east of the spiral and 0.80 m west of the west lane. The north pit still exits to the spine (pit z=16.0, spine z=16.4). No alley was closed. East-to-crash lane is z 24-30; the kickball bench ends z=21.75 (2.25 m clear) and was not moved. Swing tiles still clear the north fence by 0.16 m; that gap got no cue. Swing bench is (71, 31.2).
- **Landmarks:** helmet (2.5, 51.2) scale 0.30 (max yaw-0 fit; ~4.1 graybox tall); foxhole (69.55, 2.55) scale 0.35 yaw 0; shield (66, 52.2); tron disc (42.1, 11.5) off Conn_Tron; crash torso (12, 52.12) scale 0.28 yaw 0, stem −0.322, NW lawn 1.0 m north of Spawn_NW so the crash lips stay clear (the mesh is too deep for the lane-to-spine gaps); ninja rail (36, 48.70) yaw 90 scale 0.725, stem −0.334, height ~1.0 (10:1 mesh cannot be a short rail). Beam stays local (−2.52, −1.19): closer to the deck hits the corner post.
- **Ring S** (36, 3): monkeys, wall, open 8 m crawl, west tower slide. **Ring N** (36, 51): tube chain with plastic collars, slide south. Ring pits grow a second column on local +X only (the west column meets SpineXw).
- **Loop W** (16, 25) and **Loop E** (56, 29): wall-run plus end tower, same 3-tile pit with ±1 wings. A second ±Z wing clips the vault rail.

Playtest, in order: spawn SW → mushroom step → hopscotch SW → bench west of the climb net (9.26, 9) → south bar → soft-play bench (8.20, 5.75) → SoftS south apron (14.5, 2.6: beam, mushrooms, spring, hops) → west climber dome → tubes and ground slide → spring rider (19.20, 10.40) → cross the south spine → bars or beams → merry → merry-north (7.2, 31.2) → cross the north spine → north bar → arch (7.70, 42) → NW cluster (9.4, 45.0) → hopscotch NW → astro (same dome, north of the tubes). East: hopscotch SE → arch (65.90, 10.5) → SE cluster (66.2, 7.5) → army spiral → bars → kickball (field still open) → swings → knight spiral, or arch (65.65, 41) and bench (65.65, 41.90) → NE cluster (66.2, 45.0) → hopscotch NE. Crash cross stays open. Density pause: no new props. Short smoke: `Docs/CAMPUS_SMOKE.md`. Parked until a deck-height tube lands or a human notes a blocker.

`PGK_Slide_TubeDeck_2m` is the west deck tube (soft-play and astro, yaw 90, stem 0). `Mega_SlideTube` / `PGK_Slide_Tube90` stay unused. Tron disc is scale 0.5 and shifted east of its Conn ramp.

Feel (read, not played here): slide clamps to entry speed (downhill only softens friction); jump `v.y` is `jumpSpeed` / fatigue; air dash cooldown is 30 s with the dummy trail tell.

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
| Toy_ClimberDome | **+0.33** | yes (west SoftPlay annex) | -0.330 | soft-play + astro only; east bunkers skip |
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
- **DresserSeat applied:** `Toy_Ramp` -0.17 / `Toy_Slide*` -0.09 / `Toy_TireStack` -0.06 (hook). Feel FoxholeTrench ramp feet in Play after Dress. `Toy_ClimberDome` is placed on the west forts with stem −0.330 (was the unused +0.33 float).
- Mannequin polish overnight OK.

