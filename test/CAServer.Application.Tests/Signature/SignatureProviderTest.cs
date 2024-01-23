using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Signature.Options;
using CAServer.Signature.Provider;
using CAServer.VerifierCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Shouldly;
using Xunit;

namespace CAServer.Signature;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class SignatureProviderTest : CAServerApplicationTestBase
{
    private readonly ISignatureProvider _signatureProvider;

    public SignatureProviderTest()
    {
        _signatureProvider = GetRequiredService<ISignatureProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockSignatureServerOptions());
        // services.AddSingleton(GetMockHttpClientFactory());
        services.AddSingleton(MockHttpFactory());
        services.AddSingleton(MockSecretProvider());
    }

    [Fact]
    public async Task SignTxMsgTest()
    {
        var sendDto = new SignResponseDto
        {
            Signature = "MockSignature"
        };
        MockHttpByPath(HttpMethod.Post, "/api/app/signature", new CommonResponseDto<SignResponseDto>(sendDto));
        
        var publicKey = "test";
        var hexMsg = "test";


        var result = await _signatureProvider.SignTxMsg(publicKey, hexMsg);
        result.ShouldNotBeNull();
    }

    private IOptionsMonitor<SignatureServerOptions> MockSignatureServerOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsMonitor<SignatureServerOptions>>();
        mockOptionsSnapshot.Setup(o => o.CurrentValue).Returns(
            new SignatureServerOptions
            {
                BaseUrl = "http://127.0.0.1:5577",
                AppId = "caserver",
                AppSecret = "12345678"
            });
        return mockOptionsSnapshot.Object;
    }

    private IHttpClientFactory GetMockHttpClientFactory()
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{'Signature':'thecodebuzz'}"),
            });

        var client = new HttpClient(mockHttpMessageHandler.Object);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
        var factory = mockFactory.Object;
        return factory;
    }
}