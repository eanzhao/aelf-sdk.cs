using AElf.Client.Core;
using AElf.SolidityContract;
using AElf.Standards.ACS0;
using AElf.Types;

namespace AElf.Client.Genesis;

public interface IGenesisService
{
    Task<SendTransactionResult> ProposeNewContract(ContractDeploymentInput contractDeploymentInput);

    Task<SendTransactionResult> ProposeUpdateContract(ContractUpdateInput contractUpdateInput);

    Task<SendTransactionResult> ReleaseApprovedContract(ReleaseContractInput releaseContractInput);

    Task<SendTransactionResult> ReleaseCodeCheckedContract(ReleaseContractInput releaseContractInput);
    Task<SendTransactionResult> DeploySoliditySmartContract(DeploySoliditySmartContractInput deploySoliditySmartContractInput);


    Task<AuthorityInfo> GetContractDeploymentController();
    Task<SmartContractRegistration> GetSmartContractRegistrationByCodeHash(Hash codeHash);
    Task<SmartContractRegistration> GetSmartContractRegistrationByAddress(Address address);
    Task<Address> GetContractAddressByName(Hash contractNameHash);
}