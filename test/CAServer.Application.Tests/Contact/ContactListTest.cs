using System;
using System.Threading.Tasks;
using CAServer.Contacts;
using CAServer.Grains.Grain.Contacts;
using CAServer.Security;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Contact;


[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ContactListTest : CAServerApplicationTestBase
{
    private const string DefaultFilter = "modificationTime: [2023-08-28T06:38:53.594Z TO 2023-08-29T03:08:17.947Z]";
    
    private readonly IContactAppService _contactAppService;

    public ContactListTest()
    {
        _contactAppService = GetRequiredService<IContactAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(ContactListMock.BuildMockIContactAppService());
    }
    
    [Fact]
    public async Task GetList_Test()
    {
        var pagedResultDto = await _contactAppService.GetListAsync(new ContactGetListDto
        {
            Filter = DefaultFilter
        });
        
        pagedResultDto.TotalCount.ShouldBe(2);
        pagedResultDto.Items[0].Name.ShouldBe("Test1");
    }
}