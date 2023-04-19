using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Contacts;
using CAServer.Grains.Grain.Contacts;
using CAServer.Security;
using Shouldly;
using Volo.Abp.Users;
using Volo.Abp.Validation;
using Xunit;

namespace CAServer.Contact;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ContactTest : CAServerApplicationTestBase
{
    private const string DefaultName = "Tom";
    public List<ContactAddressDto> Addresses = new List<ContactAddressDto>();
    private const string DefaultChainId = "DefaultChainId";
    private const string DefaultAddress = "DefaultAddress";

    private readonly IContactAppService _contactAppService;
    private ICurrentUser _currentUser;

    public ContactTest()
    {
        _contactAppService = GetRequiredService<IContactAppService>();
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
    }

    [Fact]
    public async Task Contact_PipeLine_Success_Test()
    {
        Addresses.Add(new ContactAddressDto
        {
            ChainId = DefaultChainId,
            Address = DefaultAddress
        });

        var dto = new CreateUpdateContactDto
        {
            Name = DefaultName,
            Addresses = Addresses
        };

        //create
        var createResult = await _contactAppService.CreateAsync(dto);

        createResult.ShouldNotBeNull();
        createResult.Name.ShouldBe(DefaultName);

        //update
        var newName = "newName";
        dto.Name = newName;
        var updateResult = await _contactAppService.UpdateAsync(createResult.Id, dto);

        updateResult.ShouldNotBeNull();
        updateResult.Name.ShouldBe(newName);

        //getExist
        var exitResult = await _contactAppService.GetExistAsync(newName);
        exitResult.ShouldNotBeNull();
        exitResult.Existed.ShouldBeTrue();
        updateResult.Name.ShouldBe(newName);

        //delete
        await _contactAppService.DeleteAsync(createResult.Id);
    }

    [Fact]
    public async Task Create_Twice_Test()
    {
        try
        {
            Addresses.Add(new ContactAddressDto
            {
                ChainId = DefaultChainId,
                Address = DefaultAddress
            });

            var dto = new CreateUpdateContactDto
            {
                Name = DefaultName,
                Addresses = Addresses
            };

            await _contactAppService.CreateAsync(dto);
            await _contactAppService.CreateAsync(dto);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ContactMessage.ExistedMessage);
        }
    }

    [Fact]
    public async Task Update_Not_Exist_Test()
    {
        try
        {
            Addresses.Add(new ContactAddressDto
            {
                ChainId = DefaultChainId,
                Address = DefaultAddress
            });

            var dto = new CreateUpdateContactDto
            {
                Name = DefaultName,
                Addresses = Addresses
            };

            await _contactAppService.UpdateAsync(Guid.Empty, dto);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ContactMessage.NotExistMessage);
        }
    }
    
    [Fact]
    public async Task Get_Not_Exist_Test()
    {
        try
        {
            await _contactAppService.GetExistAsync(string.Empty);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ContactMessage.NotExistMessage);
        }
    }
    
    [Fact]
    public async Task Delete_Not_Exist_Test()
    {
        try
        {
            await _contactAppService.DeleteAsync(Guid.Empty);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ContactMessage.NotExistMessage);
        }
    }


    [Fact]
    public async Task CreateOrUpdate_Body_Empty_Test()
    {
        try
        {
            await _contactAppService.CreateAsync(new CreateUpdateContactDto
            {
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_Name_NullOrEmpty_Test()
    {
        try
        {
            Addresses.Add(new ContactAddressDto
            {
                ChainId = DefaultChainId,
                Address = DefaultAddress
            });

            await _contactAppService.CreateAsync(new CreateUpdateContactDto
            {
                Name = "",
                Addresses = Addresses
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_Addresses_Null_Test()
    {
        try
        {
            await _contactAppService.CreateAsync(new CreateUpdateContactDto
            {
                Name = DefaultName,
                Addresses = null
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_ChainId_NullOrEmpty_Test()
    {
        try
        {
            Addresses.Add(new ContactAddressDto
            {
                ChainId = "",
                Address = DefaultAddress
            });

            await _contactAppService.CreateAsync(new CreateUpdateContactDto
            {
                Name = DefaultName,
                Addresses = Addresses
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_Address_NullOrEmpty_Test()
    {
        try
        {
            Addresses.Add(new ContactAddressDto
            {
                ChainId = DefaultChainId,
                Address = ""
            });

            await _contactAppService.CreateAsync(new CreateUpdateContactDto
            {
                Name = DefaultName,
                Addresses = Addresses
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }
}