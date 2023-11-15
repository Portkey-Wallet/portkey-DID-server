using System;
using System.Threading.Tasks;
using CAServer.RedPackage.Dtos;

namespace CAServer.RedPackage;

public interface IRedPackageAppService
{
    Task<GenerateRedPackageOutputDto> GenerateRedPackageAsync(GenerateRedPackageInputDto redPackageInput);
    Task<SendRedPackageOutputDto> SendRedPackageAsync(SendRedPackageInputDto input);
    Task<GetCreationResultOutputDto> GetCreationResultAsync(Guid sessionId);
    Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, int skipCount, int maxResultCount)
}