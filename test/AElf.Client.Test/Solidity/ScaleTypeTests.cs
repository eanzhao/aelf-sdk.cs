using System.Numerics;
using Google.Protobuf;
using Scale;
using Shouldly;

namespace AElf.Client.Test.Solidity;

public class ScaleTypeTests
{
    [Fact]
    public void BooleanTypeTest()
    {
        BoolType.From("0x00").Value.ShouldBeFalse();
        BoolType.From("0x01").Value.ShouldBeTrue();

        BoolType.From(true).ToString().ShouldBe("True");
        BoolType.From(false).ToString().ShouldBe("False");
        
        BoolType.From(true).ByteStringValue.ToHex(true).ShouldBe("0x01");
    }

    [Fact]
    public void IntegerTypesTest()
    {
        UIntType.GetByteStringFrom(16).ToByteArray().ToHex(true).ShouldStartWith("0x10");

        UIntType.From("0x10").Value.ShouldBe(16);
        UIntType.From(10000).Value.ShouldBe(10000);

        UIntType.From("0x10").ToString().ShouldBe("16");
        UIntType.From(10000).ToString().ShouldBe("10000");
    }

    [Fact]
    public void BytesTypeTest()
    {
        BytesType.From("0x1234").TypeName.ShouldBe("bytes2");
    }
    
    private static bool ParseBoolType(byte[] byteArray)
    {
        return BoolType.From(byteArray).Value;
    }
    
    private static BigInteger ParseUIntType(byte[] byteArray)
    {
        return UIntType.From(byteArray).Value;
    }
    
    private static uint ParseUInt32Type(byte[] byteArray)
    {
        return UInt32Type.From(byteArray).Value;
    }
}