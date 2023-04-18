using Volo.Abp.Application.Dtos;

namespace CAServer.Contacts;

public class GetContractsDto : PagedAndSortedResultRequestDto
{
    public bool ContainDeleted { get; set; }
    public long MinModificationTime { get; set; }
}