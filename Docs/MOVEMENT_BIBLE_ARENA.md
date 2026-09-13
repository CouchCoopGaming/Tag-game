# TAG ARENA MOVEMENT BIBLE
## Apex Legends parkour × Tribes: Ascend skiing
### For a first-person / readable-body tag arena

---

## 0. What this package is

This is a complete locomotion contract, not a pile of isolated abilities.

Apex without Tribes is a tight, ground-greedy parkour language: slide to keep speed, climb to steal height, bounce to redirect, glide to turn a mantle into a launch.

Tribes without Apex is a terrain language: gravity is a battery, skis are a switch that tells the ground “do not eat my speed,” jets are a tax you pay to keep that speed over the next crest.

Together they make a tag arena feel like a hunt through a living slope, not a corridor shooter with extra jump buttons.

All numbers live in `MovementConfig`. The motor never hard-codes balance. Tune the ScriptableObject, not the if-statements.

Default bindings used by `PlayerInputReader`:

| Action | Key |
|---|---|
| Move | WASD |
| Look | Mouse |
| Jump / climb / bounce | Space |
| Crouch / slide / super-glide | C or Left Ctrl |
| Ski | Left Shift |
| Sprint (when not skiing) | Left Alt (Shift is ski) |
| Jet | Right Mouse |
| Tagger lunge | Middle Mouse |
| Tap-strafe pulse | W tap while holding A/D in air |

Rebind in the inspector. If you want Shift = sprint like Apex, set ski to Q or Mouse4 and flip `useShiftAsSprintWhenNotSkiing`.

---

## 1. The law of Flow

Treat momentum as a conserved resource with named leaks.

```
Flow  =  horizontal speed you actually get to keep
Leaks =  walk friction, uphill gravity, wall slams, land stun, mantle lock
Batteries =  downhill gravity, slide boost, jet, bounce redirect, super-glide
```

Rules the motor obeys:

1. Never zero horizontal velocity to “enter a state.” States decorate velocity.
2. Ground walking is the only high-friction mode.
3. Slide, ski, air, and jet all use wish-acceleration (Quake-style). You add speed along a wish vector. You do not point velocity at the stick.
4. Authored cinematic windows (mantle warp, super-glide launch) are the only places velocity is overwritten.
5. Climbing clears jump fatigue. Slamming a wall face-first at ski speed reflects and bleeds. Glancing a wall becomes a bounce or a wall-run.

If a change violates rule 1, it will feel like Titanfall 2 on a bad day or like Fortnite sprint cancel — snappy, but not this game.

---

## 2. Two bloodlines, one body

### Apex bloodline (the knife)

- Sprint cap ~7.6 m/s. Not fast. Fast is what you *build*.
- Slide requires a speed gate (~5.6 m/s). Sliding from a walk is a crouch, not a slide.
- Slide on flat ground is a decaying boost. Slide on a downslope is an engine.
- Slide-jump in the first ~0.24s keeps almost all horizontal speed and is the basic traversal verb.
- Jump fatigue: a second jump inside 0.15s is tiny (~3.1 m/s up). Full jump returns at 0.75s. This kills bunny-hop dominance and makes wall contact valuable, because climb clears fatigue.
- Climb any near-vertical for up to ~6 m. Ledge in range becomes a mantle.
- Wall bounce = enter climb and leave it inside the green window (first ~0.04–0.22s). Redirect + height.
- Super-glide = jump (+ crouch) at mantle peak. Velocity is rewritten to ~10.3 m/s along wish/look.
- Landing from a true fall above ~28 m/s down applies a short land stun. Ski kisses do not.

### Tribes bloodline (the river)

- Ski held = friction collapses as a function of slope. Never quite zero (T1 truth), almost zero on steep faces (T:A feel).
- Gravity is projected along the plane. Downhill you accelerate. Uphill you bleed unless you jet.
- Crests launch you if velocity points off the plane. That launch is not a bug. It is the ski-jump.
- Jet spends energy. While jetting, gravity is almost suspended (T:A’s dirty secret, cleaned up here as `gravityWhileJetting = 0.15`). Vertical thrust plus wishdir thrust.
- Energy regenerates only after a short delay. Runners get a larger tank than the tagger.
- High-speed steering falls off. At 25 m/s you carve, you do not flick.

### Fusion that is *not* in either parent

- You may ski into a slide by releasing ski and holding crouch above the slide gate. Use this to shrink your hurtbox in a tag check.
- You may climb out of a ski-jump if you meet a wall with jump held. Fatigue clears. This is how a runner escapes a slope they overshot.
- Wall-run exists, but it is short (0.85s) and speed-gated. It is a tag-arena connector, not Titanfall tourism.
- Tagger has a tiny sprint bonus and a cooldown lunge. Tagger does not get extra jet. The hunter is slightly better on floors. The prey is slightly better in the sky.

