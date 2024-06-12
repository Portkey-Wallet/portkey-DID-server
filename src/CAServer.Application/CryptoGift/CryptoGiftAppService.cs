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
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;

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
    private readonly ILogger<CryptoGiftAppService> _logger;

    public CryptoGiftAppService(INESTRepository<RedPackageIndex, Guid> redPackageIndexRepository,
        IClusterClient clusterClient,
        IObjectMapper objectMapper,
        ICryptoGiftProvider cryptoGiftProvider,
        IIpInfoAppService ipInfoAppService,
        IAbpDistributedLock distributedLock,
        IdentityUserManager userManager,
        ILogger<CryptoGiftAppService> logger)
    {
        _redPackageIndexRepository = redPackageIndexRepository;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _cryptoGiftProvider = cryptoGiftProvider;
        _ipInfoAppService = ipInfoAppService;
        _distributedLock = distributedLock;
        _userManager = userManager;
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
        var grabbedItemDtos = _objectMapper.Map<List<PreGrabItem>, List<PreGrabbedItemDto>>(cryptoGiftDto.Items);
        var expireMilliseconds = _cryptoGiftProvider.GetExpirationSeconds() * 1000;
        foreach (var preGrabbedItemDto in grabbedItemDtos)
        {
            preGrabbedItemDto.ExpirationTime = preGrabbedItemDto.GrabTime + expireMilliseconds;
        }
        return new PreGrabbedDto()
        {
            Items = grabbedItemDtos
        };
    }

    public async Task<string> PreGrabCryptoGift(Guid redPackageId)
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
        var identityCode = HashHelper.ComputeFrom(ipAddress + "#" + redPackageId).ToString();
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
        return identityCode;
    }
    
    private PreGrabBucketItemDto GetBucket(CryptoGiftDto cryptoGiftDto, string identityCode)
    {
        var random = new Random();
        var index = random.Next(cryptoGiftDto.BucketNotClaimed.Count);
        var bucket = cryptoGiftDto.BucketNotClaimed[index];
        bucket.IdentityCode = identityCode;
        bucket.Index = index;
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
        var identityCode = HashHelper.ComputeFrom(ipAddress + "#" + redPackageId).ToString();
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
        var identityCode = HashHelper.ComputeFrom(ipAddress + "#" + redPackageId).ToString();
        await CheckAndUpdateCryptoGiftExpirationStatus(cryptoGiftGrain, cryptoGiftDto, identityCode);
        
        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(redPackageDetailDto.SenderId);
        var caHolderGrainDto = await caHolderGrain.GetCaHolder();
        if (!caHolderGrainDto.Success || caHolderGrainDto.Data == null)
        {
            throw new UserFriendlyException("the crypto gift sender does not exist");
        }
        var claimedPreGrabItem = cryptoGiftDto.Items.FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode)
                                                     && GrabbedStatus.Claimed.Equals(crypto.GrabbedStatus));
        if (claimedPreGrabItem != null)
        {
            return new CryptoGiftPhaseDto()
            {
                CryptoGiftPhase = CryptoGiftPhase.Claimed,
                Prompt = $"{caHolderGrainDto.Data.Nickname} sent you a Crypto Gift",
                SubPrompt = "Congratulations! You have claimed successfully",
                Memo = redPackageDetailDto.Memo,
                RemainingWaitingSeconds = 0,
                RemainingExpirationSeconds = claimedPreGrabItem.GrabTime / 1000 + _cryptoGiftProvider.GetExpirationSeconds() - DateTimeOffset.Now.ToUnixTimeSeconds(),
                Sender = new UserInfoDto()
                {
                    Avatar = caHolderGrainDto.Data.Avatar,
                    Nickname = caHolderGrainDto.Data.Nickname
                }
            };
        }
        if (RedPackageStatus.Expired.Equals(redPackageDetailDto.Status))
        {
            return new CryptoGiftPhaseDto()
            {
                CryptoGiftPhase = CryptoGiftPhase.Expired,
                Prompt = $"{caHolderGrainDto.Data.Nickname} sent you a Crypto Gift",
                SubPrompt = "Oops! The crypto gift has been expired",
                Memo = redPackageDetailDto.Memo,
                RemainingWaitingSeconds = 0,
                Sender = new UserInfoDto()
                {
                    Avatar = caHolderGrainDto.Data.Avatar,
                    Nickname = caHolderGrainDto.Data.Nickname
                }
            };
        }

        if (RedPackageStatus.FullyClaimed.Equals(redPackageDetailDto.Status)
            || cryptoGiftDto.PreGrabbedAmount >= cryptoGiftDto.TotalAmount)
        {
            return new CryptoGiftPhaseDto()
            {
                CryptoGiftPhase = CryptoGiftPhase.FullyClaimed,
                Prompt = $"{caHolderGrainDto.Data.Nickname} sent you a Crypto Gift",
                SubPrompt = "Oops! None left...",
                Memo = redPackageDetailDto.Memo,
                RemainingWaitingSeconds = 0,
                Sender = new UserInfoDto()
                {
                    Avatar = caHolderGrainDto.Data.Avatar,
                    Nickname = caHolderGrainDto.Data.Nickname
                }
            };
        }

        if ((RedPackageStatus.NotClaimed.Equals(redPackageDetailDto.Status)
             || RedPackageStatus.Claimed.Equals(redPackageDetailDto.Status))
            && cryptoGiftDto.PreGrabbedAmount < cryptoGiftDto.TotalAmount)
        {
            var preGrabItem = cryptoGiftDto.Items
                .FirstOrDefault(crypto => crypto.IdentityCode.Equals(identityCode)
                                          && GrabbedStatus.Created.Equals(crypto.GrabbedStatus));
            if (preGrabItem == null)
            {
                return new CryptoGiftPhaseDto()
                {
                    CryptoGiftPhase = CryptoGiftPhase.Available,
                    Prompt = $"{caHolderGrainDto.Data.Nickname} sent you a Crypto Gift",
                    SubPrompt = "Claim and Join Portkey",
                    Memo = redPackageDetailDto.Memo,
                    RemainingWaitingSeconds = 0,
                    Sender = new UserInfoDto()
                    {
                        Avatar = caHolderGrainDto.Data.Avatar,
                        Nickname = caHolderGrainDto.Data.Nickname
                    }
                };
            }
            return new CryptoGiftPhaseDto()
            {
                CryptoGiftPhase = CryptoGiftPhase.GrabbedQuota,
                Prompt = $"{caHolderGrainDto.Data.Nickname} sent you a Crypto Gift",
                SubPrompt = "Claim and Join Portkey",
                Memo = redPackageDetailDto.Memo,
                Amount = preGrabItem.Amount,
                RemainingWaitingSeconds = DateTimeOffset.Now.ToUnixTimeSeconds() - preGrabItem.GrabTime / 1000,
                Sender = new UserInfoDto()
                {
                    Avatar = caHolderGrainDto.Data.Avatar,
                    Nickname = caHolderGrainDto.Data.Nickname
                }
            };
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
            return new CryptoGiftPhaseDto()
            {
                CryptoGiftPhase = CryptoGiftPhase.NoQuota,
                Prompt = $"{caHolderGrainDto.Data.Nickname} sent you a Crypto Gift",
                SubPrompt = "Don't worry,it hasn't been claimed yet! You can keep trying to claim after",
                Memo = redPackageDetailDto.Memo,
                RemainingWaitingSeconds = DateTimeOffset.Now.ToUnixTimeSeconds() - preGrabItem.GrabTime / 1000,
                Sender = new UserInfoDto()
                {
                    Avatar = caHolderGrainDto.Data.Avatar,
                    Nickname = caHolderGrainDto.Data.Nickname
                }
            };
        }
        
        throw new UserFriendlyException("there is no crypto gift condition like this");
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
        var identityCode = HashHelper.ComputeFrom(ipAddress + "#" + redPackageId).ToString();
        await CheckAndUpdateCryptoGiftExpirationStatus(cryptoGiftGrain, cryptoGiftDto, identityCode);
        
        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(redPackageDetailDto.SenderId);
        var caHolderResult = await caHolderGrain.GetCaHolder();
        if (!caHolderResult.Success || caHolderResult.Data == null)
        {
            throw new UserFriendlyException("the crypto gift sender does not exist");
        }
        var caHolderGrainDto = caHolderResult.Data;
        var grabItemDto = redPackageDetailDto.Items.FirstOrDefault(red => red.UserId.Equals(receiverId));
        if (grabItemDto != null)
        {
            return new CryptoGiftPhaseDto()
            {
                CryptoGiftPhase = CryptoGiftPhase.Claimed,
                Prompt = $"{caHolderGrainDto.Nickname} sent you a Crypto Gift",
                SubPrompt = $"You have already claimed {grabItemDto.Amount} {redPackageDetailDto.Symbol} of this Crypto Gift and can't reclaim it.",
                Memo = redPackageDetailDto.Memo,
                Sender = new UserInfoDto()
                {
                    Avatar = caHolderGrainDto.Avatar,
                    Nickname = caHolderGrainDto.Nickname
                }
            };
        }
        
        if (RedPackageStatus.Expired.Equals(redPackageDetailDto.Status))
        {
            return new CryptoGiftPhaseDto()
            {
                CryptoGiftPhase = CryptoGiftPhase.Expired,
                Prompt = $"{caHolderGrainDto.Nickname} sent you a Crypto Gift",
                SubPrompt = "Oops! The crypto gift has been expired",
                Memo = redPackageDetailDto.Memo,
                Sender = new UserInfoDto()
                {
                    Avatar = caHolderGrainDto.Avatar,
                    Nickname = caHolderGrainDto.Nickname
                }
            };
        }

        if (RedPackageStatus.FullyClaimed.Equals(redPackageDetailDto.Status))
        {
            return new CryptoGiftPhaseDto()
            {
                CryptoGiftPhase = CryptoGiftPhase.FullyClaimed,
                Prompt = $"{caHolderGrainDto.Nickname} sent you a Crypto Gift",
                SubPrompt = "Oops! None left...",
                Memo = redPackageDetailDto.Memo,
                Sender = new UserInfoDto()
                {
                    Avatar = caHolderGrainDto.Avatar,
                    Nickname = caHolderGrainDto.Nickname
                }
            };
        }

        if ((RedPackageStatus.NotClaimed.Equals(redPackageDetailDto.Status)
             || RedPackageStatus.Claimed.Equals(redPackageDetailDto.Status))
            && cryptoGiftDto.PreGrabbedAmount < cryptoGiftDto.TotalAmount)
        {
            var preGrabItem = cryptoGiftDto.Items
                .Where(crypto => crypto.IdentityCode.Equals(identityCode) 
                                 && GrabbedStatus.Expired.Equals(crypto.GrabbedStatus))
                .MaxBy(crypto => crypto.GrabTime);
            if (preGrabItem == null)
            {
                return new CryptoGiftPhaseDto()
                {
                    CryptoGiftPhase = CryptoGiftPhase.Available,
                    Prompt = $"{caHolderGrainDto.Nickname} sent you a Crypto Gift",
                    SubPrompt = "Claim and Join Portkey",
                    Memo = redPackageDetailDto.Memo,
                    Sender = new UserInfoDto()
                    {
                        Avatar = caHolderGrainDto.Avatar,
                        Nickname = caHolderGrainDto.Nickname
                    }
                };
            }
            return new CryptoGiftPhaseDto()
            {
                CryptoGiftPhase = CryptoGiftPhase.ExpiredReleased,
                Prompt = $"{caHolderGrainDto.Nickname} sent you a Crypto Gift",
                SubPrompt = "Sorry, you miss the claim expiration time.", 
                Memo = redPackageDetailDto.Memo,
                Sender = new UserInfoDto()
                {
                    Avatar = caHolderGrainDto.Avatar,
                    Nickname = caHolderGrainDto.Nickname
                }
            };
        }
        
        throw new UserFriendlyException("there is no red package condition like this");
    }
    
    public async Task CryptoGiftTransferToRedPackage(string caHash, string caAddress, ReferralInfo referralInfo, bool isNewUser)
    {
        if (referralInfo is not { ProjectCode: CommonConstant.CryptoGiftProjectCode } || referralInfo.ReferralCode.IsNullOrEmpty())
        {
            _logger.LogInformation("CryptoGiftTransferToRedPackage ProjectCode isn't 20000, referralInfo={0}", JsonConvert.SerializeObject(referralInfo));
            return;
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
        cryptoGiftDto.BucketNotClaimed.Insert(preGrabItem.Index, preGrabBucketItemDto);
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