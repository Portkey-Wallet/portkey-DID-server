using CAServer.Dtos;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Moq;

namespace CAServer.EntityEventHandler.Tests.CAVerifier;

public partial class CaVerifierHandlerTests
{
    private IVerifierServerClient GetClient()
    {
        var verifierServerClient = new Mock<IVerifierServerClient>();

        verifierServerClient.Setup(m => m.SendVerificationRequestAsync(It.IsAny<VerifierCodeRequestDto>()))
            .ReturnsAsync(new ResponseResultDto<VerifierServerResponse>());
        
        return verifierServerClient.Object;
    }
}