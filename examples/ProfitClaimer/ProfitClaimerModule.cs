using AElf.Client.Core;
using AElf.Client.Profit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace ProfitClaimer;

[DependsOn(
    typeof(AElfClientModule),
    typeof(AElfClientProfitModule)
)]
public class ProfitClaimerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
    }
}