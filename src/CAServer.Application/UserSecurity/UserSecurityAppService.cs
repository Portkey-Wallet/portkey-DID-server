using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.Common;
using CAServer.Options;
using CAServer.Security;
using CAServer.Security.Dtos;
using CAServer.Security.Etos;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using CAServer.UserSecurity.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.UserSecurity;

public class UserSecurityAppService : CAServerAppService, IUserSecurityAppService
{
    private readonly ILogger<UserSecurityAppService> _logger;
    private readonly SecurityOptions _securityOptions;
    private readonly IContractProvider _contractProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IUserAssetsProvider _assetsProvider;
    private readonly IUserSecurityProvider _userSecurityProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private const string _defaultSymbol = "ELF";

    public UserSecurityAppService(IOptionsSnapshot<SecurityOptions> securityOptions,
        IUserSecurityProvider userSecurityProvider, IOptionsSnapshot<ChainOptions> chainOptions,
        IContractProvider contractProvider, ILogger<UserSecurityAppService> logger, IUserAssetsProvider assetsProvider,
        IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _assetsProvider = assetsProvider;
        _distributedEventBus = distributedEventBus;
        _chainOptions = chainOptions.Value;
        _securityOptions = securityOptions.Value;
        _contractProvider = contractProvider;
        _userSecurityProvider = userSecurityProvider;
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
            var res = await _userSecurityProvider.GetTransferLimitListByCaHash(input.CaHash);
            _logger.LogDebug("CaHash: {caHash} have {COUNT} transfer limit change history.", input.CaHash,
                res.CaHolderTransferLimit.TotalRecordCount);

            foreach (var transferLimit in res.CaHolderTransferLimit.Data)
            {
                var tempKey = transferLimit.ChainId + "-" + transferLimit.Symbol;
                if (dic.TryGetValue(tempKey, out _))
                {
                    dic[tempKey].DailyLimit = transferLimit.DailyLimit;
                    dic[tempKey].SingleLimit = transferLimit.SingleLimit;
                    dic[tempKey].Restricted = !(transferLimit.DailyLimit == "-1" && transferLimit.SingleLimit == "-1");
                }
            }

            _logger.LogDebug("CaHash: {caHash} have {COUNT} transfer limit list.", input.CaHash, dic.Count);

            return new TransferLimitListResultDto
            {
                TotalRecordCount = dic.Count,
                Data = dic.Values.ToList().OrderBy(t => t.Symbol != _defaultSymbol).ThenBy(t => t.Symbol)
                    .ThenBy(t => t.ChainId).ToList()
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
            var res = await _userSecurityProvider.GetManagerApprovedListByCaHash(input.CaHash, input.Spender,
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
            var guardianSet = new HashSet<string>();
            foreach (var chainInfo in _chainOptions.ChainInfos)
            {
                var info = await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(input.CaHash), null,
                    chainInfo.Value.ChainId);
                if (!(info?.GuardianList?.Guardians?.Count > 0))
                {
                    continue;
                }

                foreach (var guardian in info.GuardianList.Guardians)
                {
                    guardianSet.AddIfNotContains(guardian.VerifierId + guardian.IdentifierHash.ToHex());
                }
            }

            _logger.LogDebug("CaHash: {caHash} have {COUNT} guardianCount.", input.CaHash, guardianSet.Count);
            if (guardianSet.Count > 1) return new TokenBalanceTransferCheckAsyncResultDto();

            var assert = await GetUserAssetsAsync(input.CaHash);
            _logger.LogDebug("CaHash: {caHash} have {COUNT} token assert.", input.CaHash,
                assert.CaHolderSearchTokenNFT.TotalRecordCount);

            foreach (var token in assert.CaHolderSearchTokenNFT.Data)
            {
                if (token.TokenInfo == null) continue;
                if (_securityOptions.TokenBalanceTransferThreshold.TryGetValue(token.TokenInfo.Symbol, out var t) &&
                    token.Balance > t) return new TokenBalanceTransferCheckAsyncResultDto { IsSafe = false };
            }

            return new TokenBalanceTransferCheckAsyncResultDto();
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "An exception occurred during GetManagerApprovedListByCaHashAsync, caHash: {caHash}", input.CaHash);
            throw new UserFriendlyException("An exception occurred during GetManagerApprovedListByCaHashAsync");
        }
    }

    private async Task<bool> AddUserTransferLimitHistoryAsync(string caHash, IndexerSearchTokenNft token)
    {
        var history =
            await _userSecurityProvider.GetUserTransferLimitHistory(caHash, token.ChainId, token.TokenInfo.Symbol);
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
            var output = await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(caHash), null,
                chainInfo.Value.ChainId);
            caAddrs.Add(new CAAddressInfo
            {
                ChainId = chainInfo.Key,
                CaAddress = output.CaAddress.ToBase58()
            });
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

        if (_securityOptions.TokenTransferLimitDict[token.ChainId].SingleTransferLimit
            .TryGetValue(token.TokenInfo.Symbol, out var singleLimit))
        {
            transferLimit.SingleLimit = (singleLimit * Math.Pow(10, token.TokenInfo.Decimals)).ToString();
            transferLimit.DefaultSingleLimit = transferLimit.SingleLimit;
        }
        else
        {
            transferLimit.SingleLimit =
                (_securityOptions.DefaultTokenTransferLimit * Math.Pow(10, token.TokenInfo.Decimals)).ToString();
        }

        if (_securityOptions.TokenTransferLimitDict[token.ChainId].DailyTransferLimit
            .TryGetValue(token.TokenInfo.Symbol, out var dailyLimit))
        {
            transferLimit.DailyLimit = (dailyLimit * Math.Pow(10, token.TokenInfo.Decimals)).ToString();
            transferLimit.DefaultDailyLimit = transferLimit.DailyLimit;
        }
        else
        {
            transferLimit.DailyLimit =
                (_securityOptions.DefaultTokenTransferLimit * Math.Pow(10, token.TokenInfo.Decimals)).ToString();
        }

        return transferLimit;
    }
}