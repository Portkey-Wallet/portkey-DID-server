using System;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.IdGenerators;
using Shouldly;
using Xunit;
using Volo.Abp.Guids;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class AlchemyOrderAppServiceTest : CAServerApplicationTestBase
{
    private readonly IAlchemyOrderAppService _alchemyOrderAppService;

    public AlchemyOrderAppServiceTest()
    {
        _alchemyOrderAppService = GetRequiredService<IAlchemyOrderAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
    }

    [Fact]
    public async Task UpdateAlchemyOrderAsyncTest()
    {
        var input = new AlchemyOrderUpdateDto
        {
            MerchantOrderNo = "00000000-0000-0000-0000-000000000000", //MerchantOrderNo = Guid.NewGuid().ToString(),
            Status = "1",
            Address = "Address"
        };
        var result = await _alchemyOrderAppService.UpdateAlchemyOrderAsync(input);
        result.Success.ShouldBe(true);

        var inputFail = new AlchemyOrderUpdateDto
        {
            MerchantOrderNo = "00000000-0000-0000-0000-000000000001", //MerchantOrderNo = Guid.NewGuid().ToString(),
            Status = "2",
            Address = "Address"
        };
        var resultFail = await _alchemyOrderAppService.UpdateAlchemyOrderAsync(inputFail);
        resultFail.Success.ShouldBe(false);
    }
}