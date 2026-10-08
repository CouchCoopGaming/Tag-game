# Front-end menu

The new door is a uGUI overlay (`Tag.Ui.Menu`). Boot stays the scene that opens the menu. Play stays the first scene in the build list, which the asset audit requires. Pressing Play on Boot or on an unarmed Play scene shows the title. The old OnGUI cards stay in the project. Set PlayerPrefs `Tag.Ui.Legacy` to 1 to use them.

Feel numbers are not on these screens. Coyote, buffer, cling, jump, gravity, speeds, dash, punch, lunge, climb, slip, wall-run, stagger, pad, zip, tag-back, and the grapple click timing are unchanged. There is no grapple on/off tile, because the game does not have that toggle. Look sensitivity and invert stay one shared pair, because the settings file does not store them per seat.

## Flow

```
Boot.unity  (MenuDoor + GameFlow)
    or Play.unity while the match is not armed
        |
        v
Title  "Press Start / South / Space"  (any connected device)
        |
        v
Main   Play | Practice | Options | Controls | Credits | Records | Quit
        |
        +-- Options ---- audio, display, accessibility, controls, look, credits
        +-- Controls --- existing binds, plus Confirm: South or East per seat. Space stays jump.
        +-- Credits
        +-- Records --- local profile matches, wins, and tags
        +-- Practice --- existing practice rows, then the loading screen
        |
        v
Join   1-4 slots. A button joins. Back on that device leaves.
       Left/right cycles the seated profile (LocalProfiles).
        |
        v
Cast   3x2 Hier colors. Each human has a colored cursor.
       LB/RB accent, North hat, confirm ready.
       Down on the bottom color row opens the name keyboard.
       Idle preview cameras. All ready, then rules.
        |
        v
Rules  Least It | Hot Potato | Trail Tag | Free play
       length, rounds, win target, starting It, handicap,
       launch pads, zip lines, AI, difficulty, split, listener
        |
        v
Arena  Mega Park | Pocket Park | Stack Yard | Random
        |
        v
Loading  arena name, the saved rules, one tip, honest bar
        |
        v
GameFlow.BeginFromMenu
        TagModeController.StartRound  (unchanged start path)
        |
        +-- Start  --> Pause (Resume, Restart, Options, Controls, Quit to menu)
        |
        v
Results  rankings from time as It, plus the mode's own result line
         Next round | Change arena | Change characters | Quit to menu
```

MainMenu.unity is the same door without the park. It is third in the build list, after Play and Boot.

## What each screen does now

| Screen | State |
|---|---|
| Title | Wordmark with a slow idle, arena photo drift, press-start pulse. Any keyboard or pad advance. |
| Main | Chunky tiles. Records sits with Credits and Quit. Practice is the existing practice mode. |
| Join | Four seats, device line, profile name, back leaves that device. |
| Characters | Grid, per-seat cursor, ready, all-ready banner, render-texture idle mannequin. Down opens a pad keyboard for that seat's name. |
| Mode and rules | The four modes that ship, plus length, rounds, win target, starting It, a handicap label, and pad and zip hazards. |
| Arena | Three parks and Random. Each card has a thumbnail, the name, the size, and a flavor line. |
| Loading | Arena name, the saved rules, one control tip, and a bar that moves only when the match actually starts. |
| Pause | Opened by whoever pressed Start. Dim so the split stays visible. |
| Results | Ranked figures on steps under a RESULTS heading. Headline is the mode's result line. |
| Options | Existing look, audio, HUD, accessibility, plus video and reduce-motion for the menu. |
| Controls | Rebind through ActionBinds. Unknown Jump keys are rejected. Space still jumps. Each seat can set Confirm to South or East. |
| Credits | Team, the SIL OFL font credit, and the tools already in the project. Open it from Main or from Options. |

## Pass 2

Tiles are rounded, outlined, and shadowed. The sky is a gradient with a scrolling chevron. Screens slide in about 0.2 s with a gold sweep. The footer is three prompt chips that swap between keyboard words and pad words. The display font is Liberation Sans Bold (SIL OFL, license in `Assets/UI/Fonts/OFL-Liberation.txt`). Seat chips follow the accessibility palette once someone has joined. Empty slots use red, blue, yellow, and green.

