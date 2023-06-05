using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Device;
using Moq;
using Orleans;

namespace CAServer.EntityEventHandler.Tests.CAAccount;

public partial class AccountHandlerTests
{
    private IClusterClient GetClusterClient()
    {
        var client = new Mock<IClusterClient>();
        var register = new Mock<IRegisterGrain>();
        var device = new Mock<IDeviceGrain>();
        var recovery = new Mock<IRecoveryGrain>();

        register.Setup(t => t.UpdateRegisterResultAsync(It.IsAny<CreateHolderResultGrainDto>())).ReturnsAsync(
            (CreateHolderResultGrainDto dto) =>
                new GrainResultDto<RegisterGrainDto>()
                {
                    Success = true,
                    Data = new RegisterGrainDto()
                    {
                        Id = dto.Id,
                        GrainId = dto.GrainId,
                        CaAddress = dto.CaAddress,
                        CaHash = dto.CaHash,
                        RegisterSuccess = dto.RegisterSuccess,
                        RegisterMessage = dto.RegisterMessage,
                        RegisteredTime = dto.RegisteredTime
                    }
                });

        recovery.Setup(t => t.UpdateRecoveryResultAsync(It.IsAny<SocialRecoveryResultGrainDto>())).ReturnsAsync(
            (SocialRecoveryResultGrainDto dto) => new GrainResultDto<RecoveryGrainDto>()
            {
                Success = true,
                Data = new RecoveryGrainDto()
                {
                    Id = dto.Id,
                    GrainId = dto.GrainId,
                    CaAddress = dto.CaAddress,
                    CaHash = dto.CaHash,
                    RecoverySuccess = dto.RecoverySuccess,
                    RecoveryMessage = dto.RecoveryMessage,
                    RecoveryTime = dto.RecoveryTime
                }
            });

        client.Setup(t => t.GetGrain<IRegisterGrain>(It.IsAny<string>(), null)).Returns(register.Object);
        client.Setup(t => t.GetGrain<IDeviceGrain>(It.IsAny<string>(), null)).Returns(device.Object);
        client.Setup(t => t.GetGrain<IRecoveryGrain>(It.IsAny<string>(), null)).Returns(recovery.Object);
        return client.Object;
    }
}