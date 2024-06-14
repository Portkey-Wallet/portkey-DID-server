using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;
using CAServer.CryptoGift.Dtos;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.CryptoGift;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Grains.State;
using CAServer.IpInfo;
using CAServer.RedPackage.Dtos;
using CAServer.Tokens.TokenPrice;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;
using NftInfoDto = CAServer.UserAssets.Dtos.NftInfoDto;

namespace CAServer.CryptoGift;

[RemoteService(isEnabled: false), DisableAuditing]
public class CryptoGiftAppService : CAServerAppService, ICryptoGiftAppService
{
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageIndexRepository;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly ICryptoGiftProvider _cryptoGiftProvider;
    private readonly IIpInfoAppService _ipInfoAppService;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IdentityUserManager _userManager;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly ILogger<CryptoGiftAppService> _logger;

    public CryptoGiftAppService(INESTRepository<RedPackageIndex, Guid> redPackageIndexRepository,
        IClusterClient clusterClient,
        IObjectMapper objectMapper,
        ICryptoGiftProvider cryptoGiftProvider,
        IIpInfoAppService ipInfoAppService,
        IAbpDistributedLock distributedLock,
        IdentityUserManager userManager,
        ITokenPriceService tokenPriceService,
        IUserAssetsProvider userAssetsProvider,
        ILogger<CryptoGiftAppService> logger)
    {
        _redPackageIndexRepository = redPackageIndexRepository;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _cryptoGiftProvider = cryptoGiftProvider;
        _ipInfoAppService = ipInfoAppService;
        _distributedLock = distributedLock;
        _userManager = userManager;
        _tokenPriceService = tokenPriceService;
        _userAssetsProvider = userAssetsProvider;
        _logger = logger;
    }

    public async Task<CryptoGiftHistoryItemDto> GetFirstCryptoGiftHistoryDetailAsync(Guid senderId)
    {
        var cryptoGiftIndices = await GetCryptoGiftHistoriesFromEs(senderId);
        var firstDetail = new CryptoGiftHistoryItemDto();
        if (cryptoGiftIndices.IsNullOrEmpty())
        {
            firstDetail.Exist = false;
            return firstDetail;
        }
        var historiesFromEs = cryptoGiftIndices.OrderByDescending(crypto => crypto.CreateTime).ToList();
        var firstItem = historiesFromEs.FirstOrDefault();
        if (firstItem == null)
        {
            firstDetail.Exist = false;
            return firstDetail;
        }

        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(firstItem.RedPackageId);
        var redPackageDetail = await grain.GetRedPackage(firstItem.RedPackageId);
        if (redPackageDetail.Success && redPackageDetail.Data != null)
        {
            firstDetail = _objectMapper.Map<RedPackageDetailDto, CryptoGiftHistoryItemDto>(redPackageDetail.Data);
        }

        return firstDetail;
    }