Character previews load `Dummy_Mannequin_*_Hier_Hi` in the editor. The editor bakes the catalog, the arena thumbs, and a mesh portrait the first time those files are missing or older than the source, and again before a player build. Until that bake has saved, a player build uses the primitive mannequin with the same bone names, and the grid draws a bust in that color. Idle breath and weight shift come from `IdlePose`. The primitive also hangs its arms with `VerbPoseClips` idle numbers. Ready stamps the panel.

Arena cards use the overview stills in `Assets/UI/ArenaThumbs` (also under Resources so the build includes them). The hovered card fills the big preview.

Results orders players by the mode that ended: Hot Potato round wins (first to 2), Least It by time as It with that mode's winner first, Trail Tag last standing, Free play by tags and no winner stamp. The 3D steps use the same mannequin. The panel heading says RESULTS. Confetti plays only when there is a winner and reduce motion is off.

## Pass 3

The Hier catalog, arena thumbs, and character portraits bake themselves when the editor opens if the files are missing or older than the FBX or the overview still. A player build runs the same bake first. The primitive mannequin and the drawn bust stay as the fallback.

Main-menu tiles carry a drawn icon: a runner, a cone, a gear, a pad, a star, and a door, each on a colored well. The character grid shows a portrait and a large name that shrinks to stay inside the tile, so LAVENDER does not touch the edge. The TAG wordmark is slanted, outlined, and backed by a chase streak. Join slots show a keyboard, a pad, or both. The selected tile bobs, and a confirm squashes in before the screen changes.

Move, confirm, and back stay on the UI bus. Ready uses the round-win clip. Starting a match uses the round-start clip. The menu asks for the existing playground music bed. No new audio files.

Tag → Menu → Capture Screens, or `-executeMethod Tag.Ui.Menu.MenuScreenCapture.Capture -screenshot Docs/UiStills/captures`, walks every screen into `Docs/UiStills/captures/` the next time Unity runs. The same walk then writes `14-hud-1`, `15-hud-2`, and `16-hud-4`. `Docs/UiStills/pass3/` are mockups until then.

## Pass 4

The Credits well keeps its gold plate, and the star on it is cream with a dark edge so it no longer disappears. The footer chips draw a real arrow cluster, a space bar, and an Esc key, or a stick, a green A, and a red B, depending on the last device.

Character select always lays out four seats. An empty seat is a card in that seat's color that says Press A / Space to join. A button on a free pad, or Space on a free keyboard, sits that player down without leaving the screen. Seated players still get the idle preview and the READY stamp.

The in-match HUD is uGUI in `Assets/Scripts/UI/Hud`. It replaces the OnGUI verb cluster, the couch score list, the It banner, the countdown card, the bottom mode box, the debug speed sheet, and the old off-screen It mark while `Tag.Ui.Legacy` is off. Those OnGUI paths stay in the project and draw again if the legacy flag is on.

Each split has a colored frame, the seat name and profile, and a gold IT badge with a glow on whoever is It. The match clock and the round counter sit top center. Least It shows time as It, Hot Potato shows round wins, Trail Tag shows in or out, and Free play shows tags. The fourth pane of a three-player match still lists the couch score lines. Dash is a radial that fills across the existing cooldown, grapple reads aim / hook / pull, and tag-back counts down the existing immunity window. An arrow at the edge of a pane points at the It, or at the nearest living runner when that pane is It. YOU'RE IT!, TAGGED!, the 3-2-1-GO countdown, and ROUND END are large callouts. Reduce motion turns off the glow pulse and the scale punch. The HUD update does not build strings, lists, or components. `Docs/UiStills/pass4/` are mockups of the fixes and of a 1-, 2-, and 4-player HUD.

## Pass 5

Pause is one card per split. Each card says Resume, Restart round, Options, and Quit to menu. Any seated player can open it, and the keyboard and every pad move the same highlight. The score pane in a three-player match is not a pause card.

Results still sort by the mode. Every row shows tags, time as It, and round wins, then the rows fade in. Reduce motion shows the board at once. The actions are Rematch, Change mode, Character select, and Main menu.

