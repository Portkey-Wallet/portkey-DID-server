using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Service;
using AElf.Contracts.Vote;
using AElf.Types;
using CAServer.Common;
using CAServer.Guardian.Provider;
using CAServer.Options;
using CAServer.Switch.Dtos;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.Encoders;
using Volo.Abp;
using Volo.Abp.Auditing;
using Address = AElf.Client.Proto.Address;

namespace CAServer.Switch;

[RemoteService(false), DisableAuditing]
public class SwitchAppService : CAServerAppService, ISwitchAppService
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly SwitchOptions _options;
    ConcurrentQueue<string> result = new ConcurrentQueue<string>();
    ConcurrentDictionary<string, string> result2 = new ConcurrentDictionary<string, string>();
    ConcurrentQueue<string> notList = new ConcurrentQueue<string>();
    ConcurrentQueue<string> failList = new ConcurrentQueue<string>();

    public SwitchAppService(IOptionsSnapshot<SwitchOptions> options, IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
        _options = options.Value;
    }

    public async Task<SwitchDto> GetSwitchStatus(string switchName)
    {
        List<string> transIds = new List<string>();
        List<string> notTransIds = new List<string>();

        var sr1 = new StreamReader(@"NewFile1.txt");

        string nextLine;
        while ((nextLine = sr1.ReadLine()) != null)
        {
            transIds.Add(nextLine);
        }

        sr1.Close();

        var sr2 = new StreamReader(@"notList.txt");

        string nextLine2;
        while ((nextLine2 = sr2.ReadLine()) != null)
        {
            notTransIds.Add(nextLine2);
        }

        sr2.Close();


        transIds = transIds.Except(notTransIds).ToList();

        Logger.LogInformation("aaa, {aa}", transIds.Count);
        var tasks = new List<Task>();

        var onceCount = 50;
        for (var i = 0; i < transIds.Count; i = i + onceCount)
        {
            var list = transIds.Skip(i).Take(onceCount).ToList();

            foreach (var transId in list)
            {
                tasks.Add(GetSth(transId));
            }


            await Task.WhenAll(tasks);
        }

        var myFile = new FileInfo("result.txt");
        var sw1 = myFile.CreateText();
        foreach (var s in result)
        {
            sw1.WriteLine(s);
        }
        
        sw1.Flush();
        sw1.Close();
        
        var myFile2 = new FileInfo("notList.txt");
        var sw2 = myFile2.CreateText();
        foreach (var s in notList)
        {
            sw2.WriteLine(s);
        }
        
        sw2.Flush();
        sw2.Close();
        
        var myFile3 = new FileInfo("failList.txt");
        var sw3 = myFile3.CreateText();
        foreach (var s in failList)
        {
            sw3.WriteLine(s);
        }
        
        sw3.Flush();
        sw3.Close();
        
        foreach (var noIndexed in result)
        {
            Console.WriteLine("###" + noIndexed);
        }

        return new SwitchDto();
    }

    public async Task GetSwitchStatus3(List<string> list)
    {
        var addresses = new List<string>();
        var dic = new Dictionary<string, string>();

        foreach (var inde in result2)
        {
            var aaa = Org.BouncyCastle.Utilities.Encoders.Base64.Decode(inde.Value);
            var voted = Voted.Parser.ParseFrom(aaa);
            var address = voted.Voter;
            var voter = address.ToBase58();

            addresses.Add(voter);
            dic.Add(inde.Key, voter);
        }

        Logger.LogInformation("address count: {count}", addresses.Count);
        int count = 0;
        foreach (var add in dic)
        {
            var holders = await GetHolderInfoAsync(string.Empty, string.Empty, new List<string>() { add.Value }, 0, 10);
            if (holders?.CaHolderInfo?.Count > 0)
            {
                count++;
                var gu = holders.CaHolderInfo;
                string str1 = $"transId: {add.Key}, [ ";
                foreach (var g in gu)
                {
                    str1 = str1 + g.CaAddress + ",";
                }

                str1.TrimEnd(',');
                str1 = str1 + " ]";

                Console.WriteLine(str1);
            }
        }


        Logger.LogInformation("holders count: {count}", count);
    }

    public async Task GetSwitchStatus2()
    {
        var list = new List<string>();
        var sr1 = new StreamReader(@"caaddresses.txt");

        string nextLine;
        while ((nextLine = sr1.ReadLine()) != null)
        {
            list.Add(nextLine);
        }

        var holders = await GetHolderInfoAsync(string.Empty, string.Empty, list.Distinct().ToList() , 0, 10);
        foreach (var holder in holders.CaHolderInfo)
        {
            Console.WriteLine($"{holder.CaAddress}, {holder.CaHash}");
        }
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

    private async Task GetSth(string transId)
    {
        try
        {
            var client = new AElfClient("https://aelf-public-node.aelf.io");
            await client.IsConnectedAsync();

            var transactionResult = client.GetTransactionResultAsync(transId).Result;
            if (transactionResult.Status != "MINED")
            {
                return;
            }

            if (transactionResult.Transaction.To == "28PcLvP41ouUd6UNGsNRvKpkFTe6am34nPy4YPsWUJnZNwUvzM" ||
                transactionResult.Transaction.To == "2cLA9kJW3gdHuGoYNY16Qir69J3Nkn6MSsuYxRkUHbz4SG2hZr")
            {
                var vote = transactionResult.Logs?.Where(t => t.Name == "Voted").FirstOrDefault();

                if (vote == null)
                {
                    return;
                }

                var noIndexed = vote.NonIndexed;

                result.Enqueue(noIndexed);
                result2.TryAdd(transId, noIndexed);
                Logger.LogInformation("txid: {txid}", transactionResult.TransactionId);
            }
            else
            {
                notList.Enqueue(transId);
            }
        }
        catch (Exception e)
        {
            failList.Enqueue(transId);
            Logger.LogError(e, "id:{id}", transId);
        }
    }
}