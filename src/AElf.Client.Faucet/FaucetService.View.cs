using AElf.Types;
using Faucet;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Client.Faucet;

public partial class FaucetService
{
    public async Task<Address> GetOwnerAsync(string symbol)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(FaucetContractAddress, "GetOwner",
            new StringValue
            {
                Value = symbol
            }, useClientAlias);
        var address = new Address();
        address.MergeFrom(result);
        return address;
    }

    public async Task<FaucetStatus> GetFaucetStatusAsync(string symbol)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(FaucetContractAddress, "GetFaucetStatus",
            new StringValue
            {
                Value = symbol
            }, useClientAlias);
        var faucetStatus = new FaucetStatus();
        faucetStatus.MergeFrom(result);
        return faucetStatus;
    }

    public async Task<long> GetLimitAmountAsync(string symbol)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(FaucetContractAddress, "GetLimitAmount",
            new StringValue
            {
                Value = symbol
            }, useClientAlias);
        var limitAmount = new Int64Value();
        limitAmount.MergeFrom(result);
        return limitAmount.Value;
    }

    public async Task<long> GetIntervalMinutesAsync(string symbol)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(FaucetContractAddress, "GetIntervalMinutes",
            new StringValue
            {
                Value = symbol
            }, useClientAlias);
        var intervalMinutes = new Int64Value();
        intervalMinutes.MergeFrom(result);
        return intervalMinutes.Value;
    }

    public async Task<bool> IsBannedByOwnerAsync(IsBannedByOwnerInput isBannedByOwnerInput)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(FaucetContractAddress, "IsBannedByOwner",
            isBannedByOwnerInput, useClientAlias);
        var isBanned = new BoolValue();
        isBanned.MergeFrom(result);
        return isBanned.Value;
    }
}