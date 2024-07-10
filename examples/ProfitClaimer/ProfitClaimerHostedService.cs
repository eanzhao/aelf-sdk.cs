using AElf.Client.Profit;
using AElf.Contracts.Profit;
using AElf.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Volo.Abp;

namespace ProfitClaimer;

public class ProfitClaimerHostedService : IHostedService
{
    private IAbpApplicationWithInternalServiceProvider _abpApplication;

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public ProfitClaimerHostedService(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _abpApplication = await AbpApplicationFactory.CreateAsync<ProfitClaimerModule>(options =>
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

        var profitService = _abpApplication.ServiceProvider.GetRequiredService<IProfitService>();

        var schemeId = Hash.LoadFromHex("6871eb0727c6a5f35d216e48ff80752085e892fa271081a728a1607dc3dddda9");
        // foreach (var voter in await File.ReadAllLinesAsync("voters.txt", cancellationToken))
        // {
        //     var address = Address.FromBase58(voter);
        //     var details = await profitService.GetProfitDetailsAsync(new GetProfitDetailsInput
        //     {
        //         SchemeId = schemeId,
        //         Beneficiary = address
        //     });
        //     Console.WriteLine($"{voter}:\n{details}");
        // }

        var details =
            await profitService.GetProfitDetailsAsync(new GetProfitDetailsInput
            {
                SchemeId = schemeId,
                Beneficiary = Address.FromBase58("2XDRhxzMbaYRCTe3NxRpARkBpjfQpyWdBKscQpc3Tph3m6dqHG")
            });
        // 12287556623449740
        // 12231325157420058
        // 12152144293669496
        Console.WriteLine(details);

        // var votersNeedToClaim = new Dictionary<string, long>();
        // foreach (var voter in await File.ReadAllLinesAsync("voters.txt", cancellationToken))
        // {
        //     var profitsAmount = await profitService.GetProfitAmountAsync(new GetProfitAmountInput
        //     {
        //         SchemeId = Hash.LoadFromHex("d638bb79ebeaa0e9fd6c562c9734947d467b2753d8108733ce1d9139e5b1e721"),
        //         Symbol = "ELF",
        //         Beneficiary = Address.FromBase58(voter)
        //     });
        //     Console.WriteLine($"{voter}: {profitsAmount}");
        //     if (profitsAmount > 0)
        //     {
        //         votersNeedToClaim.Add(voter, profitsAmount);
        //     }
        // }
        //
        // votersNeedToClaim = votersNeedToClaim.OrderByDescending(v => v.Value).ToDictionary(v => v.Key, v => v.Value);
        // await File.WriteAllLinesAsync("votersToClaim.txt", votersNeedToClaim.Select(v => $"{v.Key}: {v.Value}"),
        //     cancellationToken);
        // await profitService.ClaimProfitsAsync(new ClaimProfitsInput
        // {
        //     SchemeId = ProfitClaimerConstants.CitizenWelfareSchemeId,
        //     Beneficiary = Address.FromBase58("")
        // });
        //
        //var remainProfits = await profitService.
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _abpApplication.ShutdownAsync();
    }
}