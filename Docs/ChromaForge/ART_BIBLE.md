# CHROMA FORGE
## Slender Mold Art Bible 1.1

Seven humanoid factory molds. One shared skeleton. Thirty-three charges of color.
This revision locks **slender** proportions across the whole roster.

These files are concept art + production specs, not finished Unity meshes.
Build the meshes in Blender from the sheets in `/concepts`. Recolor in engine.

---

## 1. Visual lock

Premium collectible action-figure. Manga-readable silhouette. Not photoreal. Not stubby toy chibi. Not bodybuilder.

### Slender proportion lock

| Measure | Target |
|---|---|
| Height | 1.80 m (Army Man 1.72 m) |
| Heads tall | 7.5–8 |
| Shoulder width | 2.0–2.2 heads |
| Waist | 0.55–0.62 of shoulder |
| Hips | slightly wider than waist, never bulky |
| Arm span | = height |
| Neck | long enough to read, thin |
| Hands | +10% toy scale |
| Joints | visible rings, slim |

If a piece of armor makes the shoulder wider than 2.3 heads, cut the armor, do not fatten the body.

### Shared construction

- Humanoid Unity / Mixamo skeleton
- 10k–16k tris LOD0 (Army Man can sit at 8–11k)
- One UV set, non-overlapping, 2K–4K
- Greyscale albedo + RGB tint mask + packed ORM + optional emissive
- Weapons socketed, not fused, unless Army Man rifle
- Molded seam lines are a design feature

---

## 2. Color law

Never store hue in the albedo. Albedo is scratched plastic, brushed metal, cloth weave.

RGB mask:

- R = Primary (body / plates / suit)
- G = Secondary (undersuit / straps / inner cloth)
- B = Accent (visor, trim, weapon line, lights)

Eleven hues × three shades = 33 official charges.

| Hue | Pale | Standard | Deep |
|---|---|---|---|
| White | `#F4F1EA` Bone | `#E6E0D4` Ivory | `#C9C2B4` Antique |
| Black | `#3E3E42` Charcoal | `#16161A` Ink | `#07070A` Void |
| Blue | `#7EB8E8` Ice | `#2E6BFF` Signal | `#0A2A6B` Abyss |
| Green | `#8FDC7A` Mint | `#2F8B3A` Army | `#163D18` Jungle |
| Yellow | `#FFE566` Sun | `#FFD100` Hazard | `#B8860B` Brass |
| Red | `#FF6B6B` Flare | `#E10600` Banner | `#7A0000` Blood-plastic |
| Orange | `#FFB347` Safety | `#FF6A00` Flare-orange | `#C44D00` Rust-fire |
| Pink | `#FFB6D9` Cotton | `#FF4FA3` Neon | `#C71585` Magenta-seal |
| Purple | `#C9A0FF` Lilac | `#7B2CBF` Royal | `#3C096C` Night-ink |
| Brown | `#C4A574` Tan | `#8B5A2B` Leather | `#4A2C14` Hull |
| Grey | `#C0C4C8` Primer | `#6E7378` Steel | `#3A3D40` Slate |

Tron exception: body stays Ink/Void. The named color lives in Accent + emission.

---

## 3. The seven slender molds

### Robot Ninja — Silent Circuit Clan
Slim mecha assassin. Reverse-joint optional. Long forearms. Thin kabuto visor. Folded back-blades as a V. Two short katanas on the hips. No bulky pauldrons.
- Primary chassis, Secondary undersuit, Accent visor slit + joint ticks
- 13–16k tris
- Techniques: Optical Smear / Circuit-Cut / Mute Field

### Crash Test Dummy — Unbreakable Cohort
Tall sensor doll, not a barrel. Rubber-ring neck. Circular eyes. Raised rib bands on a narrow torso. Long limbs with ball joints. Hazard mallet or open hands.
- Vinyl, almost no metal
- 11–14k tris
- Techniques: Rebound / Data Scar / Full Dummy Protocol

### Tron Bot — Grid Sentinel
Lithe Program. Flat planes, hard bevels, neon in grooves. Helmet is one volume. Identity disc on the back. Runner legs.
- Albedo near black, Accent is the light
- 11–15k tris
- Techniques: Line Walk / Identity Throw / Derezz

### Pirate — Salt-and-Chrome Corsair
Lean coat, not a stuffed admiral. Tricorn. Hook. Cutlass. Fitted waist. Coat as a separate mesh with 4–6 cloth bones.
- 13–16k tris
- Techniques: Black Spot / Boarding Hook / Powder Night

### Knight — Painted Order
Fitted plate, wasp waist, long greaves, thin crest. Modular helm / chest / shoulders / tabard / shield / sword. Hidden body deleted under armor.
- 14–16k tris assembled
- Techniques: Color Guard / Oath Swing / Last Standard

### Army Man — Plastic Battalion
Classic 54mm language stretched to slender hero height. Single-color plastic. Mold line down the sagittal plane. Face is a molded suggestion.
- 8–11k tris
- Techniques: Bag Spill / Mold Line / Whole Platoon
- Default charge is Green Standard `#2F8B3A`

### Astronaut — Void Walker
Sleek technical suit, not a marshmallow. Compact backpack. Spherical helmet. Cyan ring light. Long legs.
- 12–15k tris
- Techniques: Soft Vacuum / Beacon / Re-entry

---

## 4. Power system (playable)

Every mold uses three verbs. Shade changes the feel, not the move list.

- **Dash** — mobility
- **Mark** — paint the world in the unit hue
- **Break** — spend shade depth on a finisher

Pale dashes farther, breaks weaker.
Deep dashes shorter, breaks the screen.

Hybrid rule: Primary chooses the animation set. Accent chooses the VFX color.

---

## 5. Unity pipeline

Scripts live in `/unity`.

1. Create URP Shader Graph `ChromaLit` from `ShaderGraph_Notes.txt`
2. Drop `ChromaPalette.cs`, `ChromaApplier.cs`, `ChromaPaletteLibrary.cs` into the project
3. Make 33 palette assets (or generate from `data/palettes.json`)
4. One prefab per mold. Colors are data. Do not duplicate prefabs per color
5. Humanoid Avatar + Mixamo first pass
6. LOD1 at 12 m, LOD2 at 28 m. Kill Tron emissive on LOD2

Sockets on every mold:

- `socket_weapon_r`
- `socket_weapon_l`
- `socket_back`
- `socket_hat`
- `socket_fx_chest`
- `socket_fx_eye`

---

## 6. Artist brief (copy-paste)

Seven slender humanoid hero molds, collectible-toy style, 8–16k tris, Unity Humanoid, T-pose, applied scale, +Z forward. Greyscale albedo + RGB tint mask + packed ORM + optional emissive. Three tint zones only. Shared 7.5–8 head proportions, narrow waist, long limbs. Weapons socketed. No baked brand colors. Build Army Man first as the shader proof.

---

## 7. File list

- `concepts/00_lineup.jpg` through `07_astronaut.jpg`
- `data/palettes.json`
- `unity/*.cs` and `ShaderGraph_Notes.txt`
- `ChromaForge_ArtBible.pdf`
