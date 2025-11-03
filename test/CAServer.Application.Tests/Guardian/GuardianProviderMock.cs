using System.Collections.Generic;
using AElf;
using AElf.Types;
using CAServer.Common;
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
                // CaHolderInfo = new List<Provider.GuardianDto>()
                // {
                //     new Provider.GuardianDto()
                //     {
                //         OriginChainId = "TEST",
                //         CaAddress = "",
                //         CaHash = "",
                //         GuardianList = new GuardianBaseListDto()
                //         {
                //             Guardians = new List<GuardianInfoBase>
                //                 { new GuardianDto { IdentifierHash = _identifierHash } }
                //         }
                //     }
                // }
            });

        provider.Setup(t => t.GetHolderInfoFromContractAsync(It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new GetHolderInfoOutput()
        {
            GuardianList = new GuardianList()
            {
                Guardians =
                {
                    new List<Portkey.Contracts.CA.Guardian>()
                    {
                        new Portkey.Contracts.CA.Guardian()
                        {
                            IdentifierHash = Hash.LoadFromHex(_identifierHash),
                            VerifierId = HashHelper.ComputeFrom("123"),
                        }
                    }
                }
            }
        });

        return provider.Object;
    }
    
    public IContractProvider GetContractProviderMock()
    {
        var provider = new Mock<IContractProvider>();

        provider.Setup(t => t.GetVerifierServersListAsync(It.IsAny<string>()))
            .ReturnsAsync(new GetVerifierServersOutput()
            {
                VerifierServers =
                {
                   new Portkey.Contracts.CA.VerifierServer
                   {
                       Id = HashHelper.ComputeFrom("123"),
                       ImageUrl= "http://localhost:8000",
                       Name = "MockName"
                   } 
                }
                
            });

        provider.Setup(t => t.GetHolderInfoAsync(It.IsAny<Hash>(), It.IsAny<Hash>(), It.IsAny<string>()))
            .ReturnsAsync(new GetHolderInfoOutput
            {
                GuardianList =  new GuardianList()
                {
                    Guardians =
                    {
                        new List<Portkey.Contracts.CA.Guardian>()
                        {
                            new Portkey.Contracts.CA.Guardian()
                            {
                                IdentifierHash = Hash.LoadFromHex(_identifierHash),
                                VerifierId = HashHelper.ComputeFrom("123"),
                                
                            }
                        }
                    }
                },
                CreateChainId = 9992731
            });

        return provider.Object;
    }
    
}