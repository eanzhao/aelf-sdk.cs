using System.Text;

namespace Scale;

public class CharType : PrimitiveType<char>
{
    public override string TypeName => "char";

    public override int TypeSize => 1;

    public static explicit operator CharType(char p) => new(p);
    public static implicit operator char(CharType p) => p.Value;

    public CharType()
    {
    }

    public CharType(char value)
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
        Value = Encoding.UTF8.GetString(value)[0];
    }

    public override void Create(char value)
    {
        Bytes = Encoding.UTF8.GetBytes(value.ToString());
        Value = value;
    }
}