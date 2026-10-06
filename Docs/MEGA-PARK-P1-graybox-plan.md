# MEGA PARK P1 — graybox plan

**Playable pass (2026-10-06):** On `cursor/mega-park-play-arena-d899`, Play loads this yard. The loop vertices, 160×100 footprint, spawns, bowl floor Y=−1, and 472 m centerline are the lock from `5ba0cfc` / host `47980c8a`. Zone shells (soft-play decks and tube, cling walls, merry, slide towers to +5 m, swings, forts, bars, hopscotch) are graybox cubes on that lock. Blue is cling only. Yellow is the three chutes only. Motor numbers are not retuned.

**Status:** Branch `level/mega-park-p1` only. **NOT landed.** No pull request. Producer still gates land.
**Base:** Campus PR #9 tip `0a42007c9a54e7c8e3b53e4ba632454b0d0e253f` (GitHub CouchCoopGaming/Tag-game).
**Campus:** That tip stays the live map. This branch does not delete campus scenes, does not rewrite campus bootstrap, and does not merge.
**Date:** 2026-09-30 · P1 only · No playtest · No Amaterasu · No HeroCity
**Supersedes:** `PARK-densify-brief-v0.md` as the long-term place fantasy (36×28). Do not revive it. Do not densify the campus.

**Scene:** Not in this branch. A `.unity` scene would need the editor. Eng wires an empty host (see below). Do not add `MegaParkP1Bootstrap` to the campus scene.

Blueprint meters below are locks. Anything marked **graybox** is a stand-in so P1 can be built, not a new lock.

---

## Footprint

| | |
|--|--|
| Playable | **160 m E–W × 100 m N–S**. Origin SW corner = (0, 0). +X east, +Z north, Y up. |
| Envelope Y | −1.0 bowl floor → 6.0 slide-tower deck. The tower is **not** built in P1. |
| OOB | 3 m grass collar outside the fence (`#3F7A4A`). |
| Modes | Hot Potato, Least It, Trail Tag. Same yard. |
| Root scale | 1. Real meters. Not the campus `WorldScale`. |

Built max height in P1 is the graybox perimeter fence (2.4 m, not a lock) and the kickball east rail (0.90 m, locked). Nothing in P1 reaches the +6.0 deck.

---

## Spawn table

Spawns face the CCW perimeter at t=0. They do **not** face the center. The first 12 m of each trail is unique. No t=0 segment enters the bowl.

Host rotation is identity, so `transform.forward` on the spawn is the trail direction. Unity yaw is `LookRotation` of that forward.

| Spawn | World (x, z) | Face | Yaw | First 12 m (unique) |
|-------|----------------|------|-----|---------------------|
| SW | (8, 8) | +X | 90° | (8, 8) → (20, 8) along z=8 |
| SE | (152, 8) | +Z | 0° | (152, 8) → (152, 20) along x=152 |
| NE | (152, 92) | −X | −90° | (152, 92) → (140, 92) along z=92 |
| NW | (8, 92) | −Z | 180° | (8, 92) → (8, 80) along x=8 |

Objects: `MegaPark/P1/Spawns/Spawn_SW|SE|NE|NW`. Child `Pad` is a rubber circle, diameter 2.0 m (**graybox**). Child `Facing` is a concrete wedge on local +Z (not yellow, not blue). No collider on the marker.

SE’s first 12 m is the northbound exit. It leaves the hopscotch pad (pad ends z=22) after 2 m and does not run east–west across the courts.

---

## Spine table

Concrete `#C5CBD1`. Flush paint, **collider off**, so the lip is not a trip wall. Ground collision is the mulch plane. Tops sit at Y=0.06 so they read above reserved paint.

