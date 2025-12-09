using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Security.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class UserSecurityHandler : IDistributedEventHandler<UserTransferLimitHistoryEto>, ITransientDependency
{
    private readonly INESTRepository<UserTransferLimitHistoryIndex, Guid> _userTransferLimitHistoryRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<UserSecurityHandler> _logger;

    public UserSecurityHandler(INESTRepository<UserTransferLimitHistoryIndex, Guid> userTransferLimitHistoryRepository,
        IObjectMapper objectMapper, ILogger<UserSecurityHandler> logger)
    {
        _userTransferLimitHistoryRepository = userTransferLimitHistoryRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(UserTransferLimitHistoryEto eventData)
    {
        try
        {
            var userTransferLimitHistoryIndex =
                _objectMapper.Map<UserTransferLimitHistoryEto, UserTransferLimitHistoryIndex>(eventData);

            await _userTransferLimitHistoryRepository.AddOrUpdateAsync(userTransferLimitHistoryIndex);

            _logger.LogInformation("Order {eventDataId} add or update success orderId.",eventData.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the event,orderId: {eventDataId}",eventData.Id);
        }
    }
}