---

## 3. Tag arena implications (design before code)

Tag dies if:

- The tagger can jet-lock onto a runner’s back.
- The runner can loop one ramp forever with no decision.
- Catch radius is larger than a slide can duck.

Tag lives if:

- Every escape route costs a resource (energy, height, a wall you already used).
- Every chase route has a punish (land stun if you drop sloppy, uphill bleed if you ski stupid).
- Vertical slices of the map are readable. Third-person body or a bright trail is mandatory even if the camera is first-person.

Recommended arena grammar:

- Long fall-line spines (Tribes) that dump into broken courtyards (Apex).
- Walls that are 1.2–2.2 m (mantle), 3–6 m (climb/bounce), and long glancing faces (wall-run).
- One “church roof” per map: a high perch you can only hold with leftover jet. Holding it is loud. Leaving it is a dive.
- No invisible walls on crests. If the ski wants to launch, let it launch.

Catch rules already in `TagRole` + motor:

- Sphere overlap at chest, radius 1.15 m.
- 0.8 s i-frames on the new runner so instant ping-pong tags die.
- Tagger lunge 14 m/s for 0.18 s, 1.6 s cooldown. This is a *commit*, not a magnet.

---

## 4. State machine

```
Idle ↔ Walk ↔ Sprint
        ↓ crouch+speed
      Slide ──slide-jump──► Air
        ↓ release / slow
      Crouch

Ground + ski ──► Ski ──crest / release──► Air
Air + jet    ──► Jet ──energy empty──► Air
Air + wall face + inward ──► WallClimb ──ledge──► Mantle
                           └──green jump──► Air (bounce)
Air + wall glance + speed ──► WallRun ──time/jump──► Air
Mantle peak + jump[ + crouch] ──► Air (super-glide)
Air + hard landing ──► LandStun ──► Idle/Walk
```

`MoveState` is an enum the animator reads as `State` (int). Do not add states for cosmetics. Cosmetics are triggers.

---

## 5. Physics primitives (implementer’s contract)

### 5.1 Wish acceleration

```
current = dot(velocity, wishDir)
add     = wishSpeed - current
if add > 0: velocity += wishDir * min(accel * dt * wishSpeed, add)
```

This is why tap-strafe works. A forward pulse while velocity faces sideways donates speed into the new wish. The code exposes this as `TapForwardPulse` (rising edge of W). Bind mousewheel to W in your input layer if you want Apex-authentic finger memory.

### 5.2 Ski step

```
v += gravity * skiGravityScale * dt
v  = project(v, groundNormal)
v  = friction(v, skiFriction * slopeDecay)
v  = accel(v, project(wish, groundNormal), steer * speedFalloff)
if dot(v.normalized, normal) > launchDot: leave ground
```

### 5.3 Slide step

Same projection as ski, but:

- entry adds `slideBoost` scaled by entry speed
- flat friction is high
- downhill accel exists
- collider height snaps to crouch

### 5.4 Climb

Kinematic-ish: overwrite velocity with up-speed + side-speed + stick. Height budget from attach point. Ledge probe wins and starts mantle.

Green window is time-on-wall, not height. Jump inside it to bounce. Stay longer and you are just climbing.

### 5.5 Mantle

Bezier-ish two-segment warp from current pos → raised mid → stand point + forward. Super-glide window opens at u ≈ 0.62 for `superGlideWindow` seconds (default 55 ms). That window is supposed to be tight.

### 5.6 Jet

```
energy -= drain * dt
v.y   += jetUpForce * dt
soft-cap v.y
wish-accel on horizontal
gravity *= gravityWhileJetting
```

---

## 6. Full animation guide

You do not need 80 clips. You need 22 clips and a disciplined parameter sheet. Everything else is blending.

### 6.1 Animator parameters (already hashed in `AnimIds`)

| Name | Type | Meaning |
|---|---|---|
| State | Int | `MoveState` |
| Speed | Float | horizontal m/s |
| VertSpeed | Float | y velocity |
| Grounded | Bool | probe |
| Ski | Bool | ski engaged and grounded |
| Jet | Bool | thrusting |
| Slide | Bool | |
| Crouch | Bool | crouch or slide |
| MoveX / MoveY | Float | raw stick, -1..1 |
| Energy | Float | 0..1 |
| WallLeft | Bool | wall-run / climb side |
| Slope | Float | degrees |
| Jump | Trigger | left ground by jump |
| Bounce | Trigger | wall bounce or wall-run jump |
| Mantle | Trigger | mantle started |
| SuperGlide | Trigger | glide launched |
| Land | Trigger | land stun |

