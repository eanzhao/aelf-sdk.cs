using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Profit;
using AElf.Types;
using Forest.Contracts.SymbolRegistrar;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.SymbolRegistrar;

public partial class SymbolRegistrarService : ContractServiceBase, ISymbolRegistrarService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfClientConfigOptions _clientConfigOptions;

    public SymbolRegistrarService(IAElfClientService clientService,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions,
        IOptionsSnapshot<AElfContractOptions> contractOptions) : base(clientService)
    {
        _clientService = clientService;
        _clientConfigOptions = clientConfigOptions.Value;
        ContractAddress = contractOptions.Value.SRAddress != null
            ? Address.FromBase58(contractOptions.Value.SRAddress)
            : throw new Exception("SymbolRegistrar contract address not found in contract directory.");
    }

    public async Task<SendTransactionResult> CreateSeedAsync(CreateSeedInput createSeedInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("CreateSeed", createSeedInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }
}