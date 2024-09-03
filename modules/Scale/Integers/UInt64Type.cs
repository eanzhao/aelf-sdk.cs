using Google.Protobuf;

namespace Scale;

public class UInt64Type: PrimitiveType<ulong>
{
    public override string TypeName => "uint64";

    public override int TypeSize => 8;

    public static explicit operator UInt64Type(ulong v) => new(v);

    public static implicit operator ulong(UInt64Type v) => v.Value;
    public static implicit operator ByteString(UInt64Type v) => From(v.Value);

    public UInt64Type()
    {
    }

    public UInt64Type(ulong value)
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
        Value = BitConverter.ToUInt64(value, 0);
    }

    public override void Create(ulong value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public static ByteString From(ulong value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(ulong value)
    {
        var bytes = new byte[8];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }
}