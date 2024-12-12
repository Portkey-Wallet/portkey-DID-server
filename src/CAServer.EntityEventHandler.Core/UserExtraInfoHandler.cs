using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class UserExtraInfoHandler : IDistributedEventHandler<UserExtraInfoEto>, ITransientDependency
{
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CaAccountHandler> _logger;

    public UserExtraInfoHandler(
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository,
        IObjectMapper objectMapper,
        ILogger<CaAccountHandler> logger)
    {
        _userExtraInfoRepository = userExtraInfoRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(UserExtraInfoEto eventData)
    {
        try
        {
            var userInfo = _objectMapper.Map<UserExtraInfoEto, UserExtraInfoIndex>(eventData);
            await _userExtraInfoRepository.AddOrUpdateAsync(userInfo);
            _logger.LogDebug("User extra info add or update success: {data}", JsonConvert.SerializeObject(userInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}: {Data}", "User extra info add or update fail",
                JsonConvert.SerializeObject(eventData));
        }
    }
}