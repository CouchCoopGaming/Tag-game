# Tag -> local playtest (Amaterasu)

## Open

1. Unity Hub -> open `C:\Users\Zubal\Dev\Tag-game-playtest` (6000.3.x / 6000.0.x).
2. Open scene **Play** (`Assets/Scenes/Play.unity`) -> **Play**.
3. Optional first-time art: **Tag -> Ensure URP Pipeline**, then **Tag -> Setup Hub Visuals**.

Branch: `cursor/character-anim-hier-spawn-238c` on campus tip `adf2c85`. Results focus, Boot/pause keys, mute-from-play, the readable dash bar, CloseMenuPanels, and TubeDeck on soft-play, astro, and army are already on that tip. Footing colors are on that tip too: grass, cedar, bark, field green, and sidewalk tan. No new props. This branch does not edit the placer. Deeper notes: `Docs/MOVEMENT.md` / `Docs/PLAY-SLICE.md`.

Already on that tip (do not re-test as new): 2-frame look/punch resume gate (ResumeInputGate + ArmLookPunchGate), bots hold on countdown/results/idle, punch DropSwing, HUD mute chip, M/N during play, dash bar dark track / cyan fill, pause keys 1-5, AudioMaster, Controls/Look/Audio row highlight, panels close when play/results/Boot starts, first-run line survives Couch/Mode and clears after a round, who-plays Esc returns to Boot on Couch, mode select Esc steps back and keeps the player count, Boot pause H opens Controls only, first countdown says WASD move and Shift sprint, results keys 1-2 highlight only and Enter or Space confirms after the arm, punch-tell floor 0.22s with strafe cancel, TRAIL soft warn ~6.6 m (avoid ~9.4 m), It hat beacon, TubeDeck on soft-play, astro, and army (yaw 90, stem 0, z+5.90, mesh pitched -90 X). Mouth center 1.91 m. Knight keeps the straight chute. Mega/Tube90 still out. No MasterVolume type.

Already on this branch: Player and the bot prefer the curved Hier HiPoly mannequin over the flat Dummy_Runner / Dummy_It prefab. Run bends only the recovery knee. Dash whip is arm pitch. Slide is a low crouch. Land buckles. Punch connect stays in front of the chest.

Already on this branch: Run arms oppose the legs. The forward arm is the opposite thigh. Rearward swing stays short.

Already on this branch: A slide is a flat wedge. Punch connect is a long straight arm in front of the chest.

Already on this branch: A landing holds a short knee buckle, then eases into the run.

Already on this branch: An air dash holds the arm whip at the start, then the arms and legs ease toward a hang.

Already on this branch: Player and bot spawn the approved Tan Hier runner. It swaps to the Orange Hier mesh.

Already on this branch: A jump reaches both arms up and tucks the knees. A fall trails the arms back and lengthens the legs.

Already on this branch: A tag splits into two poses. The tagged runner guards. The new It lifts one knee.

Already on this branch: A climb is hand-over-hand. A wall run plants the wall hand and steps with the outer leg.

Already on this branch: At a standstill the arms hang slightly forward and out. That offset fades as the walk starts.

Already on this branch: A jump shows a long arm line and a knee tuck before the apex. A fall trails the arms before the landing.

Already on this branch: The run plant holds. The front thigh reaches farther than the back thigh, and only that knee bends.

Already on this branch: A slide's arms are a long low line. Elbows stay nearly straight.

Already on this branch: On a landing the arms come out to the sides for balance while the knees stay buckled, then ease back into the stride.

Already on this branch: A climb keeps both hands on a long line while they swap reach and pull. A wall run presses the wall hand with the stride, and the outer hand stays straight.

Already on this branch: The punch windup cocks the fist beside the head, clear of the chest. Windup time is still 0.12s.

Already on this branch: After an air dash the arms stay in the hang and ease into the fall or the run. They do not throw back a second time.

Already on this branch: A tag catch is a long V of arms in front of the chest, with both knees bent. It stays distinct from the new It's claim.

Already on this branch: Starting a run keeps the hands out of the hips. Resting arms do not pick up extra roll.

Already on this branch: Skiing eases into a lower glide with the arms out, then eases back into the run. Jet stays off.

Already on this branch: Leaving a wall run eases into the fall or the run. The wall hand does not snap off the wall.

Already on this branch: Leaving a climb eases into the fall or the run. The reaching hand does not snap off the wall.

Already on this branch: A grapple pull, only while the gate is on, reaches both arms in a long line with the legs long. The default gate stays off.

Already on this branch: The new It raises one arm and holds the other out, with the chest open and one knee up. It does not match the tagged runner's two-arm V.

Already on this branch: After a punch connects, the fist eases back into the run during the recover. Windup time is still 0.12s.

Already on this branch: At the top of a jump the arms hang out to the sides before the fall trail. Jump height is unchanged.

Already on this branch: Skiing stays a longer stride than the run, but the knee still bends and the arms keep a short swing. Letting go eases back into the sprint instead of popping. Ski speed is unchanged. Jet stays off.

Already on this branch: A slide stays a flat wedge with the trail leg straight. The arms sit out from the chest instead of stacking on it, and the head stays up off the knees. Slide speed is unchanged.

Already on this branch: A held crouch is a low guard: chest up, both knees bent, elbows folded in front. A slide stays the flat wedge with straight arms and one trail leg. Holding crouch still slides only when you already have speed. Slide speed is unchanged.

Already on this branch: Holding crouch on the way down pitches the chest down and folds the arms in. A normal fall still trails the arms back. The fast-fall speed is unchanged.

Already on this branch: Landing into a run still buckles both knees, then the trail leg and the arms enter the stride while the front knee is still up. A standstill land still opens both legs together. Land time is unchanged.

Already on this branch: On a run plant the arm opposite the front knee stays a long reach, a little wider than the back arm. The elbow fold sits on the back arm. Only the front knee bends. The back swing still stays short of the hip.

Already on this branch: A punch that tags holds the fist, then eases it into the new It's claim: one arm up, the other out, one knee up. A hit that does not tag still eases into the run. The tagged runner still uses the two-arm V. Windup time is unchanged.

Already on this branch: After a tag, the runner's V holds with both knees bent, then the arms and the trail leg enter the stride while one knee is still up. Standing still, both legs open together. The new It claim is unchanged. Flinch time is unchanged.

Already on this branch: At a standstill the chest breathes and sways slightly side to side. The hands stay forward and out of the hips, with no extra twist. The sway fades as the stride starts.

Already on this branch: Letting go of a sprint or a walk closes the stride under the hips instead of freezing a leg out. The last step eases into the idle sway, or into the shorter walk if you are still moving. Speed is unchanged.

Already on this branch: Walk into a sprint, and sprint back into a walk, eases the stride length and the step rate. The feet keep moving, and the hips stay level. Speed is unchanged.

Already on this branch: A sharp turn while walking or standing plants the outside foot. The chest and the hips lean together, so the waist does not twist. The hands stay out from the hips. Look speed is unchanged.

Already on this branch: A short hop into a walk bends the knees, then the stride comes back under the hips. The arms stay in the walk. A hard landing still brings the arms out. Land time is unchanged.

Already on this branch: Letting go of a sprint settles the last hip sway into the idle breath. The hips do not freeze flat, and they do not pop sideways when the sway starts. Speed is unchanged.

Already on this branch: From a stand, the first step pushes off the planted foot into the stride. The idle sway fades as the walk starts. The feet do not skate. Speed is unchanged.

Already on this branch: From a run, a crouch or a slide drops into the pose. The speed you already have carries. Letting go returns to the stride under the hips. The slide does not speed up.

Already on this branch: After an air dash, the feet come back into the stride under the hips. They do not skate. The dash is still a short burst, and the cooldown is unchanged.

Already on this branch: On a wall run or a climb, the hand meets the surface, then the swing starts. The arm does not pop. Letting go still returns under the hips. The exit time is unchanged.

Already on this branch: After a punch, the arm opposite the front knee gets back into the stride. The hips do not stay twisted. The fist still eases out. Windup time is unchanged.

Already on this branch: If a grapple is on, the hands and the chest settle into the long line. They do not twist. The grapple stays off unless you turn it on. Range and speed are unchanged.

Already on this branch: After a tag, the hands and the chest ease into the stride together, under the hips. They do not stay folded and then pop. One knee can still be up. Flinch time is unchanged.

Already on this branch: After you become It, the hands and the chest ease into the stride. They do not stay in the claim and then pop. One knee can still be up. The claim time is unchanged.

Already on this branch: Going between a ski and a run eases the feet under the hips. They do not skate, and the hips do not pop. Speed is unchanged.

Already on this branch: A hard landing from a stand eases the knees into the idle breath and sway. The arms do not lock in the flare. A short hop still does not flare the arms. The landing does not feel longer.

Already on this branch: An air crouch reads as a crouch in the air. The arms ease into the landing. They do not pop. The fast fall is unchanged.

Already on this branch: An air dash pitches the chest and throws the arms wide for the short burst. They read, then ease back. The burst and the cooldown are unchanged.

Already on this branch: Leaving a wall eases the hips and the feet into the stride. They do not pop. The hands still take the same time to leave. A climb leaves the same way.

Already on this branch: While you run, the reaching arm follows the look and stays clear of the hip. It does not fight the stride. Look speed is unchanged.

Already on this branch: Letting go of a slide stands up into the run under the hips. The feet do not pop. The slide does not speed up.

Already on this branch: A jump pushes off the planted foot. The other knee comes up, then the tuck. The jump does not go higher.

Already on this branch: A punch cocks beside the head and holds that beat, then strikes. The cock does not feel longer.

Already on this branch: Stopping from a sprint plants the last foot under the hip, then the idle sway. The foot does not skate. Speed is unchanged.

Already on this branch: In the air, the arms stay clear of the torso. The jump tuck is unchanged. Look speed is unchanged.

Already on this branch: Starting or stopping a walk keeps the hands forward and out. They do not drift into the hips. Speed is unchanged.

Already on this branch: A crouch walk is a short shuffle under the hips. The feet do not skate. The crouch does not speed up.

Already on this branch: Tagged while standing, the hands and the chest ease into the idle breath. They do not freeze and then pop. Flinch time is unchanged.

Already on this branch: A hard landing into a sprint brings the arms into the stride under the hips. They flare, then ease, and do not lock. A short hop still does not flare. The landing does not feel longer.

Already on this branch: A turn while sprinting plants the outside foot. The chest and the hips lean together. The waist does not twist. Speed is unchanged.

Already on this branch: Becoming It while standing eases the hands and the chest into the idle breath. They do not freeze and then pop. One knee can still be up. The claim does not feel longer.

Already on this branch: Letting go of a grapple eases the hands and the chest out of the long line. They do not twist. The grapple stays off unless you turn it on.

Already on this branch: A punch that misses while standing eases the fists into the idle hands. They do not freeze. The cock does not feel longer.

Already on this branch: When the dash is ready again, the chest and the arms settle. The dash does not last longer, and it still has to recharge.

Already on this branch: A walk turn plants the outside foot at a medium turn. It does not wait for a sharp yaw. Look speed is unchanged.

Already on this branch: After an air dash, a soft landing bends the knees and keeps the arms in the stride. They do not flare. The burst and the cooldown are unchanged.

Already on this branch: Letting go of a crouch into a stand eases the hips into the idle breath. They do not pop. Speed is unchanged.

Already on this branch: A walk into a sprint pushes off the back foot, then the stride opens. The feet do not skate. Speed is unchanged.

Already on this branch: A soft landing into a walk settles the knees into the stride. It does not come to a stop. A hard landing still absorbs.

Already on this branch: Letting a slide die into a stand brings the body up into the idle breath. The hips do not pop. Speed is unchanged.

Already on this branch: Letting go of a crouch walk raises the hips into the stride. They do not hitch. Speed is unchanged.

Already on this branch: A sprint into a walk closes the stride with the step. The feet do not skate to a stop. Speed is unchanged.

Already on this branch: A walk turn into a sprint plants the outside foot, then the stride opens. The feet do not skate. Speed is unchanged.

Already on this branch: A hard landing into a walk absorbs, then takes a step. It does not sit in the idle. A stand still absorbs.

Already on this branch: After a jump, the arms ease into the look pose in the air. They do not snap. Look speed is unchanged. Jump height is unchanged.

Already on this branch: Leaving a wall into a walk eases the hands into the stride. They do not hitch. The leave time is unchanged.

Already on this branch: Leaving a climb into a walk eases the hands into the stride. They do not hitch. The leave time is unchanged.

Already on this branch: An air crouch into a soft landing opens into the absorb. It does not stay folded and then pop. The fast fall is unchanged.

Already on this branch: A punch that misses while walking returns the hands to the stride. They do not drop into the idle. The cock does not feel longer.

Already on this branch: After a tag while walking, the arms settle into the stride. They do not drop into the idle. Flinch time is unchanged.

Already on this branch: After you become It while walking, the arms settle into the stride. They do not drop into the idle. The claim does not feel longer.

Already on this branch: Letting go of a grapple while walking returns the hands to the stride. They do not hitch. The grapple stays off unless you turn it on.

Already on this branch: When the dash is ready and you are standing, the chest and the arms give a small pulse, then the idle breath. The dash does not last longer, and it still has to recharge.

Already on this branch: Letting go of a ski into a walk returns the stride. The feet do not skate. Speed is unchanged.

Already on this branch: A walk into a ski eases the legs into the glide. They do not snap. Speed is unchanged.

Already on this branch: A sprint into a ski closes the stride into the glide. It does not pop. Speed is unchanged.

Already on this branch: Letting go of a ski into a sprint opens the glide into the stride. It does not pop. Speed is unchanged.

Already on this branch: A crouch walk into a sprint raises the hips and opens the stride. It does not pop. Speed is unchanged.

Already on this branch: A soft landing into a sprint absorbs, then opens into the stride. It does not stop. A hard landing still absorbs.

Already on this branch: A hard landing into a sprint absorbs, then opens into the stride. It does not sit in the buckle. A hard landing into a walk still takes a step.

Already on this branch: Leaving a wall into a sprint opens the hands into the stride. They do not hitch. The leave time is unchanged.

Already on this branch: Leaving a climb into a sprint opens the hands into the stride. They do not hitch. The leave time is unchanged.

Already on this branch: A punch that misses while sprinting returns the hands to the stride. They do not stay in the limp. The cock does not feel longer.

Already on this branch: After a tag while sprinting, the arms settle into the stride. They do not stay folded. Flinch time is unchanged.

Already on this branch: After you become It while sprinting, the arms settle into the stride. They do not stay folded. One knee can still be up. Claim time is unchanged.

Already on this branch: Letting go of a grapple while sprinting returns the hands to the stride. They do not hitch. The gate stays off. Range and speed are unchanged.

Already on this branch: Letting a slide die into a walk brings the body up into the stride. The hips do not pop. A slide into a stand still rises into the idle breath. A slide into a sprint is unchanged. Speed is unchanged.

Already on this branch: Letting a slide die into a sprint brings the body up into the long stride. The hips do not pop. A slide into a walk still rises into the walk. A slide into a stand still rises into the idle breath. Speed is unchanged.

Already on this branch: A still crouch into a sprint raises the hips into the long stride. They do not pop. A still crouch into a stand still rises into the idle breath. A crouch walk into a sprint is unchanged. Speed is unchanged.

Already on this branch: An air dash into a walk ends in the stride. It does not come to a stop. An air dash into a sprint is unchanged. Duration and cooldown are unchanged.

Already on this branch: An air dash into a sprint ends in the long stride. It does not come to a stop. An air dash into a walk still ends in the walk. Duration and cooldown are unchanged.

Already on this branch: A jump into a crouch walk absorbs into the low stride. The hips stay down. A still crouch keeps the old absorb. Land time is unchanged.

Already on this branch: A soft landing into a still crouch absorbs into the guard. The hips stay down. A hard landing keeps the old absorb. A crouch walk still absorbs into the low stride. Land time is unchanged.

Already on this branch: A hard landing into a still crouch absorbs deeper into the guard. The hips stay down. A soft landing still uses the lighter guard. A crouch walk still absorbs into the low stride. Land time is unchanged.

Already on this branch: A jump into a still crouch settles into the guard in the air. The push still reads. The fall dart is unchanged. An air dash is unchanged. Jump height is unchanged.

Already on this branch: An air dash into a still crouch ends in the guard. A crouch walk keeps the stride leave. Duration and cooldown are unchanged.

Already on this branch: An air dash into a crouch walk ends in the low stride. A still crouch still ends in the guard. An upright walk still ends in the stride. Duration and cooldown are unchanged.

Already on this branch: A still crouch into a ski eases the guard into the glide. It does not pop. A walk into a ski is unchanged. A sprint into a ski is unchanged. Speed is unchanged. Jet stays off.

Already on this branch: Letting go of a ski into a still crouch eases the glide into the guard. It does not pop. A still crouch into a ski is unchanged. A walk out of a ski is unchanged. Speed is unchanged. Jet stays off.

Already on this branch: Letting go of a ski into a crouch walk eases the glide into the low stride. A still crouch still ends in the guard. A walk out of a ski is unchanged. Speed is unchanged. Jet stays off.

Already on this branch: A crouch walk into a ski eases the low stride into the glide. It does not pop. A still crouch into a ski is unchanged. A walk into a ski is unchanged. Speed is unchanged. Jet stays off.

Already on this branch: A slide into a still crouch eases the wedge into the guard. It does not snap. A slide into a stand still rises into the idle breath. A slide into a walk is unchanged. Speed is unchanged.

Already on this branch: A still crouch into a slide eases the guard into the wedge. It does not snap. A slide into a still crouch still eases into the guard. A slide into a walk is unchanged. Speed is unchanged.

Already on this branch: A slide into a crouch walk eases the wedge into the low stride. A still crouch still ends in the guard. A slide into a walk is unchanged. Speed is unchanged.

Already on this branch: A crouch walk into a slide eases the low stride into the wedge. It does not snap. A still crouch into a slide is unchanged. A slide into a crouch walk is unchanged. Speed is unchanged.

Already on this branch: A still crouch into a climb eases the guard onto the wall. A wall run from that crouch does the same. A normal climb is unchanged. The entry time is unchanged.

Already on this branch: A climb into a still crouch eases into the guard. A wall leave is unchanged. A climb into a walk is unchanged. The leave time is unchanged.

Already on this branch: A wall run into a still crouch eases into the guard. A climb into a still crouch is unchanged. A wall run into a walk is unchanged. The leave time is unchanged.

Already on this branch: A climb into a crouch walk eases into the low stride. A still crouch still ends in the guard. A wall run into a crouch walk is unchanged. The leave time is unchanged.

Already on this branch: A wall run into a crouch walk eases into the low stride. A climb into a crouch walk is unchanged. A still crouch still ends in the guard. The leave time is unchanged.

Already on this branch: A punch that misses in a still crouch eases into the guard. A standing miss still eases into the idle hang. A walk miss and a sprint miss are unchanged. Windup time is unchanged.

Already on this branch: A tag in a still crouch eases the V into the guard. A standing tag still eases into the idle breath. A walk tag and a sprint tag are unchanged. Flinch time is unchanged.

Already on this branch: Becoming It in a still crouch eases the claim into the guard. A standing claim still eases into the idle breath. A walk claim and a sprint claim are unchanged. Claim time is unchanged.

Already on this branch: Letting go of a grapple in a still crouch eases the line into the guard. A standing release is unchanged. A walk release and a sprint release are unchanged. The pull is unchanged. The gate stays off.

Already on this branch: A dash coming off cooldown in a still crouch pulses inside the guard. A standing ready still pulses into the idle breath. A moving ready is unchanged. Duration and cooldown are unchanged.

Already on this branch: A punch that misses in a crouch walk eases into the guard and the low stride. A still crouch still ends in the guard. A walk miss and a sprint miss are unchanged. Windup time is unchanged.

Already on this branch: A tag in a crouch walk eases the V into the guard and the low stride. A still crouch still ends in the guard. A walk tag and a sprint tag are unchanged. Flinch time is unchanged.

Already on this branch: Becoming It in a crouch walk eases the claim into the guard and the low stride. A still crouch still ends in the guard. A walk claim and a sprint claim are unchanged. Claim time is unchanged.

Already on this branch: Letting go of a grapple in a crouch walk eases the line into the guard and the low stride. A still crouch still ends in the guard. A walk release and a sprint release are unchanged. The pull is unchanged. The gate stays off.

Already on this branch: A dash coming off cooldown in a crouch walk pulses inside the guard and the low stride. A still crouch still pulses inside the guard. A standing ready still pulses into the idle breath. A moving ready is unchanged. Duration and cooldown are unchanged.

Already on this branch: A soft landing into a crouch walk absorbs into the low stride. A hard landing into a crouch walk is unchanged. A soft landing into a still crouch is unchanged. Land time is unchanged.

Already on this branch: A hard landing into a crouch walk absorbs deeper into the low stride. A soft landing into a crouch walk stays lighter. A hard landing into a still crouch is unchanged. Land time is unchanged.

Already on this branch: A ski into a slide eases the glide into the wedge. A walk into a ski is unchanged. A slide into a stand is unchanged. Ski speed is unchanged. Jet stays off.

Already on this branch: A slide into a ski eases the wedge into the glide. A ski into a slide is unchanged. A walk into a ski is unchanged. Ski speed is unchanged. Jet stays off.

Already on this branch: An air crouch into a crouch walk eases into the low stride. A still air crouch keeps the dart. An air dash into a crouch walk is unchanged. Fall speed is unchanged. Jump height is unchanged.

Already on this branch: An air crouch into a still crouch lands the dart into the guard. A moving air crouch keeps the flare. A still crouch without the dart is unchanged. Fall speed is unchanged. Land time is unchanged.

Already on this branch: An air crouch into a soft land opens the dart into the absorb. A hard landing keeps the flare. A still crouch keeps the guard. A moving air crouch keeps the flare. Land time is unchanged.

Already on this branch: A still crouch into a jump eases the guard into the push. A standing jump is unchanged. A crouch walk into a jump is unchanged. Jump height is unchanged.

Already on this branch: A crouch walk into a jump eases the low stride into the push. A still crouch into a jump is unchanged. A standing jump is unchanged. Jump height is unchanged.

Already on this branch: A ski into a jump eases the glide into the push. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A standing jump is unchanged. Jump height is unchanged. Ski speed is unchanged.

Already on this branch: A slide into a jump eases the wedge into the push. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A standing jump is unchanged. Jump height is unchanged. slideBoost stays 0.

Already on this branch: A jump into a ski eases the glide or the landing into the stride. A walk into a ski is unchanged. A sprint into a ski is unchanged. A crouch into a ski is unchanged. A slide into a ski is unchanged. Ski speed is unchanged. Jump height is unchanged.

