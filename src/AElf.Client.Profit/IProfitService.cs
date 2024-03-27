using AElf.Client.Core;
using AElf.Contracts.Profit;
using AElf.Types;

namespace AElf.Client.Profit;

public interface IProfitService
{
    Task<SendTransactionResult> ClaimProfitsAsync(ClaimProfitsInput claimProfitsInput);

    Task<Scheme> GetSchemeAsync(Hash schemeId);
    Task<long> GetProfitAmountAsync(GetProfitAmountInput getProfitAmountInput);
    Task<ProfitDetails> GetProfitDetailsAsync(GetProfitDetailsInput getProfitDetailsInput);
}