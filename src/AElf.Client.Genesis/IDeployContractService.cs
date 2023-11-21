using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Client.Genesis;

public interface IDeployContractService
{
    Task<Tuple<Address?, string>> DeployCSharpContract(string contractFileName);
    Task<Address> DeploySolidityContract(DeploySoliditySmartContractInput deploySoliditySmartContractInput);
}