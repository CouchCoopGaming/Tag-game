# CUT — Graybox blockout brief v0.1
**Owner:** Level  
**Status:** Retune pass **v0.1** — Systems Movement Numbers v0 **locked**. All kit meters from that sheet.  
**Arena:** CUT — only slice map  
**Engine:** Unity — hold drop until Engineer has a project  
**Mode:** 2–4p transfer-It, least time-as-It, ~90–120s  
**Date:** 2026-09-04  
**Topology:** unchanged from v0 (Bowl, West Spine, East Alley, South Lane, North Loft, 4 G spawns). Scale only.

Verbs only: sprint, jump, slide, wall run, wall jump, vault.  
Out: climb, double jump, grapple, guns, second map, art.

---

## Locked scale (Systems v0)

| | |
|--|--|
| Stand capsule | **1.8 m** |
| Slide capsule | **0.9 m** |
| Jump apex | **1.15 m** |
| Wall-jump extra height | **~0.54 m** (up 5.5 @ g≈28) |
| Hard-land | drop **> 1.73 m** (1.5× apex) |
| Vault low | **0.45–1.10 m** — CUT uses **0.90 and 1.05** |
| Vault high | **1.10–1.70 m** — CUT uses **1.50** on Loft only |
| Wall attach | routes that can still be **≥ 5.0 m/s** (sprint 7.0) |
| Wall-run max | 1.25 s × 7.0 ≈ **8.8 m** of face |
| Ping-pong gap | **3–5 m** — CUT uses **3.2 m** (2–3 crossings from G) |
| 3-wall cap | after 3 contacts no ground, attach min → 6.2. Geo forces a ground touch. |

Verticality is Bowl −1.0, G 0, Loft +1.5, wall tops ≤ 3.5. No extra decks.

---

## Footprint

Unity: origin = SW corner of playable. +X east, +Z north, +Y up. Plan north-up.

| Bound | Meters |
|-------|--------|
| Playable | X [0, 36], Z [0, 28] |
| OOB skirt | 2 m (volume X [−2, 38], Z [−2, 30]) |
| G | Y = 0 |
| Bowl floor | Y = **−1.0** |
| Loft | Y = **+1.5** |
| Wall tops | ≤ **3.5** |
| Envelope | Y [−1.0, 3.5] |

Crossing 36 m at 7.0 ≈ 5 s. Sized for 2–4p tag.

---

## Vaults

| Piece | H | Band | Where |
|-------|---|------|--------|
| Bowl rim | 1.00 | low | all four sides, G |
| South rail A | 0.90 | low | X [10, 12], Z ≈ 4 |
| South rail B | 1.05 | low | X [14, 16], Z ≈ 4.5 (stagger) |
| Chain 1 landing | 1.00 | low | X [10.4, 12.4], Z [15, 16] — **2.8 m** east of West Wall |
| Loft lip | 1.50 | high | south edge of loft |
| Illegal | < 0.45 or > 1.70 | — | — |

Clear **≥ 3 m** after every vault (low lock 0.28 s × 7 ≈ 2 m). Never vault into a wall.  
Slide lanes are **open floor** — no 0.9 m tunnels.

---

## Walls

| Piece | Size | Place | Role |
|-------|------|-------|------|
| **West Wall** | **10 × 3.2**, 0.4 thick | X = 8.0, Z [8, 18], Y [0, 3.2] | Chain 1 |
| **SW Wall** (4p) | **8 × 3.2** | X = 8.0, Z [0, 8] | copy, not required for 2p |
| **East Alley W face** | **10 × 3.5** | X = 28.0, Z [8, 18], Y [0, 3.5] | Chain 2 |
| **East Alley E face** | **10 × 3.5** | X = **31.2**, Z [8, 18] | **3.2 m gap** |
| **SE Alley** (4p) | **8 × 3.5**, 3.2 gap | X = 28.0 & 31.2, Z [0, 8] | copy |

Cyan marker = WallRun (not climb). 2 m clear approach on the attach side.

### 3-wall cap — ground pads (cheese dies)

