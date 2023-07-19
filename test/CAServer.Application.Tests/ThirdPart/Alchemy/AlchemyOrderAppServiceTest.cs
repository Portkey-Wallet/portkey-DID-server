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

    public AlchemyOrderAppServiceTest()
    {
        _alchemyOrderAppService = GetRequiredService<IAlchemyOrderAppService>();
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
            Crypto = "Crypto",
            OrderNo = "OrderNo",
            Signature = "a384b2b7150b1593bd1f9de5e07cd6cbe427edea"
        };
        var result = await _alchemyOrderAppService.UpdateAlchemyOrderAsync(input);
        result.Success.ShouldBe(true);

        var inputFail = new AlchemyOrderUpdateDto
        {
            MerchantOrderNo = "00000000-0000-0000-0000-000000000001", //MerchantOrderNo = Guid.NewGuid().ToString(),
            Status = "2",
            Address = "Address",
            Crypto = "Crypto",
            OrderNo = "OrderNo",
            Signature = "5f4e9f8c1f3a63c12032b9c6c59a019c259bd063"
        };
        var resultFail = await _alchemyOrderAppService.UpdateAlchemyOrderAsync(inputFail);
        resultFail.Success.ShouldBe(false);

        var signatureFail = new AlchemyOrderUpdateDto
        {
            MerchantOrderNo = "00000000-0000-0000-0000-000000000001", //MerchantOrderNo = Guid.NewGuid().ToString(),
            Status = "2",
            Address = "Address",
            Crypto = "Crypto",
            OrderNo = "OrderNo",
            Signature = "1111111111111111111111111111111111"
        };
        var signResultFail = await _alchemyOrderAppService.UpdateAlchemyOrderAsync(signatureFail);
        signResultFail.Success.ShouldBe(false);
    }

    [Fact]
    public async Task UpdateAlchemyTxHashAsyncTest()
    {
        var input = new SendAlchemyTxHashDto()
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

        var inputFail = new SendAlchemyTxHashDto()
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
            Status = "1",
            Address = "Address",
            Crypto = "Crypto",
            OrderNo = "OrderNo",
            Signature = "a384b2b7150b1593bd1f9de5e07cd6cbe427edea"
        };

        var signResultFail = await _alchemyOrderAppService.UpdateAlchemyOrderAsync(input);
        signResultFail.Success.ShouldBe(true);
    }

    [Fact]
    public async Task SignatureTest()
    {
        await _alchemyOrderAppService.TransactionAsync(new TransactionDto()
        {
            MerchantName = "Alchemy",
            OrderId = Guid.Parse("5ee4a7b7-5c41-a40b-f17d-3a0c7607f66e"),
            RawTransaction =
                "0a220a20e53eff822ad4b33e8ed0356a55e5b8ea83a88afdb15bdedcf52646d8c13209c812220a20f9f90416670ec1a0f2d302c9474d1bc7a475cb08caa366bcca16e2f3d7e549f518b6f8ff082204fe3bce362a124d616e61676572466f727761726443616c6c32e3010a220a20ffc98c7be1a50ada7ca839da2ecd94834525bdcea392792957cc7f1b2a0c3a1e12220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc1a085472616e73666572228e010a220a20e53eff822ad4b33e8ed0356a55e5b8ea83a88afdb15bdedcf52646d8c13209c812220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc18b5f8ff082204f531bbae2a085472616e73666572322f0a220a2061033c0453282232747683ffa571455f5511b5274f2125e2ee226b7fb2ebc9c11203454c461880d88ee16f82f10441b44d4da51193549f31d8862825799c0d20e08afb961b92ec9f8eb4edbc8cfab9518d4454ad0a703e812b777d3bc5d249b70b1cb02daf60a7116f2ec0f78657d001",
            PublicKey =
                "04bc680e9f8ea189fb510f3f9758587731a9a64864f9edbc706cea6e8bf85cf6e56f236ba58d8840f3fce34cbf16a97f69dc784183d2eef770b367f6e8a90151af",
            Signature =
                "f1a97777a32fdc6960d9de07939441bda63066de9aefe49d06acd392253e30870f5187475594dd82c6986c8425e684264b93e6f464e326222b212d231223791d00"
        });
    }
}