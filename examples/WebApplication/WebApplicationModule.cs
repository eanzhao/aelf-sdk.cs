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
        
    }
}