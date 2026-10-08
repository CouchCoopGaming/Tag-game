# Animation and FX plan

Pass 1 is the exit poses and the terminal landing roll. Pass 2 deepens those poses. Pass 3 is running dust and comic contact words. Pass 4 redraws those words as a classic comic burst and adds the verb FX layer. Pass 5 refits those exits onto the live verbs and adds the remaining FX. Exits, the roll, and the FX stay visual: they do not change speed, stun, coyote, cling, slide, or the camera.

## Pass 5 exit fit (this branch)

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

## Pass 5 (this branch)

- BAM! and WHAM! are about 10% larger inside their bursts so they match POP and POW. The four-cell atlas is unchanged.
- A hard landing grows a ring and a debris flick with the light, medium, and heavy fall tiers. The shoulder roll swirls dust along the travel. Both live in `VerbOwnedFx`.
- A dash leaves 4 ghosts at Full and 3 at Low, in the player color, for the 0.10 s dash.
- The rope still sags and wobbles. The hook chips the surface, and letting go snaps.
- A stagger shows three comic dizzy stars, the same burst, outline, and print dots as the words, for the existing 0.25 s.
- Tag-back keeps its 1.0 s. A soft rim in the player color pulses in every split pane. It is not a light.
- A wet wall drips on the foot plant. The scuff streaks stay with the other trails.
- Pause → Settings → Effects is still Off, Low, or Full. Off and Reduced flashing hide this layer. Low draws fewer ghosts and particles.
- Stills are in `Docs/AnimStills/pass5/` (`owned-fx.png`, `dizzy-star.png`, `bam-wham.png`).

## Pass 6 (this branch)

- Dust puffs are an 8-frame flipbook. The landing ring is a soft decal, and a heavy fall uses the cracked variant for that surface (grass, dirt, concrete, wood, metal, wet). Wet plants use a drip sprite. Dash ghosts use an afterimage shader with a fresnel rim and the player color. Art is project-owned, in `Assets/Resources/FX`.
- Alpha and scale ease in and out. A card starts at 0 and ends at 0.
- Zip drop, the hands-down absorb, wall jump, climb top-out, and mantle ease in over 0.16 s. The other exits keep the 0.08 s onset. Cancel stays 0.06 s. Durations stay put.
- Rendered stills are in `Docs/AnimStills/pass6/`.

## Pass 7 (this branch)

- Dash ghosts are frozen copies of the mannequin mesh, in the pose at each of the last four dash samples. `Tag/Afterimage` tints that mesh with the player color and a fresnel rim. They are not cards. The newest copy is the brightest. The oldest has eased out. Low still draws one fewer.
- The shoulder roll tucks the chin, keeps the lead arm bent into a hoop, and turns across the back onto the far hip, then stands into the run. The shape follows the grade-B roll clips (usable with care) on `cursor/tag-storror-mocap`. Time stays 0.52 s. The 65% gate stays. Speed, stun, and the camera stay put.
- Exit onset is one curve. `VerbExitFit` joins the live pose over 0.22 of the clip. `VerbExitEase.Enter` stays at 1 for every joined exit, so the 0.16 s show-weight is not stacked on that blend. Stagger and tag-back do not join, and they still rise over 0.08 s. Cancel stays 0.06 s.
- Speed lines, wall-scrape sparks, the tag hit burst, the It handoff flash, and the pad and zip trails stay on `Pass5Host` and `Pass5Burst`. This pass renders them on the tinted mannequin.
- Stills are in `Docs/AnimStills/pass7/` (`landing-tiers.png`, `roll-swirl.png`, `dash-ghosts.png`, `roll-strip.png`, `speed-lines.png`, `wall-scrape.png`, `tag-burst.png`, `handoff-flash.png`).

## Pass 8 (this branch)

- The dash still is a three-quarter side view. Four mesh ghosts trail behind the runner along the path and overlap the body. Alphas are 0.60, 0.45, 0.30, 0.15. Each one is the player tint with a fresnel rim, not an opaque white shell.
- The roll stills are a medium side view. The strip is five frames: hand to the ground, tuck, shoulder contact with the legs in, coming up, then a run. The swirl frame is the shoulder contact. The shape follows the grade-B roll strips on clips 01 and 02. Time stays 0.52 s. The 65% gate stays.
- The tag still is a letter-free comic burst at the fist: jagged spikes, a black outline, halftone dots, a white inner flash, and speed spikes. POP, POW, BAM, and WHAM stay on the words.
- The ground is a grass plane plus a concrete pad, both noised so they read as texture. The key is a warm sun under a Hosek sky.
- Stills are in `Docs/AnimStills/pass8/` (`dash-ghosts.png`, `roll-swirl.png`, `roll-strip.png`, `tag-burst.png`). Coyote, speeds, the 0.52 s roll, and the 65% gate stay.

