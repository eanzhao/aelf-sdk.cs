using System.Numerics;
using Google.Protobuf;

namespace Scale;

public class Int128Type : PrimitiveType<BigInteger>
{
    public override string TypeName => "int128";

    public override int TypeSize => 16;

    public static explicit operator Int128Type(BigInteger v) => new(v);

    public static implicit operator BigInteger(Int128Type v) => v.Value;
    public static implicit operator ByteString(Int128Type v) => From(v.Value);

    public Int128Type()
    {
    }

    public Int128Type(BigInteger value)
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
        Value = new BigInteger(value);
    }

    public void Create(long value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public override void Create(BigInteger value)
    {
        Bytes = GetBytesFrom(value);
        Value = value;
    }

    public static ByteString From(long value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }
    
    public static ByteString From(BigInteger value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(long value)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return bytes;
    }

    public static byte[] GetBytesFrom(BigInteger value)
    {
        var byteArray = value.ToByteArray();
        if (byteArray.Length > 16)
        {
            throw new NotSupportedException("Exceeded the max size for int128.");
        }

        var bytes = new byte[16];
        byteArray.CopyTo(bytes, 0);
        return bytes;
    }
}