Options opens onto Audio, Display, Accessibility, Controls, and Look. Audio steps the existing volumes and draws a slider. Display is resolution, fullscreen, and vsync. Accessibility is reduce motion, text size, and the colorblind-safe player colors, with a swatch for each seat. Controls lists the binds and can still change one. Space stays jump. Stick inner deadzone, outer deadzone, response curve, and gamepad look accel read StickInner, StickOuter, StickCurve, and LookAccel. If a build does not have those fields yet, the rows say Coming soon and do not write.

Screens slide for a fifth of a second and ignore input until the slide finishes. Back returns to the screen you came from, and an empty stack lands on the main menu or the title. A pad that drops shows Controller disconnected and that seat's reconnect line. Resume waits until the pad is back.

`Docs/UiStills/pass5/` are mockups. Capture Screens also writes `17-pause-4`, `18-audio`, `19-display`, `20-access`, and `21-disconnect`.

## Pass 6

The title drifts the arena photo, bobs the TAG logo, and pulses Press Start. Reduce motion holds all three still. Tiles still scale on hover and squash on confirm, and a pale sweep crosses the highlighted tile. Move, confirm, back, join, and error each use a clip that was already on the bus. Join and confirm buzz the pad that pressed, and only when that seat's rumble is above 0 and reduce motion is off. A keyboard never buzzes.

When every seated player is ready, the character screen counts Starting in 3, 2, 1. Anyone who unreadies or backs out cancels it. Mode select keeps a How to play card for the highlighted mode: one existing rule line and a small diagram.

Arena cards show the overview, the measured size (160 x 100 m, 80 x 50 m, 110 x 70 m), a flavor line, and a top-down of that park's spawns and pads. Seat colors are the same red, blue, yellow, and green (or the accessibility palette once someone has joined) on the join card, the character card, the results row, and the figure.

The results panel is headed RESULTS. First place stands in the center on the tall step, second on the left, third on the right, and fourth on the floor beside them. The winner uses the existing claim pose. The others use the give-up pose or the stumble. Confetti stays on the winner and stays off when reduce motion is on. The heading on that panel is RESULTS. Capture Screens still writes `09-results`. Mockups are in `Docs/UiStills/pass6/`.

## Pass 7

The title has no box. The slanted TAG logo sits in the center, large. Behind it, Mega Park fills the screen and drifts. Four Hier figures in the seat colors run and vault across that photo. A dark vignette sits on the edges. Press Start is one glyph under the logo: a space bar after the keyboard, or South after a pad.

The headless check walks the menu as a controller. Keyboard-only and pad-only each reach title, main, join for one to four seats, character, mode, arena, loading, match, pause and each of its options, results and each of its actions, options and each page, and back. Back always lands, and focus stays on a row. The line starts with `ui-flow`.

The settings file remembers the mode, the arena, the rules, and each seat's character and color. Rematch keeps that set. A pad can sit down on the join, character, mode, or arena screen. A seat whose pad dropped can be taken again, and that character stays. Starting with fewer people than the mode needs shows a sentence: free play needs one, and the tag modes need two.

Menu music and the move, confirm, back, join, error, ready, start, and results stingers are hooks on the menu bus. Each slot points at a clip that was already in the project. The twenty gameplay hooks are unchanged. Mockups are in `Docs/UiStills/pass7/`.

## Pass 8

The menu and the HUD share one UI scale, 80% to 130%, on the Display page. It is saved as `uiScale`. Every screen keeps a 5% margin. Body and fine type sit at 30 px, so at 80% they are still 24 px on a 1080p TV. The headless walk checks that floor.

The last pad you touched picks a glyph set: Xbox (A / B), PlayStation (Cross / Circle), Switch Pro (B confirm / A back), or a generic South / East. The keyboard keeps arrows, Space, and Esc. Those pictures show in the footer, the title prompt, the join cards, and the binds list.

Hold Select, or Tab on the keyboard, and that player's split shows STANDINGS: the round, the clock, and each seat's mode value. The other splits stay clear.

Each split has a three-line tag feed in the lower corner. A line reads P1 tagged P3, in the tagger's color, then fades. The sentences are built once.

Round 2 opens with ROUND 2. The last round opens with FINAL ROUND. A tie, or Trail Tag's sudden death, shows SUDDEN DEATH. Hot Potato shows MATCH POINT when someone is one win from taking the match.

The loading card names the arena, shows one real tip (a verb or a rule), and fills a bar only when the match is actually asked to start, then when the round is active. It does not pretend to creep.

