using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Google.Protobuf.WellKnownTypes;

namespace CAServer.ThirdPart.Processors;

public interface IThirdPartTreasuryProcessor
{
    ThirdPartNameType ThirdPartName();

    Task<TreasuryBaseResult> GetPriceAsync<TPriceInput>(TPriceInput priceInput) where TPriceInput : TreasuryBaseContext;

    Task NotifyOrderAsync<TOrderInput>(TOrderInput orderInput) where TOrderInput : TreasuryBaseContext;
    
    Task HandlePendingTreasuryOrderAsync(OrderDto rampOrder, PendingTreasuryOrderDto pendingTreasuryOrder);
    
    Task<CommonResponseDto<Empty>> RefreshTransferMultiConfirmAsync(Guid orderId, long blockHeight, long confirmedHeight);
    
    Task<CommonResponseDto<Empty>> CallBackAsync(Guid orderId);
    
}