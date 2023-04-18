using CAServer.MongoDB;
using Volo.Abp.Modularity;

namespace CAServer;

[DependsOn(
    typeof(CAServerMongoDbTestModule)
    )]
public class CAServerDomainTestModule : AbpModule
{

}
