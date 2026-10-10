namespace Tag.Art
{
    /// <summary>
    /// Soft body foam. The primitive dummy and the FX seat tints both read this.
    /// </summary>
    public static class BodyFoam
    {
        public readonly struct Rgb
        {
            public readonly float R;
            public readonly float G;
            public readonly float B;

            public Rgb(float r, float g, float b)
            {
                R = r;
                G = g;
                B = b;
            }
        }

        public static readonly Rgb Blue = new Rgb(0.42f, 0.68f, 0.92f);
        public static readonly Rgb Mint = new Rgb(0.42f, 0.82f, 0.70f);
        public static readonly Rgb Orange = new Rgb(0.94f, 0.42f, 0.14f);
        public static readonly Rgb Lavender = new Rgb(0.70f, 0.58f, 0.88f);
        public static readonly Rgb Tan = new Rgb(0.90f, 0.76f, 0.52f);
        public static readonly Rgb Red = new Rgb(0.88f, 0.22f, 0.24f);

        /// <summary>Seat 0 red, 1 blue, 2 orange, 3 lavender.</summary>
        public static Rgb ForSeat(int seat)
        {
            if (seat == 1) return Blue;
            if (seat == 2) return Orange;
            if (seat == 3) return Lavender;
            return Red;
        }
    }
}
