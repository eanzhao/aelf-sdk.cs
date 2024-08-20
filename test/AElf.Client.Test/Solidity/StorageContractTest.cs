using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Runtime.WebAssembly.Types;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Scale.Encoders;
using Shouldly;
using Solang;

namespace AElf.Client.Test.Solidity;

public class StorageContractTest : AElfClientAbpContractServiceTestBase
{
    private const string StorageContractPath = "contracts/Storage.contract";

    private readonly IDeployContractService _deployService;
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;

    public StorageContractTest()
    {
        _deployService = GetRequiredService<IDeployContractService>();
        _aelfClientService = GetRequiredService<IAElfClientService>();
        _aelfClientConfigOptions = GetRequiredService<IOptionsSnapshot<AElfClientConfigOptions>>().Value;
    }

    [Fact]
    public async Task<Address> DeployStorageContractTest()
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(StorageContractPath);
        _solangAbi = solangAbi;
        var input = new DeploySoliditySmartContractInput
        {
            Category = 1,
            Code = wasmCode.ToByteString(),
            Parameter = ByteString.Empty
        };
        var contractAddress = await _deployService.DeploySolidityContract(input);
        contractAddress.Value.ShouldNotBeEmpty();
        return contractAddress;
    }

    [Fact]
    public async Task<Address> StoreTest()
    {
        var contractAddress = await DeployStorageContractTest();
        _solidityContractService =
            new SolidityContractService(_aelfClientService, contractAddress, _aelfClientConfigOptions);
        var selector = _solangAbi.GetSelector("store");
        var parameter = ByteString.CopyFrom(new IntegerTypeEncoder().Encode(1616));
        var sendTxResult = await _solidityContractService.SendAsync(selector, parameter);
        sendTxResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return contractAddress;
    }

    [Fact]
    public async Task<Address> RetrieveTest()
    {
        var contractAddress = await StoreTest();
        _solidityContractService =
            new SolidityContractService(_aelfClientService, contractAddress, _aelfClientConfigOptions);
        var sendTxResult = await _solidityContractService.SendAsync(_solangAbi.GetSelector("retrieve"));
        sendTxResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        sendTxResult.TransactionResult.ReturnValue.ToByteArray().ToInt64(false).ShouldBe(1616);
        return contractAddress;
    }
}