The IT badge stays in the top-right corner of a right-hand split. The name sits beside it and does not cross the clock, at two players and four, and at 80%, 100%, and 130%. Mockups are in `Docs/UiStills/pass8/`.

## Pass 9

A Switch Pro pad confirms with A, the east button, and goes back with B, the south button. The glyphs match. Xbox and PlayStation still confirm on the south face and go back on the east face. Controls has a row per seat, Confirm: South or East, and Auto keeps the pad's own rule. The keyboard ignores that row. Jump and punch stay on the physical south button, so Space still jumps.

Rules adds round length, round count, a win target, who starts as It (random, last place, or a chosen seat), a handicap word per seat, and launch pads and zip lines on or off. Those values are saved and listed on the loading screen. The handicap is a label. It does not change speed, punch, or any other feel number. Pads and zips stay on in practice. Turning them off only skips the lobby copies.

Character select opens an on-screen keyboard when you press Down on the bottom color row. The stick moves, confirm types, and OK stores the name on that seat's local profile. Records, on the main menu, lists each profile's matches, wins, and tags from the match stats that were already saved.

Credits names the team, Liberation Sans Bold and the SIL Open Font License, and the tools already in the project. Main and Options both open it.

Text that sits on a highlighted tile is ink, so it stays readable. Seat colors are mixed down before they sit behind words. Every control keeps a gold ring. At 130% the lists reflow inside the safe area, including the rules window, the name keys, and the records card. Mockups are in `Docs/UiStills/pass9/`.

## Pass 10

Tiles are beveled. A soft shadow sits under each one, and a gloss band sits across the top. The sky is a three-stop gradient. Four soft orbs, in the player colors, drift behind the page. Title, the main menu, arena select, and loading also lay a slow Mega Park photo under that.

Headings use Bangers. Body text stays Liberation Sans Bold. Both are SIL OFL. The license files sit next to the fonts. Credits names them.

On character select each seat stands on a disc that turns. The figure idles. Ready lifts the arms and hops once. The READY stamp and the 3-2-1 count punch in. GO punches in the match. Move, confirm, back, ready, and start still use the menu bus. Confirm and ready also buzz the pad when that seat's rumble is on.

The gold ring, the ink-on-highlight contrast, and the 80% to 130% scale stay. Mockups are in `Docs/UiStills/pass10/`.

## Pass 11

Character select, the title parade, and results use the Hier mannequin. The poses are idle, a ready hop, a claim, a give-up, and a stumble. Bone names stay the generic set. The title line is PRESS, the confirm glyph, and A on a pad, or PRESS START with the space glyph on a keyboard. It breathes. The TAG lockup is Bangers with a thick outline, an inner highlight, a halftone shadow, and a small tilt. READY! is the same comic burst, larger, and it pops once.

The title and the main menu sit on a golden-hour render of Mega Park, softened, with a vignette. The four orbs stay, quieter. The main menu's left side is two mannequins mid-chase and a tip of the day. Character cards show a swatch row, the pad that joined, and the ready burst.

The gold ring, the contrast pairs, the 80% to 130% scale, and the proof lines stay. Space still jumps. Mockups are in `Docs/UiStills/pass11/`.

## Pass 12

The title prompt shows the confirm glyph once. A pad reads PRESS and the face button. A keyboard reads PRESS START. The line stays centered under the lockup and keeps the pulse.

Character cards use two lines. The name and the color pair sit on the first when they fit. Hat and ready sit on the second. A 12-letter name wraps inside the card. The check walks every card at 80%, 100%, and 130%. The figure is framed full body, feet on the turning disc, with a contact shadow. The six colors are a short legend under the cards, so the cards keep the height.

Results stands the four figures on a stepped block with a number, a trim lip, and a confetti burst. First celebrates, second and third clap, fourth slumps. The park photo sits behind them. The inner RESULTS line is gone. The header is the only one. "2 round wins" has its own line on the first-place card. Mockups are in `Docs/UiStills/pass12/`.

## Pass 13

Button labels sit in the well under the highlight stripe, with 6 px of clearance, and a sublabel on its own line. That layout is the same on the main menu, arena, character colors, records, and the results actions.

