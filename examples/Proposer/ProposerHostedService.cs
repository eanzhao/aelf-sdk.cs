using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Parliament;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp;

namespace Proposer;

public class ProposerHostedService : IHostedService
{
    private IAbpApplicationWithInternalServiceProvider _abpApplication;

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public ProposerHostedService(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _abpApplication = await AbpApplicationFactory.CreateAsync<SeedCreatorModule>(options =>
        {
            var builder = new ConfigurationBuilder()
                .AddConfiguration(_configuration)
                .AddJsonFile($"appsettings.json")
                .AddJsonFile($"appsettings.local.json", true);
            options.Services.ReplaceConfiguration(builder.Build());
            options.Services.AddSingleton(_hostEnvironment);

            options.UseAutofac();
            options.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());
        });

        await _abpApplication.InitializeAsync();

        var parliamentService = _abpApplication.ServiceProvider.GetRequiredService<IParliamentService>();
        var clientService = _abpApplication.ServiceProvider.GetRequiredService<IAElfClientService>();
        
        
    }

    private async Task ProposeAsync(IParliamentService parliamentService, Address toAddress, string methodName, IMessage parameter)
    {
        // Create proposal
        var createProposalResult = await parliamentService.CreateProposalAsync(new CreateProposalInput
        {
            ContractMethodName = methodName,
            Params = parameter.ToByteString(),
            ToAddress = toAddress,
        });
        
        // Approve proposal
        var minerList = _abpApplication.ServiceProvider.GetRequiredService<IOptionsSnapshot<AElfMinerListOptions>>()
            .Value;
        foreach (var miner in minerList.MinerList)
        {
            
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _abpApplication.ShutdownAsync();
    }
}