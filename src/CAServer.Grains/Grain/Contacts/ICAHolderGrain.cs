using Orleans;

namespace CAServer.Grains.Grain.Contacts;

public interface ICAHolderGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<CAHolderGrainDto>> AddHolderAsync(CAHolderGrainDto caHolderDto);
    Task<GrainResultDto<CAHolderGrainDto>> UpdateNicknameAsync(string nickname);
}