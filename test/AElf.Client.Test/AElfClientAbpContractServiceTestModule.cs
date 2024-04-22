using AElf.Client.Election;
using AElf.Client.Faucet;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Client.TestBase;
using AElf.Client.Token;
using Volo.Abp.Modularity;

namespace AElf.Client.Test;

[DependsOn(
    typeof(AElfClientAbpTestBaseModule),
    typeof(AElfClientTokenModule),
    typeof(AElfClientGenesisModule),
    typeof(AElfClientSolidityModule),
    typeof(AElfClientFaucetModule),
    typeof(AElfClientElectionModule)
)]
public class AElfClientAbpContractServiceTestModule : AbpModule
{
    
}