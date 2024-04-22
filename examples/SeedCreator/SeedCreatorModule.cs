using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.SymbolRegistrar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace SeedCreator;

[DependsOn(
    typeof(AElfClientModule),
    typeof(AElfClientSymbolRegistrarModule)
)]
public class SeedCreatorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<AElfContractOptions>(options => { configuration.GetSection("AElfContract").Bind(options); });
    }
}