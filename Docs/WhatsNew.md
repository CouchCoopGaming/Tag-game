# What's new

Plain-English log for the playtest on this branch. Each bullet is a feature or a notable fix that is actually on this tip, from PR #92 through PR #117, plus the five movement verbs that were already approved here, plus the map lane that this branch converges. Sources are the git log and `Docs/*.md`. Coyote 0.10, jump buffer 0.16, cling grace 0.08, jump speed 24.7, slide boost 0, air dash 0.10 / 15 / cooldown 30, punch reach 1.55, lunge 16 / 0.20 / cooldown 1, lunge tell 0.45, climb 6.0, slip 3.7, wall-run 9.5, punch stagger 0.25 / immunity 0.50, pad cooldown 0.3, zip 14 m/s, zip regrab 0.3, and tag-back immunity 1.0 stay as they were. Ground speed and fall speed were raised in the retune below. Rising gravity stays 22, so the jump still tops out at the same height.

This map-lane branch is the converged test tip. Pressing Play in `Assets/Scenes/Play.unity` builds Mega Park until the arena row says otherwise.

## Playtest fixes

- Space jumps for the keyboard player, including couch seat 1, even when a saved profile rebound Jump. A pad seat does not take Space. An empty or unknown saved Jump key is put back to Space. A real rebind such as E stays saved, and Space still jumps beside it. Try it: rebind Jump to E, start a match, and press Space.
- The park fence collider is the 2.75 m rail you can see, on all three arenas. The old 33 m invisible wall is gone. Sand ramps keep their colliders on the pieces you can see. A surprise move only happens when you fall below -2.5. The Console logs `snap` plus the reason. Try it: run the open middle of Mega Park, then jump the rail.
- The yellow rope fires from the left hand. Click once to attach. On a static surface, one more click pulls you to that point after a short wait. Two clicks inside about 0.28 s let go and do not pull. A moving anchor does not pull. Try it: turn the solo grapple on, click a wall, click once, then double-click to drop.
- A punch moves the right arm on the mannequin. Reach stays 1.55. Try it: Practice, dummy on, and punch.
- A vault keeps the horizontal speed you had when you left the ground. It does not add a long launch. Slide is unchanged. Try it: sprint at a rail you can vault and see that you are not thrown across the park.

## Speed retune

Ground speed is 15% higher for every player and every AI runner, on all three arenas. Fall speed is 8% higher on the way down only. Jump speed stays 24.7 and rising gravity stays 22, so the top of the jump is the same.

| What | Old | New |
|---|---|---|
| Walk | 6.0 | 6.9 |
| Sprint | 12.0 | 13.8 |
| Crouch gait | 3.2 | 3.68 |
| Air wish cap | 12.0 | 13.8 |
| Arena loop sprint (Mega, Pocket, Stack) | 12 | 13.8 |
| Fall gravity multiplier | 1.50 | 1.62 |
| Terminal fall | 52 | 56.16 |

Coyote, jump buffer, cling, dash, lunge, punch reach, zip, pad, climb, slip, and wall-run were not in this retune. Slide entry, slide stay, and the slide jump cap were left alone. The enemy-ai, pocket-ai, and stack-ai proof lines move because the dummies use these speeds. That is the retune, not a behavior bug. The chase-ribbon length cap moved from 2.2 m to 2.53 m so the same 0.16 s streak still fits a 13.8 m/s sprint. Skill-route saves moved from 10–25% to -6–25% because ground sprint caught up with zip, climb, and wall-run, which stayed put. Pocket loop time moved from 17–20 s to 14.76–17.41 s. Stack loop time moved from 24.8–25.2 s to 21.50–21.95 s. The loops are the same length. Practice route times moved with the sprint: mega-beginner 7.550→6.600 s, mega-wall 2.650→2.483 s, mega-toy 2.500→2.317 s, pocket-beginner 2.017→1.983 s, pocket-toy 5.267→5.067 s, stack-beginner 5.383→4.717 s, stack-toy 10.033→9.050 s. Ground-nav beginner times on the skill routes moved the same way where the path is a sprint: ClingSkip 2.14→1.86 s, BowlLine 2.35→2.05 s, KnightLine 1.23→1.07 s, FortHop 1.88→1.63 s, WestZip 2.75→2.39 s, LaneZip 1.36→1.18 s, SouthPad 1.25→1.09 s. DomeClimb, LaneRun, YardHop, RoofZip, YardPad, and WestClimb stayed within a few hundredths because those comparisons are a zip, a pad, or a wall, not a sprint.

