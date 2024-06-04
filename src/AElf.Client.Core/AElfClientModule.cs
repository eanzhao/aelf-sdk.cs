using AElf.Client.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElf.Client.Core;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule)
    )]
public class AElfClientModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        var builder = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json")
            .AddJsonFile($"appsettings.local.json", true)
            .AddConfiguration(configuration);

        context.Services.ReplaceConfiguration(builder.Build());
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddConfiguration(configuration);
        context.Services.ReplaceConfiguration(builder.Build());
        Configure<AElfClientOptions>(options => { configuration.GetSection("AElfClient").Bind(options); });
        Configure<AElfAccountOptions>(options => { configuration.GetSection("AElfAccount").Bind(options); });
        Configure<AElfClientConfigOptions>(options => { configuration.GetSection("AElfClientConfig").Bind(options); });
        Configure<AElfMinerAccountOptions>(options => { configuration.GetSection("AElfMinerAccount").Bind(options); });

        context.Services.AddAutoMapperObjectMapper<AElfClientModule>();

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AElfClientModule>();
        });
    }
}