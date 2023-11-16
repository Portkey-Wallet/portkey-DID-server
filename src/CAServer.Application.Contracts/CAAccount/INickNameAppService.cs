using System;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;

namespace CAServer.CAAccount;

public interface INickNameAppService
{
    Task<CAHolderResultDto> SetNicknameAsync(UpdateNickNameDto nickNameDto);
    Task<CAHolderResultDto> GetCaHolderAsync();
    Task<CAHolderResultDto> DeleteAsync();
    Task<CAHolderResultDto> UpdateHolderInfoAsync(HolderInfoDto holderInfo);
}