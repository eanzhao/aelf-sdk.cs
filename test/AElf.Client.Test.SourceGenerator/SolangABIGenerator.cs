using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Solang;
using Solang.Extensions;

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

        var fileName = Path.GetFileName(contractFile.Path);
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

public interface I{contractName}Stub
{{
    Task<Address> DeployAsync();
");

        foreach (var message in solangAbi.Spec.Messages)
        {
            stringBuilder.Append(GenerateMethodInterface(message.Label));
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

    private void AssertContractDeployed()
    {{
        if (_solidityContractService == null)
        {{
            throw new NullReferenceException(""Solidity Contract not deployed."");
        }}
    }}
");

        foreach (var message in solangAbi.Spec.Messages)
        {
            stringBuilder.Append(GenerateMethodImplementation(solangAbi, message.Label));
        }

        stringBuilder.Append($@"
}}
");
        var options = ((CSharpCompilation)context.Compilation).SyntaxTrees[0].Options;
        var sourceTree = CSharpSyntaxTree.ParseText(stringBuilder.ToString(), (CSharpParseOptions)options);
        var compilation = context.Compilation.AddSyntaxTrees(sourceTree);
        context.AddSource($"{contractName}Stub.g.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
    }

    private string GenerateMethodInterface(string label)
    {
        return $@"
    Task<SendTransactionResult> {GetMethodName(label)}Async(ByteString parameter = null, Weight? gasLimit = null, long value = 0);
";
    }

    private string GenerateMethodImplementation(SolangABI solangAbi, string label)
    {
        var selector = solangAbi.GetSelector(label);
        return $@"

    public async Task<SendTransactionResult> {GetMethodName(label)}Async(ByteString parameter = null, Weight? gasLimit = null, long value = 0)
    {{
        AssertContractDeployed();
        return await _solidityContractService.SendAsync(""{selector}"", parameter ?? ByteString.Empty, gasLimit, value);
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