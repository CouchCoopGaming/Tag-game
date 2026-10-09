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

## 2026-10-09 — pass 2

Heads were read after `git fetch`. #125 moved during the sample (`abf0d79b`, then `bc02b9c4`). #119 and the stack under #118 were listed, not re-sampled. #126 is merged and is recorded here. It is not reopened.

Movement plan for this cadence: A1 #118 consolidates. C1 #120 and E #130 stay helpers. Storror S1 #123 is reference only. S2 #124 stays idle. Design does not merge anything.

### Standing rule — never merge — PR #126 into #121

`cursor/tag-ui-screens2` was merged into `cursor/tag-ui-menu` at 2026-10-09 00:18:04 UTC by `app/cursor` (cursor[bot]). Merge commit `35a535d93d6cff9809371af428edf32f335659dc`. Parents are #121's previous tip `b89c5acd` and #126's tip `31519be7`. The message is "Absorb the Hier idle screens and keep every results body at 1.8 m."

This breaks the never-merge rule. Do not unmerge. The absorb is already on D1, and later commits sit on top of it (`16491d51`, then `0d3b9d74`).

What landed: `Docs/UiPlan.md` pass 51 says the lane absorbed `31519be7`. Load, join, the main menu, and the title keep that Hier idle. Results uses the same body on every card at 1.8 m, rank on the plinth, seat shape on the badge. Grapple's pad column is LT. RT stays free.

Correction: none to the screens. Ororo, do not ask D1 to undo the merge. The rule for the next pass is that no lane merges another lane.

### D1 UI — PR #121 — `0d3b9d7408ccc6187b2b2c6e648055da47d8a67a`

Latest: `0d3b9d74` gives each results rank its own pose and softens the idle hang. Before that, the absorb commit, then `16491d51` samples a celebrate frame and a relaxed stand.

Aligned: the live grapple column is LT. `ActionBinds.GrapplePadDefault` is `leftTrigger`. `MenuHost` uses `pad.GrapplePad` or that default. `Docs/Controls.md` says "The pad column reads LT. RT is free" and the table row is Grapple / RMB / LT. "Not on pad yet" is gone from C# and from `Docs/Controls.md`. Results body is `const float hierMeters = 1.8f` (`MenuHost` around line 3017). Pass 51 says the body does not grow. Pass 52 keeps one 1.8 m slot for the celebrate V and the relaxed stand, root planted, soles at 0.5 cm. Pass 53 keeps that square: first celebrates, second pumps a fist, third leans, fourth slumps, soles print 0.005, no hop. Header string is `MenuSheet.ResultsWord` `"RESULTS"`. Seat table in `MenuMannequin.Colors.cs` is red circle, blue triangle, orange square, lavender diamond. `SeatMark` uses `MenuMannequin.Shape`. `PlayerGlyph` on this branch is `● ▲ ■ ◆`.

Drift:

- **Nit.** Pass 48 of `Docs/UiPlan.md` (line 388) and `Docs/WhatsNew.md` (line 365) still say the pad column reads "Not on pad yet". Pass 50 still says "First is a little taller." Those are older pass logs. The live builders do not.
- **Nit.** Pass 51 says the loading tip still does not invent a pad glyph. The controls row does print LT.
- **Nit.** The code type is still `MenuPodium`. The word the player reads is RESULTS.
- **Should-fix, shared with A1.** Palette 0 in this branch's `Accessibility.cs` is still the measured row (comment: "red, blue, yellow, green"). Seat chrome is a separate swatch. The default couch paint is still Landon's call (`DECISIONS.md`).

Correction: Leave the pass 48 and pass 50 sentences as history. Do not put "Not on pad yet" back on the controls row. Keep every results body at 1.8 m. The play tip still has to take the seat glyphs (see A1).

### A1 Movement — PR #118 — `5d4cd74b866b02ec1ec4ca4fa9b08678e3c19779`

Latest: `5d4cd74b` drops the vault hips on the plant and the landing. Feel locks were not retuned in this commit.

Aligned: `Docs/SmoothMotionAudit.md` on the vault plant: hips bone drops 10.0 cm, both knees bend 61.9°, knees 24.1 cm ahead of the ankles and 27.2 cm ahead of the pelvis, soles 0.38 cm off the floor, pelvis 11.8 cm behind the support foot. Landing drops the hips bone 20.0 cm, knees 79.9°, pelvis 10.2 cm behind the feet. Hinge ratio at least 2.25. Pose hit 0.42 cm. `hip-sit clips=2 loadedFrames=24 pelvisBackMin=-52.3 cm hingeMin=1.75 kneeMin=61.9 pelvisDropMin=10.0 cm fails=0`. The negative pelvisBack is the airborne swing, not a plant. Stills: `Docs/SmoothStills/pass23/vault-hip-after.png`. Root motion stays off.

Drift:

- **Should-fix.** Seat marks on the play tip are still wrong. `Accessibility.cs` `PlayerGlyph` is `● ■ ▲ ◆`. Palette 0 is still yellow, green, white, cyan. D1 already has the glyph order. This tip does not.
- **Should-fix.** `Docs/Controls.md` on this tip still has no rope row. `Docs/KnownIssues.md` and `Docs/PlaytestChecklist.md` were not changed by this commit. The motor still gives the rope to all four human seats.

Correction: Default marks are P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Add the rope to `Docs/Controls.md` as RMB / LT with RT free.

### C1 Animation — PR #120 — `176983ee76e5a38dc6dcd24d22e4a1f16cc261e8`

Unchanged since pass 1.

Aligned: plant frames still report pelvis 9.0 cm or more behind the foot, hinge about 3.4 to 4.0, `hip-sit` fails=0 on the three scored clips. Feel locks untouched. This lane stays a helper. It does not consolidate.

Drift:

- **Should-fix.** The climb plant still does not drop the pelvis. Pass 25 of `Docs/AnimFxPlan.md` still says the mesh pelvis stays at bind height (89.7 to 89.9 cm) and "No Drop." The support knee stays a few centimeters behind the pelvis. The 8 cm behind-the-foot number is met. The 8 cm drop and the 25° shin-forward knee are not.

Correction: Drop the climb-plant pelvis at least 8 cm, bend the support knee at least 25° with the shin forward, and keep the pelvis at least 8 cm behind the foot. Stay a helper. Do not merge into A1.

