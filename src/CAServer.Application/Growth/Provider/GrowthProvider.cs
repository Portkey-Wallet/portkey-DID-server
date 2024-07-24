using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Growth.Dtos;
using CAServer.Options;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.DependencyInjection;

namespace CAServer.Growth.Provider;

public interface IGrowthProvider
{
    Task<GrowthIndex> GetGrowthInfoByLinkCodeAsync(string shortLinkCode);
    Task<GrowthIndex> GetGrowthInfoByCaHashAsync(string caHash);
    Task<List<GrowthIndex>> GetGrowthInfosAsync(List<string> caHashes, List<string> inviteCodes);

    Task<ReferralInfoDto> GetReferralInfoAsync(List<string> caHashes, List<string> referralCodes,
        List<string> methodNames, long startTime, long endTime);

    Task<List<GrowthIndex>> GetAllGrowthInfosAsync(int skip, int limit);

    Task<List<ReferralRecordIndex>> GetReferralRecordListAsync(string caHash, string referralCaHash, int skip,
        int limit, DateTime startDate, DateTime endDate, List<int> referralTypes);

    Task<bool> AddReferralRecordAsync(ReferralRecordIndex referralRecordIndex);
    Task<List<HamsterScoreDto>> GetHamsterScoreListAsync(List<string> addresses, DateTime startTime, DateTime endTime);
}

public class GrowthProvider : IGrowthProvider, ISingletonDependency
{
    private readonly INESTRepository<GrowthIndex, string> _growthRepository;
    private readonly INESTRepository<ReferralRecordIndex, string> _referralRecordRepository;

    private readonly IGraphQLHelper _graphQlHelper;
    private readonly HamsterOptions _hamsterOptions;

    public GrowthProvider(INESTRepository<GrowthIndex, string> growthRepository, IGraphQLHelper graphQlHelper,
        INESTRepository<ReferralRecordIndex, string> referralRecordRepository,
        IOptionsSnapshot<HamsterOptions> hamsterOptions)
    {
        _growthRepository = growthRepository;
        _graphQlHelper = graphQlHelper;
        _referralRecordRepository = referralRecordRepository;
        _hamsterOptions = hamsterOptions.Value;
    }

    public async Task<GrowthIndex> GetGrowthInfoByLinkCodeAsync(string shortLinkCode)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GrowthIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ShortLinkCode).Value(shortLinkCode))
        };

        QueryContainer Filter(QueryContainerDescriptor<GrowthIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _growthRepository.GetAsync(Filter);
    }

    public async Task<GrowthIndex> GetGrowthInfoByCaHashAsync(string caHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GrowthIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.CaHash).Value(caHash))
        };

        QueryContainer Filter(QueryContainerDescriptor<GrowthIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _growthRepository.GetAsync(Filter);
    }

    public async Task<List<GrowthIndex>> GetGrowthInfosAsync(List<string> caHashes, List<string> inviteCodes)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GrowthIndex>, QueryContainer>>();

        if (!inviteCodes.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.InviteCode).Terms(inviteCodes)));
        }

        if (!caHashes.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.CaHash).Terms(caHashes)));
        }

        QueryContainer Filter(QueryContainerDescriptor<GrowthIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (total, data) = await _growthRepository.GetListAsync(Filter);
        return data;
    }

    public async Task<ReferralInfoDto> GetReferralInfoAsync(List<string> caHashes, List<string> referralCodes,
        List<string> methodNames, long startTime, long endTime)
    {
        return await _graphQlHelper.QueryAsync<ReferralInfoDto>(new GraphQLRequest
        {
            Query = @"
			      query($caHashes:[String],$referralCodes:[String],$methodNames:[String],$startTime:Long!,$endTime:Long!) {
              referralInfo(dto: {caHashes:$caHashes,referralCodes:$referralCodes,methodNames:$methodNames,startTime:$startTime,endTime:$endTime}){
                     caHash,referralCode,projectCode,methodName,timestamp}
                }",
            Variables = new
            {
                caHashes, referralCodes, methodNames, startTime, endTime
            }
        });
    }


    public async Task<List<GrowthIndex>> GetAllGrowthInfosAsync(int skip, int limit)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GrowthIndex>, QueryContainer>>();
        QueryContainer Filter(QueryContainerDescriptor<GrowthIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (total, data) = await _growthRepository.GetListAsync(Filter, skip: skip, limit: limit);
        return data;
    }

    public async Task<List<ReferralRecordIndex>> GetReferralRecordListAsync(string caHash, string referralCaHash,
        int skip, int limit, DateTime startDate, DateTime endDate, List<int> referralTypes)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ReferralRecordIndex>, QueryContainer>>();

        if (!caHash.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.CaHash).Terms(caHash)));
        }

        if (!referralCaHash.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ReferralCaHash).Terms(referralCaHash)));
        }

        if (!referralTypes.IsNullOrEmpty())
        {
            //mustQuery.Add(q => q.Terms(i => i.Field(f => f.ReferralType).Terms(referralTypes)));
        }

        if (startDate != new DateTime())
        {
            mustQuery.Add(q =>
                q.DateRange(i => i.Field(f => f.ReferralDate).TimeZone("GMT+8").GreaterThanOrEquals(startDate)));
        }

        if (endDate != new DateTime())
        {
            mustQuery.Add(q =>
                q.DateRange(i => i.Field(f => f.ReferralDate).TimeZone("GMT+8").LessThanOrEquals(endDate)));
        }

        QueryContainer Filter(QueryContainerDescriptor<ReferralRecordIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (total, data) = await _referralRecordRepository.GetListAsync(Filter, sortExp: k => k.ReferralDate,
            sortType: SortOrder.Descending, skip: skip, limit: limit);
        return data;
    }

    public async Task<bool> AddReferralRecordAsync(ReferralRecordIndex referralRecordIndex)
    {
        var record =
            await GetReferralRecordListAsync(referralRecordIndex.CaHash, referralRecordIndex.ReferralCaHash, 0, 1,
                new DateTime(), new DateTime(), new List<int> { 0 });
        if (!record.IsNullOrEmpty())
        {
            return false;
        }

        await _referralRecordRepository.AddAsync(referralRecordIndex);
        return true;
    }

    public async Task<List<HamsterScoreDto>> GetHamsterScoreListAsync(List<string> caAddressList, DateTime beginTime,
        DateTime endTime)
    {
        var graphQlClient = new GraphQLHttpClient(_hamsterOptions.HamsterEndPoints,
            new NewtonsoftJsonSerializer());
        var sendQueryAsync = await graphQlClient.SendQueryAsync<List<HamsterScoreDto>>(new GraphQLRequest
        {
            Query = @"
			      query($caAddressList:[String],$beginTime:DateTime,$endTime:DateTime) {
              getScoreInfos(getScoreInfosDto: {caAddressList:$caAddressList,beginTime:$startTime,endTime:$endTime}){
                     caAddress,sumScore,symbol,decimals}
                }",
            Variables = new
            {
                caAddressList, beginTime, endTime
            }
        });
        return sendQueryAsync.Data;
    }
}