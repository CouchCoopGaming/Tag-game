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

## Front-end menu

There is a real couch menu now, in the Mario Kart shape without a kart step. Press Play on Boot, or on Play before a match is armed. The title asks for Start, the south button, or Space. Any connected pad or the keyboard can press it.

The main tiles are Play, Practice, Options, Controls, Credits, and Quit. Online is on the screen and does nothing (coming soon). Play joins up to four people, each person picks a color on the Hier grid and presses ready, then the group picks the mode, the round rules, and the park. A short loading card names the arena and shows one control tip, then the match starts the same way it always has.

During a match, Start opens pause for whoever pressed it: Resume, Restart, Options, Controls, or Quit to menu. The split stays visible behind a dim. When the round ends, a results screen lists who spent the least time as It, with the mode's own result line on top. From there you can play the next round, change the park, change characters, or quit to the menu.

Options covers the volumes, the shared look (sensitivity and invert are still one setting for the couch, not per player), the video mode, and reduce motion for the menu slides. Controls rebinds the actions that already exist. Space still jumps. An unknown saved Jump key is put back to Space. A real rebind such as E stays, and Space still jumps beside it.

The pictures in `Docs/UiStills/pass1/` are mockups, drawn to show the layout. They are not captures from the Unity editor. The screen order and the leftover list are in `Docs/UiPlan.md`.

The old title cards are still in the game. Set the PlayerPrefs key `Tag.Ui.Legacy` to 1 if you want those instead. Nothing about how the pawn moves was changed for this menu.

The menu's second pass is the look. Tiles are chunky and rounded, with a thick edge and a shadow. The background is a bright gradient and a slow chevron. Moving between screens slides the page and a gold bar in about a fifth of a second. The bottom chips say Arrows or Stick, Space or South, and Esc or East, matching the last device you touched. The letters are Liberation Sans Bold. The license sits next to the font file. No new paid art.

On character select, each player gets a colored panel and a turning preview. In the Unity editor that preview is the Hier mannequin for the color you picked. It breathes and shifts its weight the way the idle pose already does. A green READY! stamp lands when that player locks in. After you run Tag → Menu → Bake Hier Catalog And Arena Thumbs, a built player uses those same meshes. Before that bake, the preview is the primitive mannequin with the same colors.

Arena cards show the overview photo of Mega Park, Pocket Park, or Stack Yard, and the big panel on the right follows the highlighted card. Results put people on steps in the order that mode actually uses: Hot Potato by round wins (first to 2), Least It by time as It, Trail Tag by who is still standing, Free play by tags with no winner. Confetti shows when someone won, and it stays off if reduce motion is on. The pictures for this pass are mockups in `Docs/UiStills/pass2/`.

The third pass bakes the Hier catalog, the arena photos, and a portrait of each mesh when you open the project or make a build. You do not have to run a menu command first. The main tiles have icons. Character names stay inside the tile. TAG is a slanted logo with a streak. Join slots show a keyboard or a pad. The highlighted tile bobs, and a confirm punches in. Ready and the match start use sounds that were already in the game, and the menu starts the playground music if that bed is present. Mockups are in `Docs/UiStills/pass3/`. Real shots land in `Docs/UiStills/captures/` after Tag → Menu → Capture Screens.

The fourth pass fixes three pictures from those mockups and adds the in-match HUD. The Credits star is cream on the gold tile, so it reads as a star. The bottom chips show arrow keys, a space bar, and Esc, or a stick and the A and B face buttons. Character select keeps all four seats on screen. An empty seat says Press A / Space to join, in that seat's color, and a free device can sit down from that screen.

During a match each split has a colored frame, the player's name, and a gold IT badge on whoever is It. The clock and the round number sit at the top center, big enough to read from the couch, including in a four-way split at 1080p. Least It shows time as It, Hot Potato shows round wins, Trail Tag shows who is still in, and Free play shows tags. Dash is a ring that fills over its cooldown, the rope says when it is hooked or pulling, and the tag-back timer counts the safe second. An arrow on the edge of your pane points at the It, or at the nearest runner when you are It. YOU'RE IT!, TAGGED!, the 3-2-1-GO, and ROUND END take the middle of the screen. Reduce motion keeps those still. The old OnGUI hud comes back if PlayerPrefs `Tag.Ui.Legacy` is 1. Mockups are in `Docs/UiStills/pass4/`. Capture Screens also writes a 1-player, 2-player, and 4-player HUD shot.

