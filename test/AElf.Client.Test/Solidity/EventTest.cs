using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.SolidityContract;
using Shouldly;
using Xunit.Abstractions;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Solang;

namespace AElf.Client.Test.Solidity;

public class EventTest : AElfClientAbpContractServiceTestBase
{
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGenesisService _genesisService;
    private readonly IDeployContractService _deployService;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private string testContract = "225ajURvev5rgX8HnMJ8GjbPnRxUrCHoD7HUjhWQqewEJ5GAv1";
    private string TestContractPath = "contracts/Events.contract";

    public EventTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _aelfClientService = GetRequiredService<IAElfClientService>();
        _aelfClientConfigOptions = GetRequiredService<IOptionsSnapshot<AElfClientConfigOptions>>().Value;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _genesisService = GetRequiredService<IGenesisService>();
        _deployService = GetRequiredService<IDeployContractService>();
    }

    [Fact]
    public async Task<Address> DeployContractTest()
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TestContractPath);
        _solangAbi = solangAbi;
        var input = new DeploySoliditySmartContractInput
        {
            Category = 1,
            Code = wasmCode.ToByteString(),
            Parameter = ByteString.Empty
        };
        var contractAddress = await _deployService.DeploySolidityContract(input);
        contractAddress.Value.ShouldNotBeEmpty();
        _testOutputHelper.WriteLine(contractAddress.ToBase58());
        var contractInfo = await _genesisService.GetContractInfo(contractAddress);
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);

        return contractAddress;
    }
    
    [Fact]
    public async Task EventFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));

        var contractInfo = await _genesisService.GetContractInfo(Address.FromBase58(testContract));
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);
        
        var value = await _solidityContractService.SendAsync("emit_event", registration);
        _testOutputHelper.WriteLine($"emit_event tx: {value.TransactionResult.TransactionId}");
        value.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var logs = value.TransactionResult.Logs;
        logs.Count.ShouldBe(5);
    }
}