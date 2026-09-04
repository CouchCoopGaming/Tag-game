# Tag Audio — Engineer Hooks v0 (Steam sprint stubs)
**Owner:** Audio · **Status:** stub pack landed · **Date:** 2026-09-04  
**Path:** `Assets/Audio/`  
**Format:** 48 kHz PCM WAV (mono SFX/UI, stereo music). Unity will import; Force To Mono OK on SFX.

Tone: plastic slapstick playground — punchy stubs, not final. Replace in place; keep filenames.

## Import
1. Open project → assets under `Assets/Audio/**` appear after refresh.
2. SFX/UI: Load Type = Decompress On Load (short one-shots) or Compressed In Memory.
3. Music: Streaming; loop checkbox ON for `music_playground_bed_loop.wav`.
4. Suggested mixer groups: `SFX`, `Music`, `UI` (volume only for MVP).

## Suggested one-shot API
Thin `AudioCuePlayer` (or existing AudioSource pool) with:
`Play(AudioClip clip, Vector3? worldPos = null, float pitchJitter = 0.05f)`

Spatial: punch / ragdoll / slide / trail at player; UI + music 2D.

## Hook map (wire these events)

| Event | Clip | Where to fire (current code) |
|-------|------|------------------------------|
| Punch hit | `SFX/sfx_punch_hit.wav` | `PunchHitbox` successful tag path (~after `OnSuccessfulPunch`) |
| Punch miss | `SFX/sfx_punch_miss.wav` | `PunchHitbox` enter `MissRecover` |
| Ragdoll start | `SFX/sfx_ragdoll.wav` | `PlayerRagdoll.TriggerRagdoll` |
| Slide start | `SFX/sfx_slide.wav` | `PlayerMotor` when slide begins (once per slide) |
| Air dodge | `SFX/sfx_air_dodge.wav` | `PlayerMotor` air-dodge impulse |
| Tag / It transfer | `SFX/sfx_tag_transfer.wav` | It handoff (`ItController` / mode `OnSuccessfulPunch`) — can layer with punch hit |
| Trail warn | `SFX/sfx_trail_warn.wav` | Near-miss / proximity to own or foreign trail (optional MVP) |
| Trail collision / elim | `SFX/sfx_trail_collision.wav` or `sfx_trail_elim.wav` (same stub) | Trail Tag contact → eliminate |
| Round start | `SFX/sfx_round_start.wav` | `ITagMode.OnRoundStart` / match go |
| Round end | `SFX/sfx_round_end.wav` | Enter post-round (generic) |
| Round win | `SFX/sfx_round_win.wav` | Local winner / team win UI |
| Round lose | `SFX/sfx_round_lose.wav` | Local loser |
| UI click / hover / confirm / back | `UI/ui_*.wav` | `ModeSelectUI` + menu buttons |
| Music bed | `Music/music_playground_bed_loop.wav` | Start on Play/Hub enter; stop/fade on quit; **loop** |

## Priority for MVP feel
1. punch hit + miss  
2. tag transfer  
3. slide + air dodge  
4. trail elim  
5. round win/lose + music bed  
6. UI click  

## Do not
- Don't invent new filenames — replace WAV bytes in place for upgrades.
- Don't block gameplay on audio load; null-safe if clip missing.

## Upgrade queue (Audio)
Plastic body layers, whoosh polish, mode-specific stingers, ducking music under punch, foot scuffs on mulch.
