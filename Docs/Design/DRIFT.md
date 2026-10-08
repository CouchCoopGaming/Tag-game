# Drift samples

Time samples of the open lane heads against `Docs/Design/DIRECTION.md`. Each entry is the lane, the PR, the head SHA, what matches, what drifts, the severity, and the one-line correction Ororo can send. Design does not instruct the lanes. Ororo relays.

Sample set for pass 1: open PRs #118 and #120–#131. Heads were read 2026-10-08 after `git fetch`. #119 is an older playtest-prep draft (2026-10-06, base `cursor/qa-sweep-3-57aa`) and is not a current department. Open drafts #52–#117 are the stack already converged into #118. They were listed, not re-sampled pose by pose.

Severity: **blocker** (a player would learn the wrong verb or the wrong body), **should-fix** (the lane is off the standing rule), **nit** (copy or a name).

## 2026-10-08 — pass 1

### A1 Movement — PR #118 — `b6c7a71c2afcb3b285d15207ba708fbc63a6c7a9`

Latest: `b6c7a71c` re-keys the roll so the tuck clears the thighs (pose on the clip 0.43 cm). Before that, `57697029` sits the vault and the roll on the hips.

Aligned: this is the play tip the locks are quoted from. Coyote 0.10, buffer 0.16, cling grace 0.08, jump speed 24.7, terminal 56.16, roll at 65% of terminal, walk 6.9, crouch 3.68, sprint 13.8, root motion off. Couch rope is LT / RMB (`dabc9782`, `CouchRope.ProofLine` `fire=RMB,LT,LT,LT`). RT is unread. Air steer and bunny-hop bleed-skip are in `KinematicStep`. Roll still clears under the 0.5 cm pose limit (`Docs/SmoothMotionAudit.md` pass 23, deepest pose hit 0.43 cm).

Drift:

- **Should-fix.** Seat marks on the play tip are not the seat identity. `Assets/Scripts/Settings/Accessibility.cs` `PlayerGlyph` is `● ■ ▲ ◆` (circle, square, triangle, diamond), so P2 is a square and P3 is a triangle. Direction is P2 triangle, P3 square. Default palette 0 colors are yellow, green, white, cyan, not red, blue, orange, lavender. `Docs/CouchPlay.md` still says blue / amber / purple / white.
- **Should-fix.** `Docs/Controls.md` has no rope row. `Docs/KnownIssues.md` and `Docs/PlaytestChecklist.md` still say couch pawns do not get the grapple. The motor gives it to all four human seats.
- **Nit.** `README.md` and `Docs/MOVEMENT.md` still publish the old party numbers (walk 5.5, sprint 9, coyote 140 ms). `Assets/TagArenaMovement/Docs/MOVEMENT_BIBLE.md` still specifies a Rigidbody and FixedUpdate.

Correction: Make the default seat marks P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond, and add the rope to `Docs/Controls.md` as RMB / LT with RT free. Update `KnownIssues` and the playtest checklist so couch humans have the rope and AI does not.

### C1 Animation — PR #120 — `176983ee76e5a38dc6dcd24d22e4a1f16cc261e8`

Latest: `176983ee` stands the climb and mantle on the lid and sits the hips back. Before that, `29fe7ce0` re-keys the climb crouch, the vault landing, and the slide rise. Stills: `Docs/AnimStills/pass25/`.

Aligned: feel locks are not retuned. Reported `hip-sit clips=3 loadedFrames=31 pelvisBackMin=9.0 cm hingeMin=2.50 fails=0` on the plant frames. Vault plants sit 13.2 to 25.2 cm back. Mantle plants sit 9.9 to 13.2 cm back. Pose 0.35 cm on the vault and 0.18 cm on the mantle. Root motion stays off. The side still `exit-ClimbTopOut-after.png` shows the pelvis behind the support foot.

Drift:

- **Should-fix.** The climb plant does not drop the pelvis. `Docs/AnimFxPlan.md` pass 25 says the capsule is already standing, the mesh pelvis stays at bind height (89.7 to 89.9 cm above the bind sole), and "No Drop." The same note says a knee-over-toes sit would need the pelvis lower, and it was not faked with a drop. The standing rule is an 8 cm pelvis drop on a plant, with the support knee bent at least 25° and the shin forward. The report itself says the support knee stays a few centimeters behind the pelvis.

