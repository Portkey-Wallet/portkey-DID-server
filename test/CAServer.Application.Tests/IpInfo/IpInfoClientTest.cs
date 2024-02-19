// using System;
// using System.Net;
// using System.Net.Http;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Extensions.DependencyInjection;
// using Moq;
// using Moq.Protected;
// using Newtonsoft.Json;
// using Shouldly;
// using Volo.Abp;
// using Xunit;
//
// namespace CAServer.IpInfo;
//
// [Collection(CAServerTestConsts.CollectionDefinitionName)]
// public class IpInfoClientTest : CAServerApplicationTestBase
// {
//     private readonly IIpInfoClient _infoClient;
//
//     public IpInfoClientTest()
//     {
//         _infoClient = GetService<IIpInfoClient>();
//     }
//
//     protected override void AfterAddApplication(IServiceCollection services)
//     {
//         base.AfterAddApplication(services);
//         services.AddSingleton(MockIpInfoHttpClient());
//     }
//
//
//     [Fact]
//     public async Task GetIpInfoTest_error()
//     {
//         var result = () => _infoClient.GetIpInfoAsync("error");
//         var exception = await Assert.ThrowsAsync<UserFriendlyException>(result);
//         exception.ShouldNotBeNull();
//         exception.Message.ShouldContain("mock error");
//
//         result = () => _infoClient.GetIpInfoAsync("NotFound");
//         exception = await Assert.ThrowsAsync<UserFriendlyException>(result);
//         exception.ShouldNotBeNull();
//         exception.Message.ShouldContain("Request error");
//     }
//
//
//     [Fact]
//     public async Task GetIpInfoTest()
//     {
//         try
//         {
//             var ip = "20.230.34.112";
//             var result = await _infoClient.GetIpInfoAsync(ip);
//             result.CountryCode.ShouldBe("US");
//         }
//         catch (Exception e)
//         {
//             e.ShouldNotBeNull();
//         }
//     }
//
//
//     public static IHttpClientFactory MockIpInfoHttpClient()
//     {
//         IpInfoDto nowhere = new IpInfoDto
//         {
//             CountryName = "MockCountry",
//             CountryCode = "NOT_FOUND",
//             Location = new LocationInfo
//             {
//                 CallingCode = "404"
//             }
//         };
//
//         IpInfoDto us = new IpInfoDto
//         {
//             CountryName = "United States",
//             CountryCode = "US",
//             Location = new LocationInfo
//             {
//                 CallingCode = "1"
//             }
//         };
//
//         var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
//         handlerMock
//             .Protected()
//             .Setup<Task<HttpResponseMessage>>(
//                 "SendAsync",
//                 ItExpr.Is<HttpRequestMessage>(request => request.RequestUri.ToString().Contains("0.0.0.0")),
//                 ItExpr.IsAny<CancellationToken>())
//             .ReturnsAsync(new HttpResponseMessage
//             {
//                 StatusCode = HttpStatusCode.OK,
//                 Content = new StringContent(JsonConvert.SerializeObject(nowhere)),
//             })
//             .Verifiable();
//         handlerMock
//             .Protected()
//             .Setup<Task<HttpResponseMessage>>(
//                 "SendAsync",
//                 ItExpr.Is<HttpRequestMessage>(request => request.RequestUri.ToString().Contains("20.230.34.112")),
//                 ItExpr.IsAny<CancellationToken>())
//             .ReturnsAsync(new HttpResponseMessage
//             {
//                 StatusCode = HttpStatusCode.OK,
//                 Content = new StringContent(JsonConvert.SerializeObject(us)),
//             })
//             .Verifiable();
//         handlerMock
//             .Protected()
//             .Setup<Task<HttpResponseMessage>>(
//                 "SendAsync",
//                 ItExpr.Is<HttpRequestMessage>(request => request.RequestUri.ToString().Contains("error")),
//                 ItExpr.IsAny<CancellationToken>())
//             .ReturnsAsync(new HttpResponseMessage
//             {
//                 StatusCode = HttpStatusCode.OK,
//                 Content = new StringContent("{\"error\":{\"info\":\"mock error\"}}"),
//             })
//             .Verifiable();
//         handlerMock
//             .Protected()
//             .Setup<Task<HttpResponseMessage>>(
//                 "SendAsync",
//                 ItExpr.Is<HttpRequestMessage>(request => request.RequestUri.ToString().Contains("NotFound")),
//                 ItExpr.IsAny<CancellationToken>())
//             .ReturnsAsync(new HttpResponseMessage
//             {
//                 StatusCode = HttpStatusCode.NotFound,
//                 Content = new StringContent("{\"error\":{\"info\":\"mock error\"}}"),
//             })
//             .Verifiable();
//         var httpClient = new HttpClient(handlerMock.Object);
//         var httpClientFactoryMock = new Mock<IHttpClientFactory>();
//         httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
//         return httpClientFactoryMock.Object;
//     }
// }