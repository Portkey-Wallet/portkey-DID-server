using System.Threading.Tasks;
using Volo.Abp;

namespace CAServer.ZkLogin;

[RemoteService(false)]
public class AetherlinkService : CAServerAppService
{
    public async Task CreateSubscriptionToAetherlink()
    {
    }
}