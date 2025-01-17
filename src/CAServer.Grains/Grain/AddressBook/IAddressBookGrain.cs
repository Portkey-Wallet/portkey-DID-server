namespace CAServer.Grains.Grain.AddressBook;

public interface IAddressBookGrain: IGrainWithGuidKey
{
    Task<GrainResultDto<AddressBookGrainDto>> AddContactAsync(AddressBookGrainDto addressBookDto);
    Task<GrainResultDto<AddressBookGrainDto>> UpdateContactAsync(AddressBookGrainDto contactDto);
    Task<GrainResultDto<AddressBookGrainDto>> DeleteContactAsync(Guid userId);
    Task<GrainResultDto<AddressBookGrainDto>> GetContactAsync();
    Task<GrainResultDto<AddressBookGrainDto>> UpdateContactInfo(string walletName, string avatar);
}