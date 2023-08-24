using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart.Provider;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace CAServer.ThirdPart.Transak;

public sealed partial class TransakTest
{

    private static void MockEnvHelper(string environments)
    {
        var type = typeof(EnvHelper);
        var field = type.GetField("_hostingEnvironment", BindingFlags.Static | BindingFlags.NonPublic);
        field?.SetValue(null, environments);
    }
    
    public static readonly Action<Mock<HttpMessageHandler>, ITestOutputHelper> MockRefreshAccessToken =
        (mockHandler, testOutputHelper) =>
        {
            var expectedUri = TransakApi.RefreshAccessToken;
            DateTimeOffset offset = DateTime.UtcNow.AddDays(7);
            var responseData = new Dictionary<string, object>
            {
                ["data"] = new Dictionary<string, object>
                {
                    ["accessToken"] =
                        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJBUElfS0VZIjoiMDljMDU2ZmQtZDQyMy00NmQ5LWE2NDEtZTRhN2ExZTdkZTMzIiwiaWF0IjoxNjkwOTU1OTQxLCJleHAiOjE2OTE1NjA3NDF9.j3mn6ctBKPnkkiYRchg-BzGgdI9ZfUgH3bbC0QIGtkM",
                    ["expiresAt"] = offset.ToUnixTimeSeconds()
                }
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(responseData), Encoding.UTF8,
                    "application/json")
            };

            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == expectedUri.Method &&
                        req.RequestUri.ToString().Contains(expectedUri.Path)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() =>
                {
                    testOutputHelper?.WriteLine($"Mock Http {expectedUri.Method.Method} to {expectedUri.Path}, resp={response}");
                    return Task.FromResult(response);
                });            
        };

    public static readonly Action<Mock<HttpMessageHandler>, ITestOutputHelper> MockUpdateWebhookUrl =
        (mockHandler, testOutputHelper) =>
        {
            var expectedUri = TransakApi.UpdateWebhook;
            var responseData = new Dictionary<string, object>
            {
                ["meta"] = new Dictionary<string, object>
                {
                    ["success"] = true
                },
                ["data"] = new Dictionary<string, object>
                {
                    ["message"] = "Your request to update webhook url is successfully processed."
                }
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(responseData), Encoding.UTF8,
                    "application/json")
            };
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == expectedUri.Method &&
                        req.RequestUri.ToString().Contains(expectedUri.Path)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() =>
                {
                    testOutputHelper?.WriteLine($"Mock Http {expectedUri.Method.Method} to {expectedUri.Path}, resp={response}");
                    return Task.FromResult(response);
                });
        };
}