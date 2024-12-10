using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Scale;
using Scale.Decoders;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Client.Test.Solidity;

public class TryCatchTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;
    private IAElfAccountProvider _accountProvider;

    private string TryCatchCallee = "2UM9eusxdRyCztbmMZadGXzwgwKfFdk8pF4ckw58D769ehaPSR";
    private string TryCatchCaller = "28PcLvP41ouUd6UNGsNRvKpkFTe6am34nPy4YPsWUJnZNwUvzM";

    public TryCatchTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _aelfClientService = GetRequiredService<IAElfClientService>();
        _aelfClientConfigOptions = GetRequiredService<IOptionsSnapshot<AElfClientConfigOptions>>().Value;
        _genesisService = GetRequiredService<IGenesisService>();
        _deployService = GetRequiredService<IDeployContractService>();
    }

    [Theory]
    [InlineData("contracts/TryCatchCallee.contract")]
    [InlineData("contracts/TryCatchCaller.contract")]
    public async Task<Address> DeployContractTest(string path)
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(path);
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
    public async Task TryCatchFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(TryCatchCaller), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(TryCatchCaller));

        for (var in_out = 0; in_out < 5; in_out++) {
            _testOutputHelper.WriteLine("Testing case: " + in_out);
            var answer =
                await _solidityContractService.SendAsync("test", registration, UInt128Type.GetByteStringFrom(in_out));
            _testOutputHelper.WriteLine($"TestAsync tx: {answer.TransactionResult.TransactionId}");
            answer.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            new IntegerTypeDecoder().Decode(answer.TransactionResult.ReturnValue.ToByteArray()).ShouldBe(in_out);
        }
    }

}