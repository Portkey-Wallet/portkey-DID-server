using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAServer.CAActivity;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Guardian.Provider;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using GraphQL;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Linq;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Balance;

[RemoteService(false)]
[DisableAuditing]
public class BalanceAppService : CAServerAppService, IBalanceAppService
{
    private readonly IGraphQLHelper _graphQlHelper;

    private static readonly List<string> AllSupportTypes = new()
    {
        "Transfer", "CrossChainTransfer", "CrossChainReceiveToken", "SocialRecovery", "RemoveManagerInfo",
        "AddManagerInfo", "CreateCAHolder", "AddGuardian", "RemoveGuardian", "UpdateGuardian", "SetGuardianForLogin",
        "UnsetGuardianForLogin", "RemoveOtherManagerInfo", "Register", "BeanGoTown-Bingo", "BeanGoTown-Play"
    };

    private long _start = 1694448000;
    private long _end = 1695657600;

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
        var balanceAll = new List<BalanceInfo>();
        var balanceBoth = new List<BalanceInfo>();
        var balanceMain = new List<BalanceInfo>();
        var balanceSide = new List<BalanceInfo>();

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
            balanceAll.Add(new BalanceInfo()
            {
                ChainId = data.ChainId,
                CaAddress = data.CaAddress,
                Balance = data.Balance
            });
        }

        foreach (var data in sideList)
        {
            if (data.Balance <= 0) continue;

            caHashListSide.Add(data.CaAddress);
            balanceAll.Add(new BalanceInfo()
            {
                ChainId = data.ChainId,
                CaAddress = data.CaAddress,
                Balance = data.Balance
            });
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
                var bal = balanceAll.FirstOrDefault(t => t.CaAddress == holder.CaAddress);
                if (bal != null)
                {
                    bal.CaHash = holder.CaHash;
                }
            }
        }

        caHashListMain = caHashListMain.Distinct().ToList();

        var sideHolders = await GetHolderInfoAsync("tDVV", string.Empty, caHashListSide, 0, 10000);
        caHashListSide.Clear();


        foreach (var holder in sideHolders.CaHolderInfo)
        {
            caHashListSide.Add(holder.CaHash);

            var bal = balanceAll.FirstOrDefault(t => t.CaAddress == holder.CaAddress);
            if (bal != null)
            {
                bal.CaHash = holder.CaHash;
            }
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
            var both = balanceAll.Where(t => t.CaHash == hash).ToList().OrderBy(t => t.ChainId);

            var builder = new StringBuilder($"{hash}");
            foreach (var bal in both)
            {
                builder.Append($"\t\t{bal.ChainId}\t\t{Math.Round(GetBalanceInUsd(bal.Balance, 8), 6).ToString()}");
            }

            Console.WriteLine(builder.ToString());
        }

        Logger.LogInformation("======main=====");
        foreach (var hash in caHashListMain.Distinct().ToList())
        {
            var bal = balanceAll.FirstOrDefault(t => t.CaHash == hash && t.ChainId == "AELF");
            Console.WriteLine($"{hash}\t{Math.Round(GetBalanceInUsd(bal.Balance, 8), 6).ToString()}");
        }

        Logger.LogInformation("======side=====");
        foreach (var hash in caHashListSide.Distinct().ToList())
        {
            var bal = balanceAll.FirstOrDefault(t => t.CaHash == hash && t.ChainId == "tDVV");
            Console.WriteLine($"{hash}\t{Math.Round(GetBalanceInUsd(bal.Balance, 8), 6).ToString()}");
        }
    }

    public async Task<Dictionary<string, int>> GetActivityCountByDayAsync()
    {
        var dic = new Dictionary<string, int>();

        var transactions = await GetActivitiesAsync(new List<CAAddressInfo>(), string.Empty,
            string.Empty, new List<string>(), 0, 1000000);

        var data = transactions.CaHolderTransaction.Data
            .Where(t => t.Status.Equals("MINED", StringComparison.OrdinalIgnoreCase))
            .Where(t => t.Timestamp > _start && t.Timestamp < _end)
            .ToList();

        var result = data.Select(t => new
                { t.MethodName, t.Timestamp, Date = GetDateTimeSeconds(t.Timestamp).ToString("yyyy-MM-dd") })
            .ToList();

        // var dd = result.GroupBy(t => t.Date).Select(t => new { name = t.Key, count = t.Count() })
        //     .OrderBy(m => m.name).ToList();
        // foreach (var d in dd)
        // {
        //     Console.WriteLine($"Transfer\t{d.name}\t {d.count}");
        // }

        var dataAll = result.GroupBy(t => new { t.Date, t.MethodName })
            .Select(t => new { name = t.Key.MethodName, Date = t.Key.Date, count = t.Count() })
            .OrderBy(m => m.Date).ThenBy(m => m.name).ToList();

        foreach (var per in dataAll)
        {
            //Console.WriteLine($"{per.Date}\t{per.name}\t{per.count}");
            Console.WriteLine($"{per.Date,-15} | {per.name,-50} | {per.count,5}");
        }


        return new Dictionary<string, int>();
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

    public static decimal GetBalanceInUsd(long balance, int decimals) =>
        (decimal)(balance / Math.Pow(10, decimals));

    private async Task<IndexerTransactions> GetActivitiesAsync(List<CAAddressInfo> caAddressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query ($chainId:String,$symbol:String,$caAddressInfos:[CAAddressInfo]!,$methodNames:[String],$startBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,symbol:$symbol,caAddressInfos:$caAddressInfos,methodNames:$methodNames,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{id,chainId,blockHeight,methodName,status,timestamp},totalRecordCount
                    }
                }",
            Variables = new
            {
                caAddressInfos = caAddressInfos, chainId = inputChainId, symbol = symbolOpt,
                methodNames = inputTransactionTypes, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
                startBlockHeight = 0, endBlockHeight = 0
            }
        });
    }

    // 1694448000
    // 1695657600
    // private long GetBlockHash()
    // {
    //     
    // }

    public static DateTime GetDateTimeSeconds(long timestamp)
    {
        var begtime = timestamp * 10000000;
        var dt_1970 = new DateTime(1970, 1, 1, 8, 0, 0);
        var tricks_1970 = dt_1970.Ticks;
        var time_tricks = tricks_1970 + begtime;
        var dt = new DateTime(time_tricks);
        return dt;
    }
}