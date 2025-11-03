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
        var data = "1asdasd";
        var redPackageGrain = Cluster.Client.GetGrain<IRedPackageKeyGrain>(Guid.NewGuid());
        var res = await redPackageGrain.GenerateKey();
        res.ShouldNotBeNull();
        res = await redPackageGrain.GenerateSignature(data);
        res.ShouldNotBeNull();
        var verify = await redPackageGrain.VerifySignature(data, res);
        verify.ShouldBe(true);

        res = await redPackageGrain.GetPublicKey();
        res.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateRedPackage_test()
    {
        var userId = Guid.NewGuid();
        var redPackageId = Guid.NewGuid();
        var redPackageGrain = Cluster.Client.GetGrain<ICryptoBoxGrain>(redPackageId);
        var res = await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(redPackageId), 8, 1, userId,86400000);
        res.Success.ShouldBe(true);
        res = await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(redPackageId), 8, 1, userId,86400000);
        res.Success.ShouldBe(false);
        await redPackageGrain.ExpireRedPackage();
        var detail = await redPackageGrain.GetRedPackage(0, 10, userId);
        detail.Data.Status.ShouldBe(RedPackageStatus.Expired);
        await redPackageGrain.CancelRedPackage();
        detail = await redPackageGrain.GetRedPackage(0, 10, userId);
        detail.Data.Status.ShouldBe(RedPackageStatus.Cancelled);
        
        var input = NewSendRedPackageInputDto(Guid.NewGuid());
        input.Type = RedPackageType.Fixed;
        await redPackageGrain.CreateRedPackage(input, 8, 1, userId,86400000);
        input.Type = RedPackageType.QuickTransfer;
        await redPackageGrain.CreateRedPackage(input, 8, 1, userId,86400000);
    }

    [Fact]
    public async Task GrabRedPackage_test()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var redPackageId = Guid.NewGuid();
        var redPackageGrain = Cluster.Client.GetGrain<ICryptoBoxGrain>(redPackageId);
        var input = NewSendRedPackageInputDto(redPackageId);
        input.Count = 2;
        var redPackage = await redPackageGrain.CreateRedPackage(input, 8, 1, userId1,86400000);
        long amount = 0;
        foreach (var item in redPackage.Data.BucketNotClaimed)
        {
            Assert.NotEqual(0, item.Amount);
            amount += item.Amount;
        }
        Assert.Equal(input.TotalAmount, amount.ToString());
        var res = await redPackageGrain.GrabRedPackage(userId1, "xxxx");
        res.Success.ShouldBe(true);
        await redPackageGrain.GrabRedPackage(userId2, "xxxx");
        res = await redPackageGrain.GrabRedPackage(userId3, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageFullyClaimed);
        
        redPackageGrain = Cluster.Client.GetGrain<ICryptoBoxGrain>(Guid.NewGuid());
        await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(Guid.NewGuid()), 8, 1, userId1,86400000);
        await redPackageGrain.CancelRedPackage();
        res = await redPackageGrain.GrabRedPackage(userId3, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageCancelled);
        await redPackageGrain.ExpireRedPackage();
        res = await redPackageGrain.GrabRedPackage(userId3, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageCancelled);

        redPackageGrain = Cluster.Client.GetGrain<ICryptoBoxGrain>(Guid.NewGuid());
        await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(Guid.NewGuid()), 8, 1, userId1,86400000);
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
            RawTransaction = "xxxxx",
            Message = "xxxx"
        };
    }

    [Fact]
    public async Task UpdateRedPackage_test()
    {
        var redPackageId =  await GetNewPackageId();
        var redPackageGrain = Cluster.Client.GetGrain<ICryptoBoxGrain>(redPackageId);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var res = await redPackageGrain.GrabRedPackage(userId1, "xxxx");
    }

    private async Task<Guid> GetNewPackageId()
    {
        var userId = Guid.NewGuid();
        var redPackageId = Guid.NewGuid();
        var redPackageGrain = Cluster.Client.GetGrain<ICryptoBoxGrain>(redPackageId);
        var res = await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(redPackageId), 8, 1, userId,86400000);
        res.Success.ShouldBe(true);
        res = await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(redPackageId), 8, 1, userId,86400000);
        res.Success.ShouldBe(false);
        return redPackageId;
    }
}