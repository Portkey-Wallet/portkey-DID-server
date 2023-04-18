using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.Tokens;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Validation;

namespace CAServer.CAActivity;

[RemoteService(false)]
[DisableAuditing]
public class UserActivityAppService : CAServerAppService, IUserActivityAppService, IValidationEnabled
{
    private readonly ILogger<UserActivityAppService> _logger;

    // private readonly IRepository<CAHolder, Guid> _cAHolderRepository;
    private readonly ITokenAppService _tokenAppService;
    private readonly IActivityProvider _activityProvider;

    public UserActivityAppService(ILogger<UserActivityAppService> logger, ITokenAppService tokenAppService, IActivityProvider activityProvider)
    {
        _logger = logger;
        _tokenAppService = tokenAppService;
        _activityProvider = activityProvider;
    }

    public async Task<List<GetActivitiesDto>> GetActivitiesAsync(GetActivitiesRequestDto request)
    {
        var filterTypes = FilterTypes(request.TransactionTypes);
        var transactions = await _activityProvider.GetActivitiesAsync(request.CaAddresses, request.ChainId, request.Symbol, filterTypes, request.SkipCount, request.MaxResultCount);
        return await IndexerTransaction2Dto(transactions, false);
    }

    private List<string> FilterTypes(IEnumerable<string> reqList)
    {
        if (reqList == null)
        {
            return ActivityConstants.DefaultTypes;
        }

        var ans = reqList.Where(e => ActivityConstants.AllSupportTypes.Contains(e)).ToList();

        return ans.Count == 0 ? ActivityConstants.DefaultTypes : ans;
    }


    public async Task<GetActivitiesDto> GetActivityAsync(GetActivityRequestDto request)
    {
        var res = await _activityProvider.GetActivityAsync(request.TransactionId, request.BlockHash);
        var val = await IndexerTransaction2Dto(res, true);
        return val == null || val.Count == 0 ? null : val[0];
    }

    private string GetIconByType(string transactionType)
    {
        return "fake_icon";
    }

    private async Task<string> GetFromWallet(string address)
    {
        return "fake_wallet_name";
        // try
        // {
        //     var holder = await _cAHolderRepository.FirstOrDefaultAsync(t => t.CaAddress == address);
        //     return holder?.NickName;
        // }
        // catch (Exception e)
        // {
        //     _logger.LogError(e, "get from wallet name failed, address={address}", address);
        //     throw e;
        // }
    }

    private async Task<string> GetToUserName(string address, string chainId)
    {
        //接收方用户名，从联系人获得
        return "fakeToUserName";
    }

    private async Task<List<GetActivitiesDto>> IndexerTransaction2Dto(IndexerTransactions indexerTransactions, bool needDetail)
    {
        if (indexerTransactions == null || indexerTransactions.CaHolderTransaction.Count == 0)
        {
            return new List<GetActivitiesDto>();
        }

        var getActivitiesDto = new List<GetActivitiesDto>();
        foreach (var ht in indexerTransactions.CaHolderTransaction)
        {
            var dto = ObjectMapper.Map<IndexerTransaction, GetActivitiesDto>(ht);
            var transactionTime = MsToDateTime(ht.Timestamp * 1000);
            if (ht.TransactionFees != null)
            {
                dto.TransactionFees = new List<TransactionFee>(ht.TransactionFees.Count);
                foreach (var tFee in ht.TransactionFees)
                {
                    var fee = new TransactionFee() { Symbol = tFee.Symbol, Fee = tFee.Amount };
                    if (tFee.Amount > 0)
                    {
                        var tPrice = await GetSymbolPrice(fee.Symbol, transactionTime);
                        fee.FeeInUsd = (tPrice.PriceInUsd * fee.Fee).ToString();
                    }

                    dto.TransactionFees.Add(fee);
                }
            }


            var price = await GetSymbolPrice(dto.Symbol, transactionTime);
            dto.PriceInUsd = price.PriceInUsd.ToString();
            if (needDetail && ht.TransferInfo != null)
            {
                dto.From = await GetFromWallet(ht.TransferInfo.FromAddress);
                dto.To = await GetToUserName(ht.TransferInfo.ToAddress, ht.TransferInfo.ToChainId);
            }

            getActivitiesDto.Add(dto);
        }

        return getActivitiesDto;
    }

    private DateTime MsToDateTime(long ms)
    {
        return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms).ToLocalTime();
    }

    private async Task<TokenPriceDataDto> GetSymbolPrice(string symbol, DateTime dateTime)
    {
        try
        {
            return await _tokenAppService.GetTokenHistoryPriceDataAsync(symbol, dateTime);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "get symbol price failed, symbol={symbol}datetime={dateTime}", symbol, dateTime);
            throw e;
        }
    }
}