using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AElf.Client.Test.ContractMethodSplitter;

public class PublicOverrideMethodFinder : CSharpSyntaxWalker
{
    private readonly Dictionary<string, MethodDeclarationSyntax> _methods = new();
    public Dictionary<string, List<MethodDeclarationSyntax>> PublicMethods { get; } = new();

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.Modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            _methods[node.Identifier.Text] = node;
        }
        else if (node.Modifiers.Any(SyntaxKind.PublicKeyword) && node.Modifiers.Any(SyntaxKind.OverrideKeyword))
        {
            if (!PublicMethods.ContainsKey(node.Identifier.Text))
            {
                PublicMethods.Add(node.Identifier.Text, new List<MethodDeclarationSyntax> { node });
            }
        }
        base.VisitMethodDeclaration(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is IdentifierNameSyntax symbol &&
            _methods.TryGetValue(symbol.Identifier.Text, out var method))
        {
            foreach (var syntaxNode in node.Ancestors().Where(a => a is MethodDeclarationSyntax))
            {
                var caller = (MethodDeclarationSyntax)syntaxNode;
                var methodName = caller.Identifier.Text;
                if (PublicMethods.TryGetValue(methodName, out var publicMethod))
                {
                    publicMethod.Add(method);
                }
            }
        }

        base.VisitInvocationExpression(node);
    }
}