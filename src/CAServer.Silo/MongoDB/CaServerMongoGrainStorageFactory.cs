using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Storage;

namespace CAServer.Silo.MongoDB;

public static class CaServerMongoGrainStorageFactory
{
    public static IGrainStorage Create(IServiceProvider services, string name)
    {
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<MongoDBGrainStorageOptions>>();
        return ActivatorUtilities.CreateInstance<CaServerMongoGrainStorage>(services, optionsMonitor.Get(name));
    }
}