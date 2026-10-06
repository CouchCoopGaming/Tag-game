# Front end

Play opens on the title. A round does not start until match setup confirms.

## Title

The card reads **TAG**. Rows are Play, Practice, Settings, How to play, and Quit.

Practice opens a free arena. See `Docs/Practice.md`.

Settings is the pause settings card (look, audio, HUD, reset). How to play is the same card as pause. Quit leaves the app. Highlight moves play `UiMove`, confirm plays `UiConfirm`, and back plays `UiBack`.

Up / Down or the left stick moves the highlight. Enter, Space, or South uses the row. Esc or East goes back from a card.

## Match setup

The arena list is `ArenaRegistry`, so a later map adds its arenas without a new menu. This tree has PARK and Mega Park.

| Row | Default | Choices |
|---|---|---|
| Arena | PARK | Every registry entry |
| AI opponents | 1 | 0–3 |
| Difficulty | Normal (0.5) | Easy 0.2, Normal 0.5, Hard 0.9 |
| Round length | 120s | 60s, 120s, 180s, 300s |
| Rounds | 1 | 1–5 |

120s is the existing Least It length. The other three lengths are presets. Choices are written into the same settings blob as look and audio (`ai`, `diff`, `roundLen`, `rounds` in `tag-settings.json` and `Tag.GameSettingsJson`).

Start match opens the join screen. Humans sit down there, then the round loads Play and runs Least It with that roster, difficulty, length, and round count. See `Docs/CouchPlay.md`. Round rules are unchanged. The match total is the sum of each round’s time as It and tags. Longest survival is the longest single chase.

## Results

After the last round the card lists, per player, time as It and tags made, then longest survival, then the winner (least time as It). Ties say tie.

Rematch runs the same setup. Change setup returns to the setup card. Title tears the match down: spawned opponents are released, `StaticLifecycle.ReleaseMatch` clears the front-end session, and Boot loads. Pause has the same Quit to title.

## Proof

`Tools/StrafeJumpSim` prints one `front-end` line. It walks title, settings, how to play, and setup, checks the settings blob, plays a 3-round match through `RoundFlow`, and checks that rematch and the title return leave no session objects.
