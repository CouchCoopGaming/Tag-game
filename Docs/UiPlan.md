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

## Pass 35

RESULTS plants every figure on its block. The sole sits half a centimetre above the cap, with a contact shadow, and that plant runs again on each cheer and slump frame. The results camera moves in so the winner is about 45% of a 1920x1080 stage, the same size as the screen, so the view is not a small picture blown up. The stage stays 2nd, 1st, 3rd, 4th. The stat cards use that same order and sit under the figures. The bar across the top of a card is the seat colour. A small swatch is the look, so a yellow bar can sit beside an orange shell. The longest chase is one header line, Longest chase 14.2s, plus the name when the match recorded who ran it. The player lines are tags, time as It, and round wins. The still uses the existing cheer at the frame where the head yaw peaks. That pose opens the arms a few degrees. It does not add a new raise. Pause shows Resume, Restart round, Options, and Quit to menu, and it asks "Leave the match?" before it quits. The panel of the player who paused takes that seat's colour. In `Docs/UiStills/pass35/` the results figures are the mesh render. The cards, the pause panels, and the labels are composite. The arena quarters in the pause picture are earlier chase stills. The proof lines are unchanged.

## Pass 36

Every look uses the runner mesh the match ships. That is Tan Hier when the catalog slot is empty, painted with the look's primary and accent. Orange was already that generation. Red, Blue, Lavender, and Mint no longer fall back to the older mannequin, on the cast cards, on the look sheet, or on RESULTS. The shared head is closed: no boundary edges, and the crown normals face out. The open cap was on the older Red head, which these screens no longer show.

The results clips are menu poses, not gameplay verbs. First place raises both arms in a V and hops. Second and third pump one fist. Fourth drops the head, brings the shoulders forward, and lets the arms hang. The no-clip rule is the same: 30 fps, 0.5 cm, a 3 cm joint exemption, and no subtraction of the rest depth. The rest hip overlap is still the only fail, so the line stays rigJoint=26 pose=0. The red stills mark that rest overlap.

Pause keeps a single menu, owned by the player who paused, centred on that pane. The other panes dim, and a small tag names the owner. Only that player's input drives the menu. Another player can press Start and take it after two seconds. Choosing Quit opens a Yes/No modal with No selected. The leave question is not on the banner before that.

In `Docs/UiStills/pass36/` the figures are renders of `Dummy_Mannequin_Tan_Hier_Hi.fbx`. The cards, the pause chrome, and the labels are composite. The arena quarters in the pause pictures are earlier chase stills. Each still is under 400 KB. The proof lines are unchanged.

## Pass 37

The winner's arms are an overhead V, about 30° off vertical on each side, hands above the head, palms turned out a little, chin tipped up. Second place pumps one fist out beside the head, elbow wide, so the face stays in view. Third place holds both fists at the chest, elbows out. Fourth still drops the head and lets the arms hang. The results camera sits a little to one side, a front three-quarter, so each face reads and all four stay in frame. The still is the planted frame: each sole is half a centimetre above the cap. The hop still lifts the winner between plants.

The bar across a results card, the P tag, and the swatch are the look that figure wears. A colour-blind shape stays on the tag: circle, square, triangle, diamond. Look-sheet chips are as wide as the name and sit centred under the figure. The no-clip rule is unchanged. pose stays 0 and rigJoint stays 26. The rig fix is still a separate change and does not bind here.

In `Docs/UiStills/pass37/` the figures are renders of `Dummy_Mannequin_Tan_Hier_Hi.fbx`. The cards and the labels are composite. The BEFORE picture is the pass 36 results still. Each still is under 400 KB. The proof lines are unchanged.

## Pass 38

The results cards are one width. Each card is centred on its block in the results picture, with a gap between neighbours, and the winner card sits higher with the gold stroke. Third place keeps the elbows down, about 30° off the body, both fists in front of the chest, a small knee bend, and a nod. Fourth still slumps, with the head turned a little aside. The shared key light comes from the front three-quarter, with a soft fill and a rim, so the face plate and the eye sockets read. Look-sheet chips use each figure's projected foot, so the name sits under the feet. The reveal raises the blocks, eases the poses in, then slides the cards up. Reduce motion and capture still snap to the finished board. `Docs/UiStills/pass38/results-reveal.gif` is that motion, about two seconds, and `results-reveal.png` is the six frames. pose stays 0 and rigJoint stays 26. The rig fix is still a separate change and does not bind here.

In `Docs/UiStills/pass38/` the figures are renders of `Dummy_Mannequin_Tan_Hier_Hi.fbx`. The cards, the chips, and the labels are composite. The BEFORE picture is the pass 37 results still. Each still is under 400 KB. The proof lines are unchanged.

## Pass 39

The four blocks share one footprint. A gap stays between them, the front edges sit on one line, and the heights stay second below first, then third, then fourth. The stat cards are one width, the gaps between them are equal, and the row is centred on the picture. Each card sits under its block. The summary line names the player: P1 was It the least, and the longest chase names P2. Second place bends the raised elbow a little so the fist clears the forearm. pose stays 0 and rigJoint stays 26.

In `Docs/UiStills/pass39/` the figures are renders of `Dummy_Mannequin_Tan_Hier_Hi.fbx`. The cards, the summary, and the labels are composite. The BEFORE picture is the pass 38 results still. Each still is under 400 KB. The proof lines are unchanged.

## Pass 40

Every results block is the same stone. A band on the front uses the seat palette, P1 red, P2 blue, P3 orange, P4 purple, from MenuTheme.SeatBand, and a large rank numeral sits on that band. A floor plane sits under the blocks, with a soft contact shadow under each one, and the key light drops the figure's shadow onto the cap. Second place pumps a fist: the upper arm is about 45° up, the elbow is about 90°, and the fist is above the shoulder. Every stat card reads the same way, tags, time as It, and round wins, with no WIN label. pose stays 0 and rigJoint stays 26.

In `Docs/UiStills/pass40/` the figures are renders of `Dummy_Mannequin_Tan_Hier_Hi.fbx`. The cards, the summary, and the labels are composite. The BEFORE picture is the pass 39 results still. Each still is under 400 KB. The proof lines are unchanged.

## Pass 41

The seat band reads MenuMannequin.Swatch. P1 is red, P2 blue, P3 orange, P4 lavender, the same swatch as the figure. Every block wears one band height, and one bold numeral at that size. The stat card keeps the place, the seat, the name, and the two stat lines. The floor fades into the sky, so the ground has no slab edge. pose stays 0 and rigJoint stays 26.

In `Docs/UiStills/pass41/` the figures are renders of `Dummy_Mannequin_Tan_Hier_Hi.fbx`. The cards, the summary, and the labels are composite. The BEFORE picture is the pass 40 results still. Each still is under 400 KB. The proof lines are unchanged.

## Pass 42

The place numeral fills most of the band and uses the comic title face. The band is the costume swatch at full strength, from MenuMannequin.LightStep. The numeral is MenuMannequin.DarkStep of that same swatch, so lavender and blue stay readable. The results cards keep the seat shapes: circle, square, triangle, diamond.

Arena select is a cup list. Each park is a golden-hour plate, Mega Park from the existing yard plate, Pocket Park and Stack Yard graded from their overview captures. A small minimap sits on the plate, with the loop, landmark marks, pad dots, and zip lines. The right panel pans that plate, and the name, the size, and the pad and zip counts stay inside it. The header is the comic title on the dark band. Random is a question-mark tile that shuffles the three plates. Each joined seat leaves a shape and colour chip on the row it is hovering, with the vote count on a dark chip. Pad and keyboard focus, and back, are unchanged.

