using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Extensions;
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
using Nethereum.ABI.Decoders;
using Solang;

namespace AElf.Client.Test.Solidity;

public class AssertsTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;

    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private string assertsContract = "2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo";
    private string debugBufferContract = "sr4zX6E7yVVL7HevExVcWv2ru3HSZakhsJMXfzxzfpnXofnZw";
    private string releaseContract = "2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ";
    private string flipperContract = "URyXBKB47QXW8TAXqJBGVt9edz2Ev5QzR6T2V6YV1hn14mVPp";

    public AssertsTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _aelfClientService = GetRequiredService<IAElfClientService>();
        _aelfClientConfigOptions = GetRequiredService<IOptionsSnapshot<AElfClientConfigOptions>>().Value;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _genesisService = GetRequiredService<IGenesisService>();
        _deployService = GetRequiredService<IDeployContractService>();
    }

    [Theory]
    [InlineData("contracts/asserts.contract")]
    [InlineData("contracts/DebugBuffer.contract")]
    [InlineData("contracts/release.contract")]
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
    public async Task<Address> DeployContractWithParameterTest()
    {
        var path = "contracts/flipper.contract";
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(path);
        _solangAbi = solangAbi;
        var input = new DeploySoliditySmartContractInput
        {
            Category = 1,
            Code = wasmCode.ToByteString(),
            Parameter = BoolType.False
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
    public async Task DebugBufferFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(debugBufferContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(debugBufferContract));
        
        var result = await _solidityContractService.SendAsync("multiple_prints", registration);
        _testOutputHelper.WriteLine($"multiple_prints tx: {result.TransactionResult.TransactionId}");
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        var txResult = await _solidityContractService.SendAsync("multiple_prints_then_revert", registration);
        _testOutputHelper.WriteLine($"multiple_prints_then_revert tx: {txResult.TransactionResult.TransactionId}");

        // await Task.Delay(5000);
        // var checkResult = await _solidityContractService.CheckResult(txResult.TransactionResult.TransactionId.ToHex());
        // checkResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
        // checkResult.Error.ShouldContain("sesa");
        _testOutputHelper.WriteLine("[Prints]");
        foreach (var print in txResult.TransactionResult.GetPrints())
        {
            _testOutputHelper.WriteLine(print);
        }

        _testOutputHelper.WriteLine("[Runtime logs]");
        foreach (var runtimeLog in txResult.TransactionResult.GetRuntimeLogs())
        {
            _testOutputHelper.WriteLine(runtimeLog);
        }

        _testOutputHelper.WriteLine("[Debug messages]");
        foreach (var debugMessage in txResult.TransactionResult.GetDebugMessages())
        {
            _testOutputHelper.WriteLine(debugMessage);
        }

        _testOutputHelper.WriteLine("[Error messages]");
        foreach (var errorMessage in txResult.TransactionResult.GetErrorMessages())
        {
            _testOutputHelper.WriteLine(errorMessage);
        }

        _testOutputHelper.WriteLine($"Charged gas fee: {txResult.TransactionResult.GetChargedGasFee()}");
    }

    [Fact]
    public async Task ReleaseFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(releaseContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(releaseContract));
        
        var txResult =  await _solidityContractService.SendAsync("print_then_error",registration,Int8Type.GetByteStringFrom(1));
        _testOutputHelper.WriteLine($"print_then_error tx: {txResult.TransactionResult.TransactionId}");
        txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        new IntegerTypeDecoder().Decode(txResult.TransactionResult.ReturnValue.ToByteArray()).ShouldBe(121);
        
        _testOutputHelper.WriteLine("[Prints]");
        foreach (var print in txResult.TransactionResult.GetPrints())
        {
            _testOutputHelper.WriteLine(print);
        }

        _testOutputHelper.WriteLine("[Runtime logs]");
        foreach (var runtimeLog in txResult.TransactionResult.GetRuntimeLogs())
        {
            _testOutputHelper.WriteLine(runtimeLog);
        }

        _testOutputHelper.WriteLine("[Debug messages]");
        foreach (var debugMessage in txResult.TransactionResult.GetDebugMessages())
        {
            _testOutputHelper.WriteLine(debugMessage);
        }

        _testOutputHelper.WriteLine("[Error messages]");
        foreach (var errorMessage in txResult.TransactionResult.GetErrorMessages())
        {
            _testOutputHelper.WriteLine(errorMessage);
        }

        _testOutputHelper.WriteLine($"Charged gas fee: {txResult.TransactionResult.GetChargedGasFee()}");
        
        txResult = await _solidityContractService.SendAsync("print_then_error",registration,Int8Type.GetByteStringFrom(10));
        _testOutputHelper.WriteLine($"print_then_error tx: {txResult.TransactionResult.TransactionId}");
        txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
        txResult.TransactionResult.Error.ShouldContain("math overflow");
    }

    [Fact]
    public async Task FlipperFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(flipperContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(flipperContract));

        var get = await _solidityContractService.CallAsync("get", registration);
        get.ShouldBe(BoolType.False);

        var flip = await _solidityContractService.SendAsync("flip", registration);
        _testOutputHelper.WriteLine($"flip tx: {flip.TransactionResult.TransactionId}");
        flip.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        _testOutputHelper.WriteLine("[Prints]");
        foreach (var print in flip.TransactionResult.GetPrints())
        {
            _testOutputHelper.WriteLine(print);
        }

        _testOutputHelper.WriteLine("[Runtime logs]");
        foreach (var runtimeLog in flip.TransactionResult.GetRuntimeLogs())
        {
            _testOutputHelper.WriteLine(runtimeLog);
        }

        _testOutputHelper.WriteLine("[Debug messages]");
        foreach (var debugMessage in flip.TransactionResult.GetDebugMessages())
        {
            _testOutputHelper.WriteLine(debugMessage);
        }

        _testOutputHelper.WriteLine("[Error messages]");
        foreach (var errorMessage in flip.TransactionResult.GetErrorMessages())
        {
            _testOutputHelper.WriteLine(errorMessage);
        }

        _testOutputHelper.WriteLine($"Charged gas fee: {flip.TransactionResult.GetChargedGasFee()}");

        
        get = await _solidityContractService.CallAsync("get", registration);
        get.ShouldBe(BoolType.False);
    }
    
    [Fact(DisplayName = "Test asserts contract.")]
    public async Task TestAsserts()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(assertsContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(assertsContract));
        
        // Query var.
        {
            var queriedVar = await _solidityContractService.CallAsync( "var", registration);
            Int64Type.From(queriedVar).Value.ShouldBe(1);
        }

        // Test test_assert_rpc method.
        {
            var txResult = await _solidityContractService.SendAsync("test_assert_rpc", registration);
            _testOutputHelper.WriteLine($"test_assert_rpc tx: {txResult.TransactionResult.TransactionId}");

            // txResult.TransactionResult.Error.ShouldContain("runtime_error: I refuse revert encountered in asserts.sol");
            // txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        // Test test_assert method.
        {
            var txResult = await _solidityContractService.SendAsync("test_assert", registration);
            _testOutputHelper.WriteLine($"test_assert tx: {txResult.TransactionResult.TransactionId}");

            // txResult.TransactionResult.Error.ShouldContain("runtime_error: assert failure in asserts.sol");
            // txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        // var is still 1.
        {
            var queriedVar = await _solidityContractService.CallAsync( "var", registration);
            Int64Type.From(queriedVar).Value.ShouldBe(1);
        }
    }

}