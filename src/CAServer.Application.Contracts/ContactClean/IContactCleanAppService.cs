using System;
using System.Threading.Tasks;

namespace CAServer.ContactClean;

public interface IContactCleanAppService
{
    Task<string> ContactCleanAsync(Guid userId);
    Task<int> ContactCleanAsync();
}