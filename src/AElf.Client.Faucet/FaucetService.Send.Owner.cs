using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Types;
using Faucet;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Faucet;

public partial class FaucetService : ContractServiceBase, IFaucetService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfContractOptions _contractOptions;
    private readonly AElfClientConfigOptions _clientConfigOptions;
    private const string FaucetContractAddress = "2SsVMejAv2kFoDYxW5f2aVZJuCBv1St7t8KYw4652fvGp3cjrz";

    protected FaucetService(IAElfClientService clientService, string smartContractName) : base(clientService,
        smartContractName)
    {
    }

    public FaucetService(IAElfClientService clientService,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions) : base(clientService,
        Address.FromBase58(FaucetContractAddress))
    {
        _clientService = clientService;
        _clientConfigOptions = clientConfigOptions.Value;
    }

    public async Task<SendTransactionResult> InitializeAsync(InitializeInput createInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("Initialize", createInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> NewFaucetAsync(NewFaucetInput newFaucetInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("NewFaucet", newFaucetInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> PourAsync(PourInput pourInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("Pour", pourInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> TurnOnAsync(TurnInput pourInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("TurnOn", pourInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> TurnOffAsync(TurnInput pourInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("TurnOff", pourInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> SetLimitAsync(SetLimitInput setLimitInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("SetLimit", setLimitInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> BanAsync(BanInput banInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("Ban", banInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> SendAsync(SendInput sendInput)
    {
        var clientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("Send", sendInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }
}