`Docs/UiStills/pass42/arena.png`, `arena-hover-4seats.png`, and `results-fix.png` are the checks. Each is under 400 KB. pose stays 0 and rigJoint stays 26. The proof lines are unchanged.

## Pass 43

Seat shapes come from MenuMannequin.Shape. P1 is a circle, P2 a triangle, P3 a square, P4 a diamond. The arena chips, the results tags, and the name plate read that one table. The overview camera sits above the yard, and anything within 15 m of that camera is left out, so poles and trees no longer cut a bar across the plate. Pocket Park and Stack Yard are a clearer golden hour, with the greens and the greys still readable. Two seats on the same row stack their shapes and the vote reads 2. The results header is the comic title on the dark band, and the subtitle stays.

`Docs/UiStills/pass43/arena.png`, `arena-2seats-one-row.png`, and `results.png` are the checks. Each is under 400 KB. pose stays 0 and rigJoint stays 26. The proof lines are unchanged.

## Pass 44

Mega Park uses the same overhead plate as Pocket Park and Stack Yard. The camera sits above the yard, anything within 15 m is left out, and the rim of the yard stays in frame. The row cards are a brighter centre of that plate. Mega reads brick, Pocket reads clay, and Stack reads olive, so the three rows separate at card size. Random is a three-way split of those plates with a question mark. The results title is the comic face, about one and a half times the previous size, on the same dark band, and the subtitle stays.

`Docs/UiStills/pass44/arena-mega.png`, `arena-random.png`, and `results.png` are the checks. Each is under 400 KB. pose stays 0 and rigJoint stays 26. The proof lines are unchanged.

## Pass 45

Random's detail plate is the three parks side by side. The title is RANDOM and the line under it is "One of the three parks". That plate has no size and no pad or zip counts. The row cards drop the colour wash, so each park keeps the ground from its overhead. Options follows the screens lane: Sound, Picture, Accessibility, Controls, Look, and Credits. Reset asks once, then clears that page. Controls shows the current binds. Cling reads Hold into wall on the keyboard and Left stick hold on the pad. The seat marks are MenuMannequin.Shape: circle, triangle, square, diamond. Jump stays Space. The pause card is unchanged.

`Docs/UiStills/pass45/arena-random.png` and `options.png` are the checks. Each is under 400 KB. pose stays 0 and rigJoint stays 26. The proof lines are unchanged.

## Pass 46

Arena row cards show the size again. Mega Park is 160 x 100 m, Pocket Park is 80 x 50 m, and Stack Yard is 110 x 70 m. Random has no size. Each card that has a vote shows the seat shape and the count. Left Alt is sampled for both Air dash and Sprint in `PlayerInputReader`, so both rows carry an Alt marker. The binds table still stores Air dash as Q and Sprint as Shift, which is why a defaults conflict count stays 0: that check only compares the stored tokens. Grapple is in the verb list. It is RMB in the left hand: a click pulls, and a second click within 0.28 s releases. Tag is the punch button, not a second action. Face buttons draw as A, B, and X chips. A PlayStation pad uses its own shapes when that pad was the last device. There is no separate Look setting for the family.

`Docs/UiStills/pass46/arena.png`, `options-controls.png`, and `options-sound.png` are the checks. Each is under 400 KB. pose stays 0 and rigJoint stays 26. The proof lines are unchanged.

## Pass 47

Left Alt is air dash only. Sprint is Left Shift on the keyboard and LB on the pad. The Alt markers and the header warning are gone. The defaults conflict check reads the keys `PlayerInputReader` samples. Putting Alt back on the sprint line fails that check. The binds table alone still would not. Mega Park, Pocket Park, and Stack Yard show their launch pad and zip counts on the detail panel. Random does not. Mega Park is 5 pads and 5 zips, Pocket Park is 2 and 2, and Stack Yard is 3 and 3. Every controls row keeps the keyboard words on the left and the pad mark in one column: A, B, and X chips, LB and RB shoulder chips, and stick words for Move, Look, and Cling. Mode and rules lists the rules that already exist. Round length, rounds, the It handicap, and pads and zips on or off are on that page. Handicap rows use MenuMannequin.Shape: circle, triangle, square, diamond. Title, the main menu, join, and pause are unchanged.

`Docs/UiStills/pass47/arena.png`, `options-controls.png`, and `rules.png` are the checks. Each is under 400 KB. pose stays 0 and rigJoint stays 26. The gameplay proof lines are unchanged.

## Pass 48

Opening a screen hides the one that was up. Mode and rules no longer draws the arena tiles underneath. The flow check fails when two screen roots stay visible, and the old stacked open fails that check. Hot Potato's line reads the live win target, so it says first to 2 wins while the win target is 2, and it does not call that the rounds row. Least It shows a gold check chip for the selected mode. The rules column starts with "Rules for Least It". Grapple's pad column reads "Not on pad yet" in the muted ink. No pad button was added. That bind is still open, and `Docs/Controls.md` says so.

`Docs/UiStills/pass48/arena.png`, `options-controls.png`, and `rules.png` are the checks. Each is under 400 KB. pose stays 0 and rigJoint stays 26. The gameplay proof lines are unchanged.

## Pass 49

The selected-mode mark is two strokes on a gold chip. Neither UI font has a check character, so the old text mark was a tofu box. A coverage check reads both font files and every screen string. A clean set reports no missing characters. Adding the check character to that set fails it.

Win target clamps to the round count on every change. A one-round match shows Win target 1, and Hot Potato's line follows that. Raising the target on one round stays at 1. Dropping the rounds pulls the target down with them. The rules list stays inside the title-safe area. A help line under the focused row says what that row does. For Least It, the handicap is a label and it does not change time as It. For Hot Potato, it does not change the fuse. The bar under that reads Space confirms and Esc goes back.

Results cards use the seat band: P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. The costume swatch stays beside that. The seat tag is the shape, not a text glyph. Title, the main menu, join, pause, and loading are unchanged.

`Docs/UiStills/pass49/rules.png` and `results.png` are the checks. Each is under 400 KB. pose stays 0 and rigJoint stays 26. The gameplay proof lines are unchanged.

## Pass 50

Rules stay as they were in pass 49. Results fills the safe area. Each card is most of the column: the seat-tinted figure stands in it, with that seat's shape on the chest. First celebrates, second and third stand, and fourth shrugs. The soles sit 0.5 cm above the block. First is a little taller. The header reads RESULTS. The lines on a card are the tracked match record: time as It, tags, punches landed, and longest time untagged. The costume is a chip that says Costume and the look name. First wears a gold ribbon. Gold on a button is focus, so Rematch is the only gold outline. The buttons sit on the bottom of the safe area, above a bar that reads Space confirms and Esc goes back.

Controls, while a bind is listening, says Press a key or a button. Esc cancels. A key that already belongs to another action swaps, and the bar says Swapped with that action. Reset says the bindings are back to the defaults and Jump is Space. Title, the main menu, join, pause, and loading are unchanged.

`Docs/UiStills/pass50/results.png` and `controls.png` are the checks. Each is under 400 KB. pose stays 0 and rigJoint stays 26. The gameplay proof lines are unchanged.

## Pass 51

