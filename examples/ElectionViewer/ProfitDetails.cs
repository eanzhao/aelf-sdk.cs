using System.Text.Json.Serialization;

namespace ElectionViewer;

public class ProfitDetails
{
    [JsonPropertyName("details")]
    public List<Detail> Details { get; set; }
}

public class Detail
{
    [JsonPropertyName("startPeriod")] public string StartPeriod { get; set; }

    [JsonPropertyName("endPeriod")] public string EndPeriod { get; set; }

    [JsonPropertyName("shares")] public string Shares { get; set; }

    [JsonPropertyName("lastProfitPeriod")] public string LastProfitPeriod { get; set; }

    [JsonPropertyName("isWeightRemoved")] public bool IsWeightRemoved { get; set; }

    [JsonPropertyName("id")] public string Id { get; set; }

    public override string ToString()
    {
        return
            $"StartPeriod: {StartPeriod}, EndPeriod: {EndPeriod}, Shares: {Shares}, LastProfitPeriod: {LastProfitPeriod}, IsWeightRemoved: {IsWeightRemoved}, Id: {Id}";
    }
}