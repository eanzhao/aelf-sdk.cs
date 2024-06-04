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

        var symbolRegistrarService = _abpApplication.ServiceProvider.GetRequiredService<ISymbolRegistrarService>();
        var clientService = _abpApplication.ServiceProvider.GetRequiredService<IAElfClientService>();
        const int count = 19;
        for (var i = 0; i < count; i++)
        {
            var symbol = GenerateRandomString(10);
            Console.WriteLine($"Symbol: {symbol}");
            var sendTxResult = await symbolRegistrarService.CreateSeedAsync(new CreateSeedInput
            {
                Symbol = symbol,
                To = Address.FromBase58("GxyKXSsTWLimZ14Cm1NkX2v62AiCkUCsEZa7H91x8EguypVSp")
            });
            var txId = sendTxResult.Transaction.GetHash();
            var txResult = await clientService.GetTransactionResultAsync(txId.ToHex(), "Example");
            Console.WriteLine(i + 1);
            Console.WriteLine($"TxResult: {txResult}");
        }
    }

    private string GenerateRandomString(int length, bool isNftCollection = false)
    {
        //const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var random = new Random();
        var symbol = new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        if (isNftCollection)
        {
            symbol += "-0";
        }

        return symbol;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _abpApplication.ShutdownAsync();
    }
}