This lane absorbed draft PR #126 at `31519be7`. Load, join, the main menu, and the title keep that Hier idle. RESULTS uses the same SeatLoad atlas on every card. The body is 1.8 m on each card. First stands on the tallest plinth, then second, third, and fourth. The body does not grow. The seat shape sits on the badge: P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. The costume chip still names the look. Rematch is the only gold button. The header stays RESULTS.

The card art is the idle atlas, so the arms stay in the bind pose. Celebrate, stand, and shrug still run on the off-screen podium bodies. Those poses do not change the card pixels, and they do not lift the root. Soles on the atlas stay at 0.5 cm.

Grapple's pad column is LT. The stored token is `leftTrigger`. RT is shown as free. The stutter double-tap stays behind a flag that defaults off, so the list does not bind it. `Docs/Controls.md` says the same. The loading tip still does not invent a pad glyph.

Picture has a Speed lines row. The label is Speed lines: Off until a seat turns it on. It is not one of the 21 settings rows and not an FX-kit toggle. The saved key is `speedLines`.

`Docs/UiStills/pass51/` holds results, controls, options, and load. Unity is not installed, so results, controls, and options are drawn to match the builders. Load is the pass 32 plate. pose stays 0. rigJoint stays 26.

## Pass 52

RESULTS cards no longer crop the idle atlas. First place samples a celebrate frame: both arms in a V, a lean back, elbows at rest. Second, third, and fourth sample a relaxed stand: chest open, upper arms off the bind hang, elbows soft. Both frames use the same Hier idle family, the same camera, and the same 1.8 m slot. The root is not hopped. After the lean, the sole is planted at 0.5 cm, the same plant as the idle atlas. Rank stays the plinth. The badges stay red circle, blue triangle, orange square, lavender diamond.

Load and join still use the bind-pose idle. The stand frame is wider than MenuAlive.Ready's arm deltas, which are about 5 degrees and still read as the bind on a card. On this FBX the raise axis is local Y, so the celebrate sample is the V, not a raw Unity Euler pasted onto the imported rest. The off-screen podium still plays Cheer, Ready, and Shrug. Grapple stays LT. RT stays free. Speed lines stay off until a seat turns them on.

`Docs/UiStills/pass52/` holds the results before and after. Unity is not installed, so the after still is a composite of the sampled frames on the results layout. pose stays 0.

## Pass 53

The pass 52 stand opened the upper arms far enough to read as an A-pose. The hanging arm is back to about 12 degrees off the torso, which is the bind shoulder, with the forearm brought in so the elbow is soft. Load and join use that hang. Results keeps the celebrate V on first. Second pumps one fist. Third leans onto one leg. Fourth slumps, head down. Same Hier family, same camera, same 1.8 m square. The root is rotated for the lean and then planted. Soles print 0.005. There is no hop.

`glyph-cover check=1 FAIL U+2713` and `screen-roots stacked=2 FAIL two roots` are spike lines, not product bugs. `FontCover.Holds` passes only when every live string is in both UI fonts (`clean=0`) and a planted U+2713 is missing (`dirty>0`), because neither font has a check mark. `ScreenDeck.Holds` passes only when `ShowOnly` leaves one root (`clean=1`) and the old `ShowStacked` open leaves two (`stacked=2`). The ui-flow line stays `glyphs=ok` and does not append `screen roots`. The sim still exits 1 on `no-clip rest overlaps`.

`Docs/UiStills/pass53/` holds results and load before and after. Join uses the same seat atlas as load. Unity is not installed, so the stills are composites. Controls were not reshot. pose stays 0.

## Pass 54

Amaterasu's Unity 6000.3 did not compile `0d3b9d74`. `MenuArt` now imports `UnityEngine.UI`. The duplicate locals `bustY`, `y0`, `y1`, and `armed` are renamed. `CoverFlyover` reads `UnityEngine.Screen`, because `MenuHost.Screen` is the menu id. `MenuTheme.SeatBand` is the non-fill seat color. Screenshot capture goes through reflection, and if that module is off a camera grabs the overlay canvas.

The station's play-mode frames are the proof from here. Composite stills are not. On the station:

`Unity -batchmode -projectPath . -executeMethod Tag.Ui.Menu.MenuScreenCapture.Capture -screenshot Docs/UiStills/captures`

Do not pass `-quit`. In the editor the same walk is Tag → Menu → Capture Screens. `Assets/Tests/EditMode` builds every `MenuScreenId` except Hidden. `Tools/ScriptCompileCheck` fails the push gate if `MenuArt` drops the UI using, `MenuHost` uses a bare `Screen.width`, or runtime scripts call `ScreenCapture` directly.

RESULTS no longer plays the comic wipe. A leftover wipe is destroyed, and a wipe on any other screen sits behind the page and is a short stripe, not a 1400 px capsule. The chevron, ribbons, and orbs stay off on RESULTS, and the dim is 0.92 so the park does not show through the type. The 3D podium, parade, and menu pair live on layer 31. Every other camera drops that bit. RESULTS passes a null view, so the podium root stays inactive and its camera stays off. The cards are the screen.

The Hier file is Z-up until it is stood up. `Spawn` no longer leaves that mesh on its back: it picks a rotation whose height is Y, yaws the chest toward +Z, and scales a wild import back into 1.15–2.7 m. `MenuCheer` and `MenuStride` keep that rotation and multiply the pose on top. The main-menu pair camera clears navy and frames the stood body, so the hero is not an empty black panel. The Play mark is a simple well, and the label starts to the right of it.

Pose retune for second, third, and fourth is still the rest of this pass. First stays the celebrate. Load stays the hang. Unity is not installed here, so this machine cannot show the play-mode frame. The local gate is `script-compile-check ok`.

## Screens 2, pass 1

Secondary screens pick up the park wash and a comic wipe under 0.4 s. Arena select is a three-card grid: Mega Park, Pocket Park, and Stack Yard, then Random and Back. Mode tiles are a 2 by 2. Right from the right-hand mode enters the rules. Left on a rule that cannot go lower returns to the modes. Up and down stay in a column. The rules list has a gold scrollbar.

RESULTS stays the heading. The runners keep the body and accent pair from the color choice. The sample set is Red, Blue, Orange, and Lavender, with Tan or Mint on the accent. Stat cards sit in the same left-to-right order as the figures: 2nd, 1st, 3rd, 4th.

Options says Sound, Picture, and Accessibility. Controls shows a keyboard glyph and a pad glyph on each bind. Jump's keyboard glyph is the space bar. An empty join seat says Press Space or A to join and draws both marks. Loading and credits sit on a navy card. Records drops the repeated name from the card line.

Stills for this pass are layout drawings in `Docs/UiStills/screens2/pass1/`. Unity is not running here, so they are not editor captures. `ui-flow` still passes, with `sheet=ok`.

## Screens 2, pass 2

The RESULTS banner names the winner's body color and accent color, as `Red / Tan` on the sample set. That line sits on a navy plate so the gold type clears the park photo. The heading stays RESULTS. The stat cards are unchanged, so the three stat lines still fit.

Loading lifts the tip off the rule list. It sits on a gold plate above the bar, with TIP in ink and the tip line in ink. The first tip is still "Jump again to leave the wall." The bar still reads Waiting until the match starts.

The layout drawings for this pass are in `Docs/UiStills/screens2/pass2/`. The chest panel on each figure is the accent color. Hands and feet in those drawings stay the body tint, because the bake does not split those meshes. The comic wipe is also shown over loading and over RESULTS. Unity is not running here.

