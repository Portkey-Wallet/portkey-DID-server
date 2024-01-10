using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Growth;

[RemoteService(false), DisableAuditing]
public class GrowthAppService : CAServerAppService, IGrowthAppService
{
}