Correction: Drop the climb-plant pelvis at least 8 cm, bend the support knee at least 25° with the shin forward, and keep the 8 cm of pelvis behind the foot. Do not hold the standing bind height to avoid the drop.

### D1 UI — PR #121 — `b89c5acd156b53f209138de690a01f5a27fa5c8d`

Latest: `b89c5acd` puts a seat figure on each results card. Before that, `4f086728` keeps the win target inside the round count. Stills: `Docs/UiStills/pass50/controls.png`, `Docs/UiStills/pass50/results.png`, `Docs/UiStills/pass48/options-controls.png`.

Aligned: the results header is RESULTS (`MenuHost` title "  RESULTS", `Docs/UiPlan.md` pass 50). `MenuTheme.BandKey` is Red, Blue, Orange, Lavender. `MenuMannequin.Shape` is circle, triangle, square, diamond, and the #121 copy of `Accessibility.PlayerGlyph` was reordered to `● ▲ ■ ◆`. Pass 49 names the results cards as P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Jump stays Space / South. Air dash is Q / RB, sprint is Shift / LB.

Drift:

- **Blocker.** The controls screen says the rope is not on the pad. `Assets/Scripts/UI/Menu/MenuHost.cs` around line 4779 sets an empty pad token to "Not on pad yet". `Docs/Controls.md` on this branch: "A pad button for grapple is unbound. The controls row reads 'Not on pad yet'." `Docs/UiPlan.md` pass 48 repeats it. The stills `Docs/UiStills/pass48/options-controls.png` and `Docs/UiStills/pass50/controls.png` show that row. The play tip has bound LT since `dabc9782`. A couch player who reads this screen will not find the rope.
- **Should-fix.** Pass 50 results figures are not the Hier mannequin at one height. `Docs/UiPlan.md`: "Each card is most of the column: the seat-tinted figure stands in it, with that seat's shape on the chest. ... First is a little taller." The still is `Docs/UiStills/pass50/results.png` (flat cards, shape on the chest). Earlier passes rendered `Dummy_Mannequin_*_Hier_Hi.fbx`. First place being taller fights the one 1.8 m body. The proof line still says `rigJoint stays 26`, which is the rest overlap, not a cleared pose.
- **Nit.** The code type is still `MenuPodium` (`Assets/Scripts/UI/Menu/MenuPodium.cs`). The header the player reads is RESULTS. Keep it that way. Older `Docs/UiPlan.md` passes still say the title badges are red, blue, yellow, and green (pass 26). Pass 49 is the seat identity; the older sentences should not be treated as current.

Correction: Change the grapple pad column from "Not on pad yet" to LT, in `MenuHost`, `Docs/Controls.md`, and the controls still. Draw each results figure as the Hier mannequin at 1.8 m, same height on every card, seat shape on the badge.

### B1 Environment — PR #122 — `af9ebd307446e422ebdfdbdd61fe5d8f8965db32`

Latest: `af9ebd30` points the vault proof at the raw mantle sample. Before that, `4f735ba8` merges the vault re-key. Scale still: `Docs/AssetStills/pass28/ranch_scale.jpg`.

Aligned: `Docs/AssetLibrary.md` opens with players at about 1.8 m and vault rails at 0.90–1.05 m. The court note on this head is a 22 × 15 m court with a 3.05 m rim, the backboard face 1.20 m inside the baseline, and "The face-to-rim gap is the real 0.375 m." The ranch scale still puts the mannequin in the doorway. No feel edits in the latest commit.

Drift: none at blocker or should-fix on this head. The court scale break is on the child lanes #125 and #129, not here.

Correction: none. Keep the 0.375 m face-to-rim gap if a child lane sends the court back.

### Motion reference — PR #123 — `4f4ee2c9d96cceaed125200f4c84b516cd38d7b3`

Latest: `4f4ee2c9` sits the tic-tac pelvis behind the support foot where the leg can reach. Before that, `0c05309b` re-keys tic-tac so the wall kick reads. Still: `Docs/HierStills/v080/pass14/hipsit_pose_12_tictac_after.png`. Report: `Docs/HierStills/v080/pass14/pose_error_pass14.txt`.

