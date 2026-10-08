# Tag model standard

Every model worker follows this document. The check is `Tools/Models/validate_assets.py`. It is pure Python and, when Blender is on the machine, the same script recomputes collider slack inside `bpy`. A pass is a process that prints `models-validate assets=N pass=P fail=F` with `fail=0`.

The library is Unity 6000.3.24f1, four-player couch tag. Coordinates in the asset scripts are meters: **+X right, +Y up, +Z forward**. Blender stays Z-up internally. FBX export bakes the Unity axis and centimeter scale. Unity's importer (`useFileScale`) brings it back to meters. Do not author in centimeters.

This standard is the bar the five lanes are already aiming at: the procedural library under `Tools/Blender/AssetLibrary/`, the street-kit and street-object stacks, the Hier clearance rebuild, and the costume lab. Numbers below are the ones those scripts already use, written down so a later pass cannot drift.

## 1. Real-world scale

A standing player is **1.8 m**. The showcase `Mannequin` and the Hier body are that height (accept 1.75–1.85 m). Vault rails in the park kit sit at **0.90–1.05 m** above the deck you take off from. Road asphalt top is 0.12 m. Sidewalk top is 0.27 m (15 cm curb). A brick bay is 4.0 m wide and 3.2 m tall.

Every scale still includes that 1.8 m figure, standing on the ground the prop uses, not sunk into it and not floating. Harbor and street stills use the Hier body at the same height. The showcase mannequin is a scale prop, not a gameplay character.

Cars are badge-free modern US bodies from model years **2022 through 2026** (the last five model years as of 8 Oct 2026). The mesh name carries the year (`_22` … `_26`). Size is the published exterior, in meters, within this envelope:

| Class | Length (Z) | Width (X) | Height (Y) |
| --- | --- | --- | --- |
| Midsize sedan | 4.70–5.05 | 1.75–1.90 | 1.38–1.50 |
| Compact sedan | 4.20–4.70 | 1.70–1.82 | 1.38–1.52 |
| Compact hatch | 3.90–4.50 | 1.70–1.85 | 1.40–1.56 |
| Compact crossover | 4.20–4.70 | 1.75–1.90 | 1.55–1.72 |
| Pickup | 5.00–5.90 | 1.85–2.10 | 1.65–1.95 |
| 40 ft city bus | 11.80–13.00 | 2.40–2.65 | 3.00–3.40 |
| 60 ft articulated bus | 17.80–19.00 | 2.40–2.65 | 3.00–3.40 |

A 2025 Camry-class table (length 193.5 in, width 72.4 in, height 56.9 in, wheelbase 111.2 in) is a legal **dimension** reference. It is not a license to use the name, the badge, or a Toyota mesh. ISO containers stay 20 ft = 6.06 × 2.44 × 2.59 m and 40 ft = 12.19 × 2.44 × 2.59 m.

Length runs on **+Z**. If width and length are swapped, the asset fails orientation.

## 2. Sources and the license manifest

Meshes and textures are **CC0 1.0** only. **OFL 1.1** is allowed for a font and never for a mesh. No other license ships.

Every asset has its own license record, in either place:

- `license` on its `Tools/Blender/AssetLibrary/manifest.json` entry, or
- a row in a `LICENSES.md` next to the art (`Art/Vehicles/LICENSES.md` and the same pattern for props and characters) that contains the **exact** asset name and `CC0-1.0` or `OFL-1.1`.

```json
"license": {
  "spdx": "CC0-1.0",
  "source": "original",
  "url": "",
  "notes": "Procedural mesh built in this repo."
}
```

`source` is `original` or `cc0-download`. A download needs the `url`. A file that only says the library is original, without naming the asset, does not count. Citing a brochure for proportions does not put that brand into the record as a source mesh.

Original work in this repo is dedicated to the public domain under CC0 1.0 when the record says so. Do not import Kenney, Quaternius, Poly Haven, or any other pack unless that exact file is CC0 and the record points at it. Stretching a CC0 toy car into a 4.9 m sedan is not a modern body. Build the body here instead.

## 3. Colliders

