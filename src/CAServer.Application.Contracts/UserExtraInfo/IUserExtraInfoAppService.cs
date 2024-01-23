using System.Threading.Tasks;
using CAServer.UserExtraInfo.Dtos;

namespace CAServer.UserExtraInfo;

public interface IUserExtraInfoAppService
{
    Task<AddAppleUserExtraInfoResultDto> AddAppleUserExtraInfoAsync(AddAppleUserExtraInfoDto extraInfoDto);
    Task<UserExtraInfoResultDto> GetUserExtraInfoAsync(string id);
    Task<UserExtraInfoResultDto> AddUserExtraInfoAsync(Verifier.Dtos.UserExtraInfo userExtraInfo);
}