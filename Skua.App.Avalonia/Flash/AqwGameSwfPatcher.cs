using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Skua.App.Avalonia.Flash;

internal static class AqwGameSwfPatcher
{
    internal static bool IsAqwGameSwf(string? path)
    {
        string file = Path.GetFileName((path ?? string.Empty).Split('?', '#')[0]);
        return file.StartsWith("Game", StringComparison.OrdinalIgnoreCase) && file.EndsWith(".swf", StringComparison.OrdinalIgnoreCase);
    }

    internal static byte[] PatchSharedObjectSecureFlag(byte[] swf, Action<string>? log = null)
    {
        if (swf.Length < 8 || swf[0] != (byte)'C' || swf[1] != (byte)'W' || swf[2] != (byte)'S')
            return swf;

        byte[] body;
        using (MemoryStream input = new(swf, 8, swf.Length - 8))
        using (ZLibStream zlib = new(input, CompressionMode.Decompress))
        using (MemoryStream output = new())
        {
            zlib.CopyTo(output);
            body = output.ToArray();
        }

        int patches = PatchDoAbcTags(body);
        if (patches == 0)
            return swf;

        using MemoryStream result = new();
        result.Write(swf, 0, 8);
        using (ZLibStream zlib = new(result, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(body);
        log?.Invoke($"proxy patched AQW secure SharedObject flag count={patches}");
        return result.ToArray();
    }

    private static int PatchDoAbcTags(byte[] body)
    {
        int nbits = body[0] >> 3;
        int offset = ((5 + nbits * 4 + 7) / 8) + 4;
        int patches = 0;

        while (offset + 2 <= body.Length)
        {
            ushort header = BitConverter.ToUInt16(body, offset);
            offset += 2;
            int tagCode = header >> 6;
            int length = header & 0x3f;
            if (length == 0x3f)
            {
                if (offset + 4 > body.Length)
                    break;
                length = BitConverter.ToInt32(body, offset);
                offset += 4;
            }

            int dataStart = offset;
            int dataEnd = offset + length;
            if (dataEnd > body.Length)
                break;

            if (tagCode == 82)
            {
                int nameEnd = Array.IndexOf(body, (byte)0, dataStart + 4);
                if (nameEnd > 0 && nameEnd < dataEnd)
                    patches += PatchSharedObjectSecureFlagInAbc(body, nameEnd + 1, dataEnd);
            }

            offset = dataEnd;
        }

        return patches;
    }

    private static int PatchSharedObjectSecureFlagInAbc(byte[] data, int abcStart, int abcEnd)
    {
        int offset = abcStart + 4;

        SkipU30Pool();
        SkipU30Pool();
        int doubleCount = ReadU30();
        offset += Math.Max(0, doubleCount - 1) * 8;

        List<int> aqwCharsIndexes = new();
        List<int> slashIndexes = new();
        int stringCount = ReadU30();
        for (int i = 1; i < stringCount; i++)
        {
            int len = ReadU30();
            string value = Encoding.UTF8.GetString(data, offset, len);
            if (value == "AQWChars")
                aqwCharsIndexes.Add(i);
            else if (value == "/")
                slashIndexes.Add(i);
            offset += len;
        }

        if (aqwCharsIndexes.Count == 0 || slashIndexes.Count == 0)
            return 0;

        int patches = 0;
        foreach (int aqwIndex in aqwCharsIndexes)
        {
            byte[] prefix = [0x2c, .. EncodeU30(aqwIndex)];
            foreach (int slashIndex in slashIndexes)
            {
                byte[] pattern = [.. prefix, 0x2c, .. EncodeU30(slashIndex), 0x26, 0x46];
                for (int i = abcStart; i <= abcEnd - pattern.Length; i++)
                {
                    if (!pattern.AsSpan().SequenceEqual(data.AsSpan(i, pattern.Length)))
                        continue;

                    data[i + pattern.Length - 2] = 0x27;
                    patches++;
                }
            }
        }

        return patches;

        int ReadU30()
        {
            int value = 0;
            int shift = 0;
            for (int i = 0; i < 5; i++)
            {
                byte b = data[offset++];
                value |= (b & 0x7f) << shift;
                if ((b & 0x80) == 0)
                    return value;
                shift += 7;
            }
            throw new InvalidDataException("Invalid ABC u30.");
        }

        void SkipU30Pool()
        {
            int count = ReadU30();
            for (int i = 1; i < count; i++)
                _ = ReadU30();
        }
    }

    private static byte[] EncodeU30(int value)
    {
        using MemoryStream ms = new();
        do
        {
            byte b = (byte)(value & 0x7f);
            value >>= 7;
            if (value != 0)
                b |= 0x80;
            ms.WriteByte(b);
        } while (value != 0);
        return ms.ToArray();
    }
}