Colliders hug the visible mesh. The builder samples the collider surface (`Tools/Blender/AssetLibrary/_common.py`, `validate_colliders`, tolerance **0.03 m**) and records the worst gap as `slackCm`. The limit is **3.0 cm**. Report the worst slack in centimeters on every asset, including a pass.

Names:

- `Col_*` is one solid piece.
- `Climb_*` is the cling face and is the wall, not a shell around it.
- `Vault_*` is the rail. `vaultHeight` is the rail height above the takeoff deck, in the 0.90–1.05 m band when it is a ground vault.

A thin `approx` slab across chain-link, a grate, or crate slats is allowed. The holes are not a passage. Road paint, the asphalt patch, `HarborWater`, and a rope (`MooringLine`) have no collider. Everything else has at least one.

**Landable tops.** If the blurb or the vault note calls the roof, deck, hood, lid, or gunwale a landing or a walk surface, a collider top sits within **8 cm** of the mesh top. Parkour lands on that collider, not on a visual-only lid. Round poles, thin tubes, and pickets are not cling walls. Say so in the note.

Pieces that are supposed to extend below the ground pivot (piles, quay foundations, boat hulls in the water, dock piles) set `allow_below` and say so in the blurb. Anything else that drops more than 2 cm below Y=0 fails. A mesh that floats more than 3 cm above the pivot fails, except a decal on the 0.12 m road, a wall sleeve (`WallAC`), and a dock-mounted piece whose pivot is the water (`Gangway`, `MooringLine`).

## 4. Triangle budgets

LOD meshes are named `LOD0`, `LOD1`, `LOD2`. LOD1 is required when LOD0 is over 400 triangles. LOD2 is required when LOD0 is over 2000, and always for cars and buses. A coarser LOD never has more triangles than the finer one. Ceilings:

| Class | LOD0 | LOD1 | LOD2 | What goes here |
| --- | --- | --- | --- | --- |
| Prop | 2500 | 1400 | 700 | Street furniture, utility, small park pieces |
| Dense prop | 6000 | 2800 | 1400 | Bike racks, newsstands, fountains, fences, playground |
| Road | 4000 | 4000 | 2000 | Tiles, curbs, junctions |
| Building bay | 3000 | 1600 | 900 | Walls, doors, windows, fences, parapets |
| Building shell | 8000 | 5000 | 3400 | Houses, stores, walk-ups, cabin, garage, gas canopy |
| Harbor | 4000 | 2200 | 1200 | Small harbor props |
| Harbor large | 9000 | 4500 | 1800 | Containers, warehouse, quay, work boat |
| Park | 4000 | 2800 | 1500 | Court, hoop, shelters |
| Tree | 3500 | 1600 | 900 | Trunk and canopy |
| Car | 15000 | 7000 | 2800 | Sedan, hatch, crossover, pickup |
| Bus | 8000 | 4000 | 2000 | 40 ft and 60 ft |
| Scale figure | 2000 | 1000 | 500 | Showcase mannequin only |
| Mannequin | 40000 | 20000 | 10000 | Hier body |
| Costume, worn | 15000 | 8000 | 4000 | One loadout on the body |

The car LOD0 band the sedan loft is aiming at is 12–15k after subdivision. A sidewalk A-frame does not get the dense-prop budget.

## 5. Names and folders

```
Assets/Art/Props/Library/<Category>/<Name>.fbx
Assets/Art/Props/Library/<Category>/Prefabs/<Name>.prefab
Assets/Art/Props/Library/Textures/Lib_<Material>.png
Assets/Art/Props/Library/Vehicles/          (cars, buses; trains and trolleys go here too)
Assets/Art/Characters/HiPoly/Dummy_Mannequin_Tan_Hier_Hi.fbx
Assets/Art/Characters/HiPoly/Candidate/     (clearance candidate, unbound)
Art/CharacterLab/                           (costume prep, not in the player build)
```

`Name` is PascalCase segments with underscores (`Brick_Wall`, `Sedan_Mid_A_25`, `Bus_City40`). Category is one of `Buildings`, `Harbor`, `Park`, `Roads`, `StreetFurniture`, `Utility`, `Vehicles`, `Showcase`. The manifest `category` matches the folder. Mesh objects inside the FBX are `LOD0`, `LOD1`, `LOD2` only.

