using AElf.Client.Core;
using AElf.Runtime.WebAssembly;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Client.Solidity;

public interface ISolidityContractService
{
    Task<long> EstimateGasFeeAsync(Transaction transaction);
    Task<SendTransactionResult> SendAsync(string selector,SmartContractRegistration registration, ByteString? parameter = null,
        int gasLimit = 0, long value = 0);
    Task<TransactionResult> CheckResult(string txId);
    Task<string> SendWithoutResultAsync(string methodName, SmartContractRegistration registration, ByteString? parameter = null,
        int gasLimit = 0, long value = 0);
    Task<string> GenerateRawTransaction(string selector, ByteString? parameter = null, string from = null,
        int gasLimit = 0, long value = 0);
    Task<List<string>?> SendMultiTransactions(List<string> rawTransactions);
    Task<byte[]> CallAsync(string methodName, SmartContractRegistration registration, ByteString? parameter = null,
    int gasLimit = 0, long value = 0);

    Task<SendTransactionResult> EstimateFeeAsync(string selector, ByteString? parameter = null);
}