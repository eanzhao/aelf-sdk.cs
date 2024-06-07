using Gradio.Net;
using WebApplication;
using WebApplication.Tools;

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

app.UseGradio(await CreateBlocksAsync());

app.MapGet("/test", () => "Hello World!");

await app.RunAsync();

return;

async Task<Blocks> CreateBlocksAsync()
{
    var blocks = gr.Blocks();

    // await ViewConsensusInfoTool.CreateAsync(app);
    // await TokenTransferTool.CreateAsync(app);
    await ViewElectionInfoTool.CreateAsync(app);

    return blocks;
}