Aligned: the report states the targets ("Plants and landings want 8 cm. A crouch wants 12 cm."). The capsule is not moved. Where the thigh would enter the spine, the sit stops instead of clipping. Feel numbers are not in this commit.

Drift:

- **Should-fix.** The wall-kick frame does not meet the target. `f=34 t=1.13s pelvisBack=-4.7cm target=8.0cm` (pelvis in front of the support foot). The fail list in that report is long (frames 7, 8, 12–15, 17–23, 27, 28, 30, 32–34, and many more through 167). Several lines say the leg cannot put the sole far enough forward without leaving the floor, or the thigh meets the spine first. Stopping is better than clipping. Shipping the short sit teaches the other lanes the wrong plant.

Correction: On every plant, put the pelvis at least 8 cm behind the support foot (12 cm in a crouch) by dropping the pelvis and bending the support knee, and list a frame only when that sit is actually on the body. Do not leave `pelvisBack` negative.

### Motion reference clips — PR #124 — `d091293ed66e0867cf4eec57f91ad287b918f98a`

Latest: `d091293e` faces the wall run in profile and stands it on a roof. Before that, `942b4755` frames the wall run on the body and seats the plant shin on the brick.

Aligned: the latest commits are camera and contact framing (4 m face, 1.5 m roof). No feel retune in the message. Root motion is not turned on.

Drift: no new measurement of the 8 cm sit was in these two commits. Treat the wall-run plant as unchecked against hip-sit until the next sample.

Correction: On the next pass, print pelvis-behind, knee bend, and pose depth for the wall-run plant the way #123 prints tic-tac. Hold the clip if the pelvis is not 8 cm behind the plant foot.

### B2 Vehicles — PR #125 — `fee26b6759547dc2823d87444f1f41f82330adc4`

Latest: `fee26b67` opens the fastback sail as dark glass. Before that, `3971a850` resamples the sedan loft. Scale still: `Docs/AssetStills/vehicles/sedan_mid_a/pass14/scale.png` (mannequin beside the car).

Aligned: sedan proportions in `Tools/Blender/AssetLibrary/vehicles/body_a.py` are length 4.90, width 1.84, height 1.44, next to the 1.8 m player. The file says "No badges." Bus sources (`vehicles/bus.py`, `bus_city40.py`) use the published sheet (height 126 inches) and say "No badges or brand marks." The scale still shows the mannequin about a head taller than the sedan roof, which matches 1.8 m beside 1.44 m.

Drift:

- **Should-fix.** This branch's `Docs/AssetLibrary.md` replaces B1's court. It is now "a 22 × 12 m street full court" with "FIBA markings scaled by 22/28 along the length and 12/15 across the width." The hoop note: rim still 3.05 m but "2.49 m in front of the pole," and "The backboard face is 0.29 m behind the rim... A literal 1.20 m face would pass through the rim on this scaled court." B1's head still has the real 0.375 m gap. A backboard that passes through the rim is the wrong size next to the player.

Correction: Restore the 22 × 15 m court and the 0.375 m face-to-rim gap from B1. Keep the sedan and the buses at the published meters, with no badges.

### D1 secondary screens — PR #126 — `31519be750fe43fa3ca5badae2dcf460276885c3`

Latest: `31519be7` records the screen handoff. Before that, `8f988837` shows four Hier idles on the drop-in cards. Stacked on #121.

Aligned: `ActionBinds.GrapplePadDefault` is `leftTrigger`, and `Show` maps it to "LT". Pass 18 of `Docs/UiPlan.md` says the grapple row is RMB on the keyboard and LT on the pad. Seat shapes in the later passes match direction (circle, triangle, square, diamond) and the swatches are the seat hues. "Not on pad yet" is gone from this head. Drop-in figures are described as a Blender render of the repo FBX.

Drift:

