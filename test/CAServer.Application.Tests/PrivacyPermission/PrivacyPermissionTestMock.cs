using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CAServer.Common;
using CAServer.Contacts;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.PrivacyPermission;
using CAServer.Guardian;
using CAServer.Guardian.Provider;

using CAServer.PrivacyPermission.Dtos;
using CAServer.Security;
using CAServer.UserAssets.Provider;
using CAServer.UserExtraInfo;
using CAServer.UserExtraInfo.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using Nest;
using NSubstitute;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp.Users;
using Xunit;
using GuardianDto = CAServer.Guardian.Provider.GuardianDto;
using Guid = System.Guid;

namespace CAServer.PrivacyPermission;

public partial class PrivacyPermissionTest : CAServerApplicationTestBase
{
    public Guid UserId = Guid.NewGuid();
    public Guid UserIdNullOriginChainId = Guid.NewGuid();
    public Guid UserIdGardianNull = Guid.NewGuid();
    public Guid UserIdLoginGardianNull = Guid.NewGuid();
    public Guid CaHolderIndexId = Guid.NewGuid();
    public string GoogleIdentifier = "googleIdentifier";
    public string AppleIdentifier = "appleIdentifier";
    protected ICurrentUser _currentUser;
    
    public PrivacyPermissionTest()
    {
        _privacyPermissionAppService = GetRequiredService<IPrivacyPermissionAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetIUserAssetsProvider());
        services.AddSingleton(GetIGuardianProvider());
        services.AddSingleton(GetIGuardianAppService());
        services.AddSingleton(GetIUserExtraInfoAppService());
        services.AddSingleton(GetIContactProvider());
        services.AddSingleton(GetIContractProvider());
        services.AddSingleton(GetIContactAppService());
        services.AddSingleton(GetIPrivacyPermissionGrain());
        //services.AddSingleton(GetIClusterClient());
    }
    
    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    private IUserAssetsProvider GetIUserAssetsProvider()
    {
        var userAssetsProviderMock = new Mock<IUserAssetsProvider>();
        userAssetsProviderMock.Setup(x => x.GetCaHolderIndexByCahashAsync(It.IsAny<string>()))
            .ReturnsAsync((string input) =>
            {
                if(input == "caHashExist")
                {
                    return new CAHolderIndex
                    {
                        CaHash = "caHashExist",
                        Id = CaHolderIndexId,
                        IsDeleted = false,
                        CreateTime = DateTime.Now,
                        UserId = UserId
                    };
                }
                else
                {
                    return null;
                }
            });


        userAssetsProviderMock.Setup(x => x.GetCaHolderIndexAsync(It.IsAny<Guid>())).ReturnsAsync((Guid input) =>
        {
            if (input == UserId)
            {
                return new CAHolderIndex
                {
                    CaHash = "caHashExist",
                    Id = CaHolderIndexId,
                    IsDeleted = false,
                    CreateTime = DateTime.Now,
                    UserId = UserId
                };
            }

            if (input == UserIdNullOriginChainId)
            {
                return new CAHolderIndex
                {
                    CaHash = "caHashExistNullOriginChainId",
                    Id = CaHolderIndexId,
                    IsDeleted = false,
                    CreateTime = DateTime.Now,
                    UserId = UserIdNullOriginChainId
                };
            }

            if (input == UserIdGardianNull)
            {
                return new CAHolderIndex
                {
                    CaHash = "caHashGardianNull",
                    Id = CaHolderIndexId,
                    IsDeleted = false,
                    CreateTime = DateTime.Now,
                    UserId = UserIdGardianNull
                };
            }

            if (input == UserIdLoginGardianNull)
            {
                return new CAHolderIndex
                {
                    CaHash = "caHashLoginGardianNull",
                    Id = CaHolderIndexId,
                    IsDeleted = false,
                    CreateTime = DateTime.Now,
                    UserId = UserIdLoginGardianNull
                };
            }

            return null;
        });
        return userAssetsProviderMock.Object;
    }

    private IGuardianProvider GetIGuardianProvider()
    {
        var guardianProviderMock = new Mock<IGuardianProvider>();
        guardianProviderMock.Setup(x => x.GetGuardiansAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string loginGuardianIdentifierHash, string caHash) =>
            {
                if (caHash == "caHashExistNull")
                {
                    return new GuardiansDto()
                    {
                        CaHolderInfo = new List<GuardianDto>()
                    };
                }

                if (caHash == "caHashExistNullOriginChainId")
                {
                    return new GuardiansDto()
                    {
                        CaHolderInfo = new List<GuardianDto>()
                        {
                            new GuardianDto()
                            {
                                OriginChainId = ""
                            }
                        }
                    };
                }

                if (caHash == "caHashGardianNull")
                {
                    return new GuardiansDto()
                    {
                        CaHolderInfo = new List<GuardianDto>()
                        {
                            new GuardianDto()
                            {
                                OriginChainId = "AELF"
                            }
                        }
                    };
                }

                if (caHash == "caHashLoginGardianNull")
                {
                    return new GuardiansDto()
                    {
                        CaHolderInfo = new List<GuardianDto>()
                        {
                            new GuardianDto()
                            {
                                OriginChainId = "AELF",
                                GuardianList = new GuardianBaseListDto()
                                {
                                    Guardians = new List<GuardianInfoBase>()
                                    {
                                    }
                                }
                            }
                        }
                    };
                }
                
                if (caHash == "caHashExist")
                {
                    return new GuardiansDto()
                    {
                        CaHolderInfo = new List<GuardianDto>()
                        {
                            new GuardianDto()
                            {
                                OriginChainId = "AELF",
                                GuardianList = new GuardianBaseListDto()
                                {
                                    Guardians = new List<GuardianInfoBase>()
                                    {
                                        new GuardianInfoBase()
                                        {
                                            IdentifierHash = "identifierHashExist",
                                            GuardianIdentifier = "guardianIdentifierExist",
                                            IsLoginGuardian = true,
                                            Type = "typeExist"
                                        }
                                    }
                                }
                            }
                        }
                    };
                }
                
                return new GuardiansDto();
            });
        return guardianProviderMock.Object;
    }

    private IGuardianAppService GetIGuardianAppService()
    {
        var guardianAppServiceMock = new Mock<IGuardianAppService>();
        guardianAppServiceMock.Setup(x => x.GetGuardianListAsync(It.IsAny<List<string>>()))
            .ReturnsAsync((List<string> input) =>
            {
                if (input != null && input.Contains("identifierHashExist"))
                {
                    return new List<GuardianIndexDto> { new GuardianIndexDto
                    {
                        IdentifierHash = "identifierHashExist",
                        Identifier = "IdentifierExist",
                    } };
                }
                else
                {
                    return new List<GuardianIndexDto>();
                }
            });
        return guardianAppServiceMock.Object;
    }

    private IUserExtraInfoAppService GetIUserExtraInfoAppService()
    {
        var userExtraInfoAppServiceMock = new Mock<IUserExtraInfoAppService>();
        userExtraInfoAppServiceMock.Setup(x => x.GetUserExtraInfoAsync(It.IsAny<string>()))
            .ReturnsAsync((string input) =>
            {
                if (input == GoogleIdentifier)
                {
                    return new UserExtraInfoResultDto()
                    {
                        Email = "aaa@google.com",
                        GuardianType = "Google",
                        IsPrivate = true
                    };
                }
                
                if (input == AppleIdentifier)
                {
                    return new UserExtraInfoResultDto()
                    {
                        Email = "aaa@apple.com",
                        GuardianType = "Apple",
                        IsPrivate = false,
                        VerifiedEmail = true
                    };
                }

                return new UserExtraInfoResultDto();
            });
        
        
        return userExtraInfoAppServiceMock.Object;
    }
    
    private IContactProvider GetIContactProvider()
    {
        var contactProviderMock = new Mock<IContactProvider>();
        contactProviderMock.Setup(x => x.GetContactsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ContactIndex>());
        return contactProviderMock.Object;
    }
    
    private IContractProvider GetIContractProvider()
    {
        var contractProviderMock = new Mock<IContractProvider>();
        return contractProviderMock.Object;
    }
    
    private IContactAppService GetIContactAppService()
    {
        var contactAppServiceMock = new Mock<IContactAppService>();
        return contactAppServiceMock.Object;
    }
    
    private IPrivacyPermissionGrain GetIPrivacyPermissionGrain()
    {
        var privacyPermissionGrainMock = new Mock<IPrivacyPermissionGrain>();
        privacyPermissionGrainMock.Setup(x => x.GetPrivacyPermissionAsync())
            .ReturnsAsync(new PrivacyPermissionDto());
        return privacyPermissionGrainMock.Object;
    }
    
    private IClusterClient GetIClusterClient()
    {
        var clusterClientMock = new Mock<IClusterClient>();
        clusterClientMock.Setup(x => x.GetGrain<IPrivacyPermissionGrain>( It.IsAny<Guid>(), null))
            .Returns(GetRequiredService<IPrivacyPermissionGrain>());
        return clusterClientMock.Object;
    }
}
