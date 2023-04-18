using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Tokens.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class UserTokenEntityHandler : EntityHandlerBase,
    IDistributedEventHandler<UserTokenEto>
{
    private readonly INESTRepository<UserTokenIndex,Guid> _userTokenIndexRepository;
    private readonly ILogger<UserTokenEntityHandler> _logger;

    public UserTokenEntityHandler(INESTRepository<UserTokenIndex, Guid> userTokenIndexRepository, ILogger<UserTokenEntityHandler> logger)
    {
        _userTokenIndexRepository = userTokenIndexRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(UserTokenEto eventData)
    {
        _logger.LogInformation($"user token is adding.{eventData.UserId}-{eventData.Token.ChainId}-{eventData.Token.Symbol}");
        var index = ObjectMapper.Map<UserTokenEto, UserTokenIndex>(eventData);
        try
        {
            await _userTokenIndexRepository.AddOrUpdateAsync(index);
            _logger.LogInformation($"user token add succuess.{eventData.UserId}-{eventData.Token.ChainId}-{eventData.Token.Symbol}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
       
    }   
}