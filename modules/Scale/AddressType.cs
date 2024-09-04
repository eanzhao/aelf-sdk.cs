using AElf.Types;
using Google.Protobuf;
using Scale.Encoders;

namespace Scale;

/// <summary>
/// For aelf address.
/// </summary>
public class AddressType
{
    public static ByteString GetByteStringFromBase58(string address)
    {
        return ByteString.CopyFrom(GetBytesFromBase58(address));
    }

    public static ByteString GetByteStringFrom(Address address)
    {
        return address.ToByteString();
    }

    public static byte[] GetBytesFromBase58(string address)
    {
        return new AddressTypeEncoder().Encode(address);
    }
}