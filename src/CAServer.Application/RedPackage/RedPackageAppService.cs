using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.Commons;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage.Dtos;
using CAServer.RedPackage.Etos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using ChainOptions = CAServer.Options.ChainOptions;
using Volo.Abp.Users;
using Volo.Abp.Users;

namespace CAServer.RedPackage;

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


    public RedPackageAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        INESTRepository<RedPackageIndex, Guid> redPackageIndexRepository,
        IHttpContextAccessor httpContextAccessor,
        IObjectMapper objectMapper,
        IOptionsSnapshot<RedPackageOptions> redPackageOptions,
        IContactProvider contactProvider,
        IOptionsSnapshot<ChainOptions> chainOptions, ILogger<RedPackageAppService> logger)
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
    }

    public  RedPackageTokenInfo GetRedPackageOption(String symbol,string chainId,out long maxCount,out string redpackageContractAddress)
    {
        var result =  _redPackageOptions.TokenInfo.Where(x =>
                string.Equals(x.Symbol, symbol, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.ChainId, chainId, StringComparison.OrdinalIgnoreCase))
            .ToList().FirstOrDefault();
        maxCount = _redPackageOptions.MaxCount;
        if (result == null)
        {
            throw new UserFriendlyException("Symbol not found");
        }
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            throw new UserFriendlyException("chain not found");
        }

        redpackageContractAddress = chainInfo.RedPackageContractAddress;
        
        return result;
    }

    public async Task<GenerateRedPackageOutputDto> GenerateRedPackageAsync(GenerateRedPackageInputDto redPackageInput)
    {
        var result = GetRedPackageOption(redPackageInput.Symbol, redPackageInput.ChainId, out long maxCount,
                out string redpackageContractAddress);
        if (!_chainOptions.ChainInfos.TryGetValue(redPackageInput.ChainId, out var chainInfo))
        {
            throw new UserFriendlyException("chain not found");
        }

        var redPackageId = Guid.NewGuid();

        var grain = _clusterClient.GetGrain<IRedPackageKeyGrain>(redPackageId);
        var (publicKey, signature) = await grain.GenerateKeyAndSignature(
            $"{redPackageId}-{redPackageInput.Symbol}-{result.MinAmount}-{maxCount}");
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
            _logger.LogInformation("SendRedPackageAsync start input param is {input}", JsonConvert.SerializeObject(input));
            var result = _redPackageOptions.TokenInfo.Where(x =>
                string.Equals(x.Symbol, input.Symbol, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.ChainId, input.ChainId, StringComparison.OrdinalIgnoreCase)).ToList().FirstOrDefault();
            if (result == null)
            {
                throw new UserFriendlyException("Symbol not found");
            }

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

            var relationToken = _httpContextAccessor.HttpContext?.Request?.Headers[ImConstant.RelationAuthHeader];
            if (string.IsNullOrEmpty(relationToken))
            {
                throw new UserFriendlyException("Relation token not found");
            }

            var portkeyToken = _httpContextAccessor.HttpContext?.Request?.Headers[CommonConstant.AuthHeader];
            if (string.IsNullOrEmpty(portkeyToken))
            {
                throw new UserFriendlyException("PortkeyToken token not found");
            }

            var grain = _clusterClient.GetGrain<ICryptoBoxGrain>(input.Id);
            var createResult = await grain.CreateRedPackage(input, result.Decimal, long.Parse(result.MinAmount),
                CurrentUser.Id.Value,_redPackageOptions.ExpireTimeMs);
            _logger.LogInformation("SendRedPackageAsync CreateRedPackage input param is {input}", input);
            if (!createResult.Success)
            {
                throw new UserFriendlyException(createResult.Message);
            }

            var sessionId = Guid.NewGuid();

            var redPackageIndex = _objectMapper.Map<RedPackageDetailDto, RedPackageIndex>(createResult.Data);
            redPackageIndex.Id = sessionId;
            redPackageIndex.RedPackageId = createResult.Data.Id;
            redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Processing;
            redPackageIndex.SenderRelationToken = relationToken;
            redPackageIndex.SenderPortkeyToken = portkeyToken;
            redPackageIndex.Message = input.Message;
            await _redPackageIndexRepository.AddOrUpdateAsync(redPackageIndex);
            _logger.LogInformation("SendRedPackageAsync AddOrUpdateAsync redPackageIndex is {redPackageIndex}",
                redPackageIndex);
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
            _logger.LogInformation("send end:{0},{1}:", input.Id.ToString(),(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString());
            _logger.LogInformation("#monitor# send:{redpackageId},{cost},{startTime}:", input.Id.ToString(), watcher.Elapsed.Milliseconds.ToString(), (startTime / TimeSpan.TicksPerMillisecond).ToString());
        }
    }

    public async Task<GetCreationResultOutputDto> GetCreationResultAsync(Guid sessionId)
    {
        GetCreationResultOutputDto res = null;
        RedPackageIndex redPackageIndex = null;
        Stopwatch watcher = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("GetCreationResultAsync sessionId is {sessionId}",sessionId.ToByteArray());
            
            redPackageIndex = await _redPackageIndexRepository.GetAsync(sessionId);
            if (redPackageIndex == null)
            {
                return new GetCreationResultOutputDto()
                {
                    Status = RedPackageTransactionStatus.Fail,
                    Message = "Session not found"
                };
            }
            _logger.LogInformation("GetCreationResultAsync redPackageIndex is {redPackageIndex}",JsonConvert.SerializeObject(redPackageIndex));

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
                    _logger.LogInformation("getCreationResult success:{0},{1}:",redPackageIndex.RedPackageId.ToString(), (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString());
                    _logger.LogInformation("#monitor# getCreationResult success:{redpackageId},{status},{cost},{endTime}:", redPackageIndex.RedPackageId.ToString(), res.Status.ToString(), watcher.Elapsed.Milliseconds.ToString(), (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString());
                }
                else
                {
                    _logger.LogInformation("#monitor# getCreationResult other:{redpackageId},{status},{cost},{endTime}:", redPackageIndex.RedPackageId.ToString(), res.Status.ToString(), watcher.Elapsed.Milliseconds.ToString(), (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString());
                }             
            }
        }
    }

    public async Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, int skipCount, int maxResultCount)
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
        var detail =  (await grain.GetRedPackage(skipCount, maxResultCount,CurrentUser.Id.Value)).Data;
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
            Console.WriteLine(e);
        }
       
        CheckLuckKing(detail);
        
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
        return detail; 
    }

    public async Task<RedPackageConfigOutput> GetRedPackageConfigAsync(string chainId ,string token)
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
                    RedPackageId = input.Id

                });
            }
            return new GrabRedPackageOutputDto()
            {
                Result = result.Data.Result,
                ErrorMessage = result.Data.ErrorMessage,
                Amount = result.Data.Amount,
                Decimal = result.Data.Decimal,
                Status = (result.Data.Status == RedPackageStatus.Expired
                          || DateTimeOffset.Now.ToUnixTimeMilliseconds() > result.Data.ExpireTime)
                    ? RedPackageStatus.Expired
                    : result.Data.Status
            };
        }
        finally
        {
            watcher.Stop();
            _logger.LogInformation("#monitor# grabRedPackage:{redpackageId},{cost},{endTime}:", input.Id.ToString(), watcher.Elapsed.Milliseconds.ToString(), (startTime / TimeSpan.TicksPerMillisecond).ToString());
        }
    }
    
    private void CheckLuckKing(RedPackageDetailDto input)
    {
        if (input.Type != RedPackageType.Random || input.Grabbed != input.Count)
        {
            input.Items?.ForEach(item => item.IsLuckyKing = false);
            input.LuckKingId = Guid.Empty;
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

    private async Task<(bool, string)> CheckSendRedPackageInputAsync(SendRedPackageInputDto input, long min, int maxCount)
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

        var tokenInfo = _redPackageOptions.TokenInfo.Where(x => x.ChainId == input.ChainId && x.Symbol == input.Symbol).ToList()
            .FirstOrDefault();
        if (tokenInfo == null)
        {
            return (false, RedPackageConsts.RedPackageChainError);
        }
        
        var grain = _clusterClient.GetGrain<IRedPackageKeyGrain>(input.Id);
        if (string.IsNullOrEmpty(await grain.GetPublicKey()))
        { 
            return (false, RedPackageConsts.RedPackageKeyError);
        }
        
        return (true, "");
    }
}