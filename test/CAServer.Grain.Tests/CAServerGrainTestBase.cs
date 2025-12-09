using Orleans.TestingHost;
using Volo.Abp.Caching;

namespace CAServer.Grain.Tests;

public class CAServerGrainTestBase :CAServerTestBase<CAServerGrainTestModule>
{
    protected readonly TestCluster Cluster;

    public CAServerGrainTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;

    }
}