using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Contacts;
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

    public ContactTest()
    {
        _contactAppService = GetService<IContactAppService>();
    }

    [Fact]
    public async Task CreateOrUpdate_Success_Test()
    {
        Addresses.Add(new ContactAddressDto
        {
            ChainId = DefaultChainId,
            Address = DefaultAddress
        });

        await _contactAppService.CreateAsync(new CreateUpdateContactDto
        {
            Name = DefaultName,
            Addresses = Addresses
        });
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
            Assert.True(e is NullReferenceException);
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