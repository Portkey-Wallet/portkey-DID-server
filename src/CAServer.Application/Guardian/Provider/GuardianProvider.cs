using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.CAAccount.Dtos.Zklogin;
using CAServer.Common;
using CAServer.Options;
using GraphQL;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
    private readonly IDistributedCache<GuardianResultDto> _guardiansCache;
    private readonly LoginCacheOptions _loginCacheOptions;

    public GuardianProvider(IGraphQLHelper graphQlHelper, IContractProvider contractProvider,
        IObjectMapper objectMapper, IDistributedCache<GuardianResultDto> guardiansCache,
        IOptions<LoginCacheOptions> loginCacheOptions)
    {
        _graphQlHelper = graphQlHelper;
        _contractProvider = contractProvider;
        _objectMapper = objectMapper;
        _guardiansCache = guardiansCache;
        _loginCacheOptions = loginCacheOptions.Value;
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

    public async Task<GuardianResultDto> GetHolderInfoFromCacheAsync(string guardianIdentifierHash, string chainId, bool needCache = false)
    {
        var key = GetHolderInfoCacheKey(guardianIdentifierHash, chainId);
        var result = await _guardiansCache.GetAsync(key: key);
        if (result != null)
        {
            return result;
        }
        var holderInfoOutput = await GetHolderInfoFromContractAsync(guardianIdentifierHash, null, chainId);
        var guardianResult = _objectMapper.Map<GetHolderInfoOutput, GuardianResultDto>(holderInfoOutput);
        AppendZkLoginInfo(holderInfoOutput, guardianResult);
        if (needCache && holderInfoOutput != null)
        {
            await _guardiansCache.SetAsync(key, guardianResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(_loginCacheOptions.HolderInfoCacheSeconds)
            });
        }
        return guardianResult;
    }

    public void AppendZkLoginInfo(GetHolderInfoOutput holderInfo, GuardianResultDto guardianResult)
    {
        if (guardianResult == null)
        {
            return;
        }

        foreach (var guardian in guardianResult.GuardianList.Guardians)
        {
            var guardianFromHolder = holderInfo.GuardianList.Guardians.FirstOrDefault(g =>
                guardian.IdentifierHash.Equals(g.IdentifierHash.ToHex()));
            if (guardianFromHolder?.ZkLoginInfo == null)
            {
                guardian.VerifiedByZk = false;
                continue;
            }
            var zkLoginInfo = guardianFromHolder.ZkLoginInfo;
            guardian.VerifiedByZk = zkLoginInfo is not null
                                    && zkLoginInfo.IdentifierHash is not null
                                    && zkLoginInfo.Salt is not (null or "")
                                    && zkLoginInfo.Nonce is not (null or "")
                                    && zkLoginInfo.ZkProof is not (null or "")
                                    && zkLoginInfo.CircuitId is not (null or "")
                                    && zkLoginInfo.Issuer is not (null or "")
                                    && zkLoginInfo.Kid is not (null or "")
                                    && zkLoginInfo.NoncePayload is not null;
            guardian.PoseidonIdentifierHash = guardianFromHolder.PoseidonIdentifierHash;
        }
    }
    
    private static string GetHolderInfoCacheKey(string guardianIdentifierHash, string chainId)
    {
        return HolderInfoCachePrefix + guardianIdentifierHash + chainId;
    }
}