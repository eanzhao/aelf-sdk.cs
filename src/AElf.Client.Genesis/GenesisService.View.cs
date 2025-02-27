using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Client.Genesis;

public partial class GenesisService
{
    public async Task<AuthorityInfo> GetContractDeploymentController()
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(_contractAddress, "GetContractDeploymentController",
            new Empty(), useClientAlias); 
        var authorityInfo = new AuthorityInfo();
        authorityInfo.MergeFrom(result);
        return authorityInfo;
    }

    public async Task<SmartContractRegistration> GetSmartContractRegistrationByCodeHash(Hash codeHash)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(_contractAddress, "GetSmartContractRegistrationByCodeHash",
            codeHash , useClientAlias); 
        var smartContractRegistration = new SmartContractRegistration();
        smartContractRegistration.MergeFrom(result);
        return smartContractRegistration;
    }

    public async Task<SmartContractRegistration> GetSmartContractRegistrationByAddress(Address address)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(_contractAddress, "GetSmartContractRegistrationByAddress",
            address , useClientAlias); 
        var smartContractRegistration = new SmartContractRegistration();
        smartContractRegistration.MergeFrom(result);
        return smartContractRegistration;
    }

    public async Task<Address> GetContractAddressByName(Hash contractNameHash)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(_contractAddress, "GetContractAddressByName",
            contractNameHash , useClientAlias); 
        var address = new Address();
        address.MergeFrom(result);
        return address;
    }

    public async Task<ContractInfo> GetContractInfo(Address contractAddress)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(_contractAddress, "GetContractInfo",
            contractAddress, useClientAlias);
        var contractInfo = new ContractInfo();
        contractInfo.MergeFrom(result);
        return contractInfo;
    }
}