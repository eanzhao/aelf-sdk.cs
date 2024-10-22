using System.Threading.Tasks;
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

public class ArrayStructMappingTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    internal readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;

    private Iarray_struct_mapping_storageStub _arrayStructMappingStorageStub;
    private string testContract = "sr4zX6E7yVVL7HevExVcWv2ru3HSZakhsJMXfzxzfpnXofnZw";

    public ArrayStructMappingTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _genesisService = GetRequiredService<IGenesisService>();
        _arrayStructMappingStorageStub = GetRequiredService<Iarray_struct_mapping_storageStub>();
    }
    
        
    [Fact]
    public async Task DeployContract()
    {
        var contractAddress = await _arrayStructMappingStorageStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");
        
        var contractInfo = await _genesisService.GetContractInfo(contractAddress); 
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);
    }

    [Fact]
    public async Task SetNumberTest()
    {
        _arrayStructMappingStorageStub.SetContractAddressToStub(Address.FromBase58(testContract));
        var contractAddress = await _arrayStructMappingStorageStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");
        
        var result = await _arrayStructMappingStorageStub.SetNumberAsync(Int64Type.From(10));
        var number = await _arrayStructMappingStorageStub.NumberAsync();
        _testOutputHelper.WriteLine($"SetNumber tx: {result.TransactionResult.TransactionId}");
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        new IntegerTypeDecoder().Decode(number).ShouldBe(10);
        _arrayStructMappingStorageStub.SetContractAddressToStub(contractAddress);
    }
    
    [Fact]
    public async Task SetGetTest()
    {
        _arrayStructMappingStorageStub.SetContractAddressToStub(Address.FromBase58(testContract));

        // set some values
        for (ulong array_no = 0; array_no < 2; array_no += 1) {
            for (ulong i = 0; i < 10; i += 1) {
                var index = 102 + i + array_no * 500;
                var val = 300331 + i;

                var result = await _arrayStructMappingStorageStub.SetAsync(TupleType.GetByteStringFrom(UInt64Type.From(array_no),
                    UInt64Type.From(index),UInt64Type.From(val)));
                _testOutputHelper.WriteLine($"Set tx: {result.TransactionResult.TransactionId}");
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }
        
        for (ulong array_no = 0; array_no < 2; array_no += 1) {
            for (ulong i = 0; i < 10; i += 1)
            {
                var index = 102 + i + array_no * 500;
                var output = await _arrayStructMappingStorageStub.GetAsync(TupleType.GetByteStringFrom(UInt64Type.From(array_no),
                    UInt64Type.From(index)));
                new IntegerTypeDecoder().Decode(output).ShouldBe(300331 + i);
            }
        }
    }
    
    [Fact]
    public async Task RmGetTest()
    {
        _arrayStructMappingStorageStub.SetContractAddressToStub(Address.FromBase58(testContract));

        var result = await _arrayStructMappingStorageStub.RmAsync(TupleType.GetByteStringFrom(UInt64Type.From(0),
            UInt64Type.From(104)));
        _testOutputHelper.WriteLine($"Set tx: {result.TransactionResult.TransactionId}");
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        for (ulong i = 0; i < 10; i += 1) {
            
            var output = await _arrayStructMappingStorageStub.GetAsync(TupleType.GetByteStringFrom(UInt64Type.From(0),
                UInt64Type.From(102 + i)));
            

            if (i != 2) {
                // new IntegerTypeDecoder().Decode(output).ShouldBe(300331 + i);
            } else {
                new IntegerTypeDecoder().Decode(output).ShouldBe(0);
            }
        }
    }

    [Fact]
    public async Task PopPushTest()
    {
        _arrayStructMappingStorageStub.SetContractAddressToStub(Address.FromBase58(testContract));

        var push = await _arrayStructMappingStorageStub.PushAsync();
        _testOutputHelper.WriteLine($"Set tx: {push.TransactionResult.TransactionId}");
        push.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var pop = await _arrayStructMappingStorageStub.PushAsync();
        _testOutputHelper.WriteLine($"Set tx: {pop.TransactionResult.TransactionId}");
        pop.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

}