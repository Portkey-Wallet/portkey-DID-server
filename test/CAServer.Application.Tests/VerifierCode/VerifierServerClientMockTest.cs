using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Settings;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;

namespace CAServer.VerifierCode;

public partial class VerifierServerClientTests
{
    private IGetVerifierServerProvider GetVerifierServerProvider()
    {
        var mockGetVerifierServerProvider = new Mock<IGetVerifierServerProvider>();
        mockGetVerifierServerProvider
            .Setup(o => o.GetVerifierServerEndPointsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string verifierId, string chainId) => chainId == DefaultChainId ? "localhost:5000" : null);
        return mockGetVerifierServerProvider.Object;
    }


    private IHttpClientFactory GetMockHttpClientFactory()
    {
        var clientHandlerStub = new DelegatingHandlerStub();
        var client = new HttpClient(clientHandlerStub);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var factory = mockFactory.Object;
        return factory;
    }

    private IOptionsSnapshot<AdaptableVariableOptions> GetAdaptableVariableOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<AdaptableVariableOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new AdaptableVariableOptions
            {
                HttpConnectTimeOut = 5,
                VerifierServerExpireTime = 1000
            });
        return mockOptionsSnapshot.Object;
    }
}

public class DelegatingHandlerStub : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public DelegatingHandlerStub()
    {
        _handlerFunc = (request, cancellationToken) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK, Content = new StringContent(JsonConvert.SerializeObject(
                new GoogleUserInfoDto
                {
                    Id = "123456789",
                    Email = "MockEmail",
                    Picture = "MockPicture",
                    VerifiedEmail = true,
                    FullName = "MockGivenName"
                }))
        });
    }

    public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}