## Map and arenas

Mega Park, Pocket Park, and Stack Yard are all on this tip. Mega Park is the default (arena id 0). Pocket Park is 1. Stack Yard is 2. The pause Arena row and the digit keys pick them: **1** Mega Park, **2** Pocket Park, **3** Stack Yard. Try it: Title → Play, then press 1, 2, and 3 and read the arena name before you start.

The fence you can see is a low rail, 2.75 m, and the collider is that same rail. You can jump it. Falling below -2.5 brings you back. That pair is the same on every arena. Try it: run at the rail on each park. You should bump the rail you can see, and a jump should clear it.

Thirteen named zones cover the three parks, and each zone has a landmark you can read at eye level. Mega Park: West Yard, North Bowl, Mid Court, South Court, East Forts. Pocket Park: West Lawn, Center Court, Fast Lane, East Sand. Stack Yard: South Yard, East Lane, West Stack, North Roof. The split label reads `Name · Zone`. Try it: start a match and walk until the label changes zone.

Practice routes sit on every arena. Mega Park has South fringe, Cling lane, and Pad zip. Pocket Park has West lawn and Lane toys. Stack Yard has South apron and East toys. Free roam is still the first row on each park. Try it: Title → Practice, set the arena, and pick a route that belongs to that park.

## Movement mechanics

These five were approved before PR #92. They are on this branch and were not given new buttons.

- A punch that connects and does not transfer It staggers that runner for 0.25 s, then that same pawn ignores another stagger for 0.50 s. Sprint drops for the stumble. Try it: Title → Practice → Arena Mega Park → Dummy On → Start practice, then punch the dummy (LMB / West). In a match, punch while you are not It.
- After you leave a wall, that face stays closed until you land or touch a different face. A corner is a new face. Cling is still holding move into the wall. Try it: on Mega Park, hold into the north cling face, jump off, and hold into that same face again before you land.
- Five launch pads in Mega Park toss you on a fixed arc. Walking onto the pad is the input. The pad then waits 0.3 s. Try it: Practice → route Pad zip, or run onto the pad near the middle of Mega Park (the CrossB pad sits on that route).
- Five downhill zip lines ride at 14 m/s. Hold into the cable the same way you cling a wall. Letting go and grabbing again waits 0.3 s. Try it: the same Pad zip route, the west-rim cable, or any of the lines off the towers and forts.
- After It changes hands, the new It cannot tag the old It for 1.0 s. The pawn glows for that window. Try it: Title → Play → a match against one AI, tag them, and watch the glow while they swing back.

## AI

- The solo opponent chases and evades with the same motor as the player, and it keeps its loop targets on the Mega Park loop inside the fence. Try it: Title → Play → Match setup → AI opponents 1, Difficulty Normal → Start match. Tag the dummy and let it chase. While you are It, it runs the loop instead of leaving the park.

## Animation poses

- Slide, launch, zip, and stagger each have a visual pose. The speed of those moves is unchanged. Try it: slide (Ctrl / East), take a launch pad, grab a zip, and land a non-tag punch.
- Wall-run, climb, wall jump, air dash, and landing have visual poses. The wall-run body leans 20° so the lean stays past the air-strafe roll. Try it: Practice → route Cling lane for the wall-run and the wall jump, then air dash (Q / RB) and land.

## Onboarding

- Hints and button glyphs follow the device you are holding, and they follow a rebind. The first run walks the verbs that already exist. Pause → How to play, or Title → How to play, lists each verb with the live glyph. Title → Settings → Replay tips plays the hints again. In a split, each pane uses that seat's device.

## Sound

- Jump, land, slide, cling, wall jump, air dash, punch, tag, a blocked tag-back, stagger, launch pads, zips, the 3-2-1 countdown, and the round end each raise their own hook. Round start and the last-ten-seconds tick play too. Try it: play a short round on Mega Park and use one of each. Comma mutes. N mutes music.
- Every hook has its own baked clip, plus grass, concrete, metal, and wood footsteps, menu blips, and the playground music bed. The mix keeps 16 voices. World sounds fall off by 40 m. The It pawn's steps are a little louder. Try it: Pause → Settings and move Master, SFX, UI, and Music. Run across mulch, then a rail, and listen for the surface change.

