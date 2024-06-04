using AElf.Client;
using AElf.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Volo.Abp;

namespace TokenManager;

public class TokenManagerHostedService : IHostedService
{
    private IAbpApplicationWithInternalServiceProvider _abpApplication;

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public TokenManagerHostedService(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _abpApplication = await AbpApplicationFactory.CreateAsync<TokenManagerModule>(options =>
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

        var tokenManagerService = _abpApplication.ServiceProvider.GetRequiredService<TokenManagerService>();
        await PerformTransferAsync(tokenManagerService, "GxyKXSsTWLimZ14Cm1NkX2v62AiCkUCsEZa7H91x8EguypVSp", "ELF",
            20000_00000000);
    }

    private async Task PerformCrossChainTransferAsync(TokenManagerService tokenManagerService, string toAddress,
        string symbol, long amount)
    {
        await tokenManagerService.CrossChainTransferAsync(
            Address.FromBase58(toAddress), symbol, amount,
            EndpointType.TestNetSidechain2.ToString());
    }

    private async Task PerformTransferAsync(TokenManagerService tokenManagerService, string toAddress, string symbol,
        long amount)
    {
        await tokenManagerService.TransferAsync(Address.FromBase58(toAddress),
            symbol, amount);
    }

    private async Task PerformCrossChainCreateAsync(TokenManagerService tokenManagerService, string symbol)
    {
        await tokenManagerService.SyncTokenInfoAsync(symbol);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _abpApplication.ShutdownAsync();
    }
}