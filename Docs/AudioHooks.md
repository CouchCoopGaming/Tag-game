# Audio hooks

The gameplay lane raises sounds through `Tag.Audio.AudioBus`. Each name below is one hook. The bus plays a placeholder that already lives in the project: a clip under `Assets/Resources/Audio` when Unity has imported it, otherwise a short procedural tone from `TagSfx`. No new paid assets.

Mute (`AudioMaster.Muted`) and a master volume of zero skip playback. When mute is off, `AudioListener.volume` follows the saved master volume, so the placeholders get quieter with that slider. Subscribers on `AudioBus.Raised` are told either way. Do not start a second copy of the placeholder from a subscriber.

| Hook | Enum | When it fires | Placeholder |
| --- | --- | --- | --- |
| jump | `Jump` | The motor accepts a ground or coyote jump, including a jump off a zip. | Procedural blip (`TagSfx.Jump`) |
| land soft | `LandSoft` | Feet touch down from a step that is not a hard stun. | Procedural thud (`TagSfx.Land`) |
| land hard | `LandHard` | The landing enters land-stun. | Same thud, louder |
| slide start | `SlideStart` | Slide begins. Speed is unchanged. | `sfx_slide` or the procedural whoosh |
| slide loop | `SlideLoop` | About every 0.45 s while the slide is still held. | Same slide clip, quieter |
| slide end | `SlideEnd` | Slide releases into a run, crouch, or air. | Quiet land thud |
| cling grab | `ClingGrab` | Climb or wall-run attaches. | Procedural thunk |
| wall jump | `WallJump` | Climb bounce or wall-run jump leaves the wall. | Air-dash whoosh (`TagSfx.Lunge`) |
| air dash | `AirDash` | The 0.10 s / 15 m/s dash starts. Cooldown stays 30 s. | `sfx_air_dodge` or the procedural whoosh |
| punch whiff | `PunchWhiff` | The punch active window ends without a runner. | `sfx_punch_miss` or a short blip |
| punch hit | `PunchHit` | The fist connects. A tag and a stagger both use this hit, then their own hook. | `sfx_punch_hit` |
| tag | `Tag` | It moves onto the punched runner. | `sfx_tag_transfer` |
| tag-back blocked | `TagBackBlocked` | The new It hits the pawn who just lost It, inside the 1.0 s window. | Procedural thunk |
| stagger | `Stagger` | A non-tag punch starts the 0.25 s stumble. | Procedural thunk |
| pad launch | `PadLaunch` | A launch pad's velocity set is applied. | Jump blip |
| zip grab | `ZipGrab` | Cling grabs a zip cable. | Air-dash whoosh |
| zip loop | `ZipLoop` | About every 0.45 s while the ride holds. | Same whoosh, quieter |
| zip drop | `ZipDrop` | The ride releases, ends, or is cut. | Quiet land thud |
| countdown beep | `CountdownBeep` | Once per whole second left on the 3 s countdown (3, 2, 1). | Procedural blip |
| round end | `RoundEnd` | The match reaches the results card. Win, lose, and draw pick the existing round clips. | `sfx_round_end`, `sfx_round_win`, or `sfx_round_lose` |

Round flow that should be audible around these hooks: countdown beeps, play, a tag, the It swap, round end, the results card, then the next countdown. The bus does not add a move. Ledge hang is not a hook.
