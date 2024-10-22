using System.Text;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Types;
using Scale;
using Scale.Decoders;
using Shouldly;
using Solang;
using Xunit.Abstractions;
using AElf.Client.Test.contract;
using Nethereum.ABI.Decoders;

namespace AElf.Client.Test.Solidity;

public class AssertsTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private IassertsStub _assertsStub;
    private IDebugBufferStub _debugBufferStub;
    private IreleaseStub _releaseStub;
    private IflipperStub _flipperStub;


    public AssertsTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _deployService = GetRequiredService<IDeployContractService>();
        _genesisService = GetRequiredService<IGenesisService>();
        _assertsStub = GetRequiredService<IassertsStub>();
        _debugBufferStub = GetRequiredService<IDebugBufferStub>();
        _releaseStub = GetRequiredService<IreleaseStub>();
        _flipperStub = GetRequiredService<IflipperStub>();
    }

    [Fact]
    public async Task AssertsFeatureTest()
    {
        var contractAddress1 = await _assertsStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress1.ToBase58()}");
        
        var value = await _assertsStub.VarAsync();
        new IntegerTypeDecoder().Decode(value).ShouldBe(1);

        //call方法进行assert？
        var stringType = await _assertsStub.Test_assert_rpcAsync();
        Encoding.UTF8.GetString(stringType).ShouldBe("I refuse");   
        
        var testAssert = await _assertsStub.Test_assertAsync();
        _testOutputHelper.WriteLine($"Set tx: {testAssert.TransactionResult.TransactionId}");
        testAssert.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
    }
    
    [Fact]
    public async Task DebugBufferFeatureTest()
    {
        var contractAddress = await _debugBufferStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");
        
        var result = await _debugBufferStub.Multiple_printsAsync();
        _testOutputHelper.WriteLine($"Set tx: {result.TransactionResult.TransactionId}");
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        var result2 = await _debugBufferStub.Multiple_prints_then_revertAsync();
        _testOutputHelper.WriteLine($"Set tx: {result2.TransactionResult.TransactionId}");
        result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        Encoding.UTF8.GetString(result2.TransactionResult.ReturnValue.ToByteArray()).ShouldBe("sesa!!!");
    }

    [Fact]
    public async Task ReleaseFeatureTest()
    {
        var contractAddress = await _releaseStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");

        var result = await _releaseStub.Print_then_errorAsync(Int8Type.GetByteStringFrom(1));
        _testOutputHelper.WriteLine($"Set tx: {result.TransactionResult.TransactionId}");
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        new IntegerTypeDecoder().Decode(result.TransactionResult.ReturnValue.ToByteArray()).ShouldBe(121);
        
        result = await _releaseStub.Print_then_errorAsync(Int8Type.GetByteStringFrom(10));
        _testOutputHelper.WriteLine($"Set tx: {result.TransactionResult.TransactionId}");
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.NodeValidationFailed);
    }

    [Fact]
    public async Task FlipperFeatureTest()
    {
        var contractAddress = await _flipperStub.DeployAsync();
        _testOutputHelper.WriteLine($"Flipper contract address: {contractAddress.ToBase58()}");

        var get = await _flipperStub.GetAsync();
        new BoolTypeDecoder().Decode(get).ShouldBeFalse();
        
        var flip = await _flipperStub.FlipAsync();
        _testOutputHelper.WriteLine($"Set tx: {flip.TransactionResult.TransactionId}");
        flip.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        get = await _flipperStub.GetAsync();
        new BoolTypeDecoder().Decode(get).ShouldBeTrue();
    }

}