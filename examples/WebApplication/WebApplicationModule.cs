using AElf.Client.Core;
using Volo.Abp.Modularity;

namespace WebApplication;

[DependsOn(
    typeof(AElfClientModule)
)]
public class WebApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var builder = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: true);
        context.Services.ReplaceConfiguration(builder.Build());
    }
}