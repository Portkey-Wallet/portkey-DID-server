using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using CAServer.CAActivity.Provider;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Commons.Etos;
using CAServer.CryptoGift;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Guardian.Provider;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using CAServer.Tokens.TokenPrice;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
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
    private readonly IpfsOptions _ipfsOptions;
    private readonly IAssetsLibraryProvider _assetsLibraryProvider;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly TokenSpenderOptions _tokenSpenderOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageIndexRepository;
    private readonly ActivitiesStatusIconOptions _activitiesStatus;
    private readonly ActivitiesSourceIconOptions _activitiesSource;

    public UserActivityAppService(ILogger<UserActivityAppService> logger, ITokenAppService tokenAppService,
        IActivityProvider activityProvider, IUserContactProvider userContactProvider,
        IOptionsSnapshot<ActivitiesIcon> activitiesIconOption, IImageProcessProvider imageProcessProvider,
        IContractProvider contractProvider, IOptionsSnapshot<ChainOptions> chainOptions,
        IOptionsSnapshot<ActivityOptions> activityOptions, IUserAssetsProvider userAssetsProvider,
        IOptionsSnapshot<ActivityTypeOptions> activityTypeOptions, IOptionsSnapshot<IpfsOptions> ipfsOptions,
        IAssetsLibraryProvider assetsLibraryProvider, ITokenPriceService tokenPriceService,
        IOptionsMonitor<TokenSpenderOptions> tokenSpenderOptions, IHttpContextAccessor httpContextAccessor,
        INESTRepository<RedPackageIndex, Guid> redPackageIndexRepository,
        IOptions<ActivitiesStatusIconOptions> activitiesStatus,
        IOptions<ActivitiesSourceIconOptions> activitiesSource)
    {
        _logger = logger;
        _tokenAppService = tokenAppService;
        _activityProvider = activityProvider;
        _userContactProvider = userContactProvider;
        _activitiesIcon = activitiesIconOption?.Value;
        _imageProcessProvider = imageProcessProvider;
        _contractProvider = contractProvider;
        _userAssetsProvider = userAssetsProvider;
        _assetsLibraryProvider = assetsLibraryProvider;
        _chainOptions = chainOptions.Value;
        _activityOptions = activityOptions.Value;
        _activityTypeOptions = activityTypeOptions.Value;
        _ipfsOptions = ipfsOptions.Value;
        _tokenPriceService = tokenPriceService;
        _httpContextAccessor = httpContextAccessor;
        _tokenSpenderOptions = tokenSpenderOptions.CurrentValue;
        _redPackageIndexRepository = redPackageIndexRepository;
        _activitiesStatus = activitiesStatus.Value;
        _activitiesSource = activitiesSource.Value;
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
            var transactionInfos = await GetTransactionInfosAsync(request);
            var activitiesDto = await IndexerTransaction2Dto(caAddresses, transactionInfos.transactions,
                request.ChainId,
                request.Width,
                request.Height, needMap: true);

            SetSeedStatusAndTypeForActivityDtoList(activitiesDto.Data);

            OptimizeSeedAliasDisplay(activitiesDto.Data);

            TryUpdateImageUrlForActivityDtoList(activitiesDto.Data);

            activitiesDto.HasNextPage = transactionInfos.haxNextPage;
            return activitiesDto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetActivitiesAsync Error. {dto}", JsonConvert.SerializeObject(request));
            return new GetActivitiesDto { Data = new List<GetActivityDto>(), TotalRecordCount = 0 };
        }
    }

    private async Task<(IndexerTransactions transactions, bool haxNextPage)> GetTransactionInfosAsync(
        GetActivitiesRequestDto request)
    {
        var transactions = new IndexerTransactions
        {
            CaHolderTransaction = new CaHolderTransaction()
        };

        var transactionsInfo = await GetTransactionsAsync(request);
        if (transactionsInfo.data.IsNullOrEmpty())
        {
            return (transactions, false);
        }

        transactions.CaHolderTransaction.Data = transactionsInfo.data;
        transactions.CaHolderTransaction.TotalRecordCount = transactionsInfo.totalCount;
        await InsertNotSuccessAsync(request, transactionsInfo.data, transactions);

        return (transactions, transactionsInfo.hasNextPage);
    }

    private async Task InsertNotSuccessAsync(GetActivitiesRequestDto request,
        List<IndexerTransaction> transactionsInfo, IndexerTransactions transactions)
    {
        var version = _httpContextAccessor.HttpContext?.Request.Headers["version"].ToString();
        if (!VersionContentHelper.CompareVersion(version, CommonConstant.ActivitiesStartVersion))
        {
            return;
        }

        var notSuccessList = await _activityProvider.GetNotSuccessTransactionsAsync(
            request.CaAddressInfos.FirstOrDefault()?.CaAddress ?? "-",
            transactionsInfo.Min(t => t.BlockHeight),
            transactionsInfo.Max(t => t.BlockHeight));

        foreach (var item in ObjectMapper
                     .Map<List<CaHolderTransactionIndex>, List<IndexerTransaction>>(notSuccessList))
        {
            transactions.CaHolderTransaction.Data.InsertAfter(
                t => t.ChainId == item.ChainId && t.BlockHeight >= item.BlockHeight, item);
        }
    }

    private async Task<(List<IndexerTransaction> data, long totalCount, bool hasNextPage)> GetTransactionsAsync(
        GetActivitiesRequestDto request)
    {
        var hasNextPage = true;
        var transactions = await _activityProvider.GetActivitiesAsync(request.CaAddressInfos, request.ChainId,
            request.Symbol, null, request.SkipCount, request.MaxResultCount);
        if (transactions.CaHolderTransaction.Data.Count < request.MaxResultCount)
        {
            hasNextPage = false;
        }

        var crossChainTransactions = transactions.CaHolderTransaction.Data
            .Where(t => t.MethodName == CommonConstant.CrossChainTransferMethodName).ToList();

        if (!crossChainTransactions.IsNullOrEmpty())
        {
            var transactionIds = crossChainTransactions.Select(t => t.TransactionId).ToList();
            var transactionsDto = await _activityProvider.GetAutoReceiveTransactionsAsync(transactionIds,
                inputMaxResultCount: transactionIds.Count);

            var completedIds = transactionsDto?.AutoReceiveTransaction?.Data?
                .Select(t => t.TransferInfo.TransferTransactionId).ToList();

            transactions.CaHolderTransaction.Data
                .RemoveAll(t => t.MethodName == CommonConstant.CrossChainTransferMethodName &&
                                !completedIds.Contains(t.TransactionId));
        }

        //filter transaction for accelerated registration and accelerated recovery
        var exists = transactions.CaHolderTransaction?.Data?.Exists(t =>
            t.MethodName == AElfContractMethodName.SocialRecovery);
        string originChainId = null;
        if (exists ?? false)
        {
            var caAddress = request.CaAddressInfos.Select(t => t.CaAddress).ToList();
            var guardian = await _activityProvider.GetCaHolderInfoAsync(caAddress, string.Empty);
            var holderInfo = guardian?.CaHolderInfo?.FirstOrDefault();
            originChainId = holderInfo?.OriginChainId ?? "AELF";
        }

        transactions?.CaHolderTransaction?.Data?.RemoveAll(t =>
            t.MethodName == AElfContractMethodName.CreateCAHolderOnNonCreateChain ||
            (t.MethodName == AElfContractMethodName.SocialRecovery && originChainId != t.ChainId));

        transactions?.CaHolderTransaction?.Data?
            .RemoveAll(t => _activityTypeOptions.NoShowTypes.Contains(t.MethodName));

        return (transactions.CaHolderTransaction.Data, transactions.CaHolderTransaction.TotalRecordCount, hasNextPage);
    }

    private void SetDAppInfo(string toContractAddress, GetActivityDto activityDto, string fromAddress,
        string methodName)
    {
        if (activityDto.TransactionType == ActivityConstants.SwapExactTokensForTokensName &&
            _activityOptions.ETransferConfigs.SelectMany(t => t.Accounts).Contains(fromAddress))
        {
            var eTransferConfig = _activityOptions.ETransferConfigs.FirstOrDefault();
            toContractAddress = eTransferConfig?.ContractAddress;
        }

        if (IsETransfer(activityDto.TransactionType, activityDto.FromChainId, activityDto.FromAddress))
        {
            var eTransferConfig = _activityOptions.ETransferConfigs.FirstOrDefault();
            toContractAddress = eTransferConfig?.ContractAddress;
        }

        if (methodName == ActivityConstants.FreeMintNftName)
        {
            activityDto.FromAddress = toContractAddress;
        }

        if (toContractAddress.IsNullOrEmpty())
        {
            return;
        }

        var contractConfig =
            _activityOptions.ContractConfigs.FirstOrDefault(t => t.ContractAddress == toContractAddress);

        if (contractConfig != null && !contractConfig.DappName.IsNullOrEmpty())
        {
            activityDto.DappName = contractConfig.DappName;
            activityDto.DappIcon = contractConfig.DappIcon;
            return;
        }

        var tokenSpender =
            _tokenSpenderOptions.TokenSpenderList.FirstOrDefault(t => t.ContractAddress == toContractAddress);
        if (tokenSpender == null)
        {
            activityDto.DappName = _activityOptions.UnknownConfig.NotUnknownContracts.Contains(toContractAddress)
                ? string.Empty
                : _activityOptions.UnknownConfig.UnknownName;
            activityDto.DappIcon = activityDto.DappName == _activityOptions.UnknownConfig.UnknownName
                ? _activityOptions.UnknownConfig.UnknownIcon
                : activityDto.DappIcon;
            return;
        }

        activityDto.DappName = tokenSpender.Name;
        activityDto.DappIcon = tokenSpender.Icon;
    }

    private void HandleTokenTransferInfos(IndexerTransaction indexerTransactionDto,
        string address)
    {
        if (indexerTransactionDto.TokenTransferInfos.IsNullOrEmpty() ||
            indexerTransactionDto.TokenTransferInfos.Count == 1)
        {
            return;
        }

        var transfers = new List<TokenTransferInfo>();
        foreach (var item in indexerTransactionDto.TokenTransferInfos)
        {
            if (item.TransferInfo.FromAddress == address || item.TransferInfo.ToAddress == address)
            {
                transfers.Add(item);
            }
        }

        indexerTransactionDto.TokenTransferInfos = transfers;

        if (transfers.Count == 1)
        {
            indexerTransactionDto.TokenInfo = transfers.First()?.TokenInfo;
            indexerTransactionDto.NftInfo = transfers.First()?.NftInfo;
            indexerTransactionDto.TransferInfo = transfers.First()?.TransferInfo;
        }

        if (!_activityTypeOptions.MergeTokenBalanceTypes.Contains(indexerTransactionDto.MethodName))
        {
            return;
        }

        var fromAddress = string.Empty;
        var toAddress = string.Empty;
        var transferInfos = indexerTransactionDto.TokenTransferInfos.Select(t => t.TransferInfo).ToList();
        var fromCaAddresses = transferInfos.Select(t => t.FromCAAddress).Distinct().ToList();
        var toAddresses = transferInfos.Select(t => t.ToAddress).Distinct().ToList();
        if (fromCaAddresses.Count() == 1 && fromCaAddresses.First() == address)
        {
            fromAddress = address;
        }
        else if (toAddresses.Count() == 1 && toAddresses.First() == address)
        {
            toAddress = address;
        }
        else
        {
            return;
        }

        _logger.LogInformation("need to merge, methodName:{methodName}", indexerTransactionDto.MethodName);
        var tokenInfos = indexerTransactionDto.TokenTransferInfos.Where(t => t.TokenInfo != null)
            .Select(t => t.TokenInfo).ToList();
        var nftInfos = indexerTransactionDto.TokenTransferInfos.Where(t => t.NftInfo != null).Select(t => t.NftInfo)
            .ToList();
        var symbols = tokenInfos.Select(t => t.Symbol).Distinct().ToList();
        if (!nftInfos.IsNullOrEmpty() && nftInfos.Count > 0 && !symbols.IsNullOrEmpty() && symbols.Count > 0)
        {
            return;
        }

        var nftSymbols = nftInfos.Select(t => t.Symbol).Distinct().ToList();
        if ((symbols.IsNullOrEmpty() || symbols.Count() > 1) && (nftSymbols.IsNullOrEmpty() || nftSymbols.Count > 1))
        {
            return;
        }

        if (nftSymbols.Count == 1)
        {
            indexerTransactionDto.NftInfo = nftInfos.First();
        }
        else
        {
            indexerTransactionDto.TokenInfo = tokenInfos.First();
        }

        indexerTransactionDto.FromAddress = fromAddress;
        var amount = indexerTransactionDto.TokenTransferInfos.Select(t => t.TransferInfo).ToList().Sum(f => f.Amount);
        indexerTransactionDto.TransferInfo = new TransferInfo()
        {
            FromAddress = fromAddress,
            FromCAAddress = fromAddress,
            ToAddress = toAddress,
            Amount = amount
        };

        indexerTransactionDto.TokenTransferInfos.Clear();
        _logger.LogInformation("end need to merge, count:{count}", indexerTransactionDto.TokenTransferInfos.Count);
    }

    private async Task SetOperationsAsync(IndexerTransaction indexerTransactionDto, GetActivityDto activityDto,
        List<string> caAddresses, string chainId, int width, int height)
    {
        if (indexerTransactionDto.TokenTransferInfos is not { Count: > 1 })
        {
            return;
        }

        foreach (var item in indexerTransactionDto.TokenTransferInfos)
        {
            if (!caAddresses.Contains(item.TransferInfo.FromAddress) &&
                !caAddresses.Contains(item.TransferInfo.ToAddress))
                continue;

            var operationInfo = new OperationItemInfo();
            operationInfo.Amount = item.TransferInfo.Amount.ToString();

            operationInfo.IsReceived = caAddresses.Contains(item.TransferInfo.ToAddress);
            if (operationInfo.IsReceived && caAddresses.Contains(item.TransferInfo.FromAddress))
            {
                operationInfo.IsReceived = false;
                if (!chainId.IsNullOrEmpty())
                {
                    operationInfo.IsReceived = chainId == item.TransferInfo.ToChainId;
                }
            }

            if (item.TokenInfo != null)
            {
                operationInfo.Decimals = item.TokenInfo.Decimals.ToString();
                operationInfo.Symbol = item.TokenInfo.Symbol;
                operationInfo.Icon = _assetsLibraryProvider.buildSymbolImageUrl(item.TokenInfo.Symbol);
            }

            if (item.NftInfo != null && !item.NftInfo.Symbol.IsNullOrWhiteSpace())
            {
                operationInfo.NftInfo = new NftDetail
                {
                    NftId = item.NftInfo.Symbol.Split("-").Last(),

                    ImageUrl = await _imageProcessProvider.GetResizeImageAsync(item.NftInfo.ImageUrl, width, height,
                        ImageResizeType.Forest),
                    Alias = item.NftInfo.TokenName
                };
                operationInfo.Decimals = item.NftInfo.Decimals.ToString();
                operationInfo.Symbol = item.NftInfo.Symbol;
            }

            activityDto.Operations.Add(operationInfo);
        }

        MergeOperations(activityDto);
    }

    private void MergeOperations(GetActivityDto activityDto)
    {
        if (activityDto.Operations.IsNullOrEmpty()) return;

        var operations = activityDto.Operations;
        var mergedOperations = new List<OperationItemInfo>();
        var symbols = activityDto.Operations.Select(t => t.Symbol);

        foreach (var symbol in symbols)
        {
            var income = operations.FirstOrDefault(t => t.Symbol == symbol && t.IsReceived);
            var outcome = operations.FirstOrDefault(t => t.Symbol == symbol && !t.IsReceived);
            if (income != null && !mergedOperations.Exists(t => t.Symbol == symbol && t.IsReceived))
            {
                income.Amount = operations.Where(t => t.Symbol == symbol && t.IsReceived)
                    .Sum(t => Convert.ToInt64(t.Amount)).ToString();
                mergedOperations.Add(income);
            }

            if (outcome != null && !mergedOperations.Exists(t => t.Symbol == symbol && !t.IsReceived))
            {
                outcome.Amount = operations.Where(t => t.Symbol == symbol && !t.IsReceived)
                    .Sum(t => Convert.ToInt64(t.Amount)).ToString();
                mergedOperations.Add(outcome);
            }
        }

        if (mergedOperations.Count == 1)
        {
            var operation = mergedOperations.First();
            activityDto.Symbol = operation.Symbol;
            activityDto.IsReceived = operation.IsReceived;
            activityDto.Amount = operation.Amount;
            activityDto.Decimals = operation.Decimals;
            activityDto.ListIcon = operation.Icon;
            activityDto.NftInfo = operation.NftInfo;
            activityDto.ToChainId = activityDto.ToChainId.IsNullOrEmpty()
                ? activityDto.FromChainId
                : activityDto.ToChainId;
            mergedOperations.Clear();
        }

        activityDto.Operations = mergedOperations;
    }

    public async Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto request)
    {
        try
        {
            var caAddressInfos = new List<CAAddressInfo>();
            var addressInfo = request.CaAddressInfos?.FirstOrDefault();
            var chainId = string.Empty;
            if (addressInfo != null)
            {
                chainId = addressInfo.ChainId;
            }

            var caAddresses = request.CaAddresses;
            if (caAddresses.IsNullOrEmpty())
            {
                caAddresses = request.CaAddressInfos.Select(t => t.CaAddress).ToList();
            }

            if (request.ActivityType != CommonConstant.TransferCard)
            {
                caAddressInfos = request.CaAddressInfos.IsNullOrEmpty()
                    ? new List<CAAddressInfo>()
                    : request.CaAddressInfos;
            }

            var indexerTransactions =
                await _activityProvider.GetActivityAsync(request.TransactionId, request.BlockHash, caAddressInfos);

            if (indexerTransactions.CaHolderTransaction.Data.IsNullOrEmpty())
            {
                var indexerTransaction =
                    ObjectMapper.Map<CaHolderTransactionIndex, IndexerTransaction>(
                        await _activityProvider.GetNotSuccessTransactionAsync(caAddresses.First(),
                            request.TransactionId));

                if (indexerTransaction != null)
                {
                    indexerTransactions.CaHolderTransaction.Data.Add(indexerTransaction);
                }
            }

            var activitiesDto =
                await IndexerTransaction2Dto(caAddresses, indexerTransactions, chainId, 0, 0, true);
            if (activitiesDto == null || activitiesDto.TotalRecordCount == 0)
            {
                return new GetActivityDto();
            }

            var activityDto = activitiesDto.Data[0];

            if (IsTransferType(activityDto))
            {
                await GetActivityName(caAddresses, activityDto,
                    indexerTransactions.CaHolderTransaction.Data[0]);
            }

            SetSeedStatusAndTypeForActivityDto(activityDto);

            OptimizeSeedAliasDisplay(activityDto);

            TryUpdateImageUrlForActivityDto(activityDto);

            return activityDto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetActivityAsync Error {request}", request);
            return new GetActivityDto();
        }
    }

    private void SetSeedStatusAndTypeForActivityDtoList(List<GetActivityDto> activityDtoList)
    {
        if (activityDtoList != null && activityDtoList.Count != 0)
        {
            foreach (var activityDto in activityDtoList)
            {
                SetSeedStatusAndTypeForActivityDto(activityDto);
            }
        }
    }

    private void SetSeedStatusAndTypeForActivityDto(GetActivityDto activityDto)
    {
        if (activityDto.NftInfo != null)
        {
            // Set IsSeed to true if Symbol starts with "SEED-", otherwise set it to false
            activityDto.NftInfo.IsSeed = activityDto.Symbol.StartsWith(TokensConstants.SeedNamePrefix);

            if (activityDto.NftInfo.IsSeed)
            {
                activityDto.NftInfo.SeedType = (int)SeedType.FT;
                // Alias is actually TokenName
                if (!string.IsNullOrEmpty(activityDto.NftInfo.Alias) &&
                    activityDto.NftInfo.Alias.StartsWith(TokensConstants.SeedNamePrefix))
                {
                    activityDto.NftInfo.SeedType = activityDto.NftInfo.Alias.Remove(0, 5).Contains("-")
                        ? (int)SeedType.NFT
                        : (int)SeedType.FT;
                }
            }
        }
    }

    private void OptimizeSeedAliasDisplay(List<GetActivityDto> activityDtoList)
    {
        if (activityDtoList != null && activityDtoList.Count != 0)
        {
            foreach (var activityDto in activityDtoList)
            {
                OptimizeSeedAliasDisplay(activityDto);
            }
        }
    }

    private void OptimizeSeedAliasDisplay(GetActivityDto activityDto)
    {
        if (activityDto.NftInfo != null && activityDto.NftInfo.IsSeed &&
            activityDto.NftInfo.Alias.EndsWith(TokensConstants.SeedAliasNameSuffix))
        {
            activityDto.NftInfo.Alias =
                activityDto.NftInfo.Alias.TrimEnd(TokensConstants.SeedAliasNameSuffix.ToCharArray());
        }
    }

    private void TryUpdateImageUrlForActivityDtoList(List<GetActivityDto> activityDtoList)
    {
        if (activityDtoList != null && activityDtoList.Count != 0)
        {
            foreach (var activityDto in activityDtoList)
            {
                TryUpdateImageUrlForActivityDto(activityDto);
            }
        }
    }

    private void TryUpdateImageUrlForActivityDto(GetActivityDto activityDto)
    {
        if (activityDto.NftInfo != null)
        {
            activityDto.NftInfo.ImageUrl =
                IpfsImageUrlHelper.TryGetIpfsImageUrl(activityDto.NftInfo.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
            activityDto.ListIcon = activityDto.NftInfo.ImageUrl;
        }

        if (activityDto.Operations.IsNullOrEmpty()) return;

        foreach (var itemInfo in activityDto.Operations.Where(itemInfo => itemInfo.NftInfo != null))
        {
            itemInfo.NftInfo.ImageUrl =
                IpfsImageUrlHelper.TryGetIpfsImageUrl(itemInfo.NftInfo.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
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
            dto.To = nameList.FirstOrDefault(t => t.Item1?.Address == transaction.TransferInfo?.ToAddress)?.Item2;
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

        GuardiansDto guardian = null;
        var exists = indexerTransactions.CaHolderTransaction?.Data?.Exists(
            t => t.MethodName == AElfContractMethodName.AddManagerInfo ||
                 t.MethodName == AElfContractMethodName.AddGuardian);
        if (exists ?? false && needMap)
        {
            guardian = await _activityProvider.GetCaHolderInfoAsync(caAddresses, string.Empty);
        }
        var platformToIcon = _activitiesSource.IconInfos.ToDictionary(s => s.Platform, s => s.Icon);
        foreach (var ht in indexerTransactions.CaHolderTransaction.Data)
        {
            HandleTokenTransferInfos(ht, caAddresses.First());
            var dto = ObjectMapper.Map<IndexerTransaction, GetActivityDto>(ht);
            var transactionTime = MsToDateTime(ht.Timestamp * 1000);

            if (dto.Symbol != null)
            {
                var price = await GetTokenPriceAsync(dto.Symbol, transactionTime);
                dto.PriceInUsd = price.ToString();

                dto.CurrentPriceInUsd = (await GetCurrentTokenPriceAsync(dto.Symbol)).ToString();
                dto.CurrentTxPriceInUsd = GetCurrentTxPrice(dto);

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
                dto.ListIcon = dto.NftInfo.ImageUrl;
            }
            else
            {
                dto.ListIcon = GetIcon(dto);
            }

            if (!_activityTypeOptions.ShowPriceTypes.Contains(dto.TransactionType))
            {
                dto.IsDelegated = true;
            }

            if (_activityTypeOptions.SystemTypes.Contains(dto.TransactionType))
            {
                dto.IsSystem = true;
            }

            if (needMap)
            {
                await MapMethodNameAsync(caAddresses, dto, ht.ToContractAddress, guardian);
                AppendStatusIcon(dto);
            }

            SetDAppInfo(ht.ToContractAddress, dto, ht.FromAddress, ht.MethodName);
            await SetOperationsAsync(ht, dto, caAddresses, chainId, weidth, height);
            dto.FromChainIdUpdated = ChainDisplayNameHelper.GetDisplayName(dto.FromChainId);
            dto.ToChainIdUpdated = ChainDisplayNameHelper.GetDisplayName(dto.ToChainId);
            dto.FromChainIcon = ChainDisplayNameHelper.GetChainUrl(dto.FromChainId);
            dto.ToChainIcon = ChainDisplayNameHelper.GetChainUrl(dto.ToChainId);
            platformToIcon.TryGetValue(ht.Platform, out var sourceIcon);
            dto.SourceIcon = sourceIcon;
            getActivitiesDto.Add(dto);
        }

        result.Data = getActivitiesDto;

        return result;
    }

    private async Task CheckCryptoGiftByTransactionId(GetActivityDto dto)
    {
        try
        {
            if (_activityTypeOptions.TypeMap[CryptoGiftConstants.CreateCryptoBox].Equals(dto.TransactionName))
            {
                await ReplaceSentRedPackageActivity(dto);
                dto.StatusIcon = _activitiesStatus.Send;
            }
            else if (_activityTypeOptions.TypeMap[CryptoGiftConstants.TransferCryptoBoxes].Equals(dto.TransactionName))
            {
                await ReplacePayedRedPackageActivity(dto);
                dto.StatusIcon = _activitiesStatus.Receive;
            }
            else if (_activityTypeOptions.TypeMap[CryptoGiftConstants.RefundCryptoBox].Equals(dto.TransactionName))
            {
                await ReplaceRefundedRedPackageActivity(dto);
                dto.StatusIcon = _activitiesStatus.Receive;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "crypto gift transaction replaced error");
        }
    }

    private async Task<bool> ReplaceSentRedPackageActivity(GetActivityDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RedPackageIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.TransactionId).Value(dto.TransactionId)));
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.RedPackageDisplayType).Value((int)RedPackageDisplayType.CryptoGift)));
        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, cryptoGiftIndices) = await _redPackageIndexRepository.GetListAsync(Filter);
        var redPackageIndex = cryptoGiftIndices.FirstOrDefault();
        if (redPackageIndex == null)
        {
            _logger.LogWarning("TransactionId:{0} cann't get redPackageIndex from es", dto.TransactionId);
            return false;
        }

        dto.TransactionName = CryptoGiftConstants.SendTransactionName;
        return true;
    }

    private async Task<bool> ReplacePayedRedPackageActivity(GetActivityDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RedPackageIndex>, QueryContainer>>();
        // mustQuery.Add(q =>
        //     q.Term(i => i.Field(f => f.PayedTransactionIds).Value($"*{dto.TransactionId}*")));
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.RedPackageDisplayType).Value((int)RedPackageDisplayType.CryptoGift)));
        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, cryptoGiftIndices) = await _redPackageIndexRepository.GetListAsync(Filter);
        if (cryptoGiftIndices.IsNullOrEmpty())
        {
            _logger.LogWarning("TransactionId:{0} cann't get redPackageIndex from es", dto.TransactionId);
            return false;
        }

        var redPackageIndex = cryptoGiftIndices.FirstOrDefault(cp =>
            !cp.PayedTransactionIds.IsNullOrEmpty() && cp.PayedTransactionIds.Contains(dto.TransactionId));
        if (redPackageIndex == null || redPackageIndex.PayedTransactionDtoList.IsNullOrEmpty())
        {
            _logger.LogWarning("TransactionId:{0} cann't get redPackageIndex from es because of redPackageIndex null",
                dto.TransactionId);
            return false;
        }

        bool payedTransactionSucceed = redPackageIndex.PayedTransactionDtoList
            .Any(payed => payed.PayedTransactionId.Equals(dto.TransactionId)
                          && RedPackageTransactionStatus.Success.Equals(payed.PayedTransactionStatus));
        if (!payedTransactionSucceed)
        {
            _logger.LogWarning(
                "TransactionId:{0} cann't get redPackageIndex from es because of payedTransactionSucceed",
                dto.TransactionId);
            return false;
        }

        dto.TransactionName = CryptoGiftConstants.ClaimTransactionName;
        return true;
    }

    private async Task<bool> ReplaceRefundedRedPackageActivity(GetActivityDto dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RedPackageIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.RefundedTransactionId).Value(dto.TransactionId)));
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.RedPackageDisplayType).Value((int)RedPackageDisplayType.CryptoGift)));
        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, cryptoGiftIndices) = await _redPackageIndexRepository.GetListAsync(Filter);
        var redPackageIndex = cryptoGiftIndices.FirstOrDefault(crypto =>
            RedPackageTransactionStatus.Success.Equals(crypto.RefundedTransactionStatus));
        if (redPackageIndex == null)
        {
            _logger.LogWarning("TransactionId:{0} cann't get redPackageIndex from es", dto.TransactionId);
            return false;
        }

        dto.TransactionName = CryptoGiftConstants.RefundTransactionName;
        return true;
    }

    private void AppendStatusIcon(GetActivityDto activityDto)
    {
        activityDto.StatusIcon = activityDto.TransactionType switch
        {
            ActivityConstants.TransferName when ActivityConstants.CrossChainTransferDisplayName.Equals(activityDto
                .TransactionName) => _activitiesStatus.Send,
            ActivityConstants.DepositTypeName when ActivityConstants.DepositName.Equals(activityDto.TransactionName) =>
                _activitiesStatus.Receive,
            _ => activityDto.StatusIcon
        };
    }

    private async Task MapMethodNameAsync(List<string> caAddresses, GetActivityDto activityDto,
        string toContractAddress,
        GuardiansDto guardian = null)
    {
        var transactionType = activityDto.TransactionType;
        var typeName =
            _activityTypeOptions.TypeMap.GetValueOrDefault(transactionType, transactionType);
        activityDto.TransactionName = typeName;
        if (_activityTypeOptions.TypeMap[CryptoGiftConstants.CreateCryptoBox].Equals(activityDto.TransactionName)
            || _activityTypeOptions.TypeMap[CryptoGiftConstants.TransferCryptoBoxes].Equals(activityDto.TransactionName)
            || _activityTypeOptions.TypeMap[CryptoGiftConstants.RefundCryptoBox].Equals(activityDto.TransactionName))
        {
            await CheckCryptoGiftByTransactionId(activityDto);
        }

        if (transactionType is ActivityConstants.AddGuardianName or ActivityConstants.AddManagerInfo)
        {
            guardian ??= await _activityProvider.GetCaHolderInfoAsync(caAddresses, string.Empty);
            var holderInfo = guardian?.CaHolderInfo?.FirstOrDefault();
            if (holderInfo?.OriginChainId != null && holderInfo?.OriginChainId != activityDto.FromChainId)
            {
                activityDto.TransactionName = GetTransactionDisplayName(activityDto.TransactionType, typeName);
            }

            return;
        }

        if (transactionType is ActivityConstants.TransferName or ActivityConstants.CrossChainTransferName)
        {
            activityDto.TransactionName =
                activityDto.IsReceived ? ActivityConstants.ReceiveName : ActivityConstants.SendName;
            activityDto.StatusIcon = activityDto.IsReceived ? _activitiesStatus.Receive : _activitiesStatus.Send;
        }

        if (IsETransfer(transactionType, activityDto.FromChainId, activityDto.FromAddress))
        {
            activityDto.TransactionName = ActivityConstants.DepositName;
            activityDto.StatusIcon = _activitiesStatus.Receive;
            return;
        }

        if (activityDto.NftInfo != null && !string.IsNullOrWhiteSpace(activityDto.NftInfo.NftId))
        {
            var nftTransactionName =
                (transactionType is ActivityConstants.TransferName or ActivityConstants.CrossChainTransferName
                    or CryptoGiftConstants.CreateCryptoBox
                    or CryptoGiftConstants.TransferCryptoBoxes or CryptoGiftConstants.RefundCryptoBox)
                    ? activityDto.TransactionName
                    : typeName;

            activityDto.TransactionName = _activityTypeOptions.ShowNftTypes.Contains(activityDto.TransactionType)
                ? nftTransactionName + " NFT"
                : nftTransactionName;
        }

        SetHamsterName(activityDto);

        activityDto.TransactionType =
            _activityTypeOptions.TransactionTypeMap.GetValueOrDefault(transactionType, transactionType);

        var contractConfig =
            _activityOptions.ContractConfigs.FirstOrDefault(t => t.ContractAddress == toContractAddress);
        if (contractConfig == null) return;

        activityDto.TransactionName = contractConfig.MethodNameMap.ContainsKey(transactionType)
            ? contractConfig.MethodNameMap[transactionType]
            : activityDto.TransactionName;
    }

    private void SetHamsterName(GetActivityDto activityDto)
    {
        if (activityDto.TransactionType == AElfContractMethodName.Issue &&
            activityDto.Symbol == CommonConstant.HamsterKingSymbol)
        {
            activityDto.TransactionName = _activityOptions.HamsterConfig.GetRewardName;
        }
        else if (activityDto.TransactionType == AElfContractMethodName.Transfer &&
                 activityDto.Symbol == CommonConstant.HamsterPassSymbol &&
                 activityDto.FromAddress == _activityOptions.HamsterConfig.FromAddress)
        {
            activityDto.TransactionName = _activityOptions.HamsterConfig.GetPassName;
        }
    }

    private bool IsETransfer(string transactionType, string fromChainId, string fromAddress)
    {
        if (transactionType == ActivityConstants.TransferName &&
            _activityOptions.ETransferConfigs != null)
        {
            var eTransferConfig =
                _activityOptions.ETransferConfigs.FirstOrDefault(e => e.ChainId == fromChainId);
            return eTransferConfig != null && eTransferConfig.Accounts.Contains(fromAddress);
        }

        return false;
    }

    private string GetTransactionDisplayName(string transactionType, string defaultName)
    {
        return transactionType switch
        {
            ActivityConstants.AddGuardianName => ActivityConstants.NotRegisterChainAddGuardianName,
            ActivityConstants.AddManagerInfo => ActivityConstants.NotRegisterChainAddManagerName,
            _ => defaultName
        };
    }

    private string GetIcon(GetActivityDto activityDto)
    {
        var icon = string.Empty;
        if (_activityTypeOptions.SystemTypes.Contains(activityDto.TransactionType))
        {
            icon = _activitiesIcon.System;
        }
        else if (IsTransferType(activityDto))
        {
            icon = _assetsLibraryProvider.buildSymbolImageUrl(activityDto.Symbol);
        }

        // compatible with front-end changes
        if (icon.IsNullOrEmpty())
        {
            icon = _activitiesIcon.Contract;
        }

        return icon;
    }

    private bool IsTransferType(GetActivityDto activityDto)
    {
        return !activityDto.Symbol.IsNullOrEmpty() || activityDto.NftInfo != null &&
            !activityDto.Operations.IsNullOrEmpty();
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

    private async Task<decimal> GetCurrentTokenPriceAsync(string symbol)
    {
        var priceResult = await _tokenPriceService.GetCurrentPriceAsync(symbol);
        return priceResult?.PriceInUsd ?? 0;
    }

    private string GetCurrentTxPrice(GetActivityDto dto)
    {
        if (decimal.TryParse(dto.Amount, out var amount) &&
            decimal.TryParse(dto.Decimals, out var decimals) &&
            decimal.TryParse(dto.CurrentPriceInUsd, out var currentPriceInUsd))
        {
            var baseValue = (decimal)Math.Pow(10, (double)decimals);
            var currentTxPriceInUsd = amount / baseValue * currentPriceInUsd;
            return currentTxPriceInUsd == 0 ? null : currentTxPriceInUsd.ToString();
        }

        throw new ArgumentException("Invalid input values");
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

    public async Task<IndexerTransactions> GetTransactionByTransactionType(string transactionType)
    {
        var filterTypes = new List<string> { transactionType };
        var transactions = await _activityProvider.GetActivitiesAsync(new List<CAAddressInfo>(), null,
            null, filterTypes, 0, MaxResultCount);
        return transactions;
    }

    public async Task<IndexerTransactions> GetActivitiesWithBlockHeightAsync(List<string> inputTransactionTypes,
        string chainId, long startHeight, long endHeight)
    {
        return await _activityProvider.GetActivitiesWithBlockHeightAsync(new List<CAAddressInfo>(), chainId,
            null, inputTransactionTypes, 0, 100, startHeight, endHeight);
    }

    public async Task<IndexerTransactions> GetActivitiesV3(List<CAAddressInfo> caAddressInfos, string chainId)
    {
        var transactions = await _activityProvider.GetActivitiesAsync(caAddressInfos, chainId,
            null, null, 0, 20);
        return transactions;
    }
}