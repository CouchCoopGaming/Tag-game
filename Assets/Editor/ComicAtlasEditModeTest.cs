using NUnit.Framework;
using Tag.FX;

/// <summary>
/// EditMode. The comic sheets are base64 in source. They used to be one
/// concatenated literal, which overflowed csc. These checks keep the decoded
/// sheets the size the burst draws.
/// </summary>
public class ComicAtlasEditModeTest
{
    [Test]
    public void WordSheetIsPng()
    {
        Expect(ComicAtlas.Png(), ComicAtlas.Columns * ComicAtlas.CellWidth, ComicAtlas.Rows * ComicAtlas.CellHeight);
    }

    [Test]
    public void BurstSheetIsPng()
    {
        Expect(ComicBurstAtlas.Png(), ComicBurstAtlas.Columns * ComicBurstAtlas.CellWidth, ComicBurstAtlas.Rows * ComicBurstAtlas.CellHeight);
    }

    [Test]
    public void DizzyStarIsPng()
    {
        Expect(ComicDizzy.Png(), ComicDizzy.Size, ComicDizzy.Size);
    }

    static void Expect(byte[] png, int width, int height)
    {
        Assert.IsNotNull(png);
        Assert.GreaterOrEqual(png.Length, 24);
        Assert.AreEqual(0x89, png[0]);
        Assert.AreEqual((byte)'P', png[1]);
        Assert.AreEqual((byte)'N', png[2]);
        Assert.AreEqual((byte)'G', png[3]);
        int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        Assert.AreEqual(width, w);
        Assert.AreEqual(height, h);
    }
}
