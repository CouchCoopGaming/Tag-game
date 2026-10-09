namespace Tag.FX
{
    /// <summary>
    /// One comic dizzy star. Same burst, outline, and print dots as the words.
    /// Built by Tools/BuildComicAtlas.py. Not a fifth atlas cell.
    /// The pixels are Assets/Art/FX/ComicDizzy.png. Png() reads that file.
    /// </summary>
    public static class ComicDizzy
    {
        public const int Size = 512;

        public static byte[] Png()
        {
            return ComicPng.Read("ComicDizzy.png");
        }
    }
}
