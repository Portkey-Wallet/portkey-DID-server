using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CAServer.AddressExtraInfo;
using CAServer.CAAccount.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using CAServer.UserAssets;
using GraphQL;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.GuardiansStatistic;

[RemoteService(false), DisableAuditing]
public class GuardiansStatisticAppService : CAServerAppService, IGuardiansStatisticAppService
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IHttpClientService _httpClientService;
    public List<string> HasLoginPhoneGuardian = new List<string>();
    public List<string> HasPhoneGuardian = new List<string>();
    private List<AddressInfoExtension> AddressInfos = new List<AddressInfoExtension>();
    private Dictionary<string, long> ActivityAddress = new Dictionary<string, long>();
    private Dictionary<string, long> Sideddress = new Dictionary<string, long>();

    public GuardiansStatisticAppService(IGraphQLHelper graphQlHelper, IHttpClientService httpClientService)
    {
        _graphQlHelper = graphQlHelper;
        _httpClientService = httpClientService;
    }

    public async Task<string> GetInfo()
    {
        // await GetAddressInfo();
        //
        // Console.WriteLine($"HasPhoneGuardian count:{HasPhoneGuardian.Distinct().Count()}");
        // Console.WriteLine($"HasLoginPhoneGuardian count:{HasLoginPhoneGuardian.Distinct().Count()}");
        //
        // await DoSth();

        // ReadHolderInfo();
        //
        // var add = AddressInfos.DistinctBy(t => t.CaHash).ToList();
        // Console.WriteLine(add.Count);
        // await GetAddressInfo();
        // WriteAll();
        ReadHolderInfo();
        // await GetMintInfo();
        // // await GetUserInfo();
        // Write123456();
        // foreach (var per in ActivityAddress)
        // {
        //     try
        //     {
        //         var ttt = AddressInfos.FirstOrDefault(t => t.CaAddress == per.Key);
        //         if (ttt == null)
        //         {
        //             Console.WriteLine($"holder not found, {per.Key}");
        //         }
        //
        //         if (ttt.ChainId == "tDVV")
        //         {
        //             Sideddress.Add(ttt.CaAddress, per.Value);
        //             continue;
        //         }
        //
        //         var t2 = AddressInfos.FirstOrDefault(t => t.CaHash == ttt.CaHash && t.ChainId == "tDVV");
        //         if (t2 == null)
        //         {
        //             Console.WriteLine($"side holder not found, {per.Key}");
        //             continue;
        //         }
        //
        //         Sideddress.Add(t2.CaAddress, per.Value);
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e);
        //     }
        // }

        Write123();

        return "ok";
    }

    public  DateTime GetUnixDateTimeSeconds(long timestamp)
    {
        long begtime = timestamp * 10000000;
        DateTime dt_1970 = new DateTime(1970, 1, 1, 0, 0, 0);
        long tricks_1970 = dt_1970.Ticks;//1970年1月1日刻度
        long time_tricks = tricks_1970 + begtime;//日志日期刻度
        DateTime dt = new DateTime(time_tricks);//转化为DateTime
        return dt;
    }
    public void Write123()
    {
        foreach (var content in Sideddress)
        {
            var aaa = GetUnixDateTimeSeconds(content.Value).ToString("yyyy-MM-dd HH:mm:ss");
            var sw = File.AppendText("side_address2.txt");
            var holderInfo = $"{content.Key}\t{aaa}";
            sw.WriteLine(holderInfo);

            sw.Flush();
            sw.Close();
        }
    }


    public void Write123456()
    {
        foreach (var content in ActivityAddress)
        {
            var sw = File.AppendText("activity_address.txt");
            var holderInfo = $"{content.Key}\t{content.Value}";
            sw.WriteLine(holderInfo);

            sw.Flush();
            sw.Close();
        }
    }

    private void ReadHolderInfo()
    {
        var sr = new StreamReader(@"side_address.txt");

        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            var aaa = nextLine.Split('\t');

            Sideddress.Add(aaa[0], Convert.ToInt64(aaa[1]));
        }

        sr.Close();
    }

    public void Write()
    {
        foreach (var content in AddressInfos)
        {
            var sw = File.AppendText("holderInfo.txt");
            var holderInfo = $"{content.ChainId}\t{content.CaHash}\t{content.CaAddress}";
            sw.WriteLine(holderInfo);

            sw.Flush();
            sw.Close();
        }
    }

    public void WriteAll()
    {
        var fileInfo = new FileInfo("holderInfo_0205.txt");
        var sw = fileInfo.CreateText();

        Console.WriteLine($"all holder info:{AddressInfos.Count}");
        foreach (var content in AddressInfos)
        {
            var holderInfo = $"{content.ChainId}\t{content.CaHash}\t{content.CaAddress}";
            sw.WriteLine(holderInfo);
        }

        sw.Flush();
        sw.Close();
    }

    private async Task DoSth()
    {
        foreach (var addressInfo in AddressInfos)
        {
            //get from i
            var url =
                $"https://aa-portkey.portkey.finance/api/app/account/guardianIdentifiers?ChainId={addressInfo.OriginChainId}&CaHash={addressInfo.CaHash}";

            var data = await _httpClientService.GetAsync<GuardianResultDto>(url);

            var guardians = data?.GuardianList?.Guardians;
            if (guardians == null || guardians.IsNullOrEmpty())
            {
                continue;
            }

            foreach (var guardian in guardians)
            {
                if (guardian.Type == (GuardianIdentifierType.Phone).ToString())
                {
                    Console.WriteLine($"caHash:{addressInfo.CaHash}, {guardian.GuardianIdentifier}");
                }
            }
        }
    }

    private async Task GetAddressInfo()
    {
        var onceCount = 10000;
        var count = 0;
        var holders = await GetHolderInfoAsync(string.Empty, string.Empty, new List<string>(), 34500, onceCount);
        if (holders == null || holders.CaHolderInfo.IsNullOrEmpty())
        {
            return;
        }

        foreach (var holder in holders.CaHolderInfo)
        {
            count++;

            AddressInfos.Add(new AddressInfoExtension()
            {
                ChainId = holder.ChainId,
                CaHash = holder.CaHash
            });
        }

        Console.WriteLine($"holder count:{count}");
    }

    private async Task GetUserInfo()
    {
        var onceCount = 10000;
        var count = 0;
        for (var i = 0; i < 100000; i = i + onceCount)
        {
            var holders = await GetHolderInfoAsync(string.Empty, string.Empty, new List<string>(), i, onceCount);
            if (holders == null || holders.CaHolderInfo.IsNullOrEmpty())
            {
                break;
            }

            foreach (var holder in holders.CaHolderInfo)
            {
                AddressInfos.Add(new AddressInfoExtension()
                {
                    ChainId = holder.ChainId,
                    CaHash = holder.CaHash,
                    CaAddress = holder.CaAddress
                });
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


    public async Task GetMintInfo()
    {
        var onceCount = 5000;
        var count = 0;

        for (var i = 0; i < 1000000; i = i + onceCount)
        {
            var activity = await GetActivitiesAsync(new List<CAAddressInfo>(), string.Empty, string.Empty,
                new List<string>() { "CreateCAHolder" }, i, onceCount);
            if (activity == null || activity.CaHolderTransaction.Data.IsNullOrEmpty())
            {
                break;
            }

            foreach (var transaction in activity.CaHolderTransaction.Data)
            {
                ActivityAddress.Add(transaction.FromAddress, transaction.Timestamp);
                count++;
            }
        }

        Console.WriteLine($"side address count:{count}");
    }


    private async Task<IndexerTransactions> GetActivitiesAsync(List<CAAddressInfo> caAddressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount,
        long startTime = 0, long endTime = 0)
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
        {
            Query = @"
			    query ($chainId:String,$symbol:String,$caAddressInfos:[CAAddressInfo]!,$methodNames:[String],$startBlockHeight:Long!,$endBlockHeight:Long!,$startTime:Long!,$endTime:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,symbol:$symbol,caAddressInfos:$caAddressInfos,methodNames:$methodNames,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,startTime:$startTime,endTime:$endTime,maxResultCount:$maxResultCount}){
                        data{fromAddress,timestamp},totalRecordCount
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
}