using AElf.Client.Core;
using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Gradio.Net;
using WebApplication;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGradio();

builder.Host.UseAutofac();

await builder.Services.AddApplicationAsync<WebApplicationModule>();

var app = builder.Build();

await app.InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseGradio(await CreateBlocks());

await app.RunAsync();

return;

async Task<Blocks> CreateBlocks()
{
    var blocks = gr.Blocks();
    gr.Markdown("# Query consensus previous round information.");
    Textbox endpoint;
    using (gr.Row())
    {
        endpoint = gr.Textbox(placeholder: "RPC endpoint");
    }

    var clientService = app.Services.GetRequiredService<IAElfClientService>();
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

    return blocks;
}