using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class AlchemyOrderAppServiceTest : CAServerApplicationTestBase
{
    private readonly IAlchemyOrderAppService _alchemyOrderAppService;
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;

    public AlchemyOrderAppServiceTest()
    {
        _alchemyOrderAppService = GetRequiredService<IAlchemyOrderAppService>();
        _alchemyServiceAppService = GetRequiredService<IAlchemyServiceAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(getMockThirdPartOptions());
    }

    [Fact]
    public async Task UpdateAlchemyOrderAsyncTest()
    {
        var input = new AlchemyOrderUpdateDto
        {
            MerchantOrderNo = "00000000-0000-0000-0000-000000000000", //MerchantOrderNo = Guid.NewGuid().ToString(),
            Status = "1",
            Address = "Address",
            Signature = "5f4e9f8c1f3a63c12032b9c6c59a019c259bd063"
        };
        var result = await _alchemyOrderAppService.UpdateAlchemyOrderAsync(input);
        result.Success.ShouldBe(true);

        var inputFail = new AlchemyOrderUpdateDto
        {
            MerchantOrderNo = "00000000-0000-0000-0000-000000000001", //MerchantOrderNo = Guid.NewGuid().ToString(),
            Status = "2",
            Address = "Address",
            Signature = "5f4e9f8c1f3a63c12032b9c6c59a019c259bd063"
        };
        var resultFail = await _alchemyOrderAppService.UpdateAlchemyOrderAsync(inputFail);
        resultFail.Success.ShouldBe(false);

        var signatureFail = new AlchemyOrderUpdateDto
        {
            MerchantOrderNo = "00000000-0000-0000-0000-000000000001", //MerchantOrderNo = Guid.NewGuid().ToString(),
            Status = "2",
            Address = "Address",
            Signature = "1111111111111111111111111111111111"
        };
        var signResultFail = await _alchemyOrderAppService.UpdateAlchemyOrderAsync(signatureFail);
        signResultFail.Success.ShouldBe(false);
    }

    [Fact]
    public async Task UpdateAlchemyTxHashAsyncTest()
    {
        var input = new UpdateAlchemyTxHashDto()
        {
            MerchantName = "Alchemy",
            OrderId = "00000000-0000-0000-0000-000000000000", //MerchantOrderNo = Guid.NewGuid().ToString(),
            TxHash = "123"
        };
        try
        {
            await _alchemyOrderAppService.UpdateAlchemyTxHashAsync(input);
        }
        catch (Exception e)
        {
            e.ShouldBe(null);
        }

        var inputFail = new UpdateAlchemyTxHashDto()
        {
            MerchantName = "Alchemy",
            OrderId = "00000000-0000-0000-0000-000000000001", //MerchantOrderNo = Guid.NewGuid().ToString(),
            TxHash = "123"
        };
        try
        {
            await _alchemyOrderAppService.UpdateAlchemyTxHashAsync(inputFail);
        }
        catch (Exception e)
        {
            e.ShouldNotBe(null);
        }
    }

    [Fact]
    public async Task UpdateAlchemySellAddressAsyncTest()
    {
        var input = new AlchemyOrderUpdateDto()
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
            UserId = new Guid("00000000-0000-0000-0000-000000000001"),
            MerchantOrderNo = "00000000-0000-0000-0000-000000000000",
            Address = "Address",
            Status = "1",
            Signature = "NOT_SET_YET",
        };

        try
        {
            await _alchemyOrderAppService.UpdateAlchemyOrderAsync(input);
        }
        catch (Exception e)
        {
            e.ShouldBe(null);
        }
    }

    [Fact]
    public async Task VerifySignatureAndConvertToDto()
    {

        // fail test
        try
        {
            await _alchemyOrderAppService.VerifyAlchemySignature<AlchemyOrderUpdateDto>(
                new Dictionary<string, string>());
        }
        catch (Exception e)
        {
            e.GetType().ShouldBe(typeof(UserFriendlyException));
        }

        // verify test
        var inputDict = new Dictionary<string, string>()
        {
            ["appid"] = "12344fdsfdsfdsfsdfdsfsdfsdfdsfsdfa",
            ["id"] = "00000000-0000-0000-0000-000000000001",
            ["userId"] = "00000000-0000-0000-0000-000000000001",
            ["merchantOrderNo"] = "00000000-0000-0000-0000-000000000000",
            ["orderNo"] = "00000000-0000-0000-0000-000000000000",
            ["Address"] = "Address123123",
            ["status"] = "1",
            ["signature"] = "03c118dfb11a20060e41b2e11d09d15a88aea93a10463d06f0dd3b157e007f89",
        };

        var input = await _alchemyOrderAppService.VerifyAlchemySignature<AlchemyOrderUpdateDto>(inputDict);

        input.Signature.ShouldBe("03c118dfb11a20060e41b2e11d09d15a88aea93a10463d06f0dd3b157e007f89");
        input.Address.ShouldBe("Address123123");
        input.Id.ShouldBe(new Guid("00000000-0000-0000-0000-000000000001"));
    }
}