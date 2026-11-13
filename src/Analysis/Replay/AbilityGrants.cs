namespace FCT.Analysis;

/// <summary>
/// Reads the raw "ActivatableAbilities" payload of a player's ability system (a FastArray delta whose field names are
/// not exported, so the library cannot decode it). Each changed element is an ability spec; when one of its object
/// references is a treasure handler class, the squad has just received a treasure.
/// </summary>
internal static class AbilityGrants
{
    private const int MaxElements = 512;

    /// <summary>The treasure handlers among the changed elements: (element id, handler name).</summary>
    public static List<(int Element, string Handler)> Find(byte[] data, int bitCount, IReadOnlyDictionary<uint, string> handlerIds)
    {
        var found = new List<(int, string)>();
        var bits = new Bits(data, bitCount) { Position = 1 }; // first bit: delta struct serialization flag
        try
        {
            bits.Skip(64); // two replication keys
            var removed = bits.Read(32);
            var changed = bits.Read(32);
            if (removed > MaxElements || changed > MaxElements || bitCount < 129)
            {
                return found;
            }
            bits.Skip((int)removed * 32);
            for (var n = 0; n < changed; n++)
            {
                var element = (int)bits.Read(32);
                for (var handle = bits.ReadPacked(); handle != 0; handle = bits.ReadPacked())
                {
                    var size = (int)bits.ReadPacked();
                    if (size < 0 || bits.Position + size > bitCount)
                    {
                        return found;
                    }
                    var end = bits.Position + size;
                    // A packed object reference takes one to five bytes; other properties are skipped whole.
                    if (size % 8 == 0 && size is >= 8 and <= 40 && handlerIds.TryGetValue(bits.ReadPackedWithin(end), out var handler))
                    {
                        found.Add((element, handler));
                    }
                    bits.Position = end;
                }
            }
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or InvalidDataException)
        {
            // not a FastArray delta of this layout: nothing to take from it
        }
        return found;
    }

    /// <summary>Little-endian bit reader over an Unreal payload.</summary>
    private sealed class Bits(byte[] data, int count)
    {
        public int Position { get; set; }

        private int Bit()
        {
            if (Position >= count)
            {
                throw new IndexOutOfRangeException();
            }
            var value = (data[Position >> 3] >> (Position & 7)) & 1;
            Position++;
            return value;
        }

        public void Skip(int bits) => Position += bits;

        public uint Read(int width)
        {
            uint value = 0;
            for (var i = 0; i < width; i++)
            {
                value |= (uint)Bit() << i;
            }
            return value;
        }

        /// <summary>Unreal "packed int": 7 bits per byte, low bit set while more bytes follow (at most five bytes).</summary>
        public uint ReadPacked()
        {
            uint value = 0;
            for (var shift = 0; shift < 35; shift += 7)
            {
                var part = Read(8);
                value |= (part >> 1) << shift;
                if ((part & 1) == 0)
                {
                    return value;
                }
            }
            throw new InvalidDataException();
        }

        /// <summary>A packed int that must end before <paramref name="end"/> (it stops there if it does not).</summary>
        public uint ReadPackedWithin(int end)
        {
            uint value = 0;
            for (var shift = 0; Position < end; shift += 7)
            {
                var part = Read(8);
                value |= (part >> 1) << shift;
                if ((part & 1) == 0)
                {
                    break;
                }
            }
            return value;
        }
    }
}
