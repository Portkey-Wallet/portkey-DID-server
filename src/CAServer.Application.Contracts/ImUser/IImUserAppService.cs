using System;
using System.Threading.Tasks;
using CAServer.ImUser.Dto;

namespace CAServer.ImUser;

public interface IImUserAppService
{
    Task<HolderInfoResultDto> GetHolderInfoAsync(Guid userId);
}