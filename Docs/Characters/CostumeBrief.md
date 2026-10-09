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

The helmet and the full sleeves are the stocky read. The meshes are Reed's three variants with Bram's blue parameter.

1. Helmet and visor, long clothes.
2. Cap, brim, pack, long clothes.
3. Hood and hood roll, long clothes.

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

Each piece is its own mesh, weighted to one existing Hier bone. The kit is a hoodie chest, upper and lower sleeves, upper and lower joggers, sneakers, cap, brim, helmet, visor, hood, hood roll, collar, hair, pack, pocket, and a back seam. Hands stay bare.

Player colour is one RGB node named `PlayerColor` on the lab material. Joggers, shoes, and the pack stay fixed colours so the torso and the head stay the seat colour. The noise bump strength is 0, so the seat colour stays a flat block. There are no image textures, logos, or brands.

Each loadout id is the license name in `Art/CharacterLab/LICENSES.md`. Bram's three ids are Reed's meshes with a blue `PlayerColor`.

LOD0 is the fitted mesh. LOD1 and LOD2 are collapse copies of those same pieces, parented to the same bones, and left out of the render loadout. Worn triangles, LOD0/LOD1/LOD2:

- Reed_1_Hood and Bram_3_Hood 8394/4398/2242. Reed_2_Cap and Bram_2_Cap 8390/4344/2002. Reed_3_Helmet and Bram_1_Helmet 8378/4343/2193.
- Pip_1_Cap 5222/2704/1244. Pip_2_Hair 4858/2517/1157. Pip_3_Helmet 5210/2703/1435.
- Sol_1_Collar_Cap 8366/4331/1996. Sol_2_Hood 8394/4398/2242. Sol_3_Helmet_Pack 8522/4417/2227.

The costume ceilings are 15000 / 8000 / 4000. Every loadout is under them, and each coarser level has fewer triangles than the one above it.

## Fit

`costume-fit sets=12 frames=4128 worldMax=7.13 fails=4128`

`costume-cover sets=12 buriedPct=1.94`

At rest, every cloth sample sits outside the body. The band check is min 0.32 cm, max 0.98 cm, under 0, over 0, inside the 0.3–1 cm layer. Hoodies and jackets use the thick end, about 9 mm. The cover check renders each of the 12 loadouts from 8 yaw angles and counts body-grey pixels against clothing pixels on the hidden plates. The worst loadout is Pip's hair, 1.94%. The pooled figure is 1.43%.

The clip measure is every 30 fps frame of the existing idle, run, sprint, wall-run plant, slide, and landing-roll clips, twelve loadouts, 4128 set-frames. It still fails every one of them. The deepest hit is 7.13 cm on the wall plant at t=0.367. Idle already starts near 2.3 cm, so shells overlap in the rest pose as well as when neighbouring bones bend. Pushing those verts back under the 0.50 cm limit is what buried the pass 3 shells inside the body, so this pass leaves the shells outside.

## Stills

Pass 1 keeps the lineup, the variant sheet, and the 30 px readability frame:

- `Docs/Characters/pass1/lineup-front.png`
- `Docs/Characters/pass1/lineup-three-quarter.png`
- `Docs/Characters/pass1/lineup-side.png`
- `Docs/Characters/pass1/readability-30px.png`
- `Docs/Characters/pass1/variants.png`

Pass 4 is the still quartet, each 1280×720 and under 400 KB. The four figures stand 1.75 m apart with their arms down, so the hands do not meet.

- `Docs/Characters/pass4/lineup-three-quarter.png` — Reed's red hood, Bram's blue helmet, Pip's orange crop, Sol's lavender collar and cap.
- `Docs/Characters/pass4/lineup-side.png` — the same four from the side.
- `Docs/Characters/pass4/joint-close.png` — Reed's right knee. The jogger shells stop short of the joint, and the shoe is a solid high-top.
- `Docs/Characters/pass4/scale-figure.png` — Reed's hood on the Hier body, feet on the ground, beside a 1.80 m staff and a bench.
- `Docs/Characters/pass4/before-reed.png` and `Docs/Characters/pass4/after-reed.png` — the same Reed hood loadout, before the shells were pushed outside and after.

Pass 3 stills stay in `Docs/Characters/pass3/` as the reviewed set. The cover numbers are in `Docs/Characters/pass4/cover.txt`.

The readability frame is a quarter of 1080p with each figure about 30 px tall. It does not replace the quartet.

## Honest flaws

- One skeleton and one height. The four outlines are headwear and the crop, not different proportions.
- Bram's variants are Reed's meshes with a different `PlayerColor`. They are not separate sculpts.
- Pip's cropped outfits land under the 8k triangle guide, about 4.9–5.2k at LOD0. The long outfits sit inside 8–15k, about 8.4–8.5k.
- Knees, elbows, hips, and ankles are open gaps so the shells do not meet on the slide or the roll. There is no high-top collar, because the shin is wider than the foot and a collar on the foot bone swings into it.
- The waist shell and the drawstrings are not in the loadouts. The waist shell passed through the thigh on the landing roll. The drawstring sat inside the pocket.
- There is no separate sole. A groove in the same layer as the shoe failed the costume-into-costume check.
- The brim, visor, hair, pack, and hood roll sit outside the 1 cm cloth band so the outline can read. The brim is the farthest, 10.09 cm off the head.
- The chest colour is a shell over the Hier plate with the flanks cut back so the arms can pass. It is not a tailored hoodie.
- Weights are one bone per piece. There is no cloth simulation.
- The mannequin's hip and thigh rest overlap is untouched.
- These clothes are fitted to the pre-ball Hier. The clearance candidate, with ball-and-socket hips and ankles, is not in this branch. Nothing here is refitted to that rig until the player lane says it is stable. The check on this tip still reports `rig-not-clearance` and `rig-proof-missing` for that reason.
- LOD1 and LOD2 are in the blend and on the `lod` lines of `Docs/Characters/pass1/fit.txt`. The models check reads only the single `worn` triangle count, so it still prints `lod1-missing` and `lod2-missing`.
- The scale still's hooded figure measures 1.873 m from the lifted sole to the top of the hood. The staff next to it is 1.80 m. The shipped Hier meshes, at armature scale (1, 1, 1), already span 1.8588 m: sole z is -0.0440, crown z is 1.8148, and the crown is the top of `Mesh_Head` (head z 1.5152..1.8148, left foot z -0.0440..0.0463). The hood adds the rest. The FBX was not rescaled.
- Covered body plates are hidden per loadout. The 8-angle cover check is `costume-cover sets=12 buriedPct=1.94`. A little body grey still shows at cuffs, open joints, and Pip's hairline.
- The clip fit fails every set-frame (`fails=4128`, `worldMax=7.13`). Rest cloth is outside the body. Idle already overlaps by about 2.3 cm, and the wall plant reaches 7.13 cm, because the shells are rigid and weighted to one bone each.
- This lab lives under `Art/CharacterLab/` and `Docs/Characters/`. It is not in the player build.