| Spine | Band (m) | Clear |
|-------|----------|--------|
| South | x[38, 118] × z[12, 20]. Center z=16. | **8.0 m** wide, locked by Z9. **Vertical under-clear ≥ 1.05 m** above the band stays empty. Bars are P2 and are not here. |
| North | x[14, 130] × z[83, 89]. Center z=86. | **6.0 m** band. The 6 m is the locked “loft approach stays open” clear, used as the path so the approach is not a second pinched lane. x[14, 130] is the **graybox** connector from the west sidewalk’s east face to the east corridor’s west face. |
| West | x[10, 14] × z[2, 98]. Center x=12. | Axis x=12 is locked. Sidewalk width **4 m is graybox**, centered on that axis. **Locked lawn** east of the cling strip is bare mulch x[14, 18] (4.0 m) inside Z2. East face has no props. |
| East | x[130, 138] × z[10, 90]. Center x=134. | **8.0 m**, the locked fort-corridor minimum. Flat. Not a fort floor. |
| Crash cross | x[52, 72] × z[40, 58] | **20 × 18 m always open.** Not paved. No concrete ribbon, no props, no toys. |

South-spine air from the concrete top up through Y=1.05 is reserved empty. P2 posts for the bars go **outside** that under-clear, not in it.

---

## Z8 Crash bowl — BUILD

Zone x[46, 78] × z[34, 66]. Floor **Y=−1.0**. Open. **No center props. No roof. No tube.**

| Object | Where | Size | Material |
|--------|--------|------|----------|
| `Bowl/Sandbox_Floor` | Center (62, −1.1, 50), top at Y=−1 | 32 × 0.2 × 32 | Sand `#E6D2A2` (**graybox** — blueprint has no sand hex) |
| `SandBank_W/E/S/N` | 4 m run inside the rim, rise 1.0 | Slope length √(4²+1²) | Same sand. **Graybox** run. The locked open rect has 6 m of margin on W/E/S and 8 m on N, so a 4 m bank stays outside x[52, 72] × z[40, 58]. |
| `Toy_Sandbox_Bucket_SW` | (51, −0.825, 42.5) | Cylinder Ø 0.40, h 0.35 | Rubber `#2A2A2E` |
| `Toy_Sandbox_Shovel_SW` | (51, −0.97, 43.6) | 0.55 × 0.06 × 0.08 | Rubber |
| `Toy_Sandbox_Mold_NE` | (73, −0.94, 60.4) | Cylinder Ø 0.50, h 0.12 | Rubber |
| `Toy_Sandbox_Sifter_NE` | (73, −0.97, 61.2) | 0.45 × 0.06 × 0.40 | Rubber |

Toys are named sandbox toys, not stones. Every dimension ≤ 0.6. They sit in the SW and NE only, outside the open rect (bucket/shovel x=51 < 52; mold/sifter x=73 > 72 and z > 58). Crossing A is the open rect itself, not a prop.

Mulch is cut out of the bowl. The sand floor is the collider. Banks are the way in and out. There is no center island.

---

## Z7 Kickball — BUILD

Zone x[64, 114] × z[28, 68]. Open field. West side is the mouth into the bowl: **no rail, no bases, no grass** on x[64, 78] × z[34, 66] (that rectangle is sand).

| Object | Where | Size | Material |
|--------|--------|------|----------|
| `Field` | x[78, 114] × z[28, 68] | Flush paint, top Y=0.04 | Field green `#3C9A58` (**graybox** hex; blueprint says field green, no hex) |
| `Field_MouthSouth` | x[64, 78] × z[28, 34] | Flush | Field green |
| `Field_MouthNorth` | x[64, 78] × z[66, 68] | Flush | Field green |
| `Base_Home` | (96, 0.10, 34), top Y=0.20 | 0.90 × 0.20 × 0.90 | Rubber. Height inside the locked 0.15–0.30. Footprint **graybox**. |
| `Base_First` | (110, 0.10, 48) | same | Rubber |
| `Base_Second` | (96, 0.10, 62) | same | Rubber |
| `Base_Third` | (82, 0.10, 48) | same | Rubber. x=82 is east of the bowl lip x=78. |
| `Mound` | (96, 0.125, 48), top Y=0.25 | Ø 2.4 × h 0.25 | Rubber. Height inside 0.15–0.30. Diameter **graybox**. Not a wall. |
| `Rail_East` | (114, 0.45, 48), top Y=0.90, z[28, 68] | 0.12 × 0.90 × 40 | Rubber. **East only.** |

