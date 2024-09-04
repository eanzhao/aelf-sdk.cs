using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Client.Token;
using AElf.Contracts.MultiToken;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Nethereum.ABI;
using Scale;
using Scale.Decoders;
using Shouldly;
using Solang;
using Xunit.Abstractions;
using AddressType = Scale.AddressType;

namespace AElf.Client.Test.Solidity;

public class TestContractTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private const string TestContractPath = "contracts/TestContractImplementation.contract";
    internal const string TokenContractPath = "contracts/MockToken.contract";

    private readonly IDeployContractService _deployService;
    internal readonly IGenesisService _genesisService;
    private readonly ITokenService _tokenService;
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly IMockTokenStub _mockTokenStub;
    internal readonly ITestContractImplementationStub _testContractStub;
    private ISolidityContractService _solidityContractService;
    private IAElfAccountProvider _accountProvider;
    private SolangABI _solangAbi;

    public TestContractTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _deployService = GetRequiredService<IDeployContractService>();
        _genesisService = GetRequiredService<IGenesisService>();
        _tokenService = GetRequiredService<ITokenService>();
        _aelfClientService = GetRequiredService<IAElfClientService>();
        _aelfClientConfigOptions = GetRequiredService<IOptionsSnapshot<AElfClientConfigOptions>>().Value;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _mockTokenStub = GetRequiredService<IMockTokenStub>();
        _testContractStub = GetRequiredService<ITestContractImplementationStub>();
    }
    
    [Fact]
    public async Task<Address> DeployMockTokenContract()
    {
        var contractAddress = await _mockTokenStub.DeployAsync();
        _testOutputHelper.WriteLine($"MockToken contract address: {contractAddress.ToBase58()}");
        
        var contractInfo = await _genesisService.GetContractInfo(contractAddress); 
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);
        
        return contractAddress;
    }

    [Fact]
    public async Task<Address> InitializeMockTokenContract()
    {
        var balance = await _tokenService.GetTokenBalanceAsync("ELF", Address.FromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"));
        var contractAddress = await DeployMockTokenContract();
        var executionResult = await _mockTokenStub.InitializeAsync(Scale.TupleType.GetByteStringFrom(Scale.StringType.GetByteStringFrom("Elf token"),
            Scale.StringType.GetByteStringFrom("ELF")));
        _testOutputHelper.WriteLine(executionResult.TransactionResult.TransactionId.ToHex());
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var afterBalance = await _tokenService.GetTokenBalanceAsync("ELF", Address.FromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"));
        _testOutputHelper.WriteLine($"{balance.Balance}");
        _testOutputHelper.WriteLine($"{afterBalance.Balance}");
        var name = await _mockTokenStub.NameAsync();
        name.ShouldBe(Scale.StringType.GetBytesFrom("Elf token"));
        var symbol = await _mockTokenStub.SymbolAsync();
        symbol.ShouldBe(Scale.StringType.GetBytesFrom("ELF"));
        return contractAddress;
    }

    [Fact]
    public async Task InitializeMockContractWithoutDeploy()
    {
        var contractAddress = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS");
        _mockTokenStub.SetContractAddressToStub(contractAddress);
        var executionResult = await _mockTokenStub.InitializeAsync(Scale.TupleType.GetByteStringFrom(Scale.StringType.GetByteStringFrom("Elf token"),
            Scale.StringType.GetByteStringFrom("ELF")));
        _testOutputHelper.WriteLine($"initialize tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var name = await _mockTokenStub.NameAsync();
        name.ShouldBe(Scale.StringType.GetBytesFrom("Elf token"));
        var symbol = await _mockTokenStub.SymbolAsync();
        symbol.ShouldBe(Scale.StringType.GetBytesFrom("ELF"));
    }

    [Fact]
    public async Task<Address> MintTest()
    {
        var contractAddress = await InitializeMockTokenContract();
        var executionResult = await _mockTokenStub.MintAsync(Scale.TupleType.GetByteStringFrom(
            AddressType.FromBase58("2ceeqZ7iNTLXfzkmNzXCiPYiZTbkRAxH48FS7rBCX5qFtptajP"),
                UInt256Type.GetByteStringFrom(100000000)));
        _testOutputHelper.WriteLine($"mint tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var balance = await _mockTokenStub.BalanceOfAsync(AddressType.FromBase58("2ceeqZ7iNTLXfzkmNzXCiPYiZTbkRAxH48FS7rBCX5qFtptajP"));
        ((long)new IntegerTypeDecoder().Decode(balance)).ShouldBePositive();
        return contractAddress;
    }

    [Fact]
    public async Task TransferTest()
    {
        await MintTest();

        {
            var balance =
                await _mockTokenStub.BalanceOfAsync(
                    AddressType.FromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"));
            new IntegerTypeDecoder().Decode(balance).ShouldBe(0);
        }

        var executionResult = await _mockTokenStub.TransferAsync(Scale.TupleType.GetByteStringFrom(
            AddressType.FromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"),
            UInt256Type.GetByteStringFrom(100000000)));
        _testOutputHelper.WriteLine($"transfer tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        {
            var balance =
                await _mockTokenStub.BalanceOfAsync(
                    AddressType.FromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"));
            new IntegerTypeDecoder().Decode(balance).ShouldBe(100000000);
        }
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
    
    [Fact]
    public async Task<Address> DeployTestContractTest()
    {
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        var contractAddress = await _testContractStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");
      
        var contractInfo = await _genesisService.GetContractInfo(contractAddress); 
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);

               
        var after = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testOutputHelper.WriteLine($"{userBalance.Balance}");
        _testOutputHelper.WriteLine($"{after.Balance}");
        
        return contractAddress;
    }

    [Fact]
    public async Task SetAdmin()
    {
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58("2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ");
        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        
        _testContractStub.SetContractAddressToStub(contractAddress);
        var executionResult = await _testContractStub.InitializeAsync(AddressType.FromBase58(address));
        _testOutputHelper.WriteLine($"SetAdmin tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var after = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testOutputHelper.WriteLine($"{userBalance.Balance}");
        _testOutputHelper.WriteLine($"{after.Balance}");

        var getAdmin = await _testContractStub.GetAdminAsync();
        _testOutputHelper.WriteLine($"{Address.FromBytes(getAdmin).ToBase58()}");
    }

    // token GwsSp1MZPmkMvXdbfSCDydHhZtDpvqkFpmPvStYho288fb7QZ
    
    [Fact]
    public async Task SetManyValueAsyncTest()
    {
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58("2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ");
        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));

        _testContractStub.SetContractAddressToStub(contractAddress);
        var executionResult = await _testContractStub.SetManyValueAsync(Scale.TupleType.GetByteStringFrom(UInt8Type.GetByteStringFrom(10),
            UInt256Type.GetByteStringFrom(100000000)));
        _testOutputHelper.WriteLine($"SetManyValueAsync tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var after = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testOutputHelper.WriteLine($"{userBalance.Balance}");
        _testOutputHelper.WriteLine($"{after.Balance}");

        var getValue = await _testContractStub.GetManyValueAsync(UInt8Type.GetByteStringFrom(1));
        _testOutputHelper.WriteLine($"{new IntegerTypeDecoder().Decode(getValue)}");
    }

    [Fact]
    public async Task FeeTest()
    {
        var txId = "34aa71e011bf2764ff9847386d30ec566d3efcdee61eec7a4d0e6bde585e5ed6";
        var result = await _aelfClientService.GetTransactionResultAsync(txId, _aelfClientConfigOptions.ClientAlias);
        var gasFeeLogs = result.Logs.First(l => l.Name.Contains("GasFeeEstimated")).NonIndexed;
        var feeChargeLog = result.Logs.First(l => l.Name.Equals("TransactionFeeCharged")).NonIndexed;
        var fee = TransactionFeeCharged.Parser.ParseFrom(feeChargeLog).Amount;
        _testOutputHelper.WriteLine($"{fee}");

        var transferLogs = result.Logs.First(l => l.Name.Contains("Transferred"));
        var nonIndexed = Transferred.Parser.ParseFrom(transferLogs.NonIndexed).Amount;
        var from = new Address();
        var to = new Address();
        var symbol = "";
        foreach (var indexed in transferLogs.Indexed)
        {
            var transferredIndexed = Transferred.Parser.ParseFrom(indexed);
            if (transferredIndexed.Symbol.Equals(""))
            {
                from = transferredIndexed.From ?? from;
                to = transferredIndexed.To ?? to;
            }
            else
                symbol = transferredIndexed.Symbol;
        }
        
        _testOutputHelper.WriteLine($"{nonIndexed}");
        _testOutputHelper.WriteLine($"{from}");
        _testOutputHelper.WriteLine($"{to}");
        _testOutputHelper.WriteLine($"{symbol}");
    }
    // 238295970
    // 16260000 + 87999252632986288 + 87999252394690320
    
        
    [Fact]
    public async Task ChangeAdmin()
    {
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var newAdmin = "6fR8YDWrGr1NHRjEMEEzmJnNbsTVuuPnSjAomVYEAXzbNCAdg";        
        var contractAddress = Address.FromBase58("2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ");
        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testContractStub.SetContractAddressToStub(contractAddress);

        var getAdmin = await _testContractStub.GetAdminAsync();
        _testOutputHelper.WriteLine($"{Address.FromBytes(getAdmin).ToBase58()}");
        
        _testContractStub.SetContractAddressToStub(contractAddress);
        var executionResult = await _testContractStub.ChangeAdminAsync(AddressType.FromBase58(newAdmin));
        _testOutputHelper.WriteLine($"ChangeAdmin tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        getAdmin = await _testContractStub.GetAdminAsync();
        _testOutputHelper.WriteLine($"{Address.FromBytes(getAdmin).ToBase58()}");
    }

    [Fact]
    public async Task Mint()
    {
        var tokenAddress = "GwsSp1MZPmkMvXdbfSCDydHhZtDpvqkFpmPvStYho288fb7QZ";
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TokenContractPath);
        _solangAbi = solangAbi;
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58(tokenAddress);
        _solidityContractService =
            new SolidityContractService(_aelfClientService, contractAddress, _aelfClientConfigOptions);
        var selector = _solangAbi.GetSelector("mint");
        var parameter = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Address.FromBase58(address), 100000000));
        var sendTxResult = await _solidityContractService.SendAsync(selector, parameter);
        sendTxResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        _testOutputHelper.WriteLine(sendTxResult.TransactionResult.TransactionId.ToHex());
        
        selector = _solangAbi.GetSelector("balanceOf");
        parameter = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Address.FromBase58(address)));

        var balanceOnERC20 = await _solidityContractService.CallAsync(selector, parameter);
        
    }

    [Fact]
    public async Task ErrorTest()
    {
        var address = "6fR8YDWrGr1NHRjEMEEzmJnNbsTVuuPnSjAomVYEAXzbNCAdg";
        var contractAddress = Address.FromBase58("2NxwCPAGJr4knVdmwhb1cK7CkZw5sMJkRDLnT7E2GoDP2dy5iZ");
        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testContractStub.SetContractAddressToStub(contractAddress);

        var getAdmin = await _testContractStub.GetAdminAsync();
        _testOutputHelper.WriteLine($"{Address.FromBytes(getAdmin).ToBase58()}");
        
        _testContractStub.SetContractAddressToStub(contractAddress);
        var executionResult = await _testContractStub.InitializeAsync(AddressType.FromBase58(address));
        _testOutputHelper.WriteLine($"SetAdmin tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        getAdmin = await _testContractStub.GetAdminAsync();
        _testOutputHelper.WriteLine($"{Address.FromBytes(getAdmin).ToBase58()}");
    }
    
    [Fact]
    public async Task StructTest()
    {
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58("2nyC8hqq3pGnRu8gJzCsTaxXB6snfGxmL2viimKXgEfYWGtjEh");
        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testContractStub.SetContractAddressToStub(contractAddress);

        var executionResult = await _testContractStub.TestAsync(Scale.TupleType.GetByteStringFrom(AddressType.FromBase58(address), UInt256Type.GetByteStringFrom(10)));
        _testOutputHelper.WriteLine($"SetStruct tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var after = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testOutputHelper.WriteLine($"{userBalance.Balance}");
        _testOutputHelper.WriteLine($"{after.Balance}");

        var getTest = await _testContractStub.GetTestAsync(AddressType.FromBase58(address));
        _testOutputHelper.WriteLine($"{new IntegerTypeDecoder().Decode(getTest)}");
    }
    
    [Fact]
    public async Task AddTest()
    {
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58("2nyC8hqq3pGnRu8gJzCsTaxXB6snfGxmL2viimKXgEfYWGtjEh");
        _testContractStub.SetContractAddressToStub(contractAddress);
        var result = await _testContractStub.AddAsync(Scale.TupleType.GetByteStringFrom(Scale.TupleType.GetByteStringFrom(UInt256Type.GetByteStringFrom(10)), UInt256Type.GetByteStringFrom(20)));
        new IntegerTypeDecoder().Decode(result).ShouldBe(30);
    }

    [Fact]
    public async Task EnumTest()
    {
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58("xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt");
        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testContractStub.SetContractAddressToStub(contractAddress);
        var executionResult = await _testContractStub.ScoreAsync(Scale.TupleType.GetByteStringFrom());
        _testOutputHelper.WriteLine($"ScoreAsync tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var after = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testOutputHelper.WriteLine($"{userBalance.Balance}");
        _testOutputHelper.WriteLine($"{after.Balance}");

        var getTest = await _testContractStub.GetTestAsync(AddressType.FromBase58(address));
        _testOutputHelper.WriteLine($"{new IntegerTypeDecoder().Decode(getTest)}");
    }

    [Fact]
    public async Task BoolTest()
    {
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58("xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt");
        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testContractStub.SetContractAddressToStub(contractAddress);
        var executionResult = await _testContractStub.SetStatusAsync(Scale.BoolType.GetByteStringFrom(true));
        _testOutputHelper.WriteLine($"SetStatus tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var after = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testOutputHelper.WriteLine($"{userBalance.Balance}");
        _testOutputHelper.WriteLine($"{after.Balance}");
        
        var getStatus = await _testContractStub.GetStatusAsync();
        _testOutputHelper.WriteLine($"{new  IntegerTypeDecoder().Decode(getStatus)}");
        new IntegerTypeDecoder().Decode(getStatus).ShouldBe(1);
    }
    
    [Fact]
    public async Task GetConstantAsyncTest()
    {
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58("xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt");
        _testContractStub.SetContractAddressToStub(contractAddress);

        var getConstant = await _testContractStub.GetConstantAsync();
        _testOutputHelper.WriteLine($"{Encoding.UTF8.GetString(getConstant)}");
    }

    [Fact]
    public async Task CreateTokenTest()
    {
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58("xsnQafDAhNTeYcooptETqWnYBksFGGXxfcQyJJ5tmu6Ak9ZZt");
        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testContractStub.SetContractAddressToStub(contractAddress);
        
        var executionResult = await _testContractStub.CreateTokenAsync(Scale.TupleType.GetByteStringFrom(Scale.StringType.GetByteStringFrom("Elf token"),
            Scale.StringType.GetByteStringFrom("ELF")));
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        _testOutputHelper.WriteLine($"CreateToken tx: {executionResult.TransactionResult.TransactionId}");
        
        var returnValue = executionResult.TransactionResult.ReturnValue;
        // _testOutputHelper.WriteLine($"{Address.FromBytes(returnValue).ToBase58()}");
    }
}