using System;
using System.Threading.Tasks;
using CAServer.RedPackage.Dtos;
using JetBrains.Annotations;

namespace CAServer.RedPackage;

public interface IRedPackageAppService
{
    Task<GenerateRedPackageOutputDto> GenerateRedPackageAsync(GenerateRedPackageInputDto redPackageInput);
    Task<SendRedPackageOutputDto> SendRedPackageAsync(SendRedPackageInputDto input);
    Task<GetCreationResultOutputDto> GetCreationResultAsync(Guid sessionId);
    Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, int skipCount, int maxResultCount);
    Task<RedPackageConfigOutput> GetRedPackageConfigAsync([CanBeNull] string chainId,
        [CanBeNull] string token);
    Task<GrabRedPackageOutputDto> GrabRedPackageAsync(GrabRedPackageInputDto input);

    RedPackageTokenInfo GetRedPackageOption(String symbol, string chainId,out long maxCount,out string redpackageContractAddress);
}