Base spots are **graybox** layout inside the locked field, east of the bowl. They are not MLB distances and not a new lock.

No north rail, no south rail, no west rail. The east rail is a 0.90 vault, not a slide, so it is **not yellow**. It is also **not steel** — steel is the bar color and the bars are P2.

---

## Fence and collar — BUILD

| Object | Where | Notes |
|--------|--------|--------|
| `Collar_S/N/E/W` | 3 m outside the playable edge | Grass `#3F7A4A`. Width is locked. |
| `Fence_S/N/E/W` | Inner face on x=0, x=160, z=0, z=100 | **Graybox height 2.4 m** (blueprint does not lock fence height). Thickness 0.08 m. Rubber, not steel, not blue. Boundary only: not a cling wall, not a vault toy, not a roof. |

---

## Reserved zones — NOT built

Paint / footing only. No meshes, no stones, no tubes, no decks, no bars, no swings, no forts, no hopscotch grids. **No blue. No yellow.** Paint top Y=0.025, collider off, so a pad cannot hide a dummy.

| Zone | Bounds | P1 | Later |
|------|--------|----|-------|
| Z1 Soft-Play Mega SW | x[2, 38] z[2, 36] | Cedar paint `#8A5A3C` (graybox, not a verb color) | **P2.** Not built. |
| Z2 Cling Wall Arena W | x[2, 18] z[38, 78] | Rubber footing only on x[2, 10]. x[14, 18] bare mulch (the 4 m lawn). **Not blue.** | **P2.** |
| Z3 Merry | x[22, 46] z[34, 60] | Cedar paint. No carousel, no picnic tables. | **P4.** Crossing B lives here later. |
| Z4 Slide Mountain NW | x[22, 56] z[72, 98] | Rim-pad paint `#6B4636`. **Not yellow.** No decks, no chutes. | **P3.** |
| Z5 Swing Grove N | x[58, 100] z[78, 98] | Cedar paint. No A-frames, no seats. | **P4.** |
| Z6 Twin Forts E | x[118, 158] z[10, 90] | Bark paint `#3E261C` on army z[10, 46] and knight z[54, 90]. **Gap z[46, 54] = 8.0 m is empty mulch** (plus the flat east spine). No crawl, no spiral, no crates, no roofs, no raised floors. | **P3.** |
| Z9 Monkey Bar Highway | x[38, 118] z[12, 20] | **Concrete spine only.** The bars themselves are not built. | Bars **P2.** |
| Z10 Hopscotch SE | x[118, 156] z[2, 22] | Concrete footing. No court lines, no 0.45 lips. | **P4.** |

Z7 and Z8 are the only zones with structures. They are specified above.

### Two exits (later phases cannot dead-end)

Every toy, when it is built, is a lane with two exits. P1 does not build those toys. The clears are reserved now:

| Clear | Lock | Where it applies |
|-------|------|------------------|
| Soft-play aisles | ≥ 1.1 m | Z1, both N–S and E–W, when P2 builds it. No single-door tube. |
| Fort crawl mouths | ≥ 0.56 m in front of every mouth | Z6, when P3 builds it. Blueprint fail at < 0.5 m; keep 0.56. |
| Bar under-clear | ≥ 1.05 m | The south spine air, already empty. |
| Fort corridor | ≥ 8 m | East spine x[130, 138] and the army/knight gap z[46, 54], already empty. |

Cling, when P2 builds it: wall climb / wall-run = **held Cling into the wall** (Move wish, not a new bind). Wall jump = Cling + Jump. Not a new climb verb.

Swings, when P4 builds them: the verb is **vault the rail**. Seat top ≤ 1.05. No sit verb.

Slide mountain, when P3 builds it: decks at +2.0 / +3.5 / +5.0 (blueprint), inside the +6.0 envelope, **on the rim** (Z4), not in the cooker. Taggable for about 2 s, then a chute to the north spine or the lawn south of the pad. The landing has to be visible from the bowl. No dead-end roof. No new climb verb. Height uses jump, held cling, and slope slide only.

