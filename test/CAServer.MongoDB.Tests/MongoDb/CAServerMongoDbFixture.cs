using System;
using Microsoft.Extensions.Logging.Abstractions;
using Mongo2Go;

namespace CAServer.MongoDB;

public class CAServerMongoDbFixture : IDisposable
{
    private static readonly MongoDbRunner MongoDbRunner;
    public static readonly string ConnectionString;
    
    static CAServerMongoDbFixture()
    {
        MongoDbRunner = MongoDbRunner.Start(singleNodeReplSet: true, singleNodeReplSetWaitTimeout: 10, logger: NullLogger.Instance);
        ConnectionString = MongoDbRunner.ConnectionString;
    }

    public void Dispose()
    {
        MongoDbRunner?.Dispose();
    }
}
