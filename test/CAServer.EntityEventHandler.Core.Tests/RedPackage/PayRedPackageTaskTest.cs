using System.Collections;
using CAServer.ContractEventHandler.Core;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage.Dtos;
using CAServer.RedPackage.Etos;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.RedPackage;

public class PayRedPackageTaskTest : CAServerEntityEventHandlerTestBase
{
    private IPayRedPackageTask _packageTask;
    
    private readonly PayRedPackageAccount _packageAccount;
 

    public PayRedPackageTaskTest()
    {
        _packageTask = GetRequiredService<IPayRedPackageTask>();
        _packageAccount = GetRequiredService<PayRedPackageAccount>();
    }

    [Fact]
    public async void PayRedPackageTaskHadleTest()
    {
        var userId = Guid.NewGuid();
        var redPackageId = Guid.NewGuid();
        var redPackageGrain = Cluster.Client.GetGrain<IRedPackageGrain>(redPackageId);
        var res = await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(redPackageId), 8, 1, userId);
        
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        await redPackageGrain.GrabRedPackage(userId1, "xxxx");
        var afterGrab = await redPackageGrain.GetRedPackage(redPackageId);
        var grabItemDtos = afterGrab.Data.Items;
        List<RedPackageCreateEto.GrabItemDto> list = new List<RedPackageCreateEto.GrabItemDto>();
        foreach (var item in grabItemDtos)
        {
            var grabItemDto = new RedPackageCreateEto.GrabItemDto();
            grabItemDto.Amount = item.Amount;
            grabItemDto.UserId = item.UserId;
            grabItemDto.CaAddress = item.CaAddress;
            grabItemDto.PaymentCompleted = item.PaymentCompleted;
            list.Add(grabItemDto);
        }
        var redPackageCreateEto = new RedPackageCreateEto()
        {
            RedPackageId = redPackageId,
            Items = list
            
        };
        await _packageTask.PayRedPackageAsync(redPackageCreateEto);
    }
    
    private SendRedPackageInputDto NewSendRedPackageInputDto(Guid redPackageId)
    {
        return new SendRedPackageInputDto()
        {
            Id = redPackageId,
            Type = RedPackageType.Random,
            Count = 500,
            TotalAmount = "1000000",
            Memo = "xxxx",
            ChainId = "AELF",
            Symbol = "ELF",
            ChannelUuid = "xxxx",
            SendUuid = "xxx",
            RawTransaction = "xxxxx",
            Message = "xxxx"
        };
    }

    [Fact]
    public async void RedPackageAccount()
    {
        _packageAccount.getOneAccountRandom();
    }
}