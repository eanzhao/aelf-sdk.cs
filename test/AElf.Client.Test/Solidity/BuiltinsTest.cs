using System;
using System.Text;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Scale;
using Scale.Decoders;
using Shouldly;
using Solang;
using Xunit.Abstractions;

namespace AElf.Client.Test.Solidity;

public class BuiltinsTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;
    private string BuiltinsContract = "2nyC8hqq3pGnRu8gJzCsTaxXB6snfGxmL2viimKXgEfYWGtjEh";
    private string TestContractPath = "contracts/builtins.contract";

    public BuiltinsTest(ITestOutputHelper testOutputHelper)
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
    public async Task BuiltinsFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(BuiltinsContract), _aelfClientConfigOptions);
        
        var contractInfo = await _genesisService.GetContractInfo(Address.FromBase58(BuiltinsContract)); 
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(BuiltinsContract));

        var inputBytes = StringType.From("Call me Ishmael.");
        var hashRiped160 = await _solidityContractService.CallAsync("hash_ripemd160", registration, inputBytes.ByteStringValue); 
        hashRiped160.ToHex().ShouldBe("0c8b641c461e3c7abbdabd7f12a8905ee480dadf");
        //0x0c8b641c461e3c7abbdabd7f12a8905ee480dadf
        var hashSha256Async = await _solidityContractService.CallAsync("hash_sha256", registration, inputBytes.ByteStringValue); 
        hashSha256Async.ToHex().ShouldBe("458f3ceeeec730139693560ecf66c9c22d9c7bc7dcb0599e8e10b667dfeac043");
        //5a47b7e9a391663c72ce75c9cf54d5c3c5dd371be4a6fc08526622c98b2452d7

        var kecccak256Async = await _solidityContractService.CallAsync("hash_kecccak256", registration, inputBytes.ByteStringValue);
        kecccak256Async.ToHex().ShouldBe("823ad8e1757b879aac338f9a18542928c668e479b37e4a56f024016215c5928c");
        //e76dce6aaec0a28b5cfead4a1b5f80bb9c76caaf262b22354ed4d7314c4bd70f
        
        var mrNow = await _solidityContractService.CallAsync("mr_now", registration);
        var dateTimeNow = Timestamp.FromDateTime(DateTime.UtcNow).Seconds;
        var getValue = new IntegerTypeDecoder().Decode(mrNow);
        getValue.ShouldBeLessThanOrEqualTo(dateTimeNow);
    }
}