## Screens 2, pass 3

The navy plate behind the banner turns on for every secondary screen that has a banner line, not only RESULTS. Title, the main menu, and character select do not use it. Records can scroll to Back. That last window is in `Docs/UiStills/screens2/pass3/`.

## Screens 2, pass 4

RESULTS runners are rasters of the posed Hier bake, the same path as the character cards: `hier-idle-0` through `hier-idle-3`, body on mat 0, accent on mat 1, soft joints. The sample pairs stay Red/Tan, Blue/Mint, Orange/Tan, and Lavender/Mint. Legs, torso, and head take the body color. The chest panel, the hands, and the feet take the accent. Stat cards are a dark navy with a quiet stroke. The winner's stroke is gold. The slot color sits only on the P badge, and the letter uses whichever of ink or cream clears that badge.

Loading keeps the three real steps (Waiting 0, Starting 50, Ready 100). While it is waiting, a gold dash slides in the track and the caption reads `Waiting  0%`. The tip plate and the bar sit above the card's bottom edge. Each visit of the loading screen advances the tip. The first one is still "Jump again to leave the wall."

Options hub buttons each have a one-line description. The hub banner no longer ends with the comic-words line. Comic words is a switch on Accessibility, `Comic words  On` by default, and the pause banner follows that switch.

The Move marks are the keyboard arrows and the pad stick, the same caps the HUD draws. Stills are in `Docs/UiStills/screens2/pass4/`. Unity Editor 6000.3.24f1 is not installed here, so `MenuScreenCapture` cannot enter play mode. The runners are `ArenaStill.WritePlaceFigures`. The chrome is composited from the menu's colors, copy, and placement.

## Screens 2, pass 5

The stills in `Docs/UiStills/screens2/pass5/` are software-raster composites. Every filename ends in `-composite`. Unity Editor is still not installed, so these are not play-mode captures. The runners are a front view of the Hier bake, facing the camera. The winner is larger and stands on a higher step. The gold frame stays on that card. The loading card keeps every rule row clear, then the tip, then a bar filled from the left. The options buttons show a grey sub-line. The red and blue P badges clear 4.5:1.

## Screens 2, pass 6

Options buttons and the RESULTS rematch row are tall enough for a full-size title under the stripe, then a grey sub-line on its own row. RESULTS bodies use the slot fallback from worker 1 tip `29b3dbdd`: red, blue, yellow, green. That tip's character-select costumes are still Red/Tan, Blue/Mint, Orange/Lavender, Lavender/Mint. The runners here follow the slot colors so the badge and the body match. Feet sit on the steps. The 2nd, 3rd, and 4th steps descend. Accessibility shows the comic-words switch. Stills are composites in `Docs/UiStills/screens2/pass6/`, and every filename ends in `-composite`.

## Screens 2, pass 7

Runtime and the composites use the same numbers. Options and Accessibility rows are 108 px tall on a 116 px step. The stripe stays in the top 16 px, the title is 40 px and starts 24 px down, and the sub-line starts at 70 px in FloorFont 30. RESULTS action buttons, including Rematch, are 128 px. RESULTS bodies are tinted with MenuTheme.Seat (fallback red, blue, yellow, green) after the costume spawn. Accent stays the costume swatch (sample Tan, Mint, Tan, Mint). The loading still is the Waiting gate: caption `Waiting  0%` and a gold dash 28% of the track, which is the frame the screen opens on. Stills are composites in `Docs/UiStills/screens2/pass7/`.

## Screens 2, pass 8

The Default accessibility palette is the seat slot colors: red, blue, yellow, green. Colorblind palettes stay on their own rows, and the swatch row names the palette that is showing. Reduce motion and Comic words draw a 96 by 40 pill; on is gold with the knob on the right. Loading uses `SceneManager.LoadSceneAsync`. The bar follows `AsyncOperation.progress`, and `allowSceneActivation` stays off until progress reaches 0.9 so the load can finish at 100. The loading still is that mid-load, `Loading  60%`, filled from the left. Control rows use the same 108 px height and 116 px step, so the title stays 40 px. At 100% the window shows 7 rows. Stills are composites in `Docs/UiStills/screens2/pass8/`.

## Screens 2, pass 9

Drop-in cards keep the seat tint and the seat stripe when they are selected. The gold stroke is the highlight. A joined seat draws a bust in the slot color and a Ready or Joined chip. An empty seat draws Space and A at 64 px. Y on a seated player toggles Ready. Confirm still opens character select. Records with no profiles is one card, "No records yet. Play a match to set one.", plus Back. The filled picture is sample data and its filename contains `-sample`. GoToPlay, the Rematch scene-load fallback, and QuitToMenu use the same async load as the menu boot. Stills are composites in `Docs/UiStills/screens2/pass9/`.

## Screens 2, pass 10

The drop-in banner reads `Everyone Ready? Press Start`. A keyboard seat's card says `Space / Enter`. A pad seat says `A`. Each joined card shows `Y  Ready`. The bust is 40% of the card and sits under the seat name. The profile name, with left and right arrows, sits under the bust. The device line sits above the Ready or Joined chip. Empty seats still say `Press Space or A to join`. The empty records mark is a cup: a rim, a bowl, two handles, a stem, and a base. The headless walk counts Records as one card plus Back when no profile is saved, and as each saved profile plus Back otherwise. OpenSetup uses the same async scene load as quit. Stills are composites in `Docs/UiStills/screens2/pass10/`.

## Screens 2, pass 11

A joined card labels the device with a keyboard icon and the word Keyboard, or a pad icon and the word Gamepad. The ready banner keeps `Everyone Ready? Press Start` and draws the Space key beside a Start button. The footer shows the keyboard glyph and the pad glyph on each hint: Arrows / Stick move, Space / A confirm, Esc / B back. The bust is the front idle outline of the Hier bake: a round head, a neck gap, a tapered chest, arms off the torso, and two legs, tinted with the seat color. Pause opens on Resume. Options from pause labels Back as Pause. Picture quality reads Medium. The text-size and UI-scale rows draw a meter. Controls opens on Move, and the cling line includes the wall jump. Credits includes the one-shot synthesis line. Stills are composites in `Docs/UiStills/screens2/pass11/`.

## Screens 2, pass 12

Each controls glyph is the sprite for that row's ActionBinds token. Punch's pad mark is the blue X, not the green A. Slide shows the Ctrl and C keycaps with B. Air dash shows Q and Alt with RB. Punch shows LMB and E with X. Cling shows the hold-into-wall mark and the stick. The line uses Xbox names: A, B, X, Y, RB, and LT. There is no footnote row. Xbox, PlayStation, and Switch share one gamepad token per action, and gameplay jump stays the south button. The pause banner is the arena and the mode. Comic words stays on Accessibility. Stills are composites in `Docs/UiStills/screens2/pass12/`.

## Screens 2, pass 13

Sound, Picture, Accessibility, and Look apply when you step a row, and the settings blob keeps the value. Master, mute, and the listener are one group. Sfx, UI, and music are the bus gains. Picture quality calls QualitySettings. Text size scales menu type and the match HUD. UI scale still scales the canvas. Comic words turns the verb words on and off. A second headless line, `ui-apply`, sets each value, reloads, and reads it back. Each options page has Reset to defaults. The first confirm asks, the second resets that page. Loading still names the arena and fills the bar from the scene load. The tip it opens on is the cling and wall-jump line. Stills are composites in `Docs/UiStills/screens2/pass13/`.