The fifth pass closes the couch loop. Pause is a card on every split, with Resume, Restart round, Options, and Quit to menu. Whoever pressed Start is named on the banner, and any pad or the keyboard can move the highlight. Results list tags, time as It, and round wins on each standing, in the order that mode already uses. The rows fade in unless reduce motion is on. From there you can rematch, change the mode, go back to character select, or return to the main menu.

Options is a short set of pages: audio sliders, display (resolution, fullscreen, vsync), and accessibility (reduce motion, text size, colorblind-safe player colors). Controls shows the bind list and can still change one bind. Space still jumps. The stick rows read StickInner, StickOuter, StickCurve, and LookAccel. Screens take a fifth of a second to slide, and buttons do nothing during that slide. Back always has somewhere to go. If a pad drops out, a card asks that player to plug it back in and Resume waits. Mockups are in `Docs/UiStills/pass5/`.

The sixth pass is the juice. The title drifts across the arena photo, the TAG logo bobs, and Press Start pulses. Reduce motion holds those still. A highlighted button scales, a pale bar sweeps across it, and a confirm still bounces. Move, confirm, back, join, and a rejected bind each play a different clip that was already in the game. Join and confirm can buzz the pad. That stays off when reduce motion is on, when that seat's rumble is 0, and always for the keyboard.

Everyone ready starts a three-second count, Starting in 3, then 2, then 1. If anyone unreadies or backs out, the count stops. Each mode shows a How to play card with one rule line and a small diagram. Arena cards add the size and a flavor line: Mega Park is 160 x 100 m, Pocket Park is 80 x 50 m, Stack Yard is 110 x 70 m. A little top-down map draws that park's spawns and pads. Player colors match on the join card, the character card, the results row, and the figure.

The results panel is headed RESULTS. The figures stand on steps: first in the center, second on the left, third on the right, fourth on the floor beside them, in that player's color. The winner raises a fist and beats the chest. The others drop their shoulders or stumble. Confetti falls for the winner and stays off if reduce motion is on. The heading on that panel is RESULTS. Mockups are in `Docs/UiStills/pass6/`.

The seventh pass opens on a scene. TAG is the same slanted logo, centered and large, with no panel behind it. Mega Park fills the background and drifts. Four figures in the player colors run and vault across it. The edges fall off in a vignette. Under the logo is one button picture for the last thing you used, a space bar or South. There is no list of button names.

A headless walk, `ui-flow`, drives every screen with the keyboard and again with a pad: title, main, join for one through four seats, character, mode, arena, loading, the match, each pause choice, each results action, each options page, and back. It fails if a screen is a dead end, if Back goes nowhere, or if the highlight falls off the list.

The last mode, arena, rules, and each seat's character and color stay in the settings file. Rematch keeps them. A pad can join while you are still in the lobby. If a pad drops, another pad can take that seat and keep the character. If you try to start with fewer people than the mode needs, the screen says so. Free play needs one player. The tag modes need two.

The menu bed and the short stingers (move, confirm, back, join, error, ready, start, results) are hooks beside the match sounds. They use clips that were already in the game. Mockups are in `Docs/UiStills/pass7/`.

The eighth pass is for the couch TV. Display has a UI scale from 80% to 130%, and the menu and the HUD both use it. The edges keep a margin. The small type stays big enough to read from the couch on a 1080p set. The headless check measures that.

The footer, the title prompt, the join cards, and the binds list use the pictures for the pad in your hands. An Xbox pad shows A and B. A PlayStation pad shows Cross and Circle. A Switch Pro pad shows B to confirm and A to go back. Anything else stays South and East. The keyboard stays arrows, Space, and Esc.

During a match, hold Select (or Tab) and only your split shows the standings, the round, and the time. A short feed in the corner says who tagged whom, in that player's color, three lines at most, then it fades. Round 2 and the last round get their own card. A tie shows SUDDEN DEATH. One win away from ending Hot Potato shows MATCH POINT.

The loading screen names the park, gives one real tip, and the bar moves only when the match has actually been started and then when the round is going. It does not fake a fill. The IT badge and the player name no longer sit on top of each other in the top-right, including a two-way and a four-way split. Mockups are in `Docs/UiStills/pass8/`.

