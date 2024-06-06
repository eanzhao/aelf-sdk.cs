using AElf.Client.Election;
using Google.Protobuf.WellKnownTypes;
using Gradio.Net;

namespace WebApplication.Tools;

public class ViewElectionInfoTool
{
    public static async Task CreateAsync(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        var electionService = app.Services.GetRequiredService<IElectionService>();

        gr.Markdown("# Query election information.");

        var btn = gr.Button("Query");
        var box = gr.Markdown();
        await btn.Click(fn: async input =>
            {
                var votersCount = await electionService.GetVotersCountAsync(new Empty());
                var votesAmount = await electionService.GetVotesAmountAsync(new Empty());
                return gr.Output($"Voters Count: {votersCount}, Votes Amount: {votesAmount}");
            },
            outputs: new[] { box });
    }
}