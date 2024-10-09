using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.CryptoGift.Dtos;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.CryptoGift;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Grains.State;
using CAServer.IpInfo;
using CAServer.Options;
using CAServer.RedPackage.Dtos;
using CAServer.RedPackage.Etos;
using CAServer.Tokens.TokenPrice;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;
using NftInfoDto = CAServer.UserAssets.Dtos.NftInfoDto;
using PreGrabbedItemDto = CAServer.RedPackage.Dtos.PreGrabbedItemDto;

namespace CAServer.CryptoGift;

[RemoteService(isEnabled: false), DisableAuditing]
public partial class CryptoGiftAppService : CAServerAppService, ICryptoGiftAppService
{
    [GeneratedRegex("\\.?0*$")]
    private static partial Regex DollarRegex();
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageIndexRepository;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly ICryptoGiftProvider _cryptoGiftProvider;
    private readonly IIpInfoAppService _ipInfoAppService;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IdentityUserManager _userManager;
    private readonly ITokenPriceService _tokenPriceService;
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly IpfsOptions _ipfsOptions;
    private readonly ILogger<CryptoGiftAppService> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IContractProvider _contractProvider;
    private readonly INESTRepository<CryptoGiftNewUsersOnlyNumStatsIndex, string> _newUsersOnlyNumStatsRepository;
    private readonly INESTRepository<CryptoGiftOldUsersNumStatsIndex, string> _oldUsersNumStatsRepository;
    private readonly INESTRepository<CryptoGiftNewUsersOnlyDetailStatsIndex, string> _newUsersOnlyDetailRepository;
    private readonly INESTRepository<CryptoGiftOldUsersDetailStatsIndex, string> _oldUsersDetailRepository;

