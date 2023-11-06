using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace CAServer;

public abstract partial class CAServerApplicationTestBase
{
    
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
    private readonly Mock<HttpMessageHandler> _mockHandler = new(MockBehavior.Strict);


    protected IHttpClientFactory MockHttpFactory()
    {
        _mockHttpClientFactory
            .Setup(_ => _.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("http://test.com/") });
        return _mockHttpClientFactory.Object;
    }


    private void MockHttpByPath(HttpMethod method, string path,
        string respData)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method && req.RequestUri.ToString().Contains(path)),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(respData, Encoding.UTF8, "application/json");
                _output?.WriteLine($"Mock Http {method} to {path}, resp={respData}");
                return Task.FromResult(response);
            });
    }

    protected void MockHttpByPath(HttpMethod method, string path, object response)
    {
        MockHttpByPath(method, path, JsonConvert.SerializeObject(response));
    }
}