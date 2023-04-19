using Volo.Abp.ObjectMapping;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using CAServer.Verifier.Etos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Tests.CAVerifier;
public class MockCaVerifierHandler: IDistributedEventHandler<VerifierCodeEto>, ITransientDependency
{
    private readonly IVerifierServerClient _verifierServerClient;

    private readonly IObjectMapper _objectMapper;

    public MockCaVerifierHandler(IVerifierServerClient verifierServerClient, IObjectMapper objectMapper)
    {
        _verifierServerClient = verifierServerClient;
        _objectMapper = objectMapper;
    }
    
    public async Task HandleEventAsync(VerifierCodeEto eventData)
    {
        //var dto = _objectMapper.Map<VerifierCodeEto, VerifierCodeRequestDto>(eventData);
        //await _verifierServerClient.SendVerificationRequestAsync(dto);
        
    }
}