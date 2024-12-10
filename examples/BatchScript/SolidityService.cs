using System.Diagnostics;
using AElf;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Solidity;
using AElf.Client.Token;
using AElf.Contracts.MultiToken;
using AElf.Runtime.WebAssembly;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.ABI;
using Scale.Encoders;
using Solang;
using Solang.Extensions;
using Volo.Abp.DependencyInjection;

namespace BatchScript;

public interface ISolidityService
{
    Task Initialize();
    Task Mint();
    Task TransferTokenForFee();

    Task ExecuteBatchTransactionTask(string from, CancellationTokenSource cts,
        CancellationToken token);
}

public class SolidityService : ISingletonDependency, ISolidityService
{
    private readonly ISolidityContractService _solidity;
    private IServiceScopeFactory ServiceScopeFactory { get; }
    private SolangABI _solangAbi;
    private ILogger<SolidityService> Logger { get; set; }
    private readonly TestContractOptions _testContractOptions;
    private readonly IBatchScriptHelper _batchScriptHelper;
    private IAElfClientService _clientService;
    private readonly AElfClientConfigOptions _clientConfigOptions;
    private readonly ITokenService _tokenService;

    public SolidityService(IServiceScopeFactory serviceScopeFactory,
        IOptionsSnapshot<TestContractOptions> testContract,
        IBatchScriptHelper batchScriptHelper,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions,
        IAElfClientService aelfClientService,
        ITokenService tokenService,
        ILogger<SolidityService> logger)
    {
        _batchScriptHelper = batchScriptHelper;
        _tokenService = tokenService;
        _testContractOptions = testContract.Value;
        ServiceScopeFactory = serviceScopeFactory;
        _clientConfigOptions = clientConfigOptions.Value;
        _clientService = aelfClientService;
        _solangAbi = _batchScriptHelper.GetSolangAbi();

        var contract = _batchScriptHelper.GetContractAddress();
        _solidity = new SolidityContractService(aelfClientService, contract, _clientConfigOptions);

        Logger = logger;
    }

    public async Task Initialize()
    {
        var selector = _solangAbi.GetSelector("initialize");
        var name = "Mock Token";
        var symbol = "MOCK";

        var dataPart1 = ByteString.CopyFrom(new StringTypeEncoder().Encode(name));
        var dataPart2 = ByteString.CopyFrom(new StringTypeEncoder().Encode(symbol));
        var combinedData = From(dataPart1, dataPart2);
        var elf = ByteArrayHelper.HexStringToByteArray("0x0c454c46");
        var elfToken = ByteArrayHelper.HexStringToByteArray("0x24456c6620746f6b656e");

        // var txResult = await _solidity.SendAsync(selector, ByteString.CopyFrom(elfToken.Concat(elf).ToArray()));
        // Logger.LogInformation("Initialize TransactionId: {0}, Status: {1}",
        //     txResult.TransactionResult.TransactionId.ToHex(), txResult.TransactionResult.Status);
    }

    public async Task Mint()
    {
        var selector = _solangAbi.GetSelector("mint");
        var account = _testContractOptions.InitAddress;
        var testAmount = 10000000_00000000;
        // var dataPart1 = ByteString.CopyFrom(Address.FromBase58(account).ToByteArray());
        // var dataPart2 = ByteString.CopyFrom(new IntegerTypeEncoder().Encode(1000000000000000000));
        // var combinedData = From(dataPart1, dataPart2);
        var combinedData = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(
            Address.FromBase58(account).ToWebAssemblyAddress(),
            testAmount.ToWebAssemblyUInt256()));

        // var mintResult = await _solidity.SendAsync(selector, combinedData);
        // Logger.LogInformation("Mint TransactionId: {0}, Status: {1}",
        //     mintResult.TransactionResult.TransactionId.ToHex(), mintResult.TransactionResult.Status);

        var transferSelector = _solangAbi.GetSelector("transfer");
        var accountList = _testContractOptions.FromAccountList;
        foreach (var from in accountList)
        {
            var amount = 10000_00000000;
            // var dataPart3 = ByteString.CopyFrom(Address.FromBase58(from).ToByteArray());
            // var dataPart4 = ByteString.CopyFrom(new IntegerTypeEncoder().Encode(amount));
            // var combinedData2 = From(dataPart3, dataPart4);           
            var combinedData2 = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(
                Address.FromBase58(from).ToWebAssemblyAddress(),
                amount.ToWebAssemblyUInt256()));

