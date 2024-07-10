using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Genesis;

namespace AElf.Client.Test.ContractMethodSplitter;

public class ContractMethodSplitterTest : AElfClientAbpContractServiceTestBase
{
    private readonly IGenesisService _genesisService;

    public ContractMethodSplitterTest()
    {
        _genesisService = GetRequiredService<IGenesisService>();
    }

    [Fact]
    public async Task ContractMethodSplit()
    {
        var contractAddress =
            await _genesisService.GetContractAddressByName(
                HashHelper.ComputeFrom("AElf.ContractNames.Token"));
        var scr = await _genesisService.GetSmartContractRegistrationByAddress(contractAddress);
        var code = scr.Code.ToByteArray();
        await DecompilerHelper.Decompile(code, "./code");
        await SplitterHelper.SplitContractMethod("./code", "./output");
    }
}