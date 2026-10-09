# FX research, pass 1

For C2 on `cursor/tag-fx-kit`. Visual only. Ororo can relay the top five below as the next build list.

Nothing in this note changes feel or gameplay. Leave these locks alone: coyote 0.10, jump buffer 0.16, jump speed 24.7, terminal fall 56.16, walk 6.9, crouch 3.68, sprint 13.8, root motion off. `ChaseCam` fov pop, shake, and slow motion stay 0. The landing roll stays at 65% of terminal (threshold 36.504 m/s) and 0.52 s. No new settings row. The pause list stays 21 rows. Reduced flashing and Effects Off keep hiding bursts.

This pass did not edit scripts, so the proof lines are untouched. `HotPathAlloc` still formats `hot-path allocs before=101 after=` plus the live count. `ropeBody` does not appear anywhere in this branch.

Pass 3 is the last section. It covers how a foot, a wall, and a landing read on the surfaces Tag already has, and which emote silhouettes survive a quarter pane. Every claim there is marked verified (the page or the file was read) or second-hand. Pass 2, just before it, covers who is It and the handoff.

The stills in `Docs/FX/research-pass1/` are labeled composites, not Unity captures. Each one contains one 960×540 cell drawn at 1:1 pixels, which is one pane of a 1920×1080 couch split. The live chase camera was not measured, so the body in each cell is drawn at 108 px (one fifth of 540) on purpose. That height is a stand-in, not a captured scale.

## Top 5 for C2

Build these in order. Each one is a gap. Do not rebuild running dust, the comic words, the roll, the wall-run ring, the scuff, or the dash ghosts.

### 1. Owner-pane edge streaks

**What.** Six to eight short streaks in the outer 12% of the owning camera only. Seat tint. They start at sprint (13.8 m/s) and get a little longer as speed rises, the same gate `Pass5Look.SpeedLines` already uses. They die in about 0.12 s. That life is estimated. It is shorter than the air-streak life already in `FxKitSim` (0.15 s). They are children of that seat’s camera, so the other three panes never draw them.

