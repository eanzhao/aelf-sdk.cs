using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Client.Token;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Scale;
using Scale.Decoders;
using Shouldly;
using Xunit.Abstractions;
using Microsoft.Extensions.Options;
using Nethereum.ABI.Decoders;

namespace AElf.Client.Test.Solidity;

public class ChainExtensionTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;
    private IAElfAccountProvider _accountProvider;

    private string ChainExtenstionContract = "";
    private string IsContractOracle = "DHo2K7oUXXq3kJRs1JpuwqBJP56gqoaeSKFfuvr9x8svf3vEJ";
    private string MyToken = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc";
    private readonly string TestAddress = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
    public ChainExtensionTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _aelfClientService = GetRequiredService<IAElfClientService>();
        _aelfClientConfigOptions = GetRequiredService<IOptionsSnapshot<AElfClientConfigOptions>>().Value;
        _genesisService = GetRequiredService<IGenesisService>();
        _deployService = GetRequiredService<IDeployContractService>();
    }
    
    [Theory]
    [InlineData("contracts/chainExtension.contract")]
    [InlineData("contracts/IsContractOracle.contract")]
    [InlineData("contracts/mytoken.contract")]
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
    public async Task ChainExtensionFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(ChainExtenstionContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(ChainExtenstionContract));
        
        var contractInfo = await _genesisService.GetContractInfo(Address.FromBase58(ChainExtenstionContract));
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);

        var addr1 = Address.FromPublicKey(_accountProvider.GenerateNewKeyPair().PublicKey).ToByteArray();
        var addr2 = Address.FromPublicKey(_accountProvider.GenerateNewKeyPair().PublicKey).ToByteArray();

        var value = await _solidityContractService.SendAsync("fetch_random", registration,BytesType.From(addr1).ByteStringValue);
        _testOutputHelper.WriteLine($"Set tx: {value.TransactionResult.TransactionId}");
        value.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var value2 = await _solidityContractService.SendAsync("fetch_random", registration,BytesType.From(addr2).ByteStringValue);
        _testOutputHelper.WriteLine($"Set tx: {value.TransactionResult.TransactionId}");
        value.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var value3 = await _solidityContractService.SendAsync("fetch_random", registration,BytesType.From(addr1).ByteStringValue);
        _testOutputHelper.WriteLine($"Set tx: {value.TransactionResult.TransactionId}");
        value.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        value.TransactionResult.ReturnValue.ShouldBe(value3.TransactionResult.ReturnValue);
        value.TransactionResult.ReturnValue.ShouldNotBe(value2.TransactionResult.ReturnValue);
    }

    [Fact]
    public async Task IsContractFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(IsContractOracle), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(IsContractOracle));
        
        var isContract =
            await _solidityContractService.CallAsync("contract_oracle",registration, AddressType.GetByteStringFrom(Address.FromBase58(IsContractOracle)));
        new BoolTypeDecoder().Decode(isContract).ShouldBeTrue();


        isContract = await _solidityContractService.CallAsync("contract_oracle", registration,
            AddressType.GetByteStringFrom(
                await _genesisService.GetContractAddressByName(
                    HashHelper.ComputeFrom(AElfTokenConstants.TokenSmartContractName))));
        new BoolTypeDecoder().Decode(isContract).ShouldBeTrue();

        isContract = await _solidityContractService.CallAsync("contract_oracle", registration,AddressType.GetByteStringFromBase58(TestAddress));
        new BoolTypeDecoder().Decode(isContract).ShouldBeFalse();
    }

    [Fact]
    public async Task MyTokenFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(MyToken), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(MyToken));

        var res = await _solidityContractService.CallAsync("test", registration, TupleType<AddressType, BoolType>.GetByteStringFrom(
            AddressType.From(Address.FromBase58(TestAddress).ToByteArray()), BoolType.From(true)));
        
        Address.FromBytes(res).ToBase58().ShouldBe(TestAddress);

        res = await _solidityContractService.CallAsync("test", registration, TupleType<AddressType, BoolType>.GetByteStringFrom(
            AddressType.From(Address.FromBase58(TestAddress).ToByteArray()), BoolType.From(false)));
        Address.FromBytes(res).ToBase58().ShouldBe(TestAddress);
    }
}