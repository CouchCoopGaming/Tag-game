# Tag shelf note, Oct 10 2026

Branch `cursor/tag-playtest-oct9`. Code tip at shelving: `18cc63f9` (this note sits on top of it).
Unity 6000.3.24f1. To resume: fetch the branch, `checkout --detach FETCH_HEAD`, let Unity reload.

## What works (real Unity station checks, job38, all 4 pass)
- Menu -> Practice reaches Playing; the pawn moves (24 m in 2 s).
- Two pads join -> split screen with two panes (top/bottom), one audio listener.
- One TagModeController; RoundPlay on; pad and keyboard both move with the cursor unlocked.
- Jump raises the pawn (Space / South); pad and mouse look turn yaw and pitch; grapple can be aimed (minPitch -65).
- Tagging is punch-only. Passive touch for 3 s never tags; the It bot turns to the runner and punch-tags within 2 s.
- Slide never raises speed above entry (entry 13.80, max 13.80); no frame snaps the pawn.
- Arms stay sane at sprint (overlay accumulation fixed); run has bent driven elbows (~90 deg) and a speed lean (15 deg spine + 9 deg pelvis).
- Compile checker clean (also fails on duplicate GUIDs, multiple asmdefs per folder, missing .meta). Proof sim passes; no-clip fails 0 (selfMax 0.08 cm). Feel locks untouched (coyote 0.10, buffer 0.16, jumpSpeed 24.7, terminal 56.16, roll 65%/0.52/0.32, RollSpeed 36.504).

## Known open issues
- Animation passes not done: jump takeoff/air/land (hip hinge), slide, vault/hurdle smoothness, punch windup/extension, idle stance, evasion moves. Sprint lean is ~24 deg chest-forward, short of the 41 deg HIP_TARGETS reference; knee drive unchanged.
- Knee guard (`Assets/Scripts/Art/KneeGuard.cs` -> `DummyLocomotor.GuardKnees`) is a safety net. The Tan rig's knee axis is right; the raw pose still goes ~7-8 deg backward at jump takeoff (earlier runs saw -55/-69 in unlogged phases). Re-key the takeoff/air legs so the guard does nothing.
- Four colour Hier mannequins (Blue, Mint, Red, Lavender `*_Hier_Hi.fbx`) have a flat hierarchy (Hips/Spine/Head siblings). The binder falls back to the rigged Tan body tinted in the look's colours (`DummyAvatarBinder.ChainedBody/PosableLook`). Re-export those four with a real chain.
- P2 HUD haze in split screen: not looked at.
- Practice freeze Landon saw: not reproduced in the station (no exception in his log). Get a timestamp/log if it returns.
- Bot facing fix is tested with a placed bot; watch real chase play for bots strafing past runners.
- Station slide check now enters a slide by steering toward (42, 92) from spawn; it depends on that lane staying clear.
- "Texture is not readable" warning never found in a log.
- Hip-floor leftovers: juke plant, punch/pad overlaps left as-is by decision; slide clip hip left for Landon.
- Rig (#128) and costumes (#131) are not merged into this branch.

## Next steps
1. Jump family (takeoff knee re-key, air, land hip hinge to soft land 48 deg / knee 105), then slide, vault, punch, idle, evasion; real Unity side frames per family into /workspace/anim-pass/<family>/.
2. Sprint lean toward 41 deg and knee drive, re-check no-clip and floors.
3. P2 HUD haze; camera feel pass; watch bots in real chases.
4. Re-export the four colour mannequins and drop the Tan fallback.
