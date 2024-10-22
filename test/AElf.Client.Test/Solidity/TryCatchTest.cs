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

namespace AElf.Client.Test.Solidity;

public class TryCatchTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private ITryCatchCalleeStub _tryCatchCalleeStub;
    private ITryCatchCallerStub _tryCatchCallerStub;

    public TryCatchTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _deployService = GetRequiredService<IDeployContractService>();
        _genesisService = GetRequiredService<IGenesisService>();
        _tryCatchCalleeStub = GetRequiredService<ITryCatchCalleeStub>();
        _tryCatchCallerStub = GetRequiredService<ITryCatchCallerStub>();
    }

    [Fact]
    public async Task TryCatchFeatureTest()
    {
        var callee = await _tryCatchCalleeStub.DeployAsync();
        _testOutputHelper.WriteLine($"callee contract address: {callee.ToBase58()}");

        var caller = await _tryCatchCallerStub.DeployAsync();
        _testOutputHelper.WriteLine($"caller contract address: {caller.ToBase58()}");

        for (var in_out = 0; in_out < 5; in_out++) {
            _testOutputHelper.WriteLine("Testing case: " + in_out);
            var answer = await _tryCatchCallerStub.TestAsync(UInt128Type.GetByteStringFrom(in_out));
            _testOutputHelper.WriteLine($"TestAsync tx: {answer.TransactionResult.TransactionId}");
            answer.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            new IntegerTypeDecoder().Decode(answer.TransactionResult.ReturnValue.ToByteArray()).ShouldBe(in_out);
        }
    }

}