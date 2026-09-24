# Tag — local playtest (Amaterasu)

## Open

1. Unity Hub → open `C:\Users\Zubal\Dev\Tag-game-playtest` (6000.3.x / 6000.0.x).
2. Open scene **Play** (`Assets/Scenes/Play.unity`) → **Play**.
3. Optional first-time art: **Tag → Ensure URP Pipeline**, then **Tag → Setup Hub Visuals**.

Branch: `cursor/playground-campus-zones-afc4` (campus kit tip; WIP backup `backup/amaterasu-wip-20260923`). Deeper notes: `Docs/MOVEMENT.md` / `Docs/PLAY-SLICE.md`.

## Stack snapshot

| Piece | What you get |
|-------|----------------|
| **TP cam** | `TpsMoveCamera` orbit/follow (~boom −5.2), soft collision, FOV by MoveState + slight speed look-ahead (`lookAheadMax` ~0.9, `speedFovBoostMax` ~3°) |
| **Hier dummy** | `DummyAvatarBinder` prefers `*_Hier_Hi` FBX → flat HiPoly → Navy Spade primitive (foam + polymer panels + matte joints; palette aligned with `Tools/Tag/build_mannequin_hier.py` — Tan runner / Orange It); `DummyLocomotor` swings limbs when bindable UpperArm/UpperLeg hierarchy exists |
| **Modes** | F1 Hot Potato / F2 Least It / F3 Trail Tag / **F4 Free play** (`TagModeController` SetMode + StartRound). Free play still transfers It on punch and does not end on a timer. |
| **HUD** | `SpeedEnergyHUD` (local P0): km/h + readable verb (RUN/SLIDE/DASH/WALL/CLIMB/LAND/…), **dash cooldown bar** (cyan, ready or seconds left; jet bar only if `enableJet`), ski flag, controls cheat-sheet, mode + phase + who is It, HP fuse (pulse when It ≤ warnSec), Least It times (brief all-standings flash); **LEAD** (mint) / **LAG** (coral) on local standings; **TAG flash** YOU'RE IT / YOU'RE FREE; **It compass** (flee / not It) + **Prey compass** (hunt / It → nearest alive); both pulse <12 m w/ distinct tints; cam bearing + m |
| **Void / XZ** | `VoidRespawn`: Y < −20 **or** mega-park XZ AABB (+~20 m) → nearest `LocalPlayerSpawner` pad; clear ragdoll/stun, zero vel, ~1 s punch i-frames |
| **Playground** | West play places (soft-play 14, 9.75 and astro loft 14, 44.25) link along **BARS W** (x=11): monkey segments that stop at the ski spines, with the merry-go-round's east apron facing the middle run. East bunker/keep (army 58, 9.75 and knight 58, 44.25) link along **BARS E** (x=61) into a fenced kickball field (west side open) and the swing set. Hopscotch SW, SE, and NE. South crawl ring, north tube ring, figure-8 wall-runs. After pull: **CutArenaBootstrap** Rebuild / **PgkLandmarkPlacer → Place**. |
| **Colliders** | `StaticPropColliders.EnsureStaticColliders` after dress/place so HiPoly/PGK toys keep Mesh/Box collision |
| **Trail Tag** | Wide bright light-cycle walls (mega-park WorldScale 10); Stay + Default-layer triggers so RB motor still eliminates; near-miss **TRAIL!** <4.5 m foreign; elim **OUT!** / TRAIL HIT |
| **SFX** | `TagSfx`: Resources/Audio clips when present, else procedural one-shots (punch, It, ski/jet, slide, lunge whoosh, jump) |
| **Ski spines** | Thicker mega-park ski spines (~3 m wide / 0.12 m thick) + zone approach ramps (pad → nearest spine); denser PGK connectors (`CutArenaBootstrap.BuildSkiSpines`) |
| **Ski crest** | Tribes leave: outward ski launch factor **1.0**, threshold `skiLaunchLeaveDot` ~0.12; DummyLocomotor air loft tell |
| **Punch** | Connect = loud kick + hold arm; miss = soft blip + limp arm (`TagSfx` / DummyLocomotor / `PunchHitbox`) |
| **Lunge** | It-only MMB: ~16 m/s, ~0.20 s, **CD ~1.0 s**; TP whip→settle via `LungeProgress` |
| **Slide** | Crouch+speed: carry entry speed + friction decay only (**no** enter boost) |
| **Air dash** | Visual whip + cyan trail; **30 s CD**; **Q / Left Alt** (in air); short planar burst (PlayerInputReader.airDashKey) |
| **Jump / land** | Fixed height (`jumpSpeed` launch, not speed-tied / additive); coyote ~0.10 s, buffer ~0.16 s; hard land → LandStun; DummyLocomotor firmer land squash |
| **Motor knobs** | Live on `Assets/Resources/TagArena/MovementConfig.asset` (`Resources.Load` `TagArena/MovementConfig`); recreate via **Tag → Create MovementConfig Asset** (won't overwrite) |
| **Mantle / climb / glide / bounce** | Stickier mega-park mantle + wall-climb, fairer super-glide window, punchier wall bounce (TP vault/climb/glide/kick tells) |
| **AI** | `DummyPatrol`: It chase + punch sync to `PunchHitbox` reach/cone; It lunges in the band just outside punch reach; both sides hop when a deck is above (chase) or It is close (flee) so a playground lip is not a dead stop; mild chase strafe outside close range; not-It flee with lead/strafe (wander when far); Least It: It prefers low TimeAsIt leaders, non-It clusters with non-It allies; drives `PlayerMotor` via input |

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
| **Air dash** | **Q / Left Alt (in air)** — ~0.1 s, cyan trail, ~30 s cooldown. Not a jet. |
| Mode hotkeys | F1 / F2 / F3 / **F4 free play** |

Punch is **not** a contact aura — only active punch hits transfer It (`PunchHitbox`).

## Tube pieces left out (measured)

`Mega_SlideTube` and `PGK_Slide_Tube90` are still not placed. Tube90 is a vertical elbow about 3.45 m tall, so grounding the feet puts the high mouth ~1.4 m above the 2.00 deck. Mega_SlideTube's floor rises ~3.4 m over ~10 m. Seating either high end on a deck buries the low mouth. Crawls are `Toy_TunnelTube` (play places + north ring) and `Mega_CrawlTunnel` (bunkers + south ring).

## What to look at next (human eye)

1. **Bar lanes.** West run x=11 (segments at z 12.6, then 22–30.4, then 40.2) should miss the pirate mast, the EW spines, and the astro spiral. East run x=61 should be the open west side of kickball, not on the swing bays. You cross the ski spines on foot between segments.
2. **Run.** Spawn SW → hopscotch SW → south bar → soft-play tubes/slide → cross the south spine → middle bars → merry (east apron) → cross the north spine → north bar → astro loft. East mirror: army crawl → bars → kickball (open west) → swings → bars → knight.
3. Spiral entrance still overlaps the 1.60 deck by about 0.25 m and should miss both east posts.
4. North plastic mouth (tube street at z=-4, mouth at z=-3) should read as a door: about 0.31 m into the rim, about 0.28 m short of the stair. End caps about 0.36 m.
5. Three-tile slide pit: far edge about 0.4 m short of the ski spine. Say if that runout still feels short.
6. Army crawl is shifted to local (−2, −4.5): about 1 m south of the stair, west edge short of Spawn_SE. Knight's copy should miss Spawn_NE.
7. Kickball fence is north, east, and behind the south goal. South fence world z≈19.75, just off the south spine. West side stays open.
8. Feel, unchanged: slide decays only, jump height is not speed-tied, air dash is the cyan trail with a 30 s cooldown.

## Tag handoff / AI (code)

- HUD mode line uses ASCII  |  separator; center MODE flash lists F1-F4.

## Tag handoff / AI (code, this pass)

- Punch transfer: PunchHitbox -> TagModeController.OnSuccessfulPunch -> TransferIt -> ItController.SetIt.
- SetIt(true) plays become-It SFX, calls PlayerMotor.NotifyBecameIt() (anim/HUD listeners), and pulses DummyLocomotor.PlayTagFlinch on the new It.
- Victim also flinches via ReceiveTagHit. HUD already flashes YOU'RE IT / YOU'RE FREE from IsIt edges.
- DummyPatrol Retargets immediately on **gain and lose** It (chase prey / flee new It without waiting for decisionHz).

## Feel check (code, not a Unity play)

Slide keeps entry planar speed: `SlideMove` only applies friction (softer downhill) and clamps to `_slideStartSpeed`. The punch +8% speed buff is skipped while `State == Slide`, then the same cap is applied again. `slideDownhillAccel` is 0 and unused. Jump sets `v.y` from `jumpSpeed` / fatigue, not from horizontal speed. Air dash is a short planar burst with `airDashCooldown` 30 and a cyan trail on `DummyLocomotor` (trail updates even if the limb bind fails). The near-zero speed floor inside `EnterSlide` cannot run: crouch only enters a slide at `slideEntrySpeed` (7.5).

Wall-run and wall-climb set a latch on exit (timeout, jump-off, or lost contact). The latch clears on the ground or after ~0.15 s with no wall hit, so air accel cannot restart the timer on the same surface. Climb up-speed (`climbSpeed` 6, decay from 0.16 s, slip × 3.5) reverses before `climbMaxHeight`; the old 7.8 / 0.40 curve hit the height cap at ~0.42 s while still going up, and `ClimbHeightUsed` never cleared on landing. Sprint stays 12 m/s, ski max 24 m/s (run was already raised; ski still wins). Jet stays off.

## Human verify next

No Unity play on this pass. After pull, open **Play**:

1. Sprint, hold Ctrl: slide should not speed up on entry. Down a slide, it should last longer and still not go faster than the speed you had at the crouch.
2. Jump from a walk and from a sprint: same height. Hold Ctrl in the air: you should drop faster than a normal fall.
3. Air dash (Q): short cyan streak, then the HUD dash bar counts ~30 s. RMB should not jet.
4. Wall-run a figure-8 panel: you should slide down and fall off. You should not re-stick until you leave the wall or land. Climb a net: rise, then slide down. After you hit the ground you can climb again.
5. Run should show a knee bend. Hands should hang / swing forward, not fold back into the hips.
6. F4: HUD says Free play · Playing, punch still moves It, the round does not end. F2 returns to Least It.
7. Let the dummy be It on a deck: it should hop toward you and lunge when it is close, not only run into the wall.

## Known leftovers

- Prefab/mat dirt after Hub visuals / URP regen — do not commit unless intentional.
- Flat HiPoly mannequins may skip hierarchical `DummyLocomotor` binds (primitive / bindable-bone path is the readable tell).
- Legacy contact `TryTag` radius still exists on motor; play modes use punch transfer.
- Trail Tag / Hot Potato polish and AI still party-slice rough.
- Do not hand-author `TagURP*.asset` YAML; use **Ensure URP Pipeline**.