using Orleans;

namespace CAServer.Grains.Grain.Contacts;

public interface IContactGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<ContactGrainDto>> AddContactAsync(Guid userId, ContactGrainDto contactDto);
    Task<GrainResultDto<ContactGrainDto>> UpdateContactAsync(Guid userId, ContactGrainDto contactDto);
    Task<GrainResultDto<ContactGrainDto>> DeleteContactAsync(Guid userId);
}