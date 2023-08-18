using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace CAServer.Contacts;

public interface IContactAppService
{
    Task<ContactResultDto> CreateAsync(CreateUpdateContactDto input);
    Task<ContactResultDto> UpdateAsync(Guid id, CreateUpdateContactDto input);
    Task DeleteAsync(Guid id);
    Task<ContractExistDto> GetExistAsync(string name);
    Task<ContactResultDto> GetAsync(Guid id);
    Task<PagedResultDto<ContactResultDto>> GetListAsync(ContactGetListDto input);
    Task MergeAsync(ContactMergeDto input);
}