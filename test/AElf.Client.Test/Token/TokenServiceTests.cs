using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Token;
using AElf.Client.Token.SyncTokenInfo;
using AElf.Contracts.Bridge;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFT;
using AElf.Contracts.Profit;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Client.Test.Token;

[Trait("Category", "TokenContractService")]
public sealed class TokenServiceTests : AElfClientAbpContractServiceTestBase
{
    private readonly ITokenService _tokenService;
    private readonly ISyncTokenInfoQueueService _syncTokenInfoQueueService;

    public TokenServiceTests()
    {
        _tokenService = GetRequiredService<ITokenService>();
        _syncTokenInfoQueueService = GetRequiredService<ISyncTokenInfoQueueService>();
    }

    [Theory]
    [InlineData("ELF")]
    public async Task GetTokenInfoTest(string symbol)
    {
        var tokenInfo = await _tokenService.GetTokenInfoAsync(symbol);
        tokenInfo.Symbol.ShouldBe(symbol);
    }

    [Theory]
    [InlineData("2nSXrp4iM3A1gB5WKXjkwJQwy56jzcw1ESNpVnWywnyjXFixGc", "ELF", 1_00000000)]
    public async Task TransferTest(string address, string symbol, long amount)
    {
        var result = await _tokenService.TransferAsync(new TransferInput
        {
            To = Address.FromBase58(address),
            Symbol = symbol,
            Amount = amount
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var logEvent = result.TransactionResult.Logs.First(l => l.Name == nameof(Contracts.MultiToken.Transferred));
        var transferred = new Contracts.MultiToken.Transferred();
        foreach (var indexed in logEvent.Indexed)
        {
            transferred.MergeFrom(indexed);
        }

        transferred.MergeFrom(logEvent.NonIndexed);
        transferred.Symbol.ShouldBe(symbol);
        transferred.To.ToBase58().ShouldBe(address);
        transferred.Amount.ShouldBe(amount);
    }

    [Theory]
    [InlineData("BA994198147")]
    public async Task SyncTokenInfoTest(string symbol)
    {
        _syncTokenInfoQueueService.Enqueue(symbol);
    }

    [Theory]
    [InlineData("bb16f381b0f2e795a988285dec3a68affacdccd7d3ac2e74edc808c102efcd95", 228, "9413000000000000000000")]
    public async Task SwapTokenTest(string swapIdHex, long receiptId, string amount)
    {
        var swapId = Hash.LoadFromHex(swapIdHex);
        await _tokenService.SwapTokenAsync(new SwapTokenInput
        {
            SwapId = swapId,
            OriginAmount = amount,
            ReceiptId = receiptId
        });
    }

    [Fact]
    public void Test()
    {
        var pubkey =
            "04f785788757c15158d39c3fd989336f334b8439592b1e14a7c17dad8bc9fe53b4bff9d39e4cc38ddc0c1f13b154ed435e560e219a375aed42b57c1705e2f04f45";
        var address = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(pubkey));
        address.ToBase58().ShouldBeNull();
    }

    [Fact]
    public void Test2()
    {
        var parameterBase64 =
            "CiIKIKpXDFiH8pHPrjiTGEbLSXCs9IVsFGEdgeKeP0KUmDogEiIKIJmyGXgy97JtGOqcDXPbvSvNPxKB2q3qs54sdjf7dfgpGiIKIDAm1SW5NFgZyLu8aihL3boLmnrgIMQoX0c+5qUseBgg";
        var parameter = RemoveBeneficiaryInput.Parser.ParseFrom(ByteString.FromBase64(parameterBase64));
        
    }
}