## Front end

- A round counts down from 3, plays, and then ends. The verb cluster stays on screen with the It mark and the clock. Try it: start any match and wait out the 3-2-1. The verb HUD is the cluster during play.
- Pause (Esc / Start) opens Resume, Controls, Look sensitivity, Audio, Quit to title, Settings, Rebind, Arena, and How to play. Settings saves look, audio, and HUD. Rebind listens for a key or a button. South confirms, East goes back, and the stick or D-pad moves. A jump held across the menu does not fire when the menu closes. Try it: Esc → Settings → change FOV → Back → Rebind → Jump → press a key → Reset bindings.
- The game opens on a title card: Play, Practice, Settings, How to play, Quit. Play opens match setup: arena, AI opponents 0–3, difficulty Easy / Normal / Hard, round length 60 / 120 / 180 / 300 seconds, and rounds 1–5. Start match opens the join screen before the round. Try it: press Play in the Boot scene, then Play on the title.
- After the last round the card lists time as It and tags, then longest survival, then the winner (least time as It). A tie stays a tie. Rematch uses the same setup. Change setup returns to the card. Title clears the match. Try it: set round length to 60s and rounds to 1 so the card appears inside this session. Confirm skips an award highlight first.

## Couch split-screen

- Two to four humans sit down on the join screen, and AI fills only the seats that are left. Humans plus AI stay at or under 4. One keyboard player joins with a letter, number, Shift, Ctrl, or Alt. Each pad joins with any button. East or Esc on that device leaves it. Enter, Space, or South starts once that device is seated. Two humans split vertical, or horizontal when the Split row says so. Three or four use quadrants. With three, the last quadrant is the score list. Each pane has its own camera and verb HUD. The listener sits on P1, or on the average of the humans when Listener says Average. Try it: Match setup → Start match → press a key, then a button on a pad → set Split if you want the horizontal cut → Start match. Each person should move only their own pawn.

## Accessibility

- The Colorblind palette row cycles Default, Deuteranopia, Protanopia, Tritanopia, and High contrast. Player marks also use a shape (circle, square, capsule, diamond) and the It marker draws a star plus the letters IT. The Player row picks which seat you are editing. P1's palette tints the shared world plates. Try it: Title → Settings, or Pause → Settings → Player, then Colorblind palette.
- Captions are small icons, off until a seat turns them on. Footsteps of a nearby It, the countdown, a tag, a pad launch, and a zip grab each get an icon. One seat can leave them off. They stay inside that pane. Text size is the HUD scale row (0.75–1.50). Try it: Settings → Captions On, then play near the It, take a pad, and grab a zip.
- Rumble is Off, 25%, 50%, 75%, or 100% per seat. It pulses when you claim It, get tagged, land a punch, get staggered, land hard, or take a pad. A keyboard seat never rumbles. Off writes nothing. Try it: sit a pad, Settings → Player for that seat → Rumble 100%, then tag someone.
- Reduced flashing flattens the tag-back glow and holds the countdown digit steady. If any seated player turns it on, the shared glow and countdown take the calmer look. The 1.0 s window and the countdown length stay the same. Try it: Settings → Reduced flashing On, then tag someone and watch the next countdown.

## Practice

- Practice is a free arena. Nobody is It. The dummy wanders and does not punch unless you turn Dummy on, and even then it does not become It. Title → Practice. Routes are on every arena: Mega Park has South fringe (sprint and one jump), Cling lane (wall-run, then wall jump), and Pad zip (pad, air dash, zip). Pocket Park has West lawn and Lane toys. Stack Yard has South apron and East toys. Free roam is the first row. Set the arena or the route row stays on Free roam. Start practice begins the run.
- Each route keeps a personal best and checkpoint splits. The best run replays as a translucent figure with no physics of its own. G or left-stick press hides it. Recording continues while it is hidden. T or North (Y) restarts on the start gate. I or right-stick press shows which of Jump, Slide, Air dash, Punch, Sprint, and Cling are held. Those three keys are not rebind rows. Try it: run Cling lane twice and beat the first time so the ghost has something to show, then press G.

## Match stats

