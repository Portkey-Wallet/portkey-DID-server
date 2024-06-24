using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CAServer.Commons;
using CAServer.Http.Dtos;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CAServer;

public abstract partial class CAServerApplicationTestBase
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
    private readonly Mock<HttpMessageHandler> _mockHandler = new(MockBehavior.Strict);
    private readonly Mock<IConnectionMultiplexer> _mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();


    protected IHttpClientFactory MockHttpFactory()
    {
        _mockHttpClientFactory
            .Setup(_ => _.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("http://test.com/") });
        return _mockHttpClientFactory.Object;
    }

    protected IConnectionMultiplexer MockIConnectionMultiplexer()
    {
        _mockConnectionMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(new Mock<IDatabase>().Object);
        return _mockConnectionMultiplexer.Object;

    }


    private void MockHttpByPath(string respData, params string[] expressions)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => ExpressionHelper.Evaluate(expressions, ParseRequest(req))),
                ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage req, CancellationToken _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(respData, Encoding.UTF8, "application/json");
                var method = req.Method.ToString();
                var path = req.RequestUri.AbsolutePath;
                _output?.WriteLine($"Mock Http {method} to {path}, resp={respData}");
                return Task.FromResult(response);
            });
    }


    private Dictionary<string, object> ParseRequest(HttpRequestMessage request)
    {
        var dict = new Dictionary<string, object>
        {
            { "method", request.Method.ToString() },
            { "path", request.RequestUri.AbsolutePath }
        };
        var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
        var urlParamDict = query.AllKeys.ToDictionary(key => key, key => (object)query[key]);
        dict.Add("param", urlParamDict);

        if (request.Content == null) return dict;

        var requestBody = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        dict.Add("bodyString", requestBody);
        var bodyObj = new Dictionary<string, object>();
        try
        {
            bodyObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);
        }
        catch (JsonException)
        {
        }
        dict.Add("bodyObject", bodyObj);
        return dict;
    }

    
    protected void MockHttpByPath(HttpMethod method, string path, object response, params string[] expressions)
    {
        var expressionList = expressions.ToList();
        if (expressionList.Any()) expressionList.Insert(0, " && ");
        expressionList.Insert(0, $""" method=="{method}" && path.Contains("{path}") """);
        MockHttpByPath(JsonConvert.SerializeObject(response), expressionList.ToArray());
    }

    protected void MockHttpByPath(ApiInfo apiInfo, object response, params string[] expressions)
    {
        var expressionList = expressions.ToList();
        if (expressionList.Any()) expressionList.Insert(0, " && ");
        expressionList.Insert(0, $""" method=="{apiInfo.Method}" && path.Contains("{apiInfo.Path}") """);
        MockHttpByPath(response is string s ? s : JsonConvert.SerializeObject(response), expressionList.ToArray());
    }
    
}