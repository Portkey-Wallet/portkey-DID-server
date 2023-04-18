using System.Threading.Tasks;

namespace CAServer.Data;

public interface ICAServerDbSchemaMigrator
{
    Task MigrateAsync();
}
