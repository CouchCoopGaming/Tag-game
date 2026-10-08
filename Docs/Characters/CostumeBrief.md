# Costume brief

Prep for player costumes on the couch tag mannequin. The shipped Hier rig, its skeleton, its bind, and the game scenes stay as they are. Another lane owns the hip and thigh rest overlap. Nothing here binds into the game.

Each seat is a quarter of a 1080p screen. A runner in that view is often 20–40 px tall. At that size the read is the outline plus one colour block, not a texture.

## Look

- The silhouette has to stay recognizable at 20–40 px. A one-centimetre fold is a fraction of a pixel.
- Who-is-who is a solid player-colour block on the torso and on the head. P1 Reed is red, P2 Bram is blue, P3 Pip is orange, P4 Sol is purple.
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

Player colour is one RGB node named `PlayerColor` on the lab material. Joggers, shoes, and the pack stay fixed colours so the torso and the head stay the seat colour. The surface is a procedural noise bump. There are no image textures, logos, or brands.

Each loadout id is the license name in `Art/CharacterLab/LICENSES.md`. Bram's three ids are Reed's meshes with a blue `PlayerColor`.

LOD0 is the fitted mesh. LOD1 and LOD2 are collapse copies of those same pieces, parented to the same bones, and left out of the render loadout. Worn triangles, LOD0/LOD1/LOD2:

- Reed_1_Hood 10858/5630/2592. Reed_2_Cap 11094/5752/2651. Reed_3_Helmet 10938/5672/2614.
- Bram_1_Helmet, Bram_2_Cap, and Bram_3_Hood match those three Reed totals.
- Pip_1_Cap 7760/4022/1856. Pip_2_Hair 7464/3870/1784. Pip_3_Helmet 7604/3942/1819.
- Sol_1_Collar_Cap 11270/5844/2693. Sol_2_Hood 10858/5630/2592. Sol_3_Helmet_Pack 11082/5746/2648.

The costume ceilings are 15000 / 8000 / 4000. Every loadout is under them, and each coarser level has fewer triangles than the one above it.

## Fit

`costume-fit sets=12 frames=4128 worldMax=0.38 fails=0`

Cloth sits 0.60 cm off the rendered hull. The band check is min 0.60 cm, max 0.60 cm, under 0, over 0, inside the 0.3–1 cm layer. The deepest costume-into-body or costume-into-costume intersection left on the clips is 0.38 cm, under the 0.50 cm limit. The measure is every 30 fps frame of the existing idle, run, sprint, wall-run plant, slide, and landing-roll clips. Twelve loadouts, 4128 set-frames.

## Stills

Pass 1 keeps the lineup, the variant sheet, and the 30 px readability frame:

- `Docs/Characters/pass1/lineup-front.png`
- `Docs/Characters/pass1/lineup-three-quarter.png`
- `Docs/Characters/pass1/lineup-side.png`
- `Docs/Characters/pass1/readability-30px.png`
- `Docs/Characters/pass1/variants.png`

Pass 3 is the still quartet, each 1280×720 and under 400 KB:

- `Docs/Characters/pass3/lineup-three-quarter.png` — Reed's hood, Bram's blue helmet, Pip's crop, Sol's collar and cap.
- `Docs/Characters/pass3/lineup-side.png` — the same four from the side.
- `Docs/Characters/pass3/joint-close.png` — Reed's right knee, where the jogger shells leave the joint open.
- `Docs/Characters/pass3/scale-figure.png` — Reed's hood on the Hier body, feet on the ground, beside a 1.80 m staff and a bench.

The readability frame is a quarter of 1080p with each figure about 30 px tall. It does not replace the quartet.

## Honest flaws

- One skeleton and one height. The four outlines are headwear and the crop, not different proportions.
- Bram's variants are Reed's meshes with a different `PlayerColor`. They are not separate sculpts.
- Pip's cropped outfits land under the 8k triangle guide, about 7.5–7.8k. The long outfits sit inside 8–15k.
- Knees, elbows, hips, and ankles are open gaps so the shells do not meet on the slide or the roll. There is no high-top collar, because the shin is wider than the foot and a collar on the foot bone swings into it.
- The waist shell and the drawstrings are not in the loadouts. The waist shell passed through the thigh on the landing roll. The drawstring sat inside the pocket.
- There is no separate sole. A groove in the same layer as the shoe failed the costume-into-costume check.
- The brim, visor, hair, pack, and hood roll sit outside the 1 cm cloth band so the outline can read. The brim is the farthest, about 12.4 cm off the head.
- The chest colour is a shell over the Hier plate with the flanks cut back so the arms can pass. It is not a tailored hoodie.
- Weights are one bone per piece. There is no cloth simulation.
- The mannequin's hip and thigh rest overlap is untouched.
- These clothes are fitted to the pre-ball Hier. The clearance candidate, with ball-and-socket hips and ankles, is not in this branch. Nothing here is refitted to that rig until the player lane says it is stable. The check on this tip still reports `rig-not-clearance` and `rig-proof-missing` for that reason.
- LOD1 and LOD2 are in the blend and on the `lod` lines of `Docs/Characters/pass1/fit.txt`. The models check reads only the single `worn` triangle count, so it still prints `lod1-missing` and `lod2-missing`.
- The Hier body in the scale still measures 1.86 m from the sole to the top of the hood. The staff next to it is 1.80 m. The mannequin scale is unchanged.
- This lab lives under `Art/CharacterLab/` and `Docs/Characters/`. It is not in the player build.
