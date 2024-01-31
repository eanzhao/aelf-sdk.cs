using Google.Protobuf;
using Nethereum.ABI;

namespace AElf.Runtime.WebAssembly.Types;

public static class CommonExtension
{
    public static ByteString ToParameter(this ABIValue abiValue)
    {
        var bytes = new ABIEncode().GetABIEncoded(abiValue);
        return ByteString.CopyFrom(bytes);
    }

    public static ABIValue ToBytes32ABIValue(this byte[] bytes)
    {
        return new ABIValue("bytes32", bytes);
    }

    public static byte[] RightPad(this byte[] bytes, int length)
    {
        if (length <= bytes.Length)
            return bytes;

        var paddedBytes = new byte[length];
        Buffer.BlockCopy(bytes, 0, paddedBytes, 0, bytes.Length);
        return paddedBytes;
    }
}