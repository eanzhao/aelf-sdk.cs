namespace Scale;

public abstract class PrimitiveType<T> : BaseType
{
    public T Value { get; set; }

    protected PrimitiveType()
    {
    }

    protected PrimitiveType(T value)
    {
        Create(value);
    }

    public abstract void Create(T value);

    public override void Decode(byte[] byteArray, ref int p)
    {
        var memory = byteArray.AsMemory();
        var result = memory.Span.Slice(p, TypeSize).ToArray();
        p += TypeSize;
        Create(result);
    }

    public override string ToString() => Value.ToString();
}