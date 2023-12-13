using System.Collections;
using CAServer.ContractEventHandler.Core;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage.Dtos;
using CAServer.RedPackage.Etos;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.RedPackage;

public class PayRedPackageTaskTest : CAServerEntityEventHandlerTestBase
{
    [Fact]
    public async void PayRedPackageTaskHadleTest()
    {
        var userId = Guid.NewGuid();
        var redPackageId = Guid.NewGuid();
        var redPackageGrain = Cluster.Client.GetGrain<ICryptoBoxGrain>(redPackageId);
        var res = await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(redPackageId), 8, 1, userId,86400000);
        
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        await redPackageGrain.GrabRedPackage(userId1, "xxxx");
        var afterGrab = await redPackageGrain.GetRedPackage(redPackageId);
        var grabItemDtos = afterGrab.Data.Items;
       
        var redPackageCreateEto = new RedPackageCreateEto()
        {
            RedPackageId = redPackageId,
            
        };
        //await _packageTask.PayRedPackageAsync(redPackageCreateEto);
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
            // SendUuid = "xxx",
            RawTransaction = "xxxxx",
            Message = "xxxx"
        };
    }
    
}