The ninth pass follows the pad in your hands. A Switch Pro pad confirms with A and goes back with B, the same way Nintendo and Mario Kart do. Xbox and PlayStation still confirm on the south button and go back on the east button. Controls lets each seat pick Confirm: South or East. Jump stays on the south button, and Space still jumps.

Rules can set the round length, how many rounds or wins end the match, who starts as It, a handicap word on each seat, and whether launch pads and zip lines are on. Those lines are saved and shown while the match loads. The handicap is only a label. Practice keeps the pads and the zips on.

On character select, Down on the bottom color opens a keyboard you can drive with the pad. OK writes that name onto the local profile. Records, next to Credits on the main menu, shows that profile's matches, wins, and tags. Credits names the team, the font license, and the tools, and Options opens it too.

Highlighted text is dark on the bright tile. A gold ring marks the control you are on. At 130% the words stay inside the panels. Mockups are in `Docs/UiStills/pass9/`.

The tenth pass is the look. Tiles have a bevel, a gloss band, and a soft shadow. The background is a layered sky with the four player colors drifting behind it, and a slow Mega Park photo on the title, the main menu, arena select, and the loading screen. Headings use Bangers. The rest of the words stay Liberation Sans Bold. Both licenses are next to the fonts, and Credits names them.

Each character stands on a turning disc, idles, and hops when that player hits Ready. The READY stamp, the 3-2-1, and GO all punch. Move, confirm, back, ready, and start still play through the menu sounds. Confirm and ready can buzz the pad when rumble is on. The gold ring and the couch type size stay. Mockups are in `Docs/UiStills/pass10/`.

The eleventh pass puts the real Hier figure in the four player colors on the title, on character select, and on results. The title asks PRESS A on a pad and PRESS START on a keyboard, and the line pulses. TAG is a comic lockup: Bangers, a thick outline, a highlight, a dotted shadow, and a tilt. The page behind it is a golden-hour photo of Mega Park, a little soft, with a vignette. The main menu shows two figures mid-chase and a tip of the day. Each character card has the color row, the pad that joined, and a bigger READY! burst. Space still jumps. Mockups are in `Docs/UiStills/pass11/`.

The twelfth pass shows that prompt once: PRESS and the button, or PRESS START. Character cards keep the name, the colors, the hat, and ready on two lines that stay inside the card from 80% to 130%. Each figure stands full body on its disc. Results puts the four places on a stepped block in front of Mega Park, with a celebration, a clap, and a slump, and the first-place card shows the round wins in full. Space still jumps. Mockups are in `Docs/UiStills/pass12/`.

The thirteenth pass centers each button label under the highlight stripe and puts the sublabel on a second line. READY sits in the corner of the portrait so the face stays visible. The card frame is the seat, marked P1 to P4, and the color you picked stays on the figure and the swatch. Results uses that same seat bar. The four figures stand larger on their own blocks, each with a contact shadow, and the time reads as seconds as It. The title crew stands on discs under the lockup. Space still jumps. Mockups are in `Docs/UiStills/pass13/`.

The fourteenth pass insets each arena photo inside the navy frame. Mega Park, Pocket Park, and Stack Yard each show a daylight view with the pads, a zip, and the loop. The two figures on the main menu stand on discs, and that screen's header reads Menu so the lockup is the only TAG. Play, Practice, Options, and Controls use the same gap under the stripe as the color swatches. The hat and ready line sits inside its box, and the 3 and 4 on results sit in front of the discs. Space still jumps. Mockups are in `Docs/UiStills/pass14/`.

The fifteenth pass keeps every button title on the comic face and every sublabel on the body face, and the sublabel uses the width of the button. Controls lists grapple, the cling hold, zip, and the launch pad the way they are bound. The chase discs clear the edge and the tip. Loading says Starting It, then Random, with no confirm or back hint. Pause reads Paused by P1. Mega Park is framed lower, with a zip and a pad in front. Space still jumps. Mockups are in `Docs/UiStills/pass15/`.

The sixteenth pass fills the tip of the day with three lines that match the binds, and Practice says you can free run any arena with no tagger. Controls scrolls, with the gold bar showing there is more past the first window, and the last window is in the same set. The arena cards use the daylight overview of each real park. A one-round match does not list a win target beside it, and handicaps read none until a seat is set. The loading card uses a blurred Mega Park photo. In a match the timer, the IT badge, and the verb words use the comic face, each seat shows its tag count, and a Comic words hint sits under the clock, on a two-player split and a four-player split. Space still jumps. Mockups are in `Docs/UiStills/pass16/`.