### B1 Environment — PR #122 — `723cc13707d899005491e4816a314a7bd43687df`

Latest: `723cc137` points the environment queue at the next buildings.

Aligned: `Docs/AssetLibrary.md` still has the 22 × 15 m court and the 0.375 m face-to-rim gap. No feel edit in the message.

Drift: none at blocker or should-fix.

Correction: none.

### S1 Motion reference — PR #123 — `879e51052e0806b8fc8b7eece8e597d36d966a57`

Latest: `879e5105` marks unlicensed and CC BY clips do-not-ship, and keys two CC0 standing emotes.

Aligned: `Docs/Movement/REFERENCE.md` says the project ships CC0 or OFL only, and a clip with no license or a CC BY license must not be retargeted into shipped animation. `emote_6step` and `emote_charleston` are marked REFERENCE-ONLY / DO-NOT-SHIP. Two CC0 emotes (`emote_agbadja`, `emote_tizi`) are marked as allowed to retarget. This lane is reference only. It does not consolidate into A1.

Drift:

- **Should-fix, reference only.** `Docs/HierStills/v080/pass14/pose_error_pass14.txt` still has `f=34 pelvisBack=-4.7cm target=8.0cm`. `plant_labels_pass14.txt` now calls f=34 and f=76 not-a-plant, because the tracked footage has the pelvis in front of the foot. The hip-sit line on that label file is `clips=1 loadedFrames=169 pelvisBackMin=-34.5 cm hingeMin=-18.01 fails=90`. A frame labeled not-a-plant can keep the footage. A frame that is a plant still has to sit.

Correction: Stay reference only. Do not import these keys into A1. On any frame you still call a plant, put the pelvis at least 8 cm behind the support foot. Do not ship the CC BY or unlicensed clips.

### S2 Motion clips — PR #124 — `80cd5f146fe945a72e20434bf063d9aa7c5f8db0`

Latest: `80cd5f14` sits the pelvis behind the support foot on the four clips. Report: `Docs/HierStills/v080/clips/pass14/hip_sit.txt`.

Aligned: the lane measured the sit. This lane stays idle. It does not hand clips to A1.

Drift:

- **Should-fix.** `hip-sit clips=4 loadedFrames=203 pelvisBackMin=-30.5 cm hingeMin=99.00 fails=36`. Slide-crouch frames go in front of the foot (for example f=2 back=-4.6, f=4 back=-11.0). Wall-run f=54 is `knee back=-30.5`. The commit did not clear the plants.

Correction: Stay idle. Do not hand these four clips to A1 until every plant is at least 8 cm behind the support foot and every crouch is at least 12 cm behind.

### B2 Vehicles — PR #125 — `bc02b9c4f650f83a38f6da522094a1bcddf27963`

Latest: `bc02b9c4` deletes the renamed street-car blockouts and keeps `Sedan_Mid_A`. The head moved during this sample.

Aligned: `Docs/AssetLibrary.md` line 147 is a 22 × 15 m court, rim 3.05 m, backboard face 1.20 m inside the baseline, "The face-to-rim gap is the real 0.375 m." The scaled-court sentence from pass 1 is gone. Vehicle sources still say "No badges" (`body_a.py`, `bus.py`, `sedan_mid_a.py`, and the other body files).

Drift: none. The pass 1 court break is closed on this head.

Correction: none. Keep 22 × 15 m and the 0.375 m gap. Keep the badges off.

### C2 Effects — PR #127 — `f2636c829fb6f05e5fac31945278d140f96c9342`

Latest: `f2636c82` adds pane streaks, It tells, and shape-based surface dust. The shapes in the message are surface marks (streaks, ticks, chips, splinters, sheets), not a second seat table.

Aligned: visual only. No feel number in the message.

Drift: none found in the latest commit.

Correction: none. Do not retune the land, and do not add a mesh-scale squash.

### A2 Rig — PR #128 — `b804954f8e93db977c096d21ef93c8724f92978b`

Latest: `b804954f` adds the hip-flex builder and the player model queue. Stills are still `Docs/LocoStills/pass6/hip-hinge-current.png` and `hip-hinge-candidate.png`. Report is still `Docs/LocoStills/pass6/hip-range.txt` (current rest overlap 3.99 cm, candidate clear at rest, fails at 9° of command, comparison pose pelvis 81 cm behind the foot).

Aligned: unbound. `Docs/Models/PLAYER_QUEUE.md` says the clearance rig stays unbound until Landon accepts the stills, `clear_hip_flex.py` is not on the candidate yet, an FBX round trip reopens a 2.54 cm knee hit so the file is not exported, and nothing is bound into #118.

Drift: none new. The 9° hinge limit is the same rig fact as pass 1. Binding stays Landon's (`DECISIONS.md`).

Correction: Leave the candidate unbound.

### B3 Props — PR #129 — `a92b5987bf759a7d31e5632cb054b011ed6d3ac4`

Latest: `a92b5987` seats the lamp colliders and brings the court fence LOD2 under the dense ceiling.

Aligned: `Docs/AssetLibrary.md` line 146 is the same 22 × 15 m court and the real 0.375 m face-to-rim gap. `GateLeaf` is the child mesh of `CourtFence`. The scaled-court sentence from pass 1 is gone.

Drift: none. The pass 1 court break is closed on this head.

Correction: none. Keep 22 × 15 m and the 0.375 m gap.

### E Evasion — PR #130 — `65aa8de052f4dc473cf9d8b162a82fd12cebde66`

Latest: `65aa8de0` seats evasion plants with a bent knee and a dropped pelvis. Doc: `Docs/EvasionMoves.md`. Stills: `Docs/EvasionStills/pass5/`.

Aligned: `EvasionMoves.Enabled` still defaults false. LT stays the couch rope. Feel numbers in the doc are unchanged. Stutter plants: hips 14° over a 4° spine, pelvis 10.4 cm down, plant knee 55°, sole within 0.21 cm. `hip-sit clips=6 loadedFrames=115 pelvisBackMin=8.99 cm hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.9 cm`. This lane stays a helper.

Drift:

