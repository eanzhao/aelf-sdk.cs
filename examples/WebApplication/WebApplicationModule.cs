using AElf.Client.Core;
using AElf.Client.Election;
using AElf.Client.Token;
using Volo.Abp.Modularity;

namespace WebApplication;

[DependsOn(
    typeof(AElfClientModule),
    typeof(AElfClientTokenModule),
    typeof(AElfClientElectionModule)
)]
public class WebApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var builder = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.local.json", optional: false, reloadOnChange: true)
            .AddConfiguration(configuration);
        context.Services.ReplaceConfiguration(builder.Build());
    }
}