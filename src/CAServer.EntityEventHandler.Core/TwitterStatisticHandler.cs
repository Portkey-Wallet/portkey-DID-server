using System.Threading.Tasks;
using CAServer.EntityEventHandler.Core.Service;
using CAServer.TwitterAuth.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class TwitterStatisticHandler : IDistributedEventHandler<TwitterStatisticEto>, ITransientDependency
{
    private readonly ITwitterStatisticService _twitterStatisticService;

    public TwitterStatisticHandler(ITwitterStatisticService twitterStatisticService)
    {
        _twitterStatisticService = twitterStatisticService;
    }

    public async Task HandleEventAsync(TwitterStatisticEto eventData)
    {
        _ = _twitterStatisticService.StatisticTwitterAsync(eventData);
    }
}