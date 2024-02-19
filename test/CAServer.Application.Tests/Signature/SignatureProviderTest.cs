// using System.Net;
// using System.Net.Http;
// using System.Threading;
// using System.Threading.Tasks;
// using CAServer.VerifierCode;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Options;
// using Moq;
// using Moq.Protected;
// using Shouldly;
// using Xunit;
//
// namespace CAServer.Signature;
//
// [Collection(CAServerTestConsts.CollectionDefinitionName)]
// public class SignatureProviderTest : CAServerApplicationTestBase
// {
//     private readonly ISignatureProvider _signatureProvider;
//
//     public SignatureProviderTest()
//     {
//         _signatureProvider = GetRequiredService<ISignatureProvider>();
//     }
//
//     protected override void AfterAddApplication(IServiceCollection services)
//     {
//         base.AfterAddApplication(services);
//         services.AddSingleton(MockSignatureServerOptions());
//         services.AddSingleton(GetMockHttpClientFactory());
//     }
//
//     [Fact]
//     public async Task SignTxMsgTest()
//     {
//         var publicKey = "test";
//         var hexMsg = "test";
//
//         var sendDto = new SignResponseDto
//         {
//             Signature = string.Empty
//         };
//
//         var result = await _signatureProvider.SignTxMsg(publicKey, hexMsg);
//         result.ShouldNotBeNull();
//     }
//
//     private IOptionsSnapshot<SignatureServerOptions> MockSignatureServerOptions()
//     {
//         var mockOptionsSnapshot = new Mock<IOptionsSnapshot<SignatureServerOptions>>();
//         mockOptionsSnapshot.Setup(o => o.Value).Returns(
//             new SignatureServerOptions
//             {
//                 BaseUrl = "http://127.0.0.1:5577/test"
//             });
//         return mockOptionsSnapshot.Object;
//     }
//
//     private IHttpClientFactory GetMockHttpClientFactory()
//     {
//         var mockFactory = new Mock<IHttpClientFactory>();
//         var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
//         mockHttpMessageHandler.Protected()
//             .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
//                 ItExpr.IsAny<CancellationToken>())
//             .ReturnsAsync(new HttpResponseMessage
//             {
//                 StatusCode = HttpStatusCode.OK,
//                 Content = new StringContent("{'Signature':'thecodebuzz'}"),
//             });
//
//         var client = new HttpClient(mockHttpMessageHandler.Object);
//         mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
//         var factory = mockFactory.Object;
//         return factory;
//     }
// }