- The mode still picks the winner. Under that line, up to three awards and a card per pawn. Awards, in order, while someone has a real score: Hot Potato (most tags), Slipperiest (most near-misses inside punch reach), Sky Walker (most air time), Wall Crawler (most wall-runs). A tie lists every seat that shares the high value. A zero score gives no award. Cards use the seat color and a shape, and they use the HUD text scale. Try it: finish a 60s round and read the cards. Near-misses need you inside 1.55 m without a tag.
- The last 8 seconds are kept at 20 Hz. The final tag freezes that window. If nobody was tagged, the closest near-miss freezes it. Playback uses the same pose figures as practice. Confirm or Back skips the highlight, then the next press uses Rematch, Change setup, or Title. Try it: get one tag near the end of the round and let the replay run, then press Enter or South to skip.

## Profiles

- A couch profile keeps a name, a color, a body tint (Blue, Mint, Orange, Lavender, Tan, Red) and an accent, that profile's binds, accessibility, and its practice bests and ghost. Lifetime matches, wins, tags, and longest survival sit on a small card. On the join screen, click a seat line to cycle a saved profile or Guest. Two seats cannot hold the same profile. If both want the same swatch, the other one shifts. Guest is not saved, and a Guest's match does not write onto the profile that seat used before. Names show on the split label, the It chip, the off-screen arrow, the minimap dot, and the results card. A seat with no profile still reads P1–P4. An older settings file becomes one profile named Player. A brand-new file may only offer the open seat and Guest until a profile exists. Try it: Match setup → Start match, then click each seat line and read the card under the seats.

## Bug fixes

