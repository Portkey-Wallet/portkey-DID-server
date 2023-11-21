using System.Threading.Tasks;
using AElf.Types;
using CAServer.Common;
using GraphQL;
using Portkey.Contracts.CA;
using Volo.Abp.DependencyInjection;

namespace CAServer.Guardian.Provider;

public class GuardianProvider : IGuardianProvider, ITransientDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IContractProvider _contractProvider;


    public GuardianProvider(IGraphQLHelper graphQlHelper, IContractProvider contractProvider)
    {
        _graphQlHelper = graphQlHelper;
        _contractProvider = contractProvider;
    }

    public async Task<GuardiansDto> GetGuardiansAsync(string loginGuardianIdentifierHash, string caHash)
    {
        return await GetGuardianListByScanAsync(loginGuardianIdentifierHash, caHash);
    }

    private async Task<GuardiansDto> GetGuardianListByScanAsync(string loginGuardianIdentifierHash, string caHash)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
			    query($caHash:String,$loginGuardianIdentifierHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caHash:$caHash,loginGuardianIdentifierHash:$loginGuardianIdentifierHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}}
                }",
            Variables = new
            {
                caHash, loginGuardianIdentifierHash, skipCount = 0, maxResultCount = 10
            }
        });
    }

    public async Task<GetHolderInfoOutput> GetHolderInfoFromContractAsync(string guardianIdentifierHash, string caHash,
        string chainId)
    {
        if (!string.IsNullOrWhiteSpace(caHash))
        {
            return await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(caHash), null, chainId);
        }

        return await _contractProvider.GetHolderInfoAsync(null, Hash.LoadFromHex(guardianIdentifierHash), chainId);
    }
}