- **Should-fix.** The latest handoff note says the pad token is `leftTrigger` "but the tip does not print a pad glyph" (`Docs/UiPlan.md` around the pass 32 control audit). An open-flaws line says "Couch-seat grapple is a gameplay bug; leave it." It is not a bug. `dabc9782` put the rope on every couch seat. Older sentences in the same file still say "no pad bind."
- **Nit.** Pass 32 stills are composites ("Unity is not installed"). The Hier idle on the drop-in card is the right goal. Do not let a flat bust replace it.

Correction: Print LT on the grapple tip and on the controls row. Delete the line that calls the couch rope a bug.

### C2 Effects — PR #127 — `ab4eae3d28c1447d70951a16906936c213c8777f`

Latest: `ab4eae3d` shoots the pass 26 close hard-land compare, forward streaks, and wall scuffs. Before that, `aed247ad` quiets the hard-land ring and adds a wall scuff. The head moved during this sample (earlier `aed247ad`, then `ab4eae3d`).

Aligned: the commits are visual (close camera, streaks outside the silhouette, dust tinted by brick, concrete, or wood). No feel number is in the message. Root motion is not mentioned.

Drift: none found in the two latest commits. Next pass should confirm the hard-land still uses the 65% roll gate and does not add a mesh-scale squash.

Correction: none. Keep streaks and dust off the silhouette, and do not retune the land.

### A2 Rig — PR #128 — `35085dbf9652314325eb0ee77dce0b4252f12391`

Latest: `35085dbf` measures hip and knee flexion before the hinge overlap limit. Before that, `533a8488` seats the clearance sole and tapers the knee, hip, and neck. Report: `Docs/LocoStills/pass6/hip-range.txt`. Stills: `hip-hinge-current.png`, `hip-hinge-candidate.png`.

Aligned: the candidate is not bound into the player. The report separates rest overlap (`rigJoint`) from pose. Current hip at rest is already past the limit (Hips inside the thigh 3.99 cm). The candidate is clear at rest. At the shared pose (hip flexion 110°, knee flexion 70°) the candidate keeps both hinges at 0 cm and puts the pelvis 81 cm behind the foot. The current rig leaves the hip hinge at 3.94 cm in that pose. Binding waits on Landon (`DECISIONS.md`).

Drift:

- **Nit.** The candidate hip hinge fails at 9° of command (7° of bone flexion) and stays over through about 80°. A plant that only needs 25° at the knee can still use it. A shallow hip fold cannot, until the hinge is wider. That is a rig fact for Landon, not a reason to bind it early.

Correction: Leave the clearance candidate unbound. Keep reporting `rigJoint` and pose separately, and do not lift poses to hide the current 3.99 cm rest overlap.

### B3 Props — PR #129 — `6fb88df4413255ee7a7c99ced142f1cbb51daf52`

Latest: `6fb88df4` records the pass 23 catalog and reshoots the three-hoop rack. The hoop mesh is unchanged. Before that, `ddfcd7da` adds a wood pole, a crossarm, and a pole-top transformer.

Aligned: the catalog still says players are about 1.8 m. The new pole is a prop, not a vehicle, so the logo rule does not apply to a wordmark-free timber pole. Hoop rim stays 3.05 m.

Drift:

- **Should-fix.** This head carries the same scaled court as #125. `Docs/AssetLibrary.md`: 22 × 12 m, FIBA paint scaled by 22/28 and 12/15, and "A literal 1.20 m face would pass through the rim on this scaled court." The three-hoop reshoot does not repair that.

Correction: Same as B2. Put the court back to 22 × 15 m and the 0.375 m face-to-rim gap. Leave the hoop mesh at a 3.05 m rim in front of the backboard.

### E Evasion — PR #130 — `8dede10c887159ee7114fa4d358af90796e7696c`

Latest: `8dede10c` adds a flag-off airborne dive flick on the right stick. Before that, `1135456c` adds a flag-off stutter prototype on a right-trigger double-tap. Doc: `Docs/EvasionMoves.md`. Stills: `Docs/EvasionStills/pass4/`.