The READY burst sits in the lower corner of the portrait so the face stays clear. Each card frame stays the seat color, with a P1 to P4 tag, and the chosen color stays on the figure and the gold-ringed swatch. Results uses that same seat bar and tag.

The results group is closer, each figure has a contact shadow on its own block, and the time reads "8.5s as It". The title crew stands on discs with contact shadows, under the lockup and above the prompt. Mockups are in `Docs/UiStills/pass13/`.

## Pass 14

The arena preview sits inside the navy frame with even padding. Mega Park, Pocket Park, and Stack Yard each have a daylight hero: a three-quarter view with the real colors, and a hint of the pads, the zip, and the loop. The main menu chase stands on discs with contact shadows. The header on that screen is Menu, so TAG is only the lockup. Button titles use the same well under the stripe as the color swatches.

Character cards keep the hat and ready line inside its box, with space above the bottom edge, and the card ends with that row. On results the 3 and 4 plates sit low and in front of the discs so the numbers read. Mode and rules, loading, pause, options, controls, and the drop-in join (two seats in, two waiting) are in the same set. Mockups are in `Docs/UiStills/pass14/`.

## Pass 15

Every button title stays on the comic display face, selected or not, and every sublabel stays on the body face. Sublabels use the full button width before they wrap. The controls list prints the keys the reader actually samples, including grapple on RMB (click pulls, a second click releases, no pad bind), zip as the cling hold, and a launch pad you walk onto. Cling stays a hold into the wall, and wall jump is that hold plus Jump. The main-menu discs sit inside the frame, above the tip. Loading reads Starting It, then Random, and that screen has no confirm or back hint. Pause says Paused by P1 in the header, with the buttons centered in the card. Mega Park is a lower three-quarter view with a pad and a zip in front and a light vignette. Mockups are in `Docs/UiStills/pass15/`.

## Pass 16

The tip of the day lists three lines that match the binds: Space jumps, hold into a wall to climb, and a double-click on RMB lets go of the grapple. Practice reads "Free run any arena, no tagger". Controls keeps a window on the list and a gold scrollbar. The first window runs through Pause. The last window is the stick rows, the confirm faces, reset, and back. Cling is still the hold into the wall, not its own key.

Arena previews use the daylight overview of the real Mega Park, Pocket Park, and Stack Yard: the loop, the structures, the pads, and a zip. Loading hides the win target when the match is one round, and it says Handicaps none unless a seat is set. The card sits on a blurred Mega Park photo. The match HUD uses the comic face for the timer, the IT badge, and the verb words, with a tag count on each seat and a Comic words hint. Mockups are in `Docs/UiStills/pass16/`.

## Pass 17

Each seat in a match has its own chase view of Mega Park, with that seat's runner in the frame. Two players split left and right. Four players get a view in each quarter. The ability rings sit in a small cluster at the bottom-left of the view, and a short label shows only while that ring is not ready. The clock is a slim plate on the center seam, with the time and the round. The options banner and the pause banner read COMIC WORDS ON. A tag moves the IT badge, flashes the tagger's color on that view's edges, and ticks the tag count. Mockups are in `Docs/UiStills/pass17/`.

## Pass 18

The chase runner is the Hier mannequin in that seat's tint, mid-run, with the foam, the panels, and the dark joints. The headless capture cannot open the Unity prefab, so it reads a posed bake of the Hier mesh. The same afternoon sun is lifted on the chase so the Mega Park ground reads warm, the way the arena overview does. A tag still puts YOU'RE IT on the tagged seat in the comic face. Screen changes are a three-frame comic wipe: menu to characters, loading to the match, and the match to RESULTS, each under 0.4 s. Mockups are in `Docs/UiStills/pass18/`.

## Pass 19

The chase drops the extra grade. Foliage stays green, concrete stays grey, the paths stay tan, and the sky stays blue, with only the afternoon sun. Each runner takes that sun on the foam and the panels, and a contact shadow sits under the feet. RESULTS keeps the raised hands inside the frame. The place words read 1st, 2nd, 3rd, and 4th, and the stat lines stay inside the cards. YOU'RE IT, the tagger flash, the seat tints, and the three wipes stay. Mockups are in `Docs/UiStills/pass19/`.

## Pass 20