## Screens 2, pass 14

Picture quality is Low, Medium, High, and Ultra. Medium stays the level the game boots on, with the same shadows, antialiasing, and LOD bias as before. The Picture row steps through all four. The options hub puts Reset and Back on one bottom row, with the confirm and back glyphs from the default binds. Accessibility shows full P1–P4 tiles and a protan, deutan, and tritan preview of those four colors, with the pair distance beside each row. Text size steps are 0.85, 1.00, 1.25, and 1.50, and 1.00 is still the size the screens already used. The mute path prints `ui-bus` from the headless run. Stills are composites in `Docs/UiStills/screens2/pass14/`.

## Screens 2, pass 15

A settings blob without `qv` still means the old single picture level. Stored quality 0 loads Medium. New saves write `qv=1`, so Low stays Low. The headless run prints `ui-quality`. Accessibility can switch the four seat colours to a Protan/Deutan set or a Tritan set. Both clear the 0.35 pair floor under protan, deutan, and tritan. Off keeps the original colours. Seat chips gain a circle, triangle, square, or diamond only while that option is on. Text size 1.50 grows every menu row and leaves the type at that size. Reset and Back on Accessibility sit on the bottom row, outside the scroll. Stills are composites in `Docs/UiStills/screens2/pass15/`.

## Screens 2, pass 16

Color-blind seat colors is the second Accessibility row, so it sits in the first window at 1080p. The older row is Player color set. That one still cycles Default, Deuteranopia, Protanopia, Tritanopia, and High contrast. The two rows are not the same control. Seat colors stay Off unless the player changes them. Drop-in says where to find them. Text size 1.25 and 1.50 keep the screen title in the top band, and the character cards name Red / Tan, Blue / Mint, Orange / Lavender, and Lavender / Mint. The hat line and the mode blurbs wrap instead of being cut off. Controls can show a conflict, and Jump stays on Space until reset. Stills are composites in `Docs/UiStills/screens2/pass16/`.

## Screens 2, pass 17

Controls lists every action in the input map. Move, Look, Jump, Cling hold, Slide, Air dash, Punch / tag, Sprint, Pause, Minimap, Arena 1, Arena 2, and Arena 3 each have a row. Grapple stays a note: RMB, a click pulls, a second click within 0.28 s releases, left hand, no pad bind. Keyboard and mouse glyphs are one column. Pad glyphs are the other. P1 through P4 choose which pad that column edits. Confirm waits 5 seconds for a button. Esc or B cancels. Space stays Jump, and a second key can sit beside it. The line says Space always jumps, then names the key that was added. When two rows want the same button, both light up, and the choice is Swap or Cancel. Reset and Back stay on the bottom row, outside the scroll. Reset asks once, then clears that pad and the shared keyboard. Stills are composites in `Docs/UiStills/screens2/pass17/`.

## Screens 2, pass 18

Shipped defaults no longer give Alt to both Air dash and Sprint. Air dash is Q and RB. Sprint is Shift and LB. The headless walk appends `defaults-conflict=0` after it counts the keyboard table and all four pads. Grapple is a bind row: RMB on the keyboard, LT on the pad. A press pulls. A second press within 0.28 s releases. Cling hold draws WASD and Left stick, which is the move-into-wall hold the motor already samples. Look draws Mouse and Right stick. Both glyph columns sit inside the row, keyboard on the left and pad on the right, under those headers. Arena 1, Arena 2, and Arena 3 stay in the bind table and show only in a development build. Stills are composites in `Docs/UiStills/screens2/pass18/`.

## Screens 2, pass 19

Each split pane shows who is It: a large IT plate on that player, and a gold arrow with an upright IT chip for everyone else. The chip stays inside the pane. A round timer sits at the top of each pane when the pane is not the full-width top of the screen; that case keeps the shared clock. The name plate carries the mode line and the tag count. After a tag, the previous It keeps the existing one-second immunity: the pane glows and the safe row reads SAFE plus the tenths. Dash shows DASH when it is ready, the cooldown digits while it fills, and GO while it fires. Comic words swaps the display face on those words and on the tag feed. The feed sentence stays, on an ink plate, with the tagger's colour and, when color-blind seat colors are on, that seat's shape. The 3-up score list uses the same cream line, colour chip, and shape. Text size still scales the HUD. Stills are composites in `Docs/UiStills/screens2/pass19/`.

## Screens 2, pass 20

One match state is shared by every pane. The script is P1 tags P2, then P2 tags P3. P3 is It. P2, the previous It, holds the 1.0 s tag-back window and the safe row reads the time left. The feed on every pane is the same two lines, newest first: `P2 tagged P3`, then `P1 tagged P2`. The pane border is that seat's colour. It adds a pulsing yellow inner frame, and only on the It pane. Safe adds a warm wash and the SAFE countdown, and only on the previous It. A runner who can see It gets an IT chip over that head. A runner who cannot gets an arrow on the pane rim, in that direction. The headless line is `hud-state`. Stills are composites in `Docs/UiStills/screens2/pass20/`.

## Screens 2, pass 21

The same scripted match now carries the beats around the chase. Each pane counts 3, 2, 1, GO. Until GO the center card says LOCKED, and the opening It flashes on that player's pane. The last ten seconds turn the clock gold and pulse it. The tick is the existing round chime. ROUND OVER comes first. Standings then list the least It time first, with NEXT ROUND under them. The final round says RESULTS. A new It gets a short gold edge on that pane, and YOU'RE IT when comic words are on. A seat that leaves drops out of the split, and every remaining pane notes `P4 left`. Solo is one human pane against the AI. The headless line is `hud-state`. Stills are composites in `Docs/UiStills/screens2/pass21/`.

## Screens 2, pass 22

The standings card grows with the rows. Each row has a rank, the seat shape, the name, It time, tags made, times tagged, and round wins. The least It time is the winner, and that row is gold. The rows slide in by rank. During 3, 2, 1 a pane shows the name and the seat chip. The stat line comes back at GO. LOCKED is a small plate under the count, inside the pane, and the center card stays clear. YOU'RE IT sits above an empty slot near the tagged player. That slot is open for a comic word. Loading between the menu and the match names the arena over the park, rotates a controls tip on each seat, and fills a bar, from one pane to four. Stills are composites in `Docs/UiStills/screens2/pass22/`.

## Screens 2, pass 23

Seat chrome reads the costume swatches already used by the menu lane: P1 red, P2 blue, P3 orange, P4 lavender. `MenuMannequin.Swatch` is that table. Pane borders, chips, standings squares, and the loading card use it. The measured mark palette stays red, blue, yellow, and green, so the color-blind distances do not move. A color-blind seat set still replaces the chrome when that option is on. Each standings chip sits in a small dark rounded well, so the orange winner chip stays visible on the gold row. The shape stays ink on that row. A row reads rank, chip and shape, then the name, then It time, tags made, times tagged, and round wins. The time header says IT TIME. Loading cards sit in the bottom of the pane so Mega Park shows above them, and the bar stays. The tip names that seat's bound mark. A pad reads Hold [Left stick] against a wall to climb, and Hold [Left stick] + [A] to wall jump. A keyboard seat reads WASD and Space. Stills are composites in `Docs/UiStills/screens2/pass23/`.

## Screens 2, pass 24