- **Should-fix.** Two dive roll-up frames fail the crouch sit. The doc says the pelvis is 9.0 cm behind the foot where the crouch bar is 12 cm, and 14.1 cm down where that lane's own bar is 20 cm. The standing drop is 8 cm, so the drop clears. The 12 cm behind does not. `fails=2`.
- **Should-fix, behind the flag.** RT is still the stutter double-tap when `EvasionMoves.Enabled` is on (`SampleRightTrigger`). The flag is off, so a match does not run it. RT is still Landon's button (`DECISIONS.md`). Do not treat the flag-on path as the controls table.

Correction: On the dive roll-up, put the pelvis at least 12 cm behind the support foot. Keep the flag off. Do not treat RT as the stutter button until Landon names it. Stay a helper.

### F Costumes — PR #131 — `e5b34c0e36343405f07f0d5acd098f28a6bd7797`

Latest: `e5b34c0e` adds costume licenses, LODs, and the still quartet.

Aligned: `Art/CharacterLab/LICENSES.md` marks each loadout CC0-1.0, original mesh. One skeleton. Not in the player build.

Drift:

- **Nit.** `Docs/Characters/CostumeBrief.md` still calls P4 Sol "purple". The seat color is lavender.

Correction: Name P4's block lavender.

### Models lead — PR #133 — `b53263cabaf4ccf51fde675a31169ecdda73e815`

First sample. Latest: `b53263ca` re-grades five tips and splits the env and player queues. Base is the map lane.

Aligned: `Docs/Models/STANDARD.md` locks the player at 1.8 m, CC0 for meshes, OFL for a font only, and no badges or brand names on cars. `Docs/Models/ENV_QUEUE.md` says #125 and #129 both restored the 22 × 15 m court and the 0.375 m gap, and says not to restore the court again.

Drift: none at blocker or should-fix on this head.

Correction: none. Keep the court note as restored. Do not bind the hip candidate.

### World — PR #134 — `ac4b471e132c00a55034fd76683f5b2d2ed1acbc`

First sample. Latest: `ac4b471e` hides the court gate leaf and closes fixed collider bugs. Branch is `cursor/tag-world-c420`, base `cursor/tag-street-objects`, not a new `cursor/tag-world`.

Aligned: play turns `Col_Gate` off and hides `GateLeaf` together. The lane does not edit the fence prefab. `Docs/World/LEDGER.md` says the leaf arrives with #129. No fourth park. Court size is the props tip, which is now 22 × 15 m.

Drift: none at blocker or should-fix.

Correction: none. Keep dressing Mega Park. Do not edit the court prefab, and do not add a park.

### Effects research — PR #135 — `d0d01d42f1e4a2053d29e02c53f077781e81f032`

First sample. Latest: `d0d01d42` adds pass-3 research on surface contact and emote silhouettes. Base is `cursor/tag-fx-kit`.

Aligned: `Docs/FX/RESEARCH.md` says nothing in the note changes feel, and it quotes coyote 0.10, jump buffer 0.16, jump speed 24.7, terminal 56.16, walk 6.9, crouch 3.68, sprint 13.8, root motion off, roll at 65% of terminal. Kenney sprites are named as CC0 fallback only.

Drift: none. Notes only.

Correction: none. Do not retune feel from this note.

## Top of this pass

Closed since pass 1, so do not resend them: the D1 grapple column now says LT; results bodies are 1.8 m (pass 52 and 53 are poses on that slot, not a taller first); #125 and #129 are back to 22 × 15 m and 0.375 m; #126's screens are on #121. The merge itself stays a violation. Do not unmerge.

Ororo, these are the five to send.

1. **A1 #118, should-fix.** Evidence: `Assets/Scripts/Settings/Accessibility.cs` on `5d4cd74b`, `PlayerGlyph` `● ■ ▲ ◆`, palette 0 yellow / green / white / cyan. D1 #121 already uses `● ▲ ■ ◆`. Correction: Default marks are P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Colour-blind sets stay behind the setting.
2. **C1 #120, should-fix.** Evidence: `Docs/AnimFxPlan.md` pass 25, mesh pelvis at bind height, "No Drop," support knee behind the pelvis. Head unchanged at `176983ee`. Correction: Drop the climb-plant pelvis at least 8 cm and bend the support knee at least 25° with the shin forward. Stay a helper.
3. **E #130, should-fix.** Evidence: `Docs/EvasionMoves.md` on `65aa8de0`, dive roll-up pelvis 9.0 cm behind where the crouch bar is 12 cm, `fails=2`. Correction: Put the pelvis at least 12 cm behind the support foot on the dive roll-up. Keep `EvasionMoves.Enabled` false. Do not treat RT as the stutter button.
4. **S2 #124, should-fix.** Evidence: `Docs/HierStills/v080/clips/pass14/hip_sit.txt` on `80cd5f14`, `fails=36`, `pelvisBackMin=-30.5 cm`. Correction: Stay idle. Do not hand the four clips to A1 until every plant is at least 8 cm behind the support foot.
5. **A1 #118, should-fix.** Evidence: `Docs/Controls.md` on `5d4cd74b` still has no rope row. The motor binds RMB / LT. Correction: Add the rope row as RMB / LT, and say RT is free.

Also for Ororo, not a lane correction: #126 was merged into #121 by cursor[bot] at 00:18 UTC. Standing-rule violation. Do not unmerge. A2 #128 `b804954f` is still unbound, waiting on Landon.

## 2026-10-09 — pass 3

Heads were read after `git fetch`. The requested tips matched. #136 moved during the sample (`bc4ab7ae`, then `9c0d3e7a`). #123 and #124 were not in the request list; both are still open, and #123 moved, so they are included. New drafts #136, #137, and #139 were open. #138 and #140 were already merged.

### Standing rule — never merge

Do not unmerge any of these.

- **#126 into #121.** Still merged. `35a535d9`, 2026-10-09 00:18:04 UTC, `app/cursor`. Parents `b89c5acd` and `31519be7`. The Hier idles, the 1.8 m body, and the LT column stayed.
- **#118 into #122.** `fee5fb84`, 2026-10-09 00:28 UTC, Cursor Agent. Parents `723cc137` and `5d4cd74b`. Message: merge `origin/cursor/tag-map-lane-pass19-8c95` into `cursor/tag-asset-library`.
- **#125 into #134.** `6044a939`, 2026-10-09 02:12 UTC, Cursor Agent. Parents `ace60c03` and `0aa3061e`.
- **#122 into #134.** `b813f3e6`, 2026-10-09 02:46 UTC, Cursor Agent. Parents `8f7b0a49` and `a066d987`.
- **#138 into #136.** PR #138 is merged (2026-10-09 01:58 UTC). Commit `5d567e43` parents `39f55a3e` and `a08d6872`.
- **#140 into #136.** PR #140 is merged (2026-10-09 03:01 UTC). Commit `98cfb017` parents `35105dc5` and `ebb8741e`.
- **#137** `bca44c86` is a merge (parents `bd480d50` and `e1e831d4`). **#139** `246205da` is a merge (parents `da614e24` and `35105dc5`).

