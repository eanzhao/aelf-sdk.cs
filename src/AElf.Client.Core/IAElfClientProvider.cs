using AElf.Client.Core.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Core;

public interface IAElfClientProvider
{
    AElfClient GetClient(string? alias = null, string? environment = null, int? chainId = null, string? chainType = null);

    void SetClient(AElfClient client, string? environment = null, int? chainId = null, string? chainType = null,
        string? alias = null);
}

public class AElfClientProvider : Dictionary<AElfClientInfo, AElfClient>, IAElfClientProvider, ISingletonDependency
{
    public AElfClientProvider(IOptionsSnapshot<AElfClientOptions> aelfClientOptions,
        IOptionsSnapshot<AElfClientConfigOptions> aelfClientConfigOptions)
    {
        var useCamelCase = aelfClientConfigOptions.Value.CamelCase;
        var clientBuilder = new AElfClientBuilder();
        SetClient(clientBuilder.UsePublicEndpoint(EndpointType.MainNetMainchain).Build(),
            "MainNet", AElfClientConstants.AELFChainId, "MainChain", EndpointType.MainNetMainchain.ToString());
        SetClient(clientBuilder.UsePublicEndpoint(EndpointType.MainNetSidechain).Build(),
            "MainNet", AElfClientConstants.tDVVChainId, "SideChain", EndpointType.MainNetSidechain.ToString());
        SetClient(clientBuilder.UsePublicEndpoint(EndpointType.TestNetMainchain).Build(),
            "TestNet", AElfClientConstants.AELFChainId, "MainChain", EndpointType.TestNetMainchain.ToString());
        SetClient(clientBuilder.UsePublicEndpoint(EndpointType.TestNetSidechain2).Build(),
            "MainNet", AElfClientConstants.tDVWChainId, "SideChain", EndpointType.TestNetSidechain2.ToString());
        SetClient(clientBuilder.UsePublicEndpoint(EndpointType.Local).Build(), "Local",
            AElfClientConstants.AELFChainId, "MainChain", EndpointType.Local.ToString());

        foreach (var clientConfig in aelfClientOptions.Value.ClientConfigList)
        {
            var client = clientBuilder
                .UseEndpoint(clientConfig.Endpoint)
                .ManagePeerInfo(clientConfig.UserName, clientConfig.Password)
                .SetHttpTimeout(clientConfig.Timeout)
                .Build();
            SetClient(client, alias: clientConfig.Alias);
        }
    }

    public AElfClient GetClient(string? alias = null, string? environment = null, int? chainId = null,
        string? chainType = null)
    {
        var keys = Keys
            .WhereIf(!alias.IsNullOrWhiteSpace(), c => string.Equals(c.Alias, alias, StringComparison.CurrentCultureIgnoreCase))
            .WhereIf(!environment.IsNullOrWhiteSpace(), c => c.Environment == environment)
            .WhereIf(chainId.HasValue, c => c.ChainId == chainId)
            .WhereIf(!chainType.IsNullOrWhiteSpace(), c => c.ChainType == chainType)
            .ToList();
        if (keys.Count != 1)
        {
            throw new AElfClientException(
                $"Failed to get client of {alias} - {environment} - {chainId} - {chainType}.");
        }

        return this[keys.Single()];
    }

    public void SetClient(AElfClient client, string? environment = null, int? chainId = null, string? chainType = null,
        string? alias = null)
    {
        TryAdd(new AElfClientInfo
        {
            Environment = environment,
            ChainId = chainId,
            ChainType = chainType,
            Alias = alias
        }, client);
    }
}

public class AElfClientInfo
{
    public string? Environment { get; set; }
    public int? ChainId { get; set; }
    public string? ChainType { get; set; }
    public string? Alias { get; set; }
}