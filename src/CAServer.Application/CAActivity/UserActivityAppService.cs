using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IContractProvider _contractProvider;
    private readonly ChainOptions _chainOptions;
    private readonly ActivityOptions _activityOptions;
    private const int MaxResultCount = 10;
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly ActivityTypeOptions _activityTypeOptions;


    public UserActivityAppService(ILogger<UserActivityAppService> logger, ITokenAppService tokenAppService,
        IActivityProvider activityProvider, IUserContactProvider userContactProvider,
        IOptions<ActivitiesIcon> activitiesIconOption, IImageProcessProvider imageProcessProvider,
        IContractProvider contractProvider, IOptions<ChainOptions> chainOptions,
        IOptions<ActivityOptions> activityOptions, IUserAssetsProvider userAssetsProvider,
        IOptions<ActivityTypeOptions> activityTypeOptions)
    {
        _logger = logger;
        _tokenAppService = tokenAppService;
        _activityProvider = activityProvider;
        _userContactProvider = userContactProvider;
        _activitiesIcon = activitiesIconOption?.Value;
        _imageProcessProvider = imageProcessProvider;
        _contractProvider = contractProvider;
        _userAssetsProvider = userAssetsProvider;
        _chainOptions = chainOptions.Value;
        _activityOptions = activityOptions.Value;
        _activityTypeOptions = activityTypeOptions.Value;
    }


    public async Task<GetActivitiesDto> GetTwoCaTransactionsAsync(GetTwoCaTransactionRequestDto request)
    {
        // addresses of current user
        var caAddresses = request.CaAddressInfos.IsNullOrEmpty()
            ? new List<string>()
            : request.CaAddressInfos.Select(info => info.CaAddress).ToList();
        try
        {
            if (request.CaAddressInfos.IsNullOrEmpty() || request.TargetAddressInfos.IsNullOrEmpty())
            {
                throw new UserFriendlyException("Parameters “CaAddressInfos” “TargetAddressInfos” must be non-empty");
            }

            var twoCaAddress = new List<CAAddressInfo>() { request.CaAddressInfos[0], request.TargetAddressInfos[0] };
            var transactionsDto = await _activityProvider.GetTwoCaTransactionsAsync(twoCaAddress,
                request.Symbol, _activityTypeOptions.RecentTypes, request.SkipCount, request.MaxResultCount);

            var transactions = ObjectMapper.Map<TransactionsDto, IndexerTransactions>(transactionsDto);
            return await IndexerTransaction2Dto(caAddresses, transactions, request.ChainId, request.Width,
                request.Height, needMap: true);
        }
        catch (UserFriendlyException e)
        {
            throw e;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTwoCaTransactionsAsync error, addresses={addresses}",
                string.Join(",", caAddresses));
            throw new UserFriendlyException("Internal service error, place try again later.");
        }
    }

    public async Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto request)
    {
        try
        {
            var caAddresses = request.CaAddressInfos.Select(t => t.CaAddress).ToList();
            var transactions = await _activityProvider.GetActivitiesAsync(request.CaAddressInfos, request.ChainId,
                request.Symbol, null, request.SkipCount, request.MaxResultCount);
            return await IndexerTransaction2Dto(caAddresses, transactions, request.ChainId, request.Width,
                request.Height, needMap: true);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetActivitiesAsync Error. {dto}", request);
            return new GetActivitiesDto { Data = new List<GetActivityDto>(), TotalRecordCount = 0 };
        }
    }

    public async Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto request)
    {
        try
        {
            var caAddressInfos = new List<CAAddressInfo>();
            var caAddresses = request.CaAddresses;
            if (caAddresses.IsNullOrEmpty())
            {
                caAddresses = request.CaAddressInfos.Select(t => t.CaAddress).ToList();
            }
          
            if (request.ActivityType != CommonConstant.TransferCard)
            {
                caAddressInfos = request.CaAddressInfos.IsNullOrEmpty()
                    ? new List<CAAddressInfo>() : request.CaAddressInfos;
            }

            var indexerTransactions =
                await _activityProvider.GetActivityAsync(request.TransactionId, request.BlockHash, caAddressInfos);
            var activitiesDto =
                await IndexerTransaction2Dto(caAddresses, indexerTransactions, null, 0, 0, true);
            if (activitiesDto == null || activitiesDto.TotalRecordCount == 0)
            {
                return new GetActivityDto();
            }

            var activityDto = activitiesDto.Data[0];

            if (!_activityTypeOptions.ContractTypes.Contains(activityDto.TransactionType))
            {
                await GetActivityName(caAddresses, activityDto,
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

    public async Task<string> GetCaHolderCreateTimeAsync(GetUserCreateTimeRequestDto request)
    {
        var result = await _userAssetsProvider.GetCaHolderManagerInfoAsync(new List<string> { request.CaAddress });
        if (result == null || result.CaHolderManagerInfo.IsNullOrEmpty())
        {
            return string.Empty;
        }

        var caHash = result.CaHolderManagerInfo.First().CaHash;
        var caAddressInfos = new List<CAAddressInfo>();

        foreach (var chainInfo in _chainOptions.ChainInfos)
        {
            try
            {
                var output =
                    await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(caHash), null, chainInfo.Value.ChainId);
                caAddressInfos.Add(new CAAddressInfo
                {
                    ChainId = chainInfo.Key,
                    CaAddress = output.CaAddress.ToBase58()
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetCaHolderCreateTimeAsync Error {caAddress}", request.CaAddress);
            }
        }

        if (caAddressInfos.Count == 0)
        {
            _logger.LogDebug("No caAddressInfos found. CaAddress is {CaAddress}", request.CaAddress);
            return string.Empty;
        }


        var filterTypes = new List<string>
        {
            "CreateCAHolder"
        };
        var transactions = await _activityProvider.GetActivitiesAsync(caAddressInfos, string.Empty,
            string.Empty, filterTypes, 0, MaxResultCount);

        if (transactions.CaHolderTransaction.Data.Count == 0)
        {
            return string.Empty;
        }

        var data = transactions.CaHolderTransaction.Data;
        foreach (var indexerTransaction in data.Where(indexerTransaction => indexerTransaction.Timestamp > 0))
        {
            return indexerTransaction.Timestamp.ToString();
        }

        return string.Empty;
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
        var curUserIsTo = transaction.TransferInfo != null && addresses.Contains(transaction.TransferInfo?.ToAddress);
        var anotherAddresses = new List<string>();

        var chainId = string.Empty;
        if (curUserIsFrom)
        {
            dto.From = nickName;
            anotherAddresses.Add(transaction.TransferInfo?.ToAddress);
            chainId = transaction.TransferInfo?.ToChainId;
        }

        if (curUserIsTo)
        {
            dto.To = nickName;
            anotherAddresses.Add(dto.FromAddress);
            chainId = transaction.ChainId;
        }

        if (!curUserIsFrom && !curUserIsTo)
        {
            anotherAddresses.Add(transaction.TransferInfo?.ToAddress);
            anotherAddresses.Add(dto.FromAddress);
        }

        var nameList =
            await _userContactProvider.BatchGetUserNameAsync(anotherAddresses, CurrentUser.GetId(),
                chainId);
        if (!curUserIsFrom && !curUserIsTo)
        {
            anotherAddresses.Add(transaction.TransferInfo?.ToAddress);
            anotherAddresses.Add(dto.FromAddress);
            dto.From = nameList.FirstOrDefault(t => t.Item1?.Address == dto.FromAddress)?.Item2;
            dto.To =  nameList.FirstOrDefault(t => t.Item1?.Address == transaction.TransferInfo?.ToAddress)?.Item2;
            return;
        }

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
        IndexerTransactions indexerTransactions, [CanBeNull] string chainId, int weidth, int height,
        bool needMap = false)
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
            if (_activityTypeOptions.NoShowTypes.Contains(dto.TransactionType))
            {
                result.TotalRecordCount -= 1;
                continue;
            }

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

                        //this means this Transation fee is pay by manager
                        var activityOptions =
                            _activityOptions.ActivityTransactionFeeFix?.Where(x => x.ChainId == ht.ChainId).ToList()
                                .FirstOrDefault();
                        if (activityOptions?.StartBlock > 0)
                        {
                            if (ht.IsManagerConsumer && ht.BlockHeight > activityOptions.StartBlock)
                            {
                                dto.TransactionFees[i].FeeInUsd = "0";
                                dto.TransactionFees[i].Fee = 0;
                            }
                        }
                    }
                }
            }

            if (ht.NftInfo != null && !ht.NftInfo.Symbol.IsNullOrWhiteSpace())
            {
                dto.NftInfo = new NftDetail
                {
                    NftId = ht.NftInfo.Symbol.Split("-").Last(),

                    ImageUrl = await _imageProcessProvider.GetResizeImageAsync(ht.NftInfo.ImageUrl, weidth, height,
                        ImageResizeType.Forest),
                    Alias = ht.NftInfo.TokenName
                };
            }

            dto.ListIcon = GetIconByType(dto.TransactionType);
            if (!_activityTypeOptions.ShowPriceTypes.Contains(dto.TransactionType))
            {
                dto.IsDelegated = true;
            }

            if (needMap)
            {
                await MapMethodNameAsync(caAddresses, dto);
            }

            getActivitiesDto.Add(dto);
        }

        result.Data = getActivitiesDto;

        return result;
    }

    private async Task MapMethodNameAsync(List<string> caAddresses, GetActivityDto activityDto)
    {
        var typeName =
            _activityTypeOptions.TypeMap.GetValueOrDefault(activityDto.TransactionType, activityDto.TransactionType);
        if (activityDto.TransactionType == ActivityConstants.AddGuardianName)
        {
            var guardian = await _activityProvider.GetCaHolderInfoAsync(caAddresses, string.Empty);
            var holderInfo = guardian?.CaHolderInfo?.FirstOrDefault();
            if (holderInfo?.OriginChainId != null && holderInfo?.OriginChainId != activityDto.FromChainId)
            {
                activityDto.TransactionName = ActivityConstants.NotRegisterChainAddGuardianName;
            }
            else
            {
                activityDto.TransactionName = typeName;
            }

            return;
        }

        if (activityDto.TransactionType == ActivityConstants.TransferName && _activityOptions.ETransferConfigs != null)
        {
            var eTransferConfig =
                _activityOptions.ETransferConfigs.FirstOrDefault(e => e.ChainId == activityDto.FromChainId);
            if (eTransferConfig != null && eTransferConfig.Accounts.Contains(activityDto.FromAddress))
            {
                activityDto.TransactionName = ActivityConstants.DepositName;
                return;
            }
        }

        activityDto.TransactionName = activityDto.NftInfo != null &&
                                      !string.IsNullOrWhiteSpace(activityDto.NftInfo.NftId) &&
                                      _activityTypeOptions.ShowNftTypes.Contains(activityDto.TransactionType)
            ? typeName + " NFT"
            : typeName;
        activityDto.TransactionType =
            _activityTypeOptions.TransactionTypeMap.GetValueOrDefault(activityDto.TransactionType,
                activityDto.TransactionType);
    }

    private string GetIconByType(string transactionType)
    {
        string icon = string.Empty;
        if (_activityTypeOptions.ContractTypes.Contains(transactionType))
        {
            icon = _activitiesIcon.Contract;
        }
        else if (_activityTypeOptions.TransferTypes.Contains(transactionType))
        {
            icon = _activitiesIcon.Transfer;
        }
        else if (_activityTypeOptions.RedPacketTypes.Contains(transactionType))
        {
            icon = _activitiesIcon.RedPacket;
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