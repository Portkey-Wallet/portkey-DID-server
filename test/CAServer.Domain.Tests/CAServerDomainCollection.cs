using CAServer.MongoDB;
using Xunit;

namespace CAServer;

[CollectionDefinition(CAServerTestConsts.CollectionDefinitionName)]
public class CAServerDomainCollection: CAServerMongoDbCollectionFixtureBase
{

}
