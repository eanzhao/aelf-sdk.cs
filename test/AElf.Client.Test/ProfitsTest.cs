using System.Threading.Tasks;
using AElf.Client.Election;
using AElf.Contracts.Election;
using Shouldly;

namespace AElf.Client.Test;

public class ProfitsTest : AElfClientAbpContractServiceTestBase
{
    private readonly IElectionService _electionService;

    public ProfitsTest()
    {
        _electionService = GetRequiredService<IElectionService>();
    }

    private const int DaySec = 86400;

    [Fact]
    public async Task GetCalculateVoteWeightAsyncTest()
    {
        var voteInformation = new VoteInformation
        {
            Amount = 10000_00000000,
            LockTime = 1080 * DaySec
        };
        var result = await _electionService.GetCalculateVoteWeightAsync(voteInformation);
        result.ShouldBeNegative();
    }
}