    public CryptoGiftAppService(INESTRepository<RedPackageIndex, Guid> redPackageIndexRepository,
        IClusterClient clusterClient,
        IObjectMapper objectMapper,
        ICryptoGiftProvider cryptoGiftProvider,
        IIpInfoAppService ipInfoAppService,
        IAbpDistributedLock distributedLock,
        IdentityUserManager userManager,
        ITokenPriceService tokenPriceService,
        IUserAssetsProvider userAssetsProvider,
        IDistributedCache<string> distributedCache,
        IOptionsSnapshot<IpfsOptions> ipfsOptions,
        ILogger<CryptoGiftAppService> logger,
        IDistributedEventBus distributedEventBus,
        IContractProvider contractProvider,
        INESTRepository<CryptoGiftNewUsersOnlyNumStatsIndex, string> newUsersOnlyNumStatsRepository,
        INESTRepository<CryptoGiftNewUsersOnlyDetailStatsIndex, string> newUsersOnlyDetailRepository,
        INESTRepository<CryptoGiftOldUsersNumStatsIndex, string> oldUsersNumStatsRepository,
        INESTRepository<CryptoGiftOldUsersDetailStatsIndex, string> oldUsersDetailRepository)
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
        _distributedCache = distributedCache;
        _ipfsOptions = ipfsOptions.Value;
        _logger = logger;
        _distributedEventBus = distributedEventBus;
        _contractProvider = contractProvider;
        _newUsersOnlyNumStatsRepository = newUsersOnlyNumStatsRepository;
        _oldUsersNumStatsRepository = oldUsersNumStatsRepository;
        _newUsersOnlyDetailRepository = newUsersOnlyDetailRepository;
        _oldUsersDetailRepository = oldUsersDetailRepository;
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
        mustQuery.Add(q => 
            q.Term(i => i.Field(f => f.RedPackageDisplayType).Value((int)RedPackageDisplayType.CryptoGift)));
        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, cryptoGiftIndices) = await _redPackageIndexRepository.GetListAsync(Filter);
        return cryptoGiftIndices.Where(crypto => RedPackageDisplayType.CryptoGift.Equals(crypto.RedPackageDisplayType)).ToList();
    }

    public async Task<List<CryptoGiftHistoryItemDto>> ListCryptoGiftHistoriesAsync(Guid senderId)
    {
        List<CryptoGiftHistoryItemDto> result = new List<CryptoGiftHistoryItemDto>();
        var cryptoGiftIndices = await GetCryptoGiftHistoriesFromEs(senderId);
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
        var expireMilliseconds = _cryptoGiftProvider.GetExpirationSeconds() * 1000;
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var grabbedItemDtos = _objectMapper.Map<List<PreGrabItem>, List<PreGrabbedItemDto>>(
            cryptoGiftDto.Items.Where(crypto => GrabbedStatus.Created.Equals(crypto.GrabbedStatus) 
                                                && crypto.GrabTime + expireMilliseconds > now).ToList());
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

    public async Task<CryptoGiftIdentityCodeDto> PreGrabCryptoGift(Guid redPackageId, string random)
    {
        await using var handle = await _distributedLock.TryAcquireAsync("CryptoGift:DistributionLock:" + redPackageId, TimeSpan.FromSeconds(1));
        if (handle == null)
        {
            throw new UserFriendlyException("please take a break for a while~");
        }
        var ipAddress = _ipInfoAppService.GetRemoteIp(random);
        if (ipAddress.IsNullOrEmpty())
        {
            throw new UserFriendlyException("portkey can't get your ip, grab failed~");
        }
        var identityCode = GetIdentityCode(redPackageId, ipAddress);
        _logger.LogInformation($"pre grab data redPackageId:{redPackageId}," +
                               $"PreGrabCrypto identityCode:{identityCode}, ipAddress:{ipAddress}");
        await _distributedCache.SetAsync(identityCode, "Claimed");
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
        //check claim condition
        var redPackageDetailDto = redPackageDetail.Data;
        var cryptoGiftDto = cryptoGiftResultDto.Data;
        CheckClaimCondition(redPackageDetailDto, cryptoGiftDto, identityCode);
        
        var grabbedResult = await cryptoGiftGrain.GrabCryptoGift(identityCode, ipAddress, redPackageDetailDto.Decimal);
        if (!grabbedResult.Success)
        {
            throw new UserFriendlyException(grabbedResult.Message);
        }

        await PutIdentityCodeInCache(identityCode, ipAddress);
        return new CryptoGiftIdentityCodeDto() { IdentityCode = identityCode };
    }

    private async Task PutIdentityCodeInCache(string identityCode, string ipAddress)
    {
        if (identityCode.IsNullOrEmpty() || ipAddress.IsNullOrEmpty())
        {
            return;
        }
        var key = GetIpAddressIdentityCodeCacheKey(ipAddress);
        try
        {
            await _distributedCache.RemoveAsync(key);
            await _distributedCache.SetAsync(key, identityCode, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1)
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "PutIdentityCodeInCache error identityCode:{0} ipAddress:{1}", identityCode, ipAddress);
        }
    }

    public async Task<string> GetIdentityCodeFromCache(string ipAddress)
    {
        return await _distributedCache.GetAsync(GetIpAddressIdentityCodeCacheKey(ipAddress));
    }

    private string GetIpAddressIdentityCodeCacheKey(string ipAddress)
    {
        return "CryptoGiftIdentity:" + ipAddress;
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
        if (!redPackageDetailDto.Items.IsNullOrEmpty() && redPackageDetailDto.Items.Any(item => identityCode.Equals(item.Identity)))
        {
            throw new UserFriendlyException("You have received a crypto gift, please complete the registration as soon as possible");
        }

        var preGrabbedAmount = GetPreGrabbedAmount(cryptoGiftDto);
        if (long.Parse(redPackageDetailDto.GrabbedAmount) + preGrabbedAmount > cryptoGiftDto.TotalAmount)
        {
            throw new UserFriendlyException("Sorry, the crypto gift has been fully claimed");
        }
        if (cryptoGiftDto.PreGrabbedAmount > cryptoGiftDto.TotalAmount)
        {
            throw new UserFriendlyException("Sorry, the crypto gift has been fully claimed");
        }
        if (cryptoGiftDto.Items.Any(c => c.IdentityCode.Equals(identityCode)
                                         && (GrabbedStatus.Claimed.Equals(c.GrabbedStatus)
                                             || GrabbedStatus.Created.Equals(c.GrabbedStatus))))
        {
            throw new UserFriendlyException("You have received this crypto gift, please complete the registration as soon as possible");
        }

        if (redPackageDetailDto.Items.Any(r => r.Identity.Equals(identityCode)))
        {
            throw new UserFriendlyException("You have claimed this crypto gift~");
        }
    }

    private static long GetPreGrabbedAmount(CryptoGiftDto cryptoGiftDto)
    {
        if (!cryptoGiftDto.Items.IsNullOrEmpty())
        { 
            return cryptoGiftDto.Items
                .Where(crypto => GrabbedStatus.Created.Equals(crypto.GrabbedStatus))
                .Sum(c => c.Amount);
        }
        return 0;
    }

    public async Task PreGrabCryptoGiftAfterLogging(Guid redPackageId, Guid userId, int index, int amountDecimal, string ipAddress, string identityCode)
    {
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        if (!cryptoGiftResultDto.Success || cryptoGiftResultDto.Data == null)
        {
            throw new UserFriendlyException("PreGrabCryptoGiftAfterLogging the crypto gift does not exist");
        }
        var cryptoGiftDto = cryptoGiftResultDto.Data;
        CheckClaimAfterLoginCondition(userId, cryptoGiftDto, identityCode);
        
        PreGrabBucketItemDto preGrabBucketItemDto = GetBucketByIndex(cryptoGiftDto, index, userId, identityCode);
        if (preGrabBucketItemDto == null)
        {
            throw new UserFriendlyException("please take a break for a while~");
        }
        cryptoGiftDto.Items.Add(new PreGrabItem()
        {
            Index = preGrabBucketItemDto.Index,
            Amount = preGrabBucketItemDto.Amount,
            Decimal = amountDecimal,
            GrabbedStatus = GrabbedStatus.Claimed,
            GrabTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            IpAddress = ipAddress,
            IdentityCode = identityCode
        });
        cryptoGiftDto.PreGrabbedAmount += preGrabBucketItemDto.Amount;
        var updateResult = await cryptoGiftGrain.UpdateCryptoGift(cryptoGiftDto);
    }

    public async Task CheckClaimQuotaAfterLoginCondition(RedPackageDetailDto redPackageDetailDto, string caHash)
    {
        var redPackageId = redPackageDetailDto.Id;
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        if (!cryptoGiftResultDto.Success || cryptoGiftResultDto.Data == null)
        {
            throw new UserFriendlyException("PreGrabCryptoGiftAfterLogging the crypto gift does not exist");
        }
        var cryptoGiftDto = cryptoGiftResultDto.Data;
        if (cryptoGiftDto.PreGrabbedAmount >= cryptoGiftDto.TotalAmount)
        {
            throw new UserFriendlyException("Sorry, the crypto gift has been fully claimed");
        }
        
        var registerCacheExist = await _distributedCache.GetAsync(string.Format(CryptoGiftConstant.RegisterCachePrefix, caHash));
        if (redPackageDetailDto.IsNewUsersOnly && registerCacheExist.IsNullOrEmpty())
        {
            throw new UserFriendlyException("the crypto gift is only for new user");
        }
    }
    
    private void CheckClaimAfterLoginCondition(Guid userId, CryptoGiftDto cryptoGiftDto, string identityCode)
    {
        
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
        if (cryptoGiftDto.BucketClaimed.Any(b => b.UserId.Equals(userId)))
        {
            throw new UserFriendlyException("You have received a crypto gift, please complete the registration as soon as possible");
        }
    }
    
    private void CheckClaimConditionWhenAutoTransfer(Guid userId, CryptoGiftDto cryptoGiftDto, string identityCode)
    {
        
        if (cryptoGiftDto.PreGrabbedAmount > cryptoGiftDto.TotalAmount)
        {
            throw new UserFriendlyException("Sorry, the crypto gift has been fully claimed");
        }
        if (!cryptoGiftDto.Items.Any(c => c.IdentityCode.Equals(identityCode)
                                         && GrabbedStatus.Created.Equals(c.GrabbedStatus)))
        {
            _logger.LogWarning("CheckClaimConditionWhenAutoTransfer redPackageId:{0} userId:{1} identityCode:{2} cryptoGiftDto:{3}", 
                cryptoGiftDto.Id, userId, identityCode, JsonConvert.SerializeObject(cryptoGiftDto));
            throw new UserFriendlyException("You didn't pre grab a crypto gift");
        }
    }
    
    private PreGrabBucketItemDto GetBucketByIndex(CryptoGiftDto cryptoGiftDto, int index, Guid userId, string identityCode)
    {
        var bucket = cryptoGiftDto.BucketNotClaimed
            .FirstOrDefault(bucket => bucket.Index.Equals(index));
        if (bucket == null)
        {
            return null;
        }
        bucket.IdentityCode = identityCode;
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

    public async Task<CryptoGiftPhaseDto> GetCryptoGiftDetailAsync(Guid redPackageId, string random)
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
        var ipAddress = _ipInfoAppService.GetRemoteIp(random);
        var identityCode = GetIdentityCode(redPackageId, ipAddress);
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
        
        //before claiming, show the available crypto gift status
        var claimedResult = await _distributedCache.GetAsync(identityCode);
        if (claimedResult.IsNullOrEmpty())
        {
            //just when the user saw the detail for the first time, showed the available detail
            return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.Available, redPackageDetailDto,
                caHolderDto, nftInfoDto, "Claim and Join Portkey", "", 0, 0, 0);
        }
        
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
            var dollarValue = await GetDollarValue(redPackageDetailDto.Symbol, claimedPreGrabItem.Amount, redPackageDetailDto.Decimal);
            var remainingExpirationSeconds = GetRemainingExpirationSeconds(claimedPreGrabItem);
            return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.Claimed, redPackageDetailDto,
                caHolderDto, nftInfoDto, "You will get",  dollarValue, claimedPreGrabItem.Amount, 0, remainingExpirationSeconds);
        }

        if ((RedPackageStatus.NotClaimed.Equals(redPackageDetailDto.Status)
             || RedPackageStatus.Claimed.Equals(redPackageDetailDto.Status)))
        {
            var preGrabItem = cryptoGiftDto.Items
                .FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode)
                                          && GrabbedStatus.Created.Equals(crypto.GrabbedStatus));
            if (preGrabItem != null)
            {
                var remainingExpirationSeconds = GetRemainingExpirationSeconds(preGrabItem);
                var dollarValue = await GetDollarValue(redPackageDetailDto.Symbol, preGrabItem.Amount, redPackageDetailDto.Decimal);
                return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.GrabbedQuota, redPackageDetailDto,
                    caHolderDto, nftInfoDto, "Claim and Join Portkey", dollarValue, preGrabItem.Amount,
                    0, remainingExpirationSeconds);
            }
        }
        
        if (RedPackageStatus.FullyClaimed.Equals(redPackageDetailDto.Status))
        {
            return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.FullyClaimed, redPackageDetailDto,
                caHolderDto, nftInfoDto, "Oh no, all the crypto gifts have been claimed.", "", 0,
                0, 0);
        }
        
        if ((RedPackageStatus.NotClaimed.Equals(redPackageDetailDto.Status)
             || RedPackageStatus.Claimed.Equals(redPackageDetailDto.Status))
            && long.Parse(redPackageDetailDto.GrabbedAmount) + GetPreGrabbedAmount(cryptoGiftDto) >= cryptoGiftDto.TotalAmount)
        {
            var preGrabItem = cryptoGiftDto.Items
                .Where(crypto => GrabbedStatus.Created.Equals(crypto.GrabbedStatus))
                .MinBy(crypto => crypto.GrabTime);
            if (preGrabItem != null)
            {
                var remainingWaitingSeconds = RemainingWaitingSeconds(preGrabItem);
                return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.NoQuota, redPackageDetailDto,
                    caHolderDto, nftInfoDto, "Unclaimed gifts may be up for grabs! Try to claim once the countdown ends.", "", 0,
                    remainingWaitingSeconds, 0);
            }
        }
        
        if ((RedPackageStatus.NotClaimed.Equals(redPackageDetailDto.Status)
             || RedPackageStatus.Claimed.Equals(redPackageDetailDto.Status)))
        {
            var preGrabItem = cryptoGiftDto.Items
                .Where(crypto => crypto.IdentityCode.Equals(identityCode)
                                 && GrabbedStatus.Expired.Equals(crypto.GrabbedStatus))
                .MaxBy(crypto=>crypto.GrabTime);
            if (preGrabItem != null)
            {
                var dollarValue = await GetDollarValue(redPackageDetailDto.Symbol, preGrabItem.Amount, redPackageDetailDto.Decimal);
                return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.ExpiredReleased, redPackageDetailDto,
                    caHolderDto, nftInfoDto, "Oops, the crypto gift has expired.", dollarValue, preGrabItem.Amount,
                    0, 0);
            }
        }
        return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.Available, redPackageDetailDto,
            caHolderDto, nftInfoDto, "Claim and Join Portkey", "", 0,
            0, 0);
    }

    private long RemainingWaitingSeconds(PreGrabItem preGrabItem)
    {
        //due to the expired-checking scheduled task is executed every minute,
        //so the other client try to claim the crypto gift won't feel the deviation time
        var remainingWaitingSeconds = preGrabItem.GrabTime / 1000 
            + _cryptoGiftProvider.GetExpirationSeconds() - DateTimeOffset.Now.ToUnixTimeSeconds();
        return remainingWaitingSeconds > 0 ? remainingWaitingSeconds : 0;
    }

    private long GetRemainingExpirationSeconds(PreGrabItem preGrabItem)
    {
        long remainingExpirationSeconds =preGrabItem.GrabTime / 1000 
            + _cryptoGiftProvider.GetExpirationSeconds() - DateTimeOffset.Now.ToUnixTimeSeconds();
        return remainingExpirationSeconds < 0 ? 0 : remainingExpirationSeconds;
    }

    private static string GetIdentityCode(Guid redPackageId, string ipAddress)
    {
        return HashHelper.ComputeFrom(ipAddress + "#" + redPackageId).ToString().Replace("\"", "");
    }

    private async Task<string> GetDollarValue(string symbol, long amount, int decimals)
    {
        var dollarValue = string.Empty;
        var tokenPriceData = await _tokenPriceService.GetCurrentPriceAsync(symbol);
        if (tokenPriceData == null)
        {
            return dollarValue;
        }
        if (decimal.Equals(tokenPriceData.PriceInUsd, 0m))
        {
            return dollarValue;
        }
        var finalAmount = decimal.Multiply(tokenPriceData.PriceInUsd, amount);
        if (decimal.Equals(finalAmount, 0m))
        {
            return dollarValue;
        }
        if (decimals > 0)
        {
            finalAmount = decimal.Multiply(finalAmount, (decimal)Math.Pow(10, -decimals));
        }
        dollarValue = "≈$ " + DollarRegex().Replace(finalAmount.ToString(CultureInfo.InvariantCulture), "");

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
            Label = ETransferConstant.SgrName.Equals(redPackageDetailDto.Symbol) ? ETransferConstant.SgrDisplayName : null,
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
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
        var redPackageDetail = await grain.GetRedPackage(redPackageId);
        _logger.LogInformation("Test redPackageDetail:{0} cryptoGiftDto:{1}", redPackageId, JsonConvert.SerializeObject(redPackageDetail.Data));
        var mustQuery = new List<Func<QueryContainerDescriptor<RedPackageIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.Id).Value(redPackageDetail.Data.SessionId)));
        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, cryptoGiftIndices) = await _redPackageIndexRepository.GetListAsync(Filter);
        _logger.LogInformation("Test redPackageIndex:{0} cryptoGiftIndices:{1}", redPackageId, JsonConvert.SerializeObject(cryptoGiftIndices));
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        var cryptoGiftDto = cryptoGiftResultDto.Data;
        _logger.LogInformation("Test redPackageId:{0} cryptoGiftDto:{1}", redPackageId, JsonConvert.SerializeObject(cryptoGiftDto));
        List<PreGrabbedItemDto> items =new List<PreGrabbedItemDto>();
        foreach (var preGrabItem in cryptoGiftDto.Items)
        {
            items.Add(new PreGrabbedItemDto()
            {
                Amount = preGrabItem.Amount.ToString(),
                Decimal = preGrabItem.Decimal,
                DisplayType = CryptoGiftDisplayType.Pending,
                ExpirationTime = preGrabItem.GrabTime + _cryptoGiftProvider.GetExpirationSeconds() * 1000,
                GrabTime = preGrabItem.GrabTime,
                Username = preGrabItem.IdentityCode
            });
        }
        List<PreGrabBucketItemAppDto> bucketNotClaimed = new List<PreGrabBucketItemAppDto>();
        foreach (var preGrabBucketItemDto in cryptoGiftDto.BucketNotClaimed)
        {
            bucketNotClaimed.Add(new PreGrabBucketItemAppDto()
            {
                Amount = preGrabBucketItemDto.Amount,
                IdentityCode = preGrabBucketItemDto.IdentityCode,
                Index = preGrabBucketItemDto.Index,
                UserId = preGrabBucketItemDto.UserId
            });
        }
        List<PreGrabBucketItemAppDto> bucketClaimed = new List<PreGrabBucketItemAppDto>();
       foreach (var preGrabBucketItemDto in cryptoGiftDto.BucketClaimed)
       {
           bucketClaimed.Add(new PreGrabBucketItemAppDto()
           {
               Amount = preGrabBucketItemDto.Amount,
               IdentityCode = preGrabBucketItemDto.IdentityCode,
               Index = preGrabBucketItemDto.Index,
               UserId = preGrabBucketItemDto.UserId
           });
       }
        return new CryptoGiftAppDto()
        {
            Id = cryptoGiftDto.Id,
            SenderId = cryptoGiftDto.SenderId,
            TotalAmount = cryptoGiftDto.TotalAmount,
            PreGrabbedAmount = cryptoGiftDto.PreGrabbedAmount,
            CreateTime = cryptoGiftDto.CreateTime,
            Symbol = cryptoGiftDto.Symbol,
            Items = items,
            BucketNotClaimed = bucketNotClaimed,
            BucketClaimed = bucketClaimed,
        };
    }

    public async Task<CryptoGiftPhaseDto> GetCryptoGiftLoginDetailAsync(string caHash, Guid redPackageId, string random)
    {
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
        var redPackageDetail = await grain.GetRedPackage(redPackageId);
        if (!redPackageDetail.Success || redPackageDetail.Data == null)
        {
            throw new UserFriendlyException("the red package does not exist");
        }
        Guid receiverId = await GetUserId(caHash, 0);
        var redPackageDetailDto = redPackageDetail.Data;
        var ipAddress = _ipInfoAppService.GetRemoteIp(random);
        var identityCode = GetIdentityCode(redPackageId, ipAddress);
        
        // get nft info
        var nftInfoDto = await GetNftInfo(redPackageDetailDto);
        var caHolderSenderGrain = _clusterClient.GetGrain<ICAHolderGrain>(redPackageDetailDto.SenderId);
        var caHolderSenderResult = await caHolderSenderGrain.GetCaHolder();
        if (!caHolderSenderResult.Success || caHolderSenderResult.Data == null)
        {
            throw new UserFriendlyException("the crypto gift sender does not exist");
        }
        var sender = caHolderSenderResult.Data;
        
        if (RedPackageStatus.Expired.Equals(redPackageDetailDto.Status))
        {
            return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.Expired, redPackageDetailDto,
                sender, nftInfoDto, "Oops, the crypto gift has expired.", "",
                0);
        }
        
        var registerCacheExist = await _distributedCache.GetAsync(string.Format(CryptoGiftConstant.RegisterCachePrefix, caHash));
        if (redPackageDetailDto.IsNewUsersOnly && registerCacheExist.IsNullOrEmpty())
        {
            return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.OnlyNewUsers, redPackageDetailDto,
                sender, nftInfoDto, "Oops! This is an exclusive gift for new users", "", 0);
        }
        
        CryptoGiftDto cryptoGiftDto = await DoGetCryptoGiftAfterLogin(redPackageId);
        var preGrabClaimedItem = cryptoGiftDto.Items.FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode)
                                                       && GrabbedStatus.Claimed.Equals(crypto.GrabbedStatus));
        if (preGrabClaimedItem != null)
        {
            var subPrompt = $"You've already claimed this crypto gift and received" +
                            $" {preGrabClaimedItem.Amount} {redPackageDetailDto.Symbol}. You can't claim it again.";
            var dollarValue = await GetDollarValue(redPackageDetailDto.Symbol, preGrabClaimedItem.Amount, redPackageDetailDto.Decimal);
            var visited = await _distributedCache.GetAsync(string.Format(CryptoGiftConstant.CryptoGiftClaimedVisitedPrefix, caHash, redPackageId));
            if (visited.IsNullOrEmpty())
            {
                await _distributedCache.SetAsync(string.Format(CryptoGiftConstant.CryptoGiftClaimedVisitedPrefix, caHash, redPackageId), CryptoGiftConstant.CryptoGiftClaimedVisitedValue);
                return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.Claimed, redPackageDetailDto,
                    sender, nftInfoDto, subPrompt, dollarValue, preGrabClaimedItem.Amount);
            }
            else
            {
                return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.ClaimedVisited, redPackageDetailDto,
                    sender, nftInfoDto, subPrompt, dollarValue, preGrabClaimedItem.Amount);
            }
        }

        var grabItemDto = redPackageDetailDto.Items.FirstOrDefault(red => red.UserId.Equals(receiverId));
        if (grabItemDto != null)
        {
            var subPrompt = $"You've already claimed this crypto gift and received" +
                            $" {grabItemDto.Amount} {redPackageDetailDto.Symbol}. You can't claim it again.";
            var dollarValue = await GetDollarValue(redPackageDetailDto.Symbol, long.Parse(grabItemDto.Amount), redPackageDetailDto.Decimal);
            var visited = await _distributedCache.GetAsync(string.Format(CryptoGiftConstant.CryptoGiftClaimedVisitedPrefix, caHash, redPackageId));
            if (visited.IsNullOrEmpty())
            {
                await _distributedCache.SetAsync(string.Format(CryptoGiftConstant.CryptoGiftClaimedVisitedPrefix, caHash, redPackageId), CryptoGiftConstant.CryptoGiftClaimedVisitedValue);
                return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.Claimed, redPackageDetailDto,
                    sender, nftInfoDto,  subPrompt, dollarValue, long.Parse(grabItemDto.Amount));
            }
            else
            {
                return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.ClaimedVisited, redPackageDetailDto,
                    sender, nftInfoDto,  subPrompt, dollarValue, long.Parse(grabItemDto.Amount));
            }
        }

        if ((RedPackageStatus.NotClaimed.Equals(redPackageDetailDto.Status)
             || RedPackageStatus.Claimed.Equals(redPackageDetailDto.Status)))
        {
            //check the latest status is expired
            var preGrabItem = cryptoGiftDto.Items
                .Where(crypto => crypto.IdentityCode.Equals(identityCode))
                .MaxBy(crypto => crypto.GrabTime);
            if (preGrabItem is { GrabbedStatus: GrabbedStatus.Expired })
            {
                return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.ExpiredReleased, redPackageDetailDto,
                    sender, nftInfoDto, "Oops, the crypto gift has expired.", "",
                    0);
            }
        }
        
        if (RedPackageStatus.FullyClaimed.Equals(redPackageDetailDto.Status))
        {
            return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.FullyClaimed, redPackageDetailDto,
                sender, nftInfoDto,  "Oh no, all the crypto gifts have been claimed.", "",
                0);
        }
        
        return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.Available, redPackageDetailDto,
            sender, nftInfoDto,  "Claim and Join Portkey", "",
                        0);
    }

    private async Task<CryptoGiftDto> GetCryptoGiftDtoAfterLoginAsync(Guid redPackageId, Guid receiverId, int retryTimes)
    {
        if (retryTimes >= 9)
        {
            return await DoGetCryptoGiftAfterLogin(redPackageId);
        }
        var cryptoGiftCacheDtoStr = await _distributedCache.GetAsync($"CryptoGiftUpdatedResult:{receiverId}:{redPackageId}");
        if (cryptoGiftCacheDtoStr.IsNullOrEmpty())
        {
            _logger.LogInformation($"GetCryptoGiftDtoAfterLoginAsync redPackageId:{redPackageId} receiverId:{receiverId} retryTimes:{retryTimes}");
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            return await GetCryptoGiftDtoAfterLoginAsync(redPackageId, receiverId, retryTimes + 1);
        }
        var cryptoGiftCacheDto = JsonConvert.DeserializeObject<CryptoGiftCacheDto>(cryptoGiftCacheDtoStr);
        if (!cryptoGiftCacheDto.Success)
        {
            return await DoGetCryptoGiftAfterLogin(redPackageId);
        }
        return cryptoGiftCacheDto.CryptoGiftDto;
    }

    private async Task<CryptoGiftDto> DoGetCryptoGiftAfterLogin(Guid redPackageId)
    {
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        if (!cryptoGiftResultDto.Success || cryptoGiftResultDto.Data == null)
        {
            throw new UserFriendlyException("the crypto gift does not exist");
        }

        return cryptoGiftResultDto.Data;
    }

    private async Task<Guid> GetUserId(string caHash, int retryTimes)
    {
        if (retryTimes > 2)
        {
            throw new UserFriendlyException("the user doesn't exist");
        }

        var user = await _userManager.FindByNameAsync(caHash);
        if (user != null)
        {
            return user.Id;
        }
        var userIdStr = await _distributedCache.GetAsync($"UserLoginHandler:{caHash}");
        if (!userIdStr.IsNullOrEmpty())
        {
            return Guid.Parse(userIdStr);
        }
        
        Thread.Sleep(TimeSpan.FromSeconds(1));
        return await GetUserId(caHash, retryTimes + 1);
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
                nftInfoDto.ImageUrl = IpfsImageUrlHelper.TryGetIpfsImageUrl(nftInfoDto.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
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
            Label = ETransferConstant.SgrName.Equals(redPackageDetailDto.Symbol) ? ETransferConstant.SgrDisplayName : null,
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
    
    public async Task CryptoGiftTransferToRedPackage(Guid userId, string caHash, string caAddress, ReferralInfo referralInfo, bool isNewUser, string ipAddress)
    {
        _logger.LogInformation("CryptoGiftTransferToRedPackage userId:{0},caAddress:{1},referralInfo:{2},isNewUser:{3}", userId, caAddress, JsonConvert.SerializeObject(referralInfo), isNewUser);
        if (referralInfo is not { ProjectCode: CommonConstant.CryptoGiftProjectCode } || referralInfo.ReferralCode.IsNullOrEmpty())
        {
            _logger.LogInformation("CryptoGiftTransferToRedPackage ProjectCode isn't 20000, referralInfo={0}", JsonConvert.SerializeObject(referralInfo));
            return;
        }
       
        var infos = referralInfo.ReferralCode.Split("#");
        string identityCode = infos[1];
        Guid redPackageId = Guid.Parse(infos[0]);
        if (Guid.Empty.Equals(userId))
        {
            await UpdateCryptoGiftCacheResultFalse(userId, redPackageId);
            _logger.LogInformation($"Transfer cached failed cause userId redPackageId:{redPackageId}");
            throw new UserFriendlyException($"the user userId:{userId} doesn't exist");
        }
        var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGran>(redPackageId);
        var cryptoGiftResultDto = await cryptoGiftGrain.GetCryptoGift(redPackageId);
        if (!cryptoGiftResultDto.Success || cryptoGiftResultDto.Data == null)
        {
            await UpdateCryptoGiftCacheResultFalse(userId, redPackageId);
            _logger.LogInformation($"Transfer cached failed cause cryptoGift from mongo redPackageId:{redPackageId}");
            throw new UserFriendlyException("the crypto gift does not exist");
        }
        var cryptoGiftDto = cryptoGiftResultDto.Data;
        try
        {
            CheckClaimConditionWhenAutoTransfer(userId, cryptoGiftDto, identityCode);
        }
        catch (Exception e)
        {
            await UpdateCryptoGiftCacheResultFalse(userId, redPackageId);
            throw new UserFriendlyException(e.Message);
        }
        
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
        var redPackageDetail = await grain.GetRedPackage(redPackageId);
        if (!redPackageDetail.Success || redPackageDetail.Data == null)
        {
            await UpdateCryptoGiftCacheResultFalse(userId, redPackageId);
            throw new UserFriendlyException("the red package does not exist");
        }
        var redPackageDetailDto = redPackageDetail.Data;
        if (redPackageDetailDto.IsNewUsersOnly && !isNewUser)
        {
            //return the quota of the crypto gift
            ReturnPreGrabbedCryptoGift(identityCode, cryptoGiftDto);
            var returnResult = await cryptoGiftGrain.UpdateCryptoGift(cryptoGiftDto);
            _logger.LogInformation("CryptoGiftTransferToRedPackage returnResult:{}", JsonConvert.SerializeObject(returnResult));
            await UpdateCryptoGiftCacheResultFalse(userId, redPackageId);
            _logger.LogInformation($"Transfer failed cause not new user redPackageId:{redPackageId}");
            return;
        }
        var preGrabBucketItemDto = GetClaimedCryptoGift(userId, identityCode, redPackageId, cryptoGiftDto);
        //0 make sure the client is whether or not a new one, according to the new user rule
        if (!Enum.IsDefined(redPackageDetailDto.RedPackageDisplayType) || RedPackageDisplayType.Common.Equals(redPackageDetailDto.RedPackageDisplayType))
        {
            await UpdateCryptoGiftCacheResultFalse(userId, redPackageId);
            return;
        }
        
        //1 red package: amount/item
        var redPackageUpdateResult = await grain.CryptoGiftTransferToRedPackage(userId, caAddress, preGrabBucketItemDto, ipAddress, identityCode);
        _logger.LogInformation("CryptoGiftTransferToRedPackage redPackageUpdateResult:{0}", JsonConvert.SerializeObject(redPackageUpdateResult));
        if (redPackageUpdateResult.Success)
        {
            _logger.LogInformation("sent PayRedPackageEto RedPackageId:{0}", redPackageId);
            //2 crypto gift: amount/item
            _logger.LogInformation("CryptoGiftTransferToRedPackage GetClaimedCryptoGift:{0}", JsonConvert.SerializeObject(preGrabBucketItemDto));
            var updateCryptoGiftResult = await cryptoGiftGrain.UpdateCryptoGift(cryptoGiftDto);
            _logger.LogInformation("CryptoGiftTransferToRedPackage updateCryptoGiftResult:{0}", JsonConvert.SerializeObject(updateCryptoGiftResult));
            await _distributedEventBus.PublishAsync(new PayRedPackageEto()
            {
                RedPackageId = redPackageId,
                DisplayType = RedPackageDisplayType.CryptoGift,
                ReceiverId = userId
            });
        }
        else
        {
            await UpdateCryptoGiftCacheResultFalse(userId, redPackageId);
        }
    }

    // private async Task DelayTransferProcess(Guid redPackageId, string chainId, string caHash, string caAddress)
    // {
    //     var executed = await ExecutedDelayedTask(redPackageId, chainId, caHash, caAddress);
    //     if (executed)
    //     {
    //         return;
    //     }
    //     var i = 1;
    //     while (i <= _cryptoGiftProvider.GetTransferDelayedRetryTimes())
    //     {
    //         await Task.Delay(TimeSpan.FromSeconds(_cryptoGiftProvider.GetTransferDelayedIntervalSeconds() * i));
    //         var succeed = await ExecutedDelayedTask(redPackageId, chainId, caHash, caAddress);
    //         if (succeed)
    //         {
    //             return;
    //         }
    //         i++;
    //     }
    // }
    
    // private async Task<bool> ExecutedDelayedTask(Guid redPackageId, string chainId, string caHash, string caAddress)
    // {
    //     bool existed = false;
    //     try
    //     {
    //         var guardiansDto = await _contactProvider.GetCaHolderInfoByAddressAsync(new List<string>() {caAddress}, chainId);
    //         _logger.LogInformation("redPackageId:{0} executed delayed task chainId:{1} caHash:{2} caAddress:{3} guardiansDto:{4}", redPackageId, chainId, caHash, caAddress, JsonConvert.SerializeObject(guardiansDto)); 
    //         existed = guardiansDto.CaHolderInfo.Any(guardian => guardian.CaHash.Equals(caHash)
    //                                                                 && guardian.CaAddress.Equals(caAddress)
    //                                                                 && guardian.ChainId.Equals(chainId));
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError(e, "redPackageId:{0} executed delayed task error", redPackageId);
    //     }
    //     if (!existed)
    //     {
    //         return false;
    //     }
    //
    //     await _distributedEventBus.PublishAsync(new PayRedPackageEto()
    //     {
    //         RedPackageId = redPackageId,
    //         DisplayType = RedPackageDisplayType.CryptoGift,
    //         ReceiverId = userId
    //     });
    //     return true;
    //
    // }

    private async Task UpdateCryptoGiftCacheResultFalse(Guid userId, Guid redPackageId)
    {
        await _distributedCache.SetAsync($"CryptoGiftUpdatedResult:{userId}:{redPackageId}", JsonConvert.SerializeObject(new CryptoGiftCacheDto()
        {
            Success = false
        }), new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(60)
        });
    }

    private void ReturnPreGrabbedCryptoGift(string identityCode, CryptoGiftDto cryptoGiftDto)
    {
        var preGrabItem = cryptoGiftDto.Items
            .FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode) && GrabbedStatus.Created.Equals(crypto.GrabbedStatus));
        if (preGrabItem == null)
        {
            throw new UserFriendlyException($"return red package:{cryptoGiftDto.Id} is not crypto gift, identityCode:{identityCode}");
        }

        PreGrabBucketItemDto preGrabBucketItemDto = cryptoGiftDto.BucketClaimed.FirstOrDefault(bucket => bucket.Index.Equals(preGrabItem.Index));
        if (preGrabBucketItemDto == null)
        {
            throw new UserFriendlyException($"old user grabbed new user crypto gift, return the quoter failed,red packageId:{cryptoGiftDto.Id}");
        }
        cryptoGiftDto.PreGrabbedAmount -= preGrabBucketItemDto.Amount;
        cryptoGiftDto.BucketNotClaimed.Add(preGrabBucketItemDto);
        cryptoGiftDto.BucketClaimed.Remove(preGrabBucketItemDto);
        cryptoGiftDto.Items.Remove(preGrabItem);
    }

    private static PreGrabBucketItemDto GetClaimedCryptoGift(Guid userId, string identityCode, Guid redPackageId,
        CryptoGiftDto cryptoGiftDto)
    {
        var preGrabItem = cryptoGiftDto.Items
            .FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode)
                             && GrabbedStatus.Created.Equals(crypto.GrabbedStatus));
        if (preGrabItem == null)
        {
            throw new UserFriendlyException($"the user:{userId} identity:{identityCode} didn't get a crypto gift:{redPackageId} or the crypto gift is expired");
        }
        preGrabItem.GrabbedStatus = GrabbedStatus.Claimed;
        var preGrabBucketItemDto = cryptoGiftDto.BucketClaimed
            .FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode)
                                      && crypto.Amount.Equals(preGrabItem.Amount));
        if (preGrabBucketItemDto == null)
        {
            throw new UserFriendlyException($"the user:{userId} identity:{identityCode} didn't grab a crypto gift:{redPackageId}");
        }
        preGrabBucketItemDto.UserId = userId;
        return preGrabBucketItemDto;
    }

    public (string, string) GetIpAddressAndIdentity(Guid redPackageId, string random)
    {
        var ipAddress = _ipInfoAppService.GetRemoteIp(random);
        var identityCode = GetIdentityCode(redPackageId, ipAddress);
        return new ValueTuple<string, string>(ipAddress, identityCode);
    }
    
    public async Task<List<CryptoGiftSentNumberDto>> ComputeCryptoGiftNumber(bool newUsersOnly, string[] symbols, long createTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RedPackageIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.IsNewUsersOnly).Value(newUsersOnly)));
        mustQuery.Add(q => 
            q.Term(i => i.Field(f => f.RedPackageDisplayType).Value((int)RedPackageDisplayType.CryptoGift)));
        mustQuery.Add(q=>
            q.Terms(i => i.Field(f => f.Symbol).Terms(symbols)));
        mustQuery.Add(q => 
            q.Range(i => i.Field(f => f.CreateTime).GreaterThan(createTime)));
        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, cryptoGiftIndices) = await _redPackageIndexRepository.GetListAsync(Filter);
        cryptoGiftIndices = cryptoGiftIndices.Where(crypto => crypto.CreateTime > createTime).ToList();
        var cryptoGiftCountByDate = cryptoGiftIndices.GroupBy(crypto => ConvertTimestampToDate(crypto.CreateTime))
            .Select(group => new CryptoGiftSentNumberDto { Date = group.Key, Number = group.Count()}).ToList();
        return cryptoGiftCountByDate.OrderBy(c => c.Date).ToList();
    }
    
    private static string ConvertTimestampToDate(long timestamp)
    {
        var dtDateTime = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);
        return dtDateTime.AddSeconds(timestamp / 1000).ToLocalTime().ToString("yyyy-MM-dd");
    }

    public async Task<List<CryptoGiftClaimDto>> ComputeCryptoGiftClaimStatistics(bool newUsersOnly, string[] symbols,
        long createTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<RedPackageIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.IsNewUsersOnly).Value(newUsersOnly)));
        mustQuery.Add(q => 
            q.Term(i => i.Field(f => f.RedPackageDisplayType).Value((int)RedPackageDisplayType.CryptoGift)));
        mustQuery.Add(q=>
            q.Terms(i => i.Field(f => f.Symbol).Terms(symbols)));
        mustQuery.Add(q => 
            q.Range(i => i.Field(f => f.CreateTime).GreaterThan(createTime)));
        QueryContainer Filter(QueryContainerDescriptor<RedPackageIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, cryptoGiftIndices) = await _redPackageIndexRepository.GetListAsync(Filter);
        cryptoGiftIndices = cryptoGiftIndices.Where(crypto => crypto.CreateTime > createTime).ToList();
        _logger.LogInformation("====================cryptoGiftIndices:{0} ", JsonConvert.SerializeObject(cryptoGiftIndices));
        var details = new List<RedPackageDetailDto>();
        foreach (var cryptoGiftIndex in cryptoGiftIndices)
        {
            var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(cryptoGiftIndex.RedPackageId);
            var redPackageDetail = await grain.GetRedPackage(cryptoGiftIndex.RedPackageId);
            if (!redPackageDetail.Success || redPackageDetail.Data == null)
            {
                continue;
            }
            details.Add(redPackageDetail.Data);
        }

        _logger.LogInformation("====================redPackageDetail:{0} ", JsonConvert.SerializeObject(details));
        var cryptoGiftClaimDtos = details.Select(detail => new CryptoGiftClaimDto()
        {
            UserId = detail.SenderId,
            Number = 1,
            Grabbed = detail.Grabbed,
            Count = detail.Count,
            ChainId = detail.ChainId
        }).ToList();
        IDictionary<string, string> groupToCaAddress = new Dictionary<string, string>();
        var userIdChainId = cryptoGiftClaimDtos.GroupBy(c => c.UserId + "#" + c.ChainId)
            .Select(group => new {Group = group.Key});
        _logger.LogInformation("===========userIdChainId:{0}", JsonConvert.SerializeObject(userIdChainId));
        foreach (var item in userIdChainId)
        {
            if (groupToCaAddress.TryGetValue(item.Group, out var value))
            {
                continue;
            }

            var split = item.Group.Split("#");
            groupToCaAddress.Add(item.Group, await GetCaAddress(Guid.Parse(split[0]), split[1]));
        }
        _logger.LogInformation("===========groupToCaAddress:{0}", JsonConvert.SerializeObject(groupToCaAddress));
        foreach (var cryptoGiftClaimDto in cryptoGiftClaimDtos)
        {
            var key = cryptoGiftClaimDto.UserId + "#" + cryptoGiftClaimDto.ChainId;
            cryptoGiftClaimDto.CaAddress = groupToCaAddress[key];
        }

        return  cryptoGiftClaimDtos.GroupBy(crypto => crypto.CaAddress)
            .Select(group => new CryptoGiftClaimDto()
            {
                CaAddress = group.Key,
                Number = group.Count(),
                Count = group.Sum(g => g.Count),
                Grabbed = group.Sum(g => g.Grabbed)
            }).ToList();
    }

    private async Task<string> GetCaAddress(Guid userId, string chainId)
    {
        var caHolderIndex = await _userAssetsProvider.GetCaHolderIndexAsync(userId);
        var caHash = caHolderIndex.CaHash;
        try
        {
            _logger.LogInformation("---------------GetCaAddress userId:{0} caHash:{1} chainId:{2}", userId, caHash, chainId);
            var result = await _contractProvider.GetHolderInfoAsync(Hash.LoadFromHex(caHash), null, chainId);
            if (result != null)
            {
                return result.CaAddress.ToBase58();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "get holder from chain error, userId:{userId}, caHash:{caHash}", userId.ToString(), caHash);
        }

        return string.Empty;
    }

    public async Task CryptoGiftHistoryStates()
    {
        var current = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var symbols = new string[] { "ELF", "USDT", "SGR-1" };
        var joinedSymbols = string.Join(",", symbols);
        var newUsersNumberDtos = await ComputeCryptoGiftNumber(true, symbols, 1719590400000);
        await SaveCryptoGiftNumberStatsAsync(newUsersNumberDtos, true, joinedSymbols, current);
        
        var oldUsersNumberDtos = await ComputeCryptoGiftNumber(false, symbols, 1719590400000);
        await SaveCryptoGiftNumberStatsAsync(oldUsersNumberDtos, false, joinedSymbols, current);

        var newUsersCryptoGiftClaimStatistics = await ComputeCryptoGiftClaimStatistics(true, symbols, 1719590400000);
        await SaveCryptoGiftDetailStatsAsync(newUsersCryptoGiftClaimStatistics, true, joinedSymbols, current);
        
        var oldUsersCryptoGiftClaimStatistics = await ComputeCryptoGiftClaimStatistics(false, symbols, 1719590400000);
        await SaveCryptoGiftDetailStatsAsync(oldUsersCryptoGiftClaimStatistics, false, joinedSymbols, current);
    }
    private async Task SaveCryptoGiftDetailStatsAsync(List<CryptoGiftClaimDto> details,
        bool newUsersOnly, string joinedSymbols, long current)
    {
        if (newUsersOnly)
        {
            foreach (var dto in details)
            {
                await _newUsersOnlyDetailRepository.AddOrUpdateAsync(new CryptoGiftNewUsersOnlyDetailStatsIndex
                {
                    Id = dto.CaAddress,
                    Symbols = joinedSymbols,
                    CaAddress = dto.CaAddress,
                    Number = dto.Number,
                    Count = dto.Count,
                    Grabbed = dto.Grabbed,
                    CreateTime = current
                });
            }
        }
        else
        {
            foreach (var dto in details)
            {
                await _oldUsersDetailRepository.AddOrUpdateAsync(new CryptoGiftOldUsersDetailStatsIndex
                {
                    Id = dto.CaAddress,
                    Symbols = joinedSymbols,
                    CaAddress = dto.CaAddress,
                    Number = dto.Number,
                    Count = dto.Count,
                    Grabbed = dto.Grabbed,
                    CreateTime = current
                });
            }
        }
    }

    private async Task SaveCryptoGiftNumberStatsAsync(List<CryptoGiftSentNumberDto> numberDtos, bool newUsersOnly, string joinedSymbols, long current)
    {
        if (newUsersOnly)
        {
            foreach (var cryptoGiftSentNumberDto in numberDtos)
            {
                try
                {
                    await _newUsersOnlyNumStatsRepository.AddOrUpdateAsync(new CryptoGiftNewUsersOnlyNumStatsIndex
                    {
                        Id = cryptoGiftSentNumberDto.Date,
                        Date = cryptoGiftSentNumberDto.Date,
                        Number = cryptoGiftSentNumberDto.Number,
                        Symbols = joinedSymbols,
                        CreateTime = current
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "add or update crypto gift error, cryptoGiftSentNumberDto:{0}", JsonConvert.SerializeObject(cryptoGiftSentNumberDto));
                }
            }
        }
        else
        {
            foreach (var cryptoGiftSentNumberDto in numberDtos)
            {
                try
                {
                    await _oldUsersNumStatsRepository.AddOrUpdateAsync(new CryptoGiftOldUsersNumStatsIndex
                    {
                        Id = cryptoGiftSentNumberDto.Date,
                        Date = cryptoGiftSentNumberDto.Date,
                        Number = cryptoGiftSentNumberDto.Number,
                        Symbols = joinedSymbols,
                        CreateTime = current
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "add or update crypto gift error, cryptoGiftSentNumberDto:{0}", JsonConvert.SerializeObject(cryptoGiftSentNumberDto));
                }
            }
        }
    }
}