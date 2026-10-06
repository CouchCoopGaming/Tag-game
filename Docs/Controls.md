# Controls

Default bindings. Pause → Rebind can change any row. Pause → Settings → Reset to defaults, or Rebind → Reset bindings, puts these back. Jump’s keyboard default is Space. Comma stays mute and N stays the music mute, so those two cannot be bound to a gameplay action.

Cling is not a separate button. Holding move into a wall is the cling. Rebinding it to a key is allowed; until you do, the motor still uses the move hold.

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Jump | Space | South (A) |
| Cling hold | Hold into the wall | Left stick held into the wall |
| Slide | Ctrl (C still slides on the default bind) | East (B) |
| Air dash | Q (Left Alt still dashes on the default bind) | RB |
| Punch / tag | LMB (E still punches on the default bind) | West (X) |
| Sprint | Shift | LB |
| Pause | Esc (Start also pauses) | Start |
| Minimap | M | Select |
| Arena 1 (Mega Park) | 1 | D-pad left |
| Arena 2 (Pocket Park) | 2 | D-pad right |
| Arena 3 (Stack Yard) | 3 | D-pad up |
| Mute | Comma | — |
| Music mute | N | — |

## Debug

F6 toggles the frame budget overlay. It stays off until you press it. The card shows FPS, frame time in milliseconds, and the same cost buckets as the headless budget: movement, AI, pose, HUD, audio, and round flow. F3 is Trail Tag, so the overlay does not use it. F6 is not a gameplay action and is not in the rebind list.

## Settings

Pause → Settings. The same card is on the direct-play pause menu. Values save to PlayerPrefs (`Tag.GameSettingsJson`) and to `tag-settings.json` under the persistent data path, and they load on boot.

| Row | Default | Range |
|---|---|---|
| Mouse sensitivity | 1.8 | 0.5 – 5 (steps 1.0, 1.4, 1.8, 2.4, 3.2) |
| Gamepad look | 2.2 | 0.5 – 8 (steps 1.0, 1.6, 2.2, 3.0, 4.5) |
| Invert Y | Off | On / Off |
| FOV | 78 | 60 – 100 |
| Master | 0.80 | 0 – 1 |
| SFX | 1.00 | 0 – 1 |
| Mute | Off | Comma toggles it |
| HUD scale | 1.00 | 0.75 – 1.50 |
| Colorblind palette | Off | Blue / yellow / white / cyan marks |
| Minimap | On | M or Select |
| Reset to defaults | — | Restores this table |
| Back | — | Returns to pause |

## Menus on a gamepad

South confirms. East goes back. Start pauses. The left stick and the D-pad move the highlight. Every pause row, the settings card, rebind, the arena picker, controls, look, audio, and the results card can be opened and left without the mouse.

| Menu | Move | Confirm | Back |
|---|---|---|---|
| Pause | Left / Right | Resume, Controls, Look, Audio, Quit, Settings, Rebind, Arena | East or Esc resumes |
| Settings | Up / Down | Reset, Back, or a toggle | East or Esc to pause |
| Rebind | Up / Down | Listen for a key or button, Reset, Back | East or Esc to pause |
| Arena | Up / Down, or 1 / 2 / 3 | Picks Mega Park, Pocket Park, or Stack Yard | East or Esc to pause |
| Results | Left / Right | Rematch or Menu | East or Esc leaves the card |

A jump, cling, or sprint that is held while a menu is open does not stick after the menu closes. Closing a menu does not fire a jump. The jump buffer is 0.16 s and coyote time is 0.10 s for Space and for the gamepad South button.
