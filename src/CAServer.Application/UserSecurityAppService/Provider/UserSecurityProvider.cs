using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Security.Dtos;
using GraphQL;

namespace CAServer.UserSecurityAppService.Provider;

public class UserSecurityProvider : IUserSecurityProvider
{
    private readonly IGraphQLHelper _graphQlHelper;

    public UserSecurityProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<IndexerTransferLimitList> GetTransferLimitListByCaHash(string caHash)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransferLimitList>(new GraphQLRequest
        {
            Query = @"
			    query($caHash:String) {
                    caHolderTransferLimit(dto: {caHash:$caHash}){
                        data{chainId,caHash,symbol,singleLimit,dailyLimit},totalRecordCount}
                }",
            Variables = new
            {
                caHash = caHash
            }
        });
    }
}