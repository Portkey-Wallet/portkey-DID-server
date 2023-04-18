using CAServer.Dtos;
using System.Threading.Tasks;

namespace CAServer.CAAccount;

public interface INickNameAppService
{
    Task<CAHolderResultDto> SetNicknameAsync(UpdateNickNameDto nickNameDto);
}