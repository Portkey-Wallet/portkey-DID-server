using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage;
using CAServer.RedPackage.Dtos;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.RedPackage;

public class RedPackageGrainTest : CAServerGrainTestBase
{
    [Fact]
    public async Task GenerateRedPackageAsync_test()
    {
        var redPackageGrain = Cluster.Client.GetGrain<IRedPackageKeyGrain>(Guid.NewGuid());
        var res = await redPackageGrain.GenerateKey();
        res.ShouldNotBeNull();
        res = await redPackageGrain.GenerateSignature("1asdasd");
        res.ShouldNotBeNull();

        res = await redPackageGrain.GetPublicKey();
        res.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateRedPackage_test()
    {
        var userId = Guid.NewGuid();
        var redPackageId = Guid.NewGuid();
        var redPackageGrain = Cluster.Client.GetGrain<IRedPackageGrain>(redPackageId);
        var res = await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(redPackageId), 8, 1, userId);
        res.Success.ShouldBe(true);
        res = await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(redPackageId), 8, 1, userId);
        res.Success.ShouldBe(false);
        await redPackageGrain.DeleteRedPackage();
        var detail = await redPackageGrain.GetRedPackage(0, 10, userId);
        detail.Data.Status.ShouldBe(RedPackageStatus.Expired);
        await redPackageGrain.CancelRedPackage();
        detail = await redPackageGrain.GetRedPackage(0, 10, userId);
        detail.Data.Status.ShouldBe(RedPackageStatus.Cancelled);
        
        var input = NewSendRedPackageInputDto(Guid.NewGuid());
        input.Type = RedPackageType.Fixed;
        await redPackageGrain.CreateRedPackage(input, 8, 1, userId);
        input.Type = RedPackageType.QuickTransfer;
        await redPackageGrain.CreateRedPackage(input, 8, 1, userId);
    }

    [Fact]
    public async Task GrabRedPackage_test()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var redPackageId = Guid.NewGuid();
        var redPackageGrain = Cluster.Client.GetGrain<IRedPackageGrain>(redPackageId);
        var input = NewSendRedPackageInputDto(redPackageId);
        input.Count = 2;
        await redPackageGrain.CreateRedPackage(input, 8, 1, userId1);
        var res = await redPackageGrain.GrabRedPackage(userId1, "xxxx");
        res.Success.ShouldBe(true);
        await redPackageGrain.GrabRedPackage(userId2, "xxxx");
        res = await redPackageGrain.GrabRedPackage(userId3, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageFullyClaimed);
        
        redPackageGrain = Cluster.Client.GetGrain<IRedPackageGrain>(Guid.NewGuid());
        await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(Guid.NewGuid()), 8, 1, userId1);
        await redPackageGrain.CancelRedPackage();
        res = await redPackageGrain.GrabRedPackage(userId3, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageCancelled);
        await redPackageGrain.DeleteRedPackage();
        res = await redPackageGrain.GrabRedPackage(userId3, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageExpired);

        redPackageGrain = Cluster.Client.GetGrain<IRedPackageGrain>(Guid.NewGuid());
        await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(Guid.NewGuid()), 8, 1, userId1);
        await redPackageGrain.GrabRedPackage(userId2, "xxxx");
        res = await redPackageGrain.GrabRedPackage(userId2, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageUserGrabbed);
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
    public async Task UpdateRedPackage_test()
    {
        var redPackageId =  await GetNewPackageId();
        var redPackageGrain = Cluster.Client.GetGrain<IRedPackageGrain>(redPackageId);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var res = await redPackageGrain.GrabRedPackage(userId1, "xxxx");

        await redPackageGrain.UpdateRedPackage(redPackageId, userId1, "xxxx");
    }

    private async Task<Guid> GetNewPackageId()
    {
        var userId = Guid.NewGuid();
        var redPackageId = Guid.NewGuid();
        var redPackageGrain = Cluster.Client.GetGrain<IRedPackageGrain>(redPackageId);
        var res = await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(redPackageId), 8, 1, userId);
        res.Success.ShouldBe(true);
        res = await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(redPackageId), 8, 1, userId);
        res.Success.ShouldBe(false);
        await redPackageGrain.UpdateRedPackage(redPackageId, userId, "xxxx");
        return redPackageId;
    }
}