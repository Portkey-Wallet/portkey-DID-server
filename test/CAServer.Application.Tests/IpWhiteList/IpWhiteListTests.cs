using System;
using System.Threading.Tasks;
using CAServer.ClaimToken.Dtos;
using CAServer.IpWhiteList.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.IpWhiteList;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class IpWhiteListTests : CAServerApplicationTestBase
{
    
    private readonly IIpWhiteListAppService _ipWhiteListAppService;

    public IpWhiteListTests()
    {
        _ipWhiteListAppService = GetRequiredService<IIpWhiteListAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockHttpClientFactory());
        services.AddSingleton(GetAddToWhiteListUrlsOptions());
    }

    [Fact]
    public async Task AddIpWhiteListAsync()
    {
        var requestDto = new AddUserIpToWhiteListRequestDto
        {
            UserId = Guid.NewGuid(),
            UserIp = "127.0.0.1"
        };
        await _ipWhiteListAppService.AddIpWhiteListAsync(requestDto);
        
    }
    
    [Fact]
    public async Task IsIpInWhiteListAsync()
    {
        var ip = "127.0.0.1";
        var result = await _ipWhiteListAppService.IsInWhiteListAsync(ip);
        result.ShouldBe(true);
        
    }
    
}