using System;
using System.Threading.Tasks;
using CAServer.Dtos;

namespace CAServer.CAAccount;

public interface INickNameAppService
{
    Task<CAHolderResultDto> SetNicknameAsync(UpdateNickNameDto nickNameDto);
}