using System.IO;
using System.Threading.Tasks;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;

namespace AElf.Client.Test.ContractMethodSplitter;

public static class DecompilerHelper
{
    public static async Task Decompile(string inputDllPath, string outputSourceFilePath)
    {
        await Decompile(await File.ReadAllBytesAsync(inputDllPath), outputSourceFilePath);
    }
    
    public static async Task Decompile(byte[] contractBytes, string outputSourceFilePath)
    {
        const string tempName = "AElf.Contract.dll";
        await using var fs = new FileStream(tempName, FileMode.Create, FileAccess.Write);
        await fs.WriteAsync(contractBytes);
        var module = new PEFile(tempName);
        var settings = new DecompilerSettings(LanguageVersion.Latest)
        {
            ThrowOnAssemblyResolveErrors = false,
            RemoveDeadCode = false,
            RemoveDeadStores = false
        };
        var resolver =
            new UniversalAssemblyResolver(tempName, false, module.Metadata.DetectTargetFrameworkId());
        var decompiler = new WholeProjectDecompiler(settings, resolver, resolver, null);
        if (!Directory.Exists(outputSourceFilePath))
        {
            Directory.CreateDirectory(outputSourceFilePath);
        }
        else
        {
            DeleteDirectoryContents(outputSourceFilePath);
        }

        decompiler.DecompileProject(module, outputSourceFilePath);
        module.Reader.Dispose();
    }
    
    public static void DeleteDirectoryContents(string directoryPath)
    {
        // Get directory info
        var directory = new DirectoryInfo(directoryPath);

        // Delete each file
        foreach (var file in directory.GetFiles())
        {
            file.Delete();
        }

        // Delete each subdirectory
        foreach (var subDirectory in directory.GetDirectories())
        {
            subDirectory.Delete(true);
        }
    }
}