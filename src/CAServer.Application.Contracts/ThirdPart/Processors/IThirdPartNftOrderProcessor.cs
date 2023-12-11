using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart.Dtos;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace CAServer.ThirdPart.Processors;

public interface IThirdPartNftOrderProcessor : ISingletonDependency
{
   string ThirdPartName();
   Task<CommonResponseDto<Empty>> UpdateThirdPartNftOrderAsync(IThirdPartNftOrderUpdateRequest request);
   Task<CommonResponseDto<Empty>> RefreshThirdPartNftOrderAsync(Guid orderId);
   Task<CommonResponseDto<Empty>> NotifyNftReleaseAsync(Guid orderId);
   Task<CommonResponseDto<Empty>> SaveOrderSettlementAsync(Guid orderId, long? finishTime = null);
   Task<CommonResponseDto<Empty>> RefreshSettlementTransferAsync(Guid orderId, long blockHeight, long confirmedHeight);
   Task SettlementTransferAsync(Guid orderId);

}