The chase path is a light warm tan, about #C8A878, and the concrete that shows is a lighter grey. The swatches the contrast proof uses stay dark. That lift is only on the upward path in the chase stills. RESULTS keeps the place plates on the steps. The extra pills under the figures are gone, because the cards already say 1st, 2nd, 3rd, and 4th. The 4th figure stands on a step. Character select shows the four Hier runners in the seat colors, with the same sun and a contact shadow, each in its own idle. READY stays on the seats that locked in. The arena line sits on the same row as the other screens, and the RESULTS actions line up with the cards. Mockups are in `Docs/UiStills/pass20/`.

## Pass 21

Character select draws each runner from the color pair on its card. The chest is the first color, the body is the second, and the gold ring sits on that same first swatch. The card is a dark navy panel with a seat-colored border, so the runner does not sink into the card. The runners fill about half the card, and the joints are charcoal so the tint reads. The thin aslate lip beside the P2 path is a grey concrete curb. The tan path and the RESULTS cards stay. Mockups are in `Docs/UiStills/pass21/`.

## Pass 22

The match runner, the RESULTS figures, and the stat-card accent use the same color pair as character select. The seat color stays on the card border, the HUD edge, and the P# tag. P4 is Lavender / Mint, because Tan / Red is the same pair as Red / Tan. A chip whose pair is already taken is gray. READY sits under the swatches, not on the legs. Pause keeps COMIC WORDS ON inside the panel. Options puts that line on a navy plate. Mockups are in `Docs/UiStills/pass22/`.

## Pass 23

The first color is the body: limbs, torso, and head. The second color is the accent on the chest panel, the hands, and the feet. P1 reads red, P2 blue, P3 orange, and P4 lavender, so the four chase runners stay apart. A chip is gray when that first color is already taken. The title and the main menu are two frames of a pass along the south straight, with those four runners on the path. The lockup sits on that frame, and the main buttons keep an even gap. Mockups are in `Docs/UiStills/pass23/`.

## Pass 24

The title camera is raised south of the straight, so the fence and the crates sit under the frame. The four runners are mid-run on the path, each large enough to read, and the lockup sits above them. The main menu uses that same line, framed in the open middle, clear of the lockup and the buttons. P2's card uses the same opaque lit body as the other seats. The wide sky key had been treating that blue as sky and punching the torso out. The card border and the HUD pane edge are a quiet stroke. The seat color stays on the small P badge, so an orange runner is not boxed in yellow and a lavender runner is not boxed in green. From this pass on, a posed runner on the title, the main menu, the character cards, results, or loading stays out of solid scenery and out of its own other parts. The check is every frame at 30 fps. Joined neighbours may meet within 3 cm of the joint, and a sink deeper than 0.5 cm fails. The menu keeps a small share of the run, the vault, the idle, and the results beats so those shells stay clear, and the confetti falls in front of the runners. The card ghost on P2 was the sky key. The pass 24 mockups stay the baked frames. Mockups are in `Docs/UiStills/pass24/`.

## Pass 26

The 1.2% scale on the menu poses is gone. It cleared the overlap check by freezing the runners, and the cause is the mannequin: the hip shell already sits about 4 cm into the thigh at rest. That rest overlap is the baseline. A pose may not deepen it, or any other overlap, by more than 0.5 cm. Joined neighbours are exempt only within 3 cm of the joint. The live poses are authored on the current rig. Idle breathes and shifts its weight. Character select settles into a ready stance. On RESULTS the winner leans back, the next two places open a little less, and last place bows. A raised arm, a clapped elbow, a tucked vault knee, and a full stride sink the chest or the hip into the limb, so those beats wait for the trimmed-rig candidate. The title camera stays raised south of the straight, with the fence and the crates under the frame. The title runners and the character-card runners wear the seat colors on the P badges: P1 red, P2 blue, P3 yellow, P4 green. That is the paint the title parade already uses. A picked skin on the live character card is still the Hier look, and the match chase stays the look pairs, so P3's orange and P4's lavender are those looks, not the seat badges. P2 stays a solid blue body. The character cards wear a red, blue, yellow, and green slot frame. The far-right title runner's left shin clears the bar by 56.93 cm. The south post that stacks over that foot in the perspective still, BarPost_S6, clears by 313.58 cm. The ortho top and side views are in the pass 26 stills, and the no-clip line reports the gap as rail. The check is still every frame at 30 fps. The deepest extra overlap on the authored poses is under half a centimetre: idle 0.43, ready 0.41, run 0.47, step 0.42, cheer 0.43, slump 0.46. Mockups are in `Docs/UiStills/pass26/`.

