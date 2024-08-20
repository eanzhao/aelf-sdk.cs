using System.Numerics;

namespace Scale.Encoders;

/// <summary>
/// Compact integer encoding.
/// </summary>
public class CompactIntegerTypeEncoder
{
    public byte[] Encode(ulong value)
    {
        switch (value)
        {
            case <= 0x3F:
                return [(byte)(value << 2)];
            case <= 0x3FFF:
                return
                [
                    (byte)((value << 2) | 0x01),
                    (byte)(value >> 6)
                ];
            case <= 0x3FFFFFFF:
                return
                [
                    (byte)((value << 2) | 0x02),
                    (byte)(value >> 6),
                    (byte)(value >> 14),
                    (byte)(value >> 22)
                ];
            default:
            {
                var bytes = BitConverter.GetBytes(value);
                var result = new byte[9];
                result[0] = 0x0b;
                Array.Copy(bytes, 0, result, 1, 8);
                return result;
            }
        }
    }

    public byte[] Encode(BigInteger value)
    {
        if (value <= 0x3F)
        {
            return [(byte)(value << 2)];
        }

        if (value <= 0x3FFF)
        {
            return
            [
                (byte)((value << 2) | 0x01),
                (byte)(value >> 6)
            ];
        }

        if (value <= 0x3FFFFFFF)
        {
            return
            [
                (byte)((value << 2) | 0x02),
                (byte)(value >> 6),
                (byte)(value >> 14),
                (byte)(value >> 22)
            ];
        }

        var bytes = value.ToByteArray();
        var result = new byte[1 + bytes.Length];
        result[0] = 0x0b;
        Array.Copy(bytes, 0, result, 1, bytes.Length);
        return result;
    }
}