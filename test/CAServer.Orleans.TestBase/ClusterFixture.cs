using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.DependencyInjection;

namespace CAServer.Orleans.TestBase;

public class ClusterFixture:IDisposable,ISingletonDependency
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    public TestCluster Cluster { get; private set; }
    
    
    
    private class TestSiloConfigurations : ISiloBuilderConfigurator
    {
        public void Configure(ISiloHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
                {
                    // services.AddSingleton<ITokenGrain, TokenGrain>();
                    // services.AddSingleton<ITokenPriceProviderGrain, TokenPriceProviderGrain>();
                    // services.AddSingleton<IRequestLimitProvider, RequestLimitProvider>();
                })
                // .AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                // {
                //     options.DataConnectionString = "localhost:6379"; // This is the deafult
                //     options.UseJson = true;
                //     options.DatabaseNumber = 0;
                // }))
                .AddSimpleMessageStreamProvider(CAServerApplicationConsts.MessageStreamName)
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault();
            
        }
    }
    
    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder
            .AddSimpleMessageStreamProvider(CAServerApplicationConsts.MessageStreamName);
    }
}