### 6.2 Clip list (body)

Make these as *in-place* clips. The motor owns translation. Root motion on these will fight the rigidbody and you will hate your life.

**Ground cycle**

1. `Idle` — 2.5s, micro-shift weight. Tag arena idle should look ready, not lobby-AFK.
2. `Walk` — 1.0s cycle, 4.4 m/s authored.
3. `Sprint` — 0.75s cycle, 7.6 m/s authored. Arms pump. Head stable.
4. `CrouchWalk` — 1.1s, low weapon / low shoulders.
5. `CrouchIdle`

**Slide / ski / jet**

6. `Slide_Enter` — 0.12s, hips fire forward, off-hand plants toward ground.
7. `Slide_Loop` — 0.4s, looping. Spine twist follows `MoveX`.
8. `Slide_Exit` — 0.16s.
9. `Ski_Loop` — 1.2s. Knees flexed, torso quiet. This is not a slide. Slide is a compact; ski is a stance.
10. `Ski_CarveL` / `Ski_CarveR` — additive 0.3s, driven by `MoveX`.
11. `Jet_Loop` — 0.5s. Knees slightly extended, off-hand back, heat at calves/pack.
12. `Jet_Ignite` — 0.08s additive punch.

**Air**

13. `Jump_Takeoff` — 0.18s.
14. `Air_Up` — pose, blend by `VertSpeed` > 0.
15. `Air_Down` — pose, blend by `VertSpeed` < 0. At high `|VertSpeed|` tuck slightly (readable dive for tag).
16. `Land_Soft` — 0.2s.
17. `Land_Hard` — 0.35s, used by LandStun.

**Parkour**

18. `Climb_Loop` — 0.45s, hand-over-hand. Play rate scales with `climbSpeed`.
19. `Mantle` — 0.42s, matches `mantleDuration`. First 55% is the pull, last 45% is the hop-over. Mark an event `MantlePeak` at 62% if you want an extra VFX hook; the motor does not need it.
20. `WallBounce` — 0.22s, kick the wall, spine opposite the normal.
21. `WallRun_L` / `WallRun_R` — 0.85s max. Select with `WallLeft`.
22. `SuperGlide` — 0.28s, flat body, one frame of crouch in the hips even if the capsule stays standing.

**Optional but worth it**

- `Lunge` for the tagger (0.18s).
- `Tagged` hit-react (0.2s), no root motion.
- First-person arms versions of 6–8, 11–15, 18–22. Body can be a shadow/child.

### 6.3 Blend trees

**Locomotion 2D (Grounded && !Slide && !Ski)**
- X = MoveX, Y = Speed
- Thresholds: Idle 0, Walk 2.5, Sprint 6.5

**Air 1D**
- VertSpeed from +10 to -30
- Jump_Takeoff is a short overlay on trigger, not a branch you get stuck in

**Ski 1D additive**
- Base Ski_Loop
- Additive carve from MoveX

**Slide**
- Enter → Loop (on State==Slide) → Exit when State leaves Slide

### 6.4 Layers

| Layer | Weight | Mask | Why |
|---|---|---|---|
| Base | 1 | All | locomotion |
| Parkour overlay | 1, override | All | climb, mantle, bounce — full-body |
| Additive upper | 0.4 | Spine+arms | jet ignite, look-at, tag point |
| First person arms | 1 | Arms only | if using a body+arms split rig |

Never put slide on an additive layer. Slide changes the silhouette. Tag reads silhouette.

### 6.5 Timing contract (animator vs motor)

The motor does **not** wait for animation events to leave mantle or bounce. Waiting for events makes online tag feel like you rubber-banded on every ledge.

Animation follows the motor:

- Mantle clip length = `cfg.mantleDuration`
- Climb loop is cosmetic
- Bounce trigger fires *after* velocity is already redirected

If you want motion warping toward hand plants, warp the *mesh* with IK, not the capsule.

### 6.6 IK

- Foot IK on Walk/Sprint/Ski only. Off during slide, air, climb, mantle.
- Hand IK during climb: two probes on the wall 0.4 m apart vertically, refresh every 0.2 s.
- Look IK: slight spine toward velocity at ski speeds > 16 m/s so the body leads the camera. Do not rotate the capsule; the camera owns yaw.

### 6.7 First-person vs tag-readable body

Run both.

