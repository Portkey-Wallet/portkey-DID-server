using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
using CAServer.Options;
using CAServer.RedPackage.Dtos;
using CAServer.Tokens.TokenPrice;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver.Linq;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
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
    private const long ExtraDeviationSeconds = 120;
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
        _distributedCache = distributedCache;
        _ipfsOptions = ipfsOptions.Value;
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
        var identityCode = GetIdentityCode(redPackageId, ipAddress);
        _logger.LogInformation($"pre grab data sync error redPackageId:{redPackageId}," +
                               $"PreGrabCrypto identityCode:{identityCode}, ipAddress:{ipAddress}");
        await _distributedCache.SetAsync(identityCode, "Claimed", new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
        });
        
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
        if (!redPackageDetailDto.Items.IsNullOrEmpty() && redPackageDetailDto.Items.Any(item => identityCode.Equals(item.Identity)))
        {
            throw new UserFriendlyException("You have received a crypto gift, please complete the registration as soon as possible");
        }

        long preGrabbedAmount = 0;
        if (!redPackageDetailDto.Items.IsNullOrEmpty())
        {
            var preGrabItems = cryptoGiftDto.Items.Where(crypto => GrabbedStatus.Created.Equals(crypto.GrabbedStatus)
                    || GrabbedStatus.Claimed.Equals(crypto.GrabbedStatus)).ToList();
            if (!preGrabItems.IsNullOrEmpty())
            {
                foreach (var preGrabItem in preGrabItems)
                {
                    if (redPackageDetailDto.Items.Any(red => red.Identity.Equals(preGrabItem.IdentityCode)))
                    {
                        continue;
                    }
                    preGrabbedAmount += preGrabItem.Amount;
                }
            }
        }
        if (long.Parse(redPackageDetailDto.GrabbedAmount) + preGrabbedAmount >= cryptoGiftDto.TotalAmount)
        {
            throw new UserFriendlyException("Sorry, the crypto gift has been fully claimed");
        }
        if (cryptoGiftDto.PreGrabbedAmount >= cryptoGiftDto.TotalAmount)
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
        _logger.LogInformation("PreGrabCryptoGiftAfterLogging before update:{0}", JsonConvert.SerializeObject(cryptoGiftDto));
        var updateResult = await cryptoGiftGrain.UpdateCryptoGift(cryptoGiftDto);
        _logger.LogInformation("PreGrabCryptoGiftAfterLogging updateResult:{0}", JsonConvert.SerializeObject(updateResult));
    }

    public async Task CheckClaimQuotaAfterLoginCondition(Guid redPackageId)
    {
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

    public async Task<CryptoGiftPhaseDto> GetCryptoGiftDetailAsync(Guid redPackageId)
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
        _logger.LogInformation($"pre grab data sync error redPackageId:{redPackageId}," +
                               $"GetCryptoGiftDetail identityCode:{identityCode}, claimedResult:{claimedResult},ipAddress:{ipAddress}");
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
        
        if ((RedPackageStatus.NotClaimed.Equals(redPackageDetailDto.Status)
             || RedPackageStatus.Claimed.Equals(redPackageDetailDto.Status))
            && cryptoGiftDto.PreGrabbedAmount >= cryptoGiftDto.TotalAmount)
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
        
        if (RedPackageStatus.FullyClaimed.Equals(redPackageDetailDto.Status)
                    || cryptoGiftDto.PreGrabbedAmount >= cryptoGiftDto.TotalAmount)
        {
            return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.FullyClaimed, redPackageDetailDto,
                caHolderDto, nftInfoDto, "Oh no, all the crypto gifts have been claimed.", "", 0,
                0, 0);
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
        _logger.LogInformation("get crypto detail default logic redPackageId:{0}, get cache identityCode:{1}", redPackageId, identityCode);
        return GetUnLoginCryptoGiftPhaseDto(CryptoGiftPhase.Available, redPackageDetailDto,
            caHolderDto, nftInfoDto, "Claim and Join Portkey", "", 0,
            0, 0);
    }

    private long RemainingWaitingSeconds(PreGrabItem preGrabItem)
    {
        //due to the expired-checking scheduled task is executed every minute,
        //so the other client try to claim the crypto gift won't feel the deviation time
        var remainingWaitingSeconds = preGrabItem.GrabTime / 1000 + ExtraDeviationSeconds
            + _cryptoGiftProvider.GetExpirationSeconds() - DateTimeOffset.Now.ToUnixTimeSeconds();
        remainingWaitingSeconds = remainingWaitingSeconds > 0 ? remainingWaitingSeconds : 0;
        return remainingWaitingSeconds;
    }

    private long GetRemainingExpirationSeconds(PreGrabItem preGrabItem)
    {
        return preGrabItem.GrabTime / 1000
               + _cryptoGiftProvider.GetExpirationSeconds()
               - DateTimeOffset.Now.ToUnixTimeSeconds();
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

    public async Task<CryptoGiftPhaseDto> GetCryptoGiftLoginDetailAsync(string caHash, Guid redPackageId)
    {
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
        var redPackageDetail = await grain.GetRedPackage(redPackageId);
        if (!redPackageDetail.Success || redPackageDetail.Data == null)
        {
            throw new UserFriendlyException("the red package does not exist");
        }
        Guid receiverId = await GetUserId(caHash, 0);
        Stopwatch sw = new Stopwatch();
        sw.Start();
        CryptoGiftDto cryptoGiftDto;
        var refreshCachedResult = await _distributedCache.GetAsync($"RefreshedPage:{caHash}:{redPackageId}");
        if (refreshCachedResult.IsNullOrEmpty())
        {
            cryptoGiftDto = await GetCryptoGiftDtoAfterLoginAsync(redPackageId, receiverId, 0);
        }
        else
        {
            cryptoGiftDto = await DoGetCryptoGiftAfterLogin(redPackageId);
        }
        await _distributedCache.SetAsync($"RefreshedPage:{caHash}:{redPackageId}", "true");
        _logger.LogInformation("get crypto gift from cache cryptoGiftDto:{0}", JsonConvert.SerializeObject(cryptoGiftDto));
        sw.Stop();
        _logger.LogInformation($"statistics GetCryptoGiftDtoAfterLoginAsync cost:{sw.ElapsedMilliseconds} ms");
        var redPackageDetailDto = redPackageDetail.Data;
        var ipAddress = _ipInfoAppService.GetRemoteIp();
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
        
        // var grabItemDto = redPackageDetailDto.Items.FirstOrDefault(red => red.UserId.Equals(receiverId));
        var preGrabClaimedItem = cryptoGiftDto.Items.FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode)
                                                       && GrabbedStatus.Claimed.Equals(crypto.GrabbedStatus));
        if (preGrabClaimedItem != null)
        {
            var subPrompt = $"You've already claimed this crypto gift and received" +
                            $" {preGrabClaimedItem.Amount} {redPackageDetailDto.Symbol}. You can't claim it again.";
            var dollarValue = await GetDollarValue(redPackageDetailDto.Symbol, preGrabClaimedItem.Amount, redPackageDetailDto.Decimal);
            return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.Claimed, redPackageDetailDto,
                sender, nftInfoDto,  subPrompt, dollarValue, preGrabClaimedItem.Amount);
        }

        var grabItemDto = redPackageDetailDto.Items.FirstOrDefault(red => red.UserId.Equals(receiverId));
        if (grabItemDto != null)
        {
            var subPrompt = $"You've already claimed this crypto gift and received" +
                            $" {grabItemDto.Amount} {redPackageDetailDto.Symbol}. You can't claim it again.";
            var dollarValue = await GetDollarValue(redPackageDetailDto.Symbol, long.Parse(grabItemDto.Amount), redPackageDetailDto.Decimal);
            return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.Claimed, redPackageDetailDto,
                sender, nftInfoDto,  subPrompt, dollarValue, long.Parse(grabItemDto.Amount));
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
        
        var receiverGrain = _clusterClient.GetGrain<ICAHolderGrain>(receiverId);
        var receiverResult = await receiverGrain.GetCaHolder();
        if (!receiverResult.Success || receiverResult.Data == null)
        {
            throw new UserFriendlyException("the crypto gift reciever does not exist");
        }
        var receiver = receiverResult.Data;
        var isNewUserRegistered = receiver.IsNewUserRegistered; //isNewUserFromCache ?? receiver.IsNewUserRegistered;
        if (redPackageDetailDto.IsNewUsersOnly && !isNewUserRegistered)
        {
            return GetLoggedCryptoGiftPhaseDto(CryptoGiftPhase.OnlyNewUsers, redPackageDetailDto,
                sender, nftInfoDto, "Oops! This is an exclusive gift for new users", "", 0);
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
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
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
            _logger.LogInformation("===============get from user manager cahash:{0}", caHash);
            return user.Id;
        }
        var userIdStr = await _distributedCache.GetAsync($"UserLoginHandler:{caHash}");
        if (!userIdStr.IsNullOrEmpty())
        {
            _logger.LogInformation("===============get from cache cahash:{0}", caHash);
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

    public async Task TestCryptoGiftTransferToRedPackage(string caHash, string caAddress,
        Guid id, string identityCode, bool isNewUser)
    {
        var user = await _userManager.FindByNameAsync(caHash);
        await CryptoGiftTransferToRedPackage(user.Id, caAddress, new ReferralInfo(){
            ReferralCode = id.ToString() + "#" + identityCode,
            ProjectCode = "20000"
            }, isNewUser, _ipInfoAppService.GetRemoteIp());
    }
    
    public async Task CryptoGiftTransferToRedPackage(Guid userId, string caAddress, ReferralInfo referralInfo, bool isNewUser, string ipAddress)
    {
        _logger.LogInformation("CryptoGiftTransferToRedPackage userId:{0},caAddress:{1},referralInfo:{2},isNewUser:{3}", userId, caAddress, JsonConvert.SerializeObject(referralInfo), isNewUser);
        if (referralInfo is not { ProjectCode: CommonConstant.CryptoGiftProjectCode } || referralInfo.ReferralCode.IsNullOrEmpty())
        {
            _logger.LogInformation("CryptoGiftTransferToRedPackage ProjectCode isn't 20000, referralInfo={0}", JsonConvert.SerializeObject(referralInfo));
            _logger.LogInformation($"Transfer cached failed userId:{userId}");
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
        var preGrabBucketItemDto = GetClaimedCryptoGift(userId, identityCode, redPackageId, cryptoGiftDto);
        await _distributedCache.SetAsync($"CryptoGiftUpdatedResult:{userId}:{redPackageId}", JsonConvert.SerializeObject(new CryptoGiftCacheDto()
        {
            Success = true,
            IsNewUser = isNewUser,
            CryptoGiftDto = cryptoGiftDto
        }), new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(600)
        });
        _logger.LogInformation($"Transfer cached succeed redPackageId:{redPackageId}");
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(redPackageId);
        var redPackageDetail = await grain.GetRedPackage(redPackageId);
        if (!redPackageDetail.Success || redPackageDetail.Data == null)
        {
            await UpdateCryptoGiftCacheResultFalse(userId, redPackageId);
            throw new UserFriendlyException("the red package does not exist");
        }
        var redPackageDetailDto = redPackageDetail.Data;
        
        //0 make sure the client is whether or not a new one, according to the new user rule
        if (!Enum.IsDefined(redPackageDetailDto.RedPackageDisplayType) || RedPackageDisplayType.Common.Equals(redPackageDetailDto.RedPackageDisplayType))
        {
            await UpdateCryptoGiftCacheResultFalse(userId, redPackageId);
            return;
        }
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
        
        //1 red package: amount/item
        var redPackageUpdateResult = await grain.CryptoGiftTransferToRedPackage(userId, caAddress, preGrabBucketItemDto, ipAddress, identityCode);
        _logger.LogInformation("CryptoGiftTransferToRedPackage redPackageUpdateResult:{0}", JsonConvert.SerializeObject(redPackageUpdateResult));
        if (redPackageUpdateResult.Success)
        {
            //2 crypto gift: amount/item
            _logger.LogInformation("CryptoGiftTransferToRedPackage GetClaimedCryptoGift:{0}", JsonConvert.SerializeObject(preGrabBucketItemDto));
            var updateCryptoGiftResult = await cryptoGiftGrain.UpdateCryptoGift(cryptoGiftDto);
            _logger.LogInformation("CryptoGiftTransferToRedPackage updateCryptoGiftResult:{0}", JsonConvert.SerializeObject(updateCryptoGiftResult));
        }
        else
        {
            await UpdateCryptoGiftCacheResultFalse(userId, redPackageId);
        }
    }

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

    public (string, string) GetIpAddressAndIdentity(Guid redPackageId)
    {
        var ipAddress = _ipInfoAppService.GetRemoteIp();
        var identityCode = GetIdentityCode(redPackageId, ipAddress);
        return new ValueTuple<string, string>(ipAddress, identityCode);
    }
}