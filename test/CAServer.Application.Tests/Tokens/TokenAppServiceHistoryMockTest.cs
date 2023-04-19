using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Tokens.TokenPrice;
using CAServer.Options;
using CAServer.Tokens.Dtos;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Options;
using MongoDB.Driver.Core.Clusters;
using Moq;
using NSubstitute;
using Orleans;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens;

public partial class TokenAppServiceHistoryTest
{
    private ITokenPriceGrain GetMockTokenPriceGrain()
    {
        var mockITokenPriceGrainClient = new Mock<ITokenPriceGrain>();
        mockITokenPriceGrainClient.Setup(o => o.GetCurrentPriceAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new GrainResultDto<TokenPriceGrainDto>()
                {
                    Success = true,
                    Data = new TokenPriceGrainDto()
                    {
                        Symbol = Symbol,
                        PriceInUsd = 1000,
                    }
                } 
            );

        return mockITokenPriceGrainClient.Object;

    }
    
    private ITokenPriceSnapshotGrain GetMockTokenPriceSnapshotGrain()
    {
        var mockITokenPriceGrainClient = new Mock<ITokenPriceSnapshotGrain>();
        mockITokenPriceGrainClient.Setup(o => o.GetHistoryPriceAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(
                new GrainResultDto<TokenPriceGrainDto>()
                {
                    Success = true,
                    Data = new TokenPriceGrainDto()
                    {
                        Symbol = Symbol,
                        PriceInUsd = 1000,
                    }
                }
            );
        return mockITokenPriceGrainClient.Object;

    }
    
    private ITokenPriceProvider GetMockTokenPriceProvider()
    {
        var mockTokenPriceProvider = new Mock<ITokenPriceProvider>();
        mockTokenPriceProvider.Setup(o => o.GetPriceAsync(It.IsAny<string>()))
            .ReturnsAsync((string dto) => dto == Symbol ? 1000 : 100);
        
        return mockTokenPriceProvider.Object;

    }

    private IOptions<TokenPriceExpirationTimeOptions> GetMockTokenPriceExpirationTimeOptions()
    {
        return new OptionsWrapper<TokenPriceExpirationTimeOptions>(new TokenPriceExpirationTimeOptions()
        {
            Time = 100
        });


    }

    private static IOptions<ContractAddressOptions> GetMockContractAddressOptions()
    {
        var tokenClaimContractAddress = new TokenClaimAddress
        {
            ContractName = "test",
            MainChainAddress = "test",
            SideChainAddress = "test"
        };
        var contractAddressOptions = new ContractAddressOptions
        {
            TokenClaimAddress = tokenClaimContractAddress
        };
        
        return new OptionsWrapper<ContractAddressOptions>(contractAddressOptions);
    }
    
    private IHttpClientFactory GetMockHttpClientFactory()
    {
        var clientHandlerStub = new DelegatingHistoryHandlerStub();
        var client = new HttpClient(clientHandlerStub);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var factory = mockFactory.Object;
        return factory;
    }
    
    private IClusterClient GetMockClusterClient()
    {
        var mockClusterClient = new Mock<IClusterClient>();
        mockClusterClient.Setup(o => o.GetGrain<IGrainWithStringKey>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string primaryKey, string namePrefix) =>
            {
                return GetMockTokenPriceGrain();
            });
        return mockClusterClient.Object;
    }
    
    private IClusterClient GetMockTokenPriceSnapshotClusterClient()
    {
        var mockClusterClient = new Mock<IClusterClient>();
        mockClusterClient.Setup(o => o.GetGrain<IGrainWithStringKey>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string primaryKey, string namePrefix) =>
            {
                return GetMockTokenPriceSnapshotGrain();
            });
        return mockClusterClient.Object;
    }
}
public class DelegatingHistoryHandlerStub : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

    public DelegatingHistoryHandlerStub()
    {
        _handlerFunc = (request, cancellationToken) => Task.FromResult(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
    }

    public DelegatingHistoryHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}