using AElf;
using Google.Protobuf;

namespace Scale;

public class EnumType<T> : PrimitiveType<T> where T : Enum
{
    public override string TypeName => typeof(T).Name;

    public override int TypeSize => 1;

    public static explicit operator EnumType<T>(T p) => new(p);

    public static implicit operator T(EnumType<T> p) => p.Value;

    public EnumType()
    {
    }

    public EnumType(T t)
    {
        Create(t);
    }

    public override byte[] Encode()
    {
        return Bytes;
    }

    public override void Decode(byte[] byteArray, ref int p)
    {
        var memory = byteArray.AsMemory();
        var result = memory.Span.Slice(p, TypeSize).ToArray();
        p += TypeSize;
        Create(result);
    }

    public override void Create(string value)
    {
        Create(ByteArrayHelper.HexStringToByteArray(value));
    }

    public override void Create(T value)
    {
        Bytes = [Convert.ToByte(value)];
        Value = value;
    }

    public override void Create(byte[] value)
    {
        Bytes = value;
        Value = (T)Enum.Parse(typeof(T), value[0].ToString(), true);
    }

    public static ByteString GetByteStringFrom(T value)
    {
        return ByteString.CopyFrom(GetBytesFrom(value));
    }

    public static byte[] GetBytesFrom(T value)
    {
        return [Convert.ToByte(value)];
    }

    public static T From(T value)
    {
        var instance = new EnumType<T>();
        instance.Create(value);
        return instance.Value;
    }
}