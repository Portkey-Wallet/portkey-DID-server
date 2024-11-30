using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ImUser.Dto;
using Volo.Abp.Application.Dtos;

namespace CAServer.Contacts;

public interface IContactAppService
{
    Task<ContactResultDto> CreateAsync(CreateUpdateContactDto input);
    Task<ContactResultDto> UpdateAsync(Guid id, CreateUpdateContactDto input);
    Task DeleteAsync(Guid id);
    Task<ContractExistDto> GetExistAsync(string name);
    Task<ContactResultDto> GetAsync(Guid id);
    Task<PagedResultDto<ContactListDto>> GetListAsync(ContactGetListDto input);
    Task<ContactImputationDto> GetImputationAsync();
    Task ReadImputationAsync(ReadImputationDto input);
    Task<ContactResultDto> GetContactAsync(Guid contactUserId);
    Task<List<GetNamesResultDto>> GetNameAsync(List<Guid> input);
    Task<List<ContactResultDto>> GetContactListAsync(ContactListRequestDto input);
    Task<List<ContactResultDto>> GetContactsByUserIdAsync(Guid userId);
    Task<ImInfoDto> GetImInfoAsync(string relationId);
    Task<ContactResultDto> GetContactsByRelationIdAsync(Guid userId, string relationId);
    Task<ContactResultDto> GetContactsByPortkeyIdAsync(Guid userId, Guid portKeyId);
}