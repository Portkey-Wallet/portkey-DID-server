using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.CAAccount.Provider;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Guardian.Provider;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class SecondaryEmailHandler : IDistributedEventHandler<AccountEmailEto>, ITransientDependency
{
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<SecondaryEmailHandler> _logger;
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly ICAAccountProvider _accountProvider;

    public SecondaryEmailHandler(INESTRepository<GuardianIndex, string> guardianRepository,
        IObjectMapper objectMapper,
        ILogger<SecondaryEmailHandler> logger,
        IGraphQLHelper graphQlHelper,
        ICAAccountProvider accountProvider)
    {
        _guardianRepository = guardianRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _graphQlHelper = graphQlHelper;
        _accountProvider = accountProvider;
    }

    // todo: need to sync.
    public async Task HandleEventAsync(AccountEmailEto eventData)
    {
        if (string.IsNullOrEmpty(eventData.SecondaryEmail) || string.IsNullOrEmpty(eventData.CaHash))
        {
            _logger.LogError("SecondaryEmailHandler Params Error received AccountEmailEto:{0}",
                JsonConvert.SerializeObject(eventData));
            return;
        }

        //1. consumer get guardianList by caHash through GraphQl
        var guardiansDto = await GetCaHolderInfoAsync(eventData.CaHash);
        if (guardiansDto == null || guardiansDto.CaHolderInfo.IsNullOrEmpty())
        {
            _logger.LogError("SecondaryEmailHandler query guardians from graphql failed guardiansDto:{0}",
                JsonConvert.SerializeObject(eventData));
            return;
        }

        var guardians = guardiansDto.CaHolderInfo
            .Where(dto => dto.GuardianList != null)
            .Select(dto => dto.GuardianList).ToList();
        foreach (var guardian in guardians.SelectMany(guardianBaseList => guardianBaseList.Guardians))
        {
            if (guardian == null)
            {
                continue;
            }

            GuardianIndex guardianIndex = null;
            try
            {
                //2. extract guardian's IdentifierHash, query 
                guardianIndex = await _accountProvider.GetIdentifiersAsync(guardian.IdentifierHash);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HandleEventAsync get guardianFromGraphQL failed identifierHash:{0}",
                    guardian.IdentifierHash);
            }

            if (guardianIndex == null)
            {
                continue;
            }

            //3、append Guardian's(from es) caHash and secondaryEmail fields 
            guardianIndex.CaHash = eventData.CaHash;
            guardianIndex.SecondaryEmail = eventData.SecondaryEmail;
            try
            {
                await _guardianRepository.AddOrUpdateAsync(guardianIndex);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HandleEventAsync AddOrUpdateAsync failed identifierHash:{0} caHash:{1} email:{2}",
                    guardian.IdentifierHash, eventData.CaHash, eventData.SecondaryEmail);
            }
        }
    }

    private async Task<GuardiansDto> GetCaHolderInfoAsync(string caHash, int skipCount = 0,
        int maxResultCount = 10)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
			    query($caHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderInfo(dto: {caHash:$caHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}}
                }",
            Variables = new
            {
                caHash, skipCount, maxResultCount
            }
        });
    }
}