`Docs/Movement/LEDGER.md` on #136 says "Nothing in this file is a git merge of a helper branch" and then lists #138 and #140 as folded with `git merge`.

Correction: none of these get undone. No lane merges another lane. A1 #118 is still the only branch that consolidates, and it has not taken these folds.

### A1 Movement — PR #118 — `97f66cb89c8ce47992cb4df94a003e11ee9473a7`

Latest: `97f66cb8` qualifies `CompressionLevel` so Unity 6000.3 compiles. Diff against `5d4cd74b` is the compile stub and `SmoothMotion.cs`. No feel edit.

Aligned: coyote, jump, and the rope binding are unchanged from pass 2. The vault hip-sit from `5d4cd74b` is still the play tip.

Drift:

- **Should-fix.** `PlayerGlyph` is still `● ■ ▲ ◆`. Palette 0 is still yellow, green, white, cyan.
- **Should-fix.** `Docs/Controls.md` still has no rope row.

Correction: Default marks are P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Add the rope row as RMB / LT, and say RT is free.

### Movement draft — PR #136 — `9c0d3e7a`

Latest: `9c0d3e7a` records the hip-floor scan and the folded climb and wall pose. The branch calls itself the movement branch.

Aligned: feel locks in `Docs/Movement/LEDGER.md` match direction (coyote 0.10, buffer 0.16, cling grace 0.08, jump speed 24.7, terminal 56.16, roll at 65% of terminal, walk 6.9, crouch 3.68, sprint 13.8, root motion off, one Move per Update). `MovementConfig.cs` still has coyote 0.10, jump speed 24.7, terminal 56.16, and `applyRootMotion = false`. The folded climb top-out clears the direction plant: pelvis 20.3 cm behind, knee 67.9°, drop 17.8 cm, hinge 2.25, pose 0. Evasion flag stays off. LT stays the rope. The ledger says the clearance rig is not approved.

Drift:

- **Blocker.** This branch is not the play tip. A1 #118 consolidates. #136 has already git-merged #138 and #140, and its ledger tells helpers the lead merges with `git merge`.
- **Should-fix.** `Docs/Movement/STANDARD.md` adds floors direction does not have: plant hip flexion 25°, and on a landing or crouch hip 35°, spine 15°, knee 45°, drop 20 cm. The scan then fails every loaded frame (`HIP clips 39 frames 769 loaded 159 hipFails 159`). A miss of those extra floors is not a direction fail. See `DECISIONS.md`.

Correction: Stay a draft. Do not replace #118, and do not merge another helper. Keep the direction hip-sit bars. The climb plant that already drops 17.8 cm can wait on A1. Do not key a third copy on #120.

### C1 Animation — PR #120 — `176983ee76e5a38dc6dcd24d22e4a1f16cc261e8`

Unchanged since pass 1.

Drift:

- **Should-fix.** Pass 25 of `Docs/AnimFxPlan.md` still says the climb plant has no pelvis drop. The sit that clears direction is the one measured on #136 after the #138 merge, not on this tip.

Correction: Leave this tip as the helper record. Do not merge it, and do not re-key a second climb plant. The 17.8 cm drop already exists on #136.

### Evasion helper — PR #137 — `bca44c86`

Latest: `bca44c86` merges the lead hip table and keeps the pass 7 sit. Base is `cursor/tag-movement`.

Aligned: `EvasionMoves.Enabled` stays off. `Docs/Movement/ASSIGNMENT.md` reports `hip-sit clips=11 fails=0 pelvisBackMin=9.72 hingeMin=1.50 kneeMin=50.0 pelvisDropMin=8.4` and `no-clip clips=11 frames=156 worldMax=0.0 pose=0.0 rigJoint=7.91 fails=0` for the six evasion moves plus the landings and rolls. S1 is cited as reference only.

Drift:

- **Blocker.** The assignment says the lead merges with `git merge` after a measured pass. This tip is already a merge.
- **Should-fix.** Stagger contact at t=0.00 is back −1.0 cm, knee 2.5°, drop 0. They left it open. If that frame is a plant, it misses the sit.

Correction: Do not merge this branch into #136 or #118. Keep the flag off. On the stagger plant, put the pelvis at least 8 cm behind the support foot, drop it at least 8 cm, and bend the support knee at least 25°.

### Exits helper — PR #139 — `f75cce89`

Latest: `f75cce89` assigns the wall run to C1 with the climb and the exits. `246205da` is a merge of the lead hip table.

Aligned: wall run sitting with the climb and the exits matches C1's helper job. It is not a new motor.

Drift:

- **Should-fix.** The commit says the lead table puts a 4.97 cm wall-run overlap on this branch, next to a played climb at 3.74 cm. Pose has to be 0. Over 0.5 cm is a fail.
- **Blocker.** It is stacked on #136 and already contains a merge. It does not consolidate into A1 by merging.

Correction: Bring the wall-run pose to 0 without lifting the plant to hide it. Do not merge this branch.

### E Evasion — PR #130 — `8d96a3f2d82be73cbb81de51b1ea85e19396f813`

Latest: `8d96a3f2` stretches the dive takeoff into a forward reach. The 0.17–0.53 s window is a lean, not a crouch hop. Stills: `Docs/Movement/evasion-pass6/`.

Aligned: `EvasionMoves.Enabled` defaults false. LT stays the couch rope. `evasion-moves` still prints `rootMotion=0 flagDefault=off`. The airborne stretch is not scored as a plant. This branch has not taken #137's pass.

Drift:

