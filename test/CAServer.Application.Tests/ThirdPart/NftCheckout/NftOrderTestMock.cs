using System;
using System.Collections.Generic;
using System.Net.Http;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.Options;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace CAServer.ThirdPart.NftCheckout;

public partial class NftOrderTest
{

    public static readonly Action<Mock<HttpMessageHandler>, ITestOutputHelper> MockSuccessWebhook =
        PathMatcher(HttpMethod.Post, "/myWebhook", new CommonResponseDto<Empty>());
}