## Pass 9 (this branch)

- The runner and the four dash ghosts are the same Hier mesh. Skin takes the player color. Sensors, joints, wear, and the teal accent stay, so the bolts read on the solid body and on the trail. Alphas stay 0.60, 0.45, 0.30, 0.15, with a fresnel rim.
- The roll is a low diagonal: hand plant, lead shoulder down, across the back, hip, then up onto the feet. Peak hip height stays under 0.90 m, the head stays off the ground, and the hips-over-head angle stays under 60 degrees. The landing-roll proof adds `hip`, `head`, and `invert`. Time stays 0.52 s. The 65% gate stays. In play the mesh banks onto the shoulder. It does not turn a full somersault.
- A tag draws one comic starburst at the contact. Black outline, orange fill, white flash, halftone, speed spikes, no letters. Full density still reports `tag=8`, which is how many spikes are on. The comic-words toggle still hides the burst.
- The dust still is one sprint on dirt beside one sprint on concrete, at the same speed, so the surface scale is visible.
- Stills are in `Docs/AnimStills/pass9/` (`dash-ghosts.png`, `roll-swirl.png`, `roll-strip-side.png`, `roll-strip-three-quarter.png`, `tag-burst.png`, `running-dust.png`). Coyote, speeds, the 0.52 s roll, and the 65% gate stay.

## Pass 10 (this branch)

- The dash still looks down on the crown, where the Hier sensors and bolts face. The solid runner and the four ghosts are the same mesh (4115 head verts, same slots). Skin is the player color. Sensors stay black, joints stay dark, and the teal accent stays. Ghost alphas stay 0.60, 0.45, 0.30, 0.15.
- The tag fist stops on the shoulder cap. The forearm stays clear of the chest. The burst sits on that contact, below the face.
- Dust stills are soft clouds that grow and fade, not a line of dots. A sprint on dirt is a warm tan cloud about 0.45 to 0.75 m across. Concrete at the same sprint is a small pale gray puff. A third panel puts a walk beside a sprint on dirt. Grass is a few flecks and no cloud. The live `DustLook` numbers are unchanged.
- A hard landing shows the 1.30 m ring and 12 debris bits. A slide shows the two 0.18 m scrape ribbons. A launch pad shows the 1.15 m cyan ring and the wind streak. A zip shows the cable with 0.43 m of sag and 8 violet sparks.
- The roll stills from pass 9 stay. Time stays 0.52 s. The 65% gate stays.
- Stills are in `Docs/AnimStills/pass10/` (`dash-ghosts.png`, `tag-burst.png`, `running-dust.png`, `landing-ring.png`, `slide-scrape.png`, `launch-pad.png`, `zip-line.png`).

## Pass 11 (this branch)

- The dash still keeps the crown view and pulls back so the solid runner's feet stay in frame.
- Dust cards are filled soft clouds. Center alpha is about 0.75, the edge fades, and the shader is alpha blend on a lit surface, not an additive ring. A sprint on dirt is a tan cloud at the feet, waist-low. Concrete is a smaller pale gray puff. A walk on dirt is a short faint puff. Close shots put the runner at about half the frame, and a wide shot puts the three together.
- A hard landing under the roll gate is a deep knee absorb: knees near 100 degrees, hips low, chest forward, head up, one hand near a foot. The 1.30 m ring and 12 debris bits stay.
- A launch pad throws short cyan rings and streaks that fade by knee height, plus a few speed lines behind a tucked leap.
- A zip leans into the travel and leaves a short spark trail at the hand.
- A punch (not a tag) shows a small POW!. The tag burst stays the large letter-free one.
- Stills are in `Docs/AnimStills/pass11/`. The live dust numbers, the 0.52 s roll, and the 65% gate stay.

## Pass 12 (this branch)

