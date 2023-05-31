using System;
using System.Collections.Generic;
using System.Reflection;
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
        AlchemySignatureResultDto signatureResultDto = await _alchemyServiceAppService.GetAlchemySignatureV2Async(input, new List<string>(){"signature"});
        input.Signature = signatureResultDto.Signature;
        
        try
        {
            await _alchemyOrderAppService.UpdateAlchemyOrderAsync(input);
        }
        catch (Exception e)
        {
            e.ShouldBe(null);
        }
    }

}