using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Runtime.WebAssembly.Types;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Scale;
using Scale.Encoders;
using Shouldly;
using Solang;
using Solang.Extensions;
using Xunit.Abstractions;

namespace AElf.Client.Test.Solidity;

public class StorageContractTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;
    
    private string testContract = "2M24EKAecggCnttZ9DUUMCXi4xC67rozA87kFgid9qEwRUMHTs";
    private string TestContractPath = "contracts/Storage.contract";
    public StorageContractTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _aelfClientService = GetRequiredService<IAElfClientService>();
        _aelfClientConfigOptions = GetRequiredService<IOptionsSnapshot<AElfClientConfigOptions>>().Value;
        _genesisService = GetRequiredService<IGenesisService>();
        _deployService = GetRequiredService<IDeployContractService>();
    }
    
    [Fact]
    public async Task<Address> DeployContractTest()
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TestContractPath);
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
    public async Task StoreTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));
        var parameter = UInt256Type.GetByteStringFrom(100);
        var sendTxResult = await _solidityContractService.SendAsync("store",registration, parameter);
        sendTxResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task RetrieveTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));
        var sendTxResult = await _solidityContractService.SendAsync("retrieve", registration);
        sendTxResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        sendTxResult.TransactionResult.ReturnValue.ToByteArray().ToInt64(false).ShouldBe(100);
    }
}