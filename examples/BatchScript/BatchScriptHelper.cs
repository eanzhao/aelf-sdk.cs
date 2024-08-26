using System.Text.Json;
using AElf;
using AElf.Client.Genesis;
using AElf.Runtime.WebAssembly;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Solang;
using Volo.Abp.DependencyInjection;

namespace BatchScript;

public interface IBatchScriptHelper
{
    Task<Address> DeployContract();
    Task SetContract(Address contractAddress);
    Address GetContractAddress();
    SolangABI GetSolangAbi();
    
}
public class BatchScriptHelper : IBatchScriptHelper, ISingletonDependency
{
    private ILogger<BatchScriptHelper> Logger { get; set; }
    private readonly TestContractOptions TestContractOptions;
    private readonly IDeployContractService _deployService;
    private Address ContractAddress { get; set; }
    private SolangABI SolangAbi { get; set; }

    public BatchScriptHelper(IServiceScopeFactory serviceScopeFactory,
        IDeployContractService deployService,
        IOptionsSnapshot<TestContractOptions> testContract,ILogger<BatchScriptHelper> _logger)
    {
        _deployService = deployService;
        TestContractOptions = testContract.Value;
        ServiceScopeFactory = serviceScopeFactory;
        Logger = _logger;
    }

    public IServiceScopeFactory ServiceScopeFactory { get; set; }

    public async Task<Address> DeployContract()
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TestContractOptions.TestContractPath);
        var input = new DeploySoliditySmartContractInput
        {
            Category = 1,
            Code = wasmCode.ToByteString(),
            Parameter = ByteString.Empty
        };
        ContractAddress = await _deployService.DeploySolidityContract(input);
        SolangAbi = solangAbi;
        Logger.LogInformation("Deploy contract {0}", ContractAddress.ToBase58());
        return ContractAddress;
    }

    public async Task SetContract(Address contractAddress)
    {
        ContractAddress = contractAddress;
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TestContractOptions.TestContractPath);
        SolangAbi = solangAbi;
    }

    public Address GetContractAddress()
    {
        return ContractAddress;
    }
    
    public SolangABI GetSolangAbi()
    {
        return SolangAbi;
    }

    private async Task<(WasmContractCode, SolangABI)> LoadWasmContractCodeAsync(string contractPath)
    {
        var abi = await File.ReadAllTextAsync(contractPath);
        var solangAbi = JsonSerializer.Deserialize<SolangABI>(abi);
        var code = ByteArrayHelper.HexStringToByteArray(solangAbi.Source.Wasm);
        var wasmCode = new WasmContractCode
        {
            Code = ByteString.CopyFrom(code),
            Abi = abi,
            CodeHash = Hash.LoadFromHex(solangAbi.Source.Hash)
        };
        return (wasmCode, solangAbi);
    }

}