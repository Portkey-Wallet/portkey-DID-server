using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Contacts.Provider;
using CAServer.CryptoGift;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains.Grain.CryptoGift;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Options;
using CAServer.RedPackage.Dtos;
using CAServer.RedPackage.Etos;
using CAServer.Tokens;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;
using ChainOptions = CAServer.Options.ChainOptions;
using Volo.Abp.Users;

namespace CAServer.RedPackage;

[RemoteService(isEnabled: false), DisableAuditing]
public class RedPackageAppService : CAServerAppService, IRedPackageAppService
{
    private readonly RedPackageOptions _redPackageOptions;
    private readonly ChainOptions _chainOptions;
    private readonly IClusterClient _clusterClient;
    private readonly INESTRepository<RedPackageIndex, Guid> _redPackageIndexRepository;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IContactProvider _contactProvider;
    private readonly ILogger<RedPackageAppService> _logger;
    private readonly ITokenAppService _tokenAppService;
    private readonly IUserAssetsProvider _userAssetsProvider;
    private readonly IpfsOptions _ipfsOptions;
    private readonly NftToFtOptions _nftToFtOptions;
    private readonly ICryptoGiftAppService _cryptoGiftAppService;
    private readonly IdentityUserManager _userManager;

    public RedPackageAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        INESTRepository<RedPackageIndex, Guid> redPackageIndexRepository,
        IHttpContextAccessor httpContextAccessor,
        IObjectMapper objectMapper,
        IOptionsSnapshot<RedPackageOptions> redPackageOptions,
        IContactProvider contactProvider,
        IOptionsSnapshot<ChainOptions> chainOptions, ILogger<RedPackageAppService> logger,
        ITokenAppService tokenAppService, IUserAssetsProvider userAssetsProvider,
        IOptionsSnapshot<IpfsOptions> ipfsOptions, IOptionsSnapshot<NftToFtOptions> nftToFtOptions,
        ICryptoGiftAppService cryptoGiftAppService, IdentityUserManager userManager)
    {
        _redPackageOptions = redPackageOptions.Value;
        _chainOptions = chainOptions.Value;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _redPackageIndexRepository = redPackageIndexRepository;
        _objectMapper = objectMapper;
        _httpContextAccessor = httpContextAccessor;
        _contactProvider = contactProvider;
        _logger = logger;
        _tokenAppService = tokenAppService;
        _userAssetsProvider = userAssetsProvider;
        _nftToFtOptions = nftToFtOptions.Value;
        _ipfsOptions = ipfsOptions.Value;
        _cryptoGiftAppService = cryptoGiftAppService;
        _userManager = userManager;
    }

    public async Task<RedPackageTokenInfo> GetRedPackageOptionAsync(String symbol, string chainId)
    {
        var result = _redPackageOptions.TokenInfo.Where(x =>
                string.Equals(x.Symbol, symbol, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.ChainId, chainId, StringComparison.OrdinalIgnoreCase))
            .ToList().FirstOrDefault();
        if (result == null)
        {
            var tokenInfo = await _tokenAppService.GetTokenInfoAsync(chainId, symbol);
            if (tokenInfo != null && chainId == tokenInfo.ChainId && symbol == tokenInfo.Symbol)
            {
                return CreateRedPackageTokenInfo(tokenInfo.Decimals);
            }

            var getNftItemInfosDto = CreateGetNftItemInfosDto(symbol, chainId);
            var nftItemInfos = await _userAssetsProvider.GetNftItemInfosAsync(getNftItemInfosDto, 0, 1000);
            _logger.LogInformation("GetNftItemInfosAsync nftItemInfos: " + JsonConvert.SerializeObject(nftItemInfos));
            if (nftItemInfos?.NftItemInfos?.Count > 0)
            {
                var firstNftItemInfo = nftItemInfos.NftItemInfos.FirstOrDefault();
                if (firstNftItemInfo != null)
                {
                    return CreateRedPackageTokenInfo(firstNftItemInfo.Decimals);
                }
            }

            throw new UserFriendlyException("Symbol not found");
        }

        return result;
    }

    private RedPackageTokenInfo CreateRedPackageTokenInfo(int decimals)
    {
        return new RedPackageTokenInfo
        {
            Decimal = decimals,
            MinAmount = "1"
        };
    }

    public async Task<GenerateRedPackageOutputDto> GenerateRedPackageAsync(GenerateRedPackageInputDto redPackageInput)
    {
        var result = await GetRedPackageOptionAsync(redPackageInput.Symbol, redPackageInput.ChainId);

        if (!_chainOptions.ChainInfos.TryGetValue(redPackageInput.ChainId, out var chainInfo))
        {
            throw new UserFriendlyException("chain not found");
        }

        var redPackageId = Guid.NewGuid();

        var grain = _clusterClient.GetGrain<IRedPackageKeyGrain>(redPackageId);
        var (publicKey, signature) = await grain.GenerateKeyAndSignature(
            $"{redPackageId}-{redPackageInput.Symbol}-{result.MinAmount}-{_redPackageOptions.MaxCount}");
        return new GenerateRedPackageOutputDto
        {
            Id = redPackageId,
            PublicKey = publicKey,
            MinAmount = result.MinAmount,
            Symbol = redPackageInput.Symbol,
            Decimal = result.Decimal,
            ChainId = redPackageInput.ChainId,
            ExpireTime = _redPackageOptions.ExpireTimeMs,
            RedPackageContractAddress = chainInfo.RedPackageContractAddress
        };
    }

    public async Task<SendRedPackageOutputDto> SendRedPackageAsync(SendRedPackageInputDto input)
    {
        Stopwatch watcher = Stopwatch.StartNew();
        var startTime = DateTime.Now.Ticks;
        try
        {
            _logger.LogInformation("SendRedPackageAsync start input param is {input}",
                JsonConvert.SerializeObject(input));

            var validationResult = ValidateAndAdaptAssetType(input);
            if (!validationResult.Item1)
            {
                throw new UserFriendlyException(validationResult.Item2);
            }

            var result = await GetRedPackageOptionAsync(input.Symbol, input.ChainId);
            _logger.LogInformation("GetRedPackageOptionAsync result: " + JsonConvert.SerializeObject(result));

            var checkResult =
                await CheckSendRedPackageInputAsync(input, long.Parse(result.MinAmount), _redPackageOptions.MaxCount);
            if (!checkResult.Item1)
            {
                throw new UserFriendlyException(checkResult.Item2);
            }

            if (CurrentUser.Id == null)
            {
                throw new UserFriendlyException("auth fail");
            }
            
            StringValues? relationToken = null;
            if (input.RedPackageDisplayType == null || RedPackageDisplayType.Common.Equals(input.RedPackageDisplayType))
            {
                relationToken = _httpContextAccessor.HttpContext?.Request?.Headers[ImConstant.RelationAuthHeader];
                if (string.IsNullOrEmpty(relationToken))
                {
                    throw new UserFriendlyException("Relation token not found");
                }
            }

            var portkeyToken = _httpContextAccessor.HttpContext?.Request?.Headers[CommonConstant.AuthHeader];
            if (string.IsNullOrEmpty(portkeyToken))
            {
                throw new UserFriendlyException("PortkeyToken token not found");
            }

            var sessionId = Guid.NewGuid(); //Guid of ES 
            input.SessionId = sessionId;
            var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(input.Id);
            var createResult = await grain.CreateRedPackage(input, result.Decimal, long.Parse(result.MinAmount),
                CurrentUser.Id.Value, _redPackageOptions.ExpireTimeMs);
            _logger.LogInformation("SendRedPackageAsync CreateRedPackage input param is {input}", input);
            _logger.LogInformation("CreateRedPackage createResult:" + JsonConvert.SerializeObject(createResult));
            if (!createResult.Success)
            {
                throw new UserFriendlyException(createResult.Message);
            }

            if (RedPackageDisplayType.CryptoGift.Equals(input.RedPackageDisplayType))
            {
                var cryptoGiftGrain = _clusterClient.GetGrain<ICryptoGiftGrain>(input.Id);
                var cryptoGiftCreateResult = await cryptoGiftGrain.CreateCryptoGift(input, createResult.Data.BucketNotClaimed,
                    createResult.Data.BucketClaimed, CurrentUser.Id.Value);
                if (!cryptoGiftCreateResult.Success)
                {
                    throw new UserFriendlyException(cryptoGiftCreateResult.Message);
                }
            }

            var redPackageIndex = _objectMapper.Map<RedPackageDetailDto, RedPackageIndex>(createResult.Data);
            redPackageIndex.Id = sessionId;
            redPackageIndex.RedPackageId = createResult.Data.Id;
            redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Processing;
            redPackageIndex.SenderRelationToken = relationToken;
            redPackageIndex.SenderPortkeyToken = portkeyToken;
            redPackageIndex.Message = input.Message;
            redPackageIndex.AssetType = input.AssetType;
            await _redPackageIndexRepository.AddOrUpdateAsync(redPackageIndex);
            _ = _distributedEventBus.PublishAsync(new RedPackageCreateEto()
            {
                UserId = CurrentUser.Id,
                ChainId = input.ChainId,
                SessionId = sessionId,
                RawTransaction = input.RawTransaction
            });
            _logger.LogInformation("SendRedPackageAsync PublishAsync redPackageIndex is {redPackageIndex}",
                redPackageIndex);
            return new SendRedPackageOutputDto()
            {
                SessionId = sessionId
            };
        }
        finally
        {
            watcher.Stop();
            _logger.LogInformation("send end:{0},{1}:", input.Id.ToString(),
                (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString());
            _logger.LogInformation("#monitor# send:{redpackageId},{cost},{startTime}:", input.Id.ToString(),
                watcher.Elapsed.Milliseconds.ToString(), (startTime / TimeSpan.TicksPerMillisecond).ToString());
        }
    }

    public async Task<GetCreationResultOutputDto> GetCreationResultAsync(Guid sessionId)
    {
        GetCreationResultOutputDto res = null;
        RedPackageIndex redPackageIndex = null;
        Stopwatch watcher = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("GetCreationResultAsync sessionId is {sessionId}", sessionId.ToByteArray());

            redPackageIndex = await _redPackageIndexRepository.GetAsync(sessionId);
            if (redPackageIndex == null)
            {
                return new GetCreationResultOutputDto()
                {
                    Status = RedPackageTransactionStatus.Fail,
                    Message = "Session not found"
                };
            }

            _logger.LogInformation("GetCreationResultAsync redPackageIndex is {redPackageIndex}",
                JsonConvert.SerializeObject(redPackageIndex));

            res = new GetCreationResultOutputDto()
            {
                Status = redPackageIndex.TransactionStatus,
                Message = redPackageIndex.ErrorMessage,
                TransactionId = redPackageIndex.TransactionId,
                TransactionResult = redPackageIndex.TransactionResult
            };
            return res;
        }
        finally
        {
            watcher.Stop();
            if (redPackageIndex != null)
            {
                if (res != null && res.Status == RedPackageTransactionStatus.Success)
                {
                    _logger.LogInformation("getCreationResult success:{0},{1}:",
                        redPackageIndex.RedPackageId.ToString(),
                        (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString());
                    _logger.LogInformation(
                        "#monitor# getCreationResult success:{redpackageId},{status},{cost},{endTime}:",
                        redPackageIndex.RedPackageId.ToString(), res.Status.ToString(),
                        watcher.Elapsed.Milliseconds.ToString(),
                        (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString());
                }
                else
                {
                    _logger.LogInformation(
                        "#monitor# getCreationResult other:{redpackageId},{status},{cost},{endTime}:",
                        redPackageIndex.RedPackageId.ToString(), res.Status.ToString(),
                        watcher.Elapsed.Milliseconds.ToString(),
                        (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString());
                }
            }
        }
    }

    public async Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, RedPackageDisplayType displayType, int skipCount, int maxResultCount)
    {
        if (CurrentUser.Id == null || id == Guid.Empty)
        {
            return new RedPackageDetailDto();
        }

        if (skipCount < 0)
        {
            skipCount = 0;
        }

        //we allow maxResultCount = 0，this means just fetch metadata
        if (maxResultCount < 0 || maxResultCount > RedPackageConsts.MaxRedPackageGrabberCount)
        {
            maxResultCount = RedPackageConsts.DefaultRedPackageGrabberCount;
        }

        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(id);
        var detail =  (await grain.GetRedPackage(skipCount, maxResultCount, CurrentUser.Id.Value, displayType)).Data;
        _logger.LogInformation("GetRedPackage detail: " + JsonConvert.SerializeObject(detail));
        try
        {
            var allResult = (await grain.GetRedPackage(detail.Id)).Data;
            allResult.Items?.ForEach(item =>
            {
                if (item.UserId == CurrentUser.GetId())
                {
                    detail.CurrentUserGrabbedAmount = item.Amount;
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "getredpackage failed, id={id}", id);
        }

        if (detail.AssetType == (int)AssetType.NFT)
        {
            var getNftItemInfosDto = CreateGetNftItemInfosDto(detail.Symbol, detail.ChainId);
            var indexerNftItemInfos = await _userAssetsProvider.GetNftItemInfosAsync(getNftItemInfosDto, 0, 1000);
            List<NftItemInfo> nftItemInfos = indexerNftItemInfos.NftItemInfos;

            if (nftItemInfos != null && nftItemInfos.Count > 0)
            {
                detail.Alias = nftItemInfos[0].TokenName;
                detail.TokenId = detail.Symbol.Split('-')[1];
                detail.ImageUrl = nftItemInfos[0].ImageUrl;
            }
        }

        CheckLuckKing(detail);
        CheckIsMe(detail); //check if the sender is the grabber
        
        await BuildAvatarAndNameAsync(detail);

        if (detail.Status == RedPackageStatus.Expired)
        {
            detail.IsRedPackageExpired = true;
            detail.Status = RedPackageStatus.Expired;
        }

        if (detail.Status == RedPackageStatus.FullyClaimed || detail.Grabbed == detail.Count)
        {
            detail.IsRedPackageFullyClaimed = true;
        }

        detail.DisplayStatus = RedPackageDisplayStatus.GetDisplayStatus(detail.Status);

        SetSeedStatusAndTypeForDetail(detail);

        TryUpdateImageUrlForDetail(detail);

        await CryptoGiftHandler(id, displayType, detail, skipCount, maxResultCount);
        return detail;
    }

    private async Task CryptoGiftHandler(Guid id, RedPackageDisplayType displayType, RedPackageDetailDto detail,
        int skipCount, int maxResultCount)
    {
        if (!RedPackageDisplayType.CryptoGift.Equals(displayType))
        {
            return;
        }
        var preGrabbedDto = await _cryptoGiftAppService.ListCryptoPreGiftGrabbedItems(id);
        foreach (var grabItemDto in detail.Items)
        {
            grabItemDto.DisplayType = CryptoGiftDisplayType.Common;
        }
        detail.Items.AddRange(_objectMapper.Map<List<PreGrabbedItemDto>, List<GrabItemDto>>(preGrabbedDto.Items));
        detail.Items = detail.Items.Skip(skipCount).Take(maxResultCount).ToList();
    }

    private void SetSeedStatusAndTypeForDetail(RedPackageDetailDto detail)
    {
        detail.IsSeed = detail.AssetType == (int)AssetType.NFT &&
                        detail.Symbol.StartsWith(TokensConstants.SeedNamePrefix);

        if (detail.IsSeed)
        {
            detail.SeedType = (int)SeedType.FT;

            if (!string.IsNullOrEmpty(detail.Alias) && detail.Alias.StartsWith(TokensConstants.SeedNamePrefix))
            {
                detail.SeedType = detail.Alias.Remove(0, 5).Contains("-") ? (int)SeedType.NFT : (int)SeedType.FT;
            }
        }
    }

    private void TryUpdateImageUrlForDetail(RedPackageDetailDto detail)
    {
        detail.ImageUrl = IpfsImageUrlHelper.TryGetIpfsImageUrl(detail.ImageUrl, _ipfsOptions?.ReplacedIpfsPrefix);
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

    public async Task<RedPackageConfigOutput> GetRedPackageConfigAsync(string chainId, string token)
    {
        var contractAddressList = new List<ContractAddressInfo>();
        foreach (var item in _chainOptions.ChainInfos)
        {
            contractAddressList.Add(new ContractAddressInfo()
            {
                ChainId = item.Key,
                ContractAddress = item.Value.RedPackageContractAddress
            });
        }

        if (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(chainId))
        {
            return new RedPackageConfigOutput()
            {
                TokenInfo = _redPackageOptions.TokenInfo,
                RedPackageContractAddress = contractAddressList
            };
        }

        var result = _redPackageOptions.TokenInfo.AsQueryable();


        if (!string.IsNullOrEmpty(token))
        {
            result = result.Where(x => string.Equals(x.Symbol, token, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(chainId))
        {
            result = result.Where(x => string.Equals(x.ChainId, chainId, StringComparison.OrdinalIgnoreCase));
        }

        var resultList = result.ToList();


        return new RedPackageConfigOutput()
        {
            TokenInfo = resultList,
            RedPackageContractAddress = contractAddressList
        };
    }

    public async Task<GrabRedPackageOutputDto> LoggedGrabRedPackageAsync(GrabRedPackageInputDto input)
    {
        if (input.CaHash.IsNullOrEmpty())
        {
            throw new UserFriendlyException("caHash is required~");
        }
        var user = await _userManager.FindByNameAsync(input.CaHash);
        if (user == null)
        {
            throw new UserFriendlyException("user doesn't exist~");
        }
        var userId = user.Id;
        if (Guid.Empty.Equals(userId))
        {
            return new GrabRedPackageOutputDto()
            {
                Result = RedPackageGrabStatus.Fail,
                ErrorMessage = RedPackageConsts.UserNotExist
            };
        }
        
        var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(input.Id);
        var redPackageResultDto= await grain.GetRedPackage(input.Id);
        if (!redPackageResultDto.Success || redPackageResultDto.Data == null)
        {
            return new GrabRedPackageOutputDto()
                        {
                            Result = RedPackageGrabStatus.Fail,
                            ErrorMessage = RedPackageConsts.UserNotExist
                        };
        }
        await _cryptoGiftAppService.CheckClaimQuotaAfterLoginCondition(redPackageResultDto.Data, input.CaHash);
        var (ipAddress, identity) = _cryptoGiftAppService.GetIpAddressAndIdentity(input.Id, input.Random);
        var result = await grain.GrabRedPackageWithIdentityInfo(userId, input.UserCaAddress, ipAddress, identity);
        if (result.Success)
        {
            await _distributedEventBus.PublishAsync(new PayRedPackageEto()
            {
                RedPackageId = input.Id,
                DisplayType = RedPackageDisplayType.CryptoGift,
                ReceiverId = userId
            });
            _logger.LogInformation("sent PayRedPackageEto RedPackageId:{0} receiverId:{1}", input.Id, userId);
        }

        var res = new GrabRedPackageOutputDto()
        {
            Result = result.Data.Result,
            ErrorMessage = result.Data.ErrorMessage,
            ErrorCode = result.Data.ErrorMessage.Equals(RedPackageConsts.RedPackageUserGrabbed) ? 10001 : 0,
            Amount = result.Data.Amount,
            Decimal = result.Data.Decimal,
            Status = result.Data.Status
        };
        //add the crypto gift logic
        if (result.Success)
        {
            try
            {
                await PreGrabCryptoGiftAfterLogging(input.Id, userId, RedPackageDisplayType.CryptoGift,
                    result.Data.BucketItem.Index, result.Data.Decimal, ipAddress, identity);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "pre grab crypto gift after logging error");
                return new GrabRedPackageOutputDto()
                {
                    Result = RedPackageGrabStatus.Fail,
                    ErrorMessage = "pre grab crypto gift after logging error"
                };
            }
        }
        if (!result.Success && !string.IsNullOrWhiteSpace(result.Data.Amount))
        {
            res.Result = RedPackageGrabStatus.Success;
            res.ErrorMessage = "";
        }

        return res;
    }

    public async Task<GrabRedPackageOutputDto> GrabRedPackageAsync(GrabRedPackageInputDto input)
    {
        Stopwatch watcher = Stopwatch.StartNew();
        var startTime = DateTime.Now.Ticks;

        try
        {
            if (CurrentUser.Id == null)
            {
                return new GrabRedPackageOutputDto()
                {
                    Result = RedPackageGrabStatus.Fail,
                    ErrorMessage = RedPackageConsts.UserNotExist
                };
            }
            
            var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(input.Id);
            var result = await grain.GrabRedPackage(CurrentUser.Id.Value, input.UserCaAddress);
            if (result.Success)
            {
                await _distributedEventBus.PublishAsync(new PayRedPackageEto()
                {
                    RedPackageId = input.Id,
                    DisplayType = RedPackageDisplayType.Common,
                    ReceiverId = CurrentUser.Id.Value
                });
                _logger.LogInformation("sent PayRedPackageEto RedPackageId:{0} receiverId:{1}", input.Id, CurrentUser.Id.Value);
            }

            var res = new GrabRedPackageOutputDto()
            {
                Result = result.Data.Result,
                ErrorMessage = result.Data.ErrorMessage,
                Amount = result.Data.Amount,
                Decimal = result.Data.Decimal,
                Status = result.Data.Status
            };
            if (!result.Success && !string.IsNullOrWhiteSpace(result.Data.Amount))
            {
                res.Result = RedPackageGrabStatus.Success;
                res.ErrorMessage = "";
            }

            return res;
        }
        finally
        {
            watcher.Stop();
            _logger.LogInformation("#monitor# grabRedPackage:{redpackageId},{cost},{endTime}:", input.Id.ToString(),
                watcher.Elapsed.Milliseconds.ToString(), (startTime / TimeSpan.TicksPerMillisecond).ToString());
        }
    }

    private async Task PreGrabCryptoGiftAfterLogging(Guid redPackageId, Guid userId, RedPackageDisplayType displayType,
        int index, int amountDecimal, string ipAddress, string identityCode)
    {
        if (!Enum.IsDefined(displayType) || !RedPackageDisplayType.CryptoGift.Equals(displayType))
        {
            return;
        }

        await _cryptoGiftAppService.PreGrabCryptoGiftAfterLogging(redPackageId, userId, index, amountDecimal, ipAddress, identityCode);
    }
    
    private void CheckLuckKing(RedPackageDetailDto input)
    {
        if (input.Type != RedPackageType.Random || input.Grabbed != input.Count)
        {
            input.Items?.ForEach(item => item.IsLuckyKing = false);
            input.LuckKingId = Guid.Empty;
        }
    }

    private void CheckIsMe(RedPackageDetailDto input)
    {
        if (input == null)
        {
            return;
        }
        foreach (var grabItemDto in input.Items)
        {
            var isMe = input.SenderId.Equals(grabItemDto.UserId);
            grabItemDto.IsMe = isMe;
            if (isMe)
            {
                break;
            }
        }
    }

    private async Task BuildAvatarAndNameAsync(RedPackageDetailDto input)
    {
        var userIds = new List<Guid>();
        userIds.Add(input.SenderId);
        userIds.AddRange(input.Items.Select(x => x.UserId));
        var users = await _contactProvider.GetCaHoldersAsync(userIds);
        input.SenderAvatar = users.FirstOrDefault(x => x.UserId == input.SenderId)?.Avatar;
        input.SenderName = users.FirstOrDefault(x => x.UserId == input.SenderId)?.NickName;
        var sendContract = await _contactProvider.GetContactAsync(CurrentUser.GetId(), input.SenderId);
        if (sendContract != null && !string.IsNullOrWhiteSpace(sendContract.Name))
        {
            input.SenderName = sendContract.Name;
        }

        input.Items?.ForEach(item =>
        {
            item.Avatar = users.FirstOrDefault(x => x.UserId == item.UserId)?.Avatar;
            item.Username = users.FirstOrDefault(x => x.UserId == item.UserId)?.NickName;
        });

        //fill remark
        var tasks = input.Items?.Select(async grabItemDto =>
        {
            var contact = await _contactProvider.GetContactAsync(CurrentUser.GetId(), grabItemDto.UserId);
            if (contact != null && !string.IsNullOrWhiteSpace(contact.Name))
            {
                grabItemDto.Username = contact.Name;
            }
        });

        if (tasks != null)
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task<(bool, string)> CheckSendRedPackageInputAsync(SendRedPackageInputDto input, long min,
        int maxCount)
    {
        var isNotInEnum = !Enum.IsDefined(typeof(RedPackageType), input.Type);

        if (isNotInEnum)
        {
            return (false, RedPackageConsts.RedPackageTypeError);
        }

        if (input.Id == Guid.Empty)
        {
            return (false, RedPackageConsts.RedPackageIdInvalid);
        }

        if (input.Count <= 0)
        {
            return (false, RedPackageConsts.RedPackageCountSmallError);
        }

        if (long.Parse(input.TotalAmount) < input.Count * min)
        {
            return (false, RedPackageConsts.RedPackageAmountError);
        }

        if (input.Count > maxCount)
        {
            return (false, RedPackageConsts.RedPackageCountBigError);
        }

        var grain = _clusterClient.GetGrain<IRedPackageKeyGrain>(input.Id);
        if (string.IsNullOrEmpty(await grain.GetPublicKey()))
        {
            return (false, RedPackageConsts.RedPackageKeyError);
        }

        return (true, "");
    }

    private (bool, string) ValidateAndAdaptAssetType(SendRedPackageInputDto input)
    {
        if (!Enum.IsDefined(typeof(AssetType), input.AssetType))
        {
            input.AssetType = (int)AssetType.FT;
        }

        if (input.AssetType == (int)AssetType.FT && _nftToFtOptions.NftToFtInfos.Keys.Contains(input.Symbol))
        {
            input.AssetType = (int)AssetType.NFT;
        }

        return ValidateAssetDetails(input);
    }

    private (bool, string) ValidateAssetDetails(SendRedPackageInputDto input)
    {
        var containsDash = input.Symbol.Contains('-');

        if (input.AssetType == (int)AssetType.NFT && !containsDash)
        {
            return (false, "Symbol must contain '-' for NFT assets");
        }

        if (input.AssetType == (int)AssetType.FT && containsDash)
        {
            return (false, "Symbol must not contain '-' for FT assets");
        }

        return (true, "");
    }
}