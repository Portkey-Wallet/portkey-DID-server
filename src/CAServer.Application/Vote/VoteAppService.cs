using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Vote;
using CAServer.Common;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using Google.Protobuf;
using GraphQL;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Vote;

[RemoteService(false)]
[DisableAuditing]
public class VoteAppService : CAServerAppService, IVoteAppService
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IHttpClientService _httpClientService;

    private List<HolderInfo> HolderInfos = new();

    public VoteAppService(IGraphQLHelper graphQlHelper, IHttpClientService httpClientService)
    {
        _graphQlHelper = graphQlHelper;
        _httpClientService = httpClientService;
    }

    public async Task<string> GetVote()
    {
        await GetUserInfo("AELF");
        await GetUserInfo("tDVV");

        var caAddresses = HolderInfos.Select(t => t.CaAddress).ToList();
        var sr = new StreamReader(@"vote20240111.txt");

        int line = 0;
        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            try
            {
                var lineData = nextLine.Split('\t');
                if (lineData[2] == "Voted")
                {
                    // handle 
                    var log = JsonConvert.DeserializeObject<TransactionLogsInfo>(lineData[4]);
                    var str = ByteString.FromBase64(log.NonIndexed);
                    // var log = new TokenCreated();
                    // log.MergeFrom(str);
                    var voted = Voted.Parser.ParseFrom(str);
                    if (caAddresses.Contains(voted.Voter.ToBase58()))
                    {
                        var holder = HolderInfos.First(t => t.CaAddress == voted.Voter.ToBase58());
                        Logger.LogInformation($"{lineData[1]}\t{voted.Voter.ToBase58()}\t{holder.CaHash}");
                    }
                }

                line++;
            }
            catch (Exception e)
            {
                Console.WriteLine("GetVote handle next line error, " + e.Message);
            }
        }

        sr.Close();

        return "ok";
    }

    public async Task<string> Beauty()
    {
        var sr = new StreamReader(@"vote-transaction.txt");
        var list = new List<VoteDataInfo>();

        int line = 0;
        string nextLine;
        while ((nextLine = sr.ReadLine()) != null)
        {
            try
            {
                var lineData = nextLine.Split('\t');
                list.Add(new VoteDataInfo()
                {
                    TransactionId = lineData[0],
                    CaAddress = lineData[1],
                    CaHash = lineData[2]
                });

                line++;
            }
            catch (Exception e)
            {
                Console.WriteLine("GetVote handle next line error, " + e.Message);
            }
        }

        sr.Close();

        list = list.OrderBy(t => t.CaAddress).ThenBy(f => f.CaHash).ToList();
        foreach (var voteDataInfo in list)
        {
            //Logger.LogInformation($"{voteDataInfo.TransactionId}\t{voteDataInfo.CaAddress}\t{voteDataInfo.CaHash}");
        }

        var address = list.Select(t => t.CaAddress).Distinct();
        foreach (var s in address)
        {
            var ho = list.First(t => t.CaAddress == s).CaHash;


            var guardian = await GetGuardian(ho, "AELF");
            var dd = guardian.GuardianList.Guardians.First(t => t.IsLoginGuardian);
            Console.WriteLine(
                $"{s}\t{ho}\t{dd.GuardianIdentifier}\t{dd.Type}\t{dd.ThirdPartyEmail ?? "-"}\t{dd.FirstName ?? "-"}\t{dd.LastName ?? "-"}");
        }

        return "ok";
    }

    public async Task<GuardianResultDto> GetGuardian(string hash, string chainId)
    {
        var url =
            $"https://did-portkey.portkey.finance/api/app/account/guardianIdentifiers?chainId={chainId}&caHash={hash}";
        try
        {
            var info = await _httpClientService.GetAsync<GuardianResultDto>(url);
            return info;
        }
        catch (Exception e)
        {
            var url2 =
                $"https://did-portkey.portkey.finance/api/app/account/guardianIdentifiers?chainId=tDVV&caHash={hash}";
            return await _httpClientService.GetAsync<GuardianResultDto>(url2);
        }
    }

    public async Task GetUserInfo(string chainId)
    {
        var onceCount = 500;
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
                    CaHash = holder.CaHash,
                    CaAddress = holder.CaAddress
                });
            }
        }

        Console.WriteLine($"chain {chainId}, holder count:{count}");
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