- Camera is first-person (`FpsMoveCamera`).
- A body mesh sits on the capsule with the head bone scaled to 0 (or a shader clip). Other players always see a full body.
- If you ship third-person, parent the camera 3.2 m back, 1.4 m up, collision-sphere the boom, keep the same motor. The motor is camera-wish already.

### 6.8 VFX that teach the language

Without these, players will not understand why they are fast.

| Event | VFX |
|---|---|
| Slide start | dust sheet, speed lines 0.2s |
| Ski grounded > 8 m/s | continuous sparks at feet (`skiSparks`) |
| Ski launch off crest | single spark burst |
| Jet | pack/calf trail (`jetTrail`), louder as Energy drops |
| Energy empty | trail dies, a click. This is the “you are mortal again” beat |
| Green-window bounce | sharper kick flash than a normal jump |
| Super-glide | white ribbon for 0.4s along launch dir |
| Land stun | camera dip + grey at edges |
| Tag | both bodies flash, 0.8s shield shimmer on the new runner |

Audio is half the motor. A ski loop that pitch-scales with `Speed` teaches Flow better than any tutorial popup.

### 6.9 Suggested Mecanim structure

```
Base Layer
 ├─ Ground BlendTree
 ├─ Slide SM (Enter/Loop/Exit)
 ├─ Ski BlendTree
 ├─ Air BlendTree
 ├─ Jet
 ├─ LandStun
 └─ AnyState → Climb / Mantle / Bounce / WallRun / SuperGlide
                 (Has Exit Time off, Transition Duration 0.05–0.08)
```

AnyState parkour transitions must be short. 0.2s blend on a bounce makes the kick feel late.

---

## 7. How to play it (the technique chapter)

This is the part you put in a training range, not a tooltip dump.

### 7.1 Ground grammar (Apex)

**Slide-jump.** Sprint, crouch, jump before 0.24s. You just bought airtime at slide speed. This is the default rotate.

**Jump-slide.** Jump first, crouch as you land. Use this when you were walking and need a slide without a sprint ramp.

**Slide-hop chain.** Slide-jump, land, immediately slide again. Fatigue will shrink your jump if you spam. Touch a wall (climb even for a frame) to wash fatigue.

**Edge slide.** Start the slide at a lip so the last frames hang in air. Air drag is lower than flat-slide friction. This is how you keep Flow across a courtyard.

### 7.2 Wall grammar (Apex)

**Climb.** Look at the wall, hold jump + forward. Six meters. If a ledge exists, you mantle.

**Wall bounce.** Slide-jump into the wall at ~45°, release forward, look nearly perpendicular, tap jump the instant you attach. Green window. You leave with height and away-speed.

**Wall-skip.** Same attach, keep holding forward. You stay close to the wall and can pop a low obstacle without a full mantle.

**Super-glide.** Mantle. At the peak (the body “settles”), jump and crouch in the same breath. Default window is 55 ms. Bind crouch next to jump. Directional glide: release W during the climb, hold A/D, then hit the window.

**Wall-run.** Glance a long wall in air above 6 m/s with forward held. You get less than a second. Jump off early. Late wall-run is free height for the tagger to read.

### 7.3 Terrain grammar (Tribes)

**The sentence.** Ski down. Jet the uphill. Land on the next down. Repeat.

If you ski an uphill you are paying gravity twice (once to climb, once because you did not convert to height). Jet at the *base* of the uphill so your horizontal Flow becomes altitude, then fall onto a downslope so altitude becomes Flow again.

**Edging.** On ski, A/D carves. At low speed the carve is tight. At high speed it is a long arc. Plan the arc before the crest.

**Crest jump.** Do not fight the launch. Look where you want the flight to go, tap jet to flatten, ski the landing slope. Landing steep-on-steep keeps Flow. Landing steep-on-flat slams and may stun.

**Energy discipline.** Empty jet in a tag arena is a death sentence. A good runner arrives at a fight with 40%+ tank. A greedy runner arrives empty and gets lunged out of the sky.

### 7.4 Combined sentences (the actual game)

**Slope escape.** Ski a spine, crest-launch, wall-bounce the first high wall, super-glide the next ledge, land in a slide. You just changed three axes. The tagger has to pick which verb to chase.

**Slope ambush (tagger).** Do not jet-mirror the runner. Cut the landing. Lunge is for the moment they land-stun or start a mantle (mantle is the only long lock in the kit).

**Fake mantle.** Start climb, bounce out the green window back the way you came. This is a peek. In tag it is also a whip-back into the hunter.

