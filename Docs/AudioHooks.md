# Audio hooks

The gameplay lane raises sounds through `Tag.Audio.AudioBus`. Each name below is one hook. The clip is a short mono WAV baked by `Tools/AudioSynth` into `Assets/Audio/` and mirrored under `Assets/Resources/Audio/` so Unity can load it. The synth is deterministic: same source, same bytes. No purchased samples.

Mute (`AudioMaster.Muted`) and a master volume of zero skip playback. World cues are 3D with logarithmic rolloff out to 40 m, so another player is audible across a park court and gone past that. Countdown and round end are 2D. The It pawn's footsteps use the same clips at 1.25× so the chaser reads through the mix. A 16-voice cap steals the quietest slot when a louder cue arrives. Music stays on its own loop and does not take a voice. Subscribers on `AudioBus.Raised` should not start a second copy.

| Hook | Enum | When it fires | Sound |
| --- | --- | --- | --- |
| jump | `Jump` | The motor accepts a ground or coyote jump, including a jump off a zip. | Rising pop, 260 Hz to 740 Hz, with a tiny attack click. `sfx_jump` |
| land soft | `LandSoft` | Feet touch down from a step that is not a hard stun. | Soft low thud, body near 96 Hz plus muffled noise. `sfx_land_soft` |
| land hard | `LandHard` | The landing enters land-stun. | Heavier 52 Hz body and a sharp noise transient. `sfx_land_hard` |
| slide start | `SlideStart` | Slide begins. Speed is unchanged. | Scrape that opens from a dull band into a brighter grit. `sfx_slide_start` |
| slide loop | `SlideLoop` | About every 0.45 s while the slide is still held. | Continuous scrape with a slow loudness wobble. `sfx_slide_loop` |
| slide end | `SlideEnd` | Slide releases into a run, crouch, or air. | Short grit that dies in under a tenth of a second. `sfx_slide_end` |
| cling grab | `ClingGrab` | Climb or wall-run attaches. | Hand plant: a click plus a 170 Hz body. `sfx_cling` |
| wall jump | `WallJump` | Climb bounce or wall-run jump leaves the wall. | Low push plus a rising whoosh. `sfx_wall_jump` |
| air dash | `AirDash` | The 0.10 s / 15 m/s dash starts. Cooldown stays 30 s. | Fast whoosh, bright then darker. `sfx_air_dash` |
| punch whiff | `PunchWhiff` | The punch active window ends without a runner. | Air swish only. No impact body. `sfx_punch_whiff` |
| punch hit | `PunchHit` | The fist connects. A tag and a stagger both use this hit, then their own hook. | Transient plus a 145 Hz body. `sfx_punch_hit` |
| tag | `Tag` | It moves onto the punched runner. | Two-note claim sting, 880 Hz then 1318 Hz. `sfx_tag_transfer` |
| tag-back blocked | `TagBackBlocked` | The new It hits the pawn who just lost It, inside the 1.0 s window. | High glass shimmer, 1960 Hz and 2480 Hz with a tremolo. Not a second punch. `sfx_tagback` |
| stagger | `Stagger` | A non-tag punch starts the 0.25 s stumble. | Low 78 Hz impact and tight noise. No voice, no grunt. `sfx_stagger` |
| pad launch | `PadLaunch` | A launch pad's velocity set is applied. | Spring boing, the pitch bouncing as it falls. `sfx_pad` |
| zip grab | `ZipGrab` | Cling grabs a zip cable. | Short metal cluster around 1–2 kHz. `sfx_zip_grab` |
| zip loop | `ZipLoop` | About every 0.45 s while the ride holds. | Thin 510 Hz whine with a slow vibrato. `sfx_zip_loop` |
| zip drop | `ZipDrop` | The ride releases, ends, or is cut. | The whine falls from 520 Hz to 160 Hz. `sfx_zip_drop` |
| countdown beep | `CountdownBeep` | Once per whole second left on the 3 s countdown (3, 2, 1). 2D. | Dry 880 Hz beep. `sfx_countdown` |
| round end | `RoundEnd` | The match reaches the results card. Win, lose, and draw pick their own sting. 2D. | Draw falls 523 Hz to 196 Hz (`sfx_round_end`). Win climbs a major arp (`sfx_round_win`). Lose falls darker (`sfx_round_lose`). |

Round flow around these hooks: countdown beeps, a round-start arp (`sfx_round_start`, 2D, not its own hook), play, a tag, the It swap, a last-10s tick each whole second from 10 down to 1 (`sfx_round_tick`, 2D), round end, the results card, then the next countdown. The bus does not add a move. Ledge hang is not a hook.

## Footsteps

Ground contact plays one of four baked steps. The collider material name picks the surface, and a shipping container, catwalk, or warehouse piece (`Ship_`, `Cat_`, `Wh_`) rings even when that story is painted concrete. Mulch, grass, field, leaf, and soft play use the soft step. Steel, metal, fence, plate, lamp, army, knight, and amber use the ring. Wood, cedar, bark, and plank use the knock. Sand, concrete, rubber, launch pads, rims, and anything else use the hard step. Mega Park, Pocket Park, and Stack Yard all use those four grounds. Pocket and Stack collars are bark. Mega wood is the bench and sign dressing. Pad launch and zip grab, loop, and drop are raised by the shared motor, so they play on every arena that builds pads and zips.

| Surface | Clip | Character |
| --- | --- | --- |
| concrete | `sfx_step_concrete` | Sharp click and a short mid thump |
| grass | `sfx_step_grass` | Soft low noise, almost no click |
| metal | `sfx_step_metal` | Click plus a 1.5 kHz / 2.3 kHz ring |
| wood | `sfx_step_wood` | Hollow knock at 210 Hz and 420 Hz |

Gait changes the gap, the pitch, and the level. Walk is 0.50 s at pitch 0.92 and a quieter level. Run is 0.36 s at pitch 1.00. Sprint is 0.28 s at pitch 1.10 and louder. Each step also jitters pitch by ±0.045 and level by ±0.08. The It pawn's steps are 1.25× on top of that, and they outrank other footsteps when the voice cap is full.

## Other world cues

These are not extra hooks. They use the same voice pool.

| Cue | When | Sound |
| --- | --- | --- |
| climb scuff | About every 0.30 s while climbing | Short mid grit. `sfx_climb_scuff` |
| wall-run patter | About every 0.16 s while wall-running | Lighter, higher scrape. `sfx_wallrun` |

## UI

Menu move, confirm, and back are 2D and follow the UI slider. Master still scales them through the listener.

| Cue | Sound |
| --- | --- |
| ui move | Short 640 Hz blip. `ui_move` |
| ui confirm | Rise from 520 Hz to 780 Hz. `ui_confirm` |
| ui back | Fall from 620 Hz to 320 Hz. `ui_back` |

## Mix

Settings stores Master, SFX, UI, and Music in the same `tag-settings.json` blob as the other rows. SFX scales world and round cues. UI scales menu cues. Music scales the playground bed. Master scales the listener. Zero on a slider, or mute, silences that bus.

Voices: 16. A tag sting or a punch outranks a footstep and will take its slot. A footstep will not cut a tag. Four pawns each playing a step and a slide, plus a punch, a tag, a UI cue, and a round cue, sit at 12 voices. Further quiet cues are rejected. Peak use in the headless proof reaches the cap and does not pass it.
