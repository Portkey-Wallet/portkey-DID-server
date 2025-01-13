using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.Common;
using CAServer.Guardian;
using CAServer.Options;
using CAServer.Security;
using CAServer.Security.Dtos;
using CAServer.Security.Etos;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using CAServer.UserSecurity.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.UserSecurity;

[RemoteService(false), DisableAuditing]
public class UserSecurityAppService : CAServerAppService, IUserSecurityAppService
{
    private readonly ILogger<UserSecurityAppService> _logger;
    private readonly SecurityOptions _securityOptions;
    private readonly IContractProvider _contractProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IUserAssetsProvider _assetsProvider;
    private readonly IUserSecurityProvider _userSecurityProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IAssetsLibraryProvider _assetsLibraryProvider;
    private const string _defaultSymbol = "ELF";

    public UserSecurityAppService(IOptionsSnapshot<SecurityOptions> securityOptions,
        IUserSecurityProvider userSecurityProvider, IOptionsSnapshot<ChainOptions> chainOptions,
        IContractProvider contractProvider, ILogger<UserSecurityAppService> logger, IUserAssetsProvider assetsProvider,
        IDistributedEventBus distributedEventBus, IAssetsLibraryProvider assetsLibraryProvider)
    {
        _logger = logger;
        _assetsProvider = assetsProvider;
        _distributedEventBus = distributedEventBus;
        _chainOptions = chainOptions.Value;
        _securityOptions = securityOptions.Value;
        _contractProvider = contractProvider;
        _userSecurityProvider = userSecurityProvider;
        _assetsLibraryProvider = assetsLibraryProvider;
    }

    public async Task<TransferLimitListResultDto> GetTransferLimitListByCaHashAsync(
        GetTransferLimitListByCaHashDto input)
    {
        try
        {
            // Obtain the balance of all token assets by caHash
            var assert = await GetUserAssetsAsync(input.CaHash);
            if (assert.CaHolderSearchTokenNFT.TotalRecordCount == 0)
            {
                _logger.LogDebug("CaHash: {caHash} don't have token assert.", input.CaHash);
                return new TransferLimitListResultDto { Data = new List<TransferLimitDto>() };
            }

            _logger.LogDebug("CaHash: {caHash} have {COUNT} token assert.", input.CaHash,
                assert.CaHolderSearchTokenNFT.TotalRecordCount);

            // Use the default token transferLimit without updating the transferLimit
            var dic = new Dictionary<string, TransferLimitDto>();
            foreach (var token in assert.CaHolderSearchTokenNFT.Data)
            {
                if (token.TokenInfo == null || !await AddUserTransferLimitHistoryAsync(input.CaHash, token)) continue;
                dic[token.ChainId + "-" + token.TokenInfo.Symbol] = await GeneratorTransferLimitAsync(token);
            }

            // If the transferLimit is updated, the token transferLimit will be overwritten
            // var res = new IndexerTransferLimitList();
            var res = await _userSecurityProvider.GetTransferLimitListByCaHashAsync(input.CaHash);
            _logger.LogDebug("CaHash: {caHash} have {COUNT} transfer limit change history.", input.CaHash,
                res.CaHolderTransferLimit.TotalRecordCount);

            foreach (var transferLimit in res.CaHolderTransferLimit.Data)
            {
                var tempKey = transferLimit.ChainId + "-" + transferLimit.Symbol;
                if (dic.TryGetValue(tempKey, out var tempValue))
                {
                    tempValue.DailyLimit = transferLimit.DailyLimit;
                    tempValue.SingleLimit = transferLimit.SingleLimit;
                    tempValue.Restricted = !(transferLimit.DailyLimit == "-1" && transferLimit.SingleLimit == "-1");
                }
            }

            _logger.LogDebug("CaHash: {caHash} have {COUNT} transfer limit list.", input.CaHash, dic.Count);

            return new TransferLimitListResultDto
            {
                TotalRecordCount = dic.Count,
                Data = dic.Values.ToList().OrderBy(t => t.Symbol != _defaultSymbol).ThenBy(t => t.Symbol)
                    .ThenBy(t => t.ChainId).ToList().Skip(input.SkipCount).Take(input.MaxResultCount).ToList()
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An exception occurred during GetTransferLimitListByCaHashAsync, caHash: {caHash}",
                input.CaHash);
            throw new UserFriendlyException("An exception occurred during GetTransferLimitListByCaHashAsync");
        }
    }

