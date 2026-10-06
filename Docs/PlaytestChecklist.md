# Playtest checklist

About 25 minutes. Open the project, press Play in Boot, run one short match against the AI on each arena, read the zone label, run a practice route and its ghost, sit a second player on a pad, flip the settings rows, and read the results card. Nothing in this pass adds a move. If a jump, a dash, or a tag feels like the numbers moved, write down the number you expected from the table at the bottom.

This map-lane branch is the converged test tip. Mega Park, Pocket Park, and Stack Yard are all in this build.

Unity **6000.0.23f1**, **6000.0.24f1**, or **6000.3.x**. Hub → Open this repo.

## 0:00 Open

1. Open `Assets/Scenes/Boot.unity`. That is the scene to press Play in. The title is the front door. Start match loads `Assets/Scenes/Play.unity`, which builds the arena you picked. Mega Park is the default (pads, zips, and the AI loop). Pocket Park and Stack Yard are the other two picks, with their own zones and landmarks. Keys are **1** Mega Park, **2** Pocket Park, **3** Stack Yard.
2. Menu **Tag → Smoke Check**. It opens Boot and Play, then writes `Logs/SmokeCheck.txt`. The first line should read `smoke scenes=2 missingScripts=0 resources=ok registry=ok json=ok`. `registry=ok` here means all three arenas (0 Mega, 1 Pocket, 2 Stack), the 13 zones, and the kill boxes. The log under that line should include `registry arenas=3`, `json routes=7`, `zones arenas=3 zones=13 landmarks=13`, and `kill fence=33 rail=2.75 plane=-2.5 arenas=3`. If `missingScripts` is not 0, or `registry` is not `ok`, stop and send that file before you play.
3. Press Play in Boot. You should get a **TAG** card: Play, Practice, Settings, How to play, Quit. Up / Down or the left stick moves. Enter, Space, or South uses the row. Esc or East goes back from a card. On the title, Esc does nothing.

## 0:04 Solo versus the AI

1. Title → **Play**. Match setup: set **Arena** to **Mega Park** (Left / Right, or the **1** key). AI opponents **1**. Difficulty **Normal**. Round length **60s**. Rounds **1**.
2. **Start match**. Press a letter to sit the keyboard. Enter or South starts. You and one dummy. Countdown 3-2-1, then someone is It (hat plus the letters IT). The split label reads `Name · Zone` (a seat with no profile still reads `P1 · West Yard` or whichever zone you spawned in).
3. In the minute, hit each of these once: jump, slide, sprint, air dash, a punch that tags, a punch that does not tag (stagger), a wall cling and a jump off it, one launch pad, one zip. Hold into a wall or a cable to cling. There is no cling button until you rebind one. Walk until the zone name changes. On Mega Park the zones are West Yard, North Bowl, Mid Court, South Court, and East Forts. The rail you see is 2.75 m. The wall that stops you is invisible and taller.
4. After you tag the dummy, the glow is the 1.0 s tag-back window. They should not be able to tag you back inside it.
5. Let the clock end. Read time as It, tags, longest survival, and the winner. If you got a tag, the highlight plays the last 8 seconds. Enter or South skips it, then picks Rematch, Change setup, or Title.
6. Change setup, press **2** for **Pocket Park**, and start another short round. Read the zone label as you cross West Lawn, Center Court, Fast Lane, and East Sand. Then press **3** for **Stack Yard** and read South Yard, East Lane, West Stack, and North Roof. On both, run at the low rail and confirm you stop before you can see a tall fence. You do not need a full minute on these two if the label and the rail are right.

## 0:12 Practice route and ghost

1. Title → **Practice**. Set **Arena** to **Mega Park**. Set **Route** to **Cling lane**. Dummy Off. Ghost On. Input display On.
2. **Start practice**. Wall-run the north cling face, then wall-jump off it, and finish. The splits show `+` or `-` against your best once you have one.
3. Press **T** (or North / Y on a pad) and run it again, faster if you can. The ghost is the previous best. **G** (or left-stick press) hides the figure. The run still records. **I** (or right-stick press) shows Jump, Slide, Air dash, Punch, Sprint, and Cling while they are held.
4. If you have time, set the route to **Pad zip** and ride the pad, an air dash, and the west-rim zip. Then **South fringe** for a sprint and one jump.
5. Set **Arena** to **Pocket Park** and run **West lawn** or **Lane toys**. Then set **Arena** to **Stack Yard** and run **South apron** or **East toys**. Each route should offer a start and a finish on that park. Free roam stays available on every arena.
6. Esc → Quit to title, or Back out. Your match setup from the first round should be what it was.

## 0:18 Two players and a pad

1. Title → Play → Start match. Press a key to sit the keyboard. Press any button on a pad to sit that pad. Click the seat line if you want to cycle a saved profile or Guest. A new machine may only show the open seat and Guest.
2. Set **Split** to Vertical, then try Horizontal. Leave **Listener** on P1 the first time.
3. Start. The screen splits. WASD moves only the keyboard pawn. The stick moves only the pad pawn. Each pane has its own timer and verb cluster.
4. Pause from the pad with **Start**, and from the keyboard with **Esc**. The card should name the seat that paused. Resume.

## 0:23 Settings and accessibility

From the title or from pause, open **Settings**. The Player row chooses the seat. Step each of these and resume so you can see it:

