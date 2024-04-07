using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.IpInfo;

public interface IIpInfoAppService
{
    Task<IpInfoResultDto> GetIpInfoAsync();
    Task UpdateRepairScore();
    Task<List<UpdateXpScoreRepairDataDto>> GetIpInfo2Async();
}