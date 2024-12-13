using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Tokens.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core.Service;

public interface IUserTokenService
{
    Task AddTokenAsync(UserTokenEto eventData);
    Task DeleteTokenAsync(UserTokenDeleteEto eventData);
}

public class UserTokenService : IUserTokenService, ISingletonDependency
{
    private readonly INESTRepository<UserTokenIndex, Guid> _userTokenIndexRepository;
    private readonly ILogger<UserTokenService> _logger;
    private readonly IObjectMapper _objectMapper;

    public UserTokenService(INESTRepository<UserTokenIndex, Guid> userTokenIndexRepository,
        ILogger<UserTokenService> logger, IObjectMapper objectMapper)
    {
        _userTokenIndexRepository = userTokenIndexRepository;
        _logger = logger;
        _objectMapper = objectMapper;
    }

    public async Task AddTokenAsync(UserTokenEto eventData)
    {
        _logger.LogInformation("user token is adding.{userId}-{chainId}-{symbol}", eventData.UserId,
            eventData.Token.ChainId, eventData.Token.Symbol);
        var index = _objectMapper.Map<UserTokenEto, UserTokenIndex>(eventData);
        try
        {
            await _userTokenIndexRepository.AddOrUpdateAsync(index);
            _logger.LogInformation("user token add success.{userId}-{chainId}-{symbol}", eventData.UserId,
                eventData.Token.ChainId, eventData.Token.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task DeleteTokenAsync(UserTokenDeleteEto eventData)
    {
        _logger.LogInformation("user token is deleting.{userId}-{chainId}-{symbol}", eventData.UserId,
            eventData.Token.ChainId, eventData.Token.Symbol);
        try
        {
            await _userTokenIndexRepository.DeleteAsync(eventData.Id);
            _logger.LogInformation("user token delete success.{userId}-{chainId}-{symbol}", eventData.UserId,
                eventData.Token.ChainId, eventData.Token.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}