The seventeenth pass puts the match HUD on a chase view for each seat, in the two-player split and the four-player split. The rings sit in the bottom-left corner and stay small, and a short label shows only while that ability is not ready. The clock is a slim plate on the seam. The options screen and pause read COMIC WORDS ON. A tag moves the IT badge, flashes the tagger's color on the screen edge, and ticks that seat's tag count. Space still jumps. Mockups are in `Docs/UiStills/pass17/`.

The eighteenth pass puts the Hier mannequin on each chase, in that seat's color, in the middle of a run. The ground keeps the Mega Park materials and reads in the warm afternoon sun. When someone is tagged, that view says YOU'RE IT in the comic face, and the edge flash is the tagger's color. Menu to characters, loading to the match, and the match to RESULTS each play as a short comic wipe. Space still jumps. Mockups are in `Docs/UiStills/pass18/`.

The nineteenth pass takes the yellow wash off the chase. Trees stay green, concrete stays grey, the paths stay tan, and the sky stays blue, with only a little warm sun. The runners are lit from that sun, and each one has a contact shadow under the feet. On RESULTS the raised hands stay in frame. The places read 1st, 2nd, 3rd, and 4th, and the stat lines stay on the cards. Space still jumps. Mockups are in `Docs/UiStills/pass19/`.

The twentieth pass makes the chase path a light warm tan, and the concrete a lighter grey. On RESULTS the place stays on the step, and the cards keep 1st, 2nd, 3rd, and 4th. The 4th figure stands on a step. Character select shows the four Hier runners in the seat colors, each standing a different way, with READY on the seats that locked in. Space still jumps. Mockups are in `Docs/UiStills/pass20/`.

The twenty-first pass keeps the tan chase path and the RESULTS cards. On character select the runner uses the two colors named on the card. The chest is the first, the body is the second, and the highlighted swatch is that first color. The card is a dark navy panel with a seat-colored border. The runners stand larger, and the joints are charcoal. The purple lip on the P2 path is a grey concrete curb. Space still jumps. Mockups are in `Docs/UiStills/pass21/`.

The twenty-second pass carries that color pair into the match and onto RESULTS. The runner and the place figure use it, and the stat card accent uses the first color. The seat color stays on the card border, the screen edge, and the P# tag. P4 is Lavender / Mint, so it does not match P1. A taken pair is gray on the other cards. READY sits under the color row. Pause keeps COMIC WORDS ON inside the panel, and options puts that line on a navy plate. Space still jumps. Mockups are in `Docs/UiStills/pass22/`.

The twenty-third pass makes the first color the body you see in the chase, and the second color the chest, the hands, and the feet. P2 and P4 no longer both read mint. A taken first color is gray on the other cards. The title and the menu sit on a pass over Mega Park with the four runners, and the menu buttons have room between them. Space still jumps. Mockups are in `Docs/UiStills/pass23/`.

The twenty-fourth pass lifts the title camera so the fence stays under the frame and all four runners read on the path. The menu keeps that line clear of the lockup and the buttons. P2's card is the same solid body as the others. The seat color stays on the P badge, and the big frames stay a quiet stroke. A menu pose keeps each body part out of the scenery and out of the other parts, checked every frame at 30 fps, with joined neighbours clear past 3 cm of the joint and a 0.5 cm depth limit. The shown run, vault, idle, and results beats stay a small share of those poses, and the confetti falls in front of the runners. Space still jumps. Mockups are in `Docs/UiStills/pass24/`.

The twenty-sixth pass takes that scale off. Idle breathes and shifts its weight, character select holds a ready stance, the RESULTS winner leans back, and last place bows. The hip shell already rests about 4 cm inside the thigh, and a pose may not push that, or any other overlap, deeper than 0.5 cm. A raised arm, a clap, a vault knee, and a full stride still sink on this rig, so they wait for the trimmed-rig candidate. The title camera stays up, the fence stays under the frame, and P2's body stays solid blue. The title and the character cards paint the seat color, the same red, blue, yellow, and green as the P badges. The far-right title runner clears the bar rail, and the gap is on the no-clip line. Space still jumps. Mockups are in `Docs/UiStills/pass26/`.