    private async Task<List<RedPackageIndex>> GetCryptoGiftHistoriesFromEs(Guid senderId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RedPackageIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.SenderId).Value(senderId)));
        // mustQuery.Add(q => 
        //     q.Term(i => i.Field(f => f.RedPackageDisplayType).Value((int)RedPackageDisplayType.CryptoGift)));
        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, cryptoGiftIndices) = await _redPackageIndexRepository.GetListAsync(Filter);
        return cryptoGiftIndices.Where(crypto => Guid.Parse("7d911f61-0511-4121-8cf3-1443d057999e").Equals(crypto.RedPackageId)
            || Guid.Parse("6c1eda46-f2d0-440b-82db-ed1abc2f0261").Equals(crypto.RedPackageId)
            || RedPackageDisplayType.CryptoGift.Equals(crypto.RedPackageDisplayType)).ToList();
    }

    public async Task<List<CryptoGiftHistoryItemDto>> ListCryptoGiftHistoriesAsync(Guid senderId)
    {
        List<CryptoGiftHistoryItemDto> result = new List<CryptoGiftHistoryItemDto>();
        var cryptoGiftIndices = await GetCryptoGiftHistoriesFromEs(senderId);
        _logger.LogInformation("senderId:{0} history from es records:{1}", senderId, JsonConvert.SerializeObject(cryptoGiftIndices));
        if (cryptoGiftIndices.IsNullOrEmpty())
        {
            return result;
        }

        var historiesFromEs = cryptoGiftIndices.OrderByDescending(crypto => crypto.CreateTime).ToList();
        var histories = _objectMapper.Map<List<RedPackageIndex>, List<CryptoGiftHistoryItemDto>>(historiesFromEs);
        foreach (var historyItem in histories)
        {
            var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(historyItem.Id);
            var redPackageDetail = await grain.GetRedPackage(historyItem.Id);
            if (!redPackageDetail.Success || redPackageDetail.Data == null)
            {
                continue;
            }
            result.Add(_objectMapper.Map<RedPackageDetailDto, CryptoGiftHistoryItemDto>(redPackageDetail.Data));
        }
        return result;
    }

    public async Task<PreGrabbedDto> ListCryptoPreGiftGrabbedItems(Guid redPackageId)
    {
        var grain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftGrainDto = await grain.GetCryptoGift(redPackageId);
        if (!cryptoGiftGrainDto.Success || cryptoGiftGrainDto.Data == null)
        {
            return new PreGrabbedDto()
            {
                Items = new List<PreGrabbedItemDto>()
            };
        }

        var cryptoGiftDto = cryptoGiftGrainDto.Data;
        _logger.LogInformation("========ListCryptoPreGiftGrabbedItems {0}", JsonConvert.SerializeObject(cryptoGiftDto.Items));
        var grabbedItemDtos = _objectMapper.Map<List<PreGrabItem>, List<PreGrabbedItemDto>>(cryptoGiftDto.Items.Where(crypto => GrabbedStatus.Created.Equals(crypto.GrabbedStatus)).ToList());
        var expireMilliseconds = _cryptoGiftProvider.GetExpirationSeconds() * 1000;
        foreach (var preGrabbedItemDto in grabbedItemDtos)
        {
            preGrabbedItemDto.DisplayType = CryptoGiftDisplayType.Pending;
            preGrabbedItemDto.ExpirationTime = preGrabbedItemDto.GrabTime + expireMilliseconds;
        }
        return new PreGrabbedDto()
        {
            Items = grabbedItemDtos
        };
    }

    public async Task<CryptoGiftIdentityCodeDto> PreGrabCryptoGift(Guid redPackageId)
    {
        //todo make sure if the user red package id is available
        await using var handle = await _distributedLock.TryAcquireAsync("CryptoGift:DistributionLock:" + redPackageId, TimeSpan.FromSeconds(3));
        if (handle == null)
        {
            throw new UserFriendlyException("please take a break for a while~");
        }
        var ipAddress = _ipInfoAppService.GetRemoteIp();
        if (ipAddress.IsNullOrEmpty())
        {
            throw new UserFriendlyException("portkey can't get your ip, grab failed~");
        }
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
        var redPackageDetail = await grain.GetRedPackage(redPackageId);
        if (!redPackageDetail.Success || redPackageDetail.Data == null)
        {
            throw new UserFriendlyException("the red package does not exist");
        }
        var identityCode = GetIdentityCode(redPackageId, ipAddress);
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        if (!cryptoGiftResultDto.Success || cryptoGiftResultDto.Data == null)
        {
            throw new UserFriendlyException("the crypto gift does not exist");
        }
        //check claim condition
        var redPackageDetailDto = redPackageDetail.Data;
        var cryptoGiftDto = cryptoGiftResultDto.Data;
        CheckClaimCondition(redPackageDetailDto, cryptoGiftDto, identityCode);

        PreGrabBucketItemDto preGrabBucketItemDto = GetBucket(cryptoGiftDto, identityCode);
        cryptoGiftDto.Items.Add(new PreGrabItem()
        {
            Index = preGrabBucketItemDto.Index,
            Amount = preGrabBucketItemDto.Amount,
            Decimal = redPackageDetailDto.Decimal,
            GrabbedStatus = GrabbedStatus.Created,
            GrabTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            IpAddress = ipAddress,
            IdentityCode = identityCode
        });
        cryptoGiftDto.PreGrabbedAmount += preGrabBucketItemDto.Amount;
        _logger.LogInformation("PreGrabCryptoGift before update:{0}", JsonConvert.SerializeObject(cryptoGiftDto));
        var updateResult = await cryptoGiftGrain.UpdateCryptoGift(cryptoGiftDto);
        _logger.LogInformation("PreGrabCryptoGift updateResult:{0}", JsonConvert.SerializeObject(updateResult));
        return new CryptoGiftIdentityCodeDto() { IdentityCode = identityCode };
    }
    
    private PreGrabBucketItemDto GetBucket(CryptoGiftDto cryptoGiftDto, string identityCode)
    {
        var random = new Random();
        var index = random.Next(cryptoGiftDto.BucketNotClaimed.Count);
        var bucket = cryptoGiftDto.BucketNotClaimed[index];
        bucket.IdentityCode = identityCode;
        cryptoGiftDto.BucketNotClaimed.Remove(bucket);
        cryptoGiftDto.BucketClaimed.Add(bucket);
        return bucket;
    }

    private static void CheckClaimCondition(RedPackageDetailDto redPackageDetailDto, CryptoGiftDto cryptoGiftDto,
        string identityCode)
    {
        if (RedPackageStatus.Expired.Equals(redPackageDetailDto.Status))
        {
            throw new UserFriendlyException("RedPackage has Expired");
        }
        if (RedPackageStatus.Cancelled.Equals(redPackageDetailDto.Status))
        {
            throw new UserFriendlyException("RedPackage has been Cancelled");
        }
        if (RedPackageStatus.FullyClaimed.Equals(redPackageDetailDto.Status))
        {
            throw new UserFriendlyException("RedPackage have been fully claimed");
        }
        if (cryptoGiftDto.PreGrabbedAmount >= cryptoGiftDto.TotalAmount)
        {
            throw new UserFriendlyException("Sorry, the crypto gift has been fully claimed");
        }
        if (cryptoGiftDto.Items.Any(c => c.IdentityCode.Equals(identityCode) 
                                         && (GrabbedStatus.Claimed.Equals(c.GrabbedStatus)
                                             || GrabbedStatus.Created.Equals(c.GrabbedStatus))))
        {
            throw new UserFriendlyException("You have received a crypto gift, please complete the registration as soon as possible");
        }
    }

    private async Task<bool> CheckAndUpdateCryptoGiftExpirationStatus(ICryptoGiftGran cryptoGiftGrain, CryptoGiftDto cryptoGiftDto, string identityCode)
    {
        if (identityCode.IsNullOrEmpty())
        {
            return false;
        }
        var expiredMills = _cryptoGiftProvider.GetExpirationSeconds() * 1000;
        var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var preGrabItems = cryptoGiftDto.Items
            .Where(crypto =>
                crypto.IdentityCode.Equals(identityCode) && GrabbedStatus.Created.Equals(crypto.GrabbedStatus)
                && (currentTime - crypto.GrabTime >= expiredMills))
            .ToList();
        foreach (var preGrabItem in preGrabItems)
        {
            preGrabItem.GrabbedStatus = GrabbedStatus.Expired;
        }
        var cryptoGrainResult = await cryptoGiftGrain.UpdateCryptoGift(cryptoGiftDto);
        _logger.LogInformation("CheckAndUpdateCryptoGiftExpirationStatus cryptoGrainResult:{0}", JsonConvert.SerializeObject(cryptoGrainResult));
        return !preGrabItems.IsNullOrEmpty();
    }

    public async Task PreGrabCryptoGiftAfterLogging(Guid redPackageId, Guid userId, int index, int amountDecimal)
    {
        var ipAddress = _ipInfoAppService.GetRemoteIp();
        if (ipAddress.IsNullOrEmpty())
        {
            throw new UserFriendlyException("PreGrabCryptoGiftAfterLogging portkey can't get your ip, grab failed~");
        }
        var identityCode = GetIdentityCode(redPackageId, ipAddress);
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        if (!cryptoGiftResultDto.Success || cryptoGiftResultDto.Data == null)
        {
            throw new UserFriendlyException("PreGrabCryptoGiftAfterLogging the crypto gift does not exist");
        }
        var cryptoGiftDto = cryptoGiftResultDto.Data;
        PreGrabBucketItemDto preGrabBucketItemDto = GetBucketByIndex(cryptoGiftDto, index, userId, identityCode);
        cryptoGiftDto.Items.Add(new PreGrabItem()
        {
            Index = preGrabBucketItemDto.Index,
            Amount = preGrabBucketItemDto.Amount,
            Decimal = amountDecimal,
            GrabbedStatus = GrabbedStatus.Created,
            GrabTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            IpAddress = ipAddress,
            IdentityCode = identityCode
        });
        cryptoGiftDto.BucketClaimed.Add(preGrabBucketItemDto);
        cryptoGiftDto.BucketNotClaimed.Remove(preGrabBucketItemDto);
        cryptoGiftDto.PreGrabbedAmount += preGrabBucketItemDto.Amount;
        _logger.LogInformation("PreGrabCryptoGiftAfterLogging before update:{0}", JsonConvert.SerializeObject(cryptoGiftDto));
        var updateResult = await cryptoGiftGrain.UpdateCryptoGift(cryptoGiftDto);
        _logger.LogInformation("PreGrabCryptoGiftAfterLogging updateResult:{0}", JsonConvert.SerializeObject(updateResult));
    }
    
    private PreGrabBucketItemDto GetBucketByIndex(CryptoGiftDto cryptoGiftDto, int index, Guid userId, string identityCode)
    {
        var bucket = cryptoGiftDto.BucketNotClaimed[index];
        bucket.IdentityCode = identityCode;
        bucket.Index = index;
        bucket.UserId = userId;
        cryptoGiftDto.BucketNotClaimed.Remove(bucket);
        cryptoGiftDto.BucketClaimed.Add(bucket);
        return bucket;
    }
    
    private GetNftItemInfosDto CreateGetNftItemInfosDto(string symbol, string chainId)
    {
        var getNftItemInfosDto = new GetNftItemInfosDto();
        getNftItemInfosDto.GetNftItemInfos = new List<GetNftItemInfo>();
        var nftItemInfo = new GetNftItemInfo();
        nftItemInfo.Symbol = symbol;
        nftItemInfo.ChainId = chainId;
        getNftItemInfosDto.GetNftItemInfos.Add(nftItemInfo);

        return getNftItemInfosDto;
    }

    public async Task<CryptoGiftPhaseDto> GetCryptoGiftDetailAsync(Guid redPackageId, string ipAddressParam)
    {
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
        var redPackageDetail = await grain.GetRedPackage(redPackageId);
        if (!redPackageDetail.Success || redPackageDetail.Data == null)
        {
            throw new UserFriendlyException("the red package does not exist");
        }
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        if (!cryptoGiftResultDto.Success || cryptoGiftResultDto.Data == null)
        {
            throw new UserFriendlyException("the crypto gift does not exist");
        }
        var redPackageDetailDto = redPackageDetail.Data;
        var cryptoGiftDto = cryptoGiftResultDto.Data;
        var ipAddress = ipAddressParam.IsNullOrEmpty() ? _ipInfoAppService.GetRemoteIp() : ipAddressParam;
        var identityCode = GetIdentityCode(redPackageId, ipAddress);
        await CheckAndUpdateCryptoGiftExpirationStatus(cryptoGiftGrain, cryptoGiftDto, identityCode);
        //get the sender info
        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(redPackageDetailDto.SenderId);
        var caHolderGrainDto = await caHolderGrain.GetCaHolder();
        if (!caHolderGrainDto.Success || caHolderGrainDto.Data == null)
        {
            throw new UserFriendlyException("the crypto gift sender does not exist");
        }

        var caHolderDto = caHolderGrainDto.Data;
        // get nft info
        var nftInfoDto = await GetNftInfo(redPackageDetailDto);
        
        if (RedPackageStatus.Expired.Equals(redPackageDetailDto.Status))
        {
            return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.Expired, redPackageDetailDto,
                caHolderDto, nftInfoDto, "Oops, the crypto gift has expired.", "", 0,
                0, 0);
        }
        
        var claimedPreGrabItem = cryptoGiftDto.Items.FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode)
                                                                              && GrabbedStatus.Claimed.Equals(crypto.GrabbedStatus));
        if (claimedPreGrabItem != null)
        {
            var dollarValue = await GetDollarValue(redPackageDetailDto.Symbol, claimedPreGrabItem.Amount);
            var remainingExpirationSeconds = claimedPreGrabItem.GrabTime / 1000 + _cryptoGiftProvider.GetExpirationSeconds() -
                                             DateTimeOffset.Now.ToUnixTimeSeconds();
            return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.Claimed, redPackageDetailDto,
                caHolderDto, nftInfoDto, "You will get",  dollarValue, claimedPreGrabItem.Amount,
                0, remainingExpirationSeconds);
        }

        if ((RedPackageStatus.NotClaimed.Equals(redPackageDetailDto.Status)
             || RedPackageStatus.Claimed.Equals(redPackageDetailDto.Status)))
        {
            var preGrabItem = cryptoGiftDto.Items
                .FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode)
                                          && GrabbedStatus.Created.Equals(crypto.GrabbedStatus));
            _logger.LogInformation("=====================");
            foreach (var grabItem in cryptoGiftDto.Items)
            {
                _logger.LogInformation("identityCode:{0} grabItem:{1}", identityCode, JsonConvert.SerializeObject(grabItem));
                _logger.LogInformation("crypto.IdentityCode.Equals(identityCode):{0}", grabItem.IdentityCode.Equals(identityCode));
                _logger.LogInformation("GrabbedStatus.Created.Equals(crypto.GrabbedStatus):{0}", GrabbedStatus.Created.Equals(grabItem.GrabbedStatus));
            }
            if (preGrabItem != null)
            {
                var remainingExpirationSeconds = DateTimeOffset.Now.ToUnixTimeSeconds() - preGrabItem.GrabTime / 1000;
                var dollarValue = await GetDollarValue(redPackageDetailDto.Symbol, preGrabItem.Amount);
                return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.GrabbedQuota, redPackageDetailDto,
                    caHolderDto, nftInfoDto, "Claim and Join Portkey", dollarValue, preGrabItem.Amount,
                    0, remainingExpirationSeconds);
            }
        }
        
        if ((RedPackageStatus.NotClaimed.Equals(redPackageDetailDto.Status)
             || RedPackageStatus.Claimed.Equals(redPackageDetailDto.Status))
            && cryptoGiftDto.PreGrabbedAmount >= cryptoGiftDto.TotalAmount)
        {
            var preGrabItem = cryptoGiftDto.Items
                .Where(crypto => GrabbedStatus.Created.Equals(crypto.GrabbedStatus))
                .MinBy(crypto => crypto.GrabTime);
            if (preGrabItem == null)
            {
                throw new UserFriendlyException("Sorry, crypto gift status occured error");
            }
            var remainingWaitingSeconds = DateTimeOffset.Now.ToUnixTimeSeconds() - preGrabItem.GrabTime / 1000;
            return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.NoQuota, redPackageDetailDto,
                caHolderDto, nftInfoDto, "Unclaimed gifts may be up for grabs! Try to claim once the countdown ends.", "", 0,
                remainingWaitingSeconds, 0);
        }
        
        if (RedPackageStatus.FullyClaimed.Equals(redPackageDetailDto.Status)
                    || cryptoGiftDto.PreGrabbedAmount >= cryptoGiftDto.TotalAmount)
        {
            return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.FullyClaimed, redPackageDetailDto,
                caHolderDto, nftInfoDto, "Oh no, all the crypto gifts have been claimed.", "", 0,
                0, 0);
        }
        
        return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.Available, redPackageDetailDto,
            caHolderDto, nftInfoDto, "Claim and Join Portkey", "", 0,
            0, 0);
    }

    private static string GetIdentityCode(Guid redPackageId, string ipAddress)
    {
        return HashHelper.ComputeFrom(ipAddress + "#" + redPackageId).ToString().Replace("\"", "");
    }

    private async Task<string> GetDollarValue(string symbol, long amount)
    {
        var dollarValue = string.Empty;
        var tokenPriceData = await _tokenPriceService.GetCurrentPriceAsync(symbol);
        if (tokenPriceData != null)
        {
            dollarValue = "≈$ " + Decimal.Multiply(tokenPriceData.PriceInUsd, amount);
        }

        return dollarValue;
    }

    private CryptoGiftPhaseDto GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase cryptoGiftPhase, RedPackageDetailDto redPackageDetailDto,
        CAHolderGrainDto caHolderGrainDto, NftInfoDto nftInfoDto, string subPrompt, string dollarValue, long amount,
        long remainingWaitingSeconds, long remainingExpirationSeconds) {
        return new CryptoGiftPhaseDto() 
        {
            CryptoGiftPhase = cryptoGiftPhase,
            Prompt = $"{caHolderGrainDto.Nickname} sent you a crypto gift",
            SubPrompt = subPrompt,
            Amount = amount,
            Decimals = redPackageDetailDto.Decimal,
            Symbol = redPackageDetailDto.Symbol,
            DollarValue = dollarValue,
            NftAlias = nftInfoDto.Alias,
            NftTokenId = nftInfoDto.TokenId,
            NftImageUrl = nftInfoDto.ImageUrl,
            AssetType = redPackageDetailDto.AssetType,
            Memo = redPackageDetailDto.Memo,
            IsNewUsersOnly = redPackageDetailDto.IsNewUsersOnly,
            RemainingWaitingSeconds = remainingWaitingSeconds,
            RemainingExpirationSeconds = remainingExpirationSeconds,
            Sender = new UserInfoDto()
            {
                Avatar = caHolderGrainDto.Avatar,
                Nickname = caHolderGrainDto.Nickname
            }
        };
    }

    public async Task<CryptoGiftAppDto> GetCryptoGiftDetailFromGrainAsync(Guid redPackageId)
    {
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        _logger.LogInformation("=========GetCryptoGiftDetailFromGrainAsync:{0}", JsonConvert.SerializeObject(cryptoGiftResultDto.Data));
        return _objectMapper.Map<CryptoGiftDto, CryptoGiftAppDto>(cryptoGiftResultDto.Data);
    }

    public async Task<CryptoGiftPhaseDto> GetCryptoGiftLoginDetailAsync(Guid receiverId, Guid redPackageId)
    {
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
        var redPackageDetail = await grain.GetRedPackage(redPackageId);
        if (!redPackageDetail.Success || redPackageDetail.Data == null)
        {
            throw new UserFriendlyException("the red package does not exist");
        }
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        if (!cryptoGiftResultDto.Success || cryptoGiftResultDto.Data == null)
        {
            throw new UserFriendlyException("the crypto gift does not exist");
        }
        var redPackageDetailDto = redPackageDetail.Data;
        var cryptoGiftDto = cryptoGiftResultDto.Data;
        var ipAddress = _ipInfoAppService.GetRemoteIp();
        var identityCode = GetIdentityCode(redPackageId, ipAddress);
        await CheckAndUpdateCryptoGiftExpirationStatus(cryptoGiftGrain, cryptoGiftDto, identityCode);
        
        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(redPackageDetailDto.SenderId);
        var caHolderResult = await caHolderGrain.GetCaHolder();
        if (!caHolderResult.Success || caHolderResult.Data == null)
        {
            throw new UserFriendlyException("the crypto gift sender does not exist");
        }
        var caHolderGrainDto = caHolderResult.Data;
        
        // get nft info
        var nftInfoDto = await GetNftInfo(redPackageDetailDto);
        
        if (redPackageDetailDto.IsNewUsersOnly && !caHolderGrainDto.IsNewUserRegistered)
        {
            return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.OnlyNewUsers, redPackageDetailDto,
                caHolderGrainDto, nftInfoDto, "Oops! This is an exclusive gift for new users", "",
                0);
        }
        
        if (RedPackageStatus.Expired.Equals(redPackageDetailDto.Status))
        {
            return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.Expired, redPackageDetailDto,
                caHolderGrainDto, nftInfoDto, "Oops, the crypto gift has expired.", "",
                0);
        }
        
        var grabItemDto = redPackageDetailDto.Items.FirstOrDefault(red => red.UserId.Equals(receiverId));
        if (grabItemDto != null)
        {
            var subPrompt = $"You've already claimed this crypto gift and received" +
                            $" {grabItemDto.Amount} {redPackageDetailDto.Symbol}. You can't claim it again.";
            var dollarValue = await GetDollarValue(redPackageDetailDto.Symbol, long.Parse(grabItemDto.Amount));
            return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.Claimed, redPackageDetailDto,
                caHolderGrainDto, nftInfoDto,  subPrompt, dollarValue,
                long.Parse(grabItemDto.Amount));
        }

        if ((RedPackageStatus.NotClaimed.Equals(redPackageDetailDto.Status)
             || RedPackageStatus.Claimed.Equals(redPackageDetailDto.Status)))
        {
            var preGrabItem = cryptoGiftDto.Items
                .Where(crypto => crypto.IdentityCode.Equals(identityCode) 
                                 && GrabbedStatus.Expired.Equals(crypto.GrabbedStatus))
                .MaxBy(crypto => crypto.GrabTime);
            if (preGrabItem != null)
            {
                return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.ExpiredReleased, redPackageDetailDto,
                    caHolderGrainDto, nftInfoDto, "Oops, the crypto gift has expired.", "",
                    0);
            }
        }
        
        if (RedPackageStatus.FullyClaimed.Equals(redPackageDetailDto.Status))
        {
            return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.FullyClaimed, redPackageDetailDto,
                caHolderGrainDto, nftInfoDto,  "Oh no, all the crypto gifts have been claimed.", "",
                0);
        }
        
        return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.Available, redPackageDetailDto,
                        caHolderGrainDto, nftInfoDto,  "Claim and Join Portkey", "",
                        0);
    }

    private async Task<NftInfoDto> GetNftInfo(RedPackageDetailDto redPackageDetailDto)
    {
        var nftInfoDto = new NftInfoDto();
        if (redPackageDetailDto.AssetType == (int)AssetType.NFT)
        {
            var getNftItemInfosDto = CreateGetNftItemInfosDto(redPackageDetailDto.Symbol, redPackageDetailDto.ChainId);
            var indexerNftItemInfos = await _userAssetsProvider.GetNftItemInfosAsync(getNftItemInfosDto, 0, 1000);
            List<NftItemInfo> nftItemInfos = indexerNftItemInfos.NftItemInfos;

            if (nftItemInfos != null && nftItemInfos.Count > 0)
            {
                nftInfoDto.Alias = nftItemInfos[0].TokenName;
                nftInfoDto.TokenId = redPackageDetailDto.Symbol.Split('-')[1];
                nftInfoDto.ImageUrl = nftItemInfos[0].ImageUrl;
            }
        }

        return nftInfoDto;
    }

    private CryptoGiftPhaseDto GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase cryptoGiftPhase, RedPackageDetailDto redPackageDetailDto,
        CAHolderGrainDto caHolderGrainDto, NftInfoDto nftInfoDto, string subPrompt, string dollarValue, long amount) {
        return new CryptoGiftPhaseDto() 
        {
            CryptoGiftPhase = cryptoGiftPhase,
            Prompt = $"{caHolderGrainDto.Nickname} sent you a crypto gift",
            SubPrompt = subPrompt,
            Amount = amount,
            Decimals = redPackageDetailDto.Decimal,
            Symbol = redPackageDetailDto.Symbol,
            DollarValue = dollarValue,
            NftAlias = nftInfoDto.Alias,
            NftTokenId = nftInfoDto.TokenId,
            NftImageUrl = nftInfoDto.ImageUrl,
            AssetType = redPackageDetailDto.AssetType,
            Memo = redPackageDetailDto.Memo,
            IsNewUsersOnly = redPackageDetailDto.IsNewUsersOnly,
            Sender = new UserInfoDto()
            {
                Avatar = caHolderGrainDto.Avatar,
                Nickname = caHolderGrainDto.Nickname
            }
        };
    }
    
    public async Task CryptoGiftTransferToRedPackage(string caHash, string caAddress, ReferralInfo referralInfo, bool isNewUser)
    {
        if (referralInfo is not { ProjectCode: CommonConstant.CryptoGiftProjectCode } || referralInfo.ReferralCode.IsNullOrEmpty())
        {
            _logger.LogInformation("CryptoGiftTransferToRedPackage ProjectCode isn't 20000, referralInfo={0}", JsonConvert.SerializeObject(referralInfo));
            return;
        }

        if (caHash.IsNullOrEmpty() || caAddress.IsNullOrEmpty())
        {
            throw new UserFriendlyException($"cahash:{caHash} and caAddress:{caAddress} are required", caHash, caAddress);
        }
        var user = await _userManager.FindByNameAsync(caHash);
        if (user == null)
        {
            throw new UserFriendlyException($"the user cahash:{caHash} doesn't exist", caHash);
        }
        Guid userId = user.Id;
        var infos = referralInfo.ReferralCode.Split("#");
        string identityCode = infos[1];
        Guid redPackageId = Guid.Parse(infos[0]);
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
        var redPackageDetail = await grain.GetRedPackage(redPackageId);
        if (!redPackageDetail.Success || redPackageDetail.Data == null)
        {
            throw new UserFriendlyException("the red package does not exist");
        }
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        if (!cryptoGiftResultDto.Success || cryptoGiftResultDto.Data == null)
        {
            throw new UserFriendlyException("the crypto gift does not exist");
        }
        var redPackageDetailDto = redPackageDetail.Data;
        var cryptoGiftDto = cryptoGiftResultDto.Data;
        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        await caHolderGrain.UpdateNewUserMarkAsync(isNewUser);
        //0 make sure the client is whether or not a new one, according to the new user rule
        if (!Enum.IsDefined(redPackageDetailDto.RedPackageDisplayType) || RedPackageDisplayType.Common.Equals(redPackageDetailDto.RedPackageDisplayType))
        {
            return;
        }
        if (redPackageDetailDto.IsNewUsersOnly && !isNewUser)
        {
            //return the quota of the crypto gift
            ReturnPreGrabbedCryptoGift(identityCode, cryptoGiftDto);
            var returnResult = await cryptoGiftGrain.UpdateCryptoGift(cryptoGiftDto);
            _logger.LogInformation("CryptoGiftTransferToRedPackage returnResult:{}", JsonConvert.SerializeObject(returnResult));
            return;
        }
        //1 crypto gift: amount/item
        var preGrabBucketItemDto = GetClaimedCryptoGift(userId, identityCode, redPackageId, cryptoGiftDto);
        _logger.LogInformation("CryptoGiftTransferToRedPackage GetClaimedCryptoGift:{0}", JsonConvert.SerializeObject(preGrabBucketItemDto));
        var updateCryptoGiftResult = cryptoGiftGrain.UpdateCryptoGift(cryptoGiftDto);
        _logger.LogInformation("CryptoGiftTransferToRedPackage updateCryptoGiftResult:{0}", JsonConvert.SerializeObject(updateCryptoGiftResult));
        
        //2 red package: amount/item
        var redPackageUpdateResult = await grain.CryptoGiftTransferToRedPackage(userId, caAddress, preGrabBucketItemDto);
        _logger.LogInformation("CryptoGiftTransferToRedPackage redPackageUpdateResult:{0}", JsonConvert.SerializeObject(redPackageUpdateResult));
    }

    private void ReturnPreGrabbedCryptoGift(string identityCode, CryptoGiftDto cryptoGiftDto)
    {
        var preGrabItem = cryptoGiftDto.Items
            .FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode) && GrabbedStatus.Created.Equals(crypto.GrabbedStatus));
        if (preGrabItem == null)
        {
            throw new UserFriendlyException($"return red package:{cryptoGiftDto.Id} is not crypto gift, identityCode:{identityCode}");
        }
        PreGrabBucketItemDto preGrabBucketItemDto = cryptoGiftDto.BucketClaimed[preGrabItem.Index];
        cryptoGiftDto.BucketNotClaimed.Add(preGrabBucketItemDto);
        cryptoGiftDto.BucketClaimed.Remove(preGrabBucketItemDto);
    }

    private static PreGrabBucketItemDto GetClaimedCryptoGift(Guid userId, string identityCode, Guid redPackageId,
        CryptoGiftDto cryptoGiftDto)
    {
        var preGrabItem = cryptoGiftDto.Items
            .Where(crypto => crypto.IdentityCode.Equals(identityCode)
                             && GrabbedStatus.Created.Equals(crypto.GrabbedStatus))
            .MaxBy(crypto => crypto.GrabTime);
        if (preGrabItem == null)
        {
            throw new UserFriendlyException($"the user:{userId} identity:{identityCode} didn't get a crypto gift:{redPackageId} or the crypto gift is expired");
        }
        preGrabItem.GrabbedStatus = GrabbedStatus.Claimed;
        var preGrabBucketItemDto = cryptoGiftDto.BucketClaimed
            .FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode) && crypto.Amount.Equals(preGrabItem.Amount));
        if (preGrabBucketItemDto == null)
        {
            throw new UserFriendlyException($"the user:{userId} identity:{identityCode} didn't grab a crypto gift:{redPackageId}");
        }
        preGrabBucketItemDto.UserId = userId;
        return preGrabBucketItemDto;
    }
}