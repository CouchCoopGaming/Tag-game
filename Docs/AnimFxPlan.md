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
- No body part may sit more than 0.5 cm inside a solid or inside another body part. Joined neighbours ignore vertices within 3 cm of the shared joint. Joined depth is the amount past that pair's bind overlap, because the rest chunks already meet and that rest overlap is not an animation. A point counts as inside only when the nearest face says so and at least 3 of 5 rays agree, so a rigid hip pitch is not a false fail. The ground test seats the soles on the standing sole, then fails any other piece below that floor. Run it again with `dotnet run --project Tools/NoClipDump/NoClipDump.csproj -c Release > /tmp/noclip-frames.txt` and `blender --background --python Tools/NoClipCheck.py`.
- This pass measured `no-clip clips=35 frames=320 worldMax=22.44 selfMax=7.97 fails=311`. The deepest solid hit is the vault forearm, 22.44 cm inside the box at the plant, with the hips 59 cm below the hands. The wall-run upper arm is 11.82 cm inside the wall. The deepest self hit is the zip-drop forearm, 7.97 cm inside the head. The punch fist is 7.13 cm inside the head. Those keys are the same numbers the locked proof prints, so they were not retuned here. On this rigid mannequin an elbow past about 10 degrees, or a knee swung forward past about 4 degrees, already exceeds 0.5 cm outside the 3 cm joint ball. Red overlap stills for the vault, the wall run, the punch, and the roll are in `Docs/AnimStills/pass15/noclip/`. Later passes keep this check and have to bring fails to 0 without moving the locked lines.

## Pass 16 (this branch)

- The same checker now prints two counts. `rigJoint` is a joined neighbour overlapping more than 0.5 cm past its bind overlap, outside the 3 cm ball around the shared joint. That is the rigid mannequin (Mesh_Hips into UpperLeg and the same family). PR #128 owns the rig, so this pass does not edit it. `poseFails` is a world hit or a non-neighbour hit, such as a fist in the head. A frame can be both. The check exits only when `poseFails` is above 0.
- Splitting the pass 15 measurement gives `poseFails=263 rigJoint=294` (`worldMax=22.44`). After the pose re-author it is `no-clip clips=35 frames=320 worldMax=0.00 poseMax=0.49 rigMax=3.40 poseFails=0 rigJoint=313`. The rig count went up because the new poses sit the rigid pieces together differently. The rig overlap is still there.
- The punch starts in a guard in front of the chin, then extends. The zip-drop release stays on the hang pitch so the forearm and the cable do not sweep through the head. The vault, mantle, and climb plant bent elbows on the box top, with the hips about 9 cm above that top and the legs outside the box footprint. The wall-run near arm yaws off the wall, and the plant leg keeps the full abduction so the upper arm is not in the wall.
- Before/after stills for vault, punch, wall-run, and exit-ZipDrop are in `Docs/AnimStills/pass16/noclip/`. The scene is the normal grey, the prop stays visible, the mannequin stays grey, and only the overlapping volume is red. The after frames have no red volume.
- Gameplay locks stayed byte-identical: coyote 0.10, jump buffer 0.16, cling 0.08, jumpSpeed 24.7, terminal 56.16, roll at 65% of terminal for 0.52 s (threshold 36.50), gameplayDelay=0, root motion off, plus the enemy-ai, pocket-ai, and stack-ai lines and `hot-path allocs before=101 after=0`. Animation-key printouts moved with the new poses. The proof diffs are in the PR body.

## Pass 16 add-on (roll and wall run)

