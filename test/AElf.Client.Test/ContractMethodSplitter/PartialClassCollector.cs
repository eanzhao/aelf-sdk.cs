using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AElf.Client.Test.ContractMethodSplitter;

public class PartialClassCollector : CSharpSyntaxWalker
{
    public Dictionary<string, List<ClassDeclarationSyntax>> PartialClasses { get; } = new();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            if (!PartialClasses.ContainsKey(node.Identifier.Text))
            {
                PartialClasses[node.Identifier.Text] = new List<ClassDeclarationSyntax>();
            }
            PartialClasses[node.Identifier.Text].Add(node);
        }
        else if (!node.Modifiers.Any(SyntaxKind.InternalKeyword))
        {
            PartialClasses.Add(node.Identifier.Text, new List<ClassDeclarationSyntax> { node });
        }

        base.VisitClassDeclaration(node);
    }
} 