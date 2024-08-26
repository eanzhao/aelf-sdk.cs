using Scale.Core;

namespace Scale;

/// <summary>
/// Ref: https://solang.readthedocs.io/en/v0.3.3/language/types.html
/// </summary>
public abstract class ABIType
{
    private static readonly Dictionary<string, Func<ABIType>> TypeMapping = new()
    {
        { "uint", () => new IntegerType() },
        { "tuple", () => new TupleType() },
        { "bool", () => new BooleanType() },
        { "address", () => new AddressType() },
        { "string", () => new StringType() },
    };
    
    protected ITypeDecoder Decoder { get; set; }
    protected ITypeEncoder Encoder { get; set; }
}