using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AElf.Client.Test.ContractMethodSplitter;

public class ContractMethodFinder : CSharpSyntaxWalker
{
    private readonly List<MethodDeclarationSyntax> _methodDeclarations = new();
    private readonly List<InvocationExpressionSyntax> _invocationExpressions = new();
    private readonly Dictionary<string, MethodDeclarationSyntax> _privateMethods = new();
    public Dictionary<string, List<MethodDeclarationSyntax>> PublicMethods { get; } = new();

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        _methodDeclarations.Add(node);
        base.VisitMethodDeclaration(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        _invocationExpressions.Add(node);
        base.VisitInvocationExpression(node);
    }
    
    public void ProcessNodes()
    {
        foreach (var methodDeclarationSyntax in _methodDeclarations)
        {
            if (methodDeclarationSyntax.Modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                _privateMethods[methodDeclarationSyntax.Identifier.Text] = methodDeclarationSyntax;
            }
            else if (methodDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     methodDeclarationSyntax.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                if (!PublicMethods.ContainsKey(methodDeclarationSyntax.Identifier.Text))
                {
                    PublicMethods.Add(methodDeclarationSyntax.Identifier.Text,
                        new List<MethodDeclarationSyntax> { methodDeclarationSyntax });
                }
            }

            base.VisitMethodDeclaration(methodDeclarationSyntax);
        }

        var invocationExpressions = _invocationExpressions.ToList();
        foreach (var invocationExpressionSyntax in invocationExpressions)
        {
            var methodName = invocationExpressionSyntax.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                _ => null
            };

            if (methodName != null && _privateMethods.TryGetValue(methodName, out var method))
            {
                foreach (var syntaxNode in invocationExpressionSyntax.Ancestors()
                             .Where(a => a is MethodDeclarationSyntax))
                {
                    var caller = (MethodDeclarationSyntax)syntaxNode;
                    var callerName = caller.Identifier.Text;
                    if (PublicMethods.TryGetValue(callerName, out var publicMethod))
                    {
                        if (!publicMethod.Contains(method))
                        {
                            publicMethod.Add(method);
                        }
                    }
                }
            }

            base.VisitInvocationExpression(invocationExpressionSyntax);
        }
    }
}