using AElf.Client.Core;
using AElf.Runtime.WebAssembly;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Client.Solidity;

public interface ISolidityContractService
{
    Task<long> EstimateGasFeeAsync(Transaction transaction);

    Task<SendTransactionResult> SendAsync(string selector, ByteString? parameter = null,
        Weight? gasLimit = null, long value = 0);

    Task<byte[]> CallAsync(string selector, ByteString parameter);
}