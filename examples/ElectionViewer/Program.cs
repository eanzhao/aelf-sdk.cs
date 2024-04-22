using System.Text.Json;
using ElectionViewer;

// var json = File.ReadAllText("response-election.json");
// var allTxs = JsonSerializer.Deserialize<ElectionContractTransactions>(json);
// var voteTxs = allTxs.Transactions.Where(t => t.Method == "Vote").ToList();
// var voteCount = voteTxs.Count;
// Console.WriteLine($"Total vote transactions: {voteCount}");
// var voters = voteTxs.DistinctBy(t => t.From).ToList();
// var voterCount = voters.Count;
// Console.WriteLine($"Total voters: {voterCount}");
// var voterLines = voters.Select(t => t.From).ToArray();
// File.WriteAllLines("voters.txt", voterLines);
//
// var grouped = voteTxs.GroupBy(t => t.From);
// var voteCountPerVoter = grouped.Select(g => new {Voter = g.Key, Count = g.Count()}).ToList();
// var maxVotes = voteCountPerVoter.Max(v => v.Count);

// var amounts = File.ReadAllLines("amounts.txt");
// var pair = amounts.Select(a => a.Split(":"))
//     .Select(a => new
//     {
//         Voter = a[0], 
//         Amount = long.Parse(a[1])
//     }).ToList();
// var ordered = pair.OrderByDescending(a => a.Amount).ToList();
// File.WriteAllLines("ordered.txt", ordered.Select(a => $"{a.Voter}, {(decimal)a.Amount / 100000000}"));
// var maxAmount = pair.Max(a => a.Amount);
// Console.WriteLine($"Max amount: {(decimal)maxAmount / 100000000}");
//
// var totalAmount = pair.Sum(a => a.Amount);
// Console.WriteLine($"Total amount: {(decimal)totalAmount / 100000000}");

// var records = File.ReadAllLines("details.txt");
// var dict = new Dictionary<string, ProfitDetails?>();
// var isAddress = true;
// var address = string.Empty;
// foreach (var text in records)
// {
//     if (!isAddress)
//     {
//         dict.Add(address, JsonSerializer.Deserialize<ProfitDetails>(text));
//         address = string.Empty;
//     }
//     else
//     {
//         address = text;
//     }
//     
//     isAddress = !isAddress;
// }
//
// foreach (var pair in dict)
// {
//     if (pair.Value?.Details == null)
//     {
//         continue;
//     }
//     foreach (var detail in pair.Value.Details)
//     {
//         if (!detail.IsWeightRemoved && int.Parse(detail.EndPeriod) <= 172)
//         {
//             var print = pair.Key.Remove(pair.Key.Length - 1);
//             Console.WriteLine($"{print},{detail.Shares}");
//         }
//     }
// }

var shares = File.ReadAllLines("shares.txt");
var pair = shares.Select(s => s.Split(","))
    .Select(s => new
    {
        Voter = s[0], 
        Shares = long.Parse(s[1])
    }).ToList();
Console.WriteLine($"total count: {pair.Count}");
var combined = pair.GroupBy(s => s.Voter)
    .Select(g => new
    {
        Voter = g.Key,
        Shares = g.Sum(s => s.Shares)
    }).ToList();
Console.WriteLine($"grouped count: {combined.Count}");
foreach (var share in combined.OrderByDescending(p => p.Shares))
{
    Console.WriteLine($"{share.Voter},{share.Shares}");
}