- **Should-fix.** Dive roll-up still fails the crouch sit. `Docs/EvasionMoves.md`: pelvis 9.0 cm behind where the crouch bar is 12 cm, drop 14.1 cm. `hip-sit clips=6 loadedFrames=115 pelvisBackMin=8.99 cm fails=2`.
- **Should-fix.** `no-clip clips=6 frames=115 worldMax=0.0 rigJoint=7.89 pose=1.4 fails=1`. The 1.4 cm hit is the spine into the pivot thigh on the spin. They logged it as rig-blocked and did not retune it. Pose is still not 0.

Correction: Keep the flag off. On the dive roll-up, put the pelvis at least 12 cm behind the support foot. Bring the spin pose to 0, or leave it until the rig lane owns that pair as `rigJoint`. Do not treat RT as the stutter button. Do not merge #137 in to paper over this tip.

### A2 Rig — PR #128 — `44fbff3f56b8d2831f6b77d5831378cf3a78b85a`

Latest: `44fbff3f` splits the hip cuff so the 110° hinge does not fan. Before that, `d809c60a` clears hip flex on the reimported candidate.

Aligned: unbound. `Docs/Models/PLAYER_QUEUE.md` says nothing is bound into #118. Pass 7 on `Dummy_Mannequin_Tan_Hier_Clearance.fbx`: `rigJoint=0`, hip flexion clear from 0° through 120° on the enclosure test, thigh edges 2.09 cm and 2.00 cm, no edge at least 1 mm long past 1.2× rest at 110°/70°. Stills: `Docs/Models/RigStills/pass7/`. Binding stays Landon's (`DECISIONS.md`).

Drift:

- **Should-fix.** The same queue says #131 is unblocked and the 12 costumes can refit. #131 `ad1582a8` correctly stayed on the current rig. A refit before Landon accepts the stills binds the candidate in practice.

Correction: Leave the candidate unbound. Costumes stay on the current Hier until Landon accepts `Docs/Models/RigStills/pass7/`.

### D1 UI — PR #121 — `780c79a98b87f8a0059f7b164b5b2751d6d5435a`

Latest: `780c79a9` records the pass 56 sim lines. Between pass 2 and this tip: compile fix, one gold controls row, results poses, rest overlap counted as a rig joint.

Aligned: `GrapplePadDefault` is still `leftTrigger`. `hierMeters` is still 1.8. `MenuSheet.ResultsWord` is still `"RESULTS"`. `PlayerGlyph` is still `● ▲ ■ ◆`. Pass 51–53 still say the body does not grow.

Drift:

- **Nit.** Pass 48 of `Docs/UiPlan.md` and `Docs/WhatsNew.md` still says "Not on pad yet". Pass 50 still says "First is a little taller." Those lines are history.

Correction: none. Leave the old sentences. Keep LT and the 1.8 m body.

### B1 Environment — PR #122 — `c727dc0ebbef2b68be6672dfe16e60e49a04292c`

Latest: `c727dc0e` shoots fresh store, container, and dock stills. The branch contains the play-tip merge `fee5fb84`.

Aligned: `Docs/AssetLibrary.md` line 158 is still a 22 × 15 m court and the real 0.375 m face-to-rim gap.

Drift: the merge is the standing-rule break above. No new court or feel drift on this head.

Correction: Do not merge the play tip again. Keep the 22 × 15 m court.

### B2 Vehicles — PR #125 — `20f0d9fa914747b7feb631e77ecddc7254df07ef`

Latest: `20f0d9fa` adds 2022–2026 model years for each vehicle line.

Aligned: `Docs/AssetLibrary.md` line 147 is still 22 × 15 m and 0.375 m. `sedan_mid_a.py` still says "No badges."

Drift: none on court or logos.

Correction: none. Keep the badges off.

### B3 Props — PR #129 — `6716e242b065f960f2b2043cdf815dfd2492f48e`

Latest: `6716e242` gives the placed street props their still quartets.

Aligned: `Docs/AssetLibrary.md` line 146 is still 22 × 15 m and 0.375 m. `CourtFence` is named as #122's mesh.

Drift: none on the court.

Correction: none.

### C2 Effects — PR #127 — `fc9c8d820433fd5898261e65b03cfdf2ad703672`

Latest: `fc9c8d82` loads comic PNGs from files so the compiler does not overflow. Before that, `c5f2d16f` reshoots pass 29 with body-foam seat colors: red, blue, orange, and lavender.

Aligned: visual only. Seat tints match the seat identity.

Drift: none found on feel or seats.

Correction: none. Do not retune the land.

### F Costumes — PR #131 — `ad1582a8e877d00356ff212ad13a78adc5d284ca`

Latest: `ad1582a8` splits the costume shells at the joints and reshoots pass 5.

Aligned: P4 Sol is lavender in `Docs/Characters/CostumeBrief.md`. The brief says the clearance candidate is not in this branch, the check reports `rig-not-clearance`, and nothing is refitted until the rig is stable. Licenses stay CC0. One skeleton. The pass 1 "purple" nit is closed.

Drift:

- **Nit.** The scale still's hooded figure measures 1.871 m. The note says the shipped Hier already spans 1.8588 m and the hood adds the rest. The body was not rescaled.

Correction: none. Stay on the current rig.

### Models lead — PR #133 — `a0a4df9dd586d95584e05c336ab431838074a556`

Latest: `a0a4df9d` grades the 2022–2026 vehicle years.

Aligned: `Docs/Models/STANDARD.md` still marks the clearance candidate unbound. The ledger still records the 22 × 15 m court and the 0.375 m gap as restored.

Drift: none at blocker or should-fix.

Correction: none. Do not bind the hip candidate.

### World — PR #134 — `5b26a0a3479c9ddcbc692682ea2d1a3c76738845`

Latest: `5b26a0a3` dresses the crash bowl, the bar highway, and the hopscotch. The branch contains the two merges above.

Aligned: still Mega Park dressing. No fourth park in the latest message.

Drift: the merges are the standing-rule break. No new scale drift in the latest commit message.

Correction: Do not merge the asset library or the vehicle lane again. Keep dressing Mega Park.

### Effects research — PR #135 — `c585dc8f988f916686eeffb7be79f85492e1160b`

Latest: `c585dc8f` corrects pass-4 seat tints to red, blue, orange, and lavender. The message says P3 and P4 are the body colors, not the yellow and green stored on `VerbFxLook.PlayerColor`.

Aligned: notes only. The seat table matches direction.

Drift: none.

Correction: none.

### S1 Motion reference — PR #123 — `8fccec65178afb30c4f3bd0dccdc4aa8240d8301`

