using Google.Protobuf;
using Scale.Encoders;

namespace Scale;

public class StringType : ABIType
{
    public static ByteString From(string value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(string value)
    {
        return new StringTypeEncoder().Encode(value);
    }
}