- Colorblind palette: Default, then one of Deuteranopia, Protanopia, Tritanopia, High contrast. The It mark should still read as a star plus IT, and the name plates keep their shapes.
- HUD scale 1.50, then back toward 1.00. Captions On. Stand near the It, take a pad, and grab a zip. The icons should stay inside your pane.
- On the pad seat, Rumble 100%. Tag someone, or get tagged, and feel the short pulse. A keyboard seat should not rumble.
- Reduced flashing On. Tag someone and watch the glow stay flat. The next countdown digit should stay steady.
- Rebind: Pause → Rebind → Jump → press a key → Back. Jump should use the new key. Reset bindings puts Space / South back. Comma stays mute and N stays the music mute. Those two cannot become a gameplay bind.

How to play, from the title or from pause, should list Move, Sprint, Jump, Slide, Cling hold, Wall jump, Air dash, and Punch / tag with the glyphs for the device you are using.

## 0:27 Results

If the 60s round already showed the card, you are done with this step. Otherwise Title → Play, 60s, one round, and finish it.

Check: the winner is whoever had the least time as It. A tie says tie. Awards, if anyone scored, are Hot Potato, Slipperiest, Sky Walker, and Wall Crawler, at most three, and a zero score does not crown one. Rematch starts clean. Title drops you back on the TAG card.

## What to watch for

- One listener. Two copies of the same sound usually means a second AudioListener survived a rematch. Title and Play again if you hear it, and say so.
- The dummy stays inside the fence of the arena you picked and uses that park's loop when it is running away.
- In a split, P2's stick never moves P1. The keyboard never moves a pad-only pawn.
- Practice never puts the It crown on you or the dummy.
- Pause on a zip you already grabbed keeps that ride. A new pad or zip should not start on the results card.
- Captions and the verb HUD stay inside the pane at 1920×1080 and at 1280×720 if you resize.
- Pink or magenta materials, a missing script in the Console, or a pawn that falls and never returns (kill height is -2.5 on every arena). The visible edge stays the 2.75 m rail. The 33 m collider stays invisible.
- The zone chip keeps the form `Name · Zone` in a split and in solo, and it changes when you cross into the next named zone.
- These are already known, so file them only if they got worse: ledge hang is not a move, jet is off, grapple is the solo pawn only, the ragdoll is a kinematic stand-in for the stun, and a HUD line rebuilds when its rounded number changes.

Feel, if you want to compare: coyote 0.10, jump buffer 0.16, cling grace 0.08, jump speed 24.7, slide boost 0, air dash 0.10 s at 15 m/s with a 30 s cooldown, punch reach 1.55, lunge 16 / 0.20 / cooldown 1, lunge tell 0.45, climb 6.0, slip 3.7, wall-run 9.5, stagger 0.25 then 0.50 immunity, pad cooldown 0.3, zip 14 m/s and regrab 0.3, tag-back 1.0.

## How to report

During a round, **F6** toggles the frame card (FPS, milliseconds, movement, AI, pose, HUD, audio, round). F3 is Trail Tag, so leave F6 for the meter. The card exists once Play is loaded. Press F6 again to hide it. A shot of that card plus what you were doing is enough for a hitch.

Send:

- `Logs/SmokeCheck.txt` from **Tag → Smoke Check**.
- The Editor log. Windows: `%LOCALAPPDATA%\Unity\Editor\Editor.log`. macOS: `~/Library/Logs/Unity/Editor.log`. In the Editor, Help → Open Editor Log. A development player writes `Player.log` next to the usual Unity persistent path. Console errors from the session are the part that matters. Copy the red lines.
- `tag-settings.json` if a setting did not stick. It lives under the Unity persistent data path. The same blob is in PlayerPrefs under `Tag.GameSettingsJson`.

With each note, say the arena row you had selected, solo or split, keyboard or which pad, and the keys you used if you had rebound.

## Controls

Defaults from `ActionBinds.Fill`, which is what `BindSampler` reads. Pause → Rebind can change any row. Pause → Settings → Reset to defaults, or Rebind → Reset bindings, puts them back. Jump's keyboard default is Space.

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Jump | Space | South (A) |
| Cling hold | Hold into the wall | Left stick held into the wall |
| Slide | Ctrl | East (B) |
| Air dash | Q | RB |
| Punch / tag | LMB | West (X) |
| Sprint | Shift | LB |
| Pause | Esc | Start |
| Minimap | M | Select |
| Arena 1 (Mega Park) | 1 | D-pad left |
| Arena 2 (Pocket Park) | 2 | D-pad right |
| Arena 3 (Stack Yard) | 3 | D-pad up |

`BindSampler` also samples these. They are not rebind rows. Comma and N are reserved so a gameplay action cannot take them.

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Mute | Comma | — |
| Music mute | N | — |
| Practice restart | T | North (Y) |
| Practice ghost | G | Left stick press |
| Practice input display | I | Right stick press |
| Frame card | F6 | — |

While a row is still on its default, the keyboard reader also accepts C for slide, Left Alt for air dash, and E for punch. Start pauses alongside Esc. F6 is not a gameplay action.

Menus: South confirms, East goes back, the left stick and the D-pad move. On the results card, Left / Right changes Rematch, Change setup, and Title.
