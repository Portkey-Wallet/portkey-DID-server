using System;
using System.Collections.Generic;
using System.IO;
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

    public async Task<SearchResponsePageDto> SearchAsync()
    {
        var list = new List<SearchResponseDto>();
        var strList = new List<string>();
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

        for (int i = 0; i < 100000; i += 1000)
        {
            var caAddressInfos = dic.Keys.Select(address => new CAAddressInfo { CaAddress = address }).Skip(i)
                .Take(1000).ToList();
            if (caAddressInfos.Count == 0)
            {
                break;
            }

            var res = await _userAssetsProvider.GetUserTokenInfoAsync(caAddressInfos, "",
                0, 1000000);

            var data =
                res.CaHolderTokenBalanceInfo.Data.Where(t => t.Balance > 0).ToList();

            foreach (var d in data)
            {
                list.Add(new SearchResponseDto
                {
                    Balance = d.Balance,
                    CaAddress = d.CaAddress,
                    CaHash = dic[d.CaAddress]
                });
            }

            list = list.OrderByDescending(t => t.Balance).ToList();
            

        }

        foreach (var d in list)
        {
            strList.Add($"{dic[d.CaAddress]}\t{d.CaAddress}\t{d.Balance}");
        }
        await File.WriteAllLinesAsync("WriteLines2.txt", strList);
        
        return new SearchResponsePageDto()
        {
            Data = list,
            TotalCount = list.Count
        };
    }
}