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
    
    public async Task HandleEventAsync(AccountEmailEto eventData)
    {
        _logger.LogDebug("SecondaryEmailHandler receive AccountEmailEto:{0}", JsonConvert.SerializeObject(eventData));
        if (string.IsNullOrEmpty(eventData.SecondaryEmail) || string.IsNullOrEmpty(eventData.CaHash))
        {
            _logger.LogError("SecondaryEmailHandler Params Error received AccountEmailEto:{0}", JsonConvert.SerializeObject(eventData));
            return;
        }
        //1、消息接收者通过扫链GraphQlHelper通过cahash查询GuardianList
        var guardiansDto = await GetCaHolderInfoAsync(eventData.CaHash);
        //2、GuardianList通过Guardian的IdentifierHash查询es
        if (guardiansDto == null || guardiansDto.CaHolderInfo.IsNullOrEmpty())
        {
            _logger.LogError("SecondaryEmailHandler query guardians from graphql failed guardiansDto:{0}", JsonConvert.SerializeObject(eventData));
            return;
        }
        _logger.LogDebug("SecondaryEmailHandler guardians from contract guardiansDto:{0}", JsonConvert.SerializeObject(guardiansDto));
        var guardians = guardiansDto.CaHolderInfo
            .Where(dto => dto.OriginChainId.Equals(dto.ChainId) && dto.GuardianList != null)
            .Select(dto => dto.GuardianList).ToList();
        //3、es的Guardian数据新增或者更新cahash和secondaryEmail字段
        foreach (var guardian in guardians.SelectMany(guardianBaseList => guardianBaseList.Guardians))
        {
            GuardianIndex guardianIndex = null;
            try
            {
                guardianIndex = await _accountProvider.GetIdentifiersAsync(guardian.IdentifierHash);
                _logger.LogDebug("SecondaryEmailHandler GetIdentifiersAsync from es identifierHash:{0} guardianIndex:{1}",
                    guardian.IdentifierHash, JsonConvert.SerializeObject(guardianIndex));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HandleEventAsync get guardianFromGraphQL failed identifierHash:{0}", guardian.IdentifierHash);
            }
            if (guardianIndex == null)
            {
                continue;
            }

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