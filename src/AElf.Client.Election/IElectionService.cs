using AElf.Contracts.Election;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Client.Election;

public interface IElectionService
{
    Task<long> GetCalculateVoteWeightAsync(VoteInformation voteInformation);
    Task<long> GetVotesAmountAsync(Empty input);
    Task<long> GetVotersCountAsync(Empty input);
    Task<CandidateInformation> GetCandidateInformationAsync(StringValue input);
    Task<PubkeyList> GetVictoriesAsync(Empty input);
}