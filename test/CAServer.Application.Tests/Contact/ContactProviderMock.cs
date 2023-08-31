using System;
using System.Collections.Generic;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using Moq;
using Nest;
using Org.BouncyCastle.Bcpg;

namespace CAServer.Contact;

public partial class ContactTest
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
            .ReturnsAsync((Guid UserId, List<string> addresses) => new List<ContactIndex>()
            {
                new ContactIndex()
                {
                    Name = "test",
                    Addresses = new List<ContactAddress>(),
                    UserId = Guid.NewGuid(),
                    ModificationTime = DateTime.UtcNow
                }
            });

        provider.Setup(t => t.GetImputationAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        // provider.Setup(t => t.GetContactAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
        //     .ReturnsAsync((Guid userId, Guid contactUserId) => new ContactIndex()
        //     {
        //         UserId = userId,
        //     });

        return provider.Object;
    }
}