namespace Tag.FX
{
    /// <summary>
    /// Bangers comic words. Built by Tools/Tag/bake_comic_layers.py. Burst is separate.
    /// The pixels are Assets/Art/FX/ComicAtlas.png. Png() reads that file.
    /// </summary>
    public static class ComicAtlas
    {
        public const int Cells = 36;
        public const int Columns = 6;
        public const int Rows = 6;
        public const int CellWidth = 512;
        public const int CellHeight = 512;

        public static void Uv(int index, out float scaleX, out float scaleY, out float offX, out float offY)
        {
            if (index < 0) index = 0;
            int col = index % Columns;
            int row = index / Columns;
            float du = 1f / Columns;
            float dv = 1f / Rows;
            float g = 1f / (Columns * CellWidth);
            scaleX = du - g * 2f;
            scaleY = dv - g * 2f;
            offX = col * du + g;
            offY = 1f - (row + 1f) * dv + g;
        }

        public static byte[] Png()
        {
            return ComicPng.Read("ComicAtlas.png");
        }
    }
}
