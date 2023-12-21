using System;
using AElf;
using AElf.Cryptography;
using CAServer.Commons;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Grains.Grain.ThirdPart;
using Microsoft.AspNetCore.Http;
using Mongo2Go;
using Moq;
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
    
    private IRedPackageKeyGrain getMockOrderGrain()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        string publicKey = keyPair.PublicKey.ToHex();
        string privateKey = keyPair.PrivateKey.ToHex();
        
        var mockockOrderGrain = new Mock<IRedPackageKeyGrain>();
        mockockOrderGrain.Setup(o => o.GenerateKey())
            .ReturnsAsync(publicKey);
        return mockockOrderGrain.Object;
    }
}