The world It marker, the ghost, the results tint, and the in-world name chip read `MenuMannequin.Swatch`. That is the same costume four as the chrome: P1 red, P2 blue, P3 orange, P4 lavender. P3 is no longer yellow in the world. When colour-blind seat colors are on, that CVD set replaces the HUD and the world together. `ui-cvd` and `ui-seat` are measured on the four that ship. They miss the 0.35 floor: `ui-cvd protan=0.08 deutan=0.05 tritan=0.20`, and `ui-seat off=0.08/0.05/0.20`. Orange against red is 0.31 under protan, 0.28 under deutan, and 0.20 under tritan. Blue against lavender is closer still (0.08 protan, 0.05 deutan). The old mark palette is not kept beside these colors. The shapes already on the seats (circle, triangle, square, diamond) plus a lighter and a darker step inside each costume hue would separate those pairs. Loading no longer veils the whole screen. Mega Park stays bright above the card, and the card is the dark plate. Each seat frames a different part of the yard, and P2 also shows the chase runners. A tip names the action from the input map, then the glyph: Sprint [LB], then slide [B]. The load strip is the three captions the plate paints: Waiting 0%, Loading 60%, and Ready 100%. Unity is not installed, so the stills are composites in `Docs/UiStills/screens2/pass24/`. The world marker is not in those stills.

## Screens 2, pass 25

Each costume hue now has a lighter or darker step, and the seat shape is drawn in that colour on a dark well wherever the colour stands alone. P1 is a darker red circle, P2 a deeper blue triangle, P3 a lighter orange square, and P4 a lighter lavender diamond. The hues stay the ones that shipped. `ui-cvd` measured on those four is `protan=0.58 blue/lavender deutan=0.53 red/orange tritan=0.50 red/orange floor=0.35`. `ui-seat off=0.58/0.53/0.50`. Every pair clears 0.35. The worst protan pair is blue against lavender. The worst deutan and tritan pair is red against orange. The HUD chip, the feed chip, the It marker glyph, the RESULTS row, the character card, and the load card all use that dark well and the coloured shape. The 1280×720 four-up draws the shape at 48 px so it reads at couch distance.

Loading plates are a mid exposure. The still grades Mega Park with contrast 1.35 and brightness 0.75. The upper half of that plate is luminance 135, and each pane's upper park sits between 121 and 133. P1 frames the left path, P2 the chase runners above the card, P3 the right arch, and P4 the center landmark. None of those windows is the empty floor. The live texture is still the ungraded plate, because this machine has no Unity to bake a graded material. Waiting 0% leaves the track empty of a fill. A thin dim sliver moves inside the track. Loading 60% and Ready 100% are the gold fill.

Cling's pad token is `leftStickHold`, and `Show` reads "Left stick hold". The short chip "Left stick" is Move, so the tip does not use it. The keyboard token is `holdIntoWall`, and `Show` reads "Hold into wall". The same check covers Jump, Sprint, Slide, Air dash, and Punch. Their chips do not collide with Move or Look: Space, Shift, Ctrl, Q, LMB, and on a pad A, LB, B, RB, X. The opening tips are Jump [Space], Sprint [LB], then slide [B], Cling hold [Left stick hold] against a wall to climb, and Cling hold [Left stick hold] + jump [A] to wall jump. Unity is not installed, so the stills are composites in `Docs/UiStills/screens2/pass25/`. The world marker is not in those stills.

## Screens 2, pass 26

The tip bar and the progress track are separate rows, with a gap between them. The arena size sits under the name: Mega Park, then `160 x 100 m`. A layout check rejects a still when any text rect leaves its row or intersects another text rect. The four-up prints `layout=ok texts=32 rows=16 hits=0`.

Lavender stays lighter than blue, and it keeps its colour. The swatch is `(0.82, 0.70, 0.98)`, about 92% of the chroma of the base lavender `(0.70, 0.58, 0.88)`. Re-measured on the four that ship: `ui-cvd protan=0.41 blue/lavender deutan=0.52 blue/lavender tritan=0.41 orange/lavender floor=0.35`. `ui-seat off=0.41/0.52/0.41`. The floor still holds. Seat shape and body live in one `MenuMannequin` table: P1 circle, P2 triangle, P3 square, P4 diamond. The chip sprite and the glyph both read that mark.

Loading stills pull the golden-hour cast until the concrete is grey (about 134, 137, 141) and the grass that is in the yard reads green. Crops are large enough to downscale onto a four-up pane, then a light sharpen. P1 frames the dock, P2 the chase runners with their heads in frame, P3 the basketball court, and P4 the gazebo. Unity is not installed, so the stills are composites in `Docs/UiStills/screens2/pass26/`. The live texture is still the ungraded plate. The world marker is not in those stills.

## Screens 2, pass 27

The load card is the bottom third of the pane, with a smaller chip and tighter rows. The 1280×720 four-up prints `layout=ok texts=31 rows=16 hits=0`. The dock, the court, and the gazebo sit in the visible upper area. White balance stays neutral: a concrete patch reads about 140, 148, 146. An S-curve and a saturation lift bring the sky back to blue (about 155, 171, 189) and leave green in the grass. The live texture is still the ungraded plate.

The diamond fill is the base lavender `(0.70, 0.58, 0.88)`. The light step `(0.82, 0.70, 0.98)` is the pane band. `ui-cvd` measures that band: `protan=0.41 blue/lavender deutan=0.52 blue/lavender tritan=0.41 orange/lavender floor=0.35`. `ui-seat off=0.41/0.52/0.41`. The floor holds on the band. The fill against blue is protan 0.23 and deutan 0.34, under 0.35. The band is the value step that keeps the measured floor.

A pad cling tip reads `Hold [Left stick] into a wall to climb`, and the wall jump reads `[Left stick] into a wall + [A] to wall jump`. The verb and the input are each said once. Keyboard cling reads `Hold [WASD] into a wall to climb`.

Pause was the weak screen. The pass 16 still left a flat empty band under the buttons, and Resume, Options, and Quit had no second line. The card now sits on the bottom third. Each row has a line. The graded park, including the gazebo, stays visible above the card. P1's circle is in the header. Unity is not installed, so the stills are composites in `Docs/UiStills/screens2/pass27/`.

## Screens 2, pass 28

The live Mega Park plate is the graded yard. `MenuBackdrop.Bright` loads `Assets/Resources/UI/Menu/MegaGrade.png`, baked with the same neutral white balance, S-curve, and saturation lift as the pass 27 stills. Mega Gold stays the ungraded source. Loading already assigns that bright plate, so a Mega Park load uses it. The baked file measures the same as the pass 27 grade: sky about 155, 171, 189, concrete about 140, 148, 146.

Pause no longer leaves the graybox showing through. It puts that graded plate up and darkens it with black at 35%, so the card reads first and the park stays recognizable. The pass 27 card is unchanged: bottom third, a second line on each row, and P1's circle. The pause still measures card fraction 0.33 and park standard deviation 16.7. The sky on that still is about 65% of the raw plate, which is the 35% darken.

The title logo keeps the lockup's shape and is large enough for the couch (860 by 658). The prompt is `Press Space or Start` on a keyboard and `Press Start or Space` on a pad. Space is `ActionBinds.Show` of Jump, and Start is `ActionBinds.Show` of Pause. The main menu uses the same graded plate, and the focused row wears a 16 px gold edge. Drop-in join stamps `MenuMannequin.Shape`: circle, triangle, square, diamond, in the seat fill on a dark well. Arena select, RESULTS, and Options are untouched.