Already on this branch: A jump into a slide eases the glide or the landing into the wedge. A crouch into a slide is unchanged. A ski into a slide is unchanged. A jump into a ski is unchanged. Jump height is unchanged. slideBoost stays 0.

Already on this branch: An air dash into a jump eases the burst into the push. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. Jump height is unchanged. Duration and cooldown are unchanged.

Already on this branch: A climb into a jump eases the climb into the push. An air dash into a jump is unchanged. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. Jump height is unchanged. Exit time is unchanged.

Already on this branch: A wall run into a jump eases the wall exit into the push. A climb into a jump is unchanged. An air dash into a jump is unchanged. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. Jump height is unchanged. Exit time is unchanged.

Already on this branch: A jump into a climb eases the contact into the grab. A crouch onto the wall is unchanged. A wall run is unchanged. A climb into a jump is unchanged. Jump height is unchanged. The meet time is unchanged.

Already on this branch: A jump into a wall run eases the contact into the attach. A jump into a climb is unchanged. A crouch onto the wall is unchanged. A wall run into a jump is unchanged. Jump height is unchanged. The meet time is unchanged.

Already on this branch: An air crouch into a jump eases the dart into the push. A moving fall uses the low stride. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. An air dash into a jump is unchanged. A climb into a jump is unchanged. A wall run into a jump is unchanged. Jump height is unchanged. Fall speed is unchanged.

Already on this branch: A jump into an air crouch eases the apex and the descent into the dart. An air crouch into a jump is unchanged. A moving fall still ends in the low stride. A still crouch into a jump is unchanged. Jump height is unchanged. Fall speed is unchanged.

Already on this branch: A jump into an air dash eases the apex into the burst. The burst still holds. An air dash into a jump is unchanged. A jump into an air crouch is unchanged. Jump height is unchanged. Duration and cooldown are unchanged.

Already on this branch: A soft landing into a jump eases the absorb into the push. A hard landing keeps its jump. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. An air dash into a jump is unchanged. A climb into a jump is unchanged. A wall run into a jump is unchanged. Jump height is unchanged. The landing is unchanged when you stay down.

Already on this branch: A hard landing into a jump eases the absorb into the push. A soft landing into a jump is unchanged. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. An air dash into a jump is unchanged. A climb into a jump is unchanged. A wall run into a jump is unchanged. Jump height is unchanged. The landing is unchanged when you stay down.

Already on this branch: A punch miss into a jump eases the whiff into the push. A crouch miss is unchanged. A soft landing into a jump is unchanged. A hard landing into a jump is unchanged. Jump height is unchanged.

Already on this branch: A tag into a jump eases the connect into the push. A crouch tag is unchanged. A punch miss into a jump is unchanged. Jump height is unchanged.

Already on this branch: Becoming It into a jump eases the claim into the push. A crouch claim is unchanged. A tag into a jump is unchanged. Jump height is unchanged.

Already on this branch: A grapple release into a jump eases the line into the push. A crouch release is unchanged. Becoming It into a jump is unchanged. Jump height is unchanged. The gate stays off.

Already on this branch: A dash coming off cooldown into a jump eases the pulse into the push. A crouch ready is unchanged. A grapple release into a jump is unchanged. Jump height is unchanged. Duration and cooldown are unchanged.

Already on this branch: A jump into a punch eases the apex or the landing into the windup. A punch from the ground is unchanged. Jump height is unchanged. Windup time is unchanged.

Already on this branch: A jump into a tag eases the apex or the landing into the connect. A crouch tag is unchanged. A jump into a punch is unchanged. A tag into a jump is unchanged. Jump height is unchanged.

Already on this branch: An air crouch into an air dash eases the dart into the burst. A moving fall uses the low stride. The burst still holds. Fall speed is unchanged. Duration and cooldown are unchanged. A jump into a tag is unchanged.

Already on this branch: An air dash into an air crouch eases the burst into the dart. An air crouch into an air dash is unchanged. The burst still holds. Fall speed is unchanged. Duration and cooldown are unchanged.

Already on this branch: A ski into an air dash eases the glide into the burst. The burst still holds. An air crouch into an air dash is unchanged. An air dash into an air crouch is unchanged. Ski speed is unchanged. Duration and cooldown are unchanged.

Already on this branch: A slide into an air dash eases the wedge into the burst. The burst still holds. A ski into an air dash is unchanged. The slide does not speed up. Duration and cooldown are unchanged.

Already on this branch: An air dash into a ski eases the burst into the glide. A slide into an air dash is unchanged. A ski into an air dash is unchanged. Ski speed is unchanged. Duration and cooldown are unchanged.

Already on this branch: An air dash into a slide eases the burst into the wedge. An air dash into a ski is unchanged. A ski into an air dash is unchanged. A slide into an air dash is unchanged. The slide does not speed up. Duration and cooldown are unchanged.

Already on this branch: A climb into an air dash eases the climb into the burst. The burst still holds. An air dash into a slide is unchanged. A ski into an air dash is unchanged. A slide into an air dash is unchanged. Exit time is unchanged. Duration and cooldown are unchanged.

Already on this branch: A wall run into an air dash eases the wall exit into the burst. The burst still holds. A climb into an air dash is unchanged. Exit time is unchanged. Duration and cooldown are unchanged.

Already on this branch: An air dash into a climb eases the burst into the grab. A wall run into an air dash is unchanged. A climb into an air dash is unchanged. Exit time is unchanged. Duration and cooldown are unchanged.

Already on this branch: An air dash into a wall run eases the burst into the attach. An air dash into a climb is unchanged. A climb into an air dash is unchanged. A wall exit into an air dash is unchanged. Exit time is unchanged. Duration and cooldown are unchanged.

Already on this branch: A punch miss into an air dash eases the whiff into the burst. The burst still holds. An air dash into a wall run is unchanged. An air dash into a climb is unchanged. A climb into an air dash is unchanged. A wall exit into an air dash is unchanged. A punch miss into a jump is unchanged. A crouch miss is unchanged. Duration and cooldown are unchanged.

Already on this branch: A tag into an air dash eases the connect into the burst. The burst still holds. A punch miss into an air dash is unchanged. A tag into a jump is unchanged. A crouch tag is unchanged. Duration and cooldown are unchanged.

Already on this branch: Becoming It into an air dash eases the claim into the burst. The burst still holds. A tag into an air dash is unchanged. Becoming It into a jump is unchanged. A crouch claim is unchanged. Duration and cooldown are unchanged.

Already on this branch: A grapple release into an air dash eases the line into the burst. The burst still holds. Becoming It into an air dash is unchanged. A grapple release into a jump is unchanged. A crouch release is unchanged. The gate stays off. Duration and cooldown are unchanged.

Already on this branch: An air dash into a punch eases the burst into the windup. A grapple release into an air dash is unchanged. A jump into a punch is unchanged. Windup time is unchanged. Duration and cooldown are unchanged.

Already on this branch: An air dash into a tag eases the burst into the connect. An air dash into a punch is unchanged. A jump into a tag is unchanged. A crouch tag is unchanged. Duration and cooldown are unchanged.

Already on this branch: A soft landing into an air dash eases the absorb into the burst. The burst still holds. An air dash into a tag is unchanged. A soft landing into a jump is unchanged. A hard landing is unchanged. Land time is unchanged. Duration and cooldown are unchanged.

Already on this branch: A hard landing into an air dash eases the deeper absorb into the burst. The burst still holds. A soft landing into an air dash is unchanged. A hard landing into a jump is unchanged. Land time is unchanged. Duration and cooldown are unchanged.

Already on this branch: A soft landing into a punch eases the absorb into the windup. A hard landing into an air dash is unchanged. A jump into a punch is unchanged. A soft landing into a jump is unchanged. Windup time is unchanged. Land time is unchanged.

Already on this branch: A hard landing into a punch eases the deeper absorb into the windup. A soft landing into a punch is unchanged. A jump into a punch is unchanged. A hard landing into a jump is unchanged. Windup time is unchanged. Land time is unchanged.

Already on this branch: A soft landing into a tag eases the absorb into the connect. A hard landing into a punch is unchanged. A jump into a tag is unchanged. A soft landing into a jump is unchanged. A crouch tag is unchanged. Connect time is unchanged. Land time is unchanged.

Already on this branch: A hard landing into a tag eases the deeper absorb into the connect. A soft landing into a tag is unchanged. A jump into a tag is unchanged. A hard landing into a jump is unchanged. A crouch tag is unchanged. Connect time is unchanged. Land time is unchanged.

Already on this branch: A ski into a punch eases the glide into the windup. A hard landing into a tag is unchanged. A ski into a jump is unchanged. A ski into an air dash is unchanged. A jump into a punch is unchanged. Windup time is unchanged. Ski speed is unchanged.

Already on this branch: A slide into a punch eases the wedge into the windup. A ski into a punch is unchanged. A slide into a jump is unchanged. A slide into an air dash is unchanged. slideBoost stays 0. Windup time is unchanged.

Already on this branch: A ski into a tag eases the glide into the connect. A slide into a punch is unchanged. A ski into a punch is unchanged. A ski into a jump is unchanged. A jump into a tag is unchanged. A crouch tag is unchanged. Connect time is unchanged. Ski speed is unchanged.

Already on this branch: A slide into a tag eases the wedge into the connect. A ski into a tag is unchanged. A slide into a punch is unchanged. A slide into a jump is unchanged. A crouch tag is unchanged. slideBoost stays 0. Connect time is unchanged.

Already on this branch: A climb into a punch eases the grab into the windup. A slide into a tag is unchanged. A climb into an air dash is unchanged. A climb into a jump is unchanged. A wall run is unchanged. Windup time is unchanged. Exit time is unchanged.

Already on this branch: A climb into a tag eases the grab into the connect. A climb into a punch is unchanged. A climb into an air dash is unchanged. A climb into a jump is unchanged. A wall run is unchanged. Connect time is unchanged. Exit time is unchanged.

Already on this branch: A wall exit into a punch eases the leave into the windup. A climb into a tag is unchanged. A wall exit into an air dash is unchanged. A wall exit into a jump is unchanged. A climb into a punch is unchanged. Windup time is unchanged. Exit time is unchanged.

Already on this branch: A wall exit into a tag eases the leave into the connect. A wall exit into a punch is unchanged. A wall exit into an air dash is unchanged. A wall exit into a jump is unchanged. A climb into a tag is unchanged. Connect time is unchanged. Exit time is unchanged.

Already on this branch: An air crouch into a punch eases the dart into the windup. A wall exit into a tag is unchanged. An air crouch into an air dash is unchanged. An air crouch into a jump is unchanged. A jump into a punch is unchanged. Windup time is unchanged. Fall speed is unchanged.

Already on this branch: An air crouch into a tag eases the dart into the connect. An air crouch into a punch is unchanged. An air crouch into an air dash is unchanged. An air crouch into a jump is unchanged. A jump into a tag is unchanged. A crouch tag is unchanged. Connect time is unchanged. Fall speed is unchanged.

Already on this branch: Becoming It into a punch eases the claim into the windup. An air crouch into a tag is unchanged. Becoming It into an air dash is unchanged. Becoming It into a jump is unchanged. A crouch claim is unchanged. Windup time is unchanged. Claim time is unchanged.

Already on this branch: Becoming It into a tag eases the claim into the connect. Becoming It into a punch is unchanged. Becoming It into an air dash is unchanged. Becoming It into a jump is unchanged. An air crouch into a tag is unchanged. A crouch claim is unchanged. Connect time is unchanged. Claim time is unchanged.

Already on this branch: A grapple release into a punch eases the line into the windup. Becoming It into a tag is unchanged. A grapple release into an air dash is unchanged. A grapple release into a jump is unchanged. A crouch release is unchanged. Windup time is unchanged. The gate stays off.

Already on this branch: A grapple release into a tag eases the line into the connect. A grapple release into a punch is unchanged. A grapple release into an air dash is unchanged. A grapple release into a jump is unchanged. Becoming It into a tag is unchanged. A crouch release is unchanged. Connect time is unchanged. The gate stays off.

Already on this branch: A dash coming off cooldown into a punch eases the pulse into the windup. A grapple release into a tag is unchanged. A dash coming off cooldown into a jump is unchanged. A crouch ready is unchanged. Windup time is unchanged. Duration and cooldown are unchanged.

Already on this branch: A dash coming off cooldown into a tag eases the pulse into the connect. A dash coming off cooldown into a punch is unchanged. A dash coming off cooldown into a jump is unchanged. A crouch ready is unchanged. A grapple release into a tag is unchanged. Connect time is unchanged. Duration and cooldown are unchanged.

Already on this branch: A punch into a tag eases the cock or the strike into the connect. A dash coming off cooldown into a tag is unchanged. A dash coming off cooldown into a punch is unchanged. A crouch tag is unchanged. Connect time is unchanged. Windup time is unchanged.

Already on this branch: A tag into a punch eases the connect into the windup. A punch into a tag is unchanged. A dash coming off cooldown into a punch is unchanged. A crouch tag is unchanged. A tag into a jump is unchanged. Windup time is unchanged. Connect time is unchanged.

Already on this branch: A punch miss into a tag eases the whiff into the connect. A tag into a punch is unchanged. A punch into a tag is unchanged. A punch miss into a jump is unchanged. A punch miss into an air dash is unchanged. A crouch miss is unchanged. Connect time is unchanged.

Already on this branch: A punch into a ski eases the cock or the strike into the glide. A ski into a punch is unchanged. A punch miss into a tag is unchanged. A slide into a ski is unchanged. A jump into a ski is unchanged. Ski speed is unchanged. Windup time is unchanged.

Already on this branch: A punch into a slide eases the cock or the strike into the wedge. A punch into a ski is unchanged. A slide into a punch is unchanged. A crouch into a slide is unchanged. A ski into a slide is unchanged. A jump into a slide is unchanged. slideBoost stays 0. Windup time is unchanged.

Already on this branch: A tag into a ski eases the connect into the glide. A punch into a slide is unchanged. A punch into a ski is unchanged. A tag into a jump is unchanged. A slide into a ski is unchanged. Ski speed is unchanged. Connect time is unchanged.

Already on this branch: A tag into a slide eases the connect into the wedge. A tag into a ski is unchanged. A punch into a slide is unchanged. A slide into a tag is unchanged. A crouch into a slide is unchanged. A ski into a slide is unchanged. A jump into a slide is unchanged. slideBoost stays 0. Connect time is unchanged.

Already on this branch: A still crouch into a punch eases the guard into the windup. A tag into a slide is unchanged. A crouch walk into a punch is unchanged. An air crouch into a punch is unchanged. A slide into a punch is unchanged. A crouch into a slide is unchanged. A crouch claim is unchanged. Windup time is unchanged.

Already on this branch: A still crouch into a tag eases the guard into the connect. A still crouch into a punch is unchanged. A crouch walk into a tag is unchanged. An air crouch into a tag is unchanged. A slide into a tag is unchanged. A crouch claim is unchanged. A punch into a tag is unchanged. Connect time is unchanged.

Already on this branch: A soft landing into a ski eases the absorb into the glide. A still crouch into a tag is unchanged. A hard landing into a ski is unchanged. A jump into a ski is unchanged. A punch into a ski is unchanged. A slide into a ski is unchanged. Ski speed is unchanged. Land time is unchanged.

Already on this branch: A soft landing into a slide eases the absorb into the wedge. A soft landing into a ski is unchanged. A hard landing into a slide is unchanged. A jump into a slide is unchanged. A punch into a slide is unchanged. A crouch into a slide is unchanged. A ski into a slide is unchanged. slideBoost stays 0. Land time is unchanged.

Already on this branch: A climb into a ski eases the grab into the glide. A soft landing into a slide is unchanged. A wall run into a ski is unchanged. A climb into a jump is unchanged. A climb into a punch is unchanged. A jump into a ski is unchanged. Ski speed is unchanged. Exit time is unchanged.

Already on this branch: A climb into a slide eases the grab into the wedge. A climb into a ski is unchanged. A wall run into a slide is unchanged. A climb into a jump is unchanged. A soft landing into a slide is unchanged. A jump into a slide is unchanged. slideBoost stays 0. Exit time is unchanged.

Already on this branch: A wall run into a ski eases the leave into the glide. A climb into a slide is unchanged. A climb into a ski is unchanged. A wall run into a jump is unchanged. A soft landing into a ski is unchanged. Ski speed is unchanged. Exit time is unchanged.

Already on this branch: A wall run into a slide eases the leave into the wedge. A wall run into a ski is unchanged. A climb into a slide is unchanged. A wall run into a jump is unchanged. A soft landing into a slide is unchanged. A jump into a slide is unchanged. slideBoost stays 0. Exit time is unchanged.

Already on this branch: An air dash into a ski eases the burst into the glide. A wall run into a slide is unchanged. A wall run into a ski is unchanged. A climb into a ski is unchanged. An air dash into a slide is unchanged. Ski speed is unchanged. Duration and cooldown are unchanged.

Already on this branch: A hard landing into a ski eases the absorb into the glide. An air dash into a ski is unchanged. A soft landing into a ski is unchanged. A jump into a ski is unchanged. A hard landing into a slide is unchanged. Ski speed is unchanged. Land time is unchanged.

Already on this branch: An air crouch into a ski eases the dart into the glide. A hard landing into a ski is unchanged. An air dash into a ski is unchanged. A jump into a ski is unchanged. An air crouch into a slide is unchanged. Fall speed is unchanged. Ski speed is unchanged.

Already on this branch: A grapple release into a ski eases the line into the glide. An air crouch into a ski is unchanged. A grapple release into a jump is unchanged. A grapple release into a punch is unchanged. An air dash into a ski is unchanged. The gate stays off. Ski speed is unchanged.

Already on this branch: A grapple release into a slide eases the line into the wedge. A grapple release into a ski is unchanged. A grapple release into a jump is unchanged. A wall run into a slide is unchanged. A soft landing into a slide is unchanged. slideBoost stays 0. The gate stays off.

Already on this branch: Becoming It into a ski eases the claim into the glide. A grapple release into a slide is unchanged. Becoming It into a punch is unchanged. Becoming It into a jump is unchanged. A grapple release into a ski is unchanged. Claim time is unchanged. Ski speed is unchanged.

Already on this branch: Becoming It into a slide eases the claim into the wedge. Becoming It into a ski is unchanged. Becoming It into a punch is unchanged. Becoming It into a jump is unchanged. A grapple release into a slide is unchanged. slideBoost stays 0. Claim time is unchanged.

Already on this branch: A dash coming off cooldown into a ski eases the pulse into the glide. Becoming It into a slide is unchanged. A dash coming off cooldown into a punch is unchanged. A dash coming off cooldown into a tag is unchanged. A dash coming off cooldown into a jump is unchanged. Duration and cooldown are unchanged. Ski speed is unchanged.

Already on this branch: A dash coming off cooldown into a slide eases the pulse into the wedge. A dash coming off cooldown into a ski is unchanged. A dash coming off cooldown into a punch is unchanged. A dash coming off cooldown into a tag is unchanged. A dash coming off cooldown into a jump is unchanged. slideBoost stays 0. Duration and cooldown are unchanged.

Already on this branch: A punch miss into a ski eases the whiff into the glide. A dash coming off cooldown into a slide is unchanged. A punch into a ski is unchanged. A punch miss into a tag is unchanged. A punch miss into a jump is unchanged. A punch miss into an air dash is unchanged. Whiff time is unchanged. Ski speed is unchanged.

Already on this branch: A punch miss into a slide eases the whiff into the wedge. A punch miss into a ski is unchanged. A punch into a slide is unchanged. A punch miss into a tag is unchanged. A punch miss into a jump is unchanged. A punch miss into an air dash is unchanged. slideBoost stays 0. Whiff time is unchanged.

Already on this branch: A still crouch into a slide eases the guard into the wedge. A punch miss into a slide is unchanged. A still crouch into a punch is unchanged. A still crouch into a tag is unchanged. A still crouch into a ski is unchanged. A crouch walk into a slide is unchanged. slideBoost stays 0.

Already on this branch: A ski into a slide eases the glide into the wedge. A still crouch into a slide is unchanged. A crouch walk into a ski is unchanged. A crouch walk into a slide is unchanged. A slide into a ski is unchanged. slideBoost stays 0. Ski speed is unchanged.

Already on this branch: A slide into an air dash eases the wedge into the burst. A ski into a slide is unchanged. A ski into an air dash is unchanged. An air dash into a slide is unchanged. slideBoost stays 0. Duration and cooldown are unchanged.

Already on this branch: A slide into a jump eases the wedge into the jump. A slide into an air dash is unchanged. A ski into a jump is unchanged. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A standing jump is unchanged. slideBoost stays 0. Jump height is unchanged.

Already on this branch: A jump into an air dash eases the jump into the burst. The burst still holds. A slide into a jump is unchanged. A ski into a jump is unchanged. A slide into an air dash is unchanged. Jump height is unchanged. Duration and cooldown are unchanged.

Already on this branch: A still crouch into an air dash eases the guard into the burst. The burst still holds. A jump into an air dash is unchanged. A slide into an air dash is unchanged. An air crouch into an air dash is unchanged. Duration and cooldown are unchanged.

Already on this branch: A punch into an air dash eases the punch into the burst. The burst still holds. A still crouch into an air dash is unchanged. A punch miss into an air dash is unchanged. A tag into an air dash is unchanged. Windup time is unchanged. Duration and cooldown are unchanged.

Already on this branch: A crouch walk into an air dash eases the low stride into the burst. The burst still holds. A hard landing into a jump is unchanged. A soft landing into a jump is unchanged. A still crouch into an air dash is unchanged. Duration and cooldown are unchanged.

Already on this branch: A dash coming off cooldown into an air dash eases the pulse into the burst. The burst still holds. A crouch walk into an air dash is unchanged. A dash coming off cooldown into a jump is unchanged. A dash coming off cooldown into a punch is unchanged. Duration and cooldown are unchanged.

Already on this branch: A punch into a jump eases the punch into the jump. Becoming It into a jump is unchanged. A tag into a jump is unchanged. A punch miss into a jump is unchanged. Windup time is unchanged. Jump height is unchanged.

Already on this branch: A run into a ski eases the stride into the glide. The glide then holds. A walk into a ski is unchanged. A slide into a ski is unchanged. A jump into a ski is unchanged. Ski speed is unchanged.

Already on this branch: A run into a slide eases the stride into the wedge. The wedge then holds. A run into a ski is unchanged. A ski into a slide is unchanged. A still crouch into a slide is unchanged. slideBoost stays 0.

Already on this branch: A jump into a still crouch eases the jump into the guard. The guard then holds. A run into a slide is unchanged. A jump into a ski is unchanged. A jump into a slide is unchanged. Jump height is unchanged.

