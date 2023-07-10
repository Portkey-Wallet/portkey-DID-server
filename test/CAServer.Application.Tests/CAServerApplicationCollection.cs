using CAServer.MongoDB;
using Xunit;

namespace CAServer;

[CollectionDefinition(CAServerTestConsts.CollectionDefinitionName)]
public class CAServerApplicationCollection
{
    public const string CollectionDefinitionName = "CAServer collection";
}