- G floor on **both sides** of every wall. No floating cages. **No third parallel wall.**
- **3 × 3 m G pads** (magenta edge) at both mouths of each alley and both ends of West/SW walls:

| Pad | Center (X, Z) |
|-----|----------------|
| East Alley S mouth | (29.6, 6.5) |
| East Alley N mouth | (29.6, 19.5) |
| SE Alley S mouth | (29.6, 1.5) |
| West Wall S | (8.0, 6.5) |
| West Wall N | (8.0, 19.5) |
| SW Wall S | (8.0, 1.5) |

Loft is a mantle, not a wall loop. Bowl ramps count as ground. You land, then you may re-attach.

---

## Zones

**Bowl** — X [13, 23], Z [9, 19], Y = −1.0. Rim vault 1.00 back to G. Four corner ramps **20°** (rise 1.0 over ~2.7 m). Drop 1.0 < hard-land 1.73. Open sight. Tag cooker.

**West Spine (Chain 1)** — G sprint X [4, 7], Z [0, 12] → **6 m slide** X [2, 7], Z [10, 12] (5° down ok) → jump → West Wall → wall jump **2.8 m** east to 1.00 vault → sprint into Bowl. Exit is a rail, **not** a pad. No mid catwalk.

**East Alley (Chain 2)** — Floor is G the whole way. Open N and S. Faces clean. Last hop dumps to G sprint.

**South Lane (Chain 3)** — 16 m G, X [6, 22], Z [2.5, 5.5]. 0.90 then 1.05, 4 m apart, 3 m clear after each. Then **6 m slide** X [16, 22], Z [3, 5] → jump into Bowl south rim. No wall in the landing cone.

**North Loft** — 6 × 4 at **+1.5**, X [15, 21], Z [24, 28]. High-vault 1.50 from G. Two drop-offs with **20°** ramps. No chest-high cover. Leave via drop or West Wall north. Tagable in ~2 s of pathing from Bowl.

---

## Chain routes

1. **SW G → West Wall → Bowl**  
   sprint → slide 6 m → jump → wall run ≤ 0.8 s → wall jump → 1.00 vault → sprint.  
   Fail: dead landing pad; drop > 1.73 with no ramp; wall shorter than ~8 m.

2. **East Alley N↔S**  
   sprint → jump → wall → wall jump → opposite wall → G sprint.  
   Fail: gap > 3.5 m; missing floor; clutter on faces; a 4th wall in the air.

3. **South Lane W→E**  
   sprint → 0.90 vault → 1.05 vault → slide → jump.  
   Fail: vault into wall; no 6 m floor after rail B; rail > 1.10 (high lock kills slide gate).

---

## Spawns / It

| Pad | Center (X, Z) | Use |
|-----|----------------|-----|
| SW | (3, 3) | 2p + 4p |
| SE | (33, 3) | 2p + 4p |
| NW | (3, 25) | 4p |
| NE | (33, 25) | 4p |

- All on **G**. Nobody starts on Loft or in Bowl. Equal path to Bowl ~12–14 m.
- **2p = opposite corners** (SW↔NE).
- **1.2 m elbow** (high-vault height, L-stub ~2 m) breaks spawn LOS. Not a full wall.
- **It is a player flag, not a unique spawn.** Read = open Bowl, silhouettes (no roofs). Spawn grace = Systems/Engineer.
- Pads orange. Elbows yellow.

---

## Graybox kit
Cubes. Ramps 15 / 20 / 30. Vault boxes **0.90, 1.05, 1.50**. Wall-run strips cyan. Slide magenta. Ground pads magenta edge. Spawn orange. Bowl dark. Loft light. OOB. No art, no climb volumes, no cover dens.

---

## Hold
Do **not** drop on Engineer until the Unity project exists. Then this file + `CUT-plan.svg`. Full mesh after graybox plays the three chains.

## QA
Chain 1 never dies below walk 4.5. Slide-jump out-distances stand jump. East Alley 2–3 ping-pongs then **ground pad**. No drop > 1.73 without a ramp. No air-loop of 4 walls.
