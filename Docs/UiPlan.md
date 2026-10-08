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
Main   Play | Practice | Options | Controls | Credits | Quit
       Online is shown and skipped (coming soon)
        |
        +-- Options ---- audio, look, video, reduce motion, accessibility
        +-- Controls --- existing binds. Space stays jump.
        +-- Credits
        +-- Practice --- existing practice rows, then the loading screen
        |
        v
Join   1-4 slots. A button joins. Back on that device leaves.
       Left/right cycles the seated profile (LocalProfiles).
        |
        v
Cast   3x2 Hier colors. Each human has a colored cursor.
       LB/RB accent, North hat, confirm ready.
       Idle preview cameras. All ready, then rules.
        |
        v
Rules  Least It | Hot Potato | Trail Tag | Free play
       length, rounds, AI count, difficulty, split, listener
        |
        v
Arena  Mega Park | Pocket Park | Stack Yard | Random
        |
        v
Loading  arena name + a How to play line
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
| Title | Wordmark, sliding ribbons, press-start pulse. Any keyboard or pad advance. |
| Main | Chunky tiles. Online is disabled. Practice is the existing practice mode. |
| Join | Four seats, device line, profile name, back leaves that device. |
| Characters | Grid, per-seat cursor, ready, all-ready banner, render-texture idle mannequin. |
| Mode and rules | The four modes that ship, plus the settings rows that already exist. |
| Arena | Three parks and Random. Cards use name and blurb. Thumbnails are color blocks. |
| Loading | Arena name and one control tip, then the existing match start. |
| Pause | Opened by whoever pressed Start. Dim so the split stays visible. |
| Results | Podium order by least time as It. Headline is the mode's result line. |
| Options | Existing look, audio, HUD, accessibility, plus video and reduce-motion for the menu. |
| Controls | Rebind through ActionBinds. Unknown Jump keys are rejected. Space still jumps. |
| Credits | Short original note. Built-in font. Existing UI sounds. |

## Pass 2

Tiles are rounded, outlined, and shadowed. The sky is a gradient with a scrolling chevron. Screens slide in about 0.2 s with a gold sweep. The footer is three prompt chips that swap between keyboard words and pad words. The display font is Liberation Sans Bold (SIL OFL, license in `Assets/UI/Fonts/OFL-Liberation.txt`). Seat chips follow the accessibility palette once someone has joined. Empty slots use red, blue, yellow, and green.

Character previews load `Dummy_Mannequin_*_Hier_Hi` in the editor. The editor bakes the catalog, the arena thumbs, and a mesh portrait the first time those files are missing or older than the source, and again before a player build. Until that bake has saved, a player build uses the primitive mannequin with the same bone names, and the grid draws a bust in that color. Idle breath and weight shift come from `IdlePose`. The primitive also hangs its arms with `VerbPoseClips` idle numbers. Ready stamps the panel.

Arena cards use the overview stills in `Assets/UI/ArenaThumbs` (also under Resources so the build includes them). The hovered card fills the big preview.

The podium orders players by the mode that ended: Hot Potato round wins (first to 2), Least It by time as It with that mode's winner first, Trail Tag last standing, Free play by tags and no winner stamp. The 3D steps use the same mannequin. Confetti plays only when there is a winner and reduce motion is off.

## Pass 3

The Hier catalog, arena thumbs, and character portraits bake themselves when the editor opens if the files are missing or older than the FBX or the overview still. A player build runs the same bake first. The primitive mannequin and the drawn bust stay as the fallback.

Main-menu tiles carry a drawn icon: a runner, a cone, a gear, a pad, a star, and a door, each on a colored well. The character grid shows a portrait and a large name that shrinks to stay inside the tile, so LAVENDER does not touch the edge. The TAG wordmark is slanted, outlined, and backed by a chase streak. Join slots show a keyboard, a pad, or both. The selected tile bobs, and a confirm squashes in before the screen changes.

Move, confirm, and back stay on the UI bus. Ready uses the round-win clip. Starting a match uses the round-start clip. The menu asks for the existing playground music bed. No new audio files.

Tag → Menu → Capture Screens, or `-executeMethod Tag.Ui.Menu.MenuScreenCapture.Capture -screenshot Docs/UiStills/captures`, walks every screen into `Docs/UiStills/captures/` the next time Unity runs. `Docs/UiStills/pass3/` are mockups until then.

## Later passes

- A 2D focus grid on rules so Left from a rule row lands on a mode tile.
- Per-player look, only if the settings blob grows a seat field. Do not invent it in the menu.
- Replace `Docs/UiStills/pass3/` with the captures from a real Unity play session.
- Online tile, when online exists. Leave it disabled until then.
- Profile rename and delete on the join screen (the profile API already has them).
- A live camera flyover of the park, once a menu scene can spin a hidden arena without loading Play.
