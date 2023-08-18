using System;
using System.Threading.Tasks;

namespace CAServer.Contacts;

public interface IContactAppService
{
    Task<ContactResultDto> CreateAsync(CreateUpdateContactDto input);
    Task<ContactResultDto> UpdateAsync(Guid id, CreateUpdateContactDto input);
    Task DeleteAsync(Guid id);
    Task<ContractExistDto> GetExistAsync(string name);
    Task MergeAsync(ContactMergeDto input);
    Task<ContactImputationDto> GetImputationAsync();
    Task ReadImputationAsync();
}