Latest: `8fccec65` annotates hip-rule reference for roll, vault, climb, mantle, wall, and slide. Before that, `305a621f` hand-keys four original emotes.

Aligned: `Docs/Movement/REFERENCE.md` says every clip that came from video stays reference-only, including the two CC0 Commons dances. The four pass 17 emotes are own work, CC0, and parked. Unlicensed and CC BY stay do-not-ship. This lane stays reference only.

Drift:

- **Nit.** Tic-tac `pose_error_pass14.txt` still reports `fails=90` and `pelvisBackMin=-34.5 cm`. The lane already calls filmed plants reference-only.

Correction: Stay reference only. Do not import the filmed keys into A1.

### S2 Motion clips — PR #124 — `80cd5f146fe945a72e20434bf063d9aa7c5f8db0`

Unchanged. `hip_sit.txt` is still `fails=36`, `pelvisBackMin=-30.5 cm`.

Drift: **should-fix**, same as pass 2. This lane stays idle.

Correction: Stay idle. Do not hand the four clips to A1 until every plant is at least 8 cm behind the support foot.

## Top of this pass

Still true, so do not resend as if they were new: D1 prints LT and keeps a 1.8 m body; #125 and #129 keep 22 × 15 m and 0.375 m; A2 is unbound; #126 stays merged.

Ororo, these are the five to send.

1. **#136, blocker.** Evidence: `Docs/Movement/LEDGER.md` on `9c0d3e7a` calls `cursor/tag-movement` the movement branch and records `git merge` of #138 and #140. A1 is #118 `97f66cb8`. Correction: Stay a draft. Do not replace #118, and do not merge another helper.
2. **A1 #118, should-fix.** Evidence: `Accessibility.cs` on `97f66cb8`, `PlayerGlyph` `● ■ ▲ ◆`, palette 0 yellow / green / white / cyan. Correction: Default marks are P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Colour-blind sets stay behind the setting.
3. **A1 #118, should-fix.** Evidence: `Docs/Controls.md` on `97f66cb8` still has no rope row. Correction: Add the rope row as RMB / LT, and say RT is free.
4. **E #130, should-fix.** Evidence: `Docs/EvasionMoves.md` on `8d96a3f2`, dive roll-up 9.0 cm behind a 12 cm crouch bar, `fails=2`, and `pose=1.4 fails=1` on the spin. Correction: Keep the flag off. Put the dive roll-up pelvis at least 12 cm behind the support foot, and bring the spin pose to 0. Do not merge #137 in to cover it.
5. **A2 #128, should-fix.** Evidence: `Docs/Models/PLAYER_QUEUE.md` on `44fbff3f` says #131 can refit now. #131 `ad1582a8` stayed on the current rig. Correction: Leave the clearance candidate unbound until Landon accepts `Docs/Models/RigStills/pass7/`.

Also for Ororo, not a lane undo: #126, the #122 play-tip merge, the #134 asset merges, and the #136 helper merges stay as they are. Do not unmerge. #120's climb plant is still "No Drop" on `176983ee`; the 17.8 cm drop is already measured on #136. Do not ask C1 to key it again.

## 2026-10-09 — correction after pass 3

Landon's calls, relayed by Ororo. Pass 3 item 1 is withdrawn. Do not send it.

