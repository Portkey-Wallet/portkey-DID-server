using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Guardian.Provider;
using GraphQL;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.GuardiansStatistic;

[RemoteService(false), DisableAuditing]
public class GuardiansStatisticAppService : CAServerAppService, IGuardiansStatisticAppService
{
    private readonly IGraphQLHelper _graphQlHelper;
    public List<string> HasLoginPhoneGuardian = new List<string>();
    public List<string> HasPhoneGuardian = new List<string>();

    public GuardiansStatisticAppService(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<string> GetInfo()
    {
        await GetUserInfo();

        Console.WriteLine($"HasPhoneGuardian count:{HasPhoneGuardian.Distinct().Count()}");
        Console.WriteLine($"HasLoginPhoneGuardian count:{HasLoginPhoneGuardian.Distinct().Count()}");
        return "ok";
    }

    private async Task GetUserInfo()
    {
        var onceCount = 10000;
        var count = 0;
        for (var i = 0; i < 100000; i = i + onceCount)
        {
            var holders = await GetHolderInfoAsync(string.Empty, string.Empty, new List<string>(), i, onceCount);
            holders.CaHolderInfo = holders?.CaHolderInfo
                ?.Where(t => t.GuardianList != null && !t.GuardianList.Guardians.IsNullOrEmpty()).ToList();
            if (holders == null || holders.CaHolderInfo.IsNullOrEmpty())
            {
                break;
            }

            foreach (var holder in holders.CaHolderInfo)
            {
                count++;
                var guardianInfo = holder.GuardianList.Guardians.FirstOrDefault(t =>
                    t.Type == ((int)GuardianIdentifierType.Phone).ToString());

                if (guardianInfo == null) continue;

                HasPhoneGuardian.Add(guardianInfo.IdentifierHash);

                var guardianInfo2 = holder.GuardianList.Guardians.FirstOrDefault(t =>
                    t.Type == ((int)GuardianIdentifierType.Phone).ToString() && t.IsLoginGuardian);

                if (guardianInfo2 == null) continue;

                HasLoginPhoneGuardian.Add(guardianInfo.IdentifierHash);
            }
        }

        Console.WriteLine($"holder count:{count}");
    }


    private async Task<GuardiansDto> GetHolderInfoAsync(string chainId, string caHash, List<string> caAddresses,
        int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
            query($chainId:String,$caHash:String,$caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
            caHolderInfo(dto: {chainId:$chainId,caHash:$caHash,caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
            id,chainId,caHash,caAddress,originChainId,guardianList{guardians{isLoginGuardian,identifierHash,type}}}
        }",
            Variables = new
            {
                chainId, caHash, caAddresses, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount
            }
        });
    }
}