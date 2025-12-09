using System;
using System.Threading.Tasks;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EventHandler;

public class CAVerifierHandler : IDistributedEventHandler<VerifierCodeEto>, ITransientDependency
{
    private readonly ILogger<CAVerifierHandler> _logger;
    private readonly IVerifierServerClient _verifierServerClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    private const int MaxRetryTimes = 5;

    public CAVerifierHandler(ILogger<CAVerifierHandler> logger, IVerifierServerClient verifierServerClient,
        IObjectMapper objectMapper, IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _verifierServerClient = verifierServerClient;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
    }

    public async Task HandleEventAsync(VerifierCodeEto eventData)
    {
        try
        {
            var dto = _objectMapper.Map<VerifierCodeEto, VerifierCodeRequestDto>(eventData);
            var result = await _verifierServerClient.SendVerificationRequestAsync(dto);
            if (!result.Success)
            {
                _logger.LogWarning("Send VerifierCode failed {message}:", result.Message);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
        }
    }
}