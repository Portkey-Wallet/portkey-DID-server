using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CAServer.CAActivity.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Entities.Es;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace CAServer.CAActivity;

public class UserActivityAppServiceTests : CAServerApplicationTestBase
{
    private readonly IUserActivityAppService _userActivityAppService;
    private readonly IActivityProvider _activityProvider;
    private readonly IGraphQLHelper _graphQlHelper;

    public UserActivityAppServiceTests()
    {
        _graphQlHelper = GetRequiredService<IGraphQLHelper>();
        _activityProvider = GetRequiredService<IActivityProvider>();
        // _cAHolderRepository = GetRequiredService<IRepository<CAHolder, Guid>>();
        // _userActivityAppService = GetRequiredService<UserActivityAppService>();
    }


    [Fact]
    public async Task TestGrapQL()
    {
        string _address = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        string _chainId = "AELF";
        string _symbol = null;
        int inputSkipCount = 0;
        int inputMaxResultCount = 10;

        try
        {
            var request = new GetActivitiesRequestDto();
            request.CaAddresses = new List<string>() { _address };
            request.ChainId = _chainId;
            request.Symbol = _symbol;
            request.TransactionTypes = ActivityConstants.DefaultTypes;
            request.SkipCount = inputSkipCount;
            request.MaxResultCount = inputMaxResultCount;
            var ans = _userActivityAppService.GetActivitiesAsync(request);
            Console.WriteLine(ans);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [Fact]
    public async Task TestGrapQL3()
    {
        string _address = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        string _chainId = "AELF";
        string _symbol = null;
        int inputSkipCount = 0;
        int inputMaxResultCount = 10;
        try
        {
            var ans = await _activityProvider.GetActivitiesAsync(new List<string>() { _address }, _chainId, _symbol, ActivityConstants.DefaultTypes, inputSkipCount, inputMaxResultCount);
            Console.WriteLine(ans.CaHolderTransaction);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }


    [Fact]
    public async Task TestGrapQL2()
    {
        using var graphQLClient = new GraphQLHttpClient("http://192.168.66.87:8083/AElfIndexer_DApp/PortKeyIndexerCASchema/graphql", new NewtonsoftJsonSerializer());

        string _address = "YUW9zH5GhRboT5JK4vXp5BLAfCDv28rRmTQwo418FuaJmkSg8";
        string _chainId = "AELF";
        string _symbol = null;
        int inputSkipCount = 0;
        int inputMaxResultCount = 10;

        var req = new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$symbol:String,$caAddresses:[String],$methodNames: [String],$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,symbol:$symbol,caAddresses:$caAddresses,methodNames:$methodNames,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        id, chainId, blockHash, blockHeight, previousBlockHash, transactionId, methodName, tokenInfo{symbol,decimals} nftInfo{url,alias,nftId} status, timestamp, transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId} fromAddress, transactionFees{symbol,amount}
                    }
                }",
            Variables = new
            {
                caAddresses = new List<string>() { }, chainId = _chainId, symbol = _symbol, methodNames = ActivityConstants.DefaultTypes, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
            }
        };

        var graphQLResponse = await graphQLClient.SendQueryAsync<IndexerTransactions>(req);
        Console.WriteLine($"{0}", req);
        Console.WriteLine($"{1}", graphQLResponse);
    }

    [Fact]
    public async Task TestLog()
    {
        GraphQLError[]? errors = { new() { Message = "message", Path = new ErrorPath() } };
        var list = errors.Select(err => err.Message).ToList();
        var errInfo = string.Join(",", list);
        Debug.Assert(errInfo.Equals("message"));
    }
}