using System.Text.Json;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Runtime.WebAssembly;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Solidity;

public class SolidityContractService : ContractServiceBase, ISolidityContractService, ITransientDependency
{
    private readonly Address _contractAddress;
    private readonly IAElfClientService _clientService;
    private readonly AElfClientConfigOptions _clientConfigOptions;

    public SolidityContractService(IAElfClientService clientService, string smartContractName,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions) : base(clientService,
        smartContractName)
    {
        _clientService = clientService;
        _clientConfigOptions = clientConfigOptions.Value;
    }

    public SolidityContractService(IAElfClientService clientService, Address contractAddress,
        AElfClientConfigOptions clientConfigOptions) : base(clientService,
        contractAddress)
    {
        _clientService = clientService;
        _contractAddress = contractAddress;
        _clientConfigOptions = clientConfigOptions;
    }

    public async Task<long> EstimateGasFeeAsync(Transaction transaction)
    {
        return await _clientService.EstimateGasFeeAsync(transaction);
    }
    

    public async Task<SendTransactionResult> SendAsync(string methodName, SmartContractRegistration registration, ByteString? parameter = null,
        int gasLimit = 0, long value = 0)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        if (gasLimit is 0)
        {
            gasLimit = int.MaxValue;
        }

        var input = new SolidityTransactionParameter
        {
            Parameter = parameter ?? ByteString.Empty,
            Value = value,
            GasLimit = gasLimit
        };;
        var tx = await PerformSendSolidityTransactionAsync(methodName, registration ,clientAlias, input);
        
        
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<TransactionResult> CheckResult(string txId)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        return await PerformGetTransactionResultAsync(txId, clientAlias);
    }

    public async Task<string> SendWithoutResultAsync(string methodName, SmartContractRegistration registration, ByteString? parameter = null,
       int gasLimit = 0, long value = 0)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        if (gasLimit is 0)
        {
            gasLimit = int.MaxValue;
        }
        
        var input = new SolidityTransactionParameter
        {
            Parameter = parameter ?? ByteString.Empty,
            Value = value,
            GasLimit = gasLimit
        };
        var tx = await PerformSendSolidityTransactionAsync(methodName, registration ,clientAlias, input);
        return tx.GetHash().ToHex();
    }
    
    public async Task<string> GenerateRawTransaction(string selector, ByteString? parameter = null, string from = null,
       int gasLimit = 0, long value = 0)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;

        if (gasLimit is 0)
        {
            gasLimit = int.MaxValue;
        }

        var input = new SolidityTransactionParameter
        {
            Parameter = parameter ?? ByteString.Empty,
            Value = value,
            GasLimit = gasLimit 
        };
        var tx = await GenerateRawTransaction(selector, input, clientAlias, from);
        return tx;
    }

    public async Task<List<string>?> SendMultiTransactions(List<string> rawTransactions)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var txIdList = await PerformSendTransactionsAsync(clientAlias, rawTransactions);
        return txIdList;
    }
    
    public async Task<byte[]> CallAsync(string methodName, SmartContractRegistration registration, ByteString? parameter = null,
        int gasLimit = 0, long value = 0)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        if (gasLimit is 0)
        {
            gasLimit = int.MaxValue;
        }
        var input = new SolidityTransactionParameter
        {
            Parameter = parameter ?? ByteString.Empty,
            Value = value,
            GasLimit = gasLimit
        };
        var result = await PerformCallSolidityTransactionAsync(methodName, registration, clientAlias, input);
        return result;
    } 

    public async Task<SendTransactionResult> EstimateFeeAsync(string selector, ByteString? parameter = null)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var input = new SolidityTransactionParameter
        {
            Parameter = parameter ?? ByteString.Empty,
        };
        var tx = await PerformSendTransactionAsync(selector, input, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }
}