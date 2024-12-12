using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.TwitterAuth.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class TwitterStatisticHandler : IDistributedEventHandler<TwitterStatisticEto>, ITransientDependency
{
    private readonly INESTRepository<TwitterStatisticIndex, string> _twitterStatisticRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TwitterStatisticHandler> _logger;

    public TwitterStatisticHandler(IObjectMapper objectMapper,
        INESTRepository<TwitterStatisticIndex, string> twitterStatisticRepository,
        ILogger<TwitterStatisticHandler> logger)
    {
        _objectMapper = objectMapper;
        _twitterStatisticRepository = twitterStatisticRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(TwitterStatisticEto eventData)
    {
        try
        {
            var item = await _twitterStatisticRepository.GetAsync(eventData.Id);
            if (item != null)
            {
                var date = TimeHelper.GetDateTimeFromSecondTimeStamp(item.UpdateTime);
                if (date.Date < DateTime.UtcNow.Date)
                {
                    item.CallCount = 1;
                }
                else
                {
                    item.CallCount += 1;
                }

                item.UpdateTime = eventData.UpdateTime;
                await _twitterStatisticRepository.UpdateAsync(item);

                _logger.LogInformation("statistic twitter call api count, userId:{userId}, count:{count}", eventData.Id,
                    item.CallCount);

                if (item.CallCount > CommonConstant.TwitterLimitCount)
                {
                    _logger.LogWarning("user call twitter api limit, userId:{userId}, count:{count}", eventData.Id,
                        item.CallCount);
                }

                return;
            }

            await _twitterStatisticRepository.AddOrUpdateAsync(
                _objectMapper.Map<TwitterStatisticEto, TwitterStatisticIndex>(eventData));
            _logger.LogInformation("statistic twitter call api count, userId:{userId}", eventData.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "statistic twitter call api count error, userId:{userId}", eventData.Id);
        }
    }
}