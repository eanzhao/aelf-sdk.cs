using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Profit;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Profit;

public partial class ProfitService : ContractServiceBase, IProfitService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfContractOptions _contractOptions;
    private readonly AElfClientConfigOptions _clientConfigOptions;

    protected ProfitService(IAElfClientService clientService, string smartContractName) : base(clientService,
        smartContractName)
    {
    }

    public ProfitService(IAElfClientService clientService,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions) : base(clientService,
        "AElf.ContractNames.Profit")
    {
        _clientService = clientService;
        _clientConfigOptions = clientConfigOptions.Value;
    }

    public async Task<SendTransactionResult> ClaimProfitsAsync(ClaimProfitsInput claimProfitsInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("ClaimProfits", claimProfitsInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }
}