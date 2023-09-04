using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Common;
using CAServer.Options;
using CAServer.Security;
using CAServer.Security.Dtos;
using CAServer.Security.Etos;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using CAServer.UserSecurityAppService.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.UserSecurityAppService;

public class UserSecurityAppService : CAServerAppService, IUserSecurityAppService
{
    private readonly ILogger<UserSecurityAppService> _logger;
    private readonly SecurityOptions _securityOptions;
    private readonly IContractProvider _contractProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IUserAssetsProvider _assetsProvider;
    private readonly IUserSecurityProvider _userSecurityProvider;
    private readonly IDistributedEventBus _distributedEventBus;


    public UserSecurityAppService(IOptions<SecurityOptions> securityOptions, IUserSecurityProvider userSecurityProvider,
        IOptions<ChainOptions> chainOptions, IContractProvider contractProvider, ILogger<UserSecurityAppService> logger,
        IUserAssetsProvider assetsProvider, IDistributedEventBus distributedEventBus)
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
            var caAddrs = new List<CAAddressInfo>();
            foreach (var chainInfo in _chainOptions.ChainInfos)
            {
                var output = await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(input.CaHash), null,
                    chainInfo.Value.ChainId);
                caAddrs.Add(new CAAddressInfo
                {
                    ChainId = chainInfo.Key,
                    CaAddress = output.CaAddress.ToBase58()
                });
            }

            // Obtain the balance of all token assets by caHash
            var assert =
                await _assetsProvider.SearchUserAssetsAsync(caAddrs, "", input.SkipCount, input.MaxResultCount);
            if (assert.CaHolderSearchTokenNFT.TotalRecordCount == 0)
            {
                _logger.LogDebug("CaHash: {caHash} don't have token assert.", input.CaHash);
                return new TransferLimitListResultDto() { Data = new List<TransferLimitDto>() };
            }

            _logger.LogDebug("CaHash: {caHash} have {COUNT} token assert.", input.CaHash,
                assert.CaHolderSearchTokenNFT.TotalRecordCount);

            // Use the default token transferLimit without updating the transferLimit
            var dic = new Dictionary<string, TransferLimitDto>();
            foreach (var token in assert.CaHolderSearchTokenNFT.Data)
            {
                if (!await AddOrUpdateUserTransferLimitHistory(input.CaHash, token)) continue;

                var defaultTokenTransferLimit = _securityOptions.DefaultTokenTransferLimit;
                if (_securityOptions.TransferLimit.TryGetValue(token.TokenInfo.Symbol, out var limit))
                {
                    defaultTokenTransferLimit = limit;
                }

                dic[token.ChainId + "-" + token.TokenInfo.Symbol] = new TransferLimitDto()
                {
                    ChainId = token.ChainId,
                    Symbol = token.TokenInfo.Symbol,
                    DailyLimit = defaultTokenTransferLimit,
                    SingleLimit = defaultTokenTransferLimit
                };
            }

            // If the transferLimit is updated, the token transferLimit will be overwritten
            // var res = await _userSecurityProvider.GetTransferLimitListByCaHash(input.CaHash);
            var res = new IndexerTransferLimitList()
            {
                CaHolderTransferLimit = new CaHolderTransferLimit()
                {
                    TotalRecordCount = 0,
                    Data = new List<TransferLimitDto>()
                }
            };
            _logger.LogDebug("CaHash: {caHash} have {COUNT} transfer limit change history.", input.CaHash,
                res.CaHolderTransferLimit.TotalRecordCount);

            foreach (var transferLimit in res.CaHolderTransferLimit.Data)
            {
                var tempKey = transferLimit.ChainId + "-" + transferLimit.Symbol;
                if (dic[tempKey] != null)
                {
                    dic[tempKey].DailyLimit = transferLimit.DailyLimit;
                    dic[tempKey].SingleLimit = transferLimit.SingleLimit;
                    dic[tempKey].Restricted = !(transferLimit.DailyLimit == -1 && transferLimit.SingleLimit == -1);
                }
            }

            _logger.LogDebug("CaHash: {caHash} have {COUNT} transfer limit list.", input.CaHash, dic.Count);

            return new TransferLimitListResultDto()
            {
                TotalRecordCount = dic.Count,
                Data = dic.Values.ToList()
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An exception occurred during GetTransferLimitListByCaHashAsync, caHash: {caHash}",
                input.CaHash);
            throw new UserFriendlyException(
                $"An exception occurred during GetTransferLimitListByCaHashAsync,caHash: {input.CaHash}");
        }
    }

    public async Task<ManagerApprovedListResultDto> GetManagerApprovedListByCaHashAsync(
        GetManagerApprovedListByCaHashDto input)
    {
        try
        {
            var res = await _userSecurityProvider.GetManagerApprovedListByCaHash(input.CaHash, input.Spender,
                input.Symbol, input.SkipCount, input.MaxResultCount);
            return new ManagerApprovedListResultDto()
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
                $"An exception occurred during GetManagerApprovedListByCaHashAsync, chainId: {input.ChainId} caHash: {input.CaHash}");
        }
    }

    public async Task<TokenBalanceTransferThresholdResultDto> GetTokenBalanceTransferThresholdAsync()
    {
        return new TokenBalanceTransferThresholdResultDto()
        {
            TotalRecordCount = _securityOptions.TokenBalanceTransferThreshold.Count,
            Data = _securityOptions.TokenBalanceTransferThreshold
        };
    }

    private async Task<bool> AddOrUpdateUserTransferLimitHistory(string caHash, IndexerSearchTokenNft token)
    {
        var history =
            await _userSecurityProvider.GetUserTransferLimitHistory(caHash, token.ChainId, token.TokenInfo.Symbol);
        if (string.IsNullOrEmpty(history.Symbol) || history.Symbol != token.TokenInfo.Symbol ||
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
}