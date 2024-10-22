using System;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using Google.Protobuf.WellKnownTypes;
using Scale;
using Scale.Decoders;
using Shouldly;
using Solang;
using Xunit.Abstractions;
using AElf.Client.Test.contract;

namespace AElf.Client.Test.Solidity;

public class BuiltinsTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private IbuiltinsStub _builtinsStub;

    public BuiltinsTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _deployService = GetRequiredService<IDeployContractService>();
        _genesisService = GetRequiredService<IGenesisService>();
        _builtinsStub = GetRequiredService<IbuiltinsStub>();
    }

    [Fact]
    public async Task BuiltinsFeatureTest()
    {
        var contractAddress = await _builtinsStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");
        
        var contractInfo = await _genesisService.GetContractInfo(contractAddress); 
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);

        var input = StringType.GetByteStringFrom("Call me Ishmae.");
        var hashRiped160 = await _builtinsStub.Hash_ripemd160Async(input); 
        hashRiped160.ToHex().ShouldBe("0x0c8b641c461e3c7abbdabd7f12a8905ee480dadf");
        // 415084a5cb80e4983b92f7f130de53bd892904b2
        
        
        var hashSha256Async = await _builtinsStub.Hash_sha256Async(input); 
        hashSha256Async.ToHex().ShouldBe("0x458f3ceeeec730139693560ecf66c9c22d9c7bc7dcb0599e8e10b667dfeac043");
        //5a47b7e9a391663c72ce75c9cf54d5c3c5dd371be4a6fc08526622c98b2452d7
        
        var kecccak256Async = await _builtinsStub.Hash_kecccak256Async(input); 
        kecccak256Async.ToHex().ShouldBe("0x823ad8e1757b879aac338f9a18542928c668e479b37e4a56f024016215c5928c");
        //e76dce6aaec0a28b5cfead4a1b5f80bb9c76caaf262b22354ed4d7314c4bd70f
        
        var mrNow = await _builtinsStub.Mr_nowAsync();
        var dateTimeNow = Timestamp.FromDateTime(DateTime.UtcNow).Seconds;
        var getValue = new IntegerTypeDecoder().Decode(mrNow);
        getValue.ShouldBeLessThanOrEqualTo(dateTimeNow);
    }
}