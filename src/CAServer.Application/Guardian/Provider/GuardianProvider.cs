using System.Threading.Tasks;
using CAServer.Common;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Volo.Abp.DependencyInjection;

namespace CAServer.Guardian.Provider;

public class GuardianProvider : IGuardianProvider, ITransientDependency
{
    private readonly IGraphQLHelper _graphQlHelper;

    public GuardianProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<GuardiansDto> GetGuardiansAsync(string loginGuardianIdentifierHash, string caHash)
    {
        return await GetGuardianListByScanAsync(loginGuardianIdentifierHash, caHash);
    }

    public Task<string> GetRegisterChainIdAsync(string loginGuardianIdentifierHash, string caHash)
    {
        throw new System.NotImplementedException();
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
}