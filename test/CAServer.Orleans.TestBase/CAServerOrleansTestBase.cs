using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace CAServer.Orleans.TestBase;

public abstract class CAServerOrleansTestBase<TStartupModule> : CAServerTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    public CAServerOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}