**Low-gate.** Slide under the tag sphere. The sphere is at chest. A slide puts your chest where your hips were. This is why slide exists in a tag game, not just because Apex had it.

---

## 8. Unity setup (do this once)

1. Create a layer `Geometry` and put maps on it. Set `groundMask` and `wallMask` to that layer. Do not include the player.
2. Player object:
   - CapsuleCollider (height 1.8, radius 0.38)
   - Rigidbody (mass 80, freeze rotation, interpolate, continuous dynamic, gravity OFF)
   - `PlayerInputReader`
   - `SurfaceProbe`
   - `PlayerMotor`
   - `MoveAnimDriver`
   - `TagRole`
3. Child `CamRig` → `Pitch` → Camera. Put `FpsMoveCamera` on CamRig. Assign pitch, camera, motor.
4. Assign `MovementConfig` asset to the motor and camera.
5. Project Settings → Time → Fixed Timestep `0.02` (50 Hz) or `0.01666`. Do not run this motor in Update.
6. Physics default solver iterations 8 / 2. Bounce threshold 2. The motor does its own bounce logic.
7. To feel it *today* with no art: empty scene, add `MovementBootstrap`, press Play.

Network note: the motor is client-authoritative in this package. For multiplayer, send input + a state hash, run the same FixedUpdate on the server, reconcile horizontal velocity. Do not interpolate climb/mantle — snap those.

---

## 9. Tuning order when it feels wrong

Always change one family at a time.

**Feels sluggish on ground.** Raise `sprintSpeed` or `groundAccel`. Do not raise `airAccel` to fix ground.

**Slide feels like ice skating.** Raise `slideFlatFriction`. If downhill is also weak, raise `slideDownhillAccel` separately.

**Ski does not accelerate.** Check you are actually on a slope > `skiMinSlope`, and that `SkiHeld` is true. Then raise `skiGravityScale` toward 1.2.

**Ski steers like a hovercraft at 30 m/s.** That is the falloff working. If you want more carve, raise `highSpeedSteerFalloff`.

**Jet hovers forever.** Raise `jetDrain` or lower `jetEnergyMax`. Tag needs scarcity.

**Mantle eats people.** Shorten `mantleDuration` toward 0.32, or give the tagger a melee that hits during mantle.

**Super-glide is free.** Shrink `superGlideWindow` toward 0.03. Do not lower `superGlideSpeed` first — the joy is the speed; the skill is the window.

**Wall bounce is inconsistent.** Players are holding W. The green window still works, but away-speed shrinks if they stay aimed into the wall. Teach “release forward.”

**Tagger cannot catch anyone.** Raise `taggerSprintBonus` a hair, or shrink runner `jetEnergyMax` bonus, or add one extra floor path on the map. Do not enlarge `tagRadius` past 1.3.

**Runner cannot live.** Opposite. Also check land stun — too much stun turns every dive into a cutscene.

---

## 10. File map

```
TagArenaMovement/
  Docs/MOVEMENT_BIBLE.md          this file
  Docs/SETUP.md
  Scripts/Core/MoveState.cs
  Scripts/Core/MovementConfig.cs   ScriptableObject
  Scripts/Core/PlayerMotor.cs      the whole verb set
  Scripts/Core/MovementBootstrap.cs play-mode arena
  Scripts/Input/PlayerInputReader.cs
  Scripts/Detection/SurfaceProbe.cs
  Scripts/Systems/WishAccel.cs
  Scripts/Camera/FpsMoveCamera.cs
  Scripts/Animation/MoveAnimDriver.cs
  Scripts/Tag/TagRole.cs
```

That is the entire runtime. No hidden singleton. No tick manager.

---

## 11. What not to add until the verbs are honest

- Grapples. They collapse both bloodlines into a single button.
- Double jump. Fatigue exists specifically so jump is not a spam.
- Infinite wall-run. It deletes the climb/bounce decision.
- Air dash that ignores wish-accel. You already have tap-strafe and jet.
- Crouch-spam head bob as a combat tool. Tag already has a slide duck.

Add a zipline only when the arena has a dead vertical you refuse to fill with climbable geometry. When you do, treat it like Apex: mount is a lock, jump-off is a launch, and that launch must feed ski/slide, not reset speed.

---

## 12. The feeling you are chasing

A runner on a good line should feel slightly too fast for their own camera, still in control if they edge, dead if they panic-jet.

A tagger on a good line should feel like they are closing a door, not chasing a particle.

If both players hit the same ramp and the one who jetted later and landed cleaner is ahead, the fusion is working.

If the one who pressed more buttons is ahead, you have an input combo game. Tighten windows, restore conservation, try again.
