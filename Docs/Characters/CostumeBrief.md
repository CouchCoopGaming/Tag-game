# Costume brief

Prep for player costumes on the couch tag mannequin. The shipped Hier rig, its skeleton, its bind, and the game scenes stay as they are. Another lane owns the hip and thigh rest overlap. Nothing here binds into the game.

Each seat is a quarter of a 1080p screen. A runner in that view is often 20–40 px tall. At that size the read is the outline plus one colour block, not a texture.

## Look

- The silhouette has to stay recognizable at 20–40 px. A one-centimetre fold is a fraction of a pixel.
- Who-is-who is a solid player-colour block on the torso and on the head. P1 Reed is red, P2 Bram is blue, P3 Pip is orange, P4 Sol is lavender.
- Clothes are athletic street and parkour: hoodies, joggers, high-tops, caps, a small pack. The pack and the hood stay tight to the body so they do not hide a limb or snag a wall.
- No capes, scarves, or loose cloth. Climbs, wall runs, slides, and the landing roll would clip them.
- Every costume keeps the same capsule collision. These meshes do not change a hitbox.

The four starters share the current Hier skeleton and the same height. Lanky, stocky, small, and mid are costume reads (hood, helmet, cropped limbs, collar), not four bodies.

## Reed — Leaves before the count.

Long sleeves, long joggers, high-tops. The head shape is the variant.

1. Hood. Hoodie chest, back seam, pocket, upper and lower sleeves, upper and lower joggers, shoes, hood, hood roll.
2. Cap. The same body, plus a cap, a brim, and a small pack.
3. Helmet. The same body, plus a helmet and a short visor.

## Bram — Stays in the doorway.

Bram has his own jacket and beanie meshes. They are not Reed's shells with a blue `PlayerColor`. The beanie is a tight crown with no brim. The jacket uses its own chest, back, waist, sleeves, cuffs, joggers, and shoes, and the waistband is shorter than Reed's.

1. Beanie, jacket, long sleeves, joggers, shoes, and the joint pads.
2. The same jacket, plus Bram's own pack.
3. The same jacket, plus Bram's own collar.

## Pip — Fits the gap you missed.

Cropped sleeves and cropped joggers, so the forearms and the shins stay the bare mannequin. That is the small, quick read.

1. Cap, brim, and pack on the cropped body.
2. Hair on the cropped body.
3. Helmet and visor on the cropped body.

## Sol — The one you spot first.

Long clothes, with the collar-and-cap variant as the mid read.

1. Collar, cap, and brim.
2. Hood and hood roll.
3. Helmet, visor, and pack.

## Pieces

Each piece is its own mesh, weighted to one existing Hier bone. The kit is a hoodie chest, upper and lower sleeves, a shoulder cap, upper and lower joggers, sneakers, cap, brim, helmet, visor, hood, hood roll, collar, hair, pack, pocket, and a back seam. Elbow, knee, hip, and ankle pads are separate shells on the parent bone of that joint. Hands stay bare. The cuff is a thin band on the hand bone, open on the sides the landing roll lays against the shin and the ankle pad.

Player colour is one RGB node named `PlayerColor` on the lab material. Joggers, shoes, and the pack stay fixed colours so the torso and the head stay the seat colour. The noise bump strength is 0, so the seat colour stays a flat block. There are no image textures, logos, or brands.

Each loadout id is the license name in `Art/CharacterLab/LICENSES.md`. Bram's three ids use his jacket and beanie meshes.

LOD0 is the fitted mesh. LOD1 and LOD2 are collapse copies of those same pieces, parented to the same bones, and left out of the render loadout. Worn triangles, LOD0/LOD1/LOD2:

- Reed_1_Hood 7212/3325/2141. Reed_2_Cap 7226/3593/2315. Reed_3_Helmet 7204/3358/2164.
- Bram_1_Beanie 6512/2954/1904. Bram_2_Jacket 6656/3076/1982. Bram_3_Collar 6640/3060/1973.
- Pip_1_Cap 5434/2680/1726. Pip_2_Hair 5070/2391/1539. Pip_3_Helmet 5412/2445/1575.
- Sol_1_Collar_Cap 7194/3566/2298. Sol_2_Hood 7212/3325/2141. Sol_3_Helmet_Pack 7348/3480/2242.

The costume ceilings are 15000 / 8000 / 4000. Every loadout is under them, and each coarser level has fewer triangles than the one above it.

## Fit

`costume-fit sets=12 frames=5544 worldMax=0.46 fails=0`

Per clip, twelve loadouts, every 30 fps frame:

- idle `worldMax=0.00` frames=3156 fails=0
- walk `worldMax=0.00` frames=1416 fails=0
- run `worldMax=0.00` frames=132 fails=0
- sprint `worldMax=0.35` frames=108 fails=0
- wall `worldMax=0.05` frames=288 fails=0
- slide `worldMax=0.43` frames=240 fails=0
- roll `worldMax=0.46` frames=204 fails=0

`costume-cover` from pass 4 remains `sets=12 buriedPct=1.94`. Pass 5 leaves the hip and spine plates visible, so that percentage is not the pass-6 picture.

At rest, every cloth sample sits outside its own bone. The band check is min 0.34 cm, max 0.98 cm, under 0, over 0, inside the 0.3–1 cm layer. Hoodies and jackets use the thick end, about 9 mm. A sample that would have to leave that band to clear a neighbouring plate is cut, not pushed.

Shell spans, joint insets, fold openings, and the clip vents live in tables in `Art/CharacterLab/costume_lab.py` (`OUTFITS`, `JOINT_FLOOR`, `JOINT_ROWS`, `FOLD_OPEN`, `SLEEVE_VENT`, `SLEEVE_ROLL`, `CALF_VENT`, `SHIN_VENT`, `CALF_HAND`, `CUFF_VENT`, `FOREARM_VENTS`, `THIGH_VENT`, `CHEST_HEM`, `CLEAR_ROWS`, `INSET_STEPS`). They are the numbers a later pass refits when the ball-and-socket hips and ankles are stable. Nothing here is refitted to that rig.

## Stills

Pass 1 keeps the lineup, the variant sheet, and the 30 px readability frame:

- `Docs/Characters/pass1/lineup-front.png`
- `Docs/Characters/pass1/lineup-three-quarter.png`
- `Docs/Characters/pass1/lineup-side.png`
- `Docs/Characters/pass1/readability-30px.png`
- `Docs/Characters/pass1/variants.png`

Pass 4 is the previous quartet, in `Docs/Characters/pass4/`. Pass 5 is the lineup. Pass 6 adds the three clip frames that were still over the line. Each frame is 1280×720 and under 400 KB. The four figures stand 1.75 m apart. The 1.80 m staff stands to the side of the row, level with Reed, so it does not cover Bram.

- `Docs/Characters/pass5/lineup-three-quarter.png` — Reed's red hood, Bram's blue beanie and jacket, Pip's orange crop, Sol's lavender collar and cap. The staff is left of Reed.
- `Docs/Characters/pass5/lineup-side.png` — the same four from the side.
- `Docs/Characters/pass5/joint-close.png` — Reed's right knee, with the knee pad on the thigh bone and the shoe clear of the shin.
- `Docs/Characters/pass5/scale-figure.png` — Reed's hood on the Hier body, feet on the ground, beside a 1.80 m staff and a bench. The hooded figure in frame is 1.871 m. The staff is beside him, not in front of the lineup.

Pass 6 is the worst frame of each clip that pass 5 left over 0.50 cm:

