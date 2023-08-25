using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.ImUser.Dto;
using GraphQL;
using Nest;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.ImUser;

[RemoteService(false), DisableAuditing]
public class ImUserAppService : CAServerAppService, IImUserAppService
{
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly IGraphQLHelper _graphQlHelper;

    public ImUserAppService(INESTRepository<CAHolderIndex, Guid> caHolderRepository, IGraphQLHelper graphQlHelper)
    {
        _caHolderRepository = caHolderRepository;
        _graphQlHelper = graphQlHelper;
    }

    public async Task<HolderInfoResultDto> GetHolderInfoAsync(Guid userId)
    {
        var holder = await GetCaHolderAsync(userId);
        if (holder == null) return null;

        var result = new HolderInfoResultDto()
        {
            UserId = userId,
            CaHash = holder.CaHash,
            WalletName = holder.NickName,
            AddressInfos = new List<AddressInfoDto>()
        };

        var guardians = await GetCaHolderInfoAsync(holder.CaHash);

        guardians?.CaHolderInfo?.Select(t => new { t.CaAddress, t.ChainId })?.ToList().ForEach(t =>
        {
            result.AddressInfos.Add(new AddressInfoDto()
            {
                ChainId = t.ChainId,
                Address = t.CaAddress
            });
        });

        return result;
    }

    public async Task<CAHolderIndex> GetCaHolderAsync(Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>() { };

        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _caHolderRepository.GetAsync(Filter);
    }

    public async Task<GuardiansDto> GetCaHolderInfoAsync(string caHash, int skipCount = 0,
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