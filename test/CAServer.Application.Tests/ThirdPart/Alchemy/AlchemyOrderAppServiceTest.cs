using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Portkey.Contracts.CA;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Validation;
using Xunit;
using TransferInput = AElf.Client.MultiToken.TransferInput;

namespace CAServer.ThirdPart.Alchemy;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public sealed partial class AlchemyOrderAppServiceTest : CAServerApplicationTestBase
{
    private readonly IAlchemyOrderAppService _alchemyOrderAppService;

    public AlchemyOrderAppServiceTest()
    {
        _alchemyOrderAppService = GetRequiredService<IAlchemyOrderAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockThirdPartOptions());
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
            await _alchemyOrderAppService.UpdateAlchemyOrderAsync(input);
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
            await _alchemyOrderAppService.UpdateAlchemyOrderAsync(input);
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
            await _alchemyOrderAppService.UpdateAlchemyOrderAsync(input);
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
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
        var transaction = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(
                "0a220a20d1b17d6b133fe2eb499e0ddf5d67789704e1ac17534898b7aae54ffb0dc2c2f012220a20f9f90416670ec1a0f2d302c9474d1bc7a475cb08caa366bcca16e2f3d7e549f518c5f8f20c2204646353af2a124d616e61676572466f727761726443616c6c3283010a220a20092ce389906b6ae0328cff5aefee3ff3c3067e97d7600a849bc2f42cf0c5493d12220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc1a085472616e73666572222f0a220a20a7376d782cdf1b1caa2f8b5f56716209045cd5720b912e8441b4404427656cb91203454c461880f4f6905d82f10441edcb9051cd47ff7015175715b32bf001a570e365b6bc869502107372f5a691080aa97ee5d1e91a966c2f44aafb860f0a0fe204530b65fed7fba190d58ee4881a00"));
        var forwardCall = ManagerForwardCallInput.Parser.ParseFrom(transaction.Params);
        var transfer = TransferInput.Parser.ParseFrom(forwardCall.Args);
        transfer.Symbol.ShouldNotBeNull();
        
        await _alchemyOrderAppService.TransactionAsync(new TransactionDto()
        {
            MerchantName = "Alchemy",
            OrderId = Guid.Parse("a4610e8a-a60b-1fca-d53f-3a0da17ce0f0"),
            RawTransaction =
                "0a220a20b0bbbde38f07b2c27294d725f514bb0b9d655e5aaf91d5db32e99696693d32f512220a209479bf7e3de88e68e8be95938909a69331d38f673816e27e6dd98bc5b813593718e8e289512204bc16e05d2a124d616e61676572466f727761726443616c6c32520a220a2004bebca8a858d6dab11f4bef1817b45bf342bacef7b0f6cf7a5c8877d33e17ae12220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc1a085472616e7366657282f10441c01ea35e4ead8d148e2475cc3d3da187567340eaf46bad3283b794e2cce4e95b44138202420a65d1a859e01b31760ea04e3c045864bd1c1b3b3893fb23b4007201",
            PublicKey =
                "042d63cbefc37ddbf9202c9e32fd6abd55c2df828b21077e77687185d94657f88a6020699db5e1a49726122ed58c4d96c25244621fbd75c621037fc3c866fbc230",
            Signature =
                "1ba3e1dbc2f2d155a1c22d1881bb20b676b02fdfda60c8b9b91f776e9efc136629c35ca99bf284723a36a2ff195ed5a290051c34658911969ba9f029df7c231300"
        });
    }

    [Fact]
    public async Task Signature_Verify_Fail_Test()
    {
        try
        {
            await _alchemyOrderAppService.TransactionAsync(new TransactionDto()
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
            await _alchemyOrderAppService.TransactionAsync(new TransactionDto()
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
        await _alchemyOrderAppService.QueryAlchemyOrderInfoAsync(input);
    }
}