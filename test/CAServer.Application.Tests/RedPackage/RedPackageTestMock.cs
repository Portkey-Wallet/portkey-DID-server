using System;
using System.Collections.Generic;
using AElf;
using AElf.Cryptography;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.RedPackage;
using CAServer.Options;
using CAServer.RedPackage.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using Orleans;

namespace CAServer.RedPackage;

public partial class RedPackageTest
{
    private IHttpContextAccessor GetMockHttpContextAccessor()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ImConstant.RelationAuthHeader] = "Bearer " + Guid.NewGuid().ToString("N");
        context.Request.Headers[CommonConstant.AuthHeader] = "Bearer " + Guid.NewGuid().ToString("N");
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(context);
        return httpContextAccessor;
    }

    private IOptionsSnapshot<ChainOptions> MockChainOptionsSnapshot()
    {
        var mock = new Mock<IOptionsSnapshot<ChainOptions>>();

        mock.Setup(o => o.Value).Returns(
            new ChainOptions
            {
                ChainInfos = new Dictionary<string, Options.ChainInfo>()
                {
                    {
                        "AELF", new Options.ChainInfo
                        {
                            ChainId = "AELF",
                            BaseUrl = "http://127.0.0.1:8000",
                            ContractAddress = "XXX",
                            TokenContractAddress = "XXX",
                            RedPackageContractAddress = "XXX"
                        }
                    }
                }
            });
        return mock.Object;
    }

    private IOptions<RedPackageOptions> MockRedpackageOptions()
    {
        var redPackageTokenInfos = new List<RedPackageTokenInfo>();
        redPackageTokenInfos.Add(new RedPackageTokenInfo
        {
            ChainId = "AELF",
            Symbol = "ELF",
            Decimal = 8,
            MinAmount = "1"
        });
        return new OptionsWrapper<RedPackageOptions>(new RedPackageOptions
        {
            MaxCount = 1000,
            ExpireTimeMs = 1000 * 60 * 60 * 24,
            TokenInfo = redPackageTokenInfos
        });
    }

    private IClusterClient GetMockClusterClient()
    {
        var mockClusterClient = new Mock<IClusterClient>();
        mockClusterClient.Setup(o => o.GetGrain<IRedPackageKeyGrain>(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns((Guid primaryKey, string namePrefix) => { return MockRedPackageKeyGrain(); });
        mockClusterClient.Setup(o => o.GetGrain<ICryptoBoxGrain>(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns((Guid primarykey, string namePrefix) => { return MockCryptoBoxGrain(); });
        return mockClusterClient.Object;
    }

    private IRedPackageKeyGrain MockRedPackageKeyGrain()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        string publicKey = keyPair.PublicKey.ToHex();
        string privateKey = keyPair.PrivateKey.ToHex();

        var mockockOrderGrain = new Mock<IRedPackageKeyGrain>();
        mockockOrderGrain.Setup(o => o.GenerateKeyAndSignature(It.IsAny<string>()))
            .ReturnsAsync(Tuple.Create(publicKey, "Signature"));
        mockockOrderGrain.Setup(o => o.GetPublicKey()).ReturnsAsync(publicKey);
        return mockockOrderGrain.Object;
    }

    private ICryptoBoxGrain MockCryptoBoxGrain()
    {
        var mock = new Mock<ICryptoBoxGrain>();
        mock.Setup(o => o.CreateRedPackage(It.IsAny<SendRedPackageInputDto>(), It.IsAny<int>(), It.IsAny<long>(),
            It.IsAny<Guid>(), It.IsAny<long>())).ReturnsAsync(new GrainResultDto<RedPackageDetailDto>
        {
            Success = true,
            Message = "",
            Data = new RedPackageDetailDto
            {
                Id = Guid.NewGuid(),
                TotalCount = 10,
                TotalAmount = "800",
                GrabbedAmount = null,
                MinAmount = "1",
                CurrentUserGrabbedAmount = null,
                Memo = null,
                ChainId = null,
                PublicKey = null,
                SenderId = default,
                LuckKingId = default,
                IsRedPackageFullyClaimed = false,
                IsRedPackageExpired = false,
                SenderAvatar = null,
                SenderName = null,
                CreateTime = 0,
                EndTime = 0,
                ExpireTime = 0,
                Symbol = null,
                Decimal = 0,
                Count = 0,
                Grabbed = 0,
                ChannelUuid = null,
                IsCurrentUserGrabbed = false,
                Type = (RedPackageType)0,
                Status = RedPackageStatus.Init,
                Items = null,
                IfRefund = false
            }
        });

        var commonResultDto = new GrainResultDto<RedPackageDetailDto>
        {
            Success = true,
            Message = "",
            Data = new RedPackageDetailDto
            {
                Id = Guid.Parse("f825f8f1-d3a4-4ee7-a98d-ad06b61094c0"),
                TotalCount = 10,
                TotalAmount = "800",
                GrabbedAmount = null,
                MinAmount = "1",
                CurrentUserGrabbedAmount = null,
                Memo = null,
                ChainId = null,
                PublicKey = null,
                SenderId = default,
                LuckKingId = default,
                IsRedPackageFullyClaimed = false,
                IsRedPackageExpired = false,
                SenderAvatar = null,
                SenderName = null,
                CreateTime = 0,
                EndTime = 0,
                ExpireTime = 0,
                Symbol = null,
                Decimal = 0,
                Count = 0,
                Grabbed = 0,
                ChannelUuid = null,
                IsCurrentUserGrabbed = false,
                Type = (RedPackageType)0,
                Status = RedPackageStatus.Claimed,
                Items = new List<GrabItemDto>(),
                IfRefund = false
            }
        };
        mock.Setup(o => o.GetRedPackage(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid>(), RedPackageDisplayType.Common)).ReturnsAsync(
            commonResultDto
        );
        mock.Setup(o => o.GetRedPackage(It.IsAny<Guid>())).ReturnsAsync(
            commonResultDto
        );
        mock.Setup(o => o.GrabRedPackage(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(new GrainResultDto<GrabResultDto>
        {
            Success = true,
            Message = null,
            Data = new GrabResultDto
            {
                Result = RedPackageGrabStatus.Success,
                ErrorMessage = null,
                Amount = "10",
                Decimal = 8,
                Status = RedPackageStatus.Claimed,
                ExpireTime = 100000
            }
        });
        return mock.Object;
    }

    private INESTRepository<RedPackageIndex, Guid> MockRedPackageIndex()
    {
        var mock = new Mock<INESTRepository<RedPackageIndex, Guid>>();
        mock.Setup(o => o.GetAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync((Guid sessionId, string Index) =>
        {
            var redPackageIndex = new RedPackageIndex
            {
                Id = default,
                RedPackageId = default,
                TotalAmount = 0,
                MinAmount = 0,
                Memo = null,
                SenderId = default,
                CreateTime = 0,
                EndTime = 0,
                ExpireTime = 0,
                Symbol = null,
                Decimal = 0,
                Count = 0,
                ChannelUuid = null,
                SendUuid = null,
                Message = null,
                Type = (RedPackageType)0,
                TransactionId = null,
                TransactionResult = null,
                ErrorMessage = null,
                SenderRelationToken = null,
                SenderPortkeyToken = null,
                TransactionStatus = RedPackageTransactionStatus.Processing,
                Items = null
            };
            var guid = Guid.Parse("1f691ad9-1a99-4456-b4d4-fdfc3cd128a2");
            if (sessionId.Equals(guid))
            {
                redPackageIndex.TransactionStatus = RedPackageTransactionStatus.Fail;
            }

            return redPackageIndex;
        });
        return mock.Object;
    }
    
    private IGraphQLHelper GetMockIGraphQLHelper()
    {
        var mockHelper = new Mock<IGraphQLHelper>();
        return mockHelper.Object;
    }
}