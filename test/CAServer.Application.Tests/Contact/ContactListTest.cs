using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Contacts;
using CAServer.Entities.Es;
using CAServer.Security;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Contact;


[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ContactListTest : CAServerApplicationTestBase
{
    private const string DefaultFilter = "modificationTime: [2023-07-28T06:38:53.594Z TO 2023-08-29T03:08:17.947Z]";
    
    private readonly IContactAppService _contactAppService;
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;
    private ICurrentUser _currentUser;
    
    public ContactListTest()
    {
        _contactAppService = GetRequiredService<IContactAppService>();
        _contactRepository = GetRequiredService<INESTRepository<ContactIndex, Guid>>();
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
    }

    [Fact]
    public async Task GetList_Test()
    {
        await MockContactListData();
        
        var pagedResultDto = await _contactAppService.GetListAsync(new ContactGetListDto
        {
            Filter = DefaultFilter
        });
        
        pagedResultDto.TotalCount.ShouldBe(1);
        pagedResultDto.Items[0].Name.ShouldBe("Test1");
    }
    
    
    [Fact]
    public async Task GetList_Filter_NullOrEmpty_Test()
    {
        await MockContactListData();
        
        var pagedResultDto = await _contactAppService.GetListAsync(new ContactGetListDto());
        
        pagedResultDto.TotalCount.ShouldBe(2);
    }

    private async Task MockContactListData()
    {
        var list = new List<ContactIndex>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _currentUser.GetId(),
                Name = "Test1",
                ModificationTime = new DateTime(2023, 8, 25, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = _currentUser.GetId(),
                Name = "Test2",
                ModificationTime = new DateTime(2023, 6, 25, 0, 0, 0, DateTimeKind.Utc)
            }
        };
        
        await _contactRepository.BulkAddOrUpdateAsync(list);
    }
}