`ui-cvd protan=0.41 blue/lavender deutan=0.52 blue/lavender tritan=0.41 orange/lavender floor=0.35`. `ui-seat off=0.41/0.52/0.41`. The floor holds on the band. The lavender fill against blue is still under it (protan 0.23, deutan 0.34). `hot-path allocs before=101 after=0`. No new pose was animated: `no-clip pose=0`. Unity is not installed, so the frames in `Docs/UiStills/screens2/pass28/` are that live plate with the menu drawn on it.

## Screens 2, pass 29

Title and the main menu scale MegaGrade to cover the 16:9 frame and crop the overflow, so the plate is not sitting in a black letterbox. The edge vignette is off on those two screens. Title darkens the plate with black at 35% so the lockup sits on the graded yard. The sky on that still stays blue (blue channel above red). `MenuBackdrop.Bright` still loads `MegaGrade.png`.

The main menu lights one row. Play is the focus. Records is quiet. `RefreshFocus` drops a second tile that shares the focus index. `ui-flow` fails the walk if `MenuSheet.OneFocus` is false or if `BuildMain` calls `SetHot(true)`. The printed line stays `focus=ok` when that holds.

The main-menu grapple tip is `MenuTips.GrappleLine`. The glyph is `ActionBinds.Show` of the grapple key. Rebinding it to Q prints `[Q]` and drops the old `RMB` word. Loading tips use `Show` for that line too.

An empty drop-in seat draws a faint outlined silhouette, the seat shape on its chest, and `Press Space or A to join` centred under it. The corner shape stays. Arena select, RESULTS, and Options are untouched.

`ui-cvd` floor 0.35 still holds on the band. `hot-path allocs before=101 after=0`. No new pose was animated: `no-clip pose=0`. Unity is not installed, so the frames in `Docs/UiStills/screens2/pass29/` are the live plate with the menu drawn on it.

## Screens 2, pass 30

The drop-in header says `Everyone Ready? Press Start` only when every joined seat is ready and at least two seats are in. P1 ready and P2 only joined now reads `Waiting for 1 player to ready up`. The count is the joined seats that are not ready. One ready seat, with nobody else in, stays on that waiting line, because two seats are required. The old rule (`Humans > 0` prints the start line) still returns `Everyone Ready? Press Start` for the P2 case, and `MenuSheet.JoinBannerHolds` fails when the new line matches that old line. `ui-flow` fails the walk with `ready banner` if the check does. The success line is unchanged.

The main menu label is a short chip. It is not a full-width strip. The chase plate is off that screen. Two seat mannequins stand in the relaxed idle, pose sample 0, with the seat shape on the chest. P1 is the red circle. P2 is the blue triangle. Their feet plant on the discs at an absolute 0.5 cm (`PlantY = 0.005f`). No new pose was animated.

Title keeps the cover-crop and the 35% darken. The plate is `MegaBlur.png`, a slight blur of `MegaGrade.png`, plus a soft vignette. The corners stay the plate (about 69, 76, 84 on the sky side). They are not black. Loading and pause stay on the sharp grade.

The lavender fill is lighter, `(0.80, 0.72, 0.92)`, bytes `(204, 184, 235)`. Blue stays above red, and red stays above green, so P4 is still a lavender diamond. The band that `ui-cvd` measures is unchanged. The fill pairs are `ui-fill protan=0.43 blue/lavender deutan=0.53 red/orange tritan=0.42 orange/lavender floor=0.35`. Every pair is at least 0.35.

`ui-cvd protan=0.41 blue/lavender deutan=0.52 blue/lavender tritan=0.41 orange/lavender floor=0.35`. `ui-seat off=0.41/0.52/0.41`. `hot-path allocs before=101 after=0`. `no-clip pose=0`. Arena select, RESULTS, options, controls, and mode/rules are untouched. Unity is not installed, so the frames in `Docs/UiStills/screens2/pass30/` are the live plate with the menu drawn on it.

## Screens 2, pass 31

The title plate is a wide Gaussian of `MegaGrade.png`, blurred in float, then a smoothstep vignette, then a little blue noise before the 8-bit round. The sky's largest step between adjacent rows is 0.155. The corners stay the plate, about 71, 78, 86 on the sky side. Loading and pause stay on the sharp grade.

The main menu figures are the Hier meshes, Red and Blue, in the relaxed idle. Pose sample 0 bends the knees 4° and levels the soles. The feet sit on the discs at an absolute 0.5 cm. Each one is turned three-quarter toward the menu, tinted with the seat color, with the seat shape on the chest and a contact shadow on the disc. P1 is the red circle. P2 is the blue triangle. No new pose was animated.

Loading and pause use the same short header chip as the menu. Loading says Loading. Pause says Paused by P1, and the place line is its own chip, so the bar is not full width. Resume is the only hot row. The load pane that used to show the chase plate shows the blue Hier idle instead. The card stays on the bottom third, and the park stays visible above it.

`ui-fill protan=0.43 blue/lavender deutan=0.53 red/orange tritan=0.42 orange/lavender floor=0.35`. `ui-cvd protan=0.41 blue/lavender deutan=0.52 blue/lavender tritan=0.41 orange/lavender floor=0.35`. `hot-path allocs before=101 after=0`. `no-clip pose=0`. Arena select, RESULTS, options, controls, and mode/rules are untouched. Unity is not installed, so the frames in `Docs/UiStills/screens2/pass31/` are the live plate with the menu drawn on it. The figures are a Blender render of the repo FBX.

## Screens 2, pass 32

Loading gives every pane the same Hier idle. The figure sits in the lower-right, the same size in each pane, feet on a disc just above the info card, whole body in frame. P1 is the red circle, P2 the blue triangle, P3 the orange square, P4 the lavender diamond. The Orange Hier file is a different skinned mesh, so P3 is the shared rig tinted orange. The waiting track is a full-height bar with a short gold cap at 0%, labeled Waiting 0%. The jump tip is a full sentence, and punctuation stays on that sentence.

Climb is the move wish into the wall. `PlayerMotor.ClingHeld` (`PlayerMotor.cs` 1226–1239) is `dot(wishDir, -wallNormal) > 0.25`. Jump is not cling, and there is no cling button. Keyboard `holdIntoWall` is not sampled: `BindSampler.HeldToken` (`BindSampler.cs` 203–205) returns false for `SharesMove` (`ActionBinds.cs` 415–417). A pad `leftStickHold` is the stick past 0.04 (`BindSampler.cs` 469), and `PlayerInputReader.ReadDriven` (`PlayerInputReader.cs` 390–391) then biases Move forward. Wall jump is `JumpPressed` on the wall or inside cling grace (`PlayerMotor.cs` 474–484, 1020–1025, 1164). Keyboard jump is Space, including the Space OR (`PlayerInputReader.cs` 409–420). Pad jump is South, shown as A. Grapple release is the second press inside 0.28 s (`ExperimentalGrapple.cs` 26, 177–185). Keyboard fire is mouse right (`ExperimentalGrapple.cs` 256–262, `ActionBinds.Show` for `mouseRight`). The pad token is `leftTrigger` (`ActionBinds.cs` 44) but the tip does not print a pad glyph.

