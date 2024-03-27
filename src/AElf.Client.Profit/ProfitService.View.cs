using AElf.Contracts.Profit;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Client.Profit;

public partial class ProfitService
{
    public async Task<Scheme> GetSchemeAsync(Hash schemeId)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewSystemAsync("AElf.ContractNames.Profit", "GetScheme",
            schemeId, useClientAlias);
        var output = new Scheme();
        output.MergeFrom(result);
        return output;
    }

    public async Task<long> GetProfitAmountAsync(GetProfitAmountInput getProfitAmountInput)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewSystemAsync("AElf.ContractNames.Profit", "GetProfitAmount",
            getProfitAmountInput, useClientAlias);
        var output = new Int64Value();
        output.MergeFrom(result);
        return output.Value;
    }

    public async Task<ProfitDetails> GetProfitDetailsAsync(GetProfitDetailsInput getProfitDetailsInput)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewSystemAsync("AElf.ContractNames.Profit", "GetProfitDetails",
            getProfitDetailsInput, useClientAlias);
        var output = new ProfitDetails();
        output.MergeFrom(result);
        return output;
    }
}