Already on this branch: An air dash into a still crouch eases the burst into the guard. The guard then holds. A jump into a still crouch is unchanged. A still crouch into an air dash is unchanged. Duration and cooldown are unchanged.

Already on this branch: A walk into a slide eases the walk into the wedge. The wedge then holds. An air dash into a still crouch is unchanged. A run into a slide is unchanged. A crouch walk into a slide is unchanged. slideBoost stays 0.

Already on this branch: A walk into a ski eases the walk into the glide. The glide then holds. A walk into a slide is unchanged. A run into a ski is unchanged. A crouch walk into a ski is unchanged. Ski speed is unchanged.

Already on this branch: A crouch walk into a jump eases the low stride into the jump. The jump then holds. A walk into a ski is unchanged. A still crouch into a jump is unchanged. A crouch walk into an air dash is unchanged. Jump height is unchanged.

Already on this branch: A walk into a jump eases the walk into the jump. The jump then holds. A crouch walk into a jump is unchanged. A walk into a ski is unchanged. A still crouch into a jump is unchanged. Jump height is unchanged.

Already on this branch: A walk into an air dash eases the walk into the burst. The burst then holds. A walk into a jump is unchanged. A crouch walk into an air dash is unchanged. A still crouch into an air dash is unchanged. Duration and cooldown are unchanged.

Already on this branch: A still crouch into a jump eases the guard into the jump. The jump then holds. A walk into an air dash is unchanged. A crouch walk into a jump is unchanged. A jump into a still crouch is unchanged. Jump height is unchanged.

Already on this branch: A walk into a punch eases the walk into the cock. The windup then holds. The strike is unchanged. A still crouch into a jump is unchanged. A jump into a punch is unchanged. A still crouch into a punch is unchanged. Windup time is unchanged.

Already on this branch: A walk into a tag eases the walk into the connect. The connect then holds. A walk into a punch is unchanged. A jump into a tag is unchanged. A still crouch into a tag is unchanged. Connect time is unchanged.

This delta: A run into a punch eases the stride into the cock. The windup then holds. The strike is unchanged. A walk into a tag is unchanged. A walk into a punch is unchanged. A jump into a punch is unchanged. Windup time is unchanged.

## Stack snapshot