The builder for a library asset is `Tools/Blender/AssetLibrary/<snake>.py` or `vehicles/<snake>.py`. Rebuild from that script. Do not hand-edit the FBX.

Player color variants of the same Hier mesh are duplicates of `Dummy_Mannequin_Tan_Hier_Hi`, not new bodies. A livery (`Sedan_Mid_A_25_White`, `Bus_City40_Blue`, `Container_20_Green`) is a separate manifest row and shares the base mesh's stills only when the license row says it is a color swap.

## 6. Pivot and orientation

Pivot is the ground contact, centered on the footprint within **5 cm**, unless the module notes a different origin (`Dock_Corner` is the center of its 4 m square). +Y is up. +Z is the street-facing or nose direction.

- Building modules: exterior on **+Z**. Brick bays stack at Y = 3.2.
- Cars and buses: nose on **+Z**, wheels on the ground, ground clearance about 0.14 m for a sedan.
- Boats: bow on **−Z**, so the slip and the quay agree. That is the one forward exception. Gunwales and decks still get colliders.
- Container doors on **+Z**.

## 7. Materials and texel density

Library materials are `Lib_*` from the shared palette in `_common.py`. No third-party textures. Brick, concrete, wood, bark, asphalt, and the worn metals are Blender node trees baked to albedo, roughness, normal, and occlusion.

UV is meters. One tile is one meter (`uv_scale` 1.0). Bakes are **512×512**, which is **512 texels per meter**. 256 and 1024 are allowed (256–1024 px/m). A non-square bake fails. The court paint is one decal measured in place, not rescaled by the importer, and is the exception to the 1 m tile. Window glass and street-light lenses may emit. Shop glass is dark and transmissive, not a light panel. Flat enamel (`Lib_BoxRed`, `Lib_Board`, `Lib_Varnish`) stays untextured on purpose so a curved hull does not pick up a brick tile.

Player costumes use a procedural noise bump and one RGB node named `PlayerColor`. No image textures on costumes.

## 8. No logos or brand badges

No badges, wordmarks, nameplates, or licensed liveries on the mesh, the material, or the texture. A mailbox is a generic box. A hydrant is a generic hydrant. Shop signs use invented words already in the library (`MARKET`, `DINER`, `WASH`), not a real chain.

Vehicles are:

- badge-free modern US **cars** from model years 2022–2026 (midsize and compact sedans, hatches, crossovers, pickups),
- **buses** (40 ft low-floor and 60 ft articulated),
- **trains** and **trolleys**.

Trains and trolleys are part of the set. None are in the library yet. Do not fill the gap with a branded locomotive.

## 9. Stills

Every asset has four stills, each **1280×720 or larger**, each **under 400 KB**, in a pass folder:

| Role | What it shows | File token |
| --- | --- | --- |
| Quarter | Three-quarter of the whole object | `quarter`, `hero`, `three-quarter`, or the catalog stem |
| Side | Side elevation | `side`, `profile` |
| Close-up | The part a player reads up close (fascia, joint, bowl, door) | `close`, `nose`, `door`, `wheel`, `bowl`, `window`, `detail` |
| Scale | Same object with the 1.8 m figure | `scale`, `figure` |

Accepted folders, newest pass wins:

- `Docs/AssetStills/passN/`
- `Docs/AssetStills/street_kit/passN/`
- `Docs/AssetStills/street_objects/passN/`
- `Docs/AssetStills/vehicles/<slug>/passN/`
- `Docs/Characters/passN/`
- `Docs/LocoStills/passN/`

A PNG or JPEG sitting outside a `passN` directory does not count. `before_` / `after_` pairs are comparison frames, not a substitute for a missing role unless the stem also carries the role token. Costume stills add `readability-30px` (a quarter of 1080p, figures about 30 px tall). That frame is extra. It does not replace the quartet.

## 10. Player rig

The gameplay body is the Hier mannequin: rigid pieces parented to bones, not a skin. The clearance candidate is rebuilt with real ball-and-socket joints. The knee work is the model for the other hinges. Current authored balls on the candidate (`Tools/Tag/build_ball_joints.py`):

