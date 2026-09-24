# Tag Ã¢â‚¬â€ local playtest (Amaterasu)

## Open

1. Unity Hub Ã¢â€ â€™ open `C:\Users\Zubal\Dev\Tag-game-playtest` (6000.3.x / 6000.0.x).
2. Open scene **Play** (`Assets/Scenes/Play.unity`) Ã¢â€ â€™ **Play**.
3. Optional first-time art: **Tag Ã¢â€ â€™ Ensure URP Pipeline**, then **Tag Ã¢â€ â€™ Setup Hub Visuals**.

Branch: `cursor/playground-campus-zones-afc4` (integration tip; map 68edd8f + PR #11 tag-loop-kite-cam). Deeper notes: `Docs/MOVEMENT.md` / `Docs/PLAY-SLICE.md`.

## Stack snapshot

| Piece | What you get |
|-------|----------------|
| **TP cam** | `TpsMoveCamera` orbit/follow (~boom -5.2), soft collision, FOV by MoveState + slight speed look-ahead (`lookAheadMax` ~0.9, `speedFovBoostMax` ~3deg). Look-ahead **direction** is smoothed (a brake used to yaw the aim point in one frame). Strafe roll is lighter so the boom does not orbit and snap back. |
| **Hier dummy** | `DummyAvatarBinder` prefers `*_Hier_Hi` FBX -> flat HiPoly -> Navy Spade primitive (foam + polymer panels + matte joints; palette aligned with `Tools/Tag/build_mannequin_hier.py` - Tan runner / Orange It); `DummyLocomotor` swings limbs when bindable UpperArm/UpperLeg hierarchy exists. It hat grows with camera distance (clamped) and has a tall beacon so it reads across a fort. |
| **Modes** | F1 Hot Potato / F2 Least It / F3 Trail Tag / **F4 Free play** (`TagModeController` SetMode + StartRound). Free play still transfers It on punch and does not end on a timer. |
| **HUD** | `SpeedEnergyHUD` (local P0): km/h + readable verb (RUN/SLIDE/DASH/WALL/CLIMB/LAND/...), **dash cooldown bar** (cyan, ready or seconds left; jet bar only if `enableJet`), ski flag, controls cheat-sheet, mode + phase + who is It. Hot Potato: fuse line **and** top-center **FUSE** pulse for everyone inside `warnSec` (not only when you are It). Least It: clock + **lowest wins** on the mode line, **WINNING (least)** / **BEHIND (more It)**; brief all-standings flash; **LEAD** (mint) / **LAG** (coral). **TAG flash** ~0.85 s names who It moved to (YOU'RE IT / YOU'RE FREE); It hat pops on handoff. **It compass** (flee) + **Prey compass** (hunt); both pulse <12 m w/ distinct tints; cam bearing + m |
| **Void / XZ** | `VoidRespawn`: Y < -20 **or** mega-park XZ AABB (+~20 m) -> nearest `LocalPlayerSpawner` pad; clear ragdoll/stun, zero vel, ~1 s punch i-frames. **F1-F4 / rematch** also `ForceRecover`, cancel land-stun, and place every pawn on a pad (P1->pad 0) so a mode switch does not resume a ragdoll in the void. |
| **Playground** | West play places (soft-play 14, 9.75 and astro loft 14, 44.25) link along **BARS W** (x=11). The merry apron now runs out under that lane (world x=11 at z 23 and 25). A ground beam lane sits at x=13.5. East bunker/keep link along **BARS E** (x=62.5), on the open west edge of kickball, with a beam lane at x=60.5. Play places have a rung ladder on the deck shoulder. Hopscotch SW runs east-west at (4.5, 9); SE and NE stay north-south. South crawl ring, north tube ring, figure-8 wall-runs. After pull: **CutArenaBootstrap** Rebuild / **PgkLandmarkPlacer -> Place**. |
| **Colliders** | `StaticPropColliders.EnsureStaticColliders` after dress/place so HiPoly/PGK toys keep Mesh/Box collision |
| **Trail Tag** | Wide bright light-cycle walls (mega-park WorldScale 10); Stay + Default-layer triggers so RB motor still eliminates; near-miss **TRAIL!** <4.5 m foreign; elim **OUT!** / TRAIL HIT, then a persistent **OUT / waiting** line while you are frozen and the round is still going. It ribbon is brighter and ~1.35x wider (line only; collider width unchanged). Self-grace is still age **and** distance. |
| **SFX** | `TagSfx` procedural tones when a Resources clip is missing. `AudioCuePlayer` (round, trail elim, ragdoll, UI) falls back to those tones instead of staying silent. Air dash is a shorter higher whoosh than the grounded lunge. Soft land (below stun) thuds; hard land still uses `MoveAnimDriver`. Rematch from the round-end card with **R**. |
| **Ski spines** | Thicker mega-park ski spines (~3 m wide / 0.12 m thick) + zone approach ramps (pad -> nearest spine); denser PGK connectors (`CutArenaBootstrap.BuildSkiSpines`) |
| **Ski crest** | Tribes leave: outward ski launch factor **1.0**, threshold `skiLaunchLeaveDot` ~0.12; DummyLocomotor air loft tell |
| **Punch** | Connect = loud kick + hold arm; miss = soft blip + limp arm (`TagSfx` / DummyLocomotor / `PunchHitbox`) |
| **Lunge** | It-only MMB: ~16 m/s, ~0.20 s, **CD ~1.0 s**; TP whipÃ¢â€ â€™settle via `LungeProgress` |
| **Slide** | Crouch+speed: carry entry speed + friction decay only (**no** enter boost) |
| **Air dash** | Visual whip + cyan trail; **30 s CD**; **Q / Left Alt** (in air; airborne MMB also counts via motor); short planar burst. Grounded MMB = It lunge. |
| **Jump / land** | Fixed height (`jumpSpeed` launch, not speed-tied / additive); coyote ~0.10 s, buffer ~0.16 s; hard land -> LandStun; DummyLocomotor land squash plus a knee-buckle / arms-out recovery pose |
| **Motor knobs** | Live on `Assets/Resources/TagArena/MovementConfig.asset` (`Resources.Load` `TagArena/MovementConfig`); recreate via **Tag -> Create MovementConfig Asset** (won't overwrite) |
| **Mantle / climb / glide / bounce** | Stickier mega-park mantle + wall-climb, fairer super-glide window, punchier wall bounce (TP vault/climb/glide/kick tells) |
| **AI** | `DummyPatrol`: chase/flee turn (no 16deg snap, capped ~150deg/s) plus a half-second weave outside punch range so a juke is not tracked perfectly. Lead intercept capped at 0.18 s. A fast strafe across the fist (~7.5 m/s lateral) usually whiffs. Still hops for decks, lunges just outside reach, retargets the moment It changes hands. Punch cone matches `PunchHitbox`. Least It still prefers low TimeAsIt. |

## Controls (`PlayerInputReader`)

| Action | Default |
|--------|---------|
| Move | WASD |
| Look | Mouse |
| Ski | Left Shift |
| Sprint (when not skiing) | Left Shift / Left Alt |
| Jet | RMB (Mouse1) |
| Jump | Space |
| Crouch / slide gate | Ctrl or C (hold + speed; in air this is the fast fall) |
| Punch (It transfer) | LMB (Mouse0) or E |
| **Lunge (It only, grounded)** | **MMB (Mouse2)** |
| **Air dash** | **Q / Left Alt (in air)**; airborne **MMB** also dashes. ~0.1 s, cyan trail, ~30 s CD. Not a jet. Grounded MMB = It lunge. |
| Mode hotkeys | **F1** Hot Potato / **F2** Least It / **F3** Trail Tag / **F4** Free play (same four in the mode menu as 1/2/3/4). Each start recovers ragdoll and places pawns on spawn pads. |

Punch is **not** a contact aura Ã¢â‚¬â€ only active punch hits transfer It (`PunchHitbox`).

## Tube pieces left out (measured)

`Mega_SlideTube` and `PGK_Slide_Tube90` are still not placed. Tube90 is a vertical elbow about 3.45 m tall, so grounding the feet puts the high mouth ~1.4 m above the 2.00 deck. Mega_SlideTube's floor rises ~3.4 m over ~10 m. Seating either high end on a deck buries the low mouth. Crawls are `Toy_TunnelTube` (play places + north ring) and `Mega_CrawlTunnel` (bunkers + south ring).

## What to look at next (human eye)

1. **Bar drops.** West bars stay at x=11 (the mast owns anything nearer around z 12Ã¢â‚¬â€œ18). The merry apron tiles reach world x=11 at z 23 and 25, so the drop is on the apron. The z=24 joint is mulch on purpose Ã¢â‚¬â€ a tile there would bury the bar feet. East bars are at x=62.5, just inside the kickball rubber's open west edge, not on the swing bays. You still cross the ski spines on foot between segments.
2. **Run.** Spawn SW Ã¢â€ â€™ hopscotch SW (east-west) Ã¢â€ â€™ south bar Ã¢â€ â€™ soft-play tubes/slide, or the new rung ladder onto the deck Ã¢â€ â€™ cross the south spine Ã¢â€ â€™ middle bars or the beam lane (x=13.5) Ã¢â€ â€™ merry apron Ã¢â€ â€™ cross the north spine Ã¢â€ â€™ north bar Ã¢â€ â€™ astro loft. East mirror: army crawl Ã¢â€ â€™ bars (x=62.5) Ã¢â€ â€™ kickball (open west) Ã¢â€ â€™ swings Ã¢â€ â€™ bars Ã¢â€ â€™ knight, with the east beam lane (x=60.5) beside the bars.
3. Spiral entrance still overlaps the 1.60 deck by about 0.25 m and should miss both east posts.
4. North plastic mouth (tube street at z=-4, mouth at z=-3) should read as a door: about 0.31 m into the rim, about 0.28 m short of the stair. End caps about 0.36 m.
5. Three-tile slide pit was **not** lengthened. Exit mesh ends ~4.05 m from the tower; far tile edge is 6.25, so ~2.2 m of pad after the chute and ~0.4 m before the spine. A fourth tile would land on the spine. Say if that runout still feels short.
6. Army crawl is shifted to local (Ã¢Ë†â€™2, Ã¢Ë†â€™4.5): about 1 m south of the stair, west edge short of Spawn_SE. Knight's copy should miss Spawn_NE.
7. Kickball fence is north, east, and behind the south goal. South fence is local z=Ã¢Ë†â€™4.0 (world z=20): about 0.36 m off the south spine and about 0.13 m behind the goal back, not in the mouth. West side stays open.
8. **Spawns and corners.** SW toys sit west of Spawn_SW. NW toys sit south of Spawn_NW (the old cluster crossed the north map edge). Helmet (2.5, 51.2, scale 0.30) and foxhole (69.8, 2, scale 0.26, yaw 90) are the sizes that stay on the map and off the pads Ã¢â‚¬â€ say if they read too small. Knight shield is north of Spawn_NE at (66, 52.2).
9. This map pass did not touch slide, jump, or air dash. Slide still decays only, jump height is not speed-tied, air dash is Q / Left Alt (airborne MMB also dashes) with a 30 s cooldown. Wall-latch and land notes are in Feel check below.

## Tag handoff / AI (code)

- Punch transfer: PunchHitbox -> TagModeController.OnSuccessfulPunch -> TransferIt -> ItController.SetIt.
- SetIt(true) plays become-It SFX, calls PlayerMotor.NotifyBecameIt() (anim/HUD listeners), and pulses DummyLocomotor.PlayTagFlinch on the new It.
- Victim also flinches via ReceiveTagHit. HUD flashes YOU'RE IT / YOU'RE FREE and names LastFromId / LastToId. The hat pops on the rising edge.
- DummyPatrol Retargets immediately on **gain and lose** It, then weaves and caps turn rate so the new chase is a kite, not a snap.
- HUD mode line uses ASCII ` | ` separator; center MODE flash lists F1-F4. F1-F4 SetMode also syncs GameFlow menu cursor via PlayerPrefs, recovers ragdoll, and places pawns on pads.
- Trail Tag self-hit still needs both age and distance grace. Dodge i-frames do not ignore trails. Punch updates It brightness the same frame for every emitter mode. ItOnly still gates who emits.
- Round over draws a center card. **R** rematches from Play even when Boot's GameFlow is absent. **Q** returns to the menu only when GameFlow is loaded. "No winners" no longer plays the win sting.

## Feel check (code, not a Unity play)

Slide keeps entry planar speed: `SlideMove` only applies friction (softer downhill) and clamps to `_slideStartSpeed`. The punch +8% speed buff is skipped while `State == Slide`, then the same cap is applied again. `slideDownhillAccel` is 0 and unused. Jump sets `v.y` from `jumpSpeed` / fatigue, not from horizontal speed. Air dash is a short planar burst with `airDashCooldown` 30 and a cyan trail on `DummyLocomotor` (trail updates even if the limb bind fails). The near-zero speed floor inside `EnterSlide` cannot run: crouch only enters a slide at `slideEntrySpeed` (7.5).

Wall-run and wall-climb set a latch on exit (timeout, jump-off, or lost contact). The latch clears on the ground or after ~0.15 s with no wall hit, so air accel cannot restart the timer on the same surface. Climb up-speed (`climbSpeed` 6, decay from 0.16 s, slip Ãƒâ€” 3.5) reverses before `climbMaxHeight`; the old 7.8 / 0.40 curve hit the height cap at ~0.42 s while still going up, and `ClimbHeightUsed` never cleared on landing. Sprint stays 12 m/s, ski max 24 m/s (run was already raised; ski still wins). Jet stays off.

## Human verify next

No Unity play on this pass (no Unity / `dotnet` on the VM). After pull, open **Play**:

1. Sprint, hold Ctrl: slide should not speed up on entry. Down a slide, it should last longer and still not go faster than the speed you had at the crouch.
2. Jump from a walk and from a sprint: same height. Hold Ctrl in the air: you should drop faster than a normal fall.
3. Air dash (Q): short cyan streak, then the HUD dash bar counts ~30 s. RMB should not jet.
4. Wall-run a figure-8 panel: you should slide down and fall off. You should not re-stick until you leave the wall or land. Climb a net: rise, then slide down. After you hit the ground you can climb again.
5. Run steps (a plant, then a lift) rather than a constant skate. Hands stay forward of the hips. A hard landing buckles the knees and opens the arms, then stands back up. Q dash should reach the whip pose inside the short burst.
6. Hard brake or sharp turn: the camera should not whip with your velocity. Mouse look should still feel stuck to the mouse.
7. Tag the dummy: hat pops, flash says YOU'RE FREE and names who is It. When they tag you: YOU'RE IT and who it came from.
8. F1 while you are It: top-center FUSE appears inside the warn window even if you pass It away. F2: mode line shows seconds left and WINNING (least) / BEHIND (more It). F3: a foreign trail still eliminates; your own trail does not until the grace ends. F4: free play, punch still moves It, no timer. Each of F1Ã¢â‚¬â€œF4 should drop you on a spawn pad, including if you were ragdolled.
9. As It, a sharp strafe should make the dummy miss more often than it connects. As runner, you should be able to cut their flank instead of losing a straight race every time.
10. Q dash and a grounded It lunge should not sound the same. A short hop lands with a soft thud; a hard land is louder. Tag, round start, and a trail elim should make a tone even with no audio files imported.
11. From across a fort the orange hat and beacon should still read. Punch windup cocks the fist out, not into the hip.
12. When a round ends, a center card names the result. R starts the next round from a pad. In Trail Tag, after OUT you should see "waiting for the round" until the match ends.

## Known leftovers

- Prefab/mat dirt after Hub visuals / URP regen Ã¢â‚¬â€ do not commit unless intentional.
- Flat HiPoly mannequins may skip hierarchical `DummyLocomotor` binds (primitive / bindable-bone path is the readable tell).
- Legacy contact `TryTag` radius still exists on motor; play modes use punch transfer.
- AI weave/whiff still needs a human feel pass. No spectator camera: an eliminated player stays on their body with a waiting line.
- Do not hand-author `TagURP*.asset` YAML; use **Ensure URP Pipeline**.
