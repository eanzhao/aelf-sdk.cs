using AElf;
using AElf.Client;
using AElf.Client.Core;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Standards.ACS4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AEDPoSViewer;

public class AEDPoSViewerService : ITransientDependency
{
    private IServiceScopeFactory ServiceScopeFactory { get; }

    public ILogger<AEDPoSViewerService> Logger { get; set; }

    public AEDPoSViewerService(IServiceScopeFactory serviceScopeFactory)
    {
        ServiceScopeFactory = serviceScopeFactory;

        Logger = NullLogger<AEDPoSViewerService>.Instance;
    }

    public async Task RunAsync()
    {
        using var scope = ServiceScopeFactory.CreateScope();

        var clientService = scope.ServiceProvider.GetRequiredService<IAElfClientService>();

        await QueryCurrentRoundInformation(clientService, "MainNetMainChain");
        //await QueryConsensusCommandAsync(clientService,
            //"04427f41c3a4f27efa69bf38943895f0fd5c60d385efeb43034e7cc76da08499f6a469f9f7f41276a2922ab3700e8c33feece89f12c2a3d4c061855d2ea1307a12");
    }

    private async Task QueryConsensusCommandAsync(IAElfClientService clientService, string pubkey)
    {
        var result = await clientService.ViewSystemAsync(AEDPoSViewerConstants.ConsensusSmartContractName,
            "GetConsensusCommand", new BytesValue { Value = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(pubkey)) }, "Example");
        var command = new ConsensusCommand();
        command.MergeFrom(result);
        Console.WriteLine(command);
        Console.WriteLine(command.ArrangedMiningTime);
    }

    private async Task QueryCurrentRoundInformation(IAElfClientService clientService, string clientAlias)
    {
        var result = await clientService.ViewSystemAsync(AEDPoSViewerConstants.ConsensusSmartContractName,
            "GetCurrentRoundInformation", new Empty(), clientAlias);

        var round = new Round();
        round.MergeFrom(result);
        Logger.LogInformation("Current round: {Round}", round);
        Logger.LogInformation("Current lib: {Lib}", round.ConfirmedIrreversibleBlockHeight);
        
        var chainStatus = await clientService.GetChainStatusAsync("MainNetMainChain");
        var height = chainStatus.BestChainHeight;
        Logger.LogInformation($"Current height: {height}");
        Logger.LogInformation($"Distance: {height - round.ConfirmedIrreversibleBlockHeight}");
    }
}