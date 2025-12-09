namespace CAServer.Grains.Grain.Contacts;

public interface IContactGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<ContactGrainDto>> AddContactAsync(Guid userId, ContactGrainDto contactDto);
    Task<GrainResultDto<ContactGrainDto>> UpdateContactAsync(Guid userId, ContactGrainDto contactDto);
    Task<GrainResultDto<ContactGrainDto>> DeleteContactAsync(Guid userId);
    Task<GrainResultDto<ContactGrainDto>> GetContactAsync();
    Task<GrainResultDto<ContactGrainDto>> ReadImputation();
    Task<GrainResultDto<ContactGrainDto>> Imputation();
    Task<GrainResultDto<ContactGrainDto>> UpdateContactInfo(string walletName, string avatar);
}