Movement. Oct 8, 6:01–6:03 PM CDT: the movement lanes are one lane. A1 leads on `cursor/tag-movement` (#136) and folds helper sub-branches in by git merge after review. #118 stays A1's PR. The old drafts stay open. A fold into #136 is not drift. #126 merged into the menu lane is still a separate break. Do not unmerge it.

Hip floors. Oct 8, 5:58 PM CDT: hips visibly sit back on every loaded frame. Adopted and now in `DIRECTION.md`: a plant has hip flexion of at least 25°; a landing or a crouch has hip flexion of at least 35° and spine flexion of at least 15°. The 1.5 ratio stays. Knee 45° and a 20 cm landing drop are not the lock. The evasion hard landing at hip 6° over spine 4° fails the new floor.

Next sample, around 11:30 PM CDT: re-sample every open lane against this direction, and check that each still shows the whole subject and a 1.8 m scale figure.

## 2026-10-09 — pass 4

Heads were read after `git fetch`. The requested tips matched, except #123: the note said `b74cca5e`, and the live head is `e06351c0` (one commit later). #136 `9c0d3e7a` and #137 `bca44c86` are unchanged. #139 moved to `5db5ffcf`. A fold into #136 is not drift. #126 stays merged. Do not unmerge it.

Hip floors in force: hips sit back on every loaded frame. A plant has hip flexion of at least 25°. A landing or a crouch has hip flexion of at least 35° and spine flexion of at least 15°. The 1.5 ratio stays. Hero, side, and scale show the whole subject. The scale frame also shows a 1.8 m figure.

### A1 shared movement — PR #136 — `9c0d3e7a`

Unchanged since pass 3. `Docs/Movement/LEDGER.md` already scores the adopted floors.

Aligned: feel locks match (coyote 0.10, jump speed 24.7, terminal 56.16, root motion off). The climb top-out still clears the older plant distances: pelvis 20.3 cm behind, knee 67.9°, drop 17.8 cm. This branch is the movement lane. Helper folds are not drift.

Drift:

- **Should-fix.** Every loaded frame fails the adopted floors. `HIP clips 39 frames 769 loaded 159 hipFails 159`. The shared recovery sit is hip 18° and spine 8° (ratio 2.25). The climb plant is hip 18°, under 25°. The dive roll-up is hip 6° over spine 4°. Exit-Roll late contact is hip 6° and spine 2°. Slide knee stays 8° with the shin behind.

Correction: On every loaded frame, a plant has hip flexion of at least 25°, and a landing or a crouch has hip flexion of at least 35° and spine flexion of at least 15°, with the 1.5 ratio and the pelvis behind the support foot. C1 keys the vault, climb top-out, wall run, mantle, and slide. E keys the dive roll-up and the launch, stagger, and soft lands.

### C1 exits — PR #139 — `5db5ffcf`

Latest: `5db5ffcf` merges the lead hip floors and records C1's new fails. The merge into the movement lane is allowed.

Aligned: `Docs/Movement/ASSIGNMENT.md` uses the adopted floors. Played climb and wall run are under 0.5 cm pose.

Drift:

- **Should-fix.** The same sits fail. Played vault: plant hip 18°, land hip 20° and spine 8°. `exit-ClimbTopOut`: 11 frames, plant hip 18°. `exit-WallRun`, `exit-Vault`, `exit-Mantle`, and `exit-Slide`: hip 18° and spine 8°.

Correction: Same as #136. Re-key those loaded frames to the floors. Do not open a third copy on #120.

### C1 old draft — PR #120 — `176983ee`

Unchanged. `Docs/AnimFxPlan.md` pass 25 still keeps the climb-plant mesh pelvis at bind height, 89.7 to 89.9 cm, and says there is no drop.

Drift: **should-fix**, stale. The working plant is the one on #139, and that one fails the 25° hip floor.

Correction: Leave this draft open. Do not re-key it. The fix belongs on #139.

### E old draft — PR #130 — `8d96a3f2`

Unchanged. `Docs/EvasionMoves.md` still has `hip-sit fails=2` (dive roll-up 9.0 cm behind a 12 cm crouch) and `pose=1.4 fails=1` on the spin. The flag defaults off.

Drift: **should-fix**. The folded read on #136 is the adopted-floor fail: hip 6° over spine 4°.

Correction: Keep the flag off. The roll-up key that folds in has to clear hip 35° and spine 15°, with the pelvis at least 12 cm behind the support foot. Bring the spin pose to 0.

### E helper — PR #137 — `bca44c86`

Unchanged. Its pass 7 line is `hip-sit clips=11 fails=0 pelvisBackMin=9.72`. That line is the older bar. It is not a pass against hip 35° and spine 15°.

Correction: Do not treat `fails=0` on that line as the adopted floor. The dive roll-up still has to clear hip 35° and spine 15°.

### A1 play PR — PR #118 — `97f66cb8`

Unchanged. Compile stub only.

Drift:

- **Should-fix.** `PlayerGlyph` is still `● ■ ▲ ◆`. Palette 0 is still yellow, green, white, cyan.
- **Should-fix.** `Docs/Controls.md` still has no rope row.

Correction: Default marks are P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Add the rope row as RMB / LT, and say RT is free.

### A2 Rig — PR #128 — `44fbff3f`

Unchanged. The clearance candidate is still unbound. Pass 7 stills are still `Docs/Models/RigStills/pass7/`. The queue still says the scale still is waiting, and it still says #131 can refit.

Drift: **should-fix**, same as pass 3. #131 did not refit.

Correction: Leave the candidate unbound until Landon accepts the pass 7 stills. Costumes stay on the current rig.

### D1 UI — PR #121 — `a925cbbe`

Latest: `a925cbbe` guards the capture against flat figures, giant capsules, and a plate over Play.

Aligned: `GrapplePadDefault` is `leftTrigger`. `PlayerGlyph` is `● ▲ ■ ◆`. The results body constant is still 1.8 m. `ResultsWord` is still `"RESULTS"`.

Drift: **nit.** Pass 48 still says "Not on pad yet". Pass 50 still says "First is a little taller."

Correction: none. Keep LT and the 1.8 m body.

### B1 Environment — PR #122 — `4fd2472d`

Latest: `4fd2472d` reshoots 94 quartets so the whole object stays in frame. `Docs/Models/ENV_QUEUE.md` on this tip states the camera rule: hero, side, and scale show the whole object, and the scale frame keeps the 1.8 m figure in view. Court text is still 22 × 15 m and 0.375 m.

Aligned: sampled `Docs/AssetStills/pass36/brick_corner_quarter.jpg`, `brick_corner_side.jpg`, and `brick_corner_scale.jpg` (1280×720). The corner is fully in frame on all three. The scale frame has the figure beside the building.

Drift:

- **Should-fix.** Pass 36 names 94 assets and 39 of them are missing at least one of quarter, side, or scale in that folder. `courtfence` has no quarter and no scale there. The models-lead grade is one commit older (`c727dc0e`) and does not cover this reshoot.

Correction: Finish quarter, side, and scale for each of those 39, whole subject, 1.8 m figure on the scale frame. Do not rebuild a mesh to fix a frame.

### B2 Vehicles — PR #125 — `73fc3a78`

Latest: `73fc3a78` reshoots pass 21 so each vehicle quartet shows the whole mesh. Court text is still 22 × 15 m and 0.375 m. `sedan_mid_a.py` still says no badges.

Aligned: sampled `Docs/AssetStills/vehicles/sedan_mid_a_26/pass21/scale.png` and `hero.png`, and `bus_city40_25/pass21/scale.png` (1280×720). The whole vehicle is in frame with margin. Both scale frames show a standing figure beside the vehicle. 27 shells have a pass 21 scale frame.

Drift:

- **Should-fix.** 18 shells have no pass 21 scale frame: the unsuffixed `Bus_City40` and `Bus_City60`, `Bus_City40_Blue`, `Bus_City40_Red`, `Crossover_Compact_25`, `Hatch_Compact_25`, `Pickup_FullSize_25`, `Sedan_Compact_25`, `Sedan_Mid_A_22` through `Sedan_Mid_A_25`, and the six `Sedan_Mid_A_25` paints. The models-lead sentence that pass 21 has no 1.8 m figure was graded on `20f0d9fa`, before this reshoot.

Correction: Shoot hero, side, and scale for those 18 the way `Sedan_Mid_A_26` is shot: whole mesh, 1.8 m figure on the scale frame. Keep the badges off.

### B3 Props — PR #129 — `f0b41659`

Latest: `f0b41659` gives the next street furniture its still quartets. Court text is still 22 × 15 m and 0.375 m. Pass 36 on this tip has 38 assets with quarter, side, scale, and close.

Aligned: sampled `Docs/AssetStills/pass36/bench_metal_scale.png` (1280×720). The whole bench is in frame and a standing figure is beside it.

Drift:

- **Should-fix.** The new folder is 38 quartets. The models-lead grade of the previous tip `6716e242` still had 119 framing fails, including a missing 1.8 m figure on most of them. This commit does not replace that list.

Correction: Keep the new quartets. Reshoot hero, side, and scale for the assets that still crop the subject or omit the 1.8 m figure. Do not rebuild a mesh to fix a frame.

### World — PR #134 — `cb320d4e`

Latest: `cb320d4e` reshoots Mega Park stills from library meshes. `Docs/World/STATUS.md` says the pass 10 stills are library LOD0 with a 1.8 m figure in frame. `world-check` reports `scaleFails=0`.

Drift:

- **Should-fix.** Sampled `Docs/WorldStills/pass10/z7_eye.png` (1280×720). The frame is the parked cars and the surrounding block. No 1.8 m figure is visible, and the block runs to the left and right edges. These are zone frames, not a hero/side/scale quartet, and the status line still claims the figure.

Correction: Put a 1.8 m figure fully in each pass 10 eye frame, and keep the dressed subject inside the frame.

### C2 Effects — PR #127 — `ff0a1e63`

Latest: `ff0a1e63` reshoots pass 28 stills in the body-foam seat colors. The message names the dash flare blue, the grapple pull orange, and the wall ribbon red.

Aligned: visual only. The named tints are seat colors.

Drift: none on feel. These are not hero/side/scale asset frames.

Correction: none.

### F Costumes — PR #131 — `6dc8b9c4`

Latest: `6dc8b9c4` opens the folded shells so sprint, slide, and roll stay under 0.5 cm. The brief says `worldMax=0.46 fails=0` on seven clips, P4 is lavender, and the clearance candidate is not in this branch. The scale still is `Docs/Characters/pass5/scale-figure.png`, a 1.80 m staff beside the figure.

Aligned: still on the current rig. Pose line is under 0.5 cm.

Drift:

- **Should-fix.** Models lead graded this same tip and reports `models-validate assets=18 pass=0 fail=18` with 13 geometry fails (cloth coverage, shards, or a buried shell). The fit line and the validator are not the same pass.

Correction: Clear the cloth-coverage and shard fails the models checker names. Do not refit onto the clearance rig.

### Models lead — PR #133 — `00c8c739`

Latest: `00c8c739` closes the costume shard and stray-still loopholes. The framing rule matches direction: hero, side, and scale keep the whole silhouette in frame, and the scale frame shows the 1.8 m figure.

Aligned: the candidate stays unbound in `Docs/Models/STANDARD.md`. The court note is still 22 × 15 m and 0.375 m.

Drift: **nit.** The validator table is one commit behind the reshoots: #122 `c727dc0e`, #125 `20f0d9fa`, #129 `6716e242`.

Correction: Re-grade #122 `4fd2472d`, #125 `73fc3a78`, and #129 `f0b41659` before treating the old fail counts as current.

### Effects research — PR #135 — `cd6a1fe6`

Latest: `cd6a1fe6` adds pass-5 research on alley occlusion, seat tint, and word overlap. No gameplay scripts. Emotes stay parked.

Drift: none.

Correction: none.

### S1 Motion reference — PR #123 — `e06351c0`

Listed tip was `b74cca5e`. Live tip is `e06351c0`, "Record loaded-frame hip targets in the Hier chest-forward convention."

Aligned: `Docs/Movement/pose/HIP_TARGETS.md` marks every quoted clip reference-only. It states the adopted floors and says a thigh angle is not hip flexion. Soft-land chest-forward of 47.9° cannot hold both a 35° hip and a 15° spine. No hip key is invented for that frame.

Drift: none. This lane stays reference only.

Correction: Stay reference only. Do not import the filmed keys. A frame whose chest sum cannot hold the floors is not a key.

### S2 Motion clips — PR #124 — `80cd5f14`

Unchanged. `hip_sit.txt` is still `fails=36`, `pelvisBackMin=-30.5 cm`. Those plants also miss the new hip floors.

Drift: **should-fix.** This lane stays idle.

Correction: Stay idle. Do not hand the four clips to A1.

## Top of this pass

Do not resend the movement-merge flag. A fold into #136 is the lane. Do not unmerge #126.

Ororo, these are the five to send.

1. **#136 and #139, should-fix.** Evidence: `Docs/Movement/LEDGER.md` on `9c0d3e7a`, `HIP clips 39 frames 769 loaded 159 hipFails 159`. Recovery sit hip 18° and spine 8°. Climb plant hip 18°. Dive roll-up hip 6° over spine 4°. `Docs/Movement/ASSIGNMENT.md` on `5db5ffcf` lists the same fails for the vault, climb top-out, wall run, mantle, and slide. Correction: On every loaded frame, a plant has hip flexion of at least 25°, and a landing or a crouch has hip flexion of at least 35° and spine flexion of at least 15°, ratio at least 1.5, pelvis behind the support foot. C1 keys the exits. E keys the dive roll-up and the launch, stagger, and soft lands.
2. **B2 #125, should-fix.** Evidence: 27 of 45 shells have `pass21/scale.png`. The 18 without it include `Sedan_Mid_A_25`, its six paints, the unsuffixed buses, and the 2025 compact, hatch, crossover, and pickup. Sampled `sedan_mid_a_26/pass21/scale.png` shows the whole car and a 1.8 m figure. Correction: Shoot hero, side, and scale for those 18 the same way, whole mesh, 1.8 m figure on the scale frame.
3. **B1 #122, should-fix.** Evidence: `Docs/AssetStills/pass36/` names 94 assets and 39 are missing quarter, side, or scale. `courtfence` has neither quarter nor scale. Sampled `brick_corner_scale.jpg` is in frame with the figure. Correction: Finish quarter, side, and scale for those 39. Whole subject. 1.8 m figure on the scale frame.
4. **World #134, should-fix.** Evidence: `Docs/World/STATUS.md` says the pass 10 stills include a 1.8 m figure. Sampled `Docs/WorldStills/pass10/z7_eye.png` shows the parked cars and the block at the frame edge, and no figure. Correction: Put a 1.8 m figure fully in each pass 10 eye frame, and keep the dressed subject inside the frame.
5. **A1 #118, should-fix.** Evidence: `Accessibility.cs` on `97f66cb8` still has `PlayerGlyph` `● ■ ▲ ◆` and palette 0 yellow, green, white, cyan. `Docs/Controls.md` still has no rope row. Correction: Default marks are P1 red circle, P2 blue triangle, P3 orange square, P4 lavender diamond. Add the rope row as RMB / LT, and say RT is free.

Also still open, already sent: #128 leave the clearance rig unbound. #130 keep the evasion flag off until the roll-up clears the new floors. #124 stays idle.
