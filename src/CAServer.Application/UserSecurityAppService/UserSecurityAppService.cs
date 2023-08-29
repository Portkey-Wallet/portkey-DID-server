using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Common;
using CAServer.Options;
using CAServer.Security;
using CAServer.Security.Dtos;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using CAServer.UserSecurityAppService.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace CAServer.UserSecurityAppService;

public class UserSecurityAppService : CAServerAppService, IUserSecurityAppService
{
    private readonly ILogger<UserSecurityAppService> _logger;
    private readonly SecurityOptions _securityOptions;
    private readonly IContractProvider _contractProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IUserAssetsProvider _assetsProvider;
    private readonly IUserSecurityProvider _userSecurityProvider;

    public UserSecurityAppService(IOptions<SecurityOptions> securityOptions, IUserSecurityProvider userSecurityProvider,
        IOptions<ChainOptions> chainOptions, IContractProvider contractProvider,
        ILogger<UserSecurityAppService> logger, IUserAssetsProvider assetsProvider)
    {
        _logger = logger;
        _assetsProvider = assetsProvider;
        _chainOptions = chainOptions.Value;
        _securityOptions = securityOptions.Value;
        _contractProvider = contractProvider;
        _userSecurityProvider = userSecurityProvider;
    }

    public async Task<TransferLimitListResultDto> GetTransferLimitListByCaHashAsync(
        GetTransferLimitListByCaHashAsyncDto input)
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


            // Use the default token transferLimit without updating the transferLimit
            var dic = new Dictionary<string, TransferLimitDto>();
            foreach (var token in assert.CaHolderSearchTokenNFT.Data)
            {
                dic[token.ChainId + "-" + token.TokenInfo.Symbol] = new TransferLimitDto()
                {
                    ChainId = token.ChainId,
                    Symbol = token.TokenInfo.Symbol,
                    DailyLimit = _securityOptions.DefaultTokenTransferLimit,
                    SingleLimit = _securityOptions.DefaultTokenTransferLimit
                };
            }

            // If the transferLimit is updated, the token transferLimit will be overwritten
            var res = await _userSecurityProvider.GetTransferLimitListByCaHash(input.CaHash);
            foreach (var transferLimit in res.CaHolderTransferLimit.Data)
            {
                var tempKey = transferLimit.ChainId + "-" + transferLimit.Symbol;
                if (dic[tempKey] != null)
                {
                    dic[tempKey].DailyLimit = transferLimit.DailyLimit;
                    dic[tempKey].SingleLimit = transferLimit.SingleLimit;
                }
            }

            return new TransferLimitListResultDto()
            {
                TotalRecordCount = dic.Count,
                Data = dic.Values.ToList()
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An exception occurred during GetTransferLimitListByCaHashAsync");
            throw new UserFriendlyException("An exception occurred during GetTransferLimitListByCaHashAsync");
        }
    }
}