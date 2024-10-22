using System.Numerics;
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
using AElf.Client.Token;
using AElf.Contracts.MultiToken;

namespace AElf.Client.Test.Solidity;

public class DelegateCallTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string TestAddress = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";

    private readonly IDeployContractService _deployService;
    private readonly ITokenService _tokenService;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;

    private IDelegateeStub _delegateeStub;
    private IDelegatorStub _delegatorStub;
    private IdestructStub _destructStub;


    public DelegateCallTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _tokenService = GetRequiredService<ITokenService>();
        _delegateeStub = GetRequiredService<IDelegateeStub>();
        _delegatorStub = GetRequiredService<IDelegatorStub>();
        _destructStub = GetRequiredService<IdestructStub>();
    }

    [Fact]
    public async Task DelegateCallFeatureTest()
    {
        var delegateeAddress = await _delegateeStub.DeployAsync();
        _testOutputHelper.WriteLine($"delegatee contract address: {delegateeAddress.ToBase58()}");
        var delegatorAddress = await _delegatorStub.DeployAsync();
        _testOutputHelper.WriteLine($"delegator contract address: {delegatorAddress.ToBase58()}");
        
        var value = new BigIntValue(1000000);
        var arg = new BigIntValue(123456789);
        
        var result = await _delegatorStub.SetVarsAsync(
            TupleType.GetByteStringFrom(AddressType.GetByteStringFrom(delegateeAddress), 
                UIntType.GetByteStringFrom(arg)));
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);


        var num = await _delegateeStub.NumAsync();
        new IntegerTypeDecoder().Decode(num).ShouldBe(new BigInteger(0));
        
        var balance = await _delegateeStub.ValueAsync();
        new IntegerTypeDecoder().Decode(balance).ShouldBe(new BigInteger(0));
        
        var sender = await _delegateeStub.SenderAsync();
        // Address.FromBytes(sender).ShouldBe(Address.FromBase58(TestAddress));

        // Storage of the delegator must have changed
        num = await _delegatorStub.NumAsync();
        // new IntegerTypeDecoder().Decode(num).ShouldBe(arg);

        balance = await _delegatorStub.ValueAsync();
        // new IntegerTypeDecoder().Decode(balance).ShouldBe(value);
        sender = await _delegateeStub.SenderAsync();
        Address.FromBytes(sender).ShouldBe(Address.FromBase58(TestAddress));
    }
    
    [Fact]
    public async Task DestructFeatureTest()
    {
        var address = await _destructStub.DeployAsync();
        _testOutputHelper.WriteLine($"destruct contract address: {address.ToBase58()}");
        var value = await _destructStub.HelloAsync();
        Encoding.UTF8.GetString(value).ShouldBe("Hello"); // 为啥会有个空格？

        await _tokenService.TransferAsync(new TransferInput
        {
            Amount = 100000000,
            Symbol = "ELF",
            To = Address.FromBase58(address.ToBase58())
        });

        var result = await _destructStub.SelfterminateAsync(AddressType.GetByteStringFromBase58(TestAddress));
        _testOutputHelper.WriteLine($"Set tx: {result.TransactionResult.TransactionId}");
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }
}