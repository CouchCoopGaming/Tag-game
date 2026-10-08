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

The title camera is raised south of the straight, so the fence and the crates sit under the frame. The four runners are mid-run on the path, each large enough to read, and the lockup sits above them. The main menu uses that same line, framed in the open middle, clear of the lockup and the buttons. P2's card uses the same opaque lit body as the other seats. The wide sky key had been treating that blue as sky and punching the torso out. The card border and the HUD pane edge are a quiet stroke. The seat color stays on the small P badge, so an orange runner is not boxed in yellow and a lavender runner is not boxed in green. Mockups are in `Docs/UiStills/pass24/`.

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

## Later passes

- Left from a rule row returns to the modes even when that rule can still decrease. Right from the right-hand mode, and Left at the end of a rule, already move between the columns.
- Per-player look, only if the settings blob grows a seat field. Do not invent it in the menu.
- Replace `Docs/UiStills/pass3/` with the captures from a real Unity play session.
- Online, when it exists. The main menu uses that row for Records until then.
- Profile delete on the join screen. Rename is the character-select keyboard.
- A live camera flyover of the park, once a menu scene can spin a hidden arena without loading Play.
