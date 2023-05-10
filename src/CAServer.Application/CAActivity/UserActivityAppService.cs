using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace CAServer.CAActivity;

[RemoteService(false)]
[DisableAuditing]
public class UserActivityAppService : CAServerAppService, IUserActivityAppService
{
    private readonly ILogger<UserActivityAppService> _logger;
    private readonly ITokenAppService _tokenAppService;
    private readonly IActivityProvider _activityProvider;
    private readonly IUserContactProvider _userContactProvider;
    private readonly ActivitiesIcon _activitiesIcon;
    private readonly IImageProcessProvider _imageProcessProvider;

    public UserActivityAppService(ILogger<UserActivityAppService> logger, ITokenAppService tokenAppService,
        IActivityProvider activityProvider, IUserContactProvider userContactProvider,
        IOptions<ActivitiesIcon> activitiesIconOption, IImageProcessProvider imageProcessProvider)
    {
        _logger = logger;
        _tokenAppService = tokenAppService;
        _activityProvider = activityProvider;
        _userContactProvider = userContactProvider;
        _activitiesIcon = activitiesIconOption?.Value;
        _imageProcessProvider = imageProcessProvider;
    }

    public async Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto request)
    {
        try
        {
            var caAddressInfos = request.CaAddressInfos;
            if (caAddressInfos == null)
            {
                caAddressInfos = request.CaAddresses.Select(address => new CAAddressInfo { CaAddress = address })
                    .ToList();
            }

            var filterTypes = FilterTypes(request.TransactionTypes);
            var transactions = await _activityProvider.GetActivitiesAsync(caAddressInfos, request.ChainId,
                request.Symbol, filterTypes, request.SkipCount, request.MaxResultCount);
            return await IndexerTransaction2Dto(request.CaAddresses, transactions, request.ChainId, request.Width,
                request.Height);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetActivitiesAsync Error. {dto}", request);
            return new GetActivitiesDto { Data = new List<GetActivityDto>(), TotalRecordCount = 0 };
        }
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


    public async Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto request)
    {
        try
        {
            var indexerTransactions =
                await _activityProvider.GetActivityAsync(request.TransactionId, request.BlockHash);
            var activitiesDto = await IndexerTransaction2Dto(request.CaAddresses, indexerTransactions, null, 0, 0);
            if (activitiesDto == null || activitiesDto.TotalRecordCount == 0)
            {
                return new GetActivityDto();
            }

            var activityDto = activitiesDto.Data[0];

            if (!ActivityConstants.ContractTypes.Contains(activityDto.TransactionType))
            {
                await GetActivityName(request.CaAddresses, activityDto,
                    indexerTransactions.CaHolderTransaction.Data[0]);
            }

            return activityDto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetActivityAsync Error {request}", request);
            return new GetActivityDto();
        }
    }

    private async Task GetActivityName(List<string> addresses, GetActivityDto dto, IndexerTransaction transaction)
    {
        // cross chain transfer to self(such as from main to side)
        // first: transfer to manager.
        // second: manager to side address.
        var isCrossFirstTransfer = string.Equals(dto.TransactionType, "Transfer")
                                   && transaction.TransferInfo != null
                                   && string.Equals(transaction.FromAddress, transaction.TransferInfo.ToAddress)
                                   && string.Equals(transaction.TransferInfo.FromCAAddress,
                                       transaction.TransferInfo.FromAddress);
        var nickName = await _activityProvider.GetCaHolderNickName(CurrentUser.GetId());
        if (isCrossFirstTransfer)
        {
            dto.From = nickName;
            return;
        }

        var curUserIsFrom = addresses.Contains(dto.FromAddress);
        var anotherAddress = "";
        var chainId = string.Empty;
        if (curUserIsFrom)
        {
            dto.From = nickName;
            anotherAddress = transaction.TransferInfo?.ToAddress;
            chainId = transaction.TransferInfo?.ToChainId;
        }
        else
        {
            dto.To = nickName;
            anotherAddress = dto.FromAddress;
            chainId = transaction.ChainId;
        }

        var nameList =
            await _userContactProvider.BatchGetUserNameAsync(new List<string> { anotherAddress }, CurrentUser.GetId(),
                chainId);

        var contactName = nameList.FirstOrDefault()?.Item2;
        if (curUserIsFrom)
        {
            dto.To = contactName;
        }
        else
        {
            dto.From = contactName;
        }
    }

    private async Task<GetActivitiesDto> IndexerTransaction2Dto(List<string> caAddresses,
        IndexerTransactions indexerTransactions, [CanBeNull] string chainId, int weidth, int height)
    {
        var result = new GetActivitiesDto
        {
            Data = new List<GetActivityDto>(),
            TotalRecordCount = indexerTransactions?.CaHolderTransaction?.TotalRecordCount ?? 0
        };

        if (indexerTransactions?.CaHolderTransaction?.Data == null ||
            indexerTransactions.CaHolderTransaction.Data.Count == 0)
        {
            return result;
        }

        var getActivitiesDto = new List<GetActivityDto>();
        var dict = new Dictionary<string, string>();

        foreach (var ht in indexerTransactions.CaHolderTransaction.Data)
        {
            var dto = ObjectMapper.Map<IndexerTransaction, GetActivityDto>(ht);

            var transactionTime = MsToDateTime(ht.Timestamp * 1000);

            if (dto.Symbol != null)
            {
                var price = await GetTokenPriceAsync(dto.Symbol, transactionTime);
                dto.PriceInUsd = price.ToString();

                if (!dto.Decimals.IsNullOrWhiteSpace() && dto.Decimals != ActivityConstants.Zero &&
                    !dict.ContainsKey(dto.Symbol))
                {
                    dict.Add(dto.Symbol, dto.Decimals);
                }
            }

            if (ht.TransferInfo != null)
            {
                dto.IsReceived = caAddresses.Contains(dto.ToAddress);
                if (dto.IsReceived && caAddresses.Contains(dto.FromAddress))
                {
                    dto.IsReceived = false;
                    if (!chainId.IsNullOrEmpty())
                    {
                        dto.IsReceived = chainId == dto.ToChainId;
                    }
                }
                
                if (!ActivityConstants.ShowPriceTypes.Contains(dto.TransactionType))
                {
                    dto.IsDelegated = true;
                }
            }

            if (ht.TransactionFees != null)
            {
                dto.TransactionFees = new List<TransactionFee>(ht.TransactionFees.Count);

                foreach (var fee in ht.TransactionFees.Select(tFee => new TransactionFee()
                             { Symbol = tFee.Symbol, Fee = tFee.Amount }))
                {
                    if (!dict.ContainsKey(fee.Symbol))
                    {
                        var decimals = await _activityProvider.GetTokenDecimalsAsync(fee.Symbol);
                        dict.Add(fee.Symbol, decimals.TokenInfo.FirstOrDefault()?.Decimals.ToString());
                    }

                    fee.Decimals = dict[fee.Symbol];
                    dto.TransactionFees.Add(fee);
                }

                if (dto.TransactionFees.Count > 0)
                {
                    var symbols = dto.TransactionFees.Select(f => f.Symbol).ToList();
                    var priceList = await GetFeePriceListAsync(symbols, transactionTime);
                    for (var i = 0; i < symbols.Count; i++)
                    {
                        if (dto.TransactionFees[i].Fee > 0 && priceList[i] > 0)
                        {
                            dto.TransactionFees[i].FeeInUsd = CalculationHelper
                                .GetBalanceInUsd(priceList[i] * dto.TransactionFees[i].Fee,
                                    Convert.ToInt32(dto.TransactionFees[i].Decimals)).ToString();
                        }
                    }
                }
            }

            if (ht.NftInfo != null && !ht.NftInfo.Symbol.IsNullOrWhiteSpace())
            {
                dto.NftInfo = new NftDetail
                {
                    NftId = ht.NftInfo.Symbol.Split("-").Last(),
                    ImageUrl = _imageProcessProvider.GetResizeImage(ht.NftInfo.ImageUrl, weidth, height),
                    Alias = ht.NftInfo.TokenName
                };
            }
            //
            // dto.PriceInUsd = CalculationHelper
            //     .GetBalanceInUsd(Convert.ToDecimal(dto.PriceInUsd), Convert.ToInt32(dto.Decimals)).ToString();

            dto.ListIcon = GetIconByType(dto.TransactionType);
            getActivitiesDto.Add(dto);
        }

        result.Data = getActivitiesDto;

        return result;
    }

    private string GetIconByType(string transactionType)
    {
        string icon = string.Empty;
        if (ActivityConstants.ContractTypes.Contains(transactionType))
        {
            icon = _activitiesIcon.Contract;
        }
        else if (ActivityConstants.TransferTypes.Contains(transactionType))
        {
            icon = _activitiesIcon.Transfer;
        }

        return icon;
    }

    private DateTime MsToDateTime(long ms)
    {
        return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms).ToLocalTime();
    }

    private async Task<decimal> GetTokenPriceAsync(string symbol, DateTime time)
    {
        ListResultDto<TokenPriceDataDto> price;
        try
        {
            price = await _tokenAppService.GetTokenHistoryPriceDataAsync(new List<GetTokenHistoryPriceInput>
            {
                new()
                {
                    Symbol = symbol,
                    DateTime = time
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get transaction symbol price failed.");
            throw;
        }

        return price.Items.First().PriceInUsd;
    }

    private async Task<List<decimal>> GetFeePriceListAsync(IEnumerable<string> symbolList, DateTime time)
    {
        try
        {
            var input = symbolList.Select(s => new GetTokenHistoryPriceInput
            {
                Symbol = s,
                DateTime = time
            }).ToList();

            var priceInUsdList = await _tokenAppService.GetTokenHistoryPriceDataAsync(input);
            var priceList = priceInUsdList.Items.Select(p => p.PriceInUsd).ToList();
            return priceList;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get transaction fee price failed.");
            return new List<decimal>();
        }
    }
}