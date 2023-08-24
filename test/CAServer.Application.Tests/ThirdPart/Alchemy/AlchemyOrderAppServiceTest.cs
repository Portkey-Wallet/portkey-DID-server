using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Validation;
using Xunit;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class AlchemyOrderAppServiceTest : CAServerApplicationTestBase
{
    private readonly IThirdPartFactory _thirdPartFactory;
    private const string MerchantName = "alchemy";
    public AlchemyOrderAppServiceTest()
    {
        _thirdPartFactory = GetRequiredService<IThirdPartFactory>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(ThirdPartMock.GetMockThirdPartOptions());
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
        var result = await _thirdPartFactory.GetProcessor(MerchantName).OrderUpdate(input);
        result.Success.ShouldBe(true);

        var inputFail = new AlchemyOrderUpdateDto
        {
            MerchantOrderNo = "00000000-0000-0000-0000-000000000001", //MerchantOrderNo = Guid.NewGuid().ToString(),
            Status = "2",
            Address = "Address",
            Crypto = "Crypto",
            OrderNo = "OrderNo",
            Signature = "a384b2b7150b1593bd1f9de5e07cd6cbe427edea"
        };
        var resultFail = await _thirdPartFactory.GetProcessor(MerchantName).OrderUpdate(inputFail);
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
        var signResultFail = await _thirdPartFactory.GetProcessor(MerchantName).OrderUpdate(signatureFail);
        signResultFail.Success.ShouldBe(false);
    }

    [Fact]
    public async Task UpdateAlchemyOrderAsync_Signature_Is_Null_Test()
    {
        try
        {
            var input = new AlchemyOrderUpdateDto
            {
                MerchantOrderNo = "00000000-0000-0000-0000-000000000000", //MerchantOrderNo = Guid.NewGuid().ToString(),
                Status = "1",
                Address = "Address",
                Crypto = "Crypto",
                OrderNo = "OrderNo",
                Signature = null
            };
            await _thirdPartFactory.GetProcessor(MerchantName).OrderUpdate(input);
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task UpdateAlchemyOrderAsync_MerchantOrderNo_Is_Null_Test()
    {
        try
        {
            var input = new AlchemyOrderUpdateDto
            {
                MerchantOrderNo = null, //MerchantOrderNo = Guid.NewGuid().ToString(),
                Status = "1",
                Address = "Address",
                Crypto = "Crypto",
                OrderNo = "OrderNo",
                Signature = "1111111111111111111111111111111111"
            };
            await _thirdPartFactory.GetProcessor(MerchantName).OrderUpdate(input);
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task UpdateAlchemyOrderAsync_Status_Not_Exist_Test()
    {
        try
        {
            var input = new AlchemyOrderUpdateDto
            {
                MerchantOrderNo = null, //MerchantOrderNo = Guid.NewGuid().ToString(),
                Status = "100",
                Address = "Address",
                Crypto = "Crypto",
                OrderNo = "OrderNo",
                Signature = "1111111111111111111111111111111111"
            };
            await _thirdPartFactory.GetProcessor(MerchantName).OrderUpdate(input);
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task UpdateAlchemyTxHashAsyncTest()
    {
        var input = new TransactionHashDto()
        {
            MerchantName = "Alchemy",
            OrderId = "00000000-0000-0000-0000-000000000000", //MerchantOrderNo = Guid.NewGuid().ToString(),
            TxHash = "123"
        };
        try
        {
            await _thirdPartFactory.GetProcessor(MerchantName).UpdateTxHashAsync(input);
        }
        catch (Exception e)
        {
            e.ShouldBe(null);
        }

        var inputFail = new TransactionHashDto()
        {
            MerchantName = "Alchemy",
            OrderId = "00000000-0000-0000-0000-000000000001", //MerchantOrderNo = Guid.NewGuid().ToString(),
            TxHash = "123"
        };
        try
        {
            await _thirdPartFactory.GetProcessor(MerchantName).UpdateTxHashAsync(inputFail);
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
            MerchantOrderNo = "00000000-0000-0000-0000-000000000000",
            Status = "1",
            Address = "Address",
            Crypto = "Crypto",
            OrderNo = "OrderNo",
            Signature = "a384b2b7150b1593bd1f9de5e07cd6cbe427edea"
        };

        var signResultFail = await _thirdPartFactory.GetProcessor(MerchantName).OrderUpdate(input);
        signResultFail.Success.ShouldBe(true);
    }

    [Fact]
    public async Task SignatureTest()
    {
        await _thirdPartFactory.GetProcessor(MerchantName).ForwardTransactionAsync(new TransactionDto()
        {
            MerchantName = "Alchemy",
            OrderId = Guid.Parse("5ee4a7b7-5c41-a40b-f17d-3a0c7607f66e"),
            RawTransaction =
                "0a220a20e53eff822ad4b33e8ed0356a55e5b8ea83a88afdb15bdedcf52646d8c13209c812220a20f9f90416670ec1a0f2d302c9474d1bc7a475cb08caa366bcca16e2f3d7e549f518f7c6800922041a18bf592a124d616e61676572466f727761726443616c6c3283010a220a20ffc98c7be1a50ada7ca839da2ecd94834525bdcea392792957cc7f1b2a0c3a1e12220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc1a085472616e73666572222f0a220a2061033c0453282232747683ffa571455f5511b5274f2125e2ee226b7fb2ebc9c11203454c461880d88ee16f82f1044170041357071da3ad12a1df406db869a94d910cd03f2dbfd3b1176de74ba0406b5e44425e611f504b6d065ad3d3b9dfe4e699e61ab584fdd0e9deb972cb05cd2700",
            PublicKey =
                "04bc680e9f8ea189fb510f3f9758587731a9a64864f9edbc706cea6e8bf85cf6e56f236ba58d8840f3fce34cbf16a97f69dc784183d2eef770b367f6e8a90151af",
            Signature =
                "af2b9305e9d85404f8c67630bc410e58d6d30dc2f0cf5a021f8bbfd4237663fb7dfa51f978179a001394b23f32f464e5be856b3298063d5bf11d0c6fd8f35ca400"
        });
    }

    [Fact]
    public async Task Signature_Verify_Fail_Test()
    {
        try
        {
            await _thirdPartFactory.GetProcessor(MerchantName).ForwardTransactionAsync(new TransactionDto()
            {
                MerchantName = "Alchemy",
                OrderId = Guid.Parse("5ee4a7b7-5c41-a40b-f17d-3a0c7607f66e"),
                RawTransaction =
                    "1a220a20e53eff822ad4b33e8ed0356a55e5b8ea83a88afdb15bdedcf52646d8c13209c812220a20f9f90416670ec1a0f2d302c9474d1bc7a475cb08caa366bcca16e2f3d7e549f518f7c6800922041a18bf592a124d616e61676572466f727761726443616c6c3283010a220a20ffc98c7be1a50ada7ca839da2ecd94834525bdcea392792957cc7f1b2a0c3a1e12220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc1a085472616e73666572222f0a220a2061033c0453282232747683ffa571455f5511b5274f2125e2ee226b7fb2ebc9c11203454c461880d88ee16f82f1044170041357071da3ad12a1df406db869a94d910cd03f2dbfd3b1176de74ba0406b5e44425e611f504b6d065ad3d3b9dfe4e699e61ab584fdd0e9deb972cb05cd2700",
                PublicKey =
                    "04bc680e9f8ea189fb510f3f9758587731a9a64864f9edbc706cea6e8bf85cf6e56f236ba58d8840f3fce34cbf16a97f69dc784183d2eef770b367f6e8a90151af",
                Signature =
                    "af2b9305e9d85404f8c67630bc410e58d6d30dc2f0cf5a021f8bbfd4237663fb7dfa51f978179a001394b23f32f464e5be856b3298063d5bf11d0c6fd8f35ca400"
            });
        }
        catch (Exception e)
        {
            e.ShouldNotBe(null);
        }
    }

    [Fact]
    public async Task Signature_Verify_Error_Test()
    {
        try
        {
            await _thirdPartFactory.GetProcessor(MerchantName).ForwardTransactionAsync(new TransactionDto()
            {
                MerchantName = "Alchemy",
                OrderId = Guid.Parse("5ee4a7b7-5c41-a40b-f17d-3a0c7607f66e"),
                RawTransaction =
                    "1a220a20e53eff822ad4b33e8ed0356a55e5b8ea83a88afdb15bdedcf52646d8c13209c812220a20f9f90416670ec1a0f2d302c9474d1bc7a475cb08caa366bcca16e2f3d7e549f518f7c6800922041a18bf592a124d616e61676572466f727761726443616c6c3283010a220a20ffc98c7be1a50ada7ca839da2ecd94834525bdcea392792957cc7f1b2a0c3a1e12220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc1a085472616e73666572222f0a220a2061033c0453282232747683ffa571455f5511b5274f2125e2ee226b7fb2ebc9c11203454c461880d88ee16f82f1044170041357071da3ad12a1df406db869a94d910cd03f2dbfd3b1176de74ba0406b5e44425e611f504b6d065ad3d3b9dfe4e699e61ab584fdd0e9deb972cb05cd2700",
                PublicKey =
                    "04bc680e9f8ea189fb510f3f9758587731a9a64864f9edbc706ce",
                Signature =
                    "af2b9305e9d85404f8c67630bc410e58d6d30dc2f0cf5a021f8bbfd4"
            });
        }
        catch (Exception e)
        {
            e.ShouldNotBe(null);
        }
    }

    [Fact]
    public async Task TestQueryAlchemyOrderInfoAsync()
    {
        var input = new OrderDto
        {
            TransDirect = "1",
            Id = Guid.Empty,
            ThirdPartOrderNo = "123"
        };
        await _thirdPartFactory.GetProcessor(MerchantName).QueryThirdOrderAsync(input);
    }
}