using System.Text.Json.Serialization;

namespace ElectionViewer;

public class ElectionContractTransaction
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("tx_id")] public string TxId { get; set; }
    [JsonPropertyName("address_from")] public string From { get; set; }
    [JsonPropertyName("method")] public string Method { get; set; }
}

public class ElectionContractTransactions
{
    [JsonPropertyName("transactions")] public List<ElectionContractTransaction> Transactions { get; set; }
}