The twenty-seventh pass stops subtracting that rest depth. The 0.5 cm limit applies to the raw sink, and joined neighbours are still exempt only within 3 cm of the joint. Every menu frame fails. The deepest sink on each pose is the right upper leg inside the pelvis shell, 9.50 cm. The neck, knee, elbow, wrist, ankle, and side panel do the same kind of rest sink. The poses were not frozen to hide it. The march lifts the chest so the head sits back over the body, and the stride still rocks. A character card shows the look that seat wears in the match: P3 is Orange and P4 is Lavender, and the yellow and green stay on the frame and the P badge. The title parade keeps the seat colors. The rail gap is unchanged. Space still jumps. Mockups are in `Docs/UiStills/pass27/`.

The twenty-eighth pass keeps that raw overlap rule and splits the count. rigJoint is 26, the pairs that already fail at rest. pose is a pair the live poses add, and it stays 0. Mode, every rule, and the arena are driven with the keyboard and again with a pad, and the values that land are the ones TagModeController and the loaded park read. Rematch keeps the seats, the looks, and the arena. Main Menu returns to the title and clears the seats. The mint chest on Blue and on Lavender is the accent panel of that look, the same pair the match body uses. Space still jumps. Mockups are in `Docs/UiStills/pass28/`.

The twenty-ninth pass keeps those lines. Title, the main menu, character select, mode and rules, arena, and results ease through a short slide and scale. Back runs that move in reverse. Input skips it, and the timer is unscaled so a pause does not stop it. The focused tile bumps to 1.08 and settles. Arena tiles show the real loop, pads, zips, and spawns, with the measured size and one line from that map. Ready leans into the ready pose and hops once, and pose stays 0. Move, confirm, back, ready, and start already play UiMove, UiConfirm, UiBack, RoundWin, and RoundStart. The extra line is `transitions=ok skip=ok`. Mockups are in `Docs/UiStills/pass29/`.

The thirtieth pass keeps those lines. One row is focused at a time. The selected mode keeps a filled tile and a check. Headers start at least 5% down on a 16:9 screen and inside each pane of a 4-way split. The prompt bar uses the same ActionBinds tokens as Controls, so Confirm, Back, and the extra action match the bindings. Character select shows four cards, each with the player name, the look colour, and a READY banner. The pictures in `Docs/UiStills/pass30/` are composites.

The thirty-first pass docks that bar to the bottom edge, and to the bottom of each pane in a split. The chips are small glyphs and labels, not buttons. Xbox shows A, B, X, and Y. PlayStation shows cross, circle, square, and triangle. A keyboard shows key caps. The token is the same ActionBinds token Controls uses, for the device that last gave input. Character select shows the Hier mannequin in the selected look, idle or ready. The frame and the name tag use that look, and so does the in-game name plate. The P chip is the seat colour, labelled Seat. Arrows change the look, and a taken look shows a lock. The pictures in `Docs/UiStills/pass31/` are composites.

The thirty-second pass puts the Hier mesh on the card. The preview is lit, at a three-quarter angle, idle or ready. Idle sways by 10 degrees. Ready hops with the hop already on that screen, and pose stays 0. The P chip stays the seat colour. The header says "P colour = controller seat" once. A taken look is grey, with a padlock. In `Docs/UiStills/pass32/cast-mesh.png` the figures are a render of the FBX. The rest of that picture, and `cast-legend.png` and `cast-lock.png`, are composites.

The thirty-third pass paints that mesh in the look. The body is the primary swatch and the panels are the accent, matte, with metallic at 0 and roughness about 0.6. One rig lights every card: key from the front-left, fill, and a rim. The well fades from navy to a lighter blue-grey. The figure fills about 80% of the well and stands on a soft shadow. A Red torso pixel in the still is `#C83E46`, against primary `#E0383D`. The bright cards in the previous still were the picture, not the menu. `Docs/UiStills/pass33/cast-looks.png` keeps the pass 31 card chrome. The figures in the AFTER wells are the FBX render.

