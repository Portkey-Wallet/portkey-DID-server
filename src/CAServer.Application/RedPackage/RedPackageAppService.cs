using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Options;
using CAServer.RedPackage.Dtos;
using CAServer.RedPackage.Etos;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver.Linq;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

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


    public RedPackageAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        INESTRepository<RedPackageIndex, Guid> redPackageIndexRepository,
        IHttpContextAccessor httpContextAccessor,
        IObjectMapper objectMapper,
        IOptionsSnapshot<RedPackageOptions> redPackageOptions,
        IOptionsSnapshot<ChainOptions> chainOptions)
    {
        _redPackageOptions = redPackageOptions.Value;
        _chainOptions = chainOptions.Value;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _redPackageIndexRepository = redPackageIndexRepository;
        _objectMapper = objectMapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<GenerateRedPackageOutputDto> GenerateRedPackageAsync(GenerateRedPackageInputDto redPackageInput)
    {
        var result = _redPackageOptions.TokenInfo.Where(x =>
                string.Equals(x.Symbol, redPackageInput.Symbol, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.ChainId, redPackageInput.ChainId, StringComparison.OrdinalIgnoreCase))
            .ToList().FirstOrDefault();
        if (result == null)
        {
            throw new UserFriendlyException("Symbol not found");
        }

        var redPackageId = Guid.NewGuid();

        var grain = _clusterClient.GetGrain<IRedPackageKeyGrain>(redPackageId);

        return new GenerateRedPackageOutputDto
        {
            Id = redPackageId,
            PublicKey = await grain.GenerateKey(),
            Signature = await grain.GenerateSignature($"{redPackageInput.Symbol}-{result.MinAmount}-{_redPackageOptions.MaxCount}"),
            MinAmount = result.MinAmount,
            Symbol = redPackageInput.Symbol,
            Decimal = result.Decimal,
            ChainId = redPackageInput.ChainId
        };
    }

    public async Task<SendRedPackageOutputDto> SendRedPackageAsync(SendRedPackageInputDto input)
    {
        var result = _redPackageOptions.TokenInfo.Where(x =>
            string.Equals(x.Symbol, input.Symbol, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.ChainId, input.ChainId, StringComparison.OrdinalIgnoreCase)).ToList().FirstOrDefault();
        if (result == null)
        {
            throw new UserFriendlyException("Symbol not found");
        }

        var checkResult = CheckSendRedPackageInput(input, decimal.Parse(result.MinAmount),_redPackageOptions.MaxCount);
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

        var grain = _clusterClient.GetGrain<IRedPackageGrain>(input.Id);
        var createResult = await grain.CreateRedPackage(input, result.Decimal, decimal.Parse(result.MinAmount), CurrentUser.Id.Value);
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
        redPackageIndex.SendUuid = input.SendUuid;
        redPackageIndex.Message = input.Message;
        await _redPackageIndexRepository.AddOrUpdateAsync(redPackageIndex);
        await _distributedEventBus.PublishAsync(new RedPackageCreateEto()
        {
            UserId = CurrentUser.Id,
            ChainId = input.ChainId,
            SessionId = sessionId,
            RawTransaction = input.RawTransaction
        });
        return new SendRedPackageOutputDto()
        {
            SessionId = sessionId
        };
    }

    public async Task<GetCreationResultOutputDto> GetCreationResultAsync(Guid sessionId)
    {
        var redPackageIndex =  await _redPackageIndexRepository.GetAsync(sessionId);
        if (redPackageIndex == null)
        {
            return new GetCreationResultOutputDto()
            {
                Status = RedPackageTransactionStatus.Fail,
                Message = "Session not found"
            };
        }
        
        return new GetCreationResultOutputDto()
        {
            Status = redPackageIndex.TransactionStatus,
            Message = redPackageIndex.ErrorMessage,
            TransactionId = redPackageIndex.TransactionId,
            TransactionResult = redPackageIndex.TransactionResult
        };
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
        
        var grain = _clusterClient.GetGrain<IRedPackageGrain>(id);
        var detail =  (await grain.GetRedPackage(skipCount, maxResultCount,CurrentUser.Id.Value)).Data;
        CheckLuckKing(detail);
        
        return detail; 
    }

    public async Task<RedPackageConfigOutput> GetRedPackageConfigAsync(string chainId ,string token)
    {
        if (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(chainId))
        {
            return new RedPackageConfigOutput()
            {
                TokenInfo = _redPackageOptions.TokenInfo
            };
        }
        
        var result = _redPackageOptions.TokenInfo.Where(x =>
                string.Equals(x.Symbol, chainId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.ChainId, token, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return new RedPackageConfigOutput()
        {
            TokenInfo = result
        };
    }

    public async Task<GrabRedPackageOutputDto> GrabRedPackageAsync(GrabRedPackageInputDto input)
    {
        if (CurrentUser.Id == null)
        {
            return new GrabRedPackageOutputDto()
            {
                Result = RedPackageGrabStatus.Fail,
                ErrorMessage = RedPackageConsts.UserNotExist
            };
        }
        
        var grain = _clusterClient.GetGrain<IRedPackageGrain>(input.Id);
        var result = await grain.GrabRedPackage(CurrentUser.Id.Value,input.UserCaAddress);
        return  new GrabRedPackageOutputDto()
        {
            Result = result.Data.Result,
            ErrorMessage = result.Data.ErrorMessage,
            Amount = result.Data.Amount,
            Decimal = result.Data.Decimal,
            Status = result.Data.Status
        };
    }
    
    private void CheckLuckKing(RedPackageDetailDto input)
    {
        if (input.Type != RedPackageType.Random || input.Grabbed != input.Count)
        {
            input.Items?.ForEach(item => item.IsLuckyKing = false);
        }
    }

    private (bool, string) CheckSendRedPackageInput(SendRedPackageInputDto input, decimal min, int maxCount)
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

        if (decimal.Parse(input.TotalAmount) < input.Count * min)
        {
            return (false, RedPackageConsts.RedPackageAmountError);
        }

        if (input.Count > maxCount)
        {
            return (false, RedPackageConsts.RedPackageCountBigError);
        }

        if (!_chainOptions.ChainInfos.TryGetValue(input.ChainId, out var chainInfo))
        {
            return (false, RedPackageConsts.RedPackageChainError);
        }

        return (true, "");
    }
}