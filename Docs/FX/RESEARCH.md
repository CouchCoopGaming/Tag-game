# FX research, pass 1

For C2 on `cursor/tag-fx-kit`. Visual only. Ororo can relay the top five below as the next build list.

Nothing in this note changes feel or gameplay. Leave these locks alone: coyote 0.10, jump buffer 0.16, jump speed 24.7, terminal fall 56.16, walk 6.9, crouch 3.68, sprint 13.8, root motion off. `ChaseCam` fov pop, shake, and slow motion stay 0. The landing roll stays at 65% of terminal (threshold 36.504 m/s) and 0.52 s. No new settings row. The pause list stays 21 rows. Reduced flashing and Effects Off keep hiding bursts.

This pass did not edit scripts, so the proof lines are untouched. `HotPathAlloc` still formats `hot-path allocs before=101 after=` plus the live count. `ropeBody` does not appear anywhere in this branch.

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