    public async Task<ManagerApprovedListResultDto> GetManagerApprovedListByCaHashAsync(
        GetManagerApprovedListByCaHashDto input)
    {
        try
        {
            var res = await _userSecurityProvider.GetManagerApprovedListByCaHashAsync(input.CaHash, input.Spender,
                input.Symbol, input.SkipCount, input.MaxResultCount);
            return new ManagerApprovedListResultDto
            {
                TotalRecordCount = res.CaHolderManagerApproved.TotalRecordCount,
                Data = res.CaHolderManagerApproved.Data
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "An exception occurred during GetManagerApprovedListByCaHashAsync, chainId: {ChainId} caHash: {caHash}",
                input.ChainId, input.CaHash);
            throw new UserFriendlyException(
                $"An exception occurred during GetManagerApprovedListByCaHashAsync, chainId: {input.ChainId}");
        }
    }

    public async Task<TokenBalanceTransferCheckAsyncResultDto> GetTokenBalanceTransferCheckAsync(
        GetTokenBalanceTransferCheckDto input)
    {
        try
        {
            var registryChainGuardianSet = new HashSet<string>();
            var nonRegistryChainGuardianSet = new HashSet<string>();
            var holderInfoOutputs = new List<GetHolderInfoOutput>();
            foreach (var chainInfo in _chainOptions.ChainInfos)
            {
                var info = await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(input.CaHash), null,
                    chainInfo.Value.ChainId);
                holderInfoOutputs.Add(info);
                if (!(info?.GuardianList?.Guardians?.Count > 0))
                {
                    continue;
                }

                var registryFlag = chainInfo.Value.ChainId == ChainHelper.ConvertChainIdToBase58(info.CreateChainId);

                foreach (var g in info.GuardianList.Guardians)
                {
                    var guardianName = g.VerifierId + g.IdentifierHash.ToHex();
                    if (registryFlag) registryChainGuardianSet.AddIfNotContains(guardianName);
                    else nonRegistryChainGuardianSet.AddIfNotContains(guardianName);
                }
            }

            var accelerateGuardians = await GetAccelerateGuardiansAsync(input.CaHash, holderInfoOutputs);

            var registryChainGuardianCount = registryChainGuardianSet.Count();
            var nonRegistryChainGuardianCount = nonRegistryChainGuardianSet.Count();
            _logger.LogDebug("CaHash: {caHash} have {COUNT} registry count {non-registry COUNT} non-registry count.",
                input.CaHash, registryChainGuardianCount, nonRegistryChainGuardianCount);
            var isSynchronizing = registryChainGuardianCount != nonRegistryChainGuardianCount;

            if (registryChainGuardianCount > 1)
            {
                return new TokenBalanceTransferCheckAsyncResultDto
                {
                    IsTransferSafe = nonRegistryChainGuardianCount > 1, IsSynchronizing = isSynchronizing,
                    AccelerateGuardians = accelerateGuardians
                };
            }

            var assert = await GetUserAssetsAsync(input.CaHash);
            _logger.LogDebug("CaHash: {caHash} have {COUNT} token assert.", input.CaHash,
                assert.CaHolderSearchTokenNFT.TotalRecordCount);

            foreach (var token in assert.CaHolderSearchTokenNFT.Data)
            {
                // when token is NFT, TokenInfo == null
                if (token.TokenInfo == null) continue;
                if (_securityOptions.TokenBalanceTransferThreshold.TryGetValue(token.TokenInfo.Symbol, out var t) &&
                    token.Balance > t)
                    return new TokenBalanceTransferCheckAsyncResultDto
                    {
                        IsTransferSafe = false, IsOriginChainSafe = false, AccelerateGuardians = accelerateGuardians
                    };
            }

            return new TokenBalanceTransferCheckAsyncResultDto
                { IsSynchronizing = isSynchronizing, AccelerateGuardians = accelerateGuardians };
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "An exception occurred during GetManagerApprovedListByCaHashAsync, caHash: {caHash}", input.CaHash);
            return new TokenBalanceTransferCheckAsyncResultDto
                { IsTransferSafe = false, IsSynchronizing = false, IsOriginChainSafe = false };
        }
    }

    public async Task<TokenBalanceTransferCheckAsyncResultDto> GetTokenBalanceTransferCheckAsync(
        GetTokenBalanceTransferCheckWithChainIdDto input)
    {
        try
        {
            CheckTransferSafeChainId(input.CheckTransferSafeChainId);
            var registryChainGuardianSet = new HashSet<string>();
            var nonRegistryChainGuardianSet = new HashSet<string>();
            var holderInfoOutputs = new List<GetHolderInfoOutput>();
            foreach (var chainInfo in _chainOptions.ChainInfos)
            {
                GetHolderInfoOutput info;
                try
                {
                    info = await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(input.CaHash), null,
                        chainInfo.Value.ChainId);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        "get holder info error in GetTokenBalanceTransferCheck, caHash: {caHash}", input.CaHash);
                    continue;
                }

                if (chainInfo.Value.ChainId == ChainHelper.ConvertChainIdToBase58(info.CreateChainId) ||
                    input.CheckTransferSafeChainId.IsNullOrWhiteSpace() ||
                    chainInfo.Value.ChainId == input.CheckTransferSafeChainId)
                {
                    holderInfoOutputs.Add(info);
                }

                if (!(info?.GuardianList?.Guardians?.Count > 0))
                {
                    continue;
                }

                var registryFlag = chainInfo.Value.ChainId == ChainHelper.ConvertChainIdToBase58(info.CreateChainId);

                foreach (var g in info.GuardianList.Guardians)
                {
                    var guardianName = g.VerifierId + g.IdentifierHash.ToHex();
                    if (registryFlag) registryChainGuardianSet.AddIfNotContains(guardianName);
                    else nonRegistryChainGuardianSet.AddIfNotContains(guardianName);
                }
            }

            var registryChainGuardianCount = registryChainGuardianSet.Count();
            var nonRegistryChainGuardianCount = nonRegistryChainGuardianSet.Count();

            var holderInfoOutput = holderInfoOutputs.FirstOrDefault(t => t.CreateChainId > 0);
            var originalChainId = await GetOriginalChainIdAsync(holderInfoOutput?.CreateChainId ?? 0, input.CaHash);
            if (input.CheckTransferSafeChainId == originalChainId)
            {
                return await GetOriginChainCheckAsync(registryChainGuardianCount, input.CaHash);
            }

            var accelerateGuardians = await GetAccelerateGuardiansAsync(input.CaHash, holderInfoOutputs);
            _logger.LogDebug("CaHash: {caHash} have {COUNT} registry count {non-registry COUNT} non-registry count.",
                input.CaHash, registryChainGuardianCount, nonRegistryChainGuardianCount);
            var isSynchronizing = registryChainGuardianCount != nonRegistryChainGuardianCount;

            if (registryChainGuardianCount > 1)
            {
                return new TokenBalanceTransferCheckAsyncResultDto
                {
                    IsTransferSafe = nonRegistryChainGuardianCount > 1, IsSynchronizing = isSynchronizing,
                    AccelerateGuardians = accelerateGuardians
                };
            }

            var assert = await GetUserAssetsAsync(input.CaHash);
            _logger.LogDebug("CaHash: {caHash} have {COUNT} token assert.", input.CaHash,
                assert.CaHolderSearchTokenNFT.TotalRecordCount);

            foreach (var token in assert.CaHolderSearchTokenNFT.Data)
            {
                // when token is NFT, TokenInfo == null
                if (token.TokenInfo == null) continue;
                if (_securityOptions.TokenBalanceTransferThreshold.TryGetValue(token.TokenInfo.Symbol, out var t) &&
                    token.Balance >= t)
                {
                    return new TokenBalanceTransferCheckAsyncResultDto
                    {
                        IsTransferSafe = false, IsOriginChainSafe = false, AccelerateGuardians = accelerateGuardians
                    };
                }
            }

            return new TokenBalanceTransferCheckAsyncResultDto
                { IsSynchronizing = isSynchronizing, AccelerateGuardians = accelerateGuardians };
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "An exception occurred during GetManagerApprovedListByCaHashAsync, caHash: {caHash}", input.CaHash);
            return new TokenBalanceTransferCheckAsyncResultDto
                { IsTransferSafe = false, IsSynchronizing = false, IsOriginChainSafe = false };
        }
    }

    private async Task<TokenBalanceTransferCheckAsyncResultDto> GetOriginChainCheckAsync(int registryChainGuardianCount,
        string caHash)
    {
        if (registryChainGuardianCount > 1)
        {
            return new TokenBalanceTransferCheckAsyncResultDto();
        }

        var assert = await GetUserAssetsAsync(caHash);
        foreach (var token in assert.CaHolderSearchTokenNFT.Data)
        {
            // when token is NFT, TokenInfo == null
            if (token.TokenInfo == null) continue;
            if (_securityOptions.TokenBalanceTransferThreshold.TryGetValue(token.TokenInfo.Symbol, out var t) &&
                token.Balance >= t)
                return new TokenBalanceTransferCheckAsyncResultDto
                {
                    IsTransferSafe = false, IsOriginChainSafe = false
                };
        }

        return new TokenBalanceTransferCheckAsyncResultDto();
    }

    private void CheckTransferSafeChainId(string transferSafeChainId)
    {
        if (!transferSafeChainId.IsNullOrWhiteSpace() &&
            !_chainOptions.ChainInfos.ContainsKey(transferSafeChainId))
        {
            throw new UserFriendlyException("invalid CheckTransferSafeChainId");
        }
    }

    private async Task<string> GetOriginalChainIdAsync(int createChainId, string caHash)
    {
        if (createChainId > 0)
        {
            return ChainHelper.ConvertChainIdToBase58(createChainId);
        }

        var holderInfos = await _userSecurityProvider.GetCaHolderInfoAsync(caHash);
        var holderInfo = holderInfos?.CaHolderInfo?.FirstOrDefault(t => !t.OriginChainId.IsNullOrEmpty());
        return holderInfo?.OriginChainId;
    }

    private async Task<List<GuardianIndexerInfoDto>> GetAccelerateGuardiansAsync(string caHash,
        List<GetHolderInfoOutput> holderInfoOutputs)
    {
        try
        {
            var guardianInfos = new List<GuardianIndexerInfoDto>();
            var guardianBasicInfos = new List<Portkey.Contracts.CA.Guardian>();
            var outputs = holderInfoOutputs?.Where(t => t is { GuardianList: { Guardians.Count: > 0 } }).ToList();
            if (outputs is { Count: 1 })
            {
                guardianBasicInfos = outputs
                    .SelectMany(t => t?.GuardianList?.Guardians)
                    .ToList();
            }
            else
            {
                var guardiansFirst = holderInfoOutputs[0].GuardianList.Guardians;
                var guardiansSecond = holderInfoOutputs[1].GuardianList.Guardians;

                guardianBasicInfos.AddRange(guardiansFirst.Where(t =>
                {
                    var info = guardiansSecond.FirstOrDefault(f =>
                        f.IdentifierHash == t.IdentifierHash && f.VerifierId == t.VerifierId);
                    return info == null;
                }).ToList());

                guardianBasicInfos.AddRange(guardiansSecond.Where(t =>
                {
                    var info = guardiansFirst.FirstOrDefault(f =>
                        f.IdentifierHash == t.IdentifierHash && f.VerifierId == t.VerifierId);
                    return info == null;
                }));
            }

            if (guardianBasicInfos.IsNullOrEmpty())
            {
                return guardianInfos;
            }

            var guardians = ObjectMapper.Map<List<Portkey.Contracts.CA.Guardian>, List<GuardianIndexerInfoDto>>(
                guardianBasicInfos);

            var holderInfo = await _userSecurityProvider.GetCaHolderInfoAsync(caHash);
            var guardiansAll = new List<GuardianIndexerInfoDto>();
            foreach (var holder in holderInfo.CaHolderInfo)
            {
                if (holder?.GuardianList?.Guardians == null || holder.GuardianList.Guardians.Count == 0)
                {
                    continue;
                }

                var guardianIndexers = ObjectMapper.Map<List<GuardianInfoBase>, List<GuardianIndexerInfoDto>>(
                    holder.GuardianList.Guardians);

                guardianIndexers.ForEach(t => t.ChainId = holder.ChainId);
                guardiansAll.AddRange(guardianIndexers);
            }

            foreach (var guardian in guardians)
            {
                var guardianInfo = guardiansAll?.FirstOrDefault(
                    f =>
                        f.IdentifierHash == guardian.IdentifierHash && f.VerifierId == guardian.VerifierId &&
                        !f.TransactionId.IsNullOrEmpty());

                if (guardianInfo == null)
                {
                    continue;
                }

                guardian.ChainId = guardianInfo.ChainId;
                guardian.TransactionId = guardianInfo.TransactionId;
            }

            return guardians;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetAccelerateGuardians error, caHash:{caHash}", caHash);
            return new List<GuardianIndexerInfoDto>();
        }
    }

    private async Task<bool> AddUserTransferLimitHistoryAsync(string caHash, IndexerSearchTokenNft token)
    {
        var history =
            await _userSecurityProvider.GetUserTransferLimitHistoryAsync(caHash, token.ChainId, token.TokenInfo.Symbol);
        if (history == null || string.IsNullOrEmpty(history.Symbol) || history.Symbol != token.TokenInfo.Symbol ||
            history.ChainId != token.ChainId)
        {
            if (token.Balance <= 0) return false;
            await _distributedEventBus.PublishAsync(new UserTransferLimitHistoryEto
            {
                Id = GuidGenerator.Create(), CaHash = caHash, ChainId = token.ChainId, Symbol = token.TokenInfo.Symbol
            });
        }

        return true;
    }

    private async Task<IndexerSearchTokenNfts> GetUserAssetsAsync(string caHash)
    {
        var caAddrs = new List<CAAddressInfo>();
        foreach (var chainInfo in _chainOptions.ChainInfos)
        {
            try
            {
                var output = await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(caHash), null,
                    chainInfo.Value.ChainId);
                caAddrs.Add(new CAAddressInfo
                {
                    ChainId = chainInfo.Key,
                    CaAddress = output.CaAddress.ToBase58()
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "get holder info error in GetUserAssetsAsync, caHash: {caHash}", caHash);
            }
        }

        // Obtain the balance of all token assets by caHash
        return await _assetsProvider.SearchUserAssetsAsync(caAddrs, "", 0, 200);
    }

    private async Task<TransferLimitDto> GeneratorTransferLimitAsync(IndexerSearchTokenNft token)
    {
        var transferLimit = new TransferLimitDto
        {
            ChainId = token.ChainId,
            Symbol = token.TokenInfo.Symbol,
            Decimals = token.TokenInfo.Decimals,
            Restricted = true
        };

        transferLimit.ImageUrl =
            _assetsLibraryProvider.buildSymbolImageUrl(token.TokenInfo.Symbol, token.TokenInfo.ImageUrl);

        if (_securityOptions.TokenTransferLimitDict[token.ChainId].SingleTransferLimit
            .TryGetValue(token.TokenInfo.Symbol, out var singleLimit))
        {
            transferLimit.SingleLimit = singleLimit.ToString();
            transferLimit.DefaultSingleLimit = transferLimit.SingleLimit;
        }
        else
        {
            transferLimit.SingleLimit = _securityOptions.DefaultTokenTransferLimit.ToString();
        }

        if (_securityOptions.TokenTransferLimitDict[token.ChainId].DailyTransferLimit
            .TryGetValue(token.TokenInfo.Symbol, out var dailyLimit))
        {
            transferLimit.DailyLimit = dailyLimit.ToString();
            transferLimit.DefaultDailyLimit = transferLimit.DailyLimit;
        }
        else
        {
            transferLimit.DailyLimit = _securityOptions.DefaultTokenTransferLimit.ToString();
        }

        return transferLimit;
    }

    class GuardianBasicInfo
    {
        public string IdentifierHash { get; set; }
        public string VerifierId { get; set; }
    }
}