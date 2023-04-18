using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Entities.Etos;
using CAServer.Grains.Grain.Tokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Tokens;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class UserTokenAppService : CAServerAppService, IUserTokenAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly TokenListOptions _tokenListOptions;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<UserTokenIndex, Guid> _userTokenIndexRepository;

    public UserTokenAppService(
        IClusterClient clusterClient, 
        IOptionsSnapshot<TokenListOptions> tokenListOptions,
        IDistributedEventBus distributedEventBus,
        INESTRepository<UserTokenIndex, Guid> userTokenIndexRepository)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _userTokenIndexRepository = userTokenIndexRepository;
        _tokenListOptions = tokenListOptions.Value;
    }


    public async Task<PagedResultDto<UserTokenDto>> GetUserTokenListAsync(GetUserTokenListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserTokenIndex>, QueryContainer>>();
        
        if (!input.Filter.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q=>q.Wildcard(i=>i.Field(t=>t.Token.Symbol).Value(input.Filter)));
        }
        mustQuery.Add(q=>q.Term(i=>i.Field(t=>t.UserId).Value(CurrentUser.Id)));
        
        mustQuery.Add(q=>q.Term(i=>i.Field(t=>t.IsDisplay).Value(input.IsDisplay)));
        QueryContainer Filter(QueryContainerDescriptor<UserTokenIndex> f) => f.Bool(b => b.Must(mustQuery));
        Func<SortDescriptor<UserTokenIndex>, IPromise<IList<ISort>>> sort = s =>
            s.Descending(a => a.SortWeight).Ascending(d => d.Token.Symbol).Ascending(d=>d.Token.ChainId);
        
        var tokenList = await _userTokenIndexRepository.GetSortListAsync(Filter,sortFunc:sort,limit: input.MaxResultCount,
            skip: input.SkipCount);
        var totalCount = await _userTokenIndexRepository.CountAsync(Filter);
        
        var resultList = ObjectMapper.Map<List<UserTokenIndex>,List<UserTokenDto>>(tokenList.Item2);
        return new PagedResultDto<UserTokenDto>
        {
            TotalCount = totalCount.Count,
            Items = resultList
        };
    }

    public async Task ChangeTokenDisplayAsync(bool isDisplay, Guid id)
    {
        var grain = _clusterClient.GetGrain<IUserTokenGrain>(id);
        await grain.ChangeTokenDisplayAsync(isDisplay);
    }

    public async Task AddUserTokenAsync(Guid userId)
    {
        try
        {
            foreach (var userTokenItem in _tokenListOptions.Token)
            {
                var grain = _clusterClient.GetGrain<IUserTokenGrain>(GuidGenerator.Create());
                var tokenItem = ObjectMapper.Map<UserTokenItem, Token>(userTokenItem);
                var token = await grain.AddUserTokenAsync(userId,tokenItem);
                await _distributedEventBus.PublishAsync(new UserTokenEto
                {
                    Id = token.Id,
                    IsDefault = token.IsDefault,
                    IsDisplay = token.IsDisplay,
                    UserId = token.UserId,
                    SortWeight = token.SortWeight,
                    Token = token.Token
                });
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to add token. Error:{e.Message}");
            throw;
        }
        
    }

    //
    // public async Task<ListResultDto<TokenDto>> GetTokenListAsync()
    // {
    //     var tokenList = await _tokenRepository.GetListAsync();
    //     var tokenListByChainId = tokenList.GroupBy(t => t.ChainId).ToList();
    //     var result = (from token in tokenListByChainId
    //         let tokenItemList =
    //             token.Select(t => new TokenItem
    //                 {Address = t.Address, Symbol = t.Symbol, Decimal = t.Decimal, PriceInUsd = t.PriceInUsd}).ToList()
    //         select new TokenDto {ChainId = token.Key, Tokens = tokenItemList}).ToList();
    //     return new ListResultDto<TokenDto>
    //     {
    //         Items = result
    //     };
    // }
    //
    // public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols)
    // {
    //     if (symbols.Count == 0)
    //     {
    //         return new ListResultDto<TokenPriceDataDto>();
    //     }
    //
    //     var queryable = await _tokenRepository.GetQueryableAsync();
    //     var result = (from tokenSymbol in symbols
    //         select queryable.FirstOrDefault(i => i.Symbol.Contains(tokenSymbol))
    //         into token
    //         where token != null
    //         select new TokenPriceDataDto {Symbol = token.Symbol, PriceInUsd = token.PriceInUsd}).ToList();
    //
    //     return new ListResultDto<TokenPriceDataDto>
    //     {
    //         Items = result
    //     };
    // }
    //
    // public async Task<TokenPriceDataDto> GetTokenHistoryPriceDataAsync(string symbol, DateTime dateTime)
    // {
    //     TokenPriceData priceData;
    //     try
    //     {
    //         var priceTime = dateTime.Date;
    //
    //         priceData =
    //             await _tokenPriceRepository.FindAsync(o => o.Symbol == symbol && o.Timestamp == priceTime);
    //         if (priceData == null)
    //         {
    //             var grain = _clusterClient.GetGrain<ITokenPriceGrain>(GuidGenerator.Create());
    //             var tokenPriceData = await grain.GetHistoryPriceAsync(symbol, dateTime);
    //             priceData = ObjectMapper.Map<TokenPriceDataDto, TokenPriceData>(tokenPriceData);
    //             await _tokenPriceRepository.InsertAsync(priceData, true);
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         Logger.LogError($"Get Token History Price failed. Error message:{e.Message}");
    //         throw;
    //     }
    //     return ObjectMapper.Map<TokenPriceData, TokenPriceDataDto>(priceData);
    // }
    //
    // public async Task UpdateTokenPriceUsdAsync()
    // {
    //     try
    //     {
    //         var tokenList = await _tokenRepository.GetListAsync();
    //         foreach (var token in tokenList)
    //         {
    //             var grain = _clusterClient.GetGrain<ITokenPriceGrain>(token.Id);
    //             var priceData = await grain.GetCurrentPriceAsync(token.Symbol);
    //             token.PriceInUsd = priceData.PriceInUsd;
    //             token.PriceUpdateTime = DateTime.UtcNow;
    //             await _tokenRepository.UpdateAsync(token, true);
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         Logger.LogError($"Update Token Price failed. Error message:{e.Message}");
    //         throw;
    //     }
    // }
    //
    // public async Task InitialToken()
    // {
    //     try
    //     {
    //         var toAdd = new List<Token>();
    //         var token = new Token
    //         {
    //             ChainId = 9992731,
    //             Symbol = "ELF",
    //             Decimal = 8,
    //             Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE"
    //         };
    //         toAdd.Add(token);
    //         var tokenSide = new Token
    //         {
    //             ChainId = 1866392,
    //             Symbol = "ELF",
    //             Decimal = 8,
    //             Address = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx"
    //         };
    //         toAdd.Add(tokenSide);
    //         var symbolList = new List<string>
    //         {
    //             "CPU","RAM","DISK","NET","READ","WRITE","STORAGE","TRAFFIC"
    //         };
    //         foreach (var symbol in symbolList)
    //         {
    //             var tokenResource = new Token
    //             {
    //                 ChainId = 9992731,
    //                 Symbol = symbol,
    //                 Decimal = 8,
    //                 Address = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE"
    //             };
    //             toAdd.Add(tokenResource);
    //             var tokenResourceSide = new Token
    //             {
    //                 ChainId = 1866392,
    //                 Symbol = symbol,
    //                 Decimal = 8,
    //                 Address = "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx"
    //             };
    //            toAdd.Add(tokenResourceSide);
    //         }
    //
    //         await _tokenRepository.InsertManyAsync(toAdd);
    //     }
    //     catch (Exception e)
    //     {
    //         Logger.LogError($"Failed to create token.Error:{e.Message}");
    //         throw;
    //     }
    // }


    
}