- The landing-roll root now orbits the lead shoulder (pivot about 1.15 m up) instead of the hip. The bank peaks at 122° near t=0.17 of 0.52 s and is back to 0° at t=0.52, so the rise pose is on its feet. Root motion stays off. The 0.52 s duration and the 65% trigger are unchanged.
- Measured on the banked mesh, seated so the lowest vertex is on the floor: at t=0.167 the lead shoulder is 13.2 cm up, the head is 10.0 cm up, and the foot is the contact at 2.1 cm. That is down from about 1.2 m, and it is not a shoulder plant. A bank past about 130° puts the head on the floor before the back does, on every head tuck and spine arch tried, so the curve stops short of a headstand. Elbows in the tuck are opened to -20°/-18°. Thighs are abducted (roll ±50). The leftover forearm/upper-arm and hip/thigh overlaps are rigJoint.
- Wall run uses a shorter plant-leg roll (-20°, run only; exits keep -36°) and swings the inner arm on bone Z (InnerRoll -12° plus the 4–8° gait roll). Yaw still only twists along the arm. Sampled hip clearance off the foot's wall face is 29.4–41.0 cm on the left and 33.0–42.1 cm on the right. The tightest left frame is 29.4 cm at t=0.2. No piece penetrates either wall.
- Checker, both wall sides included: `no-clip clips=36 frames=329 worldMax=0.00 poseMax=0.49 rigMax=3.40 poseFails=0 rigJoint=322`. Grey-scene strips with the prop are in `Docs/AnimStills/pass16/addon/`. Gameplay proof lines stayed byte-identical with the pass 16 sim.

## Pass 16 C2 (wall clearance and shoulder plant)

C2 measured the runtime poses. This pass re-authored the wall run and the landing roll against that spec.

Wall run, both sides, every 30 fps frame of the cycle. The wall face is the stepping sole. Nothing else is inside that plane.

- Hips sit 29.9–30.2 cm off the left wall and 29.8–33.1 cm off the right wall.
- The torso leans out 12.9°. The run's ±15° lean had been applied with the sign that cancelled the hip roll, so the chest was upright and the shoulder rolled toward the wall. The spine roll for that 15° band is flipped. Wall-jump lean is left alone.
- The nearest non-sole piece is the plant shin, 1.2–4.1 cm outside the face. World penetration is 0.
- The wall-side arm swings on bone Z (`InnerRoll` ±10° with the step). Yaw still only twists along the arm. The trail thigh is abducted 14° so the outward lean does not put it through the spine.

Landing roll. Elbows in the tuck sit at an interior angle of about 100–107° (euler −72°/−66°). Thighs stay abducted. The bank peaks at 135° near t=0.16. Measured on the banked mesh, lowest vertex on the floor:

- t=0.133: lead shoulder on the floor, head 16.9 cm up, chest 19.5 cm, hip 22.1 cm, foot 60 cm.
- t=0.167: lead shoulder on the floor, head 11.4 cm up, chest 18.5 cm, hip 27.2 cm.
- t=0.520: foot on the floor, lead shoulder 1.37 m up.

The shoulder is the contact from t=0.133 through t=0.300. The back and the opposite hip do not take a turn on the floor. On this mannequin the shoulder mesh, and then the head, stick out past the spine and the pelvis on every axis the roll is allowed to use, so the curve comes back to the feet before the head becomes the contact. The closest the head gets while the shoulder is still down is 2.8 cm at t=0.267. Duration stays 0.52 s, the trigger stays 65% of terminal, root motion stays off.

Gameplay proof lines stayed byte-identical with the previous sim, including `landing-roll` and `verb-exit` `roll=0.52`. The rig is untouched.

Checker, both wall sides and the roll included: `no-clip clips=36 frames=329 worldMax=0.00 poseMax=0.49 rigMax=3.40 poseFails=0 rigJoint=322`. Absolute depth on the roll is pose 0.33 cm and rig 2.81 cm. On the left wall run it is pose 0.35 cm and rig 2.09 cm. On the right wall run it is pose 0.29 cm and rig 2.22 cm. World depth is 0 on all three. `poseFails` is 0. The elbow hinge overlap stays in `rigJoint`.

3/4 and side sheets, grey scene, prop visible, red only on penetrating volume: `Docs/AnimStills/pass16/c2/`.

