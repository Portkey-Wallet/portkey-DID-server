using System.Threading.Tasks;

namespace CAServer.ContactClean.Provider;

public interface IContactCleanProvider
{
    Task SetNameAsync(string relationId, string name);
    Task FollowAndRemarkAsync(string relationId, string followRelationId, string name);
}