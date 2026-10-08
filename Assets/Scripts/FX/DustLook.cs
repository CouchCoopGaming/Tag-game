using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Foot dust numbers. Size, opacity, life, and count follow planar speed.
    /// The surface changes the color and how thick the puff is.
    /// Walk is a tiny heel puff. Run is a clear cloud. Sprint is a plume
    /// that kicks back, rises a little, and lingers. These sizes are visual
    /// FX, not a feel lock. Run start, a hard pivot, and the first slide
    /// frame kick harder. Metal stays quiet except a few sparks on a hard
    /// pivot. Wet throws dark droplets. Nothing here changes speed or slide.
    /// </summary>
    public static class DustLook
    {
        public const float Walk = 6.9f;
        public const float Sprint = 13.8f;
        public const float Pi = 3.14159265f;
        public const int SurfaceCount = 6;
        public const int TagNone = -1;
        /// <summary>Seconds between slide-trail puffs. Short enough that the trail reads as one streak.</summary>
        public const float TrailGap = 0.045f;

        public enum Surface
        {
            Grass = 0,
            Dirt = 1,
            Concrete = 2,
            Wood = 3,
            Metal = 4,
            Wet = 5,
            // Extra tint. SurfaceCount stays 6 so the running-dust proof line does not change.
            // Kind 6 is brick. Physics friction stays on the concrete material.
            Brick = 6
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
            /// <summary>Overall cloud length, metres. The mote diameter is Size.</summary>
            public float Span;
            /// <summary>How far the cloud rises off the foot, metres.</summary>
            public float Lift;
            /// <summary>How far the cloud is kicked back behind the foot, metres.</summary>
            public float Back;
            /// <summary>0 hides the grit. 1 draws a darker core inside the plume.</summary>
            public float Core;
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
            if (kind == (int)Surface.Brick) return Surface.Brick;
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
        /// Brick is the material name, the object name, or kind 6. It is not concrete.
        /// </summary>
        public static Surface Classify(string material, string objectName, int tag)
        {
            if (tag == (int)Surface.Brick) return Surface.Brick;
            if (tag >= 0 && tag < SurfaceCount) return (Surface)tag;
            if (Has(material, "brick") || Has(objectName, "brick")
                || Has(material, "masonry") || Has(objectName, "masonry")
                || Has(material, "mortar") || Has(objectName, "mortar"))
                return Surface.Brick;
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
            if (surface == (int)Surface.Brick)
                return At(Surface.Brick, speed, (Kick)kick);
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
                puff.Span = 0.05f;
                if (kick == Kick.Pivot)
                {
                    puff.Count = 3;
                    puff.Opacity = 0.85f;
                }
                return puff;
            }

            // 0 at a walk, 0.42 at a run (9 m/s), 1 at a sprint.
            // The old linear blend left run and sprint the same smudge.
            float u = SpeedU(speed);
            float t = 0f;
            if (Sprint > Walk)
                t = (speed - Walk) / (Sprint - Walk);
            if (t < 0f) t = 0f;
            if (t > 1.35f) t = 1.35f;
            float life;
            float size;
            float opacity;
            float count;
            float span;
            float lift;
            float back;
            if (surface == Surface.Wet)
            {
                // Wet was not in the speed table. It keeps the previous curve.
                size = (0.07f + t * 0.20f) * 0.90f;
                opacity = (0.18f + t * 0.54f) * 0.85f;
                count = (2f + t * 6f) * 1.10f;
                life = (0.18f + t * 0.20f) * 0.55f;
                span = size * 2f;
                lift = 0.08f;
                back = span * 0.30f;
                puff.R = 0.12f;
                puff.G = 0.22f;
                puff.B = 0.40f;
                puff.Splash = 1;
                puff.Core = 0f;
            }
            else
            {
                // Pass 14. Walk is u = 0, sprint is u = 1, run (9 m/s) is u = 0.42.
                // Pass 12 endpoints, too close to read (concrete 0.18/0.32/4 → 0.24/0.44/5):
                //   grass    0.08/0.24/4 → 0.11/0.31/5   colour (0.80, 0.76, 0.62)
                //   dirt     0.24/0.62/7 → 0.335/0.68/10 colour (0.84, 0.58, 0.30)
                //   wood     0.055/0.70/6 → 0.078/0.80/8 colour (1, 0.97, 0.88)
                //   concrete 0.18/0.32/4 → 0.24/0.44/5   colour (0.96, 0.96, 0.94)
                // Older curve, fainter still: size 0.07+t*0.20, opacity 0.18+t*0.54, count 2+t*6.
                float size0, size1, op0, op1, count0, count1, life0, life1, span0, span1, lift1;
                if (surface == Surface.Grass)
                {
                    size0 = 0.04f; size1 = 0.13f; op0 = 0.20f; op1 = 0.50f; count0 = 2f; count1 = 6f;
                    life0 = 0.14f; life1 = 0.42f; span0 = 0.07f; span1 = 0.48f; lift1 = 0.10f;
                    puff.R = 0.76f; puff.G = 0.77f; puff.B = 0.70f;
                }
                else if (surface == Surface.Dirt)
                {
                    size0 = 0.12f; size1 = 0.34f; op0 = 0.55f; op1 = 0.88f; count0 = 5f; count1 = 11f;
                    life0 = 0.22f; life1 = 0.58f; span0 = 0.18f; span1 = 0.90f; lift1 = 0.22f;
                    puff.R = 0.68f; puff.G = 0.46f; puff.B = 0.24f;
                }
                else if (surface == Surface.Wood)
                {
                    size0 = 0.04f; size1 = 0.10f; op0 = 0.45f; op1 = 0.78f; count0 = 3f; count1 = 8f;
                    life0 = 0.14f; life1 = 0.40f; span0 = 0.06f; span1 = 0.38f; lift1 = 0.08f;
                    puff.R = 0.90f; puff.G = 0.76f; puff.B = 0.56f;
                }
                else if (surface == Surface.Brick)
                {
                    // Same body as concrete. The tint is the brick, not the grey dust.
                    size0 = 0.05f; size1 = 0.20f; op0 = 0.28f; op1 = 0.82f; count0 = 2f; count1 = 9f;
                    life0 = 0.16f; life1 = 0.50f; span0 = 0.10f; span1 = 0.72f; lift1 = 0.16f;
                    puff.R = 0.62f; puff.G = 0.28f; puff.B = 0.16f;
                }
                else
                {
                    size0 = 0.05f; size1 = 0.20f; op0 = 0.28f; op1 = 0.82f; count0 = 2f; count1 = 9f;
                    life0 = 0.16f; life1 = 0.50f; span0 = 0.10f; span1 = 0.72f; lift1 = 0.16f;
                    puff.R = 0.78f; puff.G = 0.78f; puff.B = 0.76f;
                }
                size = size0 + (size1 - size0) * u;
                opacity = op0 + (op1 - op0) * u;
                count = count0 + (count1 - count0) * u;
                life = life0 + (life1 - life0) * u;
                span = span0 + (span1 - span0) * u;
                lift = 0.02f + (lift1 - 0.02f) * u;
                back = span * (0.35f + 0.45f * u);
                puff.Core = u;
            }
            if (speed < Walk)
            {
                float slow = Walk > 0.01f ? speed / Walk : 0f;
                if (slow < 0f) slow = 0f;
                size *= slow;
                opacity *= slow;
                count *= slow;
                span *= slow;
                back *= slow;
            }
            if (speed < 2.2f) count = 0f;

            float mul = 1f;
            if (kick == Kick.RunStart) mul = 1.85f;
            else if (kick == Kick.Pivot) mul = 1.70f;
            else if (kick == Kick.Slide) mul = 2.10f;
            else if (kick == Kick.Trail) mul = 1.25f;
            size *= mul;
            span *= mul;
            back *= mul;
            opacity *= mul;
            if (opacity > 0.95f) opacity = 0.95f;

            puff.Size = size;
            puff.Opacity = opacity;
            puff.Life = life;
            puff.Span = span;
            puff.Lift = lift;
            puff.Back = back;
            int n = (int)(count + 0.5f);
            if (n > 12) n = 12;
            if (n < 0) n = 0;
            puff.Count = n;
            return puff;
        }

        /// <summary>
        /// 0 at a walk, 0.42 at 9 m/s, 1 at a sprint. Above the sprint it holds at 1.
        /// </summary>
        public static float SpeedU(float speed)
        {
            const float run = 9f;
            const float atRun = 0.42f;
            if (speed <= Walk) return 0f;
            if (speed >= Sprint) return 1f;
            if (speed <= run)
                return (speed - Walk) / (run - Walk) * atRun;
            return atRun + (speed - run) / (Sprint - run) * (1f - atRun);
        }

        /// <summary>
        /// Radial land dust. Radius follows the same fall-speed curve as the land ring.
        /// The tier swell (0.55 + 0.45 × tier) is applied by the caller to Span.
        /// </summary>
        public static Puff LandDust(int surface, float impact)
        {
            Puff puff = At(surface, Sprint, (int)Kick.None);
            float gate = Tag.Art.LandingRollPose.Threshold;
            if (gate < 0.01f) gate = 0.01f;
            float t = impact / gate;
            if (t < 0f) t = 0f;
            if (t > 1.35f) t = 1.35f;
            puff.Span = 0.45f + t * 0.85f;
            puff.Size = 0.06f + t * 0.10f;
            puff.Opacity = 0.45f + t * 0.35f;
            if (puff.Opacity > 0.90f) puff.Opacity = 0.90f;
            puff.Life = 0.36f + t * 0.14f;
            int n = 8 + (int)(t * 8f);
            if (n > 16) n = 16;
            puff.Count = n;
            puff.Lift = 0.05f + t * 0.07f;
            puff.Back = 0f;
            puff.Core = t > 0.45f ? 0.65f : 0.2f;
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
            // Grass stays pale. Walk is a small puff, sprint is a longer one.
            if (Mathf.Abs(walk.Size - 0.04f) > 0.004f || Mathf.Abs(walk.Opacity - 0.20f) > 0.015f || walk.Count != 2)
                return false;
            if (Mathf.Abs(sprint.Size - 0.13f) > 0.004f || Mathf.Abs(sprint.Opacity - 0.50f) > 0.015f || sprint.Count != 6)
                return false;
            if (sprint.Size <= walk.Size || sprint.Count <= walk.Count) return false;
            if (sprint.Span <= walk.Span * 3f) return false;
            if (sprint.R < 0.75f || sprint.G < 0.70f || sprint.B < 0.55f) return false;
            if (sprint.G > sprint.R + 0.02f) return false;

            Puff dirt = At(Surface.Dirt, Sprint, Kick.None);
            Puff concrete = At(Surface.Concrete, Sprint, Kick.None);
            Puff concreteWalk = At(Surface.Concrete, Walk, Kick.None);
            Puff concreteRun = At(Surface.Concrete, 9f, Kick.None);
            Puff wood = At(Surface.Wood, Sprint, Kick.None);
            Puff metal = At(Surface.Metal, Sprint, Kick.None);
            Puff sparks = At(Surface.Metal, Sprint, Kick.Pivot);
            Puff wet = At(Surface.Wet, Sprint, Kick.None);
            if (Mathf.Abs(dirt.Size - 0.34f) > 0.008f || Mathf.Abs(dirt.Opacity - 0.88f) > 0.02f || dirt.Count != 11)
                return false;
            if (Mathf.Abs(concrete.Size - 0.20f) > 0.008f || Mathf.Abs(concrete.Opacity - 0.82f) > 0.02f || concrete.Count != 9)
                return false;
            if (Mathf.Abs(concrete.Span - 0.72f) > 0.02f || concrete.Life < 0.40f || concrete.Life > 0.60f) return false;
            if (concreteWalk.Span > 0.14f || concreteWalk.Count > 3) return false;
            if (concreteRun.Span < 0.30f || concreteRun.Span > 0.40f) return false;
            if (concrete.Back <= concreteRun.Back || concrete.Lift < 0.10f) return false;
            if (concrete.Core < 0.9f || concreteWalk.Core > 0.05f) return false;
            if (Mathf.Abs(wood.Size - 0.10f) > 0.006f || Mathf.Abs(wood.Opacity - 0.78f) > 0.02f || wood.Count != 8)
                return false;
            if (dirt.Opacity <= sprint.Opacity) return false;
            if (dirt.Life <= concrete.Life) return false;
            if (concrete.R < 0.75f || Mathf.Abs(concrete.R - concrete.G) > 0.05f) return false;
            if (wood.Size >= dirt.Size * 0.5f) return false;
            Puff landLight = LandDust((int)Surface.Concrete, 8f);
            Puff landHard = LandDust((int)Surface.Concrete, Tag.Art.LandingRollPose.Threshold);
            if (landHard.Span <= landLight.Span || landHard.Count <= landLight.Count) return false;
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
            if (Classify("Brick_Red", "Wall", TagNone) != Surface.Brick) return false;
            if (Classify(null, "masonry_01", TagNone) != Surface.Brick) return false;
            if (Classify("asphalt", "Road", (int)Surface.Brick) != Surface.Brick) return false;
            Puff brick = At(Surface.Brick, Sprint, Kick.None);
            if (brick.R <= brick.G + 0.2f || brick.B >= brick.R) return false;
            if (Mathf.Abs(brick.R - concrete.R) < 0.12f) return false;
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
            Puff concreteWalk = At(Surface.Concrete, Walk, Kick.None);
            Puff concreteRun = At(Surface.Concrete, 9f, Kick.None);
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
                + " surfaces=" + SurfaceCount.ToString()
                + " conc=" + concreteWalk.Span.ToString("0.00")
                + "/" + concreteRun.Span.ToString("0.00")
                + "/" + concrete.Span.ToString("0.00")
                + " back=" + concrete.Back.ToString("0.00")
                + " lift=" + concrete.Lift.ToString("0.00");
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
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(part)) return false;
            return name.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