No idle clip lowers the Hier arms. `VerbPoseClips.IdleArmPitch` is a constant for the primitive body only (`MenuIdle.cs` sets the Hier hang to 0). There is no `.anim` idle. The arms stay on the bind A-pose. Pose sample 0 is unchanged: knees −4°, soles at 0.5 cm, no root lift. The 30 fps no-clip check was not run, because no new pose was sampled. `no-clip pose=0`.

Drop-in puts that same idle on all four seats, seat color and chest shape, feet on the discs. P1 ready and P2 joined still read Waiting for 1 player to ready up. Empty seats keep Press Space or A to join.

The title plate, the main-menu Red and Blue pair, and the pause card stay as accepted in pass 31, except the main-menu climb sentence and the arms note above. Arena select, RESULTS, options, controls, and mode/rules are untouched.

`ui-flow screens=15 kb=15 pad=15 dead=0 focus=ok back=ok seats=4 drop=ok reclaim=ok min=ok keep=ok cues=9 text=ok hud=ok glyphs=ok feed=ok load=ok board=ok faces=ok rules=ok records=ok contrast=ok style=ok sheet=ok defaults-conflict=0`. `ui-apply master=ok sfx=ok ui=ok music=ok mute=ok res=ok full=ok vsync=ok quality=ok scale=ok motion=ok text=ok player=ok palette=ok comic=ok mouse=ok pad=ok invert=ok fov=ok reset=ok apply=ok persist=ok`. `hot-path allocs before=101 after=0`. Stills: `32-load.png` 383063, `32-main.png` 332883, `32-join.png` 242863. Unity is not installed, so the frames in `Docs/UiStills/screens2/pass32/` are the live plate with the menu drawn on it. The figures are a Blender render of the repo FBX.

## Handoff to #121

PR #126 stays a draft on `cursor/tag-ui-screens2` into `cursor/tag-ui-menu`. Do not mark it ready, merge it, or close it from this lane. #121 is draft PR #121, head `cursor/tag-ui-menu`. After pass 32 that branch owns every screen, including the ones listed here.

Screens this lane last touched, and the live builders in `MenuHost.cs`: title `BuildTitle`, main `BuildMain`, drop-in `BuildJoin`, loading `BuildLoading` / `BuildLoadSeats`, pause `BuildPause`. Character select is `BuildCast`. Credits, records, and practice are `BuildCredits`, `BuildRecords`, `BuildPractice`. Arena (`BuildArena`), RESULTS (`BuildResults`), options (`BuildOptions`), controls (`BuildControls`), and rules (`BuildRules`) were left for #121 and were not edited in pass 32.

Files that carry those screens: `MenuHost.cs`, `MenuWidgets.cs` (`JoinDress`, `HierSeat`), `MenuTips.cs`, `MenuBackdrop.cs` (`SeatIdle`, `SeatLoad`, `MegaGrade`, `MegaBlur`), `MenuIdle.cs`, `MenuPreview.cs`, `MenuSheet.cs`, `MenuMannequin.Colors.cs`, `LoadGate.cs`, `UiFlow.cs`. Art: `Assets/Resources/UI/Menu/SeatIdle.png` (main pair, guid `a91c4e7b2d8f4a0e9c3b6d15f7048e22`), `SeatLoad.png` (four-up atlas, guid `b7e2c91a4f6d4e0a8c5b1d37e90f6a44`), `MegaGrade.png`, `MegaBlur.png`. Seat colors are one table: P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. #121 `SeatMark` still maps P2 square and P3 triangle. Do not import that mapping over this one without a deliberate rebase.

Render, from `/workspace`. Unity is not installed. Blender 4.0.2 is. Main pair: `blender --background --python Tools/UiScreens/render_mannequin.py` writes `SeatIdle.png`. Four loading and join figures: `blender --background --python Tools/UiScreens/render_seatload.py` writes `/tmp/seatload/seat0.png` through `seat3.png` (override with `SEATLOAD_OUT`). Then `python3 Tools/UiScreens/render_pass32.py` packs `SeatLoad.png` and writes `Docs/UiStills/screens2/pass32/32-load.png`, `32-main.png`, and `32-join.png`. Pass 31 title, main, and pause: `python3 Tools/UiScreens/render_pass31.py`. Stills must stay under 400000 bytes. Do not commit `Tools/UiScreens/__pycache__/`.

Proof, from `/workspace`, with `PATH=/tmp/dotnet` and `DOTNET_CLI_TELEMETRY_OPTOUT=1`:

`dotnet run --project Tools/StrafeJumpSim/StrafeJumpSim.csproj -c Release -- --ui-flow`

`dotnet run --project Tools/StrafeJumpSim/StrafeJumpSim.csproj -c Release --no-build -- --alloc`

The success lines must stay byte-identical except ui fields a pass was told to add. Pass 32 left them as: `ui-flow screens=15 kb=15 pad=15 dead=0 focus=ok back=ok seats=4 drop=ok reclaim=ok min=ok keep=ok cues=9 text=ok hud=ok glyphs=ok feed=ok load=ok board=ok faces=ok rules=ok records=ok contrast=ok style=ok sheet=ok defaults-conflict=0` and `hot-path allocs before=101 after=0 flags=dropped`. `MenuTips.Holds` and `MenuSheet.Holds` read source from disk. Keep `PlantY = 0.005f`, `HoldRest()`, `IdlePose.At(0f, 0f)`, `ShowMenuPair`, `MenuSheet.JoinBanner(`, no `MenuBackdrop.Chase` inside `BuildMain`, no `CouchPlay.Humans > 0` inside `BuildJoin`, `At(0)` equal to `Space jumps.`, and the grapple line going through `ActionBinds.Show`. Do not edit `ActionBinds.Show` or `ControlGlyphs.GlyphOf`. Feel locks stay: coyote 0.10, jump buffer 0.16, cling grace 0.08, jumpSpeed 24.7, terminal fall 56.16. `SettingsFile.Version` stays 2. `TagBackImmunity.DefaultSeconds` is 1.0. Grapple is not a new `PlayAction`. `enableGrapple` stays false. `FireButton` stays RMB.

Open flaws: Hier arms are still the bind A-pose, because no idle clip lowers them. The world It marker is still a primitive. Least It wins stay 0. A controls swap can still mark two rows. Pad grapple's stored token is `leftTrigger`. The loading tip does not invent a glyph. The controls row prints LT, and RT stays free. The couch rope is on every human seat. pngquant is what fits the stills under 400 KB, and it crushes figure colors. The Orange Hier FBX is a different skinned mesh, so the orange seat is the shared rig tinted. Title sky step 0.155 was accepted; do not re-blur it. Pause card was accepted.

Next, if the work continues: capture these screens in play mode once Unity is available, and replace the composites. If a real idle clip lowers the arms, sample that clip only, then run a 30 fps check that reports arm euler as rig joint and root translation as pose, with pose staying 0 and the soles at an absolute 0.5 cm, and do not lift the root to fake the plant. The loading tip stays without a pad glyph. The controls row prints the stored LT.

## Later passes

- Left from a rule row returns to the modes even when that rule can still decrease. Right from the right-hand mode, and Left at the end of a rule, already move between the columns.
- Per-player look, only if the settings blob grows a seat field. Do not invent it in the menu.
- Replace `Docs/UiStills/pass3/` with the captures from a real Unity play session.
- Online, when it exists. The main menu uses that row for Records until then.
- Profile delete on the join screen. Rename is the character-select keyboard.
- A live camera flyover of the park, once a menu scene can spin a hidden arena without loading Play.
