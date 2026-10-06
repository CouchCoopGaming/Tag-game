# Performance

A four-player round on the Mega Park graybox should hold a steady 60 Hz frame on a mid PC, with room left over. This pass measures that as operation weights, then cuts the work that was still happening every frame.

## Budget

One frame is 16.7 ms at 60 Hz. The headless proof does not time the CPU. It counts the same operation weights the in-game meter uses, and it treats 240 as a full frame. That leaves the measured spike at 144, which is 96 under the cap.

| Bucket | Weight | When it is charged |
|---|---|---|
| Movement | 8 per pawn | Every frame. One kinematic step, standing in for the one `CharacterController.Move`. |
| Pose | 6 per pawn | Every frame. Idle weight and the punch-stagger weight. |
| HUD | 5 | Every frame. Glyph, hint, and digit read. The strings are already cached. |
| Audio | 2 | Every frame. Bus poll. A real cue is extra and rare. |
| Round | 3 | Every frame. Phase and clock. |
| AI tick | 2 per AI | Every physics step. The verb roll and the reaction countdown still advance. |
| AI decide | 14 per AI | Only on a refresh. |
| AI path | 11 per AI | Only on a refresh. Pad, zip, and the loop sample. |
| AI loop | 1 per AI | Only on a refresh. A cached loop point. |

A quiet frame is 72. A frame where all three AI refresh together is 144. Worst over median is 2.00, under the 2.50 spike cap.

## Proof

`Tools/StrafeJumpSim` runs `FrameBudget.RunAll`. Each arena is a 120 second round at 60 Hz (7200 frames) with one human stand-in and three AI. The human and the It follow that arena's loop. The two runners call `EnemyAi.Evade`. Path and loop searches refresh at 5.2 Hz, or sooner after 1.25 m of travel, which is the same rule the pawn uses. Mega Park, Pocket Park, and Stack Yard each stay under the spike cap.

```
frame-budget seconds=120 hz=60 frames=7200 players=4 ai=3 map=mega-park median=72 worst=144 ratio=2.00 move=230400 ai=117096 pose=172800 hud=36000 audio=14400 round=21600 decides=3079 loopSearches=3079 budget=240 headroom=96 steady=ok
frame-budget seconds=120 hz=60 frames=7200 players=4 ai=3 map=pocket-park median=72 worst=144 ratio=2.00 move=230400 ai=116256 pose=172800 hud=36000 audio=14400 round=21600 decides=3044 loopSearches=3044 budget=240 headroom=96 steady=ok
frame-budget seconds=120 hz=60 frames=7200 players=4 ai=3 map=stack-yard median=72 worst=144 ratio=2.00 move=230400 ai=117096 pose=172800 hud=36000 audio=14400 round=21600 decides=3079 loopSearches=3079 budget=240 headroom=96 steady=ok
```

On Mega Park, 3079 decides across three AI is about 8.6 Hz each, not 60. Loop searches match decides on every arena: one projection per refresh. A second projection of the same point is a cache hit and returns the same point. The loop cache resets when the arena changes, so Pocket Park and Stack Yard do not reuse a Mega Park sample.

A second line, `frame-budget-split`, is the same 120 seconds with four human pawns and four chase cameras and no AI. Each pawn still takes one move. The cameras keep fov pop, shake, and slow motion at 0. Mega Park, Pocket Park, and Stack Yard each stay flat under the same 240 cap, with headroom left.

```
frame-budget-split seconds=120 hz=60 frames=7200 players=4 humans=4 ai=0 cameras=4 map=mega-park median=97 worst=97 ratio=1.00 move=230400 ai=0 pose=172800 hud=144000 audio=14400 round=21600 cam=115200 budget=240 headroom=143 steady=ok
frame-budget-split seconds=120 hz=60 frames=7200 players=4 humans=4 ai=0 cameras=4 map=pocket-park median=97 worst=97 ratio=1.00 move=230400 ai=0 pose=172800 hud=144000 audio=14400 round=21600 cam=115200 budget=240 headroom=143 steady=ok
frame-budget-split seconds=120 hz=60 frames=7200 players=4 humans=4 ai=0 cameras=4 map=stack-yard median=97 worst=97 ratio=1.00 move=230400 ai=0 pose=172800 hud=144000 audio=14400 round=21600 cam=115200 budget=240 headroom=143 steady=ok
```

## What the scan found

The hot-path scan counts `.ToString(`, interpolated strings, `new List`, `new GUIStyle`, `GetComponent`, and LINQ inside `Update`, `FixedUpdate`, `LateUpdate`, `OnGUI`, and the methods those call directly. The historical baseline in that line is 101. At the start of this pass the count was 9, all of them in match start and match end: one list and one log on `BeginPlaying`, and a list, four logs, and two component lookups on `EndMatch`. After the cuts the count is 0.

```
hot-path allocs before=101 after=0 flags=dropped
```

## What changed

- Match start reuses one living list. Match end reuses the trail and punch references captured when the roster is built, and it logs with plain strings.
- The It banner and the round clock rebuild when the text actually changes, not every OnGUI.
- The speed HUD keeps the dash line and the zone line until the digit or the zone changes.
- Pad, zip, and loop samples on the dummy refresh on the 5.2 Hz decision clock, or after 1.25 m of travel. The verb roll, `DelayedAim`, `Decorate`, and `Evade` still step every physics tick, so the roll cadence and the reaction countdown are unchanged. `EnemyAi` given the same sense still returns the same overlay. The enemy-ai proof line is unchanged.
- `LoopProject` remembers the last two points. A repeated query returns the same distance along the loop. `LoopPoint` keeps one vector per Mega Park loop slot.
- The Mega Park lookup and the fallback It search follow that same clock.
- The tag-back spark keeps four pooled objects instead of building a new one per blocked punch.
- It, ragdoll, locomotor, and the punch camera are cached on the pawn after the first lookup.
- F6 toggles the frame overlay. It is off until that press. F3 stays Trail Tag. The overlay shows FPS, frame milliseconds, and the six buckets. It is documented in `Docs/Controls.md` and it is not a rebindable action.

Feel locks are untouched: coyote 0.10, jump buffer 0.16, cling grace 0.08, jump speed 24.7, slide boost 0, air dash 0.10 / 15 / cooldown 30, punch reach 1.55, lunge 16 / 0.20 / 1, lunge tell 0.45, climb 6.0, slip 3.7, wall-run 9.5, punch stagger 0.25 / 0.50, pad cooldown 0.3, zip 14 m/s, regrab 0.3, tag-back immunity 1.0. One `CharacterController.Move` per `Update`. No rigidbody locomotion. No root motion. No new verb.
