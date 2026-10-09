namespace Tag.FX
{
    /// <summary>
    /// Bangers comic words, one 1024 cell each. Built by Tools/BuildComicAtlas.py.
    /// The pixels are Assets/Art/FX/ComicAtlas.png. Png() reads that file.
    /// </summary>
    public static class ComicAtlas
    {
        public const int Cells = 4;
        public const int CellWidth = 1024;
        public const int CellHeight = 1024;

        public static byte[] Png()
        {
            return ComicPng.Read("ComicAtlas.png");
        }
    }
}