---

## Trail Tag

One CCW loop plus two crossings. Not a maze. No third path through soft-play.

### Primary loop (this is the stamped line)

Mulch / ground. Y=0. Vertices are locked spawns, or the crossing of a spawn line with the locked south spine (z=16, x=38 and x=118).

| # | From | To | Beat | Length |
|---|------|----|------|--------|
| 0 | (8, 8) | (38, 8) | SW depart, soft-play **south fringe** | 30 |
| 1 | (38, 8) | (38, 16) | Fringe corner onto the bars (zone edge, not a cut through Z1) | 8 |
| 2 | (38, 16) | (118, 16) | Bar highway centerline | 80 |
| 3 | (118, 16) | (118, 8) | Drop to the SE approach, west edge of hopscotch | 8 |
| 4 | (118, 8) | (152, 8) | Into the SE corner. Spawn then leaves **north**. | 34 |
| 5 | (152, 8) | (152, 92) | East forts, army apron, kickball east fence on the west, knight | 84 |
| 6 | (152, 92) | (8, 92) | Swing grove, slide-mountain lawn | 144 |
| 7 | (8, 92) | (8, 8) | Cling edge, merry west fringe, back to SW | 84 |
| | | | **Total** | **472 m** |

Empties: `MegaPark/P1/TrailTag/WP_00` … `WP_07`, in that order, each facing the next point. Close the loop back to `WP_00`.

The west leg (x=8) and the south fringe (z=8, x from 8 to 38) only **graze** Z1. They are the perimeter. They are not a shortcut across soft-play. P2 must not add one.

The concrete spines are the footing of those beats (south spine **is** leg 2). They are not extra crossings. No props sit between the spawn line and the nearby spine in a way that splits the north or west beat into two routes.

### Crossings (not part of the 472 m)

| Crossing | P1 | Rule |
|----------|----|------|
| **A** | The open bowl rect x[52, 72] × z[40, 58], then the kickball mouth west of x=78. Example sand line (52, 50) → (72, 50) is 20 m of empty floor. | Always open. Late crossing only. Not a t=0 spawn line. |
| **B** | Not built. Future merry, x[22, 46] z[34, 60]. | Optional. Still only the second crossing. Do not add a third. |

### Time (stamped off 472 m, not off the blueprint estimate)

Eng confirm `07fe480`: `MovementConfig.asset` sprintSpeed = **12 m/s** (class default 12). Walk is 6. It ground cap is **12.55** (taggerSprintBonus 0.55). Legacy `MovementTuning` sprint 9 is not this motor.

| Who | Speed | 472 m |
|-----|-------|-------|
| Runner (stamped) | 12 m/s | **472 / 12 = 39.333… s (39.3 s)** |
| It | 12.55 m/s | **472 / 12.55 ≈ 37.6 s** (same loop, slightly faster) |

The blueprint’s ~380 m is an **estimate, not this centerline**. At 12 m/s, 380 m would be ~31.7 s. The old “~70 s at sprint 7” figure is retired and is not used. Sprint 9 is not used.

---

## Read rules (160×100)

1. **One cooker.** Kickball + crash bowl stay the open middle. It stays visible. No roofs, no tubes a full dummy can hide inside, no fort floors that hide a silhouette. Bowl center stays empty. Crossing A stays open. Hot Potato handoffs and Least It swaps use that sightline.
2. **One loop, two crossings.** A = bowl / kickball west. B = merry (not built). No third path through soft-play. Spawns face along the loop. First 12 m unique. No t=0 dump into the bowl.
3. **Two exits.** Clears above. A one-door fort or tube is a dead end and is out of spec even in a later phase.
4. **Color is the verb.** Blue `#3D7EFF` only on a real cling / wall-run surface. Yellow `#F5D547` only on a real slide that can extend a slide. **P1 uses neither.** Field green = kickball. Sand = bowl. Concrete = spines (and the hopscotch footing). Steel `#B8C0C8` = bars, and the bars are P2, so P1 does not paint steel. Reserved pads are cedar / bark / rim-brown / rubber, none of which are cling or slide.
5. **Height on the rim.** Towers stay on Z4, dump back to the north spine or the lawn, landing visible from the bowl. Not built in P1. The ground pad is reserved so the rim is already claimed. No dead-end roof. No new climb verb.
6. **Four weenies, readable from the bowl as ground, not as stone toys.** Slide-tower pad NW (Z4). Soft-play pad SW (Z1). Twin-fort pads E (Z6, with the gap). Bar highway S (the concrete south spine; the steel bars come in P2).
7. Loop seconds are the table above. Do not put the sprint-7 number back.

