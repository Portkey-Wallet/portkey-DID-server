using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.ContractService;

public class SyncRecordGrainTests : CAServerGrainTestBase
{
    [Fact]
    public async Task ValidatedRecordsTests()
    {
        var grain = Cluster.Client.GetGrain<ISyncRecordGrain>("test");

        await grain.AddValidatedRecordsAsync(new List<SyncRecord>
        {
            new()
            {
                CaHash = "hash",
                BlockHeight = 1000,
                ChangeType = "type",
                NotLoginGuardian = "not",
                RetryTimes = 1,
                ValidateHeight = 1000,
                ValidateTransactionInfoDto = new TransactionInfo
                {
                    BlockNumber = 1000,
                    TransactionId = "id",
                    Transaction = new byte[]{1, 2, 3}
                }
            }
        });

        var list = await grain.GetValidatedRecordsAsync();
        list.Count.ShouldBe(1);
        list.First().BlockHeight.ShouldBe(1000);
        list.First().CaHash.ShouldBe("hash");
        list.First().ChangeType.ShouldBe("type");
        list.First().NotLoginGuardian.ShouldBe("not");
        list.First().RetryTimes.ShouldBe(1);
        list.First().ValidateHeight.ShouldBe(1000);
        list.First().ValidateTransactionInfoDto.BlockNumber.ShouldBe(1000);
        list.First().ValidateTransactionInfoDto.TransactionId.ShouldBe("id");
        list.First().ValidateTransactionInfoDto.Transaction.ShouldBe(new byte[]{1, 2, 3});

        await grain.SetValidatedRecords(new List<SyncRecord>());
        list = await grain.GetValidatedRecordsAsync();
        list.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ToBeValidatedRecordsTests()
    {
        var grain = Cluster.Client.GetGrain<ISyncRecordGrain>("test");

        await grain.AddToBeValidatedRecordsAsync(new List<SyncRecord>
        {
            new()
            {
                CaHash = "hash",
                BlockHeight = 1000,
                ChangeType = "type",
                NotLoginGuardian = "not",
                RetryTimes = 1,
                ValidateHeight = 1000,
                ValidateTransactionInfoDto = new TransactionInfo
                {
                    BlockNumber = 1000,
                    TransactionId = "id",
                    Transaction = new byte[]{1, 2, 3}
                }
            }
        });

        var list = await grain.GetToBeValidatedRecordsAsync();
        list.Count.ShouldBe(1);
        list.First().BlockHeight.ShouldBe(1000);
        list.First().CaHash.ShouldBe("hash");
        list.First().ChangeType.ShouldBe("type");
        list.First().NotLoginGuardian.ShouldBe("not");
        list.First().RetryTimes.ShouldBe(1);
        list.First().ValidateHeight.ShouldBe(1000);
        list.First().ValidateTransactionInfoDto.BlockNumber.ShouldBe(1000);
        list.First().ValidateTransactionInfoDto.TransactionId.ShouldBe("id");
        list.First().ValidateTransactionInfoDto.Transaction.ShouldBe(new byte[]{1, 2, 3});

        await grain.SetToBeValidatedRecords(new List<SyncRecord>());
        list = await grain.GetToBeValidatedRecordsAsync();
        list.Count.ShouldBe(0);
    }
}