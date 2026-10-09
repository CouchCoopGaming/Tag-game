# Profiles

Friends on the couch keep a name, a color, a look, their binds, and their practice board. The verbs are the ones already in the game. Coyote, jump, cling, dash, punch, lunge, climb, and tag-back are not retuned here.

## Save

Profiles live in the same `tag-settings.json` blob as look and audio. The blob version is 2. A future version still resets the whole file. A corrupt file resets. An empty file does not wipe a caller that has not stored one. Numbers that are not numbers are ignored.

An older blob (version 0 or 1) migrates into one profile named Player. That profile takes seat 0's palette, captions, rumble, and HUD scale, the keyboard and pad tables, and any personal bests or ghosts that were stored at the top level (`pb.`, `sp.`, `gh.`). The active profile's board is still written on those top-level lines so a solo practice load round-trips. Every profile's own board is also stored under `pN.pb.`, `pN.sp.`, and `pN.gh.`.

A profile holds:

| Field | Notes |
|---|---|
| Name | Up to 12 letters, digits, or spaces. The on-screen grid is driven with the pad: move, confirm a cell, backspace. Names are not filtered. |
| Color | One of the four swatches in that profile's colorblind palette |
| Look | A Hier body tint (Blue, Mint, Orange, Lavender, Tan, Red) and an accent stripe from the same existing panel colors. The Hier set has no headband mesh. The hat flag reuses the existing It hat; it does not add a mesh. |
| Binds | That profile's keyboard and pad tables |
| Accessibility | Palette, text scale (the HUD scale range), captions, rumble |
| Practice | Personal bests and ghosts for that profile |
| Lifetime | Matches, wins, tags, longest survival |

## Join

Match setup still opens the join screen from split-screen. Each seat line cycles a profile, or Guest. Guest is not saved. Two seats cannot hold the same profile. A seat that leaves frees it.

Colors stay apart under the colorblind simulations (pairwise distance at least 0.35). If two profiles want the same swatch, the one with the lower profile id keeps it and the other shifts to a free swatch. Seat 0 does not win that tie. Swapping seats does not change which profile keeps the color.

## Where the name shows

The split viewport label reads `Name · Zone` (a seat with no profile still reads P1–P4). The off-screen It arrow, the minimap dot, the It chip, the results stat card, and the award line use the profile name and the resolved color. Shape glyphs stay on the seat. Practice bests and ghosts are stored per profile under `arena/route` (`0/mega-beginner` through the seven routes on arenas 0, 1, and 2). An older `pb.mega-beginner` line migrates onto arena 0. Lifetime stats sit on a small card: matches, wins, tags, and longest survival. A win is the unique least time as It. A tie awards no win.

Rename and delete ask for a confirm first. A second press without a new confirm does nothing.

## Proof

`Tools/StrafeJumpSim` prints one `profiles` line after `match-stats`:

`profiles create=ok rename=ok delete=ok migrate=ok dupSeat=blocked colorClash=fixed bindsPerProfile=ok pbMoved=ok alloc=0 leftovers=0`

Create walks the pad grid. Rename and delete require the confirm. An old version-1 blob moves its best, ghost, binds, and accessibility onto the profile. Two seats cannot share a profile. A color clash shifts without preferring P1. Each profile keeps its own bind table. Resolving colors does not allocate.
