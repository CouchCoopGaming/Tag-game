using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Foot dust numbers. Size, opacity, life, and count follow planar speed.
    /// The surface changes the color and how thick the puff is.
    /// A walk at 6.9 is a faint puff. A sprint at 13.8 is a clear cloud.
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
            float size = 0.07f + t * 0.20f;
            float opacity = 0.18f + t * 0.54f;
            float life = 0.18f + t * 0.20f;
            float count = 2f + t * 6f;
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

            float sizeMul = 1f;
            float opMul = 1f;
            float lifeMul = 1f;
            float countMul = 1f;
            if (surface == Surface.Grass)
            {
                sizeMul = 0.75f; opMul = 0.40f; lifeMul = 0.85f; countMul = 0.55f;
                puff.R = 0.40f; puff.G = 0.48f; puff.B = 0.18f;
            }
            else if (surface == Surface.Dirt)
            {
                sizeMul = 1.45f; opMul = 1.15f; lifeMul = 1.70f; countMul = 1.35f;
                puff.R = 0.76f; puff.G = 0.55f; puff.B = 0.30f;
            }
            else if (surface == Surface.Wood)
            {
                sizeMul = 0.38f; opMul = 0.32f; lifeMul = 0.60f; countMul = 0.40f;
                puff.R = 0.55f; puff.G = 0.42f; puff.B = 0.28f;
            }
            else if (surface == Surface.Wet)
            {
                sizeMul = 0.90f; opMul = 0.85f; lifeMul = 0.55f; countMul = 1.10f;
                puff.R = 0.12f; puff.G = 0.22f; puff.B = 0.40f;
                puff.Splash = 1;
            }
            else
            {
                sizeMul = 0.55f; opMul = 0.50f; lifeMul = 0.42f; countMul = 0.50f;
                puff.R = 0.82f; puff.G = 0.82f; puff.B = 0.80f;
            }

            puff.Size = size * sizeMul;
            puff.Opacity = opacity * opMul;
            if (puff.Opacity > 0.95f) puff.Opacity = 0.95f;
            puff.Life = life * lifeMul;
            count *= countMul;
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
            if (walk.Opacity > 0.12f || sprint.Opacity < 0.22f) return false;
            if (sprint.Opacity < walk.Opacity * 2f) return false;
            if (sprint.Size <= walk.Size) return false;
            if (sprint.Count <= walk.Count) return false;

            Puff dirt = At(Surface.Dirt, Sprint, Kick.None);
            Puff concrete = At(Surface.Concrete, Sprint, Kick.None);
            Puff wood = At(Surface.Wood, Sprint, Kick.None);
            Puff metal = At(Surface.Metal, Sprint, Kick.None);
            Puff sparks = At(Surface.Metal, Sprint, Kick.Pivot);
            Puff wet = At(Surface.Wet, Sprint, Kick.None);
            if (dirt.Opacity <= sprint.Opacity) return false;
            if (dirt.Life <= concrete.Life * 2f) return false;
            if (concrete.R < 0.75f || Mathf.Abs(concrete.R - concrete.G) > 0.05f) return false;
            if (wood.Size >= dirt.Size * 0.5f) return false;
            if (wood.Opacity > 0.35f) return false;
            if (metal.Count != 0 || metal.Spark != 1) return false;
            if (sparks.Count < 2 || sparks.Spark != 1 || sparks.Size > 0.08f) return false;
            if (wet.Splash != 1 || wet.B <= wet.R) return false;
            if (sprint.G <= sprint.R) return false;
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