| Piece | What you get |
|-------|----------------|
| **TP cam** | `TpsMoveCamera` orbit/follow (~boom -5.2), soft collision, FOV by MoveState + slight speed look-ahead (`lookAheadMax` ~0.9, `speedFovBoostMax` ~3deg). Look-ahead **direction** is smoothed (a brake used to yaw the aim point in one frame). Strafe roll is lighter so the boom does not orbit and snap back. |
| **Hier dummy** | `DummyAvatarBinder` defaults Runner to `Dummy_Mannequin_Tan_Hier_Hi` and It to `Dummy_Mannequin_Orange_Hier_Hi`, then other `*_Hier_Hi`, flat HiPoly, and the Navy Spade primitive (foam + polymer panels + matte joints; palette aligned with `Tools/Tag/build_mannequin_hier.py`); `DummyLocomotor` swings limbs when bindable UpperArm/UpperLeg hierarchy exists. It hat grows with camera distance (clamped) and has a tall beacon so it reads across a fort. Your own chase cam hides the beacon, slows the spin, and shortens the light so the hat is not a lens blocker. |
| **Modes** | F1 Hot Potato / F2 Least It / F3 Trail Tag / **F4 Free play** (`TagModeController` SetMode + StartRound). Free play still transfers It on punch and does not end on a timer. |
| **HUD** | `SpeedEnergyHUD` (local P0): km/h + readable verb (RUN/SLIDE/DASH/WALL/CLIMB/LAND/...), **dash cooldown bar** (cyan, ready or seconds left; jet bar only if `enableJet`), ski flag, controls cheat-sheet, mode + phase + who is It. Hot Potato: fuse line **and** top-center **FUSE** pulse for everyone inside `warnSec` (not only when you are It). Least It: clock + **lowest wins** on the mode line, **WINNING (least)** / **BEHIND (more It)**; brief all-standings flash; **LEAD** (mint) / **LAG** (coral). **TAG flash** ~0.85 s names who It moved to (YOU'RE IT / YOU'RE FREE); It hat pops on handoff. **It compass** (flee) + **Prey compass** (hunt); both pulse <12 m w/ distinct tints; cam bearing + m |
| **Void / XZ** | `VoidRespawn`: Y < -20 **or** mega-park XZ AABB (+~20 m) -> nearest `LocalPlayerSpawner` pad; clear ragdoll/stun, zero vel, ~1 s punch i-frames. **F1-F4 / rematch** also `ForceRecover`, cancel land-stun, and place every pawn on a pad (P1->pad 0) so a mode switch does not resume a ragdoll in the void. |
| **Playground** | Four hopscotch courts, one per corner. West run: spawn SW, mushroom step, hopscotch SW, a low bench west of the climb net, south bar, soft-play (a bench west of the tube street, a ground slide east of the tube cap, a balance beam with mushrooms, a spring, and two hop tiles on the south apron, a climber dome west of that apron, a spring rider on the east shoulder, then rung to the 2.00 deck), spine, bars or beams, merry (west bench, south picnic, and a spring at (2.50, 24) on the west lawn), then a mushroom, spring, and two hop tiles north of merry and west of the bars, spine, north bar, arch to a mushroom/spring/hop cluster at (9.4, 45.0) then hopscotch NW, astro. East forts use that same 2.4 m rung opposite a 1.8 m ladder, plus a spiral climber (feet on the ground, top at 2.40) that the west forts do not have. East run: hopscotch SE, arch (with a mushroom/spring/hop cluster at (66.2, 7.5) south of that arch), army, bars onto open kickball (a low bench west of the bars, not in the field), swings (fall tiles clear of the north fence), bars, then knight to the west or the arch and a low bench into hopscotch NE. North of that arch, a net frame, mushroom steps, and a spring rider sit east of the bars. Crash bowl stays open. Spawn_NW faces southeast; the torso is north of that pad and off the exit. After pull: **CutArenaBootstrap** Rebuild / **PgkLandmarkPlacer -> Place**. |
| **Colliders** | `StaticPropColliders.EnsureStaticColliders` after dress/place so HiPoly/PGK toys keep Mesh/Box collision |
| **Trail Tag** | Wide bright light-cycle walls (mega-park WorldScale 10); Stay + Default-layer triggers so RB motor still eliminates; near-miss **TRAIL!** ~6.3 m foreign; elim **OUT!** / TRAIL HIT, then a persistent **OUT / waiting** line while you are frozen and the round is still going. It ribbon is brighter and ~1.35x wider (line only; collider width unchanged). Self-grace is still age **and** distance. |
| **SFX** | `TagSfx` procedural tones when a Resources clip is missing. `AudioCuePlayer` (round, trail elim, ragdoll, UI) falls back to those tones instead of staying silent. Air dash is a shorter higher whoosh than the grounded lunge. Soft land (below stun) thuds; hard land still uses `MoveAnimDriver`. Rematch from the round-end card with **R**. |
| **Ski spines** | Thicker mega-park ski spines (~3 m wide / 0.12 m thick) + zone approach ramps (pad -> nearest spine); denser PGK connectors (`CutArenaBootstrap.BuildSkiSpines`) |
| **Ski crest** | Tribes leave: outward ski launch factor **1.0**, threshold `skiLaunchLeaveDot` ~0.12; DummyLocomotor air loft tell |
| **Punch** | Connect = loud kick + hold arm; miss = soft blip + limp arm (`TagSfx` / DummyLocomotor / `PunchHitbox`). Local It cocks the fist while the punch buffer is armed (same tell as dummy telegraph). |
| **Lunge** | It-only MMB: ~16 m/s, ~0.20 s, **CD ~1.0 s**; TP whip->settle via `LungeProgress` |
| **Slide** | Crouch+speed: carry entry speed + friction decay only (**no** enter boost) |
| **Air dash** | Visual whip + cyan trail; **30 s CD**; **Q / Left Alt** (in air; airborne MMB also counts via motor); short planar burst. Grounded MMB = It lunge. |
| **Jump / land** | Fixed height (`jumpSpeed` launch, not speed-tied / additive); coyote ~0.10 s, buffer ~0.16 s; hard land -> LandStun; DummyLocomotor land squash plus a knee-buckle / arms-out recovery pose |
| **Motor knobs** | Live on `Assets/Resources/TagArena/MovementConfig.asset` (`Resources.Load` `TagArena/MovementConfig`); recreate via **Tag -> Create MovementConfig Asset** (won't overwrite) |
| **Mantle / climb / glide / bounce** | Stickier mega-park mantle + wall-climb, fairer super-glide window, punchier wall bounce (TP vault/climb/glide/kick tells) |
| **AI** | `DummyPatrol`: chase/flee turn (no 16deg snap, capped ~150deg/s) plus a half-second weave outside punch range so a juke is not tracked perfectly. Lead intercept capped at 0.18 s. A fast strafe across the fist (~7 m/s lateral) usually whiffs. Before the swing the dummy cocks its arm (~0.34 s, down to 0.22 s on a hot fuse) and cancels if you leave the fist. Punch connect kicks the attacker's camera and a lighter kick on the victim's (no hitstop). Still hops for decks (probe + panic), lunges just outside reach, retargets the moment It changes hands. Punch cone matches `PunchHitbox`. Least It still prefers low TimeAsIt. |

## Controls (`PlayerInputReader`)

| Action | Default |
|--------|---------|
| Move | WASD |
| Look | Mouse |
| Ski | Left Shift |
| Sprint (when not skiing) | Left Shift / Left Alt |
| Jet | RMB (Mouse1) |
| Jump | Space |
| Crouch / slide gate | Ctrl or C (hold + speed; in air this is the fast fall) |
| Punch (It transfer) | LMB (Mouse0) or E |
| **Lunge (It only, grounded)** | **MMB (Mouse2)** |
| **Air dash** | **Q / Left Alt (in air)**; airborne **MMB** also dashes. ~0.1 s, cyan trail, ~30 s CD. Not a jet. Grounded MMB = It lunge. |
| Mode hotkeys | **F1** Hot Potato / **F2** Least It / **F3** Trail Tag / **F4** Free play (same four in the mode menu as 1/2/3/4). Each start recovers ragdoll and places pawns on spawn pads. |

Punch is **not** a contact aura -> only active punch hits transfer It (`PunchHitbox`).

## Tube pieces left out (measured)

`PGK_Slide_TubeDeck_2m` is on soft-play, astro, and army, one each. Yaw 90, stem 0, pivot local (0, 0, 5.90). The mesh is pitched -90 X because the FBX rise is on Z; root-local mouth center is 1.91 m and the shell top is 2.48 m. Low mouth stays on the mulch. Knight keeps the straight chute. The west sleeve stays 3.28 m off the tube street, about 7 m off the ground `Toy_Slide`, and 7.2 m off the dome. Army's sleeve is 1.49 m off the spiral climber. `Mega_SlideTube` and `PGK_Slide_Tube90` are still not placed and are not scaled. Crawls are `Toy_TunnelTube` (play places + north ring) and `Mega_CrawlTunnel` (bunkers + south ring).

## Human Play path

Walk these in order. Spines and the crash cross stay empty. This pass did not move the crash torso, the net beam, or the ninja rail. Seating on the dome, ground slide, spiral climber, beams, arches, and benches already meets the support plane, so no y change. Arch piers still clear courts and bars by at least 1.3 m. The NE hop tiles now clear the spawn seesaw by 0.56 m. The other gaps under 0.5 m are beside the walk, not across it.

Chase these structures. Do not add props. SoftS aisles, the NE hop, and the merry-spine bar stay as they are. `Mega_SlideTube` and `PGK_Slide_Tube90` are not placed.

- [ ] SW: mushroom (4.5, 6.94) -> hopscotch SW -> fort-west spring (8.53, 7.23) -> soft-play bench (8.20, 5.75)
- [ ] Soft-play: tube street, SoftS apron, slide beam (18.48, 2.97), dome, ground slide, deck tube. Leave the 1.11 m aisles.
- [ ] South spine: overhead bar at (11, 17.78). Posts sit outside the spine. Rungs are at y=2, so the chase under them stays open.
- [ ] Merry: bench (4, 24), picnic (4.8, 21.4), west spring (2.50, 24), then merry-north (7.2, 31.2)
- [ ] North spine -> NW arch (7.70, 42) -> NW cluster (9.4, 45.0) -> hopscotch NW -> astro dome and deck tube
- [ ] East: hopscotch SE -> SE arch (65.90, 10.5) -> SE cluster -> army crawl mouths and the spiral (crawl spring and conn spring)
- [ ] Open kickball (field empty) -> swing beam (63.45, 31.20) -> swings. The 0.16 m fall-tile gap stays empty.
- [ ] Knight crawl mouths and spiral -> NE arch (65.65, 41) -> NE cluster. The hop gap stays 0.56 m. Then hopscotch NE.
- [ ] Crash cross on z 24-30. Both lips and the middle stay open.

Seats: SW bench (9.26, 9), soft-play bench (8.20, 5.75), merry bench (4, 24) and picnic (4.8, 21.4), kickball bench (61.30, 21.05), swing bench (71, 31.2), NE bench (65.65, 41.90). Each hopscotch court also has a bench at local (-2, 0).

1. **SW exit.** Spawn_SW faces northeast. Mushroom steps at (4.5, 6.94) sit beside that line, between the pad and hopscotch SW. Feet are on the ground. No arch here: the court and the south bar are already about 2.2 m apart. The bench moved from (9.85, 9) to (9.26, 9). The old spot was on the climb net. It is now about 0.29 m west of that net and off the court. The passage east of the net, toward the bar, stays open. The court is still the destination.
2. **West loop.** Hopscotch SW -> south bar (x=11) -> soft-play. A bench at (8.20, 5.75) is west of the tube street, 0.50 m off the plastic cap, south of the climb net. A spring at (8.53, 7.23) sits north of that bench, between it and hopscotch SW: x 8.18-9.03, z 6.99-7.47, 1.01 m off the bench, 0.75 m west of the climb net, 0.74 m off the nearest court tile. Astro does not get it. The street itself stays clear. A ground slide at local (5.90, -5.53) sits east of the tube cap: low mouth on the mulch, high end about 2.41, 0.79 m south of the tubes. East forts do not get it. East of that mouth, three wall panels at z=1.70 span x 21.40-26.20: 0.86 m off the slide, 1.35 m west of the south ring ground stair, 0.79 m south of the ring side stair. The outer lane stays north of them. South of the tube street, a cluster at (14.5, 2.6): a balance beam, mushroom steps, a spring rider, and two hop tiles south of the beam. The mushrooms stop at z=4.45, 0.58 m south of the tube mesh. An east-west step between the beam and the tubes spans x 13.15-15.74 at z 3.46-4.34, 0.68 m off both and 1.11 m off the north-south mushrooms and the spring. The spring ends at x=17.70, 1.56 m west of the ground slide. A balance beam at (18.48, 2.97) yaw 90 fills that gap: x 18.42-18.54, z 1.47-4.47, 0.72 m off the spring and the slide, 0.56 m south of the tube mesh. The SoftS aisles stay as they are. Astro does not get the beam. The street and the climb-net gap stay open. Both west forts also get a climber dome at local (-4.66, -6.70). Feet are seated (stem -0.330) and the top is about 1.31, a low round climb the east bunkers do not have. On soft-play it sits at x 7.95-10.73, z 1.66-4.44, 0.59 m south of the tubes and 0.43 m west of the apron mushrooms. On astro the same piece lands at x 17.27-20.05, z 49.56-52.34, 0.45 m west of the west lane and off Spawn_NW's exit. Then tubes, the deck tube on both west forts, 2.4 m rung, net beam, or the tall net. A spring rider at (19.20, 10.40) sits on the east shoulder, 0.78 m east of the spiral and 0.80 m west of the west lane. The north pit still ends at z=16.0 with the spine at 16.4, so that exit stays open. No west alley was sealed. Cross the south spine on the overhead bar at x=11, z=17.78. Its posts sit at z 15.74-15.82 and 19.74-19.82, outside the spine, and the rungs are at y=2 so the chase under them stays open. Then middle bars or the beam lane -> merry apron into the bars. Merry bench is (4, 24); picnic is (4.8, 21.4). A spring at (2.50, 24) is the west lawn mark: x 2.15-3.00, z 23.76-24.24, 2.15 m off the map edge and 0.78 m west of that bench. No second bench. North of the pad, west of the bars, a cluster at (7.2, 31.2): mushroom steps, a spring rider, and two hop tiles. It stays 0.56 m west of the bar and 1.9 m south of the north spine. Cross the north spine -> north bar -> astro.
3. **NW exit.** Spawn_NW faces southeast (yaw 135), into astro. The crash torso at (12, 52.12) is 1.0 m north of the pad and about 3.6 m east of the pad's east edge, so it is not on that exit. Astro's climber dome is north of its tubes (x 17.27-20.05, z 49.56-52.34), 0.45 m west of the west lane, so it is off this exit too. East of that dome, two wall panels at z=51 span x 21.36-24.56: 1.31 m off the dome, 1.32 m west of the north ring side stair, north of the tube street and the outer lane. Hopscotch NW is at (3.2, 42), south of the pad, west of astro. The arch at (7.70, 42) spans that court toward the north bar. Piers stay about 1.7 m off both. The deck is at 1.05 and the span underneath is open, so the court stays a destination. North of that arch, a cluster at (9.4, 45.0): mushrooms, a spring, and two hop tiles. The mushrooms end about 0.22 m west of the north bar face, and the bar line stays clear.
4. **East loop.** Spawn_SE -> hopscotch SE -> arch at (65.90, 10.5). South of that arch, a cluster at (66.2, 7.5): mushrooms, a spring, and two hop tiles. Then army, whose 2.00 chute is the same pitched deck tube. Knight keeps the straight chute. Army and knight each have the 2.4 m rung on the west shoulder, the 1.8 m ladder on the net side, and a spiral climber at local (2.80, 0) on the deck's far side. Feet are on the ground and the top is 2.40, so it is a climb onto the 2.00 deck. On army it should sit about 0.4 m south of the south bar. On knight, about 1.7 m north of the conn. A spring at local (2.80, -2.302) is the step between that climb and the crawl: 0.56 m off the crawl and the south tiles. Army world (60.80, 7.45), x 60.45-61.30, z 7.21-7.69. Knight world (55.20, 46.55), x 54.70-55.55, z 46.31-46.79. A second spring at local (2.80, 1.748) is the step from the conn up to that climb: 0.56 m off the spiral and 0.66 m off the conn. Army world (60.80, 11.50), x 60.45-61.30, z 11.26-11.74. Knight world (55.20, 42.50), x 54.70-55.55, z 42.26-42.74. West play places keep the slide spiral and the tube street instead. Each bunker also has one crate on the outer apron, local (-0.80, -6.81): 0.56 m off the crawl wall, 2.4 m off both mouths. Army world (57.20, 2.94) is 1.72 m west of Spawn_SE's bumper. The bumper sits south of the army crawl, 0.57 m off the mesh, so that east mouth stays open. Knight world (58.80, 51.06) is 1.68 m west of the shield. West forts do not get the crate. Two monkeys continue the south ring's east bar at z=3.35, x 42.3-50.7, abutting that bar and stopping 1.3 m west of the army crawl mouth. A mushroom step at (53.73, 2.85) sits on the army south apron, 1.78 m east of those bars and 1.78 m west of the crate, 0.56 m south of the crawl wall. The west mouth stays open. Knight does not get that step. A second mushroom at (50.10, 5.00) is the approach to that mouth: x 48.85-51.44, z 4.56-5.44, 0.56 m west of the opening and 0.78 m south of the south rail. Three monkeys continue the north ring's east bar at z=51, x 42.3-54.9, abutting that bar and stopping 1.1 m west of the knight crawl mouth. A mushroom at (54.10, 49.48) is the approach to that mouth: x 52.85-55.44, z 49.04-49.92, 0.56 m west of the opening and 1.04 m off the climb net and the bars. Army does not get that step. A mushroom at (65.81, 48.75) is the approach to the knight crawl's east mouth: x 64.56-67.15, z 48.31-49.19, 0.56 m east of the opening. Army does not get it. Bars at x=62.5 into the open west side of kickball -> swings. A bench at (61.30, 21.05) is west of those bars, south of the Loop E stair and north of the south spine. The diamond stays empty. Swing fall tiles should still clear the kickball north fence by about 0.16 m. That gap is the clearance, so nothing was added there. A balance beam at (63.45, 31.20) yaw 90 sits north of the fence, between the east bars and the swing frame: x 63.39-63.51, z 29.70-32.70, 0.85 m off the bars and 0.84 m off the swing. The diamond stays empty. Swing bench is (71, 31.2). Continue the bars. Knight is west of those bars (same rung and ladder). The arch at (65.65, 41) is east of them. A bench at (65.65, 41.90) sits just north of that arch, about 2.4 m off the bars and off hopscotch NE. It is not a second arch. North of that bench, a cluster at (66.2, 45.0) holds a net frame, mushroom steps, a spring rider, and two hop tiles. The hop east edge is x=68.45, 0.56 m west of the spawn seesaw (west edge x=69.01). The south hop clears the net's southeast post by 0.10 m and the mushroom cap by 0.16 m. The spring is 1.86 m east of the bar. The mushroom stays 0.58 m north of the bench and 1.16 m west of the seesaw. Spawn_NE's southwest exit stays north of the cluster, and the arch span under the bench is still open. Then hopscotch NE. Spawn_NE faces southwest, into the keep. The shield is north of that pad. Merry, hopscotch, and swings were not moved.
5. **Crash.** Cross the bowl east-west. Both lips and the middle should be open lawn. From the east bars the cross lane is z 24-30. The nearest new bench is the kickball seat, north edge z=21.75, which is 2.25 m south of that lane. The arches are farther north or south. Nothing on that approach was nudged.
6. Chase, in order: SW mushroom -> hopscotch SW -> fort-west spring -> soft-play (SoftS apron, tube street, slide beam, dome, ground slide, deck tube) -> soft-merry bar across the south spine -> merry (bench, picnic, west spring) -> merry-north -> north spine -> NW arch -> NW cluster -> hopscotch NW -> astro. East: hopscotch SE -> SE arch -> SE cluster -> army mouths and spiral -> open kickball -> swing beam -> swings -> knight mouths and spiral -> NE arch -> NE cluster -> hopscotch NE -> crash cross. SoftS aisles, the NE hop, and the merry-spine bar were not densified. Mega and Tube90 are not placed. Feel was not edited.

## Tag handoff / AI (code)

- Punch transfer: PunchHitbox -> TagModeController.OnSuccessfulPunch -> TransferIt -> ItController.SetIt.
- SetIt(true) plays become-It SFX, calls PlayerMotor.NotifyBecameIt() (anim/HUD listeners), and pulses DummyLocomotor.PlayTagFlinch on the new It.
- Victim also flinches via ReceiveTagHit. HUD flashes YOU'RE IT / YOU'RE FREE and names LastFromId / LastToId. The hat pops on the rising edge.
- DummyPatrol Retargets immediately on **gain and lose** It, then weaves and caps turn rate so the new chase is a kite, not a snap.
- Flee panic hop uses dy 0.9 (was 0.55, below ConsumeHop minDy 0.85, so it never fired). Trail Tag mode line shows **SUDDEN DEATH** when the cap/stall failsafe trips.
- HUD mode line uses ASCII ` | ` separator; center MODE flash lists F1-F4. F1-F4 SetMode also syncs GameFlow menu cursor via PlayerPrefs, recovers ragdoll, and places pawns on pads.
- Trail Tag self-hit still needs both age and distance grace. Dodge i-frames do not ignore trails. Punch updates It brightness the same frame for every emitter mode. ItOnly still gates who emits.
- Results card: solo headline is YOU WIN, YOU LOSE, or DRAW, and it names the mode and the winners. Couch with a split result stays ROUND OVER. Rematch click plays UiConfirm. A loss line does not say "winner", so it does not play the win tone. Hot Potato HUD says YOU HOLD THE FUSE when you are It; Trail marks YOU and OUT waiting for round; sudden death explains the next hit. Trail OUT! / TRAIL HIT flash holds ~1.0s with the other center beats. **R** / Rematch starts once; Boot does not also rematch. A second StartRound inside 0.05 s is ignored. **Q** or **Esc** returns to Boot. F1-F4 or R leave pause and round-end so the countdown is not frozen. Esc on player-count and mode screens steps back to Boot. Keys 1-4: you + 1 bot, or 2-4 humans with the bot off. Direct Play pauses on Esc; Q loads Boot. Pause zeroes look and punch, clears a buffered jump; HUD flashes use scaled time so they freeze. Countdown card names the mode (3-2-1). Local punch windup flares the elbow (0.12 s unchanged). Look sensitivity is a five-step stub (default 1.8); Boot and pause open it; Left/Right change it. Direct-Play pause notes Countdown frozen. Playground music stays silent when the clip is null. Ski entry still uses TagSfx.EnsureSource.
- Look sensitivity stays in PlayerPrefs (`Tag.LookSensitivity`, default 1.8) and loads when the camera wakes. Controls help is on Boot, the pause menu, and Direct Play (H). Air dash can be Q (default), V, or Mouse4; Left Alt still dashes, and that choice is saved. A jump or crouch held through pause is not a new press on resume. A dash or punch tap during pause does not fire later. The first Boot visit says Play is you and one bot in Least It until you start.
- Punch can be LMB (default), F, or Mouse3; E still punches. That choice is `Tag.PunchKey`. Volume and mute stay on the Boot and pause Audio card (`AudioMaster`, default 0.8). On the results card the cursor is unlocked and gameplay input is zeroed, so a Rematch click does not punch or yaw. StartRound locks the cursor and ends any swing that was still out. Windup stays 0.12 s. Slide, jump, dash, and jet numbers are unchanged.

## Feel check (code, not a Unity play)
- Jetpack off: `MovementConfig.enableJet` is false (asset enableJet: 0). RMB does not jet.

Slide keeps entry planar speed: `SlideMove` only applies friction (softer downhill) and clamps to `_slideStartSpeed`. The punch +8% speed buff is skipped while `State == Slide`, then the same cap is applied again. `slideDownhillAccel` is 0 and unused. Jump sets `v.y` from `jumpSpeed` / fatigue, not from horizontal speed. Air dash is a short planar burst with `airDashCooldown` 30 and a cyan trail on `DummyLocomotor` (trail updates even if the limb bind fails). The near-zero speed floor inside `EnterSlide` cannot run: crouch only enters a slide at `slideEntrySpeed` (7.5).

Wall-run and wall-climb set a latch on exit (timeout, jump-off, or lost contact). The latch clears on the ground or after ~0.15 s with no wall hit, so air accel cannot restart the timer on the same surface. Climb up-speed (`climbSpeed` 6, decay from 0.16 s, slip -> 3.5) reverses before `climbMaxHeight`; the old 7.8 / 0.40 curve hit the height cap at ~0.42 s while still going up, and `ClimbHeightUsed` never cleared on landing. Sprint stays 12 m/s, ski max 24 m/s (run was already raised; ski still wins). Jet stays off.

## Human verify next

No Unity play on this pass (no Unity / `dotnet` on the VM). After pull, open **Play**:

1. Sprint, hold Ctrl: slide should not speed up on entry. Down a slide, it should last longer and still not go faster than the speed you had at the crouch.
2. Jump from a walk and from a sprint: same height. Hold Ctrl in the air: you should drop faster than a normal fall.
3. Air dash (Q): short cyan streak, then the HUD dash bar counts ~30 s. RMB should not jet.
4. Wall-run a figure-8 panel: you should slide down and fall off. You should not re-stick until you leave the wall or land. Climb a net: rise, then slide down. After you hit the ground you can climb again. Climb decay should start peeling before you hit the height cap (slip reads a bit stronger). Late wall-run should peel harder before the attach timer ends.
5. Run steps (a plant, then a lift) rather than a constant skate. Hands stay forward of the hips. A hard landing buckles the knees and opens the arms, then stands back up. Q dash should reach the whip pose inside the short burst. Mid hop-offs should still show a brief crouch squash.
6. Chase, in order: SW mushroom -> hopscotch SW -> fort-west spring -> soft-play (SoftS apron, tube street, slide beam, dome, ground slide, deck tube) -> soft-merry bar across the south spine -> merry (bench, picnic, west spring) -> merry-north -> north spine -> NW arch -> NW cluster -> hopscotch NW -> astro. East: hopscotch SE -> SE arch -> SE cluster -> army mouths and spiral -> open kickball -> swing beam -> swings -> knight mouths and spiral -> NE arch -> NE cluster -> hopscotch NE -> crash cross. SoftS aisles, the NE hop, and the merry-spine bar were not densified. Mega and Tube90 are not placed. Feel was not edited.
7. Tag the dummy: hat pops, flash says YOU'RE FREE and names who is It. When they tag you: YOU'RE IT and who it came from. Handoff flash holds ~1.0 s.
8. F1 while you are It: top-center FUSE appears inside the warn window even if you pass It away. F2: mode line shows seconds left and WINNING (least) / BEHIND (more It). F3: a foreign trail still eliminates; your own trail does not until the grace ends. F4: free play, punch still moves It, no timer. Each of F1->F4 should drop you on a spawn pad, including if you were ragdolled.
9. As It, a sharp strafe should make the dummy miss more often than it connects. As runner, you should be able to cut their flank instead of losing a straight race every time.
10. Q dash and a grounded It lunge should not sound the same. A short hop lands with a soft thud; a hard land is louder. Tag, round start, and a trail elim should make a tone even with no audio files imported.
11. From across a fort the orange hat and beacon should still read. Your punch windup should flare the elbow out beside the head within the same short windup. Holding LMB as It should still cock the fist before the swing.
12. When a round ends, a center card names the result. One R (or Rematch click) starts the next round from a pad (a second R in the same moment does not restart it again). Esc pause, then F1: the countdown should move. Q, Esc, or Menu from the card returns to Boot, including a direct Play scene. In Trail Tag, after OUT you should see "waiting for the round" until the match ends.
13. Your own hat should sit on your head without a tall spike in the camera. The dummy's beacon should still read from across a fort. When the dummy is It, you should see the arm cock before the punch, and leaving that range should cancel it. Getting tagged should nudge your camera. Pause resume and quit should click.
14. Esc during Play pauses. Mouse look should stop and a click on Resume should not punch. A jump you buffered just before Esc should not fire when you resume. HUD flashes should freeze while paused. Direct Play (opened without Boot) still pauses, and Q loads Boot with the cursor unlocked and the music bed stopped. Direct Play pause matches Boot: Left/Right highlights Resume, Controls, Look, Audio, Quit and stops at the ends. Enter or Space uses that row. Keys 1-5 only move the highlight. Esc on Controls, Look, or Audio stays paused. Boot's player and mode screens: Esc steps back. Row 1 is you + 1 bot. Rows 2-4 are humans and the bot stays off. The countdown names the mode and counts 3, then 2, then 1. No music bed is expected. A ski entry still makes a tone.
15. Look panel: Up/Down highlights Sensitivity then Back and stops at the ends. Left/Right steps the sensitivity only while that row is highlighted. Enter on Back closes. Esc leaves the panel without unpausing. Default should still feel like the current camera. Quit and relaunch: the same step should still be selected. When a solo round ends, the card should say YOU WIN, YOU LOSE, or DRAW, and name the mode and the winners. Rematch and Menu still click.
16. First Boot visit should say Play is you and one bot in Least It. Controls lists the real keys. On the Controls card, Up/Down highlights Dash, Punch, then Back. Left/Right steps the highlighted bind (Q, V, Mouse4 for dash; LMB, F, Mouse3 for punch). Alt still dashes. E still punches. Keys 1-3 only move the highlight. Enter on Back closes. Direct Play pause: H opens that same card.
17. Default LMB should feel the same. Volume stays on the Audio card. On the results card, clicking Rematch should not punch and the mouse should not turn you. The next countdown should not still be swinging. Direct Play results should unlock the cursor the same way. Enter on the results card does not also click a different button.
18. Audio panel: Up/Down highlights SFX, Music, Mute, Music mute, then Back and stops at the ends. Left/Right steps the highlighted row (SFX or the music bed). M still mutes all. N still mutes music. Default bed should sound the same. Enter on Back closes. Esc does not unpause. On the results card, Left/Right can move Rematch / Menu during the short arm; Enter does nothing until the arm ends. R still rematches even if Menu is highlighted, and a second R does not. Q and Esc still return to Boot.
19. Boot: Up/Down highlights Play, Controls, Look, Audio, Mode, Couch and stops at the ends. Enter uses it. With Play highlighted, Enter still starts you and one bot and does not also open another row. Keys 1-6 only move the highlight. Pause: Left/Right or 1-5 picks highlights Resume through Quit and stops at the ends. Enter or Space uses it. Esc still resumes and Q still quits. Up/Down on the main pause card is still the music bed.
20. Who-plays and mode select no longer wrap. Up on the first row and Down on the last row stay put. Keys 1-4 still jump to that row. A click on Boot or Pause moves the highlight and uses that row only. Enter uses only the highlighted row.
21. Pause, open Controls (or Look or Audio), then F1. The panel should be gone and the countdown should run. Esc pauses. After a round, that same panel should not cover Rematch / Menu, and Q should show Boot, not the panel. Direct Play: if a round ends while the local pause card is up, the results card should still take Left/Right and Enter.
22. During play, M mutes all and N mutes music. The HUD chip should show. Pressing M on the pause card or the audio card still toggles once, not twice. After a dash, the bar is a dark track with a cyan fill that grows back; ready or the burst itself is mint. In Hot Potato the It beacon warms toward white as the fuse drops. Your own camera still hides that beacon.
23. First Boot visit: the line names the punch key (LMB or E by default) and M/N. It should not say LMB/F. Open Couch or Mode select, then Esc: the first-run line should still be there. Play a round, then Q back to Boot: that line should be gone, and Play should be highlighted. The first countdown says WASD move and Shift sprint. Rematch, and the next countdown, should say to punch the dummy with the orange hat. On the pause card, H opens Controls and stays paused. Digits on who-plays and mode select only move the highlight. Enter or Space confirms.
24. Set 3 humans on who-plays, then Esc from mode select: you should be back on who-plays with 3 highlighted, not Boot. Esc again: Boot, Couch highlighted, and the next Mode select should still say 3 humans. Opening Mode select from Boot should not change that count. The highlighted mode should be the one you played last.
25. On the results card, 1 and 2 only move Rematch / Menu. During the short arm, Enter and Space do nothing. After it, Enter or Space uses the highlight. R still rematches even if Menu is highlighted. Q and Esc still return to Boot. A click during the arm only moves the highlight. The same keys work on the loose round-end card if no mode controller is showing results.
26. Play: you and the bot should be the curved Hier mannequin, not the flat Dummy_Runner mesh. The console should say Hier mannequin, not Navy Spade, unless the FBX failed to bind. The runner is tan with a teal band. Becoming It should swap to the orange Hier mesh with black Vs. A run should show the knees bend. Arms should hang slightly out, not fold into the butt.
27. Sprint: one knee should lift on the forward leg and the back leg should look straight, not two bent skates. Slide should look low, lead knee tucked. A hop should buckle the knees on landing. Air dash should throw the arms back without twisting the hands into the hips. A punch should cock the elbow out, then the fist should stay in front of the chest.
28. Sprint from the chase cam: the arm that reaches forward should be opposite the leg that is forward. The other arm stays back and does not fold into the hips. You and the bot should still be the curved Hier mesh.
29. Slide: the body should look flat, lead knee under the chest, trail leg straight back, arms forward. Punch: the fist should be a long line in front of the chest, not a folded elbow. It should not pass through the torso.
30. Jump and keep sprinting: knees buckle on the landing, then open back into the run over a short moment. They should not snap straight on the first frame. A small hop still buckles. Arms stay slightly out and do not fold into the hips.
31. Air dash: the arms should throw back at the start, then ease down before the streak ends. They should not stay fully whipped and then snap into the run. Hands stay clear of the hips.
32. After the v0.1 Hier FBX: you and the bot are the tan mannequin, It is the orange mannequin, and the same poses still read (recovery knee, opposite arms, flat slide, long punch, land ease, dash settle).
33. Jump: both arms should reach up and the knees should tuck. Fall: the arms should trail back and the legs should lengthen. Hands stay slightly out and clear of the hips.
34. Tag someone: the runner who was hit should guard with both arms up and both knees bent. The new It should raise both arms and lift one knee. The two poses should not match. Hands stay clear of the hips.
35. Climb a wall: one hand should reach while the other pulls, and the opposite knee should step. The lower leg should look long. Wall-run: the wall hand stays on the wall, the outer leg steps, and that knee bends only while it swings forward. Hands stay clear of the hips.
36. Stand still: the hands should hang just forward and outside the hips, not against the pelvis. Start sprinting: the opposite arm/leg stride should return, with no extra twist of the hands into the hips.
37. Jump, including a short hop: both arms should be a long line up and the knees tucked before you reach the top. On the way down, even a short drop, the arms should trail back and the legs should lengthen before you land. Hands stay clear of the hips.
38. Sprint: each plant should hold a moment, one knee up and the other leg long behind, not two straight legs sliding together. The forward arm should still be the opposite side. Hands stay clear of the hips.
39. Slide: the arms should be a long line forward and low, not a folded pair at the chest. Lead knee stays under the chest and the trail leg stays straight. Hands stay clear of the hips.
40. Land, including a small hop: knees buckle, and both arms should come out to the sides while that buckle holds. They should ease back into the run. Hands stay clear of the hips.
41. Climb: both hands should stay visible as they swap, the low hand a line and not a fold at the chest. Wall-run: the wall hand should move up and down the wall with the step, and the outer arm should stay straight. Hands stay clear of the hips.
42. Punch: the windup fist should sit beside the head, not inside the chest. The connect should still be a long line in front of the chest. The cock should not feel longer.
43. Air dash: the arms throw back at the start, then ease down before the streak ends. When the streak ends they should keep easing forward. They should not throw back again. The burst should still feel short.
44. Tag someone: the runner who was hit should show a long V of arms, not a fold at the chest, and both knees should bend. The new It should still raise both arms and lift one knee. The two poses should not match, and neither should look like a landing.
45. Stand still, then sprint: the hands should stay outside the hips the whole way into the stride. They should not tuck in as the walk starts, and they should not twist into the pelvis at a standstill.
46. Hold ski, then let go into a sprint: the body should ease into a lower glide with the arms out, then ease back into the run. It should not pop, and the glide should not look like the sprint. Jet stays off.
47. Wall-run, then drop or land into a sprint: the wall hand and the outer leg should ease into the fall or the run. They should not snap off the wall in one frame.
48. Climb, then drop or step off into a sprint: the reaching hand and the stepping knee should ease into the fall or the run. They should not snap off the wall in one frame.
49. Only if you add ExperimentalGrapple and turn enableGrapple on: holding the rope should reach both arms in a long line, legs staying long. It should not look like a jump. With the gate off, RMB still does not hook. Jet stays off.
50. Tag someone: the runner who was hit should still show the two-arm V with both knees bent. The new It should raise one arm, hold the other out, and lift one knee. The two poses should not match.
51. Punch a connect, then keep sprinting: the fist should stay out for a moment, then ease into the run. It should not snap back when the punch ends. The cock should not feel longer.
52. Jump: the arms should tuck on the way up, hang out to the sides at the top, then trail back on the way down. The jump should not feel higher. Hands stay clear of the hips.
53. Hold ski, then let go into a sprint: the glide should still show a knee and a short arm swing, not locked straight legs. The change into the run should ease. The run knee should still lift higher than the glide. Jet stays off.
54. Slide, then crouch: the slide should show a flat back, head up, and both arms out in a long line clear of the chest. A crouch should stay a low guard with bent elbows. The slide should not speed up.
55. From a standstill, hold crouch: both knees should bend and the elbows should fold in front of the chest. The back should stay up. Sprint and keep holding crouch: that becomes the flat slide, straight arms and one leg back, and it should not speed up. When the speed dies while crouch is still held, the guard should return.
56. Jump, then hold crouch on the way down: the chest should pitch down and the arms should fold in, not trail out like a normal fall. The drop should still feel like the same fast fall. Letting go should return to the trail. A jump with no crouch should feel the same height.
57. Land while sprinting: both knees should buckle, then the back leg and the arms should already be in the stride while the front knee is still up. They should not both snap straight and then start the run. A small hop still buckles. Standing still, both legs should open together. The land should not feel longer.
58. Sprint: the arm opposite the lifted knee should be a long reach, wider than the back arm. The back elbow may bend, but that hand should stay clear of the hip. The back leg should stay straight. Only the front knee should lift.
59. Punch someone so you become It: the fist should stay out, then ease into one arm up and the other held out, with one knee up. It should not snap into the run. The runner who was hit should still show the two-arm V with both knees bent. The two poses should not match. The cock should not feel longer.
60. Get tagged while sprinting: both arms should come up in a V and both knees should bend, then the arms and the back leg should already be in the stride while one knee is still up. They should not both snap straight and then start the run. Standing still, both legs should open together. The new It should still raise one arm and hold the other out. The catch should not feel longer.
61. Stand still: the chest should breathe and rock slightly side to side. The hands should stay just forward and outside the hips, not against the pelvis. Start sprinting: the sway should fade and the opposite arm/leg stride should return. They should not twist into the hips at a standstill.
62. Sprint, then let go: the last step should close under the hips, then the idle sway should return. Drop to a walk instead: the stride should shorten into the walk, not freeze one leg out. It should not feel like a skate stop. Speed should feel the same.
63. Walk, then sprint, then drop back to a walk: the steps should get longer and quicker, then shorter, without a foot sticking or the hips popping. Speed should feel the same.
64. Walk and turn hard, and turn in place: the outside foot should stay planted while the other leg steps. The waist should not twist. The hands should stay clear of the hips. Look should feel the same.
65. From a walk, hop a short hop and keep walking: the knees should bend, then the stride should come back under the hips. The arms should stay in the walk, not flare out. A high drop should still bring the arms out. The landing should not feel longer.
66. Sprint, then let go into a stand: after the feet close, the hips should ease into the idle side sway. They should not freeze flat and then jerk sideways. The breath should still be there. Speed should feel the same.
67. Stand still, then walk: the first step should push off the foot that stays down, and the other leg should reach into the stride. The idle sway should fade out, not pop off. The feet should not skate. Speed should feel the same.
68. Sprint, then crouch: the body should drop into the slide without a speed bump. Let go: the stride should come back under the hips, not skate. From a slower run, a crouch should drop into the guard and stand back into the stride. The slide should not speed up.
69. Air dash, then land into a run or a walk: after the burst, the feet should be back in the stride under the hips. They should not skate. The dash should still be a short burst, and it should still have to recharge.
70. Wall run and climb: the hand should meet the surface, then move. It should not pop when you touch the wall. Letting go should bring the stride back under the hips. The leave should not feel longer or shorter.
71. Punch while sprinting, and miss or hit without becoming It: the arm opposite the front knee should get back into the stride. The hips should not stay twisted. The fist should still ease out, not snap. The cock should not feel longer.
72. If you turn the grapple on and hook: the hands and the chest should settle into one long line. They should not twist. Turning it off should return to the stride. It should stay off unless you turn it on. The pull should feel the same.
73. Get tagged while sprinting: the hands and the chest should ease into the stride together, under the hips. They should not stay folded and then pop. One knee can still be up. The catch should not feel longer.
74. Become It while sprinting: the hands and the chest should ease into the stride. They should not stay in the claim and then pop. One knee can still be up. The claim should not feel longer.
75. Hold ski, then let go into a run, and go back into the ski: the feet should ease under the hips. They should not skate, and the hips should not pop. Speed should feel the same. Jet stays off.
76. Drop from a height and land standing still: the knees should ease into the idle breath and sway. The arms should flare, then ease, and should not stay locked out. A short hop should still keep the arms in the idle pose. The landing should not feel longer.
77. Jump, hold crouch on the way down, and land: the body should read as a crouch in the air, with the arms in. On landing the arms should ease. They should not pop. The drop should still feel like the same fast fall. A jump with no crouch should feel the same height.
78. Air dash: the chest should pitch and the arms should fly wide for the short burst. They should read, then ease back. The dash should not last longer, and it should still have to recharge.
79. Wall-run, then drop into a run: the hips and the feet should ease into the stride under the hips. They should not pop. The hands should not snap, and the leave should not feel longer or shorter. A climb should leave the same way.
80. Run and look up and down: the reaching arm should follow the look and stay clear of the hip. The other arm should stay in the stride. Look should feel the same.
81. Slide down a slope, then let go: the body should stand up into the run under the hips. The feet should not pop. The slide should not speed up.
82. Jump from a walk or a run: the foot that was down should push, and the other knee should come up, then the tuck. The jump should not go higher.
83. Punch: the fist should cock beside the head and hold that beat, then strike. It should not feel like a longer cock. The strike should still come out.
84. Sprint, then let go: the last foot should plant under the hip before the idle sway. It should not skate. Speed should feel the same.
85. Jump and look around: the arms should stay clear of the torso on the way down. The tuck on the way up should stay the tuck. Look should feel the same.
86. Stand, then walk, then stop: the hands should stay forward and out the whole way. They should not drift into the hips. The idle sway and the first step should still be there. Speed should feel the same.
87. Hold crouch and walk: the feet should take short steps under the hips. They should not skate, and you should not speed up. Standing still in a crouch should stay the guard. A fast crouch should still be the slide.
88. Stand still and get tagged: the hands and the chest should ease into the idle breath. They should not freeze and then pop. Both knees can still bend. The catch should not feel longer.
89. Sprint, jump, and land still holding sprint: the arms should flare, then ease into the stride under the hips. They should not stay locked out. A short hop should still keep the arms in the stride. The landing should not feel longer.
90. Sprint and turn: the outside foot should plant while the other leg steps. The chest and the hips should lean together. The waist should not twist. Speed should feel the same. Look should feel the same.
91. Stand still and become It: the hands and the chest should ease into the idle breath. They should not freeze and then pop. One knee can still be up. The claim should not feel longer.
92. If you turn the grapple on, then let go: the hands and the chest should ease out of the long line into the run or the idle. They should not twist. It should stay off unless you turn it on. The pull should feel the same.
93. Stand still and punch a miss: the fists should ease into the idle hands. They should not freeze and then pop. The cock should not feel longer.
94. Air dash, then wait for the bar: when it is ready, the chest and the arms should settle. The dash should not last longer, and it should still have to recharge.
95. Walk and turn at a medium yaw: the outside foot should plant while the other leg steps. It should not wait for a sharp turn. Look should feel the same.
96. Air dash, then land softly into a walk: the knees should bend and the arms should stay in the stride. They should not flare out. The dash should not last longer, and it should still have to recharge.
97. Hold crouch while standing, then let go: the hips should ease into the idle breath and sway. They should not pop flat. Speed should feel the same.
98. Walk, then sprint: the back foot should push, then the stride should open. The feet should not skate. Speed should feel the same.
99. Walk, hop a short hop, and keep walking: the knees should settle into the stride. It should not look like a full stop. A hard landing should still absorb.
100. Slide, then let it die into a stand: the body should come up into the idle breath. The hips should not pop. Speed should feel the same.
101. Crouch and walk, then let go: the hips should rise into the stride. They should not hitch. Speed should feel the same.
102. Sprint, then drop to a walk: the stride should close with the step. It should not skate to a stop. Speed should feel the same.
103. Walk and turn, then sprint: the outside foot should plant, then the stride should open. The feet should not skate. Speed should feel the same.
104. Walk, then drop hard and keep walking: the knees should absorb, then take a step. It should not sit in the idle. A landing from a stand should still absorb.
105. Jump and look: the arms should ease into the pose in the air. They should not snap. Look should feel the same. The jump should not go higher.
106. Wall-run, then step off into a walk: the hands should ease into the stride. They should not hitch. The leave should not feel longer or shorter.
107. Climb, then step off into a walk: the hands should ease into the stride. They should not hitch. The leave should not feel longer or shorter.
108. Hold crouch in the air, then land softly: the fall pose should open into the absorb. It should not stay folded and then pop. The drop should still feel like the same fast fall.
109. Walk and punch a miss: the hands should return to the stride. They should not drop into the idle. The cock should not feel longer.
110. Walk and get tagged: the arms should settle into the stride. They should not drop into the idle. The catch should not feel longer.
111. Walk and become It: the arms should settle into the stride. They should not drop into the idle. One knee can still be up. The claim should not feel longer.
112. If you turn the grapple on, hook, then let go while walking: the hands should return to the stride. They should not hitch. It should stay off unless you turn it on. The pull should feel the same.
113. Stand and wait for the dash bar: when it is ready, the chest and the arms should give a small pulse, then the idle breath. The dash should not last longer, and it should still have to recharge.
114. Ski, then let go into a walk: the stride should come back. The feet should not skate. Speed should feel the same.
115. Walk, then ski: the legs should ease into the glide. They should not snap. Speed should feel the same.
116. Sprint, then ski: the stride should close into the glide. It should not pop. Speed should feel the same.
117. Ski, then let go into a sprint: the glide should open into the stride. It should not pop. Speed should feel the same.
118. Crouch and walk, then sprint: the hips should rise and the stride should open. It should not pop. Speed should feel the same.
119. Sprint, hop a short hop, and keep sprinting: the knees should absorb, then the stride should open. It should not look like a stop. A hard landing should still absorb.
120. Sprint, drop hard, and keep sprinting: the knees should absorb, then the stride should open. It should not sit in the buckle. A hard landing into a walk should still take a step.
121. Wall-run, then sprint off: the hands should open into the stride. They should not hitch. The leave should not feel longer or shorter.
122. Climb, then sprint off: the hands should open into the stride. They should not hitch. The leave should not feel longer or shorter.
123. Sprint and punch a miss: the hands should return to the stride. They should not stay in the limp. The cock should not feel longer.
124. Sprint and get tagged: the arms should settle into the stride. They should not stay folded. The catch should not feel longer.
125. Sprint and become It: the arms should settle into the stride. They should not stay folded. One knee can still be up. The claim should not feel longer.
126. If you turn the grapple on, hook, then let go while sprinting: the hands should return to the stride. They should not hitch. It should stay off unless you turn it on. The pull should feel the same.
127. Slide, then let it die into a walk: the body should rise into the stride. The hips should not pop. A slide into a stand should still rise into the idle breath. A slide into a sprint should feel the same. Speed should feel the same.
128. Slide, then let it die into a sprint: the body should rise into the long stride. The hips should not pop. A slide into a walk should still rise into the walk. A slide into a stand should still rise into the idle breath. Speed should feel the same.
129. Crouch still, then sprint: the hips should rise into the long stride. They should not pop. A still crouch into a stand should still rise into the idle breath. A crouch walk into a sprint should feel the same. Speed should feel the same.
130. Air dash, then walk: the burst should end in the stride. It should not come to a stop. An air dash into a sprint should feel the same. The dash should not last longer, and it should still have to recharge.
131. Air dash, then sprint: the burst should end in the long stride. It should not come to a stop. An air dash into a walk should still end in the walk. The dash should not last longer, and it should still have to recharge.
132. Jump, then land into a crouch walk: the landing should absorb into the low stride. The hips should stay down. A still crouch should still use the old absorb. The landing should not feel longer.
133. Hop, then land into a still crouch: the landing should absorb into the guard. The hips should stay down. A hard landing should still use the old absorb. A crouch walk should still absorb into the low stride. The landing should not feel longer.
134. Drop hard into a still crouch: the landing should absorb deeper into the guard. The hips should stay down. A soft landing should still use the lighter guard. A crouch walk should still absorb into the low stride. The landing should not feel longer.
135. Jump while crouched and still: in the air, the body should settle into the guard. The push should still read. A fast fall should still use the dart. An air dash should feel the same. The jump should not feel higher.
136. Air dash while crouched and still: the burst should end in the guard. A crouch walk should still end in the stride. The dash should not last longer, and it should still have to recharge.
137. Air dash into a crouch walk: the burst should end in the low stride. A still crouch should still end in the guard. An upright walk should still end in the stride. The dash should not last longer, and it should still have to recharge.
138. Crouch still, then ski: the guard should ease into the glide. It should not pop. A walk into a ski should feel the same. A sprint into a ski should feel the same. Speed should feel the same. Jet stays off.
139. Ski, then crouch still: the glide should ease into the guard. It should not pop. A still crouch into a ski should feel the same. A ski into a walk should feel the same. Speed should feel the same. Jet stays off.
140. Ski, then crouch walk: the glide should ease into the low stride. A still crouch should still end in the guard. A ski into a walk should feel the same. Speed should feel the same. Jet stays off.
141. Crouch walk, then ski: the low stride should ease into the glide. It should not pop. A still crouch into a ski should feel the same. A walk into a ski should feel the same. Speed should feel the same. Jet stays off.
142. Slide, then crouch still: the wedge should ease into the guard. It should not snap. A slide into a stand should still rise into the idle breath. A slide into a walk should feel the same. Speed should feel the same.
143. Crouch still, then slide: the guard should ease into the wedge. It should not snap. A slide into a still crouch should still ease into the guard. A slide into a walk should feel the same. Speed should feel the same.
144. Slide, then crouch walk: the wedge should ease into the low stride. A still crouch should still end in the guard. A slide into a walk should feel the same. Speed should feel the same.
145. Crouch walk, then slide: the low stride should ease into the wedge. It should not snap. A still crouch into a slide should feel the same. A slide into a crouch walk should feel the same. Speed should feel the same.
146. Crouch still, then climb: the guard should ease onto the wall. It should not snap. A wall run from that crouch should do the same. A normal climb should feel the same. The entry should not feel longer.
147. Climb, then crouch still: the body should ease into the guard. It should not stay on the wall. A wall leave should feel the same. A climb into a walk should feel the same. The leave should not feel longer.
148. Wall-run, then crouch still: the body should ease into the guard. It should not stay on the wall. A climb into a still crouch should feel the same. A wall run into a walk should feel the same. The leave should not feel longer.
149. Climb, then crouch walk: the body should ease into the low stride. A still crouch should still end in the guard. A wall run into a crouch walk should feel the same. The leave should not feel longer.
150. Wall-run, then crouch walk: the body should ease into the low stride. A climb into a crouch walk should feel the same. A still crouch should still end in the guard. The leave should not feel longer.
151. Crouch still, then miss a punch: the fists should ease into the guard. A standing miss should still ease into the idle hang. A walk miss and a sprint miss should feel the same. The cock should not feel longer.
152. Crouch still, then get tagged: the V should ease into the guard. A standing tag should still ease into the idle breath. A walk tag and a sprint tag should feel the same. The flinch should not feel longer.
153. Crouch still, then become It: the claim should ease into the guard. A standing claim should still ease into the idle breath. A walk claim and a sprint claim should feel the same. The claim should not feel longer.
154. Crouch still, then let go of a grapple: the line should ease into the guard. A standing release should feel the same. A walk release and a sprint release should feel the same. The pull should feel the same. The gate stays off.
155. Crouch still, then wait out a dash cooldown: the settle should stay inside the guard. A standing ready should still pulse into the idle breath. A moving ready should feel the same. The dash should not feel longer.
156. Crouch walk, then miss a punch: the body should ease into the guard and the low stride. A still crouch should still end in the guard. A walk miss and a sprint miss should feel the same. The cock should not feel longer.
157. Crouch walk, then get tagged: the V should ease into the guard and the low stride. A still crouch should still end in the guard. A walk tag and a sprint tag should feel the same. The flinch should not feel longer.
158. Crouch walk, then become It: the claim should ease into the guard and the low stride. A still crouch should still end in the guard. A walk claim and a sprint claim should feel the same. The claim should not feel longer.
159. Crouch walk, then let go of a grapple: the line should ease into the guard and the low stride. A still crouch should still end in the guard. A walk release and a sprint release should feel the same. The pull should feel the same. The gate stays off.
160. Crouch walk, then wait out a dash cooldown: the settle should stay inside the guard and the low stride. A still crouch should still pulse inside the guard. A standing ready should still pulse into the idle breath. The dash should not feel longer.
161. Jump, then crouch walk and land soft: the absorb should stay in the low stride. A hard landing into a crouch walk should feel the same. A soft landing into a still crouch should feel the same. The land should not feel longer.
162. Jump, then crouch walk and land hard: the absorb should go deeper in the low stride. A soft landing into a crouch walk should stay lighter. A hard landing into a still crouch should feel the same. The land should not feel longer.
163. Ski, then slide: the glide should ease into the wedge. A walk into a ski should feel the same. A slide into a stand should feel the same. Speed should feel the same. Jet stays off.
164. Slide, then ski: the wedge should ease into the glide. A ski into a slide should feel the same. A walk into a ski should feel the same. Speed should feel the same. Jet stays off.
165. Jump, hold crouch, and move: the fall should ease into the low stride. A still air crouch should keep the dart. An air dash into a crouch walk should feel the same. The fall should not feel faster.
166. Jump, hold crouch, and land still: the dart should ease into the guard. A moving air crouch should keep the flare. A still crouch without the dart should feel the same. The land should not feel longer.
167. Jump, hold crouch, let go, and land soft: the dart should open into the absorb. A hard landing should keep the flare. A still crouch should keep the guard. The land should not feel longer.
168. Crouch still, then jump: the guard should ease into the push. A standing jump should feel the same. A crouch walk into a jump should feel the same. The jump should not feel higher.
169. Crouch walk, then jump: the low stride should ease into the push. A still crouch into a jump should feel the same. A standing jump should feel the same. The jump should not feel higher.
170. Ski, then jump: the glide should ease into the push. A still crouch into a jump should feel the same. A crouch walk into a jump should feel the same. A standing jump should feel the same. The jump should not feel higher. Speed should feel the same.
171. Slide, then jump: the wedge should ease into the push. A still crouch into a jump should feel the same. A crouch walk into a jump should feel the same. A ski into a jump should feel the same. A standing jump should feel the same. The jump should not feel higher. The slide should not feel faster.
172. Jump, then ski: the glide or the landing should ease into the stride. A walk into a ski should feel the same. A crouch into a ski should feel the same. A slide into a ski should feel the same. Speed should feel the same. The jump should not feel higher.
173. Jump, then slide: the glide or the landing should ease into the wedge. A crouch into a slide should feel the same. A ski into a slide should feel the same. A jump into a ski should feel the same. The jump should not feel higher. The slide should not feel faster.
174. Air dash, then jump: the burst should ease into the push. A still crouch into a jump should feel the same. A crouch walk into a jump should feel the same. A ski into a jump should feel the same. A slide into a jump should feel the same. The jump should not feel higher. The dash should not feel longer.
175. Climb, then jump: the climb should ease into the push. An air dash into a jump should feel the same. A still crouch into a jump should feel the same. The jump should not feel higher. The climb should not let go faster.
176. Wall run, then jump: the wall exit should ease into the push. A climb into a jump should feel the same. An air dash into a jump should feel the same. The jump should not feel higher. The wall should not let go faster.
177. Jump, then climb: the contact should ease into the grab. A crouch onto the wall should feel the same. A wall run should feel the same. A climb into a jump should feel the same. The jump should not feel higher. The grab should not feel slower.
178. Jump, then wall run: the contact should ease into the attach. A jump into a climb should feel the same. A crouch onto the wall should feel the same. A wall run into a jump should feel the same. The jump should not feel higher. The attach should not feel slower.
179. Fall in a crouch, then jump: the dart should ease into the push. A moving fall should ease from the low stride. A still crouch into a jump should feel the same. A crouch walk into a jump should feel the same. The jump should not feel higher. The fall should not feel faster.
180. Jump, then hold crouch: the apex and the descent should ease into the dart. An air crouch into a jump should feel the same. A moving fall should still end in the low stride. A still crouch into a jump should feel the same. The jump should not feel higher. The fall should not feel faster.
181. Jump, then air dash: the apex should ease into the burst. The burst should still hold. An air dash into a jump should feel the same. A jump into an air crouch should feel the same. The jump should not feel higher. The dash should not feel longer.
182. Land soft, then jump: the absorb should ease into the push. A hard landing should keep its jump. A still crouch into a jump should feel the same. The jump should not feel higher. Staying down should feel the same.
183. Land hard, then jump: the absorb should ease into the push. A soft landing into a jump should feel the same. A still crouch into a jump should feel the same. The jump should not feel higher. Staying down should feel the same.
184. Miss a punch, then jump: the whiff should ease into the push. A crouch miss should feel the same. A soft landing into a jump should feel the same. A hard landing into a jump should feel the same. The jump should not feel higher.
185. Tag, then jump: the connect should ease into the push. A crouch tag should feel the same. A punch miss into a jump should feel the same. The jump should not feel higher.
186. Become It, then jump: the claim should ease into the push. A crouch claim should feel the same. A tag into a jump should feel the same. The jump should not feel higher.
187. Let go of a grapple, then jump: the line should ease into the push. A crouch release should feel the same. Becoming It into a jump should feel the same. The jump should not feel higher. The gate stays off.
188. When the dash is ready, jump: the pulse should ease into the push. A crouch ready should feel the same. A grapple release into a jump should feel the same. The jump should not feel higher. The dash should not feel longer.
189. Jump, then punch: the apex or the landing should ease into the windup. A punch from the ground should feel the same. The jump should not feel higher. The cock should not feel longer.
190. Jump, then tag: the apex or the landing should ease into the connect. A crouch tag should feel the same. A jump into a punch should feel the same. A tag into a jump should feel the same. The jump should not feel higher.
191. Fall in a crouch, then air dash: the dart should ease into the burst. A moving fall should ease from the low stride. The burst should still hold. The fall should not feel faster. The dash should not feel longer. A jump into a tag should feel the same.
192. Air dash, then hold crouch: the burst should ease into the dart. An air crouch into an air dash should feel the same. The burst should still hold. The fall should not feel faster. The dash should not feel longer.
193. Ski, then air dash: the glide should ease into the burst. The burst should still hold. An air crouch into an air dash should feel the same. An air dash into an air crouch should feel the same. Speed should feel the same. The dash should not feel longer. Jet stays off.
194. Slide, then air dash: the wedge should ease into the burst. The burst should still hold. A ski into an air dash should feel the same. The slide should not feel faster. The dash should not feel longer. Jet stays off.
195. Air dash, then ski: the burst should ease into the glide. A slide into an air dash should feel the same. A ski into an air dash should feel the same. Speed should feel the same. The dash should not feel longer. Jet stays off.
196. Air dash, then slide: the burst should ease into the wedge. An air dash into a ski should feel the same. A ski into an air dash should feel the same. A slide into an air dash should feel the same. The slide should not feel faster. The dash should not feel longer. Jet stays off.
197. Climb, then air dash: the climb should ease into the burst. The burst should still hold. An air dash into a slide should feel the same. A ski into an air dash should feel the same. A slide into an air dash should feel the same. The leave should not feel longer. The dash should not feel longer. Jet stays off.
198. Wall run, then air dash: the wall exit should ease into the burst. The burst should still hold. A climb into an air dash should feel the same. The leave should not feel longer. The dash should not feel longer. Jet stays off.
199. Air dash, then climb: the burst should ease into the grab. A wall run into an air dash should feel the same. A climb into an air dash should feel the same. The leave should not feel longer. The dash should not feel longer. Jet stays off.
200. Air dash, then wall run: the burst should ease into the attach. An air dash into a climb should feel the same. A climb into an air dash should feel the same. A wall exit into an air dash should feel the same. The leave should not feel longer. The dash should not feel longer. Jet stays off.
201. Miss a punch, then air dash: the whiff should ease into the burst. The burst should still hold. An air dash into a wall run should feel the same. An air dash into a climb should feel the same. A punch miss into a jump should feel the same. A crouch miss should feel the same. The dash should not feel longer. Jet stays off.
202. Tag, then air dash: the connect should ease into the burst. The burst should still hold. A punch miss into an air dash should feel the same. A tag into a jump should feel the same. A crouch tag should feel the same. The dash should not feel longer. Jet stays off.
203. Become It, then air dash: the claim should ease into the burst. The burst should still hold. A tag into an air dash should feel the same. Becoming It into a jump should feel the same. A crouch claim should feel the same. The dash should not feel longer. Jet stays off.
204. Let go of a grapple, then air dash: the line should ease into the burst. The burst should still hold. Becoming It into an air dash should feel the same. A grapple release into a jump should feel the same. A crouch release should feel the same. The dash should not feel longer. The gate stays off. Jet stays off.
205. Air dash, then punch: the burst should ease into the windup. A grapple release into an air dash should feel the same. A jump into a punch should feel the same. The cock should not feel longer. The dash should not feel longer. Jet stays off.
206. Air dash, then tag: the burst should ease into the connect. An air dash into a punch should feel the same. A jump into a tag should feel the same. A crouch tag should feel the same. The dash should not feel longer. Jet stays off.
207. Soft land, then air dash: the absorb should ease into the burst. The burst should still hold. An air dash into a tag should feel the same. A soft landing into a jump should feel the same. A hard landing should feel the same. The landing should not feel longer. The dash should not feel longer. Jet stays off.
208. Hard land, then air dash: the deeper absorb should ease into the burst. The burst should still hold. A soft landing into an air dash should feel the same. A hard landing into a jump should feel the same. The landing should not feel longer. The dash should not feel longer. Jet stays off.
209. Soft land, then punch: the absorb should ease into the windup. A hard landing into an air dash should feel the same. A jump into a punch should feel the same. A soft landing into a jump should feel the same. The cock should not feel longer. The landing should not feel longer.
210. Hard land, then punch: the deeper absorb should ease into the windup. A soft landing into a punch should feel the same. A jump into a punch should feel the same. A hard landing into a jump should feel the same. The cock should not feel longer. The landing should not feel longer.
211. Soft land, then tag: the absorb should ease into the connect. A hard landing into a punch should feel the same. A jump into a tag should feel the same. A soft landing into a jump should feel the same. A crouch tag should feel the same. The connect should not feel longer. The landing should not feel longer.
212. Hard land, then tag: the deeper absorb should ease into the connect. A soft landing into a tag should feel the same. A jump into a tag should feel the same. A hard landing into a jump should feel the same. A crouch tag should feel the same. The connect should not feel longer. The landing should not feel longer.
213. Ski, then punch: the glide should ease into the windup. A hard landing into a tag should feel the same. A ski into a jump should feel the same. A ski into an air dash should feel the same. A jump into a punch should feel the same. The cock should not feel longer. Speed should feel the same. Jet stays off.
214. Slide, then punch: the wedge should ease into the windup. A ski into a punch should feel the same. A slide into a jump should feel the same. A slide into an air dash should feel the same. The slide should not feel faster. The cock should not feel longer. Jet stays off.
215. Ski, then tag: the glide should ease into the connect. A slide into a punch should feel the same. A ski into a punch should feel the same. A ski into a jump should feel the same. A jump into a tag should feel the same. A crouch tag should feel the same. The connect should not feel longer. Speed should feel the same. Jet stays off.
216. Slide, then tag: the wedge should ease into the connect. A ski into a tag should feel the same. A slide into a punch should feel the same. A slide into a jump should feel the same. A crouch tag should feel the same. The slide should not feel faster. The connect should not feel longer. Jet stays off.
217. Climb, then punch: the grab should ease into the windup. A slide into a tag should feel the same. A climb into an air dash should feel the same. A climb into a jump should feel the same. A wall run should feel the same. The cock should not feel longer. The leave should not feel longer. Jet stays off.
218. Climb, then tag: the grab should ease into the connect. A climb into a punch should feel the same. A climb into an air dash should feel the same. A climb into a jump should feel the same. A wall run should feel the same. The connect should not feel longer. The leave should not feel longer. Jet stays off.
219. Wall run, then punch: the leave should ease into the windup. A climb into a tag should feel the same. A wall exit into an air dash should feel the same. A wall exit into a jump should feel the same. A climb into a punch should feel the same. The cock should not feel longer. The leave should not feel longer. Jet stays off.
220. Wall run, then tag: the leave should ease into the connect. A wall exit into a punch should feel the same. A wall exit into an air dash should feel the same. A wall exit into a jump should feel the same. A climb into a tag should feel the same. The connect should not feel longer. The leave should not feel longer. Jet stays off.
221. Air crouch, then punch: the dart should ease into the windup. A wall exit into a tag should feel the same. An air crouch into an air dash should feel the same. An air crouch into a jump should feel the same. A jump into a punch should feel the same. The cock should not feel longer. The fall should not feel faster. Jet stays off.
222. Air crouch, then tag: the dart should ease into the connect. An air crouch into a punch should feel the same. An air crouch into an air dash should feel the same. An air crouch into a jump should feel the same. A jump into a tag should feel the same. A crouch tag should feel the same. The connect should not feel longer. The fall should not feel faster. Jet stays off.
223. Become It, then punch: the claim should ease into the windup. An air crouch into a tag should feel the same. Becoming It into an air dash should feel the same. Becoming It into a jump should feel the same. A crouch claim should feel the same. The cock should not feel longer. The claim should not feel longer. Jet stays off.
224. Become It, then tag: the claim should ease into the connect. Becoming It into a punch should feel the same. Becoming It into an air dash should feel the same. Becoming It into a jump should feel the same. An air crouch into a tag should feel the same. A crouch claim should feel the same. The connect should not feel longer. The claim should not feel longer. Jet stays off.
225. Let go of a grapple, then punch: the line should ease into the windup. Becoming It into a tag should feel the same. A grapple release into an air dash should feel the same. A grapple release into a jump should feel the same. A crouch release should feel the same. The cock should not feel longer. The gate stays off. Jet stays off.
226. Let go of a grapple, then tag: the line should ease into the connect. A grapple release into a punch should feel the same. A grapple release into an air dash should feel the same. A grapple release into a jump should feel the same. Becoming It into a tag should feel the same. A crouch release should feel the same. The connect should not feel longer. The gate stays off. Jet stays off.
227. When the dash is ready, punch: the pulse should ease into the windup. A grapple release into a tag should feel the same. A dash coming off cooldown into a jump should feel the same. A crouch ready should feel the same. The cock should not feel longer. The dash should not feel longer. Jet stays off.
228. When the dash is ready, tag: the pulse should ease into the connect. A dash coming off cooldown into a punch should feel the same. A dash coming off cooldown into a jump should feel the same. A crouch ready should feel the same. A grapple release into a tag should feel the same. The connect should not feel longer. The dash should not feel longer. Jet stays off.
229. Punch, then tag: the cock or the strike should ease into the connect. A dash coming off cooldown into a tag should feel the same. A dash coming off cooldown into a punch should feel the same. A crouch tag should feel the same. The connect should not feel longer. The cock should not feel longer. Jet stays off.
230. Tag, then punch: the connect should ease into the windup. A punch into a tag should feel the same. A dash coming off cooldown into a punch should feel the same. A crouch tag should feel the same. A tag into a jump should feel the same. The cock should not feel longer. The connect should not feel longer. Jet stays off.
231. Miss a punch, then tag: the whiff should ease into the connect. A tag into a punch should feel the same. A punch into a tag should feel the same. A punch miss into a jump should feel the same. A punch miss into an air dash should feel the same. A crouch miss should feel the same. The connect should not feel longer. The whiff should not feel longer. Jet stays off.
232. Punch, then ski: the cock or the strike should ease into the glide. A ski into a punch should feel the same. A punch miss into a tag should feel the same. A slide into a ski should feel the same. A jump into a ski should feel the same. The glide should not feel longer. The cock should not feel longer. Speed should feel the same. Jet stays off.
233. Punch, then slide: the cock or the strike should ease into the wedge. A punch into a ski should feel the same. A slide into a punch should feel the same. A crouch into a slide should feel the same. A ski into a slide should feel the same. A jump into a slide should feel the same. The slide should not feel faster. The cock should not feel longer. Jet stays off.
234. Tag, then ski: the connect should ease into the glide. A punch into a slide should feel the same. A punch into a ski should feel the same. A tag into a jump should feel the same. A slide into a ski should feel the same. The glide should not feel longer. The connect should not feel longer. Speed should feel the same. Jet stays off.
235. Tag, then slide: the connect should ease into the wedge. A tag into a ski should feel the same. A punch into a slide should feel the same. A slide into a tag should feel the same. A crouch into a slide should feel the same. A ski into a slide should feel the same. A jump into a slide should feel the same. The slide should not feel faster. The connect should not feel longer. Jet stays off.
236. Still crouch, then punch: the guard should ease into the windup. A tag into a slide should feel the same. A crouch walk into a punch should feel the same. An air crouch into a punch should feel the same. A slide into a punch should feel the same. A crouch into a slide should feel the same. The cock should not feel longer. The guard should not feel longer. Jet stays off.
237. Still crouch, then tag: the guard should ease into the connect. A still crouch into a punch should feel the same. A crouch walk into a tag should feel the same. An air crouch into a tag should feel the same. A slide into a tag should feel the same. The connect should not feel longer. The guard should not feel longer. Jet stays off.
238. Soft land, then ski: the absorb should ease into the glide. A still crouch into a tag should feel the same. A hard landing into a ski should feel the same. A jump into a ski should feel the same. A punch into a ski should feel the same. A slide into a ski should feel the same. The glide should not feel longer. The landing should not feel longer. Speed should feel the same. Jet stays off.
239. Soft land, then slide: the absorb should ease into the wedge. A soft landing into a ski should feel the same. A hard landing into a slide should feel the same. A jump into a slide should feel the same. A punch into a slide should feel the same. A crouch into a slide should feel the same. A ski into a slide should feel the same. The slide should not feel faster. The landing should not feel longer. Jet stays off.
240. Climb, then ski: the grab should ease into the glide. A soft landing into a slide should feel the same. A wall run into a ski should feel the same. A climb into a jump should feel the same. A climb into a punch should feel the same. A jump into a ski should feel the same. The glide should not feel longer. The leave should not feel longer. Speed should feel the same. Jet stays off.
241. Climb, then slide: the grab should ease into the wedge. A climb into a ski should feel the same. A wall run into a slide should feel the same. A climb into a jump should feel the same. A soft landing into a slide should feel the same. A jump into a slide should feel the same. The slide should not feel faster. The leave should not feel longer. Jet stays off.
242. Wall run, then ski: the leave should ease into the glide. A climb into a slide should feel the same. A climb into a ski should feel the same. A wall run into a jump should feel the same. A soft landing into a ski should feel the same. The glide should not feel longer. The leave should not feel longer. Speed should feel the same. Jet stays off.
243. Wall run, then slide: the leave should ease into the wedge. A wall run into a ski should feel the same. A climb into a slide should feel the same. A wall run into a jump should feel the same. A soft landing into a slide should feel the same. A jump into a slide should feel the same. The slide should not feel faster. The leave should not feel longer. Jet stays off.
244. Air dash, then ski: the burst should ease into the glide. A wall run into a slide should feel the same. A wall run into a ski should feel the same. A climb into a ski should feel the same. An air dash into a slide should feel the same. The glide should not feel longer. The dash should not feel longer. Speed should feel the same. Jet stays off.
245. Hard land, then ski: the absorb should ease into the glide. An air dash into a ski should feel the same. A soft landing into a ski should feel the same. A jump into a ski should feel the same. A hard landing into a slide should feel the same. The glide should not feel longer. The landing should not feel longer. Speed should feel the same. Jet stays off.
246. Air crouch, then ski: the dart should ease into the glide. A hard landing into a ski should feel the same. An air dash into a ski should feel the same. A jump into a ski should feel the same. An air crouch into a slide should feel the same. The glide should not feel longer. The fall should not feel longer. Speed should feel the same. Jet stays off.
247. Let go of a grapple, then ski: the line should ease into the glide. An air crouch into a ski should feel the same. A grapple release into a jump should feel the same. A grapple release into a punch should feel the same. An air dash into a ski should feel the same. The glide should not feel longer. The line should not feel longer. The gate stays off. Jet stays off.
248. Let go of a grapple, then slide: the line should ease into the wedge. A grapple release into a ski should feel the same. A grapple release into a jump should feel the same. A wall run into a slide should feel the same. A soft landing into a slide should feel the same. The slide should not feel faster. The line should not feel longer. The gate stays off. Jet stays off.
249. Become It, then ski: the claim should ease into the glide. A grapple release into a slide should feel the same. Becoming It into a punch should feel the same. Becoming It into a jump should feel the same. A grapple release into a ski should feel the same. The glide should not feel longer. The claim should not feel longer. Speed should feel the same. Jet stays off.
250. Become It, then slide: the claim should ease into the wedge. Becoming It into a ski should feel the same. Becoming It into a punch should feel the same. Becoming It into a jump should feel the same. A grapple release into a slide should feel the same. The slide should not feel faster. The claim should not feel longer. Jet stays off.
251. When the dash is ready, ski: the pulse should ease into the glide. Becoming It into a slide should feel the same. A dash coming off cooldown into a punch should feel the same. A dash coming off cooldown into a tag should feel the same. A dash coming off cooldown into a jump should feel the same. The glide should not feel longer. The dash should not feel longer. Speed should feel the same. Jet stays off.
252. When the dash is ready, slide: the pulse should ease into the wedge. A dash coming off cooldown into a ski should feel the same. A dash coming off cooldown into a punch should feel the same. A dash coming off cooldown into a tag should feel the same. A dash coming off cooldown into a jump should feel the same. The slide should not feel faster. The dash should not feel longer. Jet stays off.
253. Miss a punch, then ski: the whiff should ease into the glide. A dash coming off cooldown into a slide should feel the same. A punch into a ski should feel the same. A punch miss into a tag should feel the same. A punch miss into a jump should feel the same. A punch miss into an air dash should feel the same. The glide should not feel longer. The whiff should not feel longer. Speed should feel the same. Jet stays off.
254. Miss a punch, then slide: the whiff should ease into the wedge. A punch miss into a ski should feel the same. A punch into a slide should feel the same. A punch miss into a tag should feel the same. A punch miss into a jump should feel the same. A punch miss into an air dash should feel the same. The slide should not feel faster. The whiff should not feel longer. Jet stays off.
255. Still crouch, then slide: the guard should ease into the wedge. A punch miss into a slide should feel the same. A still crouch into a punch should feel the same. A still crouch into a tag should feel the same. A still crouch into a ski should feel the same. A crouch walk into a slide should feel the same. The slide should not feel faster. The guard should not feel longer. Jet stays off.
256. Ski, then slide: the glide should ease into the wedge. A still crouch into a slide should feel the same. A crouch walk into a ski should feel the same. A crouch walk into a slide should feel the same. A slide into a ski should feel the same. The slide should not feel faster. The glide should not feel longer. Jet stays off.
257. Slide, then air dash: the wedge should ease into the burst. The burst should still hold. A ski into a slide should feel the same. A ski into an air dash should feel the same. An air dash into a slide should feel the same. The slide should not feel faster. The dash should not feel longer. Jet stays off.
258. Slide, then jump: the wedge should ease into the jump. A ski into a jump should feel the same. A still crouch into a jump should feel the same. A crouch walk into a jump should feel the same. A standing jump should feel the same. A slide into an air dash should feel the same. The jump should not feel higher. The slide should not feel faster. Jet stays off.
259. Jump, then air dash: the jump should ease into the burst. The burst should still hold. A slide into a jump should feel the same. A ski into a jump should feel the same. A slide into an air dash should feel the same. The jump should not feel higher. The dash should not feel longer. Jet stays off.
260. Still crouch, then air dash: the guard should ease into the burst. The burst should still hold. A jump into an air dash should feel the same. A slide into an air dash should feel the same. An air crouch into an air dash should feel the same. The guard should not feel longer. The dash should not feel longer. Jet stays off.
261. Punch, then air dash: the punch should ease into the burst. The burst should still hold. A still crouch into an air dash should feel the same. A punch miss into an air dash should feel the same. A tag into an air dash should feel the same. The cock should not feel longer. The dash should not feel longer. Jet stays off.
262. Crouch walk, then air dash: the low stride should ease into the burst. The burst should still hold. A hard landing into a jump should feel the same. A soft landing into a jump should feel the same. A still crouch into an air dash should feel the same. The stride should not feel longer. The dash should not feel longer. Jet stays off.
263. When the dash is ready, air dash: the pulse should ease into the burst. The burst should still hold. A crouch walk into an air dash should feel the same. A dash coming off cooldown into a jump should feel the same. A dash coming off cooldown into a punch should feel the same. The pulse should not feel longer. The dash should not feel longer. Jet stays off.
264. Punch, then jump: the punch should ease into the jump. Becoming It into a jump should feel the same. A tag into a jump should feel the same. A punch miss into a jump should feel the same. The cock should not feel longer. The jump should not feel higher. Jet stays off.
265. Run, then ski: the stride should ease into the glide. The glide should then hold. A walk into a ski should feel the same. A slide into a ski should feel the same. A jump into a ski should feel the same. The glide should not feel longer. Speed should feel the same. Jet stays off.
266. Run, then slide: the stride should ease into the wedge. The wedge should then hold. A run into a ski should feel the same. A ski into a slide should feel the same. A still crouch into a slide should feel the same. The slide should not feel faster. The wedge should not feel longer. Jet stays off.
267. Jump, then still crouch: the jump should ease into the guard. The guard should then hold. A run into a slide should feel the same. A jump into a ski should feel the same. A jump into a slide should feel the same. The jump should not feel higher. The guard should not feel longer. Jet stays off.
268. Air dash, then still crouch: the burst should ease into the guard. The guard should then hold. A jump into a still crouch should feel the same. A still crouch into an air dash should feel the same. The dash should not feel longer. The guard should not feel longer. Jet stays off.
269. Walk, then slide: the walk should ease into the wedge. The wedge should then hold. An air dash into a still crouch should feel the same. A run into a slide should feel the same. A crouch walk into a slide should feel the same. The slide should not feel faster. The wedge should not feel longer. Jet stays off.
270. Walk, then ski: the walk should ease into the glide. The glide should then hold. A walk into a slide should feel the same. A run into a ski should feel the same. A crouch walk into a ski should feel the same. The glide should not feel longer. Speed should feel the same. Jet stays off.
271. Crouch walk, then jump: the low stride should ease into the jump. The jump should then hold. A walk into a ski should feel the same. A still crouch into a jump should feel the same. A crouch walk into an air dash should feel the same. The jump should not feel higher. The stride should not feel longer. Jet stays off.
272. Walk, then jump: the walk should ease into the jump. The jump should then hold. A crouch walk into a jump should feel the same. A walk into a ski should feel the same. A still crouch into a jump should feel the same. The jump should not feel higher. The stride should not feel longer. Jet stays off.
273. Walk, then air dash: the walk should ease into the burst. The burst should then hold. A walk into a jump should feel the same. A crouch walk into an air dash should feel the same. A still crouch into an air dash should feel the same. The dash should not feel longer. The stride should not feel longer. Jet stays off.
274. Still crouch, then jump: the guard should ease into the jump. The jump should then hold. A walk into an air dash should feel the same. A crouch walk into a jump should feel the same. A jump into a still crouch should feel the same. The jump should not feel higher. The guard should not feel longer. Jet stays off.
275. Walk, then punch: the walk should ease into the cock. The windup should then hold. The strike should feel the same. A still crouch into a jump should feel the same. A jump into a punch should feel the same. A still crouch into a punch should feel the same. The cock should not feel longer. The stride should not feel longer. Jet stays off.
276. Walk, then tag: the walk should ease into the connect. The connect should then hold. A walk into a punch should feel the same. A jump into a tag should feel the same. A still crouch into a tag should feel the same. The connect should not feel longer. The stride should not feel longer. Jet stays off.
277. Run, then punch: the stride should ease into the cock. The windup should then hold. The strike should feel the same. A walk into a tag should feel the same. A walk into a punch should feel the same. A jump into a punch should feel the same. The cock should not feel longer. The stride should not feel longer. Jet stays off.

## Known leftovers

- Prefab/mat dirt after Hub visuals / URP regen -> do not commit unless intentional.
- Flat HiPoly mannequins may skip hierarchical `DummyLocomotor` binds (primitive / bindable-bone path is the readable tell).
- Legacy contact `TryTag` radius still exists on motor; play modes use punch transfer.
- Trail avoid starts peeling ~9.2 m (weight 0.80) off a foreign ribbon (HUD TRAIL! soft warn ~6.2 m).
- AI punch tell drops for ~0.32 s after a juke/leave-cone whiff so the arm drop is readable (still needs a fuller human feel pass). No spectator camera: an eliminated player stays on their body with a waiting line. Playground music stays silent: `music_playground_bed_loop.wav` is meta only, so PlayMusic returns. No hitstop. Hot Potato flee may air-dash once while airborne if the motor CD is ready. A juke whiff also refreshes weave so they peel off the punch line.
- Dash HUD: jet off = one CD bar (dark track, cyan fill, mint when ready or bursting) and one DASH line (DASH! while bursting). Jet on keeps a dash CD line under JET. Cooldown stays 30 s.
- Dummy MissRecover: limp whiff drops faster than HitRecover hold (short shoulder sag). AI HoldPunchTelegraph matches the windup cock beside the head.
- Bots hold still on countdown, results, and Idle (no chase until Playing).
- Resume / leave-results: look, punch, jump, dash, and lunge ignore two frames after the cursor locks (shared resume gate + cameras) so the menu click that closed the card cannot yaw or punch. Rematch / F-keys from Direct Play also arm that gate when the cursor locks.
- Do not hand-author `TagURP*.asset` YAML; use **Ensure URP Pipeline**.

## Audio
- HUD shows MUTED (M), MUSIC OFF (N), or both chips when both mutes are on. M and N work during play, Boot, results, and the subpanels. Pause and the audio card use that same listener, so one press toggles once.
- Dash cooldown bar: dark track, cyan fill while cooling, mint when ready or during the burst. Label still says DASH, the seconds, or DASH ready. Cooldown stays 30 s.
- Master volume / mute: Boot or pause Audio. Up/Down highlights SFX, Music, Mute, Music mute, Back. Left/Right steps the highlighted SFX or music bed (default bed 0.35). M mute all. N music only. On the main pause card, Up/Down is still the music bed. Saved in PlayerPrefs on AudioMaster.

## Results
- Rematch / Menu: results ignore activate keys for ~0.25s and one-shot R/Q/Esc/click (Esc mirrors menu) so the round-end key cannot rematch or quit early. Keys 1-2 and Left/Right can move the highlight during that arm and still stop at the ends. Enter and Space wait until the arm ends, then use the highlight. R still rematches. A click during the arm only moves the highlight. The loose round-end card uses the same keys. Punch ForceEnd on results and pause.
- Direct Play pause matches Boot: Left/Right arms Resume, Controls, Look, Audio, Quit. Enter or Space uses that row. Esc on the main card resumes. Esc inside Controls, Look, or Audio only closes the panel. Those three panels use the same Up/Down highlight as Boot. Q to Boot unlocks the cursor and stops the music bed.
- Controls / Look / Audio close on play, results, and Boot (F1-F4, rematch, Q). A panel left open on the pause card does not stay drawn over the round or the Boot menu. Direct Play clears its pause overlay when results start so Left/Right still move Rematch / Menu.
- Boot, pause, and subpanel clicks are mouse-only. Enter/Space uses the highlight.

## Shippable slice checklist
1. F1 Hot Potato / F2 Least It / F3 Trail Tag / F4 Free play start a clean round (menu cursor syncs).
2. Controls: Up/Down highlights Dash, Punch, Back. Left/Right steps the highlighted bind. Enter on Back closes. E still punches.
3. Audio panel: Up/Down highlights SFX, Music, Mute, Music mute, Back. Left/Right steps that row. M mute all. N music mute.
4. Results: 0.25s arm; Left/Right can move during the arm; Enter activates after it; R rematch; Q/Esc menu; one-shot. A click does not double-fire with Enter.
5. Trail Tag SD: HUD says SD; center flash on rising edge; rematch re-arms flash.
   Center SD flash holds ~1.0 s (same beat as It handoff / F-key mode flash).
   It handoff flash re-arms after rematch so spawn-as-It and the first tag still read.
   Trail OUT! / TRAIL HIT flash also holds ~1.0 s.
   Near-miss soft edge + TRAIL! starts ~6.2 m from a foreign ribbon (readability only; hit rules unchanged).
6. Chase, in order: SW mushroom -> hopscotch SW -> fort-west spring -> soft-play (SoftS apron, tube street, slide beam, dome, ground slide, deck tube) -> soft-merry bar across the south spine -> merry (bench, picnic, west spring) -> merry-north -> north spine -> NW arch -> NW cluster -> hopscotch NW -> astro. East: hopscotch SE -> SE arch -> SE cluster -> army mouths and spiral -> open kickball -> swing beam -> swings -> knight mouths and spiral -> NE arch -> NE cluster -> hopscotch NE -> crash cross. SoftS aisles, the NE hop, and the merry-spine bar were not densified. Mega and Tube90 are not placed. Feel was not edited.
7. Boot Up/Down and pause Left/Right arm a row and stop at the ends. Enter uses it. Play stays the default Boot row.
8. Who-plays and mode select stop at the first and last row. A Boot or Pause click uses that row only. Enter does not also fire a different button.
9. Resume or leave-results: the tip's shared resume gate still drops look and one-shots for two frames after the cursor locks. This merge does not change that gate.
10. Direct Play pause matches Boot, including Controls / Look / Audio highlight. Esc on a subpanel stays paused. Q back to Boot shows the cursor.
11. Open Controls from pause, then F1: the panel closes and the round runs. Results and Boot are not covered by that panel. Direct Play results still accept keys if the local pause card was up.
12. M during play mutes once and shows the chip. N mutes music. The dash bar fill is visible against a dark track. Hot Potato heats the It beacon with the fuse.
13. First-run Boot copy names the punch key and clears after a round, including Direct Play back to Boot. Opening Couch or Mode select does not clear it. The long countdown hint shows once and says move versus sprint. Rematch uses the short orange-hat line. Boot pause H opens Controls. Digits highlight. Enter / Space confirms.
14. Who-plays Esc returns to Boot on Couch. Mode select Esc returns to who-plays after Couch, and to Boot after the Mode row. The Mode row does not reset the player count. Highlights match the saved count and the last mode.
15. Results: keys 1-2 highlight Rematch and Menu. Enter or Space activates after the 0.25s arm. R rematches. Q/Esc menu. A click during the arm only moves the highlight.
16. Player and bot spawn the curved Hier HiPoly mannequin. Runner is Tan Hier. It is Orange Hier. Flat Dummy_Runner on the Play scene does not win. Navy Spade only if Hier has no limb bones.
17. Run: recovery knee flexes, stance leg stays nearly straight. Dash whip does not add arm roll. Slide is a low crouch. Land buckles. Punch connect stays in front of the chest.
18. Run arms oppose the legs. Forward arm is the opposite side of the forward thigh. Hands stay clear of the pelvis. Hier spawn still wins over flat Dummy_Runner.
19. Slide silhouette is flat (chest down, lead knee tucked, trail leg back). Punch connect is a long arm in front of the chest.
20. Land holds a short buckle, then eases into the run. A hop still buckles. Arms stay clear of the pelvis.
21. Air dash whip stretches early and settles before the burst ends. No extra arm roll into the pelvis.
22. v0.1 Tan and Orange Hier still bind UpperArm / LowerArm / UpperLeg / LowerLeg. Pose drivers were not reverted.
23. Jump arms reach up with a knee tuck. Fall arms trail back and the legs lengthen. Hands stay clear of the pelvis.
24. Tag handoff: the tagged runner guards. The new It lifts one knee. Hands stay clear of the pelvis.
25. Climb is hand-over-hand with one bent knee. Wall-run plants the wall hand and steps the outer leg. Hands stay clear of the hips.
26. Idle hands hang forward and out of the hips. The offset is gone once the stride is up. No extra arm roll.
27. Jump tuck and fall trail show before the landing, including a short hop. Hands stay clear of the hips.
28. Run plant holds. Front knee bends, back leg stays long, arms still oppose the front leg. No extra arm roll.
29. Slide arms are a long low line. Elbows stay nearly straight. Lead knee tucked, trail leg long.
30. Land arms come out for balance during the buckle, then ease into the run. Hands stay clear of the hips.
31. Climb hands stay a long line through the reach and the pull. The wall-run hand presses with the stride. The outer arm stays straight. Hands stay clear of the hips.
32. Punch windup sits beside the head, clear of the chest. Connect stays a long line in front. Windup time is unchanged.
33. After an air dash the arms ease out of the hang. They do not whip again when the burst ends. Dash time and cooldown are unchanged.
34. Tag catch is a long V of arms with both knees bent. Hands stay clear of the chest. The new It still lifts one knee. Neither pose matches a landing.
35. Idle into a run keeps the hands outside the hips. Resting arms have no extra roll. The stride still opposes the front leg.
36. Ski eases into a lower glide with the arms out, then eases back into the run. It does not pop. Jet stays off. Ski speed is unchanged.
37. Leaving a wall run eases into the fall or the run. The wall hand and the outer leg do not snap. Wall-run speed is unchanged.
38. Leaving a climb eases into the fall or the run. The reaching hand and the stepping knee do not snap. Climb speed is unchanged.
39. Grapple pose is a long two-arm reach with long legs, only while enableGrapple is on and a rope is attached. The default gate stays off. Jet stays off.
40. The new It raises one arm and holds the other out, chest open, one knee up. The tagged runner still uses the two-arm V. The poses do not match.
41. Punch connect eases into the run during the recover. The fist does not snap back when the punch ends. Windup time is unchanged.
42. Jump apex hangs the arms out before the fall trail. The leave is still a tuck. Jump height is unchanged.
43. Ski glide keeps a knee and a short arm swing. The run knee still lifts higher. The blend eases. Ski speed is unchanged. Jet stays off.
44. Slide arms sit out from the chest on the flat wedge, and the head stays up. A crouch stays a bent-elbow guard. Slide speed is unchanged.
45. Crouch is a low guard with both knees bent and the elbows folded in front. Slide stays the flat wedge with straight arms and a trail leg. Holding crouch still slides only with speed. Slide speed is unchanged.
46. Air-crouch fall is a nose-down dart with the arms in. A normal fall still trails the arms. The 2x fall speed is unchanged.
47. Landing into a run opens the trail leg and the arms into the stride while the front knee is still up. A standstill land opens both legs together. Land time is unchanged.
48. The run arm opposite the front knee is a long reach, wider than the back arm. The back elbow bends short of the hip. Only the front knee lifts.
49. A tagging punch eases the fist into the It claim. A hit that does not tag still eases into the run. The catch stays the two-arm V. Windup time is unchanged.
50. The tagged runner's V eases into the stride: arms and the trail leg first, one knee still up. A standstill catch opens both legs together. Flinch time is unchanged. The new It claim is unchanged.
51. Idle breath and a slight side sway. Hands stay clear of the hips. No extra arm roll. The sway is gone once the stride is up.
52. A stop closes the stride under the hips, then idle. Braking into a walk shortens the stride. A leg does not freeze out. Speed is unchanged.
53. Walk and sprint ease into each other. Stride length and step rate change together. The feet keep moving and the hips stay level. Speed is unchanged.
54. A sharp turn plants the outside foot. The chest and the hips lean together. Hands stay clear of the hips. Look speed is unchanged.
55. A short hop into a walk bends the knees, then the stride returns under the hips. The arms stay in the walk. A hard landing still flares the arms. Land time is unchanged.
56. A stop from a sprint settles the last hip sway into the idle breath and sway. The hips do not freeze flat or pop. Speed is unchanged.
57. A walk from idle pushes off the planted foot into the stride. Idle sway fades. The feet do not skate. Speed is unchanged.
58. A crouch or a slide from a run drops into the pose. The speed you already have carries. Letting go returns to the stride under the hips. The slide does not speed up.
59. After an air dash, the feet return to the stride under the hips. They do not skate. The burst stays short and the cooldown is unchanged.
60. A wall run or a climb sets the hand on the surface, then the swing starts. The arm does not pop. The exit returns under the hips. Exit time is unchanged.
61. After a punch, the arm opposite the front knee returns to the stride. The hips do not stay twisted. The fist still eases out. Windup time is unchanged.
62. A grapple, if turned on, settles the hands and the chest into the long line. They do not twist. The gate stays off. Range and speed are unchanged.
63. After a tag, the hands and the chest ease into the stride together, under the hips. They do not stay folded and then pop. One knee can still be up. Flinch time is unchanged.
64. After you become It, the hands and the chest ease into the stride. They do not stay in the claim and then pop. One knee can still be up. The claim time is unchanged.
65. A ski and a run ease the feet under the hips. They do not skate, and the hips do not pop. Speed is unchanged. Jet stays off.
66. A hard landing from a stand eases the knees into the idle breath and sway. The arms flare, then ease, and do not lock. A short hop does not flare the arms. Land time is unchanged.
67. An air crouch reads as a crouch in the air. The arms ease into the landing and do not pop. The 2x fall speed is unchanged.
68. An air dash pitches the chest and throws the arms wide for the short burst. They ease back after. Duration and cooldown are unchanged.
69. Leaving a wall eases the hips and the feet into the stride. They do not pop. The hands still take the same time to leave. A climb leaves the same way.
70. While running, the reaching arm follows the look and stays clear of the hip. It does not fight the stride. Look speed is unchanged.
71. Letting go of a slide stands up into the run under the hips. The feet do not pop. The slide does not speed up.
72. A jump pushes off the planted foot. The other knee comes up, then the tuck. Jump height is unchanged.
73. A punch cocks beside the head and holds that beat, then strikes. Windup time is unchanged.
74. A stop from a sprint plants the last foot under the hip, then the idle sway. The foot does not skate. Speed is unchanged.
75. In the air, the arms stay clear of the torso. The jump tuck is unchanged. Look speed is unchanged.
76. Starting or stopping a walk keeps the hands forward and out. They do not drift into the hips. Speed is unchanged.
77. A crouch walk is a short shuffle under the hips. The feet do not skate. The crouch does not speed up. A still crouch stays the guard.
78. Tagged while standing, the hands and the chest ease into the idle breath. They do not freeze and then pop. Both knees can still bend. Flinch time is unchanged.
79. A hard landing into a sprint brings the arms into the stride under the hips. They flare, then ease, and do not lock. A short hop does not flare the arms. Land time is unchanged.
80. A turn while sprinting plants the outside foot. The chest and the hips lean together. The waist does not twist. Speed is unchanged. Look speed is unchanged.
81. Becoming It while standing eases the hands and the chest into the idle breath. They do not freeze and then pop. One knee can still be up. The claim time is unchanged.
82. Letting go of a grapple eases the hands and the chest out of the long line into the run or the idle. They do not twist. The gate stays off. Range and speed are unchanged.
83. A punch that misses while standing eases the fists into the idle hands. They do not freeze and then pop. Windup time is unchanged.
84. When the dash cooldown ends, the chest and the arms settle. It is not a second whip. Duration and cooldown are unchanged.
85. A walk turn plants the outside foot at a medium yaw. It does not wait for a sharp turn. Look speed is unchanged.
86. After an air dash, a soft landing bends the knees and keeps the arms in the stride. They do not flare. Duration and cooldown are unchanged.
87. Letting go of a still crouch eases the hips into the idle breath and sway. They do not pop flat. Speed is unchanged.
88. A walk into a sprint pushes off the back foot, then the stride opens. The feet do not skate. Speed is unchanged.
89. A soft landing into a walk settles the knees into the stride. It does not come to a stop. A hard landing still absorbs. Land time is unchanged.
90. Letting a slide die into a stand brings the body up into the idle breath. The hips do not pop. A slide into a run is unchanged. Speed is unchanged.
91. Letting go of a crouch walk raises the hips into the stride. They do not hitch. The feet keep stepping. Speed is unchanged.
92. A sprint into a walk closes the stride with the step. The feet do not skate to a stop. Speed is unchanged.
93. A walk turn into a sprint plants the outside foot, then the stride opens. The feet do not skate. Look speed is unchanged. Speed is unchanged.
94. A hard landing into a walk absorbs, then takes a step. It does not sit in the idle. A stand still absorbs. Land time is unchanged.
95. After a jump, the arms ease into the look pose in the air. They do not snap. Look speed is unchanged. Jump height is unchanged.
96. Leaving a wall into a walk eases the hands into the stride. They do not hitch. The leave time is unchanged.
97. Leaving a climb into a walk eases the hands into the stride. They do not hitch. The leave time is unchanged. A drop keeps the old leave.
98. An air crouch into a soft landing opens into the absorb. It does not stay folded and then pop. The fast fall is unchanged. Land time is unchanged.
99. A punch that misses while walking returns the hands to the stride. They do not drop into the idle. A standing miss still eases into the idle hands. Windup time is unchanged.
100. After a tag while walking, the arms settle into the stride. They do not drop into the idle. A standing catch still eases into the idle breath. Flinch time is unchanged.
101. After you become It while walking, the arms settle into the stride. They do not drop into the idle. One knee can still be up. A standing claim still eases into the idle breath. Claim time is unchanged.
102. Letting go of a grapple while walking returns the hands to the stride. They do not hitch. A sprint and a stand keep the old leave. The gate stays off. Range and speed are unchanged.
103. When the dash is ready and you are standing, the chest and the arms give a small pulse, then the idle breath. It is not a second whip. Duration and cooldown are unchanged.
104. Letting go of a ski into a walk returns the stride. The feet do not skate. A ski into a run is unchanged. Speed is unchanged. Jet stays off.
105. A walk into a ski eases the legs into the glide. They do not snap. Speed is unchanged. Jet stays off.
106. A sprint into a ski closes the stride into the glide. It does not pop. Speed is unchanged. Jet stays off.
107. Letting go of a ski into a sprint opens the glide into the stride. It does not pop. Speed is unchanged. Jet stays off.
108. A crouch walk into a sprint raises the hips and opens the stride. It does not pop. A crouch walk into a walk is unchanged. Speed is unchanged.
109. A soft landing into a sprint absorbs, then opens into the stride. It does not come to a stop. A hard landing still absorbs. Land time is unchanged.
110. A hard landing into a sprint absorbs, then opens into the stride. It does not sit in the buckle. A hard landing into a walk still takes a step. Land time is unchanged.
111. Leaving a wall into a sprint opens the hands into the stride. They do not hitch. The leave time is unchanged.
112. Leaving a climb into a sprint opens the hands into the stride. They do not hitch. The leave time is unchanged. A climb into a walk is unchanged.
113. A punch that misses while sprinting returns the hands to the stride. They do not stay in the limp. A walking miss still returns to the walk. Windup time is unchanged.
114. After a tag while sprinting, the arms settle into the stride. They do not stay folded. A walking tag still settles into the walk. Flinch time is unchanged.
115. After you become It while sprinting, the arms settle into the stride. They do not stay folded. One knee can still be up. A walking claim still settles into the walk. Claim time is unchanged.
116. Letting go of a grapple while sprinting returns the hands to the stride. They do not hitch. A walking release still returns to the walk. A stand keeps the old leave. The gate stays off. Range and speed are unchanged.
117. Letting a slide die into a walk brings the body up into the stride. The hips do not pop. A slide into a stand still rises into the idle breath. A slide into a sprint is unchanged. Speed is unchanged.
118. Letting a slide die into a sprint brings the body up into the long stride. The hips do not pop. A slide into a walk still rises into the walk. A slide into a stand still rises into the idle breath. Speed is unchanged.
119. A still crouch into a sprint raises the hips into the long stride. They do not pop. A still crouch into a stand still rises into the idle breath. A crouch walk into a sprint is unchanged. Speed is unchanged.
120. An air dash into a walk ends in the stride. It does not come to a stop. An air dash into a sprint is unchanged. Duration and cooldown are unchanged.
121. An air dash into a sprint ends in the long stride. It does not come to a stop. An air dash into a walk still ends in the walk. Duration and cooldown are unchanged.
122. A jump into a crouch walk absorbs into the low stride. The hips stay down. A still crouch keeps the old absorb. Land time is unchanged.
123. A soft landing into a still crouch absorbs into the guard. The hips stay down. A hard landing keeps the old absorb. A crouch walk still absorbs into the low stride. Land time is unchanged.
124. A hard landing into a still crouch absorbs deeper into the guard. The hips stay down. A soft landing still uses the lighter guard. A crouch walk still absorbs into the low stride. Land time is unchanged.
125. A jump into a still crouch settles into the guard in the air. The push still reads. The fall dart is unchanged. An air dash is unchanged. Jump height is unchanged.
126. An air dash into a still crouch ends in the guard. A crouch walk keeps the stride leave. Duration and cooldown are unchanged.
127. An air dash into a crouch walk ends in the low stride. A still crouch still ends in the guard. An upright walk still ends in the stride. Duration and cooldown are unchanged.
128. A still crouch into a ski eases the guard into the glide. It does not pop. A walk into a ski is unchanged. A sprint into a ski is unchanged. Speed is unchanged. Jet stays off.
129. Letting go of a ski into a still crouch eases the glide into the guard. It does not pop. A still crouch into a ski is unchanged. A walk out of a ski is unchanged. Speed is unchanged. Jet stays off.
130. Letting go of a ski into a crouch walk eases the glide into the low stride. A still crouch still ends in the guard. A walk out of a ski is unchanged. Speed is unchanged. Jet stays off.
131. A crouch walk into a ski eases the low stride into the glide. It does not pop. A still crouch into a ski is unchanged. A walk into a ski is unchanged. Speed is unchanged. Jet stays off.
132. A slide into a still crouch eases the wedge into the guard. It does not snap. A slide into a stand still rises into the idle breath. A slide into a walk is unchanged. Speed is unchanged.
133. A still crouch into a slide eases the guard into the wedge. It does not snap. A slide into a still crouch still eases into the guard. A slide into a walk is unchanged. Speed is unchanged.
134. A slide into a crouch walk eases the wedge into the low stride. A still crouch still ends in the guard. A slide into a walk is unchanged. Speed is unchanged.
135. A crouch walk into a slide eases the low stride into the wedge. It does not snap. A still crouch into a slide is unchanged. A slide into a crouch walk is unchanged. Speed is unchanged.
136. A still crouch into a climb eases the guard onto the wall. A wall run from that crouch does the same. A normal climb is unchanged. The entry time is unchanged.
137. A climb into a still crouch eases into the guard. A wall leave is unchanged. A climb into a walk is unchanged. The leave time is unchanged.
138. A wall run into a still crouch eases into the guard. A climb into a still crouch is unchanged. A wall run into a walk is unchanged. The leave time is unchanged.
139. A climb into a crouch walk eases into the low stride. A still crouch still ends in the guard. A wall run into a crouch walk is unchanged. The leave time is unchanged.
140. A wall run into a crouch walk eases into the low stride. A climb into a crouch walk is unchanged. A still crouch still ends in the guard. The leave time is unchanged.
141. A punch that misses in a still crouch eases into the guard. A standing miss still eases into the idle hang. A walk miss and a sprint miss are unchanged. Windup time is unchanged.
142. A tag in a still crouch eases the V into the guard. A standing tag still eases into the idle breath. A walk tag and a sprint tag are unchanged. Flinch time is unchanged.
143. Becoming It in a still crouch eases the claim into the guard. A standing claim still eases into the idle breath. A walk claim and a sprint claim are unchanged. Claim time is unchanged.
144. Letting go of a grapple in a still crouch eases the line into the guard. A standing release is unchanged. A walk release and a sprint release are unchanged. The pull is unchanged. The gate stays off.
145. A dash coming off cooldown in a still crouch pulses inside the guard. A standing ready still pulses into the idle breath. A moving ready is unchanged. Duration and cooldown are unchanged.
146. A punch that misses in a crouch walk eases into the guard and the low stride. A still crouch still ends in the guard. A walk miss and a sprint miss are unchanged. Windup time is unchanged.
147. A tag in a crouch walk eases the V into the guard and the low stride. A still crouch still ends in the guard. A walk tag and a sprint tag are unchanged. Flinch time is unchanged.
148. Becoming It in a crouch walk eases the claim into the guard and the low stride. A still crouch still ends in the guard. A walk claim and a sprint claim are unchanged. Claim time is unchanged.
149. Letting go of a grapple in a crouch walk eases the line into the guard and the low stride. A still crouch still ends in the guard. A walk release and a sprint release are unchanged. The pull is unchanged. The gate stays off.
150. A dash coming off cooldown in a crouch walk pulses inside the guard and the low stride. A still crouch still pulses inside the guard. A standing ready still pulses into the idle breath. A moving ready is unchanged. Duration and cooldown are unchanged.
151. A soft landing into a crouch walk absorbs into the low stride. A hard landing into a crouch walk is unchanged. A soft landing into a still crouch is unchanged. Land time is unchanged.
152. A hard landing into a crouch walk absorbs deeper into the low stride. A soft landing into a crouch walk stays lighter. A hard landing into a still crouch is unchanged. Land time is unchanged.
153. A ski into a slide eases the glide into the wedge. A walk into a ski is unchanged. A slide into a stand is unchanged. Ski speed is unchanged. Jet stays off.
154. A slide into a ski eases the wedge into the glide. A ski into a slide is unchanged. A walk into a ski is unchanged. Ski speed is unchanged. Jet stays off.
155. An air crouch into a crouch walk eases into the low stride. A still air crouch keeps the dart. An air dash into a crouch walk is unchanged. Fall speed is unchanged. Jump height is unchanged.
156. An air crouch into a still crouch lands the dart into the guard. A moving air crouch keeps the flare. A still crouch without the dart is unchanged. Fall speed is unchanged. Land time is unchanged.
157. An air crouch into a soft land opens the dart into the absorb. A hard landing keeps the flare. A still crouch keeps the guard. A moving air crouch keeps the flare. Land time is unchanged.
158. A still crouch into a jump eases the guard into the push. A standing jump is unchanged. A crouch walk into a jump is unchanged. Jump height is unchanged.
159. A crouch walk into a jump eases the low stride into the push. A still crouch into a jump is unchanged. A standing jump is unchanged. Jump height is unchanged.
160. A ski into a jump eases the glide into the push. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A standing jump is unchanged. Jump height is unchanged. Ski speed is unchanged.
161. A slide into a jump eases the wedge into the push. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A standing jump is unchanged. Jump height is unchanged. slideBoost stays 0.
162. A jump into a ski eases the glide or the landing into the stride. A walk into a ski is unchanged. A sprint into a ski is unchanged. A crouch into a ski is unchanged. A slide into a ski is unchanged. Ski speed is unchanged. Jump height is unchanged.
163. A jump into a slide eases the glide or the landing into the wedge. A crouch into a slide is unchanged. A ski into a slide is unchanged. A jump into a ski is unchanged. Jump height is unchanged. slideBoost stays 0.
164. An air dash into a jump eases the burst into the push. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. Jump height is unchanged. Duration and cooldown are unchanged.
165. A climb into a jump eases the climb into the push. An air dash into a jump is unchanged. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. Jump height is unchanged. Exit time is unchanged.
166. A wall run into a jump eases the wall exit into the push. A climb into a jump is unchanged. An air dash into a jump is unchanged. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. Jump height is unchanged. Exit time is unchanged.
167. A jump into a climb eases the contact into the grab. A crouch onto the wall is unchanged. A wall run is unchanged. A climb into a jump is unchanged. Jump height is unchanged. The meet time is unchanged.
168. A jump into a wall run eases the contact into the attach. A jump into a climb is unchanged. A crouch onto the wall is unchanged. A wall run into a jump is unchanged. Jump height is unchanged. The meet time is unchanged.
169. An air crouch into a jump eases the dart into the push. A moving fall uses the low stride. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. An air dash into a jump is unchanged. A climb into a jump is unchanged. A wall run into a jump is unchanged. Jump height is unchanged. Fall speed is unchanged.
170. A jump into an air crouch eases the apex and the descent into the dart. An air crouch into a jump is unchanged. A moving fall still ends in the low stride. A still crouch into a jump is unchanged. Jump height is unchanged. Fall speed is unchanged.
171. A jump into an air dash eases the apex into the burst. The burst still holds. An air dash into a jump is unchanged. A jump into an air crouch is unchanged. Jump height is unchanged. Duration and cooldown are unchanged.
172. A soft landing into a jump eases the absorb into the push. A hard landing keeps its jump. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. An air dash into a jump is unchanged. A climb into a jump is unchanged. A wall run into a jump is unchanged. Jump height is unchanged. The landing is unchanged when you stay down.
173. A hard landing into a jump eases the absorb into the push. A soft landing into a jump is unchanged. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A ski into a jump is unchanged. A slide into a jump is unchanged. An air dash into a jump is unchanged. A climb into a jump is unchanged. A wall run into a jump is unchanged. Jump height is unchanged. The landing is unchanged when you stay down.
174. A punch miss into a jump eases the whiff into the push. A crouch miss is unchanged. A soft landing into a jump is unchanged. A hard landing into a jump is unchanged. Jump height is unchanged.
175. A tag into a jump eases the connect into the push. A crouch tag is unchanged. A punch miss into a jump is unchanged. Jump height is unchanged.
176. Becoming It into a jump eases the claim into the push. A crouch claim is unchanged. A tag into a jump is unchanged. Jump height is unchanged.
177. A grapple release into a jump eases the line into the push. A crouch release is unchanged. Becoming It into a jump is unchanged. Jump height is unchanged. The gate stays off.
178. A dash coming off cooldown into a jump eases the pulse into the push. A crouch ready is unchanged. A grapple release into a jump is unchanged. Jump height is unchanged. Duration and cooldown are unchanged.
179. A jump into a punch eases the apex or the landing into the windup. A punch from the ground is unchanged. Jump height is unchanged. Windup time is unchanged.
180. A jump into a tag eases the apex or the landing into the connect. A crouch tag is unchanged. A jump into a punch is unchanged. A tag into a jump is unchanged. Jump height is unchanged.
181. An air crouch into an air dash eases the dart into the burst. A moving fall uses the low stride. The burst still holds. Fall speed is unchanged. Duration and cooldown are unchanged. A jump into a tag is unchanged.
182. An air dash into an air crouch eases the burst into the dart. An air crouch into an air dash is unchanged. The burst still holds. Fall speed is unchanged. Duration and cooldown are unchanged.
183. A ski into an air dash eases the glide into the burst. The burst still holds. An air crouch into an air dash is unchanged. An air dash into an air crouch is unchanged. Ski speed is unchanged. Duration and cooldown are unchanged. Jet stays off.
184. A slide into an air dash eases the wedge into the burst. The burst still holds. A ski into an air dash is unchanged. slideBoost stays 0. Duration and cooldown are unchanged. Jet stays off.
185. An air dash into a ski eases the burst into the glide. A slide into an air dash is unchanged. A ski into an air dash is unchanged. Ski speed is unchanged. Duration and cooldown are unchanged. Jet stays off.
186. An air dash into a slide eases the burst into the wedge. An air dash into a ski is unchanged. A ski into an air dash is unchanged. A slide into an air dash is unchanged. The slide does not speed up. Duration and cooldown are unchanged. Jet stays off.
187. A climb into an air dash eases the climb into the burst. The burst still holds. An air dash into a slide is unchanged. A ski into an air dash is unchanged. A slide into an air dash is unchanged. Exit time is unchanged. Duration and cooldown are unchanged. Jet stays off.
188. A wall run into an air dash eases the wall exit into the burst. The burst still holds. A climb into an air dash is unchanged. Exit time is unchanged. Duration and cooldown are unchanged. Jet stays off.
189. An air dash into a climb eases the burst into the grab. A wall run into an air dash is unchanged. A climb into an air dash is unchanged. Exit time is unchanged. Duration and cooldown are unchanged. Jet stays off.
190. An air dash into a wall run eases the burst into the attach. An air dash into a climb is unchanged. A climb into an air dash is unchanged. A wall exit into an air dash is unchanged. Exit time is unchanged. Duration and cooldown are unchanged. Jet stays off.
191. A punch miss into an air dash eases the whiff into the burst. The burst still holds. An air dash into a wall run is unchanged. An air dash into a climb is unchanged. A climb into an air dash is unchanged. A wall exit into an air dash is unchanged. A punch miss into a jump is unchanged. A crouch miss is unchanged. Duration and cooldown are unchanged. Jet stays off.
192. A tag into an air dash eases the connect into the burst. The burst still holds. A punch miss into an air dash is unchanged. A tag into a jump is unchanged. A crouch tag is unchanged. Duration and cooldown are unchanged. Jet stays off.
193. Becoming It into an air dash eases the claim into the burst. The burst still holds. A tag into an air dash is unchanged. Becoming It into a jump is unchanged. A crouch claim is unchanged. Duration and cooldown are unchanged. Jet stays off.
194. A grapple release into an air dash eases the line into the burst. The burst still holds. Becoming It into an air dash is unchanged. A grapple release into a jump is unchanged. A crouch release is unchanged. The gate stays off. Duration and cooldown are unchanged. Jet stays off.
195. An air dash into a punch eases the burst into the windup. A grapple release into an air dash is unchanged. A jump into a punch is unchanged. Windup time is unchanged. Duration and cooldown are unchanged. Jet stays off.
196. An air dash into a tag eases the burst into the connect. An air dash into a punch is unchanged. A jump into a tag is unchanged. A crouch tag is unchanged. Duration and cooldown are unchanged. Jet stays off.
197. A soft landing into an air dash eases the absorb into the burst. The burst still holds. An air dash into a tag is unchanged. A soft landing into a jump is unchanged. A hard landing is unchanged. Land time is unchanged. Duration and cooldown are unchanged. Jet stays off.
198. A hard landing into an air dash eases the deeper absorb into the burst. The burst still holds. A soft landing into an air dash is unchanged. A hard landing into a jump is unchanged. Land time is unchanged. Duration and cooldown are unchanged. Jet stays off.
199. A soft landing into a punch eases the absorb into the windup. A hard landing into an air dash is unchanged. A jump into a punch is unchanged. A soft landing into a jump is unchanged. Windup time is unchanged. Land time is unchanged.
200. A hard landing into a punch eases the deeper absorb into the windup. A soft landing into a punch is unchanged. A jump into a punch is unchanged. A hard landing into a jump is unchanged. Windup time is unchanged. Land time is unchanged.
201. A soft landing into a tag eases the absorb into the connect. A hard landing into a punch is unchanged. A jump into a tag is unchanged. A soft landing into a jump is unchanged. A crouch tag is unchanged. Connect time is unchanged. Land time is unchanged.
202. A hard landing into a tag eases the deeper absorb into the connect. A soft landing into a tag is unchanged. A jump into a tag is unchanged. A hard landing into a jump is unchanged. A crouch tag is unchanged. Connect time is unchanged. Land time is unchanged.
203. A ski into a punch eases the glide into the windup. A hard landing into a tag is unchanged. A ski into a jump is unchanged. A ski into an air dash is unchanged. A jump into a punch is unchanged. Windup time is unchanged. Ski speed is unchanged. Jet stays off.
204. A slide into a punch eases the wedge into the windup. A ski into a punch is unchanged. A slide into a jump is unchanged. A slide into an air dash is unchanged. slideBoost stays 0. Windup time is unchanged. Jet stays off.
205. A ski into a tag eases the glide into the connect. A slide into a punch is unchanged. A ski into a punch is unchanged. A ski into a jump is unchanged. A jump into a tag is unchanged. A crouch tag is unchanged. Connect time is unchanged. Ski speed is unchanged. Jet stays off.
206. A slide into a tag eases the wedge into the connect. A ski into a tag is unchanged. A slide into a punch is unchanged. A slide into a jump is unchanged. A crouch tag is unchanged. slideBoost stays 0. Connect time is unchanged. Jet stays off.
207. A climb into a punch eases the grab into the windup. A slide into a tag is unchanged. A climb into an air dash is unchanged. A climb into a jump is unchanged. A wall run is unchanged. Windup time is unchanged. Exit time is unchanged. Jet stays off.
208. A climb into a tag eases the grab into the connect. A climb into a punch is unchanged. A climb into an air dash is unchanged. A climb into a jump is unchanged. A wall run is unchanged. Connect time is unchanged. Exit time is unchanged. Jet stays off.
209. A wall exit into a punch eases the leave into the windup. A climb into a tag is unchanged. A wall exit into an air dash is unchanged. A wall exit into a jump is unchanged. A climb into a punch is unchanged. Windup time is unchanged. Exit time is unchanged. Jet stays off.
210. A wall exit into a tag eases the leave into the connect. A wall exit into a punch is unchanged. A wall exit into an air dash is unchanged. A wall exit into a jump is unchanged. A climb into a tag is unchanged. Connect time is unchanged. Exit time is unchanged. Jet stays off.
211. An air crouch into a punch eases the dart into the windup. A wall exit into a tag is unchanged. An air crouch into an air dash is unchanged. An air crouch into a jump is unchanged. A jump into a punch is unchanged. Windup time is unchanged. Fall speed is unchanged. Jet stays off.
212. An air crouch into a tag eases the dart into the connect. An air crouch into a punch is unchanged. An air crouch into an air dash is unchanged. An air crouch into a jump is unchanged. A jump into a tag is unchanged. A crouch tag is unchanged. Connect time is unchanged. Fall speed is unchanged. Jet stays off.
213. Becoming It into a punch eases the claim into the windup. An air crouch into a tag is unchanged. Becoming It into an air dash is unchanged. Becoming It into a jump is unchanged. A crouch claim is unchanged. Windup time is unchanged. Claim time is unchanged. Jet stays off.
214. Becoming It into a tag eases the claim into the connect. Becoming It into a punch is unchanged. Becoming It into an air dash is unchanged. Becoming It into a jump is unchanged. An air crouch into a tag is unchanged. A crouch claim is unchanged. Connect time is unchanged. Claim time is unchanged. Jet stays off.
215. A grapple release into a punch eases the line into the windup. Becoming It into a tag is unchanged. A grapple release into an air dash is unchanged. A grapple release into a jump is unchanged. A crouch release is unchanged. Windup time is unchanged. The gate stays off. Jet stays off.
216. A grapple release into a tag eases the line into the connect. A grapple release into a punch is unchanged. A grapple release into an air dash is unchanged. A grapple release into a jump is unchanged. Becoming It into a tag is unchanged. A crouch release is unchanged. Connect time is unchanged. The gate stays off. Jet stays off.
217. A dash coming off cooldown into a punch eases the pulse into the windup. A grapple release into a tag is unchanged. A dash coming off cooldown into a jump is unchanged. A crouch ready is unchanged. Windup time is unchanged. Duration and cooldown are unchanged. Jet stays off.
218. A dash coming off cooldown into a tag eases the pulse into the connect. A dash coming off cooldown into a punch is unchanged. A dash coming off cooldown into a jump is unchanged. A crouch ready is unchanged. A grapple release into a tag is unchanged. Connect time is unchanged. Duration and cooldown are unchanged. Jet stays off.
219. A punch into a tag eases the cock or the strike into the connect. A dash coming off cooldown into a tag is unchanged. A dash coming off cooldown into a punch is unchanged. A crouch tag is unchanged. Connect time is unchanged. Windup time is unchanged. Jet stays off.
220. A tag into a punch eases the connect into the windup. A punch into a tag is unchanged. A dash coming off cooldown into a punch is unchanged. A crouch tag is unchanged. A tag into a jump is unchanged. Windup time is unchanged. Connect time is unchanged. Jet stays off.
221. A punch miss into a tag eases the whiff into the connect. A tag into a punch is unchanged. A punch into a tag is unchanged. A punch miss into a jump is unchanged. A punch miss into an air dash is unchanged. A crouch miss is unchanged. Connect time is unchanged. Jet stays off.
222. A punch into a ski eases the cock or the strike into the glide. A ski into a punch is unchanged. A punch miss into a tag is unchanged. A slide into a ski is unchanged. A jump into a ski is unchanged. Ski speed is unchanged. Windup time is unchanged. Jet stays off.
223. A punch into a slide eases the cock or the strike into the wedge. A punch into a ski is unchanged. A slide into a punch is unchanged. A crouch into a slide is unchanged. A ski into a slide is unchanged. A jump into a slide is unchanged. slideBoost stays 0. Windup time is unchanged. Jet stays off.
224. A tag into a ski eases the connect into the glide. A punch into a slide is unchanged. A punch into a ski is unchanged. A tag into a jump is unchanged. A slide into a ski is unchanged. Ski speed is unchanged. Connect time is unchanged. Jet stays off.
225. A tag into a slide eases the connect into the wedge. A tag into a ski is unchanged. A punch into a slide is unchanged. A slide into a tag is unchanged. A crouch into a slide is unchanged. A ski into a slide is unchanged. A jump into a slide is unchanged. slideBoost stays 0. Connect time is unchanged. Jet stays off.
226. A still crouch into a punch eases the guard into the windup. A tag into a slide is unchanged. A crouch walk into a punch is unchanged. An air crouch into a punch is unchanged. A slide into a punch is unchanged. A crouch into a slide is unchanged. A crouch claim is unchanged. Windup time is unchanged. Jet stays off.
227. A still crouch into a tag eases the guard into the connect. A still crouch into a punch is unchanged. A crouch walk into a tag is unchanged. An air crouch into a tag is unchanged. A slide into a tag is unchanged. A crouch claim is unchanged. A punch into a tag is unchanged. Connect time is unchanged. Jet stays off.
228. A soft landing into a ski eases the absorb into the glide. A still crouch into a tag is unchanged. A hard landing into a ski is unchanged. A jump into a ski is unchanged. A punch into a ski is unchanged. A slide into a ski is unchanged. Ski speed is unchanged. Land time is unchanged. Jet stays off.
229. A soft landing into a slide eases the absorb into the wedge. A soft landing into a ski is unchanged. A hard landing into a slide is unchanged. A jump into a slide is unchanged. A punch into a slide is unchanged. A crouch into a slide is unchanged. A ski into a slide is unchanged. slideBoost stays 0. Land time is unchanged. Jet stays off.
230. A climb into a ski eases the grab into the glide. A soft landing into a slide is unchanged. A wall run into a ski is unchanged. A climb into a jump is unchanged. A climb into a punch is unchanged. A jump into a ski is unchanged. Ski speed is unchanged. Exit time is unchanged. Jet stays off.
231. A climb into a slide eases the grab into the wedge. A climb into a ski is unchanged. A wall run into a slide is unchanged. A climb into a jump is unchanged. A soft landing into a slide is unchanged. A jump into a slide is unchanged. slideBoost stays 0. Exit time is unchanged. Jet stays off.
232. A wall run into a ski eases the leave into the glide. A climb into a slide is unchanged. A climb into a ski is unchanged. A wall run into a jump is unchanged. A soft landing into a ski is unchanged. Ski speed is unchanged. Exit time is unchanged. Jet stays off.
233. A wall run into a slide eases the leave into the wedge. A wall run into a ski is unchanged. A climb into a slide is unchanged. A wall run into a jump is unchanged. A soft landing into a slide is unchanged. A jump into a slide is unchanged. slideBoost stays 0. Exit time is unchanged. Jet stays off.
234. An air dash into a ski eases the burst into the glide. A wall run into a slide is unchanged. A wall run into a ski is unchanged. A climb into a ski is unchanged. An air dash into a slide is unchanged. Ski speed is unchanged. Duration and cooldown are unchanged. Jet stays off.
235. A hard landing into a ski eases the absorb into the glide. An air dash into a ski is unchanged. A soft landing into a ski is unchanged. A jump into a ski is unchanged. A hard landing into a slide is unchanged. Ski speed is unchanged. Land time is unchanged. Jet stays off.
236. An air crouch into a ski eases the dart into the glide. A hard landing into a ski is unchanged. An air dash into a ski is unchanged. A jump into a ski is unchanged. An air crouch into a slide is unchanged. Fall speed is unchanged. Ski speed is unchanged. Jet stays off.
237. A grapple release into a ski eases the line into the glide. An air crouch into a ski is unchanged. A grapple release into a jump is unchanged. A grapple release into a punch is unchanged. An air dash into a ski is unchanged. The gate stays off. Ski speed is unchanged. Jet stays off.
238. A grapple release into a slide eases the line into the wedge. A grapple release into a ski is unchanged. A grapple release into a jump is unchanged. A wall run into a slide is unchanged. A soft landing into a slide is unchanged. slideBoost stays 0. The gate stays off. Jet stays off.
239. Becoming It into a ski eases the claim into the glide. A grapple release into a slide is unchanged. Becoming It into a punch is unchanged. Becoming It into a jump is unchanged. A grapple release into a ski is unchanged. Claim time is unchanged. Ski speed is unchanged. Jet stays off.
240. Becoming It into a slide eases the claim into the wedge. Becoming It into a ski is unchanged. Becoming It into a punch is unchanged. Becoming It into a jump is unchanged. A grapple release into a slide is unchanged. slideBoost stays 0. Claim time is unchanged. Jet stays off.
241. A dash coming off cooldown into a ski eases the pulse into the glide. Becoming It into a slide is unchanged. A dash coming off cooldown into a punch is unchanged. A dash coming off cooldown into a tag is unchanged. A dash coming off cooldown into a jump is unchanged. Duration and cooldown are unchanged. Ski speed is unchanged. Jet stays off.
242. A dash coming off cooldown into a slide eases the pulse into the wedge. A dash coming off cooldown into a ski is unchanged. A dash coming off cooldown into a punch is unchanged. A dash coming off cooldown into a tag is unchanged. A dash coming off cooldown into a jump is unchanged. slideBoost stays 0. Duration and cooldown are unchanged. Jet stays off.
243. A punch miss into a ski eases the whiff into the glide. A dash coming off cooldown into a slide is unchanged. A punch into a ski is unchanged. A punch miss into a tag is unchanged. A punch miss into a jump is unchanged. A punch miss into an air dash is unchanged. Whiff time is unchanged. Ski speed is unchanged. Jet stays off.
244. A punch miss into a slide eases the whiff into the wedge. A punch miss into a ski is unchanged. A punch into a slide is unchanged. A punch miss into a tag is unchanged. A punch miss into a jump is unchanged. A punch miss into an air dash is unchanged. slideBoost stays 0. Whiff time is unchanged. Jet stays off.
245. A still crouch into a slide eases the guard into the wedge. A punch miss into a slide is unchanged. A still crouch into a punch is unchanged. A still crouch into a tag is unchanged. A still crouch into a ski is unchanged. A crouch walk into a slide is unchanged. slideBoost stays 0. Jet stays off.
246. A ski into a slide eases the glide into the wedge. A still crouch into a slide is unchanged. A crouch walk into a ski is unchanged. A crouch walk into a slide is unchanged. A slide into a ski is unchanged. slideBoost stays 0. Ski speed is unchanged. Jet stays off.
247. A slide into an air dash eases the wedge into the burst. A ski into a slide is unchanged. A ski into an air dash is unchanged. An air dash into a slide is unchanged. slideBoost stays 0. Duration and cooldown are unchanged. Jet stays off.
248. A slide into a jump eases the wedge into the jump. A slide into an air dash is unchanged. A ski into a jump is unchanged. A still crouch into a jump is unchanged. A crouch walk into a jump is unchanged. A standing jump is unchanged. slideBoost stays 0. Jump height is unchanged. Jet stays off.
249. A jump into an air dash eases the jump into the burst. The burst still holds. A slide into a jump is unchanged. A ski into a jump is unchanged. A slide into an air dash is unchanged. Jump height is unchanged. Duration and cooldown are unchanged. Jet stays off.
250. A still crouch into an air dash eases the guard into the burst. The burst still holds. A jump into an air dash is unchanged. A slide into an air dash is unchanged. An air crouch into an air dash is unchanged. Duration and cooldown are unchanged. Jet stays off.
251. A punch into an air dash eases the punch into the burst. The burst still holds. A still crouch into an air dash is unchanged. A punch miss into an air dash is unchanged. A tag into an air dash is unchanged. Windup time is unchanged. Duration and cooldown are unchanged. Jet stays off.
252. A crouch walk into an air dash eases the low stride into the burst. The burst still holds. A hard landing into a jump is unchanged. A soft landing into a jump is unchanged. A still crouch into an air dash is unchanged. Duration and cooldown are unchanged. Jet stays off.
253. A dash coming off cooldown into an air dash eases the pulse into the burst. The burst still holds. A crouch walk into an air dash is unchanged. A dash coming off cooldown into a jump is unchanged. A dash coming off cooldown into a punch is unchanged. Duration and cooldown are unchanged. Jet stays off.
254. A punch into a jump eases the punch into the jump. Becoming It into a jump is unchanged. A tag into a jump is unchanged. A punch miss into a jump is unchanged. Windup time is unchanged. Jump height is unchanged. Jet stays off.
255. A run into a ski eases the stride into the glide. The glide then holds. A walk into a ski is unchanged. A slide into a ski is unchanged. A jump into a ski is unchanged. Ski speed is unchanged. Jet stays off.
256. A run into a slide eases the stride into the wedge. The wedge then holds. A run into a ski is unchanged. A ski into a slide is unchanged. A still crouch into a slide is unchanged. slideBoost stays 0. Jet stays off.
257. A jump into a still crouch eases the jump into the guard. The guard then holds. A run into a slide is unchanged. A jump into a ski is unchanged. A jump into a slide is unchanged. Jump height is unchanged. Jet stays off.
258. An air dash into a still crouch eases the burst into the guard. The guard then holds. A jump into a still crouch is unchanged. A still crouch into an air dash is unchanged. Duration and cooldown are unchanged. Jet stays off.
259. A walk into a slide eases the walk into the wedge. The wedge then holds. An air dash into a still crouch is unchanged. A run into a slide is unchanged. A crouch walk into a slide is unchanged. slideBoost stays 0. Jet stays off.
260. A walk into a ski eases the walk into the glide. The glide then holds. A walk into a slide is unchanged. A run into a ski is unchanged. A crouch walk into a ski is unchanged. Ski speed is unchanged. Jet stays off.
261. A crouch walk into a jump eases the low stride into the jump. The jump then holds. A walk into a ski is unchanged. A still crouch into a jump is unchanged. A crouch walk into an air dash is unchanged. Jump height is unchanged. Jet stays off.
262. A walk into a jump eases the walk into the jump. The jump then holds. A crouch walk into a jump is unchanged. A walk into a ski is unchanged. A still crouch into a jump is unchanged. Jump height is unchanged. Jet stays off.
263. A walk into an air dash eases the walk into the burst. The burst then holds. A walk into a jump is unchanged. A crouch walk into an air dash is unchanged. A still crouch into an air dash is unchanged. Duration and cooldown are unchanged. Jet stays off.
264. A still crouch into a jump eases the guard into the jump. The jump then holds. A walk into an air dash is unchanged. A crouch walk into a jump is unchanged. A jump into a still crouch is unchanged. Jump height is unchanged. Jet stays off.
265. A walk into a punch eases the walk into the cock. The windup then holds. The strike is unchanged. A still crouch into a jump is unchanged. A jump into a punch is unchanged. A still crouch into a punch is unchanged. Windup time is unchanged. Jet stays off.
266. A walk into a tag eases the walk into the connect. The connect then holds. A walk into a punch is unchanged. A jump into a tag is unchanged. A still crouch into a tag is unchanged. Connect time is unchanged. Jet stays off.
267. A run into a punch eases the stride into the cock. The windup then holds. The strike is unchanged. A walk into a tag is unchanged. A walk into a punch is unchanged. A jump into a punch is unchanged. Windup time is unchanged. Jet stays off.

## Grapple (experimental, off)
- Not part of the default tag loop. The spawned pawn does not get `ExperimentalGrapple` unless you add it. `enableGrapple` stays false, so RMB does not hook and does not jet.
- If you add the component and turn it on, hold RMB (JetHeld) for a rope pull. Release drops it. Pause or the results card drops it too. Slide, jump, dash, and jet numbers stay the same. Audio stays on AudioMaster.
