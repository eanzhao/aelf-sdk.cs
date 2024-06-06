using AElf.Client.Core;
using AElf.Contracts.Bridge;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFT;
using AElf.Types;
using ApproveInput = AElf.Contracts.MultiToken.ApproveInput;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Client.Token;

public interface ITokenService
{
    Task<SendTransactionResult> CreateTokenAsync(Contracts.MultiToken.CreateInput createInput);
    Task<SendTransactionResult> ValidateTokenInfoExistsAsync(ValidateTokenInfoExistsInput validateTokenInfoExistsInput);
    Task<SendTransactionResult> CrossChainCreateTokenAsync(CrossChainCreateTokenInput crossChainCreateTokenInput);
    Task<SendTransactionResult> CrossChainTransferAsync(CrossChainTransferInput crossChainTransferInput,
        string clientAlias);
    Task<SendTransactionResult> CrossChainReceiveTokenAsync(CrossChainReceiveTokenInput crossChainReceiveTokenInput,
        string clientAlias);

    Task<SendTransactionResult> TransferAsync(TransferInput transferInput);
    Task<SendTransactionResult> ApproveAsync(ApproveInput approveInput);

    Task<SendTransactionResult> SwapTokenAsync(SwapTokenInput swapTokenInput);

    Task<TokenInfo> GetTokenInfoAsync(string symbol);
    Task<Contracts.MultiToken.GetBalanceOutput> GetTokenBalanceAsync(string symbol, Address owner);
    Task<Contracts.MultiToken.GetAllowanceOutput> GetTokenAllowanceAsync(string symbol, Address owner, Address spender);
}