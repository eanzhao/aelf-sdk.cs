using System.Numerics;
using Google.Protobuf;
using Scale.Encoders;

namespace Scale;

public class IntegerType : ABIType
{
    public static ByteString From(BigInteger value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(BigInteger value)
    {
        return new IntegerTypeEncoder().Encode(value);
    }
}