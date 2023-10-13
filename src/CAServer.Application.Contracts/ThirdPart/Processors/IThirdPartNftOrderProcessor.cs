using System;
using System.Threading.Tasks;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart.Dtos;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Processors;

public interface IThirdPartNftOrderProcessor : ISingletonDependency
{
    public string ThirdPartName();
    public Task<CommonResponseDto<Empty>> UpdateThirdPartNftOrderAsync(IThirdPartNftOrderUpdateRequest input);
    public Task<CommonResponseDto<Empty>> RefreshThirdPartNftOrderAsync(Guid orderId);
    public Task<CommonResponseDto<Empty>> NotifyNftReleaseAsync(Guid orderId);
    public Task<CommonResponseDto<Empty>> RefreshSettlementTransfer(Guid orderId, long blockHeight, long confirmedHeight);
    public Task SettlementTransfer(Guid orderId);

}