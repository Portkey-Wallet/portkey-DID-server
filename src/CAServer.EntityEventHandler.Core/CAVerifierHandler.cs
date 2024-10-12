using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using CAServer.Monitor.Interceptor;
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

    [ExceptionHandler(typeof(Exception),
        Message = "VerifierCodeEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(VerifierCodeEto eventData)
    {
        var dto = _objectMapper.Map<VerifierCodeEto, VerifierCodeRequestDto>(eventData);
        var result = await _verifierServerClient.SendVerificationRequestAsync(dto);
        if (!result.Success)
        {
            _logger.LogWarning("VerifierCodeEto Send VerifierCode failed {message}:", result.Message);
        }
    }
}