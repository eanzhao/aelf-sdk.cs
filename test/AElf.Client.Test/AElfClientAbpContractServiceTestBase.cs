using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.Client.TestBase;
using AElf.Runtime.WebAssembly;
using AElf.Types;
using Google.Protobuf;
using Solang;

namespace AElf.Client.Test;

public abstract class
    AElfClientAbpContractServiceTestBase : AElfClientAbpTestBase<AElfClientAbpContractServiceTestModule>
{
    protected async Task<(WasmContractCode, SolangABI)> LoadWasmContractCodeAsync(string contractPath)
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