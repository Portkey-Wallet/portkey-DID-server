using CAServer.MongoDB;
using Xunit;

namespace CAServer;

[CollectionDefinition(CAServerTestConsts.CollectionDefinitionName)]
public class CAServerApplicationCollection : CAServerMongoDbCollectionFixtureBase
{
    public const string CollectionDefinitionName = "CAServer collection";
}
