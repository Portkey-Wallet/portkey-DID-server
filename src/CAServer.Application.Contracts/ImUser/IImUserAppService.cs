using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ImUser.Dto;

namespace CAServer.ImUser;

public interface IImUserAppService
{
    Task<HolderInfoResultDto> GetHolderInfoAsync(Guid userId);
    Task<List<Guid>> ListHolderInfoAsync(string keyword);
    Task<List<HolderInfoResultDto>> GetUserInfoAsync(List<Guid> userIds);
}