using AElf.Contracts.Election;

namespace AElf.Client.Election;

public interface IElectionService
{
    Task<long> GetCalculateVoteWeightAsync(VoteInformation voteInformation);
}