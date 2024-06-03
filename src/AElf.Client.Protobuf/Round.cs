using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS4;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS;

public partial class Round
{
    public long RoundId
    {
        get
        {
            if (RealTimeMinersInformation.Values.All(bpInfo => bpInfo.ExpectedMiningTime != null))
                return RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds).Sum();

            return RoundIdForValidation;
        }
    }

    /// <summary>
    ///     Actually the expected mining time of the miner whose order is 1.
    /// </summary>
    /// <returns></returns>
    public Timestamp GetRoundStartTime()
    {
        return FirstMiner().ExpectedMiningTime;
    }
    
    public MinerInRound FirstMiner()
    {
        return RealTimeMinersInformation.Count > 0
            ? RealTimeMinersInformation.Values.FirstOrDefault(m => m.Order == 1)
            // Unlikely.
            : new MinerInRound();
    }

    /// <summary>
    ///     If periodSeconds == 7:
    ///     1, 1, 1 => 0 != 1 - 1 => false
    ///     1, 2, 1 => 0 != 1 - 1 => false
    ///     1, 8, 1 => 1 != 1 - 1 => true => term number will be 2
    ///     1, 9, 2 => 1 != 2 - 1 => false
    ///     1, 15, 2 => 2 != 2 - 1 => true => term number will be 3.
    /// </summary>
    /// <param name="blockchainStartTimestamp"></param>
    /// <param name="termNumber"></param>
    /// <param name="blockProducedTimestamp"></param>
    /// <param name="periodSeconds"></param>
    /// <returns></returns>
    private static bool IsTimeToChangeTerm(Timestamp blockchainStartTimestamp, Timestamp blockProducedTimestamp,
        long termNumber, long periodSeconds)
    {
        return (blockProducedTimestamp - blockchainStartTimestamp).Seconds / periodSeconds != termNumber - 1;
    }

    private static int GetAbsModulus(long longValue, int intValue)
    {
        return (int)Math.Abs(longValue % intValue);
    }
}