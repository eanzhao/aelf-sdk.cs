using AElf;
using Google.Protobuf;

namespace Scale;

public class BooleanType : ABIType
{
    public static ByteString True => ByteString.CopyFrom(0x01);
    public static ByteString False => ByteString.CopyFrom(0x00);

    public static ByteString From(bool value)
    {
        return value ? True : False;
    }

    public static byte[] GetBytesFrom(bool value)
    {
        return value ? [0x01] : [0x00];
    }
}