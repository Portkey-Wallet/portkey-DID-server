using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Monitor.Interceptor;
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

    [ExceptionHandler(typeof(Exception),
        Message = "UserSecurityHandler UserTransferLimitHistoryEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(UserTransferLimitHistoryEto eventData)
    {
        var userTransferLimitHistoryIndex =
            _objectMapper.Map<UserTransferLimitHistoryEto, UserTransferLimitHistoryIndex>(eventData);

        await _userTransferLimitHistoryRepository.AddOrUpdateAsync(userTransferLimitHistoryIndex);

        _logger.LogInformation("UserSecurityHandler UserTransferLimitHistoryEto Order {eventDataId} add or update success orderId.",eventData.Id);
    }
}