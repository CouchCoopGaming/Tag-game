# PGK_Slide_TubeDeck_2m

Deck-compatible campus slide tube (Art Director brief v0.1 / PR #9).

## Origin / pivot
- **FBX origin** = mulch under low mouth `(0,0,0)` local — **one pivot**.
- Low-mouth **center** at local `(0, 0, 0.60)` so shell rests on mulch (no bury).
- High-mouth center at local `(5.0, 0, 1.91)` — ~0.09 m tuck under 2.00 deck lip.
- **+Y up** (Unity). Run along **+local X**.

## Hard meters
- Mulch → high mouth vertical = **1.91 m** (≤ 2.00).
- Mouth-center span = **1.31 m**.
- Horizontal run ≈ **5.0 m**.
- **One sleeve** — shell R=0.48, bed R=0.36, same centerline whole run.

## Materials (bible §3)
- Shell / lips: `#F5D547` (lip hotter)
- Bed: darker yellow grip
- Steel mouth hoops only: `#B8C0C8` — no climb-blue

## Do not
- Scale `PGK_Slide_Tube90` or `Mega_SlideTube`
- Split shell/bed paths or add a second pivot
- Curb lips that kill 1.05 m trail under-clear

## Engineer
`Assets/Art/Props/Playground/PGK_Slide_TubeDeck_2m.fbx` — placer bind for west/deck tubes (Ororo / PR #9).
