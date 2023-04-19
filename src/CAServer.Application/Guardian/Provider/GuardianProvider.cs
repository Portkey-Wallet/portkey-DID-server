using System.Threading.Tasks;
using AElf.Types;
using CAServer.Common;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Portkey.Contracts.CA;
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

    public async Task<GetHolderInfoOutput> GetHolderInfoFromContractAsync(
        string guardianIdentifierHash,
        string caHash,
        ChainInfo chainInfo)
    {
        var param = new GetHolderInfoInput();

        if (!string.IsNullOrWhiteSpace(caHash))
        {
            param.CaHash = Hash.LoadFromHex(caHash);
            param.LoginGuardianIdentifierHash = null;
        }
        else
        {
            param.LoginGuardianIdentifierHash = Hash.LoadFromHex(guardianIdentifierHash);
            param.CaHash = null;
        }

        var output =
            await ContractHelper.CallTransactionAsync<GetHolderInfoOutput>(MethodName.GetHolderInfo, param, false,
                chainInfo);

        return output;
    }
}