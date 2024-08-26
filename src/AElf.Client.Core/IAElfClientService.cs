using AElf.Client.Dto;
using Google.Protobuf;

namespace AElf.Client.Core;

public interface IAElfClientService
{
    Task<byte[]> ViewAsync(string contractAddress, string methodName, IMessage parameter, string clientAlias,
        string accountAlias = "Default");

    Task<byte[]> ViewSystemAsync(string systemContractName, string methodName, IMessage parameter,
        string clientAliasOrEndpoint, string accountAlias = "Default");

    Task<Transaction> SendAsync(string contractAddress, string methodName, IMessage parameter,
        string clientAlias, string? accountAlias = null, string? accountAddress = null);

    Task<List<string>?> SendTransactionsAsync(string clientAliasOrEndpoint, List<string> rawTransactions);
    Task<string> GenerateRawTransaction(string contractAddress, string methodName, IMessage parameter,
        string clientAliasOrEndpoint, string? address = null, string? alias = null);
    Task<Transaction> SendSystemAsync(string systemContractName, string methodName, IMessage parameter,
        string clientAlias, string? accountAlias = null, string? accountAddress = null);

    Task<TransactionResult> GetTransactionResultAsync(string transactionId, string clientAlias);

    Task<ChainStatusDto> GetChainStatusAsync(string clientAlias);

    Task<MerklePath> GetMerklePathByTransactionIdAsync(string transactionId, string clientAlias);

    Task<long> EstimateGasFeeAsync(Transaction transaction);
}