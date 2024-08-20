using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Runtime.WebAssembly;
using AElf.Types;
using Google.Protobuf;
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
        _contractAddress = contractAddress;
        _clientConfigOptions = clientConfigOptions;
    }

    public async Task<long> EstimateGasFeeAsync(Transaction transaction)
    {
        return await _clientService.EstimateGasFeeAsync(transaction);
    }

    public async Task<SendTransactionResult> SendAsync(string selector, ByteString? parameter = null,
        Weight? gasLimit = null, long value = 0)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var input = new SolidityTransactionParameter
        {
            Parameter = parameter ?? ByteString.Empty,
            Value = value,
            GasLimit = gasLimit ?? new Weight { ProofSize = int.MaxValue, RefTime = int.MaxValue }
        };
        var tx = await PerformSendTransactionAsync(selector, input, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<byte[]> CallAsync(string selector, ByteString parameter)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var input = new SolidityTransactionParameter
        {
            Parameter = parameter,
        };
        var result = await _clientService.ViewAsync(_contractAddress.ToBase58(), selector, input, clientAlias);
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