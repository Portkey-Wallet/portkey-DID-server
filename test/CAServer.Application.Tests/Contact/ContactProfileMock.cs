using System;
using System.Collections.Generic;
using System.Linq;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using Moq;
using Nest;
using Org.BouncyCastle.Bcpg;

namespace CAServer.Contact;

public partial class ContactProfileTest
{
    private IContactProvider GetMockContactProvider()
    {
        var provider = new Mock<IContactProvider>();

        provider.Setup(t => t.GetCaHolderInfoAsync(It.IsAny<List<string>>(), It.IsAny<string>(), 0, 10))
            .ReturnsAsync(new GuardiansDto()
            {
                CaHolderInfo = new List<GuardianDto>()
                {
                    new GuardianDto()
                    {
                        CaAddress = "test",
                        CaHash = "test",
                        ChainId = "AELF"
                    }
                }
            });

        provider.Setup(t => t.GetCaHolderAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync((Guid userId, string caHash) => new CAHolderIndex()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CaHash = caHash,
                CreateTime = DateTime.UtcNow,
                NickName = "test"
            });

        provider.Setup(t => t.GetContactByAddressesAsync(It.IsAny<Guid>(), It.IsAny<List<string>>()))
            .ReturnsAsync((Guid userId, List<string> addresses) => new List<ContactIndex>()
            {
                new ContactIndex()
                {
                    Id = Guid.Parse(addresses.First()),
                    Name = "test",
                    Addresses = new List<ContactAddress>(),
                    UserId = Guid.NewGuid(),
                    ModificationTime = DateTime.UtcNow
                }
            });

        provider.Setup(t => t.GetImputationAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        provider.Setup(t => t.GetContactsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<ContactIndex>()
            {
                new()
                {
                    Name = "test",
                    CaHolderInfo = new CaHolderInfo()
                    {
                        UserId = Guid.Empty,
                        WalletName = "test"
                    },
                    ImInfo = new ImInfo()
                    {
                        PortkeyId = Guid.NewGuid().ToString(),
                        Name = "test"
                    }
                },
                new()
                {
                    Name = "",
                    CaHolderInfo = new CaHolderInfo()
                    {
                        UserId = Guid.Empty,
                        WalletName = "test"
                    },
                    ImInfo = new ImInfo()
                    {
                        PortkeyId = Guid.NewGuid().ToString(),
                        Name = "test"
                    }
                }
            });

        // provider.Setup(t => t.GetContactAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
        //     .ReturnsAsync((Guid userId, Guid contactUserId) => new ContactIndex()
        //     {
        //         UserId = userId,
        //     });

        provider.Setup(t => t.GetCaHoldersAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new List<CAHolderIndex>()
            {
                new()
                {
                    Id = Guid.Empty,
                    CaHash = "test",
                    NickName = "test"
                }
            });
        
        provider.Setup(t => t.GetContactListAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<ContactIndex>()
            {
                new()
                {
                    Id = new Guid()
                }
            });

        provider.Setup(t => t.GetContactByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new ContactIndex()
        {
                Name = "Name"
        });

        return provider.Object;
    }
}