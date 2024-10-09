using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.ZeroHoldings;
using CAServer.UserAssets.Provider;
using CAServer.ZeroHoldings.constant;
using CAServer.ZeroHoldings.Dtos;
using CAServer.ZeroHoldings.Etos;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.ZeroHoldings;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class ZeroHoldingsConfigAppService : CAServerAppService, IZeroHoldingsConfigAppService
{
    private readonly ILogger<ZeroHoldingsConfigAppService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<ZeroHoldingsConfigIndex, Guid> _userTokenIndexRepository;

    public ZeroHoldingsConfigAppService(
        ILogger<ZeroHoldingsConfigAppService> logger, IUserAssetsProvider userAssetsProvider,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        INESTRepository<ZeroHoldingsConfigIndex, Guid> userTokenIndexRepository)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _userTokenIndexRepository = userTokenIndexRepository;
    }


    public async Task<bool> SetStatus(ZeroHoldingsConfigDto configDto)
    {
        configDto.UserId = CurrentUser.GetId();
        try
        {
            var grain = _clusterClient.GetGrain<IZeroHoldingsConfigGrain>(configDto.UserId);
            var grainDto = ObjectMapper.Map<ZeroHoldingsConfigDto, ZeroHoldingsGrainDto>(configDto);
            await grain.AddOrUpdateAsync(grainDto);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"ZeroHoldingsConfigService SetStatus grain failed, dto: {JsonConvert.SerializeObject(configDto)}");
        }

        try
        {
            var toPublish = ObjectMapper.Map<ZeroHoldingsConfigDto, ZeroHoldingsConfigEto>(configDto);
            _logger.LogInformation(
                $"ZeroHoldingsConfigService publish SetStatus event：{toPublish.UserId}-{toPublish.Status}");
            await _distributedEventBus.PublishAsync(toPublish);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"ZeroHoldingsConfigService SetStatus event failed, dto: {JsonConvert.SerializeObject(configDto)}");
        }

        return true;
    }

    public async Task<ZeroHoldingsConfigDto> GetStatus()
    {
        Guid userId = CurrentUser.GetId();
        try
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<ZeroHoldingsConfigIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));
            QueryContainer QueryFilter(QueryContainerDescriptor<ZeroHoldingsConfigIndex> f) => f.Bool(b => b.Must(mustQuery));

            ZeroHoldingsConfigIndex config = await _userTokenIndexRepository.GetAsync(QueryFilter);
            if (null == config)
            {
                return new ZeroHoldingsConfigDto
                {
                    Status = ZeroHoldingsConfigConstant.DefaultStatus
                };
            }

            return new ZeroHoldingsConfigDto
            {
                Status = config.Status,
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"ZeroHoldingsConfigService GetStatus failed, userId: {userId}");
            return new ZeroHoldingsConfigDto
            {
                Status = ZeroHoldingsConfigConstant.DefaultStatus
            };
        }
    }
}