| Joint | Ball radius | Notes |
| --- | --- | --- |
| Hip (`UpperLeg`) | 3.0 cm | Socket on the pelvis. Has to clear through the flex range, not only at rest and at a deep fold. |
| Knee (`LowerLeg`) | 3.6 cm | Cone holds a bend past 140°. |
| Ankle (`Foot`) | 3.0 cm | Same ball-and-socket as the hip. Not a hinge buried in the shin. |
| Shoulder | 4.0 cm | |
| Elbow | 3.0 cm | |
| Wrist | 2.8 cm | |

The candidate writes `Assets/Art/Characters/HiPoly/Candidate/Dummy_Mannequin_Tan_Hier_Clearance.fbx`. It does **not** replace `Dummy_Mannequin_Tan_Hier_Hi.fbx` and it is **not** bound into the game. Landon accepts the stills before anyone binds it.

### No-clip

Sample every **30 fps** frame of the motion set (`DT = 1.0 / 30.0` in `Tools/Tag/render_loco_stills.py`). Overlap deeper than **0.5 cm** fails. The depth is absolute. Do not subtract a bind pose to hide it.

Verts within **3 cm** of the hinge are the joint hardware and are not a hit. Past that radius:

- Overlap between a parent piece and its child at the **same joint**, measured on the rest pose, is `rigJoint`.
- Overlap that appears on a live frame and was clean at rest is `pose`.

`pose` must be **0**. `rigJoint` must be **0**. Report them as two numbers, never folded into one counter. `selfMax` and `worldMax` are in centimeters and both stay at or under 0.5.

### Hip-sit

On loaded frames, the pelvis sits behind the support foot (horizontal distance along the facing direction):

- Plants and landings: at least **8 cm**.
- Crouch: at least **12 cm**.
- On those same frames, hip flexion is at least **1.5×** spine flexion.

A single deep fold (the 110° hip still) does not satisfy this. The check is the loaded plant, the landing, and the crouch, every 30 fps frame.

## 11. Costumes

Costumes read as clothes on the Hier body, not as a second body. Hoodies, joggers, high-tops, caps, helmets, a small pack. No capes, scarves, or loose cloth. Hands stay bare. Who-is-who is a solid player color on the torso and the head: P1 Reed red, P2 Bram blue, P3 Pip orange, P4 Sol purple. Joggers, shoes, and the pack stay fixed colors.

Each piece is its own mesh, weighted to **one** bone of the rig it was built for, and it has to be the clearance rig once that rig is the body. A costume fitted to the pre-ball Hier does not pass. Cloth sits in a **0.3–1.0 cm** band off the rendered hull. Costume-into-body and costume-into-costume stay under the same **0.5 cm** no-clip limit, measured on every 30 fps frame of idle, run, sprint, wall-run plant, slide, and landing roll. Worn LOD0 stays inside the costume budget above. Gaps left so a sleeve does not eat the joint are acceptable only while the ball is visible and the piece still reads as a garment. The lab lives in `Art/CharacterLab/` and `Docs/Characters/`. It is not in the player build, and it is not bound into the map lane.

## 12. What the validator grades

`python3 Tools/Models/validate_assets.py --root <checkout>`

It grades, on that checkout:

- every entry in `Tools/Blender/AssetLibrary/manifest.json`,
- any FBX under `Assets/Art/Props/Library/` that the manifest forgot,
- `Dummy_Mannequin_*_Hier_Hi` and the clearance candidate,
- every costume loadout in `Art/CharacterLab/loadouts.json`.

It does not grade the playground kit under `Assets/Art/Props/Playground/` or the flat `Dummy_Runner` / `Dummy_It` block-in. Those are not this library.

With Blender on `PYTHONPATH` (or `blender --background --python Tools/Models/validate_assets.py -- --root <checkout>`), slack is measured again from the FBX and the manifest colliders. Without Blender, slack is the manifest's `slackCm` from that same 3 cm test, and a missing `slackCm` fails.

One line per asset, then:

```
models-validate assets=N pass=P fail=F
```
