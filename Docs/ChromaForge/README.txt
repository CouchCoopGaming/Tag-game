CHROMA FORGE — Slender Mold Pack
================================
Attach this whole folder (or the zip) to any chat, artist brief, or production thread.

WHAT IS IN HERE
---------------
ChromaForge_ArtBible.pdf     Printable / shareable art bible with concepts
README.txt                   This file
ART_BIBLE.md                 Full text spec (Markdown)
concepts/                    Hero concept sheets (JPG)
  00_lineup.jpg
  01_robot_ninja.jpg
  02_crash_dummy.jpg
  03_tron_bot.jpg
  04_pirate.jpg
  05_knight.jpg
  06_army_man.jpg
  07_astronaut.jpg
data/palettes.json           All 33 colors as machine-readable data
unity/
  ChromaPalette.cs
  ChromaApplier.cs
  ChromaPaletteLibrary.cs
  ShaderGraph_Notes.txt

IMPORTANT
---------
These are concept models and a production system, not Unity FBX meshes.
Drop the C# scripts into Assets/_Game/Characters/_Shared/Scripts/
Build seven slender humanoid molds in Blender from the concept sheets.
Recolor at runtime with MaterialPropertyBlock + RGB tint masks.

PROPORTION LOCK (SLENDER)
-------------------------
Height        1.80 m hero scale (Army Man 1.72 m)
Head          7.5–8 heads tall
Shoulder      2.0–2.2 head widths (not 2.6+)
Waist         0.55–0.62 of shoulder width
Arm span      equal to height
Limb          long forearm and shin, thin joints
Hands         slightly oversized for toy readability
Feet          compact, not clown boots

COLOR LAW
---------
Never bake hue into albedo.
Albedo is greyscale material.
RGB mask: R = Primary, G = Secondary, B = Accent.
33 palettes live in data/palettes.json and ScriptableObjects.

MOLD ORDER TO BUILD
-------------------
1. Army Man   (proves plastic + single tint)
2. Knight     (proves metal + cloth)
3. Tron Bot   (proves emissive)
4. Dummy
5. Astronaut
6. Pirate
7. Robot Ninja
