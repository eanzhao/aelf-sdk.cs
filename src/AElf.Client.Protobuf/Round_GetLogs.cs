using System;
using System.Linq;
using System.Text;

namespace AElf.Contracts.Consensus.AEDPoS;

public partial class Round : IFormattable
{
    public string ToString(string format, IFormatProvider formatProvider = null)
    {
        if (string.IsNullOrEmpty(format)) format = "G";

        switch (format)
        {
            case "G": return ToString();
            case "M":
                // Return formatted miner list.
                return RealTimeMinersInformation.Keys.Aggregate("\n", (key1, key2) => key1 + "\n" + key2);
        }

        return GetLogs(format);
    }

    private string GetLogs(string publicKey)
    {
        var logs = new StringBuilder($"# [Round {RoundNumber}](Round Id: {RoundId})[Term {TermNumber}]\n");
        foreach (var minerInRound in RealTimeMinersInformation.Values.OrderBy(m => m.Order))
        {
            var minerInformation = new StringBuilder("\n");
            minerInformation.Append($"## [{minerInRound.Pubkey[..10]}]");
            minerInformation.Append(minerInRound.IsExtraBlockProducer ? "(Current EBP)" : "");
            minerInformation.AppendLine();
            minerInformation.AppendLine(minerInRound.Pubkey == publicKey
                ? "(This Node)"
                : "");
            minerInformation.AppendLine();
            minerInformation.AppendLine($"Order:\t {minerInRound.Order}");
            minerInformation.AppendLine();
            minerInformation.AppendLine(
                $"Expect:\t {minerInRound.ExpectedMiningTime?.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,ffffff}");
            minerInformation.AppendLine();

            var roundStartTime = GetRoundStartTime();
            var actualMiningTimes = minerInRound.ActualMiningTimes.OrderBy(t => t).Select(t =>
            {
                if (t < roundStartTime)
                {
                    
                    return $"{t.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,ffffff} (For Extra Block Slot Of Previous Round)\n";
                }

                return t.ToDateTime().ToUniversalTime().ToString("yyyy-MM-dd HH.mm.ss,ffffff") + "\n";
            });
            var actualMiningTimesStr =
                minerInRound.ActualMiningTimes.Any() ? string.Join("\n\t ", actualMiningTimes) : "";
            
            minerInformation.AppendLine($"Actual:\t {actualMiningTimesStr}");
            minerInformation.AppendLine();

            minerInformation.AppendLine($"Out:\t {minerInRound.OutValue?.ToHex()}");
            minerInformation.AppendLine();

            if (RoundNumber != 1)
                minerInformation.AppendLine($"PreIn:\t {minerInRound.PreviousInValue?.ToHex()}");
            minerInformation.AppendLine();

            minerInformation.AppendLine($"In:\t {minerInRound.InValue?.ToHex()}");
            minerInformation.AppendLine();

            minerInformation.AppendLine($"Sig:\t {minerInRound.Signature?.ToHex()}");
            minerInformation.AppendLine();

            minerInformation.AppendLine($"Mine:\t {minerInRound.ProducedBlocks}");
            minerInformation.AppendLine();

            minerInformation.AppendLine($"Miss:\t {minerInRound.MissedTimeSlots}");
            minerInformation.AppendLine();

            minerInformation.AppendLine($"Tiny:\t {minerInRound.ActualMiningTimes.Count}");
            minerInformation.AppendLine();

            minerInformation.AppendLine($"NOrder:\t {minerInRound.FinalOrderOfNextRound}");
            minerInformation.AppendLine();

            minerInformation.AppendLine($"Lib:\t {minerInRound.ImpliedIrreversibleBlockHeight}");
            minerInformation.AppendLine();

            logs.AppendLine(minerInformation.ToString());
        }

        return logs.ToString();
    }
}