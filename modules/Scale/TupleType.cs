using Google.Protobuf;

namespace Scale;

public class TupleType
{
    public static ByteString From(params ByteString[] values)
    {
        using var memoryStream = new MemoryStream();
        foreach (var value in values)
        {
            var byteArray = value.ToByteArray();
            memoryStream.Write(byteArray, 0, byteArray.Length);
        }

        return ByteString.CopyFrom(memoryStream.ToArray());
    }
}