- F6 toggles a frame card: FPS, milliseconds, and the movement, AI, pose, HUD, audio, and round buckets. It stays off until you press it. F3 is still Trail Tag. The card is on once Play is loaded. The 4-player Mega Park frame stays under the same budget, and the hot path does not allocate. Try it: during a round, press F6, then press it again to hide the card.
- A NaN or infinite slider, or a settings file whose version is garbage or from the future, falls back. An older file still migrates. A missing file leaves the current settings alone. Try it: Pause → Settings → Reset to defaults if a row looks wrong.
- Pause, the end of a round, or unplugging a pad stops rumble instead of leaving the motors spinning. Try it: rumble at 100%, pause with Esc, and the pad should go quiet.
- Pause silences world sounds and the music bed. Menu sounds still play. A music volume of 0 does not start the bed. Try it: pause during a slide or a zip.
- An empty pad slot does not steal another player's pad. Try it: leave pad 1 empty and move on pad 2.
- Unplugging a pad during play pauses and asks that seat to reconnect. Plugging it back does not resume on its own. Try it: pull the pad, read the pause card, plug it back, then choose Resume.
- Any seated pad can open pause with Start, and the card uses that seat's accessibility rows. Try it: pause from P2's pad and change that seat's captions.
- Each seat keeps its own first-run hints. Try it: in a two-player split, advance one pane's hints and leave the other.
- The It hat, the tag-back glow, and the couch tint follow that pawn's palette, including a change made mid-round. Try it: pause, change P1's palette, resume, and look at the hat.
- Reduced flashing also calms the It hat pulse and bob. Try it: turn Reduced flashing on and watch the hat.
- Caption icons stop at the bottom of their split pane. Try it: captions on, two-player split, stand so a cue would draw low in the pane.
- A zip, a launch pad, a punch stagger, and a kill-box teleport start only while the round is playing. The kill height is -2.5. Pause keeps a zip you are already riding. Try it: pause on a zip and confirm you keep moving along it only as the ride you already had. Start a new pad or zip after the countdown, not on the results card.
- Rematch clears leftover stagger, tag-back, and positions. Equal time as It is a tie, and player 1 is not crowned for it. With three players the fourth quadrant shows the times. Try it: Rematch, and with three humans read the score pane.
- Rematch, Change setup, and Title do not leave a second roster or a second listener behind. Try it: Title, then Play again, and listen for a single mix.
- Soft-play ground uses the grass footstep. Sand stays on the hard step. Try it: run the mulch, then a sandy patch.
- A person who joined on a pad is not also steered by the keyboard. Try it: pad-only seat, move the stick, and leave WASD alone.
- Restarting a run clears a zip, a pad arc, a wall-run, a slide, a climb, or a stagger that was still going. Try it: Practice, grab a zip, press T before it ends, and confirm you are back on the start gate.
- Two practice checkpoints that overlap count once. Try it: run South fringe and watch each split tick one time.
- Pausing practice does not save a personal best. Try it: pause mid-route with Esc and resume. The best should be the one you already finished.
- Backing out of a run, or finishing slower, does not replace the best. Try it: finish a route, restart, and quit before the finish.
- The practice ghost plays only on the arena that route belongs to. Try it: record a Mega Park ghost, then switch the practice arena away and back.
- Changing the seated profile swaps that profile's best and ghost, and leaves the other profile's replay off the course. Try it: two profiles, two bests, cycle the seat on the join screen.
- A broken or oversized ghost entry does not occupy a replay slot. A good recording still loads.
- An older ghost entry still loads. A newer prefix is left unused.
- The practice input line shows the keys you rebound, for that seat. Try it: rebind Jump, turn Input display on, and press the new key. I or right-stick press toggles the line.
- An unplugged pad does not restart the run, hide the ghost, or toggle the input line. T, G, and I on the keyboard still do. Try it: unplug, then press those pad buttons. They should wait until the seat is live again.
- The practice dummy never becomes It. Try it: Dummy On, punch it, and confirm there is no It crown.
- Leaving practice puts the match arena, AI count, difficulty, length, and rounds back. Try it: set a 60s match, open Practice, change the arena, Back, and open Play again.
- Rematch clears the stat book and the highlight from the previous match. Try it: finish, Rematch, and confirm the new cards start clean.
- A tag in the opening seconds of the next round does not play a highlight from the previous round. Try it: tag immediately after the next countdown.
- A seat that leaves, or a pawn that is replaced, does not keep the previous body's tags. Try it: finish a match, change who is seated, play again, and read that seat's card.
- A tag in the first moments of a match still has a highlight. Try it: tag as soon as the round opens and watch the replay.
- The highlight cuts across a respawn instead of sliding through the teleport. Try it: if someone is brought back from the kill height during the replay window, the figure should jump to the new spot.
- One human versus AI does not draw an empty ghost in a vacant seat. Try it: a solo match's highlight shows the pawns who were actually in it.
- Nobody gets an award when every stat is zero. A four-player award with long names wraps instead of clipping at the large HUD scale. Try it: a very short round with no tags should list the winner without Hot Potato.
- Deleting the profile that is sitting in a seat clears that seat's palette, rumble, captions, and ghost. The seat goes back to what it had before. Rename and delete ask for a confirm. A second press without a new confirm does nothing.
- Rename refuses a blank name, a name that is only spaces, or a duplicate of another profile. A padded name is trimmed. Names allow letters, digits, and spaces, up to 12 characters.
- A long profile list on the join screen scrolls instead of sticking on the first rows. Try it: click the seat line until the list moves.
- An older settings file migrates into a profile named Player and keeps that profile's palette, captions, binds, and practice board. A future version of the file resets.
- Playing as Guest does not add that match's tags or win onto the profile the seat was using before. Try it: cycle to Guest, play, and read the previous profile's card.
- The It crown uses a different color from the seated player's swatch, so the crown still reads when the body is the same hue. Try it: look at the It hat against that pawn's color.
- Saving settings replaces the file in one step, so a crash mid-save does not leave a half-written `tag-settings.json`.
- Rebinding one profile does not change another profile's keyboard or pad table. Try it: two seated profiles, rebind Jump on one, and check the other seat.

## Smoother motion

The body, the camera, and the mesh ease through the moves that used to pop. Nothing about the speeds changed. A press still happens on the frame you press it. The animation does not make you wait.

- A punch, an air dash, a vault, a wall grab, and a grapple catch no longer slam the limbs into the new pose. They settle in about a sixteenth to an eighth of a second. The run and the walk were already smooth, and they stay that way. Try it: Practice, dummy on, punch, then air dash, then vault a rail.
- Climbing turns you toward the wall, and a wall run turns you along it, instead of the chest jerking to the wall. Letting go eases back. Your movement still faces the camera on the same frame, including on the ground. Try it: Cling lane, hold into the wall, then let go.
- Stepping up, a small skin push, and the end of a vault no longer pop the mannequin. The capsule still lands on the exact spot. A fall off the map still teleports, on purpose. Try it: run stairs or a low rail, then vault and watch the hips.
- Landing compresses the mesh and comes back, instead of squashing on one frame. A quick hop still skips the thud. Try it: jump off something about head height, then chain two jumps.
- The camera ducks for a slide and rises for a climb, and it slides sideways on a wall run, instead of jumping there. No zoom punch, no shake, no slow motion. Try it: slide, then wall-run the cling lane.
- The full list and the before/after numbers are in `Docs/SmoothMotionAudit.md`. A frame strip is in `Docs/SmoothStills/pass1/`.

