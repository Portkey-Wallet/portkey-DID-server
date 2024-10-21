using System;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Common;
using GraphQL;
using Microsoft.Extensions.Caching.Distributed;
using Portkey.Contracts.CA;
using Serilog;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.Guardian.Provider;

public class GuardianProvider : IGuardianProvider, ITransientDependency
{
    private const string HolderInfoCachePrefix = "Portkey:HolderInfoCache:";
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IContractProvider _contractProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedCache<GetHolderInfoOutput> _guardiansCache;

    public GuardianProvider(IGraphQLHelper graphQlHelper, IContractProvider contractProvider,
        IObjectMapper objectMapper, IDistributedCache<GetHolderInfoOutput> guardiansCache)
    {
        _graphQlHelper = graphQlHelper;
        _contractProvider = contractProvider;
        _objectMapper = objectMapper;
        _guardiansCache = guardiansCache;
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

    public async Task<GetHolderInfoOutput> GetHolderInfoFromCacheAsync(string guardianIdentifierHash, string chainId, bool needCache = false)
    {
        var key = GetHolderInfoCacheKey(guardianIdentifierHash, chainId);
        var result = await _guardiansCache.GetAsync(key: key);
        if (result != null)
        {
            Log.Logger.Information("===================================================GetHolderInfoFromCacheAsync invoked");
            return result;
        }
        var holderInfoOutput = await GetHolderInfoFromContractAsync(guardianIdentifierHash, null, chainId);
        // var guardianResult = _objectMapper.Map<GetHolderInfoOutput, GuardianResultDto>(holderInfoOutput);
        if (needCache && holderInfoOutput != null)
        {
            await _guardiansCache.SetAsync(key, holderInfoOutput, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(20)
            });
        }
        return holderInfoOutput;
    }
    
    private static string GetHolderInfoCacheKey(string guardianIdentifierHash, string chainId)
    {
        return HolderInfoCachePrefix + guardianIdentifierHash + chainId;
    }
}