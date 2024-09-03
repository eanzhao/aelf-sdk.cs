using Google.Protobuf;

namespace Scale;

public class UInt8Type : PrimitiveType<byte>
{
    public override string TypeName => "uint8";

    public override int TypeSize => 1;
    
    public static explicit operator UInt8Type(byte v) => new(v);

    public static implicit operator byte(UInt8Type v) => v.Value;
    public static implicit operator ByteString(UInt8Type v) => From(v.Value);

    public UInt8Type()
    {
    }

    public UInt8Type(byte value)
    {
        Create(value);
    }

    public override byte[] Encode()
    {
        return Bytes;
    }

    public override void Create(byte[] value)
    {
        Bytes = value;
        Value = value[0];
    }

    public override void Create(byte value)
    {
        Bytes = [value];
        Value = value;
    }

    public static ByteString From(byte value)
    {
        return ByteString.CopyFrom(value);
    }
}