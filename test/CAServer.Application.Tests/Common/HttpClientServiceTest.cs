using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace CAServer.Common;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class HttpClientServiceTest : CAServerApplicationTestBase
{
    private readonly IHttpClientService _httpClientService;
    private const string Url = "http://137.0.0.1:5577";

    public HttpClientServiceTest()
    {
        _httpClientService = GetRequiredService<IHttpClientService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockHttpClientFactory());
    }

    [Fact]
    public async Task Get_With_Header_Test()
    {
        var response =
            await _httpClientService.GetAsync<Person>(Url, new Dictionary<string, string>() { ["test"] = "test" });

        response.Name.ShouldBe("test");
    }

    [Fact]
    public async Task Post_Test()
    {
        var response =
            await _httpClientService.PostAsync<Person>(Url);

        response.Name.ShouldBe("test");
    }

    [Fact]
    public async Task Post_With_Header_Test()
    {
        var response =
            await _httpClientService.PostAsync<Person>(Url, new Dictionary<string, string>() { ["test"] = "test" });

        response.Name.ShouldBe("test");
    }

    [Fact]
    public async Task Post_With_Params_Test()
    {
        var response =
            await _httpClientService.PostAsync<Person>(Url, new Person());

        response.Name.ShouldBe("test");
    }

    [Fact]
    public async Task Post_With_Params_And_Header_Test()
    {
        var response =
            await _httpClientService.PostAsync<Person>(Url, new Person(),
                new Dictionary<string, string>() { ["test"] = "test" });

        response.Name.ShouldBe("test");
    }

    [Fact]
    public async Task Post_With_Params_And_JsonMediaType_And_Header_Test()
    {
        var response =
            await _httpClientService.PostAsync<Person>(Url, RequestMediaType.Json, new Person(),
                new Dictionary<string, string>() { ["test"] = "test" });

        response.Name.ShouldBe("test");
    }


    [Fact]
    public async Task Post_With_Params_And_FormMediaType_And_Header_Test()
    {
        var response =
            await _httpClientService.PostAsync<Person>(Url, RequestMediaType.Form,
                new Dictionary<string, string>() { ["name"] = "John" },
                new Dictionary<string, string>() { ["test"] = "test" });

        response.Name.ShouldBe("test");
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
                Content = new StringContent(JsonConvert.SerializeObject(new Person { Name = "test", Age = 20 })),
            });

        var client = new HttpClient(mockHttpMessageHandler.Object);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
        var factory = mockFactory.Object;
        return factory;
    }

    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}