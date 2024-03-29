using CAServer.CAAccount.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.Contacts;

public interface ICAHolderGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<CAHolderGrainDto>> AddHolderAsync(CAHolderGrainDto caHolderDto);
    Task<GrainResultDto<CAHolderGrainDto>> UpdateNicknameAsync(string nickname);
    Task<GrainResultDto<CAHolderGrainDto>> DeleteAsync();
    Task<string> GetCAHashAsync();
    Task<GrainResultDto<CAHolderGrainDto>> GetCaHolder();
    Task<GrainResultDto<CAHolderGrainDto>> UpdateHolderInfo(HolderInfoDto holderInfo);
}