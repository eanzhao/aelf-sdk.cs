using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Election;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Election;

public partial class ElectionService : ContractServiceBase, IElectionService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfContractOptions _contractOptions;
    private readonly AElfClientConfigOptions _clientConfigOptions;

    protected ElectionService(IAElfClientService clientService, string smartContractName) : base(clientService,
        smartContractName)
    {
    }

    public ElectionService(IAElfClientService clientService,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions) : base(clientService,
        "AElf.ContractNames.Election")
    {
        _clientService = clientService;
        _clientConfigOptions = clientConfigOptions.Value;
    }

    public async Task<long> GetCalculateVoteWeightAsync(VoteInformation voteInformation)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewSystemAsync("AElf.ContractNames.Election", "GetCalculateVoteWeight",
            voteInformation, useClientAlias);
        var output = new Int64Value();
        output.MergeFrom(result);
        return output.Value;
    }
}