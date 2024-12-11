using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Providers.MongoDB.StorageProviders;
using Orleans.Providers.MongoDB.Utils;

namespace CAServer.Silo.MongoDB;

public class CaServerMongoGrainStorage : MongoGrainStorage
{
    private readonly GrainCollectionNameOptions _grainCollectionNameOptions;
    
    public CaServerMongoGrainStorage(IMongoClientFactory mongoClientFactory, ILogger<MongoGrainStorage> logger,
        MongoDBGrainStorageOptions options, IOptionsSnapshot<GrainCollectionNameOptions> grainCollectionNameOptions)
        : base(mongoClientFactory, logger, options)
    {
        _grainCollectionNameOptions = grainCollectionNameOptions.Value;
    }

    protected override string ReturnGrainName<T>(string stateName, Orleans.Runtime.GrainId grainId)
    {
        return _grainCollectionNameOptions.GrainSpecificCollectionName.TryGetValue(typeof(T).FullName,
            out var grainName)
            ? grainName
            : base.ReturnGrainName<T>(stateName, grainId);
    }
}