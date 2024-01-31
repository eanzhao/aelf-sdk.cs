using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Client.Solidity;

[DependsOn(
    typeof(AElfClientModule),
    typeof(CoreAElfModule),
    typeof(AElfClientGenesisModule)
)]
public class AElfClientSolidityModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<AElfContractOptions>(options => { configuration.GetSection("AElfContract").Bind(options); });
    }
}