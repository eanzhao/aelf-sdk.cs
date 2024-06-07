using AElf;
using AElf.Client.Election;
using AElf.Types;
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
                var information = await electionService.GetCandidateInformationAsync(new StringValue
                {
                    Value =
                        "042fb90f64151a71b3d8423f30c42ba2609d58629693b2bc21afda40c998be35f97fb8b22143326649e3942d56d962d2095554a4a184ba90c1982271709003241a"
                });
                return gr.Output(information);
            },
            outputs: new[] { box });
    }
}