## Pass 17 C2 (launch, pull, absorb, elbows)

C2 (PR #127 `4d2b25bc`) measured these with absolute depth, on the poses the game plays. Pose fails are 0. Nothing here damps or straightens a fold at runtime. The rig is untouched. Timing is unchanged.

Launch rise, `LaunchPose.At(24.7)`, pitch −155, windmill ±28.

- Before: yaw ±16, roll 0, elbow −14. Both upper arms go through the head at 9.54 cm. The lower arms go through at 8.91 and 8.79 cm. The hands sit at x ±0.12, inside the head (head x −0.12 to 0.13).
- After: yaw 140 and bone-Z roll ±56, elbow at the fold limit −10, thighs abducted 14°. Hands at x ±0.47 and z 2.07. The crown is z 1.81, so the hands are 26 cm above it and 34 cm outside the skull. Both windmill frames stay on their own sides of the head. Pose depth is 0.24 cm (spine/thigh). `risePitch` stays −155.

Grapple pull, both hands on the line. The pull this branch plays (pitch −78/−84, yaw ±32, elbows −2/−4) was already clear: pose 0.39 cm, hands forward of the chest and below the crown. Those keys did not move. The before still rebuilds the older stored pull (pitch −134/−126, spine 56, hip 36). On that rebuild the head overlap is 1.44 cm (upper arm) and 0.83 cm (lower arm), and the arms go through each other at 3.14 cm. C2's 7.69 cm was that same pitch family with the rest of that runtime, which these keys do not contain.

Hard land, left hand down. Printed `hardKnee`, `hardThigh`, `hardHand`, `hardFree`, and `hardDrop` stay. Knees abduct 40°. The free arm rolls −14° on bone Z so the opened elbow does not enter the chest. Before that roll, the free upper arm was 0.95 cm inside the chest. After, pose depth is 0.43 cm. Spine through the thighs was already under 0.5 cm on this branch's keys, so the 2.96 cm figure is not this pose. The knee spread is what keeps it there.

Elbows. `ElbowClear` is −10°. Past about −12° the forearm enters the upper arm (0.58 cm added at −12, 1.25 cm added at −30, raw 1.57). At −10° the added depth is 0.08 cm, which is the joined hinge and stays in `rigJoint`. Stumble hold is −10/−10 (was −30/−24). The punch strike's off arm is −10 (was −36, raw 1.56 cm). The cock stays −78 and the recover stays −96, because those silhouette holds are the windup. Run at speed 9 pulls the back elbow to −9.8 instead of about −28. No runtime clamp was added.

Checker: `no-clip clips=40 frames=337 worldMax=0.00 poseMax=0.49 rigMax=3.40 poseFails=0 rigJoint=330`.

Absolute depth, world 0 on all of them:

- launch-rise pose 0.24 cm, rig 0.89 cm
- launch-mill pose 0.24 cm, rig 1.18 cm
- grapple-pull pose 0.39 cm, rig 2.34 cm
- land-hard pose 0.43 cm, rig 0.97 cm
- stagger pose 0.44 cm, rig 1.49 cm
- punch pose 0.49 cm, rig 3.27 cm
- run-speed9 pose 0.45 cm, rig 1.86 cm

The roll and both wall runs are unchanged: exit-Roll pose 0.33 / rig 2.81, wall-run pose 0.35 / rig 2.09, wall-run-right pose 0.29 / rig 2.22.

StrafeJumpSim exits 0. Gameplay lines are byte-identical with the previous sim. One animation line moved: punch `guardStrike` elbow −36 → −10.

Grey stills, red only on the overlap: `Docs/AnimStills/pass17/c2/`. Before frames carry the red. After frames have none. The punch and run marks are small (45 and 43 pixels) because that forearm entry sits against the hinge. Each PNG is under 400 KB.

Pass 3 mockups stay in `Docs/AnimStills/pass3/`.

## Pass 21 (grapple reach, launch V)

The lane's shoulder flare was added on the inward sign. On these keys outward is a negative left yaw and a positive right yaw, and the samples already sit there. Stacking another 24° only makes a wider pose. The inward sign pulls the forearm through the chest and the rope back through the head. `ShoulderFlare` stays 24. It is not stacked, and it is not flipped into a wider spread.

The rope hand is pitched further along the same reach: 0° extra at the fall (−30°) and −32° at the pull (−80°). It eases off as the arm drops. A miss keeps the other arm down, so that reach does not touch it. Printed aim, latch, and pull constants stay. The body pitch the exit fit reads moves with the reach.

Per frame, absolute depth, 0.5 cm limit, joined pieces exempt inside 3 cm of the joint. World depth is 0. Pose fails are 0.

- Grapple latch, 4 frames: pose 0.13–0.18 cm, rig up to 2.35 cm. The lead hand is 10–34 cm in front of the face. Before, at t=0.070, the head met the rope at 1.39 cm and the hand sat 32 cm behind the face.
- Grapple hold, 15 frames: pose 0.15–0.39 cm, rig up to 2.34 cm. Yaw stays ±32. The left hand is 3.3 cm in front of the face and 38 cm in front of the chest. Before, that hand was 35 cm behind the face. The worst frame was already under 0.5 cm, so its before still has no red.
- Grapple release, 4 frames: pose 0.00–0.17 cm, rig up to 1.57 cm. The hand stays in front through the fall. Before, at t=0, the head met the rope at 1.39 cm.
- Grapple miss, 9 frames: pose 0.00–0.42 cm, rig up to 3.40 cm (the off-arm hinge). The throw hand is 33 cm in front of the face. Before, at t=0, the lead upper arm was 1.91 cm inside the chest.
- Launch rise and both windmill frames: pose 0.24 cm, rig 0.89 cm on the rise and 1.18 cm on the mill. Before, both upper arms went through the head at 9.54 cm and the lower arms at 8.91 and 8.79 cm. After, the hands are at x ±0.47 and z 2.07, beside the head and above the crown.

Side stills, grey scene, red only on the pose overlap: `Docs/AnimStills/pass21/`. Each PNG is 1024×576 and under 400 KB.

StrafeJumpSim exits 0. Gameplay lines match the post-merge sim. One animation token moved because the exit fit prints this body pitch: `exit-fit` `grapple=-78` → `grapple=-109`.

## Pass 22 (yank, thin rope, launch 3/4)

The pull the last pass left in the picture was a spider on a pole: hip 40 and spine 42, both arms nearly straight, head −20, and a tuck knee of −94 that still dangled at the hold's downward speed. `ShoulderFlare` stays 24. The reach is still there, but it now ramps in once the higher arm is past −20° and is full at −40°, so a one-arm yank does not pop the latch off the reach.

The hold is a yank. Body rope arm pitch −118, yaw −28, elbow −46. The other arm sits at pitch −12, yaw +68, elbow −28, about 59 cm out to the side. Hip 14 and spine 4, so the chest is 18° off vertical and about 8° off the rope. Head 18, chin up toward the anchor. The rope hand is 10 cm in front of the face and 9 cm above the crown. Thighs 12° and 6°, knees −28° and −18°, feet 27 cm apart and 21 cm behind the hips. The old −94 tuck is −36, so the hold no longer folds both knees up behind the hips.

The cord is 1.2 cm (`RopeDiameter` 0.012). It stays straight while slack is at most 0.08, then eases to a 6 cm vertical drop. The 16 cm bar is gone, and the latched rope no longer falls back to that cylinder. `VerbFxLook.RopeSag` is unchanged, so `fx-verbs` stays put. The side still bows that same 6 cm off the chord, because a vertical drop on a rope 9° off vertical is almost along the rope and does not read from the side.

Launch arms stay the V (pitch −155, yaw 140, roll ±56, elbow −10). `risePitch` stays −155. The rise legs were a 72 cm split with the free foot 37 cm behind the hips. Both legs now drive together: thigh 36, knee −84, roll ±8. Feet are 49 cm apart, 4 cm behind the hips, and the soles sit at z 0.31.

Per frame on the seven grapple and launch clips, absolute depth, 0.5 cm, joined pieces exempt inside 3 cm. World 0. Pose fails 0.

`no-clip clips=7 frames=40 worldMax=0.00 poseMax=0.42 rigMax=3.40 poseFails=0 rigJoint=39`

- Pull, 15 frames: pose 0.01–0.11 cm, rig up to 2.35 cm (hip/thigh). The rope elbow hinge is joined, so it stays in `rigJoint`.
- Release, 4 frames: pose 0.06–0.11 cm. The hold and release stills with a 6 cm sag are world 0.
- Miss is unchanged: worst pose 0.42 cm, rig 3.40 cm on the off-arm hinge.
- Launch rise and mill: pose 0.24 cm. Rig on the rise is 0.73 cm.

The full 40-clip set was not re-run. Exit clips sample this pull through `ForBody`, so their arm channels moved with the yank and were not measured again here.

Stills, dark backdrop and mid-grey floor, key and rim: `Docs/AnimStills/pass22/`. Backdrop pixels are about (89, 94, 101). The floor is about (150, 149, 146). Near-white pixels are under 0.01%. The pull cord is 2 px across and straight. The old cord in the before frame is 8 px across. Hold and release bow about 5 px off that line. The launch 3/4 frame shows both arms. Red overlap pixels are 0 because these frames are already under 0.5 cm. Each PNG is 1024×576 and under 400 KB.

The map lane tip `9219d01` arrived during this pass. It substitutes a straight-arm hang (elbow −2, yaw 120, trunk and legs zeroed) so its own overlap count stays at zero. `ForBody` keeps the yank. The lane's hip lean (−2.5°) and left clavicle lift (12°) stay on the locomotor. The trunk still meets the rope through `LineFix`, so a level rope does not stack a second fold on the cuff.

StrafeJumpSim exits 0. Gameplay lines match the pass 21 sim. Animation lines that moved:

- grapple pose: `pullPitch=-78/-84 pullYaw=32/-32 pullElbow=-2/-4 pullSpine=42 pullHip=40 pullHead=-20 strideThigh=20/-30 tuckKnee=-94 pullRead=-84` → `pullPitch=-118/-12 pullYaw=28/-68 pullElbow=-46/-28 pullSpine=4 pullHip=14 pullHead=18 strideThigh=12/6 tuckKnee=-36 pullRead=-12`
- grapple-pose-polish: `pull=two-hand-line pullPitch=-78/-84 pullChest=82 pullZ=0.73/0.73` → `pull=yank pullPitch=-118/-12 pullChest=18 pullZ=0.18/0.02`
- rope: `width=0.16 halo=0.30` → `width=0.01 halo=0.02` (the constants are 0.012 and 0.016; the line prints two decimals)
- body-line: `rope=60.0>0.0` → `rope=4.0>0.0`
- handoff2: `grappleOut=13.0>5.9` → `grappleOut=21.2>9.6`
- exit-fit: `grapple=-109` → `grapple=-118`

## Pass 23 (trail knees, free arm, full clip set, exit stills)

The full 40-clip set was re-run before the re-key and again after it. Both times:

`no-clip clips=40 frames=337 worldMax=0.00 poseMax=0.49 rigMax=3.40 poseFails=0 rigJoint=330`

Pose max is the punch at 0.49 cm. Nothing was over 0.5 cm, so nothing was straightened to clear the check. Grapple arrive and release now sample the live yank through `ForBody` for the first 22% of the clip. Their worst pose is 0.16 cm. The other exits stay the authored recoveries. The live poses they join (wall run, vault, zip, launch, fall) are already in the same 40.

The pull at the hold speed was still a plank on the toes: knees about 30° on thighs of 15° and 10°, feet 21–25 cm behind the hips and 89–91 cm below them, and the free hand 8 cm below the hip. Both knees on the played pull are now 35° and 36°. The feet trail 50 cm and 56 cm behind the hips and sit 13–17 cm higher. The free arm swings forward and up: 68 cm in front of the chest and 45 cm above the hip, elbow bent, still out to the side. The rope hand stays just above the head. `ShoulderFlare` stays 24. The extra reach stays off while the rope hand is past −100°, so raising the free arm does not pop the rope hand.

The map lane was not merged. It had moved past `9219d01`.

Stills, same dark backdrop and mid-grey floor: `Docs/AnimStills/pass23/`. Sky about (89, 94, 101), floor about (150, 149, 146). Pull before and after, plus one 3/4 still for each movement exit. The roll still is the shoulder plant with the existing 135° bank. Red pixels are 0. Each PNG is under 400 KB.

StrafeJumpSim exits 0. Gameplay lines match pass 22, including `exit-fit` `grapple=-118` and `handoff2` `grappleOut=21.2>9.6`. Animation lines that moved:

- grapple pose: `pullPitch=-118/-12 pullYaw=28/-68 pullElbow=-46/-28 strideThigh=12/6 strideKnee=-28/-18 tuckThigh=16 tuckKnee=-36 pullRead=-12` → `pullPitch=-118/-68 pullYaw=28/-52 pullElbow=-46/-38 strideThigh=18/8 strideKnee=-32/-24 tuckThigh=52 tuckKnee=-40 pullRead=-68`
- grapple-pose-polish: `pullPitch=-118/-12 pullZ=0.18/0.02` → `pullPitch=-118/-68 pullZ=0.18/0.24`

## Pass 24 (climb crouch, vault landing, slide pump, exit strips)

The grapple pull from pass 23 stays locked. Bent knees, the free arm forward, the rope hand clean. `GrapplePose` was not edited. The smooth-motion lane was not merged.

Climb top-out is a low crouch on the lid. Both thighs and both knees match, so the soles share one height, 26 cm under the hips, and both feet sit under the hips (about 1 cm fore-aft, 14 cm to each side). One hand stays on the lip. The other reaches forward (19 cm to 44 cm in front of the hips). The chest does not stand up: chest stays about 20 cm in front of the hips. Opening a knee or unfolding the hip drops that foot through the lid, which the checker still rejects. Hips are 31.5 cm above the box. Worst pose 0.25 cm, world 0.

Vault no longer sits on a box. The trail foot starts high and behind the lead foot, then both feet meet the ground in front of it and the arms pump. A trail thigh past about 54° walks into the spine (58° is 0.50 cm, 62° is 0.73 cm), so the clear is a low one: the drawn lid is 18 cm, and 1281 trail-foot vertices sit over it on the first frame. Both landing feet are in front of that face. Nothing rests on the top. A long run stride would put the heel back on the obstacle, so the arms carry the run and the feet stay short of the face. The dump tests this exit against the ground. A box that follows the hands cannot sit behind the body: the arms do not reach past the hips. Worst pose 0.35 cm, world 0.

Slide rises through a forward lean. The chest goes from 6 cm behind the hips to 14 cm in front. Both arm pitches stay negative, so the pump stays in the line of the run. The widest hand is 34 cm to the side, down from the old arm that stuck straight out. Worst pose 0.33 cm, world 0.

Every exit has a five-frame strip (start, 25%, 50%, 75%, end) in three-quarter view in `Docs/AnimStills/pass24/`. The obstacle is in frame where the clip has one. Each PNG is under 400 KB. The mesh check on all 95 panels found no straddle, no foot inside a lid, and no arm stuck out past 55 cm. Mantle is unchanged and still uses the wide plant; its hips stay about 10 cm above its box.

Full set, same convention, after the re-key:

`no-clip clips=40 frames=337 worldMax=0.00 poseMax=0.49 rigMax=3.40 poseFails=0 rigJoint=330`

StrafeJumpSim exits 0. Gameplay lines match pass 23, including `exit-fit` `grapple=-118`, `closest=54`, and `handoff2` `grappleOut=21.2>9.6`. Animation lines that moved:

- verb-exit: `exit-chain vault-slide=17.6` → `vault-slide=13.5` (budget 96)
- exit-fit: `step=49.7` → `step=49.5` (budget 96)

## Pass 25 (climb stands up, vault pump, mantle plant, hip sit)

Slide was accepted and was not re-keyed. The grapple pull stays locked. The smooth-motion lane was not merged. This is the last pass on this branch.

The climb top-out is a plant on the lid for the whole exit (`VerbExitClock` 0.32 s). The capsule stays standing: `TickClimb` sets standing height 1.80 m, and `TickMantle` never changes height. The stand point is still the ledge hit plus 0.06 m and 0.24 m onto the deck. The hips bone drops inside that capsule. This is not a visual-root offset, and the mesh pelvis is not left at bind height.

Both feet are the support. Thigh 64°, knee 70°, thigh yaw ±26°. `Drop` is 0.178 m on the hips bone. Measured on the hier mannequin, every frame of the exit:

- pelvis 13.3 cm behind the support foot
- hinge 2.25 (hip 18 / spine 8). The chest bone stays at rest
- support knee 70°, in front of the pelvis by 32.8 cm and in front of the ankle by 19.1 cm, so the shin points forward
- pelvis drop 17.8 cm under the standing hips bone
- both soles 0.05 cm above the lid

Pose overlap on that plant is 0.36 cm, under the 0.5 cm line. World contact is 0. The parent-child cuff stays rigJoint.

Vault keeps the trail thigh at 54°. The trail knee stays bent until that thigh is already in front, so the foot does not land behind the pelvis. Plant frames sit 13.2 to 25.2 cm back. The arm pitches go to -78 / -42 and finish at -64 / -22. The widest hand is 42 cm, under 55 cm. Pose 0.35 cm, world 0.

Mantle drops the wide plant and the 3 cm visual drop. Both feet stay on the lid for all 10 frames (0.28 s). Pelvis sits 9.9 to 13.2 cm behind them. Hinge 3.0 to 3.5. The lid is the sole, not the hand box, so the hips are about 87 cm above the deck rather than 10 cm above a box around the hands. Pose 0.18 cm, world 0. Same capsule numbers as the climb: bottom at the stand point, height 1.80 m, standing pelvis at bottom + 1.05 m.

`hip-sit clips=1 fails=0 pelvisBackMin=13.3 hingeMin=2.25 kneeMin=70 pelvisDropMin=17.8`

That line is the climb plant only. Both feet stay down for all 11 frames, so there is no cruise in this exit. The previous combined line for the old climb, vault, and mantle keys was `hip-sit clips=3 loadedFrames=31 pelvisBackMin=9.0 cm hingeMin=2.50 fails=0`. Vault and mantle stay with the movement lead.

Full set, same convention:

`no-clip clips=40 frames=337 worldMax=0.00 poseMax=0.49 rigMax=3.40 poseFails=0 rigJoint=330`

StrafeJumpSim exits 0. `grapple=-118`, `closest=54`, and `handoff2` `grappleOut=21.2>9.6` are unchanged. `exit-fit` `step=49.5` is unchanged. One animation token moved: `exit-chain vault-slide=13.5` → `vault-slide=18.3` (budget 96).

Stills are in `Docs/AnimStills/pass25/`. Five-frame strips for every exit, a side strip of the climb, and a full-size side still before and after for the climb, the vault landing, and the mantle. Each before/after still has a vertical line through the support foot and a dot on the pelvis. Each PNG is under 400 KB.

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
