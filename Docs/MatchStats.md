# Match stats

A match keeps a story for the results card. The mode still picks the winner. Least time as It, and a tie when those times match, are unchanged.

## What is counted

Each pawn, human or AI, up to eight:

| Stat | When it moves |
|---|---|
| Tags made | A punch transfers It |
| Times tagged | That pawn becomes It |
| Time as It | While they are It |
| Longest survival | Longest stretch they are not It |
| Distance | Path length |
| Top speed | Fastest planar speed |
| Air time | Off the ground, not on a wall run or a zip |
| Wall-runs | A run starts |
| Wall-jumps | The motor's wall-jump count steps |
| Pads | A launch arc starts |
| Zips | A ride starts |
| Air dashes | A dash starts |
| Punches landed | A swing connects |
| Punches whiffed | The active window ends with no connect |
| Staggers caused | A connect that is not a tag |
| Near-misses | It comes inside punch reach (1.55 m) and does not tag |
| Tag-backs blocked | A swing meets tag-back immunity |

That is 17 stats. Feel numbers are not stored here. Coyote, jump buffer, cling grace, jump speed, slide boost, air dash, punch reach, lunge, climb, slip, wall-run, stagger, pad cooldown, zip speed, and tag-back immunity stay where they were.

## No per-frame garbage

The counters, the previous-sample slots, and the highlight ring are static buffers. A frame writes numbers into them. The results strings are built once, when the match seals. Sampling caches the motor on round start.

Rounds in one match add up. Rematch clears the book.

## Results

The winner line is the one the mode already wrote. Under it, two or three awards and a card per pawn.

Awards, in order, while there is a positive leader, stopping at three:

- **Hot Potato** — most tags made
- **Slipperiest** — most near-misses
- **Sky Walker** — most air time
- **Wall Crawler** — most wall-runs

A tie lists every seat that shares the high value. Seat 0 is not preferred. A zero high value gives no award.

Cards use the seat palette (`PaletteOf`) and the HUD text scale. The basis is large enough that the 0.75 scale is still readable from the couch. Glyphs sit next to the names so color is not the only cue. The same card is the whole screen in a 1–4 player split and when AI are in the roster.

Confirm and Back skip the highlight first. The next press uses Rematch, Change setup, or Title as before.

## Highlight

A ring keeps pawn position, yaw, and pose for the last 8 seconds at 20 Hz (160 samples). It is allocated once. The final tag freezes that window. If the match never tags, the closest near-miss inside punch reach freezes it instead.

Playback uses the practice pose weights on ghost figures. A figure is a mesh and a chest bone. It has no character controller, no rigidbody, no collider, and it does not move itself. Leaving the card or skipping destroys the figures.

## Proof

`Tools/StrafeJumpSim` prints one `match-stats` line. It runs a four-pawn round with the existing opponent brain, checks the 17 stats fill, checks a shared award does not crown only the first seat, and checks the ring does not allocate.
