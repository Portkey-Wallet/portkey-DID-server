using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using CAServer.Security.Dtos;
using CAServer.UserAssets;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.Security;

public partial class UserSecurityAppServiceTest : CAServerApplicationTestBase
{
    protected IUserSecurityAppService _userSecurityAppService;

    public UserSecurityAppServiceTest()
    {
        _userSecurityAppService = GetRequiredService<IUserSecurityAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockUserAssetsProvider());
        services.AddSingleton(GetMockUserSecurityProvider());
        services.AddSingleton(GetContractProvider());
    }

    [Fact]
    public async Task GetTransferLimitListByCaHashAsyncTest()
    {
        var param = new GetTokenRequestDto
        {
            SkipCount = 0,
            MaxResultCount = 10,
            CaAddresses = new List<string> { "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo" }
        };


        var result = await _userSecurityAppService.GetTransferLimitListByCaHashAsync(
            new GetTransferLimitListByCaHashAsyncDto
            {
                MaxResultCount = 100,
                SkipCount = 0,
                CaHash = "a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033"
            });
        result.TotalRecordCount.ShouldBe(1);

        var data = result.Data.First();
        data.Symbol.ShouldBe("ELF");
        data.ChainId.ShouldBe("AELF");
        data.DailyLimit.ShouldBe(10000);
        data.SingleLimit.ShouldBe(10000);
    }
}