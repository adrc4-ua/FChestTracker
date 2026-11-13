namespace FCT.Analysis;

/// <summary>
/// Peeks at the start of a .replay file (Unreal's local file header) without parsing the match: whether it is still
/// being recorded and whether its encryption key has been written. Opened with full sharing, since Fortnite keeps
/// writing the file during the match.
/// </summary>
public static class ReplayHeaderProbe
{
    /// <summary>
    /// True when the data cannot be read yet: an encrypted replay whose key is still all zeros (Fortnite writes the
    /// real key when the match ends). Also true for anything that does not look like a replay header.
    /// </summary>
    public static bool KeyMissing(string path)
    {
        var header = Read(path, throughKey: true);
        return header is not { } h || (h.Encrypted && h.KeyIsBlank);
    }

    /// <summary>The header's "live" flag; null when the file is not a readable replay.</summary>
    public static bool? IsLive(string path) => Read(path, throughKey: false)?.Live;

    private const uint Magic = 0x1CA2E27F;

    private readonly record struct Header(bool Live, bool Encrypted, bool KeyIsBlank);

    /// <summary>Walks the header; stops after the live flag unless the key is wanted. Null on any inconsistency.</summary>
    private static Header? Read(string path, bool throughKey)
    {
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new BinaryReader(file);
            if (reader.ReadUInt32() != Magic)
            {
                return null;
            }
            if (reader.ReadUInt32() >= 7) // file version 7 added a table of 20-byte custom versions
            {
                var customVersions = reader.ReadInt32();
                if (customVersions is < 0 or > 64)
                {
                    return null;
                }
                file.Seek(20L * customVersions, SeekOrigin.Current);
            }
            file.Seek(3 * sizeof(uint), SeekOrigin.Current); // length in ms, network version, changelist
            SkipFString(reader);

            var live = reader.ReadUInt32() != 0;
            if (!throughKey)
            {
                return new Header(live, false, false);
            }
            file.Seek(sizeof(long) + sizeof(uint), SeekOrigin.Current); // timestamp, compressed flag
            if (reader.ReadUInt32() == 0)
            {
                return new Header(live, Encrypted: false, KeyIsBlank: false);
            }
            var keyLength = reader.ReadInt32();
            if (keyLength is <= 0 or > 256)
            {
                return null;
            }
            var key = reader.ReadBytes(keyLength);
            return new Header(live, Encrypted: true, KeyIsBlank: Array.TrueForAll(key, b => b == 0));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Unreal string: a length, negative for UTF-16 (two bytes per character), then the characters.</summary>
    private static void SkipFString(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        reader.BaseStream.Seek(length < 0 ? -2L * length : length, SeekOrigin.Current);
    }
}