- `Docs/Characters/pass6/sprint.png` — Bram's blue beanie and jacket at sprint t=0.067. The remaining sample is the ankle pad, 0.35 cm.
- `Docs/Characters/pass6/slide.png` — Reed's red hood at slide t=0.267. The remaining sample is the back panel into the hips, 0.43 cm.
- `Docs/Characters/pass6/roll.png` — Reed's red hood at roll t=0.267. The remaining sample is the lower chest into the raised thigh, 0.46 cm.

Pass 3 stills stay in `Docs/Characters/pass3/` as the reviewed set. The pass-4 cover numbers stay in `Docs/Characters/pass4/cover.txt`.

The readability frame is a quarter of 1080p with each figure about 30 px tall. It does not replace the quartet.

## Honest flaws

- One skeleton and one height. The four outlines are headwear and the crop, not different proportions.
- Bram's variants are his own jacket and beanie. The seat colour is still the blue parameter. The silhouette difference is the crown and the shorter waist, not a second body.
- Pip's cropped outfits leave the mid-forearm and the mid-shin bare. The elbow, knee, hip, and ankle pads are still on. Pip's LOD0 is about 5.1–5.4k. The long outfits sit inside 8–15k, about 6.5–7.3k.
- The pads are rigid one-bone shells with a rib and a beveled cuff. They are not a flexing sleeve. Elbows, knees, and ankles read as a short pad. The hip pad sits on the thigh, below the pelvis bury.
- The hip shell is a rear seat plus a narrow belly. A full waistband enters the raised thigh on the wall plant (about 6 cm before it was cut back). The lower-back shell is cut away for the same reason, so the spine plate stays visible. The hip plate stays visible on the front and sides.
- The shoulder cap is weighted to the upper arm, not the shoulder bone, so a raised arm does not sweep a shell that stays behind.
- There is no separate sole. A groove in the same layer as the shoe failed the costume-into-costume check.
- The brim, visor, hair, pack, and hood roll sit outside the 1 cm cloth band so the outline can read. The farthest accessory is Bram's pack, 11.60 cm off the chest.
- The chest colour is a shell over the Hier plate with the flanks cut back so the arms can pass. It is not a tailored hoodie.
- Weights are one bone per piece. There is no cloth simulation.
- The mannequin's hip and thigh rest overlap is untouched.
- These clothes are fitted to the pre-ball Hier. The clearance candidate, with ball-and-socket hips and ankles, is not in this branch. Nothing here is refitted to that rig until the player lane says it is stable. The check on this tip still reports `rig-not-clearance` and `rig-proof-missing` for that reason. The shell tables are what a later pass refits.
- LOD1 and LOD2 are in the blend and on the `lod` lines of `Docs/Characters/pass1/fit.txt`. The models check reads only the single `worn` triangle count, so it still prints `lod1-missing` and `lod2-missing`.
- The scale still's hooded figure measures 1.871 m from the lifted sole to the top of the hood. The staff next to it is 1.80 m. The shipped Hier meshes, at armature scale (1, 1, 1), already span 1.8588 m: sole z is -0.0440, crown z is 1.8148, and the crown is the top of `Mesh_Head` (head z 1.5152..1.8148, left foot z -0.0440..0.0463). The hood adds the rest. The FBX was not rescaled.
- All seven clips are at or under 0.50 cm. The header is `worldMax=0.46` fails=0. Per clip: idle 0.00, walk 0.00, run 0.00, sprint 0.35, wall 0.05, slide 0.43, roll 0.46. The deep folds are open cloth, not a thinner plate: the upper sleeve drops its chest-side cap past mid-arm, the hamstring side of the thigh is open for 24 cm above the knee, the calf drops the back cap and the inner cap where the roll meets the arm, and the lower hoodie hem drops its front corners. The cuff is a short band on the hand, not a forearm ring. Bram's collar stops mid-neck so the jaw stays clear. Sprint, slide, and roll are under the limit and not zero.
- This lab lives under `Art/CharacterLab/` and `Docs/Characters/`. It is not in the player build.
