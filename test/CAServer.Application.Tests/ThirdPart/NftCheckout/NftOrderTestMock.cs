using System;
using System.Net.Http;
using CAServer.Commons;
using Google.Protobuf.WellKnownTypes;
using Moq;
using Xunit.Abstractions;

namespace CAServer.ThirdPart.NftCheckout;

public partial class NftOrderTest
{

    public static readonly Action<Mock<HttpMessageHandler>, ITestOutputHelper> MockSuccessWebhook =
        PathMatcher(HttpMethod.Post, "/myWebhook", new CommonResponseDto<Empty>());
}