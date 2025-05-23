using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;

namespace CAServer.Silo.MongoDB;

public class CaServerMongoDBGrainStorageConfigurator : IPostConfigureOptions<MongoDBGrainStorageOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public CaServerMongoDBGrainStorageConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void PostConfigure(string name, MongoDBGrainStorageOptions options)
    {
        if (options.GrainStateSerializer == default)
        {
            // First, try to get a IGrainStateSerializer that was registered with the same name as the State provider
            // If none is found, fallback to system wide default
            options.GrainStateSerializer = _serviceProvider.GetKeyedService<IGrainStateSerializer>(name) ??
                                           _serviceProvider.GetRequiredService<IGrainStateSerializer>();
        }
    }
}