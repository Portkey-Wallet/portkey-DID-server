using System.Threading.Tasks;
using CAServer.PrivacyPolicy.Dtos;

namespace CAServer.PrivacyPolicy;

public interface IPrivacyPolicyAppService
{
    Task SignAsync(PrivacyPolicySignDto input);
}