Aligned: `EvasionMoves.Enabled` starts false. The doc quotes coyote 0.10, buffer 0.16, cling 0.08, jump speed 24.7, terminal 56.16, roll at 65% of terminal, walk 6.9, crouch 3.68, sprint 13.8, root motion off. Moves brake or cut and do not go past the 13.8 sprint cap. No-clip line: `clips=6 frames=115 worldMax=0.12 rigJoint=7.96 pose=0.0 fails=0`. LT is not read. `PlayerInputReader` on this branch still says the pad rope is LT. The stutter comment: "LT stays the couch rope. LB stays sprint. RB stays air dash." Keyboard proposals `X`, `B`, `Z`, and `R` are not bound. Jump height stays on the jump button.

Drift:

- **Should-fix.** RT is not free on this branch once the flag is on. Stutter is a double-tap of `pad.rightTrigger` inside 0.25 s (`SampleRightTrigger`, `Docs/EvasionMoves.md`). Direction says RT is free. The LT-versus-stutter choice is still Landon's (`DECISIONS.md`). Taking RT ahead of that decision spends the free button. Juke, spin, and dive also take the right stick, which is look. The gesture code puts the look sample back when a move actually starts. That part is measured (`cameraFP=0/72`). It is still a shared stick, so it stays behind the flag.

Correction: Keep the flag off. Leave LT on the rope. Do not treat RT as the stutter button until Landon picks it. A single RT press must keep doing nothing.

### F Costumes — PR #131 — `0273d82c9a67bb176be5a125747eb916c7a5ec50`

Latest: `0273d82c` adds the prep-only costume lab. Before that, `e5385cf4` reshoots the couch rope as four seat cameras. Doc: `Docs/Characters/CostumeBrief.md`. Stills: `Docs/Characters/pass1/lineup-front.png` and `readability-30px.png`.

Aligned: the lab is not in the player build (`Art/CharacterLab/`, not a player scene). `costume_lab.py` sets armature and bone scale to `(1, 1, 1)`. The brief says the four starters share the current Hier skeleton and the same height, and that lanky, stocky, small, and mid are costume reads, not four bodies. Fit line: `costume-fit sets=12 frames=4128 worldMax=0.38 fails=0`, cloth 0.60 cm off the hull, under the 0.5 cm intersection limit. No logos. P1 red, P2 blue, P3 orange. The hip/thigh rest overlap is left for the rig lane.

Drift:

- **Nit.** The brief calls P4 Sol "purple". The seat color is lavender (`MenuMannequin.Swatch` lavender, not a second purple). Bram is Reed's mesh with a different `PlayerColor`, which matches "one skeleton" and will not read as a stockier body unless the helmet does that work.

Correction: Name P4's block lavender, the same swatch as the seat, and keep every loadout on the one Hier height.

## Top of this pass

Ororo, these are the five to send. The one-line correction is the whole note.

1. **D1 #121, blocker.** Evidence: `MenuHost.cs` "Not on pad yet", `Docs/Controls.md` on that branch, stills `Docs/UiStills/pass50/controls.png` and `pass48/options-controls.png`. The play tip binds LT in `dabc9782`. Correction: Change the grapple pad column from "Not on pad yet" to LT.
2. **Motion reference #123, should-fix.** Evidence: `Docs/HierStills/v080/pass14/pose_error_pass14.txt` frame 34 `pelvisBack=-4.7cm target=8.0cm`, and the fail list in that file. Still: `hipsit_pose_12_tictac_after.png`. Correction: Put the pelvis at least 8 cm behind the support foot on every plant, and do not leave it in front.
3. **B2 #125 and B3 #129, should-fix.** Evidence: `Docs/AssetLibrary.md` on both heads, "A literal 1.20 m face would pass through the rim on this scaled court." B1 #122 still has the 0.375 m gap. Correction: Restore the 22 × 15 m court and the 0.375 m face-to-rim gap.
4. **D1 #121, should-fix.** Evidence: `Docs/UiPlan.md` pass 50, "First is a little taller," still `Docs/UiStills/pass50/results.png`. Correction: Draw the Hier mannequin at 1.8 m on every results card, and put the seat shape on the badge.
5. **A1 #118, should-fix.** Evidence: `Assets/Scripts/Settings/Accessibility.cs` `PlayerGlyph` `● ■ ▲ ◆` and palette-0 colors yellow, green, white, cyan. Correction: Default marks are P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Colour-blind sets stay behind the setting.
