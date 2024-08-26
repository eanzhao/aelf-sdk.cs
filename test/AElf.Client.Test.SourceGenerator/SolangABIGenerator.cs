using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Solang;

namespace AElf.Client.Test.SourceGenerator;

[Generator]
public class SolangAbiGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var contractFiles =
            context.AdditionalFiles.Where(at => at.Path.EndsWith(".contract"));
        foreach (var contractFile in contractFiles)
        {
            ProcessContractFile(contractFile, context);
        }
    }

    private void ProcessContractFile(AdditionalText contractFile, GeneratorExecutionContext context)
    {
        var json = contractFile.GetText(context.CancellationToken)?.ToString();
        if (json == null) return;

        var solangAbi = JsonSerializer.Deserialize<SolangABI>(json);
        var contractName = solangAbi.Contract.Name;

        var stringBuilder = new StringBuilder($@"using System;
using System.IO;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Runtime.WebAssembly;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Test;

#nullable enable

public interface I{contractName}Stub
{{
    void SetContractAddressToStub(Address contractAddress);
    Task<Address> DeployAsync();
");

        var interfaceList = new List<string>();
        foreach (var message in solangAbi.Spec.Messages.Where(message => !interfaceList.Contains(message.Label)))
        {
            stringBuilder.Append(GenerateMethodInterface(solangAbi, message.Label));
            interfaceList.Add(message.Label);
        }

        stringBuilder.Append($@"
}}

public partial class {contractName}Stub : I{contractName}Stub, ITransientDependency
{{
    private readonly IDeployContractService _deployContractService;
    private readonly IAElfClientService _clientService;
    private readonly AElfClientConfigOptions _clientConfigOptions;
    private ISolidityContractService? _solidityContractService;

    private const string WasmCode =
        ""{solangAbi.Source.Wasm}"";

    public WasmContractCode WasmContractCode = new()
    {{
        Code = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(WasmCode)),
        Abi = File.ReadAllText(""{contractFile.Path}""),
        CodeHash = Hash.LoadFromHex(""{solangAbi.Source.Hash}"")
    }};

    public {contractName}Stub(IDeployContractService deployContractService, IAElfClientService clientService,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions)
    {{
        _deployContractService = deployContractService;
        _clientService = clientService;
        _clientConfigOptions = clientConfigOptions.Value;
    }}

    public async Task<Address> DeployAsync()
    {{
        var contractAddress = await _deployContractService.DeploySolidityContract(new DeploySoliditySmartContractInput
        {{
            Category = 1,
            Code = WasmContractCode.ToByteString(),
            Parameter = ByteString.Empty
        }});
        _solidityContractService = new SolidityContractService(_clientService, contractAddress, _clientConfigOptions);
        return contractAddress;
    }}

    public void SetContractAddressToStub(Address contractAddress)
    {{
        _solidityContractService = new SolidityContractService(_clientService, contractAddress, _clientConfigOptions);
    }}

    private void AssertContractDeployed()
    {{
        if (_solidityContractService == null)
        {{
            throw new NullReferenceException(""Solidity Contract not deployed."");
        }}
    }}
");

        var methodList = new List<string>();
        foreach (var message in solangAbi.Spec.Messages.Where(message => !methodList.Contains(message.Label)))
        {
            stringBuilder.Append(GenerateMethodImplementation(solangAbi, message.Label));
            methodList.Add(message.Label);
        }

        stringBuilder.Append($@"
}}
");

        context.AddSource($"{contractName}Stub.g.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
    }

    private string GenerateMethodInterface(SolangABI solangAbi, string label)
    {
        var mutates = solangAbi.GetMutates(label);

        if (mutates)
        {
            return $@"
    Task<SendTransactionResult> {GetMethodName(label)}Async(ByteString? parameter = null, Weight? gasLimit = null, long value = 0);
";
        }
        
        return $@"
    Task<byte[]> {GetMethodName(label)}Async(ByteString? parameter = null);
";

    }

    private string GenerateMethodImplementation(SolangABI solangAbi, string label)
    {
        var selector = solangAbi.GetSelector(label);
        var mutates = solangAbi.GetMutates(label);
        if (mutates)
        {
            return $@"

    public async Task<SendTransactionResult> {GetMethodName(label)}Async(ByteString? parameter = null, Weight? gasLimit = null, long value = 0)
    {{
        AssertContractDeployed();
        return await _solidityContractService.SendAsync(""{selector}"", parameter ?? ByteString.Empty, gasLimit, value);
    }}
";
        }

        return $@"

    public async Task<byte[]> {GetMethodName(label)}Async(ByteString? parameter = null)
    {{
        AssertContractDeployed();
        return await _solidityContractService.CallAsync(""{selector}"", parameter ?? ByteString.Empty);
    }}
";
    }

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    private string GetMethodName(string label)
    {
        return $"{char.ToUpperInvariant(label[0])}{label[1..]}";
    }
}