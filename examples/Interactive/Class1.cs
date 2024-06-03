// using System.CommandLine;
// using AElf.Client;
// using Google.Protobuf.Reflection;
// using Microsoft.AspNetCore.Html;
// using Microsoft.DotNet.Interactive;
// using Microsoft.DotNet.Interactive.Formatting;
//
// namespace Interactive;
//
// public static class InteractiveService
// {
//     public static void Load(Kernel kernel)
//     {
//         var aelfCommand = InitAElfCommand();
//
//         var infoCommand = InitInfoCommand();
//
//         var accountCommand = InitAccountCommand();
//
//         aelfCommand.AddCommand(infoCommand);
//         aelfCommand.AddCommand(accountCommand);
//
//         kernel.AddDirective(aelfCommand);
//
//         PocketView view = div(
//             code(nameof(AElfExtensionDemo)),
//             " is loaded. It helps you query information from aelf node",
//             ". Try it by running: ",
//             code("#!aelf -c {contractAddress}"),
//             " or ",
//             code("#!aelf -t {txId}")
//         );
//
//         KernelInvocationContext.Current?.Display(view);
//     }
//
//     private static Command InitAElfCommand()
//     {
//         var txOption = new Option<string>(["-t", "--tx"],
//             "Query transaction information.");
//         var contractOption = new Option<string>(["-c", "--contract"],
//             "Query contract information.");
//
//         var aelfCommand = new Command("#!aelf", "Query information from aelf blockchain.")
//         {
//             txOption,
//             contractOption
//         };
//
//         aelfCommand.SetHandler(txId => KernelInvocationContext.Current.Display(QueryTransactionResult(txId)), txOption);
//         aelfCommand.SetHandler(
//             contractAddress => KernelInvocationContext.Current.Display(QueryContractMethods(contractAddress)),
//             contractOption);
//
//         return aelfCommand;
//     }
//
//     private static Command InitInfoCommand()
//     {
//         var infoCommand = new Command("info", "Query chain status.");
//         infoCommand.SetHandler(_ => KernelInvocationContext.Current.Display(QueryChainStatus()));
//         return infoCommand;
//     }
//
//     private static Command InitAccountCommand()
//     {
//         var accountCommand = new Command("account", "Manage aelf accounts.");
//
//         var passwordOption = new Option<string>(["-p", "--password"], getDefaultValue: () => "aelftest",
//             "Provide the password of new aelf account keystore file.");
//         var newCommand = new Command("new", "Create a new aelf account");
//
//         newCommand.SetHandler(password =>
//         {
//             var extKey = new AElfWalletFactory().Create(passphrase: password);
//             KernelInvocationContext.Current.Display(PrintAccount(extKey));
//         }, passwordOption);
//
//         accountCommand.AddCommand(newCommand);
//
//         return accountCommand;
//     }
//
//     private static IHtmlContent PrintAccount(ExtendedKey extKey)
//     {
//         return div(
//             p($"Private Key: {extKey.PrivateKey.ToHex()}"),
//             p($"Public Key: {extKey.PrivateKey.PublicKey}"),
//             p($"Address: {extKey.PrivateKey.PublicKey.ToAddress()}"),
//             p($"Mnemonic: {extKey.Mnemonic}")
//         );
//     }
//
//     private static IHtmlContent QueryChainStatus()
//     {
//         var client = new AElfClientBuilder().UseEndpoint("https://tdvw-test-node.aelf.io").Build();
//         var chainStatus = client.GetChainStatusAsync().Result;
//         return div(
//             p($"ChainId: {chainStatus.ChainId}"),
//             p($"Height: {chainStatus.BestChainHeight}"),
//             p($"Hash: {chainStatus.BestChainHash}")
//         );
//     }
//
//     private static IHtmlContent QueryTransactionResult(string txId)
//     {
//         var client = new AElfClientBuilder().UseEndpoint("https://tdvw-test-node.aelf.io").Build();
//         var txResult = client.GetTransactionResultAsync(txId).Result!;
//
//         return div(
//             p($"TxId: {txResult.TransactionId}"),
//             p($"From: {txResult.Transaction.From}"),
//             p($"To: {txResult.Transaction.To}"),
//             p($"Status: {txResult.Status}"),
//             p($"ReturnValue: {txResult.ReturnValue}")
//         );
//     }
//
//     private static IHtmlContent QueryContractMethods(string contractAddress)
//     {
//         var client = new AElfClientBuilder().UseEndpoint("https://tdvw-test-node.aelf.io").Build();
//         var bytes = client.GetContractFileDescriptorSetAsync(contractAddress).Result;
//         var fileDescriptorSet = FileDescriptorSet.Parser.ParseFrom(bytes);
//         var methods =
//             (from file in fileDescriptorSet.File
//                 from service in file.Service
//                 from method in service.Method
//                 select method.Name).ToList();
//
//         return div(methods.Select(s => p(s)));
//     }
// }