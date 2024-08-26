using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Solidity;
using AElf.Client.Token;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace BatchScript;

[DependsOn(
    typeof(AElfClientModule),
    typeof(AElfClientSolidityModule),typeof(AElfClientTokenModule))]
public class BatchScriptModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<TestContractOptions>(options => { configuration.GetSection("TestContract").Bind(options); });
        Configure<AElfContractOptions>(options => { configuration.GetSection("AElfContract").Bind(options); }); 
    }
}
