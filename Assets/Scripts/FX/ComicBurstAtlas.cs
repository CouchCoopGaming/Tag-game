namespace Tag.FX
{
    /// <summary>
    /// Comic burst layer, one cell per event. Built by Tools/Tag/bake_comic_layers.py.
    /// The pixels are Assets/Art/FX/ComicBurstAtlas.png. Png() reads that file.
    /// </summary>
    public static class ComicBurstAtlas
    {
        public const int Cells = 10;
        public const int Columns = 5;
        public const int Rows = 2;
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
            return ComicPng.Read("ComicBurstAtlas.png");
        }
    }
}
