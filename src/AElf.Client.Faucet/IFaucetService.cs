using AElf.Client.Core;
using AElf.Types;
using Faucet;

namespace AElf.Client.Faucet;

public interface IFaucetService
{
    Task<SendTransactionResult> InitializeAsync(InitializeInput createInput);
    Task<SendTransactionResult> NewFaucetAsync(NewFaucetInput newFaucetInput);
    Task<SendTransactionResult> PourAsync(PourInput pourInput);
    Task<SendTransactionResult> TurnOnAsync(TurnInput pourInput);
    Task<SendTransactionResult> TurnOffAsync(TurnInput pourInput);
    Task<SendTransactionResult> SetLimitAsync(SetLimitInput setLimitInput);
    Task<SendTransactionResult> BanAsync(BanInput banInput);
    Task<SendTransactionResult> SendAsync(SendInput sendInput);

    Task<SendTransactionResult> TakeAsync(TakeInput takeInput);
    Task<SendTransactionResult> ReturnAsync(ReturnInput returnInput);

    Task<Address> GetOwnerAsync(string symbol);
    Task<FaucetStatus> GetFaucetStatusAsync(string symbol);
    Task<long> GetLimitAmountAsync(string symbol);
    Task<long> GetIntervalMinutesAsync(string symbol);
    Task<bool> IsBannedByOwnerAsync(IsBannedByOwnerInput isBannedByOwnerInput);
}