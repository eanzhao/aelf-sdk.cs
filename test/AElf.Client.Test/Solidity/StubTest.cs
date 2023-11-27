using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Shouldly;

namespace AElf.Client.Test;

public class StubTest : AElfClientAbpContractServiceTestBase
{
    [Fact]
    public async Task<IStorageStub> DeployStorageContractTest()
    {
        var storageStub = GetRequiredService<IStorageStub>();
        await storageStub.DeployAsync();
        return storageStub;
    }

    [Fact]
    public async Task<IStorageStub> StoreTest()
    {
        var storageStub = await DeployStorageContractTest();
        var result = await storageStub.StoreAsync(1616.ToWebAssemblyUInt256().ToParameter());
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return storageStub;
    }

    [Fact]
    public async Task RetrieveTest()
    {
        var storageStub = await StoreTest();
        var result = await storageStub.RetrieveAsync();
        result.TransactionResult.ReturnValue.ToByteArray().ToInt64(false).ShouldBe(1616);
    }
}