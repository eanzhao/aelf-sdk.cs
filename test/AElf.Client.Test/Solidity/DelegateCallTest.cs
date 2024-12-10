using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Types;
using Scale;
using Scale.Decoders;
using Shouldly;
using Xunit.Abstractions;
using AElf.Client.Token;
using AElf.Contracts.MultiToken;
using AElf.SolidityContract;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Solang;

namespace AElf.Client.Test.Solidity;

public class DelegateCallTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private readonly ITokenService _tokenService;
    
    private ISolidityContractService _solidityContractService;

    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;
    

    private string delegateeContract = "2hqsqJndRAZGzk96fsEvyuVBTAvoBjcuwTjkuyJffBPueJFrLa";
    private string delegatorContract = "GwsSp1MZPmkMvXdbfSCDydHhZtDpvqkFpmPvStYho288fb7QZ";
    private string destructContract = "SsSqZWLf7Dk9NWyWyvDwuuY5nzn5n99jiscKZgRPaajZP5p8y";

    public DelegateCallTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _aelfClientService = GetRequiredService<IAElfClientService>();
        _aelfClientConfigOptions = GetRequiredService<IOptionsSnapshot<AElfClientConfigOptions>>().Value;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _genesisService = GetRequiredService<IGenesisService>();
        _tokenService = GetRequiredService<ITokenService>();
        _deployService = GetRequiredService<IDeployContractService>();
    }
    
    [Theory]
    [InlineData("contracts/Delegatee.contract")]
    [InlineData("contracts/Delegator.contract")]
    [InlineData("contracts/destruct.contract")]
    public async Task<Address> DeployContractTest(string path)
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(path);
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
    public async Task DelegateCallFeatureTest()
    {
        const long vars = 1616;
        const long transferValue = 100;
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(delegatorContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(delegatorContract));

        {
            var txResult= await _solidityContractService.SendAsync("setVars", registration,
                TupleType<AddressType, UInt256Type>.GetByteStringFrom(
                    AddressType.From(Address.FromBase58(delegateeContract).ToByteArray()),
                    UInt256Type.From(vars)
                ), 0, transferValue);
            _testOutputHelper.WriteLine(txResult.TransactionResult.TransactionId.ToHex());
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // Checks
        {
            var txResult = await _solidityContractService.SendAsync("num", registration);
            _testOutputHelper.WriteLine(txResult.TransactionResult.TransactionId.ToHex());
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            txResult.TransactionResult.ReturnValue.ToByteArray().ToInt64(false).ShouldBe(vars);        
        }

        {
            var txResult = await _solidityContractService.SendAsync("value", registration);
            _testOutputHelper.WriteLine(txResult.TransactionResult.TransactionId.ToHex());
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            txResult.TransactionResult.ReturnValue.ToByteArray().ToInt64(false).ShouldBe(transferValue);
        }
    }
    
    [Fact]
    public async Task DestructFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(destructContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(destructContract));

        var value = await _solidityContractService.CallAsync("hello", registration);
        Encoding.UTF8.GetString(value).ShouldBe("\u0014Hello"); // 为啥会有个空格？

        await _tokenService.TransferAsync(new TransferInput
        {
            Amount = 100000000,
            Symbol = "ELF",
            To = Address.FromBase58(destructContract)
        });

        var result = await _solidityContractService.SendAsync("selfterminate", registration,
            AddressType.GetByteStringFromBase58(destructContract));
        _testOutputHelper.WriteLine($"selfterminate tx: {result.TransactionResult.TransactionId}");
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }
}