using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Genesis;
using AElf.Client.Token;
using AElf.Types;
using Scale;
using Scale.Decoders;
using Shouldly;
using Solang;
using Xunit.Abstractions;
using AElf.Client.Test.contract;
using Nethereum.ABI.Decoders;

namespace AElf.Client.Test.Solidity;

public class ChainExtensionTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly IGenesisService _genesisService;
    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private IChainExtensionStub _chainExtensionStub;
    private IIsContractOracleStub _isContractOracleStub;
    private ImytokenStub _mytokenStub;
    private readonly string TestAddress = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";

    public ChainExtensionTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _genesisService = GetRequiredService<IGenesisService>();
        _chainExtensionStub = GetRequiredService<IChainExtensionStub>();
        _isContractOracleStub = GetRequiredService<IIsContractOracleStub>();
        _mytokenStub = GetRequiredService<ImytokenStub>();
    }

    [Fact]
    public async Task ChainExtensionFeatureTest()
    {
        var contractAddress = await _chainExtensionStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");

        var contractInfo = await _genesisService.GetContractInfo(contractAddress);
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);

        var addr1 = Address.FromPublicKey(_accountProvider.GenerateNewKeyPair().PublicKey).ToByteArray();
        var addr2 = Address.FromPublicKey(_accountProvider.GenerateNewKeyPair().PublicKey).ToByteArray();

        var value = await _chainExtensionStub.Fetch_randomAsync(BytesType.From(addr1).ByteStringValue);
        _testOutputHelper.WriteLine($"Set tx: {value.TransactionResult.TransactionId}");
        value.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var value2 = await _chainExtensionStub.Fetch_randomAsync(BytesType.From(addr2).ByteStringValue);
        _testOutputHelper.WriteLine($"Set tx: {value2.TransactionResult.TransactionId}");
        value2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var value3 = await _chainExtensionStub.Fetch_randomAsync(BytesType.From(addr1).ByteStringValue);
        _testOutputHelper.WriteLine($"Set tx: {value3.TransactionResult.TransactionId}");
        value3.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        value.TransactionResult.ReturnValue.ShouldBe(value3.TransactionResult.ReturnValue);
        value.TransactionResult.ReturnValue.ShouldNotBe(value2.TransactionResult.ReturnValue);
    }

    [Fact]
    public async Task IsContractFeatureTest()
    {
        var contractAddress = await _isContractOracleStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");

        var isContract =
            await _isContractOracleStub.Contract_oracleAsync(AddressType.GetByteStringFrom(contractAddress));
        new BoolTypeDecoder().Decode(isContract).ShouldBeTrue();


        isContract = await _isContractOracleStub.Contract_oracleAsync(
            AddressType.GetByteStringFrom(
                await _genesisService.GetContractAddressByName(
                    HashHelper.ComputeFrom(AElfTokenConstants.TokenSmartContractName))));
        new BoolTypeDecoder().Decode(isContract).ShouldBeTrue();

        isContract = await _isContractOracleStub.Contract_oracleAsync(AddressType.GetByteStringFromBase58(TestAddress));
        new BoolTypeDecoder().Decode(isContract).ShouldBeFalse();
    }

    [Fact]
    public async Task MyTokenFeatureTest()
    {
        var contractAddress = await _mytokenStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");

        var res = await _mytokenStub.TestAsync(TupleType.GetByteStringFrom(
            AddressType.GetByteStringFromBase58(TestAddress), BoolType.GetByteStringFrom(true)));
        
        Address.FromBytes(res).ToBase58().ShouldBe(TestAddress);

        res = await _mytokenStub.TestAsync(TupleType.GetByteStringFrom(
            AddressType.GetByteStringFromBase58(TestAddress), BoolType.GetByteStringFrom(false)));
        Address.FromBytes(res).ToBase58().ShouldBe(TestAddress);
    }
}