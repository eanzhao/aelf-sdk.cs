using AElf.Client.Token;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Gradio.Net;

namespace WebApplication.Tools;

public static class TokenTransferTool
{
    public static async Task CreateAsync(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        var tokenService = app.Services.GetRequiredService<ITokenService>();

        gr.Markdown("# Token Transfer");
        Textbox symbol;
        Textbox toAddress;
        Textbox amount;
        using (gr.Row())
        {
            symbol = gr.Textbox(label: "Symbol", placeholder: "Token symbol");
        }
        using (gr.Row())
        {
            amount = gr.Textbox(label: "Amount", placeholder: "Token amount");
        }
        using (gr.Row())
        {
            toAddress = gr.Textbox(label: "To Address", placeholder: "Receiver address");
        }

        var btn = gr.Button("Transfer");
        var box = gr.Markdown();
        await btn.Click(fn: async input =>
            {
                var result = await tokenService.TransferAsync(new TransferInput
                {
                    Symbol = Textbox.Payload(input.Data[0]),
                    Amount = long.Parse(Textbox.Payload(input.Data[1])),
                    To = Address.FromBase58(Textbox.Payload(input.Data[2]))
                });
                return gr.Output(result.TransactionResult);
            },
            inputs: new[] { symbol, amount, toAddress },
            outputs: new[] { box });
    }
}