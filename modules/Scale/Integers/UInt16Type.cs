using Google.Protobuf;

namespace Scale;

public class UInt16Type : PrimitiveType<ushort>
{
    public override string TypeName => "uint16";

    public override int TypeSize => 2;

    public static explicit operator UInt16Type(ushort v) => new(v);

    public static implicit operator ushort(UInt16Type v) => v.Value;
    public static implicit operator ByteString(UInt16Type v) => From(v.Value);

    public UInt16Type()
    {
    }

    public UInt16Type(ushort value)
    {
        Create(value);
    }

    public override byte[] Encode()
    {
        return Bytes;
    }

    public override void Create(byte[] value)
    {
        if (value.Length < TypeSize)
        {
            var newByteArray = new byte[TypeSize];
            value.CopyTo(newByteArray, 0);
            value = newByteArray;
        }

        Bytes = value;
        Value = BitConverter.ToUInt16(value, 0);
    }

    public override void Create(ushort value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public static ByteString From(ushort value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(ushort value)
    {
        var bytes = new byte[2];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }
}