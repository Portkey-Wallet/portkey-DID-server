using CAServer.Grains.Grain.CrossChain;
using CAServer.Grains.State.CrossChain;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.CrossChain;

[Collection(ClusterCollection.Name)]
public class CrossChainTransferGrainTests : CAServerGrainTestBase
{
    [Fact]
    public async Task CrossChainTransferTest()
    {
        var grain = Cluster.Client.GetGrain<ICrossChainTransferGrain>("AELF");
        
        var unFinished = await grain.GetUnFinishedTransfersAsync();
        unFinished.Data.Count.ShouldBe(0);

        var lastedProcessedHeight = await grain.GetLastedProcessedHeightAsync();
        lastedProcessedHeight.Data.ShouldBe(0);

        var transfer = new CrossChainTransferDto
        {
            Id = "txhash",
            Status = CrossChainStatus.Indexing,
            FromChainId = "AELF",
            ToChainId = "tDVV",
            TransferTransactionId = "TransferTransactionId",
            TransferTransactionHeight = 100,
            TransferTransactionBlockHash = "TransferTransactionBlockHash"
        };
        await grain.AddTransfersAsync(90,new List<CrossChainTransferDto>());
        
        unFinished = await grain.GetUnFinishedTransfersAsync();
        unFinished.Data.Count.ShouldBe(0);

        lastedProcessedHeight = await grain.GetLastedProcessedHeightAsync();
        lastedProcessedHeight.Data.ShouldBe(90);
        
        await grain.AddTransfersAsync(100, new List<CrossChainTransferDto> { transfer });
        
        unFinished = await grain.GetUnFinishedTransfersAsync();
        unFinished.Data.Count.ShouldBe(1);
        unFinished.Data[0].Id.ShouldBe(transfer.Id);
        unFinished.Data[0].Status.ShouldBe(transfer.Status);
        unFinished.Data[0].FromChainId.ShouldBe(transfer.FromChainId);
        unFinished.Data[0].ToChainId.ShouldBe(transfer.ToChainId);
        unFinished.Data[0].TransferTransactionId.ShouldBe(transfer.TransferTransactionId);
        unFinished.Data[0].TransferTransactionHeight.ShouldBe(transfer.TransferTransactionHeight);
        unFinished.Data[0].TransferTransactionBlockHash.ShouldBe(transfer.TransferTransactionBlockHash);
        
        lastedProcessedHeight = await grain.GetLastedProcessedHeightAsync();
        lastedProcessedHeight.Data.ShouldBe(100);

        transfer.Status = CrossChainStatus.Receiving;
        await grain.UpdateTransferAsync(transfer);
        
        unFinished = await grain.GetUnFinishedTransfersAsync();
        unFinished.Data.Count.ShouldBe(1);
        
        transfer.Status = CrossChainStatus.Received;
        await grain.UpdateTransferAsync(transfer);
        
        unFinished = await grain.GetUnFinishedTransfersAsync();
        unFinished.Data.Count.ShouldBe(1);
        
        transfer.Status = CrossChainStatus.Confirmed;
        await grain.UpdateTransferAsync(transfer);
        
        unFinished = await grain.GetUnFinishedTransfersAsync();
        unFinished.Data.Count.ShouldBe(0);
    }
}