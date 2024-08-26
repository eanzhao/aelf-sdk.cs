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
    Task<string> SendWithoutResultAsync(string selector, ByteString? parameter = null,
        Weight? gasLimit = null, long value = 0);
    Task<string> GenerateRawTransaction(string selector, ByteString? parameter = null, string from = null,
        Weight? gasLimit = null, long value = 0);
    Task<List<string>?> SendMultiTransactions(List<string> rawTransactions);
    Task<byte[]> CallAsync(string selector, ByteString parameter);

    Task<SendTransactionResult> EstimateFeeAsync(string selector, ByteString? parameter = null);
}