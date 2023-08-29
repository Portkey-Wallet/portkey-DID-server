using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Kernel;
using AElf.Types;
using CAServer.Common;
using CAServer.Contacts;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.Contacts;
using CAServer.Options;
using CAServer.Security;
using CAServer.Verifier;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp.Users;
using Volo.Abp.Validation;
using Xunit;
using Environment = CAServer.Options.Environment;

namespace CAServer.Contact;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class ContactTest : CAServerApplicationTestBase
{
    private const string DefaultName = "Tom";
    public List<ContactAddressDto> Addresses = new List<ContactAddressDto>();
    private const string DefaultChainId = "DefaultChainId";
    private const string DefaultAddress = "DefaultAddress";

    private readonly IContactAppService _contactAppService;
    private ICurrentUser _currentUser;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;

    public ContactTest()
    {
        _contactAppService = GetRequiredService<IContactAppService>();
        _caHolderRepository = GetRequiredService<INESTRepository<CAHolderIndex, Guid>>();

        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockContactProvider());
        services.AddSingleton(GetMockHostInfoOptions());
        services.AddSingleton(GetMockVariablesOptions());
    }

    [Fact]
    public async Task Contact_PipeLine_Success_Test()
    {
        Addresses.Add(new ContactAddressDto
        {
            ChainId = DefaultChainId,
            Address = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58(),
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
        
        // //update
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
            e.Message.ShouldBe("Holder not found");
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
                Addresses = null
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

    [Fact]
    public async Task Get_Test()
    {
        try
        {
            await _contactAppService.GetAsync(Guid.Empty);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ContactMessage.NotExistMessage);
        }
    }

    private IOptionsSnapshot<HostInfoOptions> GetMockHostInfoOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<HostInfoOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new HostInfoOptions
            {
                Environment = Environment.Development
            });
        return mockOptionsSnapshot.Object;
    }

    private IOptions<VariablesOptions> GetMockVariablesOptions()
    {
        var mockOptions = new Mock<IOptions<VariablesOptions>>();
        mockOptions.Setup(o => o.Value).Returns(
            new VariablesOptions
            {
                ImageMap = new Dictionary<string, string>()
                {
                    ["aelf"] = "aelfImage",
                    ["eth"] = "ethImage"
                }
            });
        return mockOptions.Object;
    }
}