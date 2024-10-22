using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using Shouldly;
using Solang;
using Xunit.Abstractions;
using AElf.Client.Test.contract;
using AElf.Types;

namespace AElf.Client.Test.Solidity;

public class EventTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private IEventsStub _eventsStub;

    public EventTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _deployService = GetRequiredService<IDeployContractService>();
        _genesisService = GetRequiredService<IGenesisService>();
        _eventsStub = GetRequiredService<IEventsStub>();
    }

    [Fact]
    public async Task EventFeatureTest()
    {
        var contractAddress = await _eventsStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");

        var contractInfo = await _genesisService.GetContractInfo(contractAddress);
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);
        
        var value = await _eventsStub.Emit_eventAsync();
        _testOutputHelper.WriteLine($"Set tx: {value.TransactionResult.TransactionId}");
        value.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var logs = value.TransactionResult.Logs;
        logs.Count.ShouldBe(5);
    }
}