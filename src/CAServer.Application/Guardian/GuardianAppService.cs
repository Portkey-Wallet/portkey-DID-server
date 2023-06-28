using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.AppleAuth.Provider;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.Guardian;
using CAServer.Guardian.Provider;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Guardian;

[RemoteService(false)]
[DisableAuditing]
public class GuardianAppService : CAServerAppService, IGuardianAppService
{
    private readonly IGuardianProvider _guardianProvider;
    private readonly IUserAssetsProvider _userAssetsProvider;

    public GuardianAppService(IGuardianProvider guardianProvider, 
        IUserAssetsProvider userAssetsProvider)
    {
        _guardianProvider = guardianProvider;
        _userAssetsProvider = userAssetsProvider;
    }

    public async Task<GuardianResultDto> GetGuardianIdentifiersAsync(GuardianIdentifierDto guardianIdentifierDto)
    {
        throw new Exception();
    }

    public async Task<RegisterInfoResultDto> GetRegisterInfoAsync(RegisterInfoDto requestDto)
    {
        return new RegisterInfoResultDto { OriginChainId = "" };
    }

    public async Task<List<SearchResponseDto>> SearchAsync()
    {
        var result = new List<SearchResponseDto>();
        var guardians = await _guardianProvider.GetGuardiansAsync(string.Empty, string.Empty);

        var users = guardians?.CaHolderInfo?.Where(t => t.GuardianList?.Guardians?.Count == 1);
        var dic = new Dictionary<string, string>();

        foreach (var per in users)
        {
            if (!dic.ContainsKey(per.CaAddress))
            {
                dic.Add(per.CaAddress, per.CaHash);
            }
        }

        var caAddressInfos = dic.Keys.Select(address => new CAAddressInfo { CaAddress = address })
            .ToList();

        var res = await _userAssetsProvider.GetUserTokenInfoAsync(caAddressInfos, "",
            0, 1000000);

        var data =
            res.CaHolderTokenBalanceInfo.Data.Where(t => t.Balance > 0).ToList();

        foreach (var d in data)
        {
            result.Add(new SearchResponseDto
            {
                Balance = d.Balance,
                CaAddress = d.CaAddress,
                CaHash = dic[d.CaAddress]
            });
        }

        return result;
    }
}