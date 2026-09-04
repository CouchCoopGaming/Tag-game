# CUT graybox reference (v0.1)

Runtime graybox is built by `Tag.Level.CutArenaBootstrap` on Play scene Awake
(primitives under a `CUT` root). Open **Play**, press Play — arena spawns in Awake.

## In this folder

| File | Role |
|------|------|
| `CUT-graybox-v0.1.md` | Authoritative meters / chains (Level brief) |
| `CUT-plan.svg` | Top-down plan |
| `README_CUT_REF.md` | This note |

Canonical shared-box paths (if present): `/workspace/tag-gdd/level/CUT-graybox-v0.1.md`, `CUT-plan.svg`.

## Scale (Systems v0 locked)

| | |
|--|--|
| Playable | X [0, 36], Z [0, 28] — origin SW, +X east, +Z north |
| G / Bowl / Loft | Y = 0 / **−1.0** / **+1.5** |
| Wall tops | ≤ 3.5 — envelope Y [−1.0, 3.5] |
| West Wall | 10×3.2 @ X=8, Z[8,18]; SW copy 8×3.2 |
| East Alley | faces @ X=28.0 & **31.2** (3.2 m gap), 10×3.5; SE copy 8 m |
| Vaults | low **0.90 / 1.05**, Chain1 landing **1.00**, loft lip **1.50**, bowl rim **1.00** |
| Spawns | (3,3) (33,3) (3,25) (33,25) — orange; elbows yellow 1.2 m |
| Markers | cyan = wall-run; magenta = slide / pad edge; yellow = vault |

No +3.6 catwalk, no +7.2 deck. Full mesh art comes after chains play.


## v0.2 density (added in CutArenaBootstrap.BuildV02Density)
- West Back-Alley: three 0.90 stubs X[0.4,2.6] @ Z=8,13,18
- Mid Cut hurdles 1.00 @ (24.8,11.5) and (26.2,16.5) — vault only, not wall-run
- Elbows 1.2 m at (18,7) and (18,21)
- Islands 1.00 at (10,22) and (26,22)
- Bowl nubs 0.90 at (15.2,11.2) and (20.8,16.8)
- South off-axis 0.90 at X[18,20] Z=2.6

v0.1 chain geo is unchanged. Brief: `CUT-graybox-v0.2.md`.
