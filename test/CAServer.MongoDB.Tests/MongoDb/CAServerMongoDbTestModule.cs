using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Volo.Abp.Modularity;

namespace CAServer.MongoDB;

[DependsOn(
    typeof(CAServerTestBaseModule),
    typeof(AbpEntityFrameworkCoreSqliteModule)
    )]
public class CAServerMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpDbContextOptions>(options =>
        {
            options.PreConfigure(c =>
            {
                c.DbContextOptions.UseSqlite(CreateDatabaseAndGetConnection());
            });
        });
    }
    
    private static SqliteConnection CreateDatabaseAndGetConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        return connection;
    }
}
