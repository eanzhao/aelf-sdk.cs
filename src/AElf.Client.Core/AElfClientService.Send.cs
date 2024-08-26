using AElf.Client.Dto;
using Castle.Core.Logging;
using Google.Protobuf;

namespace AElf.Client.Core;

public partial class AElfClientService
{
    public async Task<Transaction> SendAsync(string contractAddress, string methodName, IMessage parameter,
        string clientAliasOrEndpoint, string? alias = null, string? address = null)
    {
        var aelfClient = clientAliasOrEndpoint.StartsWith("http")
            ? new AElfClient(clientAliasOrEndpoint)
            : _aelfClientProvider.GetClient(alias: clientAliasOrEndpoint);
        var aelfAccount = SetAccount(alias, address);
        var builder = new TransactionBuilder(aelfClient);
        builder = builder
            .UsePrivateKey(aelfAccount)
            .UseContract(contractAddress)
            .UseMethod(methodName)
            .UseParameter(parameter);
        var tx = await builder.Build();
        await PerformSendAsync(aelfClient, tx);
        return tx;
    }

    public async Task<Transaction> SendSystemAsync(string systemContractName, string methodName, IMessage parameter,
        string clientAliasOrEndpoint, string? alias = null, string? address = null)
    {
        var aelfClient = clientAliasOrEndpoint.StartsWith("http")
            ? new AElfClient(clientAliasOrEndpoint)
            : _aelfClientProvider.GetClient(alias: clientAliasOrEndpoint);
        var aelfAccount = SetAccount(alias, address);
        var builder = new TransactionBuilder(aelfClient);
        builder = builder
            .UsePrivateKey(aelfAccount)
            .UseSystemContract(systemContractName)
            .UseMethod(methodName)
            .UseParameter(parameter);
        var tx = await builder.Build();
        await PerformSendAsync(aelfClient, tx);
        return tx;
    }

    public async Task<string> GenerateRawTransaction(string contractAddress, string methodName, IMessage parameter,
        string clientAliasOrEndpoint,  string? address = null, string? alias = null)
    {
        var aelfClient = clientAliasOrEndpoint.StartsWith("http")
            ? new AElfClient(clientAliasOrEndpoint)
            : _aelfClientProvider.GetClient(alias: clientAliasOrEndpoint);
        var aelfAccount = SetAccount(alias, address);
        var builder = new TransactionBuilder(aelfClient);
        var tx = await builder.UsePrivateKey(aelfAccount)
            .UseContract(contractAddress)
            .UseMethod(methodName)
            .UseParameter(parameter)
            .Build();
        return tx.ToByteArray().ToHex();
    }

    private static async Task PerformSendAsync(AElfClient aelfClient, Transaction tx)
    {
        var result = await aelfClient.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = tx.ToByteArray().ToHex()
        });
    }
    
    public async Task<List<string>?> SendTransactionsAsync(string clientAliasOrEndpoint, List<string> rawTransactionList)
    {
        var aelfClient = clientAliasOrEndpoint.StartsWith("http")
            ? new AElfClient(clientAliasOrEndpoint)
            : _aelfClientProvider.GetClient(alias: clientAliasOrEndpoint);
        var raws = string.Join(",", rawTransactionList);
        var transactions = await aelfClient.SendTransactionsAsync(new SendTransactionsInput
        {
            RawTransactions = raws
        });

        return transactions?.ToList();
    }

    private byte[] SetAccount(string? alias, string? address)
    {
        byte[] aelfAccount;
        if (!string.IsNullOrWhiteSpace(address))
        {
            _aelfAccountProvider.SetPrivateKey(address, _aelfAccountProvider.GetDefaultPassword());
            aelfAccount = _aelfAccountProvider.GetPrivateKey(null, address);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(alias))
                alias = _clientConfigOptions.AccountAlias;
            aelfAccount = _aelfAccountProvider.GetPrivateKey(alias);
        }

        return aelfAccount;
    }
}