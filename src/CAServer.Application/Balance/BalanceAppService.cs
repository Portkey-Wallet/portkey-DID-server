using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Guardian.Provider;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using GraphQL;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Balance;

[RemoteService(false)]
[DisableAuditing]
public class BalanceAppService : CAServerAppService, IBalanceAppService
{
    private readonly IGraphQLHelper _graphQlHelper;

    public BalanceAppService(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task GetBalanceInfoAsync(string chainId)
    {
        var caHashListMain = new List<string>();
        var caAddressListMain = new List<string>();
        var caHashListSide = new List<string>();
        var caHashListBoth = new List<string>();

        var tokenInfoMain = await GetUserTokenInfoAsync("AELF", "ELF", 0, 10000);
        var mainList = tokenInfoMain.CaHolderTokenBalanceInfo.Data;
        var mainTotalCount = tokenInfoMain.CaHolderTokenBalanceInfo.totalRecordCount;

        var tokenInfoSide = await GetUserTokenInfoAsync("tDVV", "ELF", 0, 10000);
        var sideList = tokenInfoSide.CaHolderTokenBalanceInfo.Data;
        var sideTotalCount = tokenInfoSide.CaHolderTokenBalanceInfo.totalRecordCount;


        foreach (var data in mainList)
        {
            if (data.Balance <= 0) continue;

            caAddressListMain.Add(data.CaAddress);
        }

        foreach (var data in sideList)
        {
            if (data.Balance <= 0) continue;

            caHashListSide.Add(data.CaAddress);
        }

        Logger.LogInformation("main chain total count: {count}", caAddressListMain.Distinct().ToList().Count);
        Logger.LogInformation("side chain total count: {count}", caHashListSide.Distinct().ToList().Count);

        var onceCount = 500;
        for (var i = 0; i < mainTotalCount; i = i + onceCount)
        {
            var list = caAddressListMain.Skip(i).Take(onceCount).ToList();
            var mainHolders = await GetHolderInfoAsync("AELF", string.Empty, list, 0, onceCount);
            foreach (var holder in mainHolders.CaHolderInfo)
            {
                caHashListMain.Add(holder.CaHash);
            }
        }

        caHashListMain = caHashListMain.Distinct().ToList();

        var sideHolders = await GetHolderInfoAsync("tDVV", string.Empty, caHashListSide, 0, 10000);
        caHashListSide.Clear();


        foreach (var holder in sideHolders.CaHolderInfo)
        {
            caHashListSide.Add(holder.CaHash);
        }

        Logger.LogInformation("main chain caHash count: {count}", caHashListMain.Distinct().ToList().Count);
        Logger.LogInformation("side chain caHash count: {count}", caHashListSide.Distinct().ToList().Count);
        
        caHashListBoth = caHashListMain.Intersect(caHashListSide).ToList();
        caHashListMain.RemoveAll(caHashListBoth);
        caHashListSide.RemoveAll(caHashListBoth);
        
        Logger.LogInformation("both chain count: {count}", caHashListBoth.Distinct().ToList().Count);
        Logger.LogInformation("main chain count: {count}", caHashListMain.Distinct().ToList().Count);
        Logger.LogInformation("side chain count: {count}", caHashListSide.Distinct().ToList().Count);
        
         Logger.LogInformation("======both=====");
         foreach (var hash in caHashListBoth.Distinct().ToList())
         {
             Console.WriteLine(hash);
         }
        
        Logger.LogInformation("======main=====");
         foreach (var hash in caHashListMain.Distinct().ToList())
         {
             Console.WriteLine(hash);
         }
        
         Logger.LogInformation("======side=====");
         foreach (var hash in caHashListSide.Distinct().ToList())
         {
             Console.WriteLine(hash);
         }
    }

    private async Task<IndexerTokenInfos> GetUserTokenInfoAsync(string chainId, string symbol,
        int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerTokenInfos>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$symbol:String,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTokenBalanceInfo(dto: {chainId:$chainId,symbol:$symbol,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{chainId,balance,caAddress,tokenIds,tokenInfo{symbol,tokenContractAddress,decimals,tokenName,totalSupply}},totalRecordCount}
                }",
            Variables = new
            {
                chainId = chainId, symbol, skipCount = inputSkipCount,
                maxResultCount = inputMaxResultCount
            }
        });
    }

    private async Task<GuardiansDto> GetHolderInfoAsync(string chainId, string caHash, List<string> caAddresses,
        int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<GuardiansDto>(new GraphQLRequest
        {
            Query = @"
            query($chainId:String,$caHash:String,$caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
            caHolderInfo(dto: {chainId:$chainId,caHash:$caHash,caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
            id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}}
        }",
            Variables = new
            {
                chainId, caHash, caAddresses, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount
            }
        });
    }
}