# Animation and FX plan

Pass 1 is the exit poses and the terminal landing roll. Pass 2 deepens those poses. Pass 3 is running dust and comic contact words. Pass 4 redraws those words as a classic comic burst and adds the verb FX layer. Pass 5 refits those exits onto the live verbs and adds the remaining FX. Exits, the roll, and the FX stay visual: they do not change speed, stun, coyote, cling, slide, or the camera.

## Pass 5 (this branch)

- Exit first frames join the live end of the verb over 0.22 s, then the authored recovery plays. The wall-jump exit waits out the shove arc (0.27 s) and joins the risen pose, so the arc is not covered. Zip arms sit on the cable (−158). Grapple arrive and release keep the left-hand pull, and the chest stays on the rope when the line was live. A pad landing joins the open swing with the softened knees. A moving punch keeps the chest lead. A sprint landing keeps the stride in the thighs. Climb top-out meets the climb, or the mantle land when the exit came from a mantle. Vault and mantle start on that land. The roll and the soft land open in the fall pose, then the shoulder turn plays. Root spin, pitch, roll, and drop are the authored ones, so the roll orbit is unchanged. Stagger and tag-back are not joined.
- Hands on a climb top-out, vault, or mantle stay on the lip through the first 28% and leave by 55%. A lip ray wins. If it misses, the hands use the real ledge stand, offset along the lip so they do not stack, the same 8 cm back from the face the climb uses. A roll, a near-stop absorb, and a heavy soft land put a palm on the ground. A planted sole pins its XZ and only follows the ground in Y, so the foot does not skate while the capsule keeps its speed. Skate at a full pin is 0 cm.
- A backward landing (faster than 0.35 m/s back) eases the legs into the reversed stride over the last 38%. Air exits do not reverse. The sole levels while the foot is planted. Any input still cancels inside 0.08 s. The roll does not change velocity. The camera does not roll.
- Speed lines appear at sprint (13.8) and faster: 4 at sprint, up to 8 as speed rises, half of that on Low, none at a walk, none when Effects is Off or Reduced flashing is on.
- Wall-scrape sparks sit at the chest and the hand during a wall run or a climb, above the foot scuff. Metal sparks, wet drips, concrete is quieter, grass is none. The pool applies the Effects slider once.
- A tag hit throws its own 8-point burst (4 on Low) with no letters. Comic words stay on their toggle. Effects Off and Reduced flashing hide the burst even when comic words are on. Comic words off still shows the burst.
- Becoming It, or losing it, plays one swell on the body (0.40 s, player color when gained, cool white when lost) plus an expanding ring. The quad faces each camera, so every split pane that can see the body reads it. It is not a strobe.
- A launch arc leaves a cyan trail. A zip ride leaves a violet trail. Both are 12 points, 6 on Low, and fade for 0.35 s after the ride. Pooled. No hot-path allocation.
- Proof: `exit-fit join=0.22 wallJump=-126 zip=-158 grapple=-134 lip=1>0 skate=12>0 reverse=1 rollOpen=58 step=56.2 budget=96` and `fx-pass5 lines=4/2 len=0.55 scrape=6 grass=0 tag=8 comicOff=1 flash=0.40 span=1.15 trail=12/6 alloc=0`.
- Stills in `Docs/AnimStills/pass5/`: `walljump`, `climb`, `vault`, `zip`, `grapple`, `pad`, `roll`, and `reversal`, each as `-front.png` and `-three-quarter.png`, plus `speed-lines.png`, `wall-scrape.png`, `tag-burst.png`, `handoff-flash.png`, and `pad-zip-trails.png`.

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

Running dust, the comic words, the verb layer, and the pass-5 effects are in.

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

Shipped in pass 5: speed lines, wall-scrape sparks beyond the foot scuff, a tag hit burst separate from the comic word, an It-handoff flash, and pad and zip trails.