## Pass 27

The no-clip check no longer subtracts the rest-pose depth before the 0.5 cm limit. Every frame at 30 fps, a sink deeper than 0.5 cm fails, and joined neighbours are exempt only within 3 cm of the joint. That rule fails on all 1778 frames. The deepest sample on idle, ready, run, step, cheer, and slump is the same rest sink: UpperLegMesh_R inside PelvisMesh, 9.50 cm, 7.06 cm from the joint. PelvisMesh sits on DummyRoot. There is no separate Mesh_Hips. It is not the only rest overlap. The rest pose also sinks at the neck, the knee, the elbow, the wrist, the ankle, and the side panel into the upper arm, each farther than 3 cm from its joint and deeper than 0.5 cm. The live poses do not add a pair the rest pose does not already have. The trimmed-rig candidate owns that fix. The poses stay alive. The march opens the chest and sets the head back over it, and the stride still rocks. The title parade still wears the seat colors. A character card paints the selected look on the mannequin and names that look. The seat color stays on the card frame and the P badge. P3's card is Orange, and P4's card is Lavender, which is the body those seats wear in a match. The rail gap is still 56.93 cm. Mockups are in `Docs/UiStills/pass27/`.

## Pass 28

The no-clip rule is the same one as pass 27. Every frame at 30 fps, a sink deeper than 0.5 cm fails. Joined neighbours are exempt only within 3 cm of the joint. The rest-pose depth is not subtracted. The line adds `rigJoint` and `pose`. rigJoint is 26, the directed piece pairs that already fail at rest. pose counts a pair that fails on a live pose and did not fail at rest. A different shell of the same piece is not a new pair. pose is 0, so the live poses add no pairs. The rest overlaps stay, including the 9.50 cm hip sink, and the trimmed rig still owns that fix. The rail gap is still 56.93 cm.

Mode and rules, and arena select, are played with the keyboard and again with a pad. Up and down walk every row. Left and right change every rule, both directions, and move across the four modes. Confirm and Start both commit. Confirm on Arena select opens the arena list. Back leaves. A short lobby cannot start a tag mode. Each park and Random are confirmed. The mode written is the value TagModeController reads from `Tag.SelectedMode`. The round length and round count are what ApplyRound receives, and the other rule fields survive the settings blob the scene loads. The arena is the id ParkArena.ApplySaved reads. The line is `handoff mode=ok rules=ok arena=ok`.

Rematch keeps the four seats, their looks, and the arena. Main Menu opens the title and clears the seats. The line is `results rematch=ok title=ok`.

The contrasting chest on Blue and on Lavender is the costume accent, not a swapped body slot. Panel_Chest, the hands, and the feet take the second color of the pair. Blue is Blue / Mint. Lavender is Lavender / Mint. The torso foam under that panel stays the body color. The match body uses that same pair.

Mockups are in `Docs/UiStills/pass28/`.

## Pass 29

Title, main menu, character select, mode and rules, arena, and results slide and scale over 0.2 s. The move is eased. Back plays it from the other side. A press during the move skips it and still counts. The clock is unscaled time, so a pause does not freeze it. The focused tile bumps to 1.08 and settles. Pause, options, controls, credits, and records keep the slide they already had. The line is `transitions=ok skip=ok`.

Each arena tile draws that park from its layout. The loop, the pads, the zips, and the spawns are the arrays on the map. Stack Yard marks the deck heights already in the yard: decks at 6 m and roofs at 12 m. Mega Park's line is 5 pads and 5 zips. Pocket Park's line is 2 pads and 2 zips. The size line is still the fence, in metres.

Character select, on ready, leans into the ready pose and hops once. The hop is the menu hop already used on that screen. It adds no pose pair. pose stays 0. rigJoint stays 26.

Menu move, confirm, back, ready, and start already call the bus. Move is TagSfx.UiMove, confirm is TagSfx.UiConfirm, back is TagSfx.UiBack, ready is TagSfx.RoundWin, and start is TagSfx.RoundStart. No new bus name.

