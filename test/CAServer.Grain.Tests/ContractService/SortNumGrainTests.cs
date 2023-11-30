using CAServer.Grains.Grain.ApplicationHandler;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.ContractService;

public class SortNumGrainTests : CAServerGrainTestBase
{
   

 

    [Fact]
    public async Task SyncTransactionAsyncTests()
    {
        var grain = Cluster.Client.GetGrain<ISortNumGrain>("qweew");
        for (int i=0;i < 10 ;i++) {
          var num=   await grain.GetSortNum(5);
          num.ShouldBeLessThan(5);
        }
    }
}