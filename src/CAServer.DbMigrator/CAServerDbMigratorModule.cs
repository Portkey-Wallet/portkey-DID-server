using CAServer.MongoDB;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace CAServer.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(CAServerMongoDbModule),
    typeof(CAServerApplicationContractsModule)
    )]
public class CAServerDbMigratorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
    }
}
