using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;

namespace CAServer.CAAccount;

public interface INickNameAppService
{
    Task<CAHolderResultDto> SetNicknameAsync(UpdateNickNameDto nickNameDto);
    Task<CAHolderResultDto> GetCaHolderAsync();
    Task<CAHolderResultDto> UpdateHolderInfoAsync(HolderInfoDto holderInfo);
    Task<CAHolderResultDto> DeleteAsync();
    Task<bool> GetPoppedUpAccountAsync();
    Task<bool> GetBubblingAccountAsync();
    Task ReplaceUserNicknameAsync(ReplaceNicknameDto replaceNicknameDto);
    
    Task<List<CAHolderWithAddressResultDto>> QueryHolderInfosAsync(List<string> addressList);

    DefaultAvatarResponse GetDefaultAvatars();
}