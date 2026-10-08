using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Foot dust numbers. Size, opacity, life, and count follow planar speed.
    /// The surface changes the color and how thick the puff is.
    /// A walk at 6.9 is a small puff. A sprint at 13.8 is about three times the volume.
    /// These sizes are visual FX, not a feel lock.
    /// Run start, a hard pivot, and the first slide frame kick harder.
    /// Metal stays quiet except a few sparks on a hard pivot.
    /// Wet throws dark droplets. Nothing here changes speed or slide.
    /// </summary>
    public static class DustLook
    {
        public const float Walk = 6.9f;
        public const float Sprint = 13.8f;
        public const float Pi = 3.14159265f;
        public const int SurfaceCount = 6;
        public const int TagNone = -1;
        public const float TrailGap = 0.08f;

        public enum Surface
        {
            Grass = 0,
            Dirt = 1,
            Concrete = 2,
            Wood = 3,
            Metal = 4,
            Wet = 5
        }

        public enum Kick
        {
            None = 0,
            RunStart = 1,
            Pivot = 2,
            Slide = 3,
            Trail = 4
        }

        public struct Puff
        {
            public float Size;
            public float Opacity;
            public float Life;
            public int Count;
            public float R;
            public float G;
            public float B;
            public int Splash;
            public int Spark;
        }

        public static bool CloudsOn(Tag.Settings.GameSettings settings)
        {
            if (settings != null && settings.AnyReduceFlash()) return false;
            if (settings != null && settings.Effects <= 0) return false;
            return true;
        }

        /// <summary>
        /// True when the gait cycle crosses the next plant (a multiple of pi).
        /// Even crossings are the left foot. The cycle only moves forward while stepping.
        /// </summary>
        public static bool FootDown(float prev, float now, out bool left)
        {
            left = false;
            if (now <= prev + 0.0001f) return false;
            int a = (int)(prev / Pi);
            int b = (int)(now / Pi);
            if (a == b) return false;
            int crossed = a + 1;
            left = (crossed & 1) == 0;
            return true;
        }

        public static Kick KickOf(float prevSpeed, float speed, float pivot, bool slideEnter)
        {
            if (slideEnter) return Kick.Slide;
            if (pivot > 0.55f) return Kick.Pivot;
            if (prevSpeed < 3.2f && speed >= Walk * 0.85f) return Kick.RunStart;
            return Kick.None;
        }

        public static Surface FromTag(int kind)
        {
            if (kind < 0 || kind >= SurfaceCount) return Surface.Concrete;
            return (Surface)kind;
        }

        public static string PhysicsName(int kind)
        {
            switch (FromTag(kind))
            {
                case Surface.Grass: return "grass";
                case Surface.Dirt: return "dirt";
                case Surface.Wood: return "wood";
                case Surface.Metal: return "metal";
                case Surface.Wet: return "wet";
                default: return "concrete";
            }
        }

        /// <summary>
        /// Tag wins. Otherwise the name is scanned for a surface word.
        /// Grass stays a light fleck. Mulch and sand are dirt. Asphalt stays with concrete.
        /// </summary>
        public static Surface Classify(string material, string objectName, int tag)
        {
            if (tag >= 0 && tag < SurfaceCount) return (Surface)tag;
            if (NamedMetal(objectName)) return Surface.Metal;
            string name = material;
            if (string.IsNullOrEmpty(name)) name = objectName;
            if (string.IsNullOrEmpty(name)) return Surface.Concrete;
            if (Has(name, "wet") || Has(name, "water") || Has(name, "puddle"))
                return Surface.Wet;
            if (Has(name, "grass") || Has(name, "leaf") || Has(name, "field") || Has(name, "soft"))
                return Surface.Grass;
            if (Has(name, "sand") || Has(name, "dirt") || Has(name, "soil") || Has(name, "mulch"))
                return Surface.Dirt;
            if (Has(name, "wood") || Has(name, "cedar") || Has(name, "bark") || Has(name, "plank"))
                return Surface.Wood;
            if (Has(name, "steel") || Has(name, "metal") || Has(name, "fence") || Has(name, "plate")
                || Has(name, "lamp") || Has(name, "army") || Has(name, "knight") || Has(name, "amber")
                || Has(name, "rail"))
                return Surface.Metal;
            return Surface.Concrete;
        }

        public static bool Named(string material)
        {
            if (string.IsNullOrEmpty(material)) return false;
            return Classify(material, null, TagNone) != Surface.Concrete
                || Has(material, "concrete") || Has(material, "asphalt")
                || Has(material, "rubber") || Has(material, "pad") || Has(material, "rim");
        }

        public static Puff At(int surface, float speed, int kick)
        {
            return At(FromTag(surface), speed, (Kick)kick);
        }

        public static Puff At(Surface surface, float speed, Kick kick)
        {
            Puff puff = new Puff();
            if (surface == Surface.Metal)
            {
                puff.R = 1f;
                puff.G = 0.90f;
                puff.B = 0.45f;
                puff.Spark = 1;
                puff.Size = 0.045f;
                puff.Life = 0.10f;
                if (kick == Kick.Pivot)
                {
                    puff.Count = 3;
                    puff.Opacity = 0.85f;
                }
                return puff;
            }

            float t = 0f;
            if (Sprint > Walk)
                t = (speed - Walk) / (Sprint - Walk);
            if (t < 0f) t = 0f;
            if (t > 1.35f) t = 1.35f;
            // Life stays on the previous curve so dirt still outlasts concrete.
            float life = 0.18f + t * 0.20f;
            float size;
            float opacity;
            float count;
            if (surface == Surface.Wet)
            {
                // Wet was not in the adopted table. It keeps the previous curve.
                size = (0.07f + t * 0.20f) * 0.90f;
                opacity = (0.18f + t * 0.54f) * 0.85f;
                count = (2f + t * 6f) * 1.10f;
                life *= 0.55f;
                puff.R = 0.12f;
                puff.G = 0.22f;
                puff.B = 0.40f;
                puff.Splash = 1;
            }
            else
            {
                // Adopted visual defaults (pass 12). Walk is t = 0, sprint is t = 1.
                // Wide, squash, lift, and spread stay in the still. They are not fields here.
                // Previous curve, too faint to read at a walk:
                //   size = 0.07+t*0.20, opacity = 0.18+t*0.54, count = 2+t*6
                //   grass    size 0.75  opacity 0.40  life 0.85  count 0.55  colour (0.40, 0.48, 0.18)
                //   dirt     size 1.45  opacity 1.15  life 1.70  count 1.35  colour (0.76, 0.55, 0.30)
                //   wood     size 0.38  opacity 0.32  life 0.60  count 0.40  colour (0.55, 0.42, 0.28)
                //   concrete size 0.55  opacity 0.50  life 0.42  count 0.50  colour (0.82, 0.82, 0.80)
                float size0;
                float size1;
                float op0;
                float op1;
                float count0;
                float count1;
                float lifeMul;
                if (surface == Surface.Grass)
                {
                    size0 = 0.08f; size1 = 0.11f; op0 = 0.24f; op1 = 0.31f; count0 = 4f; count1 = 5f; lifeMul = 0.85f;
                    puff.R = 0.80f; puff.G = 0.76f; puff.B = 0.62f;
                }
                else if (surface == Surface.Dirt)
                {
                    size0 = 0.24f; size1 = 0.335f; op0 = 0.62f; op1 = 0.68f; count0 = 7f; count1 = 10f; lifeMul = 1.70f;
                    puff.R = 0.84f; puff.G = 0.58f; puff.B = 0.30f;
                }
                else if (surface == Surface.Wood)
                {
                    size0 = 0.055f; size1 = 0.078f; op0 = 0.70f; op1 = 0.80f; count0 = 6f; count1 = 8f; lifeMul = 0.60f;
                    puff.R = 1.00f; puff.G = 0.97f; puff.B = 0.88f;
                }
                else
                {
                    size0 = 0.18f; size1 = 0.24f; op0 = 0.32f; op1 = 0.44f; count0 = 4f; count1 = 5f; lifeMul = 0.42f;
                    puff.R = 0.96f; puff.G = 0.96f; puff.B = 0.94f;
                }
                size = size0 + (size1 - size0) * t;
                opacity = op0 + (op1 - op0) * t;
                count = count0 + (count1 - count0) * t;
                life *= lifeMul;
            }
            if (speed < Walk)
            {
                float u = Walk > 0.01f ? speed / Walk : 0f;
                if (u < 0f) u = 0f;
                size *= u;
                opacity *= u;
                count *= u;
            }
            if (speed < 2.2f) count = 0f;

            float mul = 1f;
            if (kick == Kick.RunStart) mul = 1.85f;
            else if (kick == Kick.Pivot) mul = 1.70f;
            else if (kick == Kick.Slide) mul = 2.10f;
            else if (kick == Kick.Trail) mul = 1.25f;
            size *= mul;
            opacity *= mul;
            if (opacity > 0.95f) opacity = 0.95f;

            puff.Size = size;
            puff.Opacity = opacity;
            puff.Life = life;
            int n = (int)(count + 0.5f);
            if (n > 12) n = 12;
            if (n < 0) n = 0;
            puff.Count = n;
            return puff;
        }

        public static bool Holds()
        {
            if (Mathf.Abs(Walk - 6.9f) > 0.001f || Mathf.Abs(Sprint - 13.8f) > 0.001f) return false;
            if (SurfaceCount != 6) return false;
            bool left;
            if (FootDown(0.2f, 1.0f, out left)) return false;
            if (!FootDown(3.0f, 3.2f, out left)) return false;
            if (!FootDown(6.2f, 6.4f, out left)) return false;

            Puff walk = At(Surface.Grass, Walk, Kick.None);
            Puff sprint = At(Surface.Grass, Sprint, Kick.None);
            // Adopted grass: walk 0.08 / 0.24 / 4, sprint 0.11 / 0.31 / 5. Pale dust, not a green cloud.
            if (Mathf.Abs(walk.Size - 0.08f) > 0.004f || Mathf.Abs(walk.Opacity - 0.24f) > 0.015f || walk.Count != 4)
                return false;
            if (Mathf.Abs(sprint.Size - 0.11f) > 0.004f || Mathf.Abs(sprint.Opacity - 0.31f) > 0.015f || sprint.Count != 5)
                return false;
            if (sprint.Size <= walk.Size || sprint.Count <= walk.Count) return false;
            float walkVol = walk.Size * walk.Size * walk.Count * walk.Opacity;
            float sprintVol = sprint.Size * sprint.Size * sprint.Count * sprint.Opacity;
            if (sprintVol < walkVol * 2.8f || sprintVol > walkVol * 3.3f) return false;
            if (sprint.R < 0.75f || sprint.G < 0.70f || sprint.B < 0.55f) return false;
            if (sprint.G > sprint.R + 0.02f) return false;

            Puff dirt = At(Surface.Dirt, Sprint, Kick.None);
            Puff concrete = At(Surface.Concrete, Sprint, Kick.None);
            Puff wood = At(Surface.Wood, Sprint, Kick.None);
            Puff metal = At(Surface.Metal, Sprint, Kick.None);
            Puff sparks = At(Surface.Metal, Sprint, Kick.Pivot);
            Puff wet = At(Surface.Wet, Sprint, Kick.None);
            if (Mathf.Abs(dirt.Size - 0.335f) > 0.008f || Mathf.Abs(dirt.Opacity - 0.68f) > 0.02f || dirt.Count != 10)
                return false;
            if (Mathf.Abs(concrete.Size - 0.24f) > 0.008f || Mathf.Abs(concrete.Opacity - 0.44f) > 0.02f || concrete.Count != 5)
                return false;
            if (Mathf.Abs(wood.Size - 0.078f) > 0.006f || Mathf.Abs(wood.Opacity - 0.80f) > 0.02f || wood.Count != 8)
                return false;
            if (dirt.Opacity <= sprint.Opacity) return false;
            if (dirt.Life <= concrete.Life * 2f) return false;
            if (concrete.R < 0.75f || Mathf.Abs(concrete.R - concrete.G) > 0.05f) return false;
            if (wood.Size >= dirt.Size * 0.5f) return false;
            if (metal.Count != 0 || metal.Spark != 1) return false;
            if (sparks.Count < 2 || sparks.Spark != 1 || sparks.Size > 0.08f) return false;
            if (wet.Splash != 1 || wet.B <= wet.R) return false;
            if (dirt.R <= dirt.G) return false;

            Puff kicked = At(Surface.Grass, Sprint, Kick.RunStart);
            Puff slide = At(Surface.Dirt, Sprint, Kick.Slide);
            Puff trail = At(Surface.Dirt, Sprint, Kick.Trail);
            if (kicked.Size <= sprint.Size) return false;
            if (slide.Size <= trail.Size || trail.Size <= dirt.Size) return false;
            if (KickOf(1f, Walk, 0f, false) != Kick.RunStart) return false;
            if (KickOf(Sprint, Sprint, 0.8f, false) != Kick.Pivot) return false;
            if (KickOf(Sprint, Sprint, 0f, true) != Kick.Slide) return false;

            if (Classify("MEGA_grass", "Lawn", TagNone) != Surface.Grass) return false;
            if (Classify("sand", "Sand_East", TagNone) != Surface.Dirt) return false;
            if (Classify("mulch", "Mulch", TagNone) != Surface.Dirt) return false;
            if (Classify("concrete", null, TagNone) != Surface.Concrete) return false;
            if (Classify("asphalt", null, TagNone) != Surface.Concrete) return false;
            if (Classify("cedar", null, TagNone) != Surface.Wood) return false;
            if (Classify("MEGA_rail", "Fence_S", TagNone) != Surface.Metal) return false;
            if (Classify("wet", null, TagNone) != Surface.Wet) return false;
            if (Classify("concrete", "Slab", (int)Surface.Wet) != Surface.Wet) return false;
            if (Classify(null, null, TagNone) != Surface.Concrete) return false;
            if (Tag.Audio.FootstepMap.Classify("sand") != Tag.Audio.FootstepMap.Surface.Concrete) return false;
            if (TagArena.Movement.ChaseCam.FovPop != 0f || TagArena.Movement.ChaseCam.Shake != 0f
                || TagArena.Movement.ChaseCam.SlowMo != 0f)
                return false;
            return true;
        }

        public static string ProofLine()
        {
            Puff walk = At(Surface.Grass, Walk, Kick.None);
            Puff sprint = At(Surface.Grass, Sprint, Kick.None);
            Puff dirt = At(Surface.Dirt, Sprint, Kick.None);
            Puff concrete = At(Surface.Concrete, Sprint, Kick.None);
            Puff wood = At(Surface.Wood, Sprint, Kick.None);
            Puff metal = At(Surface.Metal, Sprint, Kick.None);
            Puff wet = At(Surface.Wet, Sprint, Kick.None);
            Puff kick = At(Surface.Grass, Sprint, Kick.RunStart);
            Puff slide = At(Surface.Dirt, Sprint, Kick.Slide);
            return "running-dust"
                + " walkOp=" + walk.Opacity.ToString("0.00")
                + " sprintOp=" + sprint.Opacity.ToString("0.00")
                + " dirtOp=" + dirt.Opacity.ToString("0.00")
                + " concreteLife=" + concrete.Life.ToString("0.00")
                + " woodSize=" + wood.Size.ToString("0.00")
                + " metal=" + metal.Count.ToString()
                + " wet=" + wet.Splash.ToString()
                + " kick=" + (kick.Size / sprint.Size).ToString("0.00")
                + " slide=" + (slide.Size / dirt.Size).ToString("0.00")
                + " plant=1"
                + " surfaces=" + SurfaceCount.ToString();
        }

        static bool NamedMetal(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return false;
            return objectName.StartsWith("Ship_", System.StringComparison.Ordinal)
                || objectName.StartsWith("Cat_", System.StringComparison.Ordinal)
                || objectName.StartsWith("Wh_", System.StringComparison.Ordinal)
                || objectName.StartsWith("Fence_", System.StringComparison.Ordinal);
        }

        static bool Has(string name, string part)
        {
            return name.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
