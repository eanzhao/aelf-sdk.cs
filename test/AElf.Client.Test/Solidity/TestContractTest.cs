using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Client.Token;
using AElf.Contracts.MultiToken;
using AElf.Runtime.WebAssembly;
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
    private const string TokenContractPath = "contracts/MockToken.contract";

    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private readonly ITokenService _tokenService;
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly IMockTokenStub _mockTokenStub;
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
    }
    
    [Fact]
    public async Task<Address> DeployMockTokenContract()
    {
        var contractAddress = await _mockTokenStub.DeployAsync();
        _testOutputHelper.WriteLine($"MockToken contract address: {contractAddress.ToBase58()}");
        return contractAddress;
    }

    [Fact]
    public async Task<Address> InitializeMockTokenContract()
    {
        var contractAddress = await DeployMockTokenContract();
        var executionResult = await _mockTokenStub.InitializeAsync(Scale.TupleType.From(Scale.StringType.From("Elf token"),
            Scale.StringType.From("ELF")));
        _testOutputHelper.WriteLine(executionResult.TransactionResult.TransactionId.ToHex());
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var name = await _mockTokenStub.NameAsync();
        name.ShouldBe(Scale.StringType.GetBytesFrom("Elf token"));
        var symbol = await _mockTokenStub.SymbolAsync();
        symbol.ShouldBe(Scale.StringType.GetBytesFrom("ELF"));
        return contractAddress;
    }

    [Fact]
    public async Task InitializeMockContractWithoutDeploy()
    {
        var contractAddress = Address.FromBase58("2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc");
        _mockTokenStub.SetContractAddressToStub(contractAddress);
        var executionResult = await _mockTokenStub.InitializeAsync(Scale.TupleType.From(Scale.StringType.From("Elf token"),
            Scale.StringType.From("ELF")));
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
        var executionResult = await _mockTokenStub.MintAsync(Scale.TupleType.From(
            AddressType.FromBase58("2ceeqZ7iNTLXfzkmNzXCiPYiZTbkRAxH48FS7rBCX5qFtptajP"),
                IntegerType.From(100000000)));
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

        var executionResult = await _mockTokenStub.TransferAsync(Scale.TupleType.From(
            AddressType.FromBase58("2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd"),
            IntegerType.From(100000000)));
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
    
    // token GwsSp1MZPmkMvXdbfSCDydHhZtDpvqkFpmPvStYho288fb7QZ
    
    [Fact]
    public async Task<Address> InitialTest()
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TestContractPath);
        _solangAbi = solangAbi;
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58("2hqsqJndRAZGzk96fsEvyuVBTAvoBjcuwTjkuyJffBPueJFrLa");
        _solidityContractService =
            new SolidityContractService(_aelfClientService, contractAddress, _aelfClientConfigOptions);
        var selector = _solangAbi.GetSelector("initialize");
        var parameter = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Address.FromBase58(address)));

        var userBalance = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));

        var sendTxResult = await _solidityContractService.SendAsync(selector,
            parameter);
        sendTxResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var after = await _tokenService.GetTokenBalanceAsync("ELF",Address.FromBase58(address));
        _testOutputHelper.WriteLine(sendTxResult.TransactionResult.TransactionId.ToHex());
        _testOutputHelper.WriteLine($"{userBalance.Balance}");
        _testOutputHelper.WriteLine($"{after.Balance}");
        return contractAddress;
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
    public async Task GetAdmin()
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TestContractPath);
        _solangAbi = solangAbi;
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58("2hqsqJndRAZGzk96fsEvyuVBTAvoBjcuwTjkuyJffBPueJFrLa");
        _solidityContractService =
            new SolidityContractService(_aelfClientService, contractAddress, _aelfClientConfigOptions);
        var selector = _solangAbi.GetSelector("getAdmin");
        var callValue = await _solidityContractService.CallAsync(selector, ByteString.Empty);
    }
    
        
    [Fact]
    public async Task ChangeAdmin()
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TestContractPath);
        _solangAbi = solangAbi;
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var newAdmin = "6fR8YDWrGr1NHRjEMEEzmJnNbsTVuuPnSjAomVYEAXzbNCAdg";
        var contractAddress = Address.FromBase58("SsSqZWLf7Dk9NWyWyvDwuuY5nzn5n99jiscKZgRPaajZP5p8y");
        _solidityContractService =
            new SolidityContractService(_aelfClientService, contractAddress, _aelfClientConfigOptions);
        var selector = _solangAbi.GetSelector("changeAdmin");
        var parameter = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(Address.FromBase58(newAdmin)));

        var sendTxResult = await _solidityContractService.SendAsync(selector,
            parameter);
        sendTxResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        _testOutputHelper.WriteLine(sendTxResult.TransactionResult.TransactionId.ToHex());
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
    public async Task SetManyValue()
    {
        var tokenAddress = "SsSqZWLf7Dk9NWyWyvDwuuY5nzn5n99jiscKZgRPaajZP5p8y";
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TestContractPath);
        _solangAbi = solangAbi;
        var address = "2r896yKhHsoNGhyJVe4ptA169P6LMvsC94BxA7xtrifSHuSdyd";
        var contractAddress = Address.FromBase58(tokenAddress);
        _solidityContractService =
            new SolidityContractService(_aelfClientService, contractAddress, _aelfClientConfigOptions);
        var selector = _solangAbi.GetSelector("setManyValue");
        var parameter = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(10, 10));
        var sendTxResult = await _solidityContractService.SendAsync(selector, parameter);
        _testOutputHelper.WriteLine(sendTxResult.TransactionResult.TransactionId.ToHex());
        sendTxResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }
}