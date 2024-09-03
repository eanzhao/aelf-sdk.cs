using AElf;
using Google.Protobuf;

namespace Scale;

public class BoolType : PrimitiveType<bool>
{
    public override string TypeName => "bool";

    public override int TypeSize => 1;
    
    public BoolType(bool value)
    {
        Create(value);
    }

    public static explicit operator BoolType(bool v) => new(v);

    public static implicit operator ByteString(BoolType v) => From(v.Value);

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

    public override byte[] Encode()
    {
        return Bytes;
    }

    public override void Create(byte[] value)
    {
        Bytes = value;
        Value = value[0] > 0;
    }

    public override void Create(bool value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }
}