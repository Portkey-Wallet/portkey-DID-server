using CAServer.Grain.Tests;
using Orleans.TestingHost;

namespace CAServer;

public abstract class CAServerApplicationTestBase : CAServerTestBase<CAServerApplicationTestModule>
{
    protected readonly TestCluster Cluster;

    public CAServerApplicationTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }

    // public override void Dispose()
    // {
    //     // Cluster.Dispose();
    //     // base.Dispose();
    // }
}