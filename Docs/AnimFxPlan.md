# Animation and FX plan

Pass 1 is the exit poses and the terminal landing roll. Pass 2 deepens those poses. Pass 3 is running dust and comic contact words. Pass 4 redraws those words as a classic comic burst and adds the verb FX layer. Exits, the roll, and the FX stay visual: they do not change speed, stun, coyote, cling, slide, or the camera.

## Pass 3 (this branch)

- Foot puffs land on the gait plant (the stride crossing each multiple of pi). Size, opacity, lifetime, and count grow with planar speed. A walk at 6.9 is faint. A sprint at 13.8 is a clear cloud. Run start, a hard pivot, and the first frame of a slide kick harder. A slide keeps a thinner trail. A wall run scuffs on the same plant, and a vault plays the hand puff. Both use the surface table. Reduced flashing skips the clouds.
- Surfaces: grass (green-brown, light), dirt/sand including mulch (tan, thick, longer), concrete/asphalt (light gray, thin, short), wood (faint splinters), metal (quiet, a few sparks only on a hard pivot), wet (dark droplets). Put a `SurfaceTag` on the collider (kind 0 grass, 1 dirt, 2 concrete, 3 wood, 4 metal, 5 wet). If there is no tag, the physics material name is used, then the renderer material name, then the object name. The parks stamp grass, mulch, sand, concrete, wood, and metal when they build. Name a material `wet`, or set the tag to 5, for splash. The stamp does not change slide friction.
- Comic words on a punch hit or a tag: POP!, POW!, BAM!, WHAM!. Random, and the same word never plays twice in a row. A punch favors POP! and POW!. A tag favors WHAM! and BAM!. Each word is its own hand-built burst (see pass 4). Settings has Comic words, on by default. Reduced flashing hides them. No hitstop, no shake.

## Pass 4 (this branch)

- Effects slider in Pause → Settings: Off, Low, Full. Full is the default. Off hides dust, comic words, and the verb layer. Low keeps the words and draws about half the particles, ghosts, and wisps. Reduced flashing hides the layer even when the slider is Full.
- Big landings grow a ground ring and a debris flick with fall speed, on the same light / medium / heavy tiers as the landings. The 65% shoulder roll also swirls dust along the travel.
- A dash leaves 3 or 4 fading ghosts in the player color during the 0.10 s. The cooldown ring is unchanged.
- A grapple rope sags and wobbles. The hook chips the surface (sparks on a hard metal hit, droplets on wet). Letting go snaps. The pull is unchanged.
- A launch pad throws an up ring and a thin wind streak on the rise.
- A zip throws trolley sparks that grow with speed, and the line shimmers.
- A stagger shows comic dizzy stars for the existing 0.25 s.
- Tag-back keeps its 1.0 s. A soft rim in the player color pulses so the safe body reads in every split pane.
- A wall run leaves speed-scaled scuff streaks. Wet walls drip.
- Sprint and faster leave world-space wisps on the body.
- Comic words, redrawn before the other effects: each word has its own jagged burst, a thick black outline, a second burst in a contrasting color, and a clear Ben-Day screen on both colors. Letters are the OFL font Bangers (`Assets/Art/FX/Fonts/Bangers-Regular.ttf`, license in `OFL.txt`): chunky, slanted, tight on one arch, a thick black stroke with a thin white inner stroke, an extruded shadow, and a highlight. The word fills most of the burst and crosses the inner edge. Each letter turns at most 6°. BAM and WHAM also throw speed lines behind the burst. The pop goes 0 to 1.25 to 1.0 inside 0.05 s, wobbles a little, then fades by 0.45 s. Cells are 1024 and sampled bilinear. Whole-word tilt stays within ±12°. Same toggle, pool, and Reduced flashing switch.
- Camera fov pop, shake, and slow motion stay 0. No hitstop. Stills are in `Docs/AnimStills/pass4/` (`comic-before.png`, `comic-after.png`, `comic-park.png`, `verb-fx.png`).

Pass 3 mockups stay in `Docs/AnimStills/pass3/`.

## FX queue

Running dust, the comic words, and the verb layer are in. Do not add the later items until a later pass.

## Pass 2 (this branch)

- The landing roll is a full turn of the mesh about the line from the lead shoulder to the opposite hip. Chin tucks, the lead arm sweeps, the shoulder meets the ground, the back crosses, the legs fold over, a foot plants, and the stride takes the speed you already have. The lowest point of the mesh sits on the floor. The capsule does not move.
- Strafe picks the lead shoulder. A straight landing alternates left and right.
- A near-stop at the same fall speed puts both hands on the ground, then stands.
- Landings under the roll gate are three reads: a light hop, a medium knee bend, and a heavy absorb with the hands near the ground.
- Each exit eases through its keys. The arms lag the torso. The head settles last. Wall-run, vault, slide, and climb top-out mirror for the other side.
- A vault into a slide, a wall jump into the next wall run, and a roll into a jump ease across. They do not pop. Jump still peels the roll inside 0.08 s.
- Stick figures with a floor line are in `Docs/AnimStills/pass2/`.

## Pass 1 (this branch)

- A recovery pose for every current verb, about 0.15–0.35 s, eased off with the existing pose blend.
- Jump, slide, punch, dash, or lunge peels an exit off in 0.06 s. Input does not wait.
- Shoulder roll when downward speed is at least 65% of terminal (56.16 m/s). Threshold is 36.50 m/s. From a stop, at fall gravity 22 × 1.62, that is a 18.69 m drop. A near-stop at the same speed is a short crouch absorb. Speed is unchanged. Camera does not roll.
- Pooled dust foundation, and a roll dust burst. Reduced flashing skips the burst. The roll sound reuses the hard-land clip (`LandingRoll` on the audio bus). No new audio file.

## Later

- Speed lines.
- Wall-scrape sparks beyond the foot scuff.
- Tag hit burst (separate from the comic word).
- It-handoff flash.
- Pad and zip trails.
