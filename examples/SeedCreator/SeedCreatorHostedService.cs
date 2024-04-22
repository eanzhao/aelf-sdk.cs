using AElf.Client.Core;
using AElf.Client.SymbolRegistrar;
using AElf.Types;
using Forest.Contracts.SymbolRegistrar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Volo.Abp;

namespace SeedCreator;

public class SeedCreatorHostedService : IHostedService
{
    private IAbpApplicationWithInternalServiceProvider _abpApplication;

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public SeedCreatorHostedService(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _abpApplication = await AbpApplicationFactory.CreateAsync<SeedCreatorModule>(options =>
        {
            options.Services.ReplaceConfiguration(_configuration);
            options.Services.AddSingleton(_hostEnvironment);

            options.UseAutofac();
            options.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());
        });

        await _abpApplication.InitializeAsync();

        var symbolRegistrarService = _abpApplication.ServiceProvider.GetRequiredService<ISymbolRegistrarService>();
        var clientService = _abpApplication.ServiceProvider.GetRequiredService<IAElfClientService>();
        for (var i = 0; i < 10; i++)
        {
            var symbol = GenerateRandomString(10);
            var sendTxResult = await symbolRegistrarService.CreateSeedAsync(new CreateSeedInput
            {
                Symbol = symbol,
                To = Address.FromBase58("GxyKXSsTWLimZ14Cm1NkX2v62AiCkUCsEZa7H91x8EguypVSp")
            });
            var txId = sendTxResult.Transaction.GetHash();
            var txResult = await clientService.GetTransactionResultAsync(txId.ToHex(), "TestNetMainChain");
            Console.WriteLine($"TxResult: {txResult}");
            Console.WriteLine(i);
        }
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _abpApplication.ShutdownAsync();
    }
}