**Reference.** Marvel’s Spider-Man (Insomniac). Doug Sheahan’s GDC talk says the traversal camera was built to carry speed, power, and enjoyment ([GDC Vault](https://www.gdcvault.com/play/1026422/Concrete-Jungle-Gym-Building-Traversal)). A public set of notes on that talk says field of view and follow distance both rise with speed ([jb siraudin](https://jbsiraudin.github.io/blog/spiderman-dolly/)). The notes mark “80 miles an hour” as not a real value. Do not treat any FOV curve as published. Mirror’s Edge is the other half: Tobias Dahl told Animation World Network that first-person animation should show what the eyes perceive, not a camera bolted to the head ([AWN, 2009](https://www.awn.com/vfxworld/mirrors-edge-leap-faith)).

**Why it fits Tag.** World-space speed lines already exist and default off. `GameSettings.SpeedLines` is a false bool, and `FxKitSim` says that is so four panes stay readable. A pane-local margin streak is the speed read without turning the other three views into noise, and without touching fov pop (locked at 0).

**Cost.** 0 particles. Estimated 1 draw if the streaks are one mesh on that camera. Eight separate `LineRenderer`s would be 8 draws. The kit already spends 14 line renderers on air streaks, so 1 batched mesh is the point. One owner, so the other cameras do not redraw it.

**Readability risk.** Low if the streaks stay in the margin, stay under about 8 px in the 960×540 cell, and never cross the body. High if they are parented in the world: four panes would each draw every runner’s lines. That is why the current toggle defaults off.

**Asset.** Self-made. Vertex color, no texture.

### 2. Wall-run seat ribbon

**What.** A strip of the seat color lying on the wall along the run, behind the feet. Estimated width 0.12 m, which is under the small wall-run ring the kit already keeps under 0.26 m when speed is under a sprint slam (`ImpactFx.WallRunStart`). Alpha clip, not a soft glow. It fades across about 0.40 s, estimated, inside the band between the existing scuff life (0.34 s) and land life (0.42 s). The ring and the scuff stay. The ribbon is the part that is still there after the body has left the pane. Wall-run speed stays 9.5. The camera does not tilt.

**Reference.** Jet Set Radio leaves a graffiti mark on the path you skated. Ryuta Ueda and Masayoshi Kikuchi told Polygon the cel-shaded look was a deliberate early choice, not a long research detour ([Polygon, 2012](https://www.polygon.com/gaming/2012/9/12/3319792/jet-set-radio-hd-creators-look-back-on-the-original-game/)). Splatoon’s producer Hisashi Nogami told The Verge the look follows the activity: the first prototype was blocks shooting paint, and the paint is how you read the fight ([The Verge, 2018](https://www.theverge.com/2018/3/26/17163674/nintendo-splatoon-art-design-interview-hisashi-nogami)). Titanfall’s readable wall contact is the hand on the wall and a camera tilt away from the wall ([Game Developer, 2017](https://www.gamedeveloper.com/design/designer-interview-getting-i-titanfall-i-s-controls-just-right)). Tag can take the hand-on-wall mark. It cannot take the tilt.

**Why it fits Tag.** The kit already pops a small ring and a scuff when a wall run starts. In a 960×540 pane the body is small, and the ring dies at the start. A seat-colored strip is how the other three players see which wall just got used.

**Cost.** 0 particles. 1 mesh per runner, about 8 to 16 quads, estimated. 1 draw per camera that can see that runner. Alpha clip keeps it out of the soft transparent stack. Four cameras that all see one ribbon is 4 thin draws, not 4 full-screen passes.

**Readability risk.** Medium if two runners share a wall and both ribbons stay bright. Cap one live ribbon per pawn, the way scuffs are already capped at 4. A dark outline is what makes orange read on brick. A hairline will vanish. The still uses a 10 px stroke inside the 960×540 cell.

**Asset.** Self-made strip mesh. Do not stack Kenney smoke on it. If a soft edge is needed later, one sprite from Kenney’s Particle Pack is CC0 ([OpenGameArt](https://opengameart.org/content/particle-pack-80-sprites), [kenney.nl](https://kenney.nl/assets/particle-pack)). Use it as a clip mask, not as a puff.

### 3. Contact ink card

**What.** One camera-facing card at a hit: a flat black shape the size of the two bodies, with a light rim. It is not a new word and not a new starburst. The existing burst and the comic word stay. Life is about three to five frames. Sakurai’s Famitsu column says Smash spends four frames blending the flinch into the hurt pose during hitstop ([Source Gaming translation](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/)). He does not give the frame rate in that column. At 60 fps, four frames is 0.067 s. That conversion assumes 60 fps and is estimated. The card uses that beat and does not freeze anyone. Whiffs do not get the card. Reduced flashing hides it, same as the words.

**Reference.** Smash, for the short contact drawing and for the free-for-all warning below. Rivals of Aether has `AG_WINDOW_HITPAUSE_FRAME`, a chosen animation frame shown during hitpause ([official attack grid](https://rivalsofaether.com/attack-grid-indexes/)). Sifu’s animation director Kevin Roger treats the impact itself as a posed frame: anticipation makes it snappy ([Point’n Think](https://www.pointnthink.fr/en/interview-kevin-roger-sifu/)). Sloclap’s making-of also adds camera shake on finishers ([PlayStation](https://www.youtube.com/watch?v=6gskZdJ8rF8)). Tag does not take the shake. Hi-Fi Rush puts key poses on the beat at 120 BPM and 60 fps ([Game Anim, citing CEDEC 2023](https://www.gameanim.com/2023/09/08/hi-fi-rush-music-synced-animation/)). The comic panel is the flavor. The card is one held shape, not a camera cut.

**Why it fits Tag.** POP, POW, BAM, and WHAM already fire, with their own words for whiffs, hard lands, and wall slams. Two bursts in one 960×540 pane cover the bodies. A body-sized ink shape still reads when the word is toggled off or when two words overlap. It does not add a 37th word. `ComicWords.WordCount` is 36.

**Cost.** 1 quad. 0 particles. Estimated 1 draw per camera that sees the contact. It dies before the word’s 0.45 s life, so it does not stack under the letters for long.

**Readability risk.** The risk is a flash, not clutter. No full-pane white. No strobe. Reduced flashing already hides comic bursts. Use that switch. If the card is smaller than the bodies it becomes another speck next to the starburst.

**Asset.** Self-made. A flat rounded blob is enough for the first build. A stamp of the mannequin meshes in an unlit black material is better and uses meshes the project already has.

### 4. Roll skid decal

**What.** One dark streak on the ground along travel, only while the existing shoulder roll plays (0.52 s, only when downward speed is at least 65% of 56.16 and the landing is not the stationary absorb). No extra dust. The roll pool is already 16 motes (`FxKitLook.DustRoll`). A 2 px seat-colored edge, estimated in screen space, keeps the streak visible on gray concrete and on dirt.

**Reference.** Celeste kicks a little dust on landing and leaves a short trail on the dash. The four-frame dash pause, the dust, the shadow trail, and the white contrail are reported by a video that read the game and interviewed Matt Thorson ([GMTK](https://www.youtube.com/watch?v=yorTG9at90g)). That four-frame figure is the video’s code read, not a design-doc number. The pause and the shake are feel. Tag keeps the directional mark only. Titanfall’s landing “spring” was a camera bop on unchanged movement code ([same 2017 interview](https://www.gamedeveloper.com/design/designer-interview-getting-i-titanfall-i-s-controls-just-right), Rayme Vinson). Tag’s camera does not take that bop. The decal is the visual that says how hard and which way, without moving the view.

**Why it fits Tag.** The roll already reads up close. In a quarter pane the body is a small tumble. A streak on the ground points the travel after the mesh has stood up. Speed is unchanged. The camera does not roll.

**Cost.** 1 decal quad. 0 new particles. Estimated 1 draw per camera that sees the ground under that runner.

**Readability risk.** Low if the streak is dark with a seat-colored edge. A white flash on concrete blows out the pane and fights the hard-land ring that is already there. The stationary absorb does not draw it.

**Asset.** Self-made smear, painted in the project. Kenney’s CC0 pack is a fallback scratch only.

### 5. Whiff fist smear

**What.** Three stretched quads on the fist for a punch that misses. Seat color. About 0.10 s, estimated, the same length as the air-dash window so the smear ends when that kind of motion ends. It does not write the dash timer. No starburst. The whiff words (WHIFF, SWISH, WHOOSH, MISS) stay on the comic toggle. A hit does not also draw the smear. The hit keeps the ink card and the word.

**Reference.** Celeste’s dash trail, from the same GMTK read: shadows plus a white contrail, short enough that the move stays clear. Rocket League’s stylized boost work treated the trail as a shape that has to read, not as a soft cloud (Preston Schulz, Psyonix, [ArtStation](https://prestonthings.artstation.com/projects/Le5W5l)). The boost mechanic itself is gameplay ([Dave Hagewood, Game Developer](https://www.gamedeveloper.com/design/game-design-deep-dive-rocket-jumping-in-i-rocket-league-i-)). Tag does not take the mechanic.

**Why it fits Tag.** A miss and a hit currently differ by which word rolls. With comic words off, a miss can look like a punch that simply ended. Three streaks on the fist say “it went through” without another explosion next to someone else’s POW.

**Cost.** 3 billboards. Estimated 3 draws unless they share one mesh. 0 ring, 0 debris.

**Readability risk.** Low on a miss. High if it also plays on a hit: the pane then has a word, a starburst, an ink card, and a smear. Gate it on the whiff event only.

**Asset.** Self-made streak. One CC0 Kenney trace sprite is fine if the hand-painted one is not ready.

## Already on this branch

C2 should not spend the next pass redrawing these. Counts below are the pool caps in code, not a frame capture.

| Effect | Where | Cap |
| --- | --- | --- |
| Running dust, speed and surface | `DustLook` | Walk 6.9 is the faint end. Sprint 13.8 is the loud end. Dirt sprint mote size 0.34 m, span 0.90 m. Metal is sparks only. |
| Comic words and starbursts | `ComicWords` | 36 words. Hits, whiffs, lands, and wall slams each have a pool. Grow, hold, shrink. Life 0.45 s. Pop 0.05 s. Toggle, plus Reduced flashing. |
| Landing roll | `LandingRollPose` | 65% of 56.16, 0.52 s. Stationary absorb is the crouch, not the roll. |
| Land dust, sparks, debris, foot motes | `FxKitLook` | 16 / 8 / 8 / 4 per pawn. Stars 5. Streaks 6. Scuffs 4. |
| Air streaks | `FxKitSim` | 14 line renderers, life 0.15 s, only if Speed lines is on. Default is off. |
| Sprint speed lines, wall scrape, tag bits, trails | `Pass5Look` | 4 lines at sprint, up to 8. Metal scrape 6. Tag bits 8 (4 on Low). Trail 12 points. |
| Wall-run start | `ImpactFx` | Small ring, held under 0.26 m below sprint-slam speed, plus a scuff. |
| Dash ghosts | verb FX | Mesh copies in the seat color. Not cards. |

Four pawns times the kit quad pools (16+8+8+4+5) is 164 quad slots allocated. That is a ceiling. It is not how many are alive in a frame, and it was not timed on a GPU.

## What the references do, and what Tag leaves behind

### Speed lines and field of view

Spider-Man sells speed with the camera: Sheahan’s talk is about that camera, and the notes say fov and follow distance rise together. Mirror’s Edge animates the perceived view. A public slide reproduction of the DICE GDC 2009 deck includes a slide titled “FOV Deformation” ([SlidePlayer](https://slideplayer.com/slide/4430297/), [GDC Vault](https://www.gdcvault.com/play/978/Creating-First-Person-Movement-for)). This pass did not watch the vault video, so the slide title is from that reproduction, not a transcript.

Tag’s chase camera is third person and split four ways. A fov kick would move all four horizons and is locked at 0. The transferable piece is a read that lives in the owner’s pane (idea 1).

Titanfall 2’s texture-streaming deck states the project target in one line: “60Hz! Avoid new GPU passes or non-threadable CPU” ([Chad Barb, GDC 2017, slide PDF](https://ubm-twvideo01.s3.amazonaws.com/o1/vault/gdc2017/Presentations/Barb_Chad_EfficientTextureStreaming.pdf)). That talk is about textures, not dust. The useful part for Tag is the refusal of an extra full-screen pass. Four cameras would pay that pass four times.

### Dust and impacts

Tag’s dust is already speed-scaled and surface-tinted. The references do not publish particle counts this pass could copy. Celeste’s landing dust is described as small. Titanfall’s published movement interview talks about animation, audio, and a camera spring, not a mote budget. Do not add a second dust system. Idea 4 is a decal because the mote pool is already the dust.

### Hit-stop and impact frames

Sakurai, in the Famitsu column translated by Source Gaming:

- Both fighters freeze for the same amount of time. Longer hits get longer freezes. A cap exists. The column does not publish the cap.
- He keeps the freeze shorter than he would like because a free-for-all gives a third player a window while two people are frozen. Tag is that free-for-all, with four people.
- The mesh can vibrate while the hurtboxes stay still. Grounded vibration is horizontal so the feet do not dig in. Amplitude grows when the camera is far, or the shake is invisible.
- The painful pose is blended in over four frames so a 3D mesh does not pop from one pose to another.

Rivals publishes the hitpause formula used by `get_hitstop_formula`: `(hbox_hitstop + (damage + hbox_damage) * hbox_hitstop_scale * .05) + hbox_extra_hitstop` ([workshop manual](https://rivalswsmanual.miraheze.org/wiki/Get_hitstop_formula)). Hitpause is the freeze. Hitstun is the later lockout. `AG_WINDOW_HITPAUSE_FRAME` is the picture shown during the freeze. Extra hitpause can apply to the target only.

Sakurai later recorded “Eight Hit Stop Techniques” for Ultimate ([YouTube](https://www.youtube.com/watch?v=tycbMSjDDLg)). This pass did not watch it, so it is a pointer, not a summary.

Tag does not freeze time, does not vibrate the capsule, and does not add hitstun. Idea 3 takes the picture and the four-frame beat. The distance lesson (make the contact big enough to read when the camera is far) is why the card is body-sized in a 540 px pane. A later Sakurai video is not a license to add freeze frames.

Sifu aims at a stable 60 fps on a base PlayStation 4 ([PlayStation Blog](https://blog.playstation.com/2021/11/18/how-sifus-kung-fu-combat-works/)). The interviews describe a keyed contact and, on finishers, camera shake. They do not publish a white-flash frame count. This note does not claim one.

### Comic and stylized hits

Hi-Fi Rush’s GDC 2024 talk is a deferred toon renderer, 60 fps at native resolution, and the writeup lists a comic shader among the topics ([80.lv](https://80.lv/articles/the-making-of-hi-fi-rush-s-3d-toon-rendering-style), [YouTube](https://www.youtube.com/watch?v=gdBACyIOCtc), [CEDiL session](https://cedil.cesa.or.jp/cedil_sessions/view/2823)). The onomatopoeia is part of the shipped game. This pass did not find a Tango particle budget for those words. Tag already has the words, the Ben-Day bursts, and Bangers (OFL) in `Assets/Art/FX/Fonts/`. Do not add a font. Do not cut the camera into a comic panel. That cut is a framing change.

Jet Set Radio’s cel outline is a style choice from the Polygon interview. Tag’s mannequin is already a light body on a gray box. A new outline shader on every runner, drawn by four cameras, is a lighting change, not an FX gap. The graffiti mark is the piece that fits (idea 2).

### Landing and roll

The roll is in. The next visual is the skid (idea 4). Do not add a camera roll, a landing fov dip, or a white impact flash. Vinson’s point on Titanfall was that the landing felt more real when the view sprang and the movement code did not change. Tag has already chosen the other half of that sentence: the view stays still.

### Wall-run trails

Titanfall tells you that you are on the wall by tilting the view, including a tilt that starts before the run so you can trust the wall. The same interview gives the chained acrobatic speed as up to 30 mph, “well over double a pilot’s sprint speed.” That 30 mph is their traversal, not a particle size, and it is not a target for Tag’s 13.8 m/s sprint. The hand “caressing the wall” is Mark Grigsby’s animation note in that interview. Tag already has a hand scrape. The missing read is the ribbon that stays on the wall (idea 2).

Splatoon’s ink is territory you can still read after the shot. Nogami’s interview is about art serving that activity. A full ink simulation is parked below. The ribbon is the cheap version: one seat-colored strip, gone in under half a second, not a paint map.

### Readability in a 960×540 pane

A 1920×1080 frame split in four is 960×540 per pane. This pass did not measure the live viewport insets, so treat 960×540 as the cell the task asked to judge, not as a captured camera rect.

What reads at that size:

- A shape as tall as the body. The stills use 108 px as the stand-in body.
- A stroke of several pixels with a dark edge. The wall ribbon still uses 10 px. A 1 px line is noise.
- Seat color against the surface. Orange on brick needs the dark edge. Gray dust on concrete needs the size change the dust table already has, not another gray puff.
- One event per contact. Word, or ink card, or whiff smear. Not all three on the same fist.
- Type that already fills its burst. The words are on screen for 0.45 s. Do not add a second line of text under them.

What is noise:

- World-space streaks in every pane. That is the current reason Speed lines defaults off.
- Soft puffs stacked on the land ring, the roll dust, and a new cloud.
- A full-pane flash. Four of those at once and nobody can see a tag.
- A trail that does not fade. The arenas are small. Four permanent graffiti paths become the level.

## Four cameras, particles, overdraw, instancing

Tag is Unity 6000.3.24f1, URP 17.3 (`Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`). This pass did not open the Editor and did not profile a GPU. There is no frame-time number below. Anything called a cost on the five ideas is an estimated draw count from the shape of the current pools.

Unity’s particle GPU instancing does not apply to the default billboard path. The 6000.3 manual page for the built-in pipeline says instancing is for mesh particles, not billboards, and the particle data goes into one buffer ([manual](https://docs.unity3d.com/6000.3/Documentation/Manual/PartSysInstancing.html)). The Unity 6 renderer-module page says “Enable Mesh GPU Instancing” exists only in Mesh render mode ([manual](https://docs.unity.com/en-us/engine/6000.7/manual/visual-effects/particle-systems/particle-system-modules/part-sys-renderer-module)). That page is the 6000.7 manual, not a checkbox this pass clicked in 6000.3.24f1. The project’s motes are quads. Instancing them would mean switching those draws to mesh particles and a shader that supports it. That is not free art. It is a renderer change, and it is not one of the five.

Overdraw is the cost that matters. Unity’s graphics-optimization page names overlapping transparent UI, particles, and sprites as the usual fill-rate problem, and points at the Overdraw draw mode ([manual](https://docs.unity3d.com/Manual/OptimizingGraphicsPerformance.html)). Each camera renders what it sees. A world-space transparent puff inside two frusta is shaded twice. Inside all four, four times. A camera-child streak (idea 1) is shaded once.

Worked example, estimated, so the size of the problem is visible. One pane is 960×540 = 518,400 pixels. A soft disc 80 px across is about π×40² ≈ 5,000 pixels. Sixteen of those, the roll pool, stacked on the same spot, is about 80,000 shaded pixels in one pane, roughly 15% of the pane, before a second camera sees the same landing. The 80 px size is assumed. `DustLook` stores metres (dirt sprint mote 0.34 m). This pass did not measure metres-to-pixels in the chase camera, so this paragraph is not a captured overdraw figure. It is why idea 4 is a decal and why the five ideas add no dust.

A full-screen pass, the kind Barb’s deck says to avoid, is 518,400 pixels per pane. Four panes is about 2.1 million pixels before the scene. An ink metaball pass would be that, every frame, in URP, on top of the graybox. Parked.

Practical budget for the next pass, estimated from the caps already in the kit:

- Stay inside the pools. Do not raise `DustRoll`, `SparkFull`, or `DebrisFull`.
- Prefer one alpha-clipped mesh over a new transparent stack.
- Parent speed reads to the owner camera.
- Effects Off and Reduced flashing hide the new draws the same way they hide bursts.
- Low density keeps using `FxAmount.LowDensity`. If a proposal has no particles, Low does not need a second art path.

No mid-range GPU was named or timed. “Mid-range can handle N particles” is not a figure this note will invent.

## Parked

These were studied. They are not in the five.

**Hitstop and hitpause.** Sakurai’s own free-for-all argument is Tag’s four-player argument. Rivals’ formula is a freeze. Shipping it would change when the next jump or tag can happen. Out.

**Field of view, follow distance, camera tilt, landing spring, camera shake.** Spider-Man, Titanfall, and Sifu’s finishers use them. `ChaseCam` fov pop, shake, and slow motion are 0, and the roll does not roll the camera. Out.

**Splatoon metaball ink and a paint map.** The Verge interview supports “the mark is the read.” A screen-space fluid and a persistent paint texture are a custom URP pass times four cameras. The ribbon is the version that fits the budget. A recreation such as Mix and Jam’s is not a Nintendo figure and is not a plan.

**Hi-Fi panel cuts.** A camera cut on a punch changes what the other three players see in that seat’s pane. It is framing, not an overlay. Out.

**Damage vibration.** Sakurai scales it with camera distance and keeps hurtboxes still. At 960×540 a small vibrate is invisible, and a large one reads as the shake that is locked off. Out.

**A new comic font or more words.** Bangers is already OFL. The pools already split hits, whiffs, lands, and wall slams. Out.

## Stills

Composites, 1600×900, each with one 960×540 cell at 1:1. Body height 108 px is the stand-in described above. Regenerated by `Docs/FX/research-pass1/make_stills.py`.

| Rank | File | What the cell is judging |
| --- | --- | --- |
| 1 | `Docs/FX/research-pass1/01-edge-streaks.png` | Margin streaks in the owner pane. The center of the pane stays clear of lines. |
| 2 | `Docs/FX/research-pass1/02-wall-ribbon.png` | A 10 px seat ribbon on brick, with the existing small ring left in so the ribbon reads as the addition. |
| 3 | `Docs/FX/research-pass1/03-contact-ink.png` | A body-sized ink card at the fists. The small POW burst is the effect that already ships. |

## Limits

- No Unity play-mode capture, no Blender scene, no GPU profile, no overdraw screenshot.
- GDC vault videos were not watched. Slide titles and talk summaries are cited from the vault page, the slide host, or a writeup, and the notes say which.
- The Sakurai Ultimate video is linked and not summarized.
- The Spider-Man fov behavior is from a viewer’s notes of Sheahan’s talk, not from a published curve. The notes’ “80 miles an hour” is explicitly not a real value.
- Celeste’s four-frame dash pause is from a video’s code read.
- Sifu interviews do not publish an impact-frame count. None is claimed.
- Pixel sizes in the stills are drawn sizes. World sizes in the tables are from code. Metres were not converted to pixels.
- Kenney CC0 is a fallback. The five ideas are specified as self-made so C2 does not need to download a pack to start.
- No purchases. No new font. OFL Bangers stays the only type.

## Sources

- Tobias Dahl, Animation World Network, 2009. [Mirror’s Edge: A Leap of Faith](https://www.awn.com/vfxworld/mirrors-edge-leap-faith).
- Jonas Åberg and Tobias Dahl, GDC 2009. [Creating First Person Movement for Mirror’s Edge](https://www.gdcvault.com/play/978/Creating-First-Person-Movement-for). Slide reproduction: [SlidePlayer](https://slideplayer.com/slide/4430297/).
- Alex Wiltshire, Game Developer, 9 March 2017. [Getting Titanfall’s controls just right](https://www.gamedeveloper.com/design/designer-interview-getting-i-titanfall-i-s-controls-just-right).
- Chad Barb, GDC 2017. [Efficient Texture Streaming in Titanfall 2, slide PDF](https://ubm-twvideo01.s3.amazonaws.com/o1/vault/gdc2017/Presentations/Barb_Chad_EfficientTextureStreaming.pdf).
- Doug Sheahan, GDC. [Concrete Jungle Gym: Building Traversal in Marvel’s Spider-Man](https://www.gdcvault.com/play/1026422/Concrete-Jungle-Gym-Building-Traversal).
- jb siraudin. [Notes on the Spider-Man traversal camera](https://jbsiraudin.github.io/blog/spiderman-dolly/). Third-party notes. The “80 miles an hour” line is marked there as not a real value.
- GMTK. [Why Does Celeste Feel So Good to Play?](https://www.youtube.com/watch?v=yorTG9at90g). Includes an interview with Matt Thorson. Frame counts in this note that come from the video are the video’s code read.
- Masahiro Sakurai, Famitsu, translation by Source Gaming, 11 November 2015. [Thinking About Hitstop](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/).
- Masahiro Sakurai. [Eight Hit Stop Techniques](https://www.youtube.com/watch?v=tycbMSjDDLg). Linked only. Not watched for this pass.
- Rivals of Aether. [get_hitstop_formula](https://rivalswsmanual.miraheze.org/wiki/Get_hitstop_formula). [Attack grid indexes](https://rivalsofaether.com/attack-grid-indexes/).
- Kevin Roger, Point’n Think. [Interview, animation director on Sifu](https://www.pointnthink.fr/en/interview-kevin-roger-sifu/).
- Sloclap, PlayStation Blog, 18 November 2021. [How Sifu’s kung fu combat works](https://blog.playstation.com/2021/11/18/how-sifus-kung-fu-combat-works/).
- Sloclap. [Sifu making-of](https://www.youtube.com/watch?v=6gskZdJ8rF8).
- Game Anim, 8 September 2023. [Hi-Fi Rush music-synced animation](https://www.gameanim.com/2023/09/08/hi-fi-rush-music-synced-animation/). Cites the CEDEC 2023 presentation by John Johanas and Masaaki Yamada for 120 BPM at 60 fps.
- 80.lv, 10 July 2024. [Hi-Fi Rush toon rendering](https://80.lv/articles/the-making-of-hi-fi-rush-s-3d-toon-rendering-style). Writeup of the GDC 2024 talk. [YouTube](https://www.youtube.com/watch?v=gdBACyIOCtc). [CEDiL](https://cedil.cesa.or.jp/cedil_sessions/view/2823).
- Hisashi Nogami, The Verge, 26 March 2018. [Splatoon art interview](https://www.theverge.com/2018/3/26/17163674/nintendo-splatoon-art-design-interview-hisashi-nogami).
- Ryuta Ueda and Masayoshi Kikuchi, Polygon, 12 September 2012. [Jet Set Radio HD look-back](https://www.polygon.com/gaming/2012/9/12/3319792/jet-set-radio-hd-creators-look-back-on-the-original-game/).
- Preston Schulz. [Rocket League 2D smoke boost](https://prestonthings.artstation.com/projects/Le5W5l).
- Dave Hagewood, Game Developer. [Rocket jumping in Rocket League](https://www.gamedeveloper.com/design/game-design-deep-dive-rocket-jumping-in-i-rocket-league-i-). Mechanic, not a trail budget.
- Unity 6000.3. [Particle System GPU instancing, built-in pipeline](https://docs.unity3d.com/6000.3/Documentation/Manual/PartSysInstancing.html).
- Unity 6. [Particle System renderer module](https://docs.unity.com/en-us/engine/6000.7/manual/visual-effects/particle-systems/particle-system-modules/part-sys-renderer-module). Mesh instancing only. Page version is 6000.7.
- Unity. [Reduce rendering work on the CPU or GPU](https://docs.unity3d.com/Manual/OptimizingGraphicsPerformance.html).
- Kenney, Particle Pack, CC0. [OpenGameArt](https://opengameart.org/content/particle-pack-80-sprites). [kenney.nl](https://kenney.nl/assets/particle-pack).

## Pass 2 — Who is It, and the handoff

Visual only. C2 is already prototyping the wall-run ribbon, then the edge streaks and the contact ink card. This section is the next read after those: which body holds It, in your pane and in the other three, and how the handoff reads without hitstop or slow motion. Feel locks stay as in pass 1. No new settings row.

The stills are composites in `Docs/FX/research-pass2/`. Each is 1600×900 with one 960×540 cell at 1:1. The body is again drawn at 108 px. That height is a stand-in. The live chase camera was not measured.

### What Tag already draws

**Verified, from this branch.**

- `ItMarker` puts a hat, a brim, a tip, a tall beacon, a floor halo, and a point light on the It. The beacon is turned off when the camera is a child of that body, so it does not fill your own lens. In that own view the hat scale is multiplied by 0.82.
- The same component draws a screen badge (a filled square, the star glyph, and the It label) from `OnGUI`. It uses `Camera.main` only. If that camera is a child of the It, the badge returns and draws nothing. It is not issued once per split camera.
- `VerbStatusHud` draws an It chip inside that seat’s camera rectangle, and only when that seat’s own body `IsIt`. Another pane does not get a copy of your chip.
- `AccessibilityPalette.ItGlyph` is `★`. `ItAgainst` picks a crown color that stays off the seat color, and off the zone colors it tests. The shirt and the crown are already meant to be two different reads.
- Becoming It or losing it plays the pass-5 swell for 0.40 s: seat color when gained, cool white when lost, plus a ring (`Pass5Host`). Reduced flashing and Effects Off hide it.
- The kit also draws edge bars for 0.18 s on the camera of the pawn whose `TagsLanded` just increased (`FxKitSim.TickFlash`). Those bars are about 8% of the pane width on the sides and about 11% of the height on the top and bottom (0.16 of half the width, 0.22 of half the height). They mark the tag landing. They are not a label that says who holds It now.
- Tag-back immunity stays 1.0 s. The kit can draw a rim on that body. The rim is the safe window, not the role.
- The hat pop eases out over 0.34 s (`ItMarker` moves `_pop` toward 0 by `deltaTime / 0.34`).

### How the other games show a role

**Mario Kart 8 Deluxe.** Verified, Nintendo’s battle page, read for this pass: in Coin Runners, “The player in first place is shown by a crown.” Shine Thief is a different sentence on that page: grab the Shine and hold it until time runs out. That page does not say the holder also wears a crown. Verified, Mario Wiki crown article, read for this pass: in Battle Mode the crown sits on the head of the first-place driver, and a short sound plays when someone gets it. In Mario Kart 8 the crown was a minimap icon. The wiki is a community encyclopedia, not a Nintendo manual. The useful part for Tag is the one this pass could check in both places: the role is a shape on the body, in the world, so any camera that can see that driver can see the crown. Tag already has that hat. The gap is the camera that cannot see the body.

**Smash Ultimate.** Verified, SmashWiki radar article, read for this pass: the radar stays hidden until a fighter is far enough off-screen that the camera stops following them. It then appears in the upper corner of the side they left. Every fighter is a diamond in their port color (team color in team battles). The size can be Small or Large, and it can be turned off. The article says this is so players can find each other, and so someone cannot hide off-screen. SmashWiki is a community wiki. This pass did not read a Nintendo page for the radar. Second-hand, and not used below: a search snippet of the versus-splash article lists eight port colors. That page was not opened, so those colors are not cited. Smash has no It role. The lesson is the off-screen mark, and the choice to show it only when the body has left. Showing every player all the time is the clutter the article says they avoided.

**Fall Guys, Tail Tag.** Verified, IGN’s Tail Tag guide, read for this pass: you grab a gold tail off another player, and you qualify by still having a tail when time runs out. The team version hoards tails. The guide is a community writeup, not a Mediatonic post. It does not publish a particle count or a nametag spec. The role is a separate object on the body. Grabbing it moves the object. This pass did not open a page that describes Fall Guys nametags, so none are claimed.

**Gang Beasts.** Verified, Steam store page, read for this pass: you customise the character, and the feature list includes Shared/Split Screen. The page does not describe an It role, a crown, or a split-screen layout. Second-hand: a Steam discussion and a player video describe changing body color, and the video says local play is one shared camera. Those pages were not opened. Steam’s feature list also says Shared/Split Screen, so this pass does not decide how many cameras Gang Beasts uses. The store sentence that does carry is the customised body. That is identity, not a role. Tag already spends seat color and a shape glyph on identity. Painting the whole runner as “the It color” would collide with that.

**Ultimate Chicken Horse.** Verified, Steam store page, read for this pass: local play for up to four, and you play as a chicken, a horse, a sheep, a raccoon, “and other wonderful animals.” The feature list includes Shared/Split Screen. The page does not describe an It role. Identity is which animal you are. A search excerpt of a Steam news post listed seven outfit colors for the horse and said the outfit does not change speed. That post was not opened. Second-hand, and not a build note.

### Top 3 for C2

These sit on top of the hat, the HUD chip, and the 0.40 s swell. They do not replace them. They do not change who is It, the 1.0 s immunity, or the camera.

#### 1. One off-screen wedge, per pane

**What.** When the It body is outside a pane’s frustum, that pane draws one star wedge on the edge it left through. One wedge, not one per player. It points at the It and uses the crown color from `ItAgainst`, with the seat glyph small beside it so you know which runner. If you are It, your own pane keeps the HUD chip and does not also draw a wedge at your own feet. The wedge hides when the hat is inside the pane.

**Reference.** Smash’s radar. Verified from the SmashWiki page above: it appears when someone has left the camera, it uses port color, and it can be turned off. Tag takes the “only when off-screen” rule and drops the “every fighter” part. Four diamonds in a 960×540 pane is the clutter that page says the radar was built to avoid, and Tag only needs the one role.

**Why it fits.** The world hat already answers “who is It” when the body is in frame. `ItMarker.OnGUI` does not answer it per split camera, and it skips the It’s own camera. A chase pane spends a lot of time with the other three runners outside the frame. The wedge is that read.

**Cost.** 0 particles. One screen quad and two short labels per pane, and only while the It is off-screen. Estimated 1 draw. It is UI on that camera, so the other cameras do not redraw it.

**Readability risk.** Low if there is one wedge and it is at least the 36 px the existing badge uses (`mark = 36` in `ItMarker`). High if every runner gets an arrow. High if it pulses. Reduced flashing should hold it steady, the same way it already zeros the hat bob.

**Asset.** Self-made. The star glyph is already in the project.

#### 2. A flat crown plate, separate from the shirt

**What.** A camera-facing plate above the hat: dark backing, crown-color star, about the width of the head. The tall beacon stays hidden in your own view, as it is now. The plate stays on in every view, including your own, at the own-view scale (0.82). It does not add a light. The floor halo stays. The shirt keeps the seat color.

**Reference.** Mario Kart’s battle crown, verified from Nintendo (first place is shown by a crown) and from the Mario Wiki sentence that the crown is on the head. Fall Guys’ gold tail, verified from the IGN guide: a separate object, not a recolor of the body. Gang Beasts and Ultimate Chicken Horse, verified from their Steam pages, put identity on the body itself. That is the seat, which Tag already has. The crown has to be a second shape.

**Why it fits.** A thin beacon reads when the camera is far and disappears when it is your own camera. In a 960×540 pane the body is about a fifth of the height in these stills. A plate the width of the head is a silhouette. `ItAgainst` already exists so the plate does not match the shirt. Do not recolor the runner.

**Cost.** 1 billboard quad on the It. 0 particles. Estimated 1 draw per camera that can see that body. No new light. The existing point light stays as it is. This pass did not profile it.

**Readability risk.** Medium if the plate uses the shirt color. Medium if it bobs enough to leave the head. Reduced flashing already stops the bob. Keep the plate still in that mode.

**Asset.** Self-made quad. No download.

#### 3. The handoff is the crown changing heads

**What.** No freeze and no slow motion. The pawn who loses It keeps the cool-white swell that already plays for 0.40 s, then has no hat and no plate. The pawn who gains It keeps the seat-colored swell and the 0.34 s hat pop, and the plate is there after the pop ends. Your pane’s existing It chip turns on when `IsIt` becomes true and turns off when it becomes false. That chip is the confirmation if you blinked the swell. Do not add a full-pane white, and do not add a second flash on the receiver. The tagger’s 0.18 s edge bars already mark the landing on the tagger’s camera. Other panes learn the new It from the plate, or from the wedge if the body is off-screen. The wedge moves to the new body’s edge. It does not play a second animation.

**Reference.** The Mario Wiki sentence, verified above: a short sound when the crown is obtained, and the crown is then on the new leader. The durable part is the crown sitting on the new head, not a freeze. Fall Guys, verified from the IGN guide: the tail leaves one body and is on the other. Smash hitstop, from pass 1, is the thing Tag does not copy. Sakurai’s free-for-all warning is unchanged.

**Why it fits.** Both players need a different picture. The code already splits gained and lost by color. What a 960×540 pane still needs is the plate remaining after 0.40 s, and the chip in the receiver’s own HUD, which `VerbStatusHud` already draws. The missing picture is the plate and the wedge agreeing about who has it once the swell is gone.

**Cost.** 0 new particles. The swell, the ring, the edge bars, and the chip stay. The plate in idea 2 is the persistent part. Estimated cost is that one quad, not a new system.

**Readability risk.** High if the receiver also gets the tagger’s edge bars. Those bars fill the frame and, on the tagger, mean “you landed the tag,” which is the opposite of “you are It.” Leave them on the tag landing only. High if both bodies flash the same color. The gained and lost colors already differ. Keep that split.

**Asset.** Self-made. The chip uses the glyph and the label the HUD already has.

### What this pass is not asking for

- A radar of all four runners. Smash shows everyone because anyone can be off-stage. Tag’s question is which one body is It.
- A recolor of the whole mannequin. Seat color is already the player.
- Hitstop, slow motion, fov pop, or shake.
- Another point light, or a full-pane flash on the receiver.
- A new word. The chip already says the It label. Comic words stay on the punch.

### Stills

| Rank | File | Cell |
| --- | --- | --- |
| 1 | `Docs/FX/research-pass2/01-offscreen-wedge.png` | You are not It. The It is off the right edge. One wedge. |
| 2 | `Docs/FX/research-pass2/02-crown-plate.png` | Someone else’s pane. The star plate is the role. The shirt is the seat. |
| 3 | `Docs/FX/research-pass2/03-handoff-receiver.png` | Your pane, the moment you become It. Plate and chip. No full-pane flash. |

### Pass 2 limits

- No gameplay scripts were edited.
- Nintendo, Mario Wiki, SmashWiki, the IGN Tail Tag guide, and the two Steam pages were opened and read. Claims from those pages are marked verified. Community wikis are not publisher manuals.
- The Gang Beasts color-button discussion, the player video about a shared camera, the Smash versus-splash port list, and the Ultimate Chicken Horse outfit-color news post were not opened. They are second-hand and are not build instructions.
- No Fall Guys nametag claim. No Shine-holder crown claim beyond the two sentences above.
- The 108 px body is a drawn stand-in. Edge-bar percentages are from the constants in `TickFlash`, not from a captured frame.
- Draw counts are estimated. No GPU profile.

## Pass 3 — Surface contact, and emote silhouettes

Visual only. C2 already has the running-dust table and `surfaces=6`. This section is how that table reads in one 960×540 pane, and a separate note for the motion-reference worker on celebrations. Feel locks stay as in pass 1. No new settings row. No gameplay scripts.

The stills are composites in `Docs/FX/research-pass3/`. Each is 1600×900 with one 960×540 cell at 1:1. The body is again drawn at 108 px. That height is a stand-in. The live chase camera was not measured.

### What Tag already draws

**Verified, from this branch.**

- `DustLook.SurfaceCount` is 6: grass, dirt, concrete, wood, metal, wet. Brick is kind 6, an extra tint. It uses the concrete size curve and a red tint `(0.62, 0.28, 0.16)`. The comment in the enum says the count stays 6 so the running-dust proof line does not change. `Holds()` returns false if the count is not 6.
- Sprint proof numbers `Holds()` checks: grass size 0.13, opacity 0.50, count 6. Dirt 0.34, 0.88, 11. Concrete 0.20, 0.82, 9, span 0.72. Wood 0.10, 0.78, 8. Metal count 0 and `Spark` 1. A metal pivot is the only metal foot burst with a count, and that count is 3, size 0.045, life 0.10. Wet sets `Splash` 1 and stays darker in blue than in red.
- Those flags do not change the picture. `FxBurstPool.PlayShaped` still emits the same particle. Splash raises gravity. Spark raises speed and lowers gravity. A metal plant emits nothing, because the count is 0 and the method returns when the count is 0.
- `LandDust` then overwrites size, life, and count for every surface, including metal. The count runs from 8 up to 16 and does not go past 16. Life is 0.36 s plus a term that reaches 0.50 s at the roll threshold. The roll pool is already 16 (`FxKitLook.DustRoll`).
- A hard landing already picks a bit shape in `ImpactFx`. Grass bits are splinters, and every fourth grass bit is a chunk. Wood bits are splinters. Dirt, brick, concrete, metal, and wet bits are chunks. Plumes, where a surface has them, stay a soft puff. Metal sets no plumes. That split is the landing burst, not the foot plant.
- The wall scuff is one soft smear (`ShapeSoft`) for 0.42 s (`ScuffSeconds`). Its puffs hide after 0.24 s. `ScuffTint` has three colors: brick, wood, and a gray for everything else. Metal, wet, grass, dirt, and concrete walls share that gray unless the material name says brick or wood.
- `Pass5Look.ScrapeSpark` is true for metal and for concrete, and false for grass. `ScrapeDrop` is true only for wet. `Holds()` checks those two facts. A wall-run on grass emits 0 scrape bits.

### How the other games tell surfaces apart

**Mirror’s Edge Catalyst.** Verified, MCV/DEVELOP, 5 August 2016, James Slavin, read for this pass: they added surface types, and “squeaky polished glass and clean marble contrast greatly with corrugated metal and dirty concrete.” Those sounds “give Faith her friction, physicality and weight as she runs, scrapes, slides, jumps, lands and rolls.” He says they told the story from the feet up. Verified, GamesBeat, 17 October 2015, the same director, read for this pass: in first person the footsteps and the landing thumps are how you know how you are doing, and the team recorded footfalls with the microphone at ear level. Neither page lists a particle count, a brick, a wood, a grass, or a wet visual. Second-hand: a search snippet of a Game Design Gazette writeup (the page did not return article text when opened) has the writer, not Slavin, saying sound outperforms the picture for velocity, and it mentions a dull concrete slide and a gutter that sounds lighter than a metal pipe. That snippet is not a build note.

**Dying Light 2.** Verified, A Sound Effect, 14 December 2022, Wojciech Siadak, read for this pass: more than 150 player movement events. Steps change volume, pitch, and equalization with speed. A very slow walk is “a gently placed foot” instead of a stock step. Two microphones on the shoes and a pair at the ears. He jumped on walls, fences, lamps, and barriers. A boot shuffle on the wall plays only once stamina is below half. The page is audio. It does not publish a dust color or a mote count for concrete, brick, wood, metal, grass, or wet.

**Titanfall.** Verified in pass 1 from the Game Developer interview with Respawn. This pass did not re-open that page. The search snippet of the same URL still has no surface particle table. The published wall read is the camera tilt and the hand on the wall, and the landing read is a camera spring with the movement code left alone. Tag still cannot take the tilt. Nothing in that interview is a mote budget.

**Celeste.** Second-hand. A search snippet of a transcript of the GMTK video “Why Does Celeste Feel So Good to Play?” describes tiny dust when Madeline hits the ground, and a four-frame pause on the dash. The video was not opened, and the transcript page was not opened. There is no surface table in that snippet. Tag does not copy the pause. Hitstop and shake stay 0.

**Neon White.** Verified, The Verge, 3 July 2022, Ben Esposito, read for this pass: levels had to be “really, really clear” so a player can enter what he called the speed zone. The mission-complete moment is a short cutscene: the character flips in front of the camera, says the same line, and “MISSION COMPLETE” comes up. He calls that a victory dance with a sound cue and a repeated line. Verified, Game Developer, 16 March 2023, the same director, read for this pass: a clean, low-detail look with lots of negative space, so the path reads at once. Red doors are the main path. Green ivy means you are going the right way. The game does not ask for platforming, shooting, and cards in the same beat. Neither page describes foot dust. The visual lesson is that one clear shape beats a busy cloud. The flip is used in the emote note below, not as a surface effect.

No page opened for this pass says how those games retint dust for a 960×540 split. Do not invent their particle counts.

### Top 3 for C2

These sit on the dust table. They do not change `SurfaceCount`, the `At` numbers `Holds()` checks, `LandDust`’s cap of 16, or `ScrapeSpark` / `ScrapeDrop`.

#### 1. The sprite is the surface. The count stays.

**What.** Keep every size, opacity, life, count, span, and color `Holds()` already checks. Change the quad.

| Surface | Sprite | Count, unchanged |
| --- | --- | --- |
| Grass | The small pale puff that already ships. | Sprint count 6, size 0.13. |
| Dirt | One thick cloud. This is the only fat puff. | Sprint count 11, size 0.34. |
| Concrete | A short soft gray sheet, wider than it is tall. | Sprint count 9, size 0.20, span 0.72. |
| Wood | Thin splinter lines, not a round puff. | Sprint count 8, size 0.10. |
| Metal | Short bright streaks. A normal plant still emits 0. A pivot still emits 3, size 0.045, life 0.10. | `Spark` stays 1. |
| Wet | Falling ticks, not a round puff. Gravity can stay where splash already puts it. | `Splash` stays 1. |
| Brick | Hard chips. Same size curve as concrete. The red tint stays. | Not a seventh counted surface. |

**Reference.** Slavin, verified above: glass, marble, metal, and concrete are different materials, and the feet are how you know. Siadak, verified above: a slow step is not the same sound as a run. Neither man published a sprite. The Tag table already changes size and color. At 108 px of body, a red circle and a gray circle are the same event.

**Why it fits.** Brick and concrete share a size curve on purpose. Wood is only a little smaller than grass. Hue is what is left, and hue fails when the mote is a few pixels. A streak, a tick, a chip, and a splinter are different pictures at that size. Dirt stays the one cloud, so “thick puff” keeps meaning dirt.

**Cost.** 0 extra particles. The emit counts stay. Estimated 0 extra draws if the shapes are frames of the particle texture the pool already uses. A separate renderer per surface would be the wrong cost. This pass did not profile it.

**Readability risk.** High if metal’s quiet plant grows a puff. The count stays 0. High if brick chips use a new count and the proof line’s `surfaces=6` has to move. High if every surface gets a cloud plus a streak. One sprite each.

**Asset.** Self-made. CC0 particle art is optional and not required. Kenney’s pack is already listed in pass 1. Do not buy anything.

#### 2. One mark that is still there when the puff is gone

**What.** One quad on the contact, in the same shape family as the sprite above. Wet leaves a dark oval. Metal leaves a scratch. Brick leaves two or three chips. Wood leaves a short line. Concrete leaves a short pale streak. Grass leaves nothing: a lawn does not take a stain, and the pale puff is the whole read. The quad uses the life the scuff already has (0.34 s in `FxKitLook`, 0.42 s in `ImpactFx`) and it should still be visible after the foot puff has faded. Do not add motes. Do not raise `DustRoll`. Do not change `LandDust`.

**Reference.** The same Slavin sentence, verified above: she scrapes, slides, lands, and rolls, and the surface is the evidence. Jet Set Radio’s mark that stays, from pass 1, is the picture version of that. This is the foot-sized version of the roll skid already proposed in pass 1. It is not a second dust system.

**Why it fits.** A sprint concrete puff lives about half a second and then the pane is empty. In a four-player chase the foot is often at the edge of the cell or already gone. The mark is what the other panes can still see. The wall scuff already does this for brick and wood (the smear outlasts the puffs). The foot plant does not, and metal, wet, and concrete walls still fall through to the same gray smear.

**Cost.** 1 quad per contact, inside the scuff slots that already exist (4 in the kit). 0 extra dust. Estimated 1 draw if the marks share the scuff renderer. No new light.

**Readability risk.** High if grass also stamps a dark oval. That reads as dirt. High if the mark is white on concrete. Keep it darker than the sheet, with a 2 px seat-colored edge only when the surface is gray enough to swallow it. That edge width is estimated in the 960×540 cell, the same way pass 1 estimated the roll streak.

**Asset.** Self-made quad. No texture required.

#### 3. The wall uses the foot’s shape, so a streak means metal in both places

**What.** Foot and wall share the sprite table in idea 1. A metal wall streak is the same streak as a metal pivot. A wet wall is the falling tick, which `ScrapeDrop` already flags. A brick wall is chips, not only the dark red smear `ScuffTint` uses today. Concrete may keep sparking, because `ScrapeSpark` is true for concrete and `Holds()` checks that. Those concrete bits use the short pale nick, not the bright metal streak. Grass stays at 0 scrape bits. Do not add a surface. Do not change the two bools.

**Reference.** Titanfall, as cited in pass 1: the hand on the wall is the contact. Tag already has a hand scrape. Slavin, verified above, treats a scrape and a footstep as the same material story. Siadak, verified above, recorded the wall as a surface you jump on, not as a different material language from the ground. The boot shuffle is stamina, which Tag does not copy.

**Why it fits.** Today a metal wall and a concrete wall can both set `Spark`, and the scuff under them is the same soft gray unless the name says brick or wood. In a small pane the player learns one vocabulary. If streaks are metal on the ground and sparks are “any hard wall,” the word is lost.

**Cost.** 0 new particles. The scrape counts in `Pass5Look` stay. Estimated cost is a texture frame, not a new emitter.

**Readability risk.** High if concrete and metal share the bright streak. The bool can stay true and the picture can still split. High if the wall-run ribbon from pass 1 is recolored per surface. The ribbon is the seat. The streak is the material. They are two reads.

**Asset.** The same self-made frames as idea 1.

### What this pass is not asking C2 to build

- A seventh counted surface, or a new proof token. Brick stays outside the 6.
- New mote counts, a louder metal plant, or a bigger `DustRoll`.
- Camera tilt, shake, hitstop, or a stamina shuffle.
- A second dust system on top of `LandDust`. The landing bits already have splinters. Do not rebuild that burst. The mark in idea 2 is the part that remains.

### Emotes, for the motion-reference worker

This is not an FX build and not a movement lock. Coyote, buffer, speeds, terminal, and root motion stay as they are. If a celebration plays, the capsule stays free the way the landing roll is a pose on top of movement. Do not add a stun, a cancel window, or a settings row.

**Smash Ultimate.** Verified, SmashWiki taunt page, read for this pass. The page is a community wiki, not a Nintendo manual. A taunt is a motion of the whole character. Durations vary: Young Link’s Melee taunt takes more than three times as long as Kirby’s, which the page calls the fastest in that game. In Ultimate, most taunts can be interrupted on frame 50, except the ones that affect gameplay (Greninja and Luigi’s down taunt are the examples given). The page does not state a frame rate. If the game is running at 60 frames a second, frame 50 is about 0.83 s. That 0.83 s is an estimate, not a number on the page. Do not copy the interrupt. It is gameplay, and it would be a new lock. Verified on the same page, the Wii U digital manual: “Taunting leaves you exposed to enemy attacks, so taunt wisely.” Tag’s answer to that sentence is that the body keeps its movement. The celebration does not become a hitbox.

**Neon White.** Verified, The Verge, above. The victory is a full-body flip across the camera, then a line of text. Esposito’s point, verified in both interviews above, is that the picture stays obvious. A chase pane will not put the face in front of the lens the way that cutscene does. Take the whole body crossing the frame. Leave the one-liner. Comic words stay on the punch.

**What a 2–4 s clip needs at this size.** No opened page measures a celebration inside a 960×540 couch pane. The still is the test this pass could run. At the 108 px stand-in the head is 22 px across. A mouth, a brow, and a hand sign do not survive that. A shrug keeps the arms against the torso, so the outline is the same as a run. These outlines do survive, from the side and from behind:

- Both arms in a V, held out past the shoulders.
- One arm straight up, the other still out, so the pose is not a spike with no width.
- A wide star: arms and legs both leave the torso.
- A full-body spin, so the body crosses itself. That is the Neon White lesson in chase-camera form.

Hold the extreme for about 0.4 s, estimated, inside the first second. A glance across the couch happens in that second. The rest of the 2–4 s can be the return to the run. Do not spend the first second on a wind-up. A prop smaller than the head fails the same way a face fails.

### Stills

| File | Cell |
| --- | --- |
| `Docs/FX/research-pass3/01-shape-not-tint.png` | Six surfaces, one sprite each. Counts are the ones already in `Holds()`. |
| `Docs/FX/research-pass3/02-contact-mark.png` | The puff is gone. One mark is still on the concrete. |
| `Docs/FX/research-pass3/03-wall-matches-foot.png` | Metal is a streak on the wall and at the foot. Concrete is a nick. |
| `Docs/FX/research-pass3/04-emote-silhouette.png` | A held V against a shrug, both at 108 px. For the motion worker. |

### Pass 3 limits

- No gameplay scripts were edited. The proof line is untouched, including `surfaces=6`.
- MCV/DEVELOP, GamesBeat, A Sound Effect, The Verge, Game Developer, and the SmashWiki taunt page were opened and read. Claims from those pages are marked verified. SmashWiki is not a Nintendo manual.
- The Game Design Gazette page did not return article text. The glass-squeak sentences from it are second-hand.
- The GMTK Celeste video and its transcript were not opened. The dust sentence is second-hand. The four-frame pause is not a Tag request.
- The Titanfall interview was not re-opened. This pass used the pass-1 reading and a search snippet of the same URL. No new Titanfall numbers.
- No particle counts from Mirror’s Edge, Dying Light, Celeste, Titanfall, or Neon White. Those pages do not publish them.
- The 108 px body is a drawn stand-in. The 0.4 s pose hold and the 2 px mark edge are estimates. The 0.83 s reading of Smash frame 50 assumes 60 fps, which the wiki page does not state.
- Draw counts are estimated. No GPU profile. No live split-screen capture.
