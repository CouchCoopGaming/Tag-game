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

## Run, jump, and contact, third pass

The stride, the jump, and the hands and feet read as one move. Speeds are the same. A press is still the same frame.

- Sprinting at 13.8 and walking at 6.9 keep the feet with the ground. The step used to skate at a sprint because the cycle capped early. Stopping still plants and settles. A fast about-face plants the outside foot, then eases back into the stride. Strafing and backpedaling use a shorter step instead of the forward run. Try it: Practice, dummy on, sprint, stop, spin, then hold sideways and back.
- A jump still crouches, tucks, and opens by how fast you are rising or falling. At the top the body floats for a moment. Chained hops keep the arms out instead of restarting the crouch. Air-strafe still banks the chest. Try it: hop twice, then hold a side key in the air.
- Feet pitch onto stairs, slopes, and ramps. On a climb or a wall run the hands sit on the wall. The capsule does not move for either of those. Try it: run a stair or a sand ramp, then wall-run the cling lane.
- The head turns a little toward where you are going, the chest counters, and the arms follow through after a stride. Try it: sprint, then let go of the stick and watch the arms settle.
- The camera eases tiny look steps. A fast mouse turn still snaps. No zoom punch, no shake, no slow motion.
- Nothing here waits on an animation. The before/after numbers are in `Docs/SmoothMotionAudit.md`. Stick figures are in `Docs/SmoothStills/pass3/loco-air-ik.png`.

## Body and transitions, fourth pass

The body fills in the poses that were still a single shape, and the hard cuts between moves ease. Speeds, slide, and the press timing are the same.

- Changing move no longer snaps the mesh through a huge pose. The big ones were air into a punch (228.8° down to 17.4° on one frame) and the vault exit (0.710 m down to 0.086 m). A normal running step is left alone so the feet stay with the ground. Try it: punch out of a jump, air dash, then vault a rail.
- Standing still breathes, shifts weight, and glances around. The It holds the chest a little higher than a runner. Crouch-walking keeps the feet under you. A slide leans, drags a hand, and looks where it is going. Sprinting pumps the arms harder than walking. Getting tagged or punched flinches on the same short stumble you already had. Try it: stand, crouch-walk, slide, sprint, then tag the dummy.
- Slipping on a wall scrabbles. A wall run plants the feet as it catches, and climb and wall run blend into each other. Try it: cling, let yourself slip, then wall-run into a climb.
- The numbers are in `Docs/SmoothMotionAudit.md`. Stick figures are in `Docs/SmoothStills/pass4/idle-wall-flinch.png`.

## Motion gallery, fifth pass

You can watch every move in one scene, and a punch no longer freezes the legs.

- Tag → Motion Gallery opens a row of dummies. Each one loops one move on the real body: run, climb, wall run, vault, mantle, slide, dash, punch, zip, pad, grapple, stagger, and idle It next to a runner. The camera orbits. F flies. T slows time in that scene only. A normal game still starts in the park, and slow motion in a match stays off. Try it: Tag → Motion Gallery, press Play, then press T.
- Punching, winding up a lunge, or aiming the rope moves the arms and turns the chest. Running, sliding, jumping, and wall-running keep their legs. Try it: sprint and punch, then punch in the air.
- Hanging on a zip sways and trails the legs harder as the ride hits 14. A grapple pull leans the chest along the rope and lets the legs trail. A launch pad windmills the arms on the way up and settles them on the way down. Try it: take a zip, pull the rope, then hit a pad.
- The hard cuts between moves are the same as the fourth pass. A press is still the same frame. Stick figures are in `Docs/SmoothStills/pass5/layer-hang.png`.

## Controls, sixth pass

The stick and the look respond on the frame you move them. Speeds, jump, and slide are the same.

- Turning the camera now turns the body before the capsule steps, so the first move of a look goes the way you are facing. Keyboard and gamepad movement were already the same frame. Mouse look was already on the picture the same frame. Try it: flick the mouse and push forward together.
- A gamepad stick uses a round deadzone. A light diagonal used to disappear, and a small push used to jump from nothing to a big step. Full tilt is still full speed, on a diagonal too. A half push is a little softer than before (about 0.38 of full, where it used to pass through as 0.50). Keyboard movement is unchanged. Try it: walk the stick in a slow circle, then hold it in the corner.
- Mouse look has no smoothing and no acceleration. Gamepad look can use an acceleration curve, and that curve is off until you turn it up. The inner deadzone, the outer edge, the stick curve, and that look curve are saved with the other settings. The pause list does not have new rows yet.
- The game waits for the display (vsync on). The physics step is unchanged, and the body still moves once per frame. Try it: sprint a straight line and watch the stride stay even.