The thirty-fourth pass keeps the Orange and Tan chest on the primary colour. Those meshes share the shell with a joint slot, and the joint no longer paints the torso. The accent stays the second colour. Every card uses the same three-quarter front, with the face toward the camera when the sway is centred. The ready hop still has about 5% of the well above the head. RESULTS uses the cheer and the slump already on that screen, places 1st through 4th, and each line shows tags, time as It, and the match's longest chase. That chase is one number for the whole match. The pictures in `Docs/UiStills/pass34/` label the mesh pixels. Space still jumps.

The thirty-fifth pass plants the RESULTS figures on their blocks. Each sole sits about half a centimetre above the cap, with a contact shadow, on the cheer and on the slump. The winner fills about 45% of a 1920x1080 stage. The stat cards follow the stage, 2nd then 1st then 3rd then 4th, so each card sits under its figure. The bar is the seat colour and a small swatch is the look. The longest chase is one header line. The still holds the existing cheer where the head yaw peaks, and the arms stay the small open already in that pose. Pause lists Resume, Restart round, Options, and Quit to menu, asks before leaving, and colours the panel for the player who paused. The pictures in `Docs/UiStills/pass35/` label the mesh pixels. Space still jumps.

The thirty-sixth pass uses one player mesh for every look. The menu loads the same Tan Hier the match uses for a runner when the catalog slot is empty, and recolours it. The cast cards, the look sheet, and RESULTS all show that body. The shared head is a closed shell and the crown normals face out. First place raises both arms in a V and hops, second and third pump a fist, and fourth slumps with the head down and the arms hanging. Those are menu clips. The no-clip line still reads rigJoint=26 pose=0, and the red stills are the rest hip overlap. Pause shows one menu on the pauser's pane, dims the others, and tags who paused. Only that player drives it until someone else asks, and the turn moves after two seconds. Quit asks with a Yes/No modal, and No starts selected. The pictures in `Docs/UiStills/pass36/` label the mesh pixels. Space still jumps.

The thirty-seventh pass lifts that V overhead, about 30° off vertical, with the chin tipped up. Second place swings one fist out beside the head so the face stays visible. Third place holds both fists at the chest. Fourth still slumps. The results camera is a slight front three-quarter so each face reads. The card bar, the tag, and the swatch use the look on that figure, and the tag keeps a circle, square, triangle, or diamond. Look-sheet names sit in chips wide enough for Orange and Lavender. pose stays 0 and rigJoint stays 26. The pictures in `Docs/UiStills/pass37/` label the mesh pixels. Space still jumps.

The thirty-eighth pass puts each results card under its block, at one width, with a gap, and lifts the winner card. Third place drops the elbows and holds both fists in front of the chest, with a nod and a small knee bend. Fourth turns the head a little while the slump stays. A front three-quarter key and a soft rim light the face plate and the eye sockets. Look-sheet chips sit on the projected feet. The board reveal raises the blocks, settles the poses, then slides the cards in. The two-second check is `Docs/UiStills/pass38/results-reveal.gif`, with the six frames beside it. pose stays 0 and rigJoint stays 26. Space still jumps.

The thirty-ninth pass gives the blocks one footprint, a gap, and a shared front edge. The heights stay second, first, third, fourth. The stat cards match that spacing, with equal gaps, and the row is centred under the blocks. The summary names who was It the least and who ran the longest chase. Second place bends the raised elbow so the fist shows. The check is `Docs/UiStills/pass39/results-reveal.gif`. pose stays 0 and rigJoint stays 26. Space still jumps.

The fortieth pass makes every results block the same stone. The front band is that seat's colour, P1 red, P2 blue, P3 orange, P4 purple, and the rank numeral is on the band. Those four colours are MenuTheme.SeatBand. A floor sits under the blocks, each block has a soft contact shadow, and the figure shadows the top of the block. Second place pumps a fist, upper arm about 45° up, elbow about 90°, fist above the shoulder. The stat line is the same on every card: tags, time as It, and round wins. The check is `Docs/UiStills/pass40/results-reveal.gif`. pose stays 0 and rigJoint stays 26. Space still jumps.

The forty-first pass reads the band from MenuMannequin.Swatch, so P4 is lavender with the figure. Every band is the same height, and the rank numeral is one bold size on each block. The results card drops the costume line and keeps the place, the seat, the name, and the two stat lines. The floor fades into the sky. The check is `Docs/UiStills/pass41/results-reveal.gif`. pose stays 0 and rigJoint stays 26. Space still jumps.

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
