using CAServer.Contacts;
using Moq;
using Volo.Abp.Application.Dtos;

namespace CAServer.Contact;

public class ContactListMock
{
    public static IContactAppService BuildMockIContactAppService()
    {
        var result = "";
        var mockIContactAppService = new Mock<IContactAppService>();
        mockIContactAppService.Setup(calc => calc.GetListAsync(It.IsAny<ContactGetListDto>()))
            .ReturnsAsync(new PagedResultDto<ContactListDto>
            {
                TotalCount = 2,
                Items = new []{
                    new ContactListDto
                    {
                        Name = "Test1"
                    },
                    new ContactListDto
                    {
                        Name = "Test2"
                    }
                }
            });

        return mockIContactAppService.Object;
    }
}