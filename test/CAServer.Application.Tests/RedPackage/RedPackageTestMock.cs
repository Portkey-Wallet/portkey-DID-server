using System;
using CAServer.Commons;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace CAServer.RedPackage;

public partial class RedPackageTest 
{
    private IHttpContextAccessor GetMockHttpContextAccessor()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ImConstant.RelationAuthHeader] = "Bearer " + Guid.NewGuid().ToString("N");
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(context);
        return httpContextAccessor;
    }
}