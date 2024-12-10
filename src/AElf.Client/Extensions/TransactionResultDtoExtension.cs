using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Client.Extensions
{
    
    public static class TransactionResultDtoExtension
    {
        public static Dictionary<string, long> GetTransactionFees(this TransactionResultDto transactionResultDto)
        {
            var result = new Dictionary<string, long>();
    
            var transactionFeeLogs =
                transactionResultDto.Logs?.Where(l => l.Name == nameof(TransactionFeeCharged)).ToList();
            if (transactionFeeLogs != null)
            {
                foreach (var transactionFee in transactionFeeLogs.Select(transactionFeeLog =>
                             TransactionFeeCharged.Parser.ParseFrom(ByteString.FromBase64(transactionFeeLog.NonIndexed))))
                {
                    result.Add(transactionFee.Symbol, transactionFee.Amount);
                }
            }
    
            var resourceTokenLogs =
                transactionResultDto.Logs?.Where(l => l.Name == nameof(ResourceTokenCharged)).ToList();
            if (resourceTokenLogs != null)
            {
                foreach (var resourceToken in resourceTokenLogs.Select(transactionFeeLog =>
                             ResourceTokenCharged.Parser.ParseFrom(ByteString.FromBase64(transactionFeeLog.NonIndexed))))
                {
                    result.Add(resourceToken.Symbol, resourceToken.Amount);
                }
            }
    
            return result;
        }
    }
    
    public static class TransactionResultExtension
    {
        public static List<string> GetRuntimeLogs(this TransactionResult transactionResult)
        {
            var logs = transactionResult.Logs.Where(l => l.Name == "RuntimeLog");
            return logs.Select(l => Encoding.UTF8.GetString(l.NonIndexed.ToByteArray())).ToList();
        }

        public static List<string> GetPrints(this TransactionResult transactionResult)
        {
            var prints = transactionResult.Logs.Where(l => l.Name == "Print");
            return prints.Select(p => Encoding.UTF8.GetString(p.NonIndexed.ToByteArray())).ToList();
        }

        public static List<string> GetErrorMessages(this TransactionResult transactionResult)
        {
            var errors = transactionResult.Logs.Where(l => l.Name == "ErrorMessage");
            return errors.Select(p => Encoding.UTF8.GetString(p.NonIndexed.ToByteArray())).ToList();
        }

        public static List<string> GetDebugMessages(this TransactionResult transactionResult)
        {
            var debugs = transactionResult.Logs.Where(l => l.Name == "DebugMessage");
            return debugs.Select(p => Encoding.UTF8.GetString(p.NonIndexed.ToByteArray())).ToList();
        }
        
        public static long GetChargedGasFee(this TransactionResult transactionResult)
        {
            var logEvent = transactionResult.Logs.LastOrDefault(l =>
                l.Name == WebAssemblyTransactionPaymentConstants.GasFeeChargedLogEventName)?.NonIndexed;
            if (logEvent == null)
            {
                return 0;
            }

            var gasFee = new Int64Value();
            gasFee.MergeFrom(logEvent);
            return gasFee.Value;
        }
    }
    
    public abstract class WebAssemblyTransactionPaymentConstants
    {
        public const int TransactionByteFee = 1;
        public const int FeeWeightRatio = 1000;
        public const string GasFeeChargedLogEventName = "AElf::GasFeeCharged";
        public const string GasFeeEstimatedLogEventName = "AElf::GasFeeEstimated";
    }
}
