# PARK v1 — playground / park (one slice arena)
**Owner:** Level (layout) · Art Director (bible) · Engineer (CutArenaBootstrap)
**Status:** Plan for Producer. Evolves CUT v0.2 — **not** a second map.
**Fantasy:** crash-test dummies playing tag in a giant playground.
**Date:** 2026-09-04

---

## Modes (layout only — Systems owns rules)

| Mode | What the map must do |
|------|----------------------|
| Hot Potato (transfer-It) | Tight catch window. Sandbox cooker. No untaggable toys. |
| Least It | Same. Readable who is It across lawns. |
| Trail Tag | **Long readable run lines** + **planned crossings**. Spawns don’t dump four trails on top of each other. |

One 36×28 m yard. Sprint 7 → a **perimeter loop ~128 m (~18 s)** is the long line. Crossings happen later, not at t=0.

---

## Locked meters (CUT v0.2)

Do **not** move C1/C2/C3, bowl, loft, walls, vaults, pads, spawns, density props. Dress and add **toy shells** on top of those volumes.

Playable 36×28. Envelope Y[−1.0, 3.5]. No climb-as-verb. No roofs. No buildings. No 4th ping-pong wall. No footprint grow.

---

## Playground toys (what you see)

All toys are **vault / wall-run / slide / jump** — look like playground, play like the kit.

| Toy | Sits on | How it plays |
|-----|---------|--------------|
| **Sandbox** | Bowl −1.0 | Open center. Nubs = sand toys at SW/NE only. Tag cooker. |
| **Play tower** | Loft +1.5 | Deck + 1.50 lip. Two 20° **slides** off W/E (existing ramps). |
| **Climbing wall** | West 10×3.2 | Wall-run C1. Looks climbable; **not** climb-as-verb. |
| **Twin towers** | East alley 3.2 m gap | C2 ping-pong faces. Slide-tower dressing. |
| **Playground slide** | C1 rubber strip X[2,7] Z[10,12] | Slide **verb** on a chute mesh (5° already). |
| **Rubber track** | C3 strip X[16,22] Z[3,5] | Slide into jump. |
| **Monkey bars** | three 0.90 rails, 1.2 m apart, along X[10,16] Z=7 (south of bowl, **north of C3 cone**) | Vault-vault-vault. Looks like bars. No hang/climb. |
| **Picnic tables** | Mid Cut 1.00 rails + islands (10,22)(26,22) | Vault only. Not wall-run. Taggable ~2 s. |
| **Park benches** | South 0.90/1.05 + SouthOff Z=2.6 | C3 + off-axis juke. |
| **Hedge planters** | 1.2 m elbows | Sightline break, dummy silhouette still reads. |
| **Rubber pads** | spawns + G pads | Start circles. |

**Bars:** 0.90 high, 0.2×1.6 footprints, yellow/furniture mat — vaultable, not a ladder.

---

## Trail Tag run lines

**Primary loop (long, readable):** spawn → perimeter **counter-clockwise**  
SW → south benches → SE → east twin-tower lawn → NE → north tower lawn → NW → west climbing-wall lawn → SW.  
Stay on G/lawn. ~full courtyard loop. Trails are visible across the sandbox.

**Crossing A (late):** sandbox. Trails may cut through open sand. Center stays empty so crossings are readable, not spaghetti.

**Crossing B (optional):** Mid Cut picnic tables (vault, not walls). Trails hop furniture; don’t spawn here.

**Spawn anti-spaghetti:** four corner pads. At t=0 each dummy faces **along the perimeter CCW**, not at the sandbox. First 8 m of each trail is unique (south, east, north, west lawns). No shared first-segment.

---

## Sightlines
Dummy silhouettes readable from any spawn into sandbox. Max clutter height besides climb walls: hedge 1.2 / tower 1.5. Lawns open. Light Cycle read = you see the loop and the two crossings.

---

## Working palette (AD locks bible)
Lawn `#5B8C4A` · Sand `#D4B483` · Rubber `#8B3A3A` · Climb `#3AA6C8` · Furniture/vault `#E6C14A` · Hedge `#3F6B3A` · Tower `#C4B8A5`

---

## Engineer drop (after Producer ACK)
1. Recolor/rename existing primitives to PARK roles (don’t move meters).
2. Add **monkey-bar** three-vault at X[10,16] Z=7, 0.90, 1.2 m spacing — only new geo.
3. Name slides/towers/bars in hierarchy (`Toy_Slide_C1`, `Toy_Tower`, `Toy_Bars`, …).
4. Same Play scene, `CutArenaBootstrap`.

## Fail
C1/C2/C3 blocked · bar >1.10 (kills slide-after-vault) · trail spawn facing the sandbox · roofs · climb volumes · second map.
