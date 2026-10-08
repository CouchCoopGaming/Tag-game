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

Character previews load `Dummy_Mannequin_*_Hier_Hi` in the editor, or the catalog after Tag → Menu → Bake Hier Catalog And Arena Thumbs. Until that bake is saved, a player build uses the primitive mannequin with the same bone names. Idle breath and weight shift come from `IdlePose`. The primitive also hangs its arms with `VerbPoseClips` idle numbers. Ready stamps the panel.

Arena cards use the overview stills in `Assets/UI/ArenaThumbs` (also under Resources so the build includes them). The hovered card fills the big preview.

The podium orders players by the mode that ended: Hot Potato round wins (first to 2), Least It by time as It with that mode's winner first, Trail Tag last standing, Free play by tags and no winner stamp. The 3D steps use the same mannequin. Confetti plays only when there is a winner and reduce motion is off.

## Later passes

- A 2D focus grid on rules so Left from a rule row lands on a mode tile.
- Per-player look, only if the settings blob grows a seat field. Do not invent it in the menu.
- Editor captures from a real Unity play session to replace the mockups in `Docs/UiStills/pass2/`.
- Online tile, when online exists. Leave it disabled until then.
- Profile rename and delete on the join screen (the profile API already has them).
- A live camera flyover of the park, once a menu scene can spin a hidden arena without loading Play.
