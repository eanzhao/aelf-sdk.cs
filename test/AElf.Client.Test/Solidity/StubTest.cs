// using System.Threading.Tasks;
// using AElf.Runtime.WebAssembly.Types;
// using AElf.Types;
// using Google.Protobuf;
// using Scale.Encoders;
// using Shouldly;
//
// namespace AElf.Client.Test.Solidity;
//
// public class StubTest : AElfClientAbpContractServiceTestBase
// {
//     [Fact]
//     public async Task<IStorageStub> DeployStorageContractTest()
//     {
//         var storageStub = GetRequiredService<IStorageStub>();
//         await storageStub.DeployAsync();
//         return storageStub;
//     }
//
//     [Fact]
//     public async Task<IStorageStub> StoreTest()
//     {
//         var storageStub = await DeployStorageContractTest();
//         var parameter = ByteString.CopyFrom(new IntegerTypeEncoder().Encode(1616));
//         var result = await storageStub.StoreAsync(parameter);
//         result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
//         return storageStub;
//     }
//
//     [Fact]
//     public async Task RetrieveTest()
//     {
//         var storageStub = await StoreTest();
//         var result = await storageStub.RetrieveAsync();
//         result.ToInt64(false).ShouldBe(1616);
//     }
// }