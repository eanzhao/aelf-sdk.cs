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

app.UseGradio(await CreateBlocksAsync());

await app.RunAsync();

return;

async Task<Blocks> CreateBlocksAsync()
{
    var blocks = gr.Blocks();

    await ConsensusInfoViewer.CreateAsync(app);

    return blocks;
}