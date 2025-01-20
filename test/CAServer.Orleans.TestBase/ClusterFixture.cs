using AutoMapper;
using CAServer.Grains;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;

namespace CAServer.Orleans.TestBase;

public class ClusterFixture : IDisposable, ISingletonDependency
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        // builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    public TestCluster Cluster { get; private set; }


    private class TestSiloConfigurations : ISiloConfigurator
    {
        public void Configure(ISiloBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
                {
                    // services.AddSingleton<ITokenPriceProvider, TokenPriceProvider>();
                    // services.AddSingleton<IRequestLimitProvider, RequestLimitProvider>();
                    services.AddMemoryCache();
                    services.AddDistributedMemoryCache();
                    // todo modify
                    //services.AddAutoMapper(typeof(CAServerGrainsModule).Assembly);

                    services.AddSingleton(typeof(IDistributedCache), typeof(MemoryDistributedCache));
                    // services.AddSingleton(typeof(IDistributedCache<>), typeof(MemoryDistributedCache<>));
                    services.AddSingleton(typeof(IDistributedCache<,>), typeof(DistributedCache<,>));

                    services.Configure<AbpDistributedCacheOptions>(cacheOptions =>
                    {
                        cacheOptions.GlobalCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
                    });
                    // services.AddSingleton<ICancellationTokenProvider>(NullCancellationTokenProvider.Instance);
                    // services.AddTransient(
                    //     typeof(IDistributedCacheSerializer),
                    //     typeof(Utf8JsonDistributedCacheSerializer)
                    // );
                    // services.AddTransient(
                    //     typeof(IJsonSerializer),
                    //     typeof(AbpSystemTextJsonSerializer)
                    // );
                    // services.AddTransient(
                    //     typeof(IDistributedCacheKeyNormalizer),
                    //     typeof(DistributedCacheKeyNormalizer)
                    // );
                    // services.AddTransient(
                    //     typeof(ICurrentTenantAccessor),
                    //     typeof(AsyncLocalCurrentTenantAccessor)
                    // );
                    // services.AddTransient(
                    //     typeof(ICurrentTenant),
                    //     typeof(CurrentTenant)
                    // );
                    services.OnExposing(onServiceExposingContext =>
                    {
                        //Register types for IObjectMapper<TSource, TDestination> if implements
                        // todo modify
                        // onServiceExposingContext.ExposedTypes.AddRange(
                        //     ReflectionHelper.GetImplementedGenericTypes(
                        //         onServiceExposingContext.ImplementationType,
                        //         typeof(IObjectMapper<,>)
                        //     )
                        // );
                    });
                    services.AddTransient(
                        typeof(IObjectMapper<>),
                        typeof(DefaultObjectMapper<>)
                    );
                    services.AddTransient(
                        typeof(IObjectMapper),
                        typeof(DefaultObjectMapper)
                    );
                    services.AddTransient(typeof(IAutoObjectMappingProvider),
                        typeof(AutoMapperAutoObjectMappingProvider));
                    services.AddTransient(sp => new MapperAccessor()
                    {
                        Mapper = sp.GetRequiredService<IMapper>()
                    });
                    services.AddTransient<IMapperAccessor>(provider => provider.GetRequiredService<MapperAccessor>());
                    
                    // services.Configure<CoinGeckoOptions>(o => { o.CoinIdMapping["ELF"] = "aelf"; });
                })
                // .AddSimpleMessageStreamProvider(CAServerApplicationConsts.MessageStreamName)
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault();
        }
    }

    public class MapperAccessor : IMapperAccessor
    {
        public IMapper Mapper { get; set; }
    }

    private class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder
            .AddMemoryStreams(CAServerApplicationConsts.MessageStreamName);
            // .AddSimpleMessageStreamProvider(CAServerApplicationConsts.MessageStreamName);
    }
}