using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AElf.Client.Test.ContractMethodSplitter;

public static class SplitterHelper
{
    public static async Task SplitContractMethod(string codePath, string outputPath)
    {
        var syntaxTrees = new List<SyntaxTree>();
        var classList = new List<ClassDeclarationSyntax>();
        var csFiles = Directory.EnumerateFiles(codePath, "*.cs", SearchOption.AllDirectories);

        var collector = new PartialClassCollector();

        foreach (var file in csFiles)
        {
            var code = await File.ReadAllTextAsync(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            syntaxTrees.Add(syntaxTree);
        }

        foreach (var syntaxTree in syntaxTrees)
        {
            collector.Visit(await syntaxTree.GetRootAsync());
        }

        foreach (var pair in collector.PartialClasses)
        {
            var mergedClass = SyntaxFactory.ClassDeclaration(pair.Key)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            foreach (var partialClass in pair.Value)
            {
                foreach (var member in partialClass.Members)
                {
                    mergedClass = mergedClass.AddMembers(member);
                }
            }

            classList.Add(mergedClass);
        }

        var finder = new ContractMethodFinder();

        foreach (var classDeclarationSyntax in classList)
        {
            finder.Visit(classDeclarationSyntax);
            finder.ProcessNodes();
        }

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        else
        {
            DecompilerHelper.DeleteDirectoryContents(outputPath);
        }

        foreach (var pair in finder.PublicMethods)
        {
            var methodsBody = string.Empty;
            foreach (var method in pair.Value)
            {
                methodsBody += method.ToFullString();
            }

            await File.WriteAllTextAsync($"./{outputPath}/{pair.Key}.txt", methodsBody);
        }
    }
}