using System.Text;
using Google.Protobuf;

namespace Scale;

public class StringType : PrimitiveType<string>
{
    public override string TypeName => "string";
    
    public static explicit operator StringType(string p) => new(p);
    public static implicit operator string(StringType p) => p.Value;

    public StringType()
    {
    }

    public StringType(string value)
    {
        Create(value);
    }

    public override byte[] Encode()
    {
        return GetBytesFrom(Value);
    }

    public override void Decode(byte[] byteArray, ref int p)
    {
        var start = p;

        var value = String.Empty;

        var length = CompactIntegerType.Decode(byteArray, ref p);
        for (var i = 0; i < length; i++)
        {
            var t = new CharType();
            t.Decode(byteArray, ref p);
            value += t.Value;
        }

        TypeSize = p - start;

        var bytes = new byte[TypeSize];
        Array.Copy(byteArray, start, bytes, 0, TypeSize);

        Bytes = bytes;
        Value = value;
    }

    public override void Create(string value)
    {
        Value = value;
        Bytes = GetBytesFrom(value);
        TypeSize = Bytes.Length;
    }
    
    public static ByteString From(string value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(string value)
    {
        var list = Encoding.UTF8.GetBytes(value).ToList();
        var result = new List<byte>();
        result.AddRange(new CompactIntegerType(list.Count).Encode());
        result.AddRange(list);
        return result.ToArray();
    }
}