            // var tx = await _solidity.SendAsync(transferSelector, combinedData2);
            // Logger.LogInformation("Transfer TransactionId: {0}, Status: {1}",
            //     tx.TransactionResult.TransactionId.ToHex(), tx.TransactionResult.Status);
        }
    }

    private async Task<List<string>> TransferForTest()
    {
        var selector = _solangAbi.GetSelector("transfer");
        var accountList = _testContractOptions.FromAccountList;
        var toAccountList = _testContractOptions.ToAccountList;
        var rawTransactionList = new List<string>();
        foreach (var account in accountList)
        {
            foreach (var to in toAccountList)
            {
                var amount = GenerateRandomNumber(1, 100);
                var dataPart1 = ByteString.CopyFrom(Address.FromBase58(to).ToByteArray());
                var dataPart2 = ByteString.CopyFrom(new IntegerTypeEncoder().Encode(amount + 1));
                var combinedData = From(dataPart1, dataPart2);

                var requestInfo =
                    await _solidity.GenerateRawTransaction(selector, combinedData, account);
                rawTransactionList.Add(requestInfo);
            }
        }

        return rawTransactionList;
    }

    private async Task<List<string>> StoreForTest(string from)
    {
        var selector = _solangAbi.GetSelector("store");

        var rawTransactionList = new List<string>();

        for (var i = 0; i < 20; i++)
        {
            var amount = GenerateRandomNumber(1, int.MaxValue);
            var parameter = ByteString.CopyFrom(new IntegerTypeEncoder().Encode(amount));

            var requestInfo =
                await _solidity.GenerateRawTransaction(selector, parameter, from);
            rawTransactionList.Add(requestInfo);
        }

        return rawTransactionList;
    }

    public async Task TransferTokenForFee()
    {
        var accountList = _testContractOptions.FromAccountList;
        foreach (var account in accountList)
        {
            var input = new TransferInput
            {
                To = Address.FromBase58(account),
                Amount = 10000_00000000,
                Symbol = "ELF"
            };
            await _tokenService.TransferAsync(input);
        }
    }

    private async Task SendMultiTransaction(List<string> rawTransactions)
    {
        var txList = await _solidity.SendMultiTransactions(rawTransactions);
        if (txList != null)
            Parallel.ForEach(txList, tx => { Logger.LogInformation("Transaction {0}", tx); });
    }

    public async Task ExecuteBatchTransactionTask(string from, CancellationTokenSource cts,
        CancellationToken token)
    {
        try
        {
            for (var r = 1; r > 0; r++) //continuous running
            {
                if (token.IsCancellationRequested)
                {
                    var endTIme = DateTime.UtcNow;
                    Logger.LogInformation(
                        $"End execution transaction request round, total round:{r - 1}, end time: {endTIme}");
                    break;
                }

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    Logger.LogInformation($"Execution transaction request round: {r}");
                    //multi task for SendTransactions query

                    await Task.Run(() =>
                    {
                        var rawTransactions = StoreForTest(from).Result;
                        SendMultiTransaction(rawTransactions).Wait(token);
                    }, token);


                    Thread.Sleep(500);
                }
                catch (AggregateException exception)
                {
                    Logger.LogError($"Request got exception, {exception}");
                }
                catch (Exception e)
                {
                    var message = "Execute continuous transaction got exception." +
                                  $"\r\nMessage: {e.Message}" +
                                  $"\r\nStackTrace: {e.StackTrace}";
                    Logger.LogError(message);
                }

                stopwatch.Stop();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e.Message);
            Logger.LogError("Cancel all tasks due to transaction execution exception.");
            cts.Cancel(); //cancel all tasks
        }
    }

    private int GenerateRandomNumber(int min, int max)
    {
        var rd = new Random(Guid.NewGuid().GetHashCode());
        var random = rd.Next(min, max);
        return random;
    }

    private ByteString From(params ByteString[] values)
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