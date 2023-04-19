using System.Collections.Generic;
using AElf.Types;
using CAServer.Guardian.Provider;
using Moq;
using Portkey.Contracts.CA;

namespace CAServer.Guardian;

public partial class GuardianTest
{
    public IGuardianProvider GetGuardianProviderMock()
    {
        var provider = new Mock<IGuardianProvider>();

        provider.Setup(t => t.GetGuardiansAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GuardiansDto()
            {
                CaHolderInfo = new List<Provider.GuardianDto>()
                {
                    new Provider.GuardianDto()
                    {
                        OriginChainId = "TEST",
                        CaAddress = "",
                        CaHash = "",
                        GuardianList = new GuardianBaseListDto()
                        {
                            Guardians = new List<GuardianInfoBase>
                                { new GuardianDto { IdentifierHash = _identifierHash } }
                        }
                    }
                }
            });

        provider.Setup(t => t.GetHolderInfoFromContractAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Grains.Grain.ApplicationHandler.ChainInfo>())).ReturnsAsync(new GetHolderInfoOutput()
        {
            GuardianList = new GuardianList()
            {
                Guardians =
                {
                    new List<Portkey.Contracts.CA.Guardian>()
                    {
                        new Portkey.Contracts.CA.Guardian()
                        {
                            IdentifierHash = Hash.LoadFromHex(_identifierHash)
                        }
                    }
                }
            }
        });

        return provider.Object;
    }
}