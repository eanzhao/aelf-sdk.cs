using AElf.Client.Core;
using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Gradio.Net;

namespace WebApplication;

public static class ConsensusInfoViewer
{
    public static async Task CreateAsync(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        var clientService = app.Services.GetRequiredService<IAElfClientService>();

        gr.Markdown("# Query consensus previous round information.");
        Textbox endpoint;
        using (gr.Row())
        {
            endpoint = gr.Textbox(label: "RPC", placeholder: "RPC endpoint");
        }

        var btn = gr.Button("Query");
        var box = gr.Markdown();
        await btn.Click(fn: async input =>
            {
                var result = await clientService.ViewSystemAsync(WebApplicationConstants.ConsensusSmartContractName,
                    "GetPreviousRoundInformation", new Empty(), Textbox.Payload(input.Data[0]));
                var round = new Round();
                round.MergeFrom(result);
                return gr.Output(round.ToString("FOO"));
            },
            inputs: new[] { endpoint },
            outputs: new[] { box });
    }
}