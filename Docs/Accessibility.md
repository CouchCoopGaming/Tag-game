# Accessibility

Palette, captions, rumble, and reduced flashing live on the existing pause settings card. They save in the same `tag-settings.json` blob as the other rows (`palette`, `palette1`–`palette3`, `captions`, `rumble`, `flash`, and `accessSeat`). Text size reuses the HUD scale row (0.75–1.50). No new verb. Cling stays a hold. Coyote 0.10, jump buffer 0.16, cling grace 0.08, jump speed 24.7, and the other feel locks are unchanged.

## Palettes

The Colorblind palette row cycles five sets: Default, Deuteranopia, Protanopia, Tritanopia, High contrast. Each set colors the four players, the It marker, the tag-back immunity glow, and the dash / safe / stagger / cling HUD marks.

Player colors stay apart under simulated deuteranopia, protanopia, and tritanopia (pairwise distance at least 0.35) and each clears 3:1 contrast against grass, mulch, concrete, sand, metal, and wood on Mega Park, Pocket Park, and Stack Yard.

Color is not the only channel. Name plates use a shape per seat: circle, triangle, square, diamond, with ● ▲ ■ ◆. The It marker draws a star plus the letters IT.

In a split, the Player row chooses which seat those four rows edit. P1’s palette tints the shared world plates, hat, and tag-back glow. Each viewport’s verb HUD, name chip, and It chip use that seat’s palette.

An older save that only has `colorblind=1` loads as Deuteranopia for P1.

## Text and captions

HUD scale sizes the verb cluster, the countdown digits, the It chip, and the split-screen name and timer. There is no second text slider.

Captions are off until a seat turns them on. They are small icons, not a transcript:

| Cue | Icon | Direction |
|---|---|---|
| It nearby footsteps | ▲ | Arrow toward the It pawn, inside 18 m |
| Countdown | ● | Center chip |
| Tag | ✦ | Arrow toward the tag |
| Pad launch | ▸ | Arrow toward the pad |
| Zip grab | ◆ | Arrow toward the zip |

One seat can leave captions off while another leaves them on.

## Rumble

Rumble is a short motor pulse. The row is Off, 25%, 50%, 75%, or 100%, stored per seat. Off writes nothing. A keyboard seat never writes a motor, even at 100%.

| Moment | Pulse |
|---|---|
| You claim It | Low, 0.12 s |
| You get tagged | Stronger low, 0.16 s |
| Punch hit | Short, 0.08 s |
| Punch stagger | 0.14 s |
| Hard landing | 0.10 s |
| Pad launch | 0.12 s |

The motors are a fixed table. The tick does not allocate.

## Reduced flashing

Reduced flashing flattens the tag-back glow strobe and holds the countdown digit at a steady white. The immunity window stays 1.0 s and the countdown timer is not slowed. If any seated player turns it on, the shared glow and the shared countdown card take the calmer look. Each seat’s flag is still stored on its own.
