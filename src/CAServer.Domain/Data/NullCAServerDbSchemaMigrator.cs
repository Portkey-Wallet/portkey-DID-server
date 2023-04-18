using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace CAServer.Data;

/* This is used if database provider does't define
 * ICAServerDbSchemaMigrator implementation.
 */
public class NullCAServerDbSchemaMigrator : ICAServerDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
