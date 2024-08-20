namespace Solang;

public static class ABIExtensions
{
    public static string GetSelector(this SolangABI solangAbi, string methodName)
    {
        var selectorWithPrefix = (solangAbi.Spec.Messages.FirstOrDefault(m => m.Label == methodName)?.Selector ??
                                  solangAbi.Spec.Messages.FirstOrDefault(m => m.Label == $"{methodName}_")
                                      ?.Selector) ??
                                 solangAbi.Spec.Messages.FirstOrDefault(m => m.Label.StartsWith(methodName))
                                     ?.Selector;
        var selector = selectorWithPrefix?.Substring(selectorWithPrefix.StartsWith("0x") ? 2 : 0);
        if (selector == null)
        {
            throw new SelectorNotFoundException($"Selector of {methodName} not found.");
        }

        return selector;
    }

    public static string GetConstructor(this SolangABI solangAbi)
    {
        var selectorWithPrefix = solangAbi.Spec.Constructors.First().Selector;
        var selector = selectorWithPrefix.Substring(selectorWithPrefix.StartsWith("0x") ? 2 : 0);
        if (selector == null)
        {
            throw new SelectorNotFoundException($"Selector of constructor not found.");
        }

        return selector;
    }

    public static bool GetMutates(this SolangABI solangAbi, string methodName)
    {
        var mutates = (solangAbi.Spec.Messages.FirstOrDefault(m => m.Label == methodName)?.Mutates ??
                       solangAbi.Spec.Messages.FirstOrDefault(m => m.Label == $"{methodName}_")
                           ?.Mutates) ??
                      solangAbi.Spec.Messages.FirstOrDefault(m => m.Label.StartsWith(methodName))
                          ?.Mutates;
        if (mutates == null)
        {
            throw new SelectorNotFoundException($"Mutates of {methodName} not found.");
        }

        return mutates.Value;
    }
}