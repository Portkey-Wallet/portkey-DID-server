using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.ImUser.Dto;
using CAServer.PrivacyPermission;
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
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly IPrivacyPermissionAppService _privacyPermissionAppService;
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly IGuardianProvider _guardianProvider;

    public ImUserAppService(INESTRepository<CAHolderIndex, Guid> caHolderRepository, IGraphQLHelper graphQlHelper,
        INESTRepository<GuardianIndex, string> guardianRepository,
        IPrivacyPermissionAppService privacyPermissionAppService,
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository, IGuardianProvider guardianProvider)
    {
        _caHolderRepository = caHolderRepository;
        _graphQlHelper = graphQlHelper;
        _guardianRepository = guardianRepository;
        _privacyPermissionAppService = privacyPermissionAppService;
        _userExtraInfoRepository = userExtraInfoRepository;
        _guardianProvider = guardianProvider;
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
            Avatar = holder.Avatar,
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

    public async Task<List<Guid>> ListHolderInfoAsync(string keyword)
    {
        var privacyType = GetPrivacyType(keyword);

        if (privacyType == PrivacyType.Unknow)
        {
            return new List<Guid>();
        }

        //query by email/phone from guardian
        var guidsByGuardian = await GetIdsByGuardianAsync(keyword, privacyType);

        //query by apple/google
        var guidsByUserExtraInfo = await GetIdsByUserExtraInfoAsync(keyword);

        //get union
        return guidsByGuardian.Union(guidsByUserExtraInfo).ToList();
    }

    public async Task<List<HolderInfoResultDto>> GetUserInfoAsync(List<Guid> userIds)
    {
        var holders = await GetHolderIndexListAsync(userIds);
        return ObjectMapper.Map<List<CAHolderIndex>, List<HolderInfoResultDto>>(holders);
    }

    private async Task<List<CAHolderIndex>> GetHolderIndexListAsync(List<Guid> userIds)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.UserId).Terms(userIds)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var userExtraInfos = await _caHolderRepository.GetListAsync(Filter);

        return userExtraInfos.Item2;
    }

    private async Task<List<Guid>> GetIdsByUserExtraInfoAsync(string keyword)
    {
        var approvedAllUserIds = new List<Guid>();
        var userExtraInfos = await ListUserExtraInfoAsync(keyword);
        var guardianGroup = userExtraInfos.GroupBy(u => u.GuardianType);

        foreach (var group in guardianGroup)
        {
            if (!Enum.TryParse<PrivacyType>(group.Key, out var privacyType))
            {
                continue;
            }

            var ids = group.Select(u =>
                StringHelper.RemovePrefix(u.Id, CommonConstant.UserExtraInfoIdPrefix)).ToList();

            var guardianTasks = ids.Select(GetGuardianInfoAsync).ToList();
            var guardians = await Task.WhenAll(guardianTasks);

            var identifierHashList = guardians.Select(g => g.IdentifierHash).ToList();

            var guardianDtos = await GetCaHashAsync(identifierHashList);
            var allCaHash = guardianDtos.Select(c => c.CaHash).ToList();

            var caHolders = await GetCaHolderByCaHashAsync(allCaHash);

            var userIds = caHolders.Select(c => c.UserId).ToList();

            var (approvedUserIds, rejectedUserIds) =
                await _privacyPermissionAppService.CheckPrivacyPermissionAsync(userIds, keyword, privacyType);
            approvedAllUserIds.AddRange(approvedUserIds);
        }

        return approvedAllUserIds;
    }

    private async Task<List<Guid>> GetIdsByGuardianAsync(string keyword, PrivacyType privacyType)
    {
        var guardianIndices = await ListGuardianInfoAsync(keyword);

        var identifierHashList = guardianIndices.Select(g => g.IdentifierHash).ToList();

        var guardianDtos = await GetCaHashAsync(identifierHashList);
        var caHashList = guardianDtos.Select(c => c.CaHash).ToList();

        var caHolders = await GetCaHolderByCaHashAsync(caHashList);

        var userIds = caHolders.Select(c => c.UserId).ToList();

        var (approvedUserIds, rejectedUserIds) =
            await _privacyPermissionAppService.CheckPrivacyPermissionAsync(userIds, keyword, privacyType);
        return approvedUserIds;
    }

    private async Task<List<GuardianDto>> GetCaHashAsync(List<string> identifierHashList)
    {
        var result = new List<GuardianDto>();
        foreach (var loginGuardianIdentifierHash in identifierHashList)
        {
            var holderInfo = await _guardianProvider.GetGuardiansAsync(loginGuardianIdentifierHash, null);
            result.AddRange(holderInfo.CaHolderInfo);
        }

        return result;
    }

    private async Task<List<UserExtraInfoIndex>> ListUserExtraInfoAsync(string keyword)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserExtraInfoIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.Email).Value(keyword)));

        QueryContainer Filter(QueryContainerDescriptor<UserExtraInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        var userExtraInfos = await _userExtraInfoRepository.GetListAsync(Filter);

        return userExtraInfos.Item2;
    }

    private async Task<List<GuardianIndex>> ListGuardianInfoAsync(string keyword)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.Identifier).Value(keyword)));
        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) => f.Bool(b => b.Must(mustQuery));
        var guardians = await _guardianRepository.GetListAsync(Filter);

        return guardians.Item2;
    }

    private async Task<GuardianIndex> GetGuardianInfoAsync(string identifier)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.Identifier).Value(identifier)));
        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) => f.Bool(b => b.Must(mustQuery));
        var guardianIndex = await _guardianRepository.GetAsync(Filter);
        return guardianIndex;
    }

    private async Task<List<CAHolderIndex>> GetCaHolderByCaHashAsync(List<string> caHashList)
    {
        if (caHashList == null || caHashList.Count == 0)
        {
            return new List<CAHolderIndex>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>() { };

        mustQuery.Add(q => q.Terms(i => i.Field(f => f.CaHash).Terms(caHashList)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var holders = await _caHolderRepository.GetListAsync(Filter);

        return holders.Item2;
    }

    private static PrivacyType GetPrivacyType(string keyword)
    {
        var privacyType = PrivacyType.Unknow;
        if (VerifyHelper.IsEmail(keyword))
        {
            privacyType = PrivacyType.Email;
        }

        if (VerifyHelper.IsPhone(keyword))
        {
            privacyType = PrivacyType.Phone;
        }

        return privacyType;
    }

    public async Task<CAHolderIndex> GetCaHolderAsync(Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<CAHolderIndex>, QueryContainer>>() { };

        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<CAHolderIndex> f) => f.Bool(b => b.Must(mustQuery));
        var holder = await _caHolderRepository.GetAsync(Filter);
        if (holder == null || holder.IsDeleted) return null;

        return holder;
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