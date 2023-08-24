using System.Threading.Tasks;
using CAServer.Common;
using GraphQL;
using Volo.Abp.DependencyInjection;

namespace CAServer.CAAccount.Provider;

public interface ICAAccountProvider
{
    Task<GuardianAddedCAHolderDto> GetGuardianAddedCAHolderAsync(string loginGuardianIdentifierHash,
        int inputSkipCount, int inputMaxResultCount);
}

public class CAAccountProvider : ICAAccountProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;


    public CAAccountProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<GuardianAddedCAHolderDto> GetGuardianAddedCAHolderAsync(string loginGuardianIdentifierHash,
        int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<GuardianAddedCAHolderDto>(new GraphQLRequest
        {
            Query = @"
			    query ($loginGuardianIdentifierHash:String,$skipCount:Int!,$maxResultCount:Int!){
                    guardianAddedCAHolderInfo(dto: {loginGuardianIdentifierHash:$loginGuardianIdentifierHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                    data{id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}},totalRecordCount}
                }",
            Variables = new
            {
                loginGuardianIdentifierHash, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount
            }
        });
    }
}