## Climb and parkour, second pass

The climb, the wall run, and the vault now read as motion instead of a held pose. Speeds are the same. A press is still the same frame.

- Climbing hand over hand follows how fast you climb. At the full climb speed the hands plant and trade grips. Standing still on the wall is a two-hand hold. Sliding down drags both hands. Grabbing the wall starts with the hands coming up, then the climb. Try it: Cling lane, hold the wall, then climb, then let yourself slip.
- A wall run strides with the run, rolled off the wall, at the wall-run speed. Try it: sprint into the cling lane and stay on the wall.
- A vault keeps the knee that was already driving, and the body follows the plant, the knee, the fold over the lip, and the land. A wall jump shoves off and eases into the rise. A zip grab closes the hands on the cable, then the arms pump a little with the ride. A launch pad swings, tucks, and opens with the arc. A grapple pull hangs both hands on the line and leans the chest at the latch. Try it: vault a rail, wall-jump, take a zip, hit a pad, then pull with the left hand.
- A sideways pop off a ledge no longer jerks the mannequin when you are still in the same move. Pushing into a wall still keeps the mesh on the surface. Falling off the map still teleports. Try it: run off a low curb, then walk into a wall.
- The camera still snaps in when a wall is inside the boom, so the lens never sits in the wall. Coming back out to full length eases. A punch no longer zooms the lens. The camera nudges and settles. No shake, no slow motion. Try it: back into a corner, step out, then punch.
- Nothing here waits on an animation. Jump, slide, dash, punch, lunge, climb, wall run, wall jump, vault, zip, pad, grapple, and stagger all show on the frame they happen.
- Exit poses, the landing roll, cling marks, dash ribbons, and dust are a separate pass. Stick figures of the climb, the wall run, and the vault are in `Docs/SmoothStills/pass2/climb-wall-mantle.png`.

## Animation exits and the landing roll

Each move now has a short recovery you can see as it ends. The body eases back into the run, the idle, or the air pose. Jump, slide, punch, dash, or lunge peels that recovery off in about six hundredths of a second, and the new move shows the same frame. Nothing here changes speed, coyote, cling, or the slide.

- Wall-run: a push off the wall, or a foot reaching down if you are already dropping.
- Wall jump: a tuck, then the body opens.
- Climb top-out: a hand plant, the lead knee up, then a stand. A climb that goes into a mantle waits until the mantle finishes, then plays this.
- Cling drop: the hands open and the body falls off the wall.
- Vault: the trail leg sweeps through and you land in stride. This is a fast mantle.
- Mantle: both hands press the lip and the chest comes up. Slower than the vault, and not the climb top-out.
- Slide: a pop-up into the run. The slide itself is unchanged.
- Air dash: the stretch settles back to center.
- Punch: the right arm folds back and the weight sits back.
- Lunge: the reach collapses, weight back, then the stride.
- Zip drop: the hands leave the cable.
- Launch pad: knees take the landing, then you stand. A fall that is fast enough to roll uses the roll instead.
- Grapple: the left hand leads as you arrive, and it opens when you let go.
- Stagger: the stumble catches a step.
- Tag-back: the flinch shakes off when the one-second window ends.
- Ordinary landings: a knee bend that gets deeper as the fall gets faster.

A fall that reaches 65% of terminal speed (36.50 m/s down, a drop of about 18.69 m from a dead stop at the current fall gravity) plays a shoulder roll along your travel. Standing almost still at that speed is a short crouch instead. You keep your speed. There is no extra stun. The camera does not roll. A small dust puff and the hard-land sound mark the shoulder. Try it: fall from high enough that the drop is about 19 meters, land while running, then land again with no stick. Jump during the roll and the jump should win immediately.