- Dirt sprint dust is one step lighter and a little less saturated than pass 11, with a warm rim on the cloud edge so it separates from the ground. The clouds stay filled. Concrete stays a small pale puff. A walk stays a short faint puff. `DustLook` is unchanged.
- The hard-landing absorb is a squat. On this rig a positive thigh X folds the knee up and back, which is why pass 11 measured a 100° bend and still rendered a downward dog standing on its hands. The still now uses a negative thigh pitch, so the knees sit forward of and below the hips, the feet sit under the hips, the chest pitches forward, the head looks ahead, and one hand rests by a foot. The 1.30 m ring and 12 debris bits stay.
- A punch uses the same side camera as the tag burst. POW! is a comic starburst at the fist, with a black outline and yellow letters, about 13.5% of the frame height. The tag burst stays the larger letter-free one.
- The launch pad, the zip sparks, and the slide scrape are the same reads as pass 11, shot in the park. A grapple still shows the sagging rope, the hook, and two chips.
- Stills are in `Docs/AnimStills/pass12/`. The live dust numbers, the 0.52 s roll, and the 65% gate stay.

## Pass 13 (this branch)

- The planted landing, the POW size, and the lighter dirt dust stay as in pass 12.
- The punch is an extended jab. The fist meets the chest (about 5 cm of mesh gap) and POW! is centered on that contact. The letters are about 13% of the frame height. The tag burst stays the larger letter-free one.
- The slide is feet-first. Hips tip back, the lead heel stays low, the other leg tucks, and one hand trails behind the hip. Scrape sparks and a dust streak sit on the lead heel. The old root pitch that put the head on the ground is gone.
- The zip is a clear side view. The whole body hangs from the handle. The cable is one line, and the eight sparks sit on the trolley.
- The grapple rope starts on the left hand bone, sags, and ends at the hook. The camera is a side view with no tree in front of the body.
- Stills are in `Docs/AnimStills/pass13/`. The live dust numbers, the 0.52 s roll, and the 65% gate stay.

## Pass 14 (this branch)

- The zip puts both hands on the trolley handle. Each hand is within 2 cm of the grip. The knees tuck, and both feet trail behind the hips. The eight sparks are warm orange-white, the same family as the other metal sparks, and they sit on the trolley.
- The slide sits on the ground. The hip bone is 0.23 m up because the pelvis mesh is on the floor, the trail foot folds under at 0.10 m, and the lead foot is 0.99 m ahead. Dirt kicks a tan dust spray off the lead heel. Concrete kicks warm orange sparks. Sparks stay off dirt.
- The grapple is a swing. The left arm stays extended along the rope (elbow 15°), the torso leans 55° off vertical, and both feet trail. The rope still starts on the left hand bone.
- The punch still meets the back. The target takes a short hit-stop: the chest gives and the head snaps forward, and the head stays 0.30 m above the hips. Three short speed lines trail the fist. POW stays 13.2% of the frame height.
- A wall run draws four faint streaks on the wall at the contact foot, about the sprint line length. A wall jump puts a pale puff on the wall at the kicking foot. A vault puts a puff on the box top at the hands.
- Stills are in `Docs/AnimStills/pass14/`. The live dust numbers, the 0.52 s roll, and the 65% gate stay.

## Pass 15 (this branch)

- Contacts are read from the evaluated mesh. The mannequin is rigid pieces parented to bones, so a bone head is not the surface.
- The slide puts the pelvis mesh on a dirt path, 1.7 cm off the surface. The lead heel is 2.7 cm up, and the head sits behind the hips. Dirt kicks a tan spray. Concrete kicks warm sparks. The ground under the body is brown dirt or gray concrete.
- The wall run is a 6.2 m by 3.2 m wall. The near foot mesh is 1.2 cm off the face, and the hip mesh is 49.6 cm clear of that face. Four streaks start at the foot.
- The wall jump is a 6.0 m by 3.2 m wall. The kicking foot is 1.2 cm off the face, and the hip is 37.0 cm clear. The puff sits on that foot.
- The vault plants both hands on the box top, 1.2 cm and 1.8 cm off it. The hips are 14.5 cm above the top. The feet are 23.9 cm and 7.4 cm above it. The puff is at the hands.
- Each contact has an orthographic debug still with red dots on the measured vertices. A 10 cm vertical pair reads back as 10.0 cm, and the pixel gap matches the centimetre gap.
- Zip, grapple, and punch keep the pass 14 poses and are rendered again into this folder.
- Stills are in `Docs/AnimStills/pass15/`. The live dust numbers, the 0.52 s roll, and the 65% gate stay.

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
