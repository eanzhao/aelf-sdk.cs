using AElf.Client.Core;
using Forest.Contracts.SymbolRegistrar;

namespace AElf.Client.SymbolRegistrar;

public interface ISymbolRegistrarService
{
    Task<SendTransactionResult> CreateSeedAsync(CreateSeedInput createSeedInput);
}