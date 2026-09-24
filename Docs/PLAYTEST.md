# Tag ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â local playtest (Amaterasu)

## Open

1. Unity Hub ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ open `C:\Users\Zubal\Dev\Tag-game-playtest` (6000.3.x / 6000.0.x).
2. Open scene **Play** (`Assets/Scenes/Play.unity`) ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ **Play**.
3. Optional first-time art: **Tag ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ Ensure URP Pipeline**, then **Tag ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ Setup Hub Visuals**.

Branch: `cursor/volume-punch-results-497c` (into `cursor/playground-campus-zones-afc4`). Deeper notes: `Docs/MOVEMENT.md` / `Docs/PLAY-SLICE.md`.

## Stack snapshot

| Piece | What you get |
|-------|----------------|
| **TP cam** | `TpsMoveCamera` orbit/follow (~boom -5.2), soft collision, FOV by MoveState + slight speed look-ahead (`lookAheadMax` ~0.9, `speedFovBoostMax` ~3deg). Look-ahead **direction** is smoothed (a brake used to yaw the aim point in one frame). Strafe roll is lighter so the boom does not orbit and snap back. |
| **Hier dummy** | `DummyAvatarBinder` prefers `*_Hier_Hi` FBX -> flat HiPoly -> Navy Spade primitive (foam + polymer panels + matte joints; palette aligned with `Tools/Tag/build_mannequin_hier.py` - Tan runner / Orange It); `DummyLocomotor` swings limbs when bindable UpperArm/UpperLeg hierarchy exists. It hat grows with camera distance (clamped) and has a tall beacon so it reads across a fort. Your own chase cam hides the beacon, slows the spin, and shortens the light so the hat is not a lens blocker. |
| **Modes** | F1 Hot Potato / F2 Least It / F3 Trail Tag / **F4 Free play** (`TagModeController` SetMode + StartRound). Free play still transfers It on punch and does not end on a timer. |
| **HUD** | `SpeedEnergyHUD` (local P0): km/h + readable verb (RUN/SLIDE/DASH/WALL/CLIMB/LAND/...), **dash cooldown bar** (cyan, ready or seconds left; jet bar only if `enableJet`), ski flag, controls cheat-sheet, mode + phase + who is It. Hot Potato: fuse line **and** top-center **FUSE** pulse for everyone inside `warnSec` (not only when you are It). Least It: clock + **lowest wins** on the mode line, **WINNING (least)** / **BEHIND (more It)**; brief all-standings flash; **LEAD** (mint) / **LAG** (coral). **TAG flash** ~0.85 s names who It moved to (YOU'RE IT / YOU'RE FREE); It hat pops on handoff. **It compass** (flee) + **Prey compass** (hunt); both pulse <12 m w/ distinct tints; cam bearing + m |
| **Void / XZ** | `VoidRespawn`: Y < -20 **or** mega-park XZ AABB (+~20 m) -> nearest `LocalPlayerSpawner` pad; clear ragdoll/stun, zero vel, ~1 s punch i-frames. **F1-F4 / rematch** also `ForceRecover`, cancel land-stun, and place every pawn on a pad (P1->pad 0) so a mode switch does not resume a ragdoll in the void. |
| **Playground** | Four hopscotch courts, one per corner. West run: spawn SW, mushroom step, hopscotch SW, south bar, soft-play, spine, bars or beams, merry (picnic on the south edge), spine, north bar, astro, hopscotch NW. East run: army, bars onto open kickball, swings (fall tiles clear of the north fence), bars, knight, hopscotch NE. Crash bowl stays open. Spawn_NW faces southeast; the torso is north of that pad and off the exit. After pull: **CutArenaBootstrap** Rebuild / **PgkLandmarkPlacer -> Place**. |
| **Colliders** | `StaticPropColliders.EnsureStaticColliders` after dress/place so HiPoly/PGK toys keep Mesh/Box collision |
| **Trail Tag** | Wide bright light-cycle walls (mega-park WorldScale 10); Stay + Default-layer triggers so RB motor still eliminates; near-miss **TRAIL!** <4.5 m foreign; elim **OUT!** / TRAIL HIT, then a persistent **OUT / waiting** line while you are frozen and the round is still going. It ribbon is brighter and ~1.35x wider (line only; collider width unchanged). Self-grace is still age **and** distance. |
| **SFX** | `TagSfx` procedural tones when a Resources clip is missing. `AudioCuePlayer` (round, trail elim, ragdoll, UI) falls back to those tones instead of staying silent. Air dash is a shorter higher whoosh than the grounded lunge. Soft land (below stun) thuds; hard land still uses `MoveAnimDriver`. Rematch from the round-end card with **R**. |
| **Ski spines** | Thicker mega-park ski spines (~3 m wide / 0.12 m thick) + zone approach ramps (pad -> nearest spine); denser PGK connectors (`CutArenaBootstrap.BuildSkiSpines`) |
| **Ski crest** | Tribes leave: outward ski launch factor **1.0**, threshold `skiLaunchLeaveDot` ~0.12; DummyLocomotor air loft tell |
| **Punch** | Connect = loud kick + hold arm; miss = soft blip + limp arm (`TagSfx` / DummyLocomotor / `PunchHitbox`). Local It cocks the fist while the punch buffer is armed (same tell as dummy telegraph). |
| **Lunge** | It-only MMB: ~16 m/s, ~0.20 s, **CD ~1.0 s**; TP whipÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢settle via `LungeProgress` |
| **Slide** | Crouch+speed: carry entry speed + friction decay only (**no** enter boost) |
| **Air dash** | Visual whip + cyan trail; **30 s CD**; **Q / Left Alt** (in air; airborne MMB also counts via motor); short planar burst. Grounded MMB = It lunge. |
| **Jump / land** | Fixed height (`jumpSpeed` launch, not speed-tied / additive); coyote ~0.10 s, buffer ~0.16 s; hard land -> LandStun; DummyLocomotor land squash plus a knee-buckle / arms-out recovery pose |
| **Motor knobs** | Live on `Assets/Resources/TagArena/MovementConfig.asset` (`Resources.Load` `TagArena/MovementConfig`); recreate via **Tag -> Create MovementConfig Asset** (won't overwrite) |
| **Mantle / climb / glide / bounce** | Stickier mega-park mantle + wall-climb, fairer super-glide window, punchier wall bounce (TP vault/climb/glide/kick tells) |
| **AI** | `DummyPatrol`: chase/flee turn (no 16deg snap, capped ~150deg/s) plus a half-second weave outside punch range so a juke is not tracked perfectly. Lead intercept capped at 0.18 s. A fast strafe across the fist (~7.5 m/s lateral) usually whiffs. Before the swing the dummy cocks its arm (~0.34 s, shorter on a hot fuse) and cancels if you leave the fist. Punch connect kicks the attacker's camera and a lighter kick on the victim's (no hitstop). Still hops for decks (probe + panic), lunges just outside reach, retargets the moment It changes hands. Punch cone matches `PunchHitbox`. Least It still prefers low TimeAsIt. |

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

Punch is **not** a contact aura ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â only active punch hits transfer It (`PunchHitbox`).

## Tube pieces left out (measured)

`Mega_SlideTube` and `PGK_Slide_Tube90` are still not placed. Tube90 is a vertical elbow about 3.45 m tall, so grounding the feet puts the high mouth ~1.4 m above the 2.00 deck. Mega_SlideTube's floor rises ~3.4 m over ~10 m. Seating either high end on a deck buries the low mouth. Crawls are `Toy_TunnelTube` (play places + north ring) and `Mega_CrawlTunnel` (bunkers + south ring).

## Human Play path

Walk these in order. Spines and the crash cross stay empty. This pass did not move the crash torso, the net beam, or the ninja rail.

1. **SW exit.** Spawn_SW faces northeast. Mushroom steps at (4.5, 6.94) sit beside that line, between the pad and hopscotch SW. Feet are on the ground.
2. **West loop.** Hopscotch SW → south bar (x=11) → soft-play (tubes, slide, rung, net beam, or the tall net) → cross the south spine on foot → middle bars or the beam lane → merry apron into the bars. Picnic table is on the merry's south edge, not on the apron. Cross the north spine → north bar → astro.
3. **NW exit.** Spawn_NW faces southeast (yaw 135), into astro. The crash torso at (12, 52.12) is 1.0 m north of the pad and about 3.6 m east of the pad's east edge, so it is not on that exit. Hopscotch NW is at (3.2, 42), south of the pad, west of astro.
4. **East loop.** Spawn_SE → hopscotch SE → army crawl or the 1.8 m ladder → bars at x=62.5 into the open west side of kickball → swings. Swing fall tiles should clear the kickball north fence by about 0.16 m. Continue the bars → knight → hopscotch NE. Spawn_NE faces southwest, into the keep. The shield is north of that pad.
5. **Crash.** Cross the bowl east-west. Both lips and the middle should be open lawn.
6. Feel was not edited. Slide still decays only, jump height is not speed-tied, air dash is Q / Left Alt (airborne MMB also dashes) with a 30 s cooldown.

## Tag handoff / AI (code)

- Punch transfer: PunchHitbox -> TagModeController.OnSuccessfulPunch -> TransferIt -> ItController.SetIt.
- SetIt(true) plays become-It SFX, calls PlayerMotor.NotifyBecameIt() (anim/HUD listeners), and pulses DummyLocomotor.PlayTagFlinch on the new It.
- Victim also flinches via ReceiveTagHit. HUD flashes YOU'RE IT / YOU'RE FREE and names LastFromId / LastToId. The hat pops on the rising edge.
- DummyPatrol Retargets immediately on **gain and lose** It, then weaves and caps turn rate so the new chase is a kite, not a snap.
- Flee panic hop uses dy 0.9 (was 0.55, below ConsumeHop minDy 0.85, so it never fired). Trail Tag mode line shows **SUDDEN DEATH** when the cap/stall failsafe trips.
- HUD mode line uses ASCII ` | ` separator; center MODE flash lists F1-F4. F1-F4 SetMode also syncs GameFlow menu cursor via PlayerPrefs, recovers ragdoll, and places pawns on pads.
- Trail Tag self-hit still needs both age and distance grace. Dodge i-frames do not ignore trails. Punch updates It brightness the same frame for every emitter mode. ItOnly still gates who emits.
- Results card: solo headline is YOU WIN, YOU LOSE, or DRAW, and it names the mode and the winners. Couch with a split result stays ROUND OVER. Rematch click plays UiConfirm. A loss line does not say "winner", so it does not play the win tone. Hot Potato HUD says YOU HOLD THE FUSE when you are It; Trail marks YOU and OUT waiting for round; sudden death explains the next hit. **R** / Rematch starts once; Boot does not also rematch. A second StartRound inside 0.05 s is ignored. **Q** or **Esc** returns to Boot. F1-F4 or R leave pause and round-end so the countdown is not frozen. Esc on player-count and mode screens steps back to Boot. Keys 1-4: you + 1 bot, or 2-4 humans with the bot off. Direct Play pauses on Esc; Q loads Boot. Pause zeroes look and punch, clears a buffered jump; HUD flashes use scaled time so they freeze. Countdown card names the mode (3-2-1). Local punch windup flares the elbow (0.12 s unchanged). Look sensitivity is a five-step stub (default 1.8); Boot and pause open it; Left/Right change it. Direct-Play pause notes Countdown frozen. Playground music stays silent when the clip is null. Ski entry still uses TagSfx.EnsureSource.
- Look sensitivity stays in PlayerPrefs (`Tag.LookSensitivity`, default 1.8) and loads when the camera wakes. Controls help is on Boot, the pause menu, and Direct Play (H). Air dash can be Q (default), V, or Mouse4; Left Alt still dashes, and that choice is saved. A jump or crouch held through pause is not a new press on resume. A dash or punch tap during pause does not fire later. The first Boot visit says Play is you and one bot in Least It until you start.
- Punch can be LMB (default), F, or Mouse3; E still punches. That choice is `Tag.PunchKey`. Volume and mute stay on the Boot and pause Audio card (`AudioMaster`, default 0.8). On the results card the cursor is unlocked and gameplay input is zeroed, so a Rematch click does not punch or yaw. StartRound locks the cursor and ends any swing that was still out. Windup stays 0.12 s. Slide, jump, dash, and jet numbers are unchanged.

## Feel check (code, not a Unity play)

Slide keeps entry planar speed: `SlideMove` only applies friction (softer downhill) and clamps to `_slideStartSpeed`. The punch +8% speed buff is skipped while `State == Slide`, then the same cap is applied again. `slideDownhillAccel` is 0 and unused. Jump sets `v.y` from `jumpSpeed` / fatigue, not from horizontal speed. Air dash is a short planar burst with `airDashCooldown` 30 and a cyan trail on `DummyLocomotor` (trail updates even if the limb bind fails). The near-zero speed floor inside `EnterSlide` cannot run: crouch only enters a slide at `slideEntrySpeed` (7.5).

Wall-run and wall-climb set a latch on exit (timeout, jump-off, or lost contact). The latch clears on the ground or after ~0.15 s with no wall hit, so air accel cannot restart the timer on the same surface. Climb up-speed (`climbSpeed` 6, decay from 0.16 s, slip ÃƒÆ’Ã¢â‚¬â€ 3.5) reverses before `climbMaxHeight`; the old 7.8 / 0.40 curve hit the height cap at ~0.42 s while still going up, and `ClimbHeightUsed` never cleared on landing. Sprint stays 12 m/s, ski max 24 m/s (run was already raised; ski still wins). Jet stays off.

## Human verify next

No Unity play on this pass (no Unity / `dotnet` on the VM). After pull, open **Play**:

1. Sprint, hold Ctrl: slide should not speed up on entry. Down a slide, it should last longer and still not go faster than the speed you had at the crouch.
2. Jump from a walk and from a sprint: same height. Hold Ctrl in the air: you should drop faster than a normal fall.
3. Air dash (Q): short cyan streak, then the HUD dash bar counts ~30 s. RMB should not jet.
4. Wall-run a figure-8 panel: you should slide down and fall off. You should not re-stick until you leave the wall or land. Climb a net: rise, then slide down. After you hit the ground you can climb again.
5. Run steps (a plant, then a lift) rather than a constant skate. Hands stay forward of the hips. A hard landing buckles the knees and opens the arms, then stands back up. Q dash should reach the whip pose inside the short burst.
6. Hard brake or sharp turn: the camera should not whip with your velocity. Mouse look should still feel stuck to the mouse.
7. Tag the dummy: hat pops, flash says YOU'RE FREE and names who is It. When they tag you: YOU'RE IT and who it came from.
8. F1 while you are It: top-center FUSE appears inside the warn window even if you pass It away. F2: mode line shows seconds left and WINNING (least) / BEHIND (more It). F3: a foreign trail still eliminates; your own trail does not until the grace ends. F4: free play, punch still moves It, no timer. Each of F1ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“F4 should drop you on a spawn pad, including if you were ragdolled.
9. As It, a sharp strafe should make the dummy miss more often than it connects. As runner, you should be able to cut their flank instead of losing a straight race every time.
10. Q dash and a grounded It lunge should not sound the same. A short hop lands with a soft thud; a hard land is louder. Tag, round start, and a trail elim should make a tone even with no audio files imported.
11. From across a fort the orange hat and beacon should still read. Your punch windup should flare the elbow out beside the head within the same short windup. Holding LMB as It should still cock the fist before the swing.
12. When a round ends, a center card names the result. One R (or Rematch click) starts the next round from a pad (a second R in the same moment does not restart it again). Esc pause, then F1: the countdown should move. Q, Esc, or Menu from the card returns to Boot, including a direct Play scene. In Trail Tag, after OUT you should see "waiting for the round" until the match ends.
13. Your own hat should sit on your head without a tall spike in the camera. The dummy's beacon should still read from across a fort. When the dummy is It, you should see the arm cock before the punch, and leaving that range should cancel it. Getting tagged should nudge your camera. Pause resume and quit should click.
14. Esc during Play pauses. Mouse look should stop and a click on Resume should not punch. A jump you buffered just before Esc should not fire when you resume. HUD flashes should freeze while paused. Direct Play (opened without Boot) still pauses, and Q loads Boot. Left/Right on that pause card changes look speed. Boot's player and mode screens: Esc steps back. Row 1 is you + 1 bot. Rows 2-4 are humans and the bot stays off. The countdown names the mode and counts 3, then 2, then 1. No music bed is expected. A ski entry still makes a tone.
15. Boot and the pause menu have Look sensitivity. Default should feel like the current camera. Left/Right or the arrows step Low, Lower, Default, Higher, High. Esc leaves the panel without unpausing if you opened it from pause. Quit and relaunch: the same step should still be selected. When a solo round ends, the card should say YOU WIN, YOU LOSE, or DRAW, and name the mode and the winners. Rematch and Menu still click.
16. First Boot visit should say Play is you and one bot in Least It. Controls lists the real keys. Left/Right on that card steps the air dash key (Q, V, Mouse4). Alt still dashes. Default Q should feel the same. Holding jump through Esc should not hop when you resume. Direct Play pause: H opens the same card.
17. Controls steps punch (LMB, F, Mouse3) with Up/Down or Punch buttons. E still punches. Default LMB should feel the same. Volume stays on the Audio card (Off, Low, Med, Default, Max, M mute). On the results card, clicking Rematch should not punch and the mouse should not turn you. The next countdown should not still be swinging. Direct Play results should unlock the cursor the same way. Direct Play pause: H still changes dash and punch; - / + changes that same volume; M mutes.

## Known leftovers

- Prefab/mat dirt after Hub visuals / URP regen ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â do not commit unless intentional.
- Flat HiPoly mannequins may skip hierarchical `DummyLocomotor` binds (primitive / bindable-bone path is the readable tell).
- Legacy contact `TryTag` radius still exists on motor; play modes use punch transfer.
- AI weave/whiff still needs a human feel pass. No spectator camera: an eliminated player stays on their body with a waiting line. Playground music stays silent: `music_playground_bed_loop.wav` is meta only, so PlayMusic returns. No hitstop.
- Do not hand-author `TagURP*.asset` YAML; use **Ensure URP Pipeline**.

## Audio
- Master volume / mute: Boot or pause **Audio**, Left/Right steps Off/Low/Med/Default/Max, **M** mute. Saved in PlayerPrefs. Drives AudioListener.volume so TagSfx + music bed share one gate.
