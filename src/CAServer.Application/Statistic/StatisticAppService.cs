using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Guardian.Provider;
using CAServer.UserAssets;
using GraphQL;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;

namespace CAServer.Statistic;

[RemoteService(false), DisableAuditing]
public class StatisticAppService : CAServerAppService, IStatisticAppService
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IDistributedCache<IndexerTransactions> _distributedCache;
    private readonly IDistributedCache<List<HolderInfoSta>> _holdersCache;

    private static readonly List<string> TransferTypes = new()
    {
        "Transfer", "CrossChainTransfer"
    };

    private long _start = 1691032892;
    private long _end = 1699240892;
    private string _cacheKey = "StatisticTrade";
    private string _cacheHolderKey = "StatisticHolders";

    private List<IndexerTransaction> StaticTransfers = new List<IndexerTransaction>();
    private List<IndexerTransaction> StaticCrossTransfers = new List<IndexerTransaction>();

    public StatisticAppService(IGraphQLHelper graphQlHelper, IDistributedCache<IndexerTransactions> distributedCache,
        IDistributedCache<List<HolderInfoSta>> holdersCache)
    {
        _graphQlHelper = graphQlHelper;
        _distributedCache = distributedCache;
        _holdersCache = holdersCache;
    }

    // transfer: fromCaAddress, toAddress
    // crossChainTransfer: toAddress
    public async Task<int> GetTransferInfoAsync()
    {
        var transactionInfos = await _distributedCache.GetAsync(_cacheKey);
        if (transactionInfos == null)
        {
            transactionInfos = await GetActivitiesAsync(new List<CAAddressInfo>(), string.Empty,
                string.Empty, TransferTypes, 0, 100000);

            await _distributedCache.SetAsync(_cacheKey, transactionInfos, new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = DateTime.Now.AddDays(5) - DateTime.Now
            });
        }

        Logger.LogInformation("total trans count: {count}", transactionInfos.CaHolderTransaction.Data.Count);

        var transactions = transactionInfos.CaHolderTransaction.Data
            .Where(t => t.Status.Equals("MINED", StringComparison.OrdinalIgnoreCase))
            .Where(t => t.TransferInfo != null && t.TokenInfo != null && t.TokenInfo.Symbol == "ELF").ToList();
        transactions = transactions.Where(t => t.Timestamp > _start && t.Timestamp < _end).ToList();
        Logger.LogInformation("elf transfer total count: {count}", transactions.Count);

        var transfers = transactions.Where(t => t.MethodName == "Transfer").ToList();
        var crossChainTransfers = transactions.Where(t => t.MethodName == "CrossChainTransfer").ToList();

        Logger.LogInformation("transfer count: {count}", transfers.Count);
        Logger.LogInformation("crossChainTransfer count: {count}", crossChainTransfers.Count);

        var result = new List<UserTransferInfo>();
        var addressAll = GetAddresses(transfers, crossChainTransfers);
        var mainAddresses = addressAll.mainAddress;
        Logger.LogInformation("mains count: {count}", mainAddresses.Count);

        var sideAddresses = addressAll.sideAddress;
        Logger.LogInformation("sides count: {count}", sideAddresses.Count);

        var holders = await GetHolders(mainAddresses, sideAddresses);
        Logger.LogInformation("holders count: {count}", holders.Count);

        // foreach (var transaction in transfers)
        // {
        //     if (transaction.ChainId == "AELF")
        //     {
        //         HandleFromMain(transaction, holders, result);
        //         HandleToMain(transaction, holders, result);
        //     }
        //     else if (transaction.ChainId == "tDVV")
        //     {
        //         HandleFromSide(transaction, holders, result);
        //         HandleToSide(transaction, holders, result);
        //     }
        // }
        //
        // foreach (var transaction in crossChainTransfers)
        // {
        //     if (transaction.ChainId == "AELF")
        //     {
        //         HandleToMain(transaction, holders, result);
        //     }
        //     else if (transaction.ChainId == "tDVV")
        //     {
        //         HandleToSide(transaction, holders, result);
        //     }
        // }

        var ll = new List<UserTransferInfoByDate>();
        foreach (var transaction in transfers)
        {
            var date = TimeHelper.GetDateTimeFromSecondTimeStamp(transaction.Timestamp).ToString("yyyy-MM-dd");
            var transByDate = ll.FirstOrDefault(t => t.Date == date);
            if (transaction.ChainId == "AELF")
            {
                if (transByDate == null)
                {
                    var dd = new UserTransferInfoByDate()
                    {
                        Date = date,
                        MainChainTradeCount = 1,
                        MainChainTradeAmount = transaction.TransferInfo.Amount ?? 0L
                    };

                    ll.Add(dd);
                }
                else
                {
                    transByDate.MainChainTradeCount++;
                    transByDate.MainChainTradeAmount += transaction.TransferInfo.Amount ?? 0L;
                }
            }
            else if (transaction.ChainId == "tDVV")
            {
                if (transByDate == null)
                {
                    var dd = new UserTransferInfoByDate()
                    {
                        Date = date,
                        SideChainTradeCount = 1,
                        SideChainTradeAmount = transaction.TransferInfo.Amount ?? 0L
                    };

                    ll.Add(dd);
                }
                else
                {
                    transByDate.SideChainTradeCount++;
                    transByDate.SideChainTradeAmount += transaction.TransferInfo.Amount ?? 0L;
                }
            }
        }

        Logger.LogInformation("result count: {count}", ll.Count);

        foreach (var per in ll.OrderBy(t => DateTime.Parse(t.Date)))
        {
            Logger.LogInformation(
                $"{per.Date,-12} | {per.MainChainTradeCount,-5} | {Math.Round(GetBalance(per.MainChainTradeAmount, 8), 4),-15}| {per.SideChainTradeCount,-5} | {Math.Round(GetBalance(per.SideChainTradeAmount, 8), 6),-10}");
        }

        // foreach (var per in result.OrderByDescending(t => t.MainChainTradeAmount)
        //              .ThenByDescending(t => t.SideChainTradeAmount))
        // {
        //     Logger.LogInformation(
        //         $"{per.CaHash,-20} | {per.MainChainTradeCount,-5} | {Math.Round(GetBalance(per.MainChainTradeAmount, 8), 4),-15}| {per.SideChainTradeCount,-5} | {Math.Round(GetBalance(per.SideChainTradeAmount, 8), 6),-10}");
        // }

        return transactions.Count;
    }

    private void HandleFromMain(IndexerTransaction transaction, List<HolderInfoSta> holders,
        List<UserTransferInfo> result)
    {
        var holder = holders.FirstOrDefault(t => t.MainAddress == transaction.TransferInfo.FromAddress);
        if (holder == null)
        {
            return;
        }

        var userTransferInfo = result.FirstOrDefault(t => t.CaHash == holder.CaHash);
        if (userTransferInfo == null)
        {
            var user = new UserTransferInfo
            {
                CaHash = holder.CaHash,
                MainChainTradeCount = 1,
                MainChainTradeAmount = transaction.TransferInfo.Amount ?? 0L
            };

            result.Add(user);
        }
        else
        {
            userTransferInfo.MainChainTradeCount++;
            userTransferInfo.MainChainTradeAmount += transaction.TransferInfo.Amount ?? 0L;
        }
    }

    private void HandleToMain(IndexerTransaction transaction, List<HolderInfoSta> holders,
        List<UserTransferInfo> result)
    {
        var holder = holders.FirstOrDefault(t => t.MainAddress == transaction.TransferInfo.ToAddress);
        if (holder == null)
        {
            return;
        }

        var userTransferInfo = result.FirstOrDefault(t => t.CaHash == holder.CaHash);
        if (userTransferInfo == null)
        {
            var user = new UserTransferInfo
            {
                CaHash = holder.CaHash,
                MainChainTradeCount = 1,
                MainChainTradeAmount = transaction.TransferInfo.Amount ?? 0L
            };

            result.Add(user);
        }
        else
        {
            userTransferInfo.MainChainTradeCount++;
            userTransferInfo.MainChainTradeAmount += transaction.TransferInfo.Amount ?? 0L;
        }
    }

    private void HandleFromSide(IndexerTransaction transaction, List<HolderInfoSta> holders,
        List<UserTransferInfo> result)
    {
        var holder = holders.FirstOrDefault(t => t.SideAddress == transaction.TransferInfo.FromAddress);
        if (holder == null)
        {
            return;
        }

        var userTransferInfo = result.FirstOrDefault(t => t.CaHash == holder.CaHash);
        if (userTransferInfo == null)
        {
            var user = new UserTransferInfo
            {
                CaHash = holder.CaHash,
                SideChainTradeCount = 1,
                SideChainTradeAmount = transaction.TransferInfo.Amount ?? 0L
            };
            result.Add(user);
        }
        else
        {
            userTransferInfo.SideChainTradeCount++;
            userTransferInfo.SideChainTradeAmount += transaction.TransferInfo.Amount ?? 0L;
        }
    }

    private void HandleToSide(IndexerTransaction transaction, List<HolderInfoSta> holders,
        List<UserTransferInfo> result)
    {
        var holder = holders.FirstOrDefault(t => t.SideAddress == transaction.TransferInfo.ToAddress);
        if (holder == null)
        {
            return;
        }

        var userTransferInfo = result.FirstOrDefault(t => t.CaHash == holder.CaHash);
        if (userTransferInfo == null)
        {
            var user = new UserTransferInfo
            {
                CaHash = holder.CaHash,
                SideChainTradeCount = 1,
                SideChainTradeAmount = transaction.TransferInfo.Amount ?? 0L
            };
            result.Add(user);
        }
        else
        {
            userTransferInfo.SideChainTradeCount++;
            userTransferInfo.SideChainTradeAmount += transaction.TransferInfo.Amount ?? 0L;
        }
    }

    private (List<string> mainAddress, List<string> sideAddress) GetAddresses(List<IndexerTransaction> transfers,
        List<IndexerTransaction> crossChainTransfers)
    {
        var mainAddresses1 = transfers.Where(t => t.ChainId == "AELF").Select(t => t.TransferInfo.FromCAAddress)
            .ToList();
        Logger.LogInformation("mainAddresses1 count: {count}", mainAddresses1.Count);
        var mainAddresses2 = transfers.Where(t => t.ChainId == "AELF").Select(t => t.TransferInfo.ToAddress).ToList();
        Logger.LogInformation("mainAddresses2 count: {count}", mainAddresses2.Count);
        var mainAddresses3 = crossChainTransfers.Where(t => t.ChainId == "AELF").Select(t => t.TransferInfo.ToAddress)
            .ToList();
        Logger.LogInformation("mainAddresses3 count: {count}", mainAddresses3.Count);
        mainAddresses1.AddRange(mainAddresses2);
        mainAddresses1.AddRange(mainAddresses3);
        var mains = mainAddresses1.Distinct().ToList();
        Logger.LogInformation("mains count: {count}", mains.Count);

        var sideAddresses1 = transfers.Where(t => t.ChainId == "tDVV").Select(t => t.TransferInfo.FromCAAddress)
            .ToList();
        Logger.LogInformation("sideAddresses1 count: {count}", sideAddresses1.Count);
        var sideAddresses2 = transfers.Where(t => t.ChainId == "tDVV").Select(t => t.TransferInfo.ToAddress).ToList();
        Logger.LogInformation("sideAddresses2 count: {count}", sideAddresses2.Count);
        var sideAddresses3 = crossChainTransfers.Where(t => t.ChainId == "tDVV").Select(t => t.TransferInfo.ToAddress)
            .ToList();
        Logger.LogInformation("sideAddresses3 count: {count}", sideAddresses3.Count);
        sideAddresses1.AddRange(sideAddresses2);
        sideAddresses1.AddRange(sideAddresses3);
        var sides = sideAddresses1.Distinct().ToList();
        Logger.LogInformation("sides count: {count}", sides.Count);

        return (mains, sides);
    }

    private async Task<List<HolderInfoSta>> GetHolders(List<string> mainAddresses, List<string> sideAddresses)
    {
        var onceCount = 500;
        var mainTotalCount = mainAddresses.Count;

        var holders = await _holdersCache.GetAsync(_cacheHolderKey);
        if (holders.IsNullOrEmpty())
        {
            holders = new List<HolderInfoSta>();
            for (var i = 0; i < mainTotalCount; i = i + onceCount)
            {
                var addressMains = mainAddresses.Skip(i).Take(onceCount).ToList();
                var mainHolders = await GetHolderInfoAsync("AELF", string.Empty, addressMains, 0, onceCount);
                foreach (var holder in mainHolders.CaHolderInfo)
                {
                    holders.Add(new HolderInfoSta()
                    {
                        CaHash = holder.CaHash,
                        MainAddress = holder.CaAddress
                    });
                }
            }

            var hashes = holders.Select(t => t.CaHash).ToList();
            for (var i = 0; i < sideAddresses.Count; i = i + onceCount)
            {
                var addressSides = sideAddresses.Skip(i).Take(onceCount).ToList();
                var sideHolders = await GetHolderInfoAsync("tDVV", string.Empty, addressSides, 0, onceCount);
                foreach (var holder in sideHolders.CaHolderInfo)
                {
                    if (hashes.Contains(holder.CaHash))
                    {
                        var hh = holders.First(t => t.CaHash == holder.CaHash);
                        hh.SideAddress = holder.CaAddress;
                        continue;
                    }

                    holders.Add(new HolderInfoSta()
                    {
                        CaHash = holder.CaHash,
                        SideAddress = holder.CaAddress
                    });
                }
            }

            await _holdersCache.SetAsync(_cacheHolderKey, holders, new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = DateTime.Now.AddDays(5) - DateTime.Now
            });
        }

        return holders;
    }

    private async Task<IndexerTransactions> GetActivitiesAsync(List<CAAddressInfo> caAddressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query ($chainId:String,$symbol:String,$caAddressInfos:[CAAddressInfo]!,$methodNames:[String],$startBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,symbol:$symbol,caAddressInfos:$caAddressInfos,methodNames:$methodNames,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{id,chainId,blockHash,blockHeight,previousBlockHash,transactionId,methodName,tokenInfo{symbol,tokenContractAddress,decimals,totalSupply,tokenName},status,timestamp,nftInfo{symbol,totalSupply,imageUrl,decimals,tokenName},transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress},fromAddress,transactionFees{symbol,amount}},totalRecordCount
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

    private static decimal GetBalance(long balance, int decimals) =>
        (decimal)(balance / Math.Pow(10, decimals));
}

public class UserTransferInfo
{
    public string CaHash { get; set; }
    public int MainChainTradeCount { get; set; }
    public long MainChainTradeAmount { get; set; }
    public int SideChainTradeCount { get; set; }
    public long SideChainTradeAmount { get; set; }
}

public class UserTransferInfoByDate
{
    public string Date { get; set; }
    public int MainChainTradeCount { get; set; }
    public long MainChainTradeAmount { get; set; }
    public int SideChainTradeCount { get; set; }
    public long SideChainTradeAmount { get; set; }
}

public class HolderInfoSta
{
    public string CaHash { get; set; }
    public string MainAddress { get; set; }
    public string SideAddress { get; set; }
}