---

## What Level placed vs what Eng wires

**Level placed (this branch):**

- `MegaParkP1Bootstrap` builds only P1 under `MegaPark/P1/…`.
- Fence, collar, mulch with a bowl hole, reserved paint, four concrete spines, open sand bowl, sand banks, four named sandbox nubs, kickball field, bases, mound, east rail.
- Four spawn transforms with facing. Eight loop empties.
- No `ParkPropDresser`, no `PgkLandmarkPlacer`, no `ExperimentalFanPlacer`, no Flow stones, no campus edit.

**Eng wires (not done here):**

- A **new** scene or a disabled host. Empty GameObject at world origin, identity, scale 1, component `Tag.Level.MegaParkP1Bootstrap`. Context menu “Rebuild Mega Park P1”, or Play. **Do not add it to the campus scene.**
- Spawn system reads `Spawn_SW/SE/NE/NW` world position and `transform.rotation` (forward = trail at t=0).
- Trail Tag follows `LoopCcw` / `WP_00`–`WP_07`, length 472 m. Crossings A and B are not that polyline.
- OOB on the collar is Eng’s (kill / respawn). The fence collider is only a boundary.
- East rail is the 0.90 vault. Do not tag the perimeter fence as cling or vault.
- Do not mark any P1 surface cling (blue) or slide (yellow). Those verbs arrive with the real surfaces in P2/P3.
- Bowl floor and banks are the Crossing A collision. Do not drop a prop in x[52, 72] × z[40, 58].
- Motor stays the `07fe480` MovementConfig. Do not retune sprint to make the loop “feel like 70 s”.

---

## Fail checks (P1)

| Fail | P1 state |
|------|----------|
| Footprint stays 36×28 | Playable is 160×100. Scale is 1. |
| Center props in the bowl | Toys are SW/NE nubs outside the open rect. |
| Roofs / hide-tubes / fort floors | None built. |
| Spawns face center | They face +X, +Z, −X, −Z. |
| Climb as a new verb | Not added. Cling note is the existing held-Cling wish, for P2. |
| Flow stones / cube clusters standing in for toys | None. Reserved zones are paint. |
| Blue or yellow on a lie | Neither color is in the script. |
| Third soft-play path | Not cut. |
| Campus rewritten | `0a42007` files are not in this commit’s diff except as the parent. |

---

## Palette actually used

| Surface | Hex | Role in P1 |
|---------|-----|------------|
| Mulch | `#5C3A2E` | Main fill. Locked. |
| OOB grass | `#3F7A4A` | Collar only. Locked. |
| Concrete | `#C5CBD1` | Spines + hopscotch footing. Locked. |
| Rubber | `#2A2A2E` | Bases, mound, east rail, fence, cling footing, sandbox nubs, spawn circles. Locked hex. |
| Field green | `#3C9A58` | Kickball. **Graybox hex.** |
| Sand | `#E6D2A2` | Bowl. **Graybox hex.** |
| Cedar | `#8A5A3C` | Z1 / Z3 / Z5 landmark paint. **Graybox.** Not a verb. |
| Bark | `#3E261C` | Fort pads. **Graybox** for the blueprint’s “darker bark”. |
| Rim pad | `#6B4636` | Z4 landmark. **Graybox.** Not yellow. |
| Yellow `#F5D547` | — | Not used. Slides only, and slides are P3. |
| Blue `#3D7EFF` | — | Not used. Cling surfaces only, and those walls are P2. |
| Steel `#B8C0C8` | — | Not used. Bars only, and bars are P2. |
