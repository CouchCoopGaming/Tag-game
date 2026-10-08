# Animation and FX plan

Pass 1 is the exit poses and the terminal landing roll. Pass 2 deepens those poses. FX waits until pass 3. Exits and the roll stay visual: they do not change speed, stun, coyote, cling, slide, or the camera.

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

## FX queue

FX waits for pass 3. Do not start it until the animation depth above is in.

### 1. Running dust clouds

First FX item, after pass 2. Foot dust while running, pooled, no allocations on the hot path.

- Size, color, and density scale with planar speed. A walk is a light puff. A sprint is a thicker cloud.
- Surface picks the look: grass, dirt/sand, concrete/asphalt, wood, metal, wet. Use the existing footstep surface names where they already match, and add dirt/sand, asphalt, and wet as labels on top of that map.
- One pooled emitter per pawn. Emit from the pool. Do not allocate in Update or LateUpdate.
- Reduced flashing (GameSettings) turns the clouds down or off.
- Visual only. Do not change walk 6.9, sprint 13.8, or the footstep timing.

### 2. Comic contact words

After the running dust. Classic comic bursts on punch and tag contact.

- Words: POP!, POW!, BAM!, WHAM!
- Pick at random. Do not repeat the same word on the next contact.
- A punch favors the smaller words (POP!, POW!). A successful tag favors the bigger words (WHAM!, BAM!).
- Look: bold outlined letters on a spiky starburst, a slight tilt, a quick scale-pop (about 0.05 s in), then hold and fade. Whole life about 0.4–0.5 s.
- World space at the contact point. Billboard to each split-screen camera so every pane can read it at couch distance.
- Pooled. No allocations on the hot path.
- Honor reduced-motion / reduced flashing, and add an on/off toggle.
- No paid fonts and no purchased art. Use a font already in the project or a Unity built-in, or draw the letters as sprites.
- Visual only. No hitstop, no camera shake, no change to punch reach 1.55 or to tag timing.

### Later

- Speed lines.
- Wall-scrape sparks.
- Tag hit burst (separate from the comic word).
- It-handoff flash.
- Pad and zip trails.
- Wire the wall-run foot scuff and the vault hand-plant puff. The pool already has those two burst kinds. They are not played yet.
