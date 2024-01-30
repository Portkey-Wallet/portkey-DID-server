using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Guardian.Provider;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using CAServer.Vote;
using GraphQL;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.CaHolder;

[RemoteService(false)]
[DisableAuditing]
public class CaHolderAppService : CAServerAppService, ICaHolderAppService
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IHttpClientService _httpClientService;

    private List<HolderInfo> HolderInfos = new();
    private List<string> MintAddress = new List<string>();

    public CaHolderAppService(IGraphQLHelper graphQlHelper, IHttpClientService httpClientService)
    {
        _graphQlHelper = graphQlHelper;
        _httpClientService = httpClientService;
    }

    public async Task<string> Statistic()
    {
        // await GetUserInfo("AELF");
        // await GetUserInfo("tDVV");
        //await GetMintInfo();
        //WriteMintAddress();
        // ReadHolderInfo();

        ReadHolderInfo();
        ReadMintAddressesInfo();

        var activity = await GetActivitiesAsync(new List<CAAddressInfo>(), string.Empty, string.Empty,
            new List<string>() { "CreateCAHolder" }, 0, 5000);

        Console.WriteLine($"total create holder count:{activity.CaHolderTransaction.TotalRecordCount}");
        Console.WriteLine($"total create holder count:{activity.CaHolderTransaction.Data.Count}");
        var newAddresses = activity.CaHolderTransaction.Data.Select(t => t.FromAddress).ToList();

        var newHolders = HolderInfos.Where(t => newAddresses.Contains(t.CaAddress)).ToList();
        var newHashes = newHolders.Select(t => t.CaHash).ToList();

        var newHoldersWithAddress = HolderInfos.Where(t => newHashes.Contains(t.CaHash)).ToList();
        var newSideAddresses = newHoldersWithAddress.Where(t => t.ChainId == "tDVV").Select(f => f.CaAddress).ToList();

        var notMintAddresses = newSideAddresses.Except(MintAddress).ToList();

        var fileInfo = new FileInfo("not_mint_address.txt");
        var sw = fileInfo.CreateText();
        foreach (var address in notMintAddresses)
        {
            sw.WriteLine(address);
        }

        sw.Flush();
        sw.Close();

        // var nftInfo = await GetUserNftInfoAsync(new List<CAAddressInfo>(), "ELEPHANT-1", 0, 10000);
        //
        // var addresses = nftInfo.CaHolderNFTBalanceInfo.Data.Select(t => new { t.CaAddress, t.Balance }).ToList();
        //
        // var aas = nftInfo.CaHolderNFTBalanceInfo.Data.Select(t => t.CaAddress).ToList();
        // var aaaaa = newAddresses.Intersect(aas).ToList();
        // Console.WriteLine(aaaaa.Count);

        // var fileInfo = new FileInfo("statistic.txt");
        // var sw = fileInfo.CreateText();
        // foreach (var holderInfo in newHoldersWithAddress)
        // {
        //     var addressInfo = addresses.FirstOrDefault(t => t.CaAddress == holderInfo.CaAddress);
        //     if (addressInfo == null)
        //     {
        //         continue;
        //     }
        //
        //     sw.WriteLine($"{holderInfo.ChainId}\t{holderInfo.CaHash}\t{holderInfo.CaAddress}\t{addressInfo.Balance}");
        // }
        //
        // sw.Flush();
        // sw.Close();

        return "ok";
    }

    public async Task<string> Statistic2()
    {
        ReadHolderInfo();
        ReadMintAddressesInfo();
        // var activityInfo1 = await GetMintInfo2(1706025600, 1706184000);
        // var activityInfo2 = await GetMintInfo2(1706443200, 1706585067);
        ReadMintAddressesInfo2();
        
        
        var minAll = MintAddress;//.Union(activityInfo1).Union(activityInfo2).ToList();

        var createCAHolder1 = await GetActivitiesAsync(new List<CAAddressInfo>(), string.Empty, string.Empty,
            new List<string>() { "CreateCAHolder" }, 0, 5000, 1706025600, 1706184000);

        Console.WriteLine($"total create holder1 count:{createCAHolder1.CaHolderTransaction.TotalRecordCount}");
        Console.WriteLine($"total create holder1 count:{createCAHolder1.CaHolderTransaction.Data.Count}");

        var createCAHolder2 = await GetActivitiesAsync(new List<CAAddressInfo>(), string.Empty, string.Empty,
            new List<string>() { "CreateCAHolder" }, 0, 5000, 1706443200, 1706585067);

        Console.WriteLine($"total create holder2 count:{createCAHolder2.CaHolderTransaction.TotalRecordCount}");
        Console.WriteLine($"total create holder2 count:{createCAHolder2.CaHolderTransaction.Data.Count}");
        var newAddress1 = createCAHolder1.CaHolderTransaction.Data.Select(t => t.FromAddress).ToList();
        var newAddress2 = createCAHolder2.CaHolderTransaction.Data.Select(t => t.FromAddress).ToList();
        var newAddresses = newAddress1.Union(newAddress2);

        var newHolders = HolderInfos.Where(t => newAddresses.Contains(t.CaAddress)).ToList();
        var newHashes = newHolders.Select(t => t.CaHash).ToList();

        var newHoldersWithAddress = HolderInfos.Where(t => newHashes.Contains(t.CaHash)).ToList();
        var newSideAddresses = newHoldersWithAddress.Where(t => t.ChainId == "tDVV").Select(f => f.CaAddress).ToList();

        var nftInfo = await GetUserNftInfoAsync(new List<CAAddressInfo>(), "ELEPHANT-1", 0, 10000);
        var hasNftAddresses = nftInfo.CaHolderNFTBalanceInfo.Data.Select(t => t.CaAddress).ToList();

        var addresses = newSideAddresses.Except(minAll).Except(hasNftAddresses).ToList();

        var fileInfo = new FileInfo("not_mint_and_no_elephant_address.txt");
        var sw = fileInfo.CreateText();
        foreach (var address in addresses)
        {
            sw.WriteLine(address);
        }

        sw.Flush();
        sw.Close();

        return "ok";
    }

    public async Task<string> Sort()
    {
        var list = GetHolderInfoWithElephant();
        list = list.DistinctBy(t => t.CaAddress).ToList();
        list = list.OrderBy(t => t.CaHash).ThenBy(f => f.ChainId).ThenBy(h => h.Balance).ToList();
        WriteStatistic(list);

        return "ok";
    }

    public Task<string> Term()
    {
    
        {
            var sr = new StreamReader(@"not_mint_address.txt");

            string nextLine;
            while ((nextLine = sr.ReadLine()) != null)
            {
                if (!MintAddress.Contains(nextLine))
                {
                    MintAddress.Add(nextLine);
                }
            }

            sr.Close();
        }
        
        var fileInfo2 = new FileInfo("not_mint_address_term.txt");
        var sw2 = fileInfo2.CreateText();
        foreach (var address in MintAddress)
        {
            sw2.WriteLine($"ELF_{address}_tDVV");
        }

        sw2.Flush();
        sw2.Close();

        return Task.FromResult("ok");
    }

    public List<HolderInfoWithElephant> GetHolderInfoWithElephant()
    {
        List<HolderInfoWithElephant> list = new List<HolderInfoWithElephant>();
        var sr = new StreamReader(@"statistic.txt");

        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            var aaa = nextLine.Split('\t');
            list.Add(new HolderInfoWithElephant()
            {
                ChainId = aaa[0],
                CaHash = aaa[1],
                CaAddress = aaa[2],
                Balance = Convert.ToInt64(aaa[3])
            });
        }

        sr.Close();

        return list;
    }

    private void WriteStatistic(List<HolderInfoWithElephant> list)
    {
        var fileInfo = new FileInfo("statistic_sort.txt");
        var sw = fileInfo.CreateText();
        foreach (var holderInfo in list)
        {
            sw.WriteLine($"{holderInfo.ChainId}\t{holderInfo.CaHash}\t{holderInfo.CaAddress}\t{holderInfo.Balance}");
        }

        sw.Flush();
        sw.Close();
    }

    private void WriteMintAddress()
    {
        var fileInfo = new FileInfo("mint_address.txt");
        var sw = fileInfo.CreateText();
        foreach (var address in MintAddress)
        {
            sw.WriteLine(address);
        }

        sw.Flush();
        sw.Close();
    }

    private void ReadMintAddressesInfo()
    {
        var sr = new StreamReader(@"mint_address.txt");

        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            if (!MintAddress.Contains(nextLine))
            {
                MintAddress.Add(nextLine);
            }
        }

        sr.Close();
    }
    
    private void ReadMintAddressesInfo2()
    {
        var sr = new StreamReader(@"not_mint_address2.txt");

        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            nextLine = nextLine.Trim(' ').Trim('"').TrimEnd('"').TrimEnd(' ');
            if (!MintAddress.Contains(nextLine))
            {
                MintAddress.Add(nextLine);
            }
        }

        sr.Close();
    }

    private void ReadHolderInfo()
    {
        var sr = new StreamReader(@"holderInfo.txt");

        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            var aaa = nextLine.Split('\t');
            HolderInfos.Add(new HolderInfo()
            {
                ChainId = aaa[0],
                CaHash = aaa[1],
                CaAddress = aaa[2]
            });
        }

        sr.Close();
    }

    public void Write()
    {
        var fileInfo = new FileInfo("holderInfo.txt");
        var sw = fileInfo.CreateText();
        foreach (var content in HolderInfos)
        {
            var holderInfo = $"{content.ChainId}\t{content.CaHash}\t{content.CaAddress}";
            sw.WriteLine(holderInfo);
        }

        sw.Flush();
        sw.Close();
    }


    public async Task GetUserInfo(string chainId)
    {
        var onceCount = 2000;
        var count = 0;
        for (var i = 0; i < 100000; i = i + onceCount)
        {
            var holders = await GetHolderInfoAsync(chainId, string.Empty, new List<string>(), i, onceCount);
            if (holders == null || holders.CaHolderInfo.IsNullOrEmpty())
            {
                break;
            }

            foreach (var holder in holders.CaHolderInfo)
            {
                count++;
                HolderInfos.Add(new HolderInfo()
                {
                    ChainId = holder.ChainId,
                    CaHash = holder.CaHash,
                    CaAddress = holder.CaAddress
                });
            }
        }

        Console.WriteLine($"chain {chainId}, holder count:{count}");
    }

    // mint success side chain ca address
    public async Task GetMintInfo()
    {
        var onceCount = 5000;
        var count = 0;
        for (var i = 0; i < 200000; i = i + onceCount)
        {
            var activity = await GetActivitiesAsync(new List<CAAddressInfo>(), string.Empty, string.Empty,
                new List<string>() { "Inscribe", "MintInscription" }, i, onceCount);
            if (activity == null || activity.CaHolderTransaction.Data.IsNullOrEmpty())
            {
                break;
            }

            foreach (var transaction in activity.CaHolderTransaction.Data)
            {
                if (MintAddress.Contains(transaction.FromAddress))
                {
                    continue;
                }

                MintAddress.Add(transaction.FromAddress);
                count++;
            }
        }

        Console.WriteLine($"mint success address count:{count}");
    }

    public async Task<List<string>> GetMintInfo2(long startTime, long endTime)
    {
        var list = new List<string>();
        var onceCount = 8000;
        var count = 0;
        for (var i = 0; i < 200000; i = i + onceCount)
        {
            var activity = await GetActivitiesAsync(new List<CAAddressInfo>(), string.Empty, string.Empty,
                new List<string>() { "Inscribe", "MintInscription" }, i, onceCount, startTime, endTime);
            if (activity == null || activity.CaHolderTransaction.Data.IsNullOrEmpty())
            {
                break;
            }

            foreach (var transaction in activity.CaHolderTransaction.Data)
            {
                if (MintAddress.Contains(transaction.FromAddress) || list.Contains(transaction.FromAddress))
                {
                    continue;
                }

                list.Add(transaction.FromAddress);
                count++;
            }
        }

        Console.WriteLine($"mint success address count:{count}");
        return list;
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

    private async Task<IndexerTransactions> GetActivitiesAsync(List<CAAddressInfo> caAddressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount,
        long startTime = 1706184000, long endTime = 1706443200)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query ($chainId:String,$symbol:String,$caAddressInfos:[CAAddressInfo]!,$methodNames:[String],$startBlockHeight:Long!,$endBlockHeight:Long!,$startTime:Long!,$endTime:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,symbol:$symbol,caAddressInfos:$caAddressInfos,methodNames:$methodNames,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,startTime:$startTime,endTime:$endTime,maxResultCount:$maxResultCount}){
                        data{id,chainId,fromAddress,blockHeight,methodName,status,timestamp},totalRecordCount
                    }
                }",
            Variables = new
            {
                caAddressInfos = caAddressInfos, chainId = inputChainId, symbol = symbolOpt,
                methodNames = inputTransactionTypes, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
                startBlockHeight = 0, endBlockHeight = 0, startTime = startTime, endTime = endTime
            }
        });
    }

    public async Task<IndexerNftInfos> GetUserNftInfoAsync(List<CAAddressInfo> caAddressInfos, string symbol,
        int inputSkipCount, int inputMaxResultCount)
    {
        return await _graphQlHelper.QueryAsync<IndexerNftInfos>(new GraphQLRequest
        {
            Query = @"
			    query($symbol:String,$caAddressInfos:[CAAddressInfo],$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderNFTBalanceInfo(dto: {symbol:$symbol,caAddressInfos:$caAddressInfos,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{chainId,balance,caAddress,nftInfo{symbol,imageUrl,collectionSymbol,collectionName,decimals,tokenName,totalSupply,supply,tokenContractAddress}},totalRecordCount}
                }",
            Variables = new
            {
                caAddressInfos = caAddressInfos, symbol, skipCount = inputSkipCount,
                maxResultCount = inputMaxResultCount
            }
        });
    }
}