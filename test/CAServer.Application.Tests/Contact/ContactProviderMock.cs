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

        return provider.Object;
    }
}