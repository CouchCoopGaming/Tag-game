# Decisions for Landon

Open questions only Landon can close. Each one has a recommendation. Until he answers, lanes follow the recommendation and do not invent a third option. Design does not ping him. Ororo does.

## 1. LT rope versus the stutter-step button

The couch rope is already on the pad. Commit `dabc9782` on the play tip gives every human seat the existing grapple. Keyboard fire is right mouse. Pad fire is the left trigger (`BindSampler.LeftTriggerHeld`, `PlayerInputReader` around line 388, `CouchRope.ProofLine` `fire=RMB,LT,LT,LT`). Right trigger is free. Nothing on the play tip reads it.

The menu lane has caught up on the live row. PR #121 head `0d3b9d74` prints Grapple as RMB / LT (`ActionBinds.GrapplePadDefault` is `leftTrigger`, `Docs/Controls.md` on that branch). RT is shown as free. Pass 48 of `Docs/UiPlan.md` and `Docs/WhatsNew.md` still contains the old sentence "Not on pad yet". That is the pass log, not the screen. PR #126 was merged into #121 at `35a535d9` on 2026-10-09 00:18 UTC. Do not unmerge it. The absorb kept the Hier idles, the 1.8 m results body, and the LT column.

Evasion (PR #130, head `65aa8de0`) keeps LT on the rope and, behind `EvasionMoves.Enabled` (default false), uses a right-trigger double-tap inside 0.25 s for the stutter. Juke, spin, and the dive flick use the right stick, which is look. Keyboard keys are proposals only.

**Recommendation.** Keep LT as the couch rope. Leave the evasion flag off. Leave RT free until you name the stutter button. Do not put stutter on LT, LB, RB, A, B, or X. Those are the rope, sprint, air dash, jump, slide, and punch. If you want stutter on RT, say so and the controls table will change. Until that sentence exists, a single press of RT does nothing, and the controls screen says the rope is LT.

## 2. Binding the hip and ankle clearance rig

The player still uses the current Hier rig. PR #128 (head `b804954f`) measured a clearance candidate and did not bind it. `Docs/Models/PLAYER_QUEUE.md` on that head says the shipped mannequin is untouched and nothing is bound into #118. `Tools/Tag/clear_hip_flex.py` is not exported over the candidate. `Docs/LocoStills/pass6/hip-range.txt`:

- Current hip, at rest, is already past the 0.5 cm pose limit: Hips inside the thigh 3.99 cm, thigh inside the hips 1.33 cm. That depth is `rigJoint`. It stays over through 130° of command.
- Current knee, at rest, is already past the limit (thigh inside the shin 1.3 cm) and stays over through 160°.
- Candidate hip is clear at rest, then fails at 9° of command (7° of bone flexion) on the worse leg. It is clear again at 110° and at 130°.
- Candidate knee is clear through 153° (bone flexion 151°). At 155° the shin enters the thigh by 0.97 cm.
- The comparison pose is hip flexion 110°, knee flexion 70°, spine at rest. On the candidate both hinges are 0 cm and the pelvis is 81 cm behind the foot. On the current rig that pose leaves the hip hinge at 3.94 cm.

Stills: `Docs/LocoStills/pass6/hip-hinge-current.png` and `hip-hinge-candidate.png`.

Poses on the play tip, the animation lane, and the costume lab are authored on the current rig. They report the rest overlap as `rigJoint` and require pose to be 0. The costume lab (`Docs/Characters/CostumeBrief.md` on #131) says the hip and thigh rest overlap is untouched, and the lab is not in the player build.

**Recommendation.** Do not bind the candidate into the player, the menu, or the costume lab until you have looked at those two stills and said yes. Lanes keep authoring on the current rig, keep `rigJoint` and pose as separate numbers, and do not lift a pose to hide the 3.99 cm rest overlap. After you say yes, A2 binds one rig, and every lane re-measures plants against the hip-sit rule on that rig. Not before.

## 3. Default seat paint versus the colour-blind palette

The seat identity is P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. On #121 `0d3b9d74` the menu seats and `PlayerGlyph` (`● ▲ ■ ◆`) match that table. The play tip's default accessibility palette does not. Palette 0 in `Accessibility.cs` on #118 `5d4cd74b` is yellow, green, white, and cyan, and the glyphs on the play tip are circle, square, triangle, diamond. The menu copy of palette 0 is still that measured row; the comment says seat chrome uses the costume bodies instead.

The menu lane measured the seat hues under colour-blind simulation and they sit close (red against orange, blue against lavender). Shapes are what separate them. The five accessibility palettes are a setting, not the default marks.

**Recommendation.** The default, with the colour-blind setting off, is the four seat colors and the four shapes. Turning a colour-blind palette on replaces the paint for that seat and keeps the shape. Do not leave yellow, green, white, and cyan as what a new couch sees. Confirm or override this and the play tip and the menu will use one table.
