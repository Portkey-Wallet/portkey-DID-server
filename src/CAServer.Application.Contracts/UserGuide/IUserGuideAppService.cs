using System;
using System.Threading.Tasks;
using CAServer.UserGuide.Dtos;

namespace CAServer.UserGuide;

public interface IUserGuideAppService 
{
    Task<UserGuideDto> ListUserGuideAsync(Guid? currentUserId);
    Task<UserGuideDto> QueryUserGuideAsync(UserGuideRequestDto input, Guid? currentUserId);
    Task<bool> FinishUserGuideAsync(UserGuideFinishRequestDto input, Guid? currentUserId);
}