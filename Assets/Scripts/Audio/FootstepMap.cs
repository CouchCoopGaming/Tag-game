namespace Tag.Audio
{
    /// <summary>
    /// Four surfaces, three gaits. Pitch, volume, and the gap between steps
    /// follow the gait. Each play also jitters pitch and volume.
    /// Mulch and grass share the soft step. Steel, catwalks, and containers share metal.
    /// Cedar, bark, and wood share the knock. Sand and concrete share the hard step.
    /// </summary>
    public static class FootstepMap
    {
        public enum Surface
        {
            Concrete = 0,
            Grass = 1,
            Metal = 2,
            Wood = 3
        }

        public enum Gait
        {
            Walk = 0,
            Run = 1,
            Sprint = 2
        }

        public const float PitchJitter = 0.045f;
        public const float VolumeJitter = 0.08f;
        public const int SurfaceCount = 4;

        static readonly string[] Files =
        {
            "SFX/sfx_step_concrete.wav",
            "SFX/sfx_step_grass.wav",
            "SFX/sfx_step_metal.wav",
            "SFX/sfx_step_wood.wav"
        };

        static readonly float[] Intervals = { 0.50f, 0.36f, 0.28f };
        static readonly float[] Pitches = { 0.92f, 1.00f, 1.10f };
        static readonly float[] Volumes = { 0.32f, 0.42f, 0.52f };

        public static string File(Surface surface)
        {
            int i = (int)surface;
            if (i < 0 || i >= Files.Length) i = 0;
            return Files[i];
        }

        public static float Interval(Gait gait)
        {
            return Intervals[Index(gait)];
        }

        public static float Pitch(Gait gait)
        {
            return Pitches[Index(gait)];
        }

        public static float Volume(Gait gait)
        {
            return Volumes[Index(gait)];
        }

        public static Gait FromSpeed(float speed)
        {
            if (speed >= 10.5f) return Gait.Sprint;
            if (speed >= 7.2f) return Gait.Run;
            return Gait.Walk;
        }

        public static Surface Classify(string name)
        {
            return Classify(name, null);
        }

        /// <summary>
        /// Material name picks the step. Shipping containers, catwalks, and the
        /// warehouse shell ring even when a story is painted concrete.
        /// </summary>
        public static Surface Classify(string material, string objectName)
        {
            if (NamedMetal(objectName)) return Surface.Metal;
            if (string.IsNullOrEmpty(material)) return Surface.Concrete;
            if (Has(material, "grass") || Has(material, "mulch") || Has(material, "field") || Has(material, "leaf"))
                return Surface.Grass;
            if (Has(material, "steel") || Has(material, "metal") || Has(material, "fence")
                || Has(material, "plate") || Has(material, "lamp")
                || Has(material, "army") || Has(material, "knight") || Has(material, "amber") || Has(material, "pad"))
                return Surface.Metal;
            if (Has(material, "wood") || Has(material, "cedar") || Has(material, "bark") || Has(material, "plank"))
                return Surface.Wood;
            return Surface.Concrete;
        }

        static bool NamedMetal(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return false;
            return objectName.StartsWith("Ship_", System.StringComparison.Ordinal)
                || objectName.StartsWith("Cat_", System.StringComparison.Ordinal)
                || objectName.StartsWith("Wh_", System.StringComparison.Ordinal);
        }

        static int Index(Gait gait)
        {
            int i = (int)gait;
            if (i < 0 || i >= Intervals.Length) return 0;
            return i;
        }

        static bool Has(string name, string part)
        {
            return name.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
