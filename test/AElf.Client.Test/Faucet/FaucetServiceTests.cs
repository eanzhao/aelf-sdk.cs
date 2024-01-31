using System.Threading.Tasks;
using AElf.Client.Faucet;
using AElf.Client.Token;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Faucet;
using Shouldly;

namespace AElf.Client.Test.Faucet;

public class FaucetServiceTests : AElfClientAbpContractServiceTestBase
{
    private readonly IFaucetService _faucetService;
    private readonly ITokenService _tokenService;

    public FaucetServiceTests()
    {
        _faucetService = GetRequiredService<IFaucetService>();
        _tokenService = GetRequiredService<ITokenService>();
    }

    [Fact]
    public async Task InitializeTest()
    {
        var result = await _faucetService.InitializeAsync(new InitializeInput());
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task PourTest()
    {
        {
            var result = await _tokenService.ApproveAsync(new ApproveInput
            {
                Symbol = "ELF",
                Amount = 100000_00000000,
                Spender = Address.FromBase58("2SsVMejAv2kFoDYxW5f2aVZJuCBv1St7t8KYw4652fvGp3cjrz")
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var result = await _faucetService.PourAsync(new PourInput
            {
                Symbol = "ELF",
                Amount = 10000_00000000
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }

    [Fact]
    public async Task TurnOnTest()
    {
        var result = await _faucetService.TurnOnAsync(new TurnInput
        {
            Symbol = "ELF",
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task TakeTest()
    {
        var result = await _faucetService.TakeAsync(new TakeInput
        {
            Symbol = "ELF",
            Amount = 11_00000000
        });
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }
}