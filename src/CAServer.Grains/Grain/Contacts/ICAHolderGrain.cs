using CAServer.CAAccount.Dtos;

namespace CAServer.Grains.Grain.Contacts;

public interface ICAHolderGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<CAHolderGrainDto>> AddHolderAsync(CAHolderGrainDto caHolderDto);
    Task<GrainResultDto<CAHolderGrainDto>> AddHolderWithAvatarAsync(CAHolderGrainDto caHolderGrainDto);
    Task<GrainResultDto<CAHolderGrainDto>> UpdateNicknameAsync(string nickname);
    Task<GrainResultDto<CAHolderGrainDto>> UpdateNicknameAndMarkBitAsync(string nickname, bool modifiedNickname, string identifierHash);
    Task UpdatePopUpAsync(bool poppedUp);
    Task<GrainResultDto<CAHolderGrainDto>> DeleteAsync();
    Task<string> GetCAHashAsync();
    Task<GrainResultDto<CAHolderGrainDto>> GetCaHolder();
    Task<GrainResultDto<CAHolderGrainDto>> UpdateHolderInfo(HolderInfoDto holderInfo);

    Task<GrainResultDto<CAHolderGrainDto>> AppendOrUpdateSecondaryEmailAsync(string secondaryEmail);
}