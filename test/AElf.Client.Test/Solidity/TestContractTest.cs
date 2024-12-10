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
using Solang.Extensions;
using Xunit.Abstractions;
using AddressType = Scale.AddressType;

namespace AElf.Client.Test.Solidity;

public class TestContractTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private const string TestContractPath = "contracts/TestContractImplementation.contract";
    private string testContract = "owZisaahpior7HEqfwCvbSEiMTEQxYGhEyBXacpuCNkeoCZd5";

    private readonly IDeployContractService _deployService;
    internal readonly IGenesisService _genesisService;
    private readonly ITokenService _tokenService;
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
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
    }
    
    [Fact]
    public async Task<Address> DeployMockTokenContract()
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
    public async Task InitializeMockTokenContract()
    {
        var balance = await _tokenService.GetTokenBalanceAsync("ELF", Address.FromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"));
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));
        
        var executionResult = await _solidityContractService.SendAsync("initialize", registration, Scale.TupleType.GetByteStringFrom(Scale.StringType.GetByteStringFrom("Elf token"),
            Scale.StringType.GetByteStringFrom("ELF")));
        _testOutputHelper.WriteLine(executionResult.TransactionResult.TransactionId.ToHex());
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var afterBalance = await _tokenService.GetTokenBalanceAsync("ELF", Address.FromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"));
        _testOutputHelper.WriteLine($"{balance.Balance}");
        _testOutputHelper.WriteLine($"{afterBalance.Balance}");
        var name = await _solidityContractService.CallAsync("name", registration);
        name.ShouldBe(Scale.StringType.GetBytesFrom("Elf token"));
        var symbol = await _solidityContractService.CallAsync("symbol", registration);
        symbol.ShouldBe(Scale.StringType.GetBytesFrom("ELF"));
    }

    [Fact]
    public async Task MintTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));

        var executionResult = await _solidityContractService.SendAsync("mint", registration,
            TupleType<AddressType, UInt256Type>.GetByteStringFrom(
                AddressType.From(Address.FromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd")
                    .ToByteArray()),
                UInt256Type.From(100000000)));
        _testOutputHelper.WriteLine($"mint tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        var balance = await _solidityContractService.CallAsync("balanceof", registration,AddressType.GetByteStringFromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"));
        new IntegerTypeDecoder().Decode(balance).ShouldBe(100000000);
    }

    // [Fact]
    // public async Task TransferTest()
    // {
    //     await MintTest();
    //
    //     {
    //         var balance =
    //             await _mockTokenStub.BalanceOfAsync(
    //                 AddressType.GetByteStringFromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"));
    //         new IntegerTypeDecoder().Decode(balance).ShouldBe(0);
    //     }
    //
    //     var executionResult = await _mockTokenStub.TransferAsync(Scale.TupleType.GetByteStringFrom(
    //         AddressType.GetByteStringFromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"),
    //         UInt256Type.GetByteStringFrom(100000000)));
    //     _testOutputHelper.WriteLine($"transfer tx: {executionResult.TransactionResult.TransactionId}");
    //     executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    //
    //     {
    //         var balance =
    //             await _mockTokenStub.BalanceOfAsync(
    //                 AddressType.GetByteStringFromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"));
    //         new IntegerTypeDecoder().Decode(balance).ShouldBe(100000000);
    //     }
    // }
    
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
        
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));

        
        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
       
        var getAdmin = await _solidityContractService.CallAsync("getAdmin", registration);
        _testOutputHelper.WriteLine($"{Address.FromBytes(getAdmin).ToBase58()}");
        
     
        var executionResult = await _solidityContractService.SendAsync("changeAdmin", registration, AddressType.GetByteStringFromBase58(newAdmin));
        _testOutputHelper.WriteLine($"ChangeAdmin tx: {executionResult.TransactionResult.TransactionId}");
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        getAdmin = await _solidityContractService.CallAsync("getAdmin", registration);
        _testOutputHelper.WriteLine($"{Address.FromBytes(getAdmin).ToBase58()}");
    }
}