## Motion, seventh pass

The feet stay with the ground, a fall off the map blinks instead of popping, and a vault no longer jerks backward at the end. Speeds and the jump are the same.

- Walk, sprint, crouch, and wall-run plant the sole. The old step was short for the current speeds, so a foot slid: about 11 cm on a walk, 52 cm on a sprint, 62 cm in a crouch, and 30 cm on a wall run. Those are now 0. A side step still shortens the leg that is in the air. The foot on the ground takes a full step. Try it: sprint, then crouch-walk, then run a wall.
- Falling out of the map, or restarting practice, still puts you on the pad immediately. The body hides and the camera pulls in for about a tenth of a second, then both come back. Starting a round does not do this. Try it: drop off the park.
- A vault finishes on the spot you stand, instead of reaching past it and snapping back. How long the vault takes, and how fast you leave it, are unchanged. Try it: vault a rail and watch the last moment.
- A very fast turn no longer spins the whole body with the capsule. The capsule still faces the camera at once, so the move goes where you look. The mesh catches up. Try it: flick the mouse through a half turn while you run.

## Climbing, eighth pass

Hands and feet meet the wall, the lip, and the rail. Climb speed, slip speed, and wall-run speed are the same.

- A climb plants one hand and the opposite foot, then swaps them. Those contacts used to slide about 60 cm (hand) and 83 cm (foot) each step. They now stay on the wall. The chest stays about 42 cm off the surface, including on a bumpy wall. Try it: climb the gallery wall, or any wall in the park.
- Grabbing the wall eases the hands onto it over the same tenth of a second as before. They used to appear on the surface. Try it: jump at a wall and hold forward.
- At the top, the hands settle on the actual lip and the body rolls over them. A vault does the same on the rail you actually hit, at that rail's height. How long the move takes is unchanged. Try it: vault a low rail, then mantle a taller ledge.
- A wall run leans into the wall more as you go faster, up to the same 20° at full speed. The lean eases in and out. The inner foot stays on the wall. Try it: join a wall run slowly, then at speed, then jump off.
- Sliding down a wall drags the hands and adds a small wobble, like friction. The slip is still 3.7. Try it: climb, then pull back.

## Motion, ninth pass

The wall jump, the rope, the zip, the launch pad, a landing, the dash flash, a hard turnaround, and a punch while you run all ease instead of snapping. Speeds, the jump, and the dash length are the same.

- A wall jump used to hold the shove, then throw the arms into the tuck in one step of about 36°. The arms now arc across the same shove and the same ease, and the biggest step is about 17°. Try it: wall-run, then jump off, and watch the arms through the push.
- On a rope the chest used to kink about 70° off the line. It now lies along the rope, and letting go eases that line off. The pull is still one click, and the drop is still a double-click. Try it: latch a grapple, hang, then double-click to drop.
- Grabbing a zip brings the hands up to the cable. They used to stop short by about 58°. Dropping off used to snap the arms in one frame (about 128°). The drop now eases, and the biggest step is about 53°. Ride speed is still 14. Try it: catch a zip, ride it, then let go.
- A launch pad used to throw the arms into the full swing on the first frame (about 155°). They now open over a sixth of a second, and the knees soften on the way up. The pad wait is still 0.3 s. Try it: run over a launch pad and watch the takeoff.
- Landing while you sprint used to fold the thighs about 104° off the stride. The legs now keep the stride, and the give stays in the knees. A hop that is faster than one frame still skips the thud. Try it: sprint off a ledge and land still running.
- The air-dash flash used to pop on at full strength. It now opens over a short beat, and the ribbons follow it. The dash is still a tenth of a second. Try it: air dash and watch the flash on the first frames.
- A hard stick reversal used to flip the stride in one frame, about 83° at a sprint. The legs now cross over about a seventh of a second. You still turn with the camera immediately. Try it: sprint, then snap the stick backward.
- A punch or a tag while you run leans the chest so the fist comes forward about 13 cm. Standing still keeps the old strike. Reach and the lunge are unchanged. Try it: sprint and punch, then punch while you stand.

## Motion, tenth pass

Running, starting, stopping, turning, idling, crouch-walking, and sliding ease instead of snapping. Speeds and the slide decay are the same.

- A sprint swing used to kink the shoulder about 52° in one frame. The arms now swing opposite the legs, about 46° at a sprint, and the biggest step is about 29°. The chest leans forward once you are up to speed, and the head stays steadier than the hips. The feet still plant. Try it: sprint a straight line and watch the arms and the head.
- Starting used to throw the chest forward on the first frame. It now leans in over one step, then settles. Stopping plants the lead foot and eases into idle instead of cutting the stride off. Try it: stand, sprint, then let go of the stick.
- A turn leans with how fast you turn. A sharp turn while walking eases the outside foot around. Idle still breathes, shifts weight, and looks around. Crouch-walk stays a low stride at the same 3.68. Try it: walk a tight circle, stand still for a few seconds, then crouch-walk.
- Sliding in and out used to drop the body in one hard step. It now eases down and back up. How fast the slide slows down is unchanged. Try it: sprint, slide, then come back up to a run.

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

## Deeper exits and a real shoulder roll

The recoveries from the last pass now move like a person changing their mind, and the fast landing is a parkour roll instead of a crouch with a tip.

- The roll turns the mesh a full circle over the lead shoulder. The chin tucks, the lead arm sweeps, the shoulder touches, the back crosses, the legs come over, a foot plants, and you are running at the speed you landed with. The body stays on the ground. The capsule does not roll, and the camera does not roll. Try it: fall about 19 meters while running, and watch the shoulder. Strafe as you land and the other shoulder leads. Land again with the stick straight and the lead shoulder swaps.
- Standing still at that same fall puts both hands on the ground, then you stand up. Try it: the same drop, but let go of the stick before you hit.
- Shorter falls have three landings. A small hop keeps the chest up. A medium fall bends the knees. A hard fall, still under the roll, brings the hands down near the ground. None of them change your speed. Try it: step off a curb, then a first-floor roof, then something just under the roll.
- Leaving a wall, a vault, a slide, or a climb plays the matching side. The arms follow the chest, and the head is the last thing to settle. Try it: wall-run the left wall and the right wall, then vault and pop into a slide. The slide should arrive without a snap.
- Jump during the roll and the jump shows within a blink. The roll does not make you wait.

## Dust and comic words

Footfalls kick up dust, and a punch or a tag pops a comic word. Speed, slide, and the camera stay as they were.

- Walking is a small puff. Sprinting is a thicker cloud. Starting a run, spinning hard, and dropping into a slide kick more dust. The slide leaves a trail. A wall run scuffs, and a vault puffs at the hands. Grass is a light fleck, sand and mulch hang in the air, concrete fades fast, wood is a faint splinter, metal is almost quiet unless you pivot, and a wet surface splashes. Try it: sprint the lawn, then the sand, then a hard turn on the concrete.
- A punch shows POP! or POW!. A tag shows BAM! or WHAM!. The same word does not repeat on the next hit. The letters face you in every split pane. Pause → Settings → Comic words turns them off. Reduced flashing hides the dust and the words. Try it: Practice, dummy on, punch, then tag.

## Verb effects and comic lettering

The contact words now read as a comic burst, and the big moves leave a trail you can see. Speed, slide, and the camera stay as they were.

- POP!, POW!, BAM!, and WHAM! each have their own explosion shape and colors. A thick black outline, a second burst the letters overlap, and print dots you can see. The letters are big, tight, and arched, with a black stroke, a thin white inner line, and a block shadow. A tag adds speed lines behind BAM! and WHAM!. They punch in a little too big, wobble, and fade on the same short beat as before. Try it: punch the dummy, then tag.
- Pause → Settings → Effects is Off, Low, or Full. Full is the default. Off hides the dust, the words, and these trails. Low draws fewer of them. Reduced flashing hides them too.
- A hard landing stamps a ring and flicks debris. The shoulder roll swirls dust along the path. A dash leaves a few ghosts in your color. The rope sags, wobbles, chips the hook point, and snaps when you let go. A pad throws an up ring and a wind streak. A zip sparks along the ride. A stagger puts dizzy stars over the head for the same quarter second. Tag-back keeps its one second and pulses a colored rim so you can see who is safe in every split pane. A wall run scuffs, and a wet wall drips. Sprinting leaves wisps. Try it: fall, dash, rope, pad, zip, then take a hit.

## Landing ring, dizzy stars, and a wet wall

BAM! and WHAM! sit a little larger in their bursts, and the big moves you own in this pass read more clearly. Speed, slide, and the camera stay as they were.

- BAM! and WHAM! are about a tenth bigger inside the explosion, so they match POP and POW. Try it: tag the dummy and look at the letters against the burst.
- A small hop stamps a small ring. A medium fall stamps a wider one and throws more debris. A heavy fall, and the shoulder roll, stamp the full ring. The roll also swirls dust along the path. Try it: step off a curb, then a roof, then fall about 19 meters while running.
- A dash leaves a short trail of ghosts in your color. Low effects draws one fewer. Try it: dash, then set Effects to Low and dash again.
- The rope still sags and wobbles. The hook chips the surface, and letting go snaps. Try it: click to pull, then double-click to release.
- A stagger puts three comic stars over the head for the same quarter second. They use the same burst, outline, and print dots as the words. Try it: let the dummy punch you.
- Tag-back still lasts one second. A colored rim pulses on the safe body in every split pane. Try it: get tagged, then look at the body before you can be tagged again.
- A wet wall drips when a foot plants. Try it: wall-run a surface named wet.
- Pause → Settings → Effects is still Off, Low, or Full. Off and Reduced flashing hide these trails.

## Softer trails and five quieter exits

The landing ring, the dust, the dash ghosts, and the wet drips now read as printed art instead of a placeholder. Speed, slide, and the camera stay as they were.

- A landing stamps a soft ring in the color of the ground. A heavy fall cracks that ring. Debris is a short puff that grows and fades. The shoulder roll leaves the same puff along the path. Try it: hop, then fall hard onto concrete, then onto dirt.
- A dash leaves a tinted afterimage with a bright rim. It fades out instead of popping off. Try it: dash.
- The rope still sags. The hook chips the surface with a small cracked stamp, and letting go eases the snap away. Try it: click to pull, then double-click to release.
- Dizzy stars grow in and fade out over the same quarter second. Try it: let the dummy punch you.
- A wet wall drips a real droplet when a foot plants. Try it: wall-run a surface named wet.
- Leaving a zip, a hard standing land, a wall jump, a climb, or a mantle eases in a little longer. Jump, slide, punch, dash, and lunge still peel an exit off in the same short window.

## A real ghost and a tucked roll

A dash now leaves copies of your body, and the fast landing rolls over the shoulder instead of planting the head. Speed, slide, and the camera stay as they were.

- The ghosts are your silhouette at the last few steps of the dash, in your color, with a bright rim. The one closest to you is the strongest. The ones behind fade out. Try it: dash, and look at the trail. It should read as you, not as a flat card.
- The roll tucks the chin and bends the lead arm into a hoop. You go over that shoulder, across the back, and up into the run. It is still the same half second, and it still waits for the same fast fall. Try it: fall about 19 meters while running, and watch the shoulder come down first. The head stays tucked.
- An exit eases in once. The live pose joins the recovery over the first 22% of the clip. The extra 0.16 s fade is not stacked on that join. A stagger, and the end of tag-back, still fade in on the short blend, because those two do not join. Try it: leave a wall jump, then take a stagger. The wall jump should leave the shove without a second fade.
- Sprinting draws pale speed lines. A wall run or a climb throws sparks at the chest and the hand, above the foot scuff. A tag throws a gold burst with no letters. Becoming It, or losing it, swells once on the body and rings outward. A pad leaves a cyan trail and a zip leaves a violet one. Try it: sprint, wall-run metal, tag, then take It.

## Dash trail, shoulder roll, and a comic tag

The pictures for the dash, the roll, and the tag now show the move from the side, on grass and concrete, in warm light. Speed, the roll timer, and the camera in play stay as they were.

- The dash ghosts trail behind you. Each one is your color and a step more transparent, with a bright rim, and they overlap your body. They are not white cards standing in a row. Try it: dash, and look back along the path.
- The roll reaches one hand down, tucks the chin, goes over the shoulder with the legs in, and comes up into the run. The whole body stays in frame. It is still the same half second, and it still waits for the same fast fall. Try it: fall about 19 meters while running.
- The tag picture is a jagged burst at the hand. Black outline, print dots, a white flash, speed spikes, and no letters. The words still say POP, POW, BAM, and WHAM. Try it: tag the dummy, then compare the hand to the still.
