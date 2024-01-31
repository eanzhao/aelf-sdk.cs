using AElf.Client.Core;
using Faucet;

namespace AElf.Client.Faucet;

public partial class FaucetService
{
    public async Task<SendTransactionResult> TakeAsync(TakeInput takeInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("Take", takeInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> ReturnAsync(ReturnInput returnInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("Return", returnInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }
}