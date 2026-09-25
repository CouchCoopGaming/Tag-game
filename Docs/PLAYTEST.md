# Tag -> local playtest (Amaterasu)

## Open

1. Unity Hub -> open `C:\Users\Zubal\Dev\Tag-game-playtest` (6000.3.x / 6000.0.x).
2. Open scene **Play** (`Assets/Scenes/Play.unity`) -> **Play**.
3. Optional first-time art: **Tag -> Ensure URP Pipeline**, then **Tag -> Setup Hub Visuals**.

Branch: `cursor/character-anim-hier-spawn-238c` on campus tip `da44b0d`. Results focus, Boot/pause keys, mute-from-play, the readable dash bar, CloseMenuPanels, and TubeDeck on soft-play, astro, and army are already on that tip. This branch does not edit the placer. Deeper notes: `Docs/MOVEMENT.md` / `Docs/PLAY-SLICE.md`.

Already on that tip (do not re-test as new): 2-frame look/punch resume gate (ResumeInputGate + ArmLookPunchGate), bots hold on countdown/results/idle, punch DropSwing, HUD mute chip, M/N during play, dash bar dark track / cyan fill, pause keys 1-5, AudioMaster, Controls/Look/Audio row highlight, panels close when play/results/Boot starts, first-run line survives Couch/Mode and clears after a round, who-plays Esc returns to Boot on Couch, mode select Esc steps back and keeps the player count, Boot pause H opens Controls only, first countdown says WASD move and Shift sprint, results keys 1-2 highlight only and Enter or Space confirms after the arm, punch-tell floor 0.22s with strafe cancel, TRAIL soft warn ~6.6 m (avoid ~9.4 m), It hat beacon, TubeDeck on soft-play, astro, and army (yaw 90, stem 0, z+5.90, mesh pitched -90 X). Mouth center 1.91 m. Knight keeps the straight chute. Mega/Tube90 still out. No MasterVolume type.

Already on this branch: Player and the bot prefer the curved Hier HiPoly mannequin over the flat Dummy_Runner / Dummy_It prefab. Run bends only the recovery knee. Dash whip is arm pitch. Slide is a low crouch. Land buckles. Punch connect stays in front of the chest.

Already on this branch: Run arms oppose the legs. The forward arm is the opposite thigh. Rearward swing stays short.

Already on this branch: A slide is a flat wedge. Punch connect is a long straight arm in front of the chest.

Already on this branch: A landing holds a short knee buckle, then eases into the run.

Already on this branch: An air dash holds the arm whip at the start, then the arms and legs ease toward a hang.

Already on this branch: Player and bot spawn the approved Tan Hier runner. It swaps to the Orange Hier mesh.

Already on this branch: A jump reaches both arms up and tucks the knees. A fall trails the arms back and lengthens the legs.

Already on this branch: A tag splits into two poses. The tagged runner guards. The new It lifts one knee.

Already on this branch: A climb is hand-over-hand. A wall run plants the wall hand and steps with the outer leg.

Already on this branch: At a standstill the arms hang slightly forward and out. That offset fades as the walk starts.

Already on this branch: A jump shows a long arm line and a knee tuck before the apex. A fall trails the arms before the landing.

Already on this branch: The run plant holds. The front thigh reaches farther than the back thigh, and only that knee bends.

Already on this branch: A slide's arms are a long low line. Elbows stay nearly straight.

Already on this branch: On a landing the arms come out to the sides for balance while the knees stay buckled, then ease back into the stride.

Already on this branch: A climb keeps both hands on a long line while they swap reach and pull. A wall run presses the wall hand with the stride, and the outer hand stays straight.

Already on this branch: The punch windup cocks the fist beside the head, clear of the chest. Windup time is still 0.12s.

Already on this branch: After an air dash the arms stay in the hang and ease into the fall or the run. They do not throw back a second time.

Already on this branch: A tag catch is a long V of arms in front of the chest, with both knees bent. It stays distinct from the new It's claim.

Already on this branch: Starting a run keeps the hands out of the hips. Resting arms do not pick up extra roll.

Already on this branch: Skiing eases into a lower glide with the arms out, then eases back into the run. Jet stays off.

Already on this branch: Leaving a wall run eases into the fall or the run. The wall hand does not snap off the wall.

Already on this branch: Leaving a climb eases into the fall or the run. The reaching hand does not snap off the wall.

Already on this branch: A grapple pull, only while the gate is on, reaches both arms in a long line with the legs long. The default gate stays off.

This delta: The new It raises one arm and holds the other out, with the chest open and one knee up. It should not match the tagged runner's two-arm V. The catch pose is unchanged.

## Stack snapshot

| Piece | What you get |
|-------|----------------|
| **TP cam** | `TpsMoveCamera` orbit/follow (~boom -5.2), soft collision, FOV by MoveState + slight speed look-ahead (`lookAheadMax` ~0.9, `speedFovBoostMax` ~3deg). Look-ahead **direction** is smoothed (a brake used to yaw the aim point in one frame). Strafe roll is lighter so the boom does not orbit and snap back. |
| **Hier dummy** | `DummyAvatarBinder` defaults Runner to `Dummy_Mannequin_Tan_Hier_Hi` and It to `Dummy_Mannequin_Orange_Hier_Hi`, then other `*_Hier_Hi`, flat HiPoly, and the Navy Spade primitive (foam + polymer panels + matte joints; palette aligned with `Tools/Tag/build_mannequin_hier.py`); `DummyLocomotor` swings limbs when bindable UpperArm/UpperLeg hierarchy exists. It hat grows with camera distance (clamped) and has a tall beacon so it reads across a fort. Your own chase cam hides the beacon, slows the spin, and shortens the light so the hat is not a lens blocker. |
| **Modes** | F1 Hot Potato / F2 Least It / F3 Trail Tag / **F4 Free play** (`TagModeController` SetMode + StartRound). Free play still transfers It on punch and does not end on a timer. |
| **HUD** | `SpeedEnergyHUD` (local P0): km/h + readable verb (RUN/SLIDE/DASH/WALL/CLIMB/LAND/...), **dash cooldown bar** (cyan, ready or seconds left; jet bar only if `enableJet`), ski flag, controls cheat-sheet, mode + phase + who is It. Hot Potato: fuse line **and** top-center **FUSE** pulse for everyone inside `warnSec` (not only when you are It). Least It: clock + **lowest wins** on the mode line, **WINNING (least)** / **BEHIND (more It)**; brief all-standings flash; **LEAD** (mint) / **LAG** (coral). **TAG flash** ~0.85 s names who It moved to (YOU'RE IT / YOU'RE FREE); It hat pops on handoff. **It compass** (flee) + **Prey compass** (hunt); both pulse <12 m w/ distinct tints; cam bearing + m |
| **Void / XZ** | `VoidRespawn`: Y < -20 **or** mega-park XZ AABB (+~20 m) -> nearest `LocalPlayerSpawner` pad; clear ragdoll/stun, zero vel, ~1 s punch i-frames. **F1-F4 / rematch** also `ForceRecover`, cancel land-stun, and place every pawn on a pad (P1->pad 0) so a mode switch does not resume a ragdoll in the void. |
| **Playground** | Four hopscotch courts, one per corner. West run: spawn SW, mushroom step, hopscotch SW, a low bench west of the climb net, south bar, soft-play (a bench west of the tube street, a ground slide east of the tube cap, a balance beam with mushrooms, a spring, and two hop tiles on the south apron, a climber dome west of that apron, a spring rider on the east shoulder, then rung to the 2.00 deck), spine, bars or beams, merry (west bench, south picnic, and a spring at (2.50, 24) on the west lawn), then a mushroom, spring, and two hop tiles north of merry and west of the bars, spine, north bar, arch to a mushroom/spring/hop cluster at (9.4, 45.0) then hopscotch NW, astro. East forts use that same 2.4 m rung opposite a 1.8 m ladder, plus a spiral climber (feet on the ground, top at 2.40) that the west forts do not have. East run: hopscotch SE, arch (with a mushroom/spring/hop cluster at (66.2, 7.5) south of that arch), army, bars onto open kickball (a low bench west of the bars, not in the field), swings (fall tiles clear of the north fence), bars, then knight to the west or the arch and a low bench into hopscotch NE. North of that arch, a net frame, mushroom steps, and a spring rider sit east of the bars. Crash bowl stays open. Spawn_NW faces southeast; the torso is north of that pad and off the exit. After pull: **CutArenaBootstrap** Rebuild / **PgkLandmarkPlacer -> Place**. |
| **Colliders** | `StaticPropColliders.EnsureStaticColliders` after dress/place so HiPoly/PGK toys keep Mesh/Box collision |
| **Trail Tag** | Wide bright light-cycle walls (mega-park WorldScale 10); Stay + Default-layer triggers so RB motor still eliminates; near-miss **TRAIL!** ~6.3 m foreign; elim **OUT!** / TRAIL HIT, then a persistent **OUT / waiting** line while you are frozen and the round is still going. It ribbon is brighter and ~1.35x wider (line only; collider width unchanged). Self-grace is still age **and** distance. |
| **SFX** | `TagSfx` procedural tones when a Resources clip is missing. `AudioCuePlayer` (round, trail elim, ragdoll, UI) falls back to those tones instead of staying silent. Air dash is a shorter higher whoosh than the grounded lunge. Soft land (below stun) thuds; hard land still uses `MoveAnimDriver`. Rematch from the round-end card with **R**. |
| **Ski spines** | Thicker mega-park ski spines (~3 m wide / 0.12 m thick) + zone approach ramps (pad -> nearest spine); denser PGK connectors (`CutArenaBootstrap.BuildSkiSpines`) |
| **Ski crest** | Tribes leave: outward ski launch factor **1.0**, threshold `skiLaunchLeaveDot` ~0.12; DummyLocomotor air loft tell |
| **Punch** | Connect = loud kick + hold arm; miss = soft blip + limp arm (`TagSfx` / DummyLocomotor / `PunchHitbox`). Local It cocks the fist while the punch buffer is armed (same tell as dummy telegraph). |
| **Lunge** | It-only MMB: ~16 m/s, ~0.20 s, **CD ~1.0 s**; TP whip->settle via `LungeProgress` |
| **Slide** | Crouch+speed: carry entry speed + friction decay only (**no** enter boost) |
| **Air dash** | Visual whip + cyan trail; **30 s CD**; **Q / Left Alt** (in air; airborne MMB also counts via motor); short planar burst. Grounded MMB = It lunge. |
| **Jump / land** | Fixed height (`jumpSpeed` launch, not speed-tied / additive); coyote ~0.10 s, buffer ~0.16 s; hard land -> LandStun; DummyLocomotor land squash plus a knee-buckle / arms-out recovery pose |
| **Motor knobs** | Live on `Assets/Resources/TagArena/MovementConfig.asset` (`Resources.Load` `TagArena/MovementConfig`); recreate via **Tag -> Create MovementConfig Asset** (won't overwrite) |
| **Mantle / climb / glide / bounce** | Stickier mega-park mantle + wall-climb, fairer super-glide window, punchier wall bounce (TP vault/climb/glide/kick tells) |
| **AI** | `DummyPatrol`: chase/flee turn (no 16deg snap, capped ~150deg/s) plus a half-second weave outside punch range so a juke is not tracked perfectly. Lead intercept capped at 0.18 s. A fast strafe across the fist (~7 m/s lateral) usually whiffs. Before the swing the dummy cocks its arm (~0.34 s, down to 0.22 s on a hot fuse) and cancels if you leave the fist. Punch connect kicks the attacker's camera and a lighter kick on the victim's (no hitstop). Still hops for decks (probe + panic), lunges just outside reach, retargets the moment It changes hands. Punch cone matches `PunchHitbox`. Least It still prefers low TimeAsIt. |

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

Punch is **not** a contact aura -> only active punch hits transfer It (`PunchHitbox`).

## Tube pieces left out (measured)

`PGK_Slide_TubeDeck_2m` is on soft-play, astro, and army, one each. Yaw 90, stem 0, pivot local (0, 0, 5.90). The mesh is pitched -90 X because the FBX rise is on Z; root-local mouth center is 1.91 m and the shell top is 2.48 m. Low mouth stays on the mulch. Knight keeps the straight chute. The west sleeve stays 3.28 m off the tube street, about 7 m off the ground `Toy_Slide`, and 7.2 m off the dome. Army's sleeve is 1.49 m off the spiral climber. `Mega_SlideTube` and `PGK_Slide_Tube90` are still not placed and are not scaled. Crawls are `Toy_TunnelTube` (play places + north ring) and `Mega_CrawlTunnel` (bunkers + south ring).

## Human Play path

Walk these in order. Spines and the crash cross stay empty. This pass did not move the crash torso, the net beam, or the ninja rail. Seating on the dome, ground slide, spiral climber, beams, arches, and benches already meets the support plane, so no y change. Arch piers still clear courts and bars by at least 1.3 m. The NE hop tiles now clear the spawn seesaw by 0.56 m. The other gaps under 0.5 m are beside the walk, not across it.

Chase these structures. Do not add props. SoftS aisles, the NE hop, and the merry-spine bar stay as they are. `Mega_SlideTube` and `PGK_Slide_Tube90` are not placed.

- [ ] SW: mushroom (4.5, 6.94) -> hopscotch SW -> fort-west spring (8.53, 7.23) -> soft-play bench (8.20, 5.75)
- [ ] Soft-play: tube street, SoftS apron, slide beam (18.48, 2.97), dome, ground slide, deck tube. Leave the 1.11 m aisles.
- [ ] South spine: overhead bar at (11, 17.78). Posts sit outside the spine. Rungs are at y=2, so the chase under them stays open.
- [ ] Merry: bench (4, 24), picnic (4.8, 21.4), west spring (2.50, 24), then merry-north (7.2, 31.2)
- [ ] North spine -> NW arch (7.70, 42) -> NW cluster (9.4, 45.0) -> hopscotch NW -> astro dome and deck tube
- [ ] East: hopscotch SE -> SE arch (65.90, 10.5) -> SE cluster -> army crawl mouths and the spiral (crawl spring and conn spring)
- [ ] Open kickball (field empty) -> swing beam (63.45, 31.20) -> swings. The 0.16 m fall-tile gap stays empty.
- [ ] Knight crawl mouths and spiral -> NE arch (65.65, 41) -> NE cluster. The hop gap stays 0.56 m. Then hopscotch NE.
- [ ] Crash cross on z 24-30. Both lips and the middle stay open.

Seats: SW bench (9.26, 9), soft-play bench (8.20, 5.75), merry bench (4, 24) and picnic (4.8, 21.4), kickball bench (61.30, 21.05), swing bench (71, 31.2), NE bench (65.65, 41.90). Each hopscotch court also has a bench at local (-2, 0).

1. **SW exit.** Spawn_SW faces northeast. Mushroom steps at (4.5, 6.94) sit beside that line, between the pad and hopscotch SW. Feet are on the ground. No arch here: the court and the south bar are already about 2.2 m apart. The bench moved from (9.85, 9) to (9.26, 9). The old spot was on the climb net. It is now about 0.29 m west of that net and off the court. The passage east of the net, toward the bar, stays open. The court is still the destination.
2. **West loop.** Hopscotch SW -> south bar (x=11) -> soft-play. A bench at (8.20, 5.75) is west of the tube street, 0.50 m off the plastic cap, south of the climb net. A spring at (8.53, 7.23) sits north of that bench, between it and hopscotch SW: x 8.18-9.03, z 6.99-7.47, 1.01 m off the bench, 0.75 m west of the climb net, 0.74 m off the nearest court tile. Astro does not get it. The street itself stays clear. A ground slide at local (5.90, -5.53) sits east of the tube cap: low mouth on the mulch, high end about 2.41, 0.79 m south of the tubes. East forts do not get it. East of that mouth, three wall panels at z=1.70 span x 21.40-26.20: 0.86 m off the slide, 1.35 m west of the south ring ground stair, 0.79 m south of the ring side stair. The outer lane stays north of them. South of the tube street, a cluster at (14.5, 2.6): a balance beam, mushroom steps, a spring rider, and two hop tiles south of the beam. The mushrooms stop at z=4.45, 0.58 m south of the tube mesh. An east-west step between the beam and the tubes spans x 13.15-15.74 at z 3.46-4.34, 0.68 m off both and 1.11 m off the north-south mushrooms and the spring. The spring ends at x=17.70, 1.56 m west of the ground slide. A balance beam at (18.48, 2.97) yaw 90 fills that gap: x 18.42-18.54, z 1.47-4.47, 0.72 m off the spring and the slide, 0.56 m south of the tube mesh. The SoftS aisles stay as they are. Astro does not get the beam. The street and the climb-net gap stay open. Both west forts also get a climber dome at local (-4.66, -6.70). Feet are seated (stem -0.330) and the top is about 1.31, a low round climb the east bunkers do not have. On soft-play it sits at x 7.95-10.73, z 1.66-4.44, 0.59 m south of the tubes and 0.43 m west of the apron mushrooms. On astro the same piece lands at x 17.27-20.05, z 49.56-52.34, 0.45 m west of the west lane and off Spawn_NW's exit. Then tubes, the deck tube on both west forts, 2.4 m rung, net beam, or the tall net. A spring rider at (19.20, 10.40) sits on the east shoulder, 0.78 m east of the spiral and 0.80 m west of the west lane. The north pit still ends at z=16.0 with the spine at 16.4, so that exit stays open. No west alley was sealed. Cross the south spine on the overhead bar at x=11, z=17.78. Its posts sit at z 15.74-15.82 and 19.74-19.82, outside the spine, and the rungs are at y=2 so the chase under them stays open. Then middle bars or the beam lane -> merry apron into the bars. Merry bench is (4, 24); picnic is (4.8, 21.4). A spring at (2.50, 24) is the west lawn mark: x 2.15-3.00, z 23.76-24.24, 2.15 m off the map edge and 0.78 m west of that bench. No second bench. North of the pad, west of the bars, a cluster at (7.2, 31.2): mushroom steps, a spring rider, and two hop tiles. It stays 0.56 m west of the bar and 1.9 m south of the north spine. Cross the north spine -> north bar -> astro.
3. **NW exit.** Spawn_NW faces southeast (yaw 135), into astro. The crash torso at (12, 52.12) is 1.0 m north of the pad and about 3.6 m east of the pad's east edge, so it is not on that exit. Astro's climber dome is north of its tubes (x 17.27-20.05, z 49.56-52.34), 0.45 m west of the west lane, so it is off this exit too. East of that dome, two wall panels at z=51 span x 21.36-24.56: 1.31 m off the dome, 1.32 m west of the north ring side stair, north of the tube street and the outer lane. Hopscotch NW is at (3.2, 42), south of the pad, west of astro. The arch at (7.70, 42) spans that court toward the north bar. Piers stay about 1.7 m off both. The deck is at 1.05 and the span underneath is open, so the court stays a destination. North of that arch, a cluster at (9.4, 45.0): mushrooms, a spring, and two hop tiles. The mushrooms end about 0.22 m west of the north bar face, and the bar line stays clear.
4. **East loop.** Spawn_SE -> hopscotch SE -> arch at (65.90, 10.5). South of that arch, a cluster at (66.2, 7.5): mushrooms, a spring, and two hop tiles. Then army, whose 2.00 chute is the same pitched deck tube. Knight keeps the straight chute. Army and knight each have the 2.4 m rung on the west shoulder, the 1.8 m ladder on the net side, and a spiral climber at local (2.80, 0) on the deck's far side. Feet are on the ground and the top is 2.40, so it is a climb onto the 2.00 deck. On army it should sit about 0.4 m south of the south bar. On knight, about 1.7 m north of the conn. A spring at local (2.80, -2.302) is the step between that climb and the crawl: 0.56 m off the crawl and the south tiles. Army world (60.80, 7.45), x 60.45-61.30, z 7.21-7.69. Knight world (55.20, 46.55), x 54.70-55.55, z 46.31-46.79. A second spring at local (2.80, 1.748) is the step from the conn up to that climb: 0.56 m off the spiral and 0.66 m off the conn. Army world (60.80, 11.50), x 60.45-61.30, z 11.26-11.74. Knight world (55.20, 42.50), x 54.70-55.55, z 42.26-42.74. West play places keep the slide spiral and the tube street instead. Each bunker also has one crate on the outer apron, local (-0.80, -6.81): 0.56 m off the crawl wall, 2.4 m off both mouths. Army world (57.20, 2.94) is 1.72 m west of Spawn_SE's bumper. The bumper sits south of the army crawl, 0.57 m off the mesh, so that east mouth stays open. Knight world (58.80, 51.06) is 1.68 m west of the shield. West forts do not get the crate. Two monkeys continue the south ring's east bar at z=3.35, x 42.3-50.7, abutting that bar and stopping 1.3 m west of the army crawl mouth. A mushroom step at (53.73, 2.85) sits on the army south apron, 1.78 m east of those bars and 1.78 m west of the crate, 0.56 m south of the crawl wall. The west mouth stays open. Knight does not get that step. A second mushroom at (50.10, 5.00) is the approach to that mouth: x 48.85-51.44, z 4.56-5.44, 0.56 m west of the opening and 0.78 m south of the south rail. Three monkeys continue the north ring's east bar at z=51, x 42.3-54.9, abutting that bar and stopping 1.1 m west of the knight crawl mouth. A mushroom at (54.10, 49.48) is the approach to that mouth: x 52.85-55.44, z 49.04-49.92, 0.56 m west of the opening and 1.04 m off the climb net and the bars. Army does not get that step. A mushroom at (65.81, 48.75) is the approach to the knight crawl's east mouth: x 64.56-67.15, z 48.31-49.19, 0.56 m east of the opening. Army does not get it. Bars at x=62.5 into the open west side of kickball -> swings. A bench at (61.30, 21.05) is west of those bars, south of the Loop E stair and north of the south spine. The diamond stays empty. Swing fall tiles should still clear the kickball north fence by about 0.16 m. That gap is the clearance, so nothing was added there. A balance beam at (63.45, 31.20) yaw 90 sits north of the fence, between the east bars and the swing frame: x 63.39-63.51, z 29.70-32.70, 0.85 m off the bars and 0.84 m off the swing. The diamond stays empty. Swing bench is (71, 31.2). Continue the bars. Knight is west of those bars (same rung and ladder). The arch at (65.65, 41) is east of them. A bench at (65.65, 41.90) sits just north of that arch, about 2.4 m off the bars and off hopscotch NE. It is not a second arch. North of that bench, a cluster at (66.2, 45.0) holds a net frame, mushroom steps, a spring rider, and two hop tiles. The hop east edge is x=68.45, 0.56 m west of the spawn seesaw (west edge x=69.01). The south hop clears the net's southeast post by 0.10 m and the mushroom cap by 0.16 m. The spring is 1.86 m east of the bar. The mushroom stays 0.58 m north of the bench and 1.16 m west of the seesaw. Spawn_NE's southwest exit stays north of the cluster, and the arch span under the bench is still open. Then hopscotch NE. Spawn_NE faces southwest, into the keep. The shield is north of that pad. Merry, hopscotch, and swings were not moved.
5. **Crash.** Cross the bowl east-west. Both lips and the middle should be open lawn. From the east bars the cross lane is z 24-30. The nearest new bench is the kickball seat, north edge z=21.75, which is 2.25 m south of that lane. The arches are farther north or south. Nothing on that approach was nudged.
6. Chase, in order: SW mushroom -> hopscotch SW -> fort-west spring -> soft-play (SoftS apron, tube street, slide beam, dome, ground slide, deck tube) -> soft-merry bar across the south spine -> merry (bench, picnic, west spring) -> merry-north -> north spine -> NW arch -> NW cluster -> hopscotch NW -> astro. East: hopscotch SE -> SE arch -> SE cluster -> army mouths and spiral -> open kickball -> swing beam -> swings -> knight mouths and spiral -> NE arch -> NE cluster -> hopscotch NE -> crash cross. SoftS aisles, the NE hop, and the merry-spine bar were not densified. Mega and Tube90 are not placed. Feel was not edited.

## Tag handoff / AI (code)

- Punch transfer: PunchHitbox -> TagModeController.OnSuccessfulPunch -> TransferIt -> ItController.SetIt.
- SetIt(true) plays become-It SFX, calls PlayerMotor.NotifyBecameIt() (anim/HUD listeners), and pulses DummyLocomotor.PlayTagFlinch on the new It.
- Victim also flinches via ReceiveTagHit. HUD flashes YOU'RE IT / YOU'RE FREE and names LastFromId / LastToId. The hat pops on the rising edge.
- DummyPatrol Retargets immediately on **gain and lose** It, then weaves and caps turn rate so the new chase is a kite, not a snap.
- Flee panic hop uses dy 0.9 (was 0.55, below ConsumeHop minDy 0.85, so it never fired). Trail Tag mode line shows **SUDDEN DEATH** when the cap/stall failsafe trips.
- HUD mode line uses ASCII ` | ` separator; center MODE flash lists F1-F4. F1-F4 SetMode also syncs GameFlow menu cursor via PlayerPrefs, recovers ragdoll, and places pawns on pads.
- Trail Tag self-hit still needs both age and distance grace. Dodge i-frames do not ignore trails. Punch updates It brightness the same frame for every emitter mode. ItOnly still gates who emits.
- Results card: solo headline is YOU WIN, YOU LOSE, or DRAW, and it names the mode and the winners. Couch with a split result stays ROUND OVER. Rematch click plays UiConfirm. A loss line does not say "winner", so it does not play the win tone. Hot Potato HUD says YOU HOLD THE FUSE when you are It; Trail marks YOU and OUT waiting for round; sudden death explains the next hit. Trail OUT! / TRAIL HIT flash holds ~1.0s with the other center beats. **R** / Rematch starts once; Boot does not also rematch. A second StartRound inside 0.05 s is ignored. **Q** or **Esc** returns to Boot. F1-F4 or R leave pause and round-end so the countdown is not frozen. Esc on player-count and mode screens steps back to Boot. Keys 1-4: you + 1 bot, or 2-4 humans with the bot off. Direct Play pauses on Esc; Q loads Boot. Pause zeroes look and punch, clears a buffered jump; HUD flashes use scaled time so they freeze. Countdown card names the mode (3-2-1). Local punch windup flares the elbow (0.12 s unchanged). Look sensitivity is a five-step stub (default 1.8); Boot and pause open it; Left/Right change it. Direct-Play pause notes Countdown frozen. Playground music stays silent when the clip is null. Ski entry still uses TagSfx.EnsureSource.
- Look sensitivity stays in PlayerPrefs (`Tag.LookSensitivity`, default 1.8) and loads when the camera wakes. Controls help is on Boot, the pause menu, and Direct Play (H). Air dash can be Q (default), V, or Mouse4; Left Alt still dashes, and that choice is saved. A jump or crouch held through pause is not a new press on resume. A dash or punch tap during pause does not fire later. The first Boot visit says Play is you and one bot in Least It until you start.
- Punch can be LMB (default), F, or Mouse3; E still punches. That choice is `Tag.PunchKey`. Volume and mute stay on the Boot and pause Audio card (`AudioMaster`, default 0.8). On the results card the cursor is unlocked and gameplay input is zeroed, so a Rematch click does not punch or yaw. StartRound locks the cursor and ends any swing that was still out. Windup stays 0.12 s. Slide, jump, dash, and jet numbers are unchanged.

## Feel check (code, not a Unity play)
- Jetpack off: `MovementConfig.enableJet` is false (asset enableJet: 0). RMB does not jet.

Slide keeps entry planar speed: `SlideMove` only applies friction (softer downhill) and clamps to `_slideStartSpeed`. The punch +8% speed buff is skipped while `State == Slide`, then the same cap is applied again. `slideDownhillAccel` is 0 and unused. Jump sets `v.y` from `jumpSpeed` / fatigue, not from horizontal speed. Air dash is a short planar burst with `airDashCooldown` 30 and a cyan trail on `DummyLocomotor` (trail updates even if the limb bind fails). The near-zero speed floor inside `EnterSlide` cannot run: crouch only enters a slide at `slideEntrySpeed` (7.5).

Wall-run and wall-climb set a latch on exit (timeout, jump-off, or lost contact). The latch clears on the ground or after ~0.15 s with no wall hit, so air accel cannot restart the timer on the same surface. Climb up-speed (`climbSpeed` 6, decay from 0.16 s, slip -> 3.5) reverses before `climbMaxHeight`; the old 7.8 / 0.40 curve hit the height cap at ~0.42 s while still going up, and `ClimbHeightUsed` never cleared on landing. Sprint stays 12 m/s, ski max 24 m/s (run was already raised; ski still wins). Jet stays off.

## Human verify next

No Unity play on this pass (no Unity / `dotnet` on the VM). After pull, open **Play**:

1. Sprint, hold Ctrl: slide should not speed up on entry. Down a slide, it should last longer and still not go faster than the speed you had at the crouch.
2. Jump from a walk and from a sprint: same height. Hold Ctrl in the air: you should drop faster than a normal fall.
3. Air dash (Q): short cyan streak, then the HUD dash bar counts ~30 s. RMB should not jet.
4. Wall-run a figure-8 panel: you should slide down and fall off. You should not re-stick until you leave the wall or land. Climb a net: rise, then slide down. After you hit the ground you can climb again. Climb decay should start peeling before you hit the height cap (slip reads a bit stronger). Late wall-run should peel harder before the attach timer ends.
5. Run steps (a plant, then a lift) rather than a constant skate. Hands stay forward of the hips. A hard landing buckles the knees and opens the arms, then stands back up. Q dash should reach the whip pose inside the short burst. Mid hop-offs should still show a brief crouch squash.
6. Chase, in order: SW mushroom -> hopscotch SW -> fort-west spring -> soft-play (SoftS apron, tube street, slide beam, dome, ground slide, deck tube) -> soft-merry bar across the south spine -> merry (bench, picnic, west spring) -> merry-north -> north spine -> NW arch -> NW cluster -> hopscotch NW -> astro. East: hopscotch SE -> SE arch -> SE cluster -> army mouths and spiral -> open kickball -> swing beam -> swings -> knight mouths and spiral -> NE arch -> NE cluster -> hopscotch NE -> crash cross. SoftS aisles, the NE hop, and the merry-spine bar were not densified. Mega and Tube90 are not placed. Feel was not edited.
7. Tag the dummy: hat pops, flash says YOU'RE FREE and names who is It. When they tag you: YOU'RE IT and who it came from. Handoff flash holds ~1.0 s.
8. F1 while you are It: top-center FUSE appears inside the warn window even if you pass It away. F2: mode line shows seconds left and WINNING (least) / BEHIND (more It). F3: a foreign trail still eliminates; your own trail does not until the grace ends. F4: free play, punch still moves It, no timer. Each of F1->F4 should drop you on a spawn pad, including if you were ragdolled.
9. As It, a sharp strafe should make the dummy miss more often than it connects. As runner, you should be able to cut their flank instead of losing a straight race every time.
10. Q dash and a grounded It lunge should not sound the same. A short hop lands with a soft thud; a hard land is louder. Tag, round start, and a trail elim should make a tone even with no audio files imported.
11. From across a fort the orange hat and beacon should still read. Your punch windup should flare the elbow out beside the head within the same short windup. Holding LMB as It should still cock the fist before the swing.
12. When a round ends, a center card names the result. One R (or Rematch click) starts the next round from a pad (a second R in the same moment does not restart it again). Esc pause, then F1: the countdown should move. Q, Esc, or Menu from the card returns to Boot, including a direct Play scene. In Trail Tag, after OUT you should see "waiting for the round" until the match ends.
13. Your own hat should sit on your head without a tall spike in the camera. The dummy's beacon should still read from across a fort. When the dummy is It, you should see the arm cock before the punch, and leaving that range should cancel it. Getting tagged should nudge your camera. Pause resume and quit should click.
14. Esc during Play pauses. Mouse look should stop and a click on Resume should not punch. A jump you buffered just before Esc should not fire when you resume. HUD flashes should freeze while paused. Direct Play (opened without Boot) still pauses, and Q loads Boot with the cursor unlocked and the music bed stopped. Direct Play pause matches Boot: Left/Right highlights Resume, Controls, Look, Audio, Quit and stops at the ends. Enter or Space uses that row. Keys 1-5 only move the highlight. Esc on Controls, Look, or Audio stays paused. Boot's player and mode screens: Esc steps back. Row 1 is you + 1 bot. Rows 2-4 are humans and the bot stays off. The countdown names the mode and counts 3, then 2, then 1. No music bed is expected. A ski entry still makes a tone.
15. Look panel: Up/Down highlights Sensitivity then Back and stops at the ends. Left/Right steps the sensitivity only while that row is highlighted. Enter on Back closes. Esc leaves the panel without unpausing. Default should still feel like the current camera. Quit and relaunch: the same step should still be selected. When a solo round ends, the card should say YOU WIN, YOU LOSE, or DRAW, and name the mode and the winners. Rematch and Menu still click.
16. First Boot visit should say Play is you and one bot in Least It. Controls lists the real keys. On the Controls card, Up/Down highlights Dash, Punch, then Back. Left/Right steps the highlighted bind (Q, V, Mouse4 for dash; LMB, F, Mouse3 for punch). Alt still dashes. E still punches. Keys 1-3 only move the highlight. Enter on Back closes. Direct Play pause: H opens that same card.
17. Default LMB should feel the same. Volume stays on the Audio card. On the results card, clicking Rematch should not punch and the mouse should not turn you. The next countdown should not still be swinging. Direct Play results should unlock the cursor the same way. Enter on the results card does not also click a different button.
18. Audio panel: Up/Down highlights SFX, Music, Mute, Music mute, then Back and stops at the ends. Left/Right steps the highlighted row (SFX or the music bed). M still mutes all. N still mutes music. Default bed should sound the same. Enter on Back closes. Esc does not unpause. On the results card, Left/Right can move Rematch / Menu during the short arm; Enter does nothing until the arm ends. R still rematches even if Menu is highlighted, and a second R does not. Q and Esc still return to Boot.
19. Boot: Up/Down highlights Play, Controls, Look, Audio, Mode, Couch and stops at the ends. Enter uses it. With Play highlighted, Enter still starts you and one bot and does not also open another row. Keys 1-6 only move the highlight. Pause: Left/Right or 1-5 picks highlights Resume through Quit and stops at the ends. Enter or Space uses it. Esc still resumes and Q still quits. Up/Down on the main pause card is still the music bed.
20. Who-plays and mode select no longer wrap. Up on the first row and Down on the last row stay put. Keys 1-4 still jump to that row. A click on Boot or Pause moves the highlight and uses that row only. Enter uses only the highlighted row.
21. Pause, open Controls (or Look or Audio), then F1. The panel should be gone and the countdown should run. Esc pauses. After a round, that same panel should not cover Rematch / Menu, and Q should show Boot, not the panel. Direct Play: if a round ends while the local pause card is up, the results card should still take Left/Right and Enter.
22. During play, M mutes all and N mutes music. The HUD chip should show. Pressing M on the pause card or the audio card still toggles once, not twice. After a dash, the bar is a dark track with a cyan fill that grows back; ready or the burst itself is mint. In Hot Potato the It beacon warms toward white as the fuse drops. Your own camera still hides that beacon.
23. First Boot visit: the line names the punch key (LMB or E by default) and M/N. It should not say LMB/F. Open Couch or Mode select, then Esc: the first-run line should still be there. Play a round, then Q back to Boot: that line should be gone, and Play should be highlighted. The first countdown says WASD move and Shift sprint. Rematch, and the next countdown, should say to punch the dummy with the orange hat. On the pause card, H opens Controls and stays paused. Digits on who-plays and mode select only move the highlight. Enter or Space confirms.
24. Set 3 humans on who-plays, then Esc from mode select: you should be back on who-plays with 3 highlighted, not Boot. Esc again: Boot, Couch highlighted, and the next Mode select should still say 3 humans. Opening Mode select from Boot should not change that count. The highlighted mode should be the one you played last.
25. On the results card, 1 and 2 only move Rematch / Menu. During the short arm, Enter and Space do nothing. After it, Enter or Space uses the highlight. R still rematches even if Menu is highlighted. Q and Esc still return to Boot. A click during the arm only moves the highlight. The same keys work on the loose round-end card if no mode controller is showing results.
26. Play: you and the bot should be the curved Hier mannequin, not the flat Dummy_Runner mesh. The console should say Hier mannequin, not Navy Spade, unless the FBX failed to bind. The runner is tan with a teal band. Becoming It should swap to the orange Hier mesh with black Vs. A run should show the knees bend. Arms should hang slightly out, not fold into the butt.
27. Sprint: one knee should lift on the forward leg and the back leg should look straight, not two bent skates. Slide should look low, lead knee tucked. A hop should buckle the knees on landing. Air dash should throw the arms back without twisting the hands into the hips. A punch should cock the elbow out, then the fist should stay in front of the chest.
28. Sprint from the chase cam: the arm that reaches forward should be opposite the leg that is forward. The other arm stays back and does not fold into the hips. You and the bot should still be the curved Hier mesh.
29. Slide: the body should look flat, lead knee under the chest, trail leg straight back, arms forward. Punch: the fist should be a long line in front of the chest, not a folded elbow. It should not pass through the torso.
30. Jump and keep sprinting: knees buckle on the landing, then open back into the run over a short moment. They should not snap straight on the first frame. A small hop still buckles. Arms stay slightly out and do not fold into the hips.
31. Air dash: the arms should throw back at the start, then ease down before the streak ends. They should not stay fully whipped and then snap into the run. Hands stay clear of the hips.
32. After the v0.1 Hier FBX: you and the bot are the tan mannequin, It is the orange mannequin, and the same poses still read (recovery knee, opposite arms, flat slide, long punch, land ease, dash settle).
33. Jump: both arms should reach up and the knees should tuck. Fall: the arms should trail back and the legs should lengthen. Hands stay slightly out and clear of the hips.
34. Tag someone: the runner who was hit should guard with both arms up and both knees bent. The new It should raise both arms and lift one knee. The two poses should not match. Hands stay clear of the hips.
35. Climb a wall: one hand should reach while the other pulls, and the opposite knee should step. The lower leg should look long. Wall-run: the wall hand stays on the wall, the outer leg steps, and that knee bends only while it swings forward. Hands stay clear of the hips.
36. Stand still: the hands should hang just forward and outside the hips, not against the pelvis. Start sprinting: the opposite arm/leg stride should return, with no extra twist of the hands into the hips.
37. Jump, including a short hop: both arms should be a long line up and the knees tucked before you reach the top. On the way down, even a short drop, the arms should trail back and the legs should lengthen before you land. Hands stay clear of the hips.
38. Sprint: each plant should hold a moment, one knee up and the other leg long behind, not two straight legs sliding together. The forward arm should still be the opposite side. Hands stay clear of the hips.
39. Slide: the arms should be a long line forward and low, not a folded pair at the chest. Lead knee stays under the chest and the trail leg stays straight. Hands stay clear of the hips.
40. Land, including a small hop: knees buckle, and both arms should come out to the sides while that buckle holds. They should ease back into the run. Hands stay clear of the hips.
41. Climb: both hands should stay visible as they swap, the low hand a line and not a fold at the chest. Wall-run: the wall hand should move up and down the wall with the step, and the outer arm should stay straight. Hands stay clear of the hips.
42. Punch: the windup fist should sit beside the head, not inside the chest. The connect should still be a long line in front of the chest. The cock should not feel longer.
43. Air dash: the arms throw back at the start, then ease down before the streak ends. When the streak ends they should keep easing forward. They should not throw back again. The burst should still feel short.
44. Tag someone: the runner who was hit should show a long V of arms, not a fold at the chest, and both knees should bend. The new It should still raise both arms and lift one knee. The two poses should not match, and neither should look like a landing.
45. Stand still, then sprint: the hands should stay outside the hips the whole way into the stride. They should not tuck in as the walk starts, and they should not twist into the pelvis at a standstill.
46. Hold ski, then let go into a sprint: the body should ease into a lower glide with the arms out, then ease back into the run. It should not pop, and the glide should not look like the sprint. Jet stays off.
47. Wall-run, then drop or land into a sprint: the wall hand and the outer leg should ease into the fall or the run. They should not snap off the wall in one frame.
48. Climb, then drop or step off into a sprint: the reaching hand and the stepping knee should ease into the fall or the run. They should not snap off the wall in one frame.
49. Only if you add ExperimentalGrapple and turn enableGrapple on: holding the rope should reach both arms in a long line, legs staying long. It should not look like a jump. With the gate off, RMB still does not hook. Jet stays off.
50. Tag someone: the runner who was hit should still show the two-arm V with both knees bent. The new It should raise one arm, hold the other out, and lift one knee. The two poses should not match.

## Known leftovers

- Prefab/mat dirt after Hub visuals / URP regen -> do not commit unless intentional.
- Flat HiPoly mannequins may skip hierarchical `DummyLocomotor` binds (primitive / bindable-bone path is the readable tell).
- Legacy contact `TryTag` radius still exists on motor; play modes use punch transfer.
- Trail avoid starts peeling ~9.2 m (weight 0.80) off a foreign ribbon (HUD TRAIL! soft warn ~6.2 m).
- AI punch tell drops for ~0.32 s after a juke/leave-cone whiff so the arm drop is readable (still needs a fuller human feel pass). No spectator camera: an eliminated player stays on their body with a waiting line. Playground music stays silent: `music_playground_bed_loop.wav` is meta only, so PlayMusic returns. No hitstop. Hot Potato flee may air-dash once while airborne if the motor CD is ready. A juke whiff also refreshes weave so they peel off the punch line.
- Dash HUD: jet off = one CD bar (dark track, cyan fill, mint when ready or bursting) and one DASH line (DASH! while bursting). Jet on keeps a dash CD line under JET. Cooldown stays 30 s.
- Dummy MissRecover: limp whiff drops faster than HitRecover hold (short shoulder sag). AI HoldPunchTelegraph matches the windup cock beside the head.
- Bots hold still on countdown, results, and Idle (no chase until Playing).
- Resume / leave-results: look, punch, jump, dash, and lunge ignore two frames after the cursor locks (shared resume gate + cameras) so the menu click that closed the card cannot yaw or punch. Rematch / F-keys from Direct Play also arm that gate when the cursor locks.
- Do not hand-author `TagURP*.asset` YAML; use **Ensure URP Pipeline**.

## Audio
- HUD shows MUTED (M), MUSIC OFF (N), or both chips when both mutes are on. M and N work during play, Boot, results, and the subpanels. Pause and the audio card use that same listener, so one press toggles once.
- Dash cooldown bar: dark track, cyan fill while cooling, mint when ready or during the burst. Label still says DASH, the seconds, or DASH ready. Cooldown stays 30 s.
- Master volume / mute: Boot or pause Audio. Up/Down highlights SFX, Music, Mute, Music mute, Back. Left/Right steps the highlighted SFX or music bed (default bed 0.35). M mute all. N music only. On the main pause card, Up/Down is still the music bed. Saved in PlayerPrefs on AudioMaster.

## Results
- Rematch / Menu: results ignore activate keys for ~0.25s and one-shot R/Q/Esc/click (Esc mirrors menu) so the round-end key cannot rematch or quit early. Keys 1-2 and Left/Right can move the highlight during that arm and still stop at the ends. Enter and Space wait until the arm ends, then use the highlight. R still rematches. A click during the arm only moves the highlight. The loose round-end card uses the same keys. Punch ForceEnd on results and pause.
- Direct Play pause matches Boot: Left/Right arms Resume, Controls, Look, Audio, Quit. Enter or Space uses that row. Esc on the main card resumes. Esc inside Controls, Look, or Audio only closes the panel. Those three panels use the same Up/Down highlight as Boot. Q to Boot unlocks the cursor and stops the music bed.
- Controls / Look / Audio close on play, results, and Boot (F1-F4, rematch, Q). A panel left open on the pause card does not stay drawn over the round or the Boot menu. Direct Play clears its pause overlay when results start so Left/Right still move Rematch / Menu.
- Boot, pause, and subpanel clicks are mouse-only. Enter/Space uses the highlight.

## Shippable slice checklist
1. F1 Hot Potato / F2 Least It / F3 Trail Tag / F4 Free play start a clean round (menu cursor syncs).
2. Controls: Up/Down highlights Dash, Punch, Back. Left/Right steps the highlighted bind. Enter on Back closes. E still punches.
3. Audio panel: Up/Down highlights SFX, Music, Mute, Music mute, Back. Left/Right steps that row. M mute all. N music mute.
4. Results: 0.25s arm; Left/Right can move during the arm; Enter activates after it; R rematch; Q/Esc menu; one-shot. A click does not double-fire with Enter.
5. Trail Tag SD: HUD says SD; center flash on rising edge; rematch re-arms flash.
   Center SD flash holds ~1.0 s (same beat as It handoff / F-key mode flash).
   It handoff flash re-arms after rematch so spawn-as-It and the first tag still read.
   Trail OUT! / TRAIL HIT flash also holds ~1.0 s.
   Near-miss soft edge + TRAIL! starts ~6.2 m from a foreign ribbon (readability only; hit rules unchanged).
6. Chase, in order: SW mushroom -> hopscotch SW -> fort-west spring -> soft-play (SoftS apron, tube street, slide beam, dome, ground slide, deck tube) -> soft-merry bar across the south spine -> merry (bench, picnic, west spring) -> merry-north -> north spine -> NW arch -> NW cluster -> hopscotch NW -> astro. East: hopscotch SE -> SE arch -> SE cluster -> army mouths and spiral -> open kickball -> swing beam -> swings -> knight mouths and spiral -> NE arch -> NE cluster -> hopscotch NE -> crash cross. SoftS aisles, the NE hop, and the merry-spine bar were not densified. Mega and Tube90 are not placed. Feel was not edited.
7. Boot Up/Down and pause Left/Right arm a row and stop at the ends. Enter uses it. Play stays the default Boot row.
8. Who-plays and mode select stop at the first and last row. A Boot or Pause click uses that row only. Enter does not also fire a different button.
9. Resume or leave-results: the tip's shared resume gate still drops look and one-shots for two frames after the cursor locks. This merge does not change that gate.
10. Direct Play pause matches Boot, including Controls / Look / Audio highlight. Esc on a subpanel stays paused. Q back to Boot shows the cursor.
11. Open Controls from pause, then F1: the panel closes and the round runs. Results and Boot are not covered by that panel. Direct Play results still accept keys if the local pause card was up.
12. M during play mutes once and shows the chip. N mutes music. The dash bar fill is visible against a dark track. Hot Potato heats the It beacon with the fuse.
13. First-run Boot copy names the punch key and clears after a round, including Direct Play back to Boot. Opening Couch or Mode select does not clear it. The long countdown hint shows once and says move versus sprint. Rematch uses the short orange-hat line. Boot pause H opens Controls. Digits highlight. Enter / Space confirms.
14. Who-plays Esc returns to Boot on Couch. Mode select Esc returns to who-plays after Couch, and to Boot after the Mode row. The Mode row does not reset the player count. Highlights match the saved count and the last mode.
15. Results: keys 1-2 highlight Rematch and Menu. Enter or Space activates after the 0.25s arm. R rematches. Q/Esc menu. A click during the arm only moves the highlight.
16. Player and bot spawn the curved Hier HiPoly mannequin. Runner is Tan Hier. It is Orange Hier. Flat Dummy_Runner on the Play scene does not win. Navy Spade only if Hier has no limb bones.
17. Run: recovery knee flexes, stance leg stays nearly straight. Dash whip does not add arm roll. Slide is a low crouch. Land buckles. Punch connect stays in front of the chest.
18. Run arms oppose the legs. Forward arm is the opposite side of the forward thigh. Hands stay clear of the pelvis. Hier spawn still wins over flat Dummy_Runner.
19. Slide silhouette is flat (chest down, lead knee tucked, trail leg back). Punch connect is a long arm in front of the chest.
20. Land holds a short buckle, then eases into the run. A hop still buckles. Arms stay clear of the pelvis.
21. Air dash whip stretches early and settles before the burst ends. No extra arm roll into the pelvis.
22. v0.1 Tan and Orange Hier still bind UpperArm / LowerArm / UpperLeg / LowerLeg. Pose drivers were not reverted.
23. Jump arms reach up with a knee tuck. Fall arms trail back and the legs lengthen. Hands stay clear of the pelvis.
24. Tag handoff: the tagged runner guards. The new It lifts one knee. Hands stay clear of the pelvis.
25. Climb is hand-over-hand with one bent knee. Wall-run plants the wall hand and steps the outer leg. Hands stay clear of the hips.
26. Idle hands hang forward and out of the hips. The offset is gone once the stride is up. No extra arm roll.
27. Jump tuck and fall trail show before the landing, including a short hop. Hands stay clear of the hips.
28. Run plant holds. Front knee bends, back leg stays long, arms still oppose the front leg. No extra arm roll.
29. Slide arms are a long low line. Elbows stay nearly straight. Lead knee tucked, trail leg long.
30. Land arms come out for balance during the buckle, then ease into the run. Hands stay clear of the hips.
31. Climb hands stay a long line through the reach and the pull. The wall-run hand presses with the stride. The outer arm stays straight. Hands stay clear of the hips.
32. Punch windup sits beside the head, clear of the chest. Connect stays a long line in front. Windup time is unchanged.
33. After an air dash the arms ease out of the hang. They do not whip again when the burst ends. Dash time and cooldown are unchanged.
34. Tag catch is a long V of arms with both knees bent. Hands stay clear of the chest. The new It still lifts one knee. Neither pose matches a landing.
35. Idle into a run keeps the hands outside the hips. Resting arms have no extra roll. The stride still opposes the front leg.
36. Ski eases into a lower glide with the arms out, then eases back into the run. It does not pop. Jet stays off. Ski speed is unchanged.
37. Leaving a wall run eases into the fall or the run. The wall hand and the outer leg do not snap. Wall-run speed is unchanged.
38. Leaving a climb eases into the fall or the run. The reaching hand and the stepping knee do not snap. Climb speed is unchanged.
39. Grapple pose is a long two-arm reach with long legs, only while enableGrapple is on and a rope is attached. The default gate stays off. Jet stays off.
40. The new It raises one arm and holds the other out, chest open, one knee up. The tagged runner still uses the two-arm V. The poses do not match.

## Grapple (experimental, off)
- Not part of the default tag loop. The spawned pawn does not get `ExperimentalGrapple` unless you add it. `enableGrapple` stays false, so RMB does not hook and does not jet.
- If you add the component and turn it on, hold RMB (JetHeld) for a rope pull. Release drops it. Pause or the results card drops it too. Slide, jump, dash, and jet numbers stay the same. Audio stays on AudioMaster.
