using System.Text.Json;
using AElf.Runtime.WebAssembly;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Solang;
using Solang.Extensions;
using Volo.Abp.Threading;

namespace AElf.Client.Core;

public class ContractServiceBase
{
    private readonly IAElfClientService _clientService;
    protected string SmartContractName { get; }
    protected Address? ContractAddress { get; set; }

    public ILogger<ContractServiceBase> Logger { get; set; }

    protected ContractServiceBase(IAElfClientService clientService, string smartContractName)
    {
        _clientService = clientService;
        SmartContractName = smartContractName;
        Logger= NullLogger<ContractServiceBase>.Instance;
    }

    protected ContractServiceBase(IAElfClientService clientService, Address contractAddress)
    {
        _clientService = clientService;
        ContractAddress = contractAddress;

        Logger= NullLogger<ContractServiceBase>.Instance;
    }
    
    protected ContractServiceBase(IAElfClientService clientService)
    {
        _clientService = clientService;

        Logger= NullLogger<ContractServiceBase>.Instance;
    }

    protected async Task<Transaction> PerformSendTransactionAsync(string methodName, IMessage parameter,
        string useClientAlias, string? smartContractName = null)
    {
        if (smartContractName == null)
        {
            smartContractName = SmartContractName;
        }

        if (ContractAddress != null)
        {
            return await _clientService.SendAsync(ContractAddress.ToBase58(), methodName, parameter, useClientAlias);
        }

        return await _clientService.SendSystemAsync(smartContractName, methodName, parameter, useClientAlias);
    }
    
    protected async Task<Transaction> PerformSendSolidityTransactionAsync(string methodName, SmartContractRegistration registration,
        string useClientAlias,  IMessage parameter)
    {
        var wasmCode = new WasmContractCode();
        wasmCode.MergeFrom(registration.Code);
        var solangAbi = JsonSerializer.Deserialize<SolangABI>(wasmCode.Abi);
        var selector = methodName == "deploy" ? solangAbi.GetConstructor() : solangAbi.GetSelector(methodName);
        return await _clientService.SendAsync(ContractAddress.ToBase58(), selector, parameter, useClientAlias);
    }
    
    
    protected async Task<byte[]> PerformCallSolidityTransactionAsync(string methodName, SmartContractRegistration registration,
        string useClientAlias,  IMessage parameter)
    {
        var wasmCode = new WasmContractCode();
        wasmCode.MergeFrom(registration.Code);
        var solangAbi = JsonSerializer.Deserialize<SolangABI>(wasmCode.Abi);
        var selector = methodName == "deploy" ? solangAbi.GetConstructor() : solangAbi.GetSelector(methodName);
        return await _clientService.ViewAsync(ContractAddress.ToBase58(), selector, parameter, useClientAlias);
    }

    
    protected async Task<string> GenerateRawTransaction(string methodName, IMessage parameter,
        string useClientAlias, string? from = null)
    {
        return await _clientService.GenerateRawTransaction(ContractAddress.ToBase58(), methodName, parameter, useClientAlias, from);
    }
    
    protected async Task<List<string>?> PerformSendTransactionsAsync( string useClientAlias, List<string> rawTransactions)
    {
        var raws = string.Join(",", rawTransactions);

        return await _clientService.SendTransactionsAsync(useClientAlias, rawTransactions);
    }

    protected async Task<TransactionResult> PerformGetTransactionResultAsync(string transactionId,
        string useClientAlias)
    {
        TransactionResult txResult;
        do
        {
            txResult = await _clientService.GetTransactionResultAsync(transactionId, useClientAlias);
        } while (txResult.Status == TransactionResultStatus.Pending);

        Logger.LogInformation("{TxResult}", txResult);
        return txResult;
    }
}