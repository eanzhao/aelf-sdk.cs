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
using Shouldly;
using Solang;
using Xunit.Abstractions;

namespace AElf.Client.Test.Solidity;

public class ArrayStructMappingTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly IGenesisService _genesisService;
    private readonly IDeployContractService _deployService;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private string testContract = "2WHXRoLRjbUTDQsuqR5CntygVfnDb125qdJkudev4kVNbLhTdG";
    private string TestContractPath = "contracts/array_struct_mapping_storage.contract";

    public ArrayStructMappingTest(ITestOutputHelper testOutputHelper)
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

    [Fact(DisplayName = "Test setNumber function.")]
    public async Task SetNumberTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        const int number = 2147483647;
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));
        // Execute setNumber function.
        var txResult = await _solidityContractService.SendAsync("setNumber", registration,
            Int64Type.GetByteStringFrom(number));
        txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        _testOutputHelper.WriteLine(txResult.TransactionResult.TransactionId.ToHex());
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
        // Read number property.
        var returnValue = await _solidityContractService.CallAsync("number", registration);
        Int64Type.From(returnValue).Value.ShouldBe(number);
    }

    [Fact(DisplayName = "Test struct map.")]
    public async Task StructMapTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        // let's add two elements to our array
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));
        // Execute setNumber function.
        var txResult1 = await _solidityContractService.SendAsync("push", registration);
        txResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        _testOutputHelper.WriteLine(txResult1.TransactionResult.TransactionId.ToHex());
        var txResult2 = await _solidityContractService.SendAsync("push", registration);
        txResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        _testOutputHelper.WriteLine(txResult2.TransactionResult.TransactionId.ToHex());

        // set some values
        for (var arrayNumber = 0; arrayNumber < 2; arrayNumber++)
        {
            for (var i = 0; i < 10; i++)
            {
                var index = 102 + i + arrayNumber * 500;
                var val = 300331 + i;
                var txResult = await _solidityContractService.SendAsync("set", registration,
                    TupleType<UInt64Type, UInt64Type, UInt64Type>.GetByteStringFrom(
                        UInt64Type.From((ulong)arrayNumber),
                        UInt64Type.From((ulong)index),
                        UInt64Type.From((ulong)val)
                    ));

                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                _testOutputHelper.WriteLine(txResult.TransactionResult.TransactionId.ToHex());

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
        }

        // test our values
        for (var arrayNumber = 0; arrayNumber < 2; arrayNumber++)
        {
            for (var i = 0; i < 10; i++)
            {
                var returnValue = await _solidityContractService.CallAsync("get", registration,
                    TupleType<UInt64Type, UInt64Type>.GetByteStringFrom(
                        UInt64Type.From((ulong)arrayNumber),
                        UInt64Type.From((ulong)(102 + i + arrayNumber * 500))
                    ));
                var output = UInt64Type.From(returnValue);
                output.Value.ShouldBe((ulong)(300331 + i));
            }
        }

        // delete one and try again
        var result = await _solidityContractService.SendAsync("rm", registration,
            TupleType<UInt64Type, UInt64Type>.GetByteStringFrom(
                UInt64Type.From(0),
                UInt64Type.From(104)
            ));
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        _testOutputHelper.WriteLine(result.TransactionResult.TransactionId.ToHex());

        _testOutputHelper.WriteLine("[Prints]");
        foreach (var print in result.TransactionResult.GetPrints())
        {
            _testOutputHelper.WriteLine(print);
        }

        _testOutputHelper.WriteLine("[Runtime logs]");
        foreach (var runtimeLog in result.TransactionResult.GetRuntimeLogs())
        {
            _testOutputHelper.WriteLine(runtimeLog);
        }

        _testOutputHelper.WriteLine("[Debug messages]");
        foreach (var debugMessage in result.TransactionResult.GetDebugMessages())
        {
            _testOutputHelper.WriteLine(debugMessage);
        }

        _testOutputHelper.WriteLine("[Error messages]");
        foreach (var errorMessage in result.TransactionResult.GetErrorMessages())
        {
            _testOutputHelper.WriteLine(errorMessage);
        }

        _testOutputHelper.WriteLine($"Charged gas fee: {result.TransactionResult.GetChargedGasFee()}");

        for (var i = 0; i < 10; i++)
        {
            var returnValue = await _solidityContractService.CallAsync("get", registration,
                TupleType<UInt64Type, UInt64Type>.GetByteStringFrom(
                    UInt64Type.From(0),
                    UInt64Type.From((ulong)(102 + i))
                ));
            var output = UInt64Type.From(returnValue);
            if (i != 2)
            {
                output.Value.ShouldBe((ulong)(300331 + i));
            }
            else
            {
                output.Value.ShouldBe((ulong)0);
            }
        }
    }
}