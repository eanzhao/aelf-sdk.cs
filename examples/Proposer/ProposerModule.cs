using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Parliament;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Proposer;

[DependsOn(
    typeof(AElfClientModule),
    typeof(AElfClientParliamentModule)
)]
public class SeedCreatorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<AElfContractOptions>(options => { configuration.GetSection("AElfContract").Bind(options); });
        Configure<AElfMinerListOptions>(options => { configuration.GetSection("AElfMinerList").Bind(options); });
    }
}