# CUT — Graybox v0.2 (chase density)
**Owner:** Level  
**Status:** **Approved** (Producer 2026-09-04). Density pass on locked v0.1. Same arena. Not a second map.  
**Engine:** Unity repo `CouchCoopGaming/Tag-game` — extend `CutArenaBootstrap` in Play.  
**Date:** 2026-09-04

Landon asked for a real Tag arena while he’s at work. v0.1 proved the three chains. v0.2 adds chase/juke routes **inside** the same 36×28 m. Graybox primitives only.

---

## Locked (do not move)

Systems v0 meters. C1 West / C2 East Alley / C3 South Lane stay exactly where v0.1 put them.

| | |
|--|--|
| Playable | 36 × 28 m, origin SW, +X east +Z north |
| Bowl | X[13,23] Z[9,19] Y=−1.0, rim 1.00, 20° corner ramps |
| Loft | X[15,21] Z[24,28] Y=+1.5, lip 1.50 |
| West Wall | 10×3.2 @ X=8 Z[8,18] |
| SW Wall | 8×3.2 @ X=8 Z[0,8] |
| East Alley | X=28.0 & 31.2, Z[8,18], 3.2 m gap, 10×3.5 |
| SE Alley | same X, Z[0,8], 8×3.5 |
| Vaults | South 0.90 then 1.05; Chain1 landing 1.00 at X[10.4,12.4] Z[15,16] |
| G pads | 3×3 at wall mouths (v0.1 list) |
| Spawns | SW(3,3) SE(33,3) NW(3,25) NE(33,25), 1.2 m elbows |
| Envelope | Y[−1.0, 3.5] |
| Verbs | sprint jump slide wall-run wall-jump vault only |

No +3.6 / +7.2 decks. No climb. No third parallel wall in East Alley. No guns/cover dens.

---

## Why densify, not grow
36×28 already crosses in ~5 s at sprint 7. Growing the footprint makes It never catch. Tag wants **decision points**, not a campus. Fill empty G with vaultable furniture and cut-throughs so a chase has 2–3 line choices without leaving the slice.

---

## Additions (v0.2)

All new pieces are **low vault 0.90–1.05** or **1.2 m elbows** (high-vault height). Nothing a runner must stop on. Clear ≥3 m after every vault.

### 1. West Back-Alley (C1 reverse / juke)
- Corridor X[0.4, 2.6] Z[6, 20], G floor (already there).
- Three 0.90 vault stubs across the alley, 0.5 m thick, Z = 8, 13, 18. Length 2.2 m in X.
- Open N and S. Lets a runner dump **west** of West Wall and loop instead of dead-ending into OOB.

### 2. Mid Cut (It intercept, Bowl → East Alley)
- Gap X(23, 28) is empty G. Add two staggered 1.00 rails, **not** walls:
  - `Vault_MidCut_S` — X=24.8, Z[10.5, 12.5], 1.00 × 0.4 × 2.0
  - `Vault_MidCut_N` — X=26.2, Z[15.5, 17.5], 1.00 × 0.4 × 2.0
- No wall-run tag. This is a hurdle cut, not a 4th ping-pong face.

### 3. Sightline breaks (readable, not camp)
Two 1.2 m L-stubs (same kit as spawn elbows), so It cannot laser the whole yard:
- `Elbow_BowlSouth` at (18, 7) — south of Bowl, arms toward Bowl / South Lane
- `Elbow_BowlNorth` at (18, 21) — north of Bowl, arms toward Bowl / Loft
No chest-high cover on Loft. These are vaultable 1.2 m (high band). **Taggable within ~2 s of pathing** — It can vault or go around; no hide.

### 4. Juke islands
Low 1.00 tables, 2×1 footprint, 0.4 thick, G:
- `Island_NW` center (10, 22) — between West Wall north and Loft
- `Island_NE` center (26, 22) — between Bowl north and East Alley north mouth
Approach from any side. Not camping boxes (no walls around them). **Taggable within ~2 s of pathing** from Bowl rim or nearest wall-run — no untaggable camp.

### 5. Bowl nubs (optional juke, keep tag readable)
Two 0.90 cubes 1×1 **inside** Bowl, 2.5 m in from SW and NE ramps:
- `Nub_Bowl_SW` (15.2, −1.0 floor, 11.2)
- `Nub_Bowl_NE` (20.8, −1.0 floor, 16.8)
Do **not** fill the center. Tag still happens in the open 10×10.

### 6. South off-axis rail
- `Vault_SouthOff` 0.90 at X[18, 20] Z=2.6 — south of C3, does not sit in the C3 landing cone (C3 stays Z≈4–5.5). Extra vault line for a juke off South Lane.

---

## Flow (what a chase should feel like)
- Runner on C1 can abort west into Back-Alley or east over Chain1 landing into Bowl.
- It can cut Mid Cut instead of committing East Alley.
- Cross-map snipe is broken by Bowl elbows; silhouettes still read (1.2 m, no roofs).
- 2p still uses opposite corners. 4p copies (SW/SE walls) unchanged.

## Watch (Producer)
- Mid Cut = vault rails only (not wall-run).
- Bowl center stays open.
- South off-axis stays out of C3 landing cone.
- Juke islands + Bowl elbows: taggable within ~2 s pathing. No untaggable camp.

## Fail (don’t ship if)
- C1/C2/C3 paths blocked or vault-into-wall.
- New piece taller than 1.70 or a wall that creates a 4th air-loop.
- Dead landing pad (any new top you must stop on).
- Drop > 1.73 without a ramp.

## Drop format
Extend `Assets/Scripts/Level/CutArenaBootstrap.cs` (`Build()` adds new methods). Keep existing BuildWalls/Vaults/Bowl/Loft/Pads/Spawns. Marker colors unchanged (cyan wall, yellow vault, magenta slide/pads, orange spawn). Update `Assets/Art/Graybox/README_CUT_REF.md` with v0.2 addition list. Do not new-scene, do not second map.

## QA walk (add to C1/C2/C3)
4. West Back-Alley loop: dump west of West Wall, vault stubs, re-enter C1 or Bowl.
5. Mid Cut: Bowl east rim → two hurdles → East Alley mouth without a wall-run attach.
6. Elbows: sprint N–S through map center, must vault or go around 1.2 m stubs.