The handoff line, the results line, and the no-clip line are unchanged. Mockups are in `Docs/UiStills/pass29/`.

## Pass 30

One item has the yellow border. On mode and rules the selected mode stays a filled tile with a check, and the focused row is the only yellow one. Arena select uses the same rule. Headers sit at least 5% down from the top of a 16:9 frame, and the same fraction down inside each pane of a 4-way split. The bottom bar reads Confirm, Back, and the extra action for that screen through ActionBinds.Show, the same tokens the Controls screen prints. Character select keeps four cards. Each card shows the seat, the player name, the look colour, and a READY banner when that player is ready. The pictures in `Docs/UiStills/pass30/` are composites. The proof lines are unchanged.

## Pass 31

The prompt bar is a thin strip on the bottom edge of the screen. In a split, each pane has its own strip on that pane's bottom edge. The chips are small glyphs plus a label. They are not buttons and they are not in the focus list. The glyph is the ActionBinds token for the device that last gave input on that seat. An Xbox pad draws A, B, X, and Y. A PlayStation pad draws cross, circle, square, and triangle. A keyboard draws key caps. Controls uses the same token for its icon.

Character select keeps four cards. Each card shows the Hier mannequin in that player's look, in the idle or ready pose. The card frame and the name tag use the look colour. The in-game name plate uses that same look. The P chip stays the seat colour and is labelled Seat, the same seat colour as the match pane edge. Left and right arrows on the card change the look. A look another player already took is grey and shows a lock. The pictures in `Docs/UiStills/pass31/` are composites. The proof lines are unchanged.

## Pass 32

The card figure is the Hier mesh. Each seat has a lit preview, three-quarter view, idle or ready. Idle yaws back and forth by 10 degrees. Ready uses the ready pose and the hop already on that screen. pose stays 0. The P chip stays the seat colour. The words "P colour = controller seat" sit once in the header. A taken look is grey and carries a padlock. In `Docs/UiStills/pass32/cast-mesh.png` the figure pixels are a render of `Dummy_Mannequin_*_Hier_Hi.fbx`. The frames, type, swatches, and prompt bar in that picture are composite, and so are `cast-legend.png` and `cast-lock.png`. The proof lines are unchanged.

## Pass 33

The preview paints the look. Primary is the body swatch and secondary is the accent swatch, the same keys as the runner foam. Both are matte: metallic 0, roughness about 0.6. One light rig serves every seat: a soft key from the front-left, a fill, and a rim. The well is a gradient from navy to a lighter blue-grey, and the figure fills about 80% of that well, with a soft shadow under the feet. A Red torso shell pixel in the still reads `#C83E46` against primary `#E0383D`. The pass 32 card picture used a bright backdrop and colour-filled cards. That was the still, not a menu change. `Docs/UiStills/pass33/cast-looks.png` puts the mesh back on the pass 31 card chrome. The figure, the well gradient, and the contact shadow in the AFTER wells are the mesh render. The frames, type, and swatches are composite. The BEFORE panel is the pass 31 composite. The proof lines are unchanged.

## Pass 34

Orange and Tan keep a Base slot on the chest and the hips. The chest mesh also carries Joint and Wear. Each slot is painted on its own, so the shell stays the primary colour and the accent panel stays the second colour. The newer looks face the other way from Orange and Tan, so those four turn 180° on a parent and the sway stays centred on that face. The card camera sits further back: a ready hop of 0.28 m still leaves about 5% of the well above the head. RESULTS shows the same mesh in the existing cheer and slump poses, places 1st through 4th, and a line of tags, time as It, and the match's longest chase. That chase is one number for the match. In `Docs/UiStills/pass34/` the figures are FBX renders. The frames, type, and stat lines are composite. The proof lines are unchanged.

## Later passes

- A 2D focus grid on rules so Left from a rule row lands on a mode tile.
- Per-player look, only if the settings blob grows a seat field. Do not invent it in the menu.
- Replace `Docs/UiStills/pass3/` with the captures from a real Unity play session.
- Online, when it exists. The main menu uses that row for Records until then.
- Profile delete on the join screen. Rename is the character-select keyboard.
- A live camera flyover of the park, once a menu scene can spin a hidden arena without loading Play.
