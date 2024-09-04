using System.Linq;
using System.Threading.Tasks;
using AElf.SolidityContract;
using AElf.Types;
using AutoMapper.Internal;
using Google.Protobuf;
using Nethereum.ABI.Decoders;
using Scale;
using Scale.Decoders;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Client.Test.Solidity;

public class TestContractPipelineTest : TestContractTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    private readonly string TestScriptAddress = "2ceeqZ7iNTLXfzkmNzXCiPYiZTbkRAxH48FS7rBCX5qFtptajP";

    public TestContractPipelineTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }
    
    [Fact]
    public async Task TestContractPipeline()
    {
        var contractAddress = await _testContractStub.DeployAsync();

        // Check deployment info
        {
            var contractInfo = await _genesisService.GetContractInfo(contractAddress); 
            contractInfo.Category.ShouldBe(1);
            contractInfo.IsSystemContract.ShouldBeFalse();
            contractInfo.Version.ShouldBe(1);
        }
        
        // Call initialize method to set admin
        {
            var executionResult = await _testContractStub.InitializeAsync(AddressType.FromBase58(TestScriptAddress));
            _testOutputHelper.WriteLine($"initialize tx: {executionResult.TransactionResult.TransactionId}");
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var admin = await _testContractStub.GetAdminAsync();
            _testOutputHelper.WriteLine($"admin: {Address.FromBytes(admin).ToBase58()}");
        }
        
        // changeAdmin
        {
            var executionResult = await _testContractStub.ChangeAdminAsync(AddressType.FromBase58(TestScriptAddress));
            _testOutputHelper.WriteLine($"changeAdmin tx: {executionResult.TransactionResult.TransactionId}");
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            _testOutputHelper.WriteLine($"changeAdmin tx result: {executionResult.TransactionResult}");

            var admin = await _testContractStub.GetAdminAsync();
            _testOutputHelper.WriteLine($"admin: {Address.FromBytes(admin).ToBase58()}");
        }
        
        // setManyValue & getManyValue
        {
            var executionResult = await _testContractStub.SetManyValueAsync(TupleType.GetByteStringFrom(UInt8Type.GetByteStringFrom(10),
                UInt256Type.GetByteStringFrom(10000000000)));
            _testOutputHelper.WriteLine($"setManyValue tx: {executionResult.TransactionResult.TransactionId}");
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var value = await _testContractStub.GetManyValueAsync(UInt8Type.GetByteStringFrom(01));
            UInt256Type.FromBytes(value).Value.ShouldBe(10000000001);
            _testOutputHelper.WriteLine($"getManyValue: {UInt256Type.FromBytes(value).Value}");
        }
        
        // setStatus & getStatus
        {
            var executionResult = await _testContractStub.SetStatusAsync(BoolType.GetByteStringFrom(true));
            _testOutputHelper.WriteLine($"setStatus tx: {executionResult.TransactionResult.TransactionId}");
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var status = await _testContractStub.GetStatusAsync();
            _testOutputHelper.WriteLine($"getStatus: {new BoolTypeDecoder().Decode(status)}");
        }
        
        // score
        {
            var executionResult = await _testContractStub.ScoreAsync(ByteString.CopyFrom(new Card
            {
                n = Number.ace,
                s = Suit.hearts
            }.Encode()));
            _testOutputHelper.WriteLine($"score tx: {executionResult.TransactionResult.TransactionId}");
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var score = new UInt32Type();
            score.Create(executionResult.TransactionResult.ReturnValue.ToByteArray());
            score.Value.ShouldBe(14u);
        }
    }

    [Fact]
    public async Task TokenRelatedPipeline()
    {
        var contractAddress = await _testContractStub.DeployAsync();
        await _testContractStub.InitializeAsync(AddressType.FromBase58(TestScriptAddress));

        // Upload MockToken Contract before calling createToken
        {
            var (wasmCode, _) = await LoadWasmContractCodeAsync(TokenContractPath);
            var executionResult = await _genesisService.UploadSoliditySmartContract(new UploadSoliditySmartContractInput
            {
                Category = 1,
                Code = wasmCode.ToByteString()
            });
            _testOutputHelper.WriteLine($"UploadSoliditySmartContract tx: {executionResult.TransactionResult.TransactionId}");
            _testOutputHelper.WriteLine($"UploadSoliditySmartContract tx result: {executionResult.TransactionResult}");
        }

        // createToken
        {
            var executionResult = await _testContractStub.CreateTokenAsync(TupleType.GetByteStringFrom(StringType.GetByteStringFrom("Elf token"),
                StringType.GetByteStringFrom("ELF")));
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            _testOutputHelper.WriteLine($"createToken tx: {executionResult.TransactionResult.TransactionId}");
            _testOutputHelper.WriteLine($"createToken tx result: {executionResult.TransactionResult}");

            var tokenAddress = Address.FromBytes(executionResult.TransactionResult.ReturnValue.ToByteArray());
            _testOutputHelper.WriteLine($"token address: {tokenAddress.ToBase58()}");
        }
    }
}

public class Card
{
    public Number n { get; set; }
    public Suit s { get; set; }

    public byte[] Encode()
    {
        var numberBytes = EnumType<Number>.GetBytesFrom(n);
        var suitBytes =  EnumType<Suit>.GetBytesFrom(s);

        return numberBytes.Concat(suitBytes).ToArray();
    }

    public static Card Decode(byte[] data)
    {
        var offset = 0;
        var number = new EnumType<Number>();
        number.Create(data.Take(1).ToArray());
        offset += 1;
        var suit = new EnumType<Suit>();
        suit.Create(data.Skip(offset).Take(1).ToArray());

        return new Card { n = number, s = suit };
    }
}

public enum Number
{
    two,
    three,
    four,
    five,
    six,
    seven,
    eight,
    nine,
    ten,
    jack,
    queen,
    king,
    ace
}

public enum Suit
{
    club,
    diamonds,
    hearts,
    spades
}