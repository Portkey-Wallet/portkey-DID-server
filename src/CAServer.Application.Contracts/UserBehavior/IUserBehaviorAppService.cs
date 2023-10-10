using System.Threading.Tasks;
using CAServer.UserBehavior.Etos;

namespace CAServer.UserBehavior;

public interface IUserBehaviorAppService
{
    Task AddUserBehaviorAsync(UserBehaviorEto userBehaviorEto);
}