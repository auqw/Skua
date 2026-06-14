using System.IO.Compression;
using Skua.App.Avalonia.Flash;
using Xunit;

namespace Skua.App.Avalonia.Tests;

public sealed class AqwGameSwfPatcherTests
{
    [Theory]
    [InlineData("/game/gamefiles/Game3096.swf", true)]
    [InlineData("/game/gamefiles/Game3096.swf?ver=R0039", true)]
    [InlineData("/game/gamefiles/title/Generic2.swf", false)]
    [InlineData("/game/gamefiles/Loader3.swf", false)]
    public void IsAqwGameSwf_OnlyMatchesGameSwfNames(string path, bool expected)
    {
        Assert.Equal(expected, AqwGameSwfPatcher.IsAqwGameSwf(path));
    }

    [Fact]
    public void PatchSharedObjectSecureFlag_ChangesSecureGetLocalToNonSecure()
    {
        byte[] swf = CreateCompressedSwfWithAqwSharedObjectCall();
        string? log = null;

        byte[] patched = AqwGameSwfPatcher.PatchSharedObjectSecureFlag(swf, message => log = message);
        byte[] body = DecompressBody(patched);

        Assert.NotSame(swf, patched);
        Assert.Contains("count=1", log);
        Assert.DoesNotContain(new byte[] { 0x2c, 0x01, 0x2c, 0x02, 0x26, 0x46 }, body);
        Assert.Contains(new byte[] { 0x2c, 0x01, 0x2c, 0x02, 0x27, 0x46 }, body);
    }

    [Fact]
    public void PatchSharedObjectSecureFlag_ReturnsOriginalWhenPatternMissing()
    {
        byte[] swf = CreateCompressedSwfWithAqwSharedObjectCall(pushTrue: false);

        byte[] patched = AqwGameSwfPatcher.PatchSharedObjectSecureFlag(swf);

        Assert.Same(swf, patched);
    }

    private static byte[] CreateCompressedSwfWithAqwSharedObjectCall(bool pushTrue = true)
    {
        byte[] abc =
        [
            0x00, 0x00, 0x2e, 0x00, // minor, major
            0x01, // int pool count
            0x01, // uint pool count
            0x01, // double pool count
            0x03, // string pool count
            0x08, (byte)'A', (byte)'Q', (byte)'W', (byte)'C', (byte)'h', (byte)'a', (byte)'r', (byte)'s',
            0x01, (byte)'/',
            0x2c, 0x01, // pushstring AQWChars
            0x2c, 0x02, // pushstring /
            pushTrue ? (byte)0x26 : (byte)0x27,
            0x46 // callproperty
        ];

        using MemoryStream body = new();
        body.WriteByte(0); // minimal RECT; patcher starts tags at byte 5
        body.Write(new byte[] { 0, 0, 1, 0 }); // frame rate + frame count
        using (MemoryStream doAbc = new())
        {
            doAbc.Write(new byte[4]); // DoABC flags
            doAbc.WriteByte(0); // empty ABC name
            doAbc.Write(abc);
            byte[] doAbcBytes = doAbc.ToArray();
            ushort header = (ushort)((82 << 6) | doAbcBytes.Length);
            body.WriteByte((byte)(header & 0xff));
            body.WriteByte((byte)(header >> 8));
            body.Write(doAbcBytes);
        }
        body.WriteByte(0); // End tag
        body.WriteByte(0);

        byte[] bodyBytes = body.ToArray();
        using MemoryStream swf = new();
        swf.Write(new byte[] { (byte)'C', (byte)'W', (byte)'S', 9 });
        swf.Write(BitConverter.GetBytes(bodyBytes.Length + 8));
        using (ZLibStream zlib = new(swf, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(bodyBytes);
        return swf.ToArray();
    }

    private static byte[] DecompressBody(byte[] swf)
    {
        using MemoryStream input = new(swf, 8, swf.Length - 8);
        using ZLibStream zlib = new(input, CompressionMode.Decompress);
        using MemoryStream output = new();
        zlib.CopyTo(output);
        return output.ToArray();
    }
}
