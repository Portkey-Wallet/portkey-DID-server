using System;
using Mongo2Go;

namespace CAServer.MongoDB;

public class CAServerMongoDbFixture : IDisposable
{
    // private static readonly MongoDbRunner MongoDbRunner;
    // public static readonly string ConnectionString;
    //
    // static CAServerMongoDbFixture()
    // {
    //     MongoDbRunner = MongoDbRunner.Start(singleNodeReplSet: true, singleNodeReplSetWaitTimeout: 10);
    //     ConnectionString = MongoDbRunner.ConnectionString;
